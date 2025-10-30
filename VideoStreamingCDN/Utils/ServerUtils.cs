namespace VideoStreamingCDN.Utils;

public class ServerUtils : IServerUtils
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServerUtils> _logger;
    private readonly string[] _allowedExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

    public ServerUtils(IConfiguration configuration, ILogger<ServerUtils> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void InitializeDirectories()
    {
        var videoStoragePath = GetVideoStoragePath();

        if (!Directory.Exists(videoStoragePath))
        {
            Directory.CreateDirectory(videoStoragePath);
            _logger.LogInformation($"Created video storage directory: {videoStoragePath}");
        }

        _logger.LogInformation($"Video storage path initialized: {videoStoragePath}");
    }

    public string GetVideoStoragePath()
    {
        return _configuration["VideoStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "videos");
    }

    public string GetCdnBaseUrl()
    {
        return _configuration["CDN:BaseUrl"] ?? "https://cdn.example.com";
    }

    public bool IsValidVideoFormat(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    public long GetMaxFileSize()
    {
        return _configuration.GetValue<long>("VideoStorage:MaxFileSize", 52428800000); // Default 500MB
    }

    public async Task<bool> CleanupOldVideosAsync(TimeSpan olderThan)
    {
        return await Task.Run(() =>
        {
            try
            {
                var videoStoragePath = GetVideoStoragePath();
                if (!Directory.Exists(videoStoragePath))
                    return true;

                var cutoffTime = DateTime.UtcNow - olderThan;
                var directories = Directory.GetDirectories(videoStoragePath);
                int deletedCount = 0;

                foreach (var directory in directories)
                {
                    var dirInfo = new DirectoryInfo(directory);
                    if (dirInfo.CreationTimeUtc < cutoffTime)
                    {
                        try
                        {
                            Directory.Delete(directory, recursive: true);
                            deletedCount++;
                            _logger.LogInformation($"Deleted old video directory: {directory}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to delete directory: {directory}");
                        }
                    }
                }

                _logger.LogInformation($"Cleanup completed. Deleted {deletedCount} old video directories.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during video cleanup");
                return false;
            }
        });
    }
}
