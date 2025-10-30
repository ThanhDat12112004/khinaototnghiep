using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Data;
using MoviesApp.Models;
using System.Globalization;

namespace MoviesApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class AnalyticsController : Controller
    {
        private readonly WebMoviesDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsController(
            WebMoviesDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> ViewStats()
        {
            ViewData["Title"] = "Thống kê Xem Phim";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item active'>Thống kê Xem Phim</li>";

            // Get view statistics
            var totalViews = await _context.LichSuXems.CountAsync();
            var uniqueViewers = await _context.LichSuXems
                .Select(l => l.MaND)
                .Distinct()
                .CountAsync();

            // Most viewed movies
            var mostViewedMovies = await _context.LichSuXems
                .Include(l => l.TapPhim)
                .ThenInclude(t => t.Phim)
                .GroupBy(l => l.TapPhim.MaPhim)
                .Select(g => new
                {
                    PhimId = g.Key,
                    TenPhim = g.First().TapPhim.Phim.TenPhim,
                    TotalViews = g.Count(),
                    UniqueViewers = g.Select(x => x.MaND).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalViews)
                .Take(10)
                .ToListAsync();

            // Views by month (last 12 months)
            var viewsByMonth = await _context.LichSuXems
                .Where(l => l.ThoiDiemXem >= DateTime.Now.AddMonths(-12))
                .GroupBy(l => new { Year = l.ThoiDiemXem.Year, Month = l.ThoiDiemXem.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    ViewCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Popular genres
            var popularGenres = await _context.LichSuXems
                .Include(l => l.TapPhim)
                .ThenInclude(t => t.Phim)
                .ThenInclude(p => p.TheLoaiPhim)
                .Where(l => l.TapPhim.Phim.TheLoaiPhim != null)
                .GroupBy(l => l.TapPhim.Phim.TheLoaiPhim!.TenTheLoai)
                .Select(g => new
                {
                    TheLoai = g.Key,
                    ViewCount = g.Count()
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(10)
                .ToListAsync();

            var model = new
            {
                TotalViews = totalViews,
                UniqueViewers = uniqueViewers,
                MostViewedMovies = mostViewedMovies,
                ViewsByMonth = viewsByMonth,
                PopularGenres = popularGenres
            };

            return View(model);
        }

        public async Task<IActionResult> UserActivity()
        {
            ViewData["Title"] = "Hoạt động Người dùng";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item active'>Hoạt động Người dùng</li>";

            // Recent user registrations
            var recentRegistrations = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(50)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.CreatedAt,
                    u.IsActive
                })
                .ToListAsync();

            // Active users in last 30 days
            var activeUsers = await _context.LichSuXems
                .Where(l => l.ThoiDiemXem >= DateTime.Now.AddDays(-30))
                .Select(l => l.MaND)
                .Distinct()
                .CountAsync();

            // User registrations by month
            var registrationsByMonth = await _userManager.Users
                .Where(u => u.CreatedAt >= DateTime.Now.AddMonths(-12))
                .GroupBy(u => new { Year = u.CreatedAt.Year, Month = u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var model = new
            {
                RecentRegistrations = recentRegistrations,
                ActiveUsers = activeUsers,
                RegistrationsByMonth = registrationsByMonth
            };

            return View(model);
        }
    }
}
