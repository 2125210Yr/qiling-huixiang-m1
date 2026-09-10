using System.Collections.Generic;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// 厚涂静帧。编队/图录/战斗头像走 portrait；首页/详情走 presenter。
    /// 像素只留给敌人缺图。
    /// </summary>
    public static class CharacterArt
    {
        static readonly Dictionary<string, Sprite> FaceCache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Sprite> BodyCache = new Dictionary<string, Sprite>();

        public static Sprite Face(string id)
        {
            return SpriteOf(id, FaceCache, "portrait", "presenter");
        }

        public static Sprite Body(string id)
        {
            return SpriteOf(id, BodyCache, "presenter", "portrait");
        }

        static Sprite SpriteOf(string id, Dictionary<string, Sprite> cache, params string[] files)
        {
            if (string.IsNullOrEmpty(id) || cache == null || files == null) return null;
            Sprite s;
            if (cache.TryGetValue(id, out s) && s != null) return s;
            for (int i = 0; i < files.Length; i++)
            {
                var tex = Load(id, files[i]);
                if (tex == null || tex.width < 400 || tex.height < 400) continue;
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.62f), 100f);
                cache[id] = s;
                return s;
            }
            return null;
        }

        public static bool TryLayers(string id, out Texture2D body, out Texture2D blink, out Texture2D hairFront, out Texture2D hairBack, out Texture2D sword)
        {
            body = Load(id, "Layers/layer_body");
            blink = Load(id, "Layers/layer_body_blink");
            hairFront = Load(id, "Layers/layer_hair_front");
            hairBack = Load(id, "Layers/layer_hair_back");
            sword = Load(id, "Layers/layer_sword");
            return body != null && (hairFront != null || hairBack != null);
        }

        public static bool TryV3Layers(string id, out Texture2D body, out Texture2D blink, out Texture2D hairFront, out Texture2D hairBack, out Texture2D sword)
        {
            body = Load(id, "V3Layers/layer_body");
            blink = Load(id, "V3Layers/layer_body_blink");
            hairFront = Load(id, "V3Layers/layer_hair_front");
            hairBack = Load(id, "V3Layers/layer_hair_back");
            sword = Load(id, "V3Layers/layer_sword");
            return body != null && (hairFront != null || hairBack != null);
        }

        public static bool TryPreviewLayers(string id, out Texture2D body, out Texture2D blink,
            out Texture2D hairFront, out Texture2D hairBack, out Texture2D hairTip)
        {
            body = Load(id, "PreviewLayers/layer_body");
            blink = Load(id, "PreviewLayers/layer_body_blink");
            hairFront = Load(id, "PreviewLayers/layer_hair_front");
            hairBack = Load(id, "PreviewLayers/layer_hair_back");
            hairTip = Load(id, "PreviewLayers/layer_hair_tip");
            return body != null && (hairFront != null || hairBack != null || hairTip != null);
        }

        public static bool TryPreviewLayers(string id, out Texture2D body, out Texture2D blink,
            out Texture2D hairFront, out Texture2D hairBack, out Texture2D hairTip, out Texture2D sword)
        {
            var ok = TryPreviewLayers(id, out body, out blink, out hairFront, out hairBack, out hairTip);
            sword = Load(id, "PreviewLayers/layer_sword");
            return ok;
        }

        public static bool TryPuppet(string id, out PuppetDto dto)
        {
            dto = null;
            if (string.IsNullOrEmpty(id)) return false;
            var ta = Resources.Load<TextAsset>("Art/Characters/" + id + "/Puppet/puppet");
            if (ta == null || string.IsNullOrEmpty(ta.text)) return false;
            dto = JsonUtility.FromJson<PuppetDto>(ta.text);
            return dto != null && dto.slots != null && dto.slots.Length > 0;
        }

        public static Texture2D LoadPuppetTex(string id, string tex)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(tex)) return null;
            return Load(id, tex);
        }

        public static bool TryCubismPlates(string id, out CubismPlates p)
        {
            p = new CubismPlates();
            p.body = Load(id, "CubismLayers/body");
            p.bust = Load(id, "CubismLayers/bust");
            p.hairBack = Load(id, "CubismLayers/hair_back");
            p.hairFront = Load(id, "CubismLayers/hair_front");
            p.hairSide = Load(id, "CubismLayers/hair_side");
            p.sword = Load(id, "CubismLayers/sword");
            return p.body != null;
        }

        public static bool TryLiveLayers(string id, out LiveParts p)
        {
            p = new LiveParts();
            p.torso = Load(id, "LiveLayers/layer_torso");
            p.head = Load(id, "LiveLayers/layer_head");
            p.headBlink = Load(id, "LiveLayers/layer_head_blink");
            p.hairBack = Load(id, "LiveLayers/layer_hair_back");
            p.hairFront = Load(id, "LiveLayers/layer_hair_front");
            p.armL = Load(id, "LiveLayers/layer_arm_l");
            p.armR = Load(id, "LiveLayers/layer_arm_r");
            p.handL = Load(id, "LiveLayers/layer_hand_l");
            p.handR = Load(id, "LiveLayers/layer_hand_r");
            p.legL = Load(id, "LiveLayers/layer_leg_l");
            p.legR = Load(id, "LiveLayers/layer_leg_r");
            p.footL = Load(id, "LiveLayers/layer_foot_l");
            p.footR = Load(id, "LiveLayers/layer_foot_r");
            p.sword = Load(id, "LiveLayers/layer_sword");
            return p.torso != null || p.head != null;
        }

        public static bool TrySpineLayers(string id, out Texture2D body, out Texture2D blink,
            out Texture2D hairFront, out Texture2D hairBack, out Texture2D hairTip, out Texture2D sword)
        {
            body = Load(id, "SpineLayers/layer_body");
            blink = Load(id, "SpineLayers/layer_body_blink");
            hairFront = Load(id, "SpineLayers/layer_hair_front");
            hairBack = Load(id, "SpineLayers/layer_hair_back");
            hairTip = Load(id, "SpineLayers/layer_hair_tip");
            sword = Load(id, "SpineLayers/layer_sword");
            return body != null && (hairFront != null || hairBack != null || hairTip != null || sword != null);
        }

        public static Texture2D LoadStage(string id)
        {
            return Load(id, "PreviewLayers/layer_stage");
        }

        public static Texture2D LoadPreview(string id)
        {
            return Load(id, "preview_new");
        }

        public static Texture2D LoadV3(string id)
        {
            return Load(id, "presenter_v3");
        }

        public static Texture2D LoadV3Blink(string id)
        {
            return Load(id, "presenter_v3_blink");
        }

        public static bool TryPresenter(string id, out Texture2D idle, out Texture2D blink)
        {
            idle = Load(id, "presenter");
            blink = Load(id, "presenter_blink");
            return idle != null;
        }

        public static bool TryPortrait(string id, out Texture2D idle, out Texture2D blink)
        {
            idle = Load(id, "portrait");
            blink = Load(id, "portrait_blink");
            return idle != null;
        }

        static Texture2D Load(string id, string file)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var tex = Resources.Load<Texture2D>("Art/Characters/" + id + "/" + file);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 8;
            return tex;
        }
    }
}
