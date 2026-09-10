using UnityEngine;

namespace Resonance.App
{
    public static class PhoneDisplay
    {
        // 竖屏 9:16。窗口夹在 1080×1920。画布基准仍是 1080×1920。
        const float Aspect = 9f / 16f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void BeforeSplash()
        {
            try { LockPortrait(); } catch { }
            if (Application.isEditor) return;
            if (Application.isMobilePlatform)
            {
                Screen.fullScreen = true;
                return;
            }
            FitDesktopWindow(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterScene()
        {
            if (Application.isEditor || Application.isMobilePlatform) return;
            FitDesktopWindow(true);
        }

        static void LockPortrait()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        static void FitDesktopWindow(bool center)
        {
            try
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                var display = default(DisplayInfo);
                try { display = Screen.mainWindowDisplayInfo; } catch { }
                var work = display.workArea;
                var sysW = 1080;
                var sysH = 1920;
                try
                {
                    if (Display.main != null)
                    {
                        sysW = Mathf.Max(800, Display.main.systemWidth);
                        sysH = Mathf.Max(600, Display.main.systemHeight);
                    }
                }
                catch { }
                var workW = work.width > 200 ? work.width : sysW;
                var workH = work.height > 200 ? work.height : sysH;
                const int margin = 48;
                var maxW = Mathf.Max(360, workW - margin);
                var maxH = Mathf.Max(640, workH - margin);
                var height = maxH;
                var width = Mathf.RoundToInt(height * Aspect);
                if (width > maxW)
                {
                    width = maxW;
                    height = Mathf.RoundToInt(width / Aspect);
                }
                width = Mathf.Clamp(width & ~1, 360, 1080);
                height = Mathf.RoundToInt(width / Aspect) & ~1;
                if (height > 1920)
                {
                    height = 1920;
                    width = Mathf.Clamp(Mathf.RoundToInt(height * Aspect) & ~1, 360, 1080);
                }
                if (height < 640)
                {
                    height = 640;
                    width = Mathf.Clamp(Mathf.RoundToInt(height * Aspect) & ~1, 360, 1080);
                    height = Mathf.Clamp(Mathf.RoundToInt(width / Aspect) & ~1, 640, 1920);
                }
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                if (!center || work.width <= 0 || work.height <= 0) return;
                var x = work.x + Mathf.Max(0, (workW - width) / 2);
                var y = work.y + Mathf.Max(0, (workH - height) / 2);
                Screen.MoveMainWindowTo(display, new Vector2Int(x, y));
            }
            catch
            {
                try { Screen.SetResolution(1080, 1920, FullScreenMode.Windowed); } catch { }
            }
        }
    }
}
