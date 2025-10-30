// Search page JavaScript functions

document.addEventListener('DOMContentLoaded', function() {
    initializeSearch();
    initializeFilters();
});

function initializeSearch() {
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                submitSearch();
            }
        });
    }
}

function initializeFilters() {
    const filterSelects = document.querySelectorAll('.filter-select');
    filterSelects.forEach(select => {
        select.addEventListener('change', applyFilters);
    });
}

function submitSearch() {
    const form = document.querySelector('.search-form');
    if (form) {
        form.submit();
    }
}

function applyFilters() {
    const url = new URL(window.location);
    const params = new URLSearchParams(url.search);
    
    // Get current search query
    const query = params.get('q') || '';
    
    // Get filter values
    const genre = document.querySelector('select[name="genre"]')?.value || '';
    const country = document.querySelector('select[name="country"]')?.value || '';
    const year = document.querySelector('select[name="year"]')?.value || '';
    const sort = document.querySelector('select[name="sort"]')?.value || 'relevance';
    
    // Build new URL
    const newParams = new URLSearchParams();
    if (query) newParams.set('q', query);
    if (genre) newParams.set('genre', genre);
    if (country) newParams.set('country', country);
    if (year) newParams.set('year', year);
    if (sort !== 'relevance') newParams.set('sort', sort);
    newParams.set('page', '1'); // Reset to first page
    
    // Navigate to new URL
    window.location.href = url.pathname + '?' + newParams.toString();
}

function clearFilters() {
    const url = new URL(window.location);
    const params = new URLSearchParams(url.search);
    const query = params.get('q') || '';
    
    // Navigate with only the search query
    const newUrl = query ? `${url.pathname}?q=${encodeURIComponent(query)}` : url.pathname;
    window.location.href = newUrl;
}

async function toggleWishlist(movieId, button) {
    try {
        const isActive = button.classList.contains('active');
        const action = isActive ? 'remove' : 'add';
        
        const response = await fetch(`/Api/PhimApi/${action}ToWatchlist/${movieId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                button.classList.toggle('active');
                
                // Show notification
                showNotification(
                    isActive ? 'Đã xóa khỏi danh sách yêu thích' : 'Đã thêm vào danh sách yêu thích',
                    'success'
                );
            } else {
                showNotification(result.message || 'Có lỗi xảy ra', 'error');
            }
        } else if (response.status === 401) {
            // User not logged in
            showLoginModal();
        } else {
            showNotification('Có lỗi xảy ra', 'error');
        }
    } catch (error) {
        console.error('Error toggling wishlist:', error);
        showNotification('Có lỗi xảy ra', 'error');
    }
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show`;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        border-radius: 8px;
        box-shadow: 0 4px 15px rgba(0,0,0,0.2);
    `;
    
    notification.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-triangle' : 'info-circle'} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(notification);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 3000);
}

function showLoginModal() {
    if (confirm('Bạn cần đăng nhập để sử dụng tính năng này. Chuyển đến trang đăng nhập?')) {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
    }
}

// Search suggestions functionality
let searchTimeout;
function setupSearchSuggestions() {
    const searchInput = document.querySelector('.search-input');
    if (!searchInput) return;
    
    searchInput.addEventListener('input', function() {
        const query = this.value.trim();
        
        clearTimeout(searchTimeout);
        
        if (query.length >= 2) {
            searchTimeout = setTimeout(() => {
                fetchSearchSuggestions(query);
            }, 300);
        } else {
            hideSearchSuggestions();
        }
    });
    
    // Hide suggestions when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.search-form')) {
            hideSearchSuggestions();
        }
    });
}

async function fetchSearchSuggestions(query) {
    try {
        const response = await fetch(`/Api/PhimApi/SearchSuggestions?q=${encodeURIComponent(query)}`);
        if (response.ok) {
            const suggestions = await response.json();
            showSearchSuggestions(suggestions);
        }
    } catch (error) {
        console.error('Error fetching search suggestions:', error);
    }
}

function showSearchSuggestions(suggestions) {
    hideSearchSuggestions(); // Remove existing suggestions
    
    if (!suggestions || suggestions.length === 0) return;
    
    const searchForm = document.querySelector('.search-form');
    const suggestionsContainer = document.createElement('div');
    suggestionsContainer.className = 'search-suggestions-dropdown';
    suggestionsContainer.style.cssText = `
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: rgba(0, 0, 0, 0.95);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 0 0 12px 12px;
        border-top: none;
        max-height: 300px;
        overflow-y: auto;
        z-index: 1000;
    `;
    
    suggestions.forEach(suggestion => {
        const item = document.createElement('div');
        item.className = 'suggestion-item';
        item.style.cssText = `
            padding: 0.75rem 1rem;
            color: rgba(255, 255, 255, 0.8);
            cursor: pointer;
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
            transition: all 0.2s ease;
        `;
        item.textContent = suggestion;
        
        item.addEventListener('mouseenter', function() {
            this.style.backgroundColor = 'rgba(229, 9, 20, 0.2)';
            this.style.color = 'white';
        });
        
        item.addEventListener('mouseleave', function() {
            this.style.backgroundColor = 'transparent';
            this.style.color = 'rgba(255, 255, 255, 0.8)';
        });
        
        item.addEventListener('click', function() {
            document.querySelector('.search-input').value = suggestion;
            hideSearchSuggestions();
            submitSearch();
        });
        
        suggestionsContainer.appendChild(item);
    });
    
    searchForm.style.position = 'relative';
    searchForm.appendChild(suggestionsContainer);
}

function hideSearchSuggestions() {
    const existing = document.querySelector('.search-suggestions-dropdown');
    if (existing) {
        existing.remove();
    }
}

// Initialize search suggestions when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    setupSearchSuggestions();
});

// Keyboard navigation for search results
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        hideSearchSuggestions();
    }
});

// Infinite scroll for search results (optional enhancement)
function setupInfiniteScroll() {
    let loading = false;
    let currentPage = parseInt(document.querySelector('[data-current-page]')?.dataset.currentPage || '1');
    const totalPages = parseInt(document.querySelector('[data-total-pages]')?.dataset.totalPages || '1');
    
    window.addEventListener('scroll', function() {
        if (loading || currentPage >= totalPages) return;
        
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const windowHeight = window.innerHeight;
        const documentHeight = document.documentElement.scrollHeight;
        
        if (scrollTop + windowHeight >= documentHeight - 1000) {
            loading = true;
            loadMoreResults();
        }
    });
    
    async function loadMoreResults() {
        try {
            const url = new URL(window.location);
            const params = new URLSearchParams(url.search);
            params.set('page', currentPage + 1);
            
            const response = await fetch(url.pathname + '?' + params.toString() + '&ajax=true');
            if (response.ok) {
                const html = await response.text();
                const tempDiv = document.createElement('div');
                tempDiv.innerHTML = html;
                
                const newMovies = tempDiv.querySelectorAll('.movie-card');
                const moviesGrid = document.querySelector('.movies-grid');
                
                newMovies.forEach(movie => {
                    moviesGrid.appendChild(movie);
                });
                
                currentPage++;
                loading = false;
                
                // Trigger AOS animation for new items
                if (typeof AOS !== 'undefined') {
                    AOS.refresh();
                }
            }
        } catch (error) {
            console.error('Error loading more results:', error);
            loading = false;
        }
    }
}
