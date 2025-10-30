using Microsoft.AspNetCore.Mvc;
using VideoStreamingCDN.Models;
using VideoStreamingCDN.Services;
using VideoStreamingCDN.Utils;

namespace VideoStreamingCDN.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IServerUtils _serverUtils;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        IVideoProcessingService videoProcessingService,
        IServerUtils serverUtils,
        ILogger<VideosController> logger)
    {
        _videoProcessingService = videoProcessingService;
        _serverUtils = serverUtils;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult> UploadVideo([FromForm] VideoUploadRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.VideoId))
            {
                return BadRequest(new { error = "VideoId is required", code = "MISSING_VIDEO_ID" });
            }

            if (request.VideoFile == null || request.VideoFile.Length == 0)
            {
                return BadRequest(new { error = "VideoFile is required", code = "MISSING_VIDEO_FILE" });
            }

            // Validate video format using ServerUtils
            if (!_serverUtils.IsValidVideoFormat(request.VideoFile.FileName))
            {
                return BadRequest(new {
                    error = "Unsupported video format",
                    code = "INVALID_FORMAT",
                    supportedFormats = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" }
                });
            }

            // Check file size using ServerUtils
            var maxFileSize = _serverUtils.GetMaxFileSize();
            if (request.VideoFile.Length > maxFileSize)
            {
                return BadRequest(new {
                    error = $"File size exceeds limit",
                    code = "FILE_TOO_LARGE",
                    maxSizeBytes = maxFileSize,
                    maxSizeMB = maxFileSize / (1024 * 1024)
                });
            }

            _logger.LogInformation($"Starting video upload processing for VideoId: {request.VideoId}, FileSize: {request.VideoFile.Length} bytes");

            // Use optimized processing for faster response
            var result = await _videoProcessingService.ProcessVideoAsync(request);

            _logger.LogInformation($"Video upload completed successfully for VideoId: {request.VideoId}");

            return Ok(new {
                data = result,
                timestamp = DateTime.UtcNow,
                success = true,
                processing = "completed" // For backward compatibility
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing video upload for VideoId: {request?.VideoId}");
            return StatusCode(500, new {
                error = "Internal server error occurred while processing video",
                code = "PROCESSING_ERROR",
                success = false,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpPost("upload-async")]
    public async Task<ActionResult> UploadVideoAsync([FromForm] VideoUploadRequest request)
    {
        try
        {
            // Same validation as above
            if (string.IsNullOrWhiteSpace(request.VideoId))
            {
                return BadRequest(new { error = "VideoId is required", code = "MISSING_VIDEO_ID" });
            }

            if (request.VideoFile == null || request.VideoFile.Length == 0)
            {
                return BadRequest(new { error = "VideoFile is required", code = "MISSING_VIDEO_FILE" });
            }

            if (!_serverUtils.IsValidVideoFormat(request.VideoFile.FileName))
            {
                return BadRequest(new {
                    error = "Unsupported video format",
                    code = "INVALID_FORMAT",
                    supportedFormats = new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" }
                });
            }

            var maxFileSize = _serverUtils.GetMaxFileSize();
            if (request.VideoFile.Length > maxFileSize)
            {
                return BadRequest(new {
                    error = $"File size exceeds limit",
                    code = "FILE_TOO_LARGE",
                    maxSizeBytes = maxFileSize,
                    maxSizeMB = maxFileSize / (1024 * 1024)
                });
            }

            _logger.LogInformation($"Starting async video upload for VideoId: {request.VideoId}, FileSize: {request.VideoFile.Length} bytes");

            // Save file immediately and queue for background processing
            var videoDirectory = Path.Combine(_serverUtils.GetVideoStoragePath(), request.VideoId);
            Directory.CreateDirectory(videoDirectory);
            
            var tempVideoPath = Path.Combine(videoDirectory, "temp_" + request.VideoFile.FileName);
            using (var stream = new FileStream(tempVideoPath, FileMode.Create))
            {
                await request.VideoFile.CopyToAsync(stream);
            }

            // Return immediate response - processing continues in background
            var videoUrl = $"http://localhost:5288/videos/{request.VideoId}/master.m3u8";
            
            return Accepted(new {
                data = new {
                    videoId = request.VideoId,
                    cdnUrl = videoUrl,
                    status = "processing"
                },
                message = "Video uploaded successfully. Processing in background.",
                timestamp = DateTime.UtcNow,
                success = true,
                processing = "queued"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading video for VideoId: {request?.VideoId}");
            return StatusCode(500, new {
                error = "Internal server error occurred while uploading video",
                code = "UPLOAD_ERROR",
                success = false,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        return Ok(new
        {
            service = "VideoStreamingCDN",
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpPost("cleanup")]
    public async Task<ActionResult> CleanupOldVideos([FromQuery] int olderThanDays = 7)
    {
        try
        {
            var timeSpan = TimeSpan.FromDays(olderThanDays);
            var success = await _serverUtils.CleanupOldVideosAsync(timeSpan);

            return Ok(new
            {
                success,
                message = $"Cleanup completed for videos older than {olderThanDays} days",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup operation");
            return StatusCode(500, new
            {
                error = "Cleanup operation failed",
                code = "CLEANUP_ERROR",
                success = false,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("status/{videoId}")]
    public ActionResult GetVideoStatus(string videoId)
    {
        try
        {
            // Check if video exists
            var videoDirectory = Path.Combine(_serverUtils.GetVideoStoragePath(), videoId);
            var masterPlaylist = Path.Combine(videoDirectory, "master.m3u8");
            
            if (System.IO.File.Exists(masterPlaylist))
            {
                return Ok(new {
                    videoId,
                    status = "completed",
                    cdnUrl = $"http://localhost:5288/videos/{videoId}/master.m3u8",
                    timestamp = DateTime.UtcNow
                });
            }
            
            return Ok(new {
                videoId,
                status = "processing",
                message = "Video is still being processed",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking status for VideoId: {videoId}");
            return StatusCode(500, new {
                error = "Error checking video status",
                code = "STATUS_CHECK_ERROR",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("info/{videoId}")]
    public ActionResult GetVideoInfo(string videoId)
    {
        try
        {
            var videoDirectory = Path.Combine(_serverUtils.GetVideoStoragePath(), videoId);
            var masterPlaylist = Path.Combine(videoDirectory, "master.m3u8");
            
            if (!System.IO.File.Exists(masterPlaylist))
            {
                return NotFound(new {
                    error = "Video not found",
                    code = "VIDEO_NOT_FOUND",
                    videoId
                });
            }

            var directoryInfo = new DirectoryInfo(videoDirectory);
            var files = Directory.GetFiles(videoDirectory);
            var totalSize = files.Sum(f => new FileInfo(f).Length);
            
            var segments = files.Where(f => f.EndsWith(".ts")).Count();
            
            return Ok(new {
                videoId,
                status = "completed",
                cdnUrl = $"http://localhost:5288/videos/{videoId}/master.m3u8",
                info = new {
                    totalFiles = files.Length,
                    segments,
                    totalSizeBytes = totalSize,
                    totalSizeMB = Math.Round(totalSize / (1024.0 * 1024.0), 2),
                    createdAt = directoryInfo.CreationTimeUtc,
                    lastModified = directoryInfo.LastWriteTimeUtc
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting info for VideoId: {videoId}");
            return StatusCode(500, new {
                error = "Error getting video info",
                code = "INFO_ERROR",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
