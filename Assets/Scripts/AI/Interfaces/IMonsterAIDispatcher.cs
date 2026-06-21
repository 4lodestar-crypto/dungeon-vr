namespace DungeonVR.AI.Interfaces
{
    /// <summary>
    /// Dispatches AI ticks to all active monsters each server tick.
    /// Enforces the global AI tick budget and prioritises monsters by threat proximity.
    /// </summary>
    public interface IMonsterAIDispatcher
    {
        /// <summary>
        /// Tick all active monsters for one server frame.
        /// </summary>
        /// <param name="gameState">Current game state (cast to DungeonVR.Shared.GameState).</param>
        /// <param name="currentTick">Monotonically incrementing server tick.</param>
        /// <param name="totalBudgetMs">Maximum total time in milliseconds for ALL monsters combined.</param>
        void TickAllMonsters(object gameState, int currentTick, float totalBudgetMs);
    }
}
