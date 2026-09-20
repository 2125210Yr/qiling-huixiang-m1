using System;
using System.Collections.Generic;

namespace Resonance.Battle
{
    /// <summary>A transient, immutable paused command. Its list position is its execution order.</summary>
    public sealed class ExpeditionQueuedCommand
    {
        public int ActorSlot { get; }
        public int RequiredEnemySlot { get; }
        public int RequiredEnemyGeneration { get; }

        internal ExpeditionQueuedCommand(int actorSlot, int requiredEnemySlot, int requiredEnemyGeneration)
        {
            ActorSlot = actorSlot;
            RequiredEnemySlot = requiredEnemySlot;
            RequiredEnemyGeneration = requiredEnemyGeneration;
        }
    }

    public sealed class ExpeditionQueueExecution
    {
        public int ActorSlot { get; }
        public int RequiredEnemySlot { get; }
        public int RequiredEnemyGeneration { get; }
        public CommandResult Result { get; }

        internal ExpeditionQueueExecution(ExpeditionQueuedCommand command, CommandResult result)
        {
            ActorSlot = command.ActorSlot;
            RequiredEnemySlot = command.RequiredEnemySlot;
            RequiredEnemyGeneration = command.RequiredEnemyGeneration;
            Result = result;
        }
    }

    public sealed partial class BattleSim
    {
        readonly List<ExpeditionQueuedCommand> _expeditionQueue = new List<ExpeditionQueuedCommand>(5);
        readonly List<ExpeditionQueueExecution> _expeditionQueueResults = new List<ExpeditionQueueExecution>(5);
        bool _drainingExpeditionQueue;
        int _expeditionRequiredEnemySlot = -1;

        // A caller can retain a view across replacement, clearing, or a completed drain without it changing.
        public IReadOnlyList<ExpeditionQueuedCommand> ExpeditionQueue => Array.AsReadOnly(_expeditionQueue.ToArray());
        public IReadOnlyList<ExpeditionQueueExecution> ExpeditionQueueResults => Array.AsReadOnly(_expeditionQueueResults.ToArray());

        public CommandReject QueueExpeditionTap(int actorSlot)
        {
            var reason = CanEditExpeditionQueue(actorSlot);
            if (reason != CommandReject.None) return reason;
            var actor = Allies[actorSlot];
            var skill = actor?.Def == null ? null : ResolveSkill(actor.Def.TapSkillId);
            int targetSlot = 0, generation = 0;
            if (BindsExpeditionEnemy(skill) && FocusEnemySlot >= 0 && FocusEnemySlot < Enemies.Count)
            {
                var target = Enemies[FocusEnemySlot];
                if (target != null && target.Alive)
                {
                    targetSlot = FocusEnemySlot;
                    generation = target.InstanceGeneration;
                }
            }
            var queued = new ExpeditionQueuedCommand(actorSlot, targetSlot, generation);
            var index = _expeditionQueue.FindIndex(c => c.ActorSlot == actorSlot);
            if (index >= 0) _expeditionQueue[index] = queued;
            else _expeditionQueue.Add(queued);
            _expeditionQueueResults.Clear();
            return CommandReject.None;
        }

        public CommandReject ClearExpeditionQueuedCommand(int actorSlot)
        {
            var reason = CanEditExpeditionQueue(actorSlot);
            if (reason != CommandReject.None) return reason;
            var index = _expeditionQueue.FindIndex(c => c.ActorSlot == actorSlot);
            if (index >= 0) _expeditionQueue.RemoveAt(index);
            return CommandReject.None;
        }

        /// <summary>Discard session-only instructions and feedback when leaving the encounter.</summary>
        public void ClearExpeditionQueue()
        {
            _expeditionQueue.Clear();
            _expeditionQueueResults.Clear();
        }

        CommandReject CanEditExpeditionQueue(int actorSlot)
        {
            if (!IsOriginalExpedition) return CommandReject.ModeDisabled;
            if (Outcome != BattleOutcome.InProgress) return CommandReject.NotInProgress;
            if (!Paused || _drainingExpeditionQueue) return CommandReject.InvalidValue;
            if (actorSlot < 0 || actorSlot >= Allies.Length) return CommandReject.SlotInvalid;
            // Readiness and actor health are deliberately checked by Submit at resume, not manufactured here.
            return CommandReject.None;
        }

        static bool BindsExpeditionEnemy(SkillDef skill)
        {
            return skill != null && skill.Type == SkillType.Tap && skill.TargetCount == 1
                && skill.Target != TargetRule.AllEnemies && TargetSemantics.Side(skill, null) == TargetSide.Foe
                && skill.HealCoef <= 0f && skill.FlatHeal <= 0 && skill.HealMaxHpFrac <= 0f
                && EffectOpcodes.IsDamageChannel(skill.Opcode)
                && (skill.AtkCoef > 0f || skill.FlatPower > 0 || skill.PercentAtk > 0f);
        }

        // Submit invokes this only AFTER recording an accepted Resume. No simulation tick occurs here.
        void DrainExpeditionQueueAfterResume()
        {
            if (!IsOriginalExpedition || Paused || _drainingExpeditionQueue) return;
            var pending = _expeditionQueue.ToArray();
            _expeditionQueue.Clear(); // Consume the batch once; failed instructions are never retried later.
            _expeditionQueueResults.Clear();
            _drainingExpeditionQueue = true;
            try
            {
                foreach (var queued in pending)
                {
                    var command = BattleCommand.Tap(queued.ActorSlot, CommandSource.Player);
                    command.RequiredEnemySlot = queued.RequiredEnemySlot;
                    command.RequiredEnemyGeneration = queued.RequiredEnemyGeneration;
                    var result = Submit(command);
                    _expeditionQueueResults.Add(new ExpeditionQueueExecution(queued, result));
                }
            }
            finally { _drainingExpeditionQueue = false; }
        }

        internal CommandReject ValidateExpeditionTarget(BattleCommand cmd)
        {
            if (cmd.RequiredEnemyGeneration == 0) return CommandReject.None;
            if (!IsOriginalExpedition) return CommandReject.ModeDisabled;
            if (cmd.RequiredEnemyGeneration < 0 || cmd.RequiredEnemySlot < 0 || cmd.RequiredEnemySlot >= Enemies.Count)
                return CommandReject.TargetInvalid;
            var target = Enemies[cmd.RequiredEnemySlot];
            return target != null && target.Alive && target.InstanceGeneration == cmd.RequiredEnemyGeneration
                ? CommandReject.None : CommandReject.TargetInvalid;
        }

        internal CommandReject RunWithExpeditionTarget(BattleCommand cmd, Func<CommandReject> execute)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            var previous = _expeditionRequiredEnemySlot;
            _expeditionRequiredEnemySlot = IsOriginalExpedition && cmd.RequiredEnemyGeneration > 0 ? cmd.RequiredEnemySlot : -1;
            try { return execute(); }
            finally { _expeditionRequiredEnemySlot = previous; }
        }
    }
}
