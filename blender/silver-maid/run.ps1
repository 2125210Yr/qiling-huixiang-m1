$Blender = 'F:\Blender\blender-5.2.0-windows-x64\blender.exe'
$Script = Join-Path $PSScriptRoot 'build_all.py'
& $Blender --background --python $Script
exit $LASTEXITCODE
