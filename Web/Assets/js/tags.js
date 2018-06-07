$(document).ready(function () {
    $(document.body).on("click", ".subscribe", function (e) {
        e.preventDefault();
        var $this = $(this);
        var $parent = $this.parent();
        var tagName = $this.data("name");

        $.post("/Json/AddFavoriteOrIgnoredTag", { name: tagName, ignore: false }, function (result) {
            var a = document.createElement("a");
            $(a).addClass("unsubscribe")
                .attr("href", "#")
                .data("name", tagName)
                .text(" - unsubscribe")
                .appendTo($parent);

            $this.remove();
        });
    });

    $(document.body).on("click", ".unsubscribe", function (e) {
        e.preventDefault();
        var $this = $(this);
        var $parent = $this.parent();
        var tagName = $this.data("name");

        $.post("/Json/RemoveFavoriteOrIgnoredTag", { name: tagName, ignore: false }, function (result) {
            var a = document.createElement("a");
            $(a).addClass("subscribe")
                .attr("href", "#")
                .data("name", tagName)
                .text(" - subscribe")
                .appendTo($parent);

            $this.remove();
        });
    });

    $("#search-for-tags").keyup(function (e) {
        var val = $(this).val();
        if (val != '' && e.keyCode == 13) {
            $(this).parents("form").submit();
        }
    });

    function showDone() {
        var $container = $(".tags-notification");
        $container.find("#title").hide();
        $container.find("#done").text("I am done, take me to the article list").css("float", "none");
    }

    $(window).scroll(function () {
        var $container = $(".tags-notification");

        var scrolled = window.pageYOffset || document.documentElement.scrollTop;

        if (scrolled < 70 && $container.hasClass("fixed-tag")) {
            $container.removeClass("fixed-tag");
        } else {
            $container.addClass("fixed-tag");
        }
    });

    $(".tag-item-container").click(function () {
        var tagName = $(this).data("name");
        if ($(this).hasClass("tag-selected")) {
            $.post("/Json/RemoveFavoriteOrIgnoredTag", { name: tagName, ignore: false }, function (result) { });
        } else {
            $.post("/Json/AddFavoriteOrIgnoredTag", { name: tagName, ignore: false }, function (result) { });
        }
        $(this).toggleClass("tag-selected");
        $("#done").fadeOut("fast").fadeIn("fast").css("font-weight", "bold");
        showDone();
    });
});