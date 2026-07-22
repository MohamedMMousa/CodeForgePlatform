// Thin fetch wrapper around the CodeForge ASP.NET API.
// Centralizes the base URL and error shape so feature code stays uniform.

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5205";

export interface ApiError {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiRequestError extends Error {
  constructor(public readonly info: ApiError) {
    super(info.detail ?? info.title ?? `Request failed (${info.status})`);
    this.name = "ApiRequestError";
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let body: Partial<ApiError> = {};
    try {
      body = await response.json();
    } catch {
      /* non-JSON error body */
    }
    throw new ApiRequestError({ status: response.status, ...body });
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
  paymentProofUrl: string;
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
  fileUrl: string | null;
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

export interface MyCourseModule {
  id: string;
  title: string;
  description: string | null;
  orderIndex: number;
  sessions: SessionItem[];
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
