# تعليمات تشغيل الاختبارات | Testing Instructions

## المتطلبات الأساسية | Prerequisites

### 1. تشغيل الـ API | Start the API

افتح Visual Studio أو Terminal وشغل الـ API:

Open Visual Studio or Terminal and run the API:

```powershell
cd "C:\Users\user\OneDrive\المستندات\Visual Studio 2022\projects\Qalam\Qalam.Api"
dotnet run
```

أو من Visual Studio:
- افتح الـ Solution
- اضغط F5 أو Start

تأكد من أن الـ API يعمل على:
- `http://localhost:5000`
- `https://localhost:5001`

### 2. استيراد Postman Collection | Import Postman Collection

1. افتح Postman
2. اضغط Import
3. اختر الملفات:
   - `Qalam.postman_collection.json`
   - `Postman/Qalam-Testing.postman_environment.json`

### 3. اختيار البيئة | Select Environment

في Postman، اختر "Qalam Testing Environment" من القائمة المنسدلة في أعلى اليمين.

---

## تنفيذ الاختبارات | Running Tests

### السيناريو 1: المسار الصحيح الكامل | Happy Path

#### خطوات المعلم | Teacher Steps

1. **إرسال OTP**
   - افتح: `Public > Teacher Registration Flow > Step 1: Send OTP`
   - غيّر رقم الهاتف إلى رقم جديد
   - اضغط Send
   - النتيجة المتوقعة: 200 OK

2. **التحقق من OTP**
   - افتح: `Step 2: Verify OTP (Login/Register)`
   - استخدم نفس رقم الهاتف
   - رمز OTP: `1234`
   - اضغط Send
   - النتيجة: Token يتم حفظه تلقائياً في `teacher_token`

3. **إكمال المعلومات الشخصية**
   - افتح: `Teacher Role > Registration > Step 3: Complete Personal Info`
   - اضغط Send
   - النتيجة: حساب المعلم مكتمل

4. **رفع الوثائق**
   - افتح: `Step 4: Upload Documents`
   - اختر ملفات PDF/صور للهوية والشهادات
   - اضغط Send
   - النتيجة: المعلم في حالة `PendingVerification`

#### خطوات الإدارة | Admin Steps

5. **تسجيل دخول المسؤول**
   - افتح: `Public > Authentication > Login`
   - غيّر البيانات إلى:
     ```json
     {
       "userName": "admin",
       "password": "Admin@123456"
     }
     ```
   - انسخ الـ Token واحفظه في المتغير `admin_token`

6. **عرض المعلمين المعلقين**
   - افتح: `Admin Role > Teacher Management > Get Pending Teachers`
   - اضغط Send
   - انسخ `teacherId` واحفظه في المتغير `teacher_id`

7. **عرض تفاصيل المعلم**
   - افتح: `Get Teacher Details`
   - اضغط Send
   - انسخ `documentId` للوثائق واحفظها في `document_id`

8. **الموافقة على الوثائق**
   - افتح: `Approve Document`
   - اضغط Send لكل وثيقة
   - بعد آخر موافقة: المعلم يصبح `Active`

---

### السيناريو 2: الرفض وإعادة الرفع | Rejection Flow

1. اتبع خطوات المعلم (1-4) من السيناريو 1
2. كمسؤول، ارفض وثيقة واحدة:
   - افتح: `Reject Document`
   - Body:
     ```json
     {
       "reason": "الوثيقة غير واضحة"
     }
     ```
3. كمعلم، تحقق من الحالة:
   - افتح: `Teacher Role > Document Management > Get Documents Status`
4. أعد رفع الوثيقة المرفوضة:
   - افتح: `Reupload Rejected Document`
   - غيّر `document_id` للوثيقة المرفوضة
5. كمسؤول، وافق على الوثيقة

---

### السيناريو 3: حظر المعلم | Block Teacher

1. اتبع خطوات المعلم (1-4)
2. كمسؤول:
   - افتح: `Block Teacher`
   - Body:
     ```json
     {
       "reason": "وثائق مشبوهة"
     }
     ```
3. تحقق: المعلم لا يستطيع الوصول لأي endpoint

---

## اختبارات التحقق السلبية | Validation Tests

### رقم هاتف مكرر | Duplicate Phone

1. سجل بنفس رقم الهاتف مرتين
2. النتيجة المتوقعة: خطأ "Phone already registered"

### رمز OTP خاطئ | Wrong OTP

1. استخدم أي رمز غير `1234`
2. النتيجة المتوقعة: خطأ "Invalid OTP"

### نوع هوية خاطئ | Wrong Identity Type

1. استخدم `IdentityType: 3` (Passport) مع `IsInSaudiArabia: true`
2. النتيجة المتوقعة: خطأ "Passport not allowed inside Saudi Arabia"

### ملف كبير | Large File

1. ارفع ملف أكبر من 10MB
2. النتيجة المتوقعة: خطأ "File size exceeds limit"

### نوع ملف غير صالح | Invalid File Type

1. ارفع ملف `.exe` أو `.txt`
2. النتيجة المتوقعة: خطأ "Invalid file type"

---

## قائمة التحقق النهائية | Final Checklist

### اختبارات المعلم ✓

- [ ] التسجيل الكامل (خطوات 1-4)
- [ ] رقم مكرر
- [ ] OTP خاطئ
- [ ] كلمة مرور ضعيفة
- [ ] نوع هوية خاطئ
- [ ] ملف كبير
- [ ] نوع ملف غير صالح
- [ ] عرض حالة الوثائق
- [ ] إعادة رفع وثيقة

### اختبارات الإدارة ✓

- [ ] تسجيل دخول المسؤول
- [ ] عرض المعلمين المعلقين
- [ ] عرض تفاصيل المعلم
- [ ] الموافقة على وثيقة
- [ ] رفض وثيقة
- [ ] حظر معلم

### E2E ✓

- [ ] المسار الصحيح الكامل
- [ ] الرفض وإعادة الرفع
- [ ] الحظر

---

## ملفات الاختبار المُنشأة | Created Test Files

| الملف | الموقع | الوصف |
|-------|--------|-------|
| `Testing-Workflow-Guide.md` | `docs/` | دليل شامل للاختبار |
| `TESTING_INSTRUCTIONS.md` | `docs/` | هذا الملف |
| `Qalam-Testing.postman_environment.json` | `Postman/` | بيئة Postman |
| `TestData/README.md` | `Postman/TestData/` | تعليمات ملفات الاختبار |

---

## الدعم | Support

إذا واجهت مشاكل:

1. تأكد من تشغيل الـ API
2. تأكد من استخدام البيئة الصحيحة في Postman
3. تأكد من أن الـ Token محدث
4. راجع logs الـ API للأخطاء
