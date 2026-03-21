// create-user.js

document.addEventListener('DOMContentLoaded', function () {
    const usernameInput = document.getElementById('Username');
    const usernameStatus = document.getElementById('usernameStatus');
    const submitBtn = document.getElementById('submitBtn');
    const passwordInput = document.getElementById('Password');
    const form = document.getElementById('createUserForm');

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

            usernameCheckTimeout = setTimeout(function () {
                fetch(`/System/CheckUsername?username=${encodeURIComponent(username)}`)
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

    // Form submission loading state
    if (form && submitBtn) {
        form.addEventListener('submit', function () {
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating...';
            submitBtn.disabled = true;
        });
    }

    // Pre-fill form from query parameters (if coming from employees list)
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.has('firstName')) {
        const firstNameInput = document.getElementById('FirstName');
        if (firstNameInput) firstNameInput.value = urlParams.get('firstName');
    }
    if (urlParams.has('secondName')) {
        const secondNameInput = document.getElementById('SecondName');
        if (secondNameInput) secondNameInput.value = urlParams.get('secondName');
    }

    // Confirm before leaving with unsaved changes
    let formChanged = false;
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

        form.addEventListener('submit', function () {
            formChanged = false;
        });
    }

    window.addEventListener('beforeunload', function (e) {
        if (formChanged) {
            e.preventDefault();
            e.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
        }
    });
});