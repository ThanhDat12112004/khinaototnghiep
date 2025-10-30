namespace VideoStreamingCDN.Utils;

public interface IServerUtils
{
    void InitializeDirectories();
    string GetVideoStoragePath();
    string GetCdnBaseUrl();
    bool IsValidVideoFormat(string fileName);
    long GetMaxFileSize();
    Task<bool> CleanupOldVideosAsync(TimeSpan olderThan);
}
