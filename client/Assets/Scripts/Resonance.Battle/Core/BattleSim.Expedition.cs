using System;

namespace Resonance.Battle
{
    public sealed partial class BattleSim
    {
        readonly ExpeditionBattleInput _expeditionInput;
        public bool IsOriginalExpedition => _expeditionInput != null;
        public ExpeditionBattleInput OpeningExpeditionInput => _expeditionInput?.DeepClone();

        CharacterDef ResolveCharacter(string id)
        {
            if (!IsOriginalExpedition) return Catalog.TryChar(id);
            foreach (var character in _expeditionInput.Characters)
                if (character.Id == id) return character;
            throw new InvalidOperationException("Unknown original character: " + id);
        }
    }
}
