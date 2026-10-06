$ErrorActionPreference = 'Stop'
$sourceRoot = Join-Path $PSScriptRoot 'src'
$outputRoot = Join-Path $PSScriptRoot 'dist'
$sourceText = Get-Content -LiteralPath (Join-Path $sourceRoot 'Sporenmessung.cs') -Raw -Encoding UTF8
$versionMatch = [regex]::Match($sourceText, 'public const string Version = "([0-9.]+)";')
if (-not $versionMatch.Success) { throw 'Programmversion fehlt.' }
$appVersion = $versionMatch.Groups[1].Value
$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) { throw 'Der C#-Compiler von .NET Framework wurde nicht gefunden. Bitte unter Windows mit .NET Framework erstellen.' }
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$buildOutput = Join-Path $outputRoot ("Trinos-Sporenmesser-" + $appVersion + '.exe')
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$buildOutput" "/win32manifest:$sourceRoot\app.manifest" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "$sourceRoot\Sporenmessung.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen.' }
Write-Output $buildOutput
