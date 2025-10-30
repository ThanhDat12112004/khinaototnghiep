using MoviesApp.Models;

namespace MoviesApp.Models
{
    public class SearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<Phim> Movies { get; set; } = new List<Phim>();
        public int TotalResults { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        
        // Filter options
        public List<TheLoaiPhim> AvailableGenres { get; set; } = new List<TheLoaiPhim>();
        public List<QuocGia> AvailableCountries { get; set; } = new List<QuocGia>();
        
        // Selected filters - changed to string to match view
        public string? SelectedGenre { get; set; }
        public string? SelectedCountry { get; set; }
        public int? SelectedYear { get; set; }
        public string SortBy { get; set; } = "relevance";
        
        // Additional data
        public List<string> PopularSearches { get; set; } = new List<string>();
        public List<string> SearchSuggestions { get; set; } = new List<string>();
    }
}
