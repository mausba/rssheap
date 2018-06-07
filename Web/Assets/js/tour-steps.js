$(document).ready(function () {
    // Instance the tour
    var tour = new Tour({
        steps: [
        {
            element: "#logo",
            title: "Welcome to rssheap",
            content: "This will be a short introduction of how to navigate arround the site!"
        },
        {
            element: ".youarehere",
            //title: "Main navigation",
            content: "This is your main navigation menu. You can see all the articles sorted by votes, those that are published this week or month, untagged articles and your favorite ones"
        },
        {
            element: "#hide-label",
            //title: "Title of my step",
            content: "If you don't want to see the articles you already visited in the list, select this checkbox."
        },
        {
            element: "#search-for-tags",
            //title: "Title of my step",
            content: "Select your favorite tags here (c#,.net,java,ruby,...). We will filter articles on the list based on the tags you define here. <br/> Click on Edit to see more options (to select tags to ignore or to hide articles that are older than specific date in the past)."
        },
        {
            element: "#all-tags",
            //title: "Title of my step",
            content: "Check out all the available tags in the system here."
        },
        {
            element: "#submit-url",
            //title: "",
            content: "If you have a url or an OPML file from your favorite reader you can upload it here!"
        },
        {
            element: ".badge ",
            //title: "",
            content: "You are the moderator of the site. You get reputation for uploading feeds, tagging articles and being an active member. The more reputation you have, the more things you can do on the site!"
        },
        {
            element: ".article-post:first .title-box a",
            //title: "",
            content: "That's it, start reading cool articles now and as you read, tag and vote so we can make this site better for everybody."
        }
        ]
    });

    // Initialize the tour
    tour.init();

    // Start the tour
    tour.start();
    //tour.restart();
});