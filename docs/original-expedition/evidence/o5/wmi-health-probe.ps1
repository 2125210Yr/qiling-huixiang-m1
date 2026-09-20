$ErrorActionPreference='Stop'
$timer=[Diagnostics.Stopwatch]::StartNew()
foreach($wmiClass in @('Win32_OperatingSystem','Win32_BIOS','Win32_ComputerSystemProduct')) {
    $rows=@(Get-CimInstance -ClassName $wmiClass -OperationTimeoutSec 5)
    [pscustomobject]@{class=$wmiClass;count=$rows.Count;elapsedMs=$timer.ElapsedMilliseconds} | ConvertTo-Json -Compress
}
