// Auth page JavaScript functions

function togglePassword() {
    const passwordInput = document.getElementById('passwordInput');
    const passwordIcon = document.getElementById('passwordIcon');
    
    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        passwordIcon.classList.remove('fa-eye');
        passwordIcon.classList.add('fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        passwordIcon.classList.remove('fa-eye-slash');
        passwordIcon.classList.add('fa-eye');
    }
}

function toggleConfirmPassword() {
    const passwordInput = document.getElementById('confirmPasswordInput');
    const passwordIcon = document.getElementById('confirmPasswordIcon');
    
    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        passwordIcon.classList.remove('fa-eye');
        passwordIcon.classList.add('fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        passwordIcon.classList.remove('fa-eye-slash');
        passwordIcon.classList.add('fa-eye');
    }
}

function initializeAuthForm() {
    // Form submission handling
    const authForm = document.querySelector('form');
    if (authForm) {
        authForm.addEventListener('submit', function(e) {
            const submitBtn = authForm.querySelector('button[type="submit"]');
            const btnText = submitBtn.querySelector('[id$="Text"]');
            const btnSpinner = submitBtn.querySelector('[id$="Spinner"]');
            
            if (submitBtn && btnText && btnSpinner) {
                submitBtn.disabled = true;
                btnText.classList.add('d-none');
                btnSpinner.classList.remove('d-none');
            }
        });
    }
    
    // Auto focus on first input
    const firstInput = document.querySelector('input:first-of-type');
    if (firstInput) {
        firstInput.focus();
    }
    
    // Enhanced form validation
    const inputs = document.querySelectorAll('input[required]');
    inputs.forEach(input => {
        input.addEventListener('blur', validateField);
        input.addEventListener('input', clearErrors);
    });
}

function validateField(e) {
    const field = e.target;
    const value = field.value.trim();
    
    if (field.name === 'Email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            showFieldError(field, 'Email không hợp lệ');
            return false;
        }
    }
    
    if (field.name === 'Password' && value) {
        if (value.length < 6) {
            showFieldError(field, 'Mật khẩu phải có ít nhất 6 ký tự');
            return false;
        }
    }
    
    if (field.name === 'ConfirmPassword' && value) {
        const passwordField = document.querySelector('input[name="Password"]');
        if (passwordField && value !== passwordField.value) {
            showFieldError(field, 'Mật khẩu xác nhận không khớp');
            return false;
        }
    }
    
    clearFieldError(field);
    return true;
}

function showFieldError(field, message) {
    clearFieldError(field);
    const errorElement = document.createElement('span');
    errorElement.className = 'field-validation-error';
    errorElement.textContent = message;
    field.parentNode.appendChild(errorElement);
    field.classList.add('is-invalid');
}

function clearFieldError(field) {
    const existingError = field.parentNode.querySelector('.field-validation-error');
    if (existingError) {
        existingError.remove();
    }
    field.classList.remove('is-invalid');
}

function clearErrors(e) {
    clearFieldError(e.target);
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeAuthForm();
});
