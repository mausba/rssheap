$(document).ready(function () {

    searchBox.doSearch(0);

    $('#Search').live('click', function () {
        searchBox.doSearch();
    });

    $('#Keyword').keydown(function (e) {
        if (e.keyCode == 13) {
            searchBox.doSearch(0);
        }
    })

    $('a.page').live('click', function (event) {
        event.preventDefault();
        var pageIndex = $(this).attr('data-value');
        searchBox.doSearch(pageIndex);
    });

    $('input.subscription-btn').live('click', function () {
        var $button = $(this);
        searchBox.subscribe($button);
    });

    $('#addsubscribe').live('click', function () {
        var path = $('#addfeed').val();
        searchBox.addFeed(path);
    });

    $("a.followers-count").live("click", function (event) {
        var $that = $(this);

        var $dialogDiv = $("<div>");
        $dialogDiv.attr("class", "followers-dialog");
        $dialogDiv.attr("title", "Followers");

        var $close = $("<a>").attr("href", "#").attr("class", "close").text("Close");
        $close.bind("click", function (event) {
            $dialogDiv.remove();
            event.preventDefault();
        });


        var feedId = $(this).attr("data-feed-id");
        $.ajax({
            url: "/users/FeedUsers?feedId=" + feedId,
            success: function (data) {
                $dialogDiv.html(data);
                $dialogDiv.append($close);
                $that.after($dialogDiv);
            }
        });

        event.preventDefault();
    });

    $("a.unfollow-btn").live("click", function () {
        searchBox.unfollow($(this));
    });

    $("a.follow-btn").live("click", function () {
        searchBox.follow($(this));
    });

});

var searchBox = (function () {

    function refreshFollowBtn(following, $btn) {
        if (following) {
            $btn.text("Unfollow");
            $btn.attr("class", "unfollow-btn");
        } else {
            $btn.text("Follow");
            $btn.attr("class", "follow-btn");
        }
    };


    return {
        doSearch: function (pageIndex) {
            if (pageIndex == null) {
                pageIndex = 0;
            }
            if ($('#Keyword').val().length > 1) {
                var parameters = { keyword: $('#Keyword').val(), pageIndex: pageIndex };
                $.ajax({
                    contentType: 'json',
                    type: 'GET',
                    cache: false,
                    url: '/Feeds/FindFeeds/',
                    data: parameters,
                    beforeSend: function (jqXHR, settings) {
                        $('.loader').show();
                        var $container = $('#search-result-container');
                        $container.empty();
                    },
                    error: function (jqXHR, textStatus, errorThrown) {
                        alert(errorThrown);
                    },
                    complete: function (jqXHR, textStatus) {
                        $('.loader').hide();
                    },
                    success: function (data) {
                        var $container = $('#search-result-container');
                        $container.html(data);
                    }
                });
            } else {
                //alert("Type more characters in search form!");
            }

        },
        subscribe: function ($button) {
            var id = $button.attr('data-id');
            var myaction = $button.attr('name');
            var parameters = { 'myaction': myaction, 'id': id };
            $.ajax({
                contentType: 'json',
                type: 'GET',
                cache: false,
                url: '/Feeds/Subscribe/',
                data: parameters,
                beforeSend: function (jqXHR, settings) {
                    //$('.loader').show();
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    alert(errorThrown);
                },
                complete: function (jqXHR, textStatus) {
                    //$('.loader').hide();
                },
                success: function (data) {
                    if (myaction == 'subscribe') {
                        $button.attr('name', 'unsubscribe');
                        $button.attr('value', 'Unsubscribe');
                    } else {
                        $button.attr('name', 'subscribe');
                        $button.attr('value', 'Subscribe');
                    }
                }
            });
        },

        addFeed: function (path) {
            var parameters = { 'path': path };
            $.ajax({
                type: 'POST',
                url: '/Feeds/AddFeed/',
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
                    if (data == "OK") {
                        $('div.addfeed-result').html('<p>RSS Feed added successfully!</p>');
                    }
                }
            });
        },

        follow: function ($btn) {
            userController.followUser($btn.attr("data-id"), function (success) {
                if (success) {
                    refreshFollowBtn(true, $btn);
                }
            });

        },

        unfollow: function ($btn) {
            userController.unFollowUser($btn.attr("data-id"), function (success) {
                if (success) {
                    refreshFollowBtn(false, $btn);
                }
            });
        }
    }
})();