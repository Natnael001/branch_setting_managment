// system-parameters.js
let currentBranch = '';
let currentBranchId = 0;

document.addEventListener("DOMContentLoaded", function () {
    let tin = sessionStorage.getItem('verifiedTin');
    if (!tin) {
        // Server should have redirected, but just in case
        window.location.href = '/System/Login';
        return;
    }

    let printableData = document.getElementById('printableData');
    if (printableData) {
        printableData.setAttribute('data-print-date', getFormattedDate());
    }
    loadBranches(tin);
});

function loadBranches(tin) {

    let branchList = document.getElementById("branchList");
    if (!branchList) {
        console.error('Branch list element not found!');
        return;
    }

    branchList.innerHTML = '<li class="branch-item"><div class="loader-small"></div><span>Loading branches...</span></li>';

    fetch('/System/GetBranches?tin=' + encodeURIComponent(tin))
        .then(res => res.json())
        .then(res => {
            branchList.innerHTML = "";

            let branches = res.data || [];
            if (branches.length === 0) {
                branchList.innerHTML = '<li class="branch-item"><span>No branches found for this TIN</span></li>';
                return;
            }

            branches.forEach((branch) => {
                let li = document.createElement("li");
                li.className = "branch-item";
                li.setAttribute('data-branch-id', branch.id);
                li.setAttribute('data-branch-name', branch.name);

                li.onclick = function () {
                    selectBranch(this, branch.name, branch.id);
                };

                let badgeHtml = branch.hasSettings
                    ? '<span class="assigned-badge"><i class="fas fa-check-circle"></i> assigned</span>'
                    : '<span></span>';

                li.innerHTML = `
                    <span>${branch.name}</span>
                    ${badgeHtml}
                `;

                branchList.appendChild(li);
            });

            let firstBranch = branchList.querySelector(".branch-item");
            if (firstBranch) {
                firstBranch.classList.add("active");
                let branchName = firstBranch.getAttribute('data-branch-name');
                let branchId = firstBranch.getAttribute('data-branch-id');
                selectBranch(firstBranch, branchName, branchId);
            }
        })
        .catch(error => {
            console.error('Fetch error:', error);
            branchList.innerHTML = '<li class="branch-item"><span>Error loading branches</span></li>';
        });
}

function loadBranchSettings(branchId) {

    let loader = document.getElementById("settingsLoader");
    let body = document.getElementById("settingsBody");
    if (!loader || !body) return;

    loader.style.display = "flex";
    body.style.opacity = "0.4";

    fetch('/System/GetBranchSettingsPartial?branchId=' + branchId)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.text();  // Get HTML
        })
        .then(html => {
            loader.style.display = "none";
            body.style.opacity = "1";
            body.innerHTML = html;   // Replace content with the partial view
            if (typeof formChanged !== 'undefined') formChanged = false;
        })
        .catch(error => {
            console.error('Error loading settings:', error);
            loader.style.display = "none";
            body.style.opacity = "1";
            body.innerHTML = '<div class="error-message">Failed to load settings. Please try again.</div>';
        });
}

function saveSettings() {
    if (typeof formChanged !== 'undefined' && !formChanged) {
        alert("No changes to save");
        return;
    }

    let saveBtn = document.getElementById('saveBtn');
    if (!saveBtn) return;

    // Get form data
    let sourceNumber = document.getElementById('sourceNumber')?.value || '';
    let clientId = document.getElementById('clientId')?.value || '';
    let clientSecret = document.getElementById('clientSecret')?.value || '';
    let apiKey = document.getElementById('apiKey')?.value || '';

    // Get file inputs
    let certFile = document.getElementById('certFile');
    let keyFile = document.getElementById('keyFile');

    // Create FormData object for file upload
    let formData = new FormData();
    formData.append('BranchId', currentBranchId);
    formData.append('SourceNumber', sourceNumber);
    formData.append('ClientId', clientId);
    formData.append('ClientSecret', clientSecret);
    formData.append('ApiKey', apiKey);

    // Append files if selected
    if (certFile && certFile.files && certFile.files[0]) {
        formData.append('DigitalCertificate', certFile.files[0]);
    }

    if (keyFile && keyFile.files && keyFile.files[0]) {
        formData.append('PrivateKey', keyFile.files[0]);
    }

    let token = document.getElementById('RequestVerificationToken')?.value;
    let originalText = saveBtn.innerHTML;
    saveBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Saving...';
    saveBtn.disabled = true;

    // Use fetch with FormData (no Content-Type header, browser will set it with boundary)
    fetch('/System/SaveBranchSettings', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
            // Do NOT set Content-Type - browser will set it automatically for FormData
        },
        body: formData
    })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                alert(result.message);

                // Add assigned badge if not present
                let activeBranch = document.querySelector(".branch-item.active");
                if (activeBranch && !activeBranch.querySelector('.assigned-badge')) {
                    let badge = document.createElement('span');
                    badge.className = 'assigned-badge';
                    badge.innerHTML = '<i class="fas fa-check-circle"></i> assigned';
                    activeBranch.appendChild(badge);
                }

                // Clear file inputs after successful save
                if (certFile) certFile.value = '';
                if (keyFile) keyFile.value = '';

                // Reload settings to show updated file paths
                loadBranchSettings(currentBranchId);

                if (typeof formChanged !== 'undefined') formChanged = false;
            } else {
                alert('Error: ' + result.message);
            }
            saveBtn.innerHTML = originalText;
            saveBtn.disabled = false;
        })
        .catch(error => {
            console.error('Save error:', error);
            alert('Failed to save settings. Check console for details.');
            saveBtn.innerHTML = originalText;
            saveBtn.disabled = false;
        });
}

function selectBranch(element, branchName, branchId) {

    document.querySelectorAll(".branch-item").forEach(item => {
        item.classList.remove("active");
    });

    element.classList.add("active");

    let selectedBranch = document.getElementById("selectedBranchName");
    if (selectedBranch) {
        selectedBranch.innerText = '- ' + branchName;
    }

    currentBranch = branchName;
    currentBranchId = parseInt(branchId);

    loadBranchSettings(branchId);
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
        let textInput = input.closest('.file-field')?.querySelector('.form-control');
        if (textInput) {
            textInput.value = filePath;
            if (typeof formChanged !== 'undefined') {
                formChanged = true;
            }
        }
    }
}

function printData() {
    let printableData = document.getElementById('printableData');
    if (printableData) {
        printableData.setAttribute('data-print-date', getFormattedDate());
    }
    window.print();
}

function getFormattedDate() {
    let today = new Date();
    return today.toLocaleDateString() + ' ' + today.toLocaleTimeString();
}

window.addEventListener("beforeunload", function (e) {
    if (typeof formChanged !== 'undefined' && formChanged) {
        e.preventDefault();
        e.returnValue = '';
    }
});

// Add file input change handlers
document.addEventListener('DOMContentLoaded', function () {
    let certFile = document.getElementById('certFile');
    if (certFile) {
        certFile.addEventListener('change', function () {
            if (this.files && this.files[0]) {
                if (typeof formChanged !== 'undefined') formChanged = true;
            }
        });
    }

    let keyFile = document.getElementById('keyFile');
    if (keyFile) {
        keyFile.addEventListener('change', function () {
            if (this.files && this.files[0]) {
                if (typeof formChanged !== 'undefined') formChanged = true;
            }
        });
    }
});