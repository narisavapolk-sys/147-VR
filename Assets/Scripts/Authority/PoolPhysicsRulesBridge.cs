using System;
using System.Collections.Generic;
using UnityEngine;
using PoolPhysics.Sim;
using PoolPhysics.Unity;
using SimVector3 = System.Numerics.Vector3;

namespace VR147.Authority
{
    /// <summary>
    /// Phase 3 — RULES &amp; SCORING LINK.
    ///
    /// Once a bay's <see cref="PoolPhysicsEventRunner"/> owns physics, PhysX stops producing the
    /// signals the rules layer used to rely on:
    ///   * balls are kinematic, so <c>CueBallCollision.OnCollisionEnter</c> never fires
    ///     (first-ball-hit for fouls is gone),
    ///   * pocket triggers cannot drag a kinematic ball down, so <c>BallPotted</c> is gone,
    ///   * rest detection from <c>Rigidbody.velocity</c> reads zero for every ball.
    ///
    /// The event simulator already knows all of that and publishes it as gameplay events, so this
    /// bridge re-binds the existing rules objects to the ACTIVE bay and feeds them from the
    /// simulator instead:
    ///   BallBall events  -> SnookerShotTracker.OnCueBallHitBall (wrong-ball / miss fouls)
    ///   Pocket events    -> SnookerBallTracker.ReportPot (pot list + turn continuation)
    ///   ShotFinished     -> M5ShotLifecycle settled boundary (already wired, re-pointed here)
    ///   post-settle      -> reconcile re-spots (ScoreManager teleports a transform the sim owns)
    ///                       and cue-ball scratch re-spot
    ///
    /// Discipline: bays whose rack is a snooker set (12+ reds) keep the existing snooker scoring
    /// transaction. Bays with numbered pool balls do NOT — SnookerScoreManager reads points == 0
    /// as "cue ball potted", which would turn every pool pot into a scratch foul, so those bays
    /// get pot-count turn logic from this bridge and the snooker transaction is switched off.
    ///
    /// SOLO-PRACTICE UNDO (owner rule: UNDO is a PRACTICE tool, never a match feature): when
    /// SoloPractice is true the bridge snapshots the pre-shot table (a canonical PoolSaveState
    /// through PoolUndoStack) at every ShotStarted, and RequestUndo — wired to
    /// BhpCueGripController.OnUndoRequested (LEFT X while walking) — restores it. Hard gates:
    /// practice mode only, physics owned AND at rest, no shot in flight. In a normal match
    /// RequestUndo always returns false.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    public sealed class PoolPhysicsRulesBridge : MonoBehaviour
    {
        public enum Discipline { None, Snooker, Pool }

        [Header("Diagnostics")]
        [SerializeField] bool logEvents = true;

        [Header("Game mode / Solo practice UNDO")]
        [Tooltip("Match is the safe default. Select SoloPractice only for single-player practice.")]
        [SerializeField] PoolGameMode gameMode = PoolGameMode.Match;
        [Tooltip("How many pre-shot snapshots the UNDO ring keeps.")]
        [SerializeField, Range(1, 16)] int undoDepth = 8;

        PoolPhysicsAuthorityDirector _director;
        SnookerBallTracker _ballTracker;
        SnookerShotTracker _shotTracker;
        SnookerTurnManager _turnManager;
        M5ShotLifecycle _lifecycle;
        BhpCueGripController _grip;

        PoolPhysicsEventRunner _bound;
        readonly List<SnookerBallTracker.PocketInfo> _pockets = new List<SnookerBallTracker.PocketInfo>();
        readonly List<Transform> _ballList = new List<Transform>();

        Discipline _discipline = Discipline.None;
        bool _shotOpen;
        int _objectPots;
        bool _cuePotted;
        bool _settlePending;
        int _reportedFirstHitIndex = -1;

        // ---- solo-practice UNDO state ----
        PoolUndoStack _undo;
        int[] _undoNumbers;                       // sim index → unique save number (0 = cue)
        readonly Dictionary<int, int> _undoIndexByNumber = new Dictionary<int, int>();
        int _undoShotCounter;                     // pre-shot snapshot numbering (restored on undo)
        bool _undoSnapshotCapturedForShot;        // only advance numbering when a true pre-shot snapshot exists
        bool _shotStartBound;
        bool _undoInputBound;
        bool _undoUsable;                         // number map validated for this bay
        bool _undoDisabledWarned;                 // Push rejected by the codec once → warn once

        public Discipline CurrentDiscipline => _discipline;
        public string BoundBay => _bound != null ? _bound.BayName : "<none>";
        public int PotsLastShot => _objectPots;

        public PoolGameMode CurrentGameMode => gameMode;
        public bool SoloPractice => gameMode == PoolGameMode.SoloPractice;
        public bool UndoInputBound => _undoInputBound;
        public bool ShotStartBound => _bound != null && _shotStartBound;
        public bool UndoMapValid => _undoUsable;
        public bool ShotLifecycleBound => _lifecycle != null;

        /// <summary>True only when the central UNDO policy allows a restore right now.</summary>
        public bool UndoAvailable => PoolUndoPolicy.CanUndo(
            gameMode,
            _undoUsable,
            _undo != null && _undo.Count > 0,
            _bound != null && _bound.OwnsPhysics,
            _bound != null && _bound.AtRest,
            _shotOpen);

        /// <summary>
        /// Change mode only at a settled boundary. Every transition clears the undo ring so a
        /// snapshot from an earlier practice session can never leak into a later one.
        /// </summary>
        public bool TrySetGameMode(PoolGameMode nextMode)
        {
            if (!Enum.IsDefined(typeof(PoolGameMode), nextMode))
                return false;
            if (gameMode == nextMode)
                return true;

            bool atRest = _bound != null && _bound.OwnsPhysics
                ? _bound.AtRest
                : _lifecycle != null
                  ? _lifecycle.CurrentState == M5ShotLifecycle.State.Idle
                    || _lifecycle.CurrentState == M5ShotLifecycle.State.Settled
                  : _bound == null;

            if (!PoolUndoPolicy.CanChangeGameMode(_shotOpen, atRest))
            {
                Debug.LogWarning($"[RulesBridge] game mode change to {nextMode} rejected: table is not at a settled boundary.", this);
                return false;
            }

            gameMode = nextMode;
            _undo?.Clear();
            _undoShotCounter = 0;
            _undoSnapshotCapturedForShot = false;
            Debug.Log($"[RulesBridge] game mode → {gameMode}; undo history cleared.", this);
            return true;
        }

        void Start()
        {
            _director = PoolPhysicsAuthorityDirector.Instance != null
                ? PoolPhysicsAuthorityDirector.Instance
                : FindFirstObjectByType<PoolPhysicsAuthorityDirector>();
            _ballTracker = FindFirstObjectByType<SnookerBallTracker>();
            _shotTracker = FindFirstObjectByType<SnookerShotTracker>();
            _turnManager = FindFirstObjectByType<SnookerTurnManager>();
            _lifecycle = FindFirstObjectByType<M5ShotLifecycle>();

            if (_ballTracker == null || _shotTracker == null || _turnManager == null)
            {
                Debug.LogWarning($"[RulesBridge] Missing rules objects (ballTracker={_ballTracker != null}, " +
                                 $"shotTracker={_shotTracker != null}, turnManager={_turnManager != null}) — " +
                                 "sim events will still drive physics, but not scoring.");
            }

            if (_director != null)
                _director.ActiveChanged += OnActiveChanged;
            if (_lifecycle != null)
                _lifecycle.ShotStarted += OnLifecycleShotStarted;

            // Solo-practice UNDO input: the controller publishes the raw request; THIS bridge is
            // the single authority that decides whether practice mode allows it.
            _grip = FindFirstObjectByType<BhpCueGripController>();
            if (_grip != null)
            {
                _grip.OnUndoRequested += HandleUndoRequested;
                _undoInputBound = true;
            }

            if (_director != null && _director.Active != null)
                Bind(_director.Active);
        }

        void OnDestroy()
        {
            if (_director != null)
                _director.ActiveChanged -= OnActiveChanged;
            if (_lifecycle != null)
                _lifecycle.ShotStarted -= OnLifecycleShotStarted;
            if (_grip != null)
                _grip.OnUndoRequested -= HandleUndoRequested;
            _undoInputBound = false;
            Unbind();
        }

        void OnActiveChanged(PoolPhysicsEventRunner runner) => Bind(runner);

        // ---- Binding a bay ----

        void Bind(PoolPhysicsEventRunner runner)
        {
            if (runner == null)
                return;
            Unbind();
            _bound = runner;

            _pockets.Clear();
            for (int i = 0; i < runner.PocketCount; i++)
                _pockets.Add(new SnookerBallTracker.PocketInfo
                {
                    name = $"{runner.BayName} Pocket {i + 1}",
                    position = runner.PocketWorldPosition(i),
                    radius = Mathf.Max(0.08f, runner.PocketWorldRadius(i))
                });

            _ballList.Clear();
            for (int i = 0; i < runner.BallCount; i++)
                _ballList.Add(runner.BallTransformAt(i));

            if (_ballTracker != null)
            {
                if (_ballTracker.BindToSimulation(_ballList, _pockets, runner.CueBallIndex))
                {
                    // Carry whatever the simulator already knows about pockets into the tracker
                    // (e.g. a bay taken over after some balls were potted in PhysX mode).
                    for (int i = 0; i < _ballList.Count; i++)
                    {
                        SnookerBallTracker.BallInfo info = _ballTracker.BallAt(i);
                        if (info != null && runner.IsBallPocketed(i))
                            info.potted = true;
                    }
                }
                else
                {
                    Debug.LogError($"[RulesBridge] {runner.BayName}: BindToSimulation rejected an empty ball list.");
                }
            }

            int reds = 0;
            for (int i = 0; i < _ballList.Count; i++)
                if (_ballList[i] != null && SnookerBallTracker.PointsForSimName(_ballList[i].name) == 1)
                    reds++;
            _discipline = _ballList.Count == 0 ? Discipline.None
                        : (reds >= 10 ? Discipline.Snooker : Discipline.Pool);

            if (_shotTracker != null)
                _shotTracker.snookerRulesEnabled = _discipline == Discipline.Snooker;
            if (_lifecycle != null)
                _lifecycle.BindRunner(runner);

            BuildUndoMap();
            _undo = new PoolUndoStack(Mathf.Clamp(undoDepth, 1, 16));
            _undoShotCounter = 0;

            runner.SimEvent += OnSimEvent;
            runner.ShotStarted += OnShotStarted;
            runner.ShotFinished += OnShotFinished;
            _shotStartBound = true;

            Debug.Log($"[RulesBridge] bound {runner.BayName}: balls={_ballList.Count} cue={runner.CueBallIndex} " +
                      $"pockets={_pockets.Count} discipline={_discipline} " +
                      $"snookerRules={(_shotTracker != null && _shotTracker.snookerRulesEnabled)} " +
                      $"undo={(_undoUsable && SoloPractice ? "on" : (_undoUsable ? "armed" : "unavailable"))}");
        }

        void Unbind()
        {
            if (_bound == null)
                return;
            _bound.SimEvent -= OnSimEvent;
            _bound.ShotStarted -= OnShotStarted;
            _bound.ShotFinished -= OnShotFinished;
            _shotStartBound = false;
            _bound = null;
        }

        // ---- Shot lifecycle ----

        void OnLifecycleShotStarted() => OnShotStarted(allowUndoSnapshot: false);

        void OnShotStarted() => OnShotStarted(allowUndoSnapshot: true);

        void OnShotStarted(bool allowUndoSnapshot)
        {
            if (_shotOpen)
            {
                // M5ShotLifecycle may announce first, before the runner has ownership.
                // The runner's pre-impulse event gets one final chance to capture safely.
                if (allowUndoSnapshot && !_undoSnapshotCapturedForShot)
                    TryCaptureUndoSnapshot();
                return;
            }

            _shotOpen = true;
            _undoSnapshotCapturedForShot = false;
            _objectPots = 0;
            _cuePotted = false;
            _reportedFirstHitIndex = -1;

            // Lifecycle and runner events both fire before motion. The lazy first-sim-event
            // fallback is already mid-shot and deliberately cannot create a false snapshot.
            if (allowUndoSnapshot)
                TryCaptureUndoSnapshot();
        }

        void TryCaptureUndoSnapshot()
        {
            // Runner canonicalizes by position before every shot. Refresh all index-based
            // consumers before snapshotting so events, trackers and undo numbers share one order.
            if (_bound == null)
                return;

            _ballList.Clear();
            for (int i = 0; i < _bound.BallCount; i++)
                _ballList.Add(_bound.BallTransformAt(i));

            if (_ballTracker != null &&
                !_ballTracker.ReorderToSimulation(_ballList, _bound.CueBallIndex))
            {
                _undoUsable = false;
                Debug.LogError($"[RulesBridge] {_bound.BayName}: tracker could not follow simulator ball permutation; UNDO disabled.");
                return;
            }

            BuildUndoMap();
            bool ownsPhysics = _bound.OwnsPhysics;
            bool atRest = _bound.AtRest;
            if (PoolUndoPolicy.CanCaptureSnapshot(gameMode, _undoUsable, ownsPhysics, atRest))
                _undoSnapshotCapturedForShot = PushUndoSnapshot();
        }

        bool _warnedLazyShotOpen;

        void OnSimEvent(EventSimulator.PoolEventSim ev)
        {
            PoolPhysicsEventRunner runner = _bound;
            if (runner == null)
                return;

            // Lazy shot open: if M5ShotLifecycle is missing or disabled, ShotStarted never fires.
            // Rather than silently dropping every pot, treat the first event as the shot boundary.
            if (!_shotOpen)
            {
                if (!_warnedLazyShotOpen)
                {
                    _warnedLazyShotOpen = true;
                    Debug.LogWarning("[RulesBridge] ShotStarted was never announced — opening the shot " +
                                     "window from the first simulator event. Check M5ShotLifecycle wiring.", this);
                }
                OnShotStarted(allowUndoSnapshot: false);
            }

            int cue = runner.CueBallIndex;

            switch (ev.Kind)
            {
                case EventSimulator.PoolEventKind.BallBall:
                    int other = ev.BallIndex == cue ? ev.OtherIndex
                              : (ev.OtherIndex == cue ? ev.BallIndex : -1);
                    if (other < 0)
                        return;                       // ball-ball contact that does not involve the cue ball
                    if (_reportedFirstHitIndex >= 0)
                        return;                       // only the cue ball's FIRST contact decides the foul
                    _reportedFirstHitIndex = other;
                    if (_shotTracker != null && _ballTracker != null)
                    {
                        SnookerBallTracker.BallInfo hit = _ballTracker.BallAt(other);
                        if (hit != null)
                        {
                            _shotTracker.OnCueBallHitBall(hit);
                            if (logEvents)
                                Debug.Log($"[RulesBridge] first contact → {hit.name} (value {hit.points}, {ev.ImpactSpeed:F2} m/s)");
                        }
                        else
                        {
                            Debug.LogWarning($"[RulesBridge] first contact index {other} has no tracker entry.");
                        }
                    }
                    break;

                case EventSimulator.PoolEventKind.Pocket:
                    // The simulator stores positions in System.Numerics; convert before handing off.
                    SimVector3 hit3 = ev.Position;
                    HandlePocket(runner, ev.BallIndex, new Vector3(hit3.X, hit3.Y, hit3.Z));
                    break;
            }
        }

        void HandlePocket(PoolPhysicsEventRunner runner, int index, Vector3 simPosition)
        {
            if (_ballTracker == null)
                return;

            bool isCue = index == runner.CueBallIndex;
            Vector3 world = runner.SimPositionToWorld(simPosition);   // already converted to Unity space by the caller
            SnookerBallTracker.PocketInfo pocket = NearestPocket(world);

            // Snooker pots feed the existing post-settle transaction. The cue ball must NOT:
            // SnookerShotTracker reads the scratch through the potted flag (a points == 0 ball is
            // treated as "cue potted" by SnookerScoreManager, which would double-count the foul).
            // Pool-numbered bays never enter the snooker transaction (points == 0 there too).
            bool raiseEvent = _discipline == Discipline.Snooker && !isCue;
            SnookerBallTracker.BallInfo potted = _ballTracker.ReportPot(index, pocket, raiseEvent);
            if (potted == null)
                return;

            _objectPots++;
            if (isCue)
                _cuePotted = true;
            if (logEvents)
                Debug.Log($"[RulesBridge] pot {(isCue ? "SCRATCH" : potted.name)} in {(pocket != null ? pocket.name : "?")} " +
                          $"[{runner.BayName}, discipline={_discipline}]");
        }

        void OnShotFinished()
        {
            if (!_shotOpen)
                return;
            _settlePending = true;   // resolved in LateUpdate, after M5/ShotTracker have run
        }

        void LateUpdate()
        {
            if (!_settlePending)
                return;
            _settlePending = false;
            _shotOpen = false;
            Reconcile();
            ApplyPoolTurnRule();
            if (_undoSnapshotCapturedForShot)
                _undoShotCounter++;     // only a shot with a valid pre-shot snapshot advances the undo number
            _undoSnapshotCapturedForShot = false;
        }

        /// <summary>Sync simulator state with what the rules layer did after the settled boundary:
        /// SnookerScoreManager re-spots by teleporting a transform, but the simulator owns those
        /// transforms, so the sim must be told the ball is back in play (and vice versa).</summary>
        void Reconcile()
        {
            PoolPhysicsEventRunner runner = _bound;
            if (runner == null || _ballTracker == null)
                return;

            int restored = 0, parked = 0;
            for (int i = 0; i < runner.BallCount; i++)
            {
                SnookerBallTracker.BallInfo info = _ballTracker.BallAt(i);
                if (info == null || info.transform == null)
                    continue;

                bool simPocketed = runner.IsBallPocketed(i);
                if (info.potted == simPocketed)
                    continue;

                if (!info.potted && simPocketed)
                {
                    // Rules re-spotted it (FindFreeSpot already moved the transform) → put it back in the sim.
                    if (runner.RestoreBall(i, info.transform.position))
                        restored++;
                }
                else
                {
                    if (runner.ParkBall(i))
                        parked++;
                }
            }

            // Scratch re-spot: nobody else does it for the cue ball because its pot event is
            // suppressed. Send it back to its rack-time home spot.
            if (_cuePotted && _ballTracker.BallAt(runner.CueBallIndex) != null)
            {
                SnookerBallTracker.BallInfo cue = _ballTracker.BallAt(runner.CueBallIndex);
                Vector3 spot = cue.hasHomePosition ? cue.homePosition : cue.transform.position;
                cue.transform.position = spot;
                if (runner.RestoreBall(runner.CueBallIndex, spot))
                {
                    _ballTracker.Unpot(cue);
                    if (logEvents)
                        Debug.Log($"[RulesBridge] cue ball re-spotted at ({spot.x:F2}, {spot.z:F2}) in {runner.BayName}");
                }
                _cuePotted = false;
            }

            if (restored > 0 || parked > 0)
                Debug.Log($"[RulesBridge] reconciled sim/rules state after settle: restored={restored} parked={parked}");
        }

        /// <summary>Pool bays bypass the snooker transaction entirely, so the bridge owns the
        /// turn rule there: pot an object ball → striker keeps the table, otherwise pass it.</summary>
        void ApplyPoolTurnRule()
        {
            if (_discipline != Discipline.Pool || _turnManager == null)
                return;

            if (_cuePotted || _objectPots == 0)
            {
                _turnManager.NextTurn();
                if (logEvents)
                    Debug.Log($"[RulesBridge] pool turn passes (objects potted={_objectPots}, scratch={_cuePotted})");
            }
            else
            {
                _turnManager.StrikerContinues();
                if (logEvents)
                    Debug.Log($"[RulesBridge] pool striker continues (potted={_objectPots})");
            }
        }

        // -----------------------------------------------------------------------
        // Solo-practice UNDO (S2 #2c) — see the class header for the gate contract.
        // -----------------------------------------------------------------------

        /// <summary>Map each sim ball to a UNIQUE save number so PoolUndoStack's canonical
        /// snapshots validate: Pool bays use the printed ball number (NineBall schema 1..9);
        /// Snooker bays use synthetic ids — reds 1..15 in index order, colours 16..21 — because
        /// fifteen reds share the same points value and the save schema demands uniqueness.
        /// These ids are UNDO bookkeeping only; rules still read PointsForSimName.</summary>
        void BuildUndoMap()
        {
            _undoIndexByNumber.Clear();
            PoolPhysicsEventRunner runner = _bound;
            if (runner == null || _ballList.Count != runner.BallCount)
            {
                _undoUsable = false;
                return;
            }

            int n = _ballList.Count;
            _undoNumbers = new int[n];
            bool ok = n > 1;
            var mapIssues = new List<string>();

            if (_discipline == Discipline.Pool)
            {
                var seen = new HashSet<int>();
                for (int i = 0; i < n; i++)
                {
                    string ballName = _ballList[i] != null ? _ballList[i].name : "<null>";
                    int num = i == runner.CueBallIndex
                        ? 0
                        : SnookerBallTracker.PointsForSimName(ballName);
                    _undoNumbers[i] = num;
                    if (i != runner.CueBallIndex && (num < 1 || num > 9 || !seen.Add(num)))
                    {
                        ok = false;
                        mapIssues.Add($"{i}:{ballName}→{num}");
                    }
                }
                if (n > 10)
                {
                    ok = false;
                    mapIssues.Add($"poolBallCount={n}");
                }
            }
            else if (_discipline == Discipline.Snooker)
            {
                int red = 0;
                var colourIds = new Dictionary<int, int> { { 2, 16 }, { 3, 17 }, { 4, 18 }, { 5, 19 }, { 6, 20 }, { 7, 21 } };
                for (int i = 0; i < n; i++)
                {
                    if (i == runner.CueBallIndex) { _undoNumbers[i] = 0; continue; }
                    string ballName = _ballList[i] != null ? _ballList[i].name : "<null>";
                    int pts = SnookerBallTracker.PointsForSimName(ballName);
                    if (pts == 1) _undoNumbers[i] = ++red;                          // reds 1..15
                    else if (colourIds.TryGetValue(pts, out int id)) _undoNumbers[i] = id;
                    else
                    {
                        _undoNumbers[i] = -1;
                        ok = false;
                        mapIssues.Add($"{i}:{ballName} (points={pts})");
                    }
                }
                if (red > 15 || n > 22)
                {
                    ok = false;
                    mapIssues.Add($"reds={red}, balls={n}");
                }
            }
            else
            {
                ok = false;
                mapIssues.Add($"unsupportedDiscipline={_discipline}");
            }

            for (int i = 0; i < n; i++)
            {
                if (_undoNumbers[i] < 0) continue;
                if (!_undoIndexByNumber.TryAdd(_undoNumbers[i], i))
                {
                    ok = false;
                    mapIssues.Add($"duplicate#{_undoNumbers[i]}@{i}");
                }
            }

            _undoUsable = ok && _undoIndexByNumber.Count == n;
            if (!_undoUsable)
            {
                string cueName = runner.CueBallIndex >= 0 && runner.CueBallIndex < _ballList.Count
                    ? (_ballList[runner.CueBallIndex] != null ? _ballList[runner.CueBallIndex].name : "<null>")
                    : "<invalid-index>";
                Debug.LogWarning($"[RulesBridge] {BoundBay}: undo number map failed validation " +
                                 "(discipline=" + _discipline + ", balls=" + n + ", cue=" + runner.CueBallIndex +
                                 ":" + cueName + ", issues=" + string.Join("; ", mapIssues) +
                                 ") — solo-practice UNDO disabled for this bay.");
            }
        }

        /// <summary>Snap the pre-shot table into the undo ring. Validation failures (e.g. an
        /// un-mappable rack) disable UNDO for this bay with a single warning instead of
        /// throwing mid-gameplay.</summary>
        bool PushUndoSnapshot()
        {
            if (_undo == null || _bound == null)
                return false;

            try
            {
                _undo.Push(CaptureUndoState());
                return true;
            }
            catch (ArgumentException ex)
            {
                _undoUsable = false;
                if (!_undoDisabledWarned)
                {
                    _undoDisabledWarned = true;
                    Debug.LogWarning("[RulesBridge] undo snapshot rejected by the save codec (" + ex.Message +
                                     ") — UNDO disabled for this bay.");
                }
                return false;
            }
        }

        PoolSaveState CaptureUndoState()
        {
            PoolPhysicsEventRunner runner = _bound;
            var balls = new PoolSaveBall[runner.BallCount];
            for (int i = 0; i < runner.BallCount; i++)
            {
                // Table-local sim coordinates — the same space PoolSaveModel documents for X/Z.
                // Resting balls always sit at Y == BallRadius, so Y is implied on restore.
                runner.TryGetSimBall(i, out Vector3 pos, out _, out _, out bool pocketed);
                balls[i] = new PoolSaveBall { Number = _undoNumbers[i], X = pos.x, Z = pos.z, Pocketed = pocketed };
            }
            return new PoolSaveState
            {
                RulesMode = _discipline == Discipline.Snooker ? "Snooker" : "NineBall",
                CurrentPlayer = _turnManager != null ? Mathf.Clamp(_turnManager.currentPlayer, 0, 1) : 0,
                RacksWon = new[] { 0, 0 },          // frame score is rules-layer territory (documented simplification)
                ShotNumber = _undoShotCounter,
                BallInHand = false,                 // bridge does not track ball-in-hand yet (documented simplification)
                IsBreakShot = _undoShotCounter == 0,
                Balls = balls
            };
        }

        /// <summary>Solo-practice UNDO entry point (BhpCueGripController.OnUndoRequested → here).
        /// Restores the newest pre-shot snapshot: ball positions, pocketed flags and the shot
        /// counter. Returns false (and touches nothing) in any non-practice situation.</summary>
        void HandleUndoRequested()
        {
            RequestUndo();
        }

        public bool RequestUndo()
        {
            if (!UndoAvailable)
                return false;
            if (!_undo.TryUndo(out PoolSaveState restored, out string error))
            {
                Debug.LogWarning("[RulesBridge] UNDO refused: " + error);
                return false;
            }

            ApplyUndoState(restored);
            _undoShotCounter = restored.ShotNumber;    // re-numbering the replayed shot keeps saves honest
            return true;
        }

        void ApplyUndoState(PoolSaveState st)
        {
            PoolPhysicsEventRunner runner = _bound;
            int restoredBalls = 0, parkedBalls = 0;
            for (int i = 0; i < st.Balls.Length; i++)
            {
                PoolSaveBall b = st.Balls[i];
                if (!_undoIndexByNumber.TryGetValue(b.Number, out int idx))
                    continue;
                SnookerBallTracker.BallInfo info = _ballTracker != null ? _ballTracker.BallAt(idx) : null;

                if (b.Pocketed)
                {
                    if (runner.ParkBall(idx)) parkedBalls++;
                    if (info != null) info.potted = true;
                }
                else
                {
                    Vector3 world = runner.SimPositionToWorld(new Vector3((float)b.X, runner.BallRadius, (float)b.Z));
                    if (runner.RestoreBall(idx, world))
                    {
                        restoredBalls++;
                        if (info != null && info.potted) _ballTracker.Unpot(info);
                    }
                }
            }

            // cancel any in-flight bookkeeping so the replayed shot starts from a clean slate
            _shotOpen = false;
            _settlePending = false;
            _objectPots = 0;
            _cuePotted = false;
            _reportedFirstHitIndex = -1;

            Debug.Log($"[RulesBridge] UNDO applied on {BoundBay}: restored={restoredBalls} parked={parkedBalls} " +
                      $"(pre-shot {st.ShotNumber}, stack left={_undo.Count})");
        }

        SnookerBallTracker.PocketInfo NearestPocket(Vector3 world)
        {
            SnookerBallTracker.PocketInfo best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _pockets.Count; i++)
            {
                Vector3 delta = _pockets[i].position - world;
                delta.y = 0f;
                float d = delta.magnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = _pockets[i];
                }
            }
            return best;
        }
    }
}
