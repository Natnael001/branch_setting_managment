// device-management.js

function toggleDeviceStatus(id) {
    if (!confirm('Are you sure you want to toggle the status of this device?')) {
        return;
    }

    var token = $('#RequestVerificationToken').val();

    $.ajax({
        url: '/Device/ToggleStatus',
        type: 'POST',
        data: { id: id },
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            if (response.success) {
                showAlert(response.message, 'success');
                setTimeout(function () {
                    location.reload();
                }, 1500);
            } else {
                showAlert(response.message, 'error');
            }
        },
        error: function (xhr) {
            console.error('Error toggling device status:', xhr);
            showAlert('An error occurred while toggling device status.', 'error');
        }
    });
}

function deleteDevice(id, deviceName) {
    if (!confirm(`Are you sure you want to delete "${deviceName}"? This action cannot be undone.`)) {
        return;
    }

    var token = $('#RequestVerificationToken').val();

    $.ajax({
        url: '/Device/Delete',
        type: 'POST',
        data: { id: id },
        headers: {
            'RequestVerificationToken': token
        },
        success: function (response) {
            if (response.success) {
                showAlert(response.message, 'success');
                setTimeout(function () {
                    location.reload();
                }, 1500);
            } else {
                showAlert(response.message, 'error');
            }
        },
        error: function (xhr) {
            console.error('Error deleting device:', xhr);
            showAlert('An error occurred while deleting the device.', 'error');
        }
    });
}

function showAlert(message, type) {
    // Remove any existing alerts
    $('.alert').remove();

    var alertClass = type === 'success' ? 'alert-success' : 'alert-error';
    var icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';

    var alertHtml = `
        <div class="alert ${alertClass}" style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
            <i class="fas ${icon}"></i>
            <span>${escapeHtml(message)}</span>
            <button type="button" class="alert-close" onclick="$(this).closest('.alert').fadeOut(300, function() { $(this).remove(); });">&times;</button>
        </div>
    `;

    $('body').append(alertHtml);

    // Auto-dismiss after 3 seconds
    setTimeout(function () {
        $('.alert').fadeOut(300, function () {
            $(this).remove();
        });
    }, 3000);
}

function escapeHtml(text) {
    var map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function (m) { return map[m]; });
}