$(document).ready(function () {
    $(".more-tags").click(function (e) {
        e.preventDefault();
        $.each($(this).parent().find("a"), function (i, n) {
            if (!$(n).is(":visible")) {
                $(n).fadeIn(400);
            }
        });
        $(this).hide();
    });
});

