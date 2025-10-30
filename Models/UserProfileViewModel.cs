using MoviesApp.Models;
using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Models
{
    public class UserProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<Phim> FavoriteMovies { get; set; } = new List<Phim>();
        public List<LichSuXem> WatchHistory { get; set; } = new List<LichSuXem>();
        public bool EmailNotifications { get; set; } = true;
        public bool NewMovieNotifications { get; set; } = true;
        public bool NewEpisodeNotifications { get; set; } = true;
        public bool MarketingEmails { get; set; } = false;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }
}
