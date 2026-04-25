param(
    [Parameter(Mandatory = $false)]
    [string]$Version = "0.1.0.0",

    [Parameter(Mandatory = $false)]
    [string]$TargetAbi = "10.11.0.0",

    [Parameter(Mandatory = $false)]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [string]$TestArch = "x64",

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$CommitAndPush,

    [Parameter(Mandatory = $false)]
    [switch]$StageAll,

    [Parameter(Mandatory = $false)]
    [switch]$CreateRelease,

    [Parameter(Mandatory = $false)]
    [switch]$ForceRelease
)

$ErrorActionPreference = "Stop"

function Resolve-RepoInfo {
    $remote = git remote get-url origin 2>$null
    if (-not $remote) {
        throw "Could not read git remote 'origin'. Pass this repo through git first or add an origin remote."
    }

    if ($remote -match "github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(\.git)?$") {
        return @{
            Owner = $Matches.owner
            Repo = $Matches.repo
        }
    }

    throw "Could not parse GitHub owner/repo from origin remote: $remote"
}

function Set-BuildYamlVersion {
    param([string]$Path, [string]$VersionValue)

    $content = Get-Content $Path -Raw
    $content = $content -replace "(?m)^version:\s*.+$", "version: $VersionValue"
    Set-Content $Path $content -Encoding utf8
}

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: dotnet $($args -join ' ')"
    }
}

function New-Manifest {
    param(
        [string]$Path,
        [string]$Owner,
        [string]$Repo,
        [string]$VersionValue,
        [string]$Abi,
        [string]$Checksum
    )

    $sourceUrl = "https://github.com/$Owner/$Repo/releases/download/v$VersionValue/Jellyfin.Plugin.SleepGuard_$VersionValue.zip"
    $imageUrl = "https://raw.githubusercontent.com/$Owner/$Repo/main/images/logo.png"
    $timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

    $manifest = @(
        [ordered]@{
            guid = "7bb5959b-5a11-45da-b9db-52eed4456090"
            name = "SleepGuard"
            description = "Pauses or stops Jellyfin playback after configurable sleep-friendly thresholds."
            overview = "Sleep-friendly playback limits for Jellyfin."
            imageUrl = $imageUrl
            owner = $Owner
            category = "General"
            versions = @(
                [ordered]@{
                    version = $VersionValue
                    changelog = "SleepGuard $VersionValue release."
                    targetAbi = $Abi
                    sourceUrl = $sourceUrl
                    checksum = $Checksum
                    timestamp = $timestamp
                }
            )
        }
    )

    ConvertTo-Json -InputObject $manifest -Depth 8 | Set-Content $Path -Encoding utf8
}

function Invoke-GitCommitAndPush {
    param([string]$VersionValue, [switch]$All)

    if ($All) {
        git add -A
    } else {
        git add build.yaml manifest.json scripts/Build-PluginRepository.ps1 .github/workflows/release.yml README.md
    }

    $staged = git diff --cached --name-only
    if ($staged) {
        git commit -m "chore: release SleepGuard $VersionValue"
        git push origin HEAD
    } else {
        Write-Host "No manifest or release-script changes to commit."
    }
}

function Invoke-GitHubRelease {
    param(
        [string]$VersionValue,
        [string]$ZipPath,
        [switch]$Force
    )

    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI 'gh' was not found. Install it or create the GitHub release manually."
    }

    $tag = "v$VersionValue"
    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $null = & gh release view $tag 2>$null
        $releaseExists = $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }

    if ($releaseExists) {
        if (-not $Force) {
            throw "Release $tag already exists. Re-run with -ForceRelease to replace the uploaded zip."
        }

        gh release upload $tag $ZipPath --clobber
        return
    }

    gh release create $tag $ZipPath --title "SleepGuard $VersionValue" --notes "SleepGuard $VersionValue release."
}

$repoInfo = Resolve-RepoInfo
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $repoRoot "artifacts\SleepGuard"
$releaseDir = Join-Path $repoRoot "artifacts\release"
$zipPath = Join-Path $releaseDir "Jellyfin.Plugin.SleepGuard_$Version.zip"
$manifestPath = Join-Path $repoRoot "manifest.json"
$buildYamlPath = Join-Path $repoRoot "build.yaml"
$solutionPath = Join-Path $repoRoot "Jellyfin.Plugin.SleepGuard.sln"
$projectPath = Join-Path $repoRoot "src\Jellyfin.Plugin.SleepGuard\Jellyfin.Plugin.SleepGuard.csproj"

Write-Host "SleepGuard release build"
Write-Host "Repo: $($repoInfo.Owner)/$($repoInfo.Repo)"
Write-Host "Version: $Version"
Write-Host "Target ABI: $TargetAbi"

Push-Location $repoRoot
try {
    Set-BuildYamlVersion -Path $buildYamlPath -VersionValue $Version

    Invoke-DotNet restore $solutionPath
    Invoke-DotNet build $solutionPath -c $Configuration --no-restore /p:Version=$Version /p:AssemblyVersion=$Version /p:FileVersion=$Version

    if (-not $SkipTests) {
        Invoke-DotNet test $solutionPath -c $Configuration --arch $TestArch /p:Version=$Version /p:AssemblyVersion=$Version /p:FileVersion=$Version
    }

    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    if (Test-Path $releaseDir) {
        Remove-Item $releaseDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force $publishDir | Out-Null
    New-Item -ItemType Directory -Force $releaseDir | Out-Null

    Invoke-DotNet publish $projectPath -c $Configuration -o $publishDir /p:Version=$Version /p:AssemblyVersion=$Version /p:FileVersion=$Version

    $zipItems = @(
        Join-Path $publishDir "Jellyfin.Plugin.SleepGuard.dll"
        Join-Path $publishDir "Jellyfin.Plugin.SleepGuard.deps.json"
    )

    Compress-Archive -Path $zipItems -DestinationPath $zipPath -Force
    $checksum = (Get-FileHash $zipPath -Algorithm MD5).Hash.ToLowerInvariant()

    New-Manifest `
        -Path $manifestPath `
        -Owner $repoInfo.Owner `
        -Repo $repoInfo.Repo `
        -VersionValue $Version `
        -Abi $TargetAbi `
        -Checksum $checksum

    Write-Host ""
    Write-Host "Built release zip:"
    Write-Host $zipPath
    Write-Host ""
    Write-Host "Generated manifest:"
    Write-Host $manifestPath
    Write-Host ""
    Write-Host "Jellyfin repository URL:"
    Write-Host "https://raw.githubusercontent.com/$($repoInfo.Owner)/$($repoInfo.Repo)/main/manifest.json"

    if ($CommitAndPush) {
        Invoke-GitCommitAndPush -VersionValue $Version -All:$StageAll
    }

    if ($CreateRelease) {
        Invoke-GitHubRelease -VersionValue $Version -ZipPath $zipPath -Force:$ForceRelease
    }
}
finally {
    Pop-Location
}
