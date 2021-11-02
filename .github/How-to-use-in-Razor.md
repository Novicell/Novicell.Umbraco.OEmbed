## Examples

As you'll see below, this OEmbed package doesn't do all that much for you outisde the backoffice. In true Umbraco style it tries very hard to not be opinionated when it comes to presentation.

Here are a few examples of how you *could* embed the responses. Some properties are available on most OEmbed responses  [as per the specs](https://oembed.com/#section2) and can be accessed directly (like ``Type`` or ``Html``), while others are specific for each type. Others again might be entirely custom for the provider. Those "custom" properties are accessed via ``TryGetValue(...)``.

###Video
In this example we make sure to honor the aspect ratio of the video when presenting it, by sizing the container before outputting the HTML from the OEmbed response.

Also notice the two ways of getting properties from the OEmbedResponse object

```csharp
@if (Model.PageVideo?.OEmbed is OEmbedResponse video)
{
    var ratio = video.Width.HasValue && video.Height.HasValue ?
        (double)video.Height.Value / (double)video.Width.Value :
        0d;

    <div class="embed" data-ratio="@ratio">
        <div class="embed__item embed__item--@(video.Type?.ToLower())" style="padding-bottom: @Math.Round(ratio * 100, 2).ToString(CultureInfo.InvariantCulture)%;">
            @Html.Raw(video.Html)
        </div>
        
        @if (video.TryGetValue<string>("upload_date", out var uploadDateValue) &&
             DateTime.TryParse(uploadDateValue, out var uploadDate))
        {
            <p>Upload from @video.ProviderName: @uploadDate</p>
        }
    </div>
}
```

###'Any'
Knowing the type of OEmbed data in the response is quite advantageous, as _not_ knowing it severely limits what you can do with the response data. As long as you remember to use ``TryGetValue(...)`` it should be safe enough, though.

```csharp
@if (Model.PageAny?.OEmbed is OEmbedResponse any)
{
    if (!string.IsNullOrWhiteSpace(Model.PageAny?.Url))
    {
        <div class="embed">
            <div class="embed__item embed__item--@(any.Type?.ToLower())">
                @Html.Raw(any.Html)
            </div>

            @if (any.TryGetValue("upload_date", out string uploadDateValue) &&
        DateTime.TryParse(uploadDateValue, out var uploadDate))
            {
                <p>Upload from @any.ProviderName: @uploadDate</p>
            }
        </div>
    }
}
```
