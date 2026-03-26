# Usage Guide - دليل الاستخدام 📖

## نظرة عامة

هذا الدليل يوضح كيفية استخدام **TeachingMode** و **SessionType** في منصة Qalam، مع أمثلة عملية شاملة لجميع السيناريوهات المتاحة.

---

## 📋 الفرق الأساسي

### 1. **TeachingMode** (المكان: أين؟) 📍

يحدد **المكان** الذي ستتم فيه الجلسة:

| ID | Code | الاسم | الوصف |
|----|------|-------|-------|
| 1 | `in_person` | حضوري | الجلسة في موقع فعلي (مركز، منزل، مدرسة، مسجد) |
| 2 | `online` | أونلاين | الجلسة عبر الإنترنت (Zoom, Teams, Meet) |

**ملاحظة:** ❌ لا يوجد خيار "هجين"

---

### 2. **SessionType** (الحجم: كم؟) 👥

يحدد **عدد الطلاب** في الجلسة:

| ID | Code | الاسم | الوصف |
|----|------|-------|-------|
| 1 | `individual` | فردي | معلم + طالب واحد |
| 2 | `group` | جماعي | معلم + مجموعة طلاب |

---

## 🎯 السيناريوهات الأربعة (2 × 2)

| # | TeachingMode | SessionType | الوصف | مثال عملي |
|---|--------------|-------------|-------|-----------|
| 1️⃣ | حضوري | فردي | درس خصوصي في موقع | معلم قرآن يزور منزل الطالب |
| 2️⃣ | حضوري | جماعي | محاضرة في قاعة | دورة برمجة في مركز القلم (25 طالب) |
| 3️⃣ | أونلاين | فردي | جلسة فردية أونلاين | جلسة Zoom للإنجليزية (1:1) |
| 4️⃣ | أونلاين | جماعي | ويبينار أونلاين | محاضرة تاريخ عبر Teams (100 طالب) |

---

## 💻 أمثلة الكود (C#)

### السيناريو 1️⃣: جلسة حضورية فردية

**الوصف:** درس قرآن خصوصي في المسجد

```csharp
using Qalam.Data.Entity.Teaching;

// إنشاء جلسة حضورية فردية
var session = new TeachingSession
{
    // المكان: حضوري
    TeachingModeId = 1,              // in_person
    LocationId = 5,                  // مسجد الحي (مطلوب للحضوري)
    
    // الحجم: فردي
    SessionTypeId = 1,               // individual
    MaxParticipants = null,          // غير مطلوب للفردي
    
    // التفاصيل
    MeetingLink = null,              // لا حاجة له في الحضوري
    TeacherId = 10,
    SubjectId = 50,                  // حفظ جزء عم
    StartTime = new DateTime(2026, 1, 15, 9, 0, 0),
    DurationMinutes = 45,
    Price = 50.00m
};

await _context.TeachingSessions.AddAsync(session);
await _context.SaveChangesAsync();
```

**استعلام:** البحث عن جميع الجلسات الحضورية الفردية

```csharp
var inPersonIndividualSessions = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Include(s => s.Location)
    .Include(s => s.Teacher)
    .Where(s => 
        s.TeachingMode.Code == "in_person" && 
        s.SessionType.Code == "individual")
    .ToListAsync();

// النتيجة:
// - درس قرآن في المسجد
// - درس رياضيات في منزل الطالب
// - درس بيانو في استوديو المعلم
```

---

### السيناريو 2️⃣: جلسة حضورية جماعية

**الوصف:** محاضرة رياضيات في مركز القلم

```csharp
// إنشاء جلسة حضورية جماعية
var session = new TeachingSession
{
    // المكان: حضوري
    TeachingModeId = 1,              // in_person
    LocationId = 12,                 // قاعة المحاضرات - مركز القلم
    
    // الحجم: جماعي
    SessionTypeId = 2,               // group
    MaxParticipants = 30,            // مطلوب للجماعي
    
    // التفاصيل
    MeetingLink = null,
    TeacherId = 15,
    SubjectId = 25,                  // رياضيات - الصف الثالث الثانوي
    StartTime = new DateTime(2026, 1, 15, 16, 0, 0),
    DurationMinutes = 90,
    Price = 20.00m                   // سعر أقل لأنها جماعية
};

await _context.TeachingSessions.AddAsync(session);
await _context.SaveChangesAsync();
```

**استعلام:** البحث عن جلسات جماعية بمقاعد متاحة

```csharp
var availableGroupSessions = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Include(s => s.Location)
    .Where(s => 
        s.SessionType.Code == "group" &&
        s.StartTime > DateTime.Now &&
        s.Enrollments.Count < s.MaxParticipants)  // مقاعد متاحة
    .Select(s => new
    {
        s.Id,
        TeacherName = s.Teacher.FullName,
        SubjectName = s.Subject.NameAr,
        s.StartTime,
        s.Location.NameAr,
        AvailableSeats = s.MaxParticipants - s.Enrollments.Count,
        s.Price
    })
    .ToListAsync();
```

---

### السيناريو 3️⃣: جلسة أونلاين فردية

**الوصف:** درس إنجليزي خصوصي عبر Zoom

```csharp
// إنشاء جلسة أونلاين فردية
var session = new TeachingSession
{
    // المكان: أونلاين
    TeachingModeId = 2,              // online
    MeetingLink = "https://zoom.us/j/123456789?pwd=abc123",  // مطلوب للأونلاين
    
    // الحجم: فردي
    SessionTypeId = 1,               // individual
    MaxParticipants = null,
    
    // التفاصيل
    LocationId = null,               // لا حاجة له في الأونلاين
    TeacherId = 20,
    SubjectId = 80,                  // English Conversation - B1
    StartTime = new DateTime(2026, 1, 15, 18, 0, 0),
    DurationMinutes = 60,
    Price = 80.00m
};

await _context.TeachingSessions.AddAsync(session);
await _context.SaveChangesAsync();
```

**استعلام:** الجلسات الأونلاين الفردية للطالب

```csharp
var studentId = 5;
var myOnlinePrivateSessions = await _context.Enrollments
    .Include(e => e.Session)
        .ThenInclude(s => s.TeachingMode)
    .Include(e => e.Session)
        .ThenInclude(s => s.SessionType)
    .Include(e => e.Session)
        .ThenInclude(s => s.Teacher)
    .Where(e => 
        e.StudentId == studentId &&
        e.Session.TeachingMode.Code == "online" &&
        e.Session.SessionType.Code == "individual" &&
        e.Session.StartTime > DateTime.Now)
    .Select(e => new
    {
        SessionId = e.Session.Id,
        TeacherName = e.Session.Teacher.FullName,
        SubjectName = e.Session.Subject.NameAr,
        e.Session.StartTime,
        e.Session.MeetingLink,
        e.Session.DurationMinutes
    })
    .OrderBy(x => x.StartTime)
    .ToListAsync();
```

---

### السيناريو 4️⃣: جلسة أونلاين جماعية

**الوصف:** ويبينار برمجة بايثون لـ 100 طالب

```csharp
// إنشاء جلسة أونلاين جماعية (ويبينار)
var session = new TeachingSession
{
    // المكان: أونلاين
    TeachingModeId = 2,              // online
    MeetingLink = "https://teams.microsoft.com/l/meetup-join/...",
    
    // الحجم: جماعي
    SessionTypeId = 2,               // group
    MaxParticipants = 100,           // ويبينار كبير
    
    // التفاصيل
    LocationId = null,
    TeacherId = 25,
    SubjectId = 150,                 // Python Programming - Beginner
    StartTime = new DateTime(2026, 1, 20, 19, 0, 0),
    DurationMinutes = 120,
    Price = 15.00m                   // سعر رمزي لويبينار جماعي
};

await _context.TeachingSessions.AddAsync(session);
await _context.SaveChangesAsync();
```

**استعلام:** ويبينارات قادمة (أونلاين + جماعي)

```csharp
var upcomingWebinars = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Include(s => s.Teacher)
    .Include(s => s.Subject)
    .Where(s => 
        s.TeachingMode.Code == "online" &&
        s.SessionType.Code == "group" &&
        s.MaxParticipants >= 50 &&           // ويبينار كبير
        s.StartTime > DateTime.Now)
    .Select(s => new
    {
        s.Id,
        Title = s.Subject.NameAr,
        TeacherName = s.Teacher.FullName,
        s.StartTime,
        s.DurationMinutes,
        TotalSeats = s.MaxParticipants,
        EnrolledCount = s.Enrollments.Count,
        AvailableSeats = s.MaxParticipants - s.Enrollments.Count,
        s.Price,
        s.MeetingLink
    })
    .OrderBy(x => x.StartTime)
    .ToListAsync();
```

---

## 🔍 استعلامات مفيدة

### 1. الحصول على جميع الجلسات الحضورية (فردية أو جماعية)

```csharp
var inPersonSessions = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Include(s => s.Location)
    .Where(s => s.TeachingMode.Code == "in_person")
    .ToListAsync();
```

---

### 2. الحصول على جميع الجلسات الفردية (حضورية أو أونلاين)

```csharp
var individualSessions = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Where(s => s.SessionType.Code == "individual")
    .ToListAsync();
```

---

### 3. إحصائيات الجلسات حسب النوع والمكان

```csharp
var sessionStats = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .GroupBy(s => new 
    { 
        TeachingModeAr = s.TeachingMode.NameAr,
        SessionTypeAr = s.SessionType.NameAr 
    })
    .Select(g => new 
    {
        TeachingMode = g.Key.TeachingModeAr,
        SessionType = g.Key.SessionTypeAr,
        Count = g.Count(),
        TotalRevenue = g.Sum(s => s.Price),
        AveragePrice = g.Average(s => s.Price)
    })
    .ToListAsync();

// النتيجة مثلاً:
// [
//   { TeachingMode: "حضوري", SessionType: "فردي", Count: 45, TotalRevenue: 2250, AvgPrice: 50 },
//   { TeachingMode: "حضوري", SessionType: "جماعي", Count: 12, TotalRevenue: 240, AvgPrice: 20 },
//   { TeachingMode: "أونلاين", SessionType: "فردي", Count: 78, TotalRevenue: 6240, AvgPrice: 80 },
//   { TeachingMode: "أونلاين", SessionType: "جماعي", Count: 25, TotalRevenue: 375, AvgPrice: 15 }
// ]
```

---

### 4. البحث عن جلسات معلم محدد حسب النوع

```csharp
var teacherId = 10;

// جميع الجلسات الخصوصية الأونلاين للمعلم
var teacherOnlinePrivateSessions = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Include(s => s.Subject)
    .Where(s => 
        s.TeacherId == teacherId &&
        s.TeachingMode.Code == "online" &&
        s.SessionType.Code == "individual")
    .OrderByDescending(s => s.StartTime)
    .ToListAsync();
```

---

### 5. البحث المتقدم بفلاتر متعددة

```csharp
public async Task<List<SessionDto>> SearchSessions(
    string? teachingModeCode = null,    // "in_person" or "online"
    string? sessionTypeCode = null,     // "individual" or "group"
    int? subjectId = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    decimal? minPrice = null,
    decimal? maxPrice = null)
{
    var query = _context.TeachingSessions
        .Include(s => s.TeachingMode)
        .Include(s => s.SessionType)
        .Include(s => s.Teacher)
        .Include(s => s.Subject)
        .AsQueryable();

    // فلتر TeachingMode
    if (!string.IsNullOrEmpty(teachingModeCode))
        query = query.Where(s => s.TeachingMode.Code == teachingModeCode);

    // فلتر SessionType
    if (!string.IsNullOrEmpty(sessionTypeCode))
        query = query.Where(s => s.SessionType.Code == sessionTypeCode);

    // فلتر Subject
    if (subjectId.HasValue)
        query = query.Where(s => s.SubjectId == subjectId.Value);

    // فلتر التاريخ
    if (startDate.HasValue)
        query = query.Where(s => s.StartTime >= startDate.Value);

    if (endDate.HasValue)
        query = query.Where(s => s.StartTime <= endDate.Value);

    // فلتر السعر
    if (minPrice.HasValue)
        query = query.Where(s => s.Price >= minPrice.Value);

    if (maxPrice.HasValue)
        query = query.Where(s => s.Price <= maxPrice.Value);

    return await query
        .Select(s => new SessionDto
        {
            Id = s.Id,
            TeacherName = s.Teacher.FullName,
            SubjectName = s.Subject.NameAr,
            TeachingMode = s.TeachingMode.NameAr,
            SessionType = s.SessionType.NameAr,
            Location = s.Location != null ? s.Location.NameAr : null,
            MeetingLink = s.MeetingLink,
            StartTime = s.StartTime,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            MaxParticipants = s.MaxParticipants,
            EnrolledCount = s.Enrollments.Count
        })
        .ToListAsync();
}
```

---

## ✅ قواعد التحقق من الصحة (Validation)

### FluentValidation Example

```csharp
using FluentValidation;

public class CreateTeachingSessionValidator : AbstractValidator<CreateTeachingSessionCommand>
{
    public CreateTeachingSessionValidator()
    {
        // 1. TeachingMode مطلوب
        RuleFor(x => x.TeachingModeId)
            .NotEmpty()
            .WithMessage("Teaching mode is required");

        // 2. SessionType مطلوب
        RuleFor(x => x.SessionTypeId)
            .NotEmpty()
            .WithMessage("Session type is required");

        // 3. إذا كانت حضورية → LocationId مطلوب
        When(x => x.TeachingModeCode == "in_person", () =>
        {
            RuleFor(x => x.LocationId)
                .NotNull()
                .WithMessage("Location is required for in-person sessions");
            
            RuleFor(x => x.MeetingLink)
                .Null()
                .WithMessage("Meeting link should not be provided for in-person sessions");
        });

        // 4. إذا كانت أونلاين → MeetingLink مطلوب
        When(x => x.TeachingModeCode == "online", () =>
        {
            RuleFor(x => x.MeetingLink)
                .NotEmpty()
                .WithMessage("Meeting link is required for online sessions")
                .Must(BeValidUrl)
                .WithMessage("Invalid meeting link URL");
            
            RuleFor(x => x.LocationId)
                .Null()
                .WithMessage("Location should not be provided for online sessions");
        });

        // 5. إذا كانت جماعية → MaxParticipants مطلوب
        When(x => x.SessionTypeCode == "group", () =>
        {
            RuleFor(x => x.MaxParticipants)
                .NotNull()
                .GreaterThan(1)
                .WithMessage("Max participants must be greater than 1 for group sessions");
        });

        // 6. إذا كانت فردية → MaxParticipants غير مطلوب
        When(x => x.SessionTypeCode == "individual", () =>
        {
            RuleFor(x => x.MaxParticipants)
                .Null()
                .WithMessage("Max participants should not be provided for individual sessions");
        });
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
```

---

## 📊 جدول المقارنة الشامل

| السيناريو | TeachingMode | SessionType | LocationId | MeetingLink | MaxParticipants | مثال حقيقي |
|-----------|--------------|-------------|------------|-------------|-----------------|------------|
| **درس خصوصي في المنزل** | حضوري (`in_person`) | فردي (`individual`) | ✅ مطلوب | ❌ | ❌ | معلم قرآن يزور منزل الطالب |
| **محاضرة في القاعة** | حضوري (`in_person`) | جماعي (`group`) | ✅ مطلوب | ❌ | ✅ مطلوب | دورة برمجة في مركز القلم (25 طالب) |
| **جلسة Zoom خاصة** | أونلاين (`online`) | فردي (`individual`) | ❌ | ✅ مطلوب | ❌ | معلم إنجليزي مع طالب واحد |
| **ويبينار أونلاين** | أونلاين (`online`) | جماعي (`group`) | ❌ | ✅ مطلوب | ✅ مطلوب | محاضرة تاريخ لـ 100 طالب عبر Teams |

---

## 🔧 أمثلة API Endpoints

### POST /api/sessions/create

#### مثال 1: إنشاء جلسة حضورية فردية

```json
POST /api/sessions/create
Content-Type: application/json
Authorization: Bearer {teacher_token}

{
  "teachingModeId": 1,
  "sessionTypeId": 1,
  "locationId": 5,
  "meetingLink": null,
  "teacherId": 10,
  "subjectId": 50,
  "startTime": "2026-01-15T09:00:00Z",
  "durationMinutes": 45,
  "price": 50.00
}
```

**Response:**

```json
{
  "succeeded": true,
  "message": "Session created successfully",
  "data": {
    "sessionId": 123,
    "teachingMode": "حضوري",
    "sessionType": "فردي",
    "location": "مسجد الحي",
    "startTime": "2026-01-15T09:00:00Z",
    "price": 50.00
  }
}
```

---

#### مثال 2: إنشاء جلسة أونلاين جماعية

```json
POST /api/sessions/create
Content-Type: application/json
Authorization: Bearer {teacher_token}

{
  "teachingModeId": 2,
  "sessionTypeId": 2,
  "locationId": null,
  "meetingLink": "https://zoom.us/j/123456789",
  "teacherId": 25,
  "subjectId": 150,
  "startTime": "2026-01-20T19:00:00Z",
  "durationMinutes": 120,
  "maxParticipants": 100,
  "price": 15.00
}
```

**Response:**

```json
{
  "succeeded": true,
  "message": "Session created successfully",
  "data": {
    "sessionId": 124,
    "teachingMode": "أونلاين",
    "sessionType": "جماعي",
    "meetingLink": "https://zoom.us/j/123456789",
    "maxParticipants": 100,
    "startTime": "2026-01-20T19:00:00Z",
    "price": 15.00
  }
}
```

---

### GET /api/sessions/search

#### البحث عن جلسات أونلاين فردية

```http
GET /api/sessions/search?teachingMode=online&sessionType=individual&startDate=2026-01-15
```

**Response:**

```json
{
  "succeeded": true,
  "data": [
    {
      "sessionId": 125,
      "teacherName": "أحمد محمد",
      "subjectName": "English Conversation - B1",
      "teachingMode": "أونلاين",
      "sessionType": "فردي",
      "meetingLink": "https://zoom.us/j/987654321",
      "startTime": "2026-01-15T18:00:00Z",
      "durationMinutes": 60,
      "price": 80.00
    },
    {
      "sessionId": 126,
      "teacherName": "فاطمة أحمد",
      "subjectName": "Python Programming - Advanced",
      "teachingMode": "أونلاين",
      "sessionType": "فردي",
      "meetingLink": "https://meet.google.com/abc-defg-hij",
      "startTime": "2026-01-16T20:00:00Z",
      "durationMinutes": 90,
      "price": 120.00
    }
  ]
}
```

---

## 📱 استعلامات SQL مباشرة

### 1. عدد الجلسات حسب النوع والمكان

```sql
SELECT 
    tm.NameAr AS 'طريقة التدريس',
    st.NameAr AS 'نوع الجلسة',
    COUNT(*) AS 'عدد الجلسات',
    SUM(ts.Price) AS 'الإجمالي',
    AVG(ts.Price) AS 'متوسط السعر'
FROM TeachingSessions ts
INNER JOIN TeachingModes tm ON ts.TeachingModeId = tm.Id
INNER JOIN SessionTypes st ON ts.SessionTypeId = st.Id
GROUP BY tm.NameAr, st.NameAr
ORDER BY COUNT(*) DESC;
```

---

### 2. الجلسات الحضورية مع المواقع

```sql
SELECT 
    ts.Id,
    t.FullName AS 'المعلم',
    s.NameAr AS 'المادة',
    l.NameAr AS 'الموقع',
    st.NameAr AS 'نوع الجلسة',
    ts.StartTime AS 'موعد الجلسة',
    ts.Price AS 'السعر'
FROM TeachingSessions ts
INNER JOIN TeachingModes tm ON ts.TeachingModeId = tm.Id
INNER JOIN SessionTypes st ON ts.SessionTypeId = st.Id
INNER JOIN Locations l ON ts.LocationId = l.Id
INNER JOIN Teachers t ON ts.TeacherId = t.Id
INNER JOIN Subjects s ON ts.SubjectId = s.Id
WHERE tm.Code = 'in_person'
ORDER BY ts.StartTime;
```

---

### 3. الويبينارات القادمة (أونلاين + جماعي)

```sql
SELECT 
    ts.Id,
    t.FullName AS 'المعلم',
    s.NameAr AS 'المادة',
    ts.StartTime AS 'موعد البدء',
    ts.DurationMinutes AS 'المدة',
    ts.MaxParticipants AS 'العدد الأقصى',
    (SELECT COUNT(*) FROM Enrollments WHERE SessionId = ts.Id) AS 'المسجلين',
    (ts.MaxParticipants - (SELECT COUNT(*) FROM Enrollments WHERE SessionId = ts.Id)) AS 'المقاعد المتاحة',
    ts.Price AS 'السعر',
    ts.MeetingLink AS 'رابط الاجتماع'
FROM TeachingSessions ts
INNER JOIN TeachingModes tm ON ts.TeachingModeId = tm.Id
INNER JOIN SessionTypes st ON ts.SessionTypeId = st.Id
INNER JOIN Teachers t ON ts.TeacherId = t.Id
INNER JOIN Subjects s ON ts.SubjectId = s.Id
WHERE tm.Code = 'online'
  AND st.Code = 'group'
  AND ts.MaxParticipants >= 50
  AND ts.StartTime > GETDATE()
ORDER BY ts.StartTime;
```

---

## 💡 نصائح للمطورين

### 1. استخدم Include() دائماً

```csharp
// ✅ جيد - تحميل العلاقات مسبقاً
var sessions = await _context.TeachingSessions
    .Include(s => s.TeachingMode)
    .Include(s => s.SessionType)
    .Include(s => s.Location)
    .Include(s => s.Teacher)
    .ToListAsync();

// ❌ سيء - سيسبب N+1 queries
var sessions = await _context.TeachingSessions.ToListAsync();
// ثم الوصول لـ session.TeachingMode.NameAr سيسبب استعلام إضافي لكل session
```

---

### 2. استخدم الـ Code وليس الـ ID في الشروط

```csharp
// ✅ جيد - واضح وسهل القراءة
.Where(s => s.TeachingMode.Code == "online")

// ❌ أقل وضوحاً - يتطلب معرفة الـ ID
.Where(s => s.TeachingModeId == 2)
```

---

### 3. تحقق من المتطلبات قبل الحفظ

```csharp
// قبل حفظ الجلسة
if (session.TeachingMode.Code == "in_person" && session.LocationId == null)
    throw new ValidationException("Location is required for in-person sessions");

if (session.TeachingMode.Code == "online" && string.IsNullOrEmpty(session.MeetingLink))
    throw new ValidationException("Meeting link is required for online sessions");

if (session.SessionType.Code == "group" && (!session.MaxParticipants.HasValue || session.MaxParticipants <= 1))
    throw new ValidationException("Max participants must be greater than 1 for group sessions");
```

---

### 4. استخدم DTOs للـ API Responses

```csharp
public class SessionDto
{
    public int Id { get; set; }
    public string TeacherName { get; set; }
    public string SubjectName { get; set; }
    
    // بدلاً من إرجاع الكائن كاملاً، أرجع فقط الاسم
    public string TeachingMode { get; set; }  // "حضوري" أو "أونلاين"
    public string SessionType { get; set; }   // "فردي" أو "جماعي"
    
    // الحقول الاختيارية حسب النوع
    public string? LocationName { get; set; }
    public string? MeetingLink { get; set; }
    public int? MaxParticipants { get; set; }
    public int EnrolledCount { get; set; }
    
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
}
```

---

### 5. لا تخلط المفهومين

```csharp
// ❌ خطأ في الصياغة
"هل الجلسة حضورية فردية؟"

// ✅ الصياغة الصحيحة
"هل الجلسة حضورية AND فردية؟"
// أو
"ما هي طريقة التدريس (حضوري/أونلاين)؟ وما هو نوع الجلسة (فردي/جماعي)؟"
```

---

## 🎯 حالات استخدام واقعية

### 1. معلم قرآن يقدم دروس في المسجد

```csharp
// المعلم: أحمد - متخصص في تحفيظ القرآن
// يقدم دروس خصوصية في المسجد

var sessions = new List<TeachingSession>
{
    // درس فردي صباحي
    new()
    {
        TeachingModeId = 1,  // حضوري
        SessionTypeId = 1,   // فردي
        LocationId = 3,      // مسجد الحي
        TeacherId = 5,
        SubjectId = 45,      // حفظ جزء عم
        StartTime = DateTime.Today.AddDays(1).AddHours(8),
        DurationMinutes = 45,
        Price = 40.00m
    },
    // درس فردي مسائي
    new()
    {
        TeachingModeId = 1,  // حضوري
        SessionTypeId = 1,   // فردي
        LocationId = 3,      // مسجد الحي
        TeacherId = 5,
        SubjectId = 46,      // تجويد سورة البقرة
        StartTime = DateTime.Today.AddDays(1).AddHours(17),
        DurationMinutes = 60,
        Price = 50.00m
    }
};

await _context.TeachingSessions.AddRangeAsync(sessions);
await _context.SaveChangesAsync();
```

---

### 2. مركز تعليمي يقدم دورات جماعية

```csharp
// مركز القلم - يقدم دورات برمجة جماعية حضورية

var course = new TeachingSession
{
    TeachingModeId = 1,      // حضوري
    SessionTypeId = 2,       // جماعي
    LocationId = 10,         // قاعة التدريب - مركز القلم
    MaxParticipants = 25,    // حد أقصى 25 طالب
    TeacherId = 15,
    SubjectId = 120,         // Python Programming - Beginner
    StartTime = DateTime.Today.AddDays(7).AddHours(16),
    DurationMinutes = 120,   // ساعتان
    Price = 30.00m           // سعر معقول للدورة الجماعية
};

await _context.TeachingSessions.AddAsync(course);
await _context.SaveChangesAsync();
```

---

### 3. معلم لغة إنجليزية يقدم دروس أونلاين

```csharp
// المعلمة: سارة - متخصصة في تعليم الإنجليزية أونلاين

var privateLessons = new List<TeachingSession>
{
    // درس خصوصي أونلاين - طالب مبتدئ
    new()
    {
        TeachingModeId = 2,  // أونلاين
        SessionTypeId = 1,   // فردي
        MeetingLink = "https://zoom.us/j/111111111",
        TeacherId = 20,
        SubjectId = 80,      // English Conversation - A2
        StartTime = DateTime.Today.AddDays(2).AddHours(18),
        DurationMinutes = 60,
        Price = 70.00m
    },
    // درس خصوصي أونلاين - طالب متقدم
    new()
    {
        TeachingModeId = 2,  // أونلاين
        SessionTypeId = 1,   // فردي
        MeetingLink = "https://zoom.us/j/222222222",
        TeacherId = 20,
        SubjectId = 85,      // Business English - C1
        StartTime = DateTime.Today.AddDays(3).AddHours(20),
        DurationMinutes = 90,
        Price = 120.00m
    }
};

await _context.TeachingSessions.AddRangeAsync(privateLessons);
await _context.SaveChangesAsync();
```

---

### 4. خبير تقني يقدم ويبينار مجاني

```csharp
// خبير: محمد - يقدم ويبينار مجاني عن الذكاء الاصطناعي

var freeWebinar = new TeachingSession
{
    TeachingModeId = 2,          // أونلاين
    SessionTypeId = 2,           // جماعي
    MaxParticipants = 500,       // ويبينار كبير
    MeetingLink = "https://teams.microsoft.com/l/meetup-join/...",
    TeacherId = 30,
    SubjectId = 200,             // Introduction to AI & Machine Learning
    StartTime = DateTime.Today.AddDays(10).AddHours(19),
    DurationMinutes = 180,       // 3 ساعات
    Price = 0.00m                // مجاني!
};

await _context.TeachingSessions.AddAsync(freeWebinar);
await _context.SaveChangesAsync();
```

---

## 📝 الخلاصة

### النقاط الرئيسية

1. **TeachingMode** = المكان (أين؟) → حضوري أو أونلاين
2. **SessionType** = الحجم (كم؟) → فردي أو جماعي
3. مستقلان تماماً → يمكن دمجهما بـ **4 طرق**
4. كل سيناريو له **متطلبات خاصة**:
   - حضوري → `LocationId` مطلوب
   - أونلاين → `MeetingLink` مطلوب
   - جماعي → `MaxParticipants` مطلوب

---

### الفوائد

✅ **وضوح تام** - لا لبس في المفاهيم  
✅ **مرونة عالية** - 4 سيناريوهات مختلفة  
✅ **سهولة الصيانة** - كود نظيف وموثق  
✅ **قابلية التوسع** - يمكن إضافة أنواع جديدة بسهولة  
✅ **سهولة الاستعلام** - استعلامات واضحة ومباشرة

---

*آخر تحديث: يناير 2026*