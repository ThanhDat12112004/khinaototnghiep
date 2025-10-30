namespace VideoStreamingCDN.Models;

public class VideoUploadRequest
{
    public string VideoId { get; set; } = string.Empty;
    public IFormFile VideoFile { get; set; } = null!;
}
