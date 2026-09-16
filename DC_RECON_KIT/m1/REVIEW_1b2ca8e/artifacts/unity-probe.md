# B1-A4 unity-probe — 2026-09-14 21:56 +08

**task:** B1-A4 (resume of closed A53-UNITY)  
**HEAD:** `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`  
**Editor launched this turn:** no  
**A53 `NOT_RUN` is not this batch’s result.**

---

## 1. A2 bind (Editor launch withheld)

```powershell
Select-String -LiteralPath "F:\天命之子\client\Assets\Scripts\Resonance.Battle\Content\VerificationCatalog.cs" -Pattern "A53_SUB_STRIP_DOT_FLAME|StripDotFlame"
```

**stdout:** (empty)  
**stderr:** (none)  
**exit:** 0  

```text
NO_MATCH in VerificationCatalog.cs
```

Same search on `Catalog.cs`:

```text
1171:public const string StripDotFlame = "A53_SUB_STRIP_DOT_FLAME";
```

**BLOCKED (this turn):** `VerificationCatalog.Apply` does not bind `A53_SUB_STRIP_DOT_FLAME`. Lease forbids opening the Editor until B1-A2 lands that bind. Coordinator resumes Play Mode after A2+A3 merge.

---

## 2. Unity.exe

```powershell
Test-Path -LiteralPath "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
(Get-Item "D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe").VersionInfo | Format-List FileVersion, ProductVersion
```

**stdout:**

```text
True
FileVersion     : 6000.3.23.643820
ProductVersion  : 6000.3.23f1_09d2ecc7fb28
```

Matches `client/ProjectSettings/ProjectVersion.txt` (`6000.3.23f1 (09d2ecc7fb28)`). Not BLOCKED.

---

## 3. EditorInstance / processes

```powershell
Test-Path "F:\天命之子\client\Library\EditorInstance.json"
Get-CimInstance Win32_Process | Where-Object { $_.Name -match '^(Unity|UnityHub|Resonance)' }
```

**stdout:** `exists=False` ; processes `(none)`  
**stderr:** (none)

---

## 4. ffmpeg

```powershell
Get-Command ffmpeg
ffmpeg -version
```

**stdout (first line):**

```text
ffmpeg=C:\Users\Administrator\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1.1-full_build\bin\ffmpeg.exe
ffmpeg version 8.1.1-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers
```

Not BLOCKED.

---

## 5. Builds/Win64 (leftover — do not hash as this batch)

```powershell
Test-Path "F:\天命之子\client\Builds\Win64\Resonance.exe"
Get-Item "F:\天命之子\client\Builds\Win64\Resonance.exe" | Format-List Length, LastWriteTime
```

**stdout:** exists; `Length=667136`; `LastWriteTime=2026-08-28T15:02:25+08:00`

This exe is **not** HEAD `1b2ca8e`. Do not hash it. Do not hash `dist/windows\Resonance.exe` (`exists=True`, leftover). Matching-source `WindowsBuild.BuildAndExit` runs **after** A2 freeze.

---

## 6. git HEAD

```powershell
git -C "F:\天命之子" rev-parse HEAD
```

**stdout:** `1b2ca8e4ff56d9f0986e158fc97b50498f735cb3`  
**exit:** 0
