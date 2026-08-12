// Thin fetch wrapper around the CodeForge ASP.NET API.
// Centralizes the base URL and error shape so feature code stays uniform.
//
// DTO shapes below are aliased from ./api-schema (generated from the backend's own
// OpenAPI document — see scripts/generate-api-types.mjs) rather than hand-mirrored, so
// a backend field rename surfaces as a tsc error here instead of a silent runtime bug.
// Two exceptions, kept hand-written: (1) the union type aliases below (SessionType,
// MaterialType, AssessmentType, AttendanceStatus, CertificateTier) — the backend types
// these fields as plain `string`, so the generated schema can't provide the narrower
// union the UI switches on; these mirror Application/Common/Constants/*.cs, not DTO
// shapes. (2) request-shape parameter objects on functions taking loose input — these
// are inputs to JSON.stringify or FormData, not response DTOs.

import type { components } from "./api-schema";
import { localInputToUtcIso } from "./datetime";

type Schemas = components["schemas"];

/** Envelope shape for the 12 paginated list endpoints — see API_CONVENTIONS.md §6.
 * Hand-written like the union-type aliases above: the backend's generic PagedResult<T>
 * always serializes to this shape regardless of T, so there's nothing tsc-checkable
 * to gain from aliasing each closed-generic schema (e.g. Schemas["CourseListDtoPagedResult"])
 * instead — T itself still comes from the generated schema. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// Browser calls go through Next's own /api/* rewrite (see next.config.mjs) so the
// request stays same-origin and cf_access/cf_refresh ride along as first-party
// cookies. Server components bypass the rewrite and call the API directly — a
// relative path has no base to resolve against outside a browser.
const BASE_URL = typeof window === "undefined"
  ? (process.env.API_INTERNAL_URL ?? "http://localhost:5205")
  : "/api";

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

const CSRF_HEADER_NAME = "X-CSRF-Token";
const SAFE_METHODS = new Set(["GET", "HEAD", "OPTIONS"]);

/** Echoes the non-HttpOnly cf_csrf cookie back as a header for unsafe methods — the
 * double-submit CSRF check CsrfProtectionFilter enforces server-side. No-op outside
 * the browser (no document, and server-side calls carry no auth cookie anyway). */
function csrfHeader(): Record<string, string> {
  if (typeof document === "undefined") return {};
  const match = document.cookie.match(/(?:^|;\s*)cf_csrf=([^;]+)/);
  return match ? { [CSRF_HEADER_NAME]: decodeURIComponent(match[1]) } : {};
}

/** Dispatched on `window` when a refresh attempt gets an explicit non-OK response from
 * the server — a genuinely dead refresh token (revoked, or past its 7-day expiry), not
 * a transient network failure. lib/auth.tsx listens for this to clear the client-side
 * session in place rather than leaving it stale after a refresh that will never
 * succeed. */
export const SESSION_EXPIRED_EVENT = "codeforge:session-expired";

// Concurrent 401s share one in-flight refresh instead of each firing their own —
// the refresh endpoint itself only lets the first request through unscathed (see
// RefreshTokenRotationPolicy on the backend); this just avoids the redundant calls.
let refreshPromise: Promise<boolean> | null = null;

function attemptRefresh(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = fetch(`${BASE_URL}/auth/refresh-token`, {
      method: "POST",
      credentials: "include",
      headers: csrfHeader()
    })
      .then((response) => {
        if (!response.ok && typeof window !== "undefined") {
          window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
        }
        return response.ok;
      })
      // A thrown network error (offline, DNS, etc.) is not proof the refresh token is
      // dead — leave the session alone so a reconnect can recover it on its own.
      .catch(() => false)
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

function canRetryOn401(path: string): boolean {
  return isBrowser() && !path.startsWith("/auth/login") && !path.startsWith("/auth/refresh-token");
}

/** Retries once, after a refresh, on a 401 — everywhere except the login/refresh
 * endpoints themselves (nothing to refresh into on those). */
async function fetchWithAuthRetry(path: string, init: RequestInit): Promise<Response> {
  const response = await fetch(`${BASE_URL}${path}`, init);
  if (response.status === 401 && canRetryOn401(path) && (await attemptRefresh())) {
    // A successful refresh rotates cf_csrf (AuthCookieWriter), so the X-CSRF-Token
    // baked into `init` by the original caller is now stale for unsafe methods —
    // recompute it from the freshly-rotated cookie, or the retry itself 403s against
    // CsrfProtectionFilter instead of succeeding.
    const method = (init.method ?? "GET").toUpperCase();
    const headers = SAFE_METHODS.has(method) ? init.headers : { ...init.headers, ...csrfHeader() };
    return fetch(`${BASE_URL}${path}`, { ...init, headers });
  }
  return response;
}

export interface ApiError {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  /** Stable per-failure codes ("slug_format", "NotEmptyValidator", …), index-aligned
   * with `errors` per key so the frontend can render bilingual copy instead of the
   * server's English messages — see API_CONVENTIONS.md §4 and lib/formErrors.ts. */
  errorCodes?: Record<string, string[]>;
  /** Machine-readable discriminator for errors the frontend must branch on,
   * e.g. "password_change_required" — see API_CONVENTIONS.md §4. */
  code?: string;
}

export class ApiRequestError extends Error {
  constructor(public readonly info: ApiError) {
    super(info.detail ?? info.title ?? `Request failed (${info.status})`);
    this.name = "ApiRequestError";
  }
}

/** Error code the API returns when MustChangePassword blocks the request —
 * see API_CONVENTIONS.md §4. */
export const PASSWORD_CHANGE_REQUIRED_CODE = "password_change_required";

/** Dispatched on `window` whenever a 403 carries PASSWORD_CHANGE_REQUIRED_CODE, so
 * PasswordChangeGate can redirect even when the locally cached session is stale
 * (e.g. it still says mustChangePassword: false). */
export const PASSWORD_CHANGE_REQUIRED_EVENT = "codeforge:password-change-required";

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: Partial<ApiError> = {};
    try {
      body = await response.json();
    } catch {
      /* non-JSON error body */
    }
    const error: ApiError = { status: response.status, ...body };
    if (error.code === PASSWORD_CHANGE_REQUIRED_CODE && typeof window !== "undefined") {
      window.dispatchEvent(new Event(PASSWORD_CHANGE_REQUIRED_EVENT));
    }
    throw new ApiRequestError(error);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { locale?: string; token?: string } = {}
): Promise<T> {
  const { locale, token, headers, method, ...rest } = options;
  const httpMethod = (method ?? "GET").toUpperCase();
  const response = await fetchWithAuthRetry(path, {
    ...rest,
    method,
    credentials: isBrowser() ? "include" : undefined,
    headers: {
      "Content-Type": "application/json",
      ...(locale ? { "Accept-Language": locale } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(SAFE_METHODS.has(httpMethod) ? {} : csrfHeader()),
      ...headers
    }
  });
  return handleResponse<T>(response);
}

/** Fetches a private file (materials, payment proofs) — auth rides along as the
 * cf_access cookie — and opens it in a new tab via a blob URL. These endpoints are
 * never plain <a href> links since the server requires authentication, which only
 * fetch (not a plain navigation) sends. */
export async function downloadAuthenticatedFile(path: string): Promise<void> {
  const response = await fetchWithAuthRetry(path, {
    credentials: isBrowser() ? "include" : undefined
  });
  if (!response.ok) {
    throw new ApiRequestError({ status: response.status, detail: "Could not download the file." });
  }
  const blob = await response.blob();
  const blobUrl = URL.createObjectURL(blob);
  window.open(blobUrl, "_blank", "noopener,noreferrer");
  setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000);
}

/** For multipart/form-data submissions (file uploads) — no Content-Type override,
 * the browser sets the correct multipart boundary itself. */
export async function apiFetchForm<T>(
  path: string,
  formData: FormData,
  options: { locale?: string; token?: string; method?: string } = {}
): Promise<T> {
  const httpMethod = (options.method ?? "POST").toUpperCase();
  const response = await fetchWithAuthRetry(path, {
    method: options.method ?? "POST",
    body: formData,
    credentials: isBrowser() ? "include" : undefined,
    headers: {
      ...(options.locale ? { "Accept-Language": options.locale } : {}),
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
      ...(SAFE_METHODS.has(httpMethod) ? {} : csrfHeader())
    }
  });
  return handleResponse<T>(response);
}

// ---------------------------------------------------------------------------
// Auth
// ---------------------------------------------------------------------------

export type CurrentUserResponse = Schemas["CurrentUserResponse"];

// Login, refresh, and change-password all return this same shape now — the tokens
// they mint never leave the API as JSON, only as httpOnly Set-Cookie headers. Kept
// as its own name since that's what callers (lib/auth.tsx, the login/change-password
// pages) already import.
export type AuthResponse = CurrentUserResponse;

export function login(
  email: string,
  password: string,
  locale?: string
): Promise<AuthResponse> {
  return apiFetch<AuthResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
    locale
  });
}

/** Reachable even while the account still has mustChangePassword set. Returns a
 * fresh token pair (mustChangePassword: false) so the caller can resume normal
 * access without a second login. */
export function changePassword(
  currentPassword: string,
  newPassword: string,
  locale?: string
): Promise<AuthResponse> {
  return apiFetch<AuthResponse>("/auth/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
    locale
  });
}

/** Re-derives the session from the server (e.g. to pick up a mustChangePassword
 * flip, or after change-password rotates the cookies). Throws ApiRequestError on
 * 401 — callers treat that as "not signed in". `signal` lets a caller bound how long
 * it's willing to wait (e.g. AuthProvider's post-expiry recovery attempt). */
export function getCurrentUser(locale?: string, signal?: AbortSignal): Promise<CurrentUserResponse> {
  return apiFetch<CurrentUserResponse>("/auth/me", { locale, signal });
}

/** Clears the session cookies server-side and revokes the refresh token. Never
 * throws on an already-signed-out caller — see AuthController.Logout. */
export function logout(locale?: string): Promise<void> {
  return apiFetch<void>("/auth/logout", { method: "POST", locale });
}

// ---------------------------------------------------------------------------
// Public catalog
// ---------------------------------------------------------------------------

export type CourseListItem = Schemas["CourseListDto"];
export type CohortInfo = Schemas["CohortListDto"];
export type CourseInstructorInfo = Schemas["CourseInstructorDto"];
export type PublicCourseDetail = Schemas["PublicCourseDetailDto"];
export type TrackListItem = Schemas["TrackListDto"];
export type TrackCourseInfo = Schemas["TrackCourseDto"];
export type PublicTrackDetail = Schemas["PublicTrackDetailDto"];

export function getPublishedCourses(params: {
  category?: string;
  search?: string;
  page?: number;
  pageSize?: number;
} = {}): Promise<PagedResult<CourseListItem>> {
  const query = new URLSearchParams();
  if (params.category) query.set("category", params.category);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<CourseListItem>>(`/catalog/courses${qs ? `?${qs}` : ""}`);
}

export function getPublishedCourseDetail(slug: string): Promise<PublicCourseDetail> {
  return apiFetch<PublicCourseDetail>(`/catalog/courses/${encodeURIComponent(slug)}`);
}

export function getPublishedTracks(params: {
  search?: string;
  page?: number;
  pageSize?: number;
} = {}): Promise<PagedResult<TrackListItem>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<TrackListItem>>(`/catalog/tracks${qs ? `?${qs}` : ""}`);
}

export function getPublishedTrackDetail(slug: string): Promise<PublicTrackDetail> {
  return apiFetch<PublicTrackDetail>(`/catalog/tracks/${encodeURIComponent(slug)}`);
}

// ---------------------------------------------------------------------------
// Coupons
// ---------------------------------------------------------------------------

export type CouponValidationResult = Schemas["CouponValidationResultDto"];

export function validateCoupon(
  code: string,
  target: { courseId?: string; trackId?: string }
): Promise<CouponValidationResult> {
  return apiFetch<CouponValidationResult>("/coupons/validate", {
    method: "POST",
    body: JSON.stringify({
      code,
      courseId: target.courseId ?? null,
      trackId: target.trackId ?? null
    })
  });
}

// ---------------------------------------------------------------------------
// Enrollment requests
// ---------------------------------------------------------------------------

export type EnrollmentRequestResult = Schemas["EnrollmentRequestDto"];

export function submitEnrollmentRequest(input: {
  fullName: string;
  email: string;
  phoneNumber?: string;
  courseId?: string;
  trackId?: string;
  paymentMethod: string;
  couponCode?: string;
  paymentProof: File;
  locale?: string;
}): Promise<EnrollmentRequestResult> {
  const form = new FormData();
  form.set("FullName", input.fullName);
  form.set("Email", input.email);
  if (input.phoneNumber) form.set("PhoneNumber", input.phoneNumber);
  if (input.courseId) form.set("CourseId", input.courseId);
  if (input.trackId) form.set("TrackId", input.trackId);
  form.set("PaymentMethod", input.paymentMethod);
  if (input.couponCode) form.set("CouponCode", input.couponCode);
  form.set("PaymentProof", input.paymentProof);

  return apiFetchForm<EnrollmentRequestResult>("/enrollment-requests", form, {
    locale: input.locale
  });
}

// ---------------------------------------------------------------------------
// Leads
// ---------------------------------------------------------------------------

export type LeadResult = Schemas["LeadDto"];

export function submitLead(input: {
  name: string;
  email: string;
  phone?: string;
  message?: string;
  courseId?: string;
  locale?: string;
}): Promise<LeadResult> {
  return apiFetch<LeadResult>("/leads", {
    method: "POST",
    body: JSON.stringify({
      name: input.name,
      email: input.email,
      phone: input.phone ?? null,
      message: input.message ?? null,
      courseId: input.courseId ?? null
    }),
    locale: input.locale
  });
}

// ---------------------------------------------------------------------------
// Instructor course access
// ---------------------------------------------------------------------------

export function getAssignedCourses(
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResult<CourseListItem>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<CourseListItem>>(`/instructor/courses${qs ? `?${qs}` : ""}`);
}

export function getAllCourses(
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResult<CourseListItem>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<CourseListItem>>(`/courses${qs ? `?${qs}` : ""}`);
}

// ---------------------------------------------------------------------------
// Modules
// ---------------------------------------------------------------------------

export type ModuleItem = Schemas["ModuleListDto"];

export function getCourseModules(courseId: string): Promise<ModuleItem[]> {
  return apiFetch<ModuleItem[]>(`/courses/${courseId}/modules`);
}

export function createModule(
  courseId: string,
  input: { title: string; description?: string },
): Promise<Schemas["ModuleResponseDto"]> {
  return apiFetch(`/courses/${courseId}/modules`, {
    method: "POST",
    body: JSON.stringify({ title: input.title, description: input.description ?? null })
  });
}

export function deleteModule(id: string): Promise<Schemas["ModuleResponseDto"]> {
  return apiFetch(`/modules/${id}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Sessions
// ---------------------------------------------------------------------------

export type SessionType = "live" | "in_person" | "recorded_lesson";

export type SessionItem = Schemas["SessionDto"];

export interface SessionInput {
  type: SessionType;
  title: string;
  description?: string;
  scheduledAt?: string;
  durationMinutes?: number;
  joinLink?: string;
  location?: string;
  videoUrl?: string;
  instructorId?: string;
}

function sessionInputBody(input: SessionInput) {
  return {
    type: input.type,
    title: input.title,
    description: input.description ?? null,
    scheduledAt: localInputToUtcIso(input.scheduledAt),
    durationMinutes: input.durationMinutes ?? null,
    joinLink: input.joinLink ?? null,
    location: input.location ?? null,
    videoUrl: input.videoUrl ?? null,
    instructorId: input.instructorId ?? null
  };
}

export function getModuleSessions(moduleId: string): Promise<SessionItem[]> {
  return apiFetch<SessionItem[]>(`/modules/${moduleId}/sessions`);
}

export function createSession(
  moduleId: string,
  input: SessionInput,
): Promise<Schemas["SessionResponseDto"]> {
  return apiFetch(`/modules/${moduleId}/sessions`, {
    method: "POST",
    body: JSON.stringify(sessionInputBody(input))
  });
}

export function deleteSession(id: string): Promise<Schemas["SessionResponseDto"]> {
  return apiFetch(`/sessions/${id}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Materials
// ---------------------------------------------------------------------------

export type MaterialType = "file" | "text" | "link";

export type MaterialItem = Schemas["MaterialDto"];

export function getModuleMaterials(moduleId: string): Promise<MaterialItem[]> {
  return apiFetch<MaterialItem[]>(`/modules/${moduleId}/materials`);
}

export function getSessionMaterials(sessionId: string): Promise<MaterialItem[]> {
  return apiFetch<MaterialItem[]>(`/sessions/${sessionId}/materials`);
}

export interface CreateMaterialInput {
  type: MaterialType;
  title: string;
  body?: string;
  linkUrl?: string;
  fileType?: string;
  file?: File;
}

function materialForm(input: CreateMaterialInput): FormData {
  const form = new FormData();
  form.set("Type", input.type);
  form.set("Title", input.title);
  if (input.body) form.set("Body", input.body);
  if (input.linkUrl) form.set("LinkUrl", input.linkUrl);
  if (input.fileType) form.set("FileType", input.fileType);
  if (input.file) form.set("File", input.file);
  return form;
}

export function createModuleMaterial(
  moduleId: string,
  input: CreateMaterialInput,
): Promise<MaterialItem> {
  return apiFetchForm<MaterialItem>(`/modules/${moduleId}/materials`, materialForm(input));
}

export function createSessionMaterial(
  sessionId: string,
  input: CreateMaterialInput,
): Promise<MaterialItem> {
  return apiFetchForm<MaterialItem>(`/sessions/${sessionId}/materials`, materialForm(input));
}

export function deleteMaterial(id: string): Promise<MaterialItem> {
  return apiFetch(`/materials/${id}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Announcements
// ---------------------------------------------------------------------------

export type AnnouncementItem = Schemas["AnnouncementDto"];

export function getAnnouncements(
  courseId?: string,
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResult<AnnouncementItem>> {
  const query = new URLSearchParams();
  if (courseId) query.set("courseId", courseId);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<AnnouncementItem>>(`/announcements${qs ? `?${qs}` : ""}`);
}

export function createAnnouncement(
  input: { courseId?: string; title: string; body: string },
): Promise<AnnouncementItem> {
  return apiFetch<AnnouncementItem>("/announcements", {
    method: "POST",
    body: JSON.stringify({ courseId: input.courseId ?? null, title: input.title, body: input.body })
  });
}

export function deleteAnnouncement(id: string): Promise<AnnouncementItem> {
  return apiFetch(`/announcements/${id}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// My courses (student view)
// ---------------------------------------------------------------------------

export type MyCourseAssessment = Schemas["MyCourseAssessmentDto"];
export type MyCourseAssignment = Schemas["MyCourseAssignmentDto"];
export type MyCourseModule = Schemas["MyCourseModuleDto"];
export type MyCourseContent = Schemas["MyCourseContentDto"];

export function getMyCourseContent(courseId: string): Promise<MyCourseContent> {
  return apiFetch<MyCourseContent>(`/my-courses/${courseId}/content`);
}

export type UpcomingSession = Schemas["UpcomingSessionDto"];
export type UpcomingItems = Schemas["UpcomingItemsDto"];

export function getUpcomingItems(): Promise<UpcomingItems> {
  return apiFetch<UpcomingItems>("/my-courses/upcoming-items");
}

// ---------------------------------------------------------------------------
// Attendance
// ---------------------------------------------------------------------------

export type AttendanceStatus = "present" | "absent" | "late" | "excused";

export interface AttendanceEntry {
  studentId: string;
  status: AttendanceStatus;
  notes?: string;
}

export function markAttendance(
  sessionId: string,
  entries: AttendanceEntry[],
): Promise<Schemas["AttendanceResponseDto"]> {
  return apiFetch(`/sessions/${sessionId}/attendance`, {
    method: "PUT",
    body: JSON.stringify({ entries })
  });
}

export type RosterEntry = Schemas["RosterEntryDto"];
export type SessionRoster = Schemas["SessionRosterDto"];

export function getSessionRoster(sessionId: string): Promise<SessionRoster> {
  return apiFetch<SessionRoster>(`/sessions/${sessionId}/attendance`);
}

export type StudentAttendanceSummary = Schemas["StudentAttendanceSummaryDto"];
export type CourseAttendanceReport = Schemas["CourseAttendanceReportDto"];

export function getCourseAttendanceReport(courseId: string): Promise<CourseAttendanceReport> {
  return apiFetch<CourseAttendanceReport>(`/courses/${courseId}/attendance-report`);
}

export type MyAttendanceSession = Schemas["MyAttendanceSessionDto"];
export type MyAttendance = Schemas["MyAttendanceDto"];

export function getMyAttendance(courseId: string): Promise<MyAttendance> {
  return apiFetch<MyAttendance>(`/my-courses/${courseId}/attendance`);
}

// ---------------------------------------------------------------------------
// Assessments (quizzes + exams)
// ---------------------------------------------------------------------------

export type AssessmentType = "quiz" | "exam";

export type AssessmentItem = Schemas["AssessmentDto"];

export interface AssessmentInput {
  type: AssessmentType;
  title: string;
  timeLimitMinutes?: number;
  passScore?: number;
  isPractice: boolean;
  maxAttempts?: number;
  randomizeQuestions: boolean;
  disableCopyPaste: boolean;
}

function assessmentInputBody(input: AssessmentInput) {
  return {
    type: input.type,
    title: input.title,
    timeLimitMinutes: input.timeLimitMinutes ?? null,
    passScore: input.passScore ?? null,
    isPractice: input.isPractice,
    maxAttempts: input.maxAttempts ?? null,
    randomizeQuestions: input.randomizeQuestions,
    disableCopyPaste: input.disableCopyPaste
  };
}

export function getModuleAssessments(moduleId: string): Promise<AssessmentItem[]> {
  return apiFetch<AssessmentItem[]>(`/modules/${moduleId}/assessments`);
}

export function createAssessment(
  moduleId: string,
  input: AssessmentInput,
): Promise<Schemas["AssessmentResponseDto"]> {
  return apiFetch(`/modules/${moduleId}/assessments`, {
    method: "POST",
    body: JSON.stringify(assessmentInputBody(input))
  });
}

export function deleteAssessment(id: string): Promise<Schemas["AssessmentResponseDto"]> {
  return apiFetch(`/assessments/${id}`, { method: "DELETE" });
}

export type OptionDto = Schemas["OptionDto"];

export interface OptionInput {
  optionText: string;
  isCorrect: boolean;
}

export type QuestionDto = Schemas["QuestionDto"];
export type AssessmentDetail = Schemas["AssessmentDetailDto"];

export function getAssessmentById(id: string): Promise<AssessmentDetail> {
  return apiFetch<AssessmentDetail>(`/assessments/${id}`);
}

export function createQuestion(
  assessmentId: string,
  questionText: string,
  options: OptionInput[],
): Promise<Schemas["QuestionResponseDto"]> {
  return apiFetch(`/assessments/${assessmentId}/questions`, {
    method: "POST",
    body: JSON.stringify({ questionText, options })
  });
}

export function deleteQuestion(id: string): Promise<Schemas["QuestionResponseDto"]> {
  return apiFetch(`/questions/${id}`, { method: "DELETE" });
}

export type AttemptOption = Schemas["AttemptOptionDto"];
export type AttemptQuestion = Schemas["AttemptQuestionDto"];
export type AttemptAssessment = Schemas["AttemptAssessmentDto"];

export function getAssessmentForAttempt(id: string): Promise<AttemptAssessment> {
  return apiFetch<AttemptAssessment>(`/assessments/${id}/attempt`);
}

export function startAttempt(id: string): Promise<Schemas["StartAttemptResponseDto"]> {
  return apiFetch(`/assessments/${id}/attempts`, { method: "POST" });
}

export interface AnswerInput {
  questionId: string;
  selectedOptionId: string | null;
}

export type AnswerResult = Schemas["AnswerResultDto"];
export type AttemptResult = Schemas["AttemptResultDto"];

export function submitAttempt(
  attemptId: string,
  answers: AnswerInput[],
): Promise<AttemptResult> {
  return apiFetch<AttemptResult>(`/attempts/${attemptId}/submit`, {
    method: "PUT",
    body: JSON.stringify({ answers })
  });
}

export type AttemptSummary = Schemas["AttemptSummaryDto"];

export function getMyAttempts(assessmentId: string): Promise<AttemptSummary[]> {
  return apiFetch<AttemptSummary[]>(`/assessments/${assessmentId}/my-attempts`);
}

export function getAttemptResult(attemptId: string): Promise<AttemptResult> {
  return apiFetch<AttemptResult>(`/attempts/${attemptId}`);
}

export type StudentAttempt = Schemas["StudentAttemptDto"];
export type AssessmentResults = Schemas["AssessmentResultsDto"];

export function getAssessmentResults(id: string): Promise<AssessmentResults> {
  return apiFetch<AssessmentResults>(`/assessments/${id}/results`);
}

// ---------------------------------------------------------------------------
// Assignments (code, Python auto-grader)
// ---------------------------------------------------------------------------

export interface TestCaseInput {
  input: string;
  expectedOutput: string;
  isHidden: boolean;
  points: number;
}

export type TestCaseDto = Schemas["TestCaseDto"];
export type AssignmentItem = Schemas["AssignmentDto"];

export interface AssignmentInput {
  title: string;
  description: string;
  isPractice: boolean;
  maxAttempts?: number;
  dueAt?: string;
  passScore?: number;
}

function assignmentInputBody(input: AssignmentInput) {
  return {
    title: input.title,
    description: input.description,
    isPractice: input.isPractice,
    maxAttempts: input.maxAttempts ?? null,
    dueAt: localInputToUtcIso(input.dueAt),
    passScore: input.passScore ?? null
  };
}

export function getModuleAssignments(moduleId: string): Promise<AssignmentItem[]> {
  return apiFetch<AssignmentItem[]>(`/modules/${moduleId}/assignments`);
}

export function createAssignment(
  moduleId: string,
  input: AssignmentInput,
): Promise<Schemas["AssignmentResponseDto"]> {
  return apiFetch(`/modules/${moduleId}/assignments`, {
    method: "POST",
    body: JSON.stringify(assignmentInputBody(input))
  });
}

export function deleteAssignment(id: string): Promise<Schemas["AssignmentResponseDto"]> {
  return apiFetch(`/assignments/${id}`, { method: "DELETE" });
}

export type AssignmentDetail = Schemas["AssignmentDetailDto"];

export function getAssignmentById(id: string): Promise<AssignmentDetail> {
  return apiFetch<AssignmentDetail>(`/assignments/${id}`);
}

export function addTestCase(
  assignmentId: string,
  input: TestCaseInput,
): Promise<Schemas["TestCaseResponseDto"]> {
  return apiFetch(`/assignments/${assignmentId}/test-cases`, {
    method: "POST",
    body: JSON.stringify(input)
  });
}

export function deleteTestCase(id: string): Promise<Schemas["TestCaseResponseDto"]> {
  return apiFetch(`/test-cases/${id}`, { method: "DELETE" });
}

export type SubmissionTestCase = Schemas["SubmissionTestCaseDto"];
export type AssignmentForSubmission = Schemas["AssignmentForSubmissionDto"];

export function getAssignmentForSubmission(id: string): Promise<AssignmentForSubmission> {
  return apiFetch<AssignmentForSubmission>(`/assignments/${id}/submission`);
}

export type TestResult = Schemas["TestResultDto"];
export type SubmissionResult = Schemas["SubmissionResultDto"];

export function submitAssignment(id: string, code: string): Promise<SubmissionResult> {
  return apiFetch<SubmissionResult>(`/assignments/${id}/submissions`, {
    method: "POST",
    body: JSON.stringify({ code })
  });
}

export function gradeSubmission(
  submissionId: string,
  manualScore: number,
  manualFeedback: string | undefined,
): Promise<SubmissionResult> {
  return apiFetch<SubmissionResult>(`/submissions/${submissionId}/grade`, {
    method: "PUT",
    body: JSON.stringify({ manualScore, manualFeedback: manualFeedback ?? null })
  });
}

export type SubmissionSummary = Schemas["SubmissionSummaryDto"];

export function getMySubmissions(assignmentId: string): Promise<SubmissionSummary[]> {
  return apiFetch<SubmissionSummary[]>(`/assignments/${assignmentId}/my-submissions`);
}

export function getSubmissionResult(submissionId: string): Promise<SubmissionResult> {
  return apiFetch<SubmissionResult>(`/submissions/${submissionId}`);
}

export type StudentSubmission = Schemas["StudentSubmissionDto"];
export type AssignmentSubmissions = Schemas["AssignmentSubmissionsDto"];

export function getSubmissionsForGrading(assignmentId: string): Promise<AssignmentSubmissions> {
  return apiFetch<AssignmentSubmissions>(`/assignments/${assignmentId}/submissions`);
}

// ---------------------------------------------------------------------------
// Gradebook
// ---------------------------------------------------------------------------

export type AssessmentGrade = Schemas["AssessmentGradeDto"];
export type AssignmentGrade = Schemas["AssignmentGradeDto"];
export type MyCourseGrades = Schemas["MyCourseGradesDto"];

export function getMyCourseGrades(courseId: string): Promise<MyCourseGrades> {
  return apiFetch<MyCourseGrades>(`/my-courses/${courseId}/grades`);
}

export type StudentGradebookRow = Schemas["StudentGradebookRowDto"];
export type CourseGradebook = Schemas["CourseGradebookDto"];

export function getCourseGradebook(courseId: string): Promise<CourseGradebook> {
  return apiFetch<CourseGradebook>(`/courses/${courseId}/gradebook`);
}

// ---------------------------------------------------------------------------
// Course detail + threshold config (admin)
// ---------------------------------------------------------------------------

export type CourseInstructorEntry = Schemas["CourseInstructorDto"];
export type CourseDetail = Schemas["CourseDetailDto"];

export function getCourseById(courseId: string): Promise<CourseDetail> {
  return apiFetch<CourseDetail>(`/courses/${courseId}`);
}

export function updateCourse(
  courseId: string,
  input: {
    title: string;
    slug: string;
    description: string | null;
    thumbnailUrl: string | null;
    category: string | null;
    price: number;
    currency: string;
    completionAttendanceThreshold: number | null;
  },
): Promise<CourseDetail> {
  return apiFetch<CourseDetail>(`/courses/${courseId}`, {
    method: "PUT",
    body: JSON.stringify(input)
  });
}

// ---------------------------------------------------------------------------
// Certificates
// ---------------------------------------------------------------------------

export type CertificateTier = "completion" | "participation";

export type Certificate = Schemas["CertificateDto"];
export type CertificateCandidate = Schemas["CertificateCandidateDto"];
export type CourseCertificateCandidates = Schemas["CourseCertificateCandidatesDto"];

export function getCourseCertificateCandidates(
  courseId: string,
): Promise<CourseCertificateCandidates> {
  return apiFetch<CourseCertificateCandidates>(`/courses/${courseId}/certificate-candidates`);
}

export function issueCertificate(
  enrollmentId: string,
  tier: CertificateTier | null,
): Promise<Certificate> {
  return apiFetch<Certificate>("/certificates", {
    method: "POST",
    body: JSON.stringify({ enrollmentId, tier })
  });
}

export function revokeCertificate(
  certificateId: string,
  reason: string | null,
): Promise<Certificate> {
  return apiFetch<Certificate>(`/certificates/${certificateId}/revoke`, {
    method: "PUT",
    body: JSON.stringify({ reason })
  });
}

export function getMyCertificates(
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResult<Certificate>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<Certificate>>(`/my-certificates${qs ? `?${qs}` : ""}`);
}

export function getCertificateById(certificateId: string): Promise<Certificate> {
  return apiFetch<Certificate>(`/certificates/${certificateId}`);
}

export type CertificateVerification = Schemas["CertificateVerificationDto"];

/** Public — no auth token needed. */
export function verifyCertificate(code: string): Promise<CertificateVerification> {
  return apiFetch<CertificateVerification>(`/certificates/verify/${encodeURIComponent(code)}`);
}

// ---------------------------------------------------------------------------
// Analytics
// ---------------------------------------------------------------------------

export type MonthlyCount = Schemas["MonthlyCountDto"];
export type RevenueByCourse = Schemas["RevenueByCourseDto"];
export type AdminBusinessDashboard = Schemas["AdminBusinessDashboardDto"];

export function getAdminBusinessDashboard(): Promise<AdminBusinessDashboard> {
  return apiFetch<AdminBusinessDashboard>("/analytics/admin/business");
}

export type CourseAcademicRow = Schemas["CourseAcademicRowDto"];
export type AdminAcademicDashboard = Schemas["AdminAcademicDashboardDto"];

export function getAdminAcademicDashboard(): Promise<AdminAcademicDashboard> {
  return apiFetch<AdminAcademicDashboard>("/analytics/admin/academic");
}

export type InstructorCourseRow = Schemas["InstructorCourseRowDto"];
export type InstructorDashboard = Schemas["InstructorDashboardDto"];

export function getInstructorDashboard(): Promise<InstructorDashboard> {
  return apiFetch<InstructorDashboard>("/analytics/instructor");
}

// ---------------------------------------------------------------------------
// Admin: Users
// ---------------------------------------------------------------------------

export type AdminUser = Schemas["UserDto"];

export function getUsers(
  params: { role?: string; isActive?: boolean; search?: string; page?: number; pageSize?: number },
): Promise<PagedResult<AdminUser>> {
  const query = new URLSearchParams();
  if (params.role) query.set("role", params.role);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<AdminUser>>(`/users${qs ? `?${qs}` : ""}`);
}

export function createInstructor(
  input: { fullName: string; email: string; phone?: string },
): Promise<AdminUser> {
  return apiFetch<AdminUser>("/users/instructors", { method: "POST", body: JSON.stringify(input) });
}

export function deactivateUser(userId: string): Promise<AdminUser> {
  return apiFetch<AdminUser>(`/users/${userId}/deactivate`, { method: "PUT" });
}

export function reactivateUser(userId: string): Promise<AdminUser> {
  return apiFetch<AdminUser>(`/users/${userId}/reactivate`, { method: "PUT" });
}

// ---------------------------------------------------------------------------
// Admin: Courses
// ---------------------------------------------------------------------------

export type CourseMutationResult = Schemas["CourseMutationResultDto"];

export function getAdminCourses(
  params: { status?: string; category?: string; search?: string; page?: number; pageSize?: number },
): Promise<PagedResult<CourseListItem>> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.category) query.set("category", params.category);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<CourseListItem>>(`/courses${qs ? `?${qs}` : ""}`);
}

export function createCourse(
  input: {
    title: string;
    slug: string;
    description?: string | null;
    thumbnailUrl?: string | null;
    category?: string | null;
    price: number;
    currency: string;
  },
): Promise<CourseDetail> {
  return apiFetch<CourseDetail>("/courses", { method: "POST", body: JSON.stringify(input) });
}

export function publishCourse(courseId: string): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/publish`, { method: "PUT" });
}

export function archiveCourse(courseId: string): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/archive`, { method: "PUT" });
}

export function deleteCourse(courseId: string): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}`, { method: "DELETE" });
}

export function assignInstructorToCourse(
  courseId: string,
  instructorId: string,
): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/instructors/${instructorId}`, {
    method: "POST" });
}

export function removeInstructorFromCourse(
  courseId: string,
  instructorId: string,
): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/instructors/${instructorId}`, {
    method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Admin: Tracks
// ---------------------------------------------------------------------------

export type TrackCourseEntry = Schemas["TrackCourseDto"];
export type TrackDetail = Schemas["TrackDetailDto"];
export type TrackMutationResult = Schemas["TrackMutationResultDto"];

export function getAdminTracks(
  params: { status?: string; search?: string; page?: number; pageSize?: number },
): Promise<PagedResult<TrackListItem>> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<TrackListItem>>(`/tracks${qs ? `?${qs}` : ""}`);
}

export function getTrackById(trackId: string): Promise<TrackDetail> {
  return apiFetch<TrackDetail>(`/tracks/${trackId}`);
}

export function createTrack(
  input: {
    title: string;
    slug: string;
    description?: string | null;
    thumbnailUrl?: string | null;
    price: number;
    currency: string;
  },
): Promise<TrackDetail> {
  return apiFetch<TrackDetail>("/tracks", { method: "POST", body: JSON.stringify(input) });
}

export function updateTrack(
  trackId: string,
  input: {
    title: string;
    slug: string;
    description?: string | null;
    thumbnailUrl?: string | null;
    price: number;
    currency: string;
  },
): Promise<TrackDetail> {
  return apiFetch<TrackDetail>(`/tracks/${trackId}`, { method: "PUT", body: JSON.stringify(input) });
}

export function publishTrack(trackId: string): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}/publish`, { method: "PUT" });
}

export function archiveTrack(trackId: string): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}/archive`, { method: "PUT" });
}

export function deleteTrack(trackId: string): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}`, { method: "DELETE" });
}

export function addCourseToTrack(
  trackId: string,
  courseId: string,
  sortOrder: number,
): Promise<TrackCourseEntry> {
  return apiFetch<TrackCourseEntry>(`/tracks/${trackId}/courses/${courseId}`, {
    method: "POST",
    body: JSON.stringify({ sortOrder })
  });
}

export function removeCourseFromTrack(
  trackId: string,
  courseId: string,
): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}/courses/${courseId}`, { method: "DELETE" });
}

// ---------------------------------------------------------------------------
// Admin: Cohorts
// ---------------------------------------------------------------------------

export type CohortMutationResult = Schemas["CohortMutationResultDto"];

export function getCourseCohortsAdmin(
  courseId: string,
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResult<CohortInfo>> {
  const query = new URLSearchParams();
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<CohortInfo>>(`/courses/${courseId}/cohorts${qs ? `?${qs}` : ""}`);
}

export function createCohort(
  courseId: string,
  input: {
    name: string;
    startDate: string;
    endDate: string;
    enrollmentCutoffDate: string;
    capacity: number;
    gracePeriodDays: number;
  },
): Promise<CohortInfo> {
  return apiFetch<CohortInfo>(`/courses/${courseId}/cohorts`, {
    method: "POST",
    body: JSON.stringify(input)
  });
}

export function updateCohort(
  cohortId: string,
  input: {
    name: string;
    startDate: string;
    endDate: string;
    enrollmentCutoffDate: string;
    capacity: number;
    gracePeriodDays: number;
  },
): Promise<CohortInfo> {
  return apiFetch<CohortInfo>(`/cohorts/${cohortId}`, { method: "PUT", body: JSON.stringify(input) });
}

export function openCohort(cohortId: string): Promise<CohortMutationResult> {
  return apiFetch<CohortMutationResult>(`/cohorts/${cohortId}/open`, { method: "PUT" });
}

export function cancelCohort(cohortId: string): Promise<CohortMutationResult> {
  return apiFetch<CohortMutationResult>(`/cohorts/${cohortId}/cancel`, { method: "PUT" });
}

export function completeCohort(cohortId: string): Promise<CohortMutationResult> {
  return apiFetch<CohortMutationResult>(`/cohorts/${cohortId}/complete`, { method: "PUT" });
}

// ---------------------------------------------------------------------------
// Admin: Coupons
// ---------------------------------------------------------------------------

export type AdminCoupon = Schemas["CouponDto"];

export function getCoupons(
  isActive: boolean | undefined,
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResult<AdminCoupon>> {
  const query = new URLSearchParams();
  if (isActive !== undefined) query.set("isActive", String(isActive));
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<AdminCoupon>>(`/coupons${qs ? `?${qs}` : ""}`);
}

export function createCoupon(
  input: { code: string; type: string; value: number; validFrom?: string | null; validUntil?: string | null; usageLimit?: number | null },
): Promise<AdminCoupon> {
  return apiFetch<AdminCoupon>("/coupons", { method: "POST", body: JSON.stringify(input) });
}

export function updateCoupon(
  couponId: string,
  input: {
    type: string;
    value: number;
    isActive: boolean;
    validFrom?: string | null;
    validUntil?: string | null;
    usageLimit?: number | null;
  },
): Promise<AdminCoupon> {
  return apiFetch<AdminCoupon>(`/coupons/${couponId}`, { method: "PUT", body: JSON.stringify(input) });
}

export function deactivateCoupon(couponId: string): Promise<AdminCoupon> {
  return apiFetch<AdminCoupon>(`/coupons/${couponId}/deactivate`, { method: "PUT" });
}

// ---------------------------------------------------------------------------
// Admin: Enrollment requests + enrollment cancellation
// ---------------------------------------------------------------------------

export type EnrollmentRequestTargetCohort = Schemas["EnrollmentRequestTargetCohortDto"];
export type EnrollmentRequestDetail = Schemas["EnrollmentRequestDetailDto"];

export function getEnrollmentRequests(
  params: { status?: string; courseId?: string; trackId?: string; page?: number; pageSize?: number },
): Promise<PagedResult<EnrollmentRequestResult>> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.courseId) query.set("courseId", params.courseId);
  if (params.trackId) query.set("trackId", params.trackId);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return apiFetch<PagedResult<EnrollmentRequestResult>>(`/enrollment-requests${qs ? `?${qs}` : ""}`);
}

export function getEnrollmentRequestById(id: string): Promise<EnrollmentRequestDetail> {
  return apiFetch<EnrollmentRequestDetail>(`/enrollment-requests/${id}`);
}

export type EnrollmentApprovalResult = Schemas["EnrollmentApprovalResultDto"];

export function approveEnrollmentRequest(id: string): Promise<EnrollmentApprovalResult> {
  return apiFetch<EnrollmentApprovalResult>(`/enrollment-requests/${id}/approve`, { method: "PUT" });
}

export function rejectEnrollmentRequest(
  id: string,
  rejectionReason: string,
): Promise<Schemas["EnrollmentRequestMessageDto"]> {
  return apiFetch(`/enrollment-requests/${id}/reject`, {
    method: "PUT",
    body: JSON.stringify({ rejectionReason })
  });
}

export function cancelEnrollment(
  enrollmentId: string,
  reason: string,
  markAsRefunded: boolean,
): Promise<Schemas["EnrollmentDto"]> {
  return apiFetch(`/enrollments/${enrollmentId}/cancel`, {
    method: "PUT",
    body: JSON.stringify({ reason, markAsRefunded })
  });
}
