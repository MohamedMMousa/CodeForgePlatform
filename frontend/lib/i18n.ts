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

export interface Dictionary {
  appName: string;
  tagline: string;
  home: {
    welcome: string;
    description: string;
    browseCourses: string;
    signIn: string;
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
  };
  courseDetail: {
    back: string;
    about: string;
    batches: string;
    noBatches: string;
    enrollInBatch: string;
    seatsLeft: string;
    seatsLeft_one: string;
    full: string;
    awaitingNextBatch: string;
    notifyMe: string;
    notifyMeSuccess: string;
    enrollmentClosesOn: string;
    startsOn: string;
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
      signIn: "Sign in"
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
      contact: "Contact us"
    },
    catalog: {
      title: "Course Catalog",
      subtitle: "Live, instructor-led programming tracks and courses.",
      searchPlaceholder: "Search courses…",
      tracksHeading: "Tracks",
      coursesHeading: "Courses",
      trackBadge: "Track",
      coursesInTrack: "{count} courses",
      viewDetails: "View details",
      empty: "No courses match your search yet.",
      loadError: "Could not load the catalog. Please try again."
    },
    courseDetail: {
      back: "Back to catalog",
      about: "About this course",
      batches: "Upcoming batches",
      noBatches: "No batches are open for enrollment right now.",
      enrollInBatch: "Enroll in this batch",
      seatsLeft: "{count} seats left",
      seatsLeft_one: "1 seat left",
      full: "Batch full",
      awaitingNextBatch: "This batch is full. Leave your details to be notified about the next one.",
      notifyMe: "Notify me for the next batch",
      notifyMeSuccess: "Thanks — we'll reach out when the next batch opens.",
      enrollmentClosesOn: "Enrollment closes {date}",
      startsOn: "Starts {date}"
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
    }
  },
  ar: {
    appName: "أكاديمية كود فورج",
    tagline: "ابنِ. أنشئ. أطلق.",
    home: {
      welcome: "مرحبًا بك في أكاديمية كود فورج",
      description: "مسارات برمجة مباشرة بنظام الدفعات لطلاب المدارس والجامعات.",
      browseCourses: "تصفّح الدورات",
      signIn: "تسجيل الدخول"
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
      contact: "تواصل معنا"
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
      empty: "لا توجد دورات مطابقة لبحثك حتى الآن.",
      loadError: "تعذّر تحميل الدليل. يرجى المحاولة مرة أخرى."
    },
    courseDetail: {
      back: "العودة إلى الدليل",
      about: "عن هذه الدورة",
      batches: "الدفعات القادمة",
      noBatches: "لا توجد دفعات متاحة للتسجيل حاليًا.",
      enrollInBatch: "التسجيل في هذه الدفعة",
      seatsLeft: "{count} مقعد متبقٍ",
      seatsLeft_one: "مقعد واحد متبقٍ",
      full: "الدفعة مكتملة",
      awaitingNextBatch: "هذه الدفعة مكتملة. اترك بياناتك ليتم إعلامك بالدفعة القادمة.",
      notifyMe: "أعلمني بالدفعة القادمة",
      notifyMeSuccess: "شكرًا لك — سنتواصل معك عند فتح الدفعة القادمة.",
      enrollmentClosesOn: "يُغلق التسجيل في {date}",
      startsOn: "يبدأ في {date}"
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
