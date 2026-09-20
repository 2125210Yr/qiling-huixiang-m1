# D3: second-pass tape mapping and one-time video sample review

Checked on 2026-09-20. The three second-pass tapes pass the new readback, but the historical video is **not established as gameplay evidence**. All three requested sample frames show unrelated desktop applications, with no visible Unity game or battle HUD. `video_sample_result=NO_GAMEPLAY_OBSERVED`.

This is a three-frame sample review, not a review of all 451.97 seconds. It does not prove that no game frame exists elsewhere in the file. No additional sample search was performed, and no source video, tape, or Unity project was modified.

## Source video

- File: `F:/天命之子/DC_RECON_KIT/m1/REVIEW_a82801b/recordings/np-continuous-20260916T085721.mp4`
- SHA-256: `5BA30FCB4477E471920BC5ADF2A40D078B346096868AADBE489F15DCA1E43911`
- `ffprobe`: duration `451.966667` seconds; size `38797348` bytes; H.264; `5120 x 1440`; `30/1` fps.
- The readable container metadata proves that this is a playable video container; it does not prove that gameplay was captured.
- Historical mapping record: [SESSION_RESULTS.md](../../../REVIEW_a82801b/artifacts/natural-play/SESSION_RESULTS.md), Recording paragraph (line 60 when checked).
- Historical launch records: `F:/天命之子/DC_RECON_KIT/m1/REVIEW_a82801b/logs/helper-rerecord-np-basic-v1.console.txt`, `helper-rerecord-np-fever-v1.console.txt`, and `helper-rerecord-np-auto-v1.console.txt`. These identify HEAD `3d2d1e67554251b77d05ad732dc06390a114de05` and the archive folders below.

## Archive and tape mapping

All paths below are under `F:/天命之子/DC_RECON_KIT/m1/REVIEW_a82801b/artifacts/natural-play/`.

| Scenario | Archive | Session | Battle IDs recorded by that session |
|---|---|---|---|
| `np.basic.v1` | `20260916T085805-np_basic_v1` | `np-20260916T085931` | `np-20260916T085931-001`, `np-20260916T085931-002` |
| `np.fever.v1` | `20260916T090011-np_fever_v1` | `np-20260916T090055` | `np-20260916T090055-001`, `np-20260916T090055-002` |
| `np.auto.v1` | `20260916T090337-np_auto_v1` | `np-20260916T090403` | `np-20260916T090403-001` |

Each archive's `natural-play.result.txt` supplies its session and battle IDs; `natural-play/BATTLE_INDEX.txt` and each battle's `scenario.txt` supply the corresponding per-battle mapping. The readback inputs are the `-001` directories at `<archive>/natural-play/battles/<battle_id>/`, each containing `commands.txt`, `digest.txt`, `events.txt`, `header.txt`, `replay.jsonl`, `result.txt`, and `scenario.txt`.

The archives also contain copies of earlier battles. These copies are not additional battles from the named session and must not be selected merely by taking the first directory.

## New readback results

The coordinator ran these readbacks with the corrected code. This review read the resulting logs; it did not rebuild or rerun the reader.

| Tape | Result | Expected / actual events | Log |
|---|---|---:|---|
| `np-20260916T085931-001` | `Match=True`, `Ok=True`, exit 0 | 192 / 192 | [second-basic.txt](../readback/second-basic.txt) |
| `np-20260916T090055-001` | `Match=True`, `Ok=True`, exit 0 | 1464 / 1464 | [second-fever.txt](../readback/second-fever.txt) |
| `np-20260916T090403-001` | `Match=True`, `Ok=True`, exit 0 | 66 / 66 | [second-auto.txt](../readback/second-auto.txt) |

All three report `openingSource=input`, `NamedOpeningDiff=null`, `FirstEventDivergence=-1`, and zero `CommandDiff`, `Unconsumed`, `DigestDiff`, `EventDiff`, and `VersionDiff`. The execution summary is [second-session-results.json](../readback/second-session-results.json). These successful tape replays do not establish the contents of the video.

## One-time visual sampling

Exactly one frame was extracted at each requested timestamp with `ffmpeg -nostdin -hide_banner -loglevel error -n -ss <seconds> -i <source-video> -frames:v 1 <new-png>`. All three PNGs were then inspected with `view_image`. The PNGs preserve the original 5120 x 1440 frame dimensions; tool display was resized to 2048 x 576.

The approximate wall-clock column below is inferred from the filename's presumed UTC start `08:57:21`, the historical launch logs, and their archived session timestamps. **This is historical time inference, not proof from an in-frame session or battle ID.** No such ID was observed in the samples.

| Requested scenario interval | Video timestamp | Approximate inferred wall clock (+08) | Actual visible sample | Verdict |
|---|---:|---|---|---|
| basic | 140 s | 2026-09-16 16:59:41 | Left display: Codex model-recovery task. Right display: unrelated browser content. No game or battle HUD visible. [Frame](basic-t140.png) | Does not visually establish basic gameplay. |
| fever | 270 s | 2026-09-16 17:01:51 | Left display: Codex model-recovery task. Right display: browser feed. No game or battle HUD visible. [Frame](fever-t270.png) | Does not visually establish fever gameplay. |
| auto / near exit | 420 s | 2026-09-16 17:04:21 | Left display: Codex model-recovery task. Right display: browser image-editing interface. No game or battle HUD visible. [Frame](auto-t420.png) | Does not visually establish auto gameplay. |

Frame SHA-256 values:

- `basic-t140.png`: `70FDE2002B2C59FED692B2284620993B03CA4473585ECB77F73CA079B35A3E54`
- `fever-t270.png`: `8051D1BE8C66941C72C8D0D6A8AA919CD4F6DFB72A3AF432D0241EE3DC221639`
- `auto-t420.png`: `381ADE23F09C67F3F07343CC8BFCBB1E412699198314CD8136AE6CA044C62313`

The historical report's association between recording time and session archives remains a log-based association. It must not be promoted to verified gameplay capture. D3's tape-readback gap is repaired; its visual-recording gap remains unsupported by these samples.
