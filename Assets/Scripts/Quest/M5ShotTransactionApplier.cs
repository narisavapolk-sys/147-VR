using System;
using System.Collections.Generic;
using M5Rules;
using UnityEngine;

/// <summary>Applies one complete decision as a single runtime transaction.</summary>
public sealed class M5ShotTransactionApplier
{
    private readonly SnookerScoreManager _score;
    private readonly SnookerBallTracker _balls;
    private readonly SnookerTurnManager _turns;
    private readonly Dictionary<int, SnookerBallTracker.BallInfo> _identity;

#if UNITY_EDITOR
    private readonly Action _editorAfterBallMutation;
#endif

    public M5ShotTransactionApplier(SnookerScoreManager score, SnookerBallTracker balls,
        SnookerTurnManager turns)
    {
        _score = score ?? throw new ArgumentNullException(nameof(score));
        _balls = balls ?? throw new ArgumentNullException(nameof(balls));
        _turns = turns ?? throw new ArgumentNullException(nameof(turns));
        _balls.RefreshIfNeeded();
        _identity = BuildIdentityMap(_balls);
    #if UNITY_EDITOR
        _editorAfterBallMutation = null;
#endif
    }

#if UNITY_EDITOR
    public M5ShotTransactionApplier(SnookerScoreManager score, SnookerBallTracker balls,
        SnookerTurnManager turns, Action editorAfterBallMutation)
        : this(score, balls, turns)
    {
        _editorAfterBallMutation = editorAfterBallMutation;
    }
#endif

    public bool Apply(ShotDecision decision, M5ShotStartState start)
    {
        if (decision == null) throw new ArgumentNullException(nameof(decision));
        if (start == null) throw new ArgumentNullException(nameof(start));
        if (decision.ShotSequence != start.ShotSequence)
            throw new InvalidOperationException("Decision/start sequence mismatch.");
        if (_turns.currentPlayer != start.Striker)
            throw new InvalidOperationException("Turn owner changed during shot.");
        ValidatePlan(decision);
        var score = _score.CaptureM5TransactionState();
        var turn = _turns.CaptureM5TransactionState();
        var balls = SnapshotBalls(decision);
        try
        {
            ApplyBalls(decision);
#if UNITY_EDITOR
            _editorAfterBallMutation?.Invoke();
#endif
            _score.ApplyM5Decision(decision, start.Striker);
            _turns.ApplyM5Decision(decision.FrameEndCandidate, decision.StrikerContinues);
            _score.StoreM5FinalBlackRecovery(decision);
            return CommitNotifications();
        }
        catch
        {
            RestoreBalls(balls);
            _score.RestoreM5TransactionState(score);
            _turns.RestoreM5TransactionState(turn);
            throw;
        }
    }

    private bool CommitNotifications() { Exception firstFault = null; try { _score.EmitM5TransactionCommitted(); } catch (Exception ex) { firstFault ??= ex; Debug.LogException(ex); } try { _turns.EmitM5TransactionCommitted(); } catch (Exception ex) { firstFault ??= ex; Debug.LogException(ex); } if (firstFault != null) Debug.LogError("[M5 TXN] State committed, but one or more commit notifications faulted."); return firstFault == null; }

    private void ValidatePlan(ShotDecision d)
    {
        var seen = new HashSet<int>();
        foreach (var p in d.BallsToRemove)
            ValidatePlannedBall(p.BallId, seen, requireHomePosition: false);
        foreach (var p in d.BallsToRespot)
            ValidatePlannedBall(p.BallId, seen, requireHomePosition: true);
        if (d.CueBallInHandInD)
            ValidatePlannedBall(1, seen, requireHomePosition: true);
        if (d.SingleFoulPenalty < 0 || d.SingleFoulPenalty > 7)
            throw new InvalidOperationException("Invalid single foul penalty.");
    }

    private void ValidatePlannedBall(int ballId, HashSet<int> seen, bool requireHomePosition)
    {
        if (!seen.Add(ballId))
            throw new InvalidOperationException("Duplicate ball in transaction plan.");
        if (!_identity.TryGetValue(ballId, out var ball) || ball == null)
            throw new InvalidOperationException($"Ball identity {ballId} is missing.");
        if (ball.transform == null)
            throw new InvalidOperationException($"Ball identity {ballId} has no live Transform.");
        if (requireHomePosition && !ball.hasHomePosition)
            throw new InvalidOperationException($"Ball identity {ballId} is missing required home position state.");
    }

    private void ApplyBalls(ShotDecision d)
    {
        foreach (var p in d.BallsToRemove) _identity[p.BallId].potted = true;
        foreach (var p in d.BallsToRespot)
        {
            var b = _identity[p.BallId]; b.potted = true; _score.RespotM5Ball(b);
        }
        if (d.CueBallInHandInD) { _identity[1].potted = true; _score.RespotM5Ball(_identity[1]); }
    }
    private List<BallSnapshot> SnapshotBalls(ShotDecision d)
    {
        var ids = new HashSet<int>();
        foreach (var p in d.BallsToRemove) ids.Add(p.BallId);
        foreach (var p in d.BallsToRespot) ids.Add(p.BallId);
        if (d.CueBallInHandInD) ids.Add(1);
        var result = new List<BallSnapshot>();
        foreach (int id in ids)
            if (_identity.TryGetValue(id, out var b) && b.transform != null)
                result.Add(new BallSnapshot(b, b.potted, b.transform.position));
        return result;
    }

    private static void RestoreBalls(List<BallSnapshot> snapshots)
    {
        foreach (var s in snapshots)
        {
            if (s.Ball?.transform == null) continue;
            s.Ball.potted = s.Potted; s.Ball.transform.position = s.Position;
        }
    }

    private static Dictionary<int, SnookerBallTracker.BallInfo> BuildIdentityMap(SnookerBallTracker tracker)
    {
        var map = new Dictionary<int, SnookerBallTracker.BallInfo>();
        var reds = new List<SnookerBallTracker.BallInfo>();
        foreach (var b in tracker.Balls)
        {
            if (b == null || b.transform == null) continue;
            if (b.points == 0) { if (map.ContainsKey(1)) throw new InvalidOperationException("Duplicate cue identity."); map[1] = b; }
            else if (b.points == 1) reds.Add(b);
            else { int id = 100 + b.points; if (map.ContainsKey(id)) throw new InvalidOperationException("Duplicate colour identity."); map[id] = b; }
        }
        if (!map.ContainsKey(1)) throw new InvalidOperationException("Cue identity missing.");
        if (reds.Count > 15) throw new InvalidOperationException("More than 15 red identities found.");
        reds.Sort((a,b) => string.CompareOrdinal(Path(a.transform), Path(b.transform)));
        for (int i=0; i<reds.Count; i++) map[10+i] = reds[i];
        return map;
    }

    private static string Path(Transform t)
    {
        var parts = new Stack<string>();
        while (t != null) { parts.Push(t.GetSiblingIndex() + ":" + t.name); t=t.parent; }
        return string.Join("/", parts.ToArray());
    }

    private readonly struct BallSnapshot
    {
        public readonly SnookerBallTracker.BallInfo Ball; public readonly bool Potted; public readonly Vector3 Position;
        public BallSnapshot(SnookerBallTracker.BallInfo ball, bool potted, Vector3 position)
        { Ball=ball; Potted=potted; Position=position; }
    }
}


