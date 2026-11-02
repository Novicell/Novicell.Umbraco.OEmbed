namespace Novicell.Umbraco.OEmbed.Models;

/// <summary>
/// What <c>Html.OEmbed</c> does with the html of an embed.
/// </summary>
public enum OEmbedScripts
{
    /// <summary>
    /// The html is written to the page as it is, but only when it is exactly one safe iframe (YouTube, Vimeo,
    /// Spotify, SoundCloud and the like). Markup that runs a script of its own (X, TikTok, Reddit) is left out.
    /// </summary>
    Block = 0,

    /// <summary>
    /// Whatever html the provider returned is written to the page as it is, scripts included. Use it only on
    /// a site where the editors, and everything that can change a saved value, are trusted with that.
    /// </summary>
    Raw = 1,
}

/// <summary>
/// Options for <c>Html.OEmbed</c>.
/// </summary>
public sealed record OEmbedRenderOptions
{
    /// <summary>
    /// What to do with html that is not a single safe iframe. The default, <see cref="OEmbedScripts.Block"/>,
    /// leaves it out.
    /// </summary>
    public OEmbedScripts Scripts { get; init; } = OEmbedScripts.Block;
}
