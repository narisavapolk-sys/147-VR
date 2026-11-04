# 147VR — Project Rules (ทุกเอไอที่เปิดโฟลเดอร์นี้ต้องอ่านก่อนทำงาน)

โปรเจกต์: **147VR** — VR billiards/snooker AAA (Unity 6, URP, Quest) • เจ้าของ: โม่ง (สื่อสารไทย 95% + อังกฤษ 5% แบบ dev)

<!-- SKILLS-HUB v1 -->
## SKILLS HUB — MANDATORY ก่อนงานเกมทุกครั้ง
ก่อนเริ่มงาน Unity/C#/Blender/asset/rip/physics/VR-XR/shader/QA/publish/devlog-video ใด ๆ:
1. อ่านสารบบ `C:\Users\mongo\SkillsHub\SKILLS-HUB.md` (สร้างใหม่: `node C:\Users\mongo\SkillsHub\scan-skills.mjs`)
2. เลือกสกิลที่ description ตรงกับงาน แล้วประกาศ 1 บรรทัด **"Using skills: ..."** ก่อนลงมือ — ถ้าไม่มีตัวไหนตรง ให้บอก "skills-hub: no match" แล้วทำงานต่อปกติ
3. เรียกใช้ได้เลยโดยไม่ต้องติดตั้งซ้ำ: เปิดไฟล์ `path` ในสารบบแล้วทำตาม SKILL.md นั้น (ไฟล์ WSL อ่านผ่าน `\\wsl.localhost\Ubuntu\...`)
4. สกิลที่มีสคริปต์: อ่านสคริปต์ก่อนรันเสมอ • ฟรีเท่านั้น • ห้าม sudoเงียบ • ห้าม publish/upload
<!-- /SKILLS-HUB -->

## กติกา 147VR สำคัญ (สรุปจากเจ้าของงาน)
- Asset ต้อง **rip จากเกมจริงเท่านั้น** — ห้ามสร้าง geometry เอง (texture เองได้เมื่อไม่มีในไฟล์ rip)
- ยืนยัน placement ทุกครั้งด้วย **rendered image** ไม่ใช่ตัวเลข transform อย่างเดียว
- ห้ามแก้ Unity scene ผ่าน GUI unattended — สร้าง Editor MenuItem ให้โม่งคลิกขั้นสุดท้ายเอง
- งานหนัก/เสี่ยง: เสนอแผนขออนุมัติก่อน แล้ว autonomous iterate จาก screenshot
- Blender GUI เปิดอยู่: เขียนไฟล์ .blend เมื่อ Blender ปิดเท่านั้น + backup ก่อน
- Unity batchmode ห้ามรันตอน RAM ต่ำ/Library เย็น — ใช้ Logs/editor_cmd.txt + ToolRunner
