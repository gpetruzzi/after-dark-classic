param([string]$Payload=(Join-Path $PSScriptRoot 'package'),[string]$Destination=(Join-Path $PSScriptRoot '..\outputs'))
$ErrorActionPreference='Stop'
$Payload=[IO.Path]::GetFullPath($Payload)
$Destination=[IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$shared=@('/nologo','/target:winexe','/platform:x86',"/win32icon:$Payload\Assets\AfterDark.ico","/win32manifest:$PSScriptRoot\src\app.manifest",'/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Xml.dll')
if(!(Test-Path -LiteralPath "$Payload\After Dark Engine.scr")){Copy-Item -LiteralPath "$Payload\After Dark.scr" -Destination "$Payload\After Dark Engine.scr"}
& $compiler @shared "/out:$Payload\After Dark.scr" "$PSScriptRoot\src\Common.cs" "$PSScriptRoot\src\Saver.cs"
if($LASTEXITCODE -ne 0){throw 'Falló la compilación del salvapantallas de Windows.'}
& $compiler @shared "/resource:$Payload\Assets\speaker.bmp,speaker.bmp" "/out:$Payload\AfterDarkControl.exe" "$PSScriptRoot\src\Common.cs" "$PSScriptRoot\src\App.cs" "$PSScriptRoot\src\Configure.cs"
if($LASTEXITCODE -ne 0){throw 'Falló la compilación del panel.'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=Join-Path $PSScriptRoot 'payload.zip'
if(Test-Path -LiteralPath $zip){Remove-Item -LiteralPath $zip}
[IO.Compression.ZipFile]::CreateFromDirectory($Payload,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
$resources=@("/resource:$zip,payload.zip","/resource:$Payload\Assets\AfterDark.ico,setup.ico")
foreach($i in 1..6){$resources+="/resource:$Payload\Assets\PAGE$i.BMP,page$i.bmp"}
& $compiler @shared @resources "/out:$Destination\AfterDark-Setup.exe" '/r:System.IO.Compression.dll' '/r:System.IO.Compression.FileSystem.dll' "$PSScriptRoot\src\Common.cs" "$PSScriptRoot\src\Setup.cs" "$PSScriptRoot\src\PackageFiles.cs"
if($LASTEXITCODE -ne 0){throw 'Falló la compilación del instalador.'}
& $compiler @shared "/resource:$Payload\AfterDarkControl.exe,control.exe" "/out:$Destination\AfterDark-Actualizar.exe" "$PSScriptRoot\src\Common.cs" "$PSScriptRoot\src\Update.cs"
if($LASTEXITCODE -ne 0){throw 'Falló la compilación del actualizador.'}
Write-Output (Join-Path $Destination 'AfterDark-Setup.exe')
