using VideoStreamingCDN.Models;

namespace VideoStreamingCDN.Services;

public interface IVideoProcessingService
{
    Task<VideoUploadResponse> ProcessVideoAsync(VideoUploadRequest request);
}
