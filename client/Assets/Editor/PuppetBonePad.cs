using Resonance.App;
using UnityEditor;
using UnityEngine;

namespace Resonance.EditorTools
{
    public sealed class PuppetBonePad : EditorWindow
    {
        [MenuItem("Resonance/C001 骨骼控制")]
        public static void ShowPad()
        {
            var w = GetWindow<PuppetBonePad>("C001 骨骼");
            w.minSize = new Vector2(320, 360);
        }

        void OnEnable()
        {
            autoRepaintOnSceneChange = true;
        }

        void OnGUI()
        {
            var rig = Object.FindFirstObjectByType<PuppetRig>();
            if (rig == null || rig.WorldRoot == null)
            {
                EditorGUILayout.HelpBox("还没有角色。点下面按钮，然后看 Game 窗口。", MessageType.Info);
                if (GUILayout.Button("打开角色", GUILayout.Height(44)))
                    PuppetBonePreview.OpenFromMenu();
                return;
            }

            EditorGUILayout.HelpBox("看 Game 窗口里的立绘。拖下面滑条就会转。", MessageType.None);
            rig.freezePose = EditorGUILayout.ToggleLeft("冻结自动动作（要手摆就勾上）", rig.freezePose);
            EditorGUILayout.Space(8);

            BoneZ(rig, "bone_chest", "胸");
            BoneZ(rig, "bone_head", "头 / 头发");
            BoneZ(rig, "bone_armL", "左臂（持剑）");
            BoneZ(rig, "bone_wrist", "手腕 / 剑");
            BoneZ(rig, "bone_thighR", "右大腿");
            BoneZ(rig, "bone_kneeR", "右膝");
            BoneZ(rig, "bone_ankleR", "右脚");
            BoneZ(rig, "bone_ankleL", "左脚");

            EditorGUILayout.Space(12);
            if (GUILayout.Button("全部回正", GUILayout.Height(36)))
            {
                foreach (var t in rig.WorldRoot.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("bone_"))
                        t.localRotation = Quaternion.identity;
                rig.RenderNow();
            }
        }

        static void BoneZ(PuppetRig rig, string boneName, string label)
        {
            var bone = FindBone(rig.WorldRoot, boneName);
            if (bone == null)
            {
                EditorGUILayout.LabelField(label, "（没有这根骨头）");
                return;
            }
            var z = bone.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            EditorGUI.BeginChangeCheck();
            z = EditorGUILayout.Slider(label, z, -40f, 40f);
            if (EditorGUI.EndChangeCheck())
            {
                bone.localRotation = Quaternion.Euler(0f, 0f, z);
                rig.RenderNow();
            }
        }

        static Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var f = FindBone(c, name);
                if (f != null) return f;
            }
            return null;
        }
    }
}
