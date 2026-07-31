// Thin fetch wrapper around the CodeForge ASP.NET API.
// Centralizes the base URL and error shape so feature code stays uniform.

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5205";

export interface ApiError {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
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
  const { locale, token, headers, ...rest } = options;
  const response = await fetch(`${BASE_URL}${path}`, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...(locale ? { "Accept-Language": locale } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers
    }
  });
  return handleResponse<T>(response);
}

/** Fetches a private file (materials, payment proofs) with the Authorization header
 * and opens it in a new tab via a blob URL. These endpoints are never plain <a href>
 * links — the server requires a Bearer token, which only fetch (not navigation) can send. */
export async function downloadAuthenticatedFile(path: string, token: string): Promise<void> {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { Authorization: `Bearer ${token}` }
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
  const response = await fetch(`${BASE_URL}${path}`, {
    method: options.method ?? "POST",
    body: formData,
    headers: {
      ...(options.locale ? { "Accept-Language": options.locale } : {}),
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {})
    }
  });
  return handleResponse<T>(response);
}

// ---------------------------------------------------------------------------
// Auth
// ---------------------------------------------------------------------------

export interface AuthResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  mustChangePassword: boolean;
}

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
  token: string,
  locale?: string
): Promise<AuthResponse> {
  return apiFetch<AuthResponse>("/auth/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
    token,
    locale
  });
}

// ---------------------------------------------------------------------------
// Public catalog
// ---------------------------------------------------------------------------

export interface CourseListItem {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  thumbnailUrl: string | null;
  category: string | null;
  price: number;
  currency: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CohortInfo {
  id: string;
  courseId: string;
  courseTitle: string;
  name: string;
  startDate: string;
  endDate: string;
  enrollmentCutoffDate: string;
  capacity: number;
  gracePeriodDays: number;
  status: string;
  enrolledCount: number;
  seatsLeft: number;
  isAcceptingEnrollment: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CourseInstructorInfo {
  id: string;
  instructorId: string;
  instructorName: string;
  instructorEmail: string;
  assignedAt: string;
}

export interface PublicCourseDetail {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  thumbnailUrl: string | null;
  category: string | null;
  price: number;
  currency: string;
  instructors: CourseInstructorInfo[];
  cohorts: CohortInfo[];
}

export interface TrackListItem {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  thumbnailUrl: string | null;
  price: number;
  currency: string;
  status: string;
  courseCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface TrackCourseInfo {
  courseId: string;
  courseTitle: string;
  courseSlug: string;
  coursePrice: number;
  sortOrder: number;
}

export interface PublicTrackDetail {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  thumbnailUrl: string | null;
  price: number;
  currency: string;
  courses: TrackCourseInfo[];
  isBundleEnrollable: boolean;
}

export function getPublishedCourses(params: {
  category?: string;
  search?: string;
} = {}): Promise<CourseListItem[]> {
  const query = new URLSearchParams();
  if (params.category) query.set("category", params.category);
  if (params.search) query.set("search", params.search);
  const qs = query.toString();
  return apiFetch<CourseListItem[]>(`/catalog/courses${qs ? `?${qs}` : ""}`);
}

export function getPublishedCourseDetail(slug: string): Promise<PublicCourseDetail> {
  return apiFetch<PublicCourseDetail>(`/catalog/courses/${encodeURIComponent(slug)}`);
}

export function getPublishedTracks(params: { search?: string } = {}): Promise<TrackListItem[]> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  const qs = query.toString();
  return apiFetch<TrackListItem[]>(`/catalog/tracks${qs ? `?${qs}` : ""}`);
}

export function getPublishedTrackDetail(slug: string): Promise<PublicTrackDetail> {
  return apiFetch<PublicTrackDetail>(`/catalog/tracks/${encodeURIComponent(slug)}`);
}

// ---------------------------------------------------------------------------
// Coupons
// ---------------------------------------------------------------------------

export interface CouponValidationResult {
  valid: boolean;
  code: string;
  type: string | null;
  value: number | null;
  originalPrice: number;
  discountAmount: number;
  finalPrice: number;
  message: string | null;
}

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

export interface EnrollmentRequestResult {
  id: string;
  applicantName: string;
  applicantEmail: string;
  applicantPhone: string | null;
  courseId: string | null;
  courseTitle: string | null;
  trackId: string | null;
  trackTitle: string | null;
  paymentMethod: string;
  /** Authenticated (admin-only) API path — fetch with downloadAuthenticatedFile, not a plain link. */
  paymentProofDownloadUrl: string;
  originalPrice: number;
  couponCode: string | null;
  discountAmount: number;
  finalPrice: number;
  status: string;
  createdAt: string;
  updatedAt: string;
}

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

export interface LeadResult {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  message: string | null;
  courseId: string | null;
  courseTitle: string | null;
  isContacted: boolean;
  createdAt: string;
}

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

export function getAssignedCourses(token: string): Promise<CourseListItem[]> {
  return apiFetch<CourseListItem[]>("/instructor/courses", { token });
}

export function getAllCourses(token: string): Promise<CourseListItem[]> {
  return apiFetch<CourseListItem[]>("/courses", { token });
}

// ---------------------------------------------------------------------------
// Modules
// ---------------------------------------------------------------------------

export interface ModuleItem {
  id: string;
  courseId: string;
  title: string;
  description: string | null;
  orderIndex: number;
  sessionCount: number;
  createdAt: string;
  updatedAt: string;
}

export function getCourseModules(courseId: string, token: string): Promise<ModuleItem[]> {
  return apiFetch<ModuleItem[]>(`/courses/${courseId}/modules`, { token });
}

export function createModule(
  courseId: string,
  input: { title: string; description?: string },
  token: string
): Promise<{ moduleId: string; message: string }> {
  return apiFetch(`/courses/${courseId}/modules`, {
    method: "POST",
    body: JSON.stringify({ title: input.title, description: input.description ?? null }),
    token
  });
}

export function deleteModule(id: string, token: string): Promise<{ moduleId: string; message: string }> {
  return apiFetch(`/modules/${id}`, { method: "DELETE", token });
}

// ---------------------------------------------------------------------------
// Sessions
// ---------------------------------------------------------------------------

export type SessionType = "live" | "in_person" | "recorded_lesson";

export interface SessionItem {
  id: string;
  moduleId: string;
  type: SessionType;
  title: string;
  description: string | null;
  orderIndex: number;
  scheduledAt: string | null;
  durationMinutes: number | null;
  joinLink: string | null;
  location: string | null;
  videoUrl: string | null;
  instructorId: string | null;
  instructorName: string | null;
  materialCount: number;
  createdAt: string;
  updatedAt: string;
}

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
    scheduledAt: input.scheduledAt ?? null,
    durationMinutes: input.durationMinutes ?? null,
    joinLink: input.joinLink ?? null,
    location: input.location ?? null,
    videoUrl: input.videoUrl ?? null,
    instructorId: input.instructorId ?? null
  };
}

export function getModuleSessions(moduleId: string, token: string): Promise<SessionItem[]> {
  return apiFetch<SessionItem[]>(`/modules/${moduleId}/sessions`, { token });
}

export function createSession(
  moduleId: string,
  input: SessionInput,
  token: string
): Promise<{ sessionId: string; message: string }> {
  return apiFetch(`/modules/${moduleId}/sessions`, {
    method: "POST",
    body: JSON.stringify(sessionInputBody(input)),
    token
  });
}

export function deleteSession(id: string, token: string): Promise<{ sessionId: string; message: string }> {
  return apiFetch(`/sessions/${id}`, { method: "DELETE", token });
}

// ---------------------------------------------------------------------------
// Materials
// ---------------------------------------------------------------------------

export type MaterialType = "file" | "text" | "link";

export interface MaterialItem {
  id: string;
  moduleId: string | null;
  sessionId: string | null;
  type: MaterialType;
  title: string;
  orderIndex: number;
  body: string | null;
  /** Authenticated API path — fetch with downloadAuthenticatedFile, not a plain link. */
  fileDownloadUrl: string | null;
  fileType: string | null;
  fileSizeKb: number | null;
  linkUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export function getModuleMaterials(moduleId: string, token: string): Promise<MaterialItem[]> {
  return apiFetch<MaterialItem[]>(`/modules/${moduleId}/materials`, { token });
}

export function getSessionMaterials(sessionId: string, token: string): Promise<MaterialItem[]> {
  return apiFetch<MaterialItem[]>(`/sessions/${sessionId}/materials`, { token });
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
  token: string
): Promise<MaterialItem> {
  return apiFetchForm<MaterialItem>(`/modules/${moduleId}/materials`, materialForm(input), { token });
}

export function createSessionMaterial(
  sessionId: string,
  input: CreateMaterialInput,
  token: string
): Promise<MaterialItem> {
  return apiFetchForm<MaterialItem>(`/sessions/${sessionId}/materials`, materialForm(input), { token });
}

export function deleteMaterial(id: string, token: string): Promise<MaterialItem> {
  return apiFetch(`/materials/${id}`, { method: "DELETE", token });
}

// ---------------------------------------------------------------------------
// Announcements
// ---------------------------------------------------------------------------

export interface AnnouncementItem {
  id: string;
  courseId: string | null;
  courseTitle: string | null;
  authorId: string;
  authorName: string;
  title: string;
  body: string;
  createdAt: string;
  updatedAt: string;
}

export function getAnnouncements(token: string, courseId?: string): Promise<AnnouncementItem[]> {
  const qs = courseId ? `?courseId=${courseId}` : "";
  return apiFetch<AnnouncementItem[]>(`/announcements${qs}`, { token });
}

export function createAnnouncement(
  input: { courseId?: string; title: string; body: string },
  token: string
): Promise<AnnouncementItem> {
  return apiFetch<AnnouncementItem>("/announcements", {
    method: "POST",
    body: JSON.stringify({ courseId: input.courseId ?? null, title: input.title, body: input.body }),
    token
  });
}

export function deleteAnnouncement(id: string, token: string): Promise<AnnouncementItem> {
  return apiFetch(`/announcements/${id}`, { method: "DELETE", token });
}

// ---------------------------------------------------------------------------
// My courses (student view)
// ---------------------------------------------------------------------------

export interface MyCourseAssessment {
  id: string;
  type: AssessmentType;
  title: string;
  timeLimitMinutes: number | null;
  passScore: number | null;
  maxAttempts: number | null;
  isPractice: boolean;
}

export interface MyCourseAssignment {
  id: string;
  title: string;
  dueAt: string | null;
  maxAttempts: number | null;
  isPractice: boolean;
}

export interface MyCourseModule {
  id: string;
  title: string;
  description: string | null;
  orderIndex: number;
  sessions: SessionItem[];
  assessments: MyCourseAssessment[];
  assignments: MyCourseAssignment[];
}

export interface MyCourseContent {
  courseId: string;
  courseTitle: string;
  modules: MyCourseModule[];
}

export function getMyCourseContent(courseId: string, token: string): Promise<MyCourseContent> {
  return apiFetch<MyCourseContent>(`/my-courses/${courseId}/content`, { token });
}

export interface UpcomingSession {
  sessionId: string;
  courseId: string;
  courseTitle: string;
  moduleTitle: string;
  type: SessionType;
  title: string;
  scheduledAt: string;
  joinLink: string | null;
  location: string | null;
}

export interface UpcomingItems {
  upcomingSessions: UpcomingSession[];
  recentAnnouncements: AnnouncementItem[];
}

export function getUpcomingItems(token: string): Promise<UpcomingItems> {
  return apiFetch<UpcomingItems>("/my-courses/upcoming-items", { token });
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
  token: string
): Promise<{ sessionId: string; message: string }> {
  return apiFetch(`/sessions/${sessionId}/attendance`, {
    method: "PUT",
    body: JSON.stringify({ entries }),
    token
  });
}

export interface RosterEntry {
  studentId: string;
  studentName: string;
  status: AttendanceStatus | null;
  notes: string | null;
}

export interface SessionRoster {
  sessionId: string;
  sessionTitle: string;
  students: RosterEntry[];
}

export function getSessionRoster(sessionId: string, token: string): Promise<SessionRoster> {
  return apiFetch<SessionRoster>(`/sessions/${sessionId}/attendance`, { token });
}

export interface StudentAttendanceSummary {
  studentId: string;
  studentName: string;
  cohortId: string;
  cohortName: string;
  sessionsHeld: number;
  sessionsPresent: number;
  attendanceRate: number;
}

export interface CourseAttendanceReport {
  courseId: string;
  courseTitle: string;
  students: StudentAttendanceSummary[];
}

export function getCourseAttendanceReport(courseId: string, token: string): Promise<CourseAttendanceReport> {
  return apiFetch<CourseAttendanceReport>(`/courses/${courseId}/attendance-report`, { token });
}

export interface MyAttendanceSession {
  sessionId: string;
  sessionTitle: string;
  scheduledAt: string;
  status: AttendanceStatus | null;
}

export interface MyAttendance {
  courseId: string;
  courseTitle: string;
  sessionsHeld: number;
  sessionsPresent: number;
  attendanceRate: number;
  sessions: MyAttendanceSession[];
}

export function getMyAttendance(courseId: string, token: string): Promise<MyAttendance> {
  return apiFetch<MyAttendance>(`/my-courses/${courseId}/attendance`, { token });
}

// ---------------------------------------------------------------------------
// Assessments (quizzes + exams)
// ---------------------------------------------------------------------------

export type AssessmentType = "quiz" | "exam";

export interface AssessmentItem {
  id: string;
  moduleId: string;
  type: AssessmentType;
  title: string;
  orderIndex: number;
  timeLimitMinutes: number | null;
  passScore: number | null;
  isPractice: boolean;
  maxAttempts: number | null;
  randomizeQuestions: boolean;
  disableCopyPaste: boolean;
  questionCount: number;
  createdAt: string;
  updatedAt: string;
}

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

export function getModuleAssessments(moduleId: string, token: string): Promise<AssessmentItem[]> {
  return apiFetch<AssessmentItem[]>(`/modules/${moduleId}/assessments`, { token });
}

export function createAssessment(
  moduleId: string,
  input: AssessmentInput,
  token: string
): Promise<{ assessmentId: string; message: string }> {
  return apiFetch(`/modules/${moduleId}/assessments`, {
    method: "POST",
    body: JSON.stringify(assessmentInputBody(input)),
    token
  });
}

export function deleteAssessment(id: string, token: string): Promise<{ assessmentId: string; message: string }> {
  return apiFetch(`/assessments/${id}`, { method: "DELETE", token });
}

export interface OptionInput {
  optionText: string;
  isCorrect: boolean;
}

export interface OptionDto {
  id: string;
  optionText: string;
  isCorrect: boolean;
}

export interface QuestionDto {
  id: string;
  questionText: string;
  orderIndex: number;
  options: OptionDto[];
}

export interface AssessmentDetail extends AssessmentItem {
  questions: QuestionDto[];
}

export function getAssessmentById(id: string, token: string): Promise<AssessmentDetail> {
  return apiFetch<AssessmentDetail>(`/assessments/${id}`, { token });
}

export function createQuestion(
  assessmentId: string,
  questionText: string,
  options: OptionInput[],
  token: string
): Promise<{ questionId: string; message: string }> {
  return apiFetch(`/assessments/${assessmentId}/questions`, {
    method: "POST",
    body: JSON.stringify({ questionText, options }),
    token
  });
}

export function deleteQuestion(id: string, token: string): Promise<{ questionId: string; message: string }> {
  return apiFetch(`/questions/${id}`, { method: "DELETE", token });
}

export interface AttemptOption {
  id: string;
  optionText: string;
}

export interface AttemptQuestion {
  id: string;
  questionText: string;
  options: AttemptOption[];
}

export interface AttemptAssessment {
  id: string;
  type: AssessmentType;
  title: string;
  timeLimitMinutes: number | null;
  maxAttempts: number | null;
  attemptsUsed: number;
  disableCopyPaste: boolean;
  questions: AttemptQuestion[];
}

export function getAssessmentForAttempt(id: string, token: string): Promise<AttemptAssessment> {
  return apiFetch<AttemptAssessment>(`/assessments/${id}/attempt`, { token });
}

export function startAttempt(id: string, token: string): Promise<{ attemptId: string; startedAt: string }> {
  return apiFetch(`/assessments/${id}/attempts`, { method: "POST", token });
}

export interface AnswerInput {
  questionId: string;
  selectedOptionId: string | null;
}

export interface AnswerResult {
  questionId: string;
  questionText: string;
  selectedOptionId: string | null;
  isCorrectSelection: boolean | null;
  options: OptionDto[];
}

export interface AttemptResult {
  attemptId: string;
  quizId: string;
  quizTitle: string;
  attemptNumber: number;
  score: number | null;
  passed: boolean | null;
  startedAt: string;
  submittedAt: string | null;
  answers: AnswerResult[];
}

export function submitAttempt(
  attemptId: string,
  answers: AnswerInput[],
  token: string
): Promise<AttemptResult> {
  return apiFetch<AttemptResult>(`/attempts/${attemptId}/submit`, {
    method: "PUT",
    body: JSON.stringify({ answers }),
    token
  });
}

export interface AttemptSummary {
  attemptId: string;
  attemptNumber: number;
  score: number | null;
  passed: boolean | null;
  startedAt: string;
  submittedAt: string | null;
}

export function getMyAttempts(assessmentId: string, token: string): Promise<AttemptSummary[]> {
  return apiFetch<AttemptSummary[]>(`/assessments/${assessmentId}/my-attempts`, { token });
}

export function getAttemptResult(attemptId: string, token: string): Promise<AttemptResult> {
  return apiFetch<AttemptResult>(`/attempts/${attemptId}`, { token });
}

export interface StudentAttempt {
  attemptId: string;
  studentId: string;
  studentName: string;
  attemptNumber: number;
  score: number | null;
  passed: boolean | null;
  startedAt: string;
  submittedAt: string | null;
}

export interface AssessmentResults {
  assessmentId: string;
  assessmentTitle: string;
  attempts: StudentAttempt[];
}

export function getAssessmentResults(id: string, token: string): Promise<AssessmentResults> {
  return apiFetch<AssessmentResults>(`/assessments/${id}/results`, { token });
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

export interface TestCaseDto {
  id: string;
  input: string;
  expectedOutput: string;
  isHidden: boolean;
  points: number;
  orderIndex: number;
}

export interface AssignmentItem {
  id: string;
  moduleId: string;
  title: string;
  description: string;
  orderIndex: number;
  isPractice: boolean;
  maxAttempts: number | null;
  dueAt: string | null;
  passScore: number | null;
  testCaseCount: number;
  createdAt: string;
  updatedAt: string;
}

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
    dueAt: input.dueAt ?? null,
    passScore: input.passScore ?? null
  };
}

export function getModuleAssignments(moduleId: string, token: string): Promise<AssignmentItem[]> {
  return apiFetch<AssignmentItem[]>(`/modules/${moduleId}/assignments`, { token });
}

export function createAssignment(
  moduleId: string,
  input: AssignmentInput,
  token: string
): Promise<{ assignmentId: string; message: string }> {
  return apiFetch(`/modules/${moduleId}/assignments`, {
    method: "POST",
    body: JSON.stringify(assignmentInputBody(input)),
    token
  });
}

export function deleteAssignment(id: string, token: string): Promise<{ assignmentId: string; message: string }> {
  return apiFetch(`/assignments/${id}`, { method: "DELETE", token });
}

export interface AssignmentDetail extends AssignmentItem {
  testCases: TestCaseDto[];
}

export function getAssignmentById(id: string, token: string): Promise<AssignmentDetail> {
  return apiFetch<AssignmentDetail>(`/assignments/${id}`, { token });
}

export function addTestCase(
  assignmentId: string,
  input: TestCaseInput,
  token: string
): Promise<{ testCaseId: string; message: string }> {
  return apiFetch(`/assignments/${assignmentId}/test-cases`, {
    method: "POST",
    body: JSON.stringify(input),
    token
  });
}

export function deleteTestCase(id: string, token: string): Promise<{ testCaseId: string; message: string }> {
  return apiFetch(`/test-cases/${id}`, { method: "DELETE", token });
}

export interface SubmissionTestCase {
  id: string;
  input: string;
  expectedOutput: string;
}

export interface AssignmentForSubmission {
  id: string;
  title: string;
  description: string;
  dueAt: string | null;
  maxAttempts: number | null;
  attemptsUsed: number;
  sampleTestCases: SubmissionTestCase[];
}

export function getAssignmentForSubmission(id: string, token: string): Promise<AssignmentForSubmission> {
  return apiFetch<AssignmentForSubmission>(`/assignments/${id}/submission`, { token });
}

export interface TestResult {
  testCaseId: string;
  isHidden: boolean;
  passed: boolean;
  actualOutput: string | null;
  errorMessage: string | null;
  executionTimeMs: number | null;
}

export interface SubmissionResult {
  submissionId: string;
  attemptNumber: number;
  submittedAt: string;
  isLate: boolean;
  autoScore: number | null;
  autoGradingStatus: string;
  manualScore: number | null;
  manualFeedback: string | null;
  finalScore: number | null;
  testResults: TestResult[];
}

export function submitAssignment(id: string, code: string, token: string): Promise<SubmissionResult> {
  return apiFetch<SubmissionResult>(`/assignments/${id}/submissions`, {
    method: "POST",
    body: JSON.stringify({ code }),
    token
  });
}

export function gradeSubmission(
  submissionId: string,
  manualScore: number,
  manualFeedback: string | undefined,
  token: string
): Promise<SubmissionResult> {
  return apiFetch<SubmissionResult>(`/submissions/${submissionId}/grade`, {
    method: "PUT",
    body: JSON.stringify({ manualScore, manualFeedback: manualFeedback ?? null }),
    token
  });
}

export interface SubmissionSummary {
  submissionId: string;
  attemptNumber: number;
  submittedAt: string;
  isLate: boolean;
  autoScore: number | null;
  autoGradingStatus: string;
  manualScore: number | null;
  finalScore: number | null;
}

export function getMySubmissions(assignmentId: string, token: string): Promise<SubmissionSummary[]> {
  return apiFetch<SubmissionSummary[]>(`/assignments/${assignmentId}/my-submissions`, { token });
}

export function getSubmissionResult(submissionId: string, token: string): Promise<SubmissionResult> {
  return apiFetch<SubmissionResult>(`/submissions/${submissionId}`, { token });
}

export interface StudentSubmission {
  submissionId: string;
  studentId: string;
  studentName: string;
  attemptNumber: number;
  submittedAt: string;
  isLate: boolean;
  autoScore: number | null;
  autoGradingStatus: string;
  manualScore: number | null;
  finalScore: number | null;
}

export interface AssignmentSubmissions {
  assignmentId: string;
  assignmentTitle: string;
  submissions: StudentSubmission[];
}

export function getSubmissionsForGrading(assignmentId: string, token: string): Promise<AssignmentSubmissions> {
  return apiFetch<AssignmentSubmissions>(`/assignments/${assignmentId}/submissions`, { token });
}

// ---------------------------------------------------------------------------
// Gradebook
// ---------------------------------------------------------------------------

export interface AssessmentGrade {
  assessmentId: string;
  title: string;
  type: AssessmentType;
  bestScore: number | null;
  passed: boolean | null;
  attemptsUsed: number;
}

export interface AssignmentGrade {
  assignmentId: string;
  title: string;
  finalScore: number | null;
  autoGradingStatus: string;
  manuallyGraded: boolean;
}

export interface MyCourseGrades {
  courseId: string;
  courseTitle: string;
  attendanceRate: number;
  assessments: AssessmentGrade[];
  assignments: AssignmentGrade[];
}

export function getMyCourseGrades(courseId: string, token: string): Promise<MyCourseGrades> {
  return apiFetch<MyCourseGrades>(`/my-courses/${courseId}/grades`, { token });
}

export interface StudentGradebookRow {
  studentId: string;
  studentName: string;
  attendanceRate: number;
  assessments: AssessmentGrade[];
  assignments: AssignmentGrade[];
}

export interface CourseGradebook {
  courseId: string;
  courseTitle: string;
  students: StudentGradebookRow[];
}

export function getCourseGradebook(courseId: string, token: string): Promise<CourseGradebook> {
  return apiFetch<CourseGradebook>(`/courses/${courseId}/gradebook`, { token });
}

// ---------------------------------------------------------------------------
// Course detail + threshold config (admin)
// ---------------------------------------------------------------------------

export interface CourseInstructorEntry {
  id: string;
  instructorId: string;
  fullName: string;
  email: string;
  assignedAt: string;
}

export interface CourseDetail {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  thumbnailUrl: string | null;
  category: string | null;
  price: number;
  currency: string;
  status: string;
  completionAttendanceThreshold: number | null;
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
  instructors: CourseInstructorEntry[];
}

export function getCourseById(courseId: string, token: string): Promise<CourseDetail> {
  return apiFetch<CourseDetail>(`/courses/${courseId}`, { token });
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
  token: string
): Promise<CourseDetail> {
  return apiFetch<CourseDetail>(`/courses/${courseId}`, {
    method: "PUT",
    body: JSON.stringify(input),
    token
  });
}

// ---------------------------------------------------------------------------
// Certificates
// ---------------------------------------------------------------------------

export type CertificateTier = "completion" | "participation";

export interface Certificate {
  id: string;
  enrollmentId: string;
  studentId: string;
  studentName: string;
  courseId: string;
  courseTitle: string;
  cohortId: string;
  cohortName: string;
  tier: CertificateTier;
  serialNumber: string;
  verificationCode: string;
  attendanceRate: number;
  assessmentsPassed: boolean;
  issuedByName: string;
  issuedAt: string;
  isRevoked: boolean;
  revokedAt: string | null;
  revocationReason: string | null;
}

export interface CertificateCandidate {
  enrollmentId: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  cohortId: string;
  cohortName: string;
  attendanceRate: number;
  attendanceThreshold: number;
  attendanceMet: boolean;
  assessmentsPassed: boolean;
  requiredAssessmentCount: number;
  recommendedTier: CertificateTier;
  existingCertificate: Certificate | null;
}

export interface CourseCertificateCandidates {
  courseId: string;
  courseTitle: string;
  candidates: CertificateCandidate[];
}

export function getCourseCertificateCandidates(
  courseId: string,
  token: string
): Promise<CourseCertificateCandidates> {
  return apiFetch<CourseCertificateCandidates>(`/courses/${courseId}/certificate-candidates`, { token });
}

export function issueCertificate(
  enrollmentId: string,
  tier: CertificateTier | null,
  token: string
): Promise<Certificate> {
  return apiFetch<Certificate>("/certificates", {
    method: "POST",
    body: JSON.stringify({ enrollmentId, tier }),
    token
  });
}

export function revokeCertificate(
  certificateId: string,
  reason: string | null,
  token: string
): Promise<Certificate> {
  return apiFetch<Certificate>(`/certificates/${certificateId}/revoke`, {
    method: "PUT",
    body: JSON.stringify({ reason }),
    token
  });
}

export function getMyCertificates(token: string): Promise<Certificate[]> {
  return apiFetch<Certificate[]>("/my-certificates", { token });
}

export function getCertificateById(certificateId: string, token: string): Promise<Certificate> {
  return apiFetch<Certificate>(`/certificates/${certificateId}`, { token });
}

export interface CertificateVerification {
  found: boolean;
  isValid: boolean;
  studentName: string | null;
  courseTitle: string | null;
  tier: CertificateTier | null;
  serialNumber: string | null;
  issuedAt: string | null;
  isRevoked: boolean;
}

/** Public — no auth token needed. */
export function verifyCertificate(code: string): Promise<CertificateVerification> {
  return apiFetch<CertificateVerification>(`/certificates/verify/${encodeURIComponent(code)}`);
}

// ---------------------------------------------------------------------------
// Analytics
// ---------------------------------------------------------------------------

export interface MonthlyCount {
  year: number;
  month: number;
  count: number;
}

export interface RevenueByCourse {
  courseId: string;
  title: string;
  revenue: number;
  approvedRequests: number;
}

export interface AdminBusinessDashboard {
  totalStudents: number;
  publishedCourses: number;
  publishedTracks: number;
  activeEnrollments: number;
  pendingEnrollmentRequests: number;
  totalRevenue: number;
  totalLeads: number;
  uncontactedLeads: number;
  openCohorts: number;
  topCoursesByRevenue: RevenueByCourse[];
  enrollmentsByMonth: MonthlyCount[];
}

export function getAdminBusinessDashboard(token: string): Promise<AdminBusinessDashboard> {
  return apiFetch<AdminBusinessDashboard>("/analytics/admin/business", { token });
}

export interface CourseAcademicRow {
  courseId: string;
  title: string;
  activeEnrollments: number;
  assessments: number;
  submittedAttempts: number;
  assessmentPassRate: number;
  certificatesIssued: number;
}

export interface AdminAcademicDashboard {
  certificatesIssued: number;
  completionCertificates: number;
  participationCertificates: number;
  revokedCertificates: number;
  totalAssessments: number;
  submittedAttempts: number;
  assessmentPassRate: number;
  totalAssignments: number;
  totalSubmissions: number;
  courses: CourseAcademicRow[];
}

export function getAdminAcademicDashboard(token: string): Promise<AdminAcademicDashboard> {
  return apiFetch<AdminAcademicDashboard>("/analytics/admin/academic", { token });
}

export interface InstructorCourseRow {
  courseId: string;
  title: string;
  status: string;
  activeEnrollments: number;
  assessments: number;
  submittedAttempts: number;
  assessmentPassRate: number;
  certificatesIssued: number;
}

export interface InstructorDashboard {
  assignedCourses: number;
  totalActiveStudents: number;
  certificatesIssued: number;
  courses: InstructorCourseRow[];
}

export function getInstructorDashboard(token: string): Promise<InstructorDashboard> {
  return apiFetch<InstructorDashboard>("/analytics/instructor", { token });
}

// ---------------------------------------------------------------------------
// Admin: Users
// ---------------------------------------------------------------------------

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  phone: string | null;
  role: string;
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
}

export function getUsers(
  params: { role?: string; isActive?: boolean; search?: string },
  token: string
): Promise<AdminUser[]> {
  const query = new URLSearchParams();
  if (params.role) query.set("role", params.role);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.search) query.set("search", params.search);
  const qs = query.toString();
  return apiFetch<AdminUser[]>(`/users${qs ? `?${qs}` : ""}`, { token });
}

export function createInstructor(
  input: { fullName: string; email: string; phone?: string },
  token: string
): Promise<AdminUser> {
  return apiFetch<AdminUser>("/users/instructors", { method: "POST", body: JSON.stringify(input), token });
}

export function deactivateUser(userId: string, token: string): Promise<AdminUser> {
  return apiFetch<AdminUser>(`/users/${userId}/deactivate`, { method: "PUT", token });
}

export function reactivateUser(userId: string, token: string): Promise<AdminUser> {
  return apiFetch<AdminUser>(`/users/${userId}/reactivate`, { method: "PUT", token });
}

// ---------------------------------------------------------------------------
// Admin: Courses
// ---------------------------------------------------------------------------

export interface CourseMutationResult {
  courseId: string;
  message: string;
}

export function getAdminCourses(
  params: { status?: string; category?: string; search?: string },
  token: string
): Promise<CourseListItem[]> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.category) query.set("category", params.category);
  if (params.search) query.set("search", params.search);
  const qs = query.toString();
  return apiFetch<CourseListItem[]>(`/courses${qs ? `?${qs}` : ""}`, { token });
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
  token: string
): Promise<CourseDetail> {
  return apiFetch<CourseDetail>("/courses", { method: "POST", body: JSON.stringify(input), token });
}

export function publishCourse(courseId: string, token: string): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/publish`, { method: "PUT", token });
}

export function archiveCourse(courseId: string, token: string): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/archive`, { method: "PUT", token });
}

export function deleteCourse(courseId: string, token: string): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}`, { method: "DELETE", token });
}

export function assignInstructorToCourse(
  courseId: string,
  instructorId: string,
  token: string
): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/instructors/${instructorId}`, {
    method: "POST",
    token
  });
}

export function removeInstructorFromCourse(
  courseId: string,
  instructorId: string,
  token: string
): Promise<CourseMutationResult> {
  return apiFetch<CourseMutationResult>(`/courses/${courseId}/instructors/${instructorId}`, {
    method: "DELETE",
    token
  });
}

// ---------------------------------------------------------------------------
// Admin: Tracks
// ---------------------------------------------------------------------------

export interface TrackCourseEntry {
  courseId: string;
  courseTitle: string;
  courseSlug: string;
  coursePrice: number;
  sortOrder: number;
}

export interface TrackDetail {
  id: string;
  title: string;
  slug: string;
  description: string | null;
  thumbnailUrl: string | null;
  price: number;
  currency: string;
  status: string;
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
  courses: TrackCourseEntry[];
}

export interface TrackMutationResult {
  trackId: string;
  message: string;
}

export function getAdminTracks(
  params: { status?: string; search?: string },
  token: string
): Promise<TrackListItem[]> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  const qs = query.toString();
  return apiFetch<TrackListItem[]>(`/tracks${qs ? `?${qs}` : ""}`, { token });
}

export function getTrackById(trackId: string, token: string): Promise<TrackDetail> {
  return apiFetch<TrackDetail>(`/tracks/${trackId}`, { token });
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
  token: string
): Promise<TrackDetail> {
  return apiFetch<TrackDetail>("/tracks", { method: "POST", body: JSON.stringify(input), token });
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
  token: string
): Promise<TrackDetail> {
  return apiFetch<TrackDetail>(`/tracks/${trackId}`, { method: "PUT", body: JSON.stringify(input), token });
}

export function publishTrack(trackId: string, token: string): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}/publish`, { method: "PUT", token });
}

export function archiveTrack(trackId: string, token: string): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}/archive`, { method: "PUT", token });
}

export function deleteTrack(trackId: string, token: string): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}`, { method: "DELETE", token });
}

export function addCourseToTrack(
  trackId: string,
  courseId: string,
  sortOrder: number,
  token: string
): Promise<TrackCourseEntry> {
  return apiFetch<TrackCourseEntry>(`/tracks/${trackId}/courses/${courseId}`, {
    method: "POST",
    body: JSON.stringify({ sortOrder }),
    token
  });
}

export function removeCourseFromTrack(
  trackId: string,
  courseId: string,
  token: string
): Promise<TrackMutationResult> {
  return apiFetch<TrackMutationResult>(`/tracks/${trackId}/courses/${courseId}`, { method: "DELETE", token });
}

// ---------------------------------------------------------------------------
// Admin: Cohorts
// ---------------------------------------------------------------------------

export interface CohortMutationResult {
  cohortId: string;
  message: string;
}

export function getCourseCohortsAdmin(courseId: string, token: string): Promise<CohortInfo[]> {
  return apiFetch<CohortInfo[]>(`/courses/${courseId}/cohorts`, { token });
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
  token: string
): Promise<CohortInfo> {
  return apiFetch<CohortInfo>(`/courses/${courseId}/cohorts`, {
    method: "POST",
    body: JSON.stringify(input),
    token
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
  token: string
): Promise<CohortInfo> {
  return apiFetch<CohortInfo>(`/cohorts/${cohortId}`, { method: "PUT", body: JSON.stringify(input), token });
}

export function openCohort(cohortId: string, token: string): Promise<CohortMutationResult> {
  return apiFetch<CohortMutationResult>(`/cohorts/${cohortId}/open`, { method: "PUT", token });
}

export function cancelCohort(cohortId: string, token: string): Promise<CohortMutationResult> {
  return apiFetch<CohortMutationResult>(`/cohorts/${cohortId}/cancel`, { method: "PUT", token });
}

export function completeCohort(cohortId: string, token: string): Promise<CohortMutationResult> {
  return apiFetch<CohortMutationResult>(`/cohorts/${cohortId}/complete`, { method: "PUT", token });
}

// ---------------------------------------------------------------------------
// Admin: Coupons
// ---------------------------------------------------------------------------

export interface AdminCoupon {
  id: string;
  code: string;
  type: string;
  value: number;
  isActive: boolean;
  validFrom: string | null;
  validUntil: string | null;
  usageLimit: number | null;
  usedCount: number;
  createdById: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string;
}

export function getCoupons(isActive: boolean | undefined, token: string): Promise<AdminCoupon[]> {
  const qs = isActive !== undefined ? `?isActive=${isActive}` : "";
  return apiFetch<AdminCoupon[]>(`/coupons${qs}`, { token });
}

export function createCoupon(
  input: { code: string; type: string; value: number; validFrom?: string | null; validUntil?: string | null; usageLimit?: number | null },
  token: string
): Promise<AdminCoupon> {
  return apiFetch<AdminCoupon>("/coupons", { method: "POST", body: JSON.stringify(input), token });
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
  token: string
): Promise<AdminCoupon> {
  return apiFetch<AdminCoupon>(`/coupons/${couponId}`, { method: "PUT", body: JSON.stringify(input), token });
}

export function deactivateCoupon(couponId: string, token: string): Promise<AdminCoupon> {
  return apiFetch<AdminCoupon>(`/coupons/${couponId}/deactivate`, { method: "PUT", token });
}

// ---------------------------------------------------------------------------
// Admin: Enrollment requests + enrollment cancellation
// ---------------------------------------------------------------------------

export interface EnrollmentRequestTargetCohort {
  cohortId: string;
  cohortName: string;
  courseId: string;
  courseTitle: string;
}

export interface EnrollmentRequestDetail {
  id: string;
  applicantName: string;
  applicantEmail: string;
  applicantPhone: string | null;
  courseId: string | null;
  courseTitle: string | null;
  trackId: string | null;
  trackTitle: string | null;
  paymentMethod: string;
  paymentProofDownloadUrl: string;
  originalPrice: number;
  couponCode: string | null;
  discountAmount: number;
  finalPrice: number;
  status: string;
  rejectionReason: string | null;
  reviewedById: string | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  createdAt: string;
  updatedAt: string;
  targetCohorts: EnrollmentRequestTargetCohort[];
  resultingEnrollmentIds: string[];
}

export function getEnrollmentRequests(
  params: { status?: string; courseId?: string; trackId?: string },
  token: string
): Promise<EnrollmentRequestResult[]> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.courseId) query.set("courseId", params.courseId);
  if (params.trackId) query.set("trackId", params.trackId);
  const qs = query.toString();
  return apiFetch<EnrollmentRequestResult[]>(`/enrollment-requests${qs ? `?${qs}` : ""}`, { token });
}

export function getEnrollmentRequestById(id: string, token: string): Promise<EnrollmentRequestDetail> {
  return apiFetch<EnrollmentRequestDetail>(`/enrollment-requests/${id}`, { token });
}

export interface EnrollmentApprovalResult {
  requestId: string;
  studentId: string;
  enrollmentIds: string[];
  studentCreated: boolean;
  message: string;
}

export function approveEnrollmentRequest(id: string, token: string): Promise<EnrollmentApprovalResult> {
  return apiFetch<EnrollmentApprovalResult>(`/enrollment-requests/${id}/approve`, { method: "PUT", token });
}

export function rejectEnrollmentRequest(
  id: string,
  rejectionReason: string,
  token: string
): Promise<{ requestId: string; message: string }> {
  return apiFetch(`/enrollment-requests/${id}/reject`, {
    method: "PUT",
    body: JSON.stringify({ rejectionReason }),
    token
  });
}

export function cancelEnrollment(
  enrollmentId: string,
  reason: string,
  markAsRefunded: boolean,
  token: string
): Promise<unknown> {
  return apiFetch(`/enrollments/${enrollmentId}/cancel`, {
    method: "PUT",
    body: JSON.stringify({ reason, markAsRefunded }),
    token
  });
}
