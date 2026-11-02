---
name: add-embed-provider
description: Add a new oEmbed provider (e.g. TikTok, Spotify, Reddit) to Novicell.Umbraco.OEmbed. Use when a paste-a-URL provider is reported as "not supported" and the platform has a public oEmbed endpoint.
---

# Adding an embed provider

A pasted URL is matched against Umbraco's `EmbedProvidersCollection` (`Services/OEmbedApiEndpointProvider.cs`); a URL no provider matches is `ProviderNotSupported`. The package never fetches the pasted page to look for an endpoint, so a provider class is the only way to support another platform.

## 1. Check the platform has a public oEmbed endpoint

- **Umbraco already ships** YouTube, X, Vimeo, Dailymotion, Flickr, SlideShare, Kickstarter, Getty Images, Ted, SoundCloud, Issuu, Hulu, Giphy, LottieFiles and others: [the list](https://docs.umbraco.com/umbraco-cms/extend-your-project/server-side-extensions/embedded-media-providers). Don't add those again.
- **Find the platform in [oembed.com/providers.json](https://oembed.com/providers.json).** Download it and search it; a summarised fetch truncates it. Note `endpoints[].url` and `endpoints[].schemes`. No entry usually means no public oEmbed API (Twitch and Imgur have none): say so instead of inventing one.

## 2. Call the endpoint yourself

With a real public URL of the platform (curl or WebFetch), check that it returns JSON with a valid `type`. The package always sends `format=json` and reads JSON only, so a provider that can only answer with XML can't be supported.

A request that fails from every non-browser tool with 401 or 403, but works in a browser, is bot protection (CodePen is like that). A server-side provider can't fix that: report it as a limitation.

## 3. Write the provider

Copy `src/Novicell.Umbraco.OEmbed/EmbedProviders/Spotify.cs` to `<Platform>.cs`, a public class on `OEmbedProviderBase`:

- `ApiEndpoint`: the endpoint URL from step 1.
- `UrlSchemeRegex`: one anchored regex per scheme (`https://open.spotify.com/*` becomes `@"^https?:\/\/open\.spotify\.com\/"`).
- `RequestParams`: empty, unless step 2 showed a parameter is required.
- `GetMarkupAsync`: `GetJsonBasedMarkupAsync`. The package's own service doesn't call it, but the class joins Umbraco's shared collection, and other Umbraco features may.

## 4. Register, test and document it

- Append it in `AddNovicellOEmbed` (`Configuration/UmbracoBuilderExtensions.cs`): `builder.EmbedProviders()...Append<Platform>()`.
- Add a `TestCaseData` with a real URL to `Providers()` in `tests/Novicell.Umbraco.OEmbed.Tests/EmbedProviders/EmbedProviderTests.cs`.
- Add it to the `EMBEDS` list in `scripts/backoffice-smoke.mjs`, with its type and preview (`iframe`, `img` or `srcdoc`).
- Add it to the README, which names the added providers under Features and in the provider section.

## 5. Verify

- `dotnet build -c Release` and `dotnet test`.
- In the test site, paste a live URL into the picker and check the preview and the `type`.
- If the html runs a script of its own (not a single iframe), `Html.OEmbed` leaves it out unless the site uses `OEmbedScripts.Raw`, and the backoffice preview runs it in a sandbox. Say so when you add such a provider.
