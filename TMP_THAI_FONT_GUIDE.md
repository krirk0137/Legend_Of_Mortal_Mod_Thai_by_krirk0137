# สร้างฟอนต์ TMP ภาษาไทย สำหรับ Legend of Mortal (แก้ตัวหนังสือเป็น □)

ปัญหา □ (tofu) ที่ **ชื่อตัวละคร (ฟอนต์เหลืองตัวใหญ่) / ตัวเลือก / UI สถานะ** เกิดเพราะพวกนั้นเป็น
**TextMeshPro (TMP)** ซึ่งไม่ได้ใช้ฟอนต์ OS (Tahoma) — TMP ใช้ "font asset" ที่ bake glyph ไว้ล่วงหน้า
ถ้า asset ไม่มี glyph ไทย → □ ต้องสร้าง TMP font asset ไทย แล้วชี้ config ไปหา

## ⚠️ เลขสำคัญของเกมนี้ (จาก LogOutput.log)
- **Unity `2020.3.49f1`** ← ต้อง build ฟอนต์ด้วยเวอร์ชันนี้เป๊ะ (คนละเวอร์ชัน = โหลดไม่ขึ้น/ว่างเปล่า)
- TextMesh Pro `1.4.0`
- เกมเป็น Windows (README ระบุ 32-bit → ใช้ BuildTarget `StandaloneWindows`)

## ทำไม bundle สำเร็จรูป (Dinkum/Kanit_sdf) ใช้ไม่ได้
AssetBundle ผูกกับเวอร์ชัน Unity ที่ build — Dinkum คนละเวอร์ชัน → โหลดไม่ขึ้น
**ต้อง build ด้วย Unity 2020.3.49f1 เอง** (ทางเดียวที่ชัวร์)

---
## ขั้นตอนสร้าง (ทำครั้งเดียว)

### 1) เตรียม
- ลง **Unity Hub** → ลง **Unity 2020.3.49f1** (ต้องเวอร์ชันนี้)
- โหลดฟอนต์ไทย .ttf จาก Google Fonts: **Kanit** หรือ **Sarabun** หรือ **Noto Sans Thai**
  (Sarabun/Noto จัดวางวรรณยุกต์ซ้อนดีสำหรับ UI; Kanit สวยทันสมัย — เลือกได้)

### 2) โปรเจกต์
- New Project (2D/3D อะไรก็ได้) → เมนู Window → Package Manager → ติดตั้ง **TextMeshPro** (ถ้ายังไม่มี)
- ลากไฟล์ .ttf เข้า `Assets/`

### 3) Font Asset Creator (Window → TextMeshPro → Font Asset Creator)
| ช่อง | ตั้งเป็น |
|---|---|
| Source Font File | ไฟล์ .ttf ไทยที่ลากเข้ามา |
| Sampling Point Size | Auto Sizing |
| **Padding** | **5** (สำคัญ — กันวรรณยุกต์ซ้อนโดนตัด) |
| Packing Method | Fast (final ค่อยเปลี่ยน Optimum) |
| **Atlas Resolution** | **2048 x 2048** (พอสำหรับไทย+ละติน; ถ้า □ ค่อยเพิ่ม 4096) |
| Character Set | **Unicode Range (Hex)** |
| **Unicode Ranges** | `0020-007E,00A0-00FF,0E00-0E7F,2000-206F` |
| Render Mode | SDFAA |

(`0E00-0E7F` = อักษรไทยทั้งหมด, `0020-007E` = ละติน/ตัวเลข, `2000-206F` = เครื่องหมาย … — " ")

→ กด **Generate Font Atlas** → เช็คว่าไม่มี □ ในตัวอย่าง → กด **Save** (ได้ไฟล์ `.asset`)

### 4) ตั้งชื่อ AssetBundle
- คลิกไฟล์ `.asset` ที่เพิ่ง save ใน Project window
- ล่างขวาของ Inspector มี dropdown **AssetBundle** → New... → พิมพ์ **`kanit_sdf_2020`**

### 5) Build เป็น bundle
สร้างไฟล์ `Assets/Editor/TextAssetBundleBuilder.cs`:
```csharp
using System.IO;
using UnityEditor;

public static class TextAssetBundleBuilder
{
    [MenuItem("Tools/Build Font AssetBundle")]
    private static void BuildAllAssetBundles()
    {
        const string folderName = "AssetsBundlePackage";
        if (!Directory.Exists(folderName))
            Directory.CreateDirectory(folderName);

        BuildPipeline.BuildAssetBundles(
            folderName,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows);   // เกม 32-bit; ถ้าโหลดไม่ขึ้นลอง StandaloneWindows64
    }
}
```
- เมนู **Tools → Build Font AssetBundle**
- ได้ไฟล์ชื่อ **`kanit_sdf_2020`** (ไม่มีนามสกุล — ปกติ) ในโฟลเดอร์ `AssetsBundlePackage/`

---
## ติดตั้งในเกม
1. เอาไฟล์ **`kanit_sdf_2020`** ไปวางใน **โฟลเดอร์เกม (ที่เดียวกับ .exe)**
   (ถ้า log ไม่โหลด ลองวางใน `BepInEx\Translation\en\` แทน)
2. แก้ `BepInEx\config\AutoTranslatorConfig.ini` → `[Behaviour]`:
   ```ini
   EnableTextMeshPro=True
   OverrideFontTextMeshPro=kanit_sdf_2020
   ```
   - **`OverrideFontTextMeshPro`** = บังคับทุก TMP เป็น Kanit (ไทยขึ้นแน่ แต่ฟอนต์พิเศษหาย)
   - หรือ **`FallbackFontTextMeshPro=kanit_sdf_2020`** = คงฟอนต์เดิม เฉพาะตัวไทย fallback มา Kanit (สวยกว่า)
   - **ตั้งอันเดียว** ลอง Override ก่อนให้ขึ้นให้ได้ แล้วค่อยลอง Fallback
3. รีสตาร์ทเกม → เช็ค:
   - ✅ □ กลายเป็นไทย
   - เปิด `LogOutput.log` → ไม่มี error โหลด bundle
   - ❌ ถ้ายัง □/ว่าง → เวอร์ชัน Unity หรือ BuildTarget ไม่ตรง (ลอง StandaloneWindows64) หรือ Unicode range ขาด (เพิ่ม padding/atlas)

---
## ทางเลือกเร็ว (ลองก่อนได้ ถ้าไม่อยากลง Unity)
โหลด **TMP_FONT_ASSETBUNDLES** จาก GitHub Release ทางการของ XUnity — มี `arialuni_sdf_<เวอร์ชัน>`
(Arial Unicode ครอบคลุมไทย) ถ้ามีตัวสำหรับ Unity 2020 **อาจ**ขึ้นไทยได้เลยโดยไม่ต้อง build
แต่ไม่ชัวร์ว่า bake ช่วงไทยไว้ไหม — ลองแล้วถ้ายัง □ ก็ต้อง build เอง (Kanit) ตามด้านบน

## แหล่งอ้างอิง
- คู่มือทางการ: XUnity.AutoTranslator Wiki — TextMeshPro Font Asset Creation & Packaging Guide
- ฟอนต์สำเร็จรูป (CJK เป็นหลัก): github.com/bbepis/XUnity.AutoTranslator releases · github.com/as176590811/XUnity-TMP
