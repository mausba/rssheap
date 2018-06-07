$(document).ready(function () {

    $(".next").click(function () {
        $(this).text("Loading");
    });

    $(".vote-up").click(function(e) {
        var $this = $(this);

        if($this.hasClass("vote-up-on")) return;
                
        $.post("/Article/UpVote", { articleId: $this.data("articleid") }, function(result) { });
        $this.addClass("vote-up-on");
        $(".vote-down-on").removeClass("vote-down-on").addClass("vote-down-off");
        $this.removeClass("vote-up-off");
    });

    $(".vote-down").click(function(e) {
        var $this = $(this);
       
        if($this.hasClass("vote-down-on")) return;
                
        $.post("/Article/DownVote", { articleId: $this.data("articleid") }, function (result) { });
        $this.addClass("vote-down-on");
        $(".vote-up-on").removeClass("vote-up-on").addClass("vote-up-off");
        $this.removeClass("vote-down-off");
    });

    $(".star").on("click", function () {
        var articleId = $(this).data("articleid");
        if($(this).hasClass("star-on")) {
            addRemoveFromFavorite(false, articleId);
            $(this).removeClass("star-on");
        } else {
            addRemoveFromFavorite(true, articleId);
            $(this).addClass("star-on");
        }
    });

    function addRemoveFromFavorite(add, articleid) {
        var url = add ? "/Article/AddToFavorites" : "/Article/RemoveFromFavorites";
        $.post(url, { articleId: articleid }, function() { });
    }

    $(".subscribetofeed").click(function () {
        var $this = $(this);
        var subscribed = $this.hasClass("subscribed");
        var feedId = $this.data("feedid");

        if (!subscribed) {
            $.post("/article/subscribetofeed", { feedId: feedId, url: null }, function () { });
            $this.addClass("subscribed");
        } else {
            $.post("/article/unsubscribefromfeed", { feedId: feedId, url: null }, function () { });
            $this.removeClass("subscribed");
        }
    });

    $(".flag").click(function (e) {
        e.preventDefault();
        var $this = $(this);

        if(!confirm("Do you want to flag this as not related")) { return; }

        var articleId = $this.data("articleid");
        $(this).addClass("flagged");

        $.post("/article/flag", { id: articleId});
        return false;
    })
});