# คู่มือ Bake แสงห้อง WipRoom — 3 ขั้นตอน (147VR)

## ทำไมต้อง bake (หลักฐานจากไฟล์ ไม่ใช่ความรู้สึก)

- ไฟทั้ง 19 ดวงใน `Assets/Prefabs/Lighting/PREFAB_147VR_LightRig_A.prefab` มี `m_Lightmapping: 4`
  ซึ่งคือ **Mixed** (Unity: Realtime=1, Baked=2, Mixed=4) → ตั้งไว้ถูกแล้วสำหรับ Quest
  (direct light เป็น realtime, indirect เป็น baked)
- **แต่ไม่มี `LightingData.asset`  anywhere ในโปรเจกต์** = ไม่เคย bake มาก่อน
  ผลคือ indirect bounce = 0 ทุกจุด → ผนัง damask, กรอบรูป, ฝ้า coffer ตกอยู่ในเงามืด
  ซึ่งต่างจาก Blender EEVEE ที่มี indirect bounce อัตโนมัติ
- `Assets/Scenes/147VR_MainScene_WipRoom.unity` มี 21 renderer ที่ `m_StaticEditorFlags: 0`
  (ไม่ static) → ถ้า bake ตอนนี้ วัตถุพวกนั้นจะไม่ถูกINCLUDED ใน lightmap

## สิ่งที่มีอยู่แล้ว — ไม่ต้องเขียนโค้ดใหม่

`Assets/Editor/Lighting/147VRBakeGI.cs` มีเมนูครบ 4 ขั้น และผูกเป็นคำสั่ง bridge ได้:

| เมนู | ToolRunner command | ทำอะไร |
|---|---|---|
| `147 VR/Lighting/Bake GI/1 Ensure UV2 (Reimport Models)` | `uv2-ensure` | ตั้ง `generateLightmapUVs` ให้ model importer แล้ว reimport |
| `147 VR/Lighting/Bake GI/2 Mark Contribute GI` | `gi-flags` | ใส่ flag `ContributeGI` ให้ renderer ที่ static อยู่แล้ว + save scene |
| `147 VR/Lighting/Bake GI/3 Flip Lamps for Bake` | `lights-baked` | TableKey → Mixed, ไฟห้อง → Baked |
| `147 VR/Lighting/Bake GI/4 Bake Now (Baked GI)` | `bake` | สร้าง lighting settings asset แล้วเรียก `Lightmapping.Bake()` แบบ synchronous |

## 3 ขั้นตอน (กดตามลำดับ)

### ขั้นที่ 1 — เตรียม UV2 + static flag
```
147 VR ▸ Lighting ▸ Bake GI ▸ 1 Ensure UV2 (Reimport Models)
147 VR ▸ Lighting ▸ Bake GI ▸ 2 Mark Contribute GI
```
- ขั้น 1 จะ **reimport model ทั้งหมด** — บนเครื่อง 15.4GB นี้อาจกินเวลา 10–30 นาที
  และ Editor จะค้าง (ปกติของงานนี้)
- ขั้น 2 นับจำนวนที่ mark ได้จริงแล้วเขียนลง `Logs/` — **อ่านเลขนั้นก่อนไปขั้น 3**
  ถ้าได้ 0 แปลว่าห้องยังไม่มี renderer ที่ static เลย ต้องไปตั้ง static ก่อน
  (ตัว `gi-flags` ข้าม object ที่ไม่ static เพื่อไม่ให้ไปแก้ prefab ที่ตั้งใจให้เป็น dynamic)
- ขั้น 3 ข้ามได้ เพราะไฟเป็น Mixed อยู่แล้ว (ตรวจจาก `m_Lightmapping: 4`)

### ขั้นที่ 2 — Bake และปล่อยเครื่อง
```
147 VR ▸ Lighting ▸ Bake GI ▸ 4 Bake Now (Baked GI)
```
- **save scene ก่อน** (Ctrl+S) — bake จะเขียน scene/lighting asset
- ปิดโปรแกรมอื่นทั้งหมด อย่าเปิด Blender พร้อมกัน
- `Lightmapping.Bake()` ในสคริปต์นี้เป็น **synchronous** → Editor จะค้างทั้งก้อน
  อย่ากด Esc อย่าปิดหน้าต่าง ปล่อยไว้ (ห้อง 1485 mesh บนเครื่องนี้คาดว่าจะใช้ **หลายชั่วโมง**)
- จบแล้ว log จะเขียนบรรทัด: `[OK] bake finished in X min; scene = <LightingData path>`
  และไฟล์ `LightingData.asset` จะโผล่ในโปรเจกต์ — **นี่คือหลักฐานว่า bake สำเร็จจริง**

### ขั้นที่ 3 — ตรวจผลด้วยภาพ (ห้ามเชื่อตัวเลขอย่างเดียว)
- เปิด Scene view โหมด **Render** แล้วดูที่ควรสว่างจากแสงเด้ง:
  ผนัง damask, กรอบรูป gallery row, ฝ้า coffer เหนือเวที
- เทียบกับ `C:\Users\mongo\OneDrive\Desktop\bay2 blender.png` จากมุมเดียวกัน
- capture เก็บไว้เทียบ (เครื่องมือ `art-capture` / `flag-verify` ใน `WipRoomArtRound3`
  ถ่ายจากมุมที่วัดไว้แล้ว และ log ขนาดไฟล์ — ภาพจริงต้อง >400KB)

## คำเตือนที่ควรรู้ก่อนเริ่ม
1. **Bake กินเวลาและ RAM มาก** — ถ้ากลัวเครื่อง freeze เหมือนรอบก่อน ให้ทำตอนกลางคืน
   และ save scene + commit ก่อนเริ่ม (งานตอนนี้ commit ถึง `5ac90234`, ahead 37)
2. **Bake แล้ว material ที่แก้ทีหลังจะไม่ sync กับ lightmap** — ควร bake *หลัง* จาก
   งานสี/เทกซ์เจอร์ (cornice, พรม, ornament, ป้าย neon) ลงตัวก่อน ไม่งั้นต้อง bake ซ้ำ
3. **Indirect จากไฟ Mixed เท่านั้น** — ถ้าเปลี่ยนไฟเป็น Baked จะหายไปจาก realtime
   ซึ่งใน VR อาจต้องการ direct light อยู่
4. ถ้า bake แล้วผนังยังมืด ให้ตรวจว่า `ContributeGI` ติดจริงที่ object ผนัง
   (ไม่ใช่แค่ floor/table) และ UV2 มีขนาดเพียงพอกับพื้นที่ผิว
