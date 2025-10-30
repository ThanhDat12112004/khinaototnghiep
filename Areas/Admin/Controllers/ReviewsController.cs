using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApp.Data;
using MoviesApp.Models;

namespace MoviesApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class ReviewsController : Controller
    {
        private readonly WebMoviesDbContext _context;

        public ReviewsController(WebMoviesDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            ViewData["Title"] = "Quản lý Đánh giá & Bình luận";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item active'>Đánh giá & Bình luận</li>";

            // Get comments with pagination
            var totalComments = await _context.BinhLuans.CountAsync();
            var comments = await _context.BinhLuans
                .Include(b => b.NguoiDung)
                .Include(b => b.TapPhim)
                .ThenInclude(t => t.Phim)
                .OrderByDescending(b => b.NgayBL)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalComments = totalComments;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalComments / pageSize);

            return View(comments);
        }

        public async Task<IActionResult> Ratings(int page = 1, int pageSize = 20)
        {
            ViewData["Title"] = "Quản lý Đánh giá";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item'><a href='/Admin/Reviews'>Đánh giá & Bình luận</a></li><li class='breadcrumb-item active'>Đánh giá</li>";

            // Get ratings with pagination
            var totalRatings = await _context.DanhGias.CountAsync();
            var ratings = await _context.DanhGias
                .Include(d => d.NguoiDung)
                .Include(d => d.TapPhim)
                .ThenInclude(t => t.Phim)
                .OrderByDescending(d => d.ThoiGianDG)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalRatings = totalRatings;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRatings / pageSize);

            return View(ratings);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(string id)
        {
            try
            {
                var comment = await _context.BinhLuans.FindAsync(id);
                if (comment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bình luận" });
                }

                _context.BinhLuans.Remove(comment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa bình luận thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRating(string id)
        {
            try
            {
                var rating = await _context.DanhGias.FindAsync(id);
                if (rating == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đánh giá" });
                }

                _context.DanhGias.Remove(rating);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa đánh giá thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> CommentDetails(string id)
        {
            var comment = await _context.BinhLuans
                .Include(b => b.NguoiDung)
                .Include(b => b.TapPhim)
                .ThenInclude(t => t.Phim)
                .FirstOrDefaultAsync(b => b.MaBL == id);

            if (comment == null)
            {
                return NotFound();
            }

            return PartialView("_CommentDetails", comment);
        }
    }
}
