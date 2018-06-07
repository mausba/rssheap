$(window).load(function () {
    $('button.follower').live('click', function () {
        users.followOrUnfollow($(this).attr('data-id'), $(this).attr('data-command'), $(this));
    });
});


var users = {
    followOrUnfollow: function (id, command, button) {
        var $button = $(button);
        var parameters = { id: id, command: command };
        $.ajax({
            type: 'POST',
            url: '/users/follow/',
            data: parameters,
            beforeSend: function (jqXHR, settings) {
                $('.loader').show();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                alert(errorThrown);
            },
            complete: function (jqXHR, textStatus) {
                $('.loader').hide();
            },
            success: function (data) {
                var text = "Follow";
                var newCommand = "follow";
                if (data == "Followed") {
                    text = "Unfollow";
                    newCommand = "unfollow";
                } else {
                    text = "Follow";
                    newCommand = "follow";
                }
                $button.text(text);
                $button.attr("data-command", newCommand);

            }
        });
    }
}