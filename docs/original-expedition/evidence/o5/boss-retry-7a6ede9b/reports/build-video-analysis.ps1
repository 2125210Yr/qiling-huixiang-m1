$ErrorActionPreference = 'Stop'
$archive = Split-Path $PSScriptRoot -Parent
$local = 'F:/天命之子/dist/original-expedition-v01/resume-7a6ede9b-20260921-144630'
$descriptions = [ordered]@{
    'defeat-005s.png'='重试页；显示 B01–B04 和全员冻结开局生命。'
    'defeat-026s.png'='战斗已推进；阶段1，两面具存活，群攻2.0倍，正在蓄势、2.2秒后结算；主动次数0。'
    'defeat-030s.png'='群攻后仍在战斗；首领41114生命，面具3159/2842，队员生命下降，主动次数0。'
    'defeat-060s.png'='阶段1，两面具已倒，群攻1.0倍；首领30522生命，队员低生命；主动次数0。'
    'defeat-090s.png'='自然败北后返回挑战未完成/原样重试页。'
    'defeat-110s.png'='关闭前仍在败北重试页；与90秒帧相同属于静止结果页。'
    'retry-005s.png'='普通重开后原样重试页；五人满生命，遗物沿用。'
    'retry-045s.png'='阶段1战术暂停；面具为集火目标，引灯已加入队列。'
    'retry-090s.png'='阶段1战术暂停；两面具已倒、1.0倍群攻正在蓄势2.9秒；首轮四个主动均已执行。'
    'retry-180s.png'='阶段1，首领22579生命，累计11个主动；有效治疗9157、护盾吸收5078。'
    'retry-245s.png'='阶段2，两面具复建；首领16387生命，两面具3571/3571，主动16次。'
    'retry-330s.png'='阶段2，两面具再次倒下；首领9623生命，护盾数值可见，主动21次。'
    'retry-410s.png'='恢复战斗后首领仅269生命；主动26次，治疗20410、护盾吸收21590。'
    'retry-425s.png'='终幕落下胜利页；有效敌伤73000、遗物追加25139、完成5场、首通解锁横扫。'
    'retry-442s.png'='返回据点；横扫配置可选择，发现6/12，临时构筑不会带入下一趟。'
    'retry-450s.png'='据点打开上趟回顾；相同末战结算数字与首通摘要保留。'
    'retry-462s.png'='从上趟回顾返回据点。'
}
$videos = foreach($entry in @(@{Name='boss-natural-defeat-before-reopen.mp4';Prefix='defeat';Stop='Window closed; recorder EOF, parent observed exit 0';Session=72598},@{Name='boss-retry-after-reopen.mp4';Prefix='retry';Stop='Recorder stdin q; parent observed exit 0';Session=52306})) {
    $path=Join-Path $local $entry.Name
    $file=Get-Item -LiteralPath $path
    $probeText=& ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_frames,duration:format=duration,start_time,format_name -of json $path
    if($LASTEXITCODE -ne 0){throw 'ffprobe failed'}
    $probe=$probeText -join "`n" | ConvertFrom-Json
    [ordered]@{
        LocalOriginal=$path; Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant(); Bytes=$file.Length
        CreationTimeUtc=$file.CreationTimeUtc.ToString('o'); LastWriteTimeUtc=$file.LastWriteTimeUtc.ToString('o')
        Probe=$probe; FullDecodeCommand="ffmpeg -hide_banner -v error -i '$path' -f null -"
        FullDecodeObservedExitCode=0
        DecodeEvidence='The independent evidence agent executed full decode of both complete files on 2026-09-21; no decoder errors. This report records that completed check and does not rerun it.'
        RecorderSession=$entry.Session; RecorderTermination=$entry.Stop
        Frames=@($descriptions.Keys | Where-Object {$_ -like ($entry.Prefix+'-*')} | ForEach-Object {
            $name=$_; $seconds=[int]([regex]::Match($name,'-(\d+)s').Groups[1].Value)
            [ordered]@{RelativeFile='video-frames/'+$name;MediaOffsetSeconds=$seconds;Extraction="ffmpeg -hide_banner -v error -ss $seconds -i '$path' -frames:v 1 -n <relative-file>";PersonallyInspectedWithViewImage=$true;Observation=$descriptions[$name]}
        })
    }
}
$report=[ordered]@{
    Scope='Independent full decoding, frame extraction and personal visual inspection. Two separate captures, not one continuous capture across process exit. All offsets are media offsets, not inferred UTC timestamps.'
    CaptureFpsIsNotRenderFps=$true
    Backend='FFmpeg gfxcapture / Windows.Graphics.Capture'
    VisualConclusion='Both clips visibly change across battle states and UI transitions; neither is a frozen-frame recording. Identical frames during a paused battle or a stationary results page are expected and do not erase observed transitions.'
    Limits='This is the boss defeat/reopen/retry/victory segment only; earlier four battles are not included. Sampling does not prove every individual captured frame changes. No claim of 30/120 actual rendering cadence.'
    Videos=@($videos)
}
$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($report | ConvertTo-Json -Depth 30)+"`n")
$out=Join-Path $PSScriptRoot 'video-analysis.json'
$s=[IO.FileStream]::new($out,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
try{$s.Write($bytes,0,$bytes.Length);$s.Flush($true)}finally{$s.Dispose()}
$videos | ForEach-Object {[pscustomobject]$_} | Select-Object LocalOriginal,Sha256,Bytes | ConvertTo-Json
