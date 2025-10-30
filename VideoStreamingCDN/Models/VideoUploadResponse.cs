namespace VideoStreamingCDN.Models;

public class VideoUploadResponse
{
    public string VideoId { get; set; } = string.Empty;
    public string CdnUrl { get; set; } = string.Empty;
}
