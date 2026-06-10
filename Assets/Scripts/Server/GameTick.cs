using System.Collections.Generic;
using DungeonVR.Gameplay;
using DungeonVR.Gameplay.Components;
using DungeonVR.Gameplay.Logic;

using DungeonVR.Shared;
using DungeonVR.Shared.Requests;
using DungeonVR.Shared.Results;
using UnityEngine;

namespace DungeonVR.Server
{
    /// <summary>
    /// Server-authoritative tick loop that processes queued MovementRequests at 20 Hz.
    /// Each FixedUpdate tick: collect requests -> validate -> apply valid ones -> emit state.
    /// V0-EXCEPTION: refactor through proper server layer in V1 (remote network layer).
    /// </summary>
    public class GameTick : MonoBehaviour
    {
        [Header("Grid Reference")]
        [SerializeField] private GridData _gridData;

        [Header("Champion Start")]
        [SerializeField] private Vector2Int _startPosition = new Vector2Int(2, 2);
        [SerializeField] private FacingDirection _startFacing = FacingDirection.North;

        [Header("Debug")]
        [SerializeField] private bool _logTicks = true;

        /// <summary>The authoritative champion state.</summary>
        public ChampionState Champion { get; private set; }

        /// <summary>The current game state (champion + RNG).</summary>
        public GameState State { get; private set; }

        /// <summary>Current tick number, incremented each FixedUpdate.</summary>
        public int CurrentTickNumber { get; private set; } = 0;

        /// <summary>Pending requests collected from input between ticks.</summary>
        private readonly List<MovementRequest> _pendingRequests = new List<MovementRequest>(8);

        /// <summary>Registered tickable systems.</summary>
        private readonly List<ITickableSystem> _systems = new List<ITickableSystem>(4);

        /// <summary>Server-layer facade for routing requests to handlers.</summary>
        private readonly GameServer _gameServer = new GameServer();

        /// <summary>Results from the most recent tick's processing.</summary>
        public IReadOnlyList<MovementResult> LastTickResults { get; private set; }
        private readonly List<MovementResult> _lastResults = new List<MovementResult>(8);

        private void Awake()
        {
            if (_gridData == null)
                _gridData = FindObjectOfType<GridData>(); // V0-EXCEPTION: DI in V1

            Champion = new ChampionState(_startPosition, _startFacing);
            State = new GameState(Champion, seed: 42);
            LastTickResults = _lastResults.AsReadOnly();

            Debug.Log($"[GameTick] Initialised: Champion at ({_startPosition.x},{_startPosition.y}) facing {_startFacing}. Grid: {_gridData.Width}x{_gridData.Height}");
        }

        /// <summary>
        /// Override the start position after Awake (called from DungeonGridSpawner.Start()).
        /// Resolves the Awake-ordering race between GameTick and DungeonGridSpawner.
        /// </summary>
        public void SetStartPosition(Vector2Int position)
        {
            if (Champion != null)
            {
                Champion.GridPosition = position;
                _startPosition = position; // keep serialised field in sync
                Debug.Log($"[GameTick] Start position overridden to ({position.x},{position.y})");
            }
            else
            {
                Debug.LogWarning("[GameTick] SetStartPosition called but Champion is null — position not applied.");
            }
        }

        /// <summary>
        /// Register a system to receive tick callbacks.
        /// </summary>
        public void RegisterSystem(ITickableSystem system)
        {
            if (!_systems.Contains(system))
                _systems.Add(system);
        }

        /// <summary>
        /// Enqueue a movement request. Called by PlayerInput or test code.
        /// </summary>
        public void EnqueueRequest(MovementRequest request)
        {
            _pendingRequests.Add(request);
        }

        /// <summary>
        /// Clear all pending requests. Called between test runs.
        /// </summary>
        public void ClearRequests()
        {
            _pendingRequests.Clear();
        }

        private void FixedUpdate()
        {
            CurrentTickNumber++;
            ProcessTick();
        }

        /// <summary>
        /// Process one tick: validate all pending requests, apply valid ones, emit debug log.
        /// Public so EditMode tests can call it directly.
        /// </summary>
        public void ProcessTick()
        {
            // Guard: GridData or its Walls array may not be ready yet
            if (_gridData == null || _gridData.Walls == null)
            {
                Debug.LogWarning($"[GameTick] Tick {CurrentTickNumber}: Skipped — GridData or Walls not initialised yet.");
                return;
            }

            _lastResults.Clear();

            if (_pendingRequests.Count == 0)
            {
                NotifySystems();
                if (_logTicks)
                    Debug.Log($"[GameTick] Tick {CurrentTickNumber}: No requests. Champion at ({Champion.GridPosition.x},{Champion.GridPosition.y}) facing {Champion.FacingDirection}");
                return;
            }

            bool[,] walls = _gridData.Walls;
            foreach (MovementRequest request in _pendingRequests)
            {
                MovementResult result = _gameServer.ProcessRequest(request, Champion, walls);

                // Adopt the returned state (cloned + mutated on success, unchanged on failure)
                if (result.Success)
                {
                    Champion = result.NewState;
                    State.Champion = result.NewState;
                }

                _lastResults.Add(result);

                if (_logTicks)
                {
                    if (result.Success)
                    {
                        Debug.Log($"[GameTick] Tick {CurrentTickNumber}: Move applied -> Champion at ({result.NewState.GridPosition.x},{result.NewState.GridPosition.y}) facing {result.NewState.FacingDirection}");
                    }
                    else
                    {
                        Debug.Log($"[GameTick] Tick {CurrentTickNumber}: Move blocked -> {result.BlockReason}");
                    }
                }
            }

            _pendingRequests.Clear();
            NotifySystems();
        }

        private void NotifySystems()
        {
            foreach (ITickableSystem system in _systems)
            {
                system.OnTick(CurrentTickNumber, State);
            }
        }

        private void OnValidate()
        {
            if (_gridData == null)
                _gridData = FindObjectOfType<GridData>(); // V0-EXCEPTION: DI in V1
        }
    }
}
