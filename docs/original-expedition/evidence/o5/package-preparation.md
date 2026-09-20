# O5 isolated project preparation evidence

2026-09-21. Completed one preparation run, with no Unity launch or build. This is an **intermediate source snapshot, not a release package**. The final source state must be copied into another new directory after the integration code is committed and verified.

## Result

- Source: `F:/Resonance/client` (the physical project; the repository's `client` junction is rejected).
- Prepared project: `F:/天命之子/dist/original-expedition-v01/prepare-20260921`.
- Caller-supplied base commit: `a31c47ff720d062aa88ba2bf3aca7a8d6ea711a4`. The snapshot **includes uncommitted changes**; this commit alone does not identify it.
- Actual input: 499 files, 11,447,768 bytes, plus `package-input-manifest.json`.
- Manifest SHA-256: `92949149d50c2370a8d6ebfb746d0fe5faa3439ac93b81af18508f71e558795e`.
- All 499 copied hashes matched the frozen input hashes. Source hashes were rechecked after copying; the independent destination check also passed.
- Exactly one Resources data file: `Assets/Resources/Fonts/NotoSansSC.otf`. Folder and asset meta files are retained.
- No native DLL, model, old reference image/media, junction/symlink, unlisted file, or asset lacking its original meta was found.
- The original `WindowsBuild.cs` and the new `OriginalExpeditionBuild.cs` were copied with their meta files. Neither entry point was executed.

## Script and contract

Script: `docs/original-expedition/evidence/prepare-package-project.ps1`, PowerShell 7+.

Parameters: `SourceProject` defaults to `F:/Resonance/client`; `Destination` is mandatory; optional `SourceCommit` defaults to empty. The manifest uses `sourceProject`, `destination`, `sourceCommit` and `files`, with each file carrying `path`, `sha256`, `bytes` and `sourceKind`. These lower-camel fields are compatible with the root agent's JsonUtility build audit. The manifest explains that the supplied commit is a base reference and per-file hashes identify the actual bytes.

The exact command used once was:

```powershell
& 'F:/天命之子/docs/original-expedition/evidence/prepare-package-project.ps1' `
  -SourceProject 'F:/Resonance/client' `
  -Destination 'F:/天命之子/dist/original-expedition-v01/prepare-20260921' `
  -SourceCommit 'a31c47ff720d062aa88ba2bf3aca7a8d6ea711a4'
```

That destination now exists. A later preparation must use a new path; rerunning into this directory is intentionally rejected. The script neither deletes nor cleans partial or old outputs. A failed copy leaves its partial directory available for diagnosis and does not silently retry.

The script checks local absolute paths and all existing ancestors for reparse points; source and destination junction/symlink paths are rejected. It assembles a positive per-file list before creating the destination, then copies each file without overwrite. Target paths are constrained to the selected physical destination root, and the entire copied tree is checked for links, forbidden extensions, unexpected files and all nested Resources directories. Only code/asmdef, original meta, the specified scene/settings, package/project settings, the audited font and notices are selected. Missing asset meta stops preflight. No source meta is generated or edited.

## Verification evidence

| Evidence | What actually ran |
| --- | --- |
| `package-preparation-guards-red.json` | Two real guard assertions failed against the initial permissive implementation: existing destination and source junction were not rejected. Guard functions only; no copy. |
| `package-preparation-guards-green.json` | 12 checks passed for existing paths, junction ancestors, source containment, relative/traversal paths, nested Resources model, native DLL, old reference image, allowed font/meta and a new physical destination. Functions were extracted from the script AST; the main preparation body was not executed. |
| `package-preparation-run.json` | The one actual script run, reporting 499 copied inputs and no Unity execution. |
| `package-preparation-manifest.json` | Exact saved copy of the prepared project's `package-input-manifest.json`. The manifest excludes its own bytes from its file-hash list; its separate SHA-256 is recorded above. |
| `package-preparation-validation.json` | Independent Python standard-library traversal/hash verification of the finished snapshot: 500 total files including manifest, no hash mismatches, extra files, missing meta, forbidden data or links. |
| `package-preparation-reentry.json` | Guard-only call rejected the now-existing prepared directory; its manifest hash remained unchanged. The main script was not run a second time. |

No testing framework was installed. PowerShell syntax parsing passed. These checks validate preparation and exclusion at the source-copy boundary, not Unity compilation, post-import dependencies, packed Player contents, UI operation or the N0–N7 run.

## Notices

The unmodified local Noto Sans SC 2.002 font retains SHA-256 `a2b93e6c2db05d6bbbf6f27d413ec73269735b7b679019c8a5aa9670ff0ffbf2`. Its copyright notice and font identity were taken from its embedded SFNT metadata. Full OFL 1.1 text was retrieved from the [official Noto CJK repository](https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/LICENSE). Its downloaded SHA-256 is `6a73f9541c2de74158c0e7cf6b0a58ef774f5a780bf191f2d7ec9cc53efe2bf2`, recorded with the URL and retrieval time in `ThirdPartyNotices/NotoSansSC/SOURCE.json`.

The source notice files are under `docs/original-expedition/ThirdPartyNotices/NotoSansSC/` and copied to the prepared project's `ThirdPartyNotices/NotoSansSC/`. The original `Assets/Inochi2D/LICENSE.md` is copied separately to `ThirdPartyNotices/Inochi2D/LICENSE.md`, because the existing asmdef dependency requires Inochi managed source. All third-party native DLLs and model/Resources data are excluded.

The next build audit must consume this manifest and inspect actual BuildReport contents. Final delivery must prepare a fresh copy from the finished source state and repeat that build audit. Existing shared project files, earlier outputs and user assets were not changed, moved or deleted by this preparation.
