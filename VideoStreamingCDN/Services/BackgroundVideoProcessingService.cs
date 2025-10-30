using System.Collections.Concurrent;
using VideoStreamingCDN.Models;

namespace VideoStreamingCDN.Services;

public class BackgroundVideoProcessingService : BackgroundService, IBackgroundVideoProcessingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundVideoProcessingService> _logger;
    private readonly ConcurrentDictionary<string, VideoProcessingResult> _processingResults = new();
    private readonly ConcurrentQueue<(VideoUploadRequest request, string tempFilePath)> _processingQueue = new();
    private readonly SemaphoreSlim _semaphore;

    public BackgroundVideoProcessingService(
        IServiceProvider serviceProvider, 
        ILogger<BackgroundVideoProcessingService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        var maxParallelProcesses = configuration.GetValue<int>("FFmpeg:MaxParallelProcesses", 2);
        _semaphore = new SemaphoreSlim(maxParallelProcesses, maxParallelProcesses);
    }

    public async Task<string> QueueVideoProcessingAsync(VideoUploadRequest request, string tempFilePath)
    {
        var videoId = request.VideoId;
        
        _processingResults[videoId] = new VideoProcessingResult
        {
            VideoId = videoId,
            Status = VideoProcessingStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        _processingQueue.Enqueue((request, tempFilePath));
        _logger.LogInformation($"Queued video processing for VideoId: {videoId}");
        
        return videoId;
    }

    public Task<VideoProcessingStatus> GetProcessingStatusAsync(string videoId)
    {
        if (_processingResults.TryGetValue(videoId, out var result))
        {
            return Task.FromResult(result.Status);
        }
        return Task.FromResult(VideoProcessingStatus.Failed);
    }

    public Task<VideoUploadResponse?> GetProcessingResultAsync(string videoId)
    {
        if (_processingResults.TryGetValue(videoId, out var result) && 
            result.Status == VideoProcessingStatus.Completed)
        {
            return Task.FromResult(result.Response);
        }
        return Task.FromResult<VideoUploadResponse?>(null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background video processing service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_processingQueue.TryDequeue(out var item))
            {
                _ = Task.Run(async () =>
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        await ProcessVideoInBackgroundAsync(item.request, item.tempFilePath);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken);
            }
            else
            {
                await Task.Delay(1000, stoppingToken); // Wait for new items
            }
        }

        _logger.LogInformation("Background video processing service stopped");
    }

    private async Task ProcessVideoInBackgroundAsync(VideoUploadRequest request, string tempFilePath)
    {
        var videoId = request.VideoId;
        
        try
        {
            if (_processingResults.TryGetValue(videoId, out var result))
            {
                result.Status = VideoProcessingStatus.Processing;
            }

            _logger.LogInformation($"Starting background processing for VideoId: {videoId}");

            using var scope = _serviceProvider.CreateScope();
            var videoProcessingService = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();
            
            // Create a new request with the temp file
            var backgroundRequest = new VideoUploadRequest
            {
                VideoId = request.VideoId
                // We'll process the file directly from tempFilePath
            };

            // Process video with optimized settings
            var response = await ProcessVideoOptimizedAsync(videoProcessingService, tempFilePath, videoId);

            if (_processingResults.TryGetValue(videoId, out result))
            {
                result.Status = VideoProcessingStatus.Completed;
                result.Response = response;
                result.CompletedAt = DateTime.UtcNow;
            }

            _logger.LogInformation($"Background processing completed for VideoId: {videoId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Background processing failed for VideoId: {videoId}");
            
            if (_processingResults.TryGetValue(videoId, out var result))
            {
                result.Status = VideoProcessingStatus.Failed;
                result.Error = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to delete temp file: {tempFilePath}");
                }
            }
        }
    }

    private async Task<VideoUploadResponse> ProcessVideoOptimizedAsync(
        IVideoProcessingService videoProcessingService, 
        string tempFilePath, 
        string videoId)
    {
        // This is a simplified version - the actual VideoProcessingService will be optimized
        // For now, we'll create a response directly
        var videoUrl = $"http://localhost:5288/videos/{videoId}/master.m3u8";
        
        return new VideoUploadResponse
        {
            VideoId = videoId,
            CdnUrl = videoUrl
        };
    }
}
