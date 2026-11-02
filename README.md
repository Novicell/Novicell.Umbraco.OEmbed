# Novicell.Umbraco.OEmbed

An [oEmbed](https://oembed.com/) integration for [Umbraco CMS](https://umbraco.com/). It lets editors paste a URL (YouTube, Vimeo, Twitter/X, etc.) and have Umbraco resolve the rich embed (HTML, thumbnail, dimensions and metadata) through the oEmbed protocol.

Built on .NET 10 and Umbraco's built-in embed provider infrastructure.

> **Beta.** Requires Umbraco 17. It replaces the Umbraco 9 version of this package: the stored value and the Razor model are different, and existing data types are converted when the site starts, see [Migrating from the Umbraco 9 version](#migrating-from-the-umbraco-9-version).

## Installation

```bash
dotnet add package Novicell.Umbraco.OEmbed --prerelease
```

The package registers itself through a composer, so nothing else is needed. See [Getting started](#getting-started) for the options.

## Versions

The major version of the package is the Umbraco major version it supports. It is built against Umbraco 17.0.0, so a site on any 17.x can install it, and a site on Umbraco 18 cannot: the package declares Umbraco 17.0.0 or higher but lower than 18, and a new Umbraco major version gets its own package version. The test site runs a newer Umbraco.

| Package | Umbraco | .NET |
| ------- | ------- | ---- |
| 17.x    | 17.x    | 10   |

Earlier versions of the package target Umbraco 9.

Embeds from X do not work on Umbraco 17.0.0 and 17.0.1: Umbraco's own X provider in those versions calls `http://publish.twitter.com/oembed`, which no longer answers. Umbraco 17.0.2 and later call `https://publish.x.com/oembed`.

## Features

- **OEmbed property editor** (`Novicell.OEmbed`) for storing embeds on documents, media and blocks.
- **Search:** the title, author, provider and description of each embed, and the URL the editor entered, are indexed by Examine, in the field named after the property. The provider's html and the technical fields are not, so a search for "iframe" or "video" does not match every embed. Rebuild the indexes after updating from a version that indexed the stored JSON. The Delivery API index is built from the property value and is not affected.
- **OEmbed resolution service** (`IOEmbedService`) that turns a URL into a strongly-typed oEmbed response.
- **Provider matching** via Umbraco's `EmbedProvidersCollection`, extended with Reddit, Spotify and TikTok.
- **Management API endpoint** for the backoffice to request embeds.
- **Backoffice UI** delivered as static assets (Lit + Vite + TypeScript).
- Reads **JSON** oEmbed responses: the request asks the provider for JSON (`format=json`), and keeps everything the provider returned.

## Project structure

| Path                                      | Description                                                                                                                           |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `src/Novicell.Umbraco.OEmbed`             | The NuGet package: oEmbed service, endpoint provider, property editor, Management API, composer and the `Client/` backoffice UI (TypeScript/Lit, built with Vite). |
| `tests/Novicell.Umbraco.OEmbed.Tests`     | NUnit tests for the service, validators and value converter.                                                                          |
| `src/Novicell.Umbraco.OEmbed.TestSite`    | A runnable Umbraco site (SQLite) for development and testing, with a ready-made setup imported by uSync.                              |

Inside the package, namespaces mirror folders: `Api`, `Composers`, `Configuration`, `EmbedProviders`, `Models`, `PropertyEditors` and `Services`. See [src/CLAUDE.md](src/CLAUDE.md) and [Client/ARCHITECTURE.md](src/Novicell.Umbraco.OEmbed/Client/ARCHITECTURE.md) for details.

## How it works

1. A URL is submitted (from the backoffice or via `IOEmbedService.GetOEmbedAsync`).
2. `OEmbedApiEndpointProvider` matches the URL against the registered `EmbedProvidersCollection` to find a provider's oEmbed API endpoint. A URL that no provider matches is `ProviderNotSupported`: the server never fetches the page an editor pasted.
3. The endpoint is called with `format=json` and the JSON response is read into an `OEmbedResponse`. A provider that can only answer with XML is not supported.
4. The result is returned as an `Attempt<OEmbedResponse?, OEmbedOperationStatus>`, surfacing clear status outcomes (e.g. `ProviderNotSupported`, `EmbedNotFound`, `InvalidResponse`).

## Getting started

### Register the package

Registration happens automatically via `OEmbedComposer`, so nothing is needed in a normal site. To register it by hand, call the builder extension from your composer or `Program.cs`; calling it when the composer has already registered the package does nothing:

```csharp
builder.AddNovicellOEmbed();
```

### Configuration

Configure options under the `Novicell:OEmbed` section in `appsettings.json`:

```json
{
    "Novicell": {
        "OEmbed": {
            "AllowPrivateNetworks": false
        }
    }
}
```

| Setting                | Default | Description                                                                                                   |
| ---------------------- | ------- | ------------------------------------------------------------------------------------------------------------- |
| `AllowPrivateNetworks` | `false` | Allows the server to call loopback and private network addresses. Off by default, see the security note below. |

A site that is not in Umbraco's list of embed providers is supported by adding a provider class; the package adds Reddit, Spotify and TikTok that way. Those classes are public (namespace `Novicell.Umbraco.OEmbed.EmbedProviders`), so a site that registers its embed providers itself can add them too, for example `builder.EmbedProviders().Append<TikTok>();`.

### Rendering an embed

The `html` of a saved embed is posted by the backoffice and is not verified, so do not write it to a page with `Html.Raw`. Use the helper, which is in Umbraco's `Umbraco.Extensions` namespace and so is available in a view without a `using`:

```cshtml
@Html.OEmbed(Model.Video)
```

It writes the html the provider returned **exactly as it is**, with nothing added or changed, when it is one ordinary iframe (YouTube, Vimeo, Spotify, SoundCloud and the like), and nothing otherwise. Ordinary means:

- exactly one `<iframe>` with its closing tag, and nothing else in or around it;
- only these attributes, each with a value of the expected shape: `src`, `width`, `height`, `title`, `allow`, `allowfullscreen`, `frameborder`, `scrolling`, `loading`, `referrerpolicy`, `sandbox`, `data-testid`, and a `style` that only sets how the frame looks and how big it is (no `position`, `z-index`, `margin`, `vertical-align` or `url()`, which could move it over the page);
- `src` is an absolute http(s) address, and not of the site's own host (an iframe of the site is not isolated from the page, so a script in what it loads, such as an SVG uploaded to the site, could reach it);
- no event handler such as `onload`, no `srcdoc`, no attribute given twice.

The check reads the text, so it cannot see where an address redirects to. To limit where frames can load from, redirects included, set a `frame-src` [Content-Security-Policy](https://developer.mozilla.org/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/frame-src) with the embed hosts your site uses, for example `frame-src https://www.youtube.com https://player.vimeo.com https://open.spotify.com https://w.soundcloud.com`. Behind a proxy, make sure the site sees the public host name (forwarded headers), as the check compares with it.

Markup with a script of its own (X and Reddit) is left out, and so is anything that does not pass. A photo or link embed has no html, so write its `OEmbed.Url` and `OEmbed.Title` and the URL the editor entered yourself. The site decides what to show instead, for example a link; the test site's `Views/Partials/oembed.cshtml` shows this.

To write any html as it is, scripts included, on a site that trusts its editors, and everything that can change a saved value, with that:

```cshtml
@Html.OEmbed(Model.Video, new OEmbedRenderOptions { Scripts = OEmbedScripts.Raw })
```

### Security note on providers

The live preview in the picker modal shows the HTML that an oEmbed provider has just returned to the server. A provider that returns a single `<iframe>` (YouTube, Vimeo, Spotify, TikTok and similar) is rendered as a sandboxed cross-origin iframe. Other markup, typically a `<blockquote>` plus the provider's script (X and Reddit), runs in a sandboxed iframe with an opaque origin. Its scripts cannot access the backoffice, even if a provider response is compromised. Because the isolated frame has no same-origin access or storage, some script-based providers may not render or behave fully in the preview.

Whatever the server requests on an editor's behalf is limited: only `http` and `https`, a 10 second limit for the whole request including reading the body, at most 3 redirects and a 1 MiB response. The server also only connects to publicly routable addresses. Loopback, private (RFC 1918), link-local (including cloud metadata at `169.254.169.254`), carrier-grade NAT, multicast and reserved ranges are refused, and the check is made on the address actually connected to, so redirects and DNS rebinding cannot get around it.

Set `AllowPrivateNetworks` to `true` if you need to call a provider on `localhost`, or if the server can only reach the internet through an outbound proxy. With the setting off, the server connects directly and ignores proxy settings, because a proxy would hide the real destination from the address check. With it on, no address check is made and the normal proxy settings apply, so the proxy has to restrict what can be reached. The setting is read when the HTTP client is created, so changing it needs a restart.

A saved value is different: the stored `oembed` JSON, including its `html`, is sent by the client and is not checked against the provider. The property editor therefore never runs it. Saved embeds are shown as a thumbnail or link card, and only the picker modal fetches a fresh response and runs it. The server rejects a saved value that has a URL other than http(s), and the published value converter removes any such URL from what a site receives (and leaves out an item whose own URL is not http(s)), so values written by uSync, a database change or an older version are covered too. Neither verifies the `html`. Write it to a page with `Html.OEmbed`, see [Rendering an embed](#rendering-an-embed), not with `Html.Raw`. The same goes for the fields that have no property on `OEmbedResponse` (`Extra`, read with `TryGetValue`): they are exactly what the provider sent, and unlike the known URL fields they are not checked.

## Delivery API

With Umbraco's [Delivery API](https://docs.umbraco.com/umbraco-cms/reference/content-delivery-api) enabled, an OEmbed picker property is an array with one object per embed. The names are camelCase and fixed, whatever the Razor model looks like, and a field the provider did not return is `null`:

```json
"video": [
  {
    "url": "https://vimeo.com/371827688",
    "oembed": {
      "type": "video",
      "version": null,
      "title": "Umbraco The Friendly CMS",
      "authorName": "Umbraco",
      "authorUrl": "https://vimeo.com/umbraco",
      "providerName": "Vimeo",
      "providerUrl": "https://vimeo.com/",
      "thumbnailUrl": "https://i.vimeocdn.com/video/829453327-f06c….jpg",
      "thumbnailHeight": 166,
      "thumbnailWidth": 295,
      "html": "<iframe src=\"https://player.vimeo.com/video/371827688\" …></iframe>",
      "url": null,
      "height": 240,
      "width": 426,
      "cacheAge": null,
      "extra": { "video_id": 371827688, "is_plus": "0", "account_type": "enterprise" }
    }
  }
]
```

- `type` is `video`, `photo`, `link` or `rich`. `html` is for `video` and `rich`; `url` is the image for `photo`.
- `extra` holds everything else the provider returned, with the provider's own names (they are not camelCased), and is left out when there is nothing more. Like `html`, it is exactly what the provider sent and is not checked, so treat it as editor-supplied content.
- A property with nothing usable is `[]`, never `null`.
- A URL that is not http(s) is returned as `null`.
- `html` is what the provider returned when the editor picked the URL and is not verified, so a headless site should treat it as editor-supplied content, as with the Razor model.

### Headless sample

The test site has a headless version of its sample page at https://localhost:44366/headless/ (see [Running the test site](#running-the-test-site)): the same page as `Views/embedPage.cshtml`, built in the browser from the Delivery API with plain JavaScript, no build step. `wwwroot/headless/app.js` shows how to read the camelCase fields and `extra`, and `wwwroot/headless/safe-embed.js` is the rule of `Html.OEmbed` for a site that is not rendered by Umbraco: it reads the `html` with the browser's own parser (`DOMParser`, which runs nothing) and only writes it when it is exactly one ordinary iframe with allowed attributes and an http(s) `src`. Anything else, such as the X post, is left out and its caption link is what shows. Each page links to the other, and **Allow scripts** (`?raw=1`) writes the provider's html as it is on both, for a site that trusts its editors with that.

## Running the test site

The backoffice client is built into `wwwroot/App_Plugins/NovicellUmbracoOEmbed` and is not committed, so build it before running the test site:

```bash
npm --prefix src/Novicell.Umbraco.OEmbed/Client ci
npm --prefix src/Novicell.Umbraco.OEmbed/Client run build
```

Then run the test site, also from the repository root:

```bash
dotnet run --project src/Novicell.Umbraco.OEmbed.TestSite
```

It installs itself on first run. Log in at https://localhost:44366/umbraco with `admin@example.com` / `1234567890` (see `appsettings.Development.json`).

On a fresh install, uSync imports a ready-made setup from `uSync/v17`:

- **Embed page** document type with the **Embed page** template. `Views/embedPage.cshtml` and `Views/Partials/oembed.cshtml` show how to render the value: one embed or a collection, `Url` and `OEmbed`, the typed fields, and `TryGetValue` for a field the provider returned that has no property
- **OEmbed picker - single video** and **OEmbed picker - multiple** data types, to try both shapes of the value
- an **Embed page** with a video and four embeds (a video, a photo and two rich embeds)

The Delivery API is switched on with public access, so the sample page is also at https://localhost:44366/umbraco/delivery/api/v2/content, and https://localhost:44366/headless/ is the same page built from it, see [Headless sample](#headless-sample).

uSync exports changes when you save, so remember to commit `uSync/` if you change these types.

### Testing on other Umbraco versions

`scripts/Test-UmbracoVersions.ps1` (PowerShell 7) runs the test site on several Umbraco 17 versions, each on a fresh database of its own, and runs `scripts/backoffice-smoke.mjs` against it: the sample page, the Delivery API and the headless page render the embeds, and in the backoffice the picker retrieves a video, audio, a photo and script-based embeds and refuses an unsupported URL. It needs the client's npm packages and Chrome, and stops a running test site first. The logs and screenshots end up in `artifacts/umbraco-versions/<version>/`.

```powershell
./scripts/Test-UmbracoVersions.ps1                                  # 17.0.0, 17.4.2 and 17.8.0
./scripts/Test-UmbracoVersions.ps1 -Versions "17.0.0=17.0.0", "17.6.2" # Umbraco version=uSync version
```

The test site's own versions are `TestSiteUmbracoVersion` and `TestSiteUSyncVersion` in `Directory.Packages.props`. The checks call the real providers, so a provider that is down fails its own line. See [scripts/README.md](scripts/README.md) for reading the results and changing the checks.

## Branches and releases

| Branch | Purpose |
|---|---|
| `v17/dev` | Development for Umbraco 17. Pull requests go here. |
| `v17/main` | Releases for Umbraco 17. Merge `v17/dev` into it when a version is ready. |

The next Umbraco LTS gets its own pair, for example `v21/dev` and `v21/main`.

**CI** (`.github/workflows/ci.yml`) runs on every pull request and on pushes to `v*/dev` and `v*/main`. It builds and tests the client (in a real browser) and the .NET solution, and uploads the NuGet package as an artifact, so it can be downloaded and tried before a release. The client's output is not committed, so build it before `dotnet build`: run `npm ci && npm run build` in `src/Novicell.Umbraco.OEmbed/Client` first. `dotnet pack` builds the client itself. `dotnet pack --no-build` refuses to pack when the client is missing, older than its sources (including a source file added, deleted or renamed since), or built after the last `dotnet build`.

**Releasing** (`.github/workflows/release.yml`):

1. Merge `v17/dev` into `v17/main`.
2. On GitHub, create a release from `v17/main` with a tag like `v17.0.0` or `v17.0.0-beta.1`. Tick **Set as a pre-release** for versions with `-beta` and the like.
3. Approve the deployment to the `nuget` environment when GitHub asks.

The workflow first runs the smoke test (`.github/workflows/smoke.yml`, see [scripts/README.md](scripts/README.md)) on Umbraco 17.0.0, 17.4.2 and 17.8.0, and publishes nothing if it fails. The smoke test can also be started by hand from the Actions tab. The checks call the real providers, so if a provider is down, re-run the failed job once it is back. The workflow then builds with the version from the tag, publishes to NuGet.org and attaches the package to the release; the package is also kept with the workflow run. Once the new version is on nuget.org, it runs the smoke test again on a site that installs it from there (`.github/workflows/nuget-test.yml`), which can also be started by hand for any published version. The versions in the repository's files (the csproj, `package.json` and the backoffice manifest) don't need updating by hand; the tag is written into them, and into the manifest's cache-busting `?v=` parameters. It refuses to publish if:

- the tag's major version doesn't match the Umbraco version in `Directory.Packages.props`
- the tag isn't on `v<major>/main`
- the release's pre-release setting doesn't match the version

### One-off setup

- **NuGet.org:** under the Novicell account, add a Trusted Publishing policy for repository owner `Novicell`, repository `Novicell.Umbraco.OEmbed`, workflow file `release.yml` and environment `nuget`.
- **GitHub:**
  - create the environment `nuget`, with required reviewers
  - add the repository variable `NUGET_USER`, the NuGet.org user name that owns the policy
  - protect `v17/dev` and `v17/main` with the CI check as required

## Requirements

- .NET 10 SDK
- Umbraco CMS 17

## Migrating from the Umbraco 9 version

The Umbraco 17 package is a rewrite. The property editor alias is unchanged, so existing data types and content keep working, and a package migration converts what has to change:

- **Data types are converted by a migration.** The first time the site starts, a package migration gives each of those data types the new property editor UI (an upgraded data type has none, so the picker would not show) and converts the old `Type` setting (`Video`, `Photo`, `Rich`, `Link`, or empty for any) to the new `Allowed types`. It runs once, only changes data types that have this editor and the old shape, and does nothing on a new site. It logs what it did, starting with `oEmbed migration:`. If a data type cannot be saved the migration fails and the site does not start, with an error that names the data type and the reason; nothing is changed, and it runs again at the next start. Content is not touched: a saved value is read as it is, and written in the new shape the next time it is edited. After it has run, export the changed data types if you use uSync, or the next import switches them back, and back up the database first, as with any migration.
- A property stores an array of `{ url, oembed }` items (it was one object), and the Razor model is `OEmbedValue?` when the data type does not allow multiple embeds and `IEnumerable<OEmbedValue>` when it does, like Umbraco's media picker (the Delivery API is always an array). Each item has `.Url` and `.OEmbed`, which is the `OEmbedResponse` (type, html, title, thumbnail and so on, as typed properties). The `oembed` object in the database is the response as the provider returned it, with the provider's names (`author_name`, `thumbnail_url` and so on) and every other field it sent, which is also what the old version stored, so the only difference in the database is the array around the items. Fields without a property are kept and read with `OEmbedResponse.TryGetValue<T>("name", out value)`. The property value converter reads the stored object leniently (a width sent as text is still a number, a URL that cannot be read is null), so one odd value never makes the whole property empty. Content saved by the old version is read as it is, the single-object shape included, and shows in the backoffice, in Razor and in the Delivery API without being converted.
- The data type has `Allowed types`, `Multiple` and `Min`/`Max` settings instead of one type setting.
- `IOEmbedService.GetOEmbedAsync` now takes an `OEmbedRequest` and returns an `Attempt` with an `OEmbedOperationStatus`; it does not throw.
- The `Novicell:OEmbed:Autodiscover` setting is gone and is ignored if it is still in `appsettings.json`: the server no longer fetches a page an editor pasted to look for an oEmbed endpoint. A site that is not in Umbraco's list of embed providers is supported by adding a provider class (Reddit, Spotify and TikTok are added by the package that way).

## License

MIT, see [LICENSE.md](LICENSE.md). Copyright © Novicell.
