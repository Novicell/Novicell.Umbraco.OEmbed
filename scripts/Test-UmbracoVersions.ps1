# See scripts/README.md.
[CmdletBinding()]
param(
    [string[]] $Versions = @("17.0.0=17.0.0", "17.4.2=17.3.2", "17.8.0=17.3.2"),
    [string] $Url = "https://localhost:44366",
    [int] $StartTimeoutSeconds = 240,
    [switch] $SkipClientBuild,
    # A version published on nuget.org: the tests run on a copy of the test site that installs it from there
    [string] $PackageVersion
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$testSite = Join-Path $root "src/Novicell.Umbraco.OEmbed.TestSite"
$client = Join-Path $root "src/Novicell.Umbraco.OEmbed/Client"
$smoke = Join-Path $PSScriptRoot "backoffice-smoke.mjs"

if ($PackageVersion) {
    $site = Join-Path $root "artifacts/nuget-site"
    # An empty package folder of its own, so a package with the same version from a local pack isn't used instead
    $sitePackages = Join-Path $root "artifacts/nuget-site-packages"
    $out = Join-Path $root "artifacts/nuget-package/$PackageVersion"
    $siteProcessName = "NugetSite"
    # Nothing from the repository is built, so the client isn't either
    $SkipClientBuild = $true
} else {
    $site = $testSite
    $out = Join-Path $root "artifacts/umbraco-versions"
    $siteProcessName = "Novicell.Umbraco.OEmbed.TestSite"
}

# The test site's own versions, for an Umbraco version given without a uSync version
$packages = Get-Content (Join-Path $root "Directory.Packages.props") -Raw
$defaultUSync = [regex]::Match($packages, "<TestSiteUSyncVersion[^>]*>([^<]+)<").Groups[1].Value
$icuVersion = [regex]::Match((Get-Content (Join-Path $testSite "Novicell.Umbraco.OEmbed.TestSite.csproj") -Raw), "<UmbracoAppLocalIcuVersion[^>]*>([^<]+)<").Groups[1].Value

function Stop-TestSite([System.Diagnostics.Process] $process) {
    # The tree: dotnet run starts the site as a child. The name is for a site started before this script;
    # Linux shortens process names to 15 characters, so it is not found there.
    if ($process -and -not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit(15000) | Out-Null
    }
    Get-Process $siteProcessName -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
}

function Get-Status([string] $path) {
    try {
        (Invoke-WebRequest "$Url$path" -SkipCertificateCheck -TimeoutSec 30 -SkipHttpErrorCheck).StatusCode
    } catch {
        0
    }
}

if (-not $SkipClientBuild) {
    Write-Host "Building the client" -ForegroundColor Cyan
    npm --prefix $client run build | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The client build failed." }
}

# The test site without its project file, and one that installs the package from nuget.org instead of building it
function New-NugetSite {
    Remove-Item $site, $sitePackages -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $site | Out-Null
    Get-ChildItem $testSite -Exclude "bin", "obj", "umbraco", "*.csproj" | Copy-Item -Destination $site -Recurse

    # The nearest of each is used, so the repository's own (central package versions, warnings as errors) don't apply
    Set-Content (Join-Path $site "Directory.Build.props") "<Project />"
    Set-Content (Join-Path $site "Directory.Packages.props") "<Project />"
    Set-Content (Join-Path $site "NugetSite.csproj") @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Novicell.Umbraco.OEmbed.TestSite</RootNamespace>
    <RestorePackagesPath>$([System.Security.SecurityElement]::Escape($sitePackages))</RestorePackagesPath>
    <CompressionEnabled>false</CompressionEnabled>
    <CopyRazorGenerateFilesToPublishDirectory>true</CopyRazorGenerateFilesToPublishDirectory>
    <RazorCompileOnBuild>false</RazorCompileOnBuild>
    <RazorCompileOnPublish>false</RazorCompileOnPublish>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Novicell.Umbraco.OEmbed" Version="$PackageVersion" />
    <PackageReference Include="Umbraco.Cms" Version="`$(TestSiteUmbracoVersion)" />
    <PackageReference Include="Umbraco.Cms.DevelopmentMode.Backoffice" Version="`$(TestSiteUmbracoVersion)" />
    <PackageReference Include="uSync" Version="`$(TestSiteUSyncVersion)" />
  </ItemGroup>

  <!-- App-local ICU, as in the test site -->
  <ItemGroup Condition="!`$([MSBuild]::IsOSPlatform('osx'))">
    <PackageReference Include="Microsoft.ICU.ICU4C.Runtime" Version="$icuVersion" />
    <RuntimeHostConfigurationOption Include="System.Globalization.AppLocalIcu" Value="$icuVersion" />
  </ItemGroup>
</Project>
"@
}

function Assert-PortFree {
    # Otherwise the new site can't start, and the checks would run against whatever answers instead
    if ((Get-Status "/") -ne 0) {
        throw "Something already answers on $Url. Stop it and run the script again."
    }
}

Stop-TestSite
Assert-PortFree
if ($PackageVersion) {
    Write-Host "Testing Novicell.Umbraco.OEmbed $PackageVersion from nuget.org" -ForegroundColor Cyan
    New-NugetSite
}
$results = @()

foreach ($entry in $Versions) {
    Assert-PortFree
    $umbraco, $usync = $entry.Split("=")
    if (-not $usync -and $PackageVersion) { $usync = $defaultUSync }
    $runOut = Join-Path $out $umbraco
    New-Item -ItemType Directory -Force $runOut | Out-Null
    $log = Join-Path $runOut "site.log"
    $result = [ordered]@{ Umbraco = $umbraco; uSync = $usync; Build = "-"; Start = "-"; Pages = "-"; Backoffice = "-" }
    Write-Host "`n=== Umbraco $umbraco" -ForegroundColor Cyan

    $buildArgs = @("build", $site, "-c", "Release", "-v", "q", "-p:TestSiteUmbracoVersion=$umbraco")
    if ($usync) { $buildArgs += "-p:TestSiteUSyncVersion=$usync" }
    dotnet @buildArgs *> (Join-Path $runOut "build.log")
    if ($LASTEXITCODE -ne 0) {
        $result.Build = "failed (see build.log)"
        $results += [pscustomobject]$result
        continue
    }
    $result.Build = "ok"

    # A fresh install each time: a new database (an older Umbraco cannot open a newer one's), and no
    # indexes or caches in TEMP, which are shared between databases.
    $data = Join-Path $site "umbraco/Data"
    Remove-Item (Join-Path $data "Umbraco.v$umbraco.sqlite.db*"), (Join-Path $data "TEMP") -Recurse -Force -ErrorAction SilentlyContinue
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__umbracoDbDSN = "Data Source=|DataDirectory|/Umbraco.v$umbraco.sqlite.db;Cache=Shared;Foreign Keys=True;Pooling=True"
    # Start-Process joins the arguments into one string, so a path with spaces needs its own quotes.
    $process = Start-Process dotnet -ArgumentList "run", "--project", "`"$site`"", "-c", "Release", "--no-build", "--no-launch-profile", "--urls", $Url `
        -RedirectStandardOutput $log -RedirectStandardError (Join-Path $runOut "site.err.log") -PassThru -NoNewWindow
    Remove-Item Env:ConnectionStrings__umbracoDbDSN, Env:ASPNETCORE_ENVIRONMENT

    $deadline = (Get-Date).AddSeconds($StartTimeoutSeconds)
    while ((Get-Date) -lt $deadline -and -not $process.HasExited -and (Get-Status "/") -ne 200) {
        Start-Sleep -Seconds 3
    }

    if ((Get-Status "/") -ne 200) {
        $result.Start = "failed (see site.log)"
        Stop-TestSite $process
        $results += [pscustomobject]$result
        continue
    }
    $result.Start = "ok"

    $pages = "/", "/headless/", "/umbraco/swagger/novicell-oembed/swagger.json" |
        ForEach-Object { "$_ $(Get-Status $_)" }
    $result.Pages = if ($pages -match " 200$" -and @($pages -notmatch " 200$").Count -eq 0) { "ok" } else { $pages -join ", " }

    # The backoffice files come from the package, with the version its release stamped into them
    if ($PackageVersion -and $result.Pages -eq "ok") {
        try {
            $manifest = Invoke-RestMethod "$Url/App_Plugins/NovicellUmbracoOEmbed/umbraco-package.json" -SkipCertificateCheck
            $scripts = @($manifest.extensions | Where-Object js | ForEach-Object js)
            $stale = @($scripts | Where-Object { -not $_.EndsWith("?v=$PackageVersion", [System.StringComparison]::OrdinalIgnoreCase) })
            if ($manifest.version -ne $PackageVersion -or $scripts.Count -eq 0 -or $stale.Count -gt 0) {
                $result.Pages = "the manifest is $($manifest.version), scripts $($scripts -join ', ')"
            }
        } catch {
            $result.Pages = "the manifest could not be read: $($_.Exception.Message)"
        }
    }

    Push-Location $client
    node $smoke $Url $runOut $umbraco | Tee-Object -FilePath (Join-Path $runOut "smoke.log") | Write-Host
    $result.Backoffice = if ($LASTEXITCODE -eq 0) { "ok" } else { "failed (see smoke.log)" }
    Pop-Location

    $errors = @(Select-String -Path $log -Pattern "\[\d\d:\d\d:\d\d (ERR|FTL)\]").Count
    if ($errors -gt 0) { $result.Pages += " ($errors errors logged)" }

    Stop-TestSite $process
    $results += [pscustomobject]$result
}

$rebuilt = $true
if (-not $PackageVersion) {
    Write-Host "`nBuilding the test site with its own versions again" -ForegroundColor Cyan
    $rebuildLog = Join-Path $out "rebuild.log"
    dotnet build $site -c Release -v q *> $rebuildLog
    $rebuilt = $LASTEXITCODE -eq 0
}

$results | Format-Table -AutoSize
if (-not $rebuilt) {
    Write-Host "Building the test site with its own versions failed, see $rebuildLog" -ForegroundColor Red
    exit 1
}
if ($results | Where-Object { $_.Build, $_.Start, $_.Pages, $_.Backoffice -ne "ok" }) { exit 1 }
