$(window).load(function () {
    if (window.location.hash == "#article") {
        $("a.article-title:first")[0].click();
    }

    if (window.location.hash == "#lastarticle") {
        $("a.article-title:last")[0].click();
    }
});

$(document).ready(function () {

    $(document).keydown(function (e) {
        switch (e.which) {
            case 37: // left
                if ($("#prev")) $("#prev").click();
                break;
            case 39: // right
                if ($("#next")) $("#next").click();
                break;
        }
    });

    $(document).on("click", "#prev", function (e) {
        e.preventDefault();

        var currentId = "#" + $("#popup").data("id");
        var prevElement = $(currentId).parent().parent().prev().find("a.article-title");
        if (prevElement.length) {
            prevElement.click();
        } else {
            if ($("#prev-page").length == 0) {
                $.magnificPopup.instance.close();
            } else {
                var nextPage = $("#prev-page").attr("href");
                $("#prev-page").attr("href", "");
                document.location = nextPage + "#lastarticle";
            }
        }
    });

    $(document).on("click", "#close", function (e) {
        e.preventDefault();
        var popup = $.magnificPopup.instance;
        if (popup) {
            popup.close();
        }
    });

    $('.ajax-popup').click(function (e) {
        if (e.button == 0 && (!e.ctrlKey && !e.metaKey)) {
            e.preventDefault();
            $(this).addClass("visited");
        }
    });

    var startWindowScroll;
    $('.ajax-popup').magnificPopup({
        type: 'ajax',
        closeOnContentClick: false,
        closeOnBgClick: true,
        showCloseBtn: false,
        alignTop: true,
        callbacks: {
            beforeOpen: function () {
                startWindowScroll = $(window).scrollTop();
            },
            close: function () {
                $(window).scrollTop(startWindowScroll);
            }
        }
    });

    $(document).mouseup(function (e) {
        var container = $("ul.dropdown-feeds");
        if (!container.is(e.target) // if the target of the click isn't the container...
            && container.has(e.target).length === 0) // ... nor a descendant of the container
        {
            container.hide();
        }
    });

    $(".folder-options").click(function (e) {
        e.preventDefault();
        $(".folder-options-ul").toggle();
    });

    $(document).on("click", ".dropdown-feeds .folder", function (e) {
        var $this = $(this);
        e.preventDefault();
        $.post("/article/addtofolder", { feedId: $this.data("feedid"), folderId: $this.data("folderid") }, function (result) {
            if (result == "ok") {
                $this.fadeOut('fast');
                $this.parents(".title-box").find(".more-options").click();
            }
        });
    });

    $(document).on("click", ".remove-folder", function (e) {
        var $this = $(this);
        e.preventDefault();
        $.post("/article/removefromfolder", { feedId: $this.data("feedid"), folderId: $this.data("folderid") }, function (result) {
            if (result == "ok") {
                $this.fadeOut('fast');
                $this.parents(".title-box").find(".more-options").click();
            }
        });
    });


    $("#delete-account").click(function (e) {
        e.preventDefault();
        if (confirm('This will delete the entire account from the site. Are you sure you want to proceed?')) {
            $.post("/json/deleteaccount", function (result) {
                if (result == "ok") {
                    alert('Your account has been deleted, thank you for using rssheap');
                    document.location.href = "/";
                }
            });
        }
        return false;
    });

    $(".js-deletefolder").on("click", function (e) {
        var $this = $(this);
        var id = $this.parent().data("id");

        e.preventDefault();
        if (!confirm("Are you sure you want to delete folder?")) return;

        $.post("/article/deletefolder", { id: id }, function (result) {
            if (result == "ok") {
                $this.parent().parent().remove();
            } else {
                alert(result);
            }
        });
    });

    $(".more-options").click(function (e) {
        e.preventDefault();
        var $this = $(this);

        var parent = $this.parents(".title-box");
        $(parent).find("ul.dropdown-feeds").remove();

        var ul = document.createElement("ul");
        var $ul = $(ul);
        $ul
            .addClass("dropdown-feeds")
            .appendTo($(parent));

        var html = '<li> \
                                <h5>Share on:</h5> \
                                <a target="_blank" href="' + $this.data("fb") + '">Facebook</a> \
                                <a target="_blank" href="' + $this.data("tw") + '">Twitter</a> \
                                <a target="_blank" href="' + $this.data("lin") + '">LinkedIn</a> \
                                <h5>Actions:</h5> \
                                <a class="ignore-feed" href="#">Ignore the entire feed</a> \
                            </li> \
                            <li class="folders"> \
                                <h5>Add feed to folder:</h5> \
                                <span id="loading" style="padding-left: 10px; font-size: 12px;"></span> \
                            </li> \
                            <li class="folders-toremove"> \
                                <h5>Remove feed from folder:</h5> \
                                <span id="loading-remove" style="padding-left: 10px; font-size: 12px;"></span> \
                            </li>';

        $(html).appendTo($(ul));

        var $loading = $ul.find("#loading");
        var $loadingremove = $ul.find("#loading-remove");

        $ul.find(".folder").remove();
        $ul.find(".remove-folder").remove();
        $ul.show();
        $loading.text("").show();
        $loadingremove.text("").show();

        $(".dropdown-feeds .ignore-feed").data("id", $this.data("id"));

        $.post("/article/userfolders", { feedId: $this.data("id") }, function (result) {
            $loading.hide();
            $loadingremove.hide();
            $.each(result.toadd, function (i, n) {
                $("<a href='#' class='folder' data-feedid='" + $this.data("id") + "' data-folderId='" + n.Id + "'>" + n.Name + "</a>").appendTo($ul.find(".folders"));
            });
            if (result.toadd.length == 0) {
                $loading.show().text("No available folders");
            }

            $.each(result.toremove, function (i, n) {
                $("<a href='#' class='remove-folder' data-feedid='" + $this.data("id") + "' data-folderId='" + n.Id + "'>" + n.Name + "</a>").appendTo($ul.find(".folders-toremove"));
            });
            if (result.toremove.length == 0) {
                $loadingremove.show().text("No folders selected");
            }
        });
    });

    $("#new-folder").magnificPopup({
        type: 'inline',
        midClick: true,
        closeOnBgClick: true,
        callbacks: {
            open: function () {
                setTimeout(function () {
                    $("#new-folder-name").focus();
                }, 200);
            }
        }
    });

    $("#new-folder-name").keypress(function (e) {
        if (e.which == 13) {
            $(".js-newfolder").click();
        }
    });

    $(".js-newfolder").click(function (e) {
        e.preventDefault();
        var folderName = $("#new-folder-name").val();
        if (folderName.length == 0) {
            alert("Folder name empty");
            return;
        }

        $.post("/article/newfolder", { name: folderName }, function (response) {
            if (!response.error) {
                var html =
                    '<li class="folder"> \
                            <a data-id=' + response.id + ' href="/articles?tab=folder&folder=' + response.id + '"> \
                                <i class="fa fa-folder-o"></i> \
                                <span id="myfeeds">' + folderName + '</span> \
                            </a> \
                        </li>';
                $(html).insertBefore("#new-folder");

                if ($("li.folder").length == 1) {
                    // Instance the tour
                    var tourFolders = new Tour({
                        name: "tour-new-folder",
                        template: "<div class='popover tour'> \
                                            <div class='arrow'></div> \
                                            <h3 class='popover-title'></h3>\
                                            <div class='popover-content'></div>\
                                            <div class='popover-navigation'>\
                                                <button class='btn btn-default' style='margin-bottom: 10px;' data-role='end'>Dismiss</button>\
                                            </div>\
                                            </nav>\
                                            </div>",
                        steps: [
                            {
                                element: ".nav-vertical li.folder",
                                title: "You just created a new folder",
                                content: "Your folders will appear here! To place the feeds in the folder, click on the icon next to the feed and select 'Add to folder' <br/> <a target='_blank' href='/assets/uploads/folders.jpg'>Click here to see the image!</a> "
                            }
                        ]
                    });
                    tourFolders.init();
                    tourFolders.start();
                }

                $.magnificPopup.instance.close();
            } else {
                alert(response.error);
            }
            $("#new-folder-name").val('')
        });
    });

    $(document).on("click", ".ignore-feed", function (e) {
        e.preventDefault();
        if (confirm('This will ignore all the articles from this feed, are you sure?')) {
            $.post("/Article/IgnoreFeed", { feedId: $(this).data("id") }, function () {
                document.location.reload(true);
            });
        }
    });

    $(".ignore-article").click(function (e) {
        e.preventDefault();
        $.post("/Article/IgnoreArticle", { articleId: $(this).data("id") }, function () {
            document.location.reload(true);
        });
    });

    $(".badge").click(function () { $(this).parent().click(); });

    $("#day").click(function (e) {
        e.preventDefault();
    });
});

(function ($) {
    window.onbeforeunload = function (e) {
        $.cookie('scrollposition', $(window).scrollTop().toString());
    };
})(jQuery);



$(document).ready(function () {

    $("#months").change(function () {
        var val = $(this).val();
        if (val == 'date') {
            $("#date").show();
        } else {
            $("#date").hide();
        }
    });

    $("#edit-interesting").click(function (e) {
        e.preventDefault();
        $(".block").removeClass("dno");
        $(".block .dno").removeClass("dno");
        $(".tags2 a").each(function () {
            var $this = $(this);
            $this.removeAttr("href");
            var tagName = $(this).text();

            var span = document.createElement("span");
            $(span)
                .addClass("delete-tag")
                .appendTo($this)
                .click(function (e) {
                    var tagName = $this.text();
                    removeUserTag(tagName, $this.hasClass("jsignore"), function () {
                        $this.parent().remove();
                    });
                });
        });
        $(this).hide();
    });

    $(".delete-tag").click(function (e) {
        e.preventDefault();
        if (!confirm('Are you sure you want to remove this tag?')) { return false; }
        var $this = $(this);
        var tagName = $this.parent().text();
        removeUserTag(tagName, $this.parent().hasClass("jsignore"), function () {
            $this.parent().remove();
        });
    });


    function extractor(query) {
        var result = /([^,]+)$/.exec(query);
        if (result && result[1])
            return result[1].trim();
        return '';
    }

    window.query_cache = {};
    $(".autocomplete").each(function () {
        var $this = $(this);
        $this.typeahead({
            source: function (query, process) {
                // if in cache use cached value, if don't wanto use cache remove this if statement
                if (query_cache[query]) {
                    process(query_cache[query]);
                    return;
                }
                if (typeof searching != "undefined") {
                    clearTimeout(searching);
                    process([]);
                }
                searching = setTimeout(function () {
                    return $.getJSON(
                        "/home/searchtags",
                        { q: query },
                        function (data) {
                            // save result to cache, remove next line if you don't want to use cache
                            query_cache[query] = data;
                            // only search if stop typing for 300ms aka fast typers
                            return process(data);
                        }
                    );
                }, 300); // 300 ms
            },
            autoSelect: false,
            updater: function (item) {
                addUserTag(item, $this);
                return "";
            },
            matcher: function (item) {
                return item;
                var tquery = extractor(this.query);
                if (!tquery) return false;
                return ~item.toLowerCase().indexOf(tquery)
            },
            highlighter: function (item) {
                var query = extractor(this.query).replace(/[\-\[\]{}()*+?.,\\\^$|#\s]/g, '\\$&')
                return item.replace(new RegExp('(' + query + ')', 'ig'), function ($1, match) {
                    return '<strong>' + match + '</strong>'
                })
            }
        });
    });

    $('.autocomplete').focus(function () {
        $(this).val('');
    });

    $(".autocomplete").keyup(function (e) {
        var val = $(this).val();
        if (val != '' && e.keyCode == 13) {
            addUserTag(val, $(this));
        }
    });
});

function addUserTag(name, input) {
    var ignored = $(input).hasClass("jsignore");
    $.post("/Json/AddFavoriteOrIgnoredTag", { name: name, ignore: ignored }, function (result) {
        if (result) {
            var $container = $(input).closest(".block").find(".tags2");

            var a = document.createElement("a");
            $(a)
                .attr("href", "/tags/" + result)
                .addClass("post-tag user-tag" + (ignored ? " jsignore" : ""))
                .text(result)
                .appendTo($container);

            var span = document.createElement("span");
            $(span)
                .addClass("delete-tag")
                .addClass("closeTag")
                .appendTo(a)
                .click(function (e) {
                    var $this = $(this);
                    var tagName = $(a).text();
                    removeUserTag(tagName, $(a).hasClass("jsignore"), function () {
                        $this.parent().remove();
                    });
                });
            $(".tags-notification").fadeIn(500);
        }
    });
}

function removeUserTag(name, ignored, cb) {
    $.post("/Json/RemoveFavoriteOrIgnoredTag", { name: name, ignore: ignored }, function (result) {
        if (result) {
            cb();
            $(".tags-notification").fadeIn(500);
        }
    });
}