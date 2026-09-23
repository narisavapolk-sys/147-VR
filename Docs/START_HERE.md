# START HERE — 147 VR

> ไฟล์นี้คือจุดเริ่มของทุก session (คนหรือ AI) — **อ่าน 5 นาที แล้วรู้ว่าต้องทำอะไรต่อ**
> วิธีใช้: คัดลอกไปเป็น `Docs/START_HERE.md` ในรีโป แล้วอัปเดตวันที่/tip ทุกครั้งที่ gate เปลี่ยน

---

## 0. 30 วินาทีแรก

**147 VR** = เกมสนุกเกอร์/พูลใน VR สำหรับ Meta Quest 2/3 บน Unity `6000.4.4f1` (ดู §5 เรื่อง 12f1)
**โปรเจกต์จริงอยู่ที่**: `C:\Users\mongo\UnityProjects\147 VR` (ห้ามสร้าง junction `C:\147VR` อีก)
**ห้าม**: เปิด Unity สองตัวพร้อมกัน · เดา/ประดิษฐ์ค่า Golden หรือ PASS · ลบ `C:\Temp` (มี editor install อยู่)

## 1. ลำดับการอ่าน (read order)

| ลำดับ | ไฟล์ | ทำไม |
|---|---|---|
| 1 | `Docs/STATE_SNAPSHOT.md` | ยืนตรงไหนตอนนี้ |
| 2 | `Docs/147VR_CURRENT_STATE.md` | ประวัติการตัดสินใจ — **อ่านจากท้ายขึ้นบน** |
| 3 | `Docs/147VR_AI_WORK_PROTOCOL.md` | สัญญาการทำงาน/การ handoff ที่ต้องปฏิบัติ |
| 4 | `Docs/147VR_PROJECT_MEMORY.md` | ความจำโปรเจกต์/สถาปัตยกรรม |
| 5 | `ARCHITECTURE.md` + `147VR_WORKING_DIRECTIVE.md` | ขอบเขตและ dependency rules |
| 6 | `Docs/147VR_MASTER_PRODUCTION_ROADMAP.md` | เฟสงานทั้งหมด และเฟสที่ active |
| 7 | `Docs/147VR_PHYSICS_CERTIFICATION_RUNBOOK.md` | วิธี certify ฟิสิกส์ทีละขั้น |
| 8 | `Docs/147VR_REALITY_MAP.md` | inventory จริงของไฟล์/scene/prefab (134 KB ใช้เป็น reference) |

## 2. กฎที่ห้ามละเมิด

1. **Physics Authority เป็นเจ้าของความจริง**: `CuePhysicsAdapter` = แรงกระแทกไม้คิวเพียงหนึ่งเดียว · `TableSurfaceProfile`(+`TableSurfaceController`) = คู่เดียวของค่าผ้าสนุกเกอร์ · `Bed_Collider` = คอลไลเดอร์สนาม · ห้ามสร้าง authority ที่สอง
2. **ห้ามประดิษฐ์ค่า**: Golden/expected/PASS ต้องมาจากการวัดจริงใน Unity เท่านั้น
3. **แยก 3 ระดับ**: implementation-complete ≠ runtime-proven ≠ production-complete
4. **LOCK**: Main Scene · M5 Rules/Scoring/Turn · V007/Golden · REAL10 · ต้องมี authorization ก่อนแตะ
5. **หลักฐานต้อง commit ได้**: `.txt/.json/.xml` ใต้ path ที่ track + header มาตรฐาน + sha256 — **ห้าม `.log`**
6. **Unity ล่ม → อ่าน `upm.log` ก่อน** แล้วจัดคลาส (delayed startup / env inheritance / spawn stall / unknown) — ห้าม reboot วน
7. **ทุกท้าย session** ต้องอัปเดต `147VR_CURRENT_STATE.md` + `147VR_PROJECT_MEMORY.md` + `STATE_SNAPSHOT.md` + push

## 3. ถ้าต้องเริ่มงานวันนี้ — ทำอะไรต่อ

ดูก้าวถัดไปที่ถูกต้องใน `Docs/STATE_SNAPSHOT.md` §"Next" (สถานะ ณ 2026-09-22):
**008 READ-ONLY MEASUREMENT ARTIFACT → Coach review → non-destructive staging → integration validation → tracked REAL10 → Coach audit → human approval → production integration**
ก่อนเริ่ม ต้องเคลียร์: F7 (integration line), F8 (editor bump commit), F10 (จำแนก upm.log), F9 (artifact เป็น `.log` → commit ไม่ได้)

## 4. คำสั่งที่ใช้บ่อย

```powershell
# ดูทุก branch ล่าสุด (สำคัญ: งานจริงไม่ได้อยู่บน main)
git fetch origin '+refs/heads/*:refs/remotes/origin/*'
git log --oneline -10 <branch>

# รัน Unity แบบปลอดภัย (ตาม LOCK — ต้องไม่มี Unity.exe รันอยู่)
# ระวัง: สคริปต์ที่ track ไว้ยัง hardcode editor 4f1 ของ Hub (F11) — ตรวจก่อนใช้
Docs\AI_TEAM\Tools\Unity_Safe_Batch_Launch.ps1 -LogName "<name>"
```

## 5. สภาพแวดล้อม (ต้องรู้)

| หัวข้อ | ค่า |
|---|---|
| Editor ในรีโป | `6000.4.4f1 (360f97ecca93)` |
| Editor ที่เครื่องใช้อยู่ (22 ก.ย.) | `C:\Temp\UnityEditors\6000.4.12f1\Editor\Unity.exe` ← ยังไม่ commit (F8) |
| Validation worktree | `C:\Temp\147VR_M53_VALIDATE` |
| UPM ENV_GUARD | `PROGRAMDATA` `ALLUSERSPROFILE` `APPDATA` `LOCALAPPDATA` `USERPROFILE` `TEMP` `TMP` `NO_PROXY=localhost,127.0.0.1` `UNITY_UPM_TIMEOUT=120` + ตัด `npm-cache\_npx` ออกจาก PATH |
| ห้าม | `-noUpm` เมื่อพึ่ง package · `git clean/reset/stash` เหวี่ยง · ลบ `C:\Temp` |
