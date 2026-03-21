// Shared JavaScript functions for both pages

document.addEventListener("input", function () {
    if (typeof formChanged !== 'undefined') {
        formChanged = true;
    }
});

window.addEventListener("beforeunload", function (e) {
    if (typeof formChanged !== 'undefined' && formChanged) {
        e.preventDefault();
        e.returnValue = '';
    }
});

function showError(message, elementId = 'errorMessage', textId = 'errorText', containerClass = '.content-box') {
    let errorMsg = document.getElementById(elementId);
    let errorText = document.getElementById(textId);

    if (!errorMsg || !errorText) return;

    errorText.innerText = message;
    errorMsg.classList.add('show');

    let container = document.querySelector(containerClass);
    if (container) {
        container.style.animation = 'shake 0.5s ease';
        setTimeout(() => {
            container.style.animation = '';
        }, 500);
    }
}

function getFormattedDate() {
    let today = new Date();
    return today.toLocaleDateString() + ' ' + today.toLocaleTimeString();
}