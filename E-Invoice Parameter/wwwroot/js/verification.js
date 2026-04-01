// verification.js
let formChanged = false;
let isRedirecting = false;

window.addEventListener("beforeunload", function (e) {
    if (formChanged && !isRedirecting) {
        e.preventDefault();
        e.returnValue = '';
    }
});

function verifyIdentification() {
    let idInput = document.getElementById('identificationNo');
    let verifyBtn = document.getElementById('verifyBtn');
    let errorMsg = document.getElementById('errorMessage');
    let errorText = document.getElementById('errorText');

    if (!idInput || !verifyBtn || !errorMsg || !errorText) return;

    let idNumber = idInput.value.trim();

    if (idNumber === '') {
        errorText.innerText = 'Please enter your identification number';
        errorMsg.classList.add('show');
        let contentBox = document.querySelector('.content-box');
        if (contentBox) {
            contentBox.style.animation = 'shake 0.5s ease';
            setTimeout(() => contentBox.style.animation = '', 500);
        }
        return;
    }

    let originalText = verifyBtn.innerHTML;
    verifyBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Verifying...';
    verifyBtn.disabled = true;
    errorMsg.classList.remove('show');

    fetch(`/System/VerifyTin?tin=${encodeURIComponent(idNumber)}`)
        .then(response => response.json())
        .then(data => {
            if (!data.success || !data.verified) {
                verifyBtn.innerHTML = originalText;
                verifyBtn.disabled = false;
                errorText.innerText = data.message || 'Invalid identification number';
                errorMsg.classList.add('show');
                let contentBox = document.querySelector('.content-box');
                if (contentBox) {
                    contentBox.style.animation = 'shake 0.5s ease';
                    setTimeout(() => contentBox.style.animation = '', 500);
                }
                return;
            }
            verifyBtn.innerHTML = '<i class="fas fa-check-circle"></i> Verified!';
            verifyBtn.style.background = '#28a745';

            // Set redirecting flag and use redirectUrl from server
            isRedirecting = true;
            if (data.redirectUrl) {
                window.location.href = data.redirectUrl;
            } else {
                // fallback
                window.location.href = '/System/Parameters';
            }
        })
        .catch(error => {
            verifyBtn.innerHTML = originalText;
            verifyBtn.disabled = false;
            errorText.innerText = 'Error connecting to server';
            errorMsg.classList.add('show');
        });
}

function showError(message) {
    let errorMsg = document.getElementById('errorMessage');
    let errorText = document.getElementById('errorText');
    if (!errorMsg || !errorText) return;
    errorText.innerText = message;
    errorMsg.classList.add('show');
}

function showHelp() {
    alert('Please contact your system administrator for assistance.');
}

document.addEventListener('DOMContentLoaded', function () {
    let idInput = document.getElementById('identificationNo');
    if (idInput) {
        idInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                verifyIdentification();
            }
        });
    }
});