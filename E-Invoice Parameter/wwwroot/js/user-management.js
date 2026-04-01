// user-management.js
function toggleUserStatus(userId) {
    if (!userId || !confirm('Are you sure you want to change this user\'s status?')) return;

    fetch('/System/ToggleUserStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.getElementById('RequestVerificationToken').value
        },
        body: JSON.stringify({ id: userId })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                location.reload();
            } else {
                alert(data.message || 'An error occurred');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('An error occurred while processing your request');
        });
}

function filterUsers(input) {
    const filter = input.value.toLowerCase();
    const table = document.getElementById('usersTable');
    if (!table) return;

    const rows = table.querySelectorAll('tbody tr');
    rows.forEach(row => {
        const usernameCell = row.cells[0];
        const employeeCell = row.cells[1];
        const rolesCell = row.cells[2];

        const username = usernameCell?.innerText.toLowerCase() || '';
        const employee = employeeCell?.innerText.toLowerCase() || '';
        const roles = rolesCell?.innerText.toLowerCase() || '';

        if (username.includes(filter) || employee.includes(filter) || roles.includes(filter)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

function clearUserFilter() {
    const filterInput = document.getElementById('userFilter');
    if (filterInput) {
        filterInput.value = '';
        filterUsers(filterInput);
    }
}