//login.js
let isRedirecting = false;

function login() {
    let username = document.getElementById('username').value.trim();
    let password = document.getElementById('password').value;
    let rememberMe = document.getElementById('rememberMe').checked;
    let loginBtn = document.getElementById('loginBtn');
    let errorMsg = document.getElementById('errorMessage');
    let errorText = document.getElementById('errorText');

    if (!username || !password) {
        showError('Please enter both username and password');
        return;
    }

    let originalText = loginBtn.innerHTML;
    loginBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Logging in...';
    loginBtn.disabled = true;
    errorMsg.style.display = 'none';

    fetch('/System/Login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: JSON.stringify({ username, password, rememberMe })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                isRedirecting = true;
                window.location.href = data.redirectUrl || '/System/Parameters';
            } else {
                showError(data.message || 'Invalid username or password');
                loginBtn.innerHTML = originalText;
                loginBtn.disabled = false;
            }
        })
        .catch(error => {
            console.error('Login error:', error);
            showError('Error connecting to server');
            loginBtn.innerHTML = originalText;
            loginBtn.disabled = false;
        });
}

function showError(message) {
    let errorMsg = document.getElementById('errorMessage');
    let errorText = document.getElementById('errorText');
    if (errorMsg && errorText) {
        errorText.innerText = message;
        errorMsg.style.display = 'flex';
        let contentBox = document.querySelector('.content-box');
        if (contentBox) {
            contentBox.style.animation = 'shake 0.5s ease';
            setTimeout(() => contentBox.style.animation = '', 500);
        }
    }
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

function forgotPassword() {
    alert('Please contact your system administrator to reset your password.');
}

document.addEventListener('DOMContentLoaded', function () {
    let passwordInput = document.getElementById('password');
    if (passwordInput) {
        passwordInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                login();
            }
        });
    }
});