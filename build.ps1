$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path $compiler)) { throw '.NET Framework 4.x compiler was not found.' }
$dist = Join-Path $PSScriptRoot 'dist\v1.0'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/win32manifest:$PSScriptRoot\app.manifest" "/resource:$PSScriptRoot\data\restaurants.json,restaurants.json" "/resource:$PSScriptRoot\data\ko.json,ko.json" "/resource:$PSScriptRoot\data\regions.tsv,regions.tsv" "/resource:$PSScriptRoot\data\food-streets.json,food-streets.json" "/out:$dist\WelcomeKorea.exe" "$PSScriptRoot\src\WelcomeKorea.cs" "$PSScriptRoot\src\Localization.cs" "$PSScriptRoot\src\Visuals.cs" "$PSScriptRoot\src\Routing.cs" "$PSScriptRoot\src\Places.cs" "$PSScriptRoot\src\Regions.cs" "$PSScriptRoot\src\FoodDiscovery.cs" "$PSScriptRoot\src\Presentation.cs"
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/win32manifest:$PSScriptRoot\app.manifest" "/resource:$dist\WelcomeKorea.exe,WelcomeKorea.exe" "/out:$dist\WelcomeKoreaSetup.exe" "$PSScriptRoot\src\Setup.cs"
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
& $compiler /nologo /target:exe /utf8output /reference:System.Windows.Forms.dll "/reference:$dist\WelcomeKorea.exe" "/reference:$dist\WelcomeKoreaSetup.exe" "/out:$dist\WelcomeKoreaTests.exe" "$PSScriptRoot\src\Tests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
& "$dist\WelcomeKoreaTests.exe"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
Get-FileHash -Algorithm SHA256 "$dist\WelcomeKorea.exe", "$dist\WelcomeKoreaSetup.exe" | ForEach-Object { '{0}  {1}' -f $_.Hash, (Split-Path $_.Path -Leaf) } | Set-Content -Encoding ASCII (Join-Path $dist 'SHA256SUMS.txt')
