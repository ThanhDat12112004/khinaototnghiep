using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Models;
using MoviesApp.Data;

namespace MoviesApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly WebMoviesDbContext _context;

    public HomeController(ILogger<HomeController> logger, WebMoviesDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Lấy danh sách phim phổ biến
            var phimPhoBien = await _context.Phims
                .Include(p => p.TheLoaiPhim)
                .Include(p => p.QuocGia)
                .Include(p => p.DanhMuc)
                .Include(p => p.TapPhims)
                .OrderByDescending(p => p.NgayTao)
                .Take(20)
                .ToListAsync();

            return View(phimPhoBien);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            // Trả về view với danh sách rỗng nếu có lỗi
            return View(new List<Phim>());
        }
    }

    public async Task<IActionResult> GetTrendingMovies()
    {
        try
        {
            var trendingMovies = await _context.Phims
                .Include(p => p.TheLoaiPhim)
                .Include(p => p.QuocGia)
                .OrderByDescending(p => p.DiemImdb)
                .Take(10)
                .Select(p => new
                {
                    p.MaPhim,
                    p.TenPhim,
                    p.AnhPhim,
                    p.DiemImdb,
                    p.NamPhatHanh,
                    p.DoTuoi,
                    TheLoai = p.TheLoaiPhim != null ? p.TheLoaiPhim.TenTheLoai : "",
                    QuocGia = p.QuocGia != null ? p.QuocGia.TenQG : ""
                })
                .ToListAsync();

            return Json(trendingMovies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading trending movies");
            return Json(new List<object>());
        }
    }

    public async Task<IActionResult> GetNewReleases()
    {
        try
        {
            var newReleases = await _context.Phims
                .Include(p => p.TheLoaiPhim)
                .Include(p => p.QuocGia)
                .OrderByDescending(p => p.NgayTao)
                .Take(10)
                .Select(p => new
                {
                    p.MaPhim,
                    p.TenPhim,
                    p.AnhPhim,
                    p.DiemImdb,
                    p.NamPhatHanh,
                    p.DoTuoi,
                    TheLoai = p.TheLoaiPhim != null ? p.TheLoaiPhim.TenTheLoai : "",
                    QuocGia = p.QuocGia != null ? p.QuocGia.TenQG : ""
                })
                .ToListAsync();

            return Json(newReleases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading new releases");
            return Json(new List<object>());
        }
    }

    public async Task<IActionResult> GetMoviesByGenre(string genre)
    {
        try
        {
            var movies = await _context.Phims
                .Include(p => p.TheLoaiPhim)
                .Include(p => p.QuocGia)
                .Where(p => p.TheLoaiPhim != null && p.TheLoaiPhim.TenTheLoai.Contains(genre))
                .OrderByDescending(p => p.DiemImdb)
                .Take(10)
                .Select(p => new
                {
                    p.MaPhim,
                    p.TenPhim,
                    p.AnhPhim,
                    p.DiemImdb,
                    p.NamPhatHanh,
                    p.DoTuoi,
                    TheLoai = p.TheLoaiPhim.TenTheLoai,
                    QuocGia = p.QuocGia != null ? p.QuocGia.TenQG : ""
                })
                .ToListAsync();

            return Json(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading movies by genre: {Genre}", genre);
            return Json(new List<object>());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult VideoPlayerDemo()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
