# Teacher Quran Specialization Design

## Overview

This document describes the design for enabling teachers to specify their Quran teaching specialization at the unit level, including content type (Memorization/Recitation/Tajweed) and level (Noorani/Beginner/Intermediate/Advanced).

## Problem Statement

Currently, `TeacherSubjectUnit` allows teachers to specify which Quran units (Surahs/Parts) they can teach, but doesn't capture:
- **Content Type**: Can they teach memorization (حفظ), recitation (تلاوة), or Tajweed (تجويد)?
- **Level**: Can they teach beginners, intermediate, or advanced students?

This limitation prevents the system from accurately matching teachers with student needs.

## Proposed Solution

Add `QuranContentTypeId` and `QuranLevelId` to the `TeacherSubjectUnit` entity, making both fields **optional (nullable)**.

### Key Design Principle

**`null` means "ALL"** - A null value indicates the teacher can teach that unit for:
- All content types (if `QuranContentTypeId` is null)
- All levels (if `QuranLevelId` is null)

---

## Quran Units: Surahs vs Parts | الوحدات القرآنية: السور والأجزاء

### Understanding Quran Units | فهم الوحدات القرآنية

The Quran can be divided in two ways, and our system supports both:

القرآن الكريم يمكن تقسيمه بطريقتين، ونظامنا يدعم كلتيهما:

#### 1. **Surahs (السور)** - 114 Surahs
- **Traditional division** by revelation and topic
- **التقسيم التقليدي** حسب النزول والموضوع
- Examples: سورة الفاتحة، سورة البقرة، سورة آل عمران...
- Each Surah has a name, number, and specific verses
- كل سورة لها اسم ورقم وآيات محددة

#### 2. **Parts (الأجزاء)** - 30 Parts (Juz)
- **Equal-length division** for easier memorization planning
- **تقسيم متساوي الطول** لتسهيل خطة الحفظ
- Examples: الجزء الأول، الجزء العم (30)، الجزء تبارك (29)...
- Each part contains approximately the same amount of text
- كل جزء يحتوي تقريباً على نفس كمية النص

### ContentUnit with UnitTypeCode

في قاعدة البيانات، كل وحدة (سورة أو جزء) هي `ContentUnit` مع `UnitTypeCode`:

```csharp
ContentUnit
{
    Id = 1,
    SubjectId = 499,  // القرآن الكريم
    NameAr = "سورة الفاتحة",
    NameEn = "Surah Al-Fatiha",
    UnitTypeCode = "QuranSurah",  // ← نوع الوحدة
    // ...
}

ContentUnit
{
    Id = 115,
    SubjectId = 499,
    NameAr = "الجزء الأول",
    NameEn = "Part 1",
    UnitTypeCode = "QuranPart",  // ← نوع الوحدة
    // ...
}
```

---

## Comprehensive Scenarios | السيناريوهات الشاملة

### Complete Teacher Specialization Matrix | مصفوفة التخصصات الكاملة

معلم القرآن يمكنه التخصص في:

| البعد | الخيارات | المعنى |
|------|---------|--------|
| **نوع الوحدة** | سورة / جزء / كلاهما | ماذا يدرس |
| **نوع المحتوى** | حفظ / تلاوة / تجويد / الكل | كيف يدرس |
| **المستوى** | نوراني / مبتدئ / متوسط / متقدم / الكل | لمن يدرس |

---

### Advanced Scenario Examples | أمثلة متقدمة

#### 📚 **Scenario A: Teacher Specializes in Specific Surahs**
#### السيناريو أ: معلم متخصص في سور محددة

```csharp
معلم محمد - متخصص في السور الطوال

TeacherSubject: القرآن الكريم (CanTeachFullSubject = false)

TeacherSubjectUnits:
[
    {
        Unit = "سورة البقرة" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 1,  // حفظ
        QuranLevelId = null      // كل المستويات
    },
    {
        Unit = "سورة آل عمران" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 1,  // حفظ
        QuranLevelId = null      // كل المستويات
    },
    {
        Unit = "سورة النساء" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 1,  // حفظ
        QuranLevelId = null      // كل المستويات
    }
]
```

**ماذا يعني هذا؟**
- ✅ يدرس حفظ السور الطوال (البقرة، آل عمران، النساء)
- ✅ يستطيع تدريس أي مستوى (من مبتدئ لمتقدم)
- ❌ لا يدرس التجويد أو التلاوة فقط
- ❌ لا يدرس الأجزاء (Parts)
- ❌ لا يدرس سور أخرى

**متى يتطابق مع طالب؟**
- طالب يريد حفظ سورة البقرة (أي مستوى) ✅
- طالب يريد تجويد سورة البقرة ❌
- طالب يريد حفظ الجزء الأول ❌

---

#### 📖 **Scenario B: Teacher Specializes in Juz (Parts)**
#### السيناريو ب: معلم متخصص في الأجزاء

```csharp
معلمة عائشة - متخصصة في الأجزاء الأخيرة

TeacherSubject: القرآن الكريم (CanTeachFullSubject = false)

TeacherSubjectUnits:
[
    {
        Unit = "الجزء 28" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = null,  // كل الأنواع
        QuranLevelId = 1            // نوراني
    },
    {
        Unit = "الجزء 29 - تبارك" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = null,  // كل الأنواع
        QuranLevelId = 1            // نوراني
    },
    {
        Unit = "الجزء 30 - عم" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = null,  // كل الأنواع
        QuranLevelId = 1            // نوراني
    }
]
```

**ماذا يعني هذا؟**
- ✅ تدرس الأجزاء الثلاثة الأخيرة (عم، تبارك، قد سمع)
- ✅ تستطيع تدريس حفظ، تلاوة، أو تجويد
- ✅ متخصصة في المستوى النوراني (المبتدئين تماماً)
- ❌ لا تدرس مستويات أخرى
- ❌ لا تدرس أجزاء أخرى
- ❌ لا تدرس السور منفصلة

**متى تتطابق مع طالب؟**
- طالب نوراني يريد حفظ جزء عم ✅
- طالب نوراني يريد تلاوة جزء تبارك ✅
- طالب متوسط يريد حفظ جزء عم ❌
- طالب نوراني يريد حفظ سورة الفاتحة ❌ (ليست من ضمن تخصصها)

---

#### 🎯 **Scenario C: Mixed Surahs and Parts**
#### السيناريو ج: خليط من السور والأجزاء

```csharp
معلم أحمد - متعدد المهارات

TeacherSubject: القرآن الكريم (CanTeachFullSubject = false)

TeacherSubjectUnits:
[
    // ===== SURAHS =====
    {
        Unit = "سورة الفاتحة" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = null,  // كل الأنواع
        QuranLevelId = null         // كل المستويات
    },
    {
        Unit = "سورة الكهف" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 3,     // تجويد فقط
        QuranLevelId = 4            // متقدم فقط
    },
    {
        Unit = "سورة يس" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 2,     // تلاوة فقط
        QuranLevelId = 3            // متوسط فقط
    },
    
    // ===== PARTS =====
    {
        Unit = "الجزء 30 - عم" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ فقط
        QuranLevelId = 1            // نوراني فقط
    },
    {
        Unit = "الجزء 29 - تبارك" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ فقط
        QuranLevelId = 2            // مبتدئ فقط
    }
]
```

**ماذا يعني هذا؟**

المعلم أحمد لديه تخصصات مختلفة:

| الوحدة | النوع | المحتوى | المستوى | الوصف |
|--------|------|---------|---------|--------|
| سورة الفاتحة | سورة | الكل | الكل | يدرس الفاتحة بأي طريقة لأي مستوى |
| سورة الكهف | سورة | تجويد | متقدم | خبير تجويد متقدم فقط |
| سورة يس | سورة | تلاوة | متوسط | تلاوة متوسطة فقط |
| الجزء 30 | جزء | حفظ | نوراني | حفظ جزء عم للنورانيين |
| الجزء 29 | جزء | حفظ | مبتدئ | حفظ جزء تبارك للمبتدئين |

**أمثلة المطابقة:**

| الطلب | المطابقة | السبب |
|-------|----------|-------|
| طالب نوراني يريد حفظ جزء عم | ✅ نعم | متطابق تماماً |
| طالب مبتدئ يريد حفظ جزء تبارك | ✅ نعم | متطابق تماماً |
| طالب متقدم يريد تجويد الكهف | ✅ نعم | متطابق تماماً |
| طالب متوسط يريد تلاوة يس | ✅ نعم | متطابق تماماً |
| طالب متقدم يريد حفظ الفاتحة | ✅ نعم | الفاتحة لكل الأنواع والمستويات |
| طالب نوراني يريد تلاوة الفاتحة | ✅ نعم | الفاتحة لكل الأنواع والمستويات |
| طالب مبتدئ يريد تجويد الكهف | ❌ لا | الكهف للمتقدمين فقط |
| طالب متقدم يريد حفظ جزء عم | ❌ لا | جزء عم للنورانيين فقط |
| طالب نوراني يريد تجويد يس | ❌ لا | يس للمتوسطين فقط |

---

#### 🌟 **Scenario D: Complete Part Coverage with Different Levels**
#### السيناريو د: تغطية كاملة للأجزاء بمستويات مختلفة

```csharp
معلمة خديجة - متخصصة في الحفظ للأجزاء الثلاثة الأخيرة

TeacherSubject: القرآن الكريم (CanTeachFullSubject = false)

TeacherSubjectUnits:
[
    // الجزء 30 - كل المستويات
    {
        Unit = "الجزء 30 - عم" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ
        QuranLevelId = null         // كل المستويات
    },
    
    // الجزء 29 - مبتدئ ومتوسط فقط
    {
        Unit = "الجزء 29 - تبارك" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ
        QuranLevelId = 2            // مبتدئ
    },
    {
        Unit = "الجزء 29 - تبارك" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ
        QuranLevelId = 3            // متوسط
    },
    
    // الجزء 28 - متقدم فقط
    {
        Unit = "الجزء 28 - قد سمع" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ
        QuranLevelId = 4            // متقدم
    }
]
```

**ملاحظة مهمة**: نفس الوحدة (الجزء 29) ظهرت مرتين بمستويات مختلفة!

**هل هذا ممكن؟** نعم! ولكن يجب معالجته بحذر في الكود:

```csharp
// Option 1: Allow multiple records (current design)
// يسمح بتسجيلات متعددة لنفس الوحدة

// Option 2: Query combines with OR
// البحث يجمع النتائج بـ OR

SELECT * FROM TeacherSubjectUnits
WHERE UnitId = [الجزء 29]
  AND QuranContentTypeId = 1
  AND (QuranLevelId = 2 OR QuranLevelId = 3)  -- مبتدئ أو متوسط
```

**بديل أفضل**: استخدام `null` للمستويات المتعددة:

```csharp
// Instead of 2 records for الجزء 29:
{
    Unit = "الجزء 29",
    QuranContentTypeId = 1,
    QuranLevelId = null  // ← يشمل مبتدئ ومتوسط (لكن ليس نوراني أو متقدم!)
}
// But this loses granularity...
```

**الحل الأمثل**: تصميم إضافي (مستقبلاً):
- إضافة جدول `TeacherSubjectUnitLevels` للمستويات المتعددة
- أو إضافة حقل `AvailableLevels` (JSON array)

لكن **للبساطة الحالية**: نسمح بتسجيلات متعددة.

---

#### 🎓 **Scenario E: Surah-Specific with Part Fallback**
#### السيناريو هـ: سور محددة مع بديل الأجزاء

```csharp
معلم يوسف - يفضل تدريس سور معينة، لكن يستطيع تدريس أجزاء للمبتدئين

TeacherSubject: القرآن الكريم (CanTeachFullSubject = false)

TeacherSubjectUnits:
[
    // === تخصص رئيسي: سور مختارة للمتقدمين ===
    {
        Unit = "سورة البقرة" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 3,     // تجويد
        QuranLevelId = 4            // متقدم
    },
    {
        Unit = "سورة الكهف" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 3,     // تجويد
        QuranLevelId = 4            // متقدم
    },
    {
        Unit = "سورة يس" (UnitTypeCode = "QuranSurah"),
        QuranContentTypeId = 3,     // تجويد
        QuranLevelId = 4            // متقدم
    },
    
    // === تخصص ثانوي: أجزاء للمبتدئين (دخل إضافي) ===
    {
        Unit = "الجزء 30 - عم" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ
        QuranLevelId = 1            // نوراني
    },
    {
        Unit = "الجزء 29 - تبارك" (UnitTypeCode = "QuranPart"),
        QuranContentTypeId = 1,     // حفظ
        QuranLevelId = 1            // نوراني
    }
]
```

**الفكرة**:
- التخصص الرئيسي: تجويد سور معينة للمتقدمين (خبرة عالية)
- التخصص الثانوي: حفظ أجزاء سهلة للمبتدئين (دخل إضافي، تعليم بسيط)

---

## Query Logic for Mixed Units | منطق البحث للوحدات المختلطة

### Finding Teachers by Surah

```sql
-- مثال: البحث عن معلمين لحفظ سورة البقرة (مستوى متوسط)

SELECT DISTINCT t.*
FROM Teachers t
JOIN TeacherSubjects ts ON ts.TeacherId = t.Id
JOIN TeacherSubjectUnits tsu ON tsu.TeacherSubjectId = ts.Id
JOIN ContentUnits cu ON cu.Id = tsu.UnitId
WHERE cu.NameAr = 'سورة البقرة'
  AND cu.UnitTypeCode = 'QuranSurah'  -- ← مهم: نحدد أنها سورة
  AND ts.SubjectId = 499
  AND (tsu.QuranContentTypeId = 1 OR tsu.QuranContentTypeId IS NULL)  -- حفظ
  AND (tsu.QuranLevelId = 3 OR tsu.QuranLevelId IS NULL)              -- متوسط
  AND ts.IsActive = true
  AND t.IsActive = true;
```

### Finding Teachers by Part (Juz)

```sql
-- مثال: البحث عن معلمين لحفظ الجزء 30 (مستوى نوراني)

SELECT DISTINCT t.*
FROM Teachers t
JOIN TeacherSubjects ts ON ts.TeacherId = t.Id
JOIN TeacherSubjectUnits tsu ON tsu.TeacherSubjectId = ts.Id
JOIN ContentUnits cu ON cu.Id = tsu.UnitId
WHERE cu.NameAr LIKE '%الجزء 30%'  -- أو cu.Id = [specific part id]
  AND cu.UnitTypeCode = 'QuranPart'  -- ← مهم: نحدد أنه جزء
  AND ts.SubjectId = 499
  AND (tsu.QuranContentTypeId = 1 OR tsu.QuranContentTypeId IS NULL)  -- حفظ
  AND (tsu.QuranLevelId = 1 OR tsu.QuranLevelId IS NULL)              -- نوراني
  AND ts.IsActive = true
  AND t.IsActive = true;
```

### Finding All Units a Teacher Can Teach

```sql
-- مثال: كل الوحدات (سور وأجزاء) التي يستطيع المعلم #123 تدريسها

SELECT 
    cu.NameAr AS UnitName,
    cu.UnitTypeCode,
    CASE 
        WHEN cu.UnitTypeCode = 'QuranSurah' THEN 'سورة'
        WHEN cu.UnitTypeCode = 'QuranPart' THEN 'جزء'
        ELSE 'أخرى'
    END AS UnitType,
    COALESCE(qct.NameAr, 'كل الأنواع') AS ContentType,
    COALESCE(ql.NameAr, 'كل المستويات') AS Level
FROM TeacherSubjectUnits tsu
JOIN TeacherSubjects ts ON ts.Id = tsu.TeacherSubjectId
JOIN ContentUnits cu ON cu.Id = tsu.UnitId
LEFT JOIN QuranContentTypes qct ON qct.Id = tsu.QuranContentTypeId
LEFT JOIN QuranLevels ql ON ql.Id = tsu.QuranLevelId
WHERE ts.TeacherId = 123
  AND ts.IsActive = true
ORDER BY 
    cu.UnitTypeCode,  -- سور أولاً، ثم أجزاء
    cu.OrderIndex;    -- ترتيب طبيعي
```

---

## Important Considerations | اعتبارات مهمة

### 1. **Overlapping Coverage | التغطية المتداخلة**

**مشكلة**: سورة الفاتحة موجودة في الجزء الأول

```
- سورة الفاتحة (كوحدة منفصلة)
- الجزء الأول (يحتوي على الفاتحة + جزء من البقرة)
```

**سؤال**: إذا معلم يدرس الجزء الأول، هل يدرس سورة الفاتحة؟

**الجواب في نظامنا**: **لا** - هما وحدتان منفصلتان!

```csharp
// المعلم يدرس الجزء الأول
TeacherSubjectUnit { UnitId = [الجزء الأول] }

// لا يعني تلقائياً أنه يدرس الفاتحة منفصلة
// الطالب لو بحث عن "سورة الفاتحة" لن يجد هذا المعلم
// إلا إذا أضاف المعلم سورة الفاتحة صراحة
```

**الحل**: المعلم يجب أن يضيف الوحدتين منفصلتين إذا كان يدرسهما:
```csharp
[
    { UnitId = [سورة الفاتحة] },  // ← منفصلة
    { UnitId = [الجزء الأول] }     // ← منفصل
]
```

### 2. **UI/UX Consideration | اعتبارات واجهة المستخدم**

عند إضافة وحدات للمعلم:

```
□ السور (Surahs)
  □ سورة الفاتحة
  □ سورة البقرة
  □ سورة آل عمران
  ...

□ الأجزاء (Parts)
  □ الجزء 1
  □ الجزء 2
  ...
  □ الجزء 30 - عم
```

**لكل وحدة يختارها**:
```
[سورة البقرة]
  نوع المحتوى: [ ] الكل  [ ] حفظ  [ ] تلاوة  [ ] تجويد
  المستوى:     [ ] الكل  [ ] نوراني  [ ] مبتدئ  [ ] متوسط  [ ] متقدم
```

### 3. **Performance | الأداء**

**Index Strategy**:
```sql
-- مهم جداً للأداء
CREATE INDEX IX_ContentUnits_UnitTypeCode 
ON ContentUnits(UnitTypeCode, SubjectId);

CREATE INDEX IX_TeacherSubjectUnits_Composite
ON TeacherSubjectUnits(TeacherSubjectId, UnitId, QuranContentTypeId, QuranLevelId);
```

---

## Summary | الخلاصة

### التصميم يدعم:
- ✅ السور (114 سورة)
- ✅ الأجزاء (30 جزء)
- ✅ خليط من السور والأجزاء
- ✅ تخصصات مختلفة لكل وحدة
- ✅ مرونة كاملة في نوع المحتوى والمستوى

### كل معلم يمكنه:
1. تحديد السور التي يدرسها
2. تحديد الأجزاء التي يدرسها
3. تحديد نوع المحتوى لكل وحدة (حفظ/تلاوة/تجويد/الكل)
4. تحديد المستوى لكل وحدة (نوراني/مبتدئ/متوسط/متقدم/الكل)
5. خلط كل ما سبق بأي طريقة!

### النظام يضمن:
- 🎯 مطابقة دقيقة بين المعلم والطالب
- 🔍 بحث سريع وفعال
- 📊 تقارير واضحة عن تخصصات المعلمين
- 💪 مرونة قصوى للتوسع المستقبلي

---

## Entity Changes

### TeacherSubjectUnit (Modified)

```csharp
using Qalam.Data.Commons;
using Qalam.Data.Entity.Education;
using Qalam.Data.Entity.Quran;

namespace Qalam.Data.Entity.Teacher;

/// <summary>
/// الوحدات المحددة التي يدرسها المعلم (إذا لم يكن يدرس المادة كاملة)
/// </summary>
public class TeacherSubjectUnit : AuditableEntity
{
    public int Id { get; set; }
    
    public int TeacherSubjectId { get; set; }
    public int UnitId { get; set; }
    
    // Quran-specific fields (optional)
    // null = can teach this unit for ALL content types
    public int? QuranContentTypeId { get; set; }
    
    // null = can teach this unit for ALL levels
    public int? QuranLevelId { get; set; }
    
    // Navigation Properties
    public TeacherSubject TeacherSubject { get; set; } = null!;
    public ContentUnit Unit { get; set; } = null!;
    public QuranContentType? QuranContentType { get; set; }
    public QuranLevel? QuranLevel { get; set; }
}
```

---

## Supported Scenarios | السيناريوهات المدعومة

### Scenario 1: Teacher Can Teach Unit for All Types & Levels
### السيناريو الأول: معلم يدرس الوحدة لكل الأنواع والمستويات

**Use Case**: Teacher is versatile and can teach a Surah in any way to any level

**حالة الاستخدام**: معلم متعدد المهارات يستطيع تدريس السورة بأي طريقة ولأي مستوى

```csharp
TeacherSubjectUnit
{
    Unit = "سورة الفاتحة",
    QuranContentTypeId = null,  // ← Can teach: Memorization, Recitation, Tajweed
    QuranLevelId = null         // ← Can teach: All levels
}
```

**Meaning**: Teacher can teach Surah Al-Fatiha for memorization, recitation, or Tajweed to any level (beginner to advanced).

**المعنى**: المعلم يستطيع تدريس سورة الفاتحة (حفظ، تلاوة، أو تجويد) لأي مستوى (من المبتدئ إلى المتقدم).

**مثال عملي**:
- طالب مبتدئ يريد حفظ الفاتحة ✅
- طالب متوسط يريد تعلم تلاوة الفاتحة ✅
- طالب متقدم يريد إتقان تجويد الفاتحة ✅

---

### Scenario 2: Teacher Specializes in Content Type Only
### السيناريو الثاني: معلم متخصص في نوع المحتوى فقط

**Use Case**: Teacher specializes in memorization but can teach all levels

**حالة الاستخدام**: معلم متخصص في التحفيظ لكن يستطيع التدريس لكل المستويات

```csharp
TeacherSubjectUnit
{
    Unit = "سورة البقرة",
    QuranContentTypeId = 1,     // ← Memorization ONLY | حفظ فقط
    QuranLevelId = null         // ← All levels | كل المستويات
}
```

**Meaning**: Teacher only teaches memorization of Surah Al-Baqarah, but can teach beginners through advanced students.

**المعنى**: المعلم يدرس حفظ سورة البقرة فقط، لكن لجميع المستويات من المبتدئ إلى المتقدم.

**مثال عملي**:
- طالب نوراني يريد حفظ البقرة ✅
- طالب متقدم يريد حفظ البقرة ✅
- طالب يريد تجويد البقرة (بدون حفظ) ❌ لا يستطيع هذا المعلم

**لماذا هذا السيناريو مفيد؟**
- معلم متخصص في الحفظ وطرق التثبيت
- لديه خبرة مع كل الأعمار (صغار وكبار)
- لكنه لا يدرس التجويد أو التلاوة

---

### Scenario 3: Teacher Specializes in Level Only
### السيناريو الثالث: معلم متخصص في المستوى فقط

**Use Case**: Teacher works with beginners only but can teach any type

**حالة الاستخدام**: معلم يعمل مع المبتدئين فقط لكن يستطيع تدريس أي نوع

```csharp
TeacherSubjectUnit
{
    Unit = "سورة يس",
    QuranContentTypeId = null,  // ← All types | كل الأنواع
    QuranLevelId = 2            // ← Beginner ONLY | مبتدئ فقط
}
```

**Meaning**: Teacher can teach Surah Yasin (memorization, recitation, or Tajweed) but only to beginner students.

**المعنى**: المعلم يستطيع تدريس سورة يس (حفظ، تلاوة، أو تجويد) لكن للطلاب المبتدئين فقط.

**مثال عملي**:
- طالب مبتدئ يريد حفظ يس ✅
- طالب مبتدئ يريد تعلم تلاوة يس ✅
- طالب مبتدئ يريد تجويد يس ✅
- طالب متقدم يريد أي شيء ❌ لا يستطيع هذا المعلم

**لماذا هذا السيناريو مفيد؟**
- معلم متخصص في تأسيس المبتدئين
- صبور ولديه أساليب تناسب المبتدئين
- يستطيع تدريس الحفظ والتلاوة والتجويد، لكن بمستوى مبسط

---

### Scenario 4: Teacher Specializes in Both Type & Level
### السيناريو الرابع: معلم متخصص في النوع والمستوى معاً

**Use Case**: Expert Tajweed teacher for advanced students only

**حالة الاستخدام**: معلم تجويد خبير للطلاب المتقدمين فقط

```csharp
TeacherSubjectUnit
{
    Unit = "سورة الكهف",
    QuranContentTypeId = 3,     // ← Tajweed ONLY | تجويد فقط
    QuranLevelId = 4            // ← Advanced ONLY | متقدم فقط
}
```

**Meaning**: Teacher only teaches advanced Tajweed for Surah Al-Kahf.

**المعنى**: المعلم يدرس تجويد سورة الكهف للمستوى المتقدم فقط.

**مثال عملي**:
- طالب متقدم يريد إتقان تجويد الكهف ✅
- طالب مبتدئ يريد تجويد الكهف ❌
- طالب متقدم يريد حفظ الكهف ❌
- طالب متوسط يريد تجويد الكهف ❌

**لماذا هذا السيناريو مفيد؟**
- معلم خبير في التجويد المتقدم
- يركز على الأحكام الدقيقة والتطبيق العملي
- لا يريد التعامل مع المبتدئين
- متخصص في التجويد فقط (ليس الحفظ)

---

### Scenario 5: Mixed Specializations (Most Flexible) ⭐
### السيناريو الخامس: تخصصات مختلطة (الأكثر مرونة) ⭐

**Use Case**: Teacher has different specializations for different Surahs

**حالة الاستخدام**: معلم لديه تخصصات مختلفة لسور مختلفة

```csharp
Teacher: Fatimah | المعلمة: فاطمة

TeacherSubject
{
    SubjectId = 499,  // القرآن الكريم
    CanTeachFullSubject = false
}

TeacherSubjectUnits:
[
    {
        Unit = "سورة الفاتحة",
        QuranContentTypeId = 1,    // Memorization | حفظ
        QuranLevelId = 1           // Noorani (absolute beginners) | نوراني
    },
    {
        Unit = "سورة البقرة",
        QuranContentTypeId = 3,    // Tajweed | تجويد
        QuranLevelId = 4           // Advanced | متقدم
    },
    {
        Unit = "سورة الكهف",
        QuranContentTypeId = 2,    // Recitation | تلاوة
        QuranLevelId = 3           // Intermediate | متوسط
    },
    {
        Unit = "سورة يس",
        QuranContentTypeId = null, // ALL types | كل الأنواع
        QuranLevelId = null        // ALL levels | كل المستويات
    }
]
```

**Meaning**: 
- Teaches Surah Al-Fatiha memorization to complete beginners
- Teaches advanced Tajweed for Surah Al-Baqarah
- Teaches intermediate recitation for Surah Al-Kahf
- Teaches Surah Yasin in any format to any level

**المعنى بالعربي**:
- تدرس حفظ سورة الفاتحة للمبتدئين تماماً (مستوى نوراني)
- تدرس تجويد سورة البقرة للمستوى المتقدم فقط
- تدرس تلاوة سورة الكهف للمستوى المتوسط
- تدرس سورة يس بأي طريقة ولأي مستوى

**أمثلة عملية**:

| الطلب | هل المعلمة فاطمة تستطيع؟ | السبب |
|-------|-------------------------|-------|
| طالب نوراني يريد حفظ الفاتحة | ✅ نعم | متطابق تماماً |
| طالب متقدم يريد تجويد البقرة | ✅ نعم | متطابق تماماً |
| طالب مبتدئ يريد تجويد البقرة | ❌ لا | تدرس البقرة للمتقدمين فقط |
| طالب متوسط يريد تلاوة الكهف | ✅ نعم | متطابق تماماً |
| طالب مبتدئ يريد حفظ يس | ✅ نعم | يس لكل المستويات |
| طالب متقدم يريد حفظ الفاتحة | ❌ لا | الفاتحة للنورانيين فقط عندها |

**لماذا هذا السيناريو مهم؟**
- يعكس الواقع العملي للمعلمين
- معلم قد يكون خبير في شيء معين ومبتدئ في شيء آخر
- يعطي أقصى مرونة للنظام
- يضمن مطابقة دقيقة بين المعلم والطالب

---

## Database Changes

### Migration

```csharp
public partial class AddQuranSpecializationToTeacherSubjectUnit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "QuranContentTypeId",
            table: "TeacherSubjectUnits",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "QuranLevelId",
            table: "TeacherSubjectUnits",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_TeacherSubjectUnits_QuranContentTypeId",
            table: "TeacherSubjectUnits",
            column: "QuranContentTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_TeacherSubjectUnits_QuranLevelId",
            table: "TeacherSubjectUnits",
            column: "QuranLevelId");

        migrationBuilder.AddForeignKey(
            name: "FK_TeacherSubjectUnits_QuranContentTypes_QuranContentTypeId",
            table: "TeacherSubjectUnits",
            column: "QuranContentTypeId",
            principalTable: "QuranContentTypes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_TeacherSubjectUnits_QuranLevels_QuranLevelId",
            table: "TeacherSubjectUnits",
            column: "QuranLevelId",
            principalTable: "QuranLevels",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TeacherSubjectUnits_QuranContentTypes_QuranContentTypeId",
            table: "TeacherSubjectUnits");

        migrationBuilder.DropForeignKey(
            name: "FK_TeacherSubjectUnits_QuranLevels_QuranLevelId",
            table: "TeacherSubjectUnits");

        migrationBuilder.DropIndex(
            name: "IX_TeacherSubjectUnits_QuranContentTypeId",
            table: "TeacherSubjectUnits");

        migrationBuilder.DropIndex(
            name: "IX_TeacherSubjectUnits_QuranLevelId",
            table: "TeacherSubjectUnits");

        migrationBuilder.DropColumn(
            name: "QuranContentTypeId",
            table: "TeacherSubjectUnits");

        migrationBuilder.DropColumn(
            name: "QuranLevelId",
            table: "TeacherSubjectUnits");
    }
}
```

---

## Query Examples

### Example 1: Find Teachers for Specific Unit, Type, and Level

**Requirement**: Find teachers who can teach **Surah Al-Baqarah memorization to intermediate students**

```sql
SELECT DISTINCT t.*
FROM Teachers t
JOIN TeacherSubjects ts ON ts.TeacherId = t.Id
JOIN TeacherSubjectUnits tsu ON tsu.TeacherSubjectId = ts.Id
JOIN ContentUnits cu ON cu.Id = tsu.UnitId
WHERE cu.NameAr = 'سورة البقرة'
  AND ts.SubjectId = 499  -- القرآن الكريم
  AND (tsu.QuranContentTypeId = 1 OR tsu.QuranContentTypeId IS NULL)  -- Memorization or ALL
  AND (tsu.QuranLevelId = 3 OR tsu.QuranLevelId IS NULL)              -- Intermediate or ALL
  AND ts.IsActive = true
  AND t.IsActive = true;
```

**Logic**:
- `tsu.QuranContentTypeId = 1` → Teacher specified memorization
- `tsu.QuranContentTypeId IS NULL` → Teacher can teach all types (includes memorization)
- Same logic applies to level

---

### Example 2: Find All Units a Teacher Can Teach (with filters)

**Requirement**: Get all units that Teacher #123 can teach for **Tajweed - Advanced**

```sql
SELECT 
    cu.NameAr AS UnitName,
    qct.NameAr AS ContentType,
    ql.NameAr AS Level,
    CASE 
        WHEN tsu.QuranContentTypeId IS NULL THEN 'All Types'
        ELSE qct.NameAr 
    END AS ActualContentType,
    CASE 
        WHEN tsu.QuranLevelId IS NULL THEN 'All Levels'
        ELSE ql.NameAr 
    END AS ActualLevel
FROM TeacherSubjectUnits tsu
JOIN TeacherSubjects ts ON ts.Id = tsu.TeacherSubjectId
JOIN ContentUnits cu ON cu.Id = tsu.UnitId
LEFT JOIN QuranContentTypes qct ON qct.Id = tsu.QuranContentTypeId
LEFT JOIN QuranLevels ql ON ql.Id = tsu.QuranLevelId
WHERE ts.TeacherId = 123
  AND (tsu.QuranContentTypeId = 3 OR tsu.QuranContentTypeId IS NULL)  -- Tajweed or ALL
  AND (tsu.QuranLevelId = 4 OR tsu.QuranLevelId IS NULL)              -- Advanced or ALL
  AND ts.IsActive = true;
```

---

## Validation Rules

### Rule 1: Quran Domain Only

These fields should only be populated for Quran domain subjects.

```csharp
// Validation in service layer
if (teacherSubject.Subject.DomainId == 2) // Quran domain
{
    // QuranContentTypeId and QuranLevelId are allowed
}
else
{
    // Must be null for non-Quran subjects
    if (teacherSubjectUnit.QuranContentTypeId.HasValue || 
        teacherSubjectUnit.QuranLevelId.HasValue)
    {
        throw new ValidationException(
            "QuranContentTypeId and QuranLevelId can only be set for Quran domain subjects");
    }
}
```

---

### Rule 2: Valid Foreign Keys

If not null, IDs must reference valid records.

```csharp
// Check if QuranContentTypeId exists
if (teacherSubjectUnit.QuranContentTypeId.HasValue)
{
    var exists = await _context.QuranContentTypes
        .AnyAsync(qct => qct.Id == teacherSubjectUnit.QuranContentTypeId.Value);
    
    if (!exists)
        throw new ValidationException("Invalid QuranContentTypeId");
}

// Check if QuranLevelId exists
if (teacherSubjectUnit.QuranLevelId.HasValue)
{
    var exists = await _context.QuranLevels
        .AnyAsync(ql => ql.Id == teacherSubjectUnit.QuranLevelId.Value);
    
    if (!exists)
        throw new ValidationException("Invalid QuranLevelId");
}
```

---

## API Examples

### Adding a Specialized Unit

**Request**: Add Surah Al-Baqarah (Tajweed, Advanced) to Teacher #123

```json
POST /api/teachers/123/subjects/499/units

{
  "unitId": 2,
  "quranContentTypeId": 3,
  "quranLevelId": 4
}
```

**Response**:
```json
{
  "statusCode": 200,
  "succeeded": true,
  "data": {
    "id": 456,
    "teacherSubjectId": 789,
    "unitId": 2,
    "unitName": "سورة البقرة",
    "quranContentTypeId": 3,
    "quranContentTypeName": "تجويد",
    "quranLevelId": 4,
    "quranLevelName": "متقدم"
  }
}
```

---

### Adding a General Unit (All Types & Levels)

**Request**: Add Surah Yasin for all types and levels

```json
POST /api/teachers/123/subjects/499/units

{
  "unitId": 36,
  "quranContentTypeId": null,
  "quranLevelId": null
}
```

**Response**:
```json
{
  "statusCode": 200,
  "succeeded": true,
  "data": {
    "id": 457,
    "teacherSubjectId": 789,
    "unitId": 36,
    "unitName": "سورة يس",
    "quranContentTypeId": null,
    "quranContentTypeName": "All Types",
    "quranLevelId": null,
    "quranLevelName": "All Levels"
  }
}
```

---

## Benefits

### 1. **Maximum Flexibility** ✅
- Supports all teaching scenarios from general to highly specialized
- Each unit can have independent content type and level specification

### 2. **Accurate Matching** ✅
- System can precisely match teachers with student requirements
- Reduces mismatches between teacher capabilities and student needs

### 3. **Scalable Design** ✅
- Can be extended to other domains if needed
- Follows existing nullable pattern used for `GradeId`, `LevelId`, etc.

### 4. **Query Performance** ✅
- Direct filtering without complex joins
- Indexed foreign keys for fast lookups

### 5. **User-Friendly** ✅
- Default behavior (null = all) is intuitive
- Progressive disclosure: specify only what's needed

---

## Future Enhancements

### Possible Extensions

1. **Preference Levels**: Add `PreferenceLevel` (Preferred, Capable, Willing to Learn)
2. **Certification**: Link to certificates for specific specializations
3. **Experience Years**: Track years of experience per specialization
4. **Student Ratings**: Rating per specialization type
5. **Availability**: Different availability for different specializations

---

## Implementation Checklist

- [ ] Update `TeacherSubjectUnit` entity
- [ ] Create and run migration
- [ ] Update DTOs (Add/Update/Response)
- [ ] Add validation rules in service layer
- [ ] Update repository methods to include new fields
- [ ] Update API endpoints
- [ ] Add unit tests for all scenarios
- [ ] Update API documentation (Swagger)
- [ ] Update seeding data (if applicable)
- [ ] Test with real data scenarios

---

## Related Documents

- [Teacher Registration Documentation](./Teacher-Registration.md)
- [Quran Domain Structure](../SEEDING_STRUCTURE.md)
- [Education Filter Service](../BUSINESS_LOGIC.md)

---

**Document Version**: 1.0  
**Created**: 2026-01-29  
**Last Updated**: 2026-01-29
