// edit-user.js

document.addEventListener('DOMContentLoaded', function () {
    const originalUsername = document.getElementById('OriginalUsername')?.value;
    const userId = document.getElementById('UserId')?.value;
    const usernameInput = document.getElementById('Username');
    const usernameStatus = document.getElementById('usernameStatus');
    const submitBtn = document.getElementById('submitBtn');
    const passwordInput = document.getElementById('Password');
    const confirmPasswordInput = document.getElementById('ConfirmPassword');
    const form = document.getElementById('editUserForm');

    let formChanged = false;
    let usernameCheckTimeout;

    // Real-time username availability check
    if (usernameInput && usernameStatus) {
        usernameInput.addEventListener('input', function () {
            clearTimeout(usernameCheckTimeout);
            const username = this.value;

            if (username.length < 3) {
                usernameStatus.innerHTML = '<span class="info">Minimum 3 characters required</span>';
                if (submitBtn) submitBtn.disabled = true;
                return;
            }

            // If username hasn't changed, it's available
            if (username === originalUsername) {
                usernameStatus.innerHTML = '<span class="available"><i class="fas fa-check"></i> Current username</span>';
                if (submitBtn) submitBtn.disabled = false;
                return;
            }

            usernameCheckTimeout = setTimeout(function () {
                fetch(`/System/CheckUsername?username=${encodeURIComponent(username)}&excludeId=${userId}`)
                    .then(response => response.json())
                    .then(data => {
                        if (data.exists) {
                            usernameStatus.innerHTML = '<span class="unavailable"><i class="fas fa-times"></i> Username already taken</span>';
                            if (submitBtn) submitBtn.disabled = true;
                        } else {
                            usernameStatus.innerHTML = '<span class="available"><i class="fas fa-check"></i> Username available</span>';
                            if (submitBtn) submitBtn.disabled = false;
                        }
                    })
                    .catch(error => {
                        console.error('Error checking username:', error);
                    });
            }, 500);
        });
    }

    // Password validation
    function validatePasswords() {
        if (!passwordInput || !confirmPasswordInput || !submitBtn) return;

        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;

        // If both are empty, it's valid (no password change)
        if (!password && !confirmPassword) {
            submitBtn.disabled = false;
            return;
        }

        // If one is filled and the other is empty
        if ((password && !confirmPassword) || (!password && confirmPassword)) {
            submitBtn.disabled = true;
            return;
        }

        // Check password length
        if (password.length < 6) {
            submitBtn.disabled = true;
            return;
        }

        // Check if passwords match
        if (password !== confirmPassword) {
            submitBtn.disabled = true;
            return;
        }

        submitBtn.disabled = false;
    }

    if (passwordInput && confirmPasswordInput) {
        passwordInput.addEventListener('input', validatePasswords);
        confirmPasswordInput.addEventListener('input', validatePasswords);
    }

    // Password strength indicator
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            const password = this.value;

            // Remove existing indicator
            const existingIndicator = document.getElementById('passwordStrength');
            if (existingIndicator) existingIndicator.remove();

            // Only show strength if password is not empty
            if (password) {
                let strength = 0;
                if (password.length >= 6) strength++;
                if (password.match(/[a-z]/)) strength++;
                if (password.match(/[A-Z]/)) strength++;
                if (password.match(/[0-9]/)) strength++;
                if (password.match(/[^a-zA-Z0-9]/)) strength++;

                let strengthText = '';
                let strengthColor = '';

                switch (strength) {
                    case 0:
                    case 1:
                        strengthText = 'Weak';
                        strengthColor = '#dc3545';
                        break;
                    case 2:
                    case 3:
                        strengthText = 'Medium';
                        strengthColor = '#ffc107';
                        break;
                    case 4:
                    case 5:
                        strengthText = 'Strong';
                        strengthColor = '#28a745';
                        break;
                }

                const indicator = document.createElement('small');
                indicator.id = 'passwordStrength';
                indicator.className = 'text-muted';
                indicator.style.color = strengthColor;
                indicator.style.marginTop = '4px';
                indicator.style.display = 'block';
                indicator.innerHTML = `Password strength: ${strengthText}`;

                this.parentNode.appendChild(indicator);
            }
        });
    }

    // Track form changes
    if (form) {
        const formInputs = form.querySelectorAll('input, select, textarea');

        formInputs.forEach(input => {
            input.addEventListener('change', () => {
                formChanged = true;
            });
            input.addEventListener('input', () => {
                formChanged = true;
            });
        });

        // Form submission loading state
        form.addEventListener('submit', function () {
            if (submitBtn) {
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Updating...';
                submitBtn.disabled = true;
            }
            formChanged = false;
        });
    }

    // Confirm before leaving with unsaved changes
    window.addEventListener('beforeunload', function (e) {
        if (formChanged) {
            e.preventDefault();
            e.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
        }
    });

    // Initialize validation on page load
    validatePasswords();
});