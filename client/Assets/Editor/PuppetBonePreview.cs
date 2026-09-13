using Resonance.App;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Resonance.EditorTools
{
    public static class PuppetBonePreview
    {
        const string HostName = "C001_PuppetPreview";
        static bool _didAuto;

        [InitializeOnLoadMethod]
        static void WatchBoneDrag()
        {
            SceneView.duringSceneGui -= OnScene;
            SceneView.duringSceneGui += OnScene;
            EditorApplication.delayCall += AutoOpenOnce;
        }

        static void AutoOpenOnce()
        {
            if (_didAuto) return;
            _didAuto = true;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (GameObject.Find(HostName) != null) return;
            Open(false);
        }

        static void OnScene(SceneView view)
        {
            var rig = Object.FindFirstObjectByType<PuppetRig>();
            if (rig == null || rig.WorldRoot == null) return;
            var t = Selection.activeTransform;
            if (t == null || !t.IsChildOf(rig.WorldRoot)) return;
            rig.RenderNow();
        }


        [MenuItem("Resonance/打开 C001 骨骼预览")]
        public static void OpenFromMenu()
        {
            Open(true);
        }

        public static void Open(bool dialog)
        {
            var host = GameObject.Find(HostName);
            if (host != null) Object.DestroyImmediate(host);

            var canvasGo = GameObject.Find("C001_PuppetCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("C001_PuppetCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            }

            host = new GameObject(HostName, typeof(RectTransform));
            host.transform.SetParent(canvasGo.transform, false);
            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.08f);
            rt.anchorMax = new Vector2(0.85f, 0.92f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            if (!PuppetRig.TryAttach(host.transform, "C001"))
            {
                if (dialog)
                    EditorUtility.DisplayDialog("C001 预览", "绑定失败：找不到 puppet 资源。", "好");
                return;
            }

            var rig = host.GetComponent<PuppetRig>();
            rig.freezePose = true;
            rig.RevealBones();
            rig.RenderNow();

            var world = rig.WorldRoot;
            if (world != null)
            {
                Selection.activeTransform = world;
                EditorGUIUtility.PingObject(world.gameObject);
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.LookAt(world.position + new Vector3(0f, 5f, 0f), Quaternion.identity, 12f);
                    SceneView.lastActiveSceneView.Repaint();
                }
            }

            var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null)
                EditorWindow.GetWindow(gameView);
            PuppetBonePad.ShowPad();

            if (dialog)
            {
                EditorUtility.DisplayDialog(
                    "C001 骨骼预览",
                    "看 Game 窗口里的角色。左边 Hierarchy 点开 C001_PuppetCanvas → C001_PuppetPreview，场景里是 Puppet_C001。\n\n" +
                    "点 bone_chest / bone_head / bone_thighR，按 E 旋转。\n" +
                    "Inspector 里 Freeze Pose 勾上才能拖骨骼。",
                    "好");
            }
        }
    }
}
