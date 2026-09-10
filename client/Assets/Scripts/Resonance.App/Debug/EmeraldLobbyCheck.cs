using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EmeraldInochi;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Resonance.App
{
    // Opt-in integration check against the real game UI; never touches party or battle data.
    public sealed class EmeraldLobbyCheck : MonoBehaviour
    {
        const string PreferenceKey = "Resonance.Lobby.EmeraldBunny.Enabled.v1";
        string folder;
        float started, nextAt;
        int phase, nextFrame, cycle, oldPreference;
        bool hadPreference, preferenceCaptured, finished;
        readonly List<string> entries = new List<string>();

        [Serializable]
        sealed class Result
        {
            public string status;
            public string message;
            public float elapsedSeconds;
            public string[] checks;
            public string screenshot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeStart()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], "-emerald-lobby-check", StringComparison.Ordinal)) continue;
                if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
                {
                    Debug.LogError("EMERALD_LOBBY_CHECK FAIL: missing output folder");
                    Application.Quit(1);
                    return;
                }
                if (FindFirstObjectByType<EmeraldLobbyCheck>() != null) return;
                var go = new GameObject("EmeraldLobbyCheck");
                DontDestroyOnLoad(go);
                var check = go.AddComponent<EmeraldLobbyCheck>();
                check.folder = Path.GetFullPath(args[i + 1]);
                check.started = Time.realtimeSinceStartup;
                return;
            }
        }

        void Update()
        {
            if (finished) return;
            if (Time.realtimeSinceStartup - started > 20f)
            {
                Finish(false, "20 second timeout at phase " + phase);
                return;
            }
            if (Time.frameCount < nextFrame || Time.realtimeSinceStartup < nextAt) return;
            try
            {
                var game = GameRoot.Live;
                if (game == null) return;
                switch (phase)
                {
                    case 0:
                        if (game.CurrentScreen != "Home") return;
                        Directory.CreateDirectory(folder);
                        hadPreference = PlayerPrefs.HasKey(PreferenceKey);
                        oldPreference = PlayerPrefs.GetInt(PreferenceKey, 1);
                        preferenceCaptured = true;
                        PlayerPrefs.SetInt(PreferenceKey, 1);
                        PlayerPrefs.Save();
                        game.Go("Home");
                        Note("Real GameRoot Home ready; enabled cosmetic for check");
                        Advance(1);
                        break;
                    case 1:
                        RequireStandee(1, "initial Home");
                        Toggle().onClick.Invoke();
                        Advance(2);
                        break;
                    case 2:
                        RequireStandee(0, "actual toggle to leader");
                        Require(PlayerPrefs.GetInt(PreferenceKey, -1) == 0, "toggle persisted disabled preference");
                        Toggle().onClick.Invoke();
                        Advance(3);
                        break;
                    case 3:
                        RequireStandee(1, "actual toggle back to bunny");
                        Require(PlayerPrefs.GetInt(PreferenceKey, -1) == 1, "toggle persisted enabled preference");
                        Advance(4);
                        break;
                    case 4:
                        game.Go("Team");
                        Advance(5);
                        break;
                    case 5:
                        Require(game.CurrentScreen == "Team", "safe Team navigation " + (cycle + 1));
                        RequireStandee(0, "Team contains no cosmetic");
                        game.Go("Home");
                        Advance(6);
                        break;
                    case 6:
                        RequireStandee(1, "Home reentry " + (++cycle));
                        Advance(cycle < 3 ? 4 : 7);
                        break;
                    case 7:
                        var standee = FindFirstObjectByType<InochiStandee>();
                        Require(EventSystem.current != null, "real EventSystem available");
                        var image = standee.GetComponent<RawImage>();
                        Require(image != null && image.texture != null && image.raycastTarget,
                            "native surface is visible and receives pointer events");
                        standee.OnPointerClick(new PointerEventData(EventSystem.current)
                        {
                            button = PointerEventData.InputButton.Left,
                            position = RectTransformUtility.WorldToScreenPoint(null, standee.transform.position)
                        });
                        Note("Actual InochiStandee.OnPointerClick invoked with PointerEventData; visual reaction recorded in screenshot");
                        Advance(8);
                        nextAt = Time.realtimeSinceStartup + 0.5f;
                        break;
                    case 8:
                        RequireStandee(1, "after actual pointer click");
                        phase = 9;
                        StartCoroutine(Capture());
                        break;
                }
            }
            catch (Exception ex) { Finish(false, ex.ToString()); }
        }

        void Advance(int next)
        {
            phase = next;
            nextFrame = Time.frameCount + 3; // Allow deferred destruction and render updates.
        }

        Button Toggle()
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == "看板切换") return button;
            throw new InvalidOperationException("Actual 看板切换 button missing");
        }

        void RequireStandee(int expected, string label)
        {
            var count = FindObjectsByType<InochiStandee>(FindObjectsSortMode.None).Length;
            Require(count == expected, label + ": expected " + expected + ", actual " + count);
        }

        void Require(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
            Note("PASS " + message);
        }

        void Note(string message)
        {
            entries.Add(message);
            Debug.Log("EMERALD_LOBBY_CHECK " + message);
        }

        IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = null;
            try
            {
                shot = EmeraldInochi.InochiStandee.CaptureLobby(FindFirstObjectByType<Canvas>());
                File.WriteAllBytes(Path.Combine(folder, "lobby-pointer-reaction.png"), shot.EncodeToPNG());
                Note("Captured actual game lobby after pointer handler");
                Finish(true, "Toggle, preference, three Team/Home cycles and pointer handler completed");
            }
            catch (Exception ex) { Finish(false, ex.ToString()); }
            finally { if (shot != null) Destroy(shot); }
        }

        void RestorePreference()
        {
            if (!preferenceCaptured) return;
            if (hadPreference) PlayerPrefs.SetInt(PreferenceKey, oldPreference);
            else PlayerPrefs.DeleteKey(PreferenceKey);
            PlayerPrefs.Save();
            preferenceCaptured = false;
        }

        void Finish(bool passed, string message)
        {
            if (finished) return;
            finished = true;
            try
            {
                RestorePreference();
                Note("Original cosmetic preference restored");
                var result = new Result
                {
                    status = passed ? "PASS" : "FAIL", message = message,
                    elapsedSeconds = Time.realtimeSinceStartup - started,
                    checks = entries.ToArray(),
                    screenshot = passed ? "lobby-pointer-reaction.png" : ""
                };
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "result.json"), JsonUtility.ToJson(result, true));
                File.WriteAllText(Path.Combine(folder, "check.log"), result.status + " " + message + "\n" + string.Join("\n", entries));
                Debug.Log("EMERALD_LOBBY_CHECK " + result.status + " " + message);
            }
            catch (Exception ex)
            {
                passed = false;
                Debug.LogError("EMERALD_LOBBY_CHECK FAIL writing result: " + ex);
            }
            Application.Quit(passed ? 0 : 1);
        }

        void OnDestroy() { RestorePreference(); }
    }
}
