namespace DungeonVR.AI.Interfaces
{
    /// <summary>
    /// Damage request sent by monster AI to the Gameplay Systems layer.
    /// AI never applies damage directly — it submits a request that the authoritative
    /// handler validates and applies.
    /// </summary>
    public readonly struct DamageRequest
    {
        /// <summary>ID of the entity requesting to deal damage (the monster).</summary>
        public readonly int SourceEntityId;

        /// <summary>ID of the target entity (typically the champion/player).</summary>
        public readonly int TargetEntityId;

        /// <summary>Raw damage amount before mitigation.</summary>
        public readonly int Amount;

        /// <summary>Server tick when the request was generated.</summary>
        public readonly int TickNumber;

        public DamageRequest(int sourceEntityId, int targetEntityId, int amount, int tickNumber)
        {
            SourceEntityId = sourceEntityId;
            TargetEntityId = targetEntityId;
            Amount = amount;
            TickNumber = tickNumber;
        }
    }

    /// <summary>
    /// Result of processing a damage request.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly bool Success;
        public readonly int ActualDamageDealt;
        public readonly int TargetRemainingHP;

        private DamageResult(bool success, int actualDamage, int remainingHP)
        {
            Success = success;
            ActualDamageDealt = actualDamage;
            TargetRemainingHP = remainingHP;
        }

        public static DamageResult Valid(int damage, int remainingHP)
            => new DamageResult(true, damage, remainingHP);

        public static DamageResult Blocked()
            => new DamageResult(false, 0, -1);
    }

    /// <summary>
    /// Handler for damage requests following the server-authoritative pattern.
    /// Monsters submit requests; the handler validates and applies damage.
    /// </summary>
    public interface IDamageRequestHandler
    {
        /// <summary>
        /// Process a damage request against the current game state.
        /// </summary>
        DamageResult Handle(DamageRequest request, object gameState);
    }
}
