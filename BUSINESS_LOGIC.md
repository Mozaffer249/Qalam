# Business Logic - منطق الأعمال 📋

## نظرة عامة

مشروع **Qalam** هو منصة تعليمية شاملة مبنية على **Clean Architecture** مع تطبيق **CQRS Pattern** باستخدام **MediatR**. يهدف المشروع لربط المعلمين بالطلاب في مختلف المجالات التعليمية.

---

## البنية المعمارية (Architecture)

### 1. **Layers (الطبقات)**

```
┌─────────────────────────────────────────┐
│         Qalam.Api (Presentation)        │  ← Controllers, Middleware
├─────────────────────────────────────────┤
│      Qalam.Core (Application Layer)     │  ← Business Logic, Commands, Queries
├─────────────────────────────────────────┤
│      Qalam.Service (Domain Services)    │  ← Complex Business Services
├─────────────────────────────────────────┤
│   Qalam.Infrastructure (Persistence)    │  ← Database, Repositories, EF Core
├─────────────────────────────────────────┤
│       Qalam.Data (Domain Entities)      │  ← Entities, DTOs, Results
└─────────────────────────────────────────┘
```

### 2. **CQRS Pattern**

المشروع يفصل بين:
- **Commands** (الأوامر): العمليات التي تُغيِّر الحالة (Create, Update, Delete)
- **Queries** (الاستعلامات): العمليات التي تقرأ البيانات فقط (Read)

---

## المجالات التعليمية (Education Domains)

### الأنواع الأربعة الأساسية

#### 1. **School Education (تعليم مدرسي)**
```csharp
Code: "school"
HasCurriculum: true
```
- **الوصف:** التعليم الأكاديمي المدرسي بجميع مراحله
- **الخصائص:**
  - يحتوي على منهج محدد (Curriculum)
  - مرتبط بمستويات تعليمية (Education Levels)
  - مقسم إلى صفوف (Grades) وفصول دراسية (Academic Terms)
  - يحتوي على مواد دراسية (Subjects) ووحدات (Content Units)

**مثال:** رياضيات - الصف الثالث الابتدائي - المنهج السعودي - الفصل الدراسي الأول

#### 2. **Quran (قرآن كريم)**
```csharp
Code: "quran"
HasCurriculum: false
```
- **الوصف:** تعليم القرآن الكريم حفظاً وتلاوة وتجويداً
- **الخصائص:**
  - لا يحتوي على منهج ثابت
  - مستويات (QuranLevels): مبتدئ، متوسط، متقدم
  - أنواع محتوى (QuranContentTypes): حفظ، تلاوة، تجويد، تفسير
  - مقسم إلى أجزاء (QuranParts) وسور (QuranSurahs)

**مثال:** حفظ جزء عم - مستوى مبتدئ

#### 3. **Languages (لغات)**
```csharp
Code: "language"
HasCurriculum: false
```
- **الوصف:** تعليم اللغات الأجنبية والعربية
- **الخصائص:**
  - مرن - بدون منهج محدد
  - يمكن تخصيص المحتوى حسب احتياج الطالب

**مثال:** تعليم اللغة الإنجليزية للمحادثة

#### 4. **General Skills (مهارات عامة)**
```csharp
Code: "skills"
HasCurriculum: false
```
- **الوصف:** المهارات الحياتية والمهنية والتقنية
- **أمثلة:**
  - البرمجة
  - التصميم
  - التصوير الفوتوغرافي
  - الطبخ
  - الخياطة

---

## قواعد التعليم (Education Rules)

كل مجال تعليمي له قواعد خاصة تحكم سير العمل:

### مثال: قواعد تعليم القرآن

```csharp
{
    DomainId: 2, // Quran
    MinSessions: 1,
    MaxSessions: 100,
    DefaultSessionDurationMinutes: 45,
    AllowExtension: true,
    AllowFlexibleCourses: true,
    MaxGroupSize: 5,
    MinGroupSize: 1
}
```

### مثال: قواعد التعليم المدرسي

```csharp
{
    DomainId: 1, // School
    MinSessions: 4,
    MaxSessions: 40,
    DefaultSessionDurationMinutes: 60,
    AllowExtension: true,
    AllowFlexibleCourses: false, // المنهج ثابت
    MaxGroupSize: 10,
    MinGroupSize: 1
}
```

---

## User Authentication Flow (تدفق المصادقة)

### 1. التسجيل (Registration)

```mermaid
sequenceDiagram
    participant User
    participant API
    participant Handler
    participant UserManager
    participant Database
    participant MessagingAPI

    User->>API: POST /api/Authentication/Register
    API->>Handler: RegisterCommand
    Handler->>UserManager: Check if email exists
    UserManager-->>Handler: Email not found
    Handler->>UserManager: CreateAsync(user, password)
    UserManager->>Database: Insert User (IsActive=false)
    Database-->>UserManager: Success
    Handler->>Handler: Generate 4-digit OTP
    Handler->>Database: Store OTP (expires in 5 min)
    Handler->>MessagingAPI: Send confirmation email
    MessagingAPI-->>Handler: Email queued
    Handler-->>API: Success Response
    API-->>User: "Check your email for OTP"
```

#### كود مبسط للتسجيل:

```csharp
public async Task<Response<object>> Handle(RegisterCommand request)
{
    // 1. التحقق من عدم وجود البريد مسبقاً
    var emailExists = await _userManager.FindByEmailAsync(request.Email);
    if (emailExists != null)
        return BadRequest("Email already registered");

    // 2. إنشاء المستخدم (غير مفعّل)
    var user = new User 
    { 
        Email = request.Email, 
        FirstName = request.FirstName,
        IsActive = false  // لن يستطيع تسجيل الدخول قبل تأكيد البريد
    };
    
    await _userManager.CreateAsync(user, request.Password);

    // 3. توليد OTP من 4 أرقام
    var otpCode = Random.Next(1000, 9999).ToString();

    // 4. حفظ OTP في قاعدة البيانات (صلاحية 5 دقائق)
    await _context.EmailConfirmationOtps.AddAsync(new EmailConfirmationOtp 
    {
        UserId = user.Id,
        OtpCode = otpCode,
        ExpiresAt = DateTime.UtcNow.AddMinutes(5)
    });

    // 5. إرسال البريد الإلكتروني
    await SendConfirmationEmail(user.Email, otpCode);

    return Success("User registered. Please check your email.");
}
```

### 2. تأكيد البريد الإلكتروني (Email Confirmation)

```csharp
public async Task<string> ConfirmEmail(int userId, string code)
{
    // 1. البحث عن OTP
    var otp = await _context.EmailConfirmationOtps
        .FirstOrDefaultAsync(o => 
            o.UserId == userId && 
            o.OtpCode == code && 
            !o.IsUsed);

    if (otp == null)
        return "Invalid or expired code";

    // 2. التحقق من صلاحية الكود
    if (DateTime.UtcNow > otp.ExpiresAt)
        return "Code has expired";

    // 3. تفعيل المستخدم
    var user = await _userManager.FindByIdAsync(userId.ToString());
    user.EmailConfirmed = true;
    user.IsActive = true;

    // 4. تحديث حالة OTP
    otp.IsUsed = true;
    await _context.SaveChangesAsync();

    return "Email confirmed successfully!";
}
```

### 3. تسجيل الدخول (Login)

```mermaid
sequenceDiagram
    participant User
    participant API
    participant Handler
    participant RiskService
    participant UserManager
    participant AuthService
    participant SessionService

    User->>API: POST /api/Authentication/Login
    API->>Handler: LoginCommand
    Handler->>RiskService: AssessLoginRisk(IP)
    
    alt Risk Level Critical
        RiskService-->>Handler: Block IP
        Handler-->>User: "Access blocked - suspicious activity"
    else Risk OK
        Handler->>UserManager: FindByEmail/Username
        Handler->>UserManager: CheckPasswordSignInAsync
        
        alt Password Invalid
            UserManager-->>Handler: Failed
            Handler->>RiskService: RecordFailedAttempt
            Handler-->>User: "Invalid credentials"
        else Password Valid
            Handler->>AuthService: GenerateJWT(user)
            AuthService-->>Handler: JWT Token + RefreshToken
            Handler->>SessionService: CreateSession(device, IP)
            Handler-->>User: Success + Tokens
        end
    end
```

#### خصائص تسجيل الدخول الآمن:

```csharp
// 1. تقييم المخاطر (Risk Assessment)
var riskLevel = await _riskService.AssessLoginRisk(ipAddress, userId);

if (riskLevel == RiskLevel.Critical)
{
    await _riskService.BlockIP(ipAddress, minutes: 30);
    return "Access blocked due to suspicious activity";
}

// 2. التحقق من كلمة المرور مع Lockout
var signInResult = await _signInManager.CheckPasswordSignInAsync(
    user, 
    password, 
    lockoutOnFailure: true  // يقفل الحساب بعد 5 محاولات فاشلة
);

// 3. إنشاء JWT Token
var jwtResult = await _authService.GetJWTToken(user);

// 4. إنشاء جلسة (Session Management)
await _sessionService.CreateSession(
    userId: user.Id,
    deviceId: "device-123",
    deviceName: "iPhone 13",
    ipAddress: "192.168.1.1",
    accessToken: jwtResult.AccessToken,
    refreshToken: jwtResult.RefreshToken
);

return jwtResult;
```

### 4. إدارة الجلسات (Session Management)

المنصة تدعم:
- **Multi-device Login**: المستخدم يستطيع الدخول من عدة أجهزة
- **Trusted Devices**: حفظ الأجهزة الموثوقة
- **Session Tracking**: تتبع جميع الجلسات النشطة

```csharp
// مثال: عرض جميع الأجهزة النشطة للمستخدم
var activeSessions = await _context.LoginSessions
    .Where(s => s.UserId == userId && s.IsActive)
    .Select(s => new 
    {
        s.DeviceName,
        s.IpAddress,
        s.Location,
        s.LastActivityAt,
        IsTrusted = s.IsTrusted
    })
    .ToListAsync();

// المستخدم يستطيع تسجيل الخروج من جهاز معين
await _sessionService.TerminateSession(sessionId);

// أو تسجيل الخروج من جميع الأجهزة
await _sessionService.TerminateAllSessions(userId);
```

---

## Password Security (أمان كلمات المرور)

### 1. سياسة كلمات المرور

```json
{
  "PasswordPolicy": {
    "MinimumLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialChar": true,
    "PreventPasswordReuse": 5,
    "PasswordExpiryDays": 90
  }
}
```

### 2. فحص قوة كلمة المرور

```csharp
public class PasswordStrength
{
    public int Score { get; set; } // 0-4
    public List<string> Feedback { get; set; }
}

var strength = await _passwordSecurityService.CheckPasswordStrength("MyP@ssw0rd");

// النتيجة:
{
    Score: 4,  // قوية جداً
    Feedback: []
}

// كلمة مرور ضعيفة:
var weakStrength = await _passwordSecurityService.CheckPasswordStrength("12345");

{
    Score: 0,
    Feedback: [
        "Password is too short",
        "This is a commonly used password",
        "Add uppercase letters",
        "Add special characters"
    ]
}
```

### 3. منع إعادة استخدام كلمات المرور

```csharp
// عند تغيير كلمة المرور
public async Task<string> ChangePassword(ChangePasswordCommand request)
{
    var user = await _userManager.GetUserAsync(request.UserId);

    // 1. التحقق من أن كلمة المرور الجديدة ليست ضمن آخر 5 كلمات
    var isInHistory = await _passwordSecurityService
        .IsPasswordInHistory(user.Id, request.NewPassword, count: 5);

    if (isInHistory)
        return "Cannot reuse recent passwords";

    // 2. تغيير كلمة المرور
    await _userManager.ChangePasswordAsync(user, 
        request.CurrentPassword, 
        request.NewPassword);

    // 3. حفظ كلمة المرور الجديدة في السجل
    await _passwordSecurityService.AddToPasswordHistory(
        user.Id, 
        user.PasswordHash
    );

    // 4. تحديث تاريخ التغيير
    user.PasswordChangedAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    return "Password changed successfully";
}
```

### 4. انتهاء صلاحية كلمة المرور

```csharp
// عند تسجيل الدخول
var isExpired = await _passwordSecurityService.IsPasswordExpired(user.Id);

if (isExpired)
{
    return new Response 
    {
        Success = false,
        Message = "Your password has expired. Please change it.",
        RequiresPasswordChange = true
    };
}
```

---

## Risk Assessment (تقييم المخاطر)

### مستويات الخطر

```csharp
public enum RiskLevel
{
    Low = 0,      // نشاط عادي
    Medium = 1,   // نشاط مشبوه قليلاً
    High = 2,     // نشاط مشبوه
    Critical = 3  // يجب حظر الوصول
}
```

### تقييم المخاطر عند تسجيل الدخول

```csharp
public class RiskAssessment
{
    public RiskLevel Level { get; set; }
    public int FailedAttemptsInWindow { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public List<string> Reasons { get; set; }
}

// مثال: تقييم محاولة تسجيل دخول
var risk = await _riskService.AssessLoginRisk("192.168.1.100", userId: 5);

// النتيجة:
{
    Level: RiskLevel.High,
    FailedAttemptsInWindow: 8,
    RequiresTwoFactor: true,
    Reasons: [
        "Multiple failed login attempts (8) in last 15 minutes",
        "Login from new location",
        "Login from new device"
    ]
}
```

### سيناريوهات المخاطر

#### **Low Risk** ✅
- تسجيل دخول من جهاز معروف
- IP معروف
- موقع جغرافي معتاد

#### **Medium Risk** ⚠️
- تسجيل دخول من جهاز جديد
- IP جديد لكن في نفس البلد
- 2-3 محاولات فاشلة

**الإجراء:** إرسال إشعار بريد إلكتروني

#### **High Risk** 🔴
- 5-10 محاولات فاشلة خلال 15 دقيقة
- تسجيل دخول من بلد جديد
- تغيير IP بشكل متكرر

**الإجراء:** طلب تأكيد بريد إلكتروني أو Two-Factor Authentication

#### **Critical Risk** 🚫
- أكثر من 10 محاولات فاشلة
- نمط هجوم Brute Force واضح
- IP مدرج في قائمة سوداء

**الإجراء:** حظر IP لمدة 30 دقيقة

---

## Rate Limiting (تحديد معدل الطلبات)

### الهدف
منع الهجمات التي تحاول استنزاف الخادم أو تخمين كلمات المرور.

### القواعد

```json
{
  "RateLimiting": {
    "Enabled": true,
    "WindowSeconds": 60,
    "MaxRequestsPerWindow": 100,
    "BlockDurationSeconds": 300
  }
}
```

### مثال: Rate Limiting Middleware

```csharp
public class RateLimitingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIP(context);
        var endpoint = context.Request.Path;

        // التحقق من عدد الطلبات
        var requestCount = await _rateLimitService.GetRequestCount(
            ipAddress, 
            endpoint, 
            windowSeconds: 60
        );

        if (requestCount > 100)
        {
            // حظر لمدة 5 دقائق
            await _rateLimitService.BlockIP(ipAddress, seconds: 300);
            
            context.Response.StatusCode = 429; // Too Many Requests
            await context.Response.WriteAsync(
                "Rate limit exceeded. Try again later."
            );
            return;
        }

        // تسجيل الطلب
        await _rateLimitService.RecordRequest(ipAddress, endpoint);

        await _next(context);
    }
}
```

---

## Audit Logging (سجل المراجعة)

### تتبع جميع العمليات الهامة

```csharp
public enum AuditAction
{
    Create,
    Update,
    Delete,
    Login,
    Logout,
    PasswordChange,
    ProfileUpdate,
    SecuritySettingChange
}

// عند أي عملية مهمة:
await _auditService.LogAsync(new AuditLog
{
    UserId = currentUserId,
    Action = AuditAction.PasswordChange,
    EntityType = "User",
    EntityId = userId.ToString(),
    IpAddress = "192.168.1.1",
    UserAgent = "Mozilla/5.0...",
    Details = "Password changed successfully",
    Timestamp = DateTime.UtcNow
});
```

### مثال: عرض سجل المراجعة

```csharp
// عرض آخر 50 عملية للمستخدم
var userAuditLog = await _context.AuditLogs
    .Where(a => a.UserId == userId)
    .OrderByDescending(a => a.Timestamp)
    .Take(50)
    .Select(a => new 
    {
        a.Action,
        a.EntityType,
        a.Details,
        a.IpAddress,
        a.Timestamp
    })
    .ToListAsync();

// النتيجة:
[
    {
        Action: "Login",
        EntityType: "User",
        Details: "Login from new device: iPhone",
        IpAddress: "192.168.1.100",
        Timestamp: "2026-01-11T10:30:00Z"
    },
    {
        Action: "ProfileUpdate",
        EntityType: "User",
        Details: "Updated phone number",
        IpAddress: "192.168.1.100",
        Timestamp: "2026-01-11T10:15:00Z"
    }
]
```

---

## Validation (التحقق من الصحة)

### FluentValidation

المشروع يستخدم **FluentValidation** للتحقق من صحة البيانات قبل تنفيذ الأوامر.

```csharp
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        // التحقق من الاسم الأول
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name too long");

        // التحقق من البريد الإلكتروني
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        // التحقق من كلمة المرور
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain digit")
            .Matches(@"[\W_]").WithMessage("Password must contain special character");

        // التحقق من تطابق كلمة المرور
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match");

        // التحقق من رقم الهاتف (اختياري)
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format");
    }
}
```

### ValidationBehavior

يتم تشغيل التحقق تلقائياً قبل تنفيذ أي Command:

```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, ...)
    {
        // 1. تشغيل جميع Validators
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        // 2. إذا كان هناك أخطاء، إرجاع Bad Request
        if (failures.Any())
        {
            var errors = failures.Select(f => f.ErrorMessage).ToList();
            return BadRequest<TResponse>(errors);
        }

        // 3. إذا لم تكن هناك أخطاء، متابعة التنفيذ
        return await next();
    }
}
```

---

## Localization (الدعم متعدد اللغات)

### الهدف
دعم اللغة العربية والإنجليزية في جميع الرسائل والمحتوى.

### الإعدادات

```csharp
// في Program.cs
builder.Services.AddLocalization(opt => { opt.ResourcesPath = ""; });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("en-US"),
        new CultureInfo("ar-EG")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
```

### ملفات الموارد (Resources)

```csharp
// AuthenticationResources.cs
public class AuthenticationResources { }

// AuthenticationResourcesKeys.cs
public static class AuthenticationResourcesKeys
{
    public const string UserNotFound = "UserNotFound";
    public const string EmailIsExist = "EmailIsExist";
    public const string PasswordNotCorrect = "PasswordNotCorrect";
    public const string UserRegisteredSuccessfully = "UserRegisteredSuccessfully";
    // ... more keys
}

// AuthenticationResources.ar-EG.resx
UserNotFound: "المستخدم غير موجود"
EmailIsExist: "البريد الإلكتروني موجود مسبقاً"

// AuthenticationResources.en-US.resx
UserNotFound: "User not found"
EmailIsExist: "Email already exists"
```

### الاستخدام

```csharp
public class RegisterCommandHandler
{
    private readonly IStringLocalizer<AuthenticationResources> _localizer;

    public async Task<Response<object>> Handle(RegisterCommand request)
    {
        if (emailExists)
        {
            // الرسالة ستكون بالعربية أو الإنجليزية حسب لغة المستخدم
            return BadRequest(_localizer[AuthenticationResourcesKeys.EmailIsExist]);
        }

        return Success(_localizer[AuthenticationResourcesKeys.UserRegisteredSuccessfully]);
    }
}
```

---

## Email Notifications (الإشعارات عبر البريد الإلكتروني)

### استراتيجيات الإرسال

```csharp
public enum EmailSendingStrategy
{
    Immediate = 1,  // إرسال فوري (Synchronous)
    Queued = 2      // إرسال عبر Queue (Asynchronous)
}
```

### مثال: إرسال بريد إلكتروني

```csharp
// إرسال فوري
await _emailService.SendEmailAsync(
    to: "user@example.com",
    subject: "Welcome to Qalam",
    body: htmlBody,
    isHtml: true,
    strategy: EmailSendingStrategy.Immediate
);

// إرسال عبر RabbitMQ Queue (مفضل للإنتاج)
await _emailService.SendEmailAsync(
    to: "user@example.com",
    subject: "Welcome to Qalam",
    body: htmlBody,
    isHtml: true,
    strategy: EmailSendingStrategy.Queued  // يُرسل إلى Queue
);
```

### أنواع الإشعارات

#### 1. **تأكيد البريد الإلكتروني**
```csharp
await _notificationService.SendEmailConfirmationAsync(user, otpCode);
```

#### 2. **تسجيل دخول من جهاز جديد**
```csharp
await _notificationService.NotifyNewDeviceLoginAsync(
    user: user,
    deviceName: "iPhone 13",
    ipAddress: "192.168.1.100"
);
```

#### 3. **نشاط مشبوه**
```csharp
await _notificationService.NotifySuspiciousActivityAsync(
    user: user,
    activity: "Multiple failed login attempts (8)",
    ipAddress: "192.168.1.100"
);
```

#### 4. **إعادة تعيين كلمة المرور**
```csharp
await _notificationService.SendPasswordResetCodeAsync(
    user: user,
    resetCode: "5678"
);
```

---

## Security Headers Middleware

### الهدف
إضافة رؤوس أمان HTTP لحماية التطبيق من الهجمات الشائعة.

```csharp
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // منع Clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // منع MIME-type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // تفعيل XSS Protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline';");

        // HSTS (تفعيل HTTPS فقط)
        context.Response.Headers.Add("Strict-Transport-Security", 
            "max-age=31536000; includeSubDomains");

        // منع تسريب Referrer
        context.Response.Headers.Add("Referrer-Policy", "no-referrer");

        await _next(context);
    }
}
```

---

## Middleware Pipeline (خط معالجة الطلبات)

### الترتيب مهم! ⚠️

```csharp
// 1. Security Headers (أولاً)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Error Handling (معالجة الأخطاء)
app.UseMiddleware<ErrorHandlerMiddleware>();

// 3. HTTPS Redirection
app.UseHttpsRedirection();

// 4. Routing
app.UseRouting();

// 5. CORS
app.UseCors("_cors");

// 6. Rate Limiting (قبل Authentication)
app.UseMiddleware<RateLimitingMiddleware>();

// 7. Authentication
app.UseAuthentication();

// 8. Authorization
app.UseAuthorization();

// 9. Audit Logging (بعد Authentication)
app.UseMiddleware<AuditLoggingMiddleware>();

// 10. Endpoints
app.MapControllers();
```

---

## أمثلة عملية كاملة

### مثال 1: تسجيل مستخدم جديد

```bash
# الطلب
POST /api/Authentication/Register
Content-Type: application/json

{
    "firstName": "أحمد",
    "lastName": "محمد",
    "email": "ahmed@example.com",
    "password": "MyP@ssw0rd123",
    "confirmPassword": "MyP@ssw0rd123",
    "phoneNumber": "+966501234567"
}

# الاستجابة (نجاح)
{
    "succeeded": true,
    "message": "User registered successfully",
    "data": {
        "message": "Please check your email for your confirmation code."
    }
}

# البريد الإلكتروني المُرسل
Subject: Confirm Your Email - Qalam
Body: Your confirmation code is: 8374
      This code will expire in 5 minutes.
```

### مثال 2: تأكيد البريد الإلكتروني

```bash
# الطلب
POST /api/Authentication/ConfirmEmail
Content-Type: application/json

{
    "userId": 5,
    "code": "8374"
}

# الاستجابة (نجاح)
{
    "succeeded": true,
    "message": "Email confirmed successfully",
    "data": "Email confirmed successfully!"
}
```

### مثال 3: تسجيل الدخول

```bash
# الطلب
POST /api/Authentication/Login
Content-Type: application/json

{
    "userNameOrEmail": "ahmed@example.com",
    "password": "MyP@ssw0rd123",
    "deviceId": "device-12345",
    "deviceName": "iPhone 13"
}

# الاستجابة (نجاح)
{
    "succeeded": true,
    "message": "Login successful",
    "data": {
        "userId": 5,
        "userName": "ahmed",
        "email": "ahmed@example.com",
        "fullName": "أحمد محمد",
        "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "refreshToken": {
            "tokenString": "a8f5f167f44f4964e6c998dee827110c",
            "expireAt": "2026-02-11T10:30:00Z"
        },
        "roles": ["Student"],
        "deviceId": "device-12345",
        "isNewDevice": false
    }
}
```

### مثال 4: تغيير كلمة المرور

```bash
# الطلب
POST /api/Authentication/ChangePassword
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
    "currentPassword": "MyP@ssw0rd123",
    "newPassword": "NewP@ssw0rd456",
    "confirmNewPassword": "NewP@ssw0rd456"
}

# الاستجابة (نجاح)
{
    "succeeded": true,
    "message": "Password changed successfully",
    "data": null
}
```

### مثال 5: إعادة تعيين كلمة المرور

```bash
# الخطوة 1: طلب كود إعادة التعيين
POST /api/Authentication/SendResetPasswordCode
Content-Type: application/json

{
    "email": "ahmed@example.com"
}

# الاستجابة
{
    "succeeded": true,
    "message": "Reset code sent to your email",
    "data": null
}

# البريد الإلكتروني المُرسل
Subject: Reset Your Password - Qalam
Body: Your password reset code is: 5678
      This code will expire in 5 minutes.

# الخطوة 2: تأكيد الكود وإعادة التعيين
POST /api/Authentication/ResetPassword
Content-Type: application/json

{
    "email": "ahmed@example.com",
    "code": "5678",
    "newPassword": "NewP@ssw0rd789"
}

# الاستجابة
{
    "succeeded": true,
    "message": "Password reset successfully",
    "data": null
}
```

---

## الخلاصة

### نقاط القوة 💪

1. **Clean Architecture** - فصل واضح بين الطبقات
2. **CQRS Pattern** - فصل بين القراءة والكتابة
3. **Security First** - أمان متقدم على كل المستويات
4. **Scalability** - قابل للتوسع بسهولة
5. **Maintainability** - سهل الصيانة والتطوير
6. **Multi-language** - دعم كامل للعربية والإنجليزية
7. **Audit Trail** - تتبع شامل لجميع العمليات

### التقنيات المستخدمة 🛠️

- ASP.NET Core 8.0
- Entity Framework Core
- MediatR (CQRS)
- FluentValidation
- Identity Framework
- JWT Authentication
- Serilog (Logging)
- RabbitMQ (Message Queue)
- Docker

---

## المصطلحات

| المصطلح | الترجمة | الشرح |
|---------|---------|--------|
| Domain | المجال | مجال تعليمي (مدرسي، قرآن، لغات، مهارات) |
| Entity | الكيان | جدول في قاعدة البيانات |
| Command | الأمر | عملية تُغيِّر حالة البيانات |
| Query | الاستعلام | عملية تقرأ البيانات فقط |
| Handler | المعالج | الكلاس الذي ينفذ Command أو Query |
| Validator | المُحقق | الكلاس الذي يتحقق من صحة البيانات |
| Repository | المستودع | الطبقة التي تتفاعل مع قاعدة البيانات |
| Middleware | البرمجية الوسيطة | كود يعمل بين الطلب والاستجابة |
| Audit Log | سجل المراجعة | تسجيل جميع العمليات المهمة |

---

*آخر تحديث: يناير 2026*
