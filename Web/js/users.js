function confirmDelete(Id, user) {
    if (Id == user) {
        Swal.fire("You can't delete yourself!");
    }
    else {
        Swal.fire({
            title: "Are you sure?",
            text: "Are you sure that you want to delete this User?",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#ec6c62",
            confirmButtonText: "Yes, delete it!",
            cancelButtonText: "No, cancel",
            closeOnConfirm: false,
            closeOnCancel: true
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: "/Users/Delete/",
                    data: {
                        "Id": Id
                    },
                    type: "DELETE",
                    success: function (data) {
                        Swal.fire({
                            title: "Deleted!",
                            text: "User was successfully deleted!",
                            icon: "success"
                        }).then((result) => {
                            window.location.href = '/Users/Index';
                        });
                    },
                    error: function (data) {
                        Swal.fire({
                            title: "Oops",
                            text: "We couldn't connect to the server!",
                            icon: "error"
                        });
                    }
                });
            }
        });
    }
}
