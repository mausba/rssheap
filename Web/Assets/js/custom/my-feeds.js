
$(document).ready(function () {

    $("#articles-list ul li a.feed").live("click", function (e) {
        e.preventDefault();
        var _this = $(this);

        var feedId = _this.attr("id");
        $.ajax({
            url: '/Feeds/GetFeedArticles',
            type: 'POST',
            async: false,
            data: { feedId: feedId },
            success: function (result) {
                $("#article-list").html(result);
            }
        });
    });

    $("a.article").live("click", function (e) {
        e.preventDefault();

        $(this).parent().removeClass("bold");

        var articleId = $(this).attr("id");
        $.ajax({
            url: '/Feeds/GetFeedArticle',
            type: 'POST',
            async: false,
            data: { articleId: articleId },
            success: function (result) {
                $("#article-content").html(result);
                markArticleAsViewed(articleId);
            }
        });
    });

    $(".like").live("click", function (e) {
        e.preventDefault();
        var id = $(this).data("id");
        $.ajax({
            url: "/Feeds/LikeArticle",
            cache: false,
            type: 'POST',
            data: { articleId: id },
            success: function (result) {

            }
        });
        $(".like-container").html("Thank you!");
    });

});

function markArticleAsViewed(articleId) {
    $.ajax({
        url: '/Feeds/MarkArticleAsViewed',
        type: 'POST',
        async: false,
        data: { articleId: articleId }
    });
}
