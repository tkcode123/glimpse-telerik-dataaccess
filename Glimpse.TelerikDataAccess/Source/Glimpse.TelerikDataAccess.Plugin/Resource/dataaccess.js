
(function ($, pubsub, data, elements) {
    var download = function () {
        var d = window.glimpse.data.currentData();
        //var j = JSON.stringify(d);
        var dt = new Date();
        var dts = dt.toJSON().replace('.', '_').replace(/-/g,'').replace(/:/g,'');
        var name = "glimpsedata_"+dts+".json";
        console.log("Store data on server to file "+name);
        var htmlUrl = window.glimpse.data.currentMetadata().resources.glimpse_telerikdataaccess_plugin_resource_htmlresource;
        $.ajax({
            // the URL for the request
            url: htmlUrl,

            // the data to send (will be converted to a query string)
            data: {
                sub: 'store',
                requestId: d.requestId,
                name: name
            },

            // whether this is a POST or GET request
            type: "GET",

            // the type of data we expect back
            dataType: "text",

            // code to run if the request succeeds;
            // the response is passed to the function
            success: function (text) {
                text = text.replace("{name}", name);
                alert(text);
            },

            // code to run if the request fails; the raw request and
            // status codes are passed to the function
            error: function (xhr, status, errorThrown) {
                alert("There was a problem storing the data: "+errorThrown+" Status="+status);
            },

            // code to run regardless of success or failure
            complete: function (xhr, status) {
                pubsub.publish("trigger.tab.select.glimpse_dataaccess", { key: 'glimpse_dataaccess' });
            }
        });
    };
    pubsub.subscribe("trigger.tab.select.download", download);
    var requestData = data.currentData();

    var old = elements.tabHolder().find('.glimpse-tabitem-ajax').first().parent();
    var myNewElement = $('<li class="glimpse-tab glimpse-tabitem-download glimpse-permanent" data-glimpsekey="download">Download</li>');
    myNewElement.appendTo(old);   

})(jQueryGlimpse, glimpse.pubsub, glimpse.data, glimpse.elements);

