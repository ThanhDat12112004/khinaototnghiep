namespace MoviesApp.Models
{
    public class VideoPlayerViewModel
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string? VideoUrl720 { get; set; }
        public string? VideoUrl480 { get; set; }
        public string? VideoUrl360 { get; set; }
        public string? PosterUrl { get; set; }
        public bool Autoplay { get; set; } = false;
        public bool Muted { get; set; } = false;
        public bool ShowDownloadOptions { get; set; } = false;
        public List<SubtitleTrack>? Subtitles { get; set; }
        public List<DownloadLink>? DownloadLinks { get; set; }
        
        // Additional properties needed by Watch.cshtml
        public Phim Phim { get; set; } = new Phim();
        public TapPhim CurrentEpisode { get; set; } = new TapPhim();
        public List<TapPhim> Episodes { get; set; } = new List<TapPhim>();
        public List<Phim> RelatedMovies { get; set; } = new List<Phim>();
        public bool IsUserLoggedIn { get; set; } = false;
        public string? UserId { get; set; }
        public int? WatchedProgress { get; set; } // Progress in seconds
        public string? LastWatchedEpisode { get; set; }
    }

    public class SubtitleTrack
    {
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    public class DownloadLink
    {
        public string Quality { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string? Filename { get; set; }
    }
}
