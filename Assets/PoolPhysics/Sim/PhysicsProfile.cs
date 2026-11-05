namespace PoolPhysics.Sim
{
    /// <summary>
    /// Numeric physics profile. Fields marked (CueVR) were recovered from
    /// libcuevr.so; fields marked [TUNE] were NOT pinned by RE and are seeded
    /// from cross-game PhysX PhysicMaterial priors (UnlimitedSnooker / Saitara /
    /// Backspin — see physics_params_from_games.md), then must be fitted to GOLDEN.
    ///
    /// NOTE: PhysX material bounciness/friction are a DIFFERENT model/units than
    /// this analytic solver's e/μ — treat the game values as *bounds & ordering
    /// priors*, not drop-in numbers.
    ///
    /// Plain data so it can be authored as a ScriptableObject (Unity/) or fed
    /// directly in headless tests.
    /// </summary>
    public sealed class PhysicsProfile
    {
        // ---- Integrator (CueVR, HIGH — from cue_phys_set_substep) ----
        /// <summary>Fixed physics substep in seconds. Cue VR uses 0.0005 (0.5 ms).</summary>
        public float Substep = 0.0005f;
        /// <summary>Max substeps per Update to avoid spiral-of-death at low framerate.</summary>
        public int   MaxSubstepsPerFrame = 2000;

        // ---- World (CueVR, HIGH) ----
        public float Gravity = 9.806f; // m/s^2 (cue_phys_step @0x8a33ec)

        // ---- Balls (CueVR radius HIGH, mass MED) ----
        public float PoolBallRadius    = 0.028575f; // 57.15 mm WPA
        public float SnookerBallRadius = 0.02625f;  // 52.5 mm
        public float BallMass          = 0.16f;     // [TUNE] games don't expose mass; WPA ~0.16-0.17 kg

        // ---- Solid sphere inertia & rolling transition (CueVR, HIGH) ----
        public float InertiaFactor = 0.4f;   // 2/5
        public float RollFactor    = 5f/7f;  // 5/7

        // ---- Cloth / friction [TUNE] (games: cloth dyn friction pool~0.2, snooker~0.5) ----
        public float SlidingFriction   = 0.2f;  // [TUNE] μ_s ball vs cloth (pool prior)
        public float RollingFriction   = 0.01f; // [TUNE] μ_r (rolling resistance)
        public float SpinningFriction  = 0.044f;// [TUNE] μ_sp (pivoting/spin decay about vertical)
        public float SpinDecay         = 1.4726f; // (CueVR) from .bss spin-decay constant
        public float RestSpeedEpsilon  = 0.002f;  // below this the ball is considered stopped

        // ---- Ball-ball collision [TUNE] (games: ball bounce 0.90–0.925 → ~0.93) ----
        public float BallBallRestitution = 0.93f; // [TUNE] seeded from game ball-bounciness
        public float BallBallFriction    = 0.06f; // [TUNE] tangential → "throw"

        // ---- Cushion [TUNE] (games: TableCushion bounce 0.78, friction ~0.35 snooker / ~0 pool) ----
        public float CushionRestitution = 0.78f;  // [TUNE] e_cush (UnlimitedSnooker TableCushion)
        public float CushionFriction    = 0.30f;  // [TUNE] μ_cush
        public float CushionSpinLoss    = 0.85f;  // [TUNE] cush_spin multiplier
        // Cushion "throw" ∝ x/√y — modelled in PoolSimulator, coefficient [TUNE]:
        public float CushionThrowCoeff  = 0.0f;   // [TUNE] 0 = disabled until fitted

        // ---- Spin gains from cue-tip offset (CueVR default 0.8/0.4 across 3 axes) ----
        public float SpinGainTangential = 0.8f;
        public float SpinGainVertical   = 0.4f;

        // ---- Nap (directional cloth) [TUNE — Cue VR models this; games don't expose] ----
        /// <summary>Extra friction fraction when rolling ACROSS the nap vs along it (0=off).</summary>
        public float NapAnisotropy = 0.15f;
        /// <summary>Lateral "steer/curve" coefficient for balls rolling across the nap (0=off).</summary>
        public float NapSteer = 0.0f;

        // ---- Throw / gearing off surfaces [TUNE] ----
        /// <summary>Tangential velocity imparted from spin when a ball leaves a cushion (0=off).</summary>
        public float CushionGear = 0.0f;

        // ---- Pockets ----
        public float PocketCaptureRadius = 0.056f; // [TUNE] capture test radius

        /// <summary>Shallow copy for parameter search / fitting.</summary>
        public PhysicsProfile Clone() => (PhysicsProfile)MemberwiseClone();
    }
}
