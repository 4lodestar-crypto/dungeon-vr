namespace DungeonVR.Shared.Enums
{
    /// <summary>
    /// Cardinal movement directions available to the champion.
    /// V0-EXCEPTION: direct enum; server-layer validation WIP in V1.
    /// </summary>
    public enum MovementDirection
    {
        Forward,
        Backward,
        RotateLeft,
        RotateRight
    }
}
