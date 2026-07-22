# Session Handoff: Enrollment Request Module Implementation

This document summarizes the Enrollment Request module generated after the authentication implementation work. The module follows the existing Clean Architecture layout and uses the finalized database schema without adding new tables.

---

## 1. Completed in This Session

### A. Application Constants Added
Added constants under `src/CodeForge.Application/Common/Constants/`:

* `Roles`
  * `admin`
  * `instructor`
  * `student`
* `EnrollmentRequestStatuses`
  * `pending`
  * `approved`
  * `rejected`
* `EnrollmentStatuses`
  * `active`
  * `expired`

These reduce string duplication in auth/enrollment flows.

### B. Application Service Abstractions Added
Added interfaces under `src/CodeForge.Application/Common/Interfaces/`:

* `IFileStorageService`
  * Saves payment proof uploads.
* `ITemporaryPasswordGenerator`
  * Generates temporary student passwords during approval.
* `IEnrollmentNotificationService`
  * Generates approval/rejection notifications.

Note: The finalized schema has no notifications table, so notifications are currently represented through this service abstraction.

### C. Enrollment Request DTOs Added
Added DTOs under `src/CodeForge.Application/EnrollmentRequests/Common/`:

* `EnrollmentRequestDto`
* `EnrollmentRequestDetailDto`
* `EnrollmentApprovalResultDto`
* `EnrollmentRequestMessageDto`

### D. Public Submit Flow Added
Added under `src/CodeForge.Application/EnrollmentRequests/SubmitEnrollmentRequest/`:

* `SubmitEnrollmentRequestCommand`
* `SubmitEnrollmentRequestCommandValidator`
* `SubmitEnrollmentRequestCommandHandler`

Behavior:
* Validates visitor name, email, phone, course, payment method, and proof file.
* Accepts proof files with content types:
  * `image/jpeg`
  * `image/png`
  * `image/webp`
  * `application/pdf`
* Verifies selected course exists.
* Saves payment proof through `IFileStorageService`.
* Creates `EnrollmentRequest` with `pending` status.

### E. Admin Query Flow Added
Added list query under `src/CodeForge.Application/EnrollmentRequests/GetEnrollmentRequests/`:

* `GetEnrollmentRequestsQuery`
* `GetEnrollmentRequestsQueryValidator`
* `GetEnrollmentRequestsQueryHandler`

Supports optional filters:
* `status`
* `courseId`

Added detail query under `src/CodeForge.Application/EnrollmentRequests/GetEnrollmentRequestById/`:

* `GetEnrollmentRequestByIdQuery`
* `GetEnrollmentRequestByIdQueryValidator`
* `GetEnrollmentRequestByIdQueryHandler`

Detail includes:
* applicant data
* course title
* payment proof URL
* review data
* rejection reason
* resulting enrollment id, when approved

### F. Admin Approval Flow Added
Added under `src/CodeForge.Application/EnrollmentRequests/ApproveEnrollmentRequest/`:

* `ApproveEnrollmentRequestCommand`
* `ApproveEnrollmentRequestCommandValidator`
* `ApproveEnrollmentRequestCommandHandler`

Behavior:
* Requires pending request.
* Resolves current admin from `ICurrentUserService`.
* Finds applicant user by email.
* If no user exists:
  * creates student account
  * generates temporary password
  * hashes password
  * sets role to `student`
  * sets `MustChangePassword = true`
* Rejects approval if matching user exists but is inactive.
* Rejects approval if student is already enrolled in the selected course.
* Creates `Enrollment` with `active` status.
* Uses provided expiration date or defaults to one year from approval.
* Updates request status to `approved`.
* Stores reviewer and reviewed timestamp.
* Clears any rejection reason.
* Writes an `ActivityLog` row.
* Calls `IEnrollmentNotificationService.NotifyEnrollmentApprovedAsync(...)`.

### G. Admin Rejection Flow Added
Added under `src/CodeForge.Application/EnrollmentRequests/RejectEnrollmentRequest/`:

* `RejectEnrollmentRequestCommand`
* `RejectEnrollmentRequestCommandValidator`
* `RejectEnrollmentRequestCommandHandler`

Behavior:
* Requires pending request.
* Stores rejection reason.
* Updates request status to `rejected`.
* Stores reviewer and reviewed timestamp.
* Writes an `ActivityLog` row.
* Calls `IEnrollmentNotificationService.NotifyEnrollmentRejectedAsync(...)`.

### H. Infrastructure Services Added
Added under `src/CodeForge.Infrastructure/EnrollmentRequests/`:

* `LocalFileStorageService`
  * Saves payment proof files to:
    `wwwroot/uploads/payment-proofs`
  * Returns URLs like:
    `/uploads/payment-proofs/{fileName}`
* `TemporaryPasswordGenerator`
  * Generates 14-character temporary passwords using `RandomNumberGenerator`.
* `LoggingEnrollmentNotificationService`
  * Logs approval/rejection notifications until a real email/notification provider exists.

Updated `src/CodeForge.Infrastructure/DependencyInjection.cs` to register:

```csharp
services.AddSingleton<IFileStorageService, LocalFileStorageService>();
services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
services.AddScoped<IEnrollmentNotificationService, LoggingEnrollmentNotificationService>();
```

### I. API Controller Added
Added `src/CodeForge.Api/Controllers/EnrollmentRequestsController.cs`.

Exposed endpoints:

* `POST /enrollment-requests`
  * Public
  * Multipart form-data
  * Uploads payment proof
* `GET /enrollment-requests`
  * Admin only
  * Optional query filters: `status`, `courseId`
* `GET /enrollment-requests/{id}`
  * Admin only
* `PUT /enrollment-requests/{id}/approve`
  * Admin only
* `PUT /enrollment-requests/{id}/reject`
  * Admin only

Controller includes Swagger metadata:
* XML-style summaries
* `ProducesResponseType`
* `Consumes("multipart/form-data")` for upload
* `RequestSizeLimit(10_000_000)` for payment proof uploads

### J. Authorization Policy Added
Updated `src/CodeForge.Api/Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
});
```

Also added:

```csharp
app.UseStaticFiles();
```

This allows stored payment proof URLs under `wwwroot` to be served.

### K. HTTP Scratch File Updated
Updated `src/CodeForge.Api/CodeForge.Api.http` with examples for:

* Submit enrollment request
* List pending requests
* Get request details
* Approve request
* Reject request

---

## 2. Current Behavior

### Visitor Submit
Visitor submits:
* full name
* email
* phone number
* course id
* payment method
* payment proof file

System:
* stores uploaded proof locally
* creates an `enrollment_requests` row
* sets status to `pending`

### Admin Review
Admin can:
* list all requests
* filter by status/course
* view request details and payment proof URL

### Admin Approval
System:
* creates student account if needed
* generates temporary password only for newly created students
* creates enrollment
* sets expiration date
* marks request approved
* writes activity log
* generates notification through logging service

### Admin Rejection
System:
* marks request rejected
* stores rejection reason
* writes activity log
* generates notification through logging service

---

## 3. Verification

The solution builds cleanly:

```text
dotnet build CodeForge.slnx
Build succeeded.
0 Warning(s)
0 Error(s)
```

No automated tests currently exist.

---

## 4. Important Notes

### A. Notifications Are Logged Only
The finalized schema does not include a notifications table, and there is no email provider yet. `LoggingEnrollmentNotificationService` logs generated notifications.

Recommended production next step:
* Add email provider integration, or
* Add a notifications table/schema if in-app notifications are required.

### B. Payment Proofs Use Local Disk
Payment proofs are stored in `wwwroot/uploads/payment-proofs`.

Recommended production next step:
* Move storage to object storage such as S3/R2/Azure Blob.
* Add private access or signed URLs if payment proofs should not be publicly accessible.

### C. No Rate Limiting Yet
`POST /enrollment-requests` is public. The SRS calls out rate limiting as a launch concern.

Recommended production next step:
* Add rate limiting middleware for public endpoints.

### D. Approval Uses One-Year Default Expiration
If admin does not pass `accessExpiresAt`, approval defaults to:

```csharp
DateTime.UtcNow.AddYears(1)
```

This can be changed later to a configurable setting.

### E. Activity Log Metadata Uses JSON
Approval/rejection handlers write `ActivityLog.Metadata` as `JsonDocument`.

### F. Existing User Flow
If a user with the applicant email already exists:
* no temporary password is generated
* no password is changed
* enrollment is created if not already enrolled

If the user exists but is inactive, approval is rejected.

---

## 5. Recommended Next Steps

1. Add tests for:
   * submit request
   * missing/invalid payment proof
   * admin list filters
   * detail not found
   * approval creates student
   * approval uses existing student
   * duplicate enrollment rejection
   * rejection stores reason
2. Add centralized API exception handling instead of controller-local exception handling.
3. Add real email/notification implementation.
4. Move payment proof upload storage to production storage.
5. Add rate limiting on public enrollment request endpoint.
6. Add config for default enrollment duration.
7. Consider making payment proof URLs private/signed.

