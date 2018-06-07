$(document).ready(function () {
    // Instance the tour
    var tour2 = new Tour({
        name: "tour-new-feeds",
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
                element: "#myfeeds",
                title: "Thank you for uploading feeds",
                content: "You may not see the articles for a couple of minutes untill we download it (if you uploaded OPML file the import could take up to 15 minutes) but now you have a tab that only contains your uploaded/subscribed feeds without any suggestions."
            }
        ]
    });

    // Initialize the tour
    tour2.init();

    // Start the tour
    tour2.start();
    //tour.restart();
});