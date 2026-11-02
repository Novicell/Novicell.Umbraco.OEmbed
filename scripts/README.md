# Smoke test on Umbraco versions

`Test-UmbracoVersions.ps1` runs the test site on one or more Umbraco 17 versions and checks the package on each with `backoffice-smoke.mjs`.

## Before you run it

- PowerShell 7 and Chrome.
- The client's npm packages: `npm --prefix src/Novicell.Umbraco.OEmbed/Client ci`.
- Port 44366 free. A running test site is stopped by the script.

## Run it

From the repository root:

```powershell
./scripts/Test-UmbracoVersions.ps1                                    # 17.0.0, 17.4.2 and 17.8.0
./scripts/Test-UmbracoVersions.ps1 -Versions "17.0.0=17.0.0", "17.6.2"   # Umbraco version=uSync version
./scripts/Test-UmbracoVersions.ps1 -SkipClientBuild                   # the client is already built
```

Give a uSync version for an Umbraco below 17.3 (uSync 17.0.x); otherwise the test site's own uSync is used. Each version takes about 1.5 minutes.

On GitHub, `.github/workflows/smoke.yml` runs it before every release, and can be started by hand from the Actions tab with a list of versions. The logs and screenshots are attached to the run.

## Read the result

The table at the end has a column per stage: Build, Start, Pages and Backoffice. The exit code is 1 if anything failed. Each version has a folder in `artifacts/umbraco-versions/<version>/`:

| File | What |
|---|---|
| `build.log` | the build of the test site |
| `site.log` | the site's log |
| `smoke.log` | every check, as `ok`, `FAIL`, `skip` or `info` |
| `*.png` | the picker for each embed, and `stopped.png` if the test stopped |

- `info` lines are errors from a provider's own script (TikTok's player, for example). They do not fail the test.
- The checks call the real providers, so a provider that is down fails its own line. Run that version again before you look for a bug.

## Test a published package

`-PackageVersion` runs the same stages on a copy of the test site that installs that version from nuget.org instead of building the package from the repository, as a site would get it:

```powershell
./scripts/Test-UmbracoVersions.ps1 -PackageVersion 17.0.0-beta.1
```

The copy is made in `artifacts/nuget-site`, with its own `Directory.Build.props` and `Directory.Packages.props` so the repository's don't apply, and restores into its own empty `artifacts/nuget-site-packages`, so a locally packed version with the same number isn't used instead. The Pages stage also checks that the site serves the package's manifest with that version and its `?v=`. The results end up in `artifacts/nuget-package/<package version>/<Umbraco version>/`.

On GitHub, `.github/workflows/nuget-test.yml` runs it after every release, once the new version is on nuget.org (that takes a few minutes), and can be started by hand from the Actions tab with any published version.

## Run only the browser test

Against a test site that is already running:

```powershell
cd src/Novicell.Umbraco.OEmbed/Client
node ../../../scripts/backoffice-smoke.mjs https://localhost:44366
```

## Change the checks

The embeds tried in the picker are the `EMBEDS` list at the top of `backoffice-smoke.mjs`: a name, a URL, the expected oEmbed type and the expected preview (`iframe`, `img` or `srcdoc`). Add `since: '17.x.y'` to skip an embed on older Umbraco versions whose own provider for it is broken, with a comment saying why.
