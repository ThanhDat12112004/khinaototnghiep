using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MoviesApp.Services;
using MoviesApp.Data;
using MoviesApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MoviesApp.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TapPhimController : Controller
    {
        private readonly WebMoviesDbContext _context;
        private readonly ICDNVideoService _cdnVideoService;
        private readonly ILogger<TapPhimController> _logger;

        public TapPhimController(
            WebMoviesDbContext context,
            ICDNVideoService cdnVideoService,
            ILogger<TapPhimController> logger)
        {
            _context = context;
            _cdnVideoService = cdnVideoService;
            _logger = logger;
        }

        // GET: TapPhim - Redirect to Admin area for episode management
        public IActionResult Index(string maPhim)
        {
            if (string.IsNullOrEmpty(maPhim))
            {
                return RedirectToAction("Index", "Movies", new { area = "Admin" });
            }

            // Redirect to Admin area for episode management
            return RedirectToAction("Index", "TapPhim", new { area = "Admin", maPhim = maPhim });
        }

        // GET: TapPhim/Upload/5
        public async Task<IActionResult> Upload(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.TapPhims)
                .FirstOrDefaultAsync(m => m.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            ViewBag.Phim = phim;
            return View();
        }        // POST: TapPhim/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2147483648)] // 2GB limit
        [RequestFormLimits(MultipartBodyLengthLimit = 2147483648)]
        public async Task<IActionResult> Upload(string maPhim, int soTap, string? tenTap, IFormFile videoFile)
        {
            try
            {
                _logger.LogInformation($"Starting upload process for phim {maPhim}, tap {soTap}");
                
                if (string.IsNullOrEmpty(maPhim) || soTap <= 0 || videoFile == null || videoFile.Length == 0)
                {
                    TempData["Error"] = "Vui lòng điền đầy đủ thông tin và chọn file video";
                    return RedirectToAction(nameof(Upload), new { id = maPhim });
                }

                _logger.LogInformation($"Upload file info: Name={videoFile.FileName}, Size={videoFile.Length} bytes ({videoFile.Length / 1024 / 1024:F2} MB)");

                // Validate video file
                var allowedExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".webm" };
                var fileExtension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Chỉ hỗ trợ file video: MP4, AVI, MKV, MOV, WEBM";
                    return RedirectToAction(nameof(Upload), new { id = maPhim });
                }

                // Check file size (max 2GB)
                if (videoFile.Length > 2147483648) // 2GB
                {
                    TempData["Error"] = "File video không được vượt quá 2GB";
                    return RedirectToAction(nameof(Upload), new { id = maPhim });
                }

                // Generate unique episode ID
                var episodeId = $"{maPhim}_tap_{soTap:D3}_{DateTime.Now:yyyyMMdd_HHmmss}";                
                
                _logger.LogInformation($"Generated episode ID: {episodeId}");                // Check if episode already exists
                var existingEpisode = await _context.TapPhims
                    .FirstOrDefaultAsync(t => t.MaPhim == maPhim && t.SoTapThuTu == soTap);

                if (existingEpisode != null)
                {
                    // Nếu tập đã tồn tại nhưng chưa có VideoId, cập nhật thông tin video
                    if (string.IsNullOrEmpty(existingEpisode.VideoId))
                    {
                        _logger.LogInformation($"Updating existing episode with new video: {episodeId}");
                        
                        // Upload to CDN with progress tracking
                        _logger.LogInformation($"Starting CDN upload for existing episode...");
                        var uploadResult = await _cdnVideoService.UploadVideoAsync(videoFile, episodeId);
                        _logger.LogInformation($"CDN upload result: Success={uploadResult.Success}, Message={uploadResult.Message}");
                        
                        // Cập nhật thông tin video cho tập đã tồn tại
                        existingEpisode.VideoUrl = $"http://localhost:5288/api/v1/videos/{episodeId}/mp4";
                        existingEpisode.VideoId = episodeId;
                        existingEpisode.VideoStatus = uploadResult.Success ? "ready" : "processing";
                        existingEpisode.VideoFileSize = videoFile.Length;
                        existingEpisode.VideoFormat = "mp4";
                        existingEpisode.NgayPhatHanh = DateTime.Now;
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Episode updated successfully in database: {episodeId}");
                        TempData["Success"] = $"Cập nhật video cho tập {soTap} thành công! (File: {videoFile.Length / 1024 / 1024:F2} MB)";
                        
                        return RedirectToAction("Details", "Phim", new { id = maPhim });
                    }
                    else
                    {
                        TempData["Error"] = $"Tập {soTap} đã có video. Vui lòng chọn số tập khác hoặc xóa tập cũ.";
                        return RedirectToAction(nameof(Upload), new { id = maPhim });
                    }                }                
                // Upload to CDN for new episode
                _logger.LogInformation($"Starting upload for new episode {episodeId}");
                _logger.LogInformation($"File details: {videoFile.FileName} ({videoFile.Length / 1024 / 1024:F2} MB)");
                
                var uploadResultNew = await _cdnVideoService.UploadVideoAsync(videoFile, episodeId);
                _logger.LogInformation($"CDN upload completed: Success={uploadResultNew.Success}, Message={uploadResultNew.Message}");

                // Tạo TapPhim record ngay lập tức với VideoId và VideoUrl
                var tapPhim = new TapPhim
                {
                    MaTap = Guid.NewGuid().ToString().Substring(0, 10),
                    MaPhim = maPhim,
                    SoTapThuTu = soTap,
                    TenTap = string.IsNullOrEmpty(tenTap) ? $"Tập {soTap}" : tenTap,
                    VideoUrl = $"http://localhost:5288/api/v1/videos/{episodeId}/mp4", // Luôn set URL này
                    VideoId = episodeId, // Luôn set VideoId
                    VideoStatus = uploadResultNew.Success ? "ready" : "processing", // Set status dựa trên kết quả upload
                    VideoFileSize = videoFile.Length,
                    VideoFormat = "mp4",
                    NgayPhatHanh = DateTime.Now
                };

                _context.TapPhims.Add(tapPhim);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Episode record created in database: {tapPhim.MaTap}");                if (!uploadResultNew.Success)
                {
                    _logger.LogWarning($"Upload to CDN failed for episode {episodeId}: {uploadResultNew.Message}");
                    TempData["Warning"] = $"Upload tập {soTap} thành công nhưng video có thể chưa sẵn sàng. File: {videoFile.Length / 1024 / 1024:F2} MB. Lỗi CDN: {uploadResultNew.Message}";
                }
                else
                {
                    _logger.LogInformation($"Episode created successfully: {episodeId}");
                    TempData["Success"] = $"Upload tập {soTap} thành công! Video đã sẵn sàng. File: {videoFile.Length / 1024 / 1024:F2} MB";
                }
                
                return RedirectToAction("Details", "Phim", new { id = maPhim });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading episode for phim {MaPhim}, tap {SoTap}. File: {FileName} ({FileSize} MB)", 
                    maPhim, soTap, videoFile?.FileName, videoFile?.Length / 1024 / 1024);
                TempData["Error"] = $"Có lỗi xảy ra khi upload video: {ex.Message}. Vui lòng thử lại với file nhỏ hơn hoặc kiểm tra kết nối mạng.";
                return RedirectToAction(nameof(Upload), new { id = maPhim });
            }
        }

        // GET: TapPhim/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var tapPhim = await _context.TapPhims
                .Include(t => t.Phim)
                .FirstOrDefaultAsync(m => m.MaTap == id);

            if (tapPhim == null)
            {
                return NotFound();
            }

            // Get video info from CDN if available
            if (!string.IsNullOrEmpty(tapPhim.VideoId))
            {
                var videoInfo = await _cdnVideoService.GetVideoInfoAsync(tapPhim.VideoId);
                ViewBag.VideoInfo = videoInfo;
            }

            return View(tapPhim);
        }

        // GET: TapPhim/Play/5
        public async Task<IActionResult> Play(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var tapPhim = await _context.TapPhims
                .Include(t => t.Phim)
                .FirstOrDefaultAsync(m => m.MaTap == id);

            if (tapPhim == null)
            {
                return NotFound();
            }

            // Generate streaming URLs
            if (!string.IsNullOrEmpty(tapPhim.VideoId))
            {
                ViewBag.HLSUrl = await _cdnVideoService.GetStreamingUrlAsync(tapPhim.VideoId);
                ViewBag.MP4Url = $"http://localhost:5288/api/v1/videos/{tapPhim.VideoId}/mp4";
            }

            return View(tapPhim);
        }

        // GET: TapPhim/Player/5
        [HttpGet("Player/{id}")]
        public async Task<IActionResult> Player(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var tapPhim = await _context.TapPhims
                .Include(t => t.Phim)
                    .ThenInclude(p => p.TheLoaiPhim)
                .Include(t => t.Phim)
                    .ThenInclude(p => p.QuocGia)
                .Include(t => t.Phim)
                    .ThenInclude(p => p.DanhMuc)
                .FirstOrDefaultAsync(t => t.MaTap == id);

            if (tapPhim == null)
            {
                return NotFound();
            }

            // Get all episodes of this movie for navigation
            var allEpisodes = await _context.TapPhims
                .Where(t => t.MaPhim == tapPhim.MaPhim)
                .OrderBy(t => t.SoTapThuTu)
                .ToListAsync();

            // Generate video URLs
            string? primaryVideoUrl = null;
            string? hlsUrl = null;
            string? mp4Url = null;

            if (!string.IsNullOrEmpty(tapPhim.VideoId))
            {
                // Create HLS streaming URL from VideoId
                hlsUrl = GenerateHLSUrl(tapPhim.VideoId);
                
                // Create MP4 direct URL as fallback
                mp4Url = $"http://localhost:5288/api/v1/videos/{tapPhim.VideoId}/mp4";
                
                // Use HLS as primary (better for adaptive streaming)
                primaryVideoUrl = hlsUrl;
            }
            else if (!string.IsNullOrEmpty(tapPhim.VideoUrl))
            {
                // Check if the VideoUrl is already an HLS URL
                if (tapPhim.VideoUrl.Contains("master.m3u8") || tapPhim.VideoUrl.Contains(".m3u8"))
                {
                    hlsUrl = tapPhim.VideoUrl;
                    primaryVideoUrl = hlsUrl;
                }
                else
                {
                    primaryVideoUrl = tapPhim.VideoUrl;
                    mp4Url = tapPhim.VideoUrl;
                }
            }

            // Get current user for watch history
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;

            // Create view model
            var viewModel = new VideoPlayerViewModel
            {
                Phim = tapPhim.Phim,
                CurrentEpisode = tapPhim,
                Episodes = allEpisodes,
                VideoUrl = primaryVideoUrl ?? "",
                PosterUrl = tapPhim.Phim?.AnhPhim,
                IsUserLoggedIn = !string.IsNullOrEmpty(userId),
                UserId = userId
            };

            // Set additional video URLs for quality switching
            if (!string.IsNullOrEmpty(tapPhim.VideoId))
            {
                viewModel.VideoUrl720 = mp4Url; // For now, use same URL for different qualities
                viewModel.VideoUrl480 = mp4Url;
                viewModel.VideoUrl360 = mp4Url;
            }

            ViewBag.HLSUrl = hlsUrl;
            ViewBag.MP4Url = mp4Url;
            ViewBag.HasHLS = !string.IsNullOrEmpty(hlsUrl);
            ViewBag.IsHLSPrimary = primaryVideoUrl == hlsUrl;

            return View(viewModel);
        }

        // POST: TapPhim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var tapPhim = await _context.TapPhims.FindAsync(id);
            if (tapPhim != null)
            {
                // Delete from CDN if video exists
                if (!string.IsNullOrEmpty(tapPhim.VideoId))
                {
                    await _cdnVideoService.DeleteVideoAsync(tapPhim.VideoId);
                }

                _context.TapPhims.Remove(tapPhim);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Đã xóa tập phim thành công";
            }

            return RedirectToAction("Details", "Phim", new { id = tapPhim?.MaPhim });
        }

        // AJAX: Check video processing status
        [HttpGet]
        public async Task<IActionResult> CheckVideoStatus(string videoId)
        {
            if (string.IsNullOrEmpty(videoId))
            {
                return BadRequest();
            }

            var status = await _cdnVideoService.CheckVideoStatusAsync(videoId);
            
            // Update database if status changed
            var tapPhim = await _context.TapPhims
                .FirstOrDefaultAsync(t => t.VideoId == videoId);
            
            if (tapPhim != null && tapPhim.VideoStatus != status.Status)
            {
                tapPhim.VideoStatus = status.Status;
                await _context.SaveChangesAsync();
            }

            return Json(new { 
                status = status.Status,
                message = status.Message,
                success = true 
            });
        }

        // API: GET /TapPhim/GetVideoInfo/5
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVideoInfo(string id)
        {
            try
            {
                var tapPhim = await _context.TapPhims
                    .Include(t => t.Phim)
                    .FirstOrDefaultAsync(t => t.MaTap == id);

                if (tapPhim == null)
                {
                    return NotFound(new { error = "Tập phim không tồn tại" });
                }
                
                string? videoUrl = null;
                string videoType = "NONE";
                string? youtubeEmbedUrl = null;
                
                if (!string.IsNullOrEmpty(tapPhim.VideoId))
                {
                    // Sử dụng VideoId để tạo URL streaming từ CDN
                    videoUrl = $"http://localhost:5288/api/v1/videos/{tapPhim.VideoId}/mp4";
                    videoType = "MP4";
                }
                else if (!string.IsNullOrEmpty(tapPhim.VideoUrl))
                {
                    if (tapPhim.VideoUrl.Contains("youtube.com") || tapPhim.VideoUrl.Contains("youtu.be"))
                    {
                        // Xử lý YouTube URL
                        var youtubeUrl = tapPhim.VideoUrl;
                        string videoId = "";
                        
                        if (youtubeUrl.Contains("watch?v="))
                        {
                            videoId = youtubeUrl.Split("watch?v=")[1].Split("&")[0];
                        }
                        else if (youtubeUrl.Contains("youtu.be/"))
                        {
                            videoId = youtubeUrl.Split("youtu.be/")[1].Split("?")[0];
                        }
                        
                        if (!string.IsNullOrEmpty(videoId))
                        {
                            youtubeEmbedUrl = $"https://www.youtube.com/embed/{videoId}";
                            videoType = "YOUTUBE";
                        }
                        else
                        {
                            videoType = "ERROR";
                        }
                    }
                    else
                    {
                        // URL khác (có thể là MP4 direct link)
                        videoUrl = tapPhim.VideoUrl;
                        videoType = "DIRECT";
                    }
                }                var result = new
                {
                    maTap = tapPhim.MaTap,
                    tenTap = tapPhim.TenTap,
                    soTapThuTu = tapPhim.SoTapThuTu,
                    chiTiet = tapPhim.ChiTiet,
                    thoiLuongTap = tapPhim.ThoiLuongTap,
                    videoUrl = videoUrl,
                    videoId = tapPhim.VideoId,
                    videoType = videoType,
                    youtubeEmbedUrl = youtubeEmbedUrl,
                    hasVideo = !string.IsNullOrEmpty(videoUrl) || !string.IsNullOrEmpty(youtubeEmbedUrl),
                    tenPhim = tapPhim.Phim?.TenPhim
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting video info for episode {EpisodeId}", id);
                return BadRequest(new { error = "Lỗi khi lấy thông tin video" });
            }
        }

        // API: GET /TapPhim/GetVideoStreamInfo/5 - Enhanced video info with streaming details
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVideoStreamInfo(string id)
        {
            try
            {
                var tapPhim = await _context.TapPhims
                    .Include(t => t.Phim)
                    .FirstOrDefaultAsync(t => t.MaTap == id);

                if (tapPhim == null)
                {
                    return NotFound(new { error = "Tập phim không tồn tại" });
                }
                
                var result = new
                {
                    maTap = tapPhim.MaTap,
                    tenTap = tapPhim.TenTap,
                    soTapThuTu = tapPhim.SoTapThuTu,
                    chiTiet = tapPhim.ChiTiet,
                    thoiLuongTap = tapPhim.ThoiLuongTap,
                    videoId = tapPhim.VideoId,
                    videoStatus = tapPhim.VideoStatus,
                    tenPhim = tapPhim.Phim?.TenPhim,
                    
                    // Video URLs
                    videoUrls = new
                    {
                        primary = !string.IsNullOrEmpty(tapPhim.VideoId) 
                            ? GenerateHLSUrl(tapPhim.VideoId)
                            : tapPhim.VideoUrl,
                        hls = !string.IsNullOrEmpty(tapPhim.VideoId) 
                            ? GenerateHLSUrl(tapPhim.VideoId)
                            : (tapPhim.VideoUrl?.Contains(".m3u8") == true ? tapPhim.VideoUrl : null),
                        mp4_fallback = !string.IsNullOrEmpty(tapPhim.VideoId) 
                            ? $"http://localhost:5288/api/v1/videos/{tapPhim.VideoId}/mp4"
                            : (!tapPhim.VideoUrl?.Contains(".m3u8") == true ? tapPhim.VideoUrl : null),
                        mp4_1080p = !string.IsNullOrEmpty(tapPhim.VideoId) 
                            ? $"http://localhost:5288/videos/{tapPhim.VideoId}/1080p.m3u8" 
                            : null,
                        mp4_720p = !string.IsNullOrEmpty(tapPhim.VideoId) 
                            ? $"http://localhost:5288/videos/{tapPhim.VideoId}/720p.m3u8" 
                            : null,
                        mp4_480p = !string.IsNullOrEmpty(tapPhim.VideoId) 
                            ? $"http://localhost:5288/videos/{tapPhim.VideoId}/480p.m3u8" 
                            : null
                    },
                    
                    // Player configuration
                    playerConfig = new
                    {
                        autoplay = false,
                        muted = false,
                        preload = "metadata",
                        playbackRates = new[] { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 },
                        controls = true,
                        fluid = true,
                        responsive = true
                    },
                    
                    hasVideo = !string.IsNullOrEmpty(tapPhim.VideoId) || !string.IsNullOrEmpty(tapPhim.VideoUrl),
                    isReady = tapPhim.VideoStatus == "ready"
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting video stream info for episode {EpisodeId}", id);
                return BadRequest(new { error = "Lỗi khi lấy thông tin video" });
            }
        }

        // Test endpoint để kiểm tra kết nối CDN
        [HttpGet("test-cdn")]
        public async Task<IActionResult> TestCDN()
        {
            try
            {
                _logger.LogInformation("Testing CDN connection...");
                
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await httpClient.GetAsync("http://localhost:5288/");
                _logger.LogInformation($"CDN Response: {response.StatusCode}");
                
                return Json(new { 
                    success = true, 
                    message = "CDN connection test completed",
                    statusCode = response.StatusCode.ToString(),
                    cdnUrl = "http://localhost:5288"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CDN connection test failed");
                return Json(new { 
                    success = false, 
                    message = "CDN connection failed: " + ex.Message 
                });
            }
        }

        // Test HLS endpoint để kiểm tra kết nối với server streaming
        [HttpGet("test-hls/{videoId}")]
        public async Task<IActionResult> TestHLS(string videoId)
        {
            try
            {
                _logger.LogInformation("Testing HLS connection for video {VideoId}", videoId);
                
                var hlsUrl = GenerateHLSUrl(videoId);
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await httpClient.GetAsync(hlsUrl);
                var content = await response.Content.ReadAsStringAsync();
                
                return Json(new { 
                    success = response.IsSuccessStatusCode, 
                    message = "HLS connection test completed",
                    statusCode = response.StatusCode.ToString(),
                    hlsUrl = hlsUrl,
                    contentLength = content.Length,
                    isM3U8 = content.Contains("#EXTM3U")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HLS connection test failed for video {VideoId}", videoId);
                return Json(new { 
                    success = false, 
                    message = "HLS connection failed: " + ex.Message,
                    hlsUrl = GenerateHLSUrl(videoId)
                });
            }
        }

        // API: POST /TapPhim/SaveWatchProgress - Save user's watch progress
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveWatchProgress([FromBody] SaveWatchProgressRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Find or create watch history record
                var watchHistory = await _context.LichSuXems
                    .FirstOrDefaultAsync(l => l.MaND == userId && l.MaTap == request.EpisodeId);

                if (watchHistory == null)
                {
                    // Create new watch history
                    watchHistory = new LichSuXem
                    {
                        MaLS = Guid.NewGuid().ToString().Substring(0, 10),
                        MaND = userId,
                        MaTap = request.EpisodeId,
                        ThoiDiemXem = DateTime.Now,
                        ThoiGianXem = request.CurrentTime
                    };
                    _context.LichSuXems.Add(watchHistory);
                }
                else
                {
                    // Update existing record
                    watchHistory.ThoiDiemXem = DateTime.Now;
                    watchHistory.ThoiGianXem = request.CurrentTime;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Watch progress saved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving watch progress for episode {EpisodeId}", request.EpisodeId);
                return BadRequest(new { success = false, error = "Failed to save progress" });
            }
        }

        // API: GET /TapPhim/GetWatchProgress/5 - Get user's watch progress for episode
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetWatchProgress(string id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var watchHistory = await _context.LichSuXems
                    .FirstOrDefaultAsync(l => l.MaND == userId && l.MaTap == id);

                if (watchHistory == null)
                {
                    return Ok(new WatchProgressResponse { HasProgress = false });
                }

                // Get episode info for duration calculation
                var episode = await _context.TapPhims.FindAsync(id);
                var progressPercentage = 0.0;
                
                if (episode?.ThoiLuong.HasValue == true && episode.ThoiLuong > 0)
                {
                    progressPercentage = (double)watchHistory.ThoiGianXem / (episode.ThoiLuong.Value * 60) * 100;
                }

                return Ok(new WatchProgressResponse
                {
                    HasProgress = true,
                    CurrentTime = watchHistory.ThoiGianXem,
                    IsCompleted = progressPercentage >= 90, // Consider 90%+ as completed
                    LastWatched = watchHistory.ThoiDiemXem,
                    ProgressPercentage = Math.Min(progressPercentage, 100)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting watch progress for episode {EpisodeId}", id);
                return BadRequest(new { error = "Failed to get progress" });
            }
        }

        // Helper method to generate HLS URL from VideoId
        private string GenerateHLSUrl(string videoId)
        {
            // Use the full VideoId for HLS URL (e.g., episode_1750884049951_lmgiw6sy0)
            return $"http://localhost:5288/videos/{videoId}/master.m3u8";
        }
    }
}
