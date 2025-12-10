// wwwroot/js/alertHelper.js

window.crmAlerts = {
    showSuccess: function (message) {
        Swal.fire({
            icon: "success",
            title: message,
            width: 320,
            padding: "10px 0 16px",
            customClass: {
                popup: "crm-alert-popup",
                title: "crm-alert-title"
            }
        });
    },

    showError: function (message) {
        Swal.fire({
            icon: "error",
            title: message,
            width: 320,
            padding: "10px 0 16px",
            customClass: {
                popup: "crm-alert-popup",
                title: "crm-alert-title"
            }
        });
    },

    confirmDelete: async function (message) {
        const result = await Swal.fire({
            title: "Confirm delete",
            text: message || "Are you sure?",
            icon: "warning",
            width: 340,
            padding: "10px 0 16px",
            customClass: {
                popup: "crm-alert-popup",
                title: "crm-alert-title"
            },
            showCancelButton: true,
            confirmButtonText: "Yes",
            cancelButtonText: "Cancel"
        });

        return result.isConfirmed;
    }
};
