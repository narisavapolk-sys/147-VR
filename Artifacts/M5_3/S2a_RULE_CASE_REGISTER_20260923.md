# S2a Rules Validation — Pre-Registered Case List

**Tree:** integration/008-m53 @ eeef2338 (registration must be committed before execution)
**Mode:** S2a log-driven probe only. No NUnit shell execution in this phase.
**Determinism:** controlled scripted inputs; deterministic decision cases are R18/R19/R39. Runtime lifecycle cases depend on controlled reset state and are not physics-determinism certification.
**Scope:** rules/gameplay spine on integration line. S3/S5 physics/REAL10 are excluded.

| case_id | behaviour class | input | expected | basis |
|---|---|---|---|---|
| R01 | shot legality/scoring | Red on; 1 red potted | LEGAL; +1; continue; next nominated colour; reds 14 | M5_3_RulesUnitTests::LegalRedSingleScoresOneAndMovesToColour |
| R02 | scoring | Red on; 2 reds potted | LEGAL; +2; reds 13 | M5_3_RulesUnitTests::LegalMultipleRedsScoreEachRed |
| R03 | foul detection | Red + colour in same stroke | FOUL; penalty 7; colour respotted; red removed | M5_3_RulesUnitTests::RedAndColourSameStrokeIsOneFoulAndRedIsRemoved |
| R04 | foul detection | Red on; first contact black | FOUL; highest single penalty 7 | M5_3_RulesUnitTests::WrongFirstContactUsesHighestSinglePenalty |
| R05 | foul detection | Red on; hit nothing/no cushion | FOUL; penalty 4; reason Miss | M5_3_RulesUnitTests::MissAllIsFoul |
| R06 | foul detection | Cue ball potted while red on | FOUL; in-hand; opponent +4 | M5_3_RulesUnitTests::CueBallPottedRequestsInHandInD |
| R07 | foul detection | Cue ball off table while red on | FOUL; in-hand; opponent +4 | M5_3_RulesUnitTests::CueBallOffTableRequestsInHandInD |
| R08 | scoring/ball-on | Nominated black potted | LEGAL; +7; black respotted; next red | M5_3_RulesUnitTests::NominatedBlackScoresSevenAndRespots |
| R09 | foul detection | Nominated black; blue potted | FOUL; opponent +7; blue respotted | M5_3_RulesUnitTests::NominatedBlackThenBlueIsFoulWithSevenPenalty |
| R10 | foul detection | Colour on; red potted | FOUL; +7; red removed; reds decrement | M5_3_RulesUnitTests::ColourOnRedPottedIsFoulAndRedRemainsRemoved |
| R11 | foul detection | Colour on; nominated black + red potted | ONE foul; +7; black respot; red removed | M5_3_RulesUnitTests::ColourOnNominatedBlackAndRedIsOneFoulWithRedRemoved |
| R12 | foul detection | Wrong first + cue potted + black potted | Multiple foul reasons; one penalty = 7 | M5_3_RulesUnitTests::MultipleFoulReasonsStillAwardOneMaximumPenalty |
| R13 | turn transition | Legal no-pot red-on shot | LEGAL; no continuation; ball-on remains red | M5_3_RulesUnitTests::LegalNoPotWithCushionEndsTurn |
| R14 | turn transition | Legal no-pot without cushion | LEGAL under Phase A; no continuation | M5_3_RulesUnitTests::NoPotWithoutCushionIsStillLegalUnderStandardSnooker |
| R15 | scoring/ball-on | Reds gone; yellow potted | LEGAL; +2; remove yellow; next green | M5_3_RulesUnitTests::ColoursRunInAscendingOrderAfterRedsAreGone |
| R16 | frame end | Final black potted legally | FRAME END; +7; no continuation; next ball null | M5_3_RulesUnitTests::FinalBlackLegalEndsFrameAndDoesNotContinue |
| R17 | frame end/foul | Final-black phase wrong first contact | FRAME END candidate; foul +7; no continuation | M5_3_RulesUnitTests::FinalBlackFoulEndsFrameAndDoesNotContinue |
| R18 | determinism | Same red pots in reversed input order | Full ShotDecision equivalent | M5_3_RulesUnitTests::PendingPotPermutationProducesEquivalentFullDecision |
| R19 | determinism | Same ShotObservation evaluated twice | Full ShotDecision equivalent | M5_3_RulesUnitTests::DuplicateSequencePreservesFullDecision |
| R20 | input validation | Duplicate BallId in pots | REJECT with ArgumentException | M5_3_RulesUnitTests::DuplicateBallIdIsRejected |
| R21 | input validation | Same red callback twice | REJECT with ArgumentException | M5_3_RulesUnitTests::SameRedCallbackTwiceIsRejected |
| R22 | input validation | Forged black value 1 | REJECT; canonical value remains 7 | M5_3_RulesUnitTests::ForgedBallValueIsRejected |
| R23 | input validation | Cue flag/list contradiction | REJECT with ArgumentException | M5_3_RulesUnitTests::CueFlagAndPotListContradictionIsRejected |
| R24 | input validation | Striker = 3 | REJECT with ArgumentOutOfRangeException | M5_3_RulesUnitTests::InvalidStrikerIsRejected |
| R25 | input validation | Reds remaining = 16 | REJECT with ArgumentOutOfRangeException | M5_3_RulesUnitTests::RedsAboveFifteenAreRejected |
| R26 | input validation | Red ball-on with zero reds | REJECT with ArgumentException | M5_3_RulesUnitTests::RedOnWithZeroRedsIsRejected |
| R27 | input validation | Nominated-colour state with zero reds but missing nomination | REJECT invalid state | M5_3_RulesUnitTests::OrderedColourMissingAfterRedsAreGoneIsRejected |
| R28 | input validation | Nominated-colour phase without nomination | REJECT invalid telemetry | M5_3_RulesUnitTests::MissingNominationIsRejectedAsInvalidTelemetry |
| R29 | input validation | Invalid enum values | REJECT invalid enum state | M5_3_RulesUnitTests::InvalidEnumStateIsRejected |
| R30 | input validation | FirstContact=CueBall while object ball was hit | REJECT invalid observation | M5_3_RulesUnitTests::FirstContactCueBallWhileObjectHitIsRejected |
| R31 | input validation | Two reds potted when only one remained | REJECT by rules engine | M5_3_RulesUnitTests::MoreRedsPottedThanWereAvailableIsRejectedByEngine |
| R32 | input validation | Default M5PottedBall | REJECT invalid/default pot | M5_3_RulesUnitTests::DefaultPottedBallIsRejected |
| R33 | ball-on | Nominated colour with no pot | LEGAL; turn ends; next red | M5_3_RulesUnitTests::NominatedColourNoPotReturnsToRed |
| R34 | ball-on/turn | Nominated-colour foul | FOUL; incoming player sees red | M5_3_RulesUnitTests::NominatedColourFoulReturnsToRedForIncomingPlayer |
| R35 | ball-on | Last red foul removes last red | Next reds=0; next ball nominated colour | M5_3_RulesUnitTests::LastRedFoulMovesNextBallToColour |
| R36 | foul detection | Red potted while colour on | Distinct RedPottedWhenColourOn reason | M5_3_RulesUnitTests::RedPottedWhenColourOnUsesDistinctFoulReason |
| R37 | frame end/tie recovery | Final black legal | Recovery candidate true; black pot identified | M5_3_RulesUnitTests::FinalBlackPreservesRecoveryDataForTieResolution |
| R38 | frame end/tie recovery | Final black foul | Recovery candidate true; no black pot; penalty 7 | M5_3_RulesUnitTests::FinalBlackFoulPreservesRecoveryPenaltyData |
| R39 | determinism | Red+colour permutation order A vs B | Full ShotDecision equivalent | M5_3_RulesUnitTests::MixedRedColourPermutationProducesEquivalentFullDecision |
| R40 | ball-on | Last red potted legally | Next reds=0; next ball nominated colour | M5_3_RulesUnitTests::LastRedTransitionsToNominatedColourPhase |
| R41 | ball-on/scoring | Post-last-red nominated black | Next ball ordered colours; next colour yellow | M5_3_RulesUnitTests::PostLastRedColourTransitionsToOrderedYellow |
| R42 | ball-on/foul | Ordered phase; wrong colour first | FOUL; same ordered colour remains on | M5_3_RulesUnitTests::OrderedColourFoulKeepsSameColourOn |
| R43 | frame end | Final black on, no pot | No frame end; black remains on; turn ends | M5_3_RulesUnitTests::FinalBlackLegalNoPotDoesNotCreateRecoveryCandidate |
| I01 | turn transition | Applier commits legal red decision | One score notification; one turn notification; P1 continues; ball-on colour | M5RuntimeTransactionTests::Applier_Commits_One_Score_And_One_Turn_Notification |
| I02 | foul/turn | Applier commits cue foul | P2 +4; turn P2; cue not left potted | M5RuntimeTransactionTests::Applier_CueFoul_Uses_StableCueId_And_PassesTurn |
| I03 | transaction integrity | Decision/start sequence mismatch | REJECT before mutation; valid retry commits | M5RuntimeTransactionTests::Applier_Rejects_MismatchedSequence_WithoutMutation_Then_Retries |
| I04 | lifecycle boundary | Stale PhysicsSettled sequence | Ignored; LastResolvedSequence unchanged | M5RuntimeTransactionTests::Tracker_SequenceGuard_Ignores_Stale_Settle |
| I05 | lifecycle boundary | ForceResolve explicit cue=false | ShotResolved reports cue off-table | M5RuntimeTransactionTests::Tracker_ForceResolve_Uses_Explicit_CueObservation |
| I06 | ball identity | Production cue points=0 | Resolve White_CueBall as cue identity | M5RuntimeTransactionTests::Tracker_Resolves_ProductionCue_ByPointsZero |
| I07 | ball identity | Calibration Sphere.009 points=0 | Resolve Sphere.009 as cue identity | M5RuntimeTransactionTests::Tracker_Resolves_CalibrationSphere009_ByPointsZero |
| I08 | ball identity | Two points=0 cue identities | REJECT duplicate cue identity | M5RuntimeTransactionTests::Tracker_Rejects_DuplicateCueIdentity |
| I09 | observation builder | Duplicate pot callback | Observation contains one stable ball ID | M5RuntimeTransactionTests::Builder_Deduplicates_BallPotted_Events |
| I10 | transaction integrity | Invalid transaction plan | STOP before mutation | M5RuntimeTransactionTests::Applier_InvalidPlan_StopsBeforeMutation |
| I11 | transaction integrity | Injected mutation failure | Rollback score/turn/balls; retry remains possible | M5RuntimeTransactionTests::Applier_RollsBack_AfterInjectedMutationFailure_And_AllowsRetry |
| I12 | lifecycle boundary | M5ShotLifecycle BeginShot | Idle/Settled -> Active; sequence increments; ShotStarted emitted | M5ShotLifecycle::BeginShot |
| I13 | lifecycle boundary | FixedUpdate measured settle | Speed <= threshold for 1.5s -> Settled; ShotSettled emitted | M5ShotLifecycle::FixedUpdate |
| I14 | event contract | Lifecycle ShotStarted/ShotSettled bridge | Contract emits ShotStarted and PhysicsSettled(sequence) | M5ShotEventContract::HandleShotStarted / HandlePhysicsSettled |
| I15 | turn transition | StrikerContinues after legal pot | Current player remains striker until next shot | SnookerTurnManager::StrikerContinues |
| I16 | turn transition | NextTurn/SetTurn | Current player toggles; TurnChanged emitted | SnookerTurnManager::NextTurn / SetTurn |
| I17 | scoring | ScoreManager M5 decision application | Points, reds, ball-on, frame state follow ShotDecision | SnookerScoreManager::ApplyM5Decision |
| I18 | frame end | Final black transaction | FrameEnd propagated to score and turn; no continuation | M5ShotTransactionApplier::Apply |
| I19 | match progression | Match/frame progression beyond frameOver | NOT IMPLEMENTED in current Quest rules spine; no match-state authority found | source scan Assets/Scripts/Quest + Assets/Scripts/AAA |
| I20 | match progression | Multi-frame match winner transition | NOT IMPLEMENTED; no match manager/state authority found | source scan Assets/Scripts/Quest + Assets/Scripts/AAA |

## Execution rules
- This register is the frozen precondition for S2a; cases may not be removed because of observed results.
- PASS/FAIL/NOT_IMPLEMENTED must be emitted per case in the S2a log and JSON evidence.
- FAIL inside frozen M5.3 rules core is STOP: report the finding; do not edit protected code to make the case pass.
- INVALID is reserved for missing argv/CWD, non-absolute log path, wrapper-boundary defects, or unknown tree/HEAD.
