$ErrorActionPreference='Stop'
$archive=Split-Path $PSScriptRoot -Parent
$video='F:/天命之子/dist/original-expedition-v01/ordinary-c-7a6ede9b-20260921-1526/ordinary-c-acquisition.mp4'
$descriptions=[ordered]@{
    5='N1暂停，承接A片末段；仅C01，主动0。此时生命已受本场影响。'
    43='N1三名辅助已施法，C01亮，主动3，治疗2040；充能发生接力。'
    89='N1胜利后双向分岔选择。'
    134='N2战斗，持有C01/C02但未亮；两次主动。本场回放触发序号均0。'
    162='N2奖励选择，C03可选，展开B01详情；不是C02接力触发。'
    193='N4开场，C01/C02/C03已持有，和声0/3。'
    248='N4三名辅助后C01/C02/C03同时亮，和声归0/3，主动3。'
    337='N4奖励可选C04，详情说明下一次主动伤害技能增加120%同通道伤害。'
    359='N5开场，完整C01-C04，强奏未储存。'
    420='N5暂停，砚盾/聆泉/引灯排队，强奏未储存，主动0。'
    438='N5三辅助执行后C01/C02/C03亮，强奏已储存，主动3。'
    452='N5再次暂停，强奏仍储存；第四敌人4036，累计敌伤10116。'
    488='同一N5暂停状态，已聚焦第四敌人；强奏仍储存。'
    490='同一N5暂停状态，点杀排队，强奏仍储存；尚未消耗。'
    493='N5点杀执行，C04亮，强奏未储存，第四敌人1462，累计敌伤12690，主动4。'
    518='N5已胜利，进入N6首领门前整备，五人恢复满生命。'
    543='仍在N6首领门前；C远征尚未完成，未进入首领战。'
}
$raw=& ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_frames,duration:format=duration,start_time,format_name -of json $video
if($LASTEXITCODE -ne 0){throw 'ffprobe failed'}
$f=Get-Item -LiteralPath $video
$report=[ordered]@{
    LocalOriginal=$video;Bytes=$f.Length;Sha256=(Get-FileHash -LiteralPath $video -Algorithm SHA256).Hash.ToLowerInvariant()
    Probe=(($raw -join "`n")|ConvertFrom-Json)
    FullDecodeCommand="ffmpeg -hide_banner -v error -i '$video' -f null -"
    FullDecodeObservedExitCode=0
    FullDecodeEvidence='Independent evidence agent decoded the complete 545.466667-second file without errors on 2026-09-21 before constructing this report.'
    RecorderTermination='Actual operator reported stdin q and exit 0; this agent independently checked the finalized artifact.'
    Method='17 frames extracted by ffmpeg -ss <seconds> -i <video> -frames:v 1 -n <PNG>; every listed PNG personally inspected through view_image. Media offsets are extraction positions, not filesystem timestamps.'
    VisualConclusion='Visible progression through N1, N2, N4, N5 and N6. N5 forte changes from absent to stored, remains stored during the later pause, then is consumed by a native targeted damage skill. This is changing footage, not the earlier frozen GDI recording.'
    SplitRecording='C selection at the original hub and C N1 entry appear in the final segment of ../ordinary-full-a-7a6ede9b. This file continues from N1 paused; there is no claim of one uninterrupted C N0-to-N6 recording.'
    CaptureFpsIsNotRenderFps=$true
    Frames=@($descriptions.GetEnumerator()|ForEach-Object{[ordered]@{MediaOffsetSeconds=$_.Key;RelativeFile=('video-frames/c-{0:D3}s.png' -f [int]$_.Key);PersonallyViewed=$true;Observation=$_.Value}})
    ScreenshotLabelCorrections=@(
        [ordered]@{File='screens/010-C02-upgraded-relay.jpg';Actual='N2 with C02 held';Limitation='N2 tape has zero C01/C02 trigger serials. Possession and an intended label cannot establish actual relay.'},
        [ordered]@{File='screens/011-C02-guide-relay-flash.jpg';Actual='N2 reward screen with B01 details';Limitation='An attempted guide click landed after battle end; this is not a relay activation picture.'},
        [ordered]@{File='screens/037-C-N5-victory-boss-door-restored.jpg';Actual='N6 boss preparation with full HP';Limitation='Event 37 intended guard click occurred after N5 ended; it is not an additional active skill execution or boss victory.'}
    )
    Limits='15 fps is capture cadence, not measured Unity rendering fps. C has four victories and is still active at N6. No claim of a complete C boss clear or continuous capture across the split between A and C files.'
}
$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($report|ConvertTo-Json -Depth 30)+"`n")
$s=[IO.FileStream]::new((Join-Path $PSScriptRoot 'video-analysis.json'),[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
try{$s.Write($bytes,0,$bytes.Length);$s.Flush($true)}finally{$s.Dispose()}
[pscustomobject]$report|Select-Object LocalOriginal,Bytes,Sha256|ConvertTo-Json
