# Legend of Mortal — ภาษาไทย 🇹🇭 (活侠传 / 活俠傳)

**ม็อดแปลภาษาไทย (ไม่เป็นทางการ) สำหรับเกม Legend of Mortal (活侠传)**
Unofficial **Thai** translation mod for the game *Legend of Mortal*.

> แปลจากภาษาจีนเป็นไทยแบบครบทั้งเกม — เนื้อเรื่องทั้ง 17 บท, เมนู, ระบบ, ไอเทม, สกิล, พรสวรรค์, คำอธิบายตัวละคร และ UI ทั้งหมด
> ใช้สำนวนแบบนิยายกำลังภายใน (ข้า / เจ้า / ท่าน) และทับศัพท์ชื่อเฉพาะเป็นไทยตลอดทั้งเกม

---

## 👤 เครดิต / Credits

| ส่วน | ผู้จัดทำ |
|---|---|
| **แปลไทย · Thai translation** | **Krirk0137** |
| ฉบับแปลอังกฤษที่ใช้เป็นฐาน · English base translation | **velvetcakes1** — https://github.com/velvetcakes1/LoM_en |
| ตัวเกม · Original game | 活侠传 / Legend of Mortal (卧龙工作室 / TeeTee Games) |
| เครื่องมือ · Tooling | [BepInEx](https://github.com/BepInEx/BepInEx) · [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) |

> ในเกมมีการใส่เครดิตไว้ที่หน้า **"ขอบคุณที่ร่วมเล่น"** (Thanks for playing) ตอนจบเกมด้วย

## 🆓 เงื่อนไขการใช้งาน / Terms

- **แจกฟรี — ห้ามจำหน่าย** · Free to share — **must NOT be sold** or used commercially.
- นำไปแจกจ่าย/แชร์ต่อได้ **แต่ต้องให้เครดิต** Krirk0137 (ไทย) และ velvetcakes1 (อังกฤษ) เสมอ
  You may redistribute, but **you must keep the credits** to Krirk0137 (Thai) and velvetcakes1 (English).
- ดูรายละเอียดสัญญาอนุญาตด้านล่าง (License)

---

## 📥 วิธีติดตั้ง / Installation

1. กดปุ่มเขียว **`<> Code` → Download ZIP** แล้วแตกไฟล์ (extract)
2. คัดลอก **ทุกอย่างในโฟลเดอร์ `Mod/`** ไปวางในโฟลเดอร์เกมบน Steam
   - ปกติอยู่ที่ `C:\Program Files (x86)\Steam\steamapps\common\Legend_of_Mortal\`
   - หาไม่เจอ: คลิกขวาเกมใน Steam → **Properties → Installed Files → Browse**
   - วางทับให้โครงสร้างเป็น `Legend_of_Mortal/BepInEx/…`, `winhttp.dll`, `doorstop_config.ini` อยู่ในโฟลเดอร์เกม
3. เปิดเกม → เลือก **ออปชั่นที่ 2** ในหน้า Title
4. คลิกดรอปดาวน์ด้านบน → เลือก **ออปชั่นที่ 2** (จีนตัวย่อ / Simplified Chinese) — ระบบจะสวมทับด้วยภาษาไทย

> ⚠️ ถ้าเคยลงเวอร์ชันเก่ามาก่อน ให้ **ลบโฟลเดอร์ `BepInEx` เดิมทิ้งก่อน** แล้วค่อยวางอันใหม่ (ไฟล์ค้างอาจทำให้แปลเพี้ยน)
> ⚠️ ตัวเกมเป็น **32-bit** และค่า `FromLanguage` ต้องเป็น `zh-CN` (ตั้งมาให้แล้ว)

## 🔤 ฟอนต์ / Font

ตั้งค่าใช้ **`OverrideFont=Tahoma`** มาให้แล้วในไฟล์ `BepInEx/config/AutoTranslatorConfig.ini` (แสดงผลภาษาไทยรวมวรรณยุกต์ซ้อนได้ดี ไม่ต้องลงฟอนต์เพิ่ม)

## 🐞 พบข้อผิดพลาด?

- **ตัวหนังสือล้นกล่อง** → แก้ได้ที่ `BepInEx/Translation/en/Text/UI.resizer.txt`
- **ตัวอักษรเป็น □ (tofu)** → ตรวจว่า `OverrideFont` ตั้งเป็นฟอนต์ที่มีสระ/วรรณยุกต์ไทย
- ปิดตัวเลือก *"Enable color change of read text"* ในเกม ไม่งั้นบางข้อความอาจไม่ถูกแทนที่

---

## 📜 License / สัญญาอนุญาต

งานชิ้นนี้เป็น **งานดัดแปลง (derivative work)** ประกอบด้วยสองส่วนที่มีสัญญาอนุญาตต่างกัน:

- **ส่วนแปลอังกฤษฐาน** โดย **velvetcakes1** — สัญญา **MIT** (ดู [`LICENSE-velvetcakes-MIT`](LICENSE-velvetcakes-MIT)) — ต้องคงประกาศลิขสิทธิ์ของ velvetcakes1 ไว้เสมอ
- **ส่วนแปลไทย** โดย **Krirk0137** — สัญญา **CC BY-NC 4.0** (ดู [`LICENSE`](LICENSE)) — ให้เครดิต + ห้ามใช้เชิงพาณิชย์

ดูสรุปเครดิตแบบเต็มได้ที่ [`CREDITS.md`](CREDITS.md)

---
*Story updated for game version 1.0.5000.13 · Thai translation by Krirk0137*
