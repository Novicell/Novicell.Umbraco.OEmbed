using Novicell.Umbraco.OEmbed.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace Novicell.Umbraco.OEmbed.PropertyEditors;

/// <summary>
/// What a search finds an embed by: its title, author, provider and description, and the URL the editor
/// entered, one text per embed. Without this Umbraco indexes the stored JSON as it is, so a search for
/// "iframe", "oembed" or "video" would match every embed, and the index would carry the provider's html.
/// </summary>
/// <remarks>
/// The same factory is used for an embed inside a Block List, Block Grid or rich text block, as Umbraco asks the
/// editor of each nested property. A value of the old version (one object) is read as well, and a value that
/// cannot be read gives nothing instead of failing the indexing of the page.
/// </remarks>
internal sealed class OEmbedPickerPropertyIndexValueFactory : IPropertyIndexValueFactory
{
    /// <inheritdoc />
    public IEnumerable<IndexValue> GetIndexValues(
        IProperty property,
        string? culture,
        string? segment,
        bool published,
        IEnumerable<string> availableCultures,
        IDictionary<Guid, IContentType> contentTypeDictionary)
    {
        List<string> texts = OEmbedDto.Deserialize(property.GetValue(culture, segment, published) as string)
            .Select(Text)
            .OfType<string>()
            .ToList();

        return texts.Count == 0
            ? []
            : [new IndexValue { Culture = culture, FieldName = property.Alias, Values = texts }];
    }

    private static string? Text(OEmbedDto? embed)
    {
        if (embed?.OEmbed is not { } oembed)
        {
            return null;
        }

        oembed.TryGetValue("description", out string? description);

        var parts = new[] { oembed.Title, oembed.AuthorName, oembed.ProviderName, description, embed.Url }
            .Where(part => !string.IsNullOrWhiteSpace(part));

        return string.Join(' ', parts);
    }
}
