// Minimal, dependency-free i18n for the app shell. Locales are expanded as new
// screens are built; this proves the bilingual (LTR/RTL) plumbing end to end.

export const locales = ["en", "ar"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "en";

export function isLocale(value: string): value is Locale {
  return (locales as readonly string[]).includes(value);
}

export function dir(locale: Locale): "ltr" | "rtl" {
  return locale === "ar" ? "rtl" : "ltr";
}

/**
 * `Course.Category` (and by extension `CourseListDto.category`) is free-text
 * admin input — there's no backend enum/constants list backing it (see
 * `ARCHITECTURE.md`'s catalog-migration note) — so it can't live in
 * `Dictionary` the way UI copy does: the same raw value needs a stable
 * bilingual label wherever it's shown (catalog chips, course-card eyebrow).
 * This is a starter set covering categories a MENA programming academy is
 * likely to use; it is deliberately NOT exhaustive. An admin-authored
 * category outside this map falls through to `titleCaseCategory` below
 * rather than leaking a lowercase slug into either language — the durable
 * fix is localized category labels in the data model, tracked as deferred
 * debt in `ARCHITECTURE.md`.
 */
const CATEGORY_LABELS: Record<string, { en: string; ar: string }> = {
  programming: { en: "Programming", ar: "البرمجة" },
  "web-development": { en: "Web Development", ar: "تطوير الويب" },
  "mobile-development": { en: "Mobile Development", ar: "تطوير تطبيقات الجوال" },
  "data-science": { en: "Data Science", ar: "علم البيانات" },
  "artificial-intelligence": { en: "Artificial Intelligence", ar: "الذكاء الاصطناعي" },
  cybersecurity: { en: "Cybersecurity", ar: "الأمن السيبراني" },
  design: { en: "Design", ar: "التصميم" },
  business: { en: "Business", ar: "إدارة الأعمال" }
};

/** "web-development" / "web development" -> "Web Development". Locale-agnostic —
 * it exists only to keep an unmapped raw value from reading as a lowercase slug. */
function titleCaseCategory(value: string): string {
  return value
    .replace(/[-_]+/g, " ")
    .trim()
    .replace(/\b\w/g, (char) => char.toUpperCase());
}

export function getCategoryLabel(category: string, locale: Locale): string {
  const entry = CATEGORY_LABELS[category.trim().toLowerCase()];
  return entry ? entry[locale] : titleCaseCategory(category);
}

/**
 * Picks the "{n} seats left" form. Arabic inflects the counted noun in four
 * bands — 1 (مفرد), 2 (مثنى), 3–10 (جمع قلة), 11+ (تمييز مفرد منصوب) — and using
 * one template for all of them produced "2 مقعد متبقٍ" / "9 مقعد متبقٍ", which are
 * both ungrammatical. English collapses the last three, so the same branches
 * serve both languages. Shared by the catalog card and the course-detail cohort
 * card so the two surfaces can't word the same state differently.
 */
export function getSeatsLeftTemplate(count: number, t: Dictionary): string {
  if (count === 1) return t.courseDetail.seatsLeft_one;
  if (count === 2) return t.courseDetail.seatsLeft_two;
  if (count <= 10) return t.courseDetail.seatsLeft_few;
  return t.courseDetail.seatsLeft;
}

export interface Dictionary {
  appName: string;
  tagline: string;
  home: {
    welcome: string;
    description: string;
    browseCourses: string;
    signIn: string;
    newHereHint: string;
  };
  login: {
    title: string;
    email: string;
    password: string;
    submit: string;
    signingIn: string;
    success: string;
    mustChange: string;
    failed: string;
    sessionExpired: string;
  };
  changePassword: {
    title: string;
    forcedNotice: string;
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
    submit: string;
    submitting: string;
    success: string;
    mismatch: string;
    wrongCurrent: string;
    failed: string;
  };
  nav: {
    switchTo: string;
    signOut: string;
    catalog: string;
    contact: string;
    restoringSession: string;
  };
  catalog: {
    title: string;
    subtitle: string;
    searchPlaceholder: string;
    tracksHeading: string;
    coursesHeading: string;
    trackBadge: string;
    coursesInTrack: string;
    viewDetails: string;
    empty: string;
    loadError: string;
    viewTrack: string;
    allCategories: string;
    categoriesUnavailable: string;
    noSearchResults: string;
    retry: string;
    showingFirstOfTotal: string;
    statusOpen: string;
    statusAwaiting: string;
    datesAnnouncedSoon: string;
    enroll: string;
    notifyMeShort: string;
    clearFilters: string;
    filterResultCount: string;
  };
  courseDetail: {
    back: string;
    about: string;
    batches: string;
    noBatches: string;
    enrollInBatch: string;
    // Four forms for the same reason as previouslyRun* below — Arabic needs
    // singular / dual / 3–10 / 11+. `seatsLeft` is the 11+ (and default) form.
    seatsLeft: string;
    seatsLeft_one: string;
    seatsLeft_two: string;
    seatsLeft_few: string;
    full: string;
    awaitingNextBatch: string;
    notifyMe: string;
    notifyMeSuccess: string;
    enrollmentClosesOn: string;
    startsOn: string;
    endsOn: string;
    enrollInCohort: string;
    enrollmentClosed: string;
    youWillJoin: string;
    noUpcomingBatches: string;
    notifyHeading: string;
    instructor: string;
    instructors: string;
    // Four forms, not one interpolation: Arabic distinguishes singular, dual,
    // 3–10 (مرات) and 11+ (مرة). English collapses the last three into "times".
    previouslyRunOnce: string;
    previouslyRunTwice: string;
    previouslyRunFew: string;
    previouslyRunMany: string;
  };
  // DESIGN_LANGUAGE.md §4 #4 — the student dashboard. Its own namespace rather
  // than more keys on `student`, which is shared copy the my-courses pages also
  // read; this surface's headings and empty states are written for it alone.
  dashboard: {
    title: string;
    nextSession: string;
    today: string;
    tomorrow: string;
    join: string;
    locatedAt: string;
    noSessionScheduled: string;
    noSessionScheduledHint: string;
    notEnrolled: string;
    notEnrolledHint: string;
    browseCatalog: string;
    myCourses: string;
    cohortDates: string;
    openCourse: string;
    progress: string;
    progressCourse: string;
    progressAttendance: string;
    progressAssessments: string;
    progressUnavailable: string;
    announcements: string;
    noAnnouncements: string;
    loadError: string;
    retry: string;
  };
  enroll: {
    title: string;
    fullName: string;
    email: string;
    phone: string;
    paymentMethod: string;
    paymentMethodPlaceholder: string;
    couponCode: string;
    couponOptional: string;
    applyCoupon: string;
    couponApplied: string;
    couponInvalid: string;
    priceOriginal: string;
    priceFinal: string;
    paymentProof: string;
    paymentProofHint: string;
    submit: string;
    submitting: string;
    success: string;
    error: string;
  };
  lead: {
    title: string;
    description: string;
    name: string;
    email: string;
    phone: string;
    message: string;
    submit: string;
    submitting: string;
    success: string;
    error: string;
  };
  instructor: {
    title: string;
    myCourses: string;
    noCourses: string;
    signInRequired: string;
    modules: string;
    addModule: string;
    moduleTitle: string;
    moduleDescription: string;
    add: string;
    cancel: string;
    delete: string;
    save: string;
    noModules: string;
    sessions: string;
    addSession: string;
    sessionType: string;
    typeLive: string;
    typeInPerson: string;
    typeRecorded: string;
    sessionTitle: string;
    sessionDescription: string;
    scheduledAt: string;
    duration: string;
    joinLink: string;
    location: string;
    videoUrl: string;
    noSessions: string;
    materials: string;
    addMaterial: string;
    materialType: string;
    typeText: string;
    typeLink: string;
    typeFile: string;
    materialTitle: string;
    materialBody: string;
    materialLink: string;
    materialFile: string;
    materialFileType: string;
    upload: string;
    noMaterials: string;
    downloadFile: string;
    announcements: string;
    addAnnouncement: string;
    announcementTitle: string;
    announcementBody: string;
    post: string;
    noAnnouncements: string;
    loadError: string;
    confirmDelete: string;
    attendance: string;
    markAttendance: string;
    saveAttendance: string;
    attendanceSaved: string;
    statusPresent: string;
    statusAbsent: string;
    statusLate: string;
    statusExcused: string;
    attendanceReport: string;
    noRoster: string;
    assessments: string;
    addAssessment: string;
    assessmentType: string;
    typeQuiz: string;
    typeExam: string;
    assessmentTitle: string;
    timeLimitMinutes: string;
    passScore: string;
    maxAttempts: string;
    unlimitedAttempts: string;
    practiceMode: string;
    randomizeQuestions: string;
    disableCopyPaste: string;
    noAssessments: string;
    questions: string;
    addQuestion: string;
    questionText: string;
    option: string;
    addOption: string;
    removeOption: string;
    correctAnswer: string;
    noQuestions: string;
    viewResults: string;
    assignments: string;
    addAssignment: string;
    assignmentTitle: string;
    instructions: string;
    dueDate: string;
    testCases: string;
    addTestCase: string;
    testInput: string;
    expectedOutput: string;
    hidden: string;
    points: string;
    noAssignments: string;
    noTestCases: string;
    submissions: string;
    noSubmissions: string;
    grade: string;
    manualScore: string;
    manualFeedback: string;
    saveGrade: string;
    graded: string;
    ungraded: string;
    gradebook: string;
    attendanceRate: string;
    bestScore: string;
    finalScore: string;
    attempt: string;
    late: string;
    autoScore: string;
  };
  student: {
    dashboard: string;
    myCourses: string;
    noCourses: string;
    coursesLoadError: string;
    upcomingSessions: string;
    noUpcoming: string;
    recentAnnouncements: string;
    noAnnouncements: string;
    goToCourse: string;
    courseContent: string;
    back: string;
    noModules: string;
    join: string;
    viewLocation: string;
    watchVideo: string;
    materials: string;
    noMaterials: string;
    downloadFile: string;
    live: string;
    inPerson: string;
    recordedLesson: string;
    loadError: string;
    attendance: string;
    myAttendance: string;
    sessionsHeld: string;
    sessionsPresent: string;
    quiz: string;
    exam: string;
    attempt: string;
    startAttempt: string;
    submitAttempt: string;
    timeLimit: string;
    attemptsUsed: string;
    of: string;
    noAttemptsLeft: string;
    score: string;
    passed: string;
    failed: string;
    yourResults: string;
    assignment: string;
    instructions: string;
    yourCode: string;
    submitAssignment: string;
    submitting: string;
    dueDate: string;
    late: string;
    autoScore: string;
    grading: string;
    gradingFailed: string;
    testResults: string;
    hiddenTest: string;
    grades: string;
    noGrades: string;
    certificates: string;
    noCertificates: string;
  };
  certificates: {
    title: string;
    myCertificates: string;
    noCertificates: string;
    completion: string;
    participation: string;
    tier: string;
    serial: string;
    issuedOn: string;
    issuedBy: string;
    verificationCode: string;
    status: string;
    valid: string;
    revoked: string;
    attendanceRate: string;
    assessmentsPassed: string;
    candidates: string;
    recommendedTier: string;
    attendanceMet: string;
    assessmentsMet: string;
    requiredAssessments: string;
    issue: string;
    issuing: string;
    issued: string;
    revoke: string;
    revoking: string;
    revokeReason: string;
    override: string;
    view: string;
    download: string;
    print: string;
    back: string;
    student: string;
    yes: string;
    no: string;
    verifyTitle: string;
    verifySubtitle: string;
    verifyPlaceholder: string;
    verifyButton: string;
    verifying: string;
    verifyValid: string;
    verifyRevoked: string;
    verifyNotFound: string;
    verifiedCertificate: string;
    loadError: string;
  };
  admin: {
    title: string;
    navCourses: string;
    navTracks: string;
    navCoupons: string;
    navEnrollmentRequests: string;
    navUsers: string;
    navAnalytics: string;

    loadError: string;
    save: string;
    saving: string;
    cancel: string;
    create: string;
    creating: string;
    edit: string;
    delete: string;
    confirmDelete: string;
    actions: string;
    search: string;
    status: string;
    all: string;

    fieldTitle: string;
    fieldSlug: string;
    slugHint: string;
    fieldDescription: string;
    fieldThumbnailUrl: string;
    fieldCategory: string;
    fieldPrice: string;
    fieldCurrency: string;

    coursesTitle: string;
    addCourse: string;
    noCourses: string;
    statusDraft: string;
    statusPublished: string;
    statusArchived: string;
    publish: string;
    archive: string;
    manageContent: string;

    instructorsTitle: string;
    assignInstructor: string;
    selectInstructor: string;
    removeInstructor: string;
    noInstructorsAssigned: string;

    cohortsTitle: string;
    addCohort: string;
    cohortName: string;
    startDate: string;
    endDate: string;
    enrollmentCutoffDate: string;
    capacity: string;
    gracePeriodDays: string;
    openCohort: string;
    cancelCohort: string;
    completeCohort: string;
    cohortStatusDraft: string;
    cohortStatusOpen: string;
    cohortStatusCancelled: string;
    cohortStatusCompleted: string;
    seatsLeft: string;
    enrolledCount: string;
    noCohorts: string;

    tracksTitle: string;
    addTrack: string;
    noTracks: string;
    coursesInTrack: string;
    addCourseToTrack: string;
    removeCourseFromTrack: string;
    sortOrder: string;
    selectCourse: string;

    couponsTitle: string;
    addCoupon: string;
    code: string;
    couponType: string;
    typePercent: string;
    typeFixed: string;
    value: string;
    validFrom: string;
    validUntil: string;
    usageLimit: string;
    usedCount: string;
    active: string;
    inactive: string;
    deactivate: string;
    noCoupons: string;

    requestsTitle: string;
    applicant: string;
    applicantEmailLabel: string;
    applicantPhoneLabel: string;
    courseOrTrack: string;
    paymentMethod: string;
    paymentProof: string;
    downloadPaymentProof: string;
    originalPrice: string;
    couponCode: string;
    discountAmount: string;
    finalPrice: string;
    submittedAt: string;
    requestStatusPending: string;
    requestStatusApproved: string;
    requestStatusRejected: string;
    approve: string;
    approving: string;
    reject: string;
    rejecting: string;
    rejectionReasonLabel: string;
    reviewedBy: string;
    reviewedAt: string;
    targetCohorts: string;
    resultingEnrollments: string;
    cancelEnrollment: string;
    cancelReasonLabel: string;
    markAsRefunded: string;
    noRequests: string;
    viewDetails: string;
    backToRequests: string;

    usersTitle: string;
    addInstructor: string;
    fullNameLabel: string;
    emailLabel: string;
    phoneLabel: string;
    roleLabel: string;
    roleAdmin: string;
    roleInstructor: string;
    roleStudent: string;
    isActiveLabel: string;
    reactivate: string;
    temporaryPasswordCreated: string;
    noUsers: string;
  };
  analytics: {
    title: string;
    businessDashboard: string;
    academicDashboard: string;
    instructorDashboard: string;
    totalStudents: string;
    publishedCourses: string;
    publishedTracks: string;
    activeEnrollments: string;
    pendingRequests: string;
    totalRevenue: string;
    totalLeads: string;
    uncontactedLeads: string;
    openCohorts: string;
    topCoursesByRevenue: string;
    enrollmentsByMonth: string;
    revenue: string;
    approvedRequests: string;
    certificatesIssued: string;
    completionCerts: string;
    participationCerts: string;
    revokedCerts: string;
    totalAssessments: string;
    submittedAttempts: string;
    passRate: string;
    totalAssignments: string;
    totalSubmissions: string;
    perCourse: string;
    course: string;
    students: string;
    assessments: string;
    attempts: string;
    certificates: string;
    assignedCourses: string;
    totalActiveStudents: string;
    status: string;
    noData: string;
    loadError: string;
  };
  pagination: {
    previous: string;
    next: string;
    pageOf: string;
    showingCount: string;
  };
  sentryTest: {
    title: string;
    description: string;
    clientButton: string;
    clientSent: string;
    serverButton: string;
    serverSent: string;
  };
  /** Copy for field-level validation failures. Keyed by the code the API returns
   * alongside each message — see lib/formErrors.ts and API_CONVENTIONS.md §4. */
  validation: {
    required: string;
    tooLong: string;
    slugFormat: string;
    slugTaken: string;
    timestampNotUtc: string;
    mustBeZeroOrMore: string;
    invalidEmail: string;
    invalidUrl: string;
    formError: string;
  };
}

const dictionaries: Record<Locale, Dictionary> = {
  en: {
    appName: "CodeForge Academy",
    tagline: "Build. Create. Launch.",
    home: {
      welcome: "Welcome to CodeForge Academy",
      description:
        "Live, cohort-based programming tracks for school and university students.",
      browseCourses: "Browse courses",
      signIn: "Sign in",
      newHereHint: "New here? Browse courses and enroll — no account needed."
    },
    login: {
      title: "Sign in",
      email: "Email",
      password: "Password",
      submit: "Sign in",
      signingIn: "Signing in…",
      success: "Signed in as {name} ({role}).",
      mustChange: "You must change your password before continuing.",
      failed: "Invalid email or password.",
      sessionExpired: "Your session expired. Please sign in again."
    },
    changePassword: {
      title: "Change your password",
      forcedNotice: "For security, you must set a new password before continuing.",
      currentPassword: "Current password",
      newPassword: "New password",
      confirmPassword: "Confirm new password",
      submit: "Change password",
      submitting: "Changing password…",
      success: "Password changed.",
      mismatch: "New password and confirmation do not match.",
      wrongCurrent: "Current password is incorrect.",
      failed: "Could not change the password. Please try again."
    },
    nav: {
      switchTo: "العربية",
      signOut: "Sign out",
      catalog: "Courses",
      contact: "Contact us",
      restoringSession: "Restoring your session…"
    },
    catalog: {
      title: "Course Catalog",
      subtitle: "Live, instructor-led programming tracks and courses.",
      searchPlaceholder: "Search courses…",
      tracksHeading: "Learning tracks",
      coursesHeading: "Courses",
      trackBadge: "Track",
      coursesInTrack: "{count} courses",
      viewDetails: "View details",
      empty: "No cohorts are open right now — check back soon.",
      loadError: "Could not load the catalog. Please try again.",
      viewTrack: "View track",
      allCategories: "All",
      categoriesUnavailable: "Categories aren't available yet.",
      noSearchResults: "No courses match your search or filters.",
      retry: "Try again",
      showingFirstOfTotal: "Showing the first {count} of {total} courses.",
      statusOpen: "Seats available",
      statusAwaiting: "Awaiting next batch",
      datesAnnouncedSoon: "Dates announced soon",
      enroll: "Enroll",
      notifyMeShort: "Notify me",
      clearFilters: "Clear filters",
      filterResultCount: "{count} of {total} courses match"
    },
    courseDetail: {
      back: "Back to catalog",
      about: "About this course",
      batches: "Upcoming batches",
      noBatches: "No batches are open for enrollment right now.",
      enrollInBatch: "Enroll in this batch",
      seatsLeft: "{count} seats left",
      seatsLeft_one: "1 seat left",
      seatsLeft_two: "2 seats left",
      seatsLeft_few: "{count} seats left",
      full: "Batch full",
      awaitingNextBatch: "This batch is full. Leave your details to be notified about the next one.",
      notifyMe: "Notify me for the next batch",
      notifyMeSuccess: "Thanks — we'll reach out when the next batch opens.",
      enrollmentClosesOn: "Enrollment closes {date}",
      startsOn: "Starts {date}",
      endsOn: "Ends {date}",
      // Names the batch the backend will actually enrol into — the API takes no
      // cohort id, it always resolves the earliest bookable one itself.
      enrollInCohort: "Enroll in the {cohort} cohort",
      enrollmentClosed: "Enrollment closed",
      youWillJoin: "You'll join this batch",
      noUpcomingBatches: "No upcoming batches are scheduled yet.",
      notifyHeading: "Get notified",
      instructor: "Instructor",
      instructors: "Instructors",
      previouslyRunOnce: "Previously run once",
      previouslyRunTwice: "Previously run twice",
      previouslyRunFew: "Previously run {count} times",
      previouslyRunMany: "Previously run {count} times"
    },
    dashboard: {
      title: "Dashboard",
      nextSession: "Next live session",
      today: "Today",
      tomorrow: "Tomorrow",
      join: "Join session",
      locatedAt: "Location: {location}",
      noSessionScheduled: "No live session scheduled",
      noSessionScheduledHint:
        "Your next class will appear here as soon as it is scheduled.",
      notEnrolled: "You are not enrolled in a cohort yet",
      notEnrolledHint:
        "Once your enrollment request is approved, your cohort and its live sessions appear here.",
      browseCatalog: "Browse courses",
      myCourses: "My courses",
      cohortDates: "{start} – {end}",
      openCourse: "Open course",
      progress: "Progress & attendance",
      progressCourse: "Course",
      progressAttendance: "Attendance",
      progressAssessments: "Assessments passed",
      progressUnavailable: "Unavailable",
      announcements: "Announcements",
      noAnnouncements: "No announcements yet.",
      loadError: "We could not load your dashboard.",
      retry: "Try again"
    },
    enroll: {
      title: "Enroll in {name}",
      fullName: "Full name",
      email: "Email",
      phone: "Phone number",
      paymentMethod: "Payment method",
      paymentMethodPlaceholder: "e.g. Bank transfer, Vodafone Cash",
      couponCode: "Coupon code",
      couponOptional: "optional",
      applyCoupon: "Apply",
      couponApplied: "Coupon applied: {label}",
      couponInvalid: "This coupon code is not valid.",
      priceOriginal: "Price",
      priceFinal: "Total after discount",
      paymentProof: "Payment proof",
      paymentProofHint: "Upload a screenshot or PDF receipt of your payment (JPG, PNG, WEBP or PDF).",
      submit: "Submit enrollment request",
      submitting: "Submitting…",
      success: "Your enrollment request was submitted. We'll review your payment proof and email you once approved.",
      error: "Could not submit your request. Please check the form and try again."
    },
    lead: {
      title: "Get in touch",
      description: "Have a question about our tracks? Leave your details and we'll follow up.",
      name: "Name",
      email: "Email",
      phone: "Phone number",
      message: "Message",
      submit: "Send message",
      submitting: "Sending…",
      success: "Thanks! We received your message and will get back to you soon.",
      error: "Could not send your message. Please try again."
    },
    instructor: {
      title: "Instructor",
      myCourses: "My courses",
      noCourses: "No courses are assigned to you yet.",
      signInRequired: "Please sign in as an instructor or admin to manage course content.",
      modules: "Modules",
      addModule: "Add module",
      moduleTitle: "Title",
      moduleDescription: "Description",
      add: "Add",
      cancel: "Cancel",
      delete: "Delete",
      save: "Save",
      noModules: "No modules yet.",
      sessions: "Sessions",
      addSession: "Add session",
      sessionType: "Type",
      typeLive: "Live",
      typeInPerson: "In-person",
      typeRecorded: "Pre-recorded lesson",
      sessionTitle: "Title",
      sessionDescription: "Description",
      scheduledAt: "Scheduled date/time",
      duration: "Duration (minutes)",
      joinLink: "Join link (Zoom/Meet/Teams)",
      location: "Location",
      videoUrl: "Video URL",
      noSessions: "No sessions yet.",
      materials: "Materials",
      addMaterial: "Add material",
      materialType: "Type",
      typeText: "Text",
      typeLink: "Link",
      typeFile: "File",
      materialTitle: "Title",
      materialBody: "Content",
      materialLink: "URL",
      materialFile: "File",
      materialFileType: "File type",
      upload: "Upload",
      noMaterials: "No materials yet.",
      downloadFile: "Download",
      announcements: "Announcements",
      addAnnouncement: "Post announcement",
      announcementTitle: "Title",
      announcementBody: "Message",
      post: "Post",
      noAnnouncements: "No announcements yet.",
      loadError: "Could not load this course's content.",
      confirmDelete: "Delete this? This cannot be undone.",
      attendance: "Attendance",
      markAttendance: "Mark attendance",
      saveAttendance: "Save attendance",
      attendanceSaved: "Attendance saved.",
      statusPresent: "Present",
      statusAbsent: "Absent",
      statusLate: "Late",
      statusExcused: "Excused",
      attendanceReport: "Attendance report",
      noRoster: "No enrolled students yet.",
      assessments: "Assessments",
      addAssessment: "Add assessment",
      assessmentType: "Type",
      typeQuiz: "Quiz",
      typeExam: "Exam",
      assessmentTitle: "Title",
      timeLimitMinutes: "Time limit (minutes)",
      passScore: "Pass score (%)",
      maxAttempts: "Max attempts",
      unlimitedAttempts: "unlimited",
      practiceMode: "Practice (not graded)",
      randomizeQuestions: "Randomize question order",
      disableCopyPaste: "Disable copy/paste",
      noAssessments: "No assessments yet.",
      questions: "Questions",
      addQuestion: "Add question",
      questionText: "Question",
      option: "Option",
      addOption: "Add option",
      removeOption: "Remove",
      correctAnswer: "Correct",
      noQuestions: "No questions yet.",
      viewResults: "View results",
      assignments: "Assignments",
      addAssignment: "Add assignment",
      assignmentTitle: "Title",
      instructions: "Instructions",
      dueDate: "Due date",
      testCases: "Test cases",
      addTestCase: "Add test case",
      testInput: "Input",
      expectedOutput: "Expected output",
      hidden: "Hidden",
      points: "Points",
      noAssignments: "No assignments yet.",
      noTestCases: "No test cases yet.",
      submissions: "Submissions",
      noSubmissions: "No submissions yet.",
      grade: "Grade",
      manualScore: "Score (0-100)",
      manualFeedback: "Feedback",
      saveGrade: "Save grade",
      graded: "Graded",
      ungraded: "Ungraded",
      gradebook: "Gradebook",
      attendanceRate: "Attendance",
      bestScore: "Best score",
      finalScore: "Final score",
      attempt: "Attempt",
      late: "late",
      autoScore: "Auto-score"
    },
    student: {
      dashboard: "Dashboard",
      myCourses: "My courses",
      noCourses: "You are not enrolled in any courses yet.",
      coursesLoadError: "Could not load your courses.",
      upcomingSessions: "Upcoming sessions",
      noUpcoming: "No upcoming sessions.",
      recentAnnouncements: "Announcements",
      noAnnouncements: "No announcements yet.",
      goToCourse: "Go to course",
      courseContent: "Course content",
      back: "Back",
      noModules: "This course doesn't have any content yet.",
      join: "Join session",
      viewLocation: "Location",
      watchVideo: "Watch",
      materials: "Materials",
      noMaterials: "No materials.",
      downloadFile: "Download",
      live: "Live",
      inPerson: "In-person",
      recordedLesson: "Recorded lesson",
      loadError: "Could not load your course content.",
      attendance: "Attendance",
      myAttendance: "My attendance",
      sessionsHeld: "Sessions held",
      sessionsPresent: "Sessions attended",
      quiz: "Quiz",
      exam: "Exam",
      attempt: "Attempt",
      startAttempt: "Start",
      submitAttempt: "Submit",
      timeLimit: "Time limit",
      attemptsUsed: "Attempts used",
      of: "of",
      noAttemptsLeft: "No attempts left.",
      score: "Score",
      passed: "Passed",
      failed: "Not passed",
      yourResults: "Your results",
      assignment: "Assignment",
      instructions: "Instructions",
      yourCode: "Your code",
      submitAssignment: "Submit",
      submitting: "Submitting…",
      dueDate: "Due date",
      late: "Late",
      autoScore: "Auto-graded score",
      grading: "Grading…",
      gradingFailed: "Auto-grading failed — your instructor will grade this manually.",
      testResults: "Test results",
      hiddenTest: "Hidden test",
      grades: "My grades",
      noGrades: "No grades yet.",
      certificates: "My certificates",
      noCertificates: "No certificates yet."
    },
    certificates: {
      title: "Certificates",
      myCertificates: "My certificates",
      noCertificates: "No certificates yet.",
      completion: "Completion",
      participation: "Participation",
      tier: "Tier",
      serial: "Serial",
      issuedOn: "Issued on",
      issuedBy: "Issued by",
      verificationCode: "Verification code",
      status: "Status",
      valid: "Valid",
      revoked: "Revoked",
      attendanceRate: "Attendance rate",
      assessmentsPassed: "Assessments passed",
      candidates: "Certificate candidates",
      recommendedTier: "Recommended",
      attendanceMet: "Attendance met",
      assessmentsMet: "Assessments passed",
      requiredAssessments: "Required assessments",
      issue: "Issue",
      issuing: "Issuing…",
      issued: "Issued",
      revoke: "Revoke",
      revoking: "Revoking…",
      revokeReason: "Reason for revocation (optional)",
      override: "Tier",
      view: "View",
      download: "Download",
      print: "Print / Save as PDF",
      back: "Back",
      student: "Student",
      yes: "Yes",
      no: "No",
      verifyTitle: "Verify a certificate",
      verifySubtitle: "Enter the verification code printed on a CodeForge certificate.",
      verifyPlaceholder: "Verification code",
      verifyButton: "Verify",
      verifying: "Verifying…",
      verifyValid: "This certificate is valid.",
      verifyRevoked: "This certificate has been revoked.",
      verifyNotFound: "No certificate matches that code.",
      verifiedCertificate: "Verified certificate",
      loadError: "Could not load certificates. Please try again."
    },
    admin: {
      title: "Admin",
      navCourses: "Courses",
      navTracks: "Tracks",
      navCoupons: "Coupons",
      navEnrollmentRequests: "Enrollment Requests",
      navUsers: "Users",
      navAnalytics: "Analytics",

      loadError: "Could not load. Please try again.",
      save: "Save",
      saving: "Saving…",
      cancel: "Cancel",
      create: "Create",
      creating: "Creating…",
      edit: "Edit",
      delete: "Delete",
      confirmDelete: "Are you sure you want to delete this?",
      actions: "Actions",
      search: "Search",
      status: "Status",
      all: "All",

      fieldTitle: "Title",
      fieldSlug: "Slug",
      slugHint: "Lowercase letters, numbers and hyphens only. Suggested from the title.",
      fieldDescription: "Description",
      fieldThumbnailUrl: "Thumbnail URL",
      fieldCategory: "Category",
      fieldPrice: "Price",
      fieldCurrency: "Currency",

      coursesTitle: "Courses",
      addCourse: "Add course",
      noCourses: "No courses yet.",
      statusDraft: "Draft",
      statusPublished: "Published",
      statusArchived: "Archived",
      publish: "Publish",
      archive: "Archive",
      manageContent: "Manage content",

      instructorsTitle: "Instructors",
      assignInstructor: "Assign",
      selectInstructor: "Select an instructor",
      removeInstructor: "Remove",
      noInstructorsAssigned: "No instructors assigned.",

      cohortsTitle: "Cohorts",
      addCohort: "Add cohort",
      cohortName: "Batch name",
      startDate: "Start date",
      endDate: "End date",
      enrollmentCutoffDate: "Enrollment cutoff",
      capacity: "Capacity",
      gracePeriodDays: "Grace period (days)",
      openCohort: "Open",
      cancelCohort: "Cancel",
      completeCohort: "Complete",
      cohortStatusDraft: "Draft",
      cohortStatusOpen: "Open",
      cohortStatusCancelled: "Cancelled",
      cohortStatusCompleted: "Completed",
      seatsLeft: "Seats left",
      enrolledCount: "Enrolled",
      noCohorts: "No cohorts yet.",

      tracksTitle: "Tracks",
      addTrack: "Add track",
      noTracks: "No tracks yet.",
      coursesInTrack: "Courses in this track",
      addCourseToTrack: "Add course",
      removeCourseFromTrack: "Remove",
      sortOrder: "Sort order",
      selectCourse: "Select a course",

      couponsTitle: "Coupons",
      addCoupon: "Add coupon",
      code: "Code",
      couponType: "Type",
      typePercent: "Percent",
      typeFixed: "Fixed amount",
      value: "Value",
      validFrom: "Valid from",
      validUntil: "Valid until",
      usageLimit: "Usage limit",
      usedCount: "Used",
      active: "Active",
      inactive: "Inactive",
      deactivate: "Deactivate",
      noCoupons: "No coupons yet.",

      requestsTitle: "Enrollment Requests",
      applicant: "Applicant",
      applicantEmailLabel: "Email",
      applicantPhoneLabel: "Phone",
      courseOrTrack: "Course / Track",
      paymentMethod: "Payment method",
      paymentProof: "Payment proof",
      downloadPaymentProof: "Download payment proof",
      originalPrice: "Original price",
      couponCode: "Coupon",
      discountAmount: "Discount",
      finalPrice: "Final price",
      submittedAt: "Submitted",
      requestStatusPending: "Pending",
      requestStatusApproved: "Approved",
      requestStatusRejected: "Rejected",
      approve: "Approve",
      approving: "Approving…",
      reject: "Reject",
      rejecting: "Rejecting…",
      rejectionReasonLabel: "Rejection reason",
      reviewedBy: "Reviewed by",
      reviewedAt: "Reviewed at",
      targetCohorts: "Target batches",
      resultingEnrollments: "Resulting enrollments",
      cancelEnrollment: "Cancel enrollment",
      cancelReasonLabel: "Cancellation reason",
      markAsRefunded: "Mark as refunded",
      noRequests: "No enrollment requests.",
      viewDetails: "View details",
      backToRequests: "Back to requests",

      usersTitle: "Users",
      addInstructor: "Add instructor",
      fullNameLabel: "Full name",
      emailLabel: "Email",
      phoneLabel: "Phone",
      roleLabel: "Role",
      roleAdmin: "Admin",
      roleInstructor: "Instructor",
      roleStudent: "Student",
      isActiveLabel: "Active",
      reactivate: "Reactivate",
      temporaryPasswordCreated: "Instructor account created. A temporary password was emailed to them.",
      noUsers: "No users found."
    },
    analytics: {
      title: "Analytics",
      businessDashboard: "Business overview",
      academicDashboard: "Academic overview",
      instructorDashboard: "My analytics",
      totalStudents: "Students",
      publishedCourses: "Published courses",
      publishedTracks: "Published tracks",
      activeEnrollments: "Active enrollments",
      pendingRequests: "Pending requests",
      totalRevenue: "Revenue (approved)",
      totalLeads: "Leads",
      uncontactedLeads: "Uncontacted leads",
      openCohorts: "Open cohorts",
      topCoursesByRevenue: "Top courses by revenue",
      enrollmentsByMonth: "Enrollments by month",
      revenue: "Revenue",
      approvedRequests: "Approved requests",
      certificatesIssued: "Certificates issued",
      completionCerts: "Completion",
      participationCerts: "Participation",
      revokedCerts: "Revoked",
      totalAssessments: "Assessments",
      submittedAttempts: "Submitted attempts",
      passRate: "Pass rate",
      totalAssignments: "Assignments",
      totalSubmissions: "Submissions",
      perCourse: "Per course",
      course: "Course",
      students: "Students",
      assessments: "Assessments",
      attempts: "Attempts",
      certificates: "Certificates",
      assignedCourses: "Assigned courses",
      totalActiveStudents: "Active students",
      status: "Status",
      noData: "No data yet.",
      loadError: "Could not load analytics. Please try again."
    },
    pagination: {
      previous: "Previous",
      next: "Next",
      pageOf: "Page {page} of {totalPages}",
      showingCount: "Showing {count} of {total}"
    },
    sentryTest: {
      title: "Sentry test page",
      description:
        "Temporary ops page for confirming error monitoring is wired up. Only reachable while SENTRY_TEST_ENABLED is set — turn it off once you're done.",
      clientButton: "Send test error (client)",
      clientSent: "Client-side test event sent to Sentry.",
      serverButton: "Send test error (server)",
      serverSent: "Server-side test event sent to Sentry."
    },
    validation: {
      required: "This field is required.",
      tooLong: "This value is too long.",
      slugFormat:
        "Use lowercase letters, numbers and hyphens only — for example: web-development-101.",
      slugTaken: "This slug is already in use. Try a different one.",
      timestampNotUtc: "Pick a date and time using the date picker.",
      mustBeZeroOrMore: "Enter zero or a positive number.",
      invalidEmail: "Enter a valid email address.",
      invalidUrl: "Enter a valid web address, e.g. zoom.us/j/123.",
      formError: "Something went wrong. Please try again."
    }
  },
  ar: {
    appName: "أكاديمية كود فورج",
    tagline: "ابنِ. أنشئ. أطلق.",
    home: {
      welcome: "مرحبًا بك في أكاديمية كود فورج",
      description: "مسارات برمجة مباشرة بنظام الدفعات لطلاب المدارس والجامعات.",
      browseCourses: "تصفّح الدورات",
      signIn: "تسجيل الدخول",
      newHereHint: "جديد هنا؟ تصفح الدورات وسجّل الآن — بدون حاجة لإنشاء حساب."
    },
    login: {
      title: "تسجيل الدخول",
      email: "البريد الإلكتروني",
      password: "كلمة المرور",
      submit: "تسجيل الدخول",
      signingIn: "جارٍ تسجيل الدخول…",
      success: "تم تسجيل الدخول باسم {name} ({role}).",
      mustChange: "يجب تغيير كلمة المرور قبل المتابعة.",
      failed: "البريد الإلكتروني أو كلمة المرور غير صحيحة.",
      sessionExpired: "انتهت صلاحية جلستك. يرجى تسجيل الدخول مرة أخرى."
    },
    changePassword: {
      title: "تغيير كلمة المرور",
      forcedNotice: "لأسباب أمنية، يجب تعيين كلمة مرور جديدة قبل المتابعة.",
      currentPassword: "كلمة المرور الحالية",
      newPassword: "كلمة المرور الجديدة",
      confirmPassword: "تأكيد كلمة المرور الجديدة",
      submit: "تغيير كلمة المرور",
      submitting: "جارٍ تغيير كلمة المرور…",
      success: "تم تغيير كلمة المرور.",
      mismatch: "كلمة المرور الجديدة وتأكيدها غير متطابقين.",
      wrongCurrent: "كلمة المرور الحالية غير صحيحة.",
      failed: "تعذّر تغيير كلمة المرور. يرجى المحاولة مرة أخرى."
    },
    nav: {
      switchTo: "English",
      signOut: "تسجيل الخروج",
      catalog: "الدورات",
      contact: "تواصل معنا",
      restoringSession: "جارٍ استعادة جلستك…"
    },
    catalog: {
      title: "دليل الدورات",
      subtitle: "مسارات ودورات برمجة مباشرة بإشراف مدربين.",
      searchPlaceholder: "ابحث عن دورة…",
      tracksHeading: "المسارات",
      coursesHeading: "الدورات",
      trackBadge: "مسار",
      coursesInTrack: "{count} دورات",
      viewDetails: "عرض التفاصيل",
      empty: "لا توجد دفعات متاحة حاليًا — يرجى المحاولة لاحقًا.",
      loadError: "تعذّر تحميل الدليل. يرجى المحاولة مرة أخرى.",
      viewTrack: "عرض المسار",
      allCategories: "الكل",
      categoriesUnavailable: "التصنيفات غير متاحة حاليًا.",
      noSearchResults: "لا توجد دورات مطابقة لبحثك أو عوامل التصفية.",
      retry: "أعد المحاولة",
      showingFirstOfTotal: "عرض أول {count} من {total} دورة.",
      statusOpen: "مقاعد متاحة",
      statusAwaiting: "بانتظار الدفعة القادمة",
      datesAnnouncedSoon: "سيتم الإعلان عن المواعيد قريبًا",
      enroll: "سجّل الآن",
      notifyMeShort: "أعلمني",
      clearFilters: "مسح عوامل التصفية",
      filterResultCount: "{count} من {total} دورة مطابقة"
    },
    courseDetail: {
      back: "العودة إلى الدليل",
      about: "عن هذه الدورة",
      batches: "الدفعات القادمة",
      noBatches: "لا توجد دفعات متاحة للتسجيل حاليًا.",
      enrollInBatch: "التسجيل في هذه الدفعة",
      seatsLeft: "{count} مقعد متبقٍ",
      seatsLeft_one: "مقعد واحد متبقٍ",
      seatsLeft_two: "مقعدان متبقيان",
      seatsLeft_few: "{count} مقاعد متبقية",
      full: "الدفعة مكتملة",
      awaitingNextBatch: "هذه الدفعة مكتملة. اترك بياناتك ليتم إعلامك بالدفعة القادمة.",
      notifyMe: "أعلمني بالدفعة القادمة",
      notifyMeSuccess: "شكرًا لك — سنتواصل معك عند فتح الدفعة القادمة.",
      enrollmentClosesOn: "يُغلق التسجيل في {date}",
      startsOn: "يبدأ في {date}",
      endsOn: "ينتهي في {date}",
      enrollInCohort: "سجّل في دفعة {cohort}",
      enrollmentClosed: "أُغلق التسجيل",
      youWillJoin: "ستنضم إلى هذه الدفعة",
      noUpcomingBatches: "لا توجد دفعات قادمة مجدولة بعد.",
      notifyHeading: "احصل على إشعار",
      instructor: "المدرب",
      instructors: "المدربون",
      // Arabic plural rules: 1 (مفرد), 2 (مثنى), 3–10 (جمع قلة — مرات),
      // 11+ (تمييز مفرد منصوب — مرة). Flagged for native review.
      previouslyRunOnce: "أُقيمت من قبل مرة واحدة",
      previouslyRunTwice: "أُقيمت من قبل مرتين",
      previouslyRunFew: "أُقيمت من قبل {count} مرات",
      previouslyRunMany: "أُقيمت من قبل {count} مرة"
    },
    dashboard: {
      title: "لوحة التحكم",
      nextSession: "الجلسة المباشرة القادمة",
      today: "اليوم",
      tomorrow: "غدًا",
      join: "انضم إلى الجلسة",
      locatedAt: "المكان: {location}",
      noSessionScheduled: "لا توجد جلسة مباشرة مجدولة",
      noSessionScheduledHint: "ستظهر محاضرتك القادمة هنا فور جدولتها.",
      notEnrolled: "لم تنضم إلى أي دفعة بعد",
      notEnrolledHint:
        "بمجرد الموافقة على طلب تسجيلك، ستظهر هنا دفعتك وجلساتها المباشرة.",
      browseCatalog: "تصفح الدورات",
      myCourses: "دوراتي",
      cohortDates: "{start} – {end}",
      openCourse: "افتح الدورة",
      progress: "التقدم والحضور",
      progressCourse: "الدورة",
      progressAttendance: "الحضور",
      progressAssessments: "الاختبارات المجتازة",
      progressUnavailable: "غير متاح",
      announcements: "الإعلانات",
      noAnnouncements: "لا توجد إعلانات بعد.",
      loadError: "تعذّر تحميل لوحة التحكم.",
      retry: "أعد المحاولة"
    },
    enroll: {
      title: "التسجيل في {name}",
      fullName: "الاسم الكامل",
      email: "البريد الإلكتروني",
      phone: "رقم الهاتف",
      paymentMethod: "طريقة الدفع",
      paymentMethodPlaceholder: "مثال: تحويل بنكي، فودافون كاش",
      couponCode: "كود الخصم",
      couponOptional: "اختياري",
      applyCoupon: "تطبيق",
      couponApplied: "تم تطبيق الكوبون: {label}",
      couponInvalid: "كود الخصم غير صالح.",
      priceOriginal: "السعر",
      priceFinal: "الإجمالي بعد الخصم",
      paymentProof: "إثبات الدفع",
      paymentProofHint: "ارفع لقطة شاشة أو ملف PDF لإيصال الدفع (JPG أو PNG أو WEBP أو PDF).",
      submit: "إرسال طلب التسجيل",
      submitting: "جارٍ الإرسال…",
      success: "تم إرسال طلب التسجيل. سنراجع إثبات الدفع ونرسل لك بريدًا إلكترونيًا عند الموافقة.",
      error: "تعذّر إرسال طلبك. يرجى التحقق من النموذج والمحاولة مرة أخرى."
    },
    lead: {
      title: "تواصل معنا",
      description: "لديك سؤال عن مساراتنا؟ اترك بياناتك وسنتواصل معك.",
      name: "الاسم",
      email: "البريد الإلكتروني",
      phone: "رقم الهاتف",
      message: "الرسالة",
      submit: "إرسال الرسالة",
      submitting: "جارٍ الإرسال…",
      success: "شكرًا لك! استلمنا رسالتك وسنتواصل معك قريبًا.",
      error: "تعذّر إرسال رسالتك. يرجى المحاولة مرة أخرى."
    },
    instructor: {
      title: "المدرّس",
      myCourses: "دوراتي",
      noCourses: "لا توجد دورات مسندة إليك حتى الآن.",
      signInRequired: "يرجى تسجيل الدخول كمدرّس أو مسؤول لإدارة محتوى الدورة.",
      modules: "الوحدات",
      addModule: "إضافة وحدة",
      moduleTitle: "العنوان",
      moduleDescription: "الوصف",
      add: "إضافة",
      cancel: "إلغاء",
      delete: "حذف",
      save: "حفظ",
      noModules: "لا توجد وحدات بعد.",
      sessions: "الجلسات",
      addSession: "إضافة جلسة",
      sessionType: "النوع",
      typeLive: "مباشرة",
      typeInPerson: "حضورية",
      typeRecorded: "درس مسجل",
      sessionTitle: "العنوان",
      sessionDescription: "الوصف",
      scheduledAt: "تاريخ ووقت الجلسة",
      duration: "المدة (دقائق)",
      joinLink: "رابط الانضمام (Zoom/Meet/Teams)",
      location: "الموقع",
      videoUrl: "رابط الفيديو",
      noSessions: "لا توجد جلسات بعد.",
      materials: "المواد",
      addMaterial: "إضافة مادة",
      materialType: "النوع",
      typeText: "نص",
      typeLink: "رابط",
      typeFile: "ملف",
      materialTitle: "العنوان",
      materialBody: "المحتوى",
      materialLink: "الرابط",
      materialFile: "الملف",
      materialFileType: "نوع الملف",
      upload: "رفع",
      noMaterials: "لا توجد مواد بعد.",
      downloadFile: "تنزيل",
      announcements: "الإعلانات",
      addAnnouncement: "نشر إعلان",
      announcementTitle: "العنوان",
      announcementBody: "الرسالة",
      post: "نشر",
      noAnnouncements: "لا توجد إعلانات بعد.",
      loadError: "تعذّر تحميل محتوى هذه الدورة.",
      confirmDelete: "هل تريد حذف هذا العنصر؟ لا يمكن التراجع عن هذا.",
      attendance: "الحضور",
      markAttendance: "تسجيل الحضور",
      saveAttendance: "حفظ الحضور",
      attendanceSaved: "تم حفظ الحضور.",
      statusPresent: "حاضر",
      statusAbsent: "غائب",
      statusLate: "متأخر",
      statusExcused: "معذور",
      attendanceReport: "تقرير الحضور",
      noRoster: "لا يوجد طلاب مسجلون بعد.",
      assessments: "التقييمات",
      addAssessment: "إضافة تقييم",
      assessmentType: "النوع",
      typeQuiz: "اختبار قصير",
      typeExam: "امتحان",
      assessmentTitle: "العنوان",
      timeLimitMinutes: "الوقت المحدد (دقائق)",
      passScore: "درجة النجاح (%)",
      maxAttempts: "الحد الأقصى للمحاولات",
      unlimitedAttempts: "غير محدود",
      practiceMode: "تدريب (غير مقيَّم)",
      randomizeQuestions: "ترتيب عشوائي للأسئلة",
      disableCopyPaste: "تعطيل النسخ واللصق",
      noAssessments: "لا توجد تقييمات بعد.",
      questions: "الأسئلة",
      addQuestion: "إضافة سؤال",
      questionText: "السؤال",
      option: "خيار",
      addOption: "إضافة خيار",
      removeOption: "إزالة",
      correctAnswer: "صحيح",
      noQuestions: "لا توجد أسئلة بعد.",
      viewResults: "عرض النتائج",
      assignments: "الواجبات",
      addAssignment: "إضافة واجب",
      assignmentTitle: "العنوان",
      instructions: "التعليمات",
      dueDate: "تاريخ التسليم",
      testCases: "حالات الاختبار",
      addTestCase: "إضافة حالة اختبار",
      testInput: "المدخل",
      expectedOutput: "المخرج المتوقع",
      hidden: "مخفية",
      points: "النقاط",
      noAssignments: "لا توجد واجبات بعد.",
      noTestCases: "لا توجد حالات اختبار بعد.",
      submissions: "التسليمات",
      noSubmissions: "لا توجد تسليمات بعد.",
      grade: "تقييم",
      manualScore: "الدرجة (0-100)",
      manualFeedback: "الملاحظات",
      saveGrade: "حفظ التقييم",
      graded: "تم التقييم",
      ungraded: "لم يُقيَّم",
      gradebook: "سجل الدرجات",
      attendanceRate: "الحضور",
      bestScore: "أفضل درجة",
      finalScore: "الدرجة النهائية",
      attempt: "محاولة",
      late: "متأخر",
      autoScore: "الدرجة التلقائية"
    },
    student: {
      dashboard: "لوحة التحكم",
      myCourses: "دوراتي",
      noCourses: "لست مسجّلاً في أي دورة حتى الآن.",
      coursesLoadError: "تعذّر تحميل دوراتك.",
      upcomingSessions: "الجلسات القادمة",
      noUpcoming: "لا توجد جلسات قادمة.",
      recentAnnouncements: "الإعلانات",
      noAnnouncements: "لا توجد إعلانات بعد.",
      goToCourse: "الانتقال إلى الدورة",
      courseContent: "محتوى الدورة",
      back: "رجوع",
      noModules: "لا يحتوي هذا المقرر على محتوى بعد.",
      join: "الانضمام إلى الجلسة",
      viewLocation: "الموقع",
      watchVideo: "مشاهدة",
      materials: "المواد",
      noMaterials: "لا توجد مواد.",
      downloadFile: "تنزيل",
      live: "مباشرة",
      inPerson: "حضورية",
      recordedLesson: "درس مسجل",
      loadError: "تعذّر تحميل محتوى دوراتك.",
      attendance: "الحضور",
      myAttendance: "حضوري",
      sessionsHeld: "الجلسات المنعقدة",
      sessionsPresent: "الجلسات التي حضرتها",
      quiz: "اختبار قصير",
      exam: "امتحان",
      attempt: "محاولة",
      startAttempt: "بدء",
      submitAttempt: "إرسال",
      timeLimit: "الوقت المحدد",
      attemptsUsed: "المحاولات المستخدمة",
      of: "من",
      noAttemptsLeft: "لا توجد محاولات متبقية.",
      score: "الدرجة",
      passed: "ناجح",
      failed: "غير ناجح",
      yourResults: "نتائجك",
      assignment: "الواجب",
      instructions: "التعليمات",
      yourCode: "الكود الخاص بك",
      submitAssignment: "إرسال",
      submitting: "جارٍ الإرسال…",
      dueDate: "تاريخ التسليم",
      late: "متأخر",
      autoScore: "الدرجة التلقائية",
      grading: "جارٍ التصحيح…",
      gradingFailed: "فشل التصحيح التلقائي — سيقوم المدرّس بتصحيح هذا يدويًا.",
      testResults: "نتائج الاختبار",
      hiddenTest: "اختبار مخفي",
      grades: "درجاتي",
      noGrades: "لا توجد درجات بعد.",
      certificates: "شهاداتي",
      noCertificates: "لا توجد شهادات بعد."
    },
    certificates: {
      title: "الشهادات",
      myCertificates: "شهاداتي",
      noCertificates: "لا توجد شهادات بعد.",
      completion: "إتمام",
      participation: "مشاركة",
      tier: "النوع",
      serial: "الرقم التسلسلي",
      issuedOn: "تاريخ الإصدار",
      issuedBy: "أصدرها",
      verificationCode: "رمز التحقق",
      status: "الحالة",
      valid: "سارية",
      revoked: "ملغاة",
      attendanceRate: "نسبة الحضور",
      assessmentsPassed: "اجتياز التقييمات",
      candidates: "المرشحون للشهادة",
      recommendedTier: "الموصى به",
      attendanceMet: "تحقق الحضور",
      assessmentsMet: "اجتياز التقييمات",
      requiredAssessments: "التقييمات المطلوبة",
      issue: "إصدار",
      issuing: "جارٍ الإصدار…",
      issued: "تم الإصدار",
      revoke: "إلغاء",
      revoking: "جارٍ الإلغاء…",
      revokeReason: "سبب الإلغاء (اختياري)",
      override: "النوع",
      view: "عرض",
      download: "تنزيل",
      print: "طباعة / حفظ PDF",
      back: "رجوع",
      student: "الطالب",
      yes: "نعم",
      no: "لا",
      verifyTitle: "التحقق من شهادة",
      verifySubtitle: "أدخل رمز التحقق المطبوع على شهادة كود فورج.",
      verifyPlaceholder: "رمز التحقق",
      verifyButton: "تحقق",
      verifying: "جارٍ التحقق…",
      verifyValid: "هذه الشهادة سارية.",
      verifyRevoked: "تم إلغاء هذه الشهادة.",
      verifyNotFound: "لا توجد شهادة مطابقة لهذا الرمز.",
      verifiedCertificate: "شهادة موثّقة",
      loadError: "تعذّر تحميل الشهادات. حاول مرة أخرى."
    },
    admin: {
      title: "الإدارة",
      navCourses: "الدورات",
      navTracks: "المسارات",
      navCoupons: "الكوبونات",
      navEnrollmentRequests: "طلبات التسجيل",
      navUsers: "المستخدمون",
      navAnalytics: "التحليلات",

      loadError: "تعذّر التحميل. حاول مرة أخرى.",
      save: "حفظ",
      saving: "جارٍ الحفظ…",
      cancel: "إلغاء",
      create: "إنشاء",
      creating: "جارٍ الإنشاء…",
      edit: "تعديل",
      delete: "حذف",
      confirmDelete: "هل أنت متأكد من الحذف؟",
      actions: "الإجراءات",
      search: "بحث",
      status: "الحالة",
      all: "الكل",

      fieldTitle: "العنوان",
      fieldSlug: "المعرّف (slug)",
      slugHint: "حروف إنجليزية صغيرة وأرقام وشرطات فقط. يُقترح تلقائيًا من العنوان.",
      fieldDescription: "الوصف",
      fieldThumbnailUrl: "رابط الصورة المصغرة",
      fieldCategory: "الفئة",
      fieldPrice: "السعر",
      fieldCurrency: "العملة",

      coursesTitle: "الدورات",
      addCourse: "إضافة دورة",
      noCourses: "لا توجد دورات بعد.",
      statusDraft: "مسودة",
      statusPublished: "منشورة",
      statusArchived: "مؤرشفة",
      publish: "نشر",
      archive: "أرشفة",
      manageContent: "إدارة المحتوى",

      instructorsTitle: "المدرّسون",
      assignInstructor: "تعيين",
      selectInstructor: "اختر مدرّسًا",
      removeInstructor: "إزالة",
      noInstructorsAssigned: "لا يوجد مدرّسون معيّنون.",

      cohortsTitle: "الدفعات",
      addCohort: "إضافة دفعة",
      cohortName: "اسم الدفعة",
      startDate: "تاريخ البدء",
      endDate: "تاريخ الانتهاء",
      enrollmentCutoffDate: "آخر موعد للتسجيل",
      capacity: "السعة",
      gracePeriodDays: "فترة السماح (أيام)",
      openCohort: "فتح",
      cancelCohort: "إلغاء",
      completeCohort: "إنهاء",
      cohortStatusDraft: "مسودة",
      cohortStatusOpen: "مفتوحة",
      cohortStatusCancelled: "ملغاة",
      cohortStatusCompleted: "منتهية",
      seatsLeft: "المقاعد المتبقية",
      enrolledCount: "المسجّلون",
      noCohorts: "لا توجد دفعات بعد.",

      tracksTitle: "المسارات",
      addTrack: "إضافة مسار",
      noTracks: "لا توجد مسارات بعد.",
      coursesInTrack: "الدورات ضمن هذا المسار",
      addCourseToTrack: "إضافة دورة",
      removeCourseFromTrack: "إزالة",
      sortOrder: "الترتيب",
      selectCourse: "اختر دورة",

      couponsTitle: "الكوبونات",
      addCoupon: "إضافة كوبون",
      code: "الرمز",
      couponType: "النوع",
      typePercent: "نسبة مئوية",
      typeFixed: "مبلغ ثابت",
      value: "القيمة",
      validFrom: "صالح من",
      validUntil: "صالح حتى",
      usageLimit: "حد الاستخدام",
      usedCount: "مرات الاستخدام",
      active: "فعّال",
      inactive: "غير فعّال",
      deactivate: "إيقاف",
      noCoupons: "لا توجد كوبونات بعد.",

      requestsTitle: "طلبات التسجيل",
      applicant: "المتقدّم",
      applicantEmailLabel: "البريد الإلكتروني",
      applicantPhoneLabel: "الهاتف",
      courseOrTrack: "الدورة / المسار",
      paymentMethod: "طريقة الدفع",
      paymentProof: "إثبات الدفع",
      downloadPaymentProof: "تنزيل إثبات الدفع",
      originalPrice: "السعر الأصلي",
      couponCode: "الكوبون",
      discountAmount: "الخصم",
      finalPrice: "السعر النهائي",
      submittedAt: "تاريخ التقديم",
      requestStatusPending: "قيد الانتظار",
      requestStatusApproved: "مقبول",
      requestStatusRejected: "مرفوض",
      approve: "قبول",
      approving: "جارٍ القبول…",
      reject: "رفض",
      rejecting: "جارٍ الرفض…",
      rejectionReasonLabel: "سبب الرفض",
      reviewedBy: "تمت المراجعة بواسطة",
      reviewedAt: "تاريخ المراجعة",
      targetCohorts: "الدفعات المستهدفة",
      resultingEnrollments: "التسجيلات الناتجة",
      cancelEnrollment: "إلغاء التسجيل",
      cancelReasonLabel: "سبب الإلغاء",
      markAsRefunded: "وضع علامة كمسترد",
      noRequests: "لا توجد طلبات تسجيل.",
      viewDetails: "عرض التفاصيل",
      backToRequests: "العودة إلى الطلبات",

      usersTitle: "المستخدمون",
      addInstructor: "إضافة مدرّس",
      fullNameLabel: "الاسم الكامل",
      emailLabel: "البريد الإلكتروني",
      phoneLabel: "الهاتف",
      roleLabel: "الدور",
      roleAdmin: "مدير",
      roleInstructor: "مدرّس",
      roleStudent: "طالب",
      isActiveLabel: "فعّال",
      reactivate: "إعادة تفعيل",
      temporaryPasswordCreated: "تم إنشاء حساب المدرّس. تم إرسال كلمة مرور مؤقتة عبر البريد الإلكتروني.",
      noUsers: "لم يتم العثور على مستخدمين."
    },
    analytics: {
      title: "التحليلات",
      businessDashboard: "نظرة عامة على الأعمال",
      academicDashboard: "نظرة عامة أكاديمية",
      instructorDashboard: "تحليلاتي",
      totalStudents: "الطلاب",
      publishedCourses: "الدورات المنشورة",
      publishedTracks: "المسارات المنشورة",
      activeEnrollments: "التسجيلات النشطة",
      pendingRequests: "الطلبات المعلّقة",
      totalRevenue: "الإيرادات (المعتمدة)",
      totalLeads: "العملاء المحتملون",
      uncontactedLeads: "لم يتم التواصل معهم",
      openCohorts: "المجموعات المفتوحة",
      topCoursesByRevenue: "أعلى الدورات إيرادًا",
      enrollmentsByMonth: "التسجيلات شهريًا",
      revenue: "الإيرادات",
      approvedRequests: "الطلبات المعتمدة",
      certificatesIssued: "الشهادات الصادرة",
      completionCerts: "إتمام",
      participationCerts: "مشاركة",
      revokedCerts: "ملغاة",
      totalAssessments: "التقييمات",
      submittedAttempts: "المحاولات المُسلَّمة",
      passRate: "نسبة النجاح",
      totalAssignments: "الواجبات",
      totalSubmissions: "التسليمات",
      perCourse: "حسب الدورة",
      course: "الدورة",
      students: "الطلاب",
      assessments: "التقييمات",
      attempts: "المحاولات",
      certificates: "الشهادات",
      assignedCourses: "الدورات المسندة",
      totalActiveStudents: "الطلاب النشطون",
      status: "الحالة",
      noData: "لا توجد بيانات بعد.",
      loadError: "تعذّر تحميل التحليلات. حاول مرة أخرى."
    },
    pagination: {
      previous: "السابق",
      next: "التالي",
      pageOf: "صفحة {page} من {totalPages}",
      showingCount: "عرض {count} من {total}"
    },
    sentryTest: {
      title: "صفحة اختبار Sentry",
      description:
        "صفحة تشغيلية مؤقتة للتأكد من عمل مراقبة الأخطاء. لا يمكن الوصول إليها إلا عند تفعيل SENTRY_TEST_ENABLED — عطّلها بعد الانتهاء.",
      clientButton: "إرسال خطأ اختباري (العميل)",
      clientSent: "تم إرسال حدث اختباري من جهة العميل إلى Sentry.",
      serverButton: "إرسال خطأ اختباري (الخادم)",
      serverSent: "تم إرسال حدث اختباري من جهة الخادم إلى Sentry."
    },
    validation: {
      required: "هذا الحقل مطلوب.",
      tooLong: "هذه القيمة أطول من المسموح.",
      // The slug rule is Latin-only, so the example stays Latin in both locales.
      slugFormat:
        "استخدم حروفًا إنجليزية صغيرة وأرقامًا وشرطات فقط — مثال: web-development-101.",
      slugTaken: "هذا المعرّف مستخدم بالفعل. جرّب معرّفًا آخر.",
      timestampNotUtc: "اختر التاريخ والوقت من محدِّد التاريخ.",
      mustBeZeroOrMore: "أدخل صفرًا أو رقمًا موجبًا.",
      invalidEmail: "أدخل بريدًا إلكترونيًا صحيحًا.",
      // Like the slug example above, a web address stays Latin in both locales.
      invalidUrl: "أدخل رابطًا صحيحًا، مثال: zoom.us/j/123.",
      formError: "حدث خطأ ما. من فضلك حاول مرة أخرى."
    }
  }
};

export function getDictionary(locale: Locale): Dictionary {
  return dictionaries[locale];
}

/** Simple {token} interpolation, avoiding a full i18n library for this app-shell stage. */
export function format(template: string, values: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (match, key) =>
    key in values ? String(values[key]) : match
  );
}
