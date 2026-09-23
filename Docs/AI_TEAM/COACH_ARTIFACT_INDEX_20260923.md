# COACH ARTIFACT INDEX — 2026-09-23

Session: memory-reset recovery → 12f1 UPM frontier (D-1 / D-3.0 / D-3a)
Discipline: F9 — tracked path, sha256 recorded, no `.log` artifacts.

**Column semantics:** the `sha256` column is the **content sha256 of the file's canonical bytes (LF)** —
it is a verification value for the artifact, **never** a git object id and never an apply anchor.
Git blob ids and commit ids live in patch `index` lines, `git ls-tree` output and `WHICH_COMMIT` fields.
Base blob of this index when Pack v8 was generated: `d0158146dc872ba9e77b9050fa7e621bd7c9696e`.

## Docs/AI_TEAM/

| Artifact | sha256 |
|---|---|
| `COACH_BRIEF_MEMORY_RESET_RECOVERY_20260923.md` | `e67391c942ede51dcd47f54a346b8400d99a6a708d786379234ac5d85a079ef3` |
| `COACH_OPERATING_CONTRACT_147VR.md` | `12616f19dd5cdb7f0902e2bc4e83f00d1086a118168a5121faa13e5de0a54ede` |
| `COACH_REVIEW_001_RECONSTRUCTED_STATE_20260923.md` | `f3d1082cb6fe923cee8ae37723a15cf7b6af2d202b22dd4dfe5622efa9c1952e` |
| `COACH_REVIEW_002_UPM_INSTALL_ARCHITECTURE_20260923.md` | `260e3f8db8e9f747426ad5cc4ff700e98a2581567fd86d44919bbee09c11930a` |
| `COACH_WORKORDER_001_D3_EMPTY_PROJECT_20260923.md` | `d0fb8634f9c6f7088d05688d8e927cc772ee2676d2b1da5323a788c31eb07ecc` |
| `COACH_WORKORDER_002_D3A_RECOVERY_20260923.md` | `b8647919b79b064119898edd3eee1363d0dc60fafe7358b407b1ae6181634ae1` |
| `COACH_WORKORDER_003_D3A_CREATION_20260923.md` | `c8c9c698bc21cf1fecf2e704e8baea6a0ebecdf8666923d3f64a2d59ca4e8b68` |
| `COACH_WORKORDER_004_INTEGRATION_LINE_20260923.md` | `8b6bde188d832e29dd2c530fb4a3c4987bc88d72e36fefdde51691bb144e2249` |
| `COACH_DISPOSITION_R3_PACKAGES_LOCK_20260923.md` | `e6e483f654533a64598d3ed6b4e2fb9967c705f780215a1bed8f98296b068b55` |
| `COACH_WORKORDER_005_S1B_S2_20260923.md` | `bf1041bea95e1a8f65c0d9c3b038c796d842755b04a09a957b9097c1e3a4ad69` |
| `COACH_WORKORDER_006_S2A_DISPOSITION_20260923.md` | `72c209ec8a480dd2f05226188f6a62206f530ae3f54239d2addfceba563cdb2e` |
| `COACH_WORKORDER_007_NEXT_ACTIONS_AND_CONVERGENCE_20260923.md` | `c974ad4fcb117f58b2fb6a5dcce923aa4eafd6287add46165566ed40aed046bb` |
| `COACH_WORKORDER_008_BLOCKED_WINDOW_20260923.md` | `2735e65bde17c1a054d3d2383c0edcc194d65d6faecaccb686ac74776ff3bedb` |
| `COACH_WORKORDER_009_PROVENANCE_AND_BLOCKED_WINDOW_20260923.md` | `cd0370a972baf052983ec2a1f12a96dc377f8c36f3100740738ba149b398cba6` |
| `COACH_WORKORDER_010_VERIFIED_PUSH_AND_UPM_SESSION_20260923.md` | `740ba897b2f8d3f8234f1df8becf10e12d49ea07076374e8ca016a45be79277d` |

## Related (recorded outside Docs/AI_TEAM/)

| Artifact | sha256 |
|---|---|
| `Docs/147VR_REMAINING_WORK_MAP_20260923.md` | `66f38c7f4d8d2bb49dfe1f279bb7393979eb33322818cc3ea9bd4fda172b9f04` |


Re-issued 2026-09-23 (same session): the self-referential row for this index file was removed.
Reason: a row written while the file is still being generated cannot ever verify. See
`COACH_WORKORDER_004_INTEGRATION_LINE_20260923.md` §6 for the disclosure.