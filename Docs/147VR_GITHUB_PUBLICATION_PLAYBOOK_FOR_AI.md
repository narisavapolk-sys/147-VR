# 147 VR — GitHub Publication Playbook for AI

**Purpose:** ให้ AI ตัวถัดไปรู้วิธีเตรียมและส่ง 147 VR เข้า GitHub โดยไม่ต้องเดา workflow หรือเสี่ยงส่งผิด

## 1. Non-Negotiable Rules
- Active Project: `C:\Users\mongo\UnityProjects\147 VR`
- Unity: `6000.4.4f1`
- ห้าม `git add .`
- ห้าม commit / push / remote add โดยไม่ได้รับคำสั่งจาก owner
- ห้าม delete/move asset, แก้ Scene, หรือแก้ Unity logic ระหว่าง Publication Prep
- ห้ามแตะ CUEWARP/original projects
- ต้อง preserve Unity `.meta` คู่กับ asset ที่ publish
- ห้ามใช้ชื่อไฟล์อย่างเดียวตัดสินว่าเป็น junk; ตรวจ reference/role ก่อน

## 2. Publication Architecture
ใช้ **separate Publication Prep copy** แทนการ staging ใน Active Project โดยตรง:
`147 VR_PUBLICATION_PREP_YYYYMMDD_HHMMSS\WORKING_SOURCE_SNAPSHOT`

Prep repo ต้องเป็น Git repo แยก และยังไม่ต้องมี remote จนกว่า owner อนุมัติ

## 3. Audit Before Staging
ตรวจ Git status, `.gitignore`, `.gitattributes`, reachable history, secrets,
large historical blobs, backup/temp/render/MCP artifacts, Unity references,
Build Settings และ source-vs-backup role ก่อน staging

ห้ามสรุปว่า history สะอาดจากการตรวจเฉพาะ working tree

## 4. Git LFS Policy
`*.blend`, `*.blend1`, `*.fbx` และ binary asset rules ที่ project กำหนด ใช้ Git LFS
ก่อน staging ตรวจว่า `.gitattributes` มี LFS rule ที่ถูกต้อง

`git add` ของ LFS อาจใช้เวลานานเพราะคำนวณ SHA256 และเขียน object ลง `.git/lfs/objects`
**ห้าม kill Git/Git-LFS เพียงเพราะ command timeout**

ถ้า timeout: ตรวจ git/git-lfs process และ `.git/index.lock` ก่อน
ถ้ายังทำงานอยู่ ให้รอจนเสร็จ
ถ้า process หายและ lock เป็น false ให้ retry `git add` เดิมแบบ idempotent

## 5. Ignore Rules
ต้อง inspect backup candidates ก่อนเพิ่ม ignore pattern
ตัวอย่างที่ผ่านการตรวจแล้ว: `*.bak*`

Production exclusions:
- `Assets/Models/Girls/*SLIM*`
- `Assets/Editor/DebugChubbyImport.cs*`
- `Assets/Editor/ImportChubbyDance.cs*`
- `Assets/Editor/VerifyChubbyFix.cs*`
- `Assets/**/*_PRE_*`

ห้าม blanket-ignore `Assets/Models/Girls/` เพราะยังมี production dependencies

## 6. Chunked Staging Method
Staging ใช้ **foreground sequential chunks** ไม่ใช้ `Start-Process` background
เพราะ session ของ Desktop Commander อาจจบ child process

ลำดับหลัก: Packages → Assets/AAA → Assets/Models → Assets/147 main →
Assets/Editor → Assets/Imports → Assets/Oculus → Assets/Plugins → Assets/Prefabs →
Assets/Resources → Assets/Scenes → Assets/Scripts → Assets/Settings → Assets/Shaders →
Assets/StreamingAssets.meta → Assets/Tests → Assets/XR → InputSystem actions → Docs → approved Blender source

ถ้า chunk ใหญ่ ให้ inspect first-level subdirectories แล้วแบ่งย่อยต่อ
หลังทุก chunk: รอ Git จบ, ตรวจ `index.lock`, แล้วบันทึก cumulative staged count

## 7. Mandatory Gates
Gate E: verify ignore rules, forbidden assets absent, v007 FBX + `.meta` present,
and all must-publish files present

Gate F: staging ครบตาม allowlist, no forbidden files staged, required `.meta` present,
and critical build scenes/dependencies verified

Gate G หลัง chunk สุดท้ายและ **ก่อน commit**:
1. staged count
2. `git lfs status`
3. `git lfs fsck` / valid LFS integrity check
4. large files not using LFS
5. forbidden-file scan
6. must-publish scan
7. `.meta` orphan check
8. secret scan

**Unborn Prep repo:** ถ้ายังไม่มี commit/HEAD, `git lfs fsck` อาจ resolve `HEAD` ไม่ได้
นั่นไม่ใช่หลักฐานว่า LFS เสีย และห้ามประกาศ PASS จากข้อความนั้นเพียงอย่างเดียว

## 8. Current 147 VR Decisions
Visual Source of Truth:
`Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v007.fbx`

Approved Blender source:
`SNOOKER   VR pool table/Blender/147VR_Table_WPBSA_Visual_Clean_v007.blend`

Publish current dancing dependencies because `SampleScene.unity` references them:
- `Chubby Girl Dancing.fbx` + `.meta`
- `Cute Girl Dancing.fbx` + `.meta`
- `CuteDance.controller` + `.meta`
- `CuteDancer.cs`
- `QuestSpawnSetup.cs`

Do not publish confirmed experimental SLIM assets or the three excluded Chubby editor scripts.

## 9. Safety Boundary
Publication Prep is **staging preparation only** until owner explicitly authorizes commit/push.
Never rewrite Git history to solve a large historical blob without a separate approved remediation plan.
Never assume GitHub upload success from local staging alone.

## 10. Handoff Principle
AI ตัวใหม่ต้องอ่านไฟล์นี้ + `147VR_PUBLICATION_MANIFEST_FOR_ASTRA.md` ก่อนเริ่ม Publication Prep

เป้าหมาย: **evidence-driven publication** — ตรวจ → allowlist → stage เป็น chunk → verify → Gate G → รอ owner อนุมัติ commit/push.


## 11. Gate G / Gate H Clarification
- Gate G is the **pre-commit staged-set gate**. Do not require `git lfs fsck` when the Prep repo has an unborn HEAD.
- Gate G must use `git lfs status` and the remaining staged-set integrity checks.
- Gate H is **post-commit verification** and is the correct point for `git lfs fsck`, because HEAD now resolves to the committed tree.
- If `git lfs fsck` cannot resolve HEAD in an unborn repo, record it as a repository-state limitation, not as LFS corruption.

## 12. Forecast Revision — 2026-09-09
- The Playbook under `WORKING_SOURCE_SNAPSHOT/Docs/` was added after the original inventory.
- Chunk 19 `Docs/` forecast is therefore **183 files**.
- Final staged-set forecast is therefore **5,706 files**.
- The duplicate Playbook in the Publication Prep root is outside the allowlist and must remain unstaged.
- The `Docs/` Playbook copy is the intended GitHub handoff document and is part of the 183-file Docs forecast.
