using UnityEngine;
using UnityEngine.UI;

namespace Resonance.App
{
    /// <summary>
    /// Live pause overlay: paints <see cref="PauseClockReadout"/> from the host battle.
    /// Speed clicks go through <see cref="GameRoot.SetBattleSpeed"/> — no BattleSim writes,
    /// no <c>Time.timeScale</c>. Uses unscaled Update so the overlay stays live while paused.
    /// </summary>
    public sealed class PauseBoardBind : MonoBehaviour
    {
        Text _status;
        Text _policy;
        Image _chipLo;
        Image _chipMid;
        Image _chipHi;
        Text _labLo;
        Text _labMid;
        Text _labHi;
        int _shownSpeed = int.MinValue;
        bool _shownPause;
        bool _shownHas;

        public void Wire(Text status, Text policy,
            Image chipLo, Text labLo,
            Image chipMid, Text labMid,
            Image chipHi, Text labHi)
        {
            _status = status;
            _policy = policy;
            _chipLo = chipLo;
            _labLo = labLo;
            _chipMid = chipMid;
            _labMid = labMid;
            _chipHi = chipHi;
            _labHi = labHi;
            Paint(PauseClockReadout.Read(Battle()));
        }

        void Update()
        {
            var view = PauseClockReadout.Read(Battle());
            if (view.HasBattle == _shownHas && view.Paused == _shownPause && view.Speed == _shownSpeed)
                return;
            Paint(view);
        }

        public static void RequestHostSpeed(int speed)
        {
            var host = GameRoot.Live;
            if (host == null || host.Battle == null) return;
            if (!PauseClockReadout.IsHostTier(speed)) return;
            if (host.Battle.Speed == speed) return;
            host.SetBattleSpeed(speed);
        }

        static Resonance.Battle.BattleSim Battle()
        {
            var host = GameRoot.Live;
            return host != null ? host.Battle : null;
        }

        void Paint(PauseClockView view)
        {
            _shownHas = view.HasBattle;
            _shownPause = view.Paused;
            _shownSpeed = view.Speed;
            if (_status != null) _status.text = view.StatusLine;
            if (_policy != null) _policy.text = view.PolicyLine;
            PaintChip(_chipLo, _labLo, view.HasBattle && view.Speed == PauseClockReadout.SpeedLo);
            PaintChip(_chipMid, _labMid, view.HasBattle && view.Speed == PauseClockReadout.SpeedMid);
            PaintChip(_chipHi, _labHi, view.HasBattle && view.Speed == PauseClockReadout.SpeedHi);
        }

        static void PaintChip(Image chip, Text lab, bool on)
        {
            if (chip != null)
            {
                chip.color = on ? VisualTokens.YellowConfirm : VisualTokens.Hex("424242");
                var ol = chip.GetComponent<Outline>();
                if (on)
                {
                    if (ol == null) ol = chip.gameObject.AddComponent<Outline>();
                    ol.enabled = true;
                    ol.effectColor = VisualTokens.TextOnYellow;
                    ol.effectDistance = new Vector2(1f, -1f);
                }
                else if (ol != null)
                {
                    ol.enabled = false;
                }
            }
            if (lab != null)
                lab.color = on ? VisualTokens.TextOnYellow : VisualTokens.TextMuted;
        }
    }
}
