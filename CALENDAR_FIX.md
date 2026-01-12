# Calendar Configuration - إصلاح التقويم 📅

## المشكلة
كانت التواريخ تُحفظ في قاعدة البيانات بالتقويم الهجري بدلاً من التقويم الميلادي (Gregorian).

## السبب
الثقافة العربية `ar-EG` في .NET تستخدم التقويم الهجري بشكل افتراضي، مما يؤدي إلى:
- تواريخ خاطئة عند استخدام `DateTime.Now` أو `DateTime.UtcNow`
- مشاكل في حفظ التواريخ في SQL Server
- عدم تطابق التواريخ بين الأنظمة المختلفة

---

## الحل المطبق ✅

### 1. إعدادات عامة في بداية التطبيق

تم إضافة الكود التالي في بداية `Program.cs`:

```csharp
// Force Gregorian calendar for all cultures to prevent Hijri dates in database
var defaultCulture = new CultureInfo("en-US");
defaultCulture.DateTimeFormat.Calendar = new GregorianCalendar();
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;
```

**الفائدة:**
- جميع العمليات في التطبيق تستخدم التقويم الميلادي افتراضياً
- يؤثر على جميع الـ Threads في التطبيق

---

### 2. إعدادات Localization للثقافة العربية

تم تعديل إعدادات `RequestLocalizationOptions`:

```csharp
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // Create cultures with Gregorian calendar
    var enCulture = new CultureInfo("en-US");
    
    var arCulture = new CultureInfo("ar-EG");
    // Force Arabic culture to use Gregorian calendar instead of Hijri
    arCulture.DateTimeFormat.Calendar = new GregorianCalendar();
    
    List<CultureInfo> supportedCultures = new List<CultureInfo>
    {
        enCulture,
        arCulture
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
```

**الفائدة:**
- حتى عند تغيير اللغة للعربية، التواريخ تبقى ميلادية
- يدعم Localization بشكل صحيح

---

## التحقق من الإصلاح 🔍

### 1. اختبار في C#

```csharp
// هذا الآن يعيد تاريخ ميلادي بغض النظر عن الثقافة
var now = DateTime.Now;
Console.WriteLine(now.ToString("yyyy-MM-dd")); 
// النتيجة: 2026-01-12 (ميلادي)

// اختبار مع الثقافة العربية
Thread.CurrentThread.CurrentCulture = new CultureInfo("ar-EG");
var nowAr = DateTime.Now;
Console.WriteLine(nowAr.ToString("yyyy-MM-dd"));
// النتيجة: 2026-01-12 (ميلادي أيضاً!)
```

---

### 2. اختبار في SQL Server

```sql
-- تحقق من التواريخ المحفوظة
SELECT TOP 10
    Id,
    CreatedAt,
    UpdatedAt,
    YEAR(CreatedAt) AS Year,
    MONTH(CreatedAt) AS Month,
    DAY(CreatedAt) AS Day
FROM Users
ORDER BY CreatedAt DESC;

-- التواريخ يجب أن تكون ميلادية:
-- Year = 2026, Month = 1, Day = 12
-- وليس: Year = 1446 (هجري)
```

---

### 3. اختبار في API

```bash
# إنشاء مستخدم جديد
POST /api/Authentication/Register
{
  "firstName": "أحمد",
  "email": "test@example.com",
  "password": "Test@123"
}

# التحقق من التاريخ المحفوظ
GET /api/Users/1

# النتيجة يجب أن تحتوي على:
{
  "createdAt": "2026-01-12T10:30:00Z",  ✅ ميلادي
  // وليس:
  // "createdAt": "1446-07-11T10:30:00Z"  ❌ هجري
}
```

---

## ملاحظات هامة 📝

### 1. SQL Server
- `GETUTCDATE()` و `GETDATE()` في SQL Server **دائماً تعيد تواريخ ميلادية**
- المشكلة كانت فقط في طبقة .NET

### 2. Entity Framework
- `DateTime` properties في الـ Entities تُحفظ الآن بشكل صحيح
- `AuditableEntity` (CreatedAt, UpdatedAt) تعمل بشكل صحيح

### 3. Localization
- عرض التواريخ للمستخدم يمكن أن يكون بالصيغة العربية (مثلاً: "١٢ يناير ٢٠٢٦")
- لكن القيمة المخزنة في قاعدة البيانات تبقى ميلادية

---

## البيانات القديمة (إذا وُجدت) ⚠️

إذا كانت لديك بيانات قديمة محفوظة بالتقويم الهجري، ستحتاج لتحويلها:

```sql
-- مثال لتحويل التواريخ الهجرية إلى ميلادية (إذا لزم الأمر)
-- ملاحظة: هذا مثال فقط - قد تحتاج لتعديل حسب البيانات الفعلية

-- لا تنفذ هذا إلا إذا كنت متأكداً من وجود بيانات هجرية!
-- UPDATE Users
-- SET CreatedAt = CONVERT(datetime2, CreatedAt, 131) -- Convert from Hijri to Gregorian
-- WHERE YEAR(CreatedAt) > 1440; -- Only Hijri years
```

**تحذير:** احتفظ بنسخة احتياطية قبل أي تعديل على البيانات!

---

## الخلاصة ✅

| العنصر | قبل الإصلاح | بعد الإصلاح |
|--------|-------------|-------------|
| **Default Culture** | ar-EG (Hijri) | en-US (Gregorian) |
| **Arabic Culture** | Uses Hijri | Uses Gregorian |
| **DateTime.Now** | Hijri date | ✅ Gregorian date |
| **Database Dates** | Hijri (1446-07-11) | ✅ Gregorian (2026-01-12) |
| **API Responses** | Hijri dates | ✅ Gregorian dates |

---

## التطبيق 🚀

بعد هذه التغييرات:

1. ✅ **لا حاجة لإعادة إنشاء قاعدة البيانات**
2. ✅ **جميع التواريخ الجديدة ستُحفظ بالتقويم الميلادي**
3. ✅ **يدعم Localization بشكل صحيح**
4. ✅ **متوافق مع جميع الأنظمة**

---

*آخر تحديث: يناير 2026*
