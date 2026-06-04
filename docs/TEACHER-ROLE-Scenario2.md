# دور المعلم — السيناريو الثاني (طلب جلسات مفتوح)
## Teacher Role Specification — Scenario 2

> **الغرض من هذا الملف:** توثيق شامل لكل ما يتعلق بدور المعلم في السيناريو الثاني — الرحلة، الشاشات، المتطلبات، الـ APIs، الصلاحيات، الإشعارات. هذا الملف مرجع للـ Frontend Developer وللـ Backend Developer وللـ QA.

---

## 📋 v1 scope at a glance

This file describes **what's shipped today**. Three things from the original wishlist did NOT land in v1 and are tagged inline as `[v1.5]`:

- **SignalR hubs.** Notifications use the existing RabbitMQ → email pipeline; the frontend polls.
- **Push notifications.** Need a device-token table that doesn't exist yet — Email + In-App only for v1.
- **Acceptance / payment / scheduling endpoints** (FR-T-010, FR-T-011, `GET /MySessions`, related notifications). These need Scenario 2 phases P6 + P7.

Other conventions to know upfront:

- **Currency = SAR.** `OpenSessionOffer.Price` is `decimal(18,2)`, no currency column — values are SAR by convention.
- **IDs are `int`** everywhere. JSON examples use `1234`, never `"guid"`.
- **Teacher does NOT propose schedules.** The offer implicitly accepts the student's `OpenSessionRequestSession` timing. `POST /Offers` has `price`, `teacherNotes`, `validityHours`, `commitmentConfirmed` — no `schedules[]`, no `priceMode`, no `currency`.
- **Conversations are keyed by `(SessionRequestId, TeacherId)`**, not by offer. This supports the preliminary "طلب توضيح" chat (Screen T-2) before any offer exists, and keeps chat history across withdraw + re-offer cycles. The conversation entry endpoint is `GET /Conversations/by-request/{requestId}/teacher/{teacherId}` — there's no `/by-offer/{offerId}` route.
- **Target-status enum names:** the code uses `Notified / Viewed / OfferSubmitted / Skipped` (this doc uses those names throughout — they map 1:1 to the frontend's "new / viewed / offered / dismissed" labels).

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
① استقبال إشعار بطلب جديد ──── (Email + In-App. Push/Real-time = v1.5)
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
⑤ شاشة "تقديم عرض" ──── (سعر + ملاحظات + صلاحية. التواريخ من الطالب)
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
⑩ إشعار بالقبول والدفع                       ⚠️ [v1.5 — P6]
   │
   ▼
⑪ تنفيذ الجلسات ──── (نفس نظام السيناريو الأول)  ⚠️ [v1.5 — P7]
```

---

## 3. المتطلبات الوظيفية (FR-T)

### FR-T-001: استقبال إشعار بطلب جديد
| الحقل | القيمة |
|------|--------|
| **الأولوية** | عالية |
| **الوصف** | المعلم يتلقى إشعار عند مطابقته لطلب |
| **القنوات** | Email + In-App (يظهر في صندوق الطلبات الجديدة). Push/SignalR `[v1.5]` |
| **المحتوى** | ملخص الطلب + رابط للتفاصيل |
| **Trigger Event** | بعد `SaveChanges` على `OpenSessionRequest.Status == Active`، يستدعي `IOpenSessionRequestTargetingService.RunMatchingAndNotifyAsync` الذي يدير المطابقة + كتابة `OpenSessionRequestTarget` rows + إرسال البريد عبر `IRabbitMQService.QueueEmailAsync`. |

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
| **المدخلات** | `price` (إجمالي بالـ SAR، رقم واحد) — `teacherNotes` اختياري (≤1000 حرف) — `validityHours` (افتراضي 48) — `commitmentConfirmed=true` إجباري |
| **ملاحظة** | المعلم **لا يقترح مواعيد** — العرض يقبل ضمنياً تواريخ الطالب المخزّنة على `OpenSessionRequestSession`. لذا لا يوجد `schedules[]` في جسم الطلب. |
| **التحقق** | السعر موجب، مدة الصلاحية بين 24 ساعة و 168 ساعة (7 أيام)، `commitmentConfirmed == true` |
| **القاعدة الحاسمة** | **معلم واحد = عرض واحد فعّال لكل طلب** (Status ≠ Withdrawn). محاولة إنشاء عرض ثانٍ → 409 Conflict مع `data.existingOfferId` |

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
| **القنوات** | Email إشعار للطرف الآخر عند رسالة جديدة + In-App. Real-time/Push `[v1.5]` |
| **الميزات** | Read receipts (TeacherLastReadAt / StudentLastReadAt) — رسائل نصية (≤4000 حرف) — رسائل System تلقائية. إرسال صور `[v2]` |
| **المفتاح الطبيعي للمحادثة** | `(SessionRequestId, TeacherId)` — يدعم شات تمهيدي قبل تقديم العرض |

### FR-T-010: إشعار بقبول العرض `[v1.5 — P6]`
| الحقل | القيمة |
|------|--------|
| **الحالة** | **غير منفّذ في v1.** يتطلب P6 (Accept/Auto-Reject flow). الإشعارات الناتجة (قبول، رفض صريح، رفض تلقائي) ستُضاف عند تنفيذ P6. |

### FR-T-011: تنفيذ الجلسات `[v1.5 — P7]`
| الحقل | القيمة |
|------|--------|
| **الحالة** | **غير منفّذ في v1.** يتطلب P6 (Accept) + P7 (Payment + Scheduling). الجلسات ستُولَّد عبر `ScheduleGenerationService` بعد اكتمال الدفع — نفس آلية السيناريو الأول. |

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
- Polling فقط — يستدعي `GET /AvailableRequests?status=Notified` على فترات. Real-time `[v1.5]`.
- عند فتح الطلب: تحديث `OpenSessionRequestTarget.Status = Viewed` تلقائياً (Side effect على `GET /AvailableRequests/{id}`).
- Badge counter محسوب من `data.pagination.totalCount` عند الفلتر `status=Notified`.

**Tab → status mapping (لتحويل تسميات الواجهة إلى قيم enum):**
| Tab | `status` query value |
|---|---|
| الكل | حذف الباراميتر (أو `?status=all`) |
| جديدة | `Notified` |
| شُوهد | `Viewed` |
| قدّمت عروض | `OfferSubmitted` |

**API Calls:**
- `GET /Api/V1/Teacher/AvailableRequests?status=Notified&pageNumber=1&pageSize=20`
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
- المادة، عدد الجلسات، النوع.

**2. السعر:**
- Input واحد للسعر الإجمالي بالـ SAR.
- ⚠️ لا توجد ميزانية مخزّنة على الطلب في v1 — مؤشر مقارنة الميزانية مؤجَّل لمرحلة لاحقة.

**3. تواريخ الجلسات (للعرض فقط):**
- تظهر تواريخ الطالب المقترحة من `OpenSessionRequestSession.PreferredDate` + `TimeSlot`.
- بجوار كل جلسة، شارة من `GET /availability-match`: ✓ `Available` / ⚠️ `Conflict` / ❌ `OutsideAvailability`.
- ⚠️ المعلم **لا يستطيع** تعديل التواريخ — يقبل توقيت الطالب ضمنياً عند الإرسال. لا يوجد زر "تغيير".

**4. ملاحظات/شروط:**
- Textarea، حد أقصى 1000 حرف، اختياري.
- يُرسَل كـ `teacherNotes` في الـ body.

**5. مدة صلاحية العرض:**
- Radio: 24 / 48 (افتراضي) / 72 / 168 ساعة. أي قيمة بين 24 و 168 مقبولة.

**6. تأكيد:**
- ✓ Checkbox إجباري: "أؤكد التزامي بمواعيد الطالب". يُرسَل كـ `commitmentConfirmed=true` (إجباري).

**Action Buttons:**
- `إلغاء`
- `إرسال العرض` (مفعّل فقط بعد Checkbox الالتزام + إدخال سعر موجب)

**القواعد:**
- السعر إجباري وموجب.
- مدة الصلاحية إجبارية ضمن النطاق المحدد.
- `commitmentConfirmed` يجب أن يكون `true`.

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
- **Polling.** SignalR `[v1.5]`. الواجهة تستدعي `GET /messages?direction=newer&cursor=...` على فترات لجلب الرسائل الجديدة.
- المحادثة قد تكون موجودة قبل العرض (شات تمهيدي عبر زر `طلب توضيح` في Screen T-2).
- البطاقة الثابتة (Sticky Card) تستخدم `OfferConversationDto.OfferId` — إن كانت `0` فلا يوجد عرض بعد (شات تمهيدي).

**API Calls:**
- `GET /Api/V1/Conversations/by-request/{requestId}/teacher/{teacherId}` — تُنشئ المحادثة عند أول استدعاء.
- `GET /Api/V1/Conversations/{conversationId}/messages?cursor=...&take=50&direction=older`
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
| `status` | enum | (الكل) | `Notified` / `Viewed` / `OfferSubmitted` / `Skipped`. حذف الباراميتر = الكل |
| `subjectId` | int | — | فلتر بالمادة |
| `dateFrom` | date | — | فلتر بالطلبات التي تحتوي على جلسة في هذا التاريخ أو بعده |
| `dateTo` | date | — | فلتر بالطلبات التي تحتوي على جلسة في هذا التاريخ أو قبله |
| `pageNumber` | int | 1 | رقم الصفحة |
| `pageSize` | int | 20 | حجم الصفحة (max: 50) |
| `sortBy` | enum | `Newest` | `Newest` / `ExpiringSoon` / `MostOffers` |

**Response 200:**
```json
{
  "succeeded": true,
  "message": "Success",
  "data": {
    "items": [
      {
        "id": 145,
        "subjectId": 5,
        "subjectNameEn": "Mathematics",
        "subjectNameAr": "الرياضيات",
        "levelId": 3,
        "levelNameEn": "Secondary 3",
        "levelNameAr": "الصف الثالث الثانوي",
        "studentId": 88,
        "studentDisplayName": "سارة محمد",
        "sessionsCount": 5,
        "teachingModeId": 1,
        "teachingModeNameEn": "Online",
        "groupType": "Individual",
        "preferredDates": ["2026-05-19", "2026-05-21", "2026-05-23", "2026-05-25", "2026-05-27"],
        "currentOffersCount": 4,
        "expiresAt": "2026-05-24T00:00:00Z",
        "targetStatus": "Notified",
        "matchedAt": "2026-05-17T10:31:00Z",
        "viewedAt": null
      }
    ],
    "totalCount": 12,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 1
  },
  "errors": null
}
```

> الحقول التطلعية `subject.nameAr/En` و `level.nameAr/En` كحقول مسطّحة (مش nested). `budgetRange`, `student.rating`, `sessionDurationMinutes` (لكل العنصر) — مؤجَّلة لمرحلة لاحقة (مش موجودة على الكيان).

---

#### `GET /AvailableRequests/{id}`
**الوصف:** تفاصيل طلب محدد. **Side Effect:** إذا كانت `targetStatus == Notified`، تُحدَّث إلى `Viewed` تلقائياً + `viewedAt` يُملأ. لاحظ أن تفاصيل الجلسات هنا **لا** تتضمن availabilityStatus — تلك من endpoint منفصل (انظر أدناه).

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "id": 145,
    "status": "Active",
    "content": {
      "domainId": 1,
      "domainNameEn": "Academic",
      "domainNameAr": "مدرسي",
      "curriculumId": 2,
      "curriculumNameEn": "Saudi",
      "levelId": 3,
      "levelNameEn": "Secondary",
      "gradeId": 3,
      "gradeNameEn": "Grade 12",
      "subjectId": 5,
      "subjectNameEn": "Mathematics",
      "subjectNameAr": "الرياضيات"
    },
    "generalSettings": {
      "sessionsCount": 5,
      "defaultDurationMinutes": 60,
      "teachingModeId": 1,
      "teachingModeNameEn": "Online",
      "groupType": "Individual",
      "studentNotes": "أحتاج مراجعة شاملة قبل النهائي"
    },
    "sessions": [
      {
        "id": 901,
        "sequenceNumber": 1,
        "preferredDate": "2026-05-19",
        "timeSlotId": 7,
        "timeSlotLabelEn": "16:00–17:00",
        "durationMinutes": 60,
        "notes": "أحتاج فهم المشتقة من الصفر",
        "units": [
          {
            "id": 5001,
            "contentUnitId": 12,
            "contentUnitNameEn": "Differentiation",
            "contentUnitNameAr": "التفاضل",
            "lessonId": null,
            "lessonNameEn": null,
            "lessonNameAr": null
          },
          {
            "id": 5002,
            "contentUnitId": null,
            "contentUnitNameEn": null,
            "contentUnitNameAr": null,
            "lessonId": 45,
            "lessonNameEn": "Chain Rule",
            "lessonNameAr": "قاعدة السلسلة"
          }
        ]
      }
    ],
    "student": {
      "id": 88,
      "displayName": "سارة محمد"
    },
    "currentOffersCount": 4,
    "myOfferStatus": null,
    "myOfferId": null,
    "expiresAt": "2026-05-24T00:00:00Z",
    "publishedAt": "2026-05-17T10:30:00Z"
  }
}
```

> ⚠️ **لاحظ**: حقول `student.rating`, `completedRequestsCount`, `acceptanceRate` و `sessionPurpose` **مش موجودة** في v1. الحقول `units[]` تستخدم نفس شكل الـ create body — `contentUnitId` OR `lessonId` (واحد فقط). لا يوجد `attachments` على الـ session — المرفقات على مستوى الطلب وليست الجلسة، وستضاف لـ DTO الـ teacher view لاحقاً عند الحاجة.

**Response 403 (إذا المعلم غير مطابق للطلب):**
```json
{ "succeeded": false, "message": "NOT_MATCHED" }
```

---

#### `PUT /AvailableRequests/{id}/mark-viewed`
**الوصف:** تحديث الـ target row إلى `Viewed` بدون جلب التفاصيل. Idempotent — لا يفعل شيئاً إذا كانت الحالة ليست `Notified`.

**Response 200:**
```json
{ "succeeded": true, "message": "Marked as viewed." }
```

**Response 403 (المعلم ليس في target rows):**
```json
{ "succeeded": false, "message": "NOT_MATCHED" }
```

---

#### `GET /AvailableRequests/{id}/availability-match`
**الوصف:** لكل جلسة في الطلب، يرجع شارة `Available` / `Conflict` / `OutsideAvailability` بناءً على:
1. توفر المعلم (`TeacherAvailability` rows لليوم/الفترة المطلوبة).
2. تعارض مع `CourseSchedule` موجود للمعلم في نفس التاريخ + نفس النافذة الزمنية.

**Response 200:**
```json
{
  "succeeded": true,
  "data": [
    {
      "sessionId": 901,
      "sequenceNumber": 1,
      "preferredDate": "2026-05-19",
      "timeSlotId": 7,
      "status": "Available",
      "conflictWith": null
    },
    {
      "sessionId": 903,
      "sequenceNumber": 3,
      "preferredDate": "2026-05-23",
      "timeSlotId": 8,
      "status": "Conflict",
      "conflictWith": "Booked 2026-05-23 14:00-15:00"
    },
    {
      "sessionId": 905,
      "sequenceNumber": 5,
      "preferredDate": "2026-05-25",
      "timeSlotId": 9,
      "status": "OutsideAvailability",
      "conflictWith": null
    }
  ]
}
```

---

#### `POST /AvailableRequests/{id}/dismiss`
**الوصف:** إخفاء طلب من قائمة المعلم (`OpenSessionRequestTarget.Status = Skipped`).

**Response 200:**
```json
{ "succeeded": true, "message": "تم تجاهل الطلب" }
```

**Response 400 (إذا المعلم سبق وقدّم عرضاً):**
```json
{ "succeeded": false, "message": "OFFER_ALREADY_SUBMITTED" }
```

---

### 5.2 العروض (Offers)

#### `POST /Offers`
**الوصف:** تقديم عرض جديد على طلب.

**Request Body:**
```json
{
  "sessionRequestId": 145,
  "price": 600,
  "teacherNotes": "السلام عليكم، يسعدني تقديم عرضي.",
  "validityHours": 48,
  "commitmentConfirmed": true
}
```

> لاحظ — **لا** يوجد `schedules[]`, `priceMode`, `currency`. المعلم لا يقترح مواعيد، السعر إجمالي بالـ SAR.

**Response 201:**
```json
{
  "succeeded": true,
  "message": "Created",
  "data": {
    "id": 892,
    "sessionRequestId": 145,
    "teacherId": 17,
    "price": 600,
    "teacherNotes": "السلام عليكم، يسعدني تقديم عرضي.",
    "status": "Pending",
    "version": 1,
    "createdAt": "2026-05-17T11:45:00Z",
    "expiresAt": "2026-05-19T11:45:00Z",
    "acceptedAt": null,
    "rejectedAt": null,
    "withdrawnAt": null,
    "expiredAt": null,
    "rejectionReason": null,
    "conversationId": 412,
    "request": null
  }
}
```

> الـ `id` هو الـ PK الـ int. شكل سلسلة "OF-2026-0892" هو سلسلة عرض يبنيها الواجهة من `id` + `createdAt.Year`.

**Response 409 (المعلم سبق وقدّم عرضاً غير منسحب على هذا الطلب):**
```json
{
  "succeeded": false,
  "message": "DUPLICATE_OFFER",
  "meta": {
    "existingOfferId": 850,
    "existingOfferStatus": "Pending"
  }
}
```

**Response 409 (الطلب ليس في حالة تقبل عروضاً):**
```json
{ "succeeded": false, "message": "REQUEST_NOT_ACTIVE" }
```

**Response 403 (المعلم غير مطابق للطلب):**
```json
{ "succeeded": false, "message": "NOT_MATCHED" }
```

**Response 400 (Validation errors — FluentValidation format):**
```json
{
  "succeeded": false,
  "message": "Bad Request",
  "errors": [
    "MUST_BE_POSITIVE",
    "INVALID_VALIDITY_HOURS",
    "COMMITMENT_NOT_CONFIRMED"
  ]
}
```

---

#### `PUT /Offers/{id}`
**الوصف:** تحديث عرض مرسل (Status = Pending). الحقول كلها اختيارية — أرسل ما تريد تعديله فقط.

**Request Body:**
```json
{
  "price": 550,
  "teacherNotes": "خصم خاص لكِ على العرض الأول",
  "validityHours": 72
}
```

**ملاحظة:** التواريخ مش قابلة للتعديل (المعلم لا يقترح مواعيد أصلاً).

**Response 200:** يعيد كامل `TeacherOfferDetailDto` — انظر شكل response الخاص بـ `POST /Offers` أعلاه. الفروقات بعد التحديث: `version` يزيد بـ 1، `expiresAt` تُعاد بناءً على `validityHours` الجديد.

**Response 409 (العرض ليس في حالة Pending):**
```json
{ "succeeded": false, "message": "OFFER_NOT_PENDING" }
```

**Side Effects:**
- `version` يزيد بـ 1.
- رسالة System تُضاف للشات بنوع `OfferUpdate`: "تم تحديث العرض - السعر الجديد: 550 ر.س"
- إعادة عدّاد الصلاحية (`expiresAt` يُحدَّث إذا أُرسل `validityHours`).
- بريد إلكتروني للطالب: "تحديث على عرض معلم".

---

#### `POST /Offers/{id}/withdraw`
**الوصف:** سحب عرض. الجسم اختياري كلياً.

**Request Body (اختياري):**
```json
{ "reason": "اعتذر، التزامات طارئة" }
```

أو يمكن إرسال body فارغ `{}` أو `null`.

**Response 200:**
```json
{ "succeeded": true, "message": "تم سحب العرض" }
```

**القواعد:**
- مسموح فقط إذا `status == Pending` → غير ذلك `409 OFFER_NOT_PENDING`.
- بعد السحب، يمكن للمعلم تقديم عرض جديد على نفس الطلب (الـ unique index مفلتر على `Status != Withdrawn`).
- بريد إلكتروني للطالب + رسالة System في الشات: "تم سحب العرض".

---

#### `GET /Offers/my`
**الوصف:** قائمة عروض المعلم.

**Query Parameters:**
| المعامل | القيم |
|---------|------|
| `status` | حذف الباراميتر = الكل، أو واحدة من: `Pending` / `Accepted` / `Rejected` / `AutoRejected` / `Withdrawn` / `Expired` |
| `dateFrom` / `dateTo` | فلتر على `createdAt` |
| `pageNumber` / `pageSize` | افتراضي 1 / 20. حد أقصى 50 |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "id": 892,
        "offerNumber": "OF-2026-0892",
        "sessionRequestId": 145,
        "subjectId": 5,
        "subjectNameEn": "Mathematics",
        "subjectNameAr": "الرياضيات",
        "studentId": 88,
        "studentDisplayName": "سارة محمد",
        "price": 600,
        "sessionsCount": 5,
        "status": "Pending",
        "version": 1,
        "createdAt": "2026-05-17T11:45:00Z",
        "expiresAt": "2026-05-19T11:45:00Z",
        "unreadMessagesCount": 3
      }
    ],
    "totalCount": 23,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 2
  }
}
```

> `offerNumber` هو خاصية محسوبة في الـ DTO من `createdAt.Year` + `id` بشكل `OF-{year}-{id:D4}`.

---

#### `GET /Offers/{id}`
**الوصف:** تفاصيل عرض محدد للمعلم (مالك العرض فقط).

**Response 200:** يعيد `TeacherOfferDetailDto` بنفس شكل `POST /Offers` response، بالإضافة إلى:
- `request`: شكل `TeacherAvailableRequestDetailDto` كامل لسياق الطلب (نفس شكل `GET /AvailableRequests/{id}`).
- `conversationId`: id المحادثة إن وُجدت، أو `null`.

**Response 404 (العرض غير موجود أو ليس مملوكاً للمعلم):**
```json
{ "succeeded": false, "message": "Offer not found." }
```

---

### 5.3 المحادثات (Conversations)

> **Base URL:** `/Api/V1/Conversations`

#### `GET /by-request/{requestId}/teacher/{teacherId}`
**الوصف:** إيجاد أو إنشاء محادثة لزوج (طلب، معلم). كلا الطرفين يستدعي نفس الـ endpoint — الـ access guard يقرر دور المتصل من الـ JWT:
- إذا كان المتصل معلماً وُيطابق `teacherId` في الـ URL → `caller = Teacher`.
- إذا كان المتصل هو `request.RequestedByUserId` (سواء الطالب أو ولي الأمر) → `caller = Student`.
- غير ذلك → `403 NOT_A_PARTICIPANT`.

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "conversationId": 412,
    "offerId": 892,
    "participants": [
      { "userId": 88, "displayName": "سارة محمد", "role": "Student" },
      { "userId": 17, "displayName": "د. أحمد العلي", "role": "Teacher" }
    ],
    "lastMessageAt": "2026-05-17T14:30:00Z",
    "unreadCount": 3
  }
}
```

> `offerId == 0` يعني لا يوجد عرض بعد — هذه محادثة تمهيدية (شات قبل تقديم العرض). الواجهة تخفي البطاقة الثابتة لملخص العرض في هذه الحالة.

---

#### `GET /{conversationId}/messages`
**الوصف:** الحصول على الرسائل (Pagination بـ Cursor).

**Query Parameters:**
| المعامل | القيمة |
|---------|--------|
| `cursor` | ISO-8601 timestamp من حقل `sentAt` للرسالة الحدّ الأخير في الصفحة السابقة. حذف الباراميتر = أول صفحة |
| `take` | 50 (افتراضي). الحد الأقصى 200 |
| `direction` | `older` (افتراضي — للسحب نحو الأعلى) أو `newer` (لجلب رسائل جديدة بعد آخر cursor) |

**Response 200:**
```json
{
  "succeeded": true,
  "data": {
    "messages": [
      {
        "id": 7001,
        "type": "Text",
        "senderUserId": 88,
        "senderDisplayName": "سارة محمد",
        "senderRole": null,
        "content": "هل ممكن خصم؟",
        "sentAt": "2026-05-17T14:25:00Z"
      },
      {
        "id": 7002,
        "type": "System",
        "senderUserId": null,
        "senderDisplayName": null,
        "senderRole": null,
        "content": "تم تحديث العرض - السعر الجديد: 550 ر.س",
        "sentAt": "2026-05-17T14:28:00Z"
      }
    ],
    "nextCursor": "2026-05-17T14:25:00.0000000Z",
    "hasMore": true
  }
}
```

> ملاحظات: 
> - `senderUserId == null` يعني رسالة نظام تلقائية.
> - `readAt` لكل رسالة **غير موجود** — الـ read receipts تُتبع على مستوى المحادثة (`TeacherLastReadAt`, `StudentLastReadAt`) لا على مستوى الرسالة.
> - `senderRole` يُملأ فقط في الـ response الفوري لـ `POST /messages`، وليس في الـ list (مكلف للحساب لكل صف).

---

#### `POST /{conversationId}/messages`
**Request Body:**
```json
{ "content": "ماشي، موافق على 550 ر.س" }
```

**Response 201:**
```json
{
  "succeeded": true,
  "message": "Created",
  "data": {
    "id": 7003,
    "type": "Text",
    "senderUserId": 17,
    "senderDisplayName": "د. أحمد العلي",
    "senderRole": "Teacher",
    "content": "ماشي، موافق على 550 ر.س",
    "sentAt": "2026-05-17T14:30:00Z"
  }
}
```

**Side Effects:**
- زيادة `lastMessageAt` على المحادثة (لـ sort الـ list).
- بريد إلكتروني للطرف الآخر: "رسالة جديدة على محادثة عرضك" — يستخدم `IRabbitMQService.QueueEmailAsync`.
- ⚠️ لا يوجد SignalR — الطرف الآخر يجلب الرسائل عند Polling.

---

#### `POST /{conversationId}/read`
**الوصف:** تمييز الرسائل كمقروءة (يضبط `TeacherLastReadAt` أو `StudentLastReadAt` حسب دور المتصل، إلى `UtcNow`).

**Request Body (اختياري — حالياً يُتجاهَل):**
```json
{ "upToMessageId": 7002 }
```

> ملاحظة: في v1، الـ mark-read يضبط الـ timestamp على `UtcNow` مباشرة بدون استخدام `upToMessageId`. الحقل محفوظ في الـ DTO للتوسع لاحقاً.

**Response 200:**
```json
{ "succeeded": true, "message": "تم تحديث حالة القراءة" }
```

**Response 403:** `NOT_A_PARTICIPANT` إذا المستخدم ليس معلم العرض ولا الطالب/الولي.

---

### 5.4 الجلسات بعد القبول (Scheduled Sessions) `[v1.5 — P6 + P7]`

#### `GET /MySessions`
**الحالة:** **غير منفّذ في v1.** يتطلب P6 (Accept) + P7 (Payment + Scheduling) أولاً. الشكل المتوقع عند التنفيذ:

```json
{
  "succeeded": true,
  "data": {
    "items": [
      {
        "scheduledSessionId": 1,
        "source": "Scenario2",
        "sessionRequestId": 145,
        "title": "مراجعة الفصل الأول - التفاضل",
        "studentDisplayName": "سارة محمد",
        "scheduledAt": "2026-05-19T16:00:00Z",
        "durationMinutes": 60,
        "zoomJoinUrl": null,
        "zoomStartUrl": null,
        "status": "Scheduled"
      }
    ]
  }
}
```

> Zoom integration نفسها مؤجَّلة — في v1 + v1.5 ستكون الحقول `null`. ستعمل عند ربط `IVideoConferencingService`.

---

## 6. SignalR Hubs `[v1.5 — مؤجَّل]`

**الحالة في v1:** SignalR **غير منفّذ.** الواجهة تستخدم Polling لتحديث الـ inbox والمحادثات. الإشعارات تذهب عبر RabbitMQ → email queue.

**الخطة عند تنفيذ v1.5** (للسياق فقط):

- `NotificationsHub` (`/hubs/notifications`): اشتراك تلقائي بمجموعة `user:{userId}`. أحداث: `RequestPublished`, `OfferAccepted`, `OfferRejected`, `OfferAutoRejected`, `OfferExpired`, `RequestCancelled`, `SessionReminder`.
- `ConversationsHub` (`/hubs/conversations`): اشتراك يدوي بـ `JoinConversation(conversationId)`. أحداث: `MessageReceived`, `MessageRead`, `OfferUpdated`.

> ⚠️ ملاحظة للواجهة: حتى لا تتم إعادة كتابة المنطق عند وصول v1.5، أبنِ طبقة `notification client` تجريدية يمكن تبديل تنفيذها من Polling إلى SignalR لاحقاً.

---

## 7. الإشعارات (Notifications)

### قنوات الإيصال للمعلم (الحالة الفعلية في v1)

| الحدث | Real-time | Push | Email | In-App |
|------|-----------|------|-------|--------|
| طلب جديد مطابق | `[v1.5]` | `[v1.5]` | ✓ | ✓ |
| رسالة جديدة من طالب | `[v1.5]` | `[v1.5]` | ✓ | ✓ |
| قبول عرضي `[P6]` | `[v1.5]` | `[v1.5]` | ✗ | ✗ |
| رفض عرضي صراحةً `[P6]` | `[v1.5]` | `[v1.5]` | ✗ | ✗ |
| رفض تلقائي `[P6]` | `[v1.5]` | `[v1.5]` | ✗ | ✗ |
| انتهاء صلاحية عرضي | `[v1.5]` | `[v1.5]` | ✓ | ✓ |
| تذكير قبل انتهاء الصلاحية (4 ساعات) | `[v1.5]` | `[v1.5]` | `[v1.5]` | `[v1.5]` |
| تذكير قبل جلسة (30 دقيقة) `[P7]` | `[v1.5]` | `[v1.5]` | ✗ | ✗ |
| إلغاء الطالب للطلب بعد قبول `[P6]` | `[v1.5]` | `[v1.5]` | ✗ | ✗ |

> **مفاتيح:**
> - ✓ = منفّذ ويعمل في v1.
> - ✗ = القناة عملياً غير مفعَّلة لهذا الحدث (الـ event نفسه قد يكون مؤجلاً، انظر `[P6]`/`[P7]`).
> - `[v1.5]` = القناة (SignalR/Push) كلها مؤجلة لـ v1.5.
> - `[P6]`/`[P7]` = الحدث نفسه يعتمد على مرحلة لم تُنفَّذ بعد.

### قوالب الإيميل المنفّذة في v1

**1. طلب جديد (`OpenSessionRequestTargetingService`):**
- **Subject:** "طلب جلسات جديد مطابق لتخصصك"
- **Body:** "يوجد طلب جلسات جديد مطابق لتخصصك. افتح لوحة \"الطلبات الجديدة\" لعرض التفاصيل وتقديم عرضك."

**2. عرض جديد للطالب (`CreateSessionOfferCommandHandler`):**
- **Subject:** "عرض جديد على طلب جلساتك"
- **Body:** "وصلك عرض جديد من معلم. افتح قائمة \"العروض\" لمراجعة التفاصيل."

**3. تحديث عرض للطالب (`UpdateSessionOfferCommandHandler`):**
- **Subject:** "تحديث على عرض معلم"
- **Body:** نص الرسالة System (مثلاً: "تم تحديث العرض - السعر الجديد: 550 ر.س")

**4. سحب عرض (`WithdrawSessionOfferCommandHandler`):**
- **Subject:** "تم سحب عرض على طلب جلساتك"
- **Body:** "قام أحد المعلمين بسحب عرضه. افتح قائمة العروض لرؤية ما تبقى من عروض."

**5. انتهاء صلاحية عرض (`SessionOfferExpirationService` background sweep):**
- إيميل للمعلم: "انتهت صلاحية عرضك"
- إيميل للطالب: "انتهت صلاحية عرض على طلب جلساتك"

**6. رسالة شات جديدة (`PostConversationMessageCommandHandler`):**
- **Subject:** "رسالة جديدة على محادثة عرضك"
- **Body:** "وصلتك رسالة جديدة. افتح المحادثة لقراءتها والرد عليها."

> ⚠️ قوالب القبول والرفض التلقائي للعرض (`OfferAccepted`, `OfferAutoRejected`) ستُضاف عند تنفيذ P6.

---

## 8. الصلاحيات والقيود الأمنية

### Authorization (الحالة الفعلية في v1)

كل الـ controllers الخاصة بالمعلم تحمل:

```csharp
[Authorize(Roles = Roles.Teacher)]
[ApiController]
[Route(Router.TeacherAvailableRequests)]
public class TeacherAvailableRequestsController : AppControllerBase { ... }
```

> ⚠️ **ليس هناك `[Authorize(Policy = "ActiveTeacherOnly")]` كـ policy مسجلة.** الفحص يتم **inline داخل كل handler** — كل handler يبدأ بـ:
> ```csharp
> var teacher = await _teacherRepo.GetByUserIdAsync(request.UserId);
> if (teacher == null || teacher.Status != TeacherStatus.Active)
>     return Unauthorized<...>("Teacher account not active.");
> ```
> يُمكن ترقيتها إلى `IAuthorizationHandler` مسجَّل لاحقاً بدون تغيير المنطق.

**Controller الـ Conversations** يحمل `[Authorize]` فقط (بدون role) لأن الطرفين (المعلم + الطالب/الولي) يستخدمونه — الـ access guard يُحلّ الدور من الـ JWT داخل الـ handler عبر `IOfferConversationRepository.ResolveParticipantAsync`.

### قواعد إضافية مفروضة في الـ handlers

1. **`POST /Offers`:**
   - وجود `OpenSessionRequestTarget` row لزوج (الطلب، المعلم) — غير ذلك → `403 NOT_MATCHED`.
   - حالة الطلب `Active` أو `ReceivingOffers` — غير ذلك → `409 REQUEST_NOT_ACTIVE`.
   - لا يوجد عرض سابق غير منسحب (`Status != Withdrawn`) — غير ذلك → `409 DUPLICATE_OFFER` مع `meta.existingOfferId`.

2. **`PUT /Offers/{id}` و `POST /Offers/{id}/withdraw`:**
   - الـ offer مملوك للمعلم — مفروض داخل `IOpenSessionOfferRepository.GetByIdForOwnerActionAsync(offerId, teacherId)` الذي يرشّح `o.TeacherId == teacher.Id` — غير ذلك → `404 Offer not found`.
   - حالة العرض `Pending` — غير ذلك → `409 OFFER_NOT_PENDING`.

3. **`GET /Conversations/{id}/messages` (وكل endpoints الشات):**
   - المتصل participant في المحادثة — مفروض داخل `ResolveParticipantAsync`:
     - إذا `conv.Teacher.UserId == currentUserId` → دور Teacher.
     - أو إذا `request.RequestedByUserId == currentUserId` → دور Student (يغطي الطالب نفسه والولي الذي أنشأ الطلب).
     - أو إذا `request.CreatedByGuardian.UserId == currentUserId` → دور Student.
   - غير ذلك → `403 NOT_A_PARTICIPANT`.

4. **محتوى الرسائل:**
   - لا فلتر آلي للكلمات المسيئة في v1 — `[v2]`.
   - لا يتم تضمين بيانات اتصال كاملة (هاتف، إيميل) في `OfferConversationMessageDto` — فقط `senderDisplayName` المركّب من `FirstName + LastName`. الواجهة لا يجب أن تعرض إيميل أو هاتف للطرف الآخر.

---

## 9. حالات الخطأ المحتملة (Error Codes)

### الكودات المنفّذة في v1

| الكود | المعنى | HTTP Status | يَصدر من |
|------|---------|-------------|---------|
| `NOT_MATCHED` | المعلم ليس له `OpenSessionRequestTarget` row على هذا الطلب | 403 | كل endpoints الـ AvailableRequests + `POST /Offers` |
| `NOT_A_PARTICIPANT` | المستخدم ليس طرفاً في المحادثة (لا معلم العرض ولا الطالب/الولي) | 403 | كل endpoints الـ Conversations |
| `DUPLICATE_OFFER` | يوجد عرض سابق غير منسحب من نفس المعلم على نفس الطلب — الـ response يتضمن `meta.existingOfferId` و `meta.existingOfferStatus` | 409 | `POST /Offers` |
| `REQUEST_NOT_ACTIVE` | الطلب ليس في حالة Active/ReceivingOffers (مثلاً Cancelled، OfferAccepted، Expired) | 409 | `POST /Offers` |
| `OFFER_NOT_PENDING` | العرض ليس في حالة Pending — يحدث للعروض المنسحبة، المنتهية، المقبولة، المرفوضة | 409 | `PUT /Offers/{id}` + `POST /Offers/{id}/withdraw` |
| `OFFER_ALREADY_SUBMITTED` | محاولة dismiss طلب سبق تقديم عرض عليه | 400 | `POST /AvailableRequests/{id}/dismiss` |
| `MUST_BE_POSITIVE` | السعر يجب أن يكون أكبر من صفر | 400 (FluentValidation) | `POST/PUT /Offers` |
| `INVALID_VALIDITY_HOURS` | مدة الصلاحية خارج النطاق [24, 168] | 400 (FluentValidation) | `POST/PUT /Offers` |
| `COMMITMENT_NOT_CONFIRMED` | `commitmentConfirmed != true` | 400 (FluentValidation) | `POST /Offers` |

### كودات في الـ doc الأصلي لكنها غير منفّذة كما هي

| الكود الأصلي | الحالة الفعلية |
|---|---|
| `NOT_IN_AVAILABILITY` | **حذف.** المعلم لا يقترح مواعيد، ولا يوجد `schedules[]` validation في `POST /Offers`. |
| `OFFER_EXPIRED` | **غير مميَّز.** الـ handler يكتشف `Status != Pending` ويعيد `OFFER_NOT_PENDING` بدلاً من تمييز الحالات المنتهية صراحةً. |
| `TEACHER_SUSPENDED` | **غير مميَّز كـ 403.** أي تعطيل (Suspended, Inactive, AwaitingDocuments) يعيد `401 "Teacher account not active."` |
| `OFFER_ALREADY_ACCEPTED_BY_STUDENT` | `[v1.5 — P6]` — يصدر عند تنفيذ تدفق القبول/الرفض التلقائي. |

### حالات HTTP إضافية يجب أن تتعامل معها الواجهة

- `401 Unauthorized` — مع رسالة "Teacher account not active." (يغطي حالات Suspended/Inactive/AwaitingDocuments + المعلم غير موجود).
- `404 Not Found` — العرض / المحادثة / الطلب غير موجود أو ليس مملوكاً للمعلم.
- `400 Bad Request` — أخطاء validation عامة من FluentValidation، شكل `errors[]` فيها سلاسل بسيطة.

---

## 10. ملحقات

### 10.1 ترقيم العروض

- **العرض هو `int Id`** على مستوى الـ DB (مثلاً `892`).
- النمط `OF-{Year}-{Id:D4}` (مثلاً `OF-2026-0892`) هو **سلسلة عرض مشتقّة** يبنيها الواجهة من `createdAt.Year` + `id`. لا يوجد عمود مستقل لها في الـ DB.
- في `TeacherOfferListItemDto` يمكن إضافة getter محسوب يسمى `offerNumber` لتسهيل الـ binding — حالياً الواجهة تبنيها بنفسها.

### 10.2 مدة الصلاحية

- الافتراضي: 48 ساعة من وقت الإنشاء.
- النطاق المسموح: [24, 168] ساعة (1 إلى 7 أيام).
- محسوب من قِبل الـ handler كـ `ExpiresAt = UtcNow + validityHours.Hours`. عند `PUT /Offers/{id}` مع `validityHours` جديدة، تُعاد بناءً على `UtcNow` (وليس وقت الإنشاء الأصلي).

### 10.3 تنبيه قبل انتهاء الصلاحية `[v1.5]`

- **غير منفّذ في v1.** الخطة:
  - 4 ساعات قبل الانتهاء → إشعار للمعلم (Push + Email).
  - 1 ساعة قبل الانتهاء → إشعار للطالب.
- يتطلب:
  - عمود `ExpiryReminderSentAt DateTime?` على `OpenSessionOffer` (للـ idempotency)، إضافة في migration لاحقة.
  - `BackgroundService` جديد `SessionOfferExpiryReminderService` يعمل كل ساعة.

### 10.4 Background Jobs المتعلقة بالمعلم

- ✅ **`SessionOfferExpirationService`** (منفّذ): يُشغَّل كل 15 دقيقة (configurable عبر `OpenSessionOfferSettings:ExpirationCheckIntervalMinutes` في `appsettings.json`). يحوّل العروض المنتهية صلاحياً (`Status == Pending && ExpiresAt < UtcNow`) إلى `Expired`، ويُرسل إيميل للمعلم + للطالب.
- 🚧 **`SessionOfferExpiryReminderService`** `[v1.5]`: لتنبيه ما قبل الانتهاء (انظر 10.3).

### 10.5 إعدادات appsettings.json

```json
{
  "OpenSessionOfferSettings": {
    "ExpirationCheckIntervalMinutes": 15
  }
}
```

### 10.6 الكيانات الأساسية (للمرجع)

| الكيان | الجدول | المفتاح الفريد المهم |
|---|---|---|
| `OpenSessionRequest` | `sr.SessionRequests` | — |
| `OpenSessionRequestTarget` | `sr.SessionRequestTargets` | `(SessionRequestId, TeacherId)` |
| `OpenSessionOffer` | `sr.SessionOffers` | `(SessionRequestId, TeacherId) WHERE Status != Withdrawn` (فلترّ) |
| `OfferConversation` | `sr.OfferConversations` | `(SessionRequestId, TeacherId)` |
| `OfferMessage` | `sr.OfferMessages` | — |

### 10.7 الحالات المؤجلة (P6 + P7 + v1.5)

| البند | المتطلب |
|---|---|
| قبول/رفض/رفض تلقائي للعرض | P6 — `AcceptSessionOfferCommand`, تدفق `AutoReject` للآخرين |
| الدفع والجدولة التلقائية | P7 — يستفيد من `IEnrollmentApprovalService` الموجود |
| `GET /Teacher/MySessions` | يعتمد على P6 + P7 |
| إشعارات القبول/الرفض | تتولد من handler P6 |
| SignalR Hubs | v1.5 |
| Push notifications | يتطلب جدول DeviceToken جديد |
| تذكير ما قبل انتهاء الصلاحية | 10.3 + 10.4 |
| فلتر الكلمات المسيئة في الشات | v2 |
| إرسال صور/مرفقات في الشات | v2 |
| Zoom auto-create | يتطلب `IVideoConferencingService` |

---

**نهاية ملف دور المعلم — متوافق مع code state اعتباراً من 2026-06-03.**
