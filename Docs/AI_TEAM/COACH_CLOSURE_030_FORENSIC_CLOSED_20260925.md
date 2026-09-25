# COACH CLOSURE #030 — สาย F5a‴ ปิด · FORENSIC CLOSED

- **วันที่**: 2026-09-25 · **ผู้ตัดสิน**: Coach · **baseline**: remote tip `a77ae40f35cf86de95f432fd4871a6baf10641ec` (branch `integration/008-m53`, ตรวจแล้วไม่ขยับ)
- **อ้างอิง**: `COACH_RULING_029_SYSTEM_CLOSE_ART_PARALLEL_20260925.md` (§2–§4 ยังมีผล · §1 ถูกแทนที่ด้วยเอกสารนี้)
- **สถานะเอกสารนี้**: ปิดสายสืบสวน F5a‴ · **ไม่ใช่** การประกาศปิดงานระบบ

---

## 1. Evidence chain (raw — ตามที่ Luna รายงาน)

### 1.1 ORD-30a — liveness (บังคับก่อนเชื่อผลใด ๆ)
```text
target  Docs\UnityBatchLogs\UnityBatch_20260924_144329.log   exists=True  lines=1606
healthy Docs\UnityBatchLogs\M5_REAL10_FIXED_20260919.log     exists=True  lines=4244
```
⇒ ตรงกับค่าที่ล็อกไว้ (1,606 / 4,244) ⇒ **การอ่านไฟล์ถูกต้อง** ⇒ ผล ORD-29a **ไม่เป็นโมฆะเพราะอ่านไฟล์ผิด** (ต่างจากสมมติฐานตั้งต้นของ Coach — บันทึกไว้)

### 1.2 ORD-30b — control (pattern ชุดเดียวกับ ORD-29a)
```text
TARGET  = 207 matches
HEALTHY = 208 matches
```
```text
HEALTHY  L117 CodeReloadManager initialized
         L122 Begin MonoManager ReloadAssembly
         L263 DisplayProgressbar: Compiling Scripts
         L268 Begin MonoManager ReloadAssembly
         L727 Reloading assemblies for play mode
         L728 Reloading assemblies after forced synchronous recompile

TARGET   L91  CodeReloadManager initialized
         L110 Begin MonoManager ReloadAssembly
         L278 DisplayProgressbar: Compiling Scripts
         L299 Begin MonoManager ReloadAssembly
         L1012 Reloading assemblies for play mode
         L1013 Reloading assemblies after forced synchronous recompile
```
⇒ **signature ชุดนี้เป็น Unity startup / play-mode boilerplate** และ **โปรไฟล์ reload/compile ของ target ≈ healthy (207 vs 208)** ⇒ ไม่มี reload/compile ส่วนเกินใน target
⇒ **ห้ามใช้ signature ชุดนี้เป็นหลักฐานของ F5a‴ ทุกกรณี**

### 1.3 ORD-30c — decisive: หลัง halt
```text
AFTER_1588_COUNT (pattern หลัก) = 0
AFTER_1588_LOOSE (Reload|Compil|Bee|MonoManager) = 0
```
tail หลัง halt ยืนยันตรงกับ 27c: `relay connected` · `connection.established` · `TrimDiskCacheJob` · `Licensing` ×2

### 1.4 ORD-29a (บันทึกเชิงระเบียบวิธี)
ORD-29a ถูกส่งมาเป็น **สองครึ่งที่ขัดกันเอง** (`ไม่พบทุกตัว` → `FALSIFIED` และ `พบ L91–L1229` → `SUPPORTED`) ⇒ Coach **ไม่รับทั้งสอง** และออก ORD-30 · 30a พิสูจน์ว่าการอ่านไฟล์ถูกต้อง ⇒ ความขัดแย้งมาจาก**การรัน/การรายงาน** ไม่ใช่จากตัวไฟล์
**บทเรียน**: รายงานที่ขัดกันเองในข้อความเดียว = สัญญาณให้หยุดและทำ control · **ห้ามเลือกข้างที่ตรงกับสมมติฐานตัวเอง** (นี่คือ occurrence ที่ 7 ของตระกูล "อ้างแรงกว่าหลักฐาน" — ถูกจับได้ก่อนหลุดเข้า state)

---

## 2. คำตัดสินสุดท้าย (ตาม decision rule ที่ประกาศก่อนเห็นผล)

```text
F5a′  lifecycle halt หลัง Cue authority (L1588) · process ยังเขียน housekeeping   SUPPORTED
F5a″  pre-FIRE session degradation · order-invariant (146.9× / 42.7× / 44.9×)      SUPPORTED
F5a‴  recompile/domain-reload เป็นกลไกของ halt                                    NOT ESTABLISHED
      → INVESTIGATION BRANCH CLOSED
F-TIME-01  current-state substitution for historical state = INVALID               SUPPORTED
F-TIMER-01 2h00m00.9 s จาก Run() ถึง log LastWrite                                 HYPOTHESIS / UNKNOWN
<60 physics steps หลัง FIRE (clock-free, margin 4 บรรทัด)                          CONDITIONALLY ESTABLISHED
stall · hook-exit · native crash                                                   NOT ESTABLISHED
TIMEOUT                                                                           UNKNOWN
```

**เหตุผลที่ F5a‴ ไม่ใช่ SUPPORTED และไม่ใช่ FALSIFIED**: หลักฐานเดียวที่ใช้ได้ (signature) ถูก control หักล้างว่าเป็น boilerplate ⇒ ไม่มีหลักฐานสองทาง ⇒ **NOT ESTABLISHED + ปิดสาย** ไม่ใช่ "พิสูจน์แล้วว่าไม่เกิด"

---

## 3. Residual register — **OPEN แต่ไม่ gate อะไร · ห้ามขุดต่อ**

| # | Residual | สถานะ | disposition |
|---|---|---|---|
| R1 | ไฟล์ 685 ตัวที่ถูกเขียนช่วง 13:22–13:29 (survivor set) คืออะไร | UNEXPLAINED | ไม่มี verdict ใดพึ่งค่านี้ ⇒ **ปล่อย** · ยังนับ survivor bias ตาม F-TIME-01 |
| R2 | 1 ไฟล์ที่บัญชีไม่ลงใน 26a (`890 = 887+2+1`) | UNRESOLVED | อาจ recover ไม่ได้ถ้าถูกเขียนทับ ⇒ **ปล่อย** |
| R3 | `F-TIMER-01` 7,200.9 s ≈ 2h00m00.9 s | HYPOTHESIS | grep 7200 ใน tracked tree = **fรี** แต่ไม่จำเป็น ⇒ ให้แนบไปกับ pre-flight ของ run ถัดไป (28d) |
| R4 | สาเหตุที่แท้จริงของ lifecycle halt | UNKNOWN | **ย้ายเป็น forward control** (§5) — ไม่ใช่งานสืบสวนย้อนหลังอีก |

---

## 4. ทำไมนี่คือผลลัพธ์ ไม่ใช่ความล้มเหลว

```
controlled negative : healthy มี signature ชุดเดียวกัน (207 vs 208)
                    + target ไม่มี reload/compile หลัง halt (COUNT = 0)
```
⇒ เรา **ตัดตัวสงสัยออกได้ 1 ตัวด้วยหลักฐานสองทาง** (มี control + มีขอบเขตหลัง halt) — นี่คือผลที่ใช้ตัดสินใจได้จริง ต่างจาก "ขุดต่อเรื่อย ๆ โดยไม่ปิด" · และทำให้ §5 มีเหตุผลรองรับ ไม่ใช่แค่ความหวัง

---

## 5. Forward control — สิ่งที่ *แทน* การขุดย้อนหลัง
**Pre-run instrumentation gate (§3 ของ RULING_029) มีผลบังคับ ณ วินาทีนี้** เพราะ R4 (สาเหตุ halt) จะถูกตอบได้เฉพาะ *ล่วงหน้า*: sampler working-set + ขนาด log ทุก 60 s + hash `ScriptAssemblies`/runner ก่อนรัน
> เพิ่มจาก ORD-30: `L1013 Reloading assemblies after forced synchronous recompile` มีจริง **ในทั้งสอง run** ⇒ assembly ถูก rebuild/reload ได้ในชีวิตปกติของ session ⇒ **hash ก่อนรันคือสิ่งเดียวที่บันทึกสภาพที่รันจริงได้** (หลังรัน = F-TIME-01 อีกกรณี)

---

## 6. ⚠ สิ่งที่เอกสารนี้ **ไม่** เปลี่ยน
```text
F-PROV-01 = OPEN → disposition = UNCLOSABLE / ACCEPTED RISK + forward closure (ยังไม่ปิดในเอกสารนี้)
F-HOOK-01 = STANDS → ต้องเคลียร์ก่อน APK / Quest (device gate)
S5        = UNKNOWN (ต้องผ่าน §3 instrumentation gate ก่อน invoke)
008       = NOT AUTHORISED   ·   Pack v10 = BLOCKED   ·   final system gate / APK = NOT STARTED
ART       = เริ่มขนานได้ ภายใต้ §4.1–4.4 ของ RULING_029 (ห้ามแตะ §4.2)
ไม่มี Unity run · ไม่มี REAL10 rerun · ไม่มี source/physics/cert mutation
```

---

## 7. GIT CLOSE SET (ชุดไฟล์ที่ต้อง commit ให้ครบ)

### 7.1 ไฟล์
```text
Artifacts/M5_3/WO027_LUNA_ORD27_PARTIAL_FORENSICS_20260924.txt        (Luna · ปัจจุบัน ?? )
Artifacts/M5_3/WO028_LUNA_ORD28_FORENSICS_20260924.txt                (Luna · ปัจจุบัน ?? )
Artifacts/M5_3/WO030_LUNA_ORD30_FORENSICS_20260924.txt                (Luna · ต้องสร้าง — ยังไม่มี)
Docs/AI_TEAM/COACH_RULING_029_SYSTEM_CLOSE_ART_PARALLEL_20260925.md   (Coach)
Docs/AI_TEAM/COACH_CLOSURE_030_FORENSIC_CLOSED_20260925.md            (Coach)
```
⚠ **WO030 artifact ต้องมี** raw output ของ 30a/30b/30c — เพราะผล ORD-29a/30 ยังอยู่แต่ในแชท ⇒ ตามกติกา = **ยังไม่ใช่หลักฐาน** ถ้าไม่ลง tracked artifact

### 7.2 ⚠ กับดัก index (A1.x integrity)
Coach artifact ใต้ `Docs/AI_TEAM/` **ถูกนับใน index** (A1.3 = 16/16 · ถ้า commit เพิ่มต้องได้ row count ตรง/MATCH) ⇒ ถ้าเพิ่ม 2 ไฟล์ ต้องเพิ่ม 2 row ใน index **ในคอมมิตเดียวกัน** แล้ว quote ผล audit
```powershell
git ls-files Docs/AI_TEAM        # หาไฟล์ index ก่อน — ห้ามเดาชื่อ
# หลังเพิ่ม row: รัน audit ของ index แล้ว quote ผลว่าตรง/MATCH
```
**fallback**: ถ้า index update ไม่สะอาดในรอบเดียว ⇒ **หยุดและ escalate** ห้าม half-update index (index gate: ไม่ตรง = ถือว่าไม่ได้ลงจริง)

### 7.3 คำสั่ง (owner/Luna เท่านั้น — Coach push ไม่ได้)
```powershell
Set-Location "C:\Users\mongo\UnityProjects\147 VR"
git status --porcelain                                  # ก่อน: ต้องเห็น ?? สองไฟล์
git add Artifacts/M5_3/WO027_LUNA_ORD27_PARTIAL_FORENSICS_20260924.txt `
        Artifacts/M5_3/WO028_LUNA_ORD28_FORENSICS_20260924.txt `
        Artifacts/M5_3/WO030_LUNA_ORD30_FORENSICS_20260924.txt `
        Docs/AI_TEAM/COACH_RULING_029_SYSTEM_CLOSE_ART_PARALLEL_20260925.md `
        Docs/AI_TEAM/COACH_CLOSURE_030_FORENSIC_CLOSED_20260925.md `
        Docs/AI_TEAM/<index ไฟล์จริง>
git commit -m "close: forensic branch F5a-prime closed (ORD-30 controlled negative) + system-close plan + ART boundaries"
git push origin HEAD:integration/008-m53
git rev-parse HEAD
git ls-tree -r --name-only HEAD -- Artifacts/M5_3
git status --porcelain                                  # หลัง: ต้องว่าง
```
**ข้อจำกัดที่ตรวจแล้ว**: Coach push ไม่ได้ (`fatal: could not read Username for 'https://github.com'` · ไม่มี `~/.git-credentials` / `GH_TOKEN` / `GITHUB_TOKEN`) และ local `git commit` บนเครื่องถูก execution layer block ⇒ **วิธีที่ถูกคือ owner รันเอง หรืออนุมัติผ่านช่องทางของ tool — ห้ามหลบ safety** (การตัดสินของคุณถูกต้อง)

### 7.4 เกณฑ์ปิด
```text
[ ] WO027 + WO028 + WO030 อยู่บน remote (ls-tree ยืนยัน)
[ ] Coach 2 ไฟล์อยู่บน remote
[ ] index row count MATCH + quote ผล audit
[ ] git status --porcelain = ว่าง
[ ] ประกาศ: FORENSIC BRANCH F5a‴ = CLOSED (NOT ESTABLISHED) · R1–R3 = OPEN non-blocking · R4 = forward control
```
