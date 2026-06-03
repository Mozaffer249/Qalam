# دور الإدارة — السيناريو الثاني (طلب جلسات مفتوح)
## Admin Role Specification — Scenario 2

> **الغرض من هذا الملف:** توثيق شامل لكل ما يتعلق بلوحة الإدارة في السيناريو الثاني — الرحلة، الشاشات، المتطلبات، الـ APIs، الصلاحيات، التقارير. هذا الملف مرجع للـ Admin Panel Developer وللـ Backend Developer وللـ QA.

---

## 1. نظرة عامة على الدور

### 1.1 من هي الإدارة؟
الإدارة (Admin) هي **المشرف على المنصة**، تراقب الطلبات والعروض، تتدخل في حالات الخلاف، وتدير التقارير المالية.

### 1.2 الصلاحيات الأساسية
| الصلاحية | متاح؟ |
|----------|-------|
| رؤية كل الطلبات (نشطة، مكتملة، ملغية) | ✓ |
| رؤية كل العروض | ✓ |
| رؤية محادثات الشات (بحدود) | ✓ |
| إيقاف طلب / حساب | ✓ |
| فتح طلب منتهي | ✓ |
| استرداد المبالغ (Refund) | ✓ |
| تعديل بيانات الطلب في حالات استثنائية | ✓ |
| توليد التقارير المالية | ✓ |
| تصدير البيانات (Excel/CSV) | ✓ |
| تعديل قواعد المطابقة (Matching Rules) | ✓ |
| استثناء معلمين معينين | ✓ |
| الوصول للـ Audit Log | ✓ |

### 1.3 أدوار الإدارة الفرعية (Sub-roles)

| الدور | الصلاحيات |
|------|-----------|
| **Super Admin** | كل الصلاحيات + إدارة الـ Admins الآخرين |
| **Support Admin** | إدارة الطلبات والنزاعات، بدون صلاحيات مالية |
| **Finance Admin** | التقارير المالية، الاستردادات، التحويلات للمعلمين |
| **Operations Admin** | مراقبة العمليات والإحصائيات، بدون تدخل |

> **ملاحظة:** RBAC يُطبّق عبر `[Authorize(Roles = "...")]` و Policies في .NET.

---

## 2. رحلة الإدارة

```
① لوحة التحكم - نظرة عامة
    • إجمالي الطلبات النشطة
    • معدل التحويل (Conversion Rate)
    • الإيرادات
    • النزاعات المعلّقة
    │
    ▼
② إدارة الطلبات
    • عرض كل الطلبات
    • فلترة (حالة، مادة، تاريخ، طالب)
    • التدخل عند الحاجة
    │
    ▼
③ مراقبة العروض
    • العروض النشطة
    • معدل القبول لكل معلم
    • العروض المتأخرة في الرد
    │
    ▼
④ متابعة الجلسات
    • الجلسات المجدولة
    • نسبة الحضور
    • التقييمات
    │
    ▼
⑤ التقارير المالية
    • الإيرادات حسب السيناريو
    • العمولات
    • التحويلات للمعلمين
    │
    ▼
⑥ حل النزاعات
    • الشكاوى الواردة
    • مراجعة الشات
    • قرارات الاسترداد
```

---

## 3. المتطلبات الوظيفية (FR-A)

### FR-A-001: لوحة تحكم رئيسية
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **الوصف** | نظرة عامة على الطلبات والإحصائيات |
| **المؤشرات (KPIs)** | إجمالي الطلبات حسب الحالة، معدل التحويل (Request → Paid)، متوسط عدد العروض لكل طلب، متوسط وقت استجابة المعلم، الإيرادات |
| **التحديث** | Live updates عبر SignalR للمؤشرات الحساسة |

### FR-A-002: إدارة الطلبات
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **الوصف** | عرض وإدارة جميع الطلبات |
| **الإجراءات** | عرض التفاصيل — إيقاف طلب مخالف (Suspend) — تعديل إداري — إعادة فتح طلب منتهي — حذف نهائي (Soft delete) |

### FR-A-003: مراقبة العروض
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **التقارير** | معلمون نشطون — معدل القبول لكل معلم — متوسط السعر — العروض المتأخرة |

### FR-A-004: حل النزاعات
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **القدرات** | مراجعة محادثات الشات (بإذن الطرفين أو لأسباب قانونية موثقة) — استرداد المبالغ — إيقاف حسابات — إغلاق طلبات بالقوة |

### FR-A-005: التقارير المالية
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **التقارير** | الإيرادات اليومية/الشهرية/السنوية — العمولات — المدفوعات للمعلمين — الاستردادات |
| **التصدير** | Excel, CSV, PDF |

### FR-A-006: إدارة قواعد المطابقة
| الحقل | القيمة |
|------|--------|
| **الأولوية** | متوسطة |
| **القدرات** | تعديل معايير المطابقة (مثلاً تفعيل/تعطيل مطابقة Quran بالـ Level) — استثناء معلمين معينين من المطابقة — تعديل أولوية الإشعارات |

---

## 4. الشاشات (Admin Screens)

### Screen A-1: لوحة التحكم الرئيسية (Admin Dashboard)

**الهدف:** نظرة عامة سريعة على المؤشرات الحساسة.

**العناصر:**

**KPI Cards (الصف الأول):**
| البطاقة | المحتوى | المقارنة |
|---------|---------|-----------|
| الطلبات النشطة | 145 | ⬆ 12% عن الأسبوع الماضي |
| العروض النشطة | 432 | ⬆ 8% |
| معدل التحويل (Conversion) | 67% | ⬆ 3% |

**KPI Cards (الصف الثاني):**
| البطاقة | المحتوى | الإشارة |
|---------|---------|----------|
| الإيرادات (هذا الشهر) | 45,230 ج | — |
| الجلسات المجدولة | 689 | — |
| نزاعات معلّقة | 3 | ⚠️ يحتاج تدخل |

**Charts:**
1. **الطلبات حسب الحالة (شهرياً):** Bar chart عمودي
2. **المعلمون الأكثر نشاطاً:** Horizontal bar chart (Top 10)
3. **معدل وقت الاستجابة:** Line chart يومي/أسبوعي
4. **الإيرادات حسب المادة:** Pie chart

**Recent Activity Feed (Real-time):**
- "طلب جديد: SR-2026-0146 (منذ 5 د)"
- "عرض مقبول: OF-2026-0890 (منذ 12 د)"
- "نزاع جديد: NZ-2026-0023 (منذ ساعة)" ← clickable

**API Calls:**
- `GET /Api/V1/Admin/Dashboard/kpis`
- `GET /Api/V1/Admin/Dashboard/charts?type=requests-by-status&period=monthly`
- `GET /Api/V1/Admin/Dashboard/recent-activity?take=20`

---

### Screen A-2: إدارة الطلبات (Requests Management)

**الهدف:** قائمة شاملة بكل الطلبات مع إمكانية الفلترة والتدخل.

**العناصر:**

**Filters Bar:**
- الحالة (Multi-select dropdown)
- المادة (Search + Dropdown)
- التاريخ (Date range picker)
- الطالب (Search by name/email/phone)
- المعلم المشارك (إذا أرادت الإدارة فلترة عروض معلم محدد)

**Search Bar:**
- البحث برقم الطلب (`SR-2026-XXXX`) أو اسم الطالب

**Bulk Actions:**
- Checkbox للتحديد المتعدد
- Dropdown إجراءات جماعية: تصدير المحدد، إيقاف المحدد، إعادة تعيين للحالة الافتراضية

**Table Columns:**
| العمود | المحتوى |
|--------|---------|
| ☐ | Checkbox |
| رقم | `SR-2026-0145` |
| الطالب | اسم + بريد + هاتف (مختصر) |
| المادة | اسم المادة + المرحلة |
| عدد الجلسات | رقم |
| الحالة | شارة ملونة |
| تاريخ الإنشاء | تاريخ + وقت نسبي |
| عدد العروض | رقم |
| الإجراءات | [👁️] [✏️] [⊘] |

**Pagination:** Server-side، 25 صف/صفحة افتراضياً.

**Export:**
- زر `📥 تصدير` يصدّر النتائج المُفلترة لـ Excel/CSV/PDF.

**API Calls:**
- `GET /Api/V1/Admin/SessionRequests?status=...&page=1&pageSize=25`
- `POST /Api/V1/Admin/SessionRequests/export?format=xlsx`

---

### Screen A-3: تفاصيل الطلب للإدارة (Admin Request Details)

**الهدف:** عرض كامل لكل بيانات الطلب + قدرات تدخل إدارية.

**الأقسام:**

**1. Header:**
- رقم الطلب
- الحالة (مع إمكانية التغيير اليدوي)
- معلومات سريعة (الطالب، المادة، عدد الجلسات، السعر إن وُجد)
- زر `Audit Log` (يفتح Modal بسجل التغييرات)

**2. Tabs:**
- **التفاصيل** — كل بيانات الطلب (مثل ما يراها الطالب)
- **العروض** — جدول بكل العروض المقدّمة (السعر، حالة، تاريخ التقديم، المعلم)
- **المحادثات** — قائمة المحادثات الموجودة + إمكانية فتحها بإذن
- **التايملاين** — تسلسل زمني لكل الأحداث
- **سجل التدقيق (Audit)** — مَن غيّر ماذا ومتى ولماذا

**3. Admin Actions Panel (Sidebar):**
- `⏸️ إيقاف الطلب` (مع حقل للسبب)
- `🔄 إعادة فتح` (إذا كان منتهياً أو ملغياً)
- `✏️ تعديل البيانات` (لحالات استثنائية، يتطلب تأكيد)
- `💰 استرداد` (إذا تم الدفع)
- `🗑️ حذف نهائي` (Soft delete، Super Admin فقط)

**4. Timeline Section:**
```
• 17 مايو 10:30 ص — تم إنشاء الطلب
• 17 مايو 10:31 ص — النظام أرسل لـ 23 معلم
• 17 مايو 11:45 ص — أول عرض من د. أحمد
• 17 مايو 2:15 م — عرض من أ. فاطمة
• 18 مايو 9:00 ص — رسالة من الطالب: "هل ممكن خصم؟"
• ...
```

**5. Audit Log:**
كل تغيير على الطلب يُسجَّل بـ:
- المستخدم (Admin أو System أو الطالب)
- التاريخ والوقت
- الحقل المتغيّر
- القيمة قبل/بعد
- السبب (إذا تغيير إداري)

**API Calls:**
- `GET /Api/V1/Admin/SessionRequests/{id}`
- `GET /Api/V1/Admin/SessionRequests/{id}/timeline`
- `GET /Api/V1/Admin/SessionRequests/{id}/audit-log`
- `POST /Api/V1/Admin/SessionRequests/{id}/suspend`
- `POST /Api/V1/Admin/SessionRequests/{id}/reactivate`
- `PUT /Api/V1/Admin/SessionRequests/{id}/admin-edit`

---

### Screen A-4: إدارة العروض (Offers Management)

**الهدف:** قائمة بكل العروض، الفلترة، والإجراءات.

**Filters:**
- الحالة (Pending, Accepted, Rejected, AutoRejected, Withdrawn, Expired)
- المعلم
- المادة
- نطاق السعر
- التاريخ

**Table Columns:**
- رقم العرض (`OF-2026-XXXX`)
- رقم الطلب (link)
- المعلم
- الطالب
- السعر
- عدد الجلسات
- الحالة
- تاريخ التقديم
- مدة الصلاحية المتبقية
- إجراءات

**Special View — "Negotiation Watch":**
- فلتر افتراضي على العروض ذات النشاط الكثيف في الشات (أكثر من 10 رسائل خلال 24 ساعة) لمراقبة احتمالية النزاعات.

**API Calls:**
- `GET /Api/V1/Admin/Offers?status=...&teacherId=...&page=1`

---

### Screen A-5: حل النزاعات (Disputes Resolution)

**الهدف:** عرض الشكاوى وإدارتها.

**Tabs:**
- **معلّقة (Open)** — تحتاج إجراء
- **قيد المعالجة (In Progress)** — Admin يعمل عليها
- **محلولة (Resolved)** — مكتملة
- **مرفوضة (Rejected)** — لا تستحق إجراء

**Card لكل نزاع:**
- رقم النزاع: `DS-2026-0023`
- نوع النزاع (Payment, Quality, Cancellation, Other)
- الطرف المُشتكي (الطالب / المعلم)
- الطرف الآخر
- الطلب أو العرض المرتبط
- وصف الشكوى (مختصر، 200 حرف)
- التاريخ
- الأولوية (Auto-detected: مرتبط بدفعة كبيرة، جلسة قريبة، شكاوى متكررة)

**Detail Screen لكل نزاع:**
- معلومات كاملة
- إمكانية فتح الشات المرتبط بإذن (مع تسجيل السبب)
- أزرار الإجراءات:
  - `استرداد كامل`
  - `استرداد جزئي` (مع تحديد النسبة)
  - `رفض الشكوى`
  - `إيقاف حساب الطرف الآخر`
  - `طلب معلومات إضافية`
- مربع نص للقرار + سبب

**API Calls:**
- `GET /Api/V1/Admin/Disputes?status=open&priority=high`
- `GET /Api/V1/Admin/Disputes/{id}`
- `POST /Api/V1/Admin/Disputes/{id}/resolve`
- `POST /Api/V1/Admin/Disputes/{id}/access-chat` (يطلب موافقة قانونية)

---

### Screen A-6: التقارير المالية (Financial Reports)

**الهدف:** تقارير شاملة عن الإيرادات والعمولات والمدفوعات.

**Filters:**
- النطاق الزمني (يومي / أسبوعي / شهري / سنوي / مخصص)
- السيناريو (الأول / الثاني / كلاهما)
- المادة
- المعلم

**Reports:**

**1. تقرير الإيرادات:**
- إجمالي الإيرادات
- العمولات المحتفظ بها
- المدفوع للمعلمين
- الاستردادات
- Net Revenue

**2. تقرير المعلمين:**
- لكل معلم: عدد العروض، نسبة القبول، إجمالي الإيرادات، العمولة المُحصَّلة، المبلغ المستحق

**3. تقرير الطلاب:**
- الطلاب الأكثر إنفاقاً
- معدل العودة (Repeat customers)

**4. تقرير العمليات (Operations):**
- الطلبات/اليوم
- العروض/الطلب
- متوسط وقت من النشر إلى أول عرض
- متوسط وقت من القبول إلى الدفع

**Export:**
- Excel (مع Pivot tables)
- CSV
- PDF (مع رسوم بيانية)

**API Calls:**
- `GET /Api/V1/Admin/Reports/revenue?from=...&to=...&groupBy=month`
- `GET /Api/V1/Admin/Reports/teachers?metric=acceptance-rate&limit=50`
- `POST /Api/V1/Admin/Reports/export?type=revenue&format=xlsx`

---

### Screen A-7: إعدادات قواعد المطابقة (Matching Rules)

**الهدف:** التحكم في خوارزمية المطابقة.

**أقسام:**

**1. القواعد العامة:**
- ✓/✗ مطابقة بـ Subject (إجباري)
- ✓/✗ مطابقة بـ Units (للمعلمين الذين لا يدرّسون كامل المادة)
- ✓/✗ مطابقة Quran بـ Content Type + Level
- ✓/✗ استبعاد المعلمين الذين تجاوزوا N عرض معلّق

**2. عوامل الترتيب (Ranking, v2):**
- وزن التقييم
- وزن سرعة الاستجابة
- وزن نسبة القبول
- وزن السعر التنافسي

**3. الاستثناءات:**
- قائمة معلمين مستثنيين من المطابقة (مع السبب وتاريخ الانتهاء)

**4. حدود الإشعارات:**
- الحد الأقصى لعدد المعلمين الذين يستقبلون كل طلب
- الحد الأقصى لعدد الطلبات في إشعار واحد لكل معلم

**API Calls:**
- `GET /Api/V1/Admin/MatchingRules`
- `PUT /Api/V1/Admin/MatchingRules`
- `POST /Api/V1/Admin/MatchingRules/exclude-teacher`

---

## 5. API Endpoints الكاملة

> **Base URL:** `/Api/V1/Admin`  
> **Authentication:** Bearer JWT (role = Admin أو SuperAdmin أو Finance أو Operations)  
> **Audit:** كل endpoint مع `POST/PUT/DELETE` يُسجَّل في Audit Log تلقائياً  

### 5.1 Dashboard

#### `GET /Dashboard/kpis`
**Query:** `?period=today` / `week` / `month` / `year` / `custom&from=&to=`

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "activeRequests": { "value": 145, "deltaPercent": 12, "deltaDirection": "up" },
    "activeOffers": { "value": 432, "deltaPercent": 8, "deltaDirection": "up" },
    "conversionRate": { "value": 0.67, "deltaPercent": 3, "deltaDirection": "up" },
    "revenue": { "value": 45230, "currency": "EGP", "deltaPercent": null },
    "scheduledSessions": { "value": 689 },
    "openDisputes": { "value": 3, "severity": "warning" }
  }
}
```

#### `GET /Dashboard/charts/{chartId}`
**Charts المتاحة:**
- `requests-by-status`
- `top-teachers`
- `response-time-trend`
- `revenue-by-subject`

**Query:** `?period=monthly&top=10`

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "chartId": "requests-by-status",
    "type": "bar",
    "labels": ["Draft", "Active", "Completed", "Cancelled"],
    "datasets": [
      { "label": "هذا الشهر", "values": [12, 145, 87, 23] },
      { "label": "الشهر الماضي", "values": [8, 130, 75, 19] }
    ]
  }
}
```

#### `GET /Dashboard/recent-activity`
**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "type": "request_created",
        "title": "طلب جديد: SR-2026-0146",
        "linkUrl": "/admin/requests/...",
        "occurredAt": "2026-05-17T15:00:00Z"
      },
      {
        "id": "guid",
        "type": "dispute_opened",
        "title": "نزاع جديد: DS-2026-0023",
        "severity": "warning",
        "linkUrl": "/admin/disputes/..."
      }
    ]
  }
}
```

---

### 5.2 إدارة الطلبات

#### `GET /SessionRequests`
**Query Parameters:**
| المعامل | النوع | الوصف |
|---------|------|--------|
| `status` | enum[] | فلترة حسب الحالة (multi) |
| `subjectId` | int | المادة |
| `studentId` | guid | الطالب |
| `teacherId` | guid | المعلم (يبحث في العروض) |
| `dateFrom`, `dateTo` | date | نطاق |
| `search` | string | بحث عام |
| `page`, `pageSize` | int | Pagination |
| `sortBy` | enum | `newest`, `oldest`, `mostOffers`, `expiringSoon` |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "requestNumber": "SR-2026-0145",
        "student": {
          "id": "guid",
          "name": "سارة محمد",
          "email": "sa***@example.com",
          "phone": "+201******123"
        },
        "subject": { "nameAr": "الرياضيات" },
        "level": { "nameAr": "الصف الثالث الثانوي" },
        "sessionsCount": 5,
        "status": "Active",
        "createdAt": "2026-05-17T10:30:00Z",
        "publishedAt": "2026-05-17T10:31:00Z",
        "expiresAt": "2026-05-24T00:00:00Z",
        "offersCount": 4,
        "lastActivityAt": "2026-05-17T14:30:00Z"
      }
    ],
    "pagination": { "page": 1, "pageSize": 25, "totalCount": 234, "totalPages": 10 }
  }
}
```

#### `GET /SessionRequests/{id}`
**Response 200:** يحتوي كل تفاصيل الطلب + العروض المرتبطة + المحادثات (Metadata فقط، بدون محتوى الرسائل).

```json
{
  "succeeded": true,
  "data": {
    "id": "guid",
    "requestNumber": "SR-2026-0145",
    "status": "Active",
    "student": { ... },
    "content": { ... },
    "generalSettings": { ... },
    "sessions": [...],
    "invitations": [...],
    "targets": [
      { "teacherId": "guid", "teacherName": "د. أحمد", "matchedAt": "...", "viewedAt": "...", "status": "Offered" }
    ],
    "offers": [
      {
        "id": "guid",
        "offerNumber": "OF-2026-0892",
        "teacher": { "id": "guid", "name": "د. أحمد", "rating": 4.8 },
        "totalPrice": 600,
        "status": "Pending",
        "version": 1,
        "createdAt": "...",
        "expiresAt": "..."
      }
    ],
    "conversations": [
      { "id": "guid", "offerId": "guid", "messageCount": 12, "lastMessageAt": "..." }
    ],
    "audit": {
      "createdBy": "guid",
      "createdAt": "...",
      "lastModifiedBy": "guid",
      "lastModifiedAt": "..."
    }
  }
}
```

#### `GET /SessionRequests/{id}/timeline`
**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "events": [
      {
        "id": "guid",
        "type": "RequestCreated",
        "occurredAt": "2026-05-17T10:30:00Z",
        "actor": { "id": "guid", "name": "سارة محمد", "role": "Student" },
        "description": "تم إنشاء الطلب",
        "metadata": {}
      },
      {
        "id": "guid",
        "type": "RequestPublished",
        "occurredAt": "2026-05-17T10:31:00Z",
        "actor": { "name": "System", "role": "System" },
        "description": "النظام أرسل لـ 23 معلم"
      },
      {
        "id": "guid",
        "type": "OfferSubmitted",
        "occurredAt": "2026-05-17T11:45:00Z",
        "actor": { "name": "د. أحمد", "role": "Teacher" },
        "description": "تقدّم بعرض 600 ج",
        "metadata": { "offerId": "guid", "price": 600 }
      }
    ]
  }
}
```

#### `GET /SessionRequests/{id}/audit-log`
**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "occurredAt": "...",
        "actor": { "id": "guid", "name": "أحمد المدير", "role": "Admin" },
        "action": "Status changed",
        "field": "Status",
        "oldValue": "Active",
        "newValue": "Suspended",
        "reason": "شكوى من طالب بسبب محتوى غير مناسب"
      }
    ]
  }
}
```

#### `POST /SessionRequests/{id}/suspend`
**Request Body:**
```json
{
  "reason": "محتوى غير مناسب",
  "notifyStudent": true,
  "notifyTeachers": true
}
```

**Response 200:**
```json
{ "succeeded": true, "message": "تم إيقاف الطلب" }
```

**Side Effects:**
- Status → `Suspended`
- إيقاف كل العروض المعلّقة على هذا الطلب → AutoRejected
- إشعار للأطراف
- Audit log entry

#### `POST /SessionRequests/{id}/reactivate`
**Request Body:**
```json
{ "reason": "تم حل المشكلة بعد التحقق" }
```

**Response 200:**
```json
{ "succeeded": true, "message": "تم إعادة فتح الطلب" }
```

#### `PUT /SessionRequests/{id}/admin-edit`
**الوصف:** تعديل بيانات الطلب في حالات استثنائية. يجب تبرير كل تغيير.

**Request Body:**
```json
{
  "changes": {
    "expiresAt": "2026-06-01T00:00:00Z"
  },
  "reason": "تمديد بناءً على طلب الطالب بسبب ظروف صحية"
}
```

**Response 200:**
```json
{ "succeeded": true, "message": "تم التعديل", "auditId": "guid" }
```

#### `DELETE /SessionRequests/{id}`
**الوصف:** حذف نهائي (Soft delete). **Super Admin فقط.**

**Request Body:**
```json
{ "reason": "ضمن خطة GDPR — حذف بناءً على طلب المستخدم" }
```

**Response 200:**
```json
{ "succeeded": true, "message": "تم الحذف" }
```

---

### 5.3 إدارة العروض

#### `GET /Offers`
**Query Parameters:** كالعادة + `teacherId`, `studentId`, `priceMin`, `priceMax`

**Response 200:** (مشابه لقائمة الطلبات لكن بحقول العروض)

#### `GET /Offers/{id}`
**Response 200:** تفاصيل كاملة + سجل التعديلات (Versions) + Conversation metadata.

#### `POST /Offers/{id}/force-withdraw`
**الوصف:** الإدارة تسحب عرضاً (مثلاً لمخالفة).

**Request Body:**
```json
{
  "reason": "تسعير مخالف لسياسة المنصة",
  "notifyTeacher": true,
  "notifyStudent": false
}
```

---

### 5.4 إدارة النزاعات

#### `GET /Disputes`
**Query Parameters:**
| المعامل | القيم |
|---------|------|
| `status` | `open` / `inProgress` / `resolved` / `rejected` |
| `type` | `payment` / `quality` / `cancellation` / `behavior` / `other` |
| `priority` | `low` / `medium` / `high` |
| `assignedTo` | guid (Admin ID) |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "disputeNumber": "DS-2026-0023",
        "type": "Quality",
        "priority": "High",
        "complainant": { "id": "guid", "name": "سارة محمد", "role": "Student" },
        "respondent": { "id": "guid", "name": "د. أحمد", "role": "Teacher" },
        "relatedTo": { "type": "Offer", "id": "guid", "number": "OF-2026-0892" },
        "summary": "المعلم لم يلتزم بالموعد ولم يرد",
        "createdAt": "2026-05-18T09:00:00Z",
        "status": "Open",
        "assignedTo": null
      }
    ],
    "pagination": {...}
  }
}
```

#### `GET /Disputes/{id}`
**Response 200:** تفاصيل كاملة + Timeline للنزاع + Evidence (مرفقات/صور).

#### `POST /Disputes/{id}/assign`
**Request Body:**
```json
{ "adminId": "guid" }
```

#### `POST /Disputes/{id}/access-chat`
**الوصف:** طلب الوصول لمحادثة الشات المرتبطة. **يتطلب تبريراً قانونياً.**

**Request Body:**
```json
{
  "legalReason": "شكوى رسمية مع إذن استخدام البيانات (موافقة الطرفين موثقة)",
  "auditNotes": "الإشارة لرقم البلاغ #1234"
}
```

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "accessToken": "...",
    "expiresAt": "2026-05-18T11:00:00Z",
    "conversationId": "guid"
  }
}
```

**ملاحظة:** الوصول مؤقت (ساعة واحدة)، ويُسجَّل في Audit Log الخاص بالنزاع.

#### `POST /Disputes/{id}/resolve`
**Request Body:**
```json
{
  "decision": "FullRefund",
  "decisionDetails": "تم استرداد كامل المبلغ بعد التحقق من عدم التزام المعلم",
  "refundAmount": 600,
  "currency": "EGP",
  "actions": [
    { "type": "RefundStudent", "amount": 600 },
    { "type": "PenalizeTeacher", "severity": "Warning" }
  ],
  "notifyParties": true
}
```

**خيارات `decision`:**
- `FullRefund`
- `PartialRefund`
- `NoRefund`
- `WarnTeacher`
- `SuspendTeacher`
- `SuspendStudent`
- `Reject`

**Response 200:**
```json
{
  "succeeded": true,
  "message": "تم حل النزاع",
  "data": {
    "resolvedAt": "...",
    "refundReference": "REF-2026-0034"
  }
}
```

---

### 5.5 التقارير المالية

#### `GET /Reports/revenue`
**Query Parameters:**
| المعامل | الوصف |
|---------|--------|
| `from`, `to` | نطاق |
| `groupBy` | `day` / `week` / `month` / `year` |
| `scenario` | `all` / `1` / `2` |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "period": { "from": "2026-05-01", "to": "2026-05-31" },
    "summary": {
      "grossRevenue": 234500,
      "commissions": 35175,
      "teacherPayouts": 199325,
      "refunds": 2400,
      "netRevenue": 32775,
      "currency": "EGP"
    },
    "breakdown": [
      { "period": "2026-05-01", "revenue": 8200, "commission": 1230 },
      { "period": "2026-05-02", "revenue": 7950, "commission": 1192 }
    ]
  }
}
```

#### `GET /Reports/teachers`
**Query Parameters:**
- `metric` = `revenue` / `acceptance-rate` / `response-time` / `sessions-count`
- `top` = N
- `from`, `to`

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "teacherId": "guid",
        "teacherName": "د. أحمد",
        "metric": "revenue",
        "value": 12450,
        "rank": 1,
        "offers": { "submitted": 45, "accepted": 28, "rate": 0.62 },
        "sessions": { "total": 87, "attended": 85 }
      }
    ]
  }
}
```

#### `POST /Reports/export`
**Request Body:**
```json
{
  "reportType": "revenue",
  "format": "xlsx",
  "filters": { "from": "...", "to": "..." }
}
```

**Response 200:** يُرجع رابط للتحميل (إن كان التقرير كبيراً يُولَّد بشكل غير متزامن).
```json
{
  "succeeded": true,
  "data": {
    "downloadUrl": "/api/v1/admin/reports/downloads/abc123.xlsx",
    "expiresAt": "2026-05-17T16:00:00Z"
  }
}
```

---

### 5.6 قواعد المطابقة

#### `GET /MatchingRules`
**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "rules": {
      "matchBySubject": { "enabled": true, "weight": 1.0 },
      "matchByUnits": { "enabled": true, "weight": 0.8 },
      "matchByQuranContentTypeAndLevel": { "enabled": true },
      "excludeTeachersWithPendingOffersAbove": { "enabled": true, "threshold": 10 }
    },
    "ranking": {
      "enabled": false,
      "weights": {
        "rating": 0.4,
        "responseTime": 0.3,
        "acceptanceRate": 0.2,
        "priceCompetitiveness": 0.1
      }
    },
    "notificationLimits": {
      "maxTeachersPerRequest": 50,
      "maxRequestsPerTeacherPerDay": 20
    },
    "exclusions": [
      {
        "teacherId": "guid",
        "teacherName": "د. خالد",
        "reason": "شكوى مفتوحة بحقه",
        "excludedAt": "2026-05-10T...",
        "expiresAt": "2026-06-10T..."
      }
    ]
  }
}
```

#### `PUT /MatchingRules`
**Request Body:** نفس بنية الـ Response.

#### `POST /MatchingRules/exclude-teacher`
**Request Body:**
```json
{
  "teacherId": "guid",
  "reason": "تحت التحقيق",
  "expiresAt": "2026-06-10T00:00:00Z"
}
```

#### `DELETE /MatchingRules/exclude-teacher/{teacherId}`
**الوصف:** إزالة معلم من قائمة الاستثناءات.

---

### 5.7 إدارة المستخدمين (Side actions)

#### `POST /Users/{userId}/suspend`
**Request Body:**
```json
{
  "reason": "مخالفة السياسات",
  "durationDays": 30,
  "notifyUser": true
}
```

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "suspensionId": "guid",
    "suspendedUntil": "2026-06-17T..."
  }
}
```

**Side Effects (إذا المستخدم Teacher):**
- إيقاف كل عروضه المعلّقة → AutoRejected
- إزالته من Matching pool
- إشعار

**Side Effects (إذا المستخدم Student):**
- إيقاف كل طلباته النشطة
- منع إنشاء طلبات جديدة

---

## 6. SignalR Hubs المتعلقة بالإدارة

### `AdminHub` (`/hubs/admin`)
**الاشتراك:** يجب أن يكون المستخدم Admin مع JWT.

**الأحداث المُستقبَلة:**

| الحدث | الـ Payload | متى يحدث |
|------|--------------|-----------|
| `NewRequest` | `{ requestId, requestNumber, studentName, subject }` | عند نشر طلب جديد |
| `NewOffer` | `{ offerId, requestId, teacherName, price }` | عند تقديم عرض |
| `NewDispute` | `{ disputeId, type, priority }` | عند فتح نزاع |
| `KpiUpdate` | `{ kpiId, newValue, deltaPercent }` | تحديث المؤشرات الحساسة |
| `HighValueTransaction` | `{ amount, requestId }` | معاملة فوق حد معين |

---

## 7. الإشعارات للإدارة

| الحدث | Email | In-App | SMS |
|------|-------|--------|-----|
| نزاع جديد بأولوية High | ✓ | ✓ | إذا حرج |
| طلب فوق X جنيه | ✗ | ✓ | ✗ |
| فشل دفع متكرر (5 محاولات) | ✓ | ✓ | ✗ |
| معدل التحويل ينخفض تحت X% | ✓ | ✓ | ✗ |
| Backend error rate > 5% | ✓ | ✓ | ✓ |
| Database storage > 80% | ✓ | ✓ | ✓ |

---

## 8. Audit Log — المعايير

### كل العمليات التي يجب تسجيلها:
- كل `POST`, `PUT`, `DELETE`, `PATCH` من الإدارة
- الوصول للمحادثات (`access-chat`)
- توليد التقارير
- تصدير البيانات
- التعديل في قواعد المطابقة
- إيقاف/تفعيل المستخدمين

### بنية سجل الـ Audit:
```json
{
  "id": "guid",
  "actor": {
    "userId": "guid",
    "userName": "أحمد المدير",
    "role": "Admin",
    "ipAddress": "...",
    "userAgent": "..."
  },
  "action": "SuspendRequest",
  "resource": { "type": "SessionRequest", "id": "guid" },
  "occurredAt": "2026-05-17T15:30:00Z",
  "changes": [
    { "field": "Status", "oldValue": "Active", "newValue": "Suspended" }
  ],
  "reason": "...",
  "metadata": {}
}
```

### استعلام Audit Log
- `GET /Api/V1/Admin/AuditLog?actorId=&actionType=&resourceType=&from=&to=`

---

## 9. الصلاحيات الموثقة (RBAC Matrix)

| العملية | Super | Admin | Support | Finance | Operations |
|---------|-------|-------|---------|---------|------------|
| عرض Dashboard | ✓ | ✓ | ✓ | ✓ | ✓ |
| عرض الطلبات | ✓ | ✓ | ✓ | ✓ | ✓ |
| إيقاف طلب | ✓ | ✓ | ✓ | ✗ | ✗ |
| تعديل طلب إدارياً | ✓ | ✓ | ✗ | ✗ | ✗ |
| حذف نهائي | ✓ | ✗ | ✗ | ✗ | ✗ |
| حل نزاع | ✓ | ✓ | ✓ | ✗ | ✗ |
| استرداد المبلغ | ✓ | ✓ | ✗ | ✓ | ✗ |
| التقارير المالية | ✓ | ✓ | ✗ | ✓ | ✓ (Read only) |
| إيقاف مستخدم | ✓ | ✓ | ✓ | ✗ | ✗ |
| تعديل قواعد المطابقة | ✓ | ✓ | ✗ | ✗ | ✗ |
| الوصول للشات | ✓ | ✓ | ✓ (تبرير) | ✗ | ✗ |
| إدارة Admins آخرين | ✓ | ✗ | ✗ | ✗ | ✗ |

---

## 10. حالات الخطأ المحتملة

| الكود | المعنى | HTTP Status |
|------|---------|-------------|
| `INSUFFICIENT_PERMISSIONS` | الـ Admin ليس له صلاحية للعملية | 403 |
| `REQUIRES_REASON` | العملية تتطلب تبريراً نصياً | 400 |
| `REQUIRES_LEGAL_JUSTIFICATION` | الوصول لمحادثة يتطلب تبريراً قانونياً | 400 |
| `RESOURCE_NOT_FOUND` | الكيان غير موجود | 404 |
| `CONFLICTING_STATE` | لا يمكن تنفيذ العملية في الحالة الحالية | 409 |
| `REFUND_ALREADY_PROCESSED` | الاسترداد تم بالفعل | 409 |
| `EXPORT_TOO_LARGE` | التصدير ضخم، سيتم توليده غير متزامناً | 202 |

---

## 11. متطلبات غير وظيفية للإدارة

### 11.1 الأداء
- لوحة التحكم يجب أن تُحمَّل في < 2 ثانية
- التقارير المعقدة (سنوية) → توليد غير متزامن مع notification عند الجاهزية
- البحث في الطلبات يجب أن يدعم 100,000+ طلب بدون بطء

### 11.2 الأمان
- 2FA إجباري لكل حسابات Admin
- Session timeout بعد 30 دقيقة من الخمول
- IP whitelist (اختياري لكل حساب)
- كل العمليات على HTTPS فقط
- Sensitive operations (Refund, Delete, Suspend) تتطلب password re-entry

### 11.3 التدقيق (Auditability)
- سجل غير قابل للحذف (Immutable Audit Log)
- يُحتفظ به 7 سنوات على الأقل
- يمكن تصديره للجهات الرقابية

### 11.4 الاحتفاظ بالبيانات (Retention)
- الطلبات المُلغاة/المنتهية: تُحفظ نشطة سنة، ثم Archive
- المحادثات: تُحفظ سنتين، ثم تُحذف (Soft delete)
- السجلات المالية: 7 سنوات (متطلب قانوني)

---

## 12. ملحقات

### 12.1 ترقيم النزاعات
- النمط: `DS-{Year}-{SequentialNumber}` مثل `DS-2026-0023`

### 12.2 ترقيم الاستردادات
- النمط: `REF-{Year}-{SequentialNumber}` مثل `REF-2026-0034`

### 12.3 الأوقات الحرجة
- استجابة الـ Admin لنزاع High priority: خلال ساعة عمل
- معالجة الاسترداد: 3-5 أيام عمل (يعتمد على بوابة الدفع)
- توليد التقارير الكبيرة: حتى 10 دقائق (للتقارير السنوية)

### 12.4 التكامل مع أنظمة خارجية
- بوابة الدفع: Moyasar / HyperPay (للاسترداد)
- نظام الإشعارات: FCM / APNS / Email Service
- نظام التخزين: Azure Blob / S3 (للمرفقات والتقارير المُصدَّرة)
- نظام المراقبة: Application Insights / Sentry / DataDog

---

**نهاية ملف دور الإدارة.**
