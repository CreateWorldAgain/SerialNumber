// Write your JavaScript code.
function getParameterByName(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function table_cellPrepared(e) {

    if (e.rowType === "data" && e.column.command === "edit") {
        
        var isEditing = e.row.isEditing,
            $links = e.cellElement.find(".dx-link");

        $links.text("");

        if (isEditing) {
            $links.filter(".dx-link-save").addClass("dx-icon-save");
            $links.filter(".dx-link-cancel").addClass("dx-icon-revert");
        } else {
            $links.filter(".dx-link-edit").addClass("dx-icon-edit");
            $links.filter(".dx-link-delete").addClass("dx-icon-trash");
            $links.filter(".dx-link-add").addClass("dx-icon-add");
        }
    }

}

function UserProfile() {
    $('#user-profile').load('/user/profile',
        function () {

            $("#popup-profile").dxPopup("instance").show();

            $('#formProfile').submit(function () {

                $('#saveButton').prepend('<span class="button-lock"></span>');

                var btnText = $('#saveButton  .dx-button-text').text();
                $('#saveButton  .dx-button-text').html("&nbsp;");

                $.ajax({
                    type: "POST",
                    url: "/user/save",
                    data: $('#formProfile').serialize(),
                    success: function () {
                        $("#popup-profile").dxPopup("hide");
                        toastr['info']('Данные успешно сохранены');

                        $('.username').text($('[name=FirstName]').val() + ' ' + $('[name=LastName]').val());

                        $('#saveButton').children()[0].remove();
                        $('#saveButton  .dx-button-text').text(btnText);
                    },
                    error: function (e) {
                        $('#saveButton').children()[0].remove();
                        $('#saveButton  .dx-button-text').text(btnText);

                        toastr["error"]("HTTP Status: " + e.status);
                    }
                });

                event.preventDefault();
                return false;
            });
        }
    );
}

function userProfilePopupShown() {
    $("#formProfile input[name=FirstName]").focus().select();
}