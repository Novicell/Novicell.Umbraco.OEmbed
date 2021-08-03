## Example
The following example illustrates embedding a video
```ruby

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
The following example illustrates embedding 'Any' type
```ruby
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
