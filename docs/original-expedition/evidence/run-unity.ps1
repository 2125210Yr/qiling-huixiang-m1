param(
    [Parameter(Mandatory=$true)][string]$Name,
    [ValidateSet('tests','render','build')][string]$Mode='tests',
    [string]$TestFilter='Resonance.EditorTests',
    [ValidateSet('o0','o1','o2','o3','o4','o5')][string]$EvidenceGroup='o0'
)
$ErrorActionPreference='Stop'
$evidenceRoot=Join-Path $PSScriptRoot $EvidenceGroup
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$runStarted=[DateTime]::UtcNow.ToString('o')
$unityArgs=@('-batchmode','-projectPath','"F:\Resonance\client"','-giCustomCacheLocation','"F:\Resonance\client\Temp\GICache-Original"','-logFile',('"'+$evidenceRoot+'\'+$Name+'.log"'))
if($Mode -eq 'tests') {
    $unityArgs+=@('-runTests','-testPlatform','EditMode','-testFilter',$TestFilter,'-testResults',('"'+$evidenceRoot+'\'+$Name+'.xml"'))
} elseif($Mode -eq 'render') {
    $env:RESONANCE_ORIGINAL_UI_EVIDENCE=Join-Path $evidenceRoot ($Name+'-frames')
    $unityArgs+=@('-executeMethod','Resonance.EditorTools.OriginalExpeditionVerification.RunAndExit')
} else {
    $unityArgs+=@('-executeMethod','Resonance.EditorTools.WindowsBuild.BuildAndExit')
}
$runProcess=Start-Process -FilePath 'D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe' -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
$runProcess.WaitForExit()
$exitCode=$runProcess.ExitCode
$result=[ordered]@{started=$runStarted;finished=[DateTime]::UtcNow.ToString('o');exitCode=$exitCode;mode=$Mode;name=$Name;arguments=$unityArgs}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceRoot ($Name+'-run.json')) -Encoding utf8
$result | ConvertTo-Json -Depth 4
exit $exitCode
