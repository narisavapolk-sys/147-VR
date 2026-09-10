# Pool Table Assets (นำเข้าจาก Blender poolTable2.blend)

| ไฟล์ | เนื้อหา | หมายเหตุ |
|---|---|---|
| `PREFAB POoL table.fbx` | โต๊ะพูล 17 ชิ้น (พร้อมป้ายทอง Naris 2 ฝั่ง) | สกิน Navy & Gold (ผ้าแดง) · **PBR** |
| `PREFAB POoL table Walnut.fbx` | โต๊ะพูล สกินวอลนัท (ผ้าเขียวป่า) | เผื่อเกม 9-ball · **PBR** |
| `PREFAB POoL table Blue.fbx` | โต๊ะพูล สกินไม้เข้ม + ทองเหลือง (ผ้าน้ำเงินเข้ม) | เผื่อเลือกเล่น · **PBR** |

## 🎨 สกินที่ใช้ได้ (4 สกิน)

| # | ชื่อ | ผ้า | ตัวโต๊ะ | โลหะ/ขอบ | FBX |
|---|---|---|---|---|---|
| 1 | **Navy & Gold** | ไวน์แดง burgundy | กรมท่า navy | ทอง Art Deco | `PREFAB POoL table.fbx` |
| 2 | **Walnut** | เขียวป่า forest green | ไม้วอลนัท | ทองแดง/บรอนซ์ | `PREFAB POoL table Walnut.fbx` |
| 3 | **Blue** | น้ำเงินเข้ม navy | ไม้เข้ม cabinet | ทองเหลือง brass | `PREFAB POoL table Blue.fbx` |
| 4 | **Poom** (สีเรียบ) | น้ำเงินเข้ม navy (0.02,0.05,0.15) | มะฮอกกานี (0.15,0.08,0.04) | ทองเหลือง (0.8,0.6,0.2) | — เขียนสคริปต์ assign ใหม่ได้ |

> สกิน 4 (Poom) เป็นสีเรียบไม่มี PBR texture — ใช้สคริปต์ต้นฉบับ assign material ชื่อ `Poom_Navy_Felt` / `Poom_Dark_Mahogany` / `Poom_Gold_Inlay` เข้า Blender แล้ว export FBX ได้ครับ
| `PREFAB POoL Balls.fbx` | ลูกพูล 16 ลูก (57.15mm สีเต็ม/สีครึ่ง/คิวบอล) | วางบนผ้าแล้ว (ไม่ลอย) |
| `PREFAB POoL Cues.fbx` | คิว 2 ต้น (1470mm) | |
| `Textures/` | texture PBR: สักหลาด/ลายไม้ (albedo+nor_gl+rough) + ลูก (n1–n15, cueball) + ป้าย | ใช้กับ URP materials |

## 🧵 PBR Textures (3 สกิน)

โหลดจาก PolyHaven (CC0): **สักหลาด** = scuba_suede, **ไม้** = black walnut veneer / wood cabinet — สีสกิน bake เข้า albedo ด้วยการหมุน hue (HSV) เก็บลาย texture ครบ
- ผ้า AAA_Felt: albedo + normal + metallic-gloss → ด้านจริง (Smoothness 0.12)
- ไม้ AAA_Wood: albedo + normal + metallic-gloss → เงาพอเหมาะ (Smoothness 0.35)
- โลหะ AAA_Metal: Metallic = 1 (ทอง/ทองแดง) | UV สร้างใหม่ด้วย Smart UV Project (เดิมไม่มี UV)
- **URP metallic-gloss map**: roughness texture (จาก PolyHaven) ถูก invert เป็น smoothness แล้ว pack ลง alpha ของ `_MetallicGlossMap` (`_SmoothnessTextureChannel=1`) — ผิวไม้/ผ้าขรุขระผันแปรตามจุดจริง (ทำโดย `make_metallic_gloss.py`)
- ไฟล์ต้นทาง: `Blender/figs/pbr/` (felt_*, wood_*_*, *.jpg)
- สร้างใหม่ได้: `download_pbr.py` → `bake_tinted_albedo.py` → `apply_pbr.py -- main|walnut|blue`

## ✅ Prefab อย่างเป็นทางการ (URP)

สร้างด้วย `Assets/Editor/PoolTablePrefabBuilder.cs` → อยู่ที่ **`Assets/Prefabs/PoolTable/`**:

| Prefab | ใช้กับ |
|---|---|
| `PREFAB POoL table.prefab` | โต๊ะสกิน Navy & Gold |
| `PREFAB POoL table Walnut.prefab` | โต๊ะสกินวอลนัท |
| `PREFAB POoL table Blue.prefab` | โต๊ะสกินไม้เข้ม + ทองเหลือง |
| `PREFAB POoL Balls.prefab` | ลูก 16 ลูก (texture ครบ) |
| `PREFAB POoL Cues.prefab` | คิว 2 ต้น |

- Material ทั้งหมดเป็น **Universal Render Pipeline / Lit** (รองรับ URP)
- ลูกบอล + ป้ายทองใช้ texture จริง (สกัดจาก FBX ฝัง) ส่วนโต๊ะ/คิวเป็น PBR สี
- ตั้งค่าไว้แล้ว: ลูก 57.15mm วางบนผ้าไม่ลอย · คิว 1470mm · โต๊ะ 2540×1270mm สูง 756mm

## มาตรฐาน
- ลูก: 57.15mm (WPA) · คิว: 1470mm · โต๊ะ: พื้นเล่น 2540×1270mm (9ft) สูง 756mm

## วิธีใช้ใน Unity
1. เปิด Scene **`Assets/Scenes/PoolTable_9Ball.unity`** — มีโต๊ะ + ลูกจัด rack 9-ball + คิวบอล + คิว 2 ต้น + กล้อง/แสงพร้อมแล้ว
2. หรือสร้างใหม่: ลาก `PREFAB POoL table.prefab` + `Balls` + `Cues` เข้า Scene
3. ตั้งค่า Model Import: Scale Factor = 1, Convert Units = ON
4. Material: FBX ฝัง texture มาแล้ว (ลูกบอล + ป้าย) ส่วนโต๊ะใช้สี PBR แบบเรียบ — ถ้าอยากได้ URP Lit ให้ลาก material เข้าไปแทนได้

## ⚡ สลับสกินด้วยปุ่มเดียวในเกม (SkinCycler)

`Assets/Scripts/PoolTable/SkinCycler.cs` — กด **Tab** (หรือปุ่มบนจอ มุมขวาล่าง) เพื่อวนสกิน Navy → Walnut → Blue ทันที พร้อมข้อความแจ้งชื่อสกิน 1.5 วิ
- เปลี่ยนปุ่มได้ที่ Inspector (`cycleKey`) · ปิดปุ่มบนจอได้ (`createUiButton`)
- เรียกจาก UI เกมได้: `GetComponent<SkinCycler>().Cycle()`
- อยู่ใน Scene แล้ว (GameObject `SkinCycler`)

## 🎮 สลับสกินโต๊ะในเกม (TableSkinManager + SkinSelectMenu)

Scene `Assets/Scenes/PoolTable_9Ball.unity` มีเมนูเลือกสกินก่อนเริ่มเกมแล้ว:

| Component | ไฟล์ | หน้าที่ |
|---|---|---|
| `TableSkinManager` | `Assets/Scripts/PoolTable/TableSkinManager.cs` | เป็นเจ้าของโต๊ะ เปลี่ยน prefab ตามสกิน (Navy/Walnut/Blue) |
| `SkinSelectMenu` | `Assets/Scripts/PoolTable/SkinSelectMenu.cs` | เมนู uGUI 3 ปุ่มสกิน + ปุ่ม Start (สร้าง Canvas ตอนรัน) |

- กดปุ่มสกิน → เปลี่ยนโต๊ะ **ทันทีหลังเมนู** (ดูตัวอย่างก่อนเริ่ม) แล้วกด Start
- บันทึกตัวเลือกไว้ที่ `PlayerPrefs "PoolTable.Skin"` — เปิดเกมครั้งหน้ายังจำสกินเดิม
- ลูกกับคิวไม่โดนแตะ (ทุกสกินใช้ geometry เดียวกัน ลูกที่จัด rack อยู่คงตำแหน่ง)
- ใช้ในเกมจริง: เรียก `TableSkinManager.SetSkin(0|1|2)` จาก UI ไหนก็ได้

## 🎱 จัด rack ลูก (BallRack)

`Assets/Scripts/PoolTable/BallRack.cs` — สคริปต์จัดลูกเป็นรูปสามเหลี่ยมมาตรฐานบนผ้า:
- **Rack 9-ball**: ลูก 1 อยู่ apex (foot spot) · ลูก 9 กลาง rack · แถว 1-2-3-3
- **Rack 8-ball**: ลูก 1 apex · ลูก 8 กลาง · มุมตรงข้าม solid+stripe · แถว 1-2-3-4-5
- **คิวบอล**: วางที่ head spot (หลัง head string) อัตโนมัติ
- **โหมดสุ่ม (shuffle)**: `shuffle=true` (ค่าเริ่มต้น) — ลูก 1/8/9 อยู่ตำแหน่งเดิมตามกติกา ลูกอื่นสลับตำแหน่งทุกครั้งที่ rack

วิธีใช้:
- **ตอนเล่นเกม**: `GetComponent<BallRack>().RackNineBall()` หรือ `RackEightBall()`
- **ใน Editor**: คลิกขวาที่ GameObject ที่เป็นลูก → `BallRack` → `Rack 9-Ball` / `Rack 8-Ball`
- ตรวจจับลูกอัตโนมัติจากชื่อ material (`ballN1`–`ballN15`, `ballCue` — รองรับ prefix จาก prefab builder) และหาโต๊ะจาก `tableBed`

## 🏓 Physics ลูกบอล

ทั้ง `PoolTable_9Ball.unity` และ `PoolTable_8Ball.unity` มีลูกทุกใบพร้อม:
- **Rigidbody** (มวล 170g, gravity on, drag 0.15, angular drag 0.6)
- **SphereCollider** รัศมี 28.575mm ตรงกับลูกจริง
- **Physics Material** `Assets/Models/PoolTable/PoolBallPhysics.physicsMaterial` — bounciness 0.92, dynamic friction 0.18, static friction 0.22 (เด้งแบบลูกพูลจริง กลิ้งบนผ้า)
- ลูกทุกใบวางแตะผ้าพอดี (center Y = ผ้า + รัศมี)

## 🎬 Scene ทั้ง 2

| Scene | เนื้อหา |
|---|---|
| `Assets/Scenes/PoolTable_9Ball.unity` | โต๊ะ + ลูก 9-ball rack (สุ่ม) + คิวบอล + คิว 2 ต้น + Physics + เมนูสกิน + SkinCycler |
| `Assets/Scenes/PoolTable_8Ball.unity` | โต๊ะ + ลูก 8-ball rack เต็ม 15 ลูก (สุ่ม) + คิวบอล + คิว 2 ต้น + Physics + เมนูสกิน + SkinCycler |

> ต้นฉบับ Blender อยู่ที่ `SNOOKER   VR pool table/Blender/poolTable2.blend`
> สคริปต์สร้าง/export อยู่ที่ `SNOOKER   VR pool table/_scripts/`

## 🎪 ฉากหลัก (คอนเสิร์ต + โต๊ะ) — SampleScene

ฉากเริ่มต้นของเกมคือ **`Assets/Scenes/SampleScene.unity`** (ไฟล์เดียวใน Build Settings) ประกอบตาม `Assets/147 main/step to do.txt`:

| ขั้นตอน | สิ่งที่อยู่ในฉาก |
|---|---|
| 1 | `ConcertRoom` — `Assets/147 main/ConcertRoom_WithTable.fbx` (ห้อง/เวที 24×24 ม. พร้อมโต๊ะพูล 109 ชิ้น) |
| 2 | Skybox `Dreamy_OLED_Skybox` — `Assets/147 main/Dreamy_OLED_HDRI.exr` (Skybox/Panoramic, ไฟบอเก้ OLED) |
| 3 | `ConcertSmoke` — Empty GO ที่ (0, 0.25, 0) กลางห้องใกล้พื้น |
| 4 | `DriftingConcertSmoke.cs` + ParticleSystem (auto) — ควันลอยฟุ้งวนซ้ำ |
| 5 | `SmokeParticle_Texture.png` ใส่ช่อง Smoke Texture แล้ว |

- เปิด Unity → กด **Play** ใน SampleScene เพื่อดูควันลอย
- ประกอบฉากใหม่ได้ด้วย `Assets/Editor/SetupConcertScene.cs` (Menu: Tools → 147 → Setup Concert Scene)
- HDRI ต้นฉบับ: `SNOOKER   VR pool table/Hdri/Dreamy_OLED_HDRI.exr`

## 👧 หุ่น 2 ตัว (สลิม ผอมเพรียว ไม่มีเสื้อผ้า)

~~วางข้างโต๊ะใน SampleScene~~ — **ลบออกจากฉากแล้ว** (ตามแผนใหม่) ไฟล์ FBX ยังอยู่พร้อมใช้:

| หุ่น | สูง | สภาพ |
|---|---|---|
| `Cute Girl SLIM.fbx` | 1.57m | body + eyes + hair + lashes (ขาใหม่สร้างใหม่ 32-seg smooth) |
| `Chubby magic girl SLIM.fbx` | 1.53m | **SkinnedMesh 8 ส่วน + rig Rigify เต็ม** — พร้อมสวมชุดใหม่ |

- ไฟล์ FBX: `Assets/Models/Girls/` (จาก `SNOOKER   VR pool table/FBX/* SLIM.fbx`)
- สคริปต์สลิม: `SNOOKER   VR pool table/_scripts/slim_girls.py`
- วางกลับในฉากได้ด้วย `Assets/Editor/AddGirlsToScene.cs` (Tools → 147 → Add Slim Girls to Scene)
- ลบออกได้ด้วย `Assets/Editor/RemoveGirlsFromScene.cs` (Tools → 147 → Remove Slim Girls from Scene)

## 💃👧 หุ่น 2 ตัวเต้น (6 ท่า Mixamo) — ตัวเต็มพร้อมชุด

ใน SampleScene ทั้ง 2 ตัว **ตัวเต็มจากต้นฉบับ พร้อมชุดครบ** เต้นวนทุกท่าอัตโนมัติ:

| หุ่น | ตำแหน่ง | ชุด | สูง |
|---|---|---|---|
| `CuteGirl_Dancing` | (-2.3, 0, 0.8) | bikini, boot, jacket, pant, sock, top + ผม/ตา/ขนตา | 1.57m |
| `ChubbyGirl_Dancing` | (2.3, 0, 0.8) | bra, Corset, skirt, top, boot, hat + หน้า/ผม/ฟัน | 1.57m |

**6 ท่าเต้น (ทุกท่าใช้กับทั้ง 2 ตัว):**

| ท่า | คลิป | ความยาว |
|---|---|---|
| Arms Hip Hop | `Cute_Arms_Hip_Hop_Dance` | 22.0s |
| Booty Hip Hop | `Cute_Booty_Hip_Hop_Dance` | 4.9s |
| Dancing Twerk | `Cute_Dancing_Twerk` | 15.2s |
| Hip Hop Dancing | `Cute_Hip_Hop_Dancing` | 4.1s |
| Hip Hop Dancing (2) | `Cute_Hip_Hop_Dancing_1` | 15.7s |
| Rumba | `Cute_Rumba_Dancing` | 2.4s |

**ไฟล์:**
- **FBX**: `Assets/Models/Girls/Cute Girl Dancing.fbx` (Humanoid, 1 mesh `CuteGirlFull` 102k verts) + `Chubby Girl Dancing.fbx` (Generic, 1 mesh `ChubbyGirlFull` 83k verts) — rig Mixamo 65 bones เหมือนกัน
- **Animator**: `Assets/Models/Girls/CuteDance.controller` — 6 states loop (ใช้ร่วมกัน 2 ตัว)
- **สคริปต์**: `Assets/Scripts/147/CuteDancer.cs` — กด **Tab** สลับท่า, ปุ่ม 1–6 กระโดดท่า, ปุ่มบนจอขวาบน เลือกท่าได้, วนอัตโนมัติ
- วางใหม่ได้ด้วย `Assets/Editor/AddDancingCuteToScene.cs` (Tools → 147 → Add Dancing Cute Girl to Scene)
- ต้นทาง Blender: `_scripts/rig_original_cute_for_dance.py` (Cute) + `_scripts/rig_original_chubby_for_dance.py` (Chubby) — เปิด blend ต้นฉบับ → ย่อ Chubby 2.9m→1.57m → ลบ rig/WGT เดิม → ผูกทุกชิ้นกับ rig Mixamo (auto weights + KDTree copy) → join เป็น mesh เดียว → รวบ 6 actions → export
- ภาพตัวอย่าง 6 ท่า (ทั้ง 2 ตัว): `SNOOKER   VR pool table/Images/_dance_shot_*.png`

> **แก้บั๊กชิ้นส่วนหัวลอย (Chubby):** ต้นตอคือการย่อ 2.9m→1.57m โดย `transform_apply` ย่อรอบ origin ของแต่ละชิ้น — ชิ้นที่ origin อยู่ระดับหัว (หมวก/ผม/ตา/ปาก) ย่อแค่ขนาดแต่ตำแหน่งไม่ลงมา → ลอยเหนือหัว 2m สคริปต์ที่แก้แล้ว `_scripts/fix_chubby_scatter.py` ย่อรอบ **world origin** แทน (SCATTERED_COUNT=0 ทั้งใน Blender และ Unity) + ตรวจภาพ `Images/_chubby_fix_unity.png`

## 🔍 ตรวจงานด้วยภาพจริงผ่าน Blender MCP (ก่อนนำเข้าฉาก)

แทนที่จะดูแค่ตัวเลข (ชื่อ/vertex/bbox) ตอนนี้ตรวจ **ภาพ viewport จริง** จาก Blender ที่เปิดอยู่ (พี่เปิด MCP server ไว้) ได้แล้ว:

```bash
cd "SNOOKER   VR pool table"
# ถ่าย 4 มุม (front/side/top/iso) ไปที่ Images/_mcp_*.png
python _scripts/mcp_shot.py all
# ถ่ายมุมเดียว + ระบุ path
python _scripts/mcp_shot.py front Images/my_check.png --shade MATERIAL
# workflow เต็ม: 4 มุม + ตรวจ bbox แต่ละชิ้นเทียบ body (เจอชิ้นลอย = SCATTERED)
python _scripts/mcp_verify.py
```

- ใช้คำสั่งในตัวของ blender-mcp addon: `execute_code` (ตั้งมุมกล้อง) + `get_viewport_screenshot` (capture ผ่าน GPUOffScreen — ไม่ต้องให้หน้าต่าง Blender อยู่เบื้องหน้า)
- **ต้องเปิด Blender GUI + เปิด MCP server** (ปุ่ม Start MCP Server ในแท็บ BlenderMCP, port 9876) ก่อน
- ภาพออกที่ `SNOOKER   VR pool table/Images/_mcp_*.png` — เปิดดูได้เลย
- ใช้สคริปต์นี้ตรวจทุกครั้งก่อน export/nำเข้าฉาก (พี่เปิด MCP ให้ตลอดเวลาจะได้ไม่เสียเวลาแก้ทีหลัง)
