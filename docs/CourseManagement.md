# نظام إدارة الدورات التعليمية - دليل المعلمين

## نظرة عامة

يتيح نظام إدارة الدورات للمعلمين إنشاء وإدارة دوراتهم التعليمية بشكل كامل. كل دورة تحتوي على معلومات تفصيلية عن المحتوى التعليمي، طريقة التدريس، الجدول الزمني، والسعر.

## هيكل الدورة التعليمية

### المكونات الأساسية

```
دورة تعليمية (Course)
├── معلومات أساسية
│   ├── العنوان (Title)
│   ├── الوصف (Description)
│   ├── تاريخ البداية والنهاية
│   └── الحالة (Draft/Published/Paused)
│
├── المحتوى التعليمي
│   ├── المجال (Domain)
│   ├── المادة (Subject)
│   ├── المنهج (Curriculum) - اختياري
│   ├── المرحلة (Level) - اختياري
│   └── الصف (Grade) - اختياري
│
├── خصائص الجلسات
│   ├── طريقة التدريس (Online/InPerson)
│   ├── نوع الجلسة (Individual/Group)
│   ├── عدد الجلسات (أو مرنة)
│   ├── مدة الجلسة (بالدقائق)
│   └── الحد الأقصى للطلاب (للجماعية)
│
└── المعلومات المالية
    ├── السعر
    └── إمكانية التضمين في الباقات
```

---

## العمليات المتاحة (APIs)

### 1. إنشاء دورة جديدة (Create Course)

**Endpoint**: `POST /Api/V1/Courses`  
**Authorization**: Teacher role  
**Initial Status**: Draft (مسودة)

#### Request Body

```json
{
  "title": "دورة تعليم النحو للمبتدئين",
  "description": "دورة شاملة لتعليم أساسيات النحو العربي من الصفر",
  "domainId": 1,
  "subjectId": 5,
  "curriculumId": 2,
  "levelId": 3,
  "gradeId": 7,
  "teachingModeId": 1,
  "sessionTypeId": 1,
  "isFlexible": false,
  "sessionsCount": 12,
  "sessionDurationMinutes": 60,
  "price": 500.00,
  "maxStudents": null,
  "canIncludeInPackages": true,
  "startDate": "2026-03-01",
  "endDate": "2026-05-31"
}
```

#### Response

```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Success",
  "data": {
    "id": 15,
    "title": "دورة تعليم النحو للمبتدئين",
    "description": "دورة شاملة لتعليم أساسيات النحو العربي من الصفر",
    "isActive": true,
    "startDate": "2026-03-01",
    "endDate": "2026-05-31",
    "teacherId": 3,
    "teacherDisplayName": "أحمد محمد",
    "domainId": 1,
    "domainNameEn": "School Education",
    "subjectId": 5,
    "subjectNameEn": "Arabic Grammar",
    "curriculumId": 2,
    "curriculumNameEn": "Saudi Curriculum",
    "levelId": 3,
    "levelNameEn": "Middle School",
    "gradeId": 7,
    "gradeNameEn": "Grade 7",
    "teachingModeId": 1,
    "teachingModeNameEn": "Online",
    "sessionTypeId": 1,
    "sessionTypeNameEn": "Individual",
    "isFlexible": false,
    "sessionsCount": 12,
    "sessionDurationMinutes": 60,
    "price": 500.00,
    "maxStudents": null,
    "canIncludeInPackages": true,
    "status": 1
  }
}
```

#### منطق العمل

1. **التحقق من المعلم**: يجب أن يكون المستخدم معلماً نشطاً
2. **التحقق من الدورة المرنة**:
   - إذا `isFlexible = false` → يجب توفير `sessionsCount` و `sessionDurationMinutes`
   - إذا `isFlexible = true` → الحقلان اختياريان
3. **التحقق من التواريخ**:
   - إذا تم توفير تاريخ البداية والنهاية → يجب أن يكون تاريخ النهاية >= تاريخ البداية
4. **التحقق من الكيانات المرتبطة**:
   - Domain يجب أن يكون موجوداً
   - Subject يجب أن يكون موجوداً
   - TeachingMode يجب أن يكون موجوداً
   - SessionType يجب أن يكون موجوداً
5. **إنشاء الدورة**: حالة افتراضية = Draft
6. **إرجاع التفاصيل الكاملة**: مع جميع الأسماء المترجمة

---

### 2. تحديث دورة (Update Course)

**Endpoint**: `PUT /Api/V1/Courses/{id}`  
**Authorization**: Teacher role (owner only)

#### Request Body

نفس بنية CreateCourse، لكن يتم تحديث دورة موجودة.

```json
{
  "title": "دورة تعليم النحو للمبتدئين - محدّثة",
  "description": "دورة محسّنة مع أمثلة إضافية",
  "domainId": 1,
  "subjectId": 5,
  "curriculumId": 2,
  "levelId": 3,
  "gradeId": 7,
  "teachingModeId": 2,
  "sessionTypeId": 2,
  "isFlexible": false,
  "sessionsCount": 15,
  "sessionDurationMinutes": 90,
  "price": 600.00,
  "maxStudents": 5,
  "canIncludeInPackages": true,
  "startDate": "2026-03-15",
  "endDate": "2026-06-15"
}
```

#### منطق العمل

1. **التحقق من الملكية**: المعلم يمكنه تحديث دوراته فقط
2. **نفس التحققات**: كما في Create
3. **تحديث جميع الحقول**: يتم استبدال جميع القيم
4. **الحفاظ على الـ Status**: لا يتم تغيير حالة الدورة في Update

**ملاحظة مهمة**: إذا كانت الدورة منشورة (Published) ولديها تسجيلات، يُنصح بعدم تغيير المعلومات الأساسية.

---

### 3. حذف دورة (Delete Course)

**Endpoint**: `DELETE /Api/V1/Courses/{id}`  
**Authorization**: Teacher role (owner only)

#### منطق الحذف الذكي

النظام يستخدم **حذف هجين** (Hybrid Delete):

##### الحالة 1: الدورة لديها تسجيلات
```
إذا: الدورة لديها طلاب مسجلين
النتيجة: حذف ناعم (Soft Delete)
  ├── IsActive = false
  ├── Status = Paused
  └── الدورة تبقى في قاعدة البيانات
```

**Response**:
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Course deactivated (has enrollments)."
}
```

##### الحالة 2: الدورة بدون تسجيلات
```
إذا: الدورة ليس لديها تسجيلات
النتيجة: حذف كامل (Hard Delete)
  └── حذف السجل من قاعدة البيانات نهائياً
```

**Response**:
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Course deleted."
}
```

#### لماذا الحذف الهجين؟

| السيناريو | نوع الحذف | السبب |
|-----------|-----------|-------|
| دورة جديدة بدون طلاب | Hard Delete | لا يوجد سجل تاريخي مهم |
| دورة لديها طلاب | Soft Delete | الحفاظ على سجل الطلاب والمدفوعات |

---

### 4. عرض تفاصيل دورة (Get Course By ID)

**Endpoint**: `GET /Api/V1/Courses/{id}`  
**Authorization**: Teacher role (owner only)

#### Response

```json
{
  "statusCode": 200,
  "succeeded": true,
  "data": {
    "id": 15,
    "title": "دورة تعليم النحو للمبتدئين",
    "description": "دورة شاملة...",
    "isActive": true,
    "startDate": "2026-03-01",
    "endDate": "2026-05-31",
    "teacherId": 3,
    "teacherDisplayName": "أحمد محمد",
    "domainId": 1,
    "domainNameEn": "School Education",
    "subjectId": 5,
    "subjectNameEn": "Arabic Grammar",
    "curriculumId": 2,
    "curriculumNameEn": "Saudi Curriculum",
    "levelId": 3,
    "levelNameEn": "Middle School",
    "gradeId": 7,
    "gradeNameEn": "Grade 7",
    "teachingModeId": 1,
    "teachingModeNameEn": "Online",
    "sessionTypeId": 1,
    "sessionTypeNameEn": "Individual",
    "isFlexible": false,
    "sessionsCount": 12,
    "sessionDurationMinutes": 60,
    "price": 500.00,
    "maxStudents": null,
    "canIncludeInPackages": true,
    "status": 1
  }
}
```

---

### 5. عرض قائمة الدورات (Get Courses List)

**Endpoint**: `GET /Api/V1/Courses`  
**Authorization**: Teacher role  
**Pagination**: Supported

#### Query Parameters

```
- pageNumber (int, default: 1)
- pageSize (int, default: 10)
- domainId (int?, optional) - تصفية حسب المجال
- status (CourseStatus?, optional) - تصفية حسب الحالة
- subjectId (int?, optional) - تصفية حسب المادة
```

#### مثال Request

```
GET /Api/V1/Courses?pageNumber=1&pageSize=10&domainId=1&status=2
```

#### Response

```json
{
  "statusCode": 200,
  "succeeded": true,
  "data": [
    {
      "id": 15,
      "title": "دورة تعليم النحو للمبتدئين",
      "descriptionShort": "دورة شاملة لتعليم أساسيات النحو...",
      "teacherId": 3,
      "domainId": 1,
      "domainNameEn": "School Education",
      "subjectId": 5,
      "subjectNameEn": "Arabic Grammar",
      "teachingModeId": 1,
      "teachingModeNameEn": "Online",
      "sessionTypeId": 1,
      "sessionTypeNameEn": "Individual",
      "status": 2,
      "isActive": true,
      "price": 500.00,
      "startDate": "2026-03-01",
      "endDate": "2026-05-31"
    }
  ],
  "meta": {
    "totalCount": 25,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

**ملاحظة**: القائمة تعرض دورات المعلم الحالي فقط (المصادق عليه).

---

## حالات الدورة (Course Status)

| الحالة | القيمة | الوصف | العمليات المسموحة |
|--------|--------|-------|-------------------|
| **Draft** | 1 | مسودة | جميع العمليات (Update, Delete, Publish) |
| **Published** | 2 | منشورة | Update محدود، Enroll، Soft Delete فقط |
| **Paused** | 3 | متوقفة | Re-activate, View فقط |

### دورة حياة الدورة

```
Draft (مسودة)
  ↓
  [Teacher publishes] → Published (منشورة)
  ↓
  [Students enroll] → Published (مع تسجيلات)
  ↓
  [Teacher pauses] → Paused (متوقفة)
  ↓
  [Teacher deletes] → Paused + IsActive=false (محذوفة ناعم)
```

---

## أنواع الدورات

### 1. دورة محددة (Fixed Duration Course)

**الخصائص**:
- `isFlexible = false`
- `sessionsCount` محدد (مثلاً: 12 جلسة)
- `sessionDurationMinutes` محدد (مثلاً: 60 دقيقة)

**مثال**:
```json
{
  "title": "دورة القرآن الكريم - 3 أشهر",
  "isFlexible": false,
  "sessionsCount": 24,
  "sessionDurationMinutes": 45,
  "startDate": "2026-03-01",
  "endDate": "2026-05-31"
}
```

**الاستخدام**: دورات منظمة بجدول زمني ثابت.

---

### 2. دورة مرنة (Flexible Course)

**الخصائص**:
- `isFlexible = true`
- `sessionsCount = null`
- `sessionDurationMinutes = null`

**مثال**:
```json
{
  "title": "استشارات لغوية مفتوحة",
  "isFlexible": true,
  "sessionsCount": null,
  "sessionDurationMinutes": null,
  "startDate": null,
  "endDate": null
}
```

**الاستخدام**: دورات بدون التزام بعدد جلسات محدد، الطالب يحجز حسب الحاجة.

---

### 3. جلسات فردية (Individual Sessions)

**الخصائص**:
- `sessionTypeId` = Individual
- `maxStudents = null`

**مثال**:
```json
{
  "title": "جلسات خاصة لتحفيظ القرآن",
  "sessionTypeId": 1,
  "maxStudents": null
}
```

**AvailableSeats**: `int.MaxValue` (لا يوجد حد)

---

### 4. جلسات جماعية (Group Sessions)

**الخصائص**:
- `sessionTypeId` = Group
- `maxStudents` محدد (مثلاً: 10 طلاب)

**مثال**:
```json
{
  "title": "ورشة عمل جماعية - التجويد",
  "sessionTypeId": 2,
  "maxStudents": 10
}
```

**AvailableSeats**: يتم حسابه تلقائياً = `MaxStudents - Active Enrollments`

---

## قواعد التحقق (Validation Rules)

### عند الإنشاء والتحديث

#### 1. الحقول الإلزامية

| الحقل | إلزامي؟ | الشرط |
|-------|---------|-------|
| Title | نعم | دائماً |
| DomainId | نعم | دائماً |
| SubjectId | نعم | دائماً |
| TeachingModeId | نعم | دائماً |
| SessionTypeId | نعم | دائماً |
| Price | نعم | >= 0 |
| SessionsCount | نعم | عندما isFlexible = false |
| SessionDurationMinutes | نعم | عندما isFlexible = false |

#### 2. قواعد المنطق

```
قاعدة 1: الدورة غير المرنة
  IF isFlexible = false
  THEN sessionsCount > 0 AND sessionDurationMinutes > 0
  
قاعدة 2: التواريخ
  IF startDate AND endDate exists
  THEN endDate >= startDate
  
قاعدة 3: الملكية
  Teacher can only manage their OWN courses
```

#### 3. الحدود القصوى

```
Title: max 200 characters
Description: max 2000 characters
Price: decimal(18,2)
SessionsCount: > 0 (when required)
SessionDurationMinutes: > 0 (when required)
```

---

## الأمان والصلاحيات

### التحقق من الملكية

```
كل عملية تتحقق من:
1. المستخدم لديه Teacher role
2. المعلم يملك الدورة (course.TeacherId = teacher.Id)
3. إذا لم يكن المالك → 404 Not Found (وليس 403 Forbidden)
```

**لماذا 404 بدلاً من 403؟**
- أمان أفضل: لا يكشف عن وجود الدورة
- UX أفضل: رسالة واحدة للدورة غير موجودة أو غير مملوكة

---

## سيناريوهات الاستخدام

### السيناريو 1: معلم ينشئ دورة قرآن فردية مرنة

**الهدف**: جلسات تحفيظ قرآن بدون التزام بعدد جلسات محدد.

**Request**:
```json
{
  "title": "تحفيظ القرآن الكريم - جلسات فردية",
  "description": "جلسات خاصة لتحفيظ القرآن الكريم حسب سرعة الطالب",
  "domainId": 2,
  "subjectId": 12,
  "teachingModeId": 1,
  "sessionTypeId": 1,
  "isFlexible": true,
  "sessionsCount": null,
  "sessionDurationMinutes": null,
  "price": 50.00,
  "maxStudents": null,
  "canIncludeInPackages": false
}
```

**النتيجة**:
- ✅ دورة مرنة بدون حد للجلسات
- ✅ فردية (طالب واحد لكل جلسة)
- ✅ السعر لكل جلسة

---

### السيناريو 2: معلم ينشئ دورة رياضيات جماعية محددة

**الهدف**: دورة منظمة لطلاب الصف السادس، 20 جلسة، حد أقصى 8 طلاب.

**Request**:
```json
{
  "title": "الرياضيات المتقدمة - الصف السادس",
  "description": "تحضير شامل لاختبارات نهاية العام",
  "domainId": 1,
  "subjectId": 3,
  "curriculumId": 2,
  "levelId": 3,
  "gradeId": 6,
  "teachingModeId": 2,
  "sessionTypeId": 2,
  "isFlexible": false,
  "sessionsCount": 20,
  "sessionDurationMinutes": 90,
  "price": 800.00,
  "maxStudents": 8,
  "canIncludeInPackages": true,
  "startDate": "2026-04-01",
  "endDate": "2026-06-30"
}
```

**النتيجة**:
- ✅ 20 جلسة × 90 دقيقة
- ✅ حد أقصى 8 طلاب
- ✅ السعر للدورة كاملة (800 ريال)
- ✅ تواريخ محددة
- ✅ يمكن تضمينها في باقات

---

### السيناريو 3: معلم يحذف دورة

#### الحالة أ: دورة جديدة بدون طلاب

**Request**: `DELETE /Api/V1/Courses/15`

**ما يحدث**:
1. التحقق: الدورة ليس لديها تسجيلات
2. حذف كامل من قاعدة البيانات
3. **Response**: "Course deleted."

#### الحالة ب: دورة لديها 5 طلاب مسجلين

**Request**: `DELETE /Api/V1/Courses/20`

**ما يحدث**:
1. التحقق: الدورة لديها 5 تسجيلات
2. **حذف ناعم**:
   - `IsActive = false`
   - `Status = Paused`
3. **Response**: "Course deactivated (has enrollments)."
4. السجل يبقى للأغراض التاريخية والمالية

---

## أمثلة برمجية للتكامل

### مثال 1: إنشاء دورة (React/TypeScript)

```typescript
interface CreateCourseRequest {
  title: string;
  description?: string;
  domainId: number;
  subjectId: number;
  curriculumId?: number;
  levelId?: number;
  gradeId?: number;
  teachingModeId: number;
  sessionTypeId: number;
  isFlexible: boolean;
  sessionsCount?: number;
  sessionDurationMinutes?: number;
  price: number;
  maxStudents?: number;
  canIncludeInPackages: boolean;
  startDate?: string;
  endDate?: string;
}

async function createCourse(data: CreateCourseRequest, token: string) {
  const response = await fetch('/Api/V1/Courses', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ data })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return await response.json();
}

// استخدام
try {
  const result = await createCourse({
    title: "دورة النحو المكثفة",
    domainId: 1,
    subjectId: 5,
    teachingModeId: 1,
    sessionTypeId: 1,
    isFlexible: false,
    sessionsCount: 12,
    sessionDurationMinutes: 60,
    price: 500.00,
    canIncludeInPackages: true
  }, userToken);
  
  console.log('Course created:', result.data.id);
} catch (error) {
  console.error('Failed to create course:', error);
}
```

---

### مثال 2: التحقق قبل الحذف (Flutter/Dart)

```dart
Future<void> deleteCourse(int courseId, String token) async {
  // عرض تأكيد للمستخدم
  final confirmed = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('حذف الدورة'),
      content: Text('هل أنت متأكد من حذف هذه الدورة؟\n\nملاحظة: إذا كانت الدورة لديها طلاب مسجلين، سيتم إلغاء تفعيلها بدلاً من حذفها نهائياً.'),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context, false),
          child: Text('إلغاء'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, true),
          child: Text('حذف'),
        ),
      ],
    ),
  );
  
  if (confirmed != true) return;
  
  final response = await http.delete(
    Uri.parse('$baseUrl/Api/V1/Courses/$courseId'),
    headers: {
      'Authorization': 'Bearer $token',
    },
  );
  
  if (response.statusCode == 200) {
    final result = jsonDecode(response.body);
    
    // التحقق من نوع الحذف
    if (result['message'].contains('deactivated')) {
      showSnackBar('تم إلغاء تفعيل الدورة (لديها طلاب مسجلين)');
    } else {
      showSnackBar('تم حذف الدورة نهائياً');
    }
  }
}
```

---

## معالجة الأخطاء

### أخطاء شائعة وحلولها

#### 1. دورة غير مرنة بدون عدد جلسات

**Request**:
```json
{
  "isFlexible": false,
  "sessionsCount": null
}
```

**Response**:
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "SessionsCount is required when course is not flexible."
}
```

**الحل**: أضف `sessionsCount` و `sessionDurationMinutes`.

---

#### 2. تاريخ نهاية قبل تاريخ البداية

**Request**:
```json
{
  "startDate": "2026-05-01",
  "endDate": "2026-04-01"
}
```

**Response**:
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "EndDate must be on or after StartDate."
}
```

**الحل**: تأكد من أن `endDate >= startDate`.

---

#### 3. محاولة تعديل دورة معلم آخر

**Request**: `PUT /Api/V1/Courses/999` (دورة لمعلم آخر)

**Response**:
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Course not found."
}
```

**التفسير**: النظام يُخفي وجود الدورة لأسباب أمنية.

---

#### 4. مجال أو مادة غير موجودة

**Request**:
```json
{
  "domainId": 999,
  "subjectId": 888
}
```

**Response**:
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "Invalid DomainId."
}
```

**الحل**: تأكد من أن الـ IDs صحيحة وموجودة.

---

## أفضل الممارسات

### 1. إنشاء دورة متكاملة

```json
{
  "title": "عنوان واضح ومختصر",
  "description": "وصف تفصيلي يشرح محتوى الدورة والأهداف",
  "domainId": 1,
  "subjectId": 5,
  "curriculumId": 2,
  "levelId": 3,
  "gradeId": 7,
  "teachingModeId": 1,
  "sessionTypeId": 1,
  "isFlexible": false,
  "sessionsCount": 12,
  "sessionDurationMinutes": 60,
  "price": 500.00,
  "startDate": "2026-03-01",
  "endDate": "2026-05-31"
}
```

**لماذا؟**
- ✅ العنوان يوضح المحتوى
- ✅ الوصف يشرح بالتفصيل
- ✅ المعلومات التعليمية كاملة
- ✅ التواريخ محددة للتنظيم
- ✅ السعر واضح

---

### 2. تسعير الدورات

#### دورات فردية
```
السعر = لكل جلسة
مثال: 50 ريال/جلسة × عدد الجلسات المحجوزة
```

#### دورات جماعية محددة
```
السعر = للدورة كاملة
مثال: 800 ريال للدورة (20 جلسة)
```

#### دورات مرنة
```
السعر = لكل جلسة أو حزمة ساعات
مثال: 60 ريال/ساعة
```

---

### 3. إدارة الحالات

#### متى تستخدم Draft؟
- ✅ دورة جديدة قيد الإعداد
- ✅ اختبار الإعدادات
- ✅ قبل النشر للطلاب

#### متى تستخدم Published؟
- ✅ الدورة جاهزة للتسجيل
- ✅ جميع المعلومات صحيحة
- ✅ الجدول الزمني مؤكد

#### متى تستخدم Paused؟
- ✅ إيقاف مؤقت للتسجيلات
- ✅ الدورة قيد المراجعة
- ✅ ظروف طارئة

---

## التكامل مع أنظمة أخرى

### 1. الربط مع أوقات التفرغ (Availability)

```
قبل نشر الدورة، تأكد من:
1. المعلم أضاف أوقات التفرغ
2. أوقات التفرغ تغطي الجدول الزمني للدورة
3. لا يوجد تعارض مع دورات أخرى
```

---

### 2. الربط مع المواد والوحدات (Subjects & Units)

```
التحقق من:
1. المعلم مسجل لتدريس هذه المادة
2. المعلم يستطيع تدريس الوحدات المطلوبة
3. Domain → Subject متوافق
```

**مثال تحقق**:
```csharp
var teacherSubjects = await _teacherSubjectRepository
    .GetTeacherSubjectsWithUnitsAsync(teacherId);
    
var canTeachSubject = teacherSubjects
    .Any(ts => ts.SubjectId == course.SubjectId);
    
if (!canTeachSubject)
    return BadRequest("You are not registered to teach this subject.");
```

---

### 3. الربط مع التسجيلات (Enrollments)

```
عند التسجيل في دورة:
1. التحقق من المقاعد المتاحة (AvailableSeats > 0)
2. التحقق من حالة الدورة (Published + IsActive)
3. التحقق من التواريخ (لم تبدأ بعد أو قيد التنفيذ)
4. التحقق من عدم تسجيل الطالب مسبقاً
```

---

## حساب التكاليف

### صيغة السعر النهائي

```
إذا كانت الدورة محددة (isFlexible = false):
  السعر النهائي = Price (ثابت)
  
إذا كانت الدورة مرنة (isFlexible = true):
  السعر النهائي = Price × عدد الجلسات المحجوزة
  
للجلسات الجماعية:
  السعر لكل طالب = Course.Price
  الإيرادات المتوقعة = Price × عدد الطلاب المسجلين
```

---

## الجدول الزمني والتنسيق

### تواريخ الدورة

```
StartDate: تاريخ بداية الدورة (اختياري)
EndDate: تاريخ نهاية الدورة (اختياري)

الحالات:
1. StartDate & EndDate محددان → دورة محددة زمنياً
2. StartDate فقط → دورة مفتوحة النهاية
3. كلاهما null → دورة مرنة بدون التزام زمني
```

---

## قاعدة البيانات

### الأعمدة الرئيسية

| العمود | النوع | إلزامي | الوصف |
|--------|------|--------|-------|
| Id | int | نعم | المعرف الفريد |
| Title | nvarchar(200) | نعم | عنوان الدورة |
| Description | nvarchar(2000) | لا | وصف تفصيلي |
| IsActive | bit | نعم | حالة التفعيل (default: true) |
| StartDate | date | لا | تاريخ البداية |
| EndDate | date | لا | تاريخ النهاية |
| DomainId | int | نعم | المجال التعليمي |
| SubjectId | int | نعم | المادة |
| TeacherId | int | نعم | المعلم المالك |
| TeachingModeId | int | نعم | طريقة التدريس |
| SessionTypeId | int | نعم | نوع الجلسة |
| IsFlexible | bit | نعم | مرنة أم محددة |
| SessionsCount | int | شرطي | عدد الجلسات (إذا غير مرنة) |
| SessionDurationMinutes | int | شرطي | مدة الجلسة (إذا غير مرنة) |
| Price | decimal(18,2) | نعم | السعر |
| MaxStudents | int | لا | الحد الأقصى للطلاب |
| Status | int | نعم | Draft/Published/Paused |

### القيود (Constraints)

```sql
-- القيد 1: عدد الجلسات
CK_Course_SessionsCount:
  (IsFlexible = 1) OR (SessionsCount IS NOT NULL AND SessionsCount > 0)

-- القيد 2: مدة الجلسة
CK_Course_SessionDuration:
  (IsFlexible = 1) OR (SessionDurationMinutes IS NOT NULL AND SessionDurationMinutes > 0)
```

### الفهارس (Indexes)

```
IX_Courses_TeacherId
IX_Courses_DomainId
IX_Courses_SubjectId
IX_Courses_Status
IX_Courses_Status_IsActive (composite)
IX_Courses_TeachingModeId_SessionTypeId (composite)
IX_Courses_StartDate_EndDate (composite)
```

---

## ملاحظات تقنية

### 1. المقاعد المتاحة (AvailableSeats)

**خاصية محسوبة** - لا تُخزن في قاعدة البيانات:

```csharp
[NotMapped]
public int AvailableSeats => MaxStudents.HasValue 
    ? MaxStudents.Value - CourseEnrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active)
    : int.MaxValue;
```

**ملاحظة للأداء**:
- يتطلب تحميل `CourseEnrollments` مع الدورة
- استخدم `.Include(c => c.CourseEnrollments)` عند الحاجة

---

### 2. التحقق من الملكية

```csharp
// في كل Handler
var teacher = await _teacherRepository.GetByUserIdAsync(request.UserId);
if (teacher == null)
    return NotFound("Teacher not found.");

var course = await _courseRepository.GetByIdAsync(request.Id);
if (course == null || course.TeacherId != teacher.Id)
    return NotFound("Course not found.");  // لا تكشف عن السبب الحقيقي
```

---

### 3. الحذف الآمن

```csharp
var hasEnrollments = await _courseRepository.HasEnrollmentsAsync(course.Id);

if (hasEnrollments)
{
    // حذف ناعم: إبقاء السجل التاريخي
    course.IsActive = false;
    course.Status = CourseStatus.Paused;
}
else
{
    // حذف كامل: لا يوجد بيانات مرتبطة
    await _courseRepository.DeleteAsync(course);
}
```

---

## الخلاصة

### الميزات الرئيسية

1. ✅ **مرونة كاملة**: دورات محددة أو مرنة
2. ✅ **أنواع متعددة**: فردية أو جماعية
3. ✅ **أمان محكم**: المعلم يدير دوراته فقط
4. ✅ **حذف ذكي**: ناعم أو كامل حسب الحالة
5. ✅ **تكامل كامل**: مع المحتوى التعليمي وأوقات التفرغ
6. ✅ **تصفية متقدمة**: حسب المجال، المادة، الحالة
7. ✅ **حساب تلقائي**: للمقاعد المتاحة

### العمليات المدعومة

| العملية | Method | Endpoint | الوصف |
|---------|--------|----------|-------|
| إنشاء | POST | `/Api/V1/Courses` | إنشاء دورة جديدة (Draft) |
| تحديث | PUT | `/Api/V1/Courses/{id}` | تحديث دورة موجودة |
| حذف | DELETE | `/Api/V1/Courses/{id}` | حذف أو إلغاء تفعيل |
| عرض تفاصيل | GET | `/Api/V1/Courses/{id}` | عرض دورة واحدة |
| قائمة | GET | `/Api/V1/Courses` | قائمة مع فلترة وpagination |

---

**تاريخ التحديث**: 2026-02-09  
**الإصدار**: 2.0  
**الحالة**: محدّث بالحقول الجديدة (Title, Description, IsActive, DomainId, Dates)
