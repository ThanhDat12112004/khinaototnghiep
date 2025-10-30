using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApp.Models;

namespace MoviesApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class SettingsController : Controller
    {
        private readonly IConfiguration _configuration;

        public SettingsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Cài đặt Hệ thống";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item active'>Cài đặt Hệ thống</li>";

            var settings = new
            {
                ApplicationName = _configuration["AppSettings:ApplicationName"] ?? "Movies App",
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                ServerName = Environment.MachineName,
                Framework = ".NET 9.0",
                OMDbApiKey = !string.IsNullOrEmpty(_configuration["OMDbSettings:ApiKey"]) ? "Đã cấu hình" : "Chưa cấu hình"
            };

            return View(settings);
        }

        public IActionResult Database()
        {
            ViewData["Title"] = "Quản lý Database";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item'><a href='/Admin/Settings'>Cài đặt</a></li><li class='breadcrumb-item active'>Database</li>";

            return View();
        }

        public IActionResult Backup()
        {
            ViewData["Title"] = "Sao lưu & Phục hồi";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item'><a href='/Admin/Settings'>Cài đặt</a></li><li class='breadcrumb-item active'>Sao lưu</li>";

            return View();
        }

        public IActionResult Logs()
        {
            ViewData["Title"] = "Nhật ký Hệ thống";
            ViewData["Breadcrumb"] = "<li class='breadcrumb-item'><a href='/Admin'>Dashboard</a></li><li class='breadcrumb-item'><a href='/Admin/Settings'>Cài đặt</a></li><li class='breadcrumb-item active'>Nhật ký</li>";

            // Get log files from wwwroot/logs or Logs folder
            var logFiles = new List<object>();
            
            try
            {
                var logPaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Logs"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs")
                };

                foreach (var logPath in logPaths)
                {
                    if (Directory.Exists(logPath))
                    {
                        var files = Directory.GetFiles(logPath, "*.log")
                            .Concat(Directory.GetFiles(logPath, "*.txt"))
                            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                            .Take(20);

                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            logFiles.Add(new
                            {
                                Name = fileInfo.Name,
                                Size = fileInfo.Length,
                                LastModified = fileInfo.LastWriteTime,
                                Path = file
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Không thể đọc file log: " + ex.Message;
            }

            return View(logFiles);
        }

        [HttpPost]
        public IActionResult ClearCache()
        {
            try
            {
                // Clear any application cache here
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                TempData["Success"] = "Đã xóa cache thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa cache: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public IActionResult DownloadLog(string fileName)
        {
            try
            {
                var logPaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Logs", fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs", fileName)
                };

                foreach (var logPath in logPaths)
                {
                    if (System.IO.File.Exists(logPath))
                    {
                        var fileBytes = System.IO.File.ReadAllBytes(logPath);
                        return File(fileBytes, "text/plain", fileName);
                    }
                }

                return NotFound("File không tồn tại");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tải file: " + ex.Message;
                return RedirectToAction("Logs");
            }
        }
    }
}
