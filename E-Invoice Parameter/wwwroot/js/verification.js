// verification.js
let formChanged = false;
let currentBranch = 'TEST BRANCH';
let isRedirecting = false;

document.addEventListener("DOMContentLoaded", function () {
    currentBranch = 'TEST BRANCH';
    let today = new Date();
    let dateStr = today.toLocaleDateString() + ' ' + today.toLocaleTimeString();
    let printableData = document.getElementById('printableData');
    if (printableData) {
        printableData.setAttribute('data-print-date', dateStr);
    }
});

function selectBranch(element, branchName) {
    document.querySelectorAll(".branch-item").forEach(item => {
        item.classList.remove("active");
    });
    element.classList.add("active");
    let selectedBranch = document.getElementById("selectedBranchName");
    if (selectedBranch) {
        selectedBranch.innerText = '- ' + branchName;
    }
    currentBranch = branchName;
    loadBranchSettings(branchName);
}

function loadBranchSettings(branchName) {
    let loader = document.getElementById("settingsLoader");
    let body = document.getElementById("settingsBody");
    if (!loader || !body) return;
    loader.style.display = "flex";
    body.style.opacity = "0.4";
    setTimeout(function () {
        loader.style.display = "none";
        body.style.opacity = "1";
        formChanged = false;
    }, 500);
}

function filterBranches(input) {
    let filter = input.value.toLowerCase();
    document.querySelectorAll(".branch-item").forEach(item => {
        let branchName = item.querySelector('span:first-child').innerText.toLowerCase();
        if (branchName.includes(filter)) {
            item.style.display = "flex";
        } else {
            item.style.display = "none";
        }
    });
}

function clearFilter() {
    let input = document.getElementById("branchFilter");
    if (input) {
        input.value = "";
        filterBranches(input);
    }
}

function updateFileInput(input) {
    if (input.files && input.files[0]) {
        let filePath = input.files[0].name;
        let textInput = input.closest('.file-field').querySelector('.form-control');
        if (textInput) {
            textInput.value = filePath;
            formChanged = true;
        }
    }
}

document.addEventListener("input", function () {
    formChanged = true;
});

function saveSettings() {
    if (!formChanged) {
        alert("No changes to save");
        return;
    }
    let formData = {
        branch: currentBranch,
        sourceNumber: document.querySelectorAll('.form-control')[0]?.value || '',
        clientId: document.querySelectorAll('.form-control')[1]?.value || '',
        clientSecret: document.querySelectorAll('.form-control')[2]?.value || '',
        apiKey: document.querySelectorAll('.form-control')[3]?.value || '',
        certificate: document.querySelectorAll('.form-control')[4]?.value || '',
        privateKey: document.querySelectorAll('.form-control')[5]?.value || ''
    };
    let saveBtn = document.getElementById('saveBtn');
    if (!saveBtn) return;
    let originalText = saveBtn.innerHTML;
    saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
    setTimeout(function () {
        alert("Settings saved successfully for " + currentBranch);
        saveBtn.innerHTML = originalText;
        formChanged = false;
    }, 800);
}

function printData() {
    let today = new Date();
    let dateStr = today.toLocaleDateString() + ' ' + today.toLocaleTimeString();
    let printableData = document.getElementById('printableData');
    if (printableData) {
        printableData.setAttribute('data-print-date', dateStr);
    }
    window.print();
}

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
    let contentBox = document.querySelector('.content-box');
    if (contentBox) {
        contentBox.style.animation = 'shake 0.5s ease';
        setTimeout(() => {
            contentBox.style.animation = '';
        }, 500);
    }
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