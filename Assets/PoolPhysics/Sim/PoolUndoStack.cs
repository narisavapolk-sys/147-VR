using System;
using System.Collections.Generic;

namespace PoolPhysics.Sim
{
    // ---------------------------------------------------------------------------
    // PoolUndoStack — UNDO-last-shot support (S2 #2c), headless-pure.
    //
    // MECHANISM: the caller (Unity bridge) pushes the PoolSaveState BEFORE every
    // shot; the player's UNDO button pops the newest entry and the match state is
    // restored from it. Entries are stored as CANONICAL JSON (PoolSaveCodec.ToJson
    // is deterministic) instead of object references:
    //   * restoring is guaranteed byte-equal to the pushed state (no aliasing),
    //   * stack contents can be compared/shipped/logged as text,
    //   * corruption surfaces as a false return, never as a partial restore.
    //
    // CONTRACT (mirrors PoolSaveModel):
    //   * Push validates via the codec and throws ArgumentException on an invalid
    //     state — an authoring bug must fail loudly at the push site (same policy
    //     as ToJson). The stack itself never mutates on a failed push.
    //   * TryUndo NEVER throws on any internal content; a stack entry that fails
    //     its round-trip is reported as false (caller keeps the current state).
    //   * Capacity is a hard ring: pushing beyond it discards the OLDEST entry
    //     first (owner asked for "UNDO last shot" only — depth is a bonus knob).
    //   * Undo restores EXACTLY the pre-shot state, including ShotNumber and the
    //     shooter, so re-playing the identical shot is deterministic (tested U1).
    // ---------------------------------------------------------------------------
    public sealed class PoolUndoStack
    {
        public int Capacity { get; }
        readonly List<string> _serialized = new();
        readonly List<string> _corrupt = new();

        public PoolUndoStack(int capacity = 8)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "undo depth must be >= 1");
            Capacity = capacity;
        }

        public int Count => _serialized.Count;

        /// <summary>Clear snapshots and diagnostic failures when the owning game mode changes.</summary>
        public void Clear()
        {
            _serialized.Clear();
            _corrupt.Clear();
        }

        /// <summary>Snap a validated save state onto the stack (ring-trims the oldest).</summary>
        public void Push(PoolSaveState state)
        {
            string json = PoolSaveCodec.ToJson(state); // validates + throws on invalid authoring
            _serialized.Add(json);
            if (_serialized.Count > Capacity)
                _serialized.RemoveAt(0);
        }

        /// <summary>
        /// Pop the newest pre-shot state and parse it back. Returns false when the
        /// stack is empty or the top entry no longer round-trips (nothing mutated).
        /// </summary>
        public bool TryUndo(out PoolSaveState restored, out string error)
        {
            restored = null;
            error = null;
            if (_serialized.Count == 0) { error = "undo stack empty"; return false; }
            int top = _serialized.Count - 1;
            string json = _serialized[top];
            if (!PoolSaveCodec.TryFromJson(json, out restored, out error))
            {
                // keep the entry in place (never mutate on failure), but flag it so
                // repeated attempts do not spin silently
                _corrupt.Add(error);
                restored = null;
                return false;
            }
            _serialized.RemoveAt(top);
            return true;
        }

        /// <summary>Diagnostic: errors recorded by failed_undo attempts since creation.</summary>
        public int CorruptCount => _corrupt.Count;

        /// <summary>Non-mutating peek of the newest entry (canonical JSON or empty).</summary>
        public string PeekTop()
            => _serialized.Count == 0 ? string.Empty : _serialized[_serialized.Count - 1];
    }
}
