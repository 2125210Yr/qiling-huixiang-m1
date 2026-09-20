using System;
using System.IO;
using Resonance.App;
using Resonance.Battle;
using UnityEditor;
using UnityEngine;

namespace Resonance.EditorTools
{
    /// <summary>Deterministic EditMode UI fixtures. Not gameplay, profile recovery, or normal-play evidence.</summary>
    public static class OriginalExpeditionVerification
    {
        public static void RunAndExit()
        {
            var code = 0;
            try { Run(); }
            catch (Exception ex) { Debug.LogException(ex); code = 1; }
            EditorApplication.Exit(code);
        }

        static void Run()
        {
            var directory = Environment.GetEnvironmentVariable("RESONANCE_ORIGINAL_UI_EVIDENCE");
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Set RESONANCE_ORIGINAL_UI_EVIDENCE.");
            Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("OriginalUiFixtureCamera", typeof(Camera));
            var canvasObject = new GameObject("OriginalUiFixtureCanvas", typeof(RectTransform), typeof(Canvas));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var hud = new ExpeditionHud(canvas.transform);
            try
            {
                hud.Render(Home());
                Shot(camera, target, directory, "01-home.png");
                hud.Render(Reward());
                Shot(camera, target, directory, "02-reward.png");
                hud.Render(Map());
                Shot(camera, target, directory, "03-map.png");

                var input = RunBattleFactory.CreateInput("N7", ExpeditionContent.SinglePreset, 260921,
                    new[] { "A01", "A02", "A03", "A04" }, null);
                var sim = RunBattleFactory.Create(input);
                // Explicit controlled fixture states; these are never represented as normal play.
                sim.Allies[0].Charge = 100; sim.Allies[1].Charge = 82; sim.Allies[2].Hp = 3800;
                sim.Allies[3].Charge = 100; sim.Allies[4].Charge = 54;
                var battle = new ExpeditionHud.BattleModel
                {
                    Title = "白面指挥者", Subtitle = "UI 夹具 · 以下读条与反馈为合成展示样本",
                    IntentTitle = "终幕回响 · 全体攻击", IntentDescription = "剩余 2.4 秒 · 2 个面具 · 当前倍率 2.0×", IntentProgress = 0.4f,
                    RelicIds = input.RelicIds, RelicState = "A 蓄能 2140 / 3360 · 阈值 1120\n过载共振：下次主动技能消费完整阈值",
                    Feedback = "样本：A03 多目标合计有效伤害 1680 · 溢出 0",
                    SkillNames = new[] { "定点破音", "全队屏障", "回声疗愈", "破幕突刺", "领奏信标" },
                    Notice = "界面夹具，未操作实际远征、未写个人存档。"
                };
                hud.RenderBattle(sim, battle, new ExpeditionHud.BattleActions());
                Shot(camera, target, directory, "04-battle-fixture.png");
                sim.Paused = true;
                battle.QueueSummary = "战术暂停 · 顺序 1：砚盾→全队屏障；2：逐锋→破幕突刺。\n恢复时重新校验；未就绪指令会拒绝。";
                battle.QueuedCommands = new[] { null, "1", null, "2", null };
                hud.RefreshBattle(sim, battle);
                Shot(camera, target, directory, "05-paused-fixture.png");
                hud.Render(Summary());
                Shot(camera, target, directory, "06-summary-fixture.png");
                File.WriteAllText(Path.Combine(directory, "FIXTURE-NOTICE.txt"),
                    "All six PNGs are synthetic EditMode UI fixtures at 1080x1920.\n" +
                    "No GameRoot lifecycle, profile IO, normal expedition input, or gameplay video is exercised.\n" +
                    "Battle health/charge, intent text, contribution text, queue and result facts are controlled display samples.\n" +
                    "The real content table supplies character/relic identities and the ordinary RunBattleFactory supplies unit definitions.\n");
                Debug.Log("ORIGINAL_EXPEDITION_UI_RENDER_PASS: six 1080x1920 synthetic UI fixtures; NOT normal-play evidence.");
            }
            finally
            {
                hud.Dispose();
                UnityEngine.Object.DestroyImmediate(canvasObject);
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        static ExpeditionHud.ScreenModel Home()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Home, Title = ExpeditionContent.ChapterName,
                Subtitle = "UI 夹具 · 据点与起始核心", Body = "五人同行，走过五场战斗。选择一个立即生效的核心，再沿节点组装本趟构筑。",
                FooterNote = "首次通关解锁等价起始配置；局内强化不跨趟保留。" };
            var characters = ExpeditionContent.CreateCharacters();
            var skills = new[] { "单体伤害", "全队护盾", "全队治疗", "单体伤害", "全队增益" };
            for (var i = 0; i < 5; i++) model.Party.Add(new ExpeditionHud.PartyMember { Id = characters[i].Id,
                Name = characters[i].Name, Role = CharacterPresenter.RoleLabel(characters[i].Role), Skill = skills[i] });
            foreach (var id in new[] { "A01", "B01", "C01" }) model.Options.Add(Relic(id));
            return model;
        }

        static ExpeditionHud.ScreenModel Reward()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Reward, Title = "选择本趟强化",
                Subtitle = "UI 夹具 · R1 · 已持有 A01 蓄能屏障", Body = "候选已保存。选择一件，或放弃本组并恢复少量生命。",
                FooterNote = "退出后仍是同一组选项；选择只会提交一次。" };
            foreach (var id in new[] { "A02", "A03", "C01" }) model.Options.Add(Relic(id));
            model.FooterActions.Add(new ExpeditionHud.Choice { Id = "recover", Title = "放弃并休整", Select = () => { } });
            return model;
        }

        static ExpeditionHud.ScreenModel Map()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Map, Title = "失声剧院 · 路线",
                Subtitle = "UI 夹具 · N2 选择一侧，之后汇合", FooterNote = "普通战胜利：存活队员恢复 15% 最大生命。" };
            AddNode(model, "N0", "据点", "选择初始核心", false, true);
            AddNode(model, "N1", "前厅", "普通战 → R1", false, true);
            AddNode(model, "N2-backstage", "后台", "较少耐打敌人\n奖励倾向 A / C", true, false);
            AddNode(model, "N2-audience", "观众席", "更多脆弱敌人\n奖励倾向 B / C", true, false);
            AddNode(model, "N3", "工坊", "休整或领取强化", false, false);
            AddNode(model, "N4", "排练厅", "精英战 → 放大件机会", false, false);
            AddNode(model, "N5", "返场回廊", "用成型构筑应对多目标战", false, false);
            AddNode(model, "N6", "首领门前", "全队复原；确认后冻结检查点", false, false);
            AddNode(model, "N7", "白面指挥者", "挑战；失败可原样重试", false, false);
            model.FooterActions.Add(new ExpeditionHud.Choice { Id = "backstage", Title = "前往后台", Select = () => { } });
            model.FooterActions.Add(new ExpeditionHud.Choice { Id = "audience", Title = "前往观众席", Select = () => { } });
            return model;
        }

        static void AddNode(ExpeditionHud.ScreenModel model, string id, string title, string description, bool current, bool visited)
            => model.Nodes.Add(new ExpeditionHud.MapNode { Id = id, Title = title, Description = description, Current = current, Visited = visited });

        static ExpeditionHud.ScreenModel Summary()
        {
            var model = new ExpeditionHud.ScreenModel { Kind = ExpeditionHud.ScreenKind.Result, Title = "剧院归于寂静",
                Subtitle = "UI 夹具 · 合成结算样本，不是实际通关成绩", Body = "首次通关：解锁等价的「双线追击」起始配置。下一趟可在据点切换。",
                FooterNote = "本趟临时强化已结束；发现图鉴与配置解锁保留。" };
            model.Facts.Add("样本：在排练厅选择 A04 后完成防护反击组合。");
            model.Facts.Add("样本：实际吸收 8400；遗物有效伤害 12600；溢出 1600。");
            model.Facts.Add("样本：五场战斗结束，下一趟可试多目标起始配置。");
            model.Body += "\n战术提示：在群攻读条期间先清面具，或准备全队护盾。";
            foreach (var id in new[] { "A01", "A02", "A03", "A04" }) model.Relics.Add(Relic(id));
            model.FooterActions.Add(new ExpeditionHud.Choice { Id = "home", Title = "返回据点", Select = () => { } });
            return model;
        }

        static ExpeditionHud.Choice Relic(string id)
        {
            var relic = ExpeditionContent.FindRelic(id);
            return new ExpeditionHud.Choice { Id = id, Title = relic.Name, Family = relic.Family,
                Description = relic.ShortDescription, Detail = "UI 夹具：实际倍率与上限须以当前内容版本及运行规则为准。",
                Relationship = relic.PrerequisiteIds.Length > 0 ? "需要已持有 " + string.Join("、", relic.PrerequisiteIds) : "核心 · 立即可用",
                Select = () => { } };
        }

        static void Shot(Camera camera, RenderTexture target, string directory, string name)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(directory, name), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
