using VideoStreamingCDN.Models;

namespace VideoStreamingCDN.Services;

public interface IBackgroundVideoProcessingService
{
    Task<string> QueueVideoProcessingAsync(VideoUploadRequest request, string tempFilePath);
    Task<VideoProcessingStatus> GetProcessingStatusAsync(string videoId);
    Task<VideoUploadResponse?> GetProcessingResultAsync(string videoId);
}

public enum VideoProcessingStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}

public class VideoProcessingResult
{
    public string VideoId { get; set; } = string.Empty;
    public VideoProcessingStatus Status { get; set; }
    public VideoUploadResponse? Response { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
