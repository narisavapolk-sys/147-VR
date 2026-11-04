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
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    public sealed class PoolPhysicsRulesBridge : MonoBehaviour
    {
        public enum Discipline { None, Snooker, Pool }

        [Header("Diagnostics")]
        [SerializeField] bool logEvents = true;

        PoolPhysicsAuthorityDirector _director;
        SnookerBallTracker _ballTracker;
        SnookerShotTracker _shotTracker;
        SnookerTurnManager _turnManager;
        M5ShotLifecycle _lifecycle;

        PoolPhysicsEventRunner _bound;
        readonly List<SnookerBallTracker.PocketInfo> _pockets = new List<SnookerBallTracker.PocketInfo>();
        readonly List<Transform> _ballList = new List<Transform>();

        Discipline _discipline = Discipline.None;
        bool _shotOpen;
        int _objectPots;
        bool _cuePotted;
        bool _settlePending;
        int _reportedFirstHitIndex = -1;

        public Discipline CurrentDiscipline => _discipline;
        public string BoundBay => _bound != null ? _bound.BayName : "<none>";
        public int PotsLastShot => _objectPots;

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
                _lifecycle.ShotStarted += OnShotStarted;

            if (_director != null && _director.Active != null)
                Bind(_director.Active);
        }

        void OnDestroy()
        {
            if (_director != null)
                _director.ActiveChanged -= OnActiveChanged;
            if (_lifecycle != null)
                _lifecycle.ShotStarted -= OnShotStarted;
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

            runner.SimEvent += OnSimEvent;
            runner.ShotFinished += OnShotFinished;

            Debug.Log($"[RulesBridge] bound {runner.BayName}: balls={_ballList.Count} cue={runner.CueBallIndex} " +
                      $"pockets={_pockets.Count} discipline={_discipline} " +
                      $"snookerRules={(_shotTracker != null && _shotTracker.snookerRulesEnabled)}");
        }

        void Unbind()
        {
            if (_bound == null)
                return;
            _bound.SimEvent -= OnSimEvent;
            _bound.ShotFinished -= OnShotFinished;
            _bound = null;
        }

        // ---- Shot lifecycle ----

        void OnShotStarted()
        {
            _shotOpen = true;
            _objectPots = 0;
            _cuePotted = false;
            _reportedFirstHitIndex = -1;
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
                OnShotStarted();
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
