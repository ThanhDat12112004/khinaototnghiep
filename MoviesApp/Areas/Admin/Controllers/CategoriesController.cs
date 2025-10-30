using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Data;
using MoviesApp.Models;

namespace MoviesApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class CategoriesController : Controller
    {
        private readonly WebMoviesDbContext _context;

        public CategoriesController(WebMoviesDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quản lý Danh mục & Thể loại";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item active'>Danh mục & Thể loại</li>";

            var categories = await _context.DanhMucs.ToListAsync();
            var genres = await _context.TheLoaiPhims.ToListAsync();

            var model = new
            {
                Categories = categories,
                Genres = genres
            };

            return View(model);
        }

        // Category Management
        [HttpPost]
        public async Task<IActionResult> CreateCategory(string tenDanhMuc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenDanhMuc))
                {
                    return Json(new { success = false, message = "Tên danh mục không được để trống" });
                }

                // Check if category already exists
                var exists = await _context.DanhMucs.AnyAsync(d => d.TenDM == tenDanhMuc);
                if (exists)
                {
                    return Json(new { success = false, message = "Danh mục đã tồn tại" });
                }

                // Generate new ID
                var lastCategory = await _context.DanhMucs
                    .OrderByDescending(d => d.MaDM)
                    .FirstOrDefaultAsync();

                string newId = "DM001";
                if (lastCategory != null && lastCategory.MaDM.StartsWith("DM"))
                {
                    var num = int.Parse(lastCategory.MaDM.Substring(2)) + 1;
                    newId = $"DM{num:000}";
                }

                var category = new DanhMuc
                {
                    MaDM = newId,
                    TenDM = tenDanhMuc
                };

                _context.DanhMucs.Add(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm danh mục thành công", category = new { category.MaDM, TenDanhMuc = category.TenDM } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCategory(string maDM, string tenDanhMuc)
        {
            try
            {
                var category = await _context.DanhMucs.FindAsync(maDM);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                category.TenDM = tenDanhMuc;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật danh mục thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(string maDM)
        {
            try
            {
                var category = await _context.DanhMucs.FindAsync(maDM);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Check if category is being used
                var hasMovies = await _context.Phims.AnyAsync(p => p.MaDM == maDM);
                if (hasMovies)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục này vì đang có phim sử dụng" });
                }

                _context.DanhMucs.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Genre Management
        [HttpPost]
        public async Task<IActionResult> CreateGenre(string tenTheLoai)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenTheLoai))
                {
                    return Json(new { success = false, message = "Tên thể loại không được để trống" });
                }

                // Check if genre already exists
                var exists = await _context.TheLoaiPhims.AnyAsync(t => t.TenTL == tenTheLoai);
                if (exists)
                {
                    return Json(new { success = false, message = "Thể loại đã tồn tại" });
                }

                // Generate new ID
                var lastGenre = await _context.TheLoaiPhims
                    .OrderByDescending(t => t.MaTL)
                    .FirstOrDefaultAsync();

                string newId = "TL001";
                if (lastGenre != null && lastGenre.MaTL.StartsWith("TL"))
                {
                    var num = int.Parse(lastGenre.MaTL.Substring(2)) + 1;
                    newId = $"TL{num:000}";
                }

                var genre = new TheLoaiPhim
                {
                    MaTL = newId,
                    TenTL = tenTheLoai
                };

                _context.TheLoaiPhims.Add(genre);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm thể loại thành công", genre = new { genre.MaTL, TenTheLoai = genre.TenTL } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGenre(string maTL, string tenTheLoai)
        {
            try
            {
                var genre = await _context.TheLoaiPhims.FindAsync(maTL);
                if (genre == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thể loại" });
                }

                genre.TenTL = tenTheLoai;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật thể loại thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGenre(string maTL)
        {
            try
            {
                var genre = await _context.TheLoaiPhims.FindAsync(maTL);
                if (genre == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thể loại" });
                }

                // Check if genre is being used
                var hasMovies = await _context.Phims.AnyAsync(p => p.MaTL == maTL);
                if (hasMovies)
                {
                    return Json(new { success = false, message = "Không thể xóa thể loại này vì đang có phim sử dụng" });
                }

                _context.TheLoaiPhims.Remove(genre);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa thể loại thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
