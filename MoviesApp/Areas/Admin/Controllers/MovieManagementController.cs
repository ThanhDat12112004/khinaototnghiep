using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Data;
using MoviesApp.Models;
using MoviesApp.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace MoviesApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class MovieManagementController : Controller
    {
        private readonly WebMoviesDbContext _context;
        private readonly IUserActivityService _userActivityService;
        private readonly OMDbService _omdbService;
        private readonly ILogger<MovieManagementController> _logger;

        public MovieManagementController(
            WebMoviesDbContext context,
            IUserActivityService userActivityService,
            OMDbService omdbService,
            ILogger<MovieManagementController> logger)
        {
            _context = context;
            _userActivityService = userActivityService;
            _omdbService = omdbService;
            _logger = logger;
        }

        // GET: Admin/MovieManagement - Unified movie management dashboard
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, 
            string? filterGenre = null, string? filterCountry = null, string? filterCategory = null)
        {
            try
            {
                var query = _context.Phims
                    .Include(p => p.QuocGia)
                    .Include(p => p.TheLoaiPhim)
                    .Include(p => p.DanhMuc)
                    .Include(p => p.TapPhims)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.TenPhim.Contains(search) || 
                                           (p.MoTaPhim != null && p.MoTaPhim.Contains(search)) ||
                                           (p.DaoDien != null && p.DaoDien.Contains(search)));
                }

                if (!string.IsNullOrEmpty(filterGenre))
                {
                    query = query.Where(p => p.MaTL == filterGenre);
                }

                if (!string.IsNullOrEmpty(filterCountry))
                {
                    query = query.Where(p => p.MaQG == filterCountry);
                }

                if (!string.IsNullOrEmpty(filterCategory))
                {
                    query = query.Where(p => p.MaDM == filterCategory);
                }

                var totalItems = await query.CountAsync();
                var movies = await query
                    .OrderByDescending(p => p.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Load filter options
                ViewBag.Genres = await _context.TheLoaiPhims
                    .Select(g => new SelectListItem
                    {
                        Value = g.MaTL,
                        Text = g.TenTL
                    }).ToListAsync();

                ViewBag.Countries = await _context.QuocGias
                    .Select(c => new SelectListItem
                    {
                        Value = c.MaQG,
                        Text = c.TenQG
                    }).ToListAsync();

                ViewBag.Categories = await _context.DanhMucs
                    .Select(c => new SelectListItem
                    {
                        Value = c.MaDM,
                        Text = c.TenDM
                    }).ToListAsync();

                // Pagination info
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                ViewBag.TotalItems = totalItems;
                ViewBag.Search = search;
                ViewBag.FilterGenre = filterGenre;
                ViewBag.FilterCountry = filterCountry;
                ViewBag.FilterCategory = filterCategory;

                // Statistics for dashboard
                ViewBag.TotalMovies = await _context.Phims.CountAsync();
                ViewBag.TotalEpisodes = await _context.TapPhims.CountAsync();
                ViewBag.MoviesThisMonth = await _context.Phims
                    .Where(p => p.NgayTao.Month == DateTime.Now.Month && p.NgayTao.Year == DateTime.Now.Year)
                    .CountAsync();

                return View(movies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading movie management dashboard");
                TempData["Error"] = "Có lỗi xảy ra khi tải dữ liệu phim.";
                return View(new List<Phim>());
            }
        }

        // GET: Admin/MovieManagement/Create
        public async Task<IActionResult> Create()
        {
            await LoadViewBagData();
            return View();
        }

        // POST: Admin/MovieManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phim phim)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    phim.MaPhim = await GenerateMovieId();
                    phim.NgayTao = DateTime.Now;
                    _context.Add(phim);
                    await _context.SaveChangesAsync();

                    await _userActivityService.LogActivityAsync(User.Identity?.Name ?? "Unknown", "CREATE_MOVIE", $"Tạo phim mới: {phim.TenPhim}");
                    
                    TempData["Success"] = "Tạo phim mới thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating movie");
                TempData["Error"] = "Có lỗi xảy ra khi tạo phim mới.";
            }

            await LoadViewBagData();
            return View(phim);
        }

        // GET: Admin/MovieManagement/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims.FindAsync(id);
            if (phim == null)
            {
                return NotFound();
            }

            await LoadViewBagData();
            return View(phim);
        }

        // POST: Admin/MovieManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Phim phim)
        {
            if (id != phim.MaPhim)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    _context.Update(phim);
                    await _context.SaveChangesAsync();

                    await _userActivityService.LogActivityAsync(User.Identity?.Name ?? "Unknown", "UPDATE_MOVIE", $"Cập nhật phim: {phim.TenPhim}");
                    
                    TempData["Success"] = "Cập nhật phim thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PhimExists(phim.MaPhim))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie");
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật phim.";
            }

            await LoadViewBagData();
            return View(phim);
        }

        // GET: Admin/MovieManagement/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.QuocGia)
                .Include(p => p.TheLoaiPhim)
                .Include(p => p.DanhMuc)
                .Include(p => p.TapPhims)
                .FirstOrDefaultAsync(m => m.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // GET: Admin/MovieManagement/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phim = await _context.Phims
                .Include(p => p.QuocGia)
                .Include(p => p.TheLoaiPhim)
                .Include(p => p.DanhMuc)
                .FirstOrDefaultAsync(m => m.MaPhim == id);

            if (phim == null)
            {
                return NotFound();
            }

            return View(phim);
        }

        // POST: Admin/MovieManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var phim = await _context.Phims.FindAsync(id);
                if (phim != null)
                {
                    _context.Phims.Remove(phim);
                    await _context.SaveChangesAsync();

                    await _userActivityService.LogActivityAsync(User.Identity?.Name ?? "Unknown", "DELETE_MOVIE", $"Xóa phim: {phim.TenPhim}");
                    
                    TempData["Success"] = "Xóa phim thành công!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie");
                TempData["Error"] = "Có lỗi xảy ra khi xóa phim.";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoint for quick actions
        [HttpPost]
        public async Task<IActionResult> QuickAction(string movieId, string action)
        {
            try
            {
                var movie = await _context.Phims.FindAsync(movieId);
                if (movie == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim" });
                }

                switch (action.ToLower())
                {
                    case "toggle_status":
                        // Assuming there's a status field
                        // movie.IsActive = !movie.IsActive;
                        break;
                    case "feature":
                        // movie.IsFeatured = !movie.IsFeatured;
                        break;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thao tác thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick action");
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        // Load OMDb data
        [HttpPost]
        public async Task<IActionResult> LoadFromOMDb(string imdbId)
        {
            try
            {
                // Note: You need to implement GetMovieByIdAsync method in OMDbService
                // var movieData = await _omdbService.GetMovieByIdAsync(imdbId);
                // if (movieData != null)
                // {
                //     return Json(new { success = true, data = movieData });
                // }
                return Json(new { success = false, message = "Chức năng OMDb đang được phát triển" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading OMDb data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu từ OMDb" });
            }
        }

        private async Task LoadViewBagData()
        {
            ViewBag.MaDM = new SelectList(await _context.DanhMucs.ToListAsync(), "MaDM", "TenDM");
            ViewBag.MaQG = new SelectList(await _context.QuocGias.ToListAsync(), "MaQG", "TenQG");
            ViewBag.MaTL = new SelectList(await _context.TheLoaiPhims.ToListAsync(), "MaTL", "TenTL");
        }

        private bool PhimExists(string id)
        {
            return _context.Phims.Any(e => e.MaPhim == id);
        }

        private async Task<string> GenerateMovieId()
        {
            string newId;
            do
            {
                newId = "P" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(100, 999).ToString();
            }
            while (await _context.Phims.AnyAsync(p => p.MaPhim == newId));
            
            return newId;
        }
    }
}
