# 147VR AAA Physics Baseline

## Calibration policy
Measured gameplay data must drive tuning. Do not promote guessed values to final production settings.

## Current profile candidates
TableSurfaceProfile
- rollingFriction: 0.18
- slidingFriction: 0.32
- spinFriction: 0.08
- settleSpeed: 0.012
- settleSpin: 0.035

PocketProfile
- captureRadius: 0.055 m
- captureDepth: 0.06 m
- captureSpeed: 2.5 m/s
- rollInAssist: 0.15

## Baseline cases
- Straight
- Stun
- Follow
- Draw
- LeftEnglish
- RightEnglish
- Cushion
- Pocket

## Acceptance rule
Every case must be measured in a controlled scene before final tuning.
Record target, measured result, error, Unity version, fixed timestep, and profile revision.

## Next
Bind the report runner to the real 147VR table and ball objects, execute repeatable shots,
and tune only the relevant Profile values from measured errors.
