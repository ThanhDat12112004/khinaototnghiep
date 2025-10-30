// Profile page JavaScript functions

document.addEventListener('DOMContentLoaded', function() {
    initializeProfileNavigation();
    initializeProfileForms();
});

function initializeProfileNavigation() {
    const navItems = document.querySelectorAll('.nav-item');
    const sections = document.querySelectorAll('.profile-section');
    
    navItems.forEach(item => {
        item.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetSection = this.getAttribute('data-section');
            
            // Update active nav item
            navItems.forEach(nav => nav.classList.remove('active'));
            this.classList.add('active');
            
            // Show target section
            sections.forEach(section => section.classList.remove('active'));
            const target = document.getElementById(targetSection);
            if (target) {
                target.classList.add('active');
            }
        });
    });
}

function initializeProfileForms() {
    // Profile form handling
    const profileForm = document.getElementById('profileForm');
    if (profileForm) {
        profileForm.addEventListener('submit', function(e) {
            e.preventDefault();
            updateProfile();
        });
    }
    
    // Change password form handling
    const changePasswordForm = document.getElementById('changePasswordForm');
    if (changePasswordForm) {
        changePasswordForm.addEventListener('submit', function(e) {
            e.preventDefault();
            changePassword();
        });
    }
}

function toggleEditMode() {
    const form = document.getElementById('profileForm');
    const inputs = form.querySelectorAll('input, textarea');
    const editBtn = document.querySelector('.btn-edit');
    const formActions = document.querySelector('.form-actions');
    
    const isReadonly = inputs[0].hasAttribute('readonly');
    
    if (isReadonly) {
        // Enable editing
        inputs.forEach(input => {
            if (input.name !== 'Email') { // Keep email readonly
                input.removeAttribute('readonly');
            }
        });
        editBtn.style.display = 'none';
        formActions.classList.remove('d-none');
    } else {
        // Disable editing
        inputs.forEach(input => input.setAttribute('readonly', true));
        editBtn.style.display = 'inline-block';
        formActions.classList.add('d-none');
    }
}

async function updateProfile() {
    const form = document.getElementById('profileForm');
    const formData = new FormData(form);
    
    try {
        const response = await fetch('/Account/UpdateProfile', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            showNotification('Cập nhật thông tin thành công!', 'success');
            toggleEditMode(); // Return to readonly mode
        } else {
            showNotification(result.message || 'Có lỗi xảy ra', 'error');
        }
    } catch (error) {
        console.error('Error updating profile:', error);
        showNotification('Có lỗi xảy ra khi cập nhật thông tin', 'error');
    }
}

async function changePassword() {
    const form = document.getElementById('changePasswordForm');
    const formData = new FormData(form);
    
    // Basic validation
    const currentPassword = formData.get('CurrentPassword');
    const newPassword = formData.get('NewPassword');
    const confirmPassword = formData.get('ConfirmPassword');
    
    if (!currentPassword || !newPassword || !confirmPassword) {
        showNotification('Vui lòng điền đầy đủ thông tin', 'error');
        return;
    }
    
    if (newPassword !== confirmPassword) {
        showNotification('Mật khẩu xác nhận không khớp', 'error');
        return;
    }
    
    if (newPassword.length < 6) {
        showNotification('Mật khẩu phải có ít nhất 6 ký tự', 'error');
        return;
    }
    
    try {
        const response = await fetch('/Account/ChangePassword', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        
        if (result.success) {
            showNotification('Đổi mật khẩu thành công!', 'success');
            form.reset();
        } else {
            showNotification(result.message || 'Có lỗi xảy ra', 'error');
        }
    } catch (error) {
        console.error('Error changing password:', error);
        showNotification('Có lỗi xảy ra khi đổi mật khẩu', 'error');
    }
}

async function removeFromWatchlist(movieId) {
    if (!confirm('Bạn có chắc muốn xóa phim này khỏi danh sách yêu thích?')) {
        return;
    }
    
    try {
        const response = await fetch(`/Api/PhimApi/RemoveFromWatchlist/${movieId}`, {
            method: 'DELETE'
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Remove movie card from DOM
            const movieCard = document.querySelector(`[data-movie-id="${movieId}"]`);
            if (movieCard) {
                movieCard.remove();
            }
            
            showNotification('Đã xóa khỏi danh sách yêu thích', 'success');
            
            // Check if watchlist is empty
            const moviesGrid = document.querySelector('.movies-grid');
            if (moviesGrid && moviesGrid.children.length === 0) {
                moviesGrid.innerHTML = `
                    <div class="empty-state">
                        <i class="fas fa-heart-broken"></i>
                        <h4>Chưa có phim yêu thích</h4>
                        <p>Hãy thêm những bộ phim bạn yêu thích vào danh sách này</p>
                        <a href="/Phim" class="btn btn-primary">Khám phá phim</a>
                    </div>
                `;
            }
        } else {
            showNotification(result.message || 'Có lỗi xảy ra', 'error');
        }
    } catch (error) {
        console.error('Error removing from watchlist:', error);
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
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

// Handle notification settings toggle
document.addEventListener('change', function(e) {
    if (e.target.type === 'checkbox' && e.target.closest('.notification-settings')) {
        updateNotificationSetting(e.target);
    }
});

async function updateNotificationSetting(checkbox) {
    const settingName = checkbox.closest('.setting-item').querySelector('h5').textContent;
    const isEnabled = checkbox.checked;
    
    try {
        const response = await fetch('/Account/UpdateNotificationSettings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                settingName: settingName,
                enabled: isEnabled
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showNotification(`Đã ${isEnabled ? 'bật' : 'tắt'} thông báo ${settingName.toLowerCase()}`, 'success');
        } else {
            // Revert checkbox state
            checkbox.checked = !isEnabled;
            showNotification('Có lỗi xảy ra khi cập nhật cài đặt', 'error');
        }
    } catch (error) {
        console.error('Error updating notification setting:', error);
        checkbox.checked = !isEnabled;
        showNotification('Có lỗi xảy ra', 'error');
    }
}

// Avatar upload handling
function handleAvatarUpload() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    input.onchange = async function(e) {
        const file = e.target.files[0];
        if (file) {
            const formData = new FormData();
            formData.append('avatarFile', file);
            
            try {
                const response = await fetch('/Account/UpdateAvatar', {
                    method: 'POST',
                    body: formData
                });
                
                const result = await response.json();
                
                if (result.success) {
                    // Update avatar image
                    const avatarImg = document.querySelector('.avatar-image');
                    const avatarPlaceholder = document.querySelector('.avatar-placeholder');
                    
                    if (avatarImg) {
                        avatarImg.src = result.avatarUrl;
                    } else if (avatarPlaceholder) {
                        avatarPlaceholder.outerHTML = `<img src="${result.avatarUrl}" alt="Avatar" class="avatar-image" />`;
                    }
                    
                    showNotification('Cập nhật ảnh đại diện thành công!', 'success');
                } else {
                    showNotification(result.message || 'Có lỗi xảy ra', 'error');
                }
            } catch (error) {
                console.error('Error uploading avatar:', error);
                showNotification('Có lỗi xảy ra khi tải ảnh', 'error');
            }
        }
    };
    input.click();
}

// Add click handler for avatar to upload new one
document.addEventListener('DOMContentLoaded', function() {
    const profileAvatar = document.querySelector('.profile-avatar');
    if (profileAvatar) {
        profileAvatar.style.cursor = 'pointer';
        profileAvatar.title = 'Nhấp để thay đổi ảnh đại diện';
        profileAvatar.addEventListener('click', handleAvatarUpload);
    }
});
