# نظام تسجيل الطلاب وأولياء الأمور - دليل الخطوات الذكية

## نظرة عامة

تم تطوير نظام ذكي لتوجيه المستخدمين خلال عملية التسجيل بناءً على نوع الحساب (Student/Parent/Both) وطريقة الاستخدام (UsageMode). يوفر النظام توضيحاً دقيقاً للخطوات الإلزامية والاختيارية في كل مرحلة.

## الحقول الجديدة في Response

تم إضافة الحقول التالية إلى `StudentRegistrationResponseDto`:

```
- NextStepName: string (required)
  الخطوة التالية الأساسية (مثل: "CompleteAcademicProfile", "AddChildren", "Dashboard")

- IsNextStepRequired: boolean
  هل الخطوة التالية إلزامية أم اختيارية

- OptionalSteps: List<string>
  قائمة بالخطوات الاختيارية المتاحة للمستخدم

- NextStepDescription: string
  وصف واضح بما يجب على المستخدم فعله في الخطوة التالية
```

## سيناريوهات التسجيل الكاملة

### السيناريو الأول: تسجيل كطالب فقط (Student)

**نوع الحساب**: Student  
**الكيانات المُنشأة**: Student entity فقط  
**الأدوار المُضافة**: Student role

**الخطوة التالية**:
```json
{
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": [],
  "nextStepDescription": "Complete your academic profile to start."
}
```

**التفسير**: الطالب يجب عليه إكمال البيانات الأكاديمية (المجال، المنهج، المرحلة، الصف) قبل أن يتمكن من الدخول للنظام.

---

### السيناريو الثاني: تسجيل كولي أمر + سيدرس بنفسه (Parent + StudySelf)

**نوع الحساب**: Parent  
**طريقة الاستخدام**: StudySelf  
**الكيانات المُنشأة**: Guardian + Student entities  
**الأدوار المُضافة**: Guardian + Student roles

**الخطوة التالية**:
```json
{
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": ["AddChildren"],
  "nextStepDescription": "Complete your academic profile. You can also add children later."
}
```

**التفسير**: ولي الأمر سيدرس بنفسه، لذا يجب عليه إكمال بياناته الأكاديمية أولاً. يمكنه إضافة أبناء لاحقاً (اختياري).

---

### السيناريو الثالث: تسجيل كولي أمر + سيضيف أبناء فقط (Parent + AddChildren)

**نوع الحساب**: Parent  
**طريقة الاستخدام**: AddChildren  
**الكيانات المُنشأة**: Guardian entity فقط  
**الأدوار المُضافة**: Guardian role

**الخطوة التالية**:
```json
{
  "nextStepName": "AddChildren",
  "isNextStepRequired": false,
  "optionalSteps": ["Dashboard"],
  "nextStepDescription": "You can add children now or skip to dashboard."
}
```

**التفسير**: ولي الأمر لن يدرس بنفسه، فقط سيدير أبناءه. يمكنه إضافة أبناء الآن أو تخطي ذلك والذهاب للوحة التحكم مباشرة.

---

### السيناريو الرابع: تسجيل كولي أمر + سيدرس وسيضيف أبناء (Parent + Both)

**نوع الحساب**: Parent  
**طريقة الاستخدام**: Both  
**الكيانات المُنشأة**: Guardian + Student entities  
**الأدوار المُضافة**: Guardian + Student roles

**الخطوة التالية**:
```json
{
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": ["AddChildren"],
  "nextStepDescription": "Complete your academic profile first, then you can add children."
}
```

**التفسير**: ولي الأمر سيدرس بنفسه وأيضاً سيضيف أبناء. يجب عليه أولاً إكمال بياناته الأكاديمية، ثم يمكنه إضافة الأبناء لاحقاً.

---

### السيناريو الخامس: تسجيل كطالب وولي أمر معاً (Both)

**نوع الحساب**: Both  
**طريقة الاستخدام**: أي قيمة (StudySelf/AddChildren/Both)  
**الكيانات المُنشأة**: Student + Guardian entities  
**الأدوار المُضافة**: Student + Guardian roles

**الخطوة التالية**:
```json
{
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": ["AddChildren"],
  "nextStepDescription": "Complete your academic profile. You can add children anytime."
}
```

**التفسير**: المستخدم مسجل كطالب وولي أمر معاً. يجب عليه أولاً إكمال بياناته الأكاديمية كطالب، ويمكنه إضافة أبناء في أي وقت.

---

## تدفق التسجيل خطوة بخطوة

### الخطوة 1: إرسال OTP (SendOtp)

**Endpoint**: `POST /Api/V1/Authentication/Student/SendOtp`

**Request**:
```json
{
  "countryCode": "+966",
  "phoneNumber": "501234567"
}
```

**Response**:
```json
{
  "isNewUser": true,
  "message": "OTP sent successfully.",
  "phoneNumber": "+966501234567"
}
```

---

### الخطوة 2: التحقق من OTP (VerifyOtp)

**Endpoint**: `POST /Api/V1/Authentication/Student/VerifyOtp`

**Request**:
```json
{
  "phoneNumber": "501234567",
  "otpCode": "1234",
  "countryCode": "+966"
}
```

**Response للمستخدم الجديد**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 1,
  "nextStepName": "ChooseAccountType",
  "isNextStepRequired": true,
  "optionalSteps": [],
  "nextStepDescription": "Choose your account type and complete profile.",
  "isRegistrationComplete": false,
  "message": "Verified. Choose account type and complete profile."
}
```

**Response للمستخدم الموجود (لديه Student/Guardian role)**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 1,
  "nextStepName": "Dashboard",
  "isNextStepRequired": false,
  "optionalSteps": [],
  "nextStepDescription": "Welcome back!",
  "isRegistrationComplete": true,
  "message": "Signed in successfully."
}
```

**Response للمستخدم الموجود (لديه Teacher role فقط)**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 1,
  "nextStepName": "ChooseAccountType",
  "isNextStepRequired": true,
  "optionalSteps": [],
  "nextStepDescription": "Choose account type to add student/parent capabilities.",
  "isRegistrationComplete": false,
  "message": "Verified. Choose account type to add student/parent capabilities."
}
```

---

### الخطوة 3: تحديد نوع الحساب وطريقة الاستخدام (SetAccountTypeAndUsage)

**Endpoint**: `POST /Api/V1/Authentication/Student/SetAccountTypeAndUsage`  
**Authorization**: Required (Bearer Token)

#### مثال 1: طالب فقط

**Request**:
```json
{
  "data": {
    "accountType": "Student",
    "firstName": "أحمد",
    "lastName": "علي",
    "email": "ahmed@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "2000-05-15",
    "cityOrRegion": "الرياض"
  }
}
```

**Response**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 2,
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": [],
  "nextStepDescription": "Complete your academic profile to start.",
  "isRegistrationComplete": false,
  "message": "Account type set successfully."
}
```

#### مثال 2: ولي أمر سيدرس بنفسه

**Request**:
```json
{
  "data": {
    "accountType": "Parent",
    "usageMode": "StudySelf",
    "firstName": "فاطمة",
    "lastName": "حسن",
    "email": "fatima@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1985-03-20",
    "cityOrRegion": "جدة"
  }
}
```

**Response**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 2,
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": ["AddChildren"],
  "nextStepDescription": "Complete your academic profile. You can also add children later.",
  "isRegistrationComplete": false,
  "message": "Account type set successfully."
}
```

#### مثال 3: ولي أمر سيضيف أبناء فقط

**Request**:
```json
{
  "data": {
    "accountType": "Parent",
    "usageMode": "AddChildren",
    "firstName": "محمد",
    "lastName": "إبراهيم",
    "email": "mohammed@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1980-07-10"
  }
}
```

**Response**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 2,
  "nextStepName": "AddChildren",
  "isNextStepRequired": false,
  "optionalSteps": ["Dashboard"],
  "nextStepDescription": "You can add children now or skip to dashboard.",
  "isRegistrationComplete": false,
  "message": "Account type set successfully."
}
```

#### مثال 4: طالب وولي أمر معاً

**Request**:
```json
{
  "data": {
    "accountType": "Both",
    "usageMode": "Both",
    "firstName": "سارة",
    "lastName": "أحمد",
    "email": "sara@example.com",
    "password": "SecurePass123!",
    "dateOfBirth": "1990-12-25"
  }
}
```

**Response**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 2,
  "nextStepName": "CompleteAcademicProfile",
  "isNextStepRequired": true,
  "optionalSteps": ["AddChildren"],
  "nextStepDescription": "Complete your academic profile. You can add children anytime.",
  "isRegistrationComplete": false,
  "message": "Account type set successfully."
}
```

---

### الخطوة 4: إكمال البيانات الأكاديمية (CompleteProfile)

**Endpoint**: `POST /Api/V1/Authentication/Student/CompleteProfile`  
**Authorization**: Required (Student or Guardian role)

**Request**:
```json
{
  "profile": {
    "domainId": 1,
    "curriculumId": 2,
    "levelId": 3,
    "gradeId": 5
  }
}
```

**Response (مستخدم لديه Student role فقط)**:
```json
{
  "currentStep": 3,
  "nextStepName": "Dashboard",
  "isNextStepRequired": false,
  "optionalSteps": [],
  "nextStepDescription": "Profile completed successfully!",
  "isRegistrationComplete": true,
  "message": "Academic profile saved successfully."
}
```

**Response (مستخدم لديه Student + Guardian roles)**:
```json
{
  "currentStep": 3,
  "nextStepName": "Dashboard",
  "isNextStepRequired": false,
  "optionalSteps": ["AddChildren"],
  "nextStepDescription": "Profile completed! You can add children or go to dashboard.",
  "isRegistrationComplete": true,
  "message": "Academic profile saved successfully."
}
```

---

### الخطوة 5 (اختيارية): إضافة أبناء (AddChild)

**Endpoint**: `POST /Api/V1/Authentication/Student/AddChild`  
**Authorization**: Required (Guardian role)

**Request**:
```json
{
  "child": {
    "fullName": "عبدالله محمد",
    "dateOfBirth": "2010-06-15",
    "gender": 1,
    "guardianRelation": 1,
    "domainId": 1,
    "curriculumId": 2,
    "levelId": 2,
    "gradeId": 3
  }
}
```

**Response**:
```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Child added successfully.",
  "data": 25
}
```

**ملاحظة**: هذه الخطوة اختيارية ويمكن تنفيذها في أي وقت بعد التسجيل.

---

## حالات خاصة

### إعادة تسجيل أدوار موجودة

إذا حاول المستخدم تسجيل دور موجود بالفعل (مثلاً لديه Student role ويحاول التسجيل كـ Student مرة أخرى):

**Response**:
```json
{
  "token": "eyJhbGc...",
  "currentStep": 1,
  "nextStepName": "Dashboard",
  "isNextStepRequired": false,
  "optionalSteps": [],
  "nextStepDescription": "You're all set!",
  "isRegistrationComplete": true,
  "message": "Account already set up with requested roles."
}
```

---

### معلم يريد إضافة دور طالب/ولي أمر

المستخدم الموجود بدور Teacher يمكنه إضافة أدوار Student أو Guardian:

1. يُرسل OTP ويتحقق منه
2. يحصل على response مع `nextStepName: "ChooseAccountType"`
3. يختار نوع الحساب المطلوب (Student/Parent/Both)
4. يكمل البيانات المطلوبة
5. يحصل على JWT جديد يحتوي على جميع الأدوار (Teacher + Student/Guardian)

---

## قواعد التحقق (Validation)

### تحديد نوع الحساب (SetAccountTypeAndUsage)

1. **العمر**: يجب أن يكون 18 سنة أو أكثر
2. **AccountType**: إلزامي، القيم المقبولة:
   - "Student" (case-insensitive)
   - "Parent" (case-insensitive)
   - "Both" (case-insensitive)

3. **UsageMode**: إلزامي عندما يكون AccountType = "Parent" أو "Both"، القيم المقبولة:
   - "StudySelf"
   - "AddChildren"
   - "Both"

4. **البيانات الشخصية**:
   - FirstName: إلزامي
   - LastName: إلزامي
   - Email: إلزامي وصالح
   - Password: إلزامي ويجب أن يتطابق مع متطلبات الأمان

### إكمال البيانات الأكاديمية (CompleteProfile)

1. **DomainId**: إلزامي ويجب أن يكون موجوداً
2. **CurriculumId**: اختياري (يعتمد على المجال)
3. **LevelId**: اختياري (يعتمد على المجال)
4. **GradeId**: اختياري (يعتمد على المرحلة)

### إضافة أبناء (AddChild)

1. **FullName**: إلزامي
2. **DateOfBirth**: إلزامي (يجب أن يكون قاصر، أقل من 18 سنة)
3. **Gender**: اختياري
4. **GuardianRelation**: اختياري (أب، أم، أخ، إلخ)
5. **البيانات الأكاديمية**: اختيارية

---

## جدول مقارنة السيناريوهات

| نوع الحساب | UsageMode | الكيانات المُنشأة | الخطوة التالية | إلزامي؟ | خطوات اختيارية |
|------------|-----------|-------------------|----------------|---------|----------------|
| Student | - | Student | CompleteAcademicProfile | نعم | - |
| Parent | StudySelf | Guardian + Student | CompleteAcademicProfile | نعم | AddChildren |
| Parent | AddChildren | Guardian | AddChildren | لا | Dashboard |
| Parent | Both | Guardian + Student | CompleteAcademicProfile | نعم | AddChildren |
| Both | أي قيمة | Student + Guardian | CompleteAcademicProfile | نعم | AddChildren |

---

## ملاحظات تقنية

### التوافق مع الإصدارات السابقة (Backward Compatibility)

الحقول الجديدة المضافة إلى `StudentRegistrationResponseDto` لا تؤثر على العملاء القدامى:

- جميع الحقول القديمة لا تزال موجودة
- الحقول الجديدة إضافية فقط
- العملاء القدامى يمكنهم تجاهل الحقول الجديدة
- العملاء الجدد يستفيدون من التوجيه الأفضل

### دعم متعدد الأدوار (Multi-role Support)

النظام يدعم المستخدمين الذين لديهم أدوار متعددة:

1. **Teacher + Student**: معلم يريد أن يتعلم أيضاً
2. **Teacher + Guardian**: معلم يريد إضافة أبنائه
3. **Student + Guardian**: طالب يريد أيضاً إدارة أبناء آخرين
4. **Teacher + Student + Guardian**: جميع الأدوار معاً

عند إنشاء JWT Token، يتم تضمين جميع الأدوار الحالية للمستخدم.

### معالجة الأخطاء

**أخطاء شائعة**:

1. **عمر أقل من 18**:
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "You must be 18 years or older to register."
}
```

2. **UsageMode مفقود مع Parent**:
```json
{
  "statusCode": 400,
  "succeeded": false,
  "message": "UsageMode is required when AccountType is Parent or Both."
}
```

3. **Student profile غير موجود عند CompleteProfile**:
```json
{
  "statusCode": 404,
  "succeeded": false,
  "message": "Student profile not found. Complete registration first."
}
```

---

## أمثلة تكامل للواجهة الأمامية (Frontend Integration)

### React/TypeScript Example

```typescript
interface NextStepInfo {
  nextStepName: string;
  isNextStepRequired: boolean;
  optionalSteps: string[];
  nextStepDescription: string;
}

function handleRegistrationResponse(response: NextStepInfo) {
  const { nextStepName, isNextStepRequired, optionalSteps, nextStepDescription } = response;
  
  // عرض الوصف للمستخدم
  showMessage(nextStepDescription);
  
  // توجيه المستخدم بناءً على الخطوة التالية
  if (nextStepName === "Dashboard") {
    // الانتقال للوحة التحكم
    router.push("/dashboard");
  } else if (nextStepName === "CompleteAcademicProfile") {
    if (isNextStepRequired) {
      // يجب إكمال البيانات الأكاديمية
      router.push("/complete-profile");
    }
  } else if (nextStepName === "AddChildren") {
    if (!isNextStepRequired) {
      // عرض خيار: إضافة أبناء أو تخطي
      showOptionalDialog("AddChildren", optionalSteps);
    } else {
      router.push("/add-children");
    }
  }
  
  // عرض الخطوات الاختيارية كأزرار إضافية
  if (optionalSteps.length > 0) {
    showOptionalButtons(optionalSteps);
  }
}
```

### Flutter/Dart Example

```dart
class NextStepInfo {
  final String nextStepName;
  final bool isNextStepRequired;
  final List<String> optionalSteps;
  final String nextStepDescription;
  
  NextStepInfo({
    required this.nextStepName,
    required this.isNextStepRequired,
    required this.optionalSteps,
    required this.nextStepDescription,
  });
}

void handleRegistrationResponse(NextStepInfo info) {
  // عرض الوصف
  showSnackBar(info.nextStepDescription);
  
  // التوجيه بناءً على الخطوة
  switch (info.nextStepName) {
    case "Dashboard":
      Navigator.pushReplacementNamed(context, '/dashboard');
      break;
      
    case "CompleteAcademicProfile":
      if (info.isNextStepRequired) {
        Navigator.pushNamed(context, '/complete-profile');
      } else {
        showOptionalDialog(info);
      }
      break;
      
    case "AddChildren":
      if (!info.isNextStepRequired) {
        showDialog(
          context: context,
          builder: (context) => OptionalStepDialog(
            message: info.nextStepDescription,
            optionalSteps: info.optionalSteps,
          ),
        );
      } else {
        Navigator.pushNamed(context, '/add-children');
      }
      break;
  }
}
```

---

## الخلاصة

النظام الجديد يوفر:

1. ✅ **توجيه واضح**: المستخدم يعرف دائماً ما هي الخطوة التالية المطلوبة
2. ✅ **مرونة عالية**: الخطوات الاختيارية لا تعيق تقدم المستخدم
3. ✅ **منطق ذكي**: يأخذ في الاعتبار نوع الحساب وطريقة الاستخدام
4. ✅ **دعم متعدد الأدوار**: يعمل بسلاسة مع المستخدمين ذوي الأدوار المتعددة
5. ✅ **توافق كامل**: لا يؤثر على الأنظمة القديمة
6. ✅ **سهولة التكامل**: واجهة واضحة ومباشرة للواجهة الأمامية

---

**تاريخ التحديث**: 2026-01-24  
**الإصدار**: 1.0  
**الحالة**: مُطبق ومُختبر
