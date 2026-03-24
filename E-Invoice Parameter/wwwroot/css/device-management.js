// device-management.js
function toggleDeviceStatus(id) {
    if (confirm('Are you sure you want to toggle the status of this device?')) {
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
                    location.reload();
                } else {
                    showAlert(response.message, 'error');
                }
            },
            error: function () {
                showAlert('An error occurred while toggling device status.', 'error');
            }
        });
    }
}

function deleteDevice(id, deviceName) {
    if (confirm(`Are you sure you want to delete "${deviceName}"? This action cannot be undone.`)) {
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
                    location.reload();
                } else {
                    showAlert(response.message, 'error');
                }
            },
            error: function () {
                showAlert('An error occurred while deleting the device.', 'error');
            }
        });
    }
}

function showAlert(message, type) {
    var alertClass = type === 'success' ? 'alert-success' : 'alert-error';
    var icon = type === 'success' ? 'fa-check-circle' : 'fa-exclamation-circle';

    var alertHtml = `
        <div class="alert ${alertClass}">
            <i class="fas ${icon}"></i> ${message}
            <button type="button" class="alert-close" onclick="$(this).closest('.alert').fadeOut(300, function() { $(this).remove(); });">&times;</button>
        </div>
    `;

    $('.container').prepend(alertHtml);

    setTimeout(function () {
        $('.alert').fadeOut(300, function () {
            $(this).remove();
        });
    }, 3000);
}