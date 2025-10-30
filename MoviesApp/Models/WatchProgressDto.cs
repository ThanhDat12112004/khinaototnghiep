namespace MoviesApp.Models
{
    public class SaveWatchProgressRequest
    {
        public string EpisodeId { get; set; } = string.Empty;
        public int CurrentTime { get; set; } // Time in seconds
        public bool IsCompleted { get; set; } = false;
        public int? Duration { get; set; } // Total video duration in seconds
    }

    public class WatchProgressResponse
    {
        public bool HasProgress { get; set; }
        public int CurrentTime { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastWatched { get; set; }
        public double ProgressPercentage { get; set; }
    }
}
