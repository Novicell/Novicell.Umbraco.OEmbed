# Novicell.Umbraco.OEmbed

An Umbraco 17 property editor: an editor pastes a URL, the package asks the provider's oEmbed endpoint, and the response is stored. One Razor class library, `src/Novicell.Umbraco.OEmbed`, with the backoffice client (Lit, Vite) in `Client/`; see [Client/ARCHITECTURE.md](./Novicell.Umbraco.OEmbed/Client/ARCHITECTURE.md). Namespaces mirror folders.

## Commands

- Client, in `src/Novicell.Umbraco.OEmbed/Client`: `npm ci`, `npm test`, `npm run build`. The build writes to `wwwroot/App_Plugins/NovicellUmbracoOEmbed` (git-ignored); build it before `dotnet build`. `dotnet pack` builds it itself.
- .NET, from the root: `dotnet build -c Release` (warnings are errors), `dotnet test`.
- After changing the Management API: run the test site and `npm run generate-client`.
- Test site: `src/Novicell.Umbraco.OEmbed.TestSite` (https://localhost:44366, content from uSync).
- Smoke test on several Umbraco versions: `scripts/Test-UmbracoVersions.ps1`, see [scripts/README.md](../scripts/README.md).

## Rules

- **One project on purpose.** Split out another (a Core package, say) only when a second, different consumer needs the same types.
- **One entry point:** `AddNovicellOEmbed()`, called by `OEmbedComposer`; safe to call twice. All services are transient.
- **Providers come from Umbraco's `EmbedProvidersCollection`.** A URL no provider matches is `ProviderNotSupported`; the pasted page is never fetched (Autodiscover was removed). A new platform is a provider class, see the `add-embed-provider` skill. The endpoint-provider interfaces are internal; the Reddit, Spotify and TikTok providers are public so a site can register them itself.
- **JSON only.** Requests send `format=json`; a non-JSON response is `InvalidResponse`. The service never throws: it returns `Attempt<OEmbedResponse?, OEmbedOperationStatus>`, and `InvalidResponse` also covers network errors and timeouts.
- **Outbound HTTP** goes through the named client in `OEmbedHttp` (http(s) only, 10 s, 3 redirects, 1 MiB), and only to public addresses unless `Novicell:OEmbed:AllowPrivateNetworks` is set.
- **The stored value is the provider's response, untouched:** an array of `{ "url", "oembed" }` with the provider's field names. No renaming or mapping anywhere; fields without a property are kept in `OEmbedResponse.Extra`. The Umbraco 9 version stored a single object, and `OEmbedDto.Deserialize` reads both shapes for good.
- **What reads the stored value cannot trust it:** the converter drops non-http(s) URLs and unusable items (`OEmbedUrlSafety`), because the save validator is not the only way a value reaches the database.
- **Razor and the Delivery API have separate contracts.** Razor gets `OEmbedValue` (a single value or an `IEnumerable`, depending on the data type's `Multiple`). The Delivery API gets `ApiOEmbedValue` with explicit camelCase names, pinned by `OEmbedDeliveryApiTests`, so renaming the Razor model can't change it.
- **The html is gated, never changed.** `Html.OEmbed` writes the provider's html only when `OEmbedHtmlGate` finds a single safe iframe; anything else needs `OEmbedScripts.Raw`. Backoffice previews of script markup run in an opaque-origin sandbox.
- **The Umbraco 9 editor alias `Novicell.OEmbed` is kept**, and a package migration (`ConvertUmbraco9DataTypes`) gives old data types the new editor UI. It fails loudly rather than half-converting.
- The search index gets each embed's title, author, provider and URL, not the raw JSON.

## Tests

- .NET (`tests/Novicell.Umbraco.OEmbed.Tests`, NUnit + NSubstitute): HTTP is stubbed with `StubHttpMessageHandler`; internals are visible to the test project.
- Client: `@web/test-runner` in Chromium, with MSW mocks in `Client/mocks`.
