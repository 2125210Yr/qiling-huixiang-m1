$ErrorActionPreference='Stop'
$archive=Split-Path $PSScriptRoot -Parent
$video='F:/天命之子/dist/original-expedition-v01/full-a-7a6ede9b-20260921-1510/ordinary-full-a.mp4'
$descriptions=[ordered]@{
    25='开始于原创据点，已有single/sweep配置，发现6/12；尚未创建本趟A。'
    125='A新run，seed13486312，前厅待进入，五人满生命。'
    232='N1暂停，队列顺序为砚盾、引灯、炽羽、逐锋；蓄能0，主动0。'
    271='N1已完成，双向分岔可选；未同时完成两侧。'
    318='N2后台战斗，两名缄默卫士；A01/A02，蓄能2151/1120，实际吸收2151。'
    363='N2之后幕间工坊；A01/A02/A04已持有，五人带当前生命。'
    480='N4排练厅战斗，主敌已倒，A04已耗能后蓄能54；主动3。'
    506='N4完成，返回回廊N5待进入；前厅/后台/工坊/排练厅已完成，观众席未走。'
    528='N5战斗中，A01/A02/A04/A03完整持有；尚未主动施法。'
    570='N5中砚盾已执行，实际吸收3897，蓄能3360/1120；主动仅1，未释放A03/A04。'
    640='N6首领门前整备；全员恢复，尚未确认首领战。'
    883='N7实际开场，首领45000、两面具各7000，完整A构筑，蓄能0；五人生命满。'
    1016='N7暂停；一面具已倒，另一637生命，倍率1.5；蓄能3360/1120，主动7。'
    1043='N7恢复释放；A04/A03同时亮起，蓄能0，两面具倒下，主动9。'
    1091='N7阶段2，两面具重建存活，2.0倍群攻读条2.2秒；A04/A03同时亮，主动14。'
    1118='完成5场胜利结算：敌伤65720，遗物24034，吸收20963，生命伤害46567，治疗25020，主动16成功0拒绝。'
    1130='返回原创据点，发现8/12，横扫配置保留，提示临时构筑不会带入下一趟。'
    1143='据点打开A上趟回顾，保持同一结算数字。'
    1199='已创建新C，seed14556671，前厅待进入；五人满生命。'
    1214='新C N1战斗中，仅C01，A未持有，主动0；生命随本场战斗下降，不能称这帧仍满生命。'
}
$raw=& ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_frames,duration:format=duration,start_time,format_name -of json $video
if($LASTEXITCODE -ne 0){throw 'ffprobe failed'}
$f=Get-Item -LiteralPath $video
$report=[ordered]@{
    LocalOriginal=$video;Bytes=$f.Length;Sha256=(Get-FileHash -LiteralPath $video -Algorithm SHA256).Hash.ToLowerInvariant()
    Probe=(($raw -join "`n")|ConvertFrom-Json)
    FullDecodeCommand="ffmpeg -hide_banner -v error -i '$video' -f null -"
    FullDecodeObservedExitCode=0
    FullDecodeEvidence='Independent evidence agent decoded the complete 1216.266667-second file without errors on 2026-09-21 before constructing this report.'
    RecorderTermination='Actual operator reported stdin q and exit 0; this agent independently checks the finalized artifact.'
    Method='20 frames extracted by ffmpeg -ss <seconds> -i <video> -frames:v 1 -n <PNG>; every listed PNG personally inspected through view_image. Media offsets are taken from extraction positions, not filesystem timestamps.'
    VisualConclusion='One finalized continuous capture contains original hub, new A run, all five completed battles, A victory/hub/summary and new C entry. Values, screens, phases and outcomes change visibly. Long stationary N6/pause frames are expected; footage is not the earlier frozen GDI recording.'
    CaptureFpsIsNotRenderFps=$true
    Frames=@($descriptions.GetEnumerator()|ForEach-Object{[ordered]@{MediaOffsetSeconds=$_.Key;RelativeFile=('video-frames/full-a-{0:D4}s.png' -f [int]$_.Key);PersonallyViewed=$true;Observation=$_.Value}})
    ScreenshotLabelCorrections=@(
        [ordered]@{File='screens/046-A03-A04-mature-combo-trigger.jpg';Actual='N6 boss preparation with restored HP';Limitation='Does not show N5 battle or A03/A04 activation. The click label records intended action after N5 had already ended.'},
        [ordered]@{File='screens/047-A-boss-start.jpg';Actual='N6 boss preparation with confirm button';Limitation='Still the pre-battle frame; use video 883 seconds for actual N7 opening.'},
        [ordered]@{File='screens/037-A04-overload-actually-triggered.jpg';Actual='N4 A04 lit, barrier54, three successful actives';Limitation='Valid activation picture; exact damage is verified from tape root rather than icon alone.'},
        [ordered]@{File='screens/063-A-boss-overload-release.jpg';Actual='N7 phase1 A04/A03 both lit, barrier0';Limitation='Valid combined activation picture corroborated by actual replay resolutions.'},
        [ordered]@{File='screens/068-A-boss-stage2-overload.jpg';Actual='N7 phase2 A04/A03 both lit, two rebuilt masks';Limitation='Valid combined activation picture corroborated by actual replay resolutions.'}
    )
    Limits='15 fps is recorder cadence, not measured Unity rendering fps. The earlier B recording remains separately classified. This file does not itself establish offline portability after extracting a ZIP away from source code.'
}
$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($report|ConvertTo-Json -Depth 30)+"`n")
$s=[IO.FileStream]::new((Join-Path $PSScriptRoot 'video-analysis.json'),[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
try{$s.Write($bytes,0,$bytes.Length);$s.Flush($true)}finally{$s.Dispose()}
[pscustomobject]$report|Select-Object LocalOriginal,Bytes,Sha256|ConvertTo-Json
