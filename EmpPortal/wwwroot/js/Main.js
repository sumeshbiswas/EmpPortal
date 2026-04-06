
$(document).ready(function () {
	$('#ACTIVE').on('change', function () {
		let status = $(this).is(':checked') ? 'Active' : 'Inactive';
		let color = $(this).is(':checked') ? 'green' : 'gray';

		$('#statusText').text(status).css('color', color);
	});
});

	document.getElementById("cancelBtn").addEventListener("click", function () {
		Swal.fire({
			title: 'Are you sure?',
			text: "Do you want to cancel all the changes?",
			icon: 'warning',
			showCancelButton: true,
			confirmButtonText: 'Yes, cancel it!',
			cancelButtonText: 'No, keep it',
			reverseButtons: true
		}).then((result) => {
			if (result.isConfirmed) {
				Swal.fire(
					'Cancelled!',
					'Your action has been cancelled.',
					'success'
				)
			} else if (result.dismiss === Swal.DismissReason.cancel) {
				Swal.fire(
					'Resumed',
					'Your action is still in progress.',
					'info'
				)
			}
		});
	});

