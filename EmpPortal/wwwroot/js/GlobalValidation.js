function validateForm(formSelector) {
    console.log("Validating form...");
    let isValid = true;
    $(formSelector).find('.error-message').remove();
    $(formSelector).find('[data-required="true"]').css("border", "");

    const requiredFields = $(formSelector).find('[data-required="true"]');
    for (let i = 0; i < requiredFields.length; i++) {
        const field = $(requiredFields[i]);
        let value = field.val();
        if (typeof value === "string") {
            value = value.trim();
        }

        const errorMsg = field.data("error") || "This field is required";

        if (!value) {
            field.focus().css("border", "1px solid red");
            showError(field, errorMsg);
            isValid = false;
            break;
        }
    }

    $(formSelector).find('[data-validate="email"]').each(function () {
        const email = $(this).val().trim();
        if (email && !validateEmail(email)) {
            isValid = false;
            showError($(this), $(this).data("error") || "Invalid email address");
        }
    });

    $(formSelector).find('[data-validate="phone"]').each(function () {
        const phone = $(this).val().trim();
        if (phone && !validatePhone(phone)) {
            isValid = false;
            showError($(this), $(this).data("error") || "Invalid phone number");
        }
    });

    return isValid;
}
function showError(element, message) {
    element.after(`<span class="error-message" style="color:red;font-size:12px;">${message}</span>`);
}
function validateEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}
function validatePhone(phone) {
    const regex = /^\d{10}$/;
    return regex.test(phone);
}
function validateRequiredField(selector, fieldName) {
    const $field = $(selector);
    const value = $field.val()?.trim();

    if (!value) {
        toastr.warning(`${fieldName} is required`);
        $field.focus();
        return false;
    }
    return true;
}
const MessageStore = {
    insert: '✅ Data inserted successfully!',
    update: '✅ Data updated successfully!',
    delete: '🗑️ Data deleted successfully!',
    error: '❌ An error occurred. Please try again.',
    warning: '⚠️ Please check your inputs.',
    info: 'ℹ️ Just so you know...'
  };
function showFlashMessageByKey(key, type = 'success') {
    const message = MessageStore[key];
    if (message) {
        localStorage.setItem('flashMessage', JSON.stringify({ message, type }));
    }
  }
$(document).ready(function () {
        const stored = localStorage.getItem('flashMessage');
        if (stored) {
          const {message, type} = JSON.parse(stored);
        if (message && type && toastr[type]) {
            toastr[type](message);
          } else {
            toastr.info(message);
          }
        localStorage.removeItem('flashMessage');
        }
});
function confirmAction(message = "Are you sure you want to delete this item group?", yesText = "Yes", noText = "No") {
    return Swal.fire({
        title: '<strong>Confirm Delete</strong>',
        html: `
            <div style="margin-bottom: 10px;">
                <div style="background: #ffe5e5; display: inline-block; border-radius: 10px; padding: 10px;">
                    <i class="fa fa-trash" aria-hidden="true" style="color:red;"></i>
                </div>
            </div>
            <div style="color: #555; font-size: 14px;">
                ${message}
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: yesText,
        cancelButtonText: noText,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#aaa',
        buttonsStyling: false,
        reverseButtons: true,    
        focusCancel: true,      
        customClass: {
            popup: 'custom-swal-spacing',
            confirmButton: 'btn btn-secondary',
            cancelButton: 'btn btn-danger'
        }
    }).then(result => result.isConfirmed);
}

$('.nav-link').on('click dblclick', function (e) {
    e.stopPropagation();
});
function showDocumentPopupjQuery(data, docCode) {
    console.log(data);
    if (!data || !Array.isArray(data) || data.length === 0) {
        toastr.error("No data to show");
        return;
    }

    const existingModal = document.getElementById("dynamicDocModal");
    if (existingModal) existingModal.remove();

    const tableRows = data.map(row => `
        <tr>
            <td>${row.code || row.doC_CODE || ''}</td>
            <td>${row.uUser || ''}</td>
            <td>${row.udate ? new Date(row.udate).toLocaleString() : ''}</td>
            <td>${row.euser || ''}</td>
            <td>${row.edate ? new Date(row.edate).toLocaleString() : ''}</td>
            <td></td>
            <td>Approved By</td>
            <td>Approved On</td>
            <td>${row.wsid || ''}</td>
            <td>${row.lip || ''}</td>
            <td>${row.lid || ''}</td>
        </tr>
    `).join('');

    const modalHTML = `
        <div class="modal fade" id="dynamicDocModal" tabindex="-1" aria-labelledby="docModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg">
                <div class="modal-content draggable-modal">
                    <div class="popup-header">
                        <h5 class="popup-title" id="docModalLabel">Document Details for ${data[0]?.uUser || 'Unknown User'}</h5>
                        <i class="fa fa-times" data-bs-dismiss="modal" style="cursor: pointer;" aria-label="Close"></i>
                    </div>
                    <div class="popup-card">
                        <div class="table-responsive">
                            <table id="tblApprovalStageMaster" class="table table-bordered">
                                <thead class="table-head">
                                    <tr>
                                        <th>Doc id</th>
                                        <th>Created By</th>
                                        <th>Created On</th>
                                        <th>Last Modified By</th>
                                        <th>Last Modified On</th>
                                        <th>Status</th>
                                        <th>Approved By</th>
                                        <th>Approved On</th>
                                        <th>Last PC Name</th>
                                        <th>Last N-Compid</th>
                                        <th>Last Login By</th>
                                    </tr>
                                </thead>
                                <tbody>${tableRows}</tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>`;

    // Inject modal into the DOM
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal using Bootstrap's API
    const modalElement = document.getElementById('dynamicDocModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();

    // Wait until modal is shown, THEN make it draggable
    $(modalElement).on('shown.bs.modal', function () {
        $(this).find('.modal-dialog').draggable({
            handle: '.popup-header',
            containment: 'window' // Optional: keep inside screen
        });
    });
}




// MaxLength Validation for Input Fields
function enforceMaxlength(selector) {
    $(document).on('input', selector, function () {
        const maxLength = $(this).data('maxlength');
        const value = $(this).val();

        if (maxLength && value.length > maxLength) {
            $(this).val(value.slice(0, maxLength));
        }
    });
}
// Usage: apply to all number inputs with data-maxlength
$(document).ready(function () {
    enforceMaxlength('input[type="number"][data-maxlength]');
});
//How to call
//<input type="number" class="form-control" id="FLAG_A" name="FLAG_A" data-maxlength="5">

