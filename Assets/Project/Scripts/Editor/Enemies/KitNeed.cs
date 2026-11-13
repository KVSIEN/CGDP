using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;
using CGD.Enemies;

namespace CGD.Editor
{
    // One piece of an enemy's kit (EnemyData › Kit: its grenade or an ability slot) that a
    // behavior's nodes cast from. Shared behaviors name no ability of their own, so a behavior
    // given to an enemy whose kit leaves that piece empty simply never fires — the enemy checks
    // and the Behavior Composer use this to say so before anyone has to play it to find out.
    internal sealed class KitNeed
    {
        public static readonly KitNeed Grenade = new("Grenade", "Kit › Grenade", data => data.Grenade != null);

        private static readonly Dictionary<AbilitySlot, KitNeed> Slots = new();

        private readonly Func<EnemyData, bool> _has;

        public string Name  { get; }
        public string Field { get; }   // where the designer fills it in

        private KitNeed(string name, string field, Func<EnemyData, bool> has)
        {
            Name  = name;
            Field = field;
            _has  = has;
        }

        public bool IsMetBy(EnemyData data) => data != null && _has(data);

        // The kit pieces these nodes cast from, in the order first used.
        public static List<KitNeed> Of(IEnumerable<BtNode> nodes)
        {
            var needs = new List<KitNeed>();
            foreach (BtNode node in nodes)
            {
                KitNeed need = For(node);
                if (need != null && !needs.Contains(need)) needs.Add(need);
            }
            return needs;
        }

        // The pieces of `needs` the enemy's kit leaves empty.
        public static List<KitNeed> Missing(EnemyData data, List<KitNeed> needs)
        {
            var missing = new List<KitNeed>();
            foreach (KitNeed need in needs)
                if (!need.IsMetBy(data)) missing.Add(need);
            return missing;
        }

        public static string Names(List<KitNeed> needs)
        {
            var names = new string[needs.Count];
            for (int i = 0; i < needs.Count; i++) names[i] = needs[i].Name;
            return string.Join(", ", names);
        }

        private static KitNeed For(BtNode node) => node switch
        {
            UseAbility use     when use.Slot   != AbilitySlot.Own => For(use.Slot),
            AbilityReady ready when ready.Slot != AbilitySlot.Own => For(ready.Slot),
            ThrowGrenade { UsesKit: true }                        => Grenade,
            SupportAlly                                           => For(AbilitySlot.Support),
            _                                                     => null,
        };

        private static KitNeed For(AbilitySlot slot)
        {
            if (Slots.TryGetValue(slot, out KitNeed need)) return need;
            need = new KitNeed(slot.ToString(), $"Kit › {slot}", data => data.KitAbility(slot) != null);
            Slots[slot] = need;
            return need;
        }
    }
}
