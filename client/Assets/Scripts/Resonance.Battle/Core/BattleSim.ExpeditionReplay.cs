using System.Collections.Generic;

namespace Resonance.Battle
{
    public sealed partial class BattleSim
    {
        readonly List<OriginalReplayInput> _originalReplayInputs = new List<OriginalReplayInput>();

        // Record intentions before execution. Resume owns its synchronous queued child submissions.
        void RecordOriginalCommand(BattleCommand command)
        {
            if (!IsOriginalExpedition || command.Source == CommandSource.Auto || _drainingExpeditionQueue) return;
            _originalReplayInputs.Add(new OriginalReplayInput
            {
                Tick = TickIndex, Kind = OriginalReplayInputKind.Command, Command = command
            });
        }

        void RecordOriginalQueueInput(OriginalReplayInputKind kind, int actorSlot = 0)
        {
            if (!IsOriginalExpedition) return;
            _originalReplayInputs.Add(new OriginalReplayInput { Tick = TickIndex, Kind = kind, ActorSlot = actorSlot });
        }

        internal List<OriginalReplayInput> CopyOriginalReplayInputs() => OriginalReplayJson.Copy(_originalReplayInputs);
    }
}
