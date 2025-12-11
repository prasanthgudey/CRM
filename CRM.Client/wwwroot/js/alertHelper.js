window.crmAlerts = {
    showSuccess: function (message) {
        Swal.fire({
            icon: 'success',
            title: 'Success',
            text: message,
            width: '340px',
            padding: '1rem',
            allowOutsideClick: false,
            confirmButtonColor: '#6c63ff',
        });
    },

    showError: function (message) {
        Swal.fire({
            icon: 'error',
            title: 'Error',
            text: message,
            width: '340px',
            padding: '1rem',
            allowOutsideClick: false,
            confirmButtonColor: '#6c63ff',
        });
    },

    showWarning: function (message) {
        Swal.fire({
            icon: 'warning',
            title: 'Warning',
            text: message,
            width: '340px',
            padding: '1rem',
            allowOutsideClick: false,
            confirmButtonColor: '#6c63ff',
        });
    },

    showInfo: function (message) {
        Swal.fire({
            icon: 'info',
            title: 'Info',
            text: message,
            width: '340px',
            padding: '1rem',
            allowOutsideClick: false,
            confirmButtonColor: '#6c63ff',
        });
    }
};
