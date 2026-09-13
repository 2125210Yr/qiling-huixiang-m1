# PROJECT_STATUS

## Now

Phone archive complete. Vertical Slice code is in `client/` (Boot→Home→契灵→编队→关卡→Battle Auto/Tap/Slide/Drive/Fever→Result→local save). BattleCore tests: **8/8 passed**.

Unity **6.3.23f1** is installed at `D:\Unity\Hub\Editor\6000.3.23f1` and registered in Hub. The client folder is registered as a Hub project.

## BLOCKED (APK)

1. **Editor license** — Hub China login activated Unity Personal, but Editor 6.3 (international licensing client 1.18.3) cannot handshake Hub’s newer client (HTTP 505) and reports no entitlements. Opening the project shows **License error**. CLI `unity auth login` is waiting in Chrome on the **global Unity ID** page. Complete that login (do not use the Hub tutorial / Unity 6.5).
2. **Android Build Support** — China CDN 404s `6000.3.23f1` Android (and even the editor installer) as of 2026-08-27. Local editor came from an earlier CLI cache. Need an overseas proxy exit or wait for the China mirror, then `unity editors module add 6000.3.23f1 -m android --child-modules --accept-eula`.

Do **not** install Unity 6.5. Do **not** click Hub「启动项目」tutorial.

Until both are done, there is no playable APK.

## Hard bans still in force

No original IP in the shipped game. No backend, gacha shop, PvP. No Live2D required for slice (static 2D + `CharacterPresenter`).
