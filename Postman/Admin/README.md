# Admin - Teacher Management API Collection

This Postman collection contains all the Admin endpoints for managing teacher verification and documents.

## 🔐 Authentication

All endpoints require **Admin** or **SuperAdmin** role authentication.

### Setup

1. **Login as Admin** (use the Authentication collection)
2. **Copy the access token** from the login response
3. **Set the `admin_access_token` variable** in the collection variables

## 📁 Collection Structure

### Queries (Read Operations)
- **Get Pending Teachers** - Paginated list of teachers pending verification
- **Get Teacher Details** - Detailed teacher information with all documents

### Commands (Write Operations)
- **Approve Document** - Approve a specific document
- **Reject Document** - Reject a document with reason
- **Block Teacher** - Block a teacher account

## 🔧 Variables

Configure these variables in the collection:

| Variable | Description | Example |
|----------|-------------|---------|
| `base_url` | API base URL | `http://localhost:5000` |
| `admin_access_token` | JWT token from admin login | Bearer token |
| `teacher_id` | Teacher ID for testing | `1` |
| `document_id` | Document ID for testing | `1` |

## 📝 Workflow Example

### 1. Get Pending Teachers
```
GET /Api/V1/Admin/TeacherManagement/Pending?pageNumber=1&pageSize=10
```

### 2. Get Teacher Details
```
GET /Api/V1/Admin/TeacherManagement/1
```

### 3. Review Documents
- Identity Document
- Academic Certificates

### 4. Take Action

**Option A: Approve Document**
```
POST /Api/V1/Admin/TeacherManagement/1/Documents/1/Approve
```

**Option B: Reject Document**
```
POST /Api/V1/Admin/TeacherManagement/1/Documents/1/Reject
Body: { "reason": "Document is unclear" }
```

**Option C: Block Teacher**
```
POST /Api/V1/Admin/TeacherManagement/1/Block
Body: { "reason": "Fraudulent documents" }
```

## 🌐 Localization

All endpoints support localization via the `Accept-Language` header:
- `ar-EG` - Arabic (Egypt)
- `en-US` - English (US)

## ✅ Response Format

All endpoints return the standard `Response<T>` format:

```json
{
  "statusCode": 200,
  "succeeded": true,
  "message": "Success message",
  "data": { ... },
  "errors": [],
  "meta": null
}
```

## 🎯 CQRS Pattern

This collection follows the CQRS (Command Query Responsibility Segregation) pattern:

- **Queries** - Read operations (GET)
  - `GetPendingTeachersQuery`
  - `GetTeacherDetailsQuery`

- **Commands** - Write operations (POST)
  - `ApproveDocumentCommand`
  - `RejectDocumentCommand`
  - `BlockTeacherCommand`

## 🔍 Testing

1. Ensure you have at least one teacher with pending documents
2. Use the login endpoint to get an admin token
3. Test each endpoint in sequence
4. Verify responses match expected format

## 🚀 Production URL

Update the `base_url` variable for production:
```
http://qalam.runasp.net
```
