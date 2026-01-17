# منصة قلم التعليمية - دليل منطق الأعمال الشامل

> **للفريق الأمامي (Frontend Team)** | آخر تحديث: يناير 2026

---

## جدول المحتويات

1. [نظرة عامة على النظام](#نظرة-عامة-على-النظام)
2. [هيكل الكيانات والعلاقات](#هيكل-الكيانات-والعلاقات)
3. [المجالات التعليمية](#المجالات-التعليمية)
4. [العمليات التفصيلية (CRUD)](#العمليات-التفصيلية-crud)
5. [سيناريوهات الاستخدام](#سيناريوهات-الاستخدام)
6. [منطق واجهة المستخدم](#منطق-واجهة-المستخدم)
7. [قواعد التحقق](#قواعد-التحقق)
8. [الأخطاء الشائعة وحلولها](#الأخطاء-الشائعة-وحلولها)

---

## نظرة عامة على النظام

### وصف المنصة

منصة **قلم** هي منصة تعليمية متكاملة تدعم عدة مجالات تعليمية:

| المجال | الوصف | يحتوي منهج؟ |
|--------|-------|-------------|
| 🏫 التعليم المدرسي | التعليم النظامي مع مناهج وفصول دراسية | ✅ نعم |
| 📖 القرآن الكريم | تحفيظ وتلاوة القرآن الكريم | ❌ لا |
| 🌍 اللغات | تعلم اللغات المختلفة | ❌ لا |
| 💡 المهارات العامة | المهارات الحياتية والتقنية | ❌ لا |

### الفرق الرئيسي

- **المجالات ذات المنهج** (مثل التعليم المدرسي): تحتاج اختيار منهج → فصل دراسي
- **المجالات بدون منهج** (مثل القرآن): تتعامل مباشرة مع المستويات

---

## هيكل الكيانات والعلاقات

### الرسم التوضيحي للعلاقات

```
                    ┌─────────────────────────────────────┐
                    │      المجال التعليمي (Domain)        │
                    │   مثال: التعليم المدرسي، القرآن       │
                    │   HasCurriculum: true/false         │
                    └─────────────────────────────────────┘
                                     │
              ┌──────────────────────┼──────────────────────┐
              │                      │                      │
              ▼                      │                      │
   ┌─────────────────────┐           │                      │
   │   المنهج (Curriculum) │           │                      │
   │  (فقط إذا HasCurriculum)│           │                      │
   │  مثال: السعودي، المصري │           │                      │
   └─────────────────────┘           │                      │
              │                      │                      │
              ▼                      │                      │
   ┌─────────────────────┐           │                      │
   │ الفصل الدراسي (Term) │           │                      │
   │  مثال: الفصل الأول    │           │                      │
   └─────────────────────┘           │                      │
                                     │                      │
                                     ▼                      │
                    ┌─────────────────────────────────────┐ │
                    │      المرحلة التعليمية (Level)       │◄┘
                    │   مثال: ابتدائي، متوسط، ثانوي        │
                    └─────────────────────────────────────┘
                                     │
                                     ▼
                    ┌─────────────────────────────────────┐
                    │        الصف الدراسي (Grade)         │
                    │   مثال: الأول، الثاني، الثالث        │
                    └─────────────────────────────────────┘
                                     │
                                     ▼
                    ┌─────────────────────────────────────┐
                    │         المادة الدراسية (Subject)    │
                    │   مثال: الرياضيات، اللغة العربية      │
                    │   (يمكن ربطها بأي مستوى في الهرم)    │
                    └─────────────────────────────────────┘
```

### تفاصيل كل كيان

#### 1. المجال التعليمي (EducationDomain)

| الحقل | النوع | مطلوب | الوصف |
|-------|------|-------|-------|
| `id` | int | تلقائي | المعرف الفريد |
| `nameAr` | string | ✅ | الاسم بالعربية |
| `nameEn` | string | ✅ | الاسم بالإنجليزية |
| `code` | string | ✅ | الرمز (school, quran, language, skills) |
| `hasCurriculum` | bool | ✅ | **مهم جداً**: هل يحتوي على منهج؟ |
| `descriptionAr` | string | ❌ | الوصف بالعربية |
| `descriptionEn` | string | ❌ | الوصف بالإنجليزية |
| `isActive` | bool | ✅ | هل مفعّل؟ |

#### 2. المنهج الدراسي (Curriculum)

> ⚠️ **يُستخدم فقط عندما** `Domain.HasCurriculum = true`

| الحقل | النوع | مطلوب | الوصف |
|-------|------|-------|-------|
| `id` | int | تلقائي | المعرف الفريد |
| `nameAr` | string | ✅ | مثل: "المنهج السعودي" |
| `nameEn` | string | ✅ | مثل: "Saudi Curriculum" |
| `country` | string | ❌ | الدولة |
| `descriptionAr` | string | ❌ | الوصف بالعربية |
| `descriptionEn` | string | ❌ | الوصف بالإنجليزية |
| `isActive` | bool | ✅ | هل مفعّل؟ |

#### 3. المرحلة التعليمية (EducationLevel)

| الحقل | النوع | مطلوب | الوصف |
|-------|------|-------|-------|
| `id` | int | تلقائي | المعرف الفريد |
| `domainId` | int | ✅ | المجال التابع له |
| `curriculumId` | int | ❌ | المنهج (فقط إذا المجال له منهج) |
| `nameAr` | string | ✅ | مثل: "المرحلة الابتدائية" |
| `nameEn` | string | ✅ | مثل: "Primary Stage" |
| `orderIndex` | int | ✅ | ترتيب العرض |
| `isActive` | bool | ✅ | هل مفعّل؟ |

#### 4. الصف الدراسي (Grade)

| الحقل | النوع | مطلوب | الوصف |
|-------|------|-------|-------|
| `id` | int | تلقائي | المعرف الفريد |
| `levelId` | int | ✅ | المرحلة التابع لها |
| `nameAr` | string | ✅ | مثل: "الصف الأول" |
| `nameEn` | string | ✅ | مثل: "Grade 1" |
| `orderIndex` | int | ✅ | ترتيب العرض |
| `isActive` | bool | ✅ | هل مفعّل؟ |

#### 5. الفصل الدراسي (AcademicTerm)

> ⚠️ **يُستخدم فقط عندما** `Domain.HasCurriculum = true`

| الحقل | النوع | مطلوب | الوصف |
|-------|------|-------|-------|
| `id` | int | تلقائي | المعرف الفريد |
| `curriculumId` | int | ✅ | المنهج التابع له |
| `nameAr` | string | ✅ | مثل: "الفصل الأول" |
| `nameEn` | string | ✅ | مثل: "First Term" |
| `orderIndex` | int | ✅ | ترتيب العرض |
| `isMandatory` | bool | ✅ | هل إلزامي؟ |
| `isActive` | bool | ✅ | هل مفعّل؟ |

#### 6. المادة الدراسية (Subject)

> 📌 **مرنة**: يمكن ربطها بأي مستوى في الهرم

| الحقل | النوع | مطلوب | الوصف |
|-------|------|-------|-------|
| `id` | int | تلقائي | المعرف الفريد |
| `domainId` | int | ✅ | **مطلوب دائماً** |
| `curriculumId` | int | ❌ | اختياري |
| `levelId` | int | ❌ | اختياري |
| `gradeId` | int | ❌ | اختياري |
| `termId` | int | ❌ | اختياري |
| `nameAr` | string | ✅ | مثل: "الرياضيات" |
| `nameEn` | string | ✅ | مثل: "Mathematics" |
| `descriptionAr` | string | ❌ | الوصف بالعربية |
| `descriptionEn` | string | ❌ | الوصف بالإنجليزية |
| `isActive` | bool | ✅ | هل مفعّل؟ |

---

## المجالات التعليمية

### 1. التعليم المدرسي (school)

```
HasCurriculum = true

المسار الكامل:
المجال → المنهج → المرحلة → الصف → الفصل → المادة

مثال:
التعليم المدرسي → المنهج السعودي → المرحلة الابتدائية → الصف الأول → الفصل الأول → الرياضيات
```

### 2. القرآن الكريم (quran)

```
HasCurriculum = false

المسار:
المجال → المرحلة → المادة

مثال:
القرآن الكريم → مستوى المبتدئين → الحفظ
```

### 3. اللغات (language)

```
HasCurriculum = false

المسار:
المجال → المرحلة → المادة

مثال:
اللغات → المستوى المتوسط → اللغة الإنجليزية
```

### 4. المهارات العامة (skills)

```
HasCurriculum = false

المسار:
المجال → المادة (مباشرة)

مثال:
المهارات العامة → مهارات التواصل
```

---

## العمليات التفصيلية (CRUD)

### 1. المجالات التعليمية (Domains)

#### عرض جميع المجالات

```http
GET /Api/V1/Education/Domains?pageNumber=1&pageSize=10&search=
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "message": null,
    "data": {
        "items": [
            {
                "id": 1,
                "nameAr": "التعليم المدرسي",
                "nameEn": "School Education",
                "code": "school",
                "hasCurriculum": true,
                "descriptionAr": "التعليم النظامي من الابتدائي للثانوي",
                "descriptionEn": "Formal education from primary to high school",
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            },
            {
                "id": 2,
                "nameAr": "القرآن الكريم",
                "nameEn": "Holy Quran",
                "code": "quran",
                "hasCurriculum": false,
                "descriptionAr": "تحفيظ وتلاوة القرآن الكريم",
                "descriptionEn": "Quran memorization and recitation",
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            }
        ],
        "totalCount": 4,
        "pageNumber": 1,
        "pageSize": 10,
        "totalPages": 1
    },
    "errors": null
}
```

#### عرض مجال واحد

```http
GET /Api/V1/Education/Domains/1
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "message": null,
    "data": {
        "id": 1,
        "nameAr": "التعليم المدرسي",
        "nameEn": "School Education",
        "code": "school",
        "hasCurriculum": true,
        "descriptionAr": "التعليم النظامي",
        "descriptionEn": "Formal education",
        "isActive": true,
        "createdAt": "2026-01-01T00:00:00"
    },
    "errors": null
}
```

**استجابة غير موجود:**
```json
{
    "statusCode": 404,
    "succeeded": false,
    "message": "Education domain not found",
    "data": null,
    "errors": null
}
```

#### إنشاء مجال جديد (Admin فقط)

```http
POST /Api/V1/Education/Domains
Authorization: Bearer {token}
Content-Type: application/json

{
    "nameAr": "البرمجة",
    "nameEn": "Programming",
    "code": "programming",
    "hasCurriculum": false,
    "descriptionAr": "تعلم البرمجة",
    "descriptionEn": "Learn programming",
    "isActive": true
}
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 201,
    "succeeded": true,
    "message": "Created Successfully",
    "data": {
        "id": 5,
        "nameAr": "البرمجة",
        "nameEn": "Programming",
        "code": "programming",
        "hasCurriculum": false,
        "isActive": true,
        "createdAt": "2026-01-16T12:00:00"
    },
    "errors": null
}
```

**استجابة خطأ (الكود موجود مسبقاً):**
```json
{
    "statusCode": 400,
    "succeeded": false,
    "message": "Domain code already exists",
    "data": null,
    "errors": null
}
```

---

### 2. المناهج (Curriculums)

#### عرض جميع المناهج

```http
GET /Api/V1/Curriculum?pageNumber=1&pageSize=10&search=
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "data": {
        "items": [
            {
                "id": 1,
                "nameAr": "المنهج السعودي",
                "nameEn": "Saudi Curriculum",
                "country": "SA",
                "descriptionAr": "منهج المملكة العربية السعودية",
                "descriptionEn": "Saudi Arabia curriculum",
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            },
            {
                "id": 2,
                "nameAr": "المنهج المصري",
                "nameEn": "Egyptian Curriculum",
                "country": "EG",
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            }
        ],
        "totalCount": 2,
        "pageNumber": 1,
        "pageSize": 10
    }
}
```

#### إنشاء منهج جديد

```http
POST /Api/V1/Curriculum
Authorization: Bearer {token}
Content-Type: application/json

{
    "nameAr": "المنهج الإماراتي",
    "nameEn": "UAE Curriculum",
    "country": "AE",
    "descriptionAr": "منهج دولة الإمارات",
    "descriptionEn": "United Arab Emirates curriculum",
    "isActive": true
}
```

#### تعديل منهج

```http
PUT /Api/V1/Curriculum/1
Authorization: Bearer {token}
Content-Type: application/json

{
    "id": 1,
    "nameAr": "المنهج السعودي المطور",
    "nameEn": "Updated Saudi Curriculum",
    "country": "SA",
    "isActive": true
}
```

#### حذف منهج

```http
DELETE /Api/V1/Curriculum/1
Authorization: Bearer {token}
```

**استجابة النجاح:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "message": "Deleted Successfully",
    "data": null
}
```

**استجابة خطأ (يوجد مراحل مرتبطة):**
```json
{
    "statusCode": 400,
    "succeeded": false,
    "message": "Cannot delete curriculum with existing education levels",
    "data": null
}
```

#### تفعيل/إلغاء تفعيل منهج

```http
PATCH /Api/V1/Curriculum/1/toggle-status
Authorization: Bearer {token}
```

**الاستجابة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "message": "Curriculum status toggled successfully",
    "data": true
}
```

---

### 3. المراحل التعليمية (Levels)

#### عرض المراحل مع الفلترة

```http
GET /Api/V1/Education/Levels?pageNumber=1&pageSize=10&curriculumId=1&search=
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "data": {
        "items": [
            {
                "id": 1,
                "domainId": 1,
                "domainNameAr": "التعليم المدرسي",
                "domainNameEn": "School Education",
                "curriculumId": 1,
                "curriculumNameAr": "المنهج السعودي",
                "curriculumNameEn": "Saudi Curriculum",
                "nameAr": "المرحلة الابتدائية",
                "nameEn": "Primary Stage",
                "orderIndex": 1,
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            },
            {
                "id": 2,
                "domainId": 1,
                "domainNameAr": "التعليم المدرسي",
                "domainNameEn": "School Education",
                "curriculumId": 1,
                "curriculumNameAr": "المنهج السعودي",
                "curriculumNameEn": "Saudi Curriculum",
                "nameAr": "المرحلة المتوسطة",
                "nameEn": "Middle Stage",
                "orderIndex": 2,
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            }
        ],
        "totalCount": 3,
        "pageNumber": 1,
        "pageSize": 10
    }
}
```

#### إنشاء مرحلة جديدة

```http
POST /Api/V1/Education/Levels
Authorization: Bearer {token}
Content-Type: application/json

{
    "domainId": 1,
    "curriculumId": 1,
    "nameAr": "المرحلة الثانوية",
    "nameEn": "High School Stage",
    "orderIndex": 3,
    "isActive": true
}
```

---

### 4. الصفوف الدراسية (Grades)

#### عرض الصفوف حسب المرحلة

```http
GET /Api/V1/Education/Grades?pageNumber=1&pageSize=10&levelId=1&search=
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "data": {
        "items": [
            {
                "id": 1,
                "levelId": 1,
                "levelNameAr": "المرحلة الابتدائية",
                "levelNameEn": "Primary Stage",
                "nameAr": "الصف الأول",
                "nameEn": "Grade 1",
                "orderIndex": 1,
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            },
            {
                "id": 2,
                "levelId": 1,
                "levelNameAr": "المرحلة الابتدائية",
                "levelNameEn": "Primary Stage",
                "nameAr": "الصف الثاني",
                "nameEn": "Grade 2",
                "orderIndex": 2,
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            }
        ],
        "totalCount": 6,
        "pageNumber": 1,
        "pageSize": 10
    }
}
```

#### إنشاء صف جديد

```http
POST /Api/V1/Education/Grades
Authorization: Bearer {token}
Content-Type: application/json

{
    "levelId": 1,
    "nameAr": "الصف السادس",
    "nameEn": "Grade 6",
    "orderIndex": 6,
    "isActive": true
}
```

---

### 5. الفصول الدراسية (Terms)

#### عرض الفصول حسب المنهج

```http
GET /Api/V1/Education/Terms?pageNumber=1&pageSize=10&curriculumId=1
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "data": {
        "items": [
            {
                "id": 1,
                "curriculumId": 1,
                "curriculumNameAr": "المنهج السعودي",
                "curriculumNameEn": "Saudi Curriculum",
                "nameAr": "الفصل الدراسي الأول",
                "nameEn": "First Semester",
                "orderIndex": 1,
                "isMandatory": true,
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            },
            {
                "id": 2,
                "curriculumId": 1,
                "curriculumNameAr": "المنهج السعودي",
                "curriculumNameEn": "Saudi Curriculum",
                "nameAr": "الفصل الدراسي الثاني",
                "nameEn": "Second Semester",
                "orderIndex": 2,
                "isMandatory": true,
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            }
        ],
        "totalCount": 3,
        "pageNumber": 1,
        "pageSize": 10
    }
}
```

---

### 6. المواد الدراسية (Subjects)

#### عرض المواد مع كل الفلاتر

```http
GET /Api/V1/Subjects?pageNumber=1&pageSize=10&domainId=1&curriculumId=1&levelId=1&gradeId=1&termId=1&search=
```

**الاستجابة الناجحة:**
```json
{
    "statusCode": 200,
    "succeeded": true,
    "data": {
        "items": [
            {
                "id": 1,
                "domainId": 1,
                "domainNameAr": "التعليم المدرسي",
                "domainNameEn": "School Education",
                "curriculumId": 1,
                "curriculumNameAr": "المنهج السعودي",
                "curriculumNameEn": "Saudi Curriculum",
                "levelId": 1,
                "levelNameAr": "المرحلة الابتدائية",
                "levelNameEn": "Primary Stage",
                "gradeId": 1,
                "gradeNameAr": "الصف الأول",
                "gradeNameEn": "Grade 1",
                "termId": 1,
                "termNameAr": "الفصل الأول",
                "termNameEn": "First Term",
                "nameAr": "الرياضيات",
                "nameEn": "Mathematics",
                "descriptionAr": "مادة الرياضيات للصف الأول",
                "descriptionEn": "Mathematics for Grade 1",
                "isActive": true,
                "createdAt": "2026-01-01T00:00:00"
            }
        ],
        "totalCount": 1,
        "pageNumber": 1,
        "pageSize": 10
    }
}
```

#### إنشاء مادة جديدة

**مادة مدرسية كاملة:**
```http
POST /Api/V1/Subjects
Authorization: Bearer {token}
Content-Type: application/json

{
    "domainId": 1,
    "curriculumId": 1,
    "levelId": 1,
    "gradeId": 1,
    "termId": 1,
    "nameAr": "العلوم",
    "nameEn": "Science",
    "descriptionAr": "مادة العلوم",
    "descriptionEn": "Science subject",
    "isActive": true
}
```

**مادة قرآنية (بدون منهج):**
```http
POST /Api/V1/Subjects
Authorization: Bearer {token}
Content-Type: application/json

{
    "domainId": 2,
    "curriculumId": null,
    "levelId": 5,
    "gradeId": null,
    "termId": null,
    "nameAr": "التلاوة",
    "nameEn": "Recitation",
    "isActive": true
}
```

**مادة مهارات عامة (على مستوى المجال):**
```http
POST /Api/V1/Subjects
Authorization: Bearer {token}
Content-Type: application/json

{
    "domainId": 4,
    "curriculumId": null,
    "levelId": null,
    "gradeId": null,
    "termId": null,
    "nameAr": "مهارات التفكير النقدي",
    "nameEn": "Critical Thinking Skills",
    "isActive": true
}
```

#### تعديل مادة

```http
PUT /Api/V1/Subjects/1
Authorization: Bearer {token}
Content-Type: application/json

{
    "id": 1,
    "domainId": 1,
    "curriculumId": 1,
    "levelId": 1,
    "gradeId": 1,
    "termId": 1,
    "nameAr": "الرياضيات المتقدمة",
    "nameEn": "Advanced Mathematics",
    "isActive": true
}
```

#### حذف مادة

```http
DELETE /Api/V1/Subjects/1
Authorization: Bearer {token}
```

---

## سيناريوهات الاستخدام

### السيناريو 1: طالب يريد اختيار مادة مدرسية

```
1. يختار المجال: "التعليم المدرسي" (domainId: 1)
   ↓ النظام يتحقق: hasCurriculum = true
   ↓ يظهر قائمة المناهج

2. يختار المنهج: "المنهج السعودي" (curriculumId: 1)
   ↓ تُحمّل المراحل والفصول

3. يختار المرحلة: "الابتدائية" (levelId: 1)
   ↓ تُحمّل الصفوف

4. يختار الصف: "الصف الأول" (gradeId: 1)
   ↓ يظهر اختيار الفصل

5. يختار الفصل: "الفصل الأول" (termId: 1)
   ↓ تُحمّل المواد

6. تظهر المواد المتاحة للصف الأول - الفصل الأول
```

### السيناريو 2: طالب يريد حفظ القرآن

```
1. يختار المجال: "القرآن الكريم" (domainId: 2)
   ↓ النظام يتحقق: hasCurriculum = false
   ↓ لا تظهر قائمة المناهج أو الفصول

2. يختار المستوى: "مستوى المبتدئين" (levelId: 5)
   ↓ تُحمّل المواد مباشرة

3. تظهر مواد القرآن: الحفظ، التلاوة، التجويد
```

### السيناريو 3: طالب يريد تعلم مهارات عامة

```
1. يختار المجال: "المهارات العامة" (domainId: 4)
   ↓ النظام يتحقق: hasCurriculum = false
   ↓ المواد على مستوى المجال (بدون مراحل)

2. تظهر المواد مباشرة:
   - مهارات التواصل
   - مهارات التفكير النقدي
   - مهارات القيادة
```

### السيناريو 4: مدير يريد إضافة منهج جديد

```
1. يدخل صفحة إدارة المناهج
2. يضغط "إضافة منهج جديد"
3. يملأ البيانات:
   - الاسم بالعربية: "المنهج الأردني"
   - الاسم بالإنجليزية: "Jordanian Curriculum"
   - الدولة: "JO"
4. يضغط حفظ
5. يتم إنشاء المنهج
6. يمكنه الآن إضافة فصول دراسية لهذا المنهج
```

---

## منطق واجهة المستخدم

### تسلسل القوائم المنسدلة

```js
// عند تغيير المجال
async function onDomainChange(domainId) {
    // 1. مسح جميع الاختيارات التابعة
    clearCurriculum();
    clearLevel();
    clearGrade();
    clearTerm();
    clearSubjects();
    
    // 2. جلب بيانات المجال
    const domain = await api.get(`/Education/Domains/${domainId}`);
    
    // 3. التحقق من وجود منهج
    if (domain.data.hasCurriculum) {
        // إظهار قوائم المنهج والفصول
        showCurriculumDropdown();
        showTermDropdown();
        
        // تحميل المناهج
        const curriculums = await api.get('/Curriculum?isActive=true');
        populateCurriculumDropdown(curriculums.data.items);
    } else {
        // إخفاء قوائم المنهج والفصول
        hideCurriculumDropdown();
        hideTermDropdown();
        
        // تحميل المراحل مباشرة (بدون منهج)
        const levels = await api.get(`/Education/Levels?domainId=${domainId}`);
        populateLevelDropdown(levels.data.items);
    }
}

// عند تغيير المنهج
async function onCurriculumChange(curriculumId) {
    // مسح الاختيارات التابعة
    clearLevel();
    clearGrade();
    clearTerm();
    clearSubjects();
    
    // تحميل المراحل حسب المنهج
    const levels = await api.get(`/Education/Levels?curriculumId=${curriculumId}`);
    populateLevelDropdown(levels.data.items);
    
    // تحميل الفصول
    const terms = await api.get(`/Education/Terms?curriculumId=${curriculumId}`);
    populateTermDropdown(terms.data.items);
}

// عند تغيير المرحلة
async function onLevelChange(levelId) {
    // مسح الاختيارات التابعة
    clearGrade();
    clearSubjects();
    
    // تحميل الصفوف
    const grades = await api.get(`/Education/Grades?levelId=${levelId}`);
    populateGradeDropdown(grades.data.items);
}

// عند تغيير الصف أو الفصل
async function loadSubjects() {
    const params = {
        domainId: selectedDomainId,
        curriculumId: selectedCurriculumId || undefined,
        levelId: selectedLevelId || undefined,
        gradeId: selectedGradeId || undefined,
        termId: selectedTermId || undefined
    };
    
    const subjects = await api.get('/Subjects', { params });
    displaySubjects(subjects.data.items);
}
```

### قواعد الإظهار والإخفاء

| العنصر | متى يظهر؟ |
|--------|----------|
| قائمة المناهج | فقط إذا `domain.hasCurriculum = true` |
| قائمة الفصول | فقط إذا `domain.hasCurriculum = true` |
| قائمة المراحل | دائماً (بعد اختيار المجال) |
| قائمة الصفوف | بعد اختيار المرحلة |
| قائمة المواد | بعد اختيار أي مستوى |

---

## قواعد التحقق

### تحقق المجال (Domain)
- ✅ `nameAr` و `nameEn` مطلوبان
- ✅ `code` فريد ولا يمكن تكراره
- ✅ `code` يحتوي فقط على أحرف إنجليزية كبيرة وأرقام وشرطات سفلية

### تحقق المنهج (Curriculum)
- ✅ `nameAr` و `nameEn` مطلوبان
- ✅ الاسم فريد

### تحقق المرحلة (Level)
- ✅ `domainId` مطلوب دائماً
- ✅ إذا كان المجال `hasCurriculum = true`، يجب تحديد `curriculumId`
- ✅ `nameAr` و `nameEn` مطلوبان

### تحقق الصف (Grade)
- ✅ `levelId` مطلوب
- ✅ المرحلة يجب أن تكون مفعّلة
- ✅ `nameAr` و `nameEn` مطلوبان

### تحقق الفصل (Term)
- ✅ `curriculumId` مطلوب
- ✅ المنهج يجب أن يكون مفعّلاً

### تحقق المادة (Subject)
- ✅ `domainId` مطلوب دائماً
- ✅ إذا تم تحديد `curriculumId`:
  - المجال يجب أن يكون `hasCurriculum = true`
- ✅ إذا تم تحديد `termId`:
  - يجب تحديد `curriculumId` أيضاً
  - الفصل يجب أن ينتمي للمنهج المختار
- ✅ إذا تم تحديد `gradeId`:
  - يجب تحديد `levelId` أيضاً
  - الصف يجب أن ينتمي للمرحلة المختارة
- ✅ إذا تم تحديد `levelId`:
  - المرحلة يجب أن تنتمي للمجال المختار

---

## الأخطاء الشائعة وحلولها

### 1. خطأ: "Domain code already exists"

**السبب:** محاولة إنشاء مجال بكود موجود مسبقاً

**الحل:** استخدم كود فريد مختلف

```json
{
    "statusCode": 400,
    "succeeded": false,
    "message": "Domain code already exists",
    "data": null
}
```

### 2. خطأ: "Cannot delete curriculum with existing education levels"

**السبب:** محاولة حذف منهج مرتبط بمراحل تعليمية

**الحل:** احذف المراحل المرتبطة أولاً، أو استخدم toggle-status لإلغاء التفعيل

```json
{
    "statusCode": 400,
    "succeeded": false,
    "message": "Cannot delete curriculum with existing education levels",
    "data": null
}
```

### 3. خطأ: "Cannot delete level with existing grades"

**السبب:** محاولة حذف مرحلة مرتبطة بصفوف

**الحل:** احذف الصفوف المرتبطة أولاً

### 4. خطأ: "Curriculum not found"

**السبب:** المنهج المطلوب غير موجود أو محذوف

**الحل:** تأكد من صحة curriculumId

```json
{
    "statusCode": 404,
    "succeeded": false,
    "message": "Curriculum not found",
    "data": null
}
```

### 5. خطأ: "Validation errors"

**السبب:** البيانات المرسلة لا تطابق قواعد التحقق

**الحل:** راجع الحقول المطلوبة

```json
{
    "statusCode": 400,
    "succeeded": false,
    "message": "Validation failed",
    "data": null,
    "errors": [
        "Arabic name is required",
        "English name is required"
    ]
}
```

### 6. خطأ: "Unauthorized"

**السبب:** عدم وجود توكن صالح أو انتهاء صلاحيته

**الحل:** أعد تسجيل الدخول للحصول على توكن جديد

```json
{
    "statusCode": 401,
    "succeeded": false,
    "message": "Unauthorized",
    "data": null
}
```

### 7. خطأ: "Forbidden"

**السبب:** المستخدم ليس لديه صلاحيات Admin

**الحل:** تأكد من أن المستخدم لديه دور Admin أو SuperAdmin

```json
{
    "statusCode": 403,
    "succeeded": false,
    "message": "You don't have permission to perform this action",
    "data": null
}
```

---

## ملاحظات مهمة للفريق الأمامي

1. **تحقق دائماً من `hasCurriculum`** قبل إظهار قوائم المنهج والفصول

2. **استخدم `orderIndex`** لترتيب العناصر في القوائم

3. **فلتر بـ `isActive = true`** للقوائم التي تظهر للمستخدمين

4. **خزّن بيانات المجالات** (Cache) لأنها لا تتغير كثيراً

5. **أعد تعيين القوائم التابعة** عند تغيير القائمة الأب

6. **تعامل مع RTL**: 
   - استخدم `nameAr` للواجهة العربية
   - استخدم `nameEn` للواجهة الإنجليزية

7. **أظهر رسائل الخطأ** من `errors[]` أو `message` للمستخدم

8. **تعامل مع الـ Pagination**:
   - استخدم `totalCount` لعرض العدد الإجمالي
   - استخدم `totalPages` للتنقل بين الصفحات

---

## للتواصل

للاستفسارات أو الملاحظات، تواصل مع فريق Backend.
