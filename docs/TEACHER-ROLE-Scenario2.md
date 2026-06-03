# دور المعلم — السيناريو الثاني (طلب جلسات مفتوح)
## Teacher Role Specification — Scenario 2

> **الغرض من هذا الملف:** توثيق شامل لكل ما يتعلق بدور المعلم في السيناريو الثاني — الرحلة، الشاشات، المتطلبات، الـ APIs، الصلاحيات، الإشعارات. هذا الملف مرجع للـ Frontend Developer وللـ Backend Developer وللـ QA.

---

## 1. نظرة عامة على الدور

### 1.1 من هو المعلم في هذا السيناريو؟
المعلم هو **مزوّد الخدمة التعليمية**. في السيناريو الثاني، المعلم **يستقبل** طلبات الطلاب (بدلاً من إنشاء دورات كما في السيناريو الأول)، ويُقدّم عروضه التنافسية.

### 1.2 الصلاحيات الأساسية
| الصلاحية | متاح؟ |
|----------|-------|
| استقبال الطلبات المؤهلة (مطابقة بالمادة) | ✓ |
| تقديم عرض واحد لكل طلب | ✓ |
| تحديث العرض قبل القبول | ✓ |
| سحب العرض قبل القبول | ✓ |
| التفاوض عبر الشات مع الطالب | ✓ |
| تجاهل/إخفاء طلب | ✓ |
| تنفيذ الجلسات بعد القبول والدفع | ✓ |
| رؤية تفاصيل دفع الطالب | ✗ |
| رؤية عروض المعلمين الآخرين | ✗ |
| رؤية معلومات الاتصال الكاملة للطالب | ✗ |

### 1.3 الحالات المختلفة للمعلم
- **Active Teacher:** نشط ومعتمد، يستقبل الطلبات.
- **Inactive Teacher:** موقوف مؤقتاً، لا يستقبل الطلبات.
- **Suspended:** موقوف بقرار إداري، لا يستقبل الطلبات ولا يقدّم عروضاً.

---

## 2. رحلة المعلم الكاملة

```
[إشعار جديد]
   │
   ▼
① استقبال إشعار بطلب جديد ──── (Real-time + Push + Email)
   │
   ▼
② شاشة "الطلبات الجديدة" ──── (قائمة بالطلبات المُوجّهة له)
   │
   ▼
③ شاشة "تفاصيل الطلب" ──── (كل التفاصيل + مطابقة مع Availability)
   │
   ▼
④ القرار:
   ├──► [تجاهل] ──────────► انتهاء التفاعل
   ├──► [طلب توضيح] ──────► شات تمهيدي
   └──► [تقديم عرض] ──────► الخطوة ⑤
   │
   ▼
⑤ شاشة "تقديم عرض" ──── (سعر + مواعيد + ملاحظات + صلاحية)
   │
   ▼
⑥ مراجعة وإرسال ──── (Status: Pending)
   │
   ▼
⑦ شاشة "عروضي" ──── (متابعة، شات، تحديث)
   │
   ▼
⑧ التفاوض (اختياري) ──── (شات + تحديث العرض)
   │
   ▼
⑨ النتيجة:
   ├──► [رفض / منتهي الصلاحية] ─► انتهاء
   └──► [قبول] ────────────────► الخطوة ⑩
   │
   ▼
⑩ إشعار بالقبول والدفع
   │
   ▼
⑪ تنفيذ الجلسات ──── (نفس نظام السيناريو الأول)
```

---

## 3. المتطلبات الوظيفية (FR-T)

### FR-T-001: استقبال إشعار بطلب جديد
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **الوصف** | المعلم يتلقى إشعار عند مطابقته لطلب |
| **القنوات** | Real-time (SignalR) + Push + Email |
| **المحتوى** | ملخص الطلب + رابط للتفاصيل |
| **Trigger Event** | `SessionRequestPublished` بعد تطبيق Matching Algorithm |

### FR-T-002: عرض الطلبات الواردة
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **الوصف** | قائمة بالطلبات التي يستطيع المعلم تقديم عرض عليها |
| **الفلاتر** | الحالة (جديد، شُوهد، قُدِّم عرض) — التاريخ — المادة |
| **الترتيب** | الأحدث أولاً |
| **Pagination** | 20 طلب/صفحة |

### FR-T-003: عرض تفاصيل الطلب
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **المعلومات المعروضة** | بيانات الطلب الكاملة، تفاصيل كل جلسة، التواريخ المقترحة، اسم الطالب وتقييمه العام، عدد العروض الموجودة (بدون تفاصيلها) |
| **Side Effect** | عند الفتح: تحديث حالة `SessionRequestTarget` إلى `Viewed` |

### FR-T-004: تجاهل/إخفاء طلب
| الحقل | القيمة |
|------|--------|
| **الأولوية** | منخفضة |
| **الوصف** | إخفاء طلب من القائمة بدون رفض رسمي |
| **النتيجة** | الطلب لا يظهر في القائمة الرئيسية، لكن لا يُسجَّل كرفض |

### FR-T-005: تقديم عرض
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **المدخلات** | السعر (إجمالي أو لكل جلسة) — التواريخ والأوقات (من Availability) — ملاحظات/شروط — مدة صلاحية العرض (افتراضي 48 ساعة) |
| **التحقق** | التواريخ في Availability، السعر موجب، مدة الصلاحية بين 24 ساعة و 7 أيام |
| **القاعدة الحاسمة** | **معلم واحد = عرض واحد فقط لكل طلب** (إذا حاول إنشاء عرض ثاني → 409 Conflict) |

### FR-T-006: عرض عروضي
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **الفلاتر** | الحالة (Pending, Accepted, Rejected, AutoRejected, Withdrawn, Expired) — التاريخ |

### FR-T-007: تحديث عرض
| الحقل | القيمة |
|------|--------|
| **الأولوية** | متوسطة |
| **التعديلات المسموحة** | السعر — الملاحظات — مدة الصلاحية |
| **التعديلات الممنوعة** | لا يمكن تعديل التواريخ بعد الإرسال (لتجنب الارتباك) |
| **القواعد** | يُحتفظ بسجل التعديلات (Version++) — رسالة System في الشات — إعادة عدّاد الصلاحية |

### FR-T-008: سحب عرض
| الحقل | القيمة |
|------|--------|
| **الأولوية** | متوسطة |
| **القواعد** | السحب ممكن فقط قبل قبول الطالب — إشعار الطالب — يُحتفظ بالسجل |

### FR-T-009: التفاوض عبر الشات
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **القنوات** | Real-time عبر SignalR + Push للموبايل |
| **الميزات** | Read receipts — إرسال نص — إرسال صور (اختياري في v2) |

### FR-T-010: إشعار بقبول العرض
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **المحتوى** | تأكيد القبول، حالة الدفع، الجلسات المجدولة |
| **القنوات** | Real-time + Push + Email |

### FR-T-011: تنفيذ الجلسات
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **التكامل** | نفس آلية السيناريو الأول — Sessions Module الحالي |

---

## 4. الشاشات (Teacher Screens)

### Screen T-1: الطلبات الجديدة (New Requests Inbox)

**الهدف:** عرض الطلبات المتاحة للمعلم لتقديم عروض عليها.

**العناصر:**
- **Tabs:** الكل | جديدة | شُوهد | قدّمت عروض
- **Filters:** المادة | النوع | الترتيب
- **Cards** لكل طلب تحتوي:
  - شارة الحالة (🔴 جديد / ⚪ شُوهد / 🟢 قُدِّم عرض)
  - وقت الورود (نسبي: "منذ 15 دقيقة")
  - المادة + المرحلة
  - اسم الطالب
  - ملخص الطلب (عدد الجلسات، المدة، النوع، الميزانية إن وُجدت)
  - التواريخ المقترحة
  - مؤشر "عدد العروض الموجودة"
  - مؤشر "تنتهي خلال X أيام"
  - أزرار: [👁️ عرض التفاصيل] [⊘ تجاهل]

**التكامل:**
- Real-time updates عبر SignalR (Hub: `RequestsHub`)
- عند فتح الطلب: تحديث `SessionRequestTarget.Status = Viewed` تلقائياً
- Badge counter على عدد الطلبات الجديدة

**API Calls:**
- `GET /Api/V1/Teacher/AvailableRequests?status=new&page=1&pageSize=20`
- `PUT /Api/V1/Teacher/AvailableRequests/{id}/mark-viewed`
- `POST /Api/V1/Teacher/AvailableRequests/{id}/dismiss`

---

### Screen T-2: تفاصيل الطلب (Request Details — Teacher View)

**الهدف:** عرض تفاصيل الطلب كاملة للمعلم قبل تقديم العرض.

**العناصر:**

**الجزء العلوي (Sticky Header):**
- رقم الطلب: `SR-2026-0145`
- المادة + المرحلة
- شارة الحالة + مؤشر "تنتهي خلال X أيام"
- مؤشر "X عروض حالياً"

**معلومات الطالب:**
- الاسم
- التقييم العام
- عدد الطلبات السابقة
- معدل القبول (إن وُجد)

**Tabs:**
1. **التفاصيل** — المادة، الإعدادات العامة، تفاصيل الجلسات
2. **التواريخ** — مع علامات الحالة (✓ متاح في جدولك / ⚠️ يتعارض / ❌ خارج Availability)
3. **المرفقات** — ملفات وصور

**الميزة الذكية الأهم:**
- **مطابقة تلقائية مع Availability:** كل جلسة تظهر بعلامة:
  - `✓` متاح في جدولك
  - `⚠️` يتعارض مع جلسة أخرى مقترحة (يمكن إعادة الجدولة)
  - `❌` خارج Availability (يحتاج تعديل في الجدول الأسبوعي أولاً)

**Action Buttons (Footer):**
- `⊘ تجاهل`
- `💬 طلب توضيح` (يفتح شات تمهيدي بدون عرض)
- `📝 تقديم عرض` ← يفتح Screen T-3

**API Calls:**
- `GET /Api/V1/Teacher/AvailableRequests/{id}` (تحديث تلقائي للحالة → Viewed)
- `GET /Api/V1/Teacher/AvailableRequests/{id}/availability-match`

---

### Screen T-3: تقديم عرض (Submit Offer)

**الهدف:** المعلم يقدّم عرضه الرسمي.

**أقسام الشاشة:**

**1. ملخص الطلب** (للتذكير، غير قابل للتعديل):
- المادة، عدد الجلسات، النوع، الميزانية

**2. السعر:**
- Radio: `سعر إجمالي` أو `سعر لكل جلسة`
- Input للمبلغ
- عرض السعر الآخر تلقائياً (إذا اختار إجمالي → يحسب السعر/جلسة، والعكس)
- مؤشر مقارنة مع ميزانية الطالب (`✓ في النطاق` / `⚠️ أعلى من النطاق`)

**3. تأكيد المواعيد:**
- كل جلسة من جلسات الطلب تظهر مع:
  - تاريخ ووقت مقترح من الطالب
  - مربع `☑ مؤكد` (افتراضياً مفعّل إذا في Availability)
  - زر `تغيير` لاقتراح بديل لكل جلسة فيها تعارض

**4. ملاحظات/شروط:**
- Textarea، 500 حرف، اختياري
- مثال: "السلام عليكم، يسعدني تقديم عرضي. لدي خبرة 15 سنة..."

**5. مدة صلاحية العرض:**
- Radio: 24 ساعة / 48 ساعة (افتراضي) / 72 ساعة / 7 أيام

**6. تأكيد:**
- ✓ Checkbox إجباري: "أؤكد التزامي بالمواعيد المُختارة"

**Action Buttons:**
- `إلغاء`
- `إرسال العرض` (مفعّل فقط بعد تأكيد كل الجلسات + Checkbox الالتزام)

**القواعد:**
- السعر إجباري (يمكن أن يكون خارج ميزانية الطالب لكن مع تنبيه)
- يجب تأكيد جميع المواعيد أو اقتراح بدائل
- الملاحظات اختيارية
- مدة الصلاحية إجبارية

**API Call:**
- `POST /Api/V1/Teacher/Offers`

---

### Screen T-4: عروضي (My Offers)

**الهدف:** قائمة بكل العروض التي قدّمها المعلم.

**العناصر:**

**Tabs:**
- قيد الانتظار (X)
- مقبولة (X)
- مرفوضة (X)
- منتهية (X)

**Cards** لكل عرض:
- رقم العرض: `OF-2026-0892`
- شارة الحالة
- المادة + المرحلة
- اسم الطالب
- السعر + عدد الجلسات
- مؤشر "تنتهي خلال X يوم" (للعروض المعلقة)
- مؤشر "X رسائل جديدة" (إن وُجد)

**Action Buttons لكل عرض:**
- `👁️ التفاصيل`
- `💬 محادثة` (badge بعدد الرسائل غير المقروءة)
- `✏️ تعديل` (فقط للحالة Pending)
- `⊘ سحب` (فقط للحالة Pending)

**API Calls:**
- `GET /Api/V1/Teacher/Offers/my?status=pending&page=1`

---

### Screen T-5: محادثة مع الطالب (Chat)

**الهدف:** التواصل الفوري مع الطالب حول عرض محدد.

**العناصر:**
- Header: اسم الطالب + رقم العرض
- Sticky Card في الأعلى: ملخص العرض الحالي (السعر، الحالة، صلاحية)
- Messages area:
  - رسائل المعلم (محاذاة اليمين)
  - رسائل الطالب (محاذاة اليسار)
  - رسائل System بنمط مختلف (مثل: "تم تحديث العرض - السعر الجديد 700 ج")
  - Timestamps + Read receipts (✓✓)
- Input area:
  - مربع نص + زر إرسال
  - زر `📎 إرفاق` (v2)
- **زر مميّز:** `[✏️ تحديث العرض]` يفتح Screen T-6

**التكامل:**
- SignalR Hub: `ConversationsHub`
- Group: `conv:{conversationId}`
- يجب الانضمام للمجموعة قبل استقبال الرسائل

**API Calls:**
- `GET /Api/V1/Conversations/by-offer/{offerId}`
- `GET /Api/V1/Conversations/{conversationId}/messages?cursor=...&take=50`
- `POST /Api/V1/Conversations/{conversationId}/messages`
- `POST /Api/V1/Conversations/{conversationId}/read`

---

### Screen T-6: تعديل العرض (Update Offer)

**الهدف:** تعديل عرض مرسل لم يُقبل بعد.

**العناصر:**
- نفس Screen T-3 لكن البيانات السابقة محفوظة ومعروضة
- **شريط تنبيه أصفر في الأعلى:** "⚠️ أي تعديل سيُبلَّغ به الطالب وستُعاد مدة الصلاحية"
- **قسم سجل التعديلات (Collapsible):**
  - Version 1: السعر 600 ج | 17 مايو 10:30 ص
  - Version 2: السعر 550 ج (مع رسالة "خصم خاص") | 18 مايو 2:15 م

**الحقول القابلة للتعديل:**
- السعر ✓
- الملاحظات ✓
- مدة الصلاحية ✓

**الحقول غير القابلة للتعديل:**
- التواريخ والأوقات (لتجنب الارتباك)

**API Call:**
- `PUT /Api/V1/Teacher/Offers/{id}`

---

## 5. API Endpoints

> **Base URL:** `/Api/V1/Teacher`  
> **Authentication:** Bearer JWT (يحتوي على `userId` و `role = Teacher`)  
> **Response Envelope:**
> ```json
> { "succeeded": true, "message": "string", "data": {}, "errors": [] }
> ```

### 5.1 الطلبات المتاحة (Available Requests)

#### `GET /AvailableRequests`
**الوصف:** قائمة الطلبات التي يستطيع المعلم تقديم عرض عليها.

**Query Parameters:**
| المعامل | النوع | افتراضي | الوصف |
|---------|------|---------|--------|
| `status` | enum | `all` | `all` / `new` / `viewed` / `offered` |
| `subjectId` | int | — | فلتر بالمادة |
| `dateFrom` | date | — | تاريخ من |
| `dateTo` | date | — | تاريخ إلى |
| `page` | int | 1 | رقم الصفحة |
| `pageSize` | int | 20 | حجم الصفحة (max: 50) |
| `sortBy` | enum | `newest` | `newest` / `expiringSoon` / `mostOffers` |

**Response 200:**
```json
{
  "succeeded": true,
  "message": null,
  "data": {
    "items": [
      {
        "id": "guid",
        "requestNumber": "SR-2026-0145",
        "subject": { "id": 5, "nameAr": "الرياضيات", "nameEn": "Mathematics" },
        "level": { "id": 3, "nameAr": "الصف الثالث الثانوي" },
        "student": {
          "id": "guid",
          "displayName": "سارة م.",
          "rating": 4.7
        },
        "sessionsCount": 5,
        "sessionDurationMinutes": 60,
        "teachingMode": "Online",
        "groupType": "Individual",
        "budgetRange": { "min": 100, "max": 150, "currency": "EGP" },
        "preferredDates": ["2026-05-19", "2026-05-21", "2026-05-23", "2026-05-25", "2026-05-27"],
        "currentOffersCount": 4,
        "expiresAt": "2026-05-24T00:00:00Z",
        "targetStatus": "new",
        "matchedAt": "2026-05-17T10:31:00Z",
        "viewedAt": null
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalCount": 12,
      "totalPages": 1
    }
  },
  "errors": []
}
```

---

#### `GET /AvailableRequests/{id}`
**الوصف:** تفاصيل طلب محدد. **Side Effect:** يتم تحديث `targetStatus` إلى `viewed` تلقائياً.

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "id": "guid",
    "requestNumber": "SR-2026-0145",
    "status": "Active",
    "content": {
      "domain": { "id": 1, "nameAr": "مدرسي" },
      "curriculum": { "id": 2, "nameAr": "سعودي" },
      "level": { "id": 3, "nameAr": "ثانوي" },
      "grade": { "id": 3, "nameAr": "الصف الثالث" },
      "subject": { "id": 5, "nameAr": "الرياضيات" },
      "units": [
        { "id": 12, "nameAr": "التفاضل" },
        { "id": 13, "nameAr": "التكامل" }
      ]
    },
    "generalSettings": {
      "sessionsCount": 5,
      "sessionDurationMinutes": 60,
      "teachingMode": "Online",
      "groupType": "Individual",
      "sessionPurpose": "Review",
      "budgetRange": { "min": 100, "max": 150 }
    },
    "sessions": [
      {
        "id": "guid",
        "sequenceNumber": 1,
        "title": "مراجعة الفصل الأول - التفاضل",
        "scheduledDate": "2026-05-19",
        "timeSlot": { "id": 7, "from": "16:00", "to": "17:00" },
        "durationMinutes": 60,
        "units": [{ "id": 12, "lessonId": 45 }],
        "notes": "أحتاج فهم المشتقة من الصفر",
        "attachments": [
          { "id": "guid", "fileName": "ملف 1.pdf", "url": "...", "sizeBytes": 1024000 }
        ],
        "availabilityStatus": "available"
      },
      {
        "id": "guid",
        "sequenceNumber": 3,
        "availabilityStatus": "conflict",
        "conflictWith": "ScheduledSession-XXXX"
      },
      {
        "id": "guid",
        "sequenceNumber": 5,
        "availabilityStatus": "outside_availability"
      }
    ],
    "student": {
      "id": "guid",
      "displayName": "سارة م.",
      "rating": 4.7,
      "completedRequestsCount": 5,
      "acceptanceRate": 0.80
    },
    "currentOffersCount": 4,
    "myOfferStatus": null,
    "expiresAt": "2026-05-24T00:00:00Z"
  }
}
```

**Response 403 (إذا المعلم غير مطابق للطلب):**
```json
{ "succeeded": false, "message": "غير مصرّح", "errors": ["NOT_MATCHED"] }
```

---

#### `POST /AvailableRequests/{id}/dismiss`
**الوصف:** إخفاء طلب من قائمة المعلم.

**Response 200:**
```json
{ "succeeded": true, "message": "تم تجاهل الطلب" }
```

---

### 5.2 العروض (Offers)

#### `POST /Offers`
**الوصف:** تقديم عرض جديد على طلب.

**Request Body:**
```json
{
  "sessionRequestId": "guid",
  "priceMode": "Total",
  "totalPrice": 600,
  "currency": "EGP",
  "schedules": [
    {
      "sessionRequestSessionId": "guid",
      "proposedDate": "2026-05-19",
      "timeSlotId": 7,
      "matchesStudentPreference": true
    },
    {
      "sessionRequestSessionId": "guid",
      "proposedDate": "2026-05-23",
      "timeSlotId": 8,
      "matchesStudentPreference": false
    }
  ],
  "teacherNotes": "السلام عليكم، يسعدني تقديم عرضي.",
  "validityHours": 48,
  "commitmentConfirmed": true
}
```

**Response 201:**
```json
{
  "succeeded": true,
  "message": "تم إرسال العرض بنجاح",
  "data": {
    "offerId": "guid",
    "offerNumber": "OF-2026-0892",
    "status": "Pending",
    "expiresAt": "2026-05-19T10:30:00Z"
  }
}
```

**Response 409 (إذا المعلم سبق وقدّم عرضاً على هذا الطلب):**
```json
{
  "succeeded": false,
  "message": "لديك عرض موجود على هذا الطلب",
  "errors": ["DUPLICATE_OFFER"],
  "data": { "existingOfferId": "guid" }
}
```

**Response 400 (Validation errors):**
```json
{
  "succeeded": false,
  "errors": [
    { "field": "totalPrice", "code": "MUST_BE_POSITIVE" },
    { "field": "schedules[2].timeSlotId", "code": "NOT_IN_AVAILABILITY" }
  ]
}
```

---

#### `PUT /Offers/{id}`
**الوصف:** تحديث عرض مرسل (Status = Pending).

**Request Body:** (نفس `POST /Offers` لكن الحقول كلها اختيارية، باستثناء الحقول المسموح بتعديلها فقط)
```json
{
  "totalPrice": 550,
  "teacherNotes": "خصم خاص لكِ على العرض الأول",
  "validityHours": 72
}
```

**ملاحظة:** `schedules` غير قابل للتعديل بعد الإرسال.

**Response 200:**
```json
{
  "succeeded": true,
  "message": "تم تحديث العرض",
  "data": {
    "offerId": "guid",
    "version": 2,
    "expiresAt": "2026-05-21T10:30:00Z"
  }
}
```

**Side Effects:**
- Version يزيد بـ 1
- رسالة System تُضاف للشات: "تم تحديث العرض - السعر الجديد: 550 ج"
- إعادة عدّاد الصلاحية
- إشعار للطالب: "تحديث على عرض د. أحمد"

---

#### `POST /Offers/{id}/withdraw`
**الوصف:** سحب عرض.

**Request Body:**
```json
{ "reason": "اعتذر، التزامات طارئة" }
```

**Response 200:**
```json
{ "succeeded": true, "message": "تم سحب العرض" }
```

**القواعد:**
- مسموح فقط إذا `status == Pending`
- إشعار للطالب
- رسالة System في الشات

---

#### `GET /Offers/my`
**الوصف:** قائمة عروض المعلم.

**Query Parameters:**
| المعامل | القيم |
|---------|------|
| `status` | `all` / `pending` / `accepted` / `rejected` / `autoRejected` / `withdrawn` / `expired` |
| `dateFrom` / `dateTo` | تاريخ |
| `page` / `pageSize` | كالعادة |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "offerNumber": "OF-2026-0892",
        "sessionRequest": {
          "id": "guid",
          "requestNumber": "SR-2026-0145",
          "subject": { "nameAr": "الرياضيات" },
          "level": { "nameAr": "الصف الثالث الثانوي" }
        },
        "student": { "displayName": "سارة م." },
        "totalPrice": 600,
        "sessionsCount": 5,
        "status": "Pending",
        "version": 1,
        "createdAt": "2026-05-17T11:45:00Z",
        "expiresAt": "2026-05-19T11:45:00Z",
        "unreadMessagesCount": 3
      }
    ],
    "pagination": { "page": 1, "pageSize": 20, "totalCount": 23 }
  }
}
```

---

#### `GET /Offers/{id}`
**الوصف:** تفاصيل عرض محدد.

**Response 200:** (مشابه `GET /AvailableRequests/{id}` لكن مع تفاصيل العرض)

---

### 5.3 المحادثات (Conversations)

> **Base URL:** `/Api/V1/Conversations`

#### `GET /by-offer/{offerId}`
**الوصف:** إيجاد أو إنشاء محادثة مرتبطة بعرض.

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "conversationId": "guid",
    "offerId": "guid",
    "participants": [
      { "userId": "guid", "displayName": "سارة م.", "role": "Student" },
      { "userId": "guid", "displayName": "د. أحمد", "role": "Teacher" }
    ],
    "lastMessageAt": "2026-05-17T14:30:00Z",
    "unreadCount": 3
  }
}
```

---

#### `GET /{conversationId}/messages`
**الوصف:** الحصول على الرسائل (Pagination بـ Cursor).

**Query Parameters:**
| المعامل | القيمة |
|---------|--------|
| `cursor` | timestamp أو messageId للرسالة الأخيرة المُحمَّلة |
| `take` | 50 (افتراضي) |
| `direction` | `older` (افتراضي) أو `newer` |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "messages": [
      {
        "id": "guid",
        "type": "Text",
        "senderId": "guid",
        "senderRole": "Student",
        "content": "هل ممكن خصم؟",
        "sentAt": "2026-05-17T14:25:00Z",
        "readAt": "2026-05-17T14:26:00Z"
      },
      {
        "id": "guid",
        "type": "System",
        "content": "تم تحديث العرض - السعر الجديد: 550 ج",
        "sentAt": "2026-05-17T14:28:00Z",
        "metadata": { "offerVersion": 2, "newPrice": 550 }
      }
    ],
    "nextCursor": "2026-05-17T14:20:00Z",
    "hasMore": true
  }
}
```

---

#### `POST /{conversationId}/messages`
**Request Body:**
```json
{ "content": "ماشي، موافق على 550 ج" }
```

**Response 201:**
```json
{
  "succeeded": true,
  "data": {
    "messageId": "guid",
    "sentAt": "2026-05-17T14:30:00Z"
  }
}
```

**Side Effects:**
- بث الرسالة عبر SignalR لكل أعضاء المحادثة
- زيادة `UnreadCount` للطرف الآخر
- تحديث `LastMessageAt`

---

#### `POST /{conversationId}/read`
**الوصف:** تمييز الرسائل كمقروءة.

**Request Body (optional):**
```json
{ "upToMessageId": "guid" }
```

**Response 200:**
```json
{ "succeeded": true, "message": "تم تحديث حالة القراءة" }
```

---

### 5.4 الجلسات بعد القبول (Scheduled Sessions)

#### `GET /MySessions`
**الوصف:** قائمة جلسات المعلم القادمة (من السيناريو الثاني والأول معاً).

**Query Parameters:**
- `scope` = `upcoming` / `today` / `past`
- `source` = `all` / `scenario1` / `scenario2`

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "scheduledSessionId": "guid",
        "source": "Scenario2",
        "sessionRequestId": "guid",
        "title": "مراجعة الفصل الأول - التفاضل",
        "student": { "displayName": "سارة م." },
        "scheduledAt": "2026-05-19T16:00:00Z",
        "durationMinutes": 60,
        "zoomJoinUrl": "https://zoom.us/...",
        "zoomStartUrl": "https://zoom.us/s/...",
        "status": "Scheduled"
      }
    ]
  }
}
```

---

## 6. SignalR Hubs المتعلقة بالمعلم

### `NotificationsHub` (`/hubs/notifications`)
**الاشتراك التلقائي:** عند الـ connect، المستخدم يُضاف لمجموعة `user:{userId}`.

**الأحداث المُستقبَلة:**

| الحدث | الـ Payload | متى يحدث |
|------|--------------|-----------|
| `RequestPublished` | `{ requestId, requestNumber, subject, expiresAt }` | عند نشر طلب جديد يطابق مواد المعلم |
| `OfferAccepted` | `{ offerId, requestId, scheduledSessionsCount }` | الطالب قبل عرض المعلم |
| `OfferRejected` | `{ offerId, requestId, reason }` | الطالب رفض عرض المعلم صراحةً |
| `OfferAutoRejected` | `{ offerId, requestId, acceptedTeacherName? }` | الطالب قبل عرض معلم آخر |
| `OfferExpired` | `{ offerId, requestId }` | انتهت مدة العرض |
| `RequestCancelled` | `{ requestId, reason }` | الطالب ألغى الطلب |
| `SessionReminder` | `{ scheduledSessionId, startsAt }` | قبل بدء الجلسة بـ 30 دقيقة |

### `ConversationsHub` (`/hubs/conversations`)
**الاشتراك اليدوي:** المعلم يستدعي `JoinConversation(conversationId)` عند فتح شاشة الشات.

**الأحداث المُستقبَلة:**

| الحدث | الـ Payload |
|------|--------------|
| `MessageReceived` | `{ messageId, conversationId, senderId, content, sentAt, type }` |
| `MessageRead` | `{ conversationId, readerId, upToMessageId, readAt }` |
| `OfferUpdated` | `{ offerId, version, newPrice }` (System message linked) |

**الطرق المستدعاة من العميل:**
- `JoinConversation(conversationId)`
- `LeaveConversation(conversationId)`
- `Typing(conversationId)` (اختياري)

---

## 7. الإشعارات (Notifications)

### قنوات الإيصال للمعلم

| الحدث | Real-time | Push | Email | In-App |
|------|-----------|------|-------|--------|
| طلب جديد مطابق | ✓ | ✓ | ✓ | ✓ |
| رسالة جديدة من طالب | ✓ | ✓ | ✗ | ✓ |
| قبول عرضي | ✓ | ✓ | ✓ | ✓ |
| رفض عرضي صراحةً | ✓ | ✓ | ✗ | ✓ |
| رفض تلقائي (قبول عرض غيري) | ✓ | ✗ | ✗ | ✓ |
| انتهاء صلاحية عرضي | ✓ | ✓ | ✗ | ✓ |
| تذكير قبل انتهاء الصلاحية (4 ساعات) | ✗ | ✓ | ✗ | ✓ |
| تذكير قبل جلسة (30 دقيقة) | ✓ | ✓ | ✗ | ✓ |
| إلغاء الطالب للطلب بعد قبول | ✓ | ✓ | ✓ | ✓ |

### قوالب الإشعارات (Templates)

**1. طلب جديد:**
- **Title:** "طلب جديد مطابق لتخصصك!"
- **Body:** "{StudentName} يطلب {SubjectName} - {SessionsCount} جلسات"
- **Action:** فتح Screen T-2

**2. قبول العرض:**
- **Title:** "🎉 تم قبول عرضك!"
- **Body:** "{StudentName} قبل عرضك بـ {Price} ج"
- **Action:** فتح Screen T-4

**3. رفض تلقائي:**
- **Title:** "تم اختيار عرض آخر"
- **Body:** "لم يتم اختيار عرضك على {RequestNumber}"
- **Action:** فتح Screen T-4

---

## 8. الصلاحيات والقيود الأمنية

### Authorization Policies (.NET)

```csharp
[Authorize(Roles = "Teacher")]
[Authorize(Policy = "ActiveTeacherOnly")]  // checks user.Status == Active
public class TeacherController : ControllerBase { ... }
```

### قواعد إضافية يجب التحقق منها

1. **`POST /Offers`:**
   - المعلم يجب أن يكون في `SessionRequestTarget` لهذا الطلب (مطابق)
   - الطلب يجب أن يكون في حالة `Active` أو `ReceivingOffers`
   - لا يوجد عرض سابق من نفس المعلم (Status != Withdrawn)

2. **`PUT /Offers/{id}` و `POST /Offers/{id}/withdraw`:**
   - المعلم يجب أن يكون مالك العرض (`offer.TeacherId == currentUserId`)
   - حالة العرض يجب أن تكون `Pending`

3. **`GET /Conversations/{id}/messages`:**
   - المستخدم يجب أن يكون participant في المحادثة
   - يتم التحقق من `offer.TeacherId == userId` أو `offer.SessionRequest.StudentId == userId`

4. **محتوى الرسائل:**
   - لا تظهر معلومات اتصال الطالب الكاملة في الرسائل
   - فلتر للكلمات المسيئة (v2)

---

## 9. حالات الخطأ المحتملة (Error Codes)

| الكود | المعنى | HTTP Status |
|------|---------|-------------|
| `NOT_MATCHED` | المعلم غير مطابق لهذا الطلب | 403 |
| `DUPLICATE_OFFER` | يوجد عرض سابق على نفس الطلب | 409 |
| `REQUEST_NOT_ACTIVE` | الطلب ليس في حالة Active/ReceivingOffers | 409 |
| `OFFER_NOT_PENDING` | العرض ليس في حالة Pending (لا يمكن تعديل/سحب) | 409 |
| `MUST_BE_POSITIVE` | السعر يجب أن يكون موجباً | 400 |
| `NOT_IN_AVAILABILITY` | الوقت المقترح خارج Availability | 400 |
| `INVALID_VALIDITY_HOURS` | مدة الصلاحية يجب أن تكون بين 24 و 168 ساعة | 400 |
| `COMMITMENT_NOT_CONFIRMED` | لم يتم تأكيد الالتزام بالمواعيد | 400 |
| `TEACHER_SUSPENDED` | المعلم موقوف | 403 |
| `OFFER_EXPIRED` | العرض منتهي الصلاحية | 409 |
| `OFFER_ALREADY_ACCEPTED_BY_STUDENT` | الطالب قبل عرضاً آخر بالفعل | 409 |

---

## 10. ملحقات

### 10.1 ترقيم العروض
- النمط: `OF-{Year}-{SequentialNumber}` مثل `OF-2026-0892`
- يُولَّد تلقائياً عند إنشاء العرض

### 10.2 مدة الصلاحية الافتراضية
- 48 ساعة من وقت الإنشاء
- قابلة للتخصيص بين 24 ساعة و 168 ساعة (7 أيام)

### 10.3 تنبيه قبل انتهاء الصلاحية
- 4 ساعات قبل الانتهاء → Push للمعلم
- 1 ساعة قبل الانتهاء → Push للطالب

### 10.4 Background Jobs المتعلقة بالمعلم
- **OfferExpiryJob:** يُشغَّل كل 15 دقيقة → يغيّر حالة العروض المنتهية إلى `Expired`
- **OfferExpiryReminderJob:** يُشغَّل كل ساعة → ينبّه المعلم قبل 4 ساعات

---

**نهاية ملف دور المعلم.**
