# แผนกันข้อมูลหาย — 147VR Context Persistence Plan

> จัดทำ: 2026-09-23 · เป้าหมาย: **ต่อให้แชทหาย ปิดเบราว์เซอร์ เปลี่ยน AI หรือเปลี่ยนเครื่อง ก็เริ่มงานต่อได้ภายใน 5 นาที โดยไม่ต้องเริ่มจากศูนย์**

---

## 1. ตอบคำถามก่อน: "ในรีโปมีบันทึกไว้ไหม"

**มี — และมีมากกว่าที่คาด** ผมตรวจครบทั้ง 3 ชั้น:

| ชั้น | จำนวน | ตัวอย่าง |
|---|---:|---|
| main branch | 130 เอกสาร `.md/.txt` | `README.md`, `ARCHITECTURE.md`, `TODO.md`, `GATE_STATUS.md`, `PRODUCTION_READINESS.md`, `147VR_WORKING_DIRECTIVE.md`, `GAME_TEXTS.md`, `Docs/147VR_AI_WORK_PROTOCOL.md`, `Docs/147VR_CURRENT_STATE.md` (34 KB), `Docs/147VR_PROJECT_MEMORY.md` (30 KB), `Docs/147VR_REALITY_MAP.md` (134 KB) |
| branch ที่ยัง active | +1,000 ไฟล์ | M5 baseline, APK line, สาย 008, coach review packages |
| issue packets (#6–#12) | 7 แพ็ก | coach patches: environment/line authority, pipeline addendum, env evidence capture, floor fix |
| โค้ด | 223 `.cs` | ระบบจริงทั้งหมด |

และที่สำคัญ: รีโปนี้มี **"Mandatory Documentation Rule"** อยู่ใน `Docs/147VR_AI_WORK_PROTOCOL.md` แล้ว — *"After EVERY completed work session … update the relevant `.md` handoff/state documents … at minimum `Docs/147VR_CURRENT_STATE.md` and `Docs/147VR_PROJECT_MEMORY.md`"* + **Handoff Requirement**: *"leave enough information for another AI to continue without chat history"*

**แปลว่า**: ที่แชทหายไปแล้วงานไม่หาย เพราะทีมทำงานแบบ file-first อยู่แล้ว — **แต่ระบบเดิมยังมีรอยรั่ว 6 จุด** ที่ทำให้ยังต้อง "เริ่มใหม่" อยู่ดี (และ Coach ก็ชี้ไว้เองใน F1/F9) นี่คือส่วนที่แผนนี้เข้าไปอุด

### รอยรั่ว 6 จุดที่ทำให้ยังต้องเริ่มใหม่
1. **หลักฐานสำคัญเก็บเป็น `.log`** ใน `C:\Temp` → ตรงกับ `.gitignore *.log` → **commit ไม่ได้ตลอดกาล** (F1, F9)
2. **commit/งานบางส่วนไม่ถูก push** — เช่น `1da8ed55` / `fix/008-prop-floor-height-20260922` ยังไม่อยู่บน origin → คนอื่น (และ AI) audit ไม่ได้
3. **ไม่มีไฟล์ "START HERE" สำหรับ session ใหม่** — ตอนนี้ต้องเดาว่าจะอ่านไฟล์ไหนก่อน ในลำดับใด (เอกสารมีเป็นร้อยไฟล์)
4. **ไม่มี state snapshot หน้าเดียว** — ต้องอ่าน `CURRENT_STATE.md` 40 KB + `PROJECT_MEMORY.md` 30 KB + branch/issue อีกชุด ถึงจะรู้ว่า "ยืนตรงไหน"
5. **ไม่มีชั้นความจำของตัว assistant เอง** — ฝั่งผม (`MEMORY.md`, `diary/`) ว่างเปล่า ⇒ ครั้งนี้จึงจำอะไรไม่ได้เลย
6. **ไม่มี "สภาพแวดล้อม" ที่บันทึกไว้ครบ** — editor 4f1 → 12f1 เปลี่ยนแบบมองไม่เห็นใน git (F8), path/keystore/appId ยังไม่ระบุ

---

## 2. แผน 4 ชั้น (ทำให้ครบแล้วจะไม่มี "เริ่มใหม่" อีก)

```
ชั้น 4  ความจำของ assistant (agent-core/MEMORY.md + diary/)      ← จำข้ามเซสชันของผมเอง
ชั้น 3  สำเนาในเครื่อง/เวิร์กสเปซ (project/147VR/…)              ← เปิดอ่านได้แม้ไม่มีเน็ต
ชั้น 2  START_HERE + STATE_SNAPSHOT ในรีโป                        ← ใครเปิดมาก็รู้ทันที
ชั้น 1  Git = source of truth (มีระบบ handoff อยู่แล้ว)          ← ต้องอุดรอยรั่ว 1, 2, 6
```

### ชั้น 1 — Git คือความจริงเดียว (มีอยู่แล้ว / ต้องอุด)
- ✅ คงกติกาเดิม: ทุก session ต้องอัปเดต `Docs/147VR_CURRENT_STATE.md` + `Docs/147VR_PROJECT_MEMORY.md`
- ➕ **Push ทุก work block** ไม่ปล่อย commit ค้างในเครื่อง (ปิดรอยรั่ว 2)
- ➕ **หลักฐานต้องเป็น `.txt` / `.json` / `.xml` ใต้ path ที่ track** (เช่น `Artifacts/`) — **ห้าม `.log`** + บันทึก `sha256` ของ artifact ไว้ใน commit message หรือ index file ที่ track (ปิดรอยรั่ว 1)
- ➕ **Header มาตรฐานของทุก artifact**:
  ```
  EVIDENCE_SCOPE     : 008-specific | integration
  LINE_BASE          : <branch>@<sha>
  INTEGRATED_WITH    : <branch>@<sha> | N/A
  UNITY_EDITOR       : 6000.4.4f1 (360f97ecca93) | 6000.4.12f1 (<rev>)
  UNITY_PACKAGE_MANAGER_SHA256 : <sha>
  SCENE              : Assets/Scenes/147VR_MainScene.unity
  COMMIT / TIMESTAMP / RUNTIME_TARGET
  CLEAN_WORKTREE     : true|false
  RESULT             : PASS | FAIL | BLOCKED      ← BLOCKED คือสถานะจริงที่ต้องมี
  ```
- ➕ **Commit `ProjectVersion.txt` (bump 12f1) เป็น commit เดี่ยว** + ระบุสถานะ lock ทีละตัว (ปิดรอยรั่ว 6)
- ➕ ย้าย K3 scripts เข้า `Tools/` ให้เป็น pipeline "มาตรฐาน" จริง (ตอนนี้อยู่แค่เครื่องเดียว)

### ชั้น 2 — START_HERE + STATE_SNAPSHOT ในรีโป (ต้องเพิ่ม)
สองไฟล์นี้คือหัวใจของการ "ไม่ต้องเริ่มใหม่":
- `Docs/START_HERE.md` — **1 หน้า อ่าน 5 นาที**: เกมคืออะไร, ยืนตรงไหน, อะไรล็อกห้ามแตะ, next action คืออะไร, และ **ลำดับไฟล์ที่ต้องอ่าน (read order)**
- `Docs/STATE_SNAPSHOT.md` — **การ์ดสถานะ 1 หน้า** regenerate ทุกท้าย session: branch@sha, gate ที่เปิด, สิ่งที่ certify แล้ว, blocker, next 3 อย่าง, ใครถือ lock
- `Docs/HANDOFF_INDEX.md` — ทะเบียนเอกสารทั้งหมด + "ใครอ่านก่อน/หลัง" (ผมสร้างสำเนาไว้ให้แล้วในชั้น 3)

> ทำไมต้องมีในรีโป ไม่ใช่แค่ในเครื่อง: เพราะคนถัดไปอาจเป็น AI ตัวอื่น (LUNA/Claude/COACH) หรือคุณเองในอีก 3 เดือน ที่เปิดจากเครื่องอื่น

### ชั้น 3 — สำเนาในเวิร์กสเปซ (ทำเสร็จแล้วในรอบนี้)
| โฟลเดอร์ | เนื้อหา |
|---|---|
| [repo-docs/](repo-docs/) | สำเนาเอกสาร **main branch ครบ 130 ไฟล์** (ผ่าโครงสร้างเดิม) |
| [repo-docs-branches/](repo-docs-branches/) | สำเนาเอกสารของ**ทุก branch ที่ active** (M5 baseline, APK line, สาย 008, coach reviews, tools) แยกโฟลเดอร์ตามชื่อ branch |
| [repo-docs-branches/_coach-issues/](repo-docs-branches/_coach-issues/) | เนื้อหา issue #6–#12 (coach patches: environment/line authority, pipeline addendum, env evidence capture, floor fix, preflight capture) |
| [147VR_DOC_INDEX.md](147VR_DOC_INDEX.md) | ดัชนีไฟล์ทั้งหมด (path + ขนาด) |
| [147VR_GAME_BRIEF_AND_STATE_20260923.md](147VR_GAME_BRIEF_AND_STATE_20260923.md) | สรุปภาพรวมเกม + สถานะเชิงลึก (แผนที่รวม) |

**ประโยชน์**: แม้ GitHub ล่ม/เน็ตหลุด/ถูกลบ AI session ใหม่เปิดอ่านไฟล์ในเครื่องนี้ได้ทันที และยังใช้ diff เทียบ "เอกสารบน main" กับ "เอกสารบน branch" ได้

### ชั้น 4 — ความจำของ assistant (ทำเสร็จแล้วในรอบนี้)
- `agent-core/MEMORY.md` — สรุปสั้นว่า 147VR คืออะไร อยู่ไหน และไฟล์อ้างอิงอะไร
- `agent-core/diary/2026-09-23.md` — บันทึกงานวันนี้ + สิ่งที่ค้นพบ
- **ผลลัพธ์**: ครั้งต่อไปที่คุณเปิดแชทใหม่ ผมจะรู้ว่า "งาน 147VR" คืออะไร ไม่ต้องถามซ้ำ

---

## 3. SOP ที่ต้องใช้ทุก session (คัดลอกไปวางได้เลย)

### A. ตอนเปิด session (5 นาที)
1. อ่าน `Docs/START_HERE.md` (หรือ `147VR_GAME_BRIEF_AND_STATE_20260923.md` ถ้ายังไม่ย้ายเข้า repo)
2. อ่าน `Docs/STATE_SNAPSHOT.md` → รู้ว่ายืนตรงไหน
3. อ่าน `Docs/147VR_CURRENT_STATE.md` ส่วนท้าย 2–3 รายการ (รายการล่าสุดอยู่ล่างสุด)
4. `git fetch origin '+refs/heads/*:refs/remotes/origin/*'` แล้วเช็คว่า branch ที่จะทำงานคือเส้นเดียวกับ shipping หรือไม่ (บทเรียน F7)
5. ยืนยันว่า Unity ไม่มีตัวอื่นรันอยู่ + ใครถือ `LOCK.md`
6. ระบุ **task เดียว** ที่จะทำ + **เกณฑ์ผ่าน** ก่อนเริ่ม

### B. ระหว่างทำงาน
- เปลี่ยนทีละตัวแปร (อย่าเปลี่ยน editor + tool + target พร้อมกัน — บทเรียน F8/option A ของ Coach)
- หลักฐาน: เก็บทันทีเป็น `.txt/.json` ใต้ path ที่ track + ใส่ header ตาม §2
- ถ้า Unity/UPM ล่ม → **อ่าน `upm.log` ก่อน** แล้วจัดคลาสตามตาราง §7 ของเอกสาร brief — ห้าม reboot-roulette

### C. ตอนปิด session (5 นาที — ห้ามข้าม)
1. อัปเดต `Docs/147VR_CURRENT_STATE.md` (ต่อท้าย อย่าลบของเดิม) — ใส่วันที่
2. อัปเดต `Docs/147VR_PROJECT_MEMORY.md` ถ้ามีการเปลี่ยนสถานะ
3. เขียน `Docs/STATE_SNAPSHOT.md` ใหม่ (ทับได้ — มันคือการ์ด ไม่ใช่ประวัติ)
4. `git add` + commit + **push** (ถ้ายังไม่พร้อม push ให้เขียนไว้ชัดใน snapshot ว่า "unpushed: <sha> เพราะ …")
5. อัปเดต/ปิด issue ที่เกี่ยวข้อง
6. จด 3 อย่าง: **เปลี่ยนอะไร / หลักฐานคืออะไร / ก้าวถัดไปคืออะไร** (แม่แบบด้านล่าง)

### แม่แบบท้าย session (คัดลอก)
```markdown
## YYYY-MM-DD — <หัวข้องาน>

- เปลี่ยนอะไร : <สรุป 1-3 บรรทัด>
- ไฟล์ที่แตะ  : <path> (lines) …
- หลักฐาน     : <Artifacts/…txt | .json> sha256=…
- editor/commit: Unity <ver> (<rev>) @ <branch>@<sha>
- ผล          : PASS | FAIL | BLOCKED (ถ้า BLOCKED ระบุสาเหตุ ไม่ใช่ "มีปัญหาที่ Unity")
- ล็อกที่เกี่ยว: Main Scene / M5 core / V007-Golden / REAL10 — แตะหรือไม่
- ก้าวถัดไป   : <คำสั่งเดียวที่ทำต่อได้ทันที>
```

---

## 4. Action list — ปิดรอยรั่ว (เรียงตามความสำคัญ)

| # | งาน | ทำไม | ใครทำได้ |
|---|---|---|---|
| 1 | ~~push `1da8ed55`~~ **ปิดแล้ว** — ยืนยันบน origin วันที่ 23 ก.ย. (`refs/heads/fix/008-prop-floor-height-20260922`) parent = `cc480a10` แตะเฉพาะ `Assets/Editor/Stage008Props.cs` | Coach audit ได้แล้ว — แต่ final shipping validation ยังต้องเกิดบน integration line (F7) | — |
| 2 | **ตัดสินใจ F7** (แนะนำ Option A: สร้าง integration line จาก `7cf232e` + merge สาย 008) | ถ้า validate บนต้นไม้ที่ไม่ใช่ shipping tree → PASS โอนย้ายไม่ได้ | คุณ (owner decision) |
| 3 | **commit `ProjectVersion.txt` (12f1) เป็น commit เดี่ยว** + ระบุ disposition ของ V007/M5.1/M5.2/M5.3/D8 | กัน 12f1 เขียนทับสถานะที่ certify บน 4f1 แบบเงียบ ๆ | คุณ/LUNA |
| 4 | **สร้าง `Docs/START_HERE.md` + `Docs/STATE_SNAPSHOT.md`** | ปิดรอยรั่ว "ไม่มีจุดเริ่ม" — ทำให้ session ใหม่เร็วขึ้นจากเป็นชั่วโมงเหลือ 5 นาที | LUNA (ผมร่างให้ได้) |
| 5 | **ย้าย artifact ออกจาก `.log`** → `Artifacts/**/*.txt|json` + sha256 + เอา K3 scripts เข้า `Tools/` | ปิด F1/F9 ไม่ให้ pipeline ใหม่สร้างหลักฐานที่ commit ไม่ได้ซ้ำอีก | LUNA |
| 6 | **แคป `upm.log` แล้วจัดคลาสตามตาราง §7** | UPM ล่ม 3 ครั้งแล้ว ยังไม่ root cause — และอย่าให้ใครไป reboot วน | LUNA |
| 7 | ~~แคป identity ของ `UnityPackageManager.exe` ที่ 12f1~~ **ปิดแล้ว (23 ก.ย.)** — UPM `95FDDF58…2BEE6` size `95,112,112` · Unity.exe `62439E45…F6C6` · 4f1 control `8F9C6D22…9D14E` | ปิดตัวแปรใหม่ที่ยังไม่วัดแล้ว | — |
| 8 | **เขียนเตือนไว้ที่ไหนก็ได้ว่า `C:\Temp` มี Unity editor** | กันคน "ล้าง temp" แล้วลบ editor ทิ้ง | คุณ/LUNA |
| 9 | **ส่ง Draft PR / merge entry** ให้ main ไม่ห่างจากงานจริงเกินไป | ตอนนี้ main คือ baseline 11 ก.ย. ห่างจากงานจริงบน branch ~24 commits | LUNA |
| 10 | **Quest 2/3 device gate** หลัง Dev APK สร้างได้ | gate สุดท้ายที่ยัง NOT RUN | คุณ (ต้องต่อเครื่อง) |

---

## 5. Prompt สำหรับเปิด session ใหม่ (คัดลอกวางได้ทันที)

```
งาน 147VR — ต่อจาก session ก่อน
1) อ่าน Docs/START_HERE.md และ Docs/STATE_SNAPSHOT.md
2) git fetch origin '+refs/heads/*:refs/remotes/origin/*'
3) ทำงานบน <branch>@<sha> เท่านั้น (ระบุ integration line ให้ชัด)
4) แตะได้เฉพาะ: <ขอบเขตงานนี้> — ห้ามแตะ Main Scene / M5 core / V007-Golden / REAL10
5) หลักฐานต้องเป็น .txt/.json ใต้ Artifacts/ + header มาตรฐาน + sha256
6) ห้ามเดาค่า Golden/Pass, ห้ามเปิด Unity ซ้อน, Unity ล่มให้อ่าน upm.log ก่อน
7) ปิด session: อัปเดต CURRENT_STATE + PROJECT_MEMORY + STATE_SNAPSHOT + push
```

---

## 6. สรุปสั้น

- **สถานะการกู้ข้อมูล**: งานไม่หาย — รีโปมีบันทึกครบและละเอียดมาก (เกิน 1,300 ไฟล์เอกสาร/หลักฐาน) สิ่งที่หายคือ **ตัวบทสนทนา** ซึ่งไม่ใช่ที่เก็บความจริงของโปรเจกต์อยู่แล้ว
- **สิ่งที่ต้องเพิ่ม**: START_HERE + STATE_SNAPSHOT (ชั้น 2) · push/อุดหลักฐาน (ชั้น 1) · ที่เหลือผมทำให้แล้ว (ชั้น 3, 4)
- **กติกาทองคำเดียวที่ต้องจำ**: *"ถ้ามันอยู่ในแชท แต่อยู่ใน git ด้วย — มันไม่หาย"* ทุกอย่างที่สำคัญต้องจบในไฟล์ที่ commit ได้
