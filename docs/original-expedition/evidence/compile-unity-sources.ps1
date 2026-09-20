param(
    [string]$Project='F:/Resonance/client',
    [Parameter(Mandatory=$true)][string]$Output
)
$ErrorActionPreference='Stop'
# C# API/compile check only. This does not launch Unity, execute UI tests, import assets or build a Player.
$projectRoot=[IO.Path]::GetFullPath($Project)
$outputRoot=[IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $outputRoot) { throw 'Use a new compile evidence directory.' }
New-Item -ItemType Directory -Path $outputRoot | Out-Null
$compiler='C:/Program Files/dotnet/sdk/6.0.410/Roslyn/bincore/csc.dll'
if(-not (Test-Path -LiteralPath $compiler)) { throw 'Verified .NET 6 compiler unavailable.' }
$assemblies=[ordered]@{
    'Inochi2D.Runtime'='Assets/Inochi2D/Runtime'
    'EmeraldInochi'='Assets/EmeraldInochi'
    'Resonance.Battle'='Assets/Scripts/Resonance.Battle'
    'Resonance.App'='Assets/Scripts/Resonance.App'
    'Resonance.Editor'='Assets/Editor'
    'Resonance.EditorTests'='Assets/Tests/Editor'
}
$results=@()
foreach($entry in $assemblies.GetEnumerator()) {
    $assembly=$entry.Key
    [xml]$projectXml=Get-Content -LiteralPath (Join-Path $projectRoot ($assembly+'.csproj'))
    $definitions=($projectXml.SelectNodes('//DefineConstants') | Select-Object -First 1).InnerText
    $unsafe=($projectXml.SelectNodes('//AllowUnsafeBlocks') | Select-Object -First 1).InnerText
    $arguments=[Collections.Generic.List[string]]::new()
    $arguments.Add('/nostdlib+'); $arguments.Add('/langversion:9')
    $arguments.Add('/target:library'); $arguments.Add('/nologo'); $arguments.Add('/out:"'+(Join-Path $outputRoot ($assembly+'.dll'))+'"')
    if($unsafe -eq 'True' -or $unsafe -eq 'true') { $arguments.Add('/unsafe+') }
    if($definitions) { $arguments.Add('/define:'+$definitions) }
    foreach($reference in $projectXml.SelectNodes('//Reference/HintPath')) {
        $referencePath=$reference.InnerText
        if(-not [IO.Path]::IsPathRooted($referencePath)) { $referencePath=Join-Path $projectRoot $referencePath }
        if(-not (Test-Path -LiteralPath $referencePath)) { throw "Missing generated-project reference: $referencePath" }
        $arguments.Add('/reference:"'+[IO.Path]::GetFullPath($referencePath)+'"')
    }
    foreach($reference in $projectXml.SelectNodes('//ProjectReference')) {
        $referencedName=[IO.Path]::GetFileNameWithoutExtension($reference.Include)
        $referencePath=Join-Path $outputRoot ($referencedName+'.dll')
        if(-not (Test-Path -LiteralPath $referencePath)) { throw "Uncompiled dependency: $referencedName" }
        $arguments.Add('/reference:"'+$referencePath+'"')
    }
    $sources=@(& rg --files (Join-Path $projectRoot $entry.Value) -g '*.cs')
    if($LASTEXITCODE -ne 0 -or $sources.Count -eq 0) { throw "No sources for $assembly" }
    $sourceManifest=@()
    foreach($source in $sources) {
        $arguments.Add('"'+[IO.Path]::GetFullPath($source)+'"')
        $sourceManifest+=@{path=$source;sha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash}
    }
    $responsePath=Join-Path $outputRoot ($assembly+'.rsp')
    [IO.File]::WriteAllLines($responsePath,$arguments,[Text.UTF8Encoding]::new($false))
    $logPath=Join-Path $outputRoot ($assembly+'.log')
    & dotnet exec $compiler /noconfig ('@'+$responsePath) *> $logPath
    $compileExit=$LASTEXITCODE
    $results+=@{assembly=$assembly;exitCode=$compileExit;sources=$sourceManifest}
    $results | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputRoot 'compile-results.json') -Encoding utf8
    Write-Output "$assembly exit=$compileExit sources=$($sources.Count)"
    if($compileExit -ne 0) { Get-Content -LiteralPath $logPath -Tail 25; exit $compileExit }
}
Write-Output 'C# source compile complete; Unity UI/runtime/build validation remains separate.'
