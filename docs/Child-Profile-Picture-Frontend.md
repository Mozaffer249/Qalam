# Child Profile Picture — Frontend Guide

Guardian-only. Photo is stored on the child’s linked user (`User.ProfilePictureUrl`). Upload is async (queued to OSS); refresh the children list after upload.

## Endpoints

| Action | Method | Path | Auth | Body |
|--------|--------|------|------|------|
| List children | `GET` | `/Api/V1/Student/MyChildren` | Bearer + **Guardian** | — |
| Update child | `PUT` | `/Api/V1/Student/Children/{studentId}` | Bearer + **Guardian** | JSON (no photo) |
| Set / replace photo | `PUT` | `/Api/V1/Student/Children/{studentId}/ProfilePicture` | Bearer + **Guardian** | multipart `file` |
| **Add child** | `POST` | `/Api/V1/Authentication/Student/AddChild` | Bearer + **Guardian** | **JSON** *or* **multipart** |

## Add child

Same URL supports both content types.

### Option A — JSON (no photo)

```http
POST /Api/V1/Authentication/Student/AddChild
Content-Type: application/json
Authorization: Bearer <token>

{
  "child": {
    "fullName": "Ahmed",
    "email": "ahmed@example.com",
    "password": "Passw0rd!",
    "confirmPassword": "Passw0rd!",
    "dateOfBirth": "2015-06-01",
    "gender": "Male",
    "guardianRelation": "Father"
  }
}
```

Then optionally upload photo with `PUT …/ProfilePicture` (see below).

### Option B — Multipart (fields + optional photo) ✅ preferred when picking a photo

```http
POST /Api/V1/Authentication/Student/AddChild
Content-Type: multipart/form-data
Authorization: Bearer <token>
```

| Field | Required | Notes |
|-------|----------|--------|
| `Child.FullName` | yes | |
| `Child.Email` | yes | |
| `Child.Password` | yes | |
| `Child.ConfirmPassword` | yes | |
| `Child.DateOfBirth` | yes | `yyyy-MM-dd` |
| `Child.Gender` | no | e.g. `Male` / `Female` |
| `Child.GuardianRelation` | no | e.g. `Father` |
| `Child.DomainId` / `CurriculumId` / `LevelId` / `GradeId` | no | education ids |
| `file` | no | jpg / jpeg / png / webp, max **5 MB** |

Invalid `file` → **400** (child is **not** created).

#### Example (JS)

```js
const form = new FormData();
form.append('Child.FullName', fullName);
form.append('Child.Email', email);
form.append('Child.Password', password);
form.append('Child.ConfirmPassword', confirmPassword);
form.append('Child.DateOfBirth', '2015-06-01'); // yyyy-MM-dd
form.append('Child.Gender', 'Male');
form.append('Child.GuardianRelation', 'Father');
if (imageFile) form.append('file', imageFile);

await fetch(`${API}/Api/V1/Authentication/Student/AddChild`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${token}` },
  // do NOT set Content-Type manually — browser sets boundary
  body: form,
});
```

#### Example (Flutter / Dio)

```dart
final map = <String, dynamic>{
  'Child.FullName': fullName,
  'Child.Email': email,
  'Child.Password': password,
  'Child.ConfirmPassword': confirmPassword,
  'Child.DateOfBirth': dateOfBirth, // yyyy-MM-dd
  if (gender != null) 'Child.Gender': gender,
  if (relation != null) 'Child.GuardianRelation': relation,
  if (photoPath != null)
    'file': await MultipartFile.fromFile(photoPath, filename: 'photo.jpg'),
};

await dio.post(
  '/Api/V1/Authentication/Student/AddChild',
  data: FormData.fromMap(map),
  options: Options(headers: {'Authorization': 'Bearer $token'}),
);
```

### Add response

```json
{
  "succeeded": true,
  "message": "Child added successfully.",
  "data": 12
}
```

`data` = new `studentId` (`int`).

---

## Replace photo only

```http
PUT /Api/V1/Student/Children/{studentId}/ProfilePicture
Authorization: Bearer <token>
Content-Type: multipart/form-data

file: <image>
```

| Rule | Value |
|------|--------|
| Form field | `file` (required) |
| Types | `jpg`, `jpeg`, `png`, `webp` |
| Max size | 5 MB |
| Non-owner child | **404** |
| Bad file | **400** |

Success returns the child DTO (`profilePictureUrl` may update after OSS finishes — refresh `MyChildren`).

```js
const form = new FormData();
form.append('file', imageFile);
await fetch(`${API}/Api/V1/Student/Children/${studentId}/ProfilePicture`, {
  method: 'PUT',
  headers: { Authorization: `Bearer ${token}` },
  body: form,
});
```

---

## List children — photo field

`GET /Api/V1/Student/MyChildren` → each item:

```ts
profilePictureUrl?: string | null;
```

Show network avatar when set; otherwise initials / placeholder. Refresh after add/upload (optionally after a short delay).

---

## Do not

- Send photo URL on `PUT …/Children/{studentId}` (JSON update has no image field)
- Clear a photo without uploading a replacement (not supported)
