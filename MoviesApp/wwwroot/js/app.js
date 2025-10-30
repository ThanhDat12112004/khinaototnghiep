/* ===== ROPHIM.ME INSPIRED INTERACTIONS ===== */

// Global app object
window.CCFilmApp = {
    init: function() {
        this.initNavbar();
        this.initSearch();
        this.initWishlist();
        this.initLazyLoading();
        this.initScrollEffects();
        this.initTooltips();
        this.initMovieCards();
    },

    // Navbar interactions
    initNavbar: function() {
        const navbar = document.getElementById('mainNavbar');
        if (!navbar) return;

        // Navbar scroll effect
        let lastScrollY = window.scrollY;
        let ticking = false;

        function updateNavbar() {
            const scrollY = window.scrollY;
            
            if (scrollY > 50) {
                navbar.classList.add('navbar-scrolled');
            } else {
                navbar.classList.remove('navbar-scrolled');
            }
            
            // Hide/show navbar on scroll
            if (scrollY > lastScrollY && scrollY > 100) {
                navbar.style.transform = 'translateY(-100%)';
            } else {
                navbar.style.transform = 'translateY(0)';
            }
            
            lastScrollY = scrollY;
            ticking = false;
        }

        function onScroll() {
            if (!ticking) {
                requestAnimationFrame(updateNavbar);
                ticking = true;
            }
        }

        window.addEventListener('scroll', onScroll, { passive: true });

        // Mobile menu toggle
        const toggleBtn = navbar.querySelector('.navbar-toggler');
        const navMenu = navbar.querySelector('.navbar-collapse');
        
        if (toggleBtn && navMenu) {
            toggleBtn.addEventListener('click', function() {
                navMenu.classList.toggle('show');
                document.body.classList.toggle('nav-open');
            });

            // Close menu when clicking outside
            document.addEventListener('click', function(e) {
                if (!navbar.contains(e.target) && navMenu.classList.contains('show')) {
                    navMenu.classList.remove('show');
                    document.body.classList.remove('nav-open');
                }
            });
        }
    },

    // Search functionality
    initSearch: function() {
        const searchInput = document.querySelector('.search-input');
        const searchContainer = document.querySelector('.search-container');
        
        if (!searchInput || !searchContainer) return;

        let searchTimeout;

        searchInput.addEventListener('focus', function() {
            searchContainer.classList.add('search-focused');
        });

        searchInput.addEventListener('blur', function() {
            setTimeout(() => {
                searchContainer.classList.remove('search-focused');
            }, 200);
        });

        // Real-time search suggestions (if needed)
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            const query = this.value.trim();
            
            if (query.length >= 2) {
                searchTimeout = setTimeout(() => {
                    // Implement search suggestions here
                    console.log('Search query:', query);
                }, 300);
            }
        });

        // Search form submission
        const searchForm = searchInput.closest('form');
        if (searchForm) {
            searchForm.addEventListener('submit', function(e) {
                const query = searchInput.value.trim();
                if (!query) {
                    e.preventDefault();
                    searchInput.focus();
                }
            });
        }
    },

    // Wishlist functionality
    initWishlist: function() {
        // Get wishlist from localStorage
        const getWishlist = () => {
            return JSON.parse(localStorage.getItem('movieWishlist') || '[]');
        };

        // Save wishlist to localStorage
        const saveWishlist = (wishlist) => {
            localStorage.setItem('movieWishlist', JSON.stringify(wishlist));
        };

        // Update wishlist button state
        const updateWishlistButton = (btn, movieId, isInWishlist) => {
            const icon = btn.querySelector('i');
            if (isInWishlist) {
                icon.classList.remove('far');
                icon.classList.add('fas');
                btn.classList.add('active');
                btn.setAttribute('title', 'Xóa khỏi yêu thích');
            } else {
                icon.classList.remove('fas');
                icon.classList.add('far');
                btn.classList.remove('active');
                btn.setAttribute('title', 'Thêm vào yêu thích');
            }
        };

        // Initialize wishlist buttons
        const wishlistButtons = document.querySelectorAll('[data-movie-id]');
        const currentWishlist = getWishlist();

        wishlistButtons.forEach(btn => {
            const movieId = btn.getAttribute('data-movie-id');
            if (movieId) {
                const isInWishlist = currentWishlist.includes(movieId);
                updateWishlistButton(btn, movieId, isInWishlist);
            }
        });

        // Handle wishlist button clicks
        document.addEventListener('click', function(e) {
            const btn = e.target.closest('[data-movie-id]');
            if (!btn) return;

            e.preventDefault();
            
            const movieId = btn.getAttribute('data-movie-id');
            if (!movieId) return;

            let wishlist = getWishlist();
            const isInWishlist = wishlist.includes(movieId);

            if (isInWishlist) {
                wishlist = wishlist.filter(id => id !== movieId);
                this.showNotification('Đã xóa khỏi danh sách yêu thích', 'success');
            } else {
                wishlist.push(movieId);
                this.showNotification('Đã thêm vào danh sách yêu thích', 'success');
            }

            saveWishlist(wishlist);
            updateWishlistButton(btn, movieId, !isInWishlist);

            // Update wishlist counter if exists
            const wishlistCounter = document.querySelector('.wishlist-counter');
            if (wishlistCounter) {
                wishlistCounter.textContent = wishlist.length;
            }
        }.bind(this));
    },

    // Lazy loading for images
    initLazyLoading: function() {
        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        
                        // Handle data-src attribute
                        if (img.dataset.src) {
                            img.src = img.dataset.src;
                            img.removeAttribute('data-src');
                        }
                        
                        img.classList.remove('lazy');
                        img.classList.add('loaded');
                        observer.unobserve(img);
                    }
                });
            }, {
                rootMargin: '50px 0px',
                threshold: 0.01
            });

            // Observe all images with loading="lazy" or .lazy class
            document.querySelectorAll('img[loading="lazy"], img.lazy').forEach(img => {
                imageObserver.observe(img);
            });
        }
    },

    // Scroll effects
    initScrollEffects: function() {
        // Parallax effect for hero section
        const heroSection = document.querySelector('.hero-section');
        if (heroSection) {
            window.addEventListener('scroll', () => {
                const scrolled = window.pageYOffset;
                const parallax = scrolled * 0.5;
                heroSection.style.transform = `translate3d(0, ${parallax}px, 0)`;
            }, { passive: true });
        }

        // Fade in elements on scroll
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-fade-in-up');
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        // Observe elements with animation classes
        document.querySelectorAll('.movie-section, .movie-card, .filter-section').forEach(el => {
            observer.observe(el);
        });
    },

    // Initialize tooltips
    initTooltips: function() {
        // Simple tooltip implementation
        const tooltipTriggers = document.querySelectorAll('[title]');
        
        tooltipTriggers.forEach(trigger => {
            trigger.addEventListener('mouseenter', function(e) {
                const tooltip = document.createElement('div');
                tooltip.className = 'custom-tooltip';
                tooltip.textContent = this.getAttribute('title');
                
                // Remove title to prevent default tooltip
                this.setAttribute('data-original-title', this.getAttribute('title'));
                this.removeAttribute('title');
                
                document.body.appendChild(tooltip);
                
                // Position tooltip
                const rect = this.getBoundingClientRect();
                tooltip.style.left = rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2) + 'px';
                tooltip.style.top = rect.top - tooltip.offsetHeight - 10 + 'px';
                
                // Show tooltip
                setTimeout(() => tooltip.classList.add('show'), 10);
                
                // Store reference
                this._tooltip = tooltip;
            });
            
            trigger.addEventListener('mouseleave', function() {
                if (this._tooltip) {
                    this._tooltip.remove();
                    this._tooltip = null;
                }
                
                // Restore title
                if (this.getAttribute('data-original-title')) {
                    this.setAttribute('title', this.getAttribute('data-original-title'));
                    this.removeAttribute('data-original-title');
                }
            });
        });
    },

    // Movie card interactions
    initMovieCards: function() {
        const movieCards = document.querySelectorAll('.movie-card');
        
        movieCards.forEach(card => {
            // Hover effects
            card.addEventListener('mouseenter', function() {
                this.classList.add('hovered');
                
                // Add stagger effect to nearby cards
                const siblings = Array.from(this.parentNode.children);
                const index = siblings.indexOf(this);
                
                siblings.forEach((sibling, i) => {
                    if (Math.abs(i - index) <= 1 && sibling !== this) {
                        sibling.style.transform = 'scale(0.95)';
                        sibling.style.opacity = '0.7';
                    }
                });
            });
            
            card.addEventListener('mouseleave', function() {
                this.classList.remove('hovered');
                
                // Reset nearby cards
                const siblings = Array.from(this.parentNode.children);
                siblings.forEach(sibling => {
                    sibling.style.transform = '';
                    sibling.style.opacity = '';
                });
            });

            // Keyboard navigation
            card.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    const link = this.querySelector('a');
                    if (link) {
                        link.click();
                    }
                }
            });
        });
    },

    // Show notification
    showNotification: function(message, type = 'info') {
        // Remove existing notifications
        const existing = document.querySelector('.notification');
        if (existing) {
            existing.remove();
        }

        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${type === 'success' ? 'check-circle' : 'info-circle'}"></i>
                <span>${message}</span>
                <button class="notification-close">
                    <i class="fas fa-times"></i>
                </button>
            </div>
        `;

        document.body.appendChild(notification);

        // Show notification
        setTimeout(() => notification.classList.add('show'), 10);

        // Auto hide after 3 seconds
        const autoHide = setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, 3000);

        // Manual close
        notification.querySelector('.notification-close').addEventListener('click', () => {
            clearTimeout(autoHide);
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        });
    },

    // Smooth scroll to element
    scrollTo: function(target, offset = 80) {
        const element = typeof target === 'string' ? document.querySelector(target) : target;
        if (!element) return;

        const targetPosition = element.offsetTop - offset;
        window.scrollTo({
            top: targetPosition,
            behavior: 'smooth'
        });
    },

    // Debounce function
    debounce: function(func, wait, immediate) {
        let timeout;
        return function executedFunction() {
            const context = this;
            const args = arguments;
            
            const later = function() {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            
            const callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            
            if (callNow) func.apply(context, args);
        };
    },

    // Throttle function
    throttle: function(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        }
    }
};

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    CCFilmApp.init();
});

// Handle page visibility change
document.addEventListener('visibilitychange', function() {
    if (document.visibilityState === 'visible') {
        // Refresh data if needed
        console.log('Page is now visible');
    }
});

// Handle online/offline status
window.addEventListener('online', function() {
    CCFilmApp.showNotification('Kết nối internet đã được khôi phục', 'success');
});

window.addEventListener('offline', function() {
    CCFilmApp.showNotification('Mất kết nối internet', 'warning');
});

// Export for use in other scripts
window.CCFilmApp = CCFilmApp;
