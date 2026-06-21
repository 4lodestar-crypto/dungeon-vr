using DungeonVR.AI.Interfaces;

// V0-EXCEPTION: This struct is a stub handoff to Gameplay Systems.
// In V3+, AttackRequest will be consumed by a server-authoritative damage
// resolution system that validates spacing, line-of-sight, and armour.
// For V0 the struct is emitted by ScreamerBehavior and discarded by the stub handler.
namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Emitted by a monster when it attempts to damage the champion.
    /// Consumed by Gameplay Systems for damage resolution (V3+).
    /// </summary>
    public readonly struct AttackRequest
    {
        /// <summary>Server-side entity ID of the attacking monster.</summary>
        public readonly int SourceEntityId;

        /// <summary>Server-side entity ID of the target (champion = 0 in V0).</summary>
        public readonly int TargetEntityId;

        /// <summary>Raw damage before mitigation.</summary>
        public readonly int Damage;

        /// <summary>Server tick when the attack was initiated.</summary>
        public readonly int TickStamp;

        public AttackRequest(int sourceEntityId, int targetEntityId, int damage, int tickStamp)
        {
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Damage = damage;
            TickStamp = tickStamp;
        }

        public override string ToString()
            => $"AttackRequest: entity {SourceEntityId} → {TargetEntityId}, {Damage} dmg @ tick {TickStamp}";
    }
}
