namespace Novicell.Umbraco.OEmbed.Tests.TestSupport;

/// <summary>Stored property values, as they are in the database.</summary>
internal static class StoredValues
{
    // Copied from the stored value in the Umbraco 9 package's demo content
    // (Novicell.Umbraco.OEmbed.Website/uSync/v9/Content/home1.config): one object, with the
    // names of the oEmbed specification.
    internal const string Umbraco9 = """
        {
          "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
          "oembed": {
            "type": "video",
            "version": "1.0",
            "title": "Rick Astley - Never Gonna Give You Up (Official Music Video)",
            "author_name": "Rick Astley",
            "author_url": "https://www.youtube.com/c/RickastleyCoUkOfficial",
            "provider_name": "YouTube",
            "provider_url": "https://www.youtube.com/",
            "thumbnail_url": "https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg",
            "thumbnail_height": 360,
            "thumbnail_width": 480,
            "html": "<iframe width=\"200\" height=\"113\" src=\"https://www.youtube.com/embed/dQw4w9WgXcQ?feature=oembed\" frameborder=\"0\" allowfullscreen></iframe>",
            "url": null,
            "height": 113,
            "width": 200,
            "cache_age": 3600
          }
        }
        """;

    // What this version stores, as saved from the backoffice: an array of the same items, with the
    // names of the oEmbed specification, as the provider returned them.
    internal const string Current = """
        [
          {
            "url": "https://www.youtube.com/watch?v=XbWCjWBuTDU",
            "oembed": {
              "type": "video",
              "version": "1.0",
              "width": 200,
              "height": 113,
              "url": null,
              "html": "<iframe width=\"200\" height=\"113\" src=\"https://www.youtube.com/embed/XbWCjWBuTDU?feature=oembed\" frameborder=\"0\" allowfullscreen></iframe>",
              "title": "BMW 840i Harman Kardon Upgrade",
              "author_url": "https://www.youtube.com/@CambridgeCarAudio",
              "author_name": "Cambridge Car Audio",
              "provider_url": "https://www.youtube.com/",
              "provider_name": "YouTube",
              "thumbnail_url": "https://i.ytimg.com/vi/XbWCjWBuTDU/hqdefault.jpg",
              "thumbnail_width": 480,
              "thumbnail_height": 360,
              "cache_age": null
            }
          }
        ]
        """;
}
