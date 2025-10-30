using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Data;
using MoviesApp.Models;

namespace MoviesApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class CountriesController : Controller
    {
        private readonly WebMoviesDbContext _context;

        public CountriesController(WebMoviesDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quản lý Quốc gia";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item active'>Quốc gia</li>";

            var countries = await _context.QuocGias.ToListAsync();
            return View(countries);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string tenQuocGia)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenQuocGia))
                {
                    return Json(new { success = false, message = "Tên quốc gia không được để trống" });
                }

                // Check if country already exists
                var exists = await _context.QuocGias.AnyAsync(q => q.TenQG == tenQuocGia);
                if (exists)
                {
                    return Json(new { success = false, message = "Quốc gia đã tồn tại" });
                }

                // Generate new ID
                var lastCountry = await _context.QuocGias
                    .OrderByDescending(q => q.MaQG)
                    .FirstOrDefaultAsync();

                string newId = "QG001";
                if (lastCountry != null && lastCountry.MaQG.StartsWith("QG"))
                {
                    var num = int.Parse(lastCountry.MaQG.Substring(2)) + 1;
                    newId = $"QG{num:000}";
                }

                var country = new QuocGia
                {
                    MaQG = newId,
                    TenQG = tenQuocGia
                };

                _context.QuocGias.Add(country);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm quốc gia thành công", country = new { country.MaQG, TenQuocGia = country.TenQG } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(string maQG, string tenQuocGia)
        {
            try
            {
                var country = await _context.QuocGias.FindAsync(maQG);
                if (country == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy quốc gia" });
                }

                country.TenQG = tenQuocGia;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật quốc gia thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string maQG)
        {
            try
            {
                var country = await _context.QuocGias.FindAsync(maQG);
                if (country == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy quốc gia" });
                }

                // Check if country is being used
                var hasMovies = await _context.Phims.AnyAsync(p => p.MaQG == maQG);
                if (hasMovies)
                {
                    return Json(new { success = false, message = "Không thể xóa quốc gia này vì đang có phim sử dụng" });
                }

                _context.QuocGias.Remove(country);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa quốc gia thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
