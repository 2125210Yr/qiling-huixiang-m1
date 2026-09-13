using Resonance.Battle;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Observable battle cue copy for HUD and UI demos (M1-G2-UI-CUES).
    ///
    /// Primary handoff frames: <c>aSbBuFD12HY</c> / <c>hgqXY5M9gFk</c> under
    /// <c>DC_RECON_KIT/docs/reference/gl-shutdown-pve/</c> (<c>FETCH_LOG.txt</c>).
    /// Also supplementary: <c>supplementary/ragna_gl/CUES.md</c>.
    /// Layout stays <see cref="LayoutStatus"/>. Not T27. Not M1 acceptance.
    ///
    /// QTE GREAT = GREAT!; QTE PERFECT = PERFECT! (primary Robin / Ragna EN).
    /// Must stay distinguishable on screen:
    /// SLIDE SHOWTIME = 开演 (IT'S SHOWTIME!! — not Fever);
    /// FEVER TIME banner = 狂热时间;
    /// QTE GREAT = GREAT!; QTE PERFECT = PERFECT!.
    /// 开战 is battle-start only — never reuse it as the Fever banner or as 开演.
    /// Primary P0 tip (~t445): Fever fill Perfect+40 / Great+30 / Good+15; tip banner FEVER TIME!! (~t445); window tip 14s (~t435).
    /// </summary>
    public static class BattleCueCopy
    {
        public const string LayoutStatus = "PRIMARY_PARTIAL_HANDOFF";
        public const string SourceNote = "primary handoff P0/P1 + ragna_gl supplementary; fidelity 0";

        public const string FeverTime = "狂热时间";
        /// <summary>Tip / stamp banner (P0 ~t445). Keep !! for tip art.</summary>
        public const string FeverTimeEn = "FEVER TIME!!";
        /// <summary>Live Fever field word (P0 cont36 t440). No bangs; countdown sits under/on arc.</summary>
        public const string FeverTimeLive = "FEVER TIME";
        public const string FeverGauge = "狂热";
        /// <summary>Live Fever leftover under the rainbow arc. P0 t440/t442 — two decimals, no unit suffix.</summary>
        public static string FeverWindowLeft(float secLeft)
        {
            if (secLeft < 0f) secLeft = 0f;
            return secLeft.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        /// <summary>Robin ND portrait overlay (r58). Inventory — trigger UNKNOWN.</summary>
        public static string FinalTimeLine(int sec)
        {
            if (sec < 0) sec = 0;
            return "FINAL TIME " + sec;
        }
        public const string SlideShowtime = "开演";
        /// <summary>HUD verb. Vfx stamp uses EN <c>SLIDE SKILL</c> (primary P0 t360).</summary>
        public const string SlideSkill = "上滑";
        /// <summary>EN stamp badge on SHOWTIME cut (primary GT / Ragna).</summary>
        public const string SlideSkillEn = "SLIDE SKILL";
        /// <summary>Portrait corner pip (Robin mid-fight shows <c>SLIDE</c>).</summary>
        public const string SlidePipEn = "SLIDE";
        /// <summary>Portrait ready plus (P0 t446 green <c>+</c> over charge-ready faces).</summary>
        public const string PortraitReadyPlus = "+";
        /// <summary>
        /// Hard r58 portrait ready stack: green <c>+</c> over red <c>SLIDE</c> when slide is ready.
        /// </summary>
        public const string SlideReadyPipStack =
            "<color=#7DFF6A><b>+</b></color>\n<color=#FF5A5A><b>SLIDE</b></color>";
        /// <summary>Boss intro epithet banner (P0 t100 Loki). Frame-locked Desire Master chrome.</summary>
        public const string BossEpithetDesire = "THE MASTER OF DESIRE";
        /// <summary>Boss intro warn panel word (P0 t100). Distinct from field <see cref="EnemyWarn"/> <c>WARNING!!</c>.</summary>
        public const string BossWarn = "WARNING";
        /// <summary>Loki boss intro role under WARNING (P0 t100).</summary>
        public const string BossRoleDarkPrince = "Dark Prince";
        /// <summary>Frame-locked boss role under WARNING. Null = name only.</summary>
        public static string BossRoleForName(string bossName)
        {
            if (string.IsNullOrEmpty(bossName)) return null;
            if (bossName.IndexOf("Loki", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return BossRoleDarkPrince;
            return null;
        }
        /// <summary>
        /// SHOWTIME rank stub when content did not fill rank/LV.
        /// Do not print RANK 1: r50 is RANK 1, r62/PVP5 t090 are RANK 7, both at LV 10/10.
        /// </summary>
        public const string SlideRankStub = "RANK — LV —/—";
        /// <summary>Format a known SHOWTIME rank line. Caller must pass real content numbers.</summary>
        public static string SlideRankLine(int rank, int lv, int lvMax)
        {
            if (rank < 1) rank = 1;
            if (lv < 1) lv = 1;
            if (lvMax < 1) lvMax = 1;
            return "RANK " + rank + " LV " + lv + "/" + lvMax;
        }
        /// <summary>
        /// SHOWTIME rank from external skill fields. 0 = unknown → em dash.
        /// Unit level is not an input (same-fight r50 vs r62).
        /// </summary>
        public static string SlideRankFromContent(int rank, int lv, int lvMax)
        {
            var rankPart = rank > 0 ? rank.ToString() : "—";
            var lvPart = (lv > 0 && lvMax > 0) ? (lv + "/" + lvMax) : "—/—";
            return "RANK " + rankPart + " LV " + lvPart;
        }
        /// <summary>Obsolete unit-level mapper. Always stub — RANK is per-skill content.</summary>
        public static string SlideRankForUnitLevel(int unitLevel)
        {
            _ = unitLevel;
            return SlideRankStub;
        }
        public const string QteGreat = "GREAT!";
        public const string QtePerfect = "PERFECT!";
        public const string QteGood = "GOOD";
        public const string QteBad = "BAD";
        /// <summary>Robin ND Perfect sub (t55). Inventory — not live ordinary PVE.</summary>
        public const string QtePerfectSub = "DAMAGE 150%";
        /// <summary>QTE PERFECT damage word (Hard r55). Live stamp splits <c>DAMAGE</c> + <c>N%</c>.</summary>
        public const string QteDamageWord = "DAMAGE";
        public const string BattleStart = "FIGHT!";
        public const string DriveSelect = "DRIVE SELECT";
        /// <summary>Drive select plate hint. Primary tip: tap portrait when Drive ready.</summary>
        public const string DriveSelectHint = "Tap a Child to use Drive Skill";
        public const string DriveReady = "READY?";
        /// <summary>Portrait ready mark CN. Never alone as EN stamp — see <see cref="DriveReadyPortraitLine"/>.</summary>
        public const string DriveReadyPortrait = "驱动就绪";
        /// <summary>Primary P0/Robin over-portrait EN. Paired under CN in <see cref="DriveReadyPortraitLine"/>.</summary>
        public const string DriveReadyPortraitEn = "DRIVE SKILL READY";
        /// <summary>Portrait ready line. Primary P0/Robin over-portrait is EN only.</summary>
        public static string DriveReadyPortraitLine
            => "<size=13><b>" + DriveReadyPortraitEn + "</b></size>";
        /// <summary>Portrait tap-ready tag. Primary tip Skill ready.</summary>
        public const string TapReadyPortrait = "TAP READY";
        /// <summary>Portrait slide-ready ping. Primary tip Slide ready.</summary>
        public const string SlideReadyPortrait = "SLIDE READY";
        /// <summary>Robin ND over-portrait EN cue. Inventory only — no CN settle yet.</summary>
        public const string DriveSkillAdditionEn = "Drive Skill Addition";
        /// <summary>Robin ND OCR/variant of Addition cue. Primary Hard PERFECT field (r56).</summary>
        public const string LiveSkillAdditionEn = "Live Skill Addition";
        /// <summary>Robin mid-fight over-portrait EN. Inventory only (r56 Active Skill Addition).</summary>
        public const string ActiveSkillAdditionEn = "Active Skill Addition";
        /// <summary>Portrait auto-cast cue. Hard mid Robin tray (r36).</summary>
        /// <summary>Hard Auto cast portrait stamp (r36). Live via <see cref="VfxRouter"/>.</summary>
        public const string AutoSkillPortraitEn = "AUTO SKILL";
        /// <summary>P0 Auto cast badge above skill name (t500). Title-case <c>Auto</c>.</summary>
        public const string AutoCastBadge = "Auto";
        /// <summary>ND/hard area line inventory (Robin ~t15 <c>AREA n/m</c> vs ordinary PHASE).</summary>
        public const string AreaLineStub = "AREA —/—";
        /// <summary>ND win splash inventory (Robin ~t73 uppercase VICTORY vs P0 勝利+victory).</summary>
        public const string VictoryEnUpper = "VICTORY";
        /// <summary>Fever combo banner label.</summary>
        public const string ComboLabel = "COMBO";
        /// <summary>Fever / tip damage ledger. Primary tip field <c>TOTAL n DAMAGE</c> (P0 t445).</summary>
        public const string TotalDamageLabel = "TOTAL DAMAGE";
        /// <summary>Field / tip ledger line. Primary P0 t445: <c>TOTAL 427 DAMAGE</c>.</summary>
        public static string TotalDamageLine(int amount)
        {
            if (amount < 0) amount = 0;
            return "TOTAL " + amount + " DAMAGE";
        }
        /// <summary>Field resist pop. Primary Robin <c>RES</c>.</summary>
        public const string ResistFloat = "RES";
        /// <summary>Field pause control. Primary GT <c>|| PAUSE</c> / <c>PAUSE</c>.</summary>
        public const string PauseHud = "|| PAUSE";
        /// <summary>Resume while paused (field). Pause board uses CONTINUE.</summary>
        public const string ResumeHud = "CONTINUE";
        /// <summary>Top timer. Primary <c>mm:ss BATTLE TIME</c>.</summary>
        public const string BattleTime = "BATTLE TIME";
        /// <summary>Timer OCR/variant (P0 t88). Inventory — primary stays <see cref="BattleTime"/>.</summary>
        public const string RemainingTime = "REMAINING TIME";
        /// <summary>Robin Hard timer substamp (r55). Inventory — primary stays <see cref="BattleTime"/> (r70).</summary>
        public static string BattleNoLine(int n)
        {
            if (n < 1) n = 1;
            return "BATTLE NO. " + n;
        }
        /// <summary>Stage phase line. Primary <c>PHASE n/m</c>.</summary>
        public static string PhaseLine(int n, int max)
        {
            if (n < 1) n = 1;
            if (max < 1) max = 1;
            return "PHASE " + n + "/" + max;
        }
        /// <summary>Hard SHOWTIME OCR wave line (r21). Inventory — live Hard stays <see cref="PhaseLine"/> (r13/r38).</summary>
        public static string FinalLine(int n, int max)
        {
            if (n < 1) n = 1;
            if (max < 1) max = 1;
            return "FINAL " + n + "/" + max;
        }
        public const string DriveCast = "DRIVE";
        /// <summary>Field crush stamp CN substamp. Primary EN <see cref="DriveCrushEn"/>.</summary>
        public const string DriveCrush = "碾压";
        /// <summary>EN substamp under crush / primary stamp. Contrast/Ragna <c>DRIVE CRUSH</c>.</summary>
        public const string DriveCrushEn = "DRIVE CRUSH";
        /// <summary>Drive cut-in skill badge (Hard r52): stacked <c>DRIVE SKILL</c> beside skill name.</summary>
        public const string DriveSkillBadge = "DRIVE\nSKILL";
        /// <summary>Brief prep flash before crush. Not portrait DRIVE SKILL READY.</summary>
        public const string DrivePrep = "READY";
        public const string EnemyWarn = "WARNING!!";
        public const string EnemyWarnSub = "ENEMY DRIVE SKILL";
        /// <summary>Win stamp prompt (CN HUD). Splash stamp uses <see cref="TapScreenEn"/>.</summary>
        public const string TapScreen = "点按屏幕";
        /// <summary>Primary P0 ~t391 / Robin win splash: <c>Tap the screen.</c></summary>
        public const string TapScreenEn = "Tap the screen.";
        /// <summary>Win splash OCR/variant (P0 t390). Inventory — primary stays <see cref="TapScreenEn"/>.</summary>
        public const string TapScreenEnEllipsis = "Tap the screen...";
        /// <summary>Lose result title. Primary ordinary uses CLEAR!! on win; fail stamp inventory.</summary>
        public const string ResultFail = "DEFEAT";
        /// <summary>Result CTA home. EN under CN 回首页.</summary>
        public const string ResultHome = "HOME";
        public const string ResultNext = "NEXT";
        public const string ResultRetry = "RETRY";
        /// <summary>CLEAR right-rail retry label (P0 t472). Title case.</summary>
        public const string ResultRetryTitle = "Retry";
        /// <summary>CLEAR right-rail back label (P0 t472). Distinct from CTA <see cref="ResultHome"/>.</summary>
        public const string ResultBack = "Back";
        /// <summary>Enemy arch. Primary GT: <c>ENEMY HP TOTAL</c> / <c>LEFT</c> / <c>REM.</c> / Robin <c>REMAINING</c>.</summary>
        public const string EnemyHpLeft = "ENEMY HP TOTAL";
        /// <summary>PHASE splash secondary enemy label (P0 t510). Paired under TOTAL.</summary>
        public const string EnemyHpCurrent = "ENEMY HP (CURRENT)";
        /// <summary>Hard mid OCR/variant (r36). Inventory — primary stays <see cref="EnemyHpLeft"/>.</summary>
        public const string EnemyDarknessTotal = "DARKNESS TOTAL";
        /// <summary>Post-Hard stage transition (r72). Inventory — not battle HUD.</summary>
        public const string NowLoading = "NOW LOADING...";
        /// <summary>Enemy arch OCR/variant (inventory).</summary>
        public const string EnemyHpLeftAlt = "ENEMY HP LEFT";
        /// <summary>Enemy arch OCR/variant (inventory).</summary>
        public const string EnemyHpRem = "ENEMY HP REM.";
        /// <summary>Robin ND OCR/variant (inventory).</summary>
        public const string EnemyHpRemaining = "ENEMY HP REMAINING";
        /// <summary>Secondary top meter under enemy HP. Primary P0 ~t250: <c>ENEMY DRIVE</c>.</summary>
        public const string EnemyDrive = "ENEMY DRIVE";
        /// <summary>Robin ND foe under-HP pip (~t68). Inventory / boss stub.</summary>
        public const string EnemyCore = "CORE";
        /// <summary>Battle dialogue skip. Primary P0 ~t120: stacked <c>&gt;&gt;</c> over <c>SKIP</c>.</summary>
        public const string BattleSkip = ">>\nSKIP";
        public const string BattleSkipLine = ">> SKIP";
        /// <summary>VN log under SKIP. Primary P0 t400: stacked <c>...</c> over <c>Log</c>.</summary>
        public const string BattleLog = "...\nLog";
        public const string BattleLogLine = "Log";
        /// <summary>Tap tip title (P0 t58).</summary>
        public const string TipTapSkill = "Attack with a Tap Skill!";
        /// <summary>Tap tip body (P0 t58).</summary>
        public const string TipTapSkillBody = "Tap the icon to attack once you've built up your Skill Gauge.";
        /// <summary>Early mid-fight tip (P0 t60). Single-line plate — no separate body.</summary>
        public const string TipKeepAttacking = "Keep using your skills and attack the enemy!";
        /// <summary>Slide tip title (P0 tips2 t358).</summary>
        public const string TipSlideSkill = "Try out a Slide Skill!";
        /// <summary>Slide tip body (P0 tips2 t358).</summary>
        public const string TipSlideSkillBody = "Touch the icon, and slide it up!";
        /// <summary>Slide tip alt title (P0 tips t370 how-to plate).</summary>
        public const string TipSlidePower = "Use Slide Skills for powerful attacks!";
        /// <summary>Slide tip alt body (P0 tips t370).</summary>
        public const string TipSlidePowerBody = "Once the Skill Gauge is full, slide upwards.";
        /// <summary>
        /// Mona's Tips slide stem (P0 t355). Truncated on tape as <c>Slide Skills are powerful a</c>.
        /// </summary>
        public const string TipSlideMonaBody = "Slide Skills are powerful attacks!";
        /// <summary>Drive tip title (P0 tips t380).</summary>
        public const string TipDriveSkill = "Try out a Drive Skill!";
        /// <summary>Drive tip body (P0 tips t380).</summary>
        public const string TipDriveSkillBody =
            "When the Drive Gauge is charged, tap the icon. You'll deal a big burst of damage.";
        /// <summary>Drive QTE tip title (P0 cont35 t382).</summary>
        public const string TipDriveTiming = "Try and tap with perfect timing!";
        /// <summary>Drive QTE tip body (P0 t382).</summary>
        public const string TipDriveTimingBody =
            "If you tap with perfect timing, you'll dole out an even greater amount of damage.";
        /// <summary>Drive QTE press cue. Primary P0 tip t382 <c>PRESS BUTTON</c>.</summary>
        public const string PressButton = "PRESS BUTTON";
        /// <summary>Drive QTE ticket OCR/variant (inventory). Live button uses <see cref="PressButton"/>.</summary>
        public const string GoodButton = "GOOD";
        /// <summary>Drive QTE tutorial callout (P0 t382). Inventory only — not live every QTE.</summary>
        public const string HereIsAPoint = "HERE IS A POINT!!";
        /// <summary>Drive tip title (P0 cont33 t388 table tip).</summary>
        public const string TipDriveTables = "Turn the tables with a Drive Skill!";
        /// <summary>Drive tip body (P0 t388).</summary>
        public const string TipDriveTablesBody =
            "Your Drive Skill gauge builds up with each attack.\n"
            + "Powerful attacks such as Slide Skills and criticals fill the gauge even faster.\n"
            + "Each Child has a Drive Skill with a different effect, so be sure to use them wisely!";
        /// <summary>Team Total HP tip body (P0 tips t345). Title uses <see cref="TeamHpTip"/>.</summary>
        public const string TipTeamHpBody =
            "The gauge below is your team's Total HP. Even if one of your team members dies you aren't defeated. Only when your team's Total HP reaches 0 do you lose.";
        /// <summary>Early tray tip (P0 cont34 / keep4 t250). Truncated mid-sentence on some frames.</summary>
        public const string TipChildsTray =
            "The Childs on your team are lined up below. For now, it's just me.";
        /// <summary>Inventory truncated stem of <see cref="TipChildsTray"/> (t250 cut).</summary>
        public const string TipChildsTrayStem = "The Childs on your team are lined up below.";
        /// <summary>Mona Tips party-size tip (P0 t344). After tray tip.</summary>
        public const string TipChildsMoreBody =
            "I'm sure you've figured this out already, but the more Childs you add to your party, the easier battles will get!";
        /// <summary>Mona Tips battle-speed tip (P0 t451). X1 SPEED highlight + full body.</summary>
        public const string TipSpeedBody =
            "If you want to kick up the pace a bit in battle, tap X2 SPEED to make all attacks two times faster.";
        /// <summary>
        /// Robin ND / Hard bottom party arc (r25) and early P0 tip (t250): <c>MY HP TOTAL</c>.
        /// Ordinary mid-fight stays <see cref="TeamHpTotal"/>.
        /// </summary>
        public const string MyHpTotal = "MY HP TOTAL";
        /// <summary>Hard/ND arch OCR short enemy HP (inventory; mid-fight r62 stays <see cref="EnemyHpLeft"/>).</summary>
        public const string EnemyHpShort = "ENEMY HP";
        /// <summary>Robin mid-fight over-portrait OCR short (r56). Inventory — not live every Perfect.</summary>
        public const string SkillAdditionEn = "Skill Addition";
        /// <summary>
        /// Portrait inner costume title. Primary P0 tray Mona <c>Lv.1 Eclipse</c> / Dark Water.
        /// Frame-locked name→title only — not a full EN costume table.
        /// </summary>
        public static string PortraitCostumeStub(int level, string costumeEn)
        {
            if (level < 1) level = 1;
            if (string.IsNullOrEmpty(costumeEn)) return "Lv." + level;
            return "Lv." + level + " " + costumeEn;
        }
        /// <summary>Frame-locked costume titles from P0 tray (t83/t88/t435). Null = unknown.</summary>
        public static string CostumeTitleForName(string charName)
        {
            if (string.IsNullOrEmpty(charName)) return null;
            if (charName.IndexOf("Mona", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CostumeEclipseEn;
            if (charName.IndexOf("Pixie", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CostumeDarkWaterEn;
            if (charName.IndexOf("Lisa", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CostumeOceanStormEn;
            if (charName.IndexOf("Davi", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CostumeFireSerpentEn;
            // Robin ND tray Tiamat (r65). Frame-locked name→title.
            if (charName.IndexOf("Tiamat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CostumePrimordialSerpentEn;
            // P0 t501 tray Valkyrie costume.
            if (charName.IndexOf("Valkyrie", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return CostumeBlackWidowEn;
            return null;
        }
        public static string PortraitInnerCostumeLine(string charName, int level)
        {
            var title = CostumeTitleForName(charName);
            if (title == null) return "";
            return PortraitCostumeStub(level, title);
        }
        /// <summary>Foe size class on bloops (P0 t250 Blue Bloop). Inventory for other sizes.</summary>
        public const string FoeSizeSmall = "Small";
        /// <summary>Frame-locked foe costume/title (Robin ND + P0 hangers). Null = unknown.</summary>
        public static string FoeTitleForName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name.StartsWith("Fresh ", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("Fresh", System.StringComparison.OrdinalIgnoreCase))
                return "Fresh";
            if (name.StartsWith("Old ", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("Old", System.StringComparison.OrdinalIgnoreCase))
                return "Old";
            if (name.IndexOf("Hades", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Seething";
            if (name.IndexOf("Robin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return AllyRoleHero;
            // Hard r42/r47: plate title Lovey-Dovey over Sytry (skill stub shares string).
            if (name.IndexOf("Sytry", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Lovey-Dovey";
            if (name.IndexOf("Werewolf", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Title only when name already carries Oracle (inventory).
                // Robin r50: plain "Dracula Werewolf" — no title stack.
                if (name.StartsWith("Oracle ", System.StringComparison.OrdinalIgnoreCase)
                    || name.Equals("Oracle", System.StringComparison.OrdinalIgnoreCase))
                    return "Oracle";
                return null;
            }
            if (name.IndexOf("Neamhain", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Battle Kitty";
            // Robin Hard r30: Green Victorix plate (title Green / N Victorix).
            if (name.IndexOf("Victorix", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Green";
            return null;
        }
        /// <summary>
        /// Foe field plate. Bloops: <c>Small</c>/<c>N Name</c>; titled: <c>Title</c>/<c>N Name</c>
        /// (ND r25; P0 Fresh/Old Skull t83; Seething Hades t70); else <c>N Name</c>.
        /// </summary>
        public static string FoePlateLine(int level, string name)
        {
            if (level < 1) level = 1;
            if (string.IsNullOrEmpty(name)) name = "";
            if (name.IndexOf("Bloop", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var body = name;
                if (body.StartsWith("Small ", System.StringComparison.OrdinalIgnoreCase))
                    body = body.Substring(6);
                return FoeSizeSmall + "\n" + level + " " + body;
            }
            var title = FoeTitleForName(name);
            if (title != null)
            {
                var body = name;
                var prefix = title + " ";
                if (body.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    body = body.Substring(prefix.Length);
                return title + "\n" + level + " " + body;
            }
            return level + " " + name;
        }
        /// <summary>Inventory costume title seen on P0 Mona tray (t456/t83).</summary>
        public const string CostumeEclipseEn = "Eclipse";
        /// <summary>Inventory costume title (handoff note; P0 Lisa tray t83).</summary>
        public const string CostumeOceanStormEn = "Ocean Storm";
        /// <summary>Inventory costume title (P0 Davi tray t88).</summary>
        public const string CostumeFireSerpentEn = "Fire Serpent";
        /// <summary>Inventory costume title (handoff note; not yet frame-locked here).</summary>
        public const string CostumeDarkWaterEn = "Dark Water";
        /// <summary>Robin ND tray costume OCR (r65 Tiamat). Inventory — not in P0 name table.</summary>
        public const string CostumePrimordialSerpentEn = "Primordial Serpent";
        /// <summary>P0 t501 tray costume for Valkyrie.</summary>
        public const string CostumeBlackWidowEn = "Black Widow";
        /// <summary>P0 t74 Hades VN forehead costume. Inventory — plate title stays Seething.</summary>
        public const string CostumeDeathPatchEn = "Death Patch";
        /// <summary>P0 t74 Hades VN line. Inventory.</summary>
        public const string BattleLineHadesMorePain =
            "This will not suffice. I need more. More pain! More suffering!";
        /// <summary>P0 t78 Davi battle-cut dialogue. Inventory stem.</summary>
        public const string BattleLineDaviOkeyDo = "Okey-do.";
        /// <summary>P0 t120 Mona battle VN. Inventory.</summary>
        public const string BattleLineMonaWipe =
            "Master, we're going to all be wiped out at this rate!";
        /// <summary>
        /// Bottom party arc label. Primary tip ~t345 calls mechanic <c>Total HP</c>;
        /// on-arc chrome reads stacked <c>N%</c> over <c>SKILL HP TOTAL</c> (t250/t508).
        /// </summary>
        public const string TeamHpTotal = "SKILL HP TOTAL";
        /// <summary>Tip / early-fight / t60 variant of party arc (<c>TEAM HP TOTAL</c>). Hard / post-Fever low-level uses <see cref="MyHpTotal"/>.</summary>
        public const string TeamHpTotalAlt = "TEAM HP TOTAL";
        /// <summary>PHASE splash secondary party label (P0 t510). Paired under TOTAL.</summary>
        public const string TeamHpCurrent = "TEAM HP (CURRENT)";
        /// <summary>Mid-fight OCR/variant of party arc. Primary Drive tip t380: <c>PARTY HP TOTAL</c>.</summary>
        public const string PartyHpTotal = "PARTY HP TOTAL";
        /// <summary>P0 Drive cut-in skill name for Mona (t382). Frame-locked stub — not a full skill table.</summary>
        public const string DriveSkillMonaEn = "Filled with Love";
        /// <summary>Hard DriveCrush skill name for Sakuragawa (r52). Frame-locked stub.</summary>
        public const string DriveSkillSakuragawaEn = "Silent Hunter";
        /// <summary>DriveCrush OCR compact (r51). Inventory — primary spaced <see cref="DriveSkillSakuragawaEn"/>.</summary>
        public const string DriveSkillSakuragawaCompactEn = "SilentHunter";
        /// <summary>Hard SHOWTIME slide skill name for Tiamat (r67 / r38). Frame-locked stub.</summary>
        public const string SlideSkillTiamatEn = "Snake Bite";
        /// <summary>Hard SHOWTIME slide skill name for Robin (r50). Frame-locked stub.</summary>
        public const string SlideSkillRobinEn = "In the Name of Justice";
        /// <summary>Hard SHOWTIME Pomona Slide (r62 / cont52 t62.5). Frame-locked stub.</summary>
        public const string SlideSkillPomonaEn = "White Veil";
        /// <summary>OCR/variant Pomona Slide (robin_t45 Saint's Blessings). Inventory.</summary>
        public const string SlideSkillSaintsBlessingsEn = "Saint's Blessings";
        /// <summary>Alias of <see cref="SlideSkillPomonaEn"/> (kept for older call sites).</summary>
        public const string SlideSkillWhiteVeilEn = "White Veil";
        /// <summary>P0 tutorial SHOWTIME slide skill for Pixie (t360). Frame-locked stub.</summary>
        public const string SlideSkillPixieEn = "Freeze Lance";
        /// <summary>Hard SHOWTIME tray skill badge for Sakuragawa (r49). Frame-locked stub.</summary>
        public const string SlideSkillSakuragawaEn = "Royal Hellboard";
        /// <summary>Hard SHOWTIME slide skill for Tamogami (r31). Frame-locked stub.</summary>
        public const string SlideSkillTamogamiEn = "Desperation";
        /// <summary>Hard mid-fight Auto cast float for Pomona (r22). Frame-locked stub.</summary>
        public const string AutoSkillPomonaEn = "Wedding Bells";
        /// <summary>P0 mid-fight Auto cast for Mona (t500). Same string as costume title — frame-locked.</summary>
        public const string AutoSkillMonaEn = "Eclipse";
        /// <summary>P0 mid-fight Auto cast for Pixie (t452). Same string as costume title — frame-locked.</summary>
        public const string AutoSkillPixieEn = "Dark Water";
        /// <summary>Hard mid-fight Auto/field skill for Sytry (r48). Frame-locked stub.</summary>
        public const string AutoSkillSytryEn = "Lovey-Dovey";
        /// <summary>Frame-locked Drive skill name by caster. Null = unknown / use cast label.</summary>
        public static string DriveSkillStubForName(string charName)
        {
            if (string.IsNullOrEmpty(charName)) return null;
            if (charName.IndexOf("Mona", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DriveSkillMonaEn;
            if (charName.IndexOf("Sakuragawa", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DriveSkillSakuragawaEn;
            return null;
        }
        /// <summary>Frame-locked Slide skill name by caster. Null = unknown / use cast label.</summary>
        public static string SlideSkillStubForName(string charName)
        {
            if (string.IsNullOrEmpty(charName)) return null;
            if (charName.IndexOf("Tiamat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return SlideSkillTiamatEn;
            if (charName.IndexOf("Robin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return SlideSkillRobinEn;
            if (charName.IndexOf("Pomona", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return SlideSkillPomonaEn;
            if (charName.IndexOf("Pixie", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return SlideSkillPixieEn;
            if (charName.IndexOf("Sakuragawa", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return SlideSkillSakuragawaEn;
            if (charName.IndexOf("Tamogami", System.StringComparison.OrdinalIgnoreCase) >= 0
                || charName.IndexOf("Tamagami", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return SlideSkillTamogamiEn;
            return null;
        }
        /// <summary>Frame-locked Auto skill name by caster. Null = unknown / use cast label.</summary>
        public static string AutoSkillStubForName(string charName)
        {
            if (string.IsNullOrEmpty(charName)) return null;
            if (charName.IndexOf("Pomona", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return AutoSkillPomonaEn;
            if (charName.IndexOf("Mona", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return AutoSkillMonaEn;
            if (charName.IndexOf("Pixie", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return AutoSkillPixieEn;
            if (charName.IndexOf("Sytry", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return AutoSkillSytryEn;
            return null;
        }
        /// <summary>P0 t70 / t455 party arc variant <c>CHILD HP TOTAL</c>.</summary>
        public const string ChildHpTotal = "CHILD HP TOTAL";
        /// <summary>Team HP tip field label (P0 t345): <c>SHARED HP TOTAL</c>.</summary>
        public const string SharedHpTotal = "SHARED HP TOTAL";
        /// <summary>Team HP tip on-arc chrome (P0 t345). Live while Total HP tip is up.</summary>
        public const string TotalHpTotal = "TOTAL HP TOTAL";
        /// <summary>Drive tip secondary (P0 t388). Live <c>13% SKILL RATE</c>.</summary>
        public const string SkillRate = "SKILL RATE";
        /// <summary>Drive tip OCR title-case (inventory). Live stays <see cref="SkillRate"/>.</summary>
        public const string SkillRateTitleCase = "Skill Rate";
        /// <summary>Childs tip OCR/variant (inventory). Live tip arc stays <see cref="TeamHpTotal"/> (t250).</summary>
        public const string CurrentHp = "CURRENT HP";
        /// <summary>Hard arch secondary during Fever (r67). Inventory/live when FeverActive.</summary>
        public const string DmgOfTotal = "DMG OF TOTAL";
        /// <summary>Hard arch secondary meter (r61): <c>PREPARATION</c> under the top fill.</summary>
        public const string EnemyPreparation = "PREPARATION";
        /// <summary>Field evade float (Hard r61). Short <c>EVA</c>.</summary>
        public const string EvaFloat = "EVA";
        /// <summary>Hard foe overhead OCR (r12 Oracle Werewolf). Inventory — wire UNKNOWN.</summary>
        public const string FoeReveal = "REVEAL";
        /// <summary>Tip wording for party arc mechanic (P0 t345 Total HP tip).</summary>
        public const string TeamHpTip = "Total HP";
        /// <summary>Alias for <see cref="TeamHpTotal"/>.</summary>
        public const string SkillHpTotal = TeamHpTotal;
        /// <summary>
        /// Stacked meter chrome. Primary P0 t250/t508: large <c>N%</c> over label
        /// (<c>SKILL HP TOTAL</c> / <c>SKILL GAUGE</c> / <c>ENEMY HP TOTAL</c>).
        /// </summary>
        public static string PctOverLabel(int pct, string label)
        {
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            if (string.IsNullOrEmpty(label)) label = "";
            return "<size=15><b>" + pct + "%</b></size>\n<size=10>" + label + "</size>";
        }
        /// <summary>
        /// Bottom drive meter mid-fight. Primary P0 t500 live: <c>SKILL GAUGE TOTAL</c>.
        /// Short <see cref="SkillGauge"/> / SHOWTIME <see cref="DriveGauge"/> stay variants.
        /// </summary>
        public const string SkillGaugeTotal = "SKILL GAUGE TOTAL";
        /// <summary>Short bottom drive meter OCR/variant (inventory). Live mid-fight uses <see cref="SkillGaugeTotal"/>.</summary>
        public const string SkillGauge = "SKILL GAUGE";
        /// <summary>Tip / SHOWTIME variant of bottom drive meter (P0 t360 / tip t388 art).</summary>
        public const string DriveGauge = "DRIVE GAUGE";
        /// <summary>Robin ND OCR/variant of party arc (r40). Inventory.</summary>
        public const string DriveTotal = "DRIVE TOTAL";
        /// <summary>Field weak-point stamp. Primary Robin ~t68 <c>WeakPoint</c>.</summary>
        public const string WeakPoint = "WeakPoint";
        /// <summary>Ally field role stub. Primary Robin ~t68 leaf + <c>Hero</c>.</summary>
        public const string AllyRoleHero = "Hero";
        /// <summary>Portrait QTE countdown. Primary P0 ~t365: large <c>N</c> + <c>DRIVE TIME</c>.</summary>
        public static string DriveTimeLine(int sec)
        {
            if (sec < 0) sec = 0;
            return "<size=22><b>" + sec + "</b></size> <size=11>DRIVE TIME</size>";
        }
        /// <summary>Portrait cool. Primary P0 tip t370/t380 + Robin mid-fight: <c>N COOL TIME</c>.</summary>
        public static string CoolTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>COOL TIME</size>";
        }
        /// <summary>Hard Fever+SHOWTIME cool overlay (r67). <c>N FEVER TIME</c>.</summary>
        public static string FeverTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>FEVER TIME</size>";
        }
        /// <summary>SHOWTIME cool OCR/variant. Inventory — Hard SHOWTIME live uses <see cref="LeadTimeLine"/> (r50).</summary>
        public static string SlideTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>SLIDE TIME</size>";
        }
        /// <summary>Hard SHOWTIME cool overlay (r50 Robin/Tiamat). <c>N LEAD TIME</c> when Fever banked low.</summary>
        public static string LeadTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>LEAD TIME</size>";
        }
        /// <summary>Hard SHOWTIME cool overlay with banked Fever (r67). <c>N CLICK TIME</c>.</summary>
        public static string ClickTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>CLICK TIME</size>";
        }
        /// <summary>Hard SHOWTIME dual cool OCR (r38 Tiamat). Inventory — trigger / second clock UNKNOWN.</summary>
        public static string LockTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>LOCK TIME</size>";
        }
        /// <summary>Ordinary SHOWTIME / Drive tip cool (r67 / P0 t365). Hard SHOWTIME uses <see cref="LeadTimeLine"/>.</summary>
        public static string SkillTimeLine(int sec)
        {
            if (sec < 1) sec = 1;
            return "<size=22><b>" + sec + "</b></size> <size=11>SKILL TIME</size>";
        }
        /// <summary>Fever auto / weak portrait stamp (P0 t452 <c>WEAKPOINT</c>).</summary>
        public const string WeakPointPortrait = "WEAKPOINT";
        /// <summary>Spaced OCR variant (inventory). Live stamp stays <see cref="WeakPointPortrait"/>.</summary>
        public const string WeakPointPortraitSpaced = "WEAK POINT";
        /// <summary>Hard SHOWTIME near-Fever portrait (r15). Stacked under WEAKPOINT.</summary>
        public const string SkillReserve = "SKILL RESERVE";
        /// <summary>Hard support-friend portrait banner (r16). Inventory.</summary>
        public const string ServicePortrait = "SERVICE";
        /// <summary>Hard boss field status OCR (r16). Inventory.</summary>
        public const string IsolationStatus = "ISOLATION";
        /// <summary>Portrait ready-tag line for <see cref="WeakPointPortrait"/> (P0 t452).</summary>
        public static string WeakPointPortraitLine
            => "<size=13><b>" + WeakPointPortrait + "</b></size>";
        /// <summary>Hard r15: WEAKPOINT over SKILL RESERVE on near-Fever SHOWTIME tray.</summary>
        public static string WeakPointSkillReserveLine
            => WeakPointPortraitLine + "\n<size=12><b>" + SkillReserve + "</b></size>";
        /// <summary>Live Fever combo damage (P0 t442 <c>353 DAMAGE</c>). Tip field keeps TOTAL.</summary>
        public static string FeverDamageLine(int amount)
        {
            if (amount < 0) amount = 0;
            return amount + " DAMAGE";
        }
        /// <summary>Tip-field damage art (P0 t444/t445). Live combo stays <see cref="FeverDamageLine"/>.</summary>
        public static string TipTotalDamageLine(int amount)
        {
            if (amount < 0) amount = 0;
            return "<size=14>TOTAL</size>\n<size=36><b>" + amount + "</b></size>\n<size=18>DAMAGE</size>";
        }
        /// <summary>Party-arc OCR/variant (tip inventory; mid-fight stays <see cref="TeamHpTotal"/>).</summary>
        public const string OwnHpTotal = "OWN HP TOTAL";
        /// <summary>Tutorial tip speech header (P0 t250/t352).</summary>
        public const string MonaTipsHeader = "Mona's Tips";
        /// <summary>Overhead foe banner (P0 t70/t83 wooden hangers). Frame-locked.</summary>
        public const string FoeBannerHanged = "The Hanged";
        /// <summary>Overhead foe banner (P0 t88 Fresh Skull). Live for Fresh Skull class.</summary>
        public const string FoeBannerRanger = "The Ranger";
        /// <summary>Overhead foe banner OCR (inventory). Not wired by name class.</summary>
        public const string FoeBannerFlanger = "The Flanger";
        /// <summary>P0 t352 SkillReady tip arc. Live during tip window; mid-fight stays TeamHpTotal.</summary>
        public const string SkillUpTotal = "SKILL UP TOTAL";
        /// <summary>P0 t88 secondary arch OCR under ENEMY HP. Inventory — live stays <see cref="EnemyDrive"/>.</summary>
        public const string EnemyTeamHpTotal = "ENEMY TEAM HP TOTAL";
        /// <summary>Hard QTE settle OCR (r55). Inventory — live arch stays EnemyHpShort / EnemyHpLeft.</summary>
        public const string NearlyTotal = "NEARLY TOTAL";
        /// <summary>Hard mid slide ray tag (r14). Inventory — performance formula UNKNOWN.</summary>
        public const string SlideSkillPerformance = "SLIDE SKILL performance";
        /// <summary>Hard mid under-timer fever factor (r14 <c>1.1 FEVER</c>). Inventory — multiplier table UNKNOWN.</summary>
        public static string FeverFactorLine(float factor)
        {
            if (factor < 0f) factor = 0f;
            return factor.ToString("0.0") + " FEVER";
        }
        /// <summary>Hard mid secondary arch OCR (r36). Inventory — live stays EnemyDrive / PREPARATION.</summary>
        public const string NextSecsTotal = "NEXT 0.0s TOTAL";
        /// <summary>SHOWTIME stamp OCR spaced (r38). Live stamp stays <c>IT'S SHOWTIME!!</c>.</summary>
        public const string ItsShowTimeSpaced = "IT'S SHOW TIME!!";
        /// <summary>Overhead banner for skull hangers; null otherwise.</summary>
        public static string FoeBannerForName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name.IndexOf("Flanger", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return FoeBannerFlanger;
            if (name.IndexOf("Fresh", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return FoeBannerRanger;
            if (name.IndexOf("Skull", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Hanged", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return FoeBannerHanged;
            return null;
        }
        /// <summary>Portrait level under name. Primary P0 tray at cap <c>60 MAX</c>; below cap <c>Lv.N</c>.</summary>
        public static string PortraitLevelLine(int level)
        {
            if (level < 1) level = 1;
            if (level >= Growth.MaxLevel) return level + " MAX";
            return "Lv." + level;
        }
        /// <summary>Tray name line. Cap: <c>60 MAX Name</c> (Robin tray); below: <c>N Name</c> (P0 t83).</summary>
        public static string PortraitTrayName(int level, string shortName)
        {
            if (level < 1) level = 1;
            if (string.IsNullOrEmpty(shortName)) shortName = "";
            if (level >= Growth.MaxLevel)
                return level + " MAX " + shortName;
            return level + " " + shortName;
        }
        /// <summary>Legacy MAX badge — tray now embeds MAX in <see cref="PortraitTrayName"/>.</summary>
        public static string PortraitMaxBadge(int level)
        {
            return "";
        }

        /// <summary>Account LEVEL UP modal CN. Primary P0 ~t470 after CLEAR.</summary>
        public const string LevelUp = "等级提升";
        /// <summary>EN emblem / substamp. Primary P0 ~t470 <c>LEVEL UP!</c>.</summary>
        public const string LevelUpEn = "LEVEL UP!";
        /// <summary>Orange sub-header under crest. Primary P0 ~t470 <c>Level up!</c>.</summary>
        public const string LevelUpSubEn = "Level up!";
        /// <summary>
        /// Stamina lines. Primary P0 ~t470 EN exact (two colors on GT):
        /// white refill + orange max+1. Amount UNKNOWN outside tutorial — stub "1".
        /// </summary>
        public const string LevelUpStaminaRefill = "Stamina has been refilled to maximum.";
        public const string LevelUpStaminaMaxUp = "Max stamina increased by 1.";
        public const string LevelUpStaminaStub =
            LevelUpStaminaRefill + "\n" + LevelUpStaminaMaxUp;
        /// <summary>Tutorial tip banner. Primary P0 tip overlays <c>TIP!</c>.</summary>
        public const string TipBanner = "TIP!";
        /// <summary>Fever window tip title (P0 t435).</summary>
        public const string TipFeverActivate = "Activate Fever Time!";
        /// <summary>Fever window tip body (P0 t435). 14s aligned to UnknownFeverWindowSec.</summary>
        public const string TipFeverWindowBody = "For 14 seconds tap the icons as fast as you can!";
        /// <summary>Fever tip body when pointing at gauge (P0 t420).</summary>
        public const string TipFeverGaugeBody = "Fever Time is activated when this gauge fills up to 100%.";
        /// <summary>Fever tip title during rate plate (P0 cont28 t445).</summary>
        public const string TipFeverBarrage = "When Fever Time hits, it's a barrage!";
        /// <summary>Fever tip body rates (P0 t445). Aligns with <c>BattleSim.QteFever</c> tip values.</summary>
        public const string TipFeverRatesBody =
            "Fever Time is activated when the gauge fills up to 100%.\n"
            + "PERFECT! Gauge at 40% charge.\n"
            + "GREAT! Gauge at 30% charge.\n"
            + "GOOD Gauge at 15% charge.";
        /// <summary>P0 t445 tip field ledger above rates plate.</summary>
        public const string TipFeverTotalDamageStub = "TOTAL 427 DAMAGE";
        /// <summary>Fever tip footer (P0 t445).</summary>
        public const string TipFeverBarrageFooter =
            "By filling your Fever Gauge, you will get a short window to use skills and unleash a slew of attacks.";
        /// <summary>Fever tip child note (P0 t445).</summary>
        public const string TipFeverChildNote = "Each Child has a different fever effect, so use them wisely!";
        /// <summary>Hard mid-fight right-rail tally tabs (Robin r30: 5/3/8). Inventory — role UNKNOWN.</summary>
        public const string HardRailTabsNote = "HARD_RAIL_TABS";
        /// <summary>
        /// Frame-locked Hard arch stage title. Robin r30/r70 Stage 8.
        /// Null = fall back to catalog name + (Hard).
        /// </summary>
        public static string HardStageArchStub(int stageIndex1Based)
        {
            if (stageIndex1Based == 8)
                return "Stage 8 You Won't Get Away! (Hard)";
            return null;
        }
        /// <summary>Pause OCR/variant <c>II PAUSE</c> (Robin). Primary field stays <see cref="PauseHud"/> (r30/r70).</summary>
        public const string PauseHudIi = "II PAUSE";
        /// <summary>Fever tip art OCR variant. Live stamp stays <see cref="FeverTimeEn"/>.</summary>
        public const string FeverTimeTipArt = "FEVER TIME!!";
        /// <summary>Confirm CTA under modal. Primary EN Confirm; CN 确认.</summary>
        public const string LevelUpConfirm = "确认";
        /// <summary>EN Confirm button label (P0 t470).</summary>
        public const string LevelUpConfirmEn = "Confirm";
        /// <summary>Level arrow. Primary P0 t470: <c>n ▶ m</c> under Level caps (or prefixed line).</summary>
        public static string LevelUpArrow(string fromLv, string toLv)
            => (fromLv ?? "—") + " ▶ " + (toLv ?? "—");
        /// <summary>OCR/variant ascii greater-than (inventory). Live uses <see cref="LevelUpArrow"/>.</summary>
        public static string LevelUpArrowAscii(string fromLv, string toLv)
            => (fromLv ?? "—") + " > " + (toLv ?? "—");
        /// <summary>Prefixed LEVEL UP line (P0 t470). Live modal uses this when caps row is omitted.</summary>
        public static string LevelUpArrowPrefixed(string fromLv, string toLv)
            => "Level " + (fromLv ?? "—") + "  ▶  " + (toLv ?? "—");
        /// <summary>OCR triangle variant (inventory ▷). Live stays black ▶.</summary>
        public static string LevelUpArrowPrefixedWhiteTri(string fromLv, string toLv)
            => "Level " + (fromLv ?? "—") + " ▷ " + (toLv ?? "—");
        /// <summary>CLEAR aftermath LEVEL UP from/to stubs (P0 t470 Stage 3 Tutorial). Not a formula.</summary>
        public const string LevelUpFromStub = "2";
        public const string LevelUpToStub = "3";
        /// <summary>CLEAR yellow-band account name stub (P0 t472 <c>GameplayHe</c>).</summary>
        public const string ClearAccountStub = "GameplayHe";
        /// <summary>
        /// Frame-locked CLEAR band amounts from P0 t472 Stage 3 Tutorial.
        /// Not a formula — other stages stay UNKNOWN; do not promote to GL_FINAL.
        /// </summary>
        public const string ClearLevelStub = "3";
        public const string ClearExpGainStub = "6";
        public const string ClearGoldGainStub = "70";
        public const string ClearExpBarStub = "EXP  0 / 18";
        /// <summary>CLEAR band inventory (P0 t396 Stage 2 Tutorial). Live band stays t472 stubs.</summary>
        public const string ClearLevelStubStage2 = "2";
        public const string ClearGoldGainStubStage2 = "73";
        public const string ClearExpBarStubStage2 = "EXP  6 / 12";
        /// <summary>Green LEVEL UP chip on CLEAR yellow band (P0 t472) before modal.</summary>
        public const string ClearLevelUpChip = "LEVEL UP";
        /// <summary>CLEAR right-rail BOSS icon label (P0 t472). Inventory — CTA still HOME/NEXT.</summary>
        public const string ResultBoss = "BOSS";
        /// <summary>CLEAR top total stub (P0 t398 <c>Total 2</c>). Inventory — count math UNKNOWN.</summary>
        public const string ClearTotalStub = "Total 2";
        /// <summary>CLEAR reward portrait name (P0 t396 left).</summary>
        public const string ClearRewardPixie = "Silent Pixie";
        /// <summary>CLEAR reward portrait name (P0 t396 right).</summary>
        public const string ClearRewardMona = "Mona";
        /// <summary>Skill-ready tip title (P0 cont34 t352 / Mona Tips t354).</summary>
        public const string TipSkillReadyTitle = "Your Skill is ready to go.";
        /// <summary>Skill-ready tip body. Primary Mona Tips t354 full stem (not truncated).</summary>
        public const string TipSkillReady =
            "When your Skill Gauge fills up, you can use either a Tap Skill or a Slide Skill.";
        /// <summary>Combined skill-ready plate under <see cref="MonaTipsHeader"/> (P0 t354).</summary>
        public static string TipSkillReadyMonaBody
            => TipSkillReadyTitle + " " + TipSkillReady;

        public enum Kind
        {
            None = 0,
            BattleStart,
            SlideShowtime,
            FeverTime,
            QtePerfect,
            QteGreat,
            QteGood,
            QteBad,
            DriveSelect,
            DriveReady,
            DriveCast,
            EnemyWarn
        }

        public static bool ShowtimeDistinctFromFever
        {
            get
            {
                return SlideShowtime != FeverTime
                    && SlideShowtime != BattleStart
                    && FeverTime != BattleStart;
            }
        }

        public static string Title(Kind kind)
        {
            switch (kind)
            {
                case Kind.BattleStart: return BattleStart;
                case Kind.SlideShowtime: return SlideShowtime;
                case Kind.FeverTime: return FeverTime;
                case Kind.QtePerfect: return QtePerfect;
                case Kind.QteGreat: return QteGreat;
                case Kind.QteGood: return QteGood;
                case Kind.QteBad: return QteBad;
                case Kind.DriveSelect: return DriveSelect;
                case Kind.DriveReady: return DriveReady;
                case Kind.DriveCast: return DriveCast;
                case Kind.EnemyWarn: return EnemyWarn;
                default: return "";
            }
        }

        public static string FeverLine(bool window, int pct)
        {
            // Live / tip: window uses live field word; tip stamp keeps FEVER TIME!!.
            if (window) return FeverTimeLive;
            // Bottom arc: primary GT "FEVER n%" (Robin / P0).
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return "FEVER " + pct + "%";
        }

        /// <summary>Mid-field fill callout. Primary Robin ~t56: <c>40% TO FEVER</c> while arc also reads FEVER 40%.</summary>
        public static string FeverTowardLine(int pct)
        {
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            return pct + "% TO FEVER";
        }

        /// <summary>
        /// QTE stamp subline. Primary Robin ~t55 shows <c>N% TO FEVER</c>; tip rates feed amount.
        /// Amount comes from <c>BattleSim.QteFever</c> (P0 tip rates), not live gauge.
        /// </summary>
        public static string QteFeverGainLine(int gainPct)
        {
            if (gainPct < 0) gainPct = 0;
            return gainPct + "% TO FEVER";
        }

        public static string SlideBanner(string skillName)
        {
            if (string.IsNullOrEmpty(skillName)) return SlideShowtime;
            return SlideShowtime + "  " + skillName;
        }

        /// <summary>
        /// Title handed to <c>VfxWordStamp</c>. Channel-owned words no-op there
        /// so Slide/Fever/QTE do not collapse into one generic slash.
        /// HUD loc for Slide stays <see cref="SlideShowtime"/> (开演); do not
        /// send 开演 through the generic stamp (it is not in that no-op list).
        /// </summary>
        public static string VfxChannelKey(Kind kind)
        {
            if (kind == Kind.SlideShowtime) return "IT'S SHOWTIME!!";
            return Title(kind);
        }

        public static Color StampTint(Kind kind)
        {
            switch (kind)
            {
                case Kind.SlideShowtime: return VisualTokens.StarEvolved;
                case Kind.FeverTime: return VisualTokens.FeverGold;
                case Kind.QtePerfect: return new Color(0.78f, 0.55f, 1f, 1f);
                case Kind.QteGreat: return VisualTokens.GoldTitle;
                case Kind.QteGood: return VisualTokens.YellowValue;
                case Kind.QteBad: return VisualTokens.TextMuted;
                case Kind.DriveSelect:
                case Kind.DriveReady:
                case Kind.DriveCast: return VisualTokens.DriveOrange;
                case Kind.EnemyWarn: return VisualTokens.StarEvolved;
                case Kind.BattleStart: return VisualTokens.YellowConfirm;
                default: return VisualTokens.TapWhite;
            }
        }
    }
}
