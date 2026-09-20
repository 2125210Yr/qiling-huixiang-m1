using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Resonance.App;
using Resonance.Battle;
using UnityEditor;
using UnityEngine;

namespace Resonance.EditorTools
{
    /// <summary>Rendered UI fixtures, not a natural-play or replay acceptance run.</summary>
    public static class TutorialPresentationVerification
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static void RunAndExit()
        {
            var code = 0;
            try { Run(); }
            catch (Exception ex) { Debug.LogException(ex); code = 1; }
            EditorApplication.Exit(code);
        }

        static void Run()
        {
            var output = Environment.GetEnvironmentVariable("RESONANCE_TUTORIAL_EVIDENCE");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set RESONANCE_TUTORIAL_EVIDENCE.");
            Directory.CreateDirectory(output);
            var hostObject = new GameObject("TutorialFixtureHost");
            hostObject.SetActive(false); // Never enter GameRoot's save/boot lifecycle.
            var host = hostObject.AddComponent<GameRoot>();
            var save = new SaveBlob();
            SaveStore.EnsureStarterKit(save);
            var battle = new BattleSim(Catalog.DefaultParty, 0, 260920);
            Set(host, "_save", save);
            Set(host, "_battle", battle);
            var canvasObject = new GameObject("TutorialFixtureCanvas", typeof(RectTransform), typeof(Canvas));
            var cameraObject = new GameObject("TutorialFixtureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            var target = new RenderTexture(1080, 1920, 24);
            camera.targetTexture = target;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            var hud = new BattleHud(host, canvas.transform, new List<GameObject>());
            try
            {
                hud.Build(0);
                Set(hud, "_showtimeT", 0f);
                VfxTipPlate.Show(canvas.transform, BattleCueCopy.TeamHpTip, BattleCueCopy.TipTeamHpBody);
                hud.Tick();
                Shot(camera, target, output, "01-tip-visible.png", canvas, true);

                battle.Paused = true;
                hud.Tick();
                Shot(camera, target, output, "02-paused-tip-hidden.png", canvas, false);
                battle.Paused = false;
                hud.Tick();
                Shot(camera, target, output, "03-tip-resumed.png", canvas, true);

                VfxTipPlate.Show(canvas.transform, BattleCueCopy.TipTapSkill, BattleCueCopy.TipTapSkillBody);
                battle.Allies[1].Charge = 100f; // UI fixture setup only.
                if (!battle.Submit(BattleCommand.Slide(1, CommandSource.Fixture)).Accepted)
                    throw new InvalidOperationException("Fixture Slide rejected.");
                Call(hud, "DrainCasts");
                foreach (var showtime in canvas.GetComponentsInChildren<VfxShowtime>())
                {
                    Set(showtime, "_age", 0.35f);
                    Call(showtime, "Update");
                }
                Shot(camera, target, output, "04-slide-no-stale-tip.png", canvas, false);

                // Advance the real core hold before opening QTE in this fixture.
                for (var i = 0; i < 180 && battle.HoldLeftSec > 0f; i++) battle.Tick();
                // The runtime's deferred Destroy is not available to an EditMode rendering fixture.
                foreach (var showtime in canvas.GetComponentsInChildren<VfxShowtime>())
                    UnityEngine.Object.DestroyImmediate(showtime.gameObject);
                battle.Drive = 100f;
                if (!battle.Submit(BattleCommand.DriveBegin(1, CommandSource.Fixture)).Accepted)
                    throw new InvalidOperationException("Fixture DriveBegin rejected.");
                VfxTipPlate.Show(canvas.transform, BattleCueCopy.TeamHpTip, BattleCueCopy.TipTeamHpBody);
                Call(hud, "OpenQte");
                Shot(camera, target, output, "05-qte-tip-hidden.png", canvas, false);
                Call(hud, "FinishQte", DriveTiming.Good);
                foreach (var button in canvas.GetComponentsInChildren<VfxGoodButton>(true))
                    Call(button, "LateUpdate");
                var judge = canvas.GetComponentInChildren<VfxJudge>();
                if (judge == null) throw new InvalidOperationException("Fixture did not produce a QTE judge.");
                Set(judge, "_age", 0.30f);
                Call(judge, "Update");
                Shot(camera, target, output, "06-judge-tip-hidden.png", canvas, false);
                Debug.Log("TUTORIAL_PRESENTATION_RENDER_PASS: visible, pause, resume, Slide, QTE, judge. UI fixtures only.");
            }
            finally
            {
                Set(host, "_save", null);
                UnityEngine.Object.DestroyImmediate(canvasObject);
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        static void Shot(Camera camera, RenderTexture target, string directory, string name, Canvas canvas, bool tipVisible)
        {
            var tip = canvas.GetComponentInChildren<VfxTipPlate>(true);
            var visible = tip != null && tip.GetComponent<CanvasGroup>().alpha > 0.01f;
            if (visible != tipVisible) throw new InvalidOperationException(name + ": unexpected tip visibility " + visible);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, name), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);
        static void Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Private).Invoke(owner, args);
    }
}
