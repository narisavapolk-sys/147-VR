# COACH RULING #029 — STOP DIGGING · SYSTEM CLOSE · ART PARALLEL

- **วันที่**: 2026-09-25 · **ผู้ตัดสิน**: Coach · **baseline**: remote tip `a77ae40f35cf86de95f432fd4871a6baf10641ec` (branch `integration/008-m53`)
- **บริบท**: หลัง WO#026/#027/#028 · คำถามจาก owner: *"เอา git ลง เอกสารลง repo จบงานระบบ แล้วไป ART ได้หรือยัง"*

---

## 0. Constraint ที่ยังผูก (ไม่ผ่อน)

| ❌ ห้าม | ✅ ต้อง |
|---|---|
| เปิด Unity ซ้อน · แก้ source/runner/cert/physics/launcher โดยไม่มีการอนุมัติ | `git status --porcelain` ก่อน/หลังทุกงาน |
| ประกาศ REAL10 PASS / Golden โดยไม่มี run | หลักฐานเป็น `.txt/.json` ที่ commit ได้ (**ไม่ใช่ `.log`**) |
| ใช้ current filesystem state attest ช่วงเวลาอดีต (F-TIME-01) | ทุก hash: **64 ตัว + มีคำสั่งที่ผลิตมันกำกับ** + อ้างจาก artifact เท่านั้น |
| อ้าง `7214...`/ค่าที่ไม่มาจาก artifact · อ้าง "chain ปิด" | ประกาศ **build** ก่อนรันทุกครั้ง |
| "ถูกตัด" ≠ "crash" ≠ "hang" ปนกัน | gate ที่ TestRunner / JSON `status` เท่านั้น |

---

## 1. FORENSIC CLOSURE — ⚠ **§1 ถูกแทนที่ด้วย** `COACH_CLOSURE_030_FORENSIC_CLOSED_20260925.md`
> เอกสารนี้เขียนก่อน ORD-30 ⇒ ข้อ "F5a‴ ยังไม่ให้ผ่าน" ใน §1 **ล้าสมัย** · คำตัดสินสุดท้ายของ F5a‴ = `NOT ESTABLISHED` + **ปิดสายสืบสวน** ตาม CLOSURE_030 · §2–§5 ของเอกสารนี้ **ยังมีผลตามเดิม**

### 1.1 รับรอง (ยึดได้)

```text
F5a′  lifecycle halt หลัง Cue authority (L1588) · process ยังเขียน housekeeping — SUPPORTED
F5a″  pre-FIRE session degradation — SUPPORTED (order-invariant: 146.9× / 42.7× / 44.9× ผ่านหมด)
<60 physics steps หลัง FIRE — CONDITIONALLY ESTABLISHED (clock-free; margin 4 บรรทัดจากสมอ Cue)
TIMEOUT = UNKNOWN · stall/hook-exit/native-crash = NOT ESTABLISHED
F-TIME-01  current-state substitution = invalid · mtime sweep = survivor set (26a 887 = lower bound)
F-27-1  offset +18 คงที่ทุกขั้น (BeginShot 12→30 · StartState 27→45 · Cue 42→60)
F-27-2  Logs\ = บันทึกประวัติศาสตร์ที่รอดชีวิตชุดเดียวของเฟส 13:17–13:29
F-TIMER-01  Run() → log LastWrite = 7,200.9 s ≈ 2h00m00.9s — HYPOTHESIS
```

### 1.2 ⛔ ยังไม่ให้ผ่าน: `F5a‴ = SUPPORTED`

หลักฐานที่ยกมา (`UnityBatch_20260924_193849.log` → `CodeReloadManager` / `MonoManager ReloadAssembly` / `ScriptCompilation requested` / `Compiling Scripts` / Bee → ScriptAssemblies) **ขัดกันเองเรื่องเวลา**:

```text
ชื่อไฟล์ 193849 (local +02:00)      ⇒ 17:38:49Z
LastWrite 17:44:13Z (= 19:44:13 local) ⇒ session ยาว 5m24s   ✓ สอดคล้องกัน
แต่ข้อความที่ยกมา: "session start 13:38:50Z"  ⇒ 15:38:50 local
   ⇒ ต่างจากชื่อไฟล์ 4h00m01s และขัดกับ session 5m24s        ✗
```

**ผลที่ถูกต้องตามหลักฐานชุดนี้**
- ✅ **ปิดครึ่งสาเหตุของ F-TIME-01**: session 19:38:49 local เป็นตัวที่ compile แล้วเขียน `Library/ScriptAssemblies/*.dll` ⇒ อธิบาย mtime 17:43 ⇒ **28e (USN Journal) ไม่จำเป็นจริง** ✓
- ❌ **ยังไม่วาง compile ไว้ "ในหน้าต่างของ target session" (12:45:08Z–15:17:15Z)** ⇒ **F5a‴ = NOT ESTABLISHED (ค้าง)**

**ตัวปิดที่แท้จริง (ORD-29a — คำสั่งเดียว ฟรี):** grep ตัวอักษรชุดเดียวกันใน **log ของ target เอง**

```powershell
Select-String -LiteralPath "Docs\UnityBatchLogs\UnityBatch_20260924_144329.log" `
  -Pattern 'CodeReloadManager|ReloadAssembly|ScriptCompilation|Compiling Scripts|Reloading assemblies|ScriptAssemblies|Bee' |
  ForEach-Object { "L{0}: {1}" -f $_.LineNumber, $_.Line }
```

- **พบ** ⇒ F5a‴ SUPPORTED (มี recompile/domain reload ในตัว target session เอง) — พร้อมเลขบรรทัดให้วางตำแหน่ง halt
- **ไม่พบ** ⇒ **F5a‴ FALSIFIED** ในรูปแบบ recompile (สมเหตุสมผลเพราะ log `193849` ของคุณเองก็บันทึกเส้นเหล่านี้ ⇒ Unity บันทึก assembly reload ทุกครั้ง) ⇒ แล้วสายสืบสวนนี้ **ปิดจริง** และสาเหตุ halt = UNKNOWN ที่มีขอบเขต

**ORD-29b** (ประกอบ): แสดงบรรทัด `Date:` ของ `193849` แบบ verbatim + บรรทัด reload พร้อม timestamp ของมันเอง ⇒ เคลียร์ความขัดกัน 4 ชั่วโมง

### 1.3 Disposition
> **"ปิดได้"** ในความหมาย: หยุดขุดย้อนหลัง **หลัง** 29a/29b ให้ผล · ส่วนที่เหลือ (สาเหตุ halt) ประกาศเป็น **UNKNOWN ที่ควบคุมได้** ไม่ใช่ "SOLVED" — และควบคุมด้วย §3

---

## 2. SYSTEM CLOSE — 7 รายการ

| # | รายการ | สถานะ | ต้องทำอะไร | Gate / Owner | ปิดได้ตอนนี้? |
|---|---|---|---|---|---|
| 1 | **F-PROV-01** | OPEN — **ปิดย้อนหลังไม่ได้โดยโครงสร้าง** | อย่าไล่ปิด · ประกาศเป็น `UNCLOSABLE / ACCEPTED RISK` + ผูกกับ **FORWARD CLOSURE** (§3) แทน | Coach + owner รับทราบ | ✅ **disposition ได้เลย** |
| 2 | **F-HOOK-01** | STANDS — **เป็น device-gate blocker จริง** | (a) เคลียร์ flag `M5_REAL10_ACTIVE` / `M5_FINAL10_ACTIVE` **ก่อนทุก run** (b) guard hook ที่ unguarded หรือย้ายเข้า asmdef (c) `_Archive/Editor_Legacy` ต้องออกจาก `Assembly-CSharp` **ก่อน player build** | ก่อน GATE 4 / APK | ⚠ **ไม่ใช่ตอนนี้** แต่ต้องในเส้นทาง S6→device |
| 3 | **S5** (REAL10 บน integration line) | UNKNOWN | ผ่าน **pre-run checklist** (§3) + `-NoQuit` patch + cert-hash-before + restore protocol · **ประกาศ build = 12f1** และ **ห้ามอ้าง build discriminator** (Library ปนเปื้อน ⇒ controlled comparison ยัง UNTESTED) | owner อนุมัติ invoke | ⚠ **หลัง §3 เท่านั้น** |
| 4 | **008 authorization** | NOT AUTHORISED | ลำดับ 8 ขั้น: ถัดไป = Contract revision → review → read-only measurement → review → non-destructive staging · ⚠ **แก้ rail ต้องทำพร้อม 008 + เพิ่ม Golden case** ไม่งั้น REAL10 เสียเปล่า · 🐞 ค้าง: กระเป๋ากลางถูก rail ปิดกั้น + ช่องโหว่ 0.34 ม. | owner | ❌ รอ staging |
| 5 | **Pack v10** | BLOCKED (owner-declared) | ⛔ ผมไม่แตะช่องทางส่งเอง · **blocker จริงที่ต้องแก้ก่อน**: commit ถูกระบบ execution block ⇒ WO027/WO028 ยังเป็น `??` (ยืนยันแล้ว: **ไม่มีบน remote**) ⇒ ต้องให้ commit/push ผ่านก่อน | owner + Luna | ❌ ต้องแก้ที่เครื่อง |
| 6 | **Final system gate** | ยังไม่เริ่ม | หลัง S5 + 008 ผ่าน | owner | ❌ |
| 7 | **APK / Quest validation** | ยังไม่เริ่ม · **Quest hardware ยังไม่เคยรันเลย** | ต้องผ่าน F-HOOK-01 + 008 ก่อน | owner | ❌ |

### 2.1 📌 Blocker ที่ต้องแก้เป็นอันดับ 1 (ไม่ใช่เทคนิค แต่เป็นกระบวนการ)
```text
Artifacts/M5_3/WO027_LUNA_ORD27_PARTIAL_FORENSICS_20260924.txt   →  ??
Artifacts/M5_3/WO028_LUNA_ORD28_FORENSICS_20260924.txt           →  ??
remote tip = a77ae40f...  (ไม่มี artifact ทั้งสอง ⇒ LOCAL ONLY / UNVERIFIED)
```
ตามกติกาของเราเอง: **หลักฐานที่ยังไม่ commit/push = LOCAL ONLY / UNVERIFIED** ไม่ใช่ FAIL และไม่ใช่ PASS · ⇒ **ห้ามอ้างผลของ WO027/WO028 ในเอกสารปิดงานใด ๆ จนกว่าจะ push** (และห้ามหลบ safety ด้วยวิธีอื่น — การตัดสินของคุณถูกต้อง)

**คำสั่งสำหรับ Luna (ผม push เองไม่ได้ — ทดสอบแล้ว: `could not read Username for 'https://github.com'`; ไม่มี `~/.git-credentials`/`GH_TOKEN`/`GITHUB_TOKEN`)**
```powershell
Set-Location "C:\Users\mongo\UnityProjects\147 VR"
git add Artifacts/M5_3/WO027_LUNA_ORD27_PARTIAL_FORENSICS_20260924.txt `
        Artifacts/M5_3/WO028_LUNA_ORD28_FORENSICS_20260924.txt `
        project/147VR/COACH_RULING_029_SYSTEM_CLOSE_ART_PARALLEL_20260925.md
git commit -m "Coach ruling 029: forensic closure + system-close plan + ART parallel boundaries"
git push origin HEAD:integration/008-m53
git rev-parse HEAD
```
จากนั้นรายงาน `git rev-parse HEAD` + `git ls-tree -r --name-only HEAD -- Artifacts/M5_3` + `git status --porcelain` (ต้องว่าง)

---

## 3. PRE-RUN INSTRUMENTATION GATE (บังคับ — ก่อน S5 ทุกครั้ง)

> นี่คือกลไกที่ **เปลี่ยน "UNKNOWN" ให้เป็น "ควบคุมได้"** โดยไม่ต้องขุดย้อนหลังอีก — ถ้าไม่มี §3 การไป S5 เท่ากับเสี่ยงเข้าหลุมดำรอบสอง

```text
(1) sha256 ของ "ทั้งสอง" runner (TMP + Final10)  เหตุผล: [InitializeOnLoad] — ตัวที่ไม่ตั้งใจรันก็โหลด
(2) Unity.exe full path + version string        (ต้องประกาศ build ให้ตรงกับที่อ้าง)
(3) sha256 + mtime ของ Library/ScriptAssemblies/*.dll
(4) invocation string เต็ม (ห้ามตัด -NoQuit ออก)
(5) ★ sampler: working set + ขนาด log file ทุก 60 s  ← F5 บังคับตั้งแต่ WO#020 และคือสิ่งที่ขาดมาตลอด
(6) cert hash ก่อนรัน → ถ้าไม่ถึง Finish() ⇒ restore จาก HEAD (ห้าม commit RUNNING ทับ)
(7) เคลียร์ flag M5_REAL10_ACTIVE + M5_FINAL10_ACTIVE (unbidden-run hazard)
(8) หลังจบ: log hash + ประกาศชัดว่าถึง `Finish()` หรือไม่ (ห้ามอนุมานจาก exit code ของ launcher)
```
**เกณฑ์**: ถ้า run หยุดอีก sampler จะบอกได้ว่า *process ยังทำงานอยู่หรือไม่* จาก **อัตราโตของ log** — ตัววัดที่ 26a ทำแทนไม่ได้ย้อนหลัง (mtime ตรวจ append ไม่ได้) ⇒ **นี่คือเหตุผลเชิงเทคนิคที่ต้องหยุด forensic และไปต่อด้วยการวัดล่วงหน้า**

---

## 4. ART PARALLEL — เริ่มได้ทันที (มี 3 เงื่อนไข)

### 4.1 ✅ ทำได้ (ไม่แตะ authority)
Table visual polish (ผ้า/ขอบไม้/ซับในกระเป๋า) · Materials **แบบ asset override** · Lighting / environment / reflection probe / post-processing · Cue & ball *appearance* · UI presentation · Menu / branding · VFX ที่ไม่เปลี่ยน gameplay authority

### 4.2 ⛔ ห้ามแตะ (locked จนกว่า system gate ผ่าน)
- `Assets/Scripts/AAA/**` (76 tracked .cs — Cue, CuePhysicsAdapter, physics) · shot lifecycle · turn authority · rules
- calibration chain: **`SnookerPhysicsSetup.MakeMaterial()` ↔ `TableSurfaceController`** — ⚠ **REV1 §1.5: แก้ฝั่งใดฝั่งหนึ่ง = invalidate calibration chain ⇒ ต้อง re-certify + REAL10 ใหม่**
- collider geometry · transform ของโต๊ะ/ลูกบอล · physics component ใน `147VR_MainScene`
- Golden cases · REAL10 cert · runner · launcher

### 4.3 ⚠ กับดักที่ต้องระบุชื่อ (เพราะ "Materials" คือรายการ ART ที่ฆ่าระบบได้เงียบ ๆ)
> **"แก้ material ของโต๊ะ/ลูกบอล" ต้องทำเป็น `Material` asset / renderer override — ห้ามทำผ่าน `SnookerPhysicsSetup.MakeMaterial()`** เพราะนั่นคือฝั่ง physics ของคู่ที่ REV1 §1.5 ประกาศว่าถ้าแก้จะทำให้ calibration chain เป็นโมฆะ

### 4.4 กลไกกันขอบ (บังคับกลไก ไม่ใช่บังคับวินัย)
1. งาน ART อยู่ใต้ `Assets/Art/**` (ถ้าต้องมีสคริปต์ → asmdef ของตัวเอง)
2. ใช้ **prefab variant / prefab override** เป็นชั้นภาพ ไม่แก้ transform ของ object ที่มี collider
3. **ถ้าจำเป็นต้อง save `147VR_MainScene`** ⇒ diff ต้องถูกตรวจว่าไม่มี physics component / transform ของโต๊ะ-ลูกถูกแก้
4. **pre-commit path check** ก่อน commit ทุกครั้งของ ART:
```powershell
git diff --name-only --cached | Select-String -Pattern 'Assets/Scripts/AAA/|Golden|Certification|Final10Runner|Unity_Batch_Safe|147VR_MainScene'
```
⇒ ถ้ามีผลออกมา **หยุดและ escalate** อย่า commit
5. ❌ **ห้าม re-run REAL10 เพื่อ ART** · ART ไม่กิน Golden และไม่ supersede cert ใด ๆ

---

## 5. คำตอบตรง ๆ ต่อคำถาม owner

```text
งาน "สืบสวน"      → ปิดได้ · หลัง ORD-29a/29b (คำสั่งเดียว) ⇒ หยุดขุด
งาน "ระบบ"        → ยังไม่จบ · แต่ไม่ใช่หลุมดำ — งานที่เหลือคือ 7 รายการที่มี gate/owner ชัด (§2)
ART               → เริ่มขนานได้ทันที วันนี้ ภายใต้ §4
เงื่อนไขที่ห้ามข้าม 3 ข้อ:
   (i)  ORD-29a/29b ให้ผลก่อน ถือว่าสาย F5a‴ ปิดสมบูรณ์
   (ii) commit/push WO027+WO028 (§2.1) ก่อนอ้างผลในเอกสารใด ๆ
   (iii) ART ห้ามแตะ §4.2 และต้องผ่าน pre-commit path check
```

## 6. State lock
```text
FORENSICS   F5a′ SUPPORTED · F5a″ SUPPORTED · F5a‴ NOT ESTABLISHED + BRANCH CLOSED (ORD-30 controlled negative) → ดู CLOSURE_030
SYSTEM      F-PROV-01 = UNCLOSABLE/ACCEPTED (§3) · F-HOOK-01 STANDS (device-gate) · S5 UNKNOWN (หลัง §3)
            008 NOT AUTHORISED · Pack v10 BLOCKED · final gate / APK NOT STARTED
ART         CAN START IN PARALLEL ภายใต้ §4.1–4.4 — ห้ามแตะ §4.2
UNCHANGED   ไม่มี Unity run · ไม่มี REAL10 rerun · ไม่มี source/physics/cert mutation
```
