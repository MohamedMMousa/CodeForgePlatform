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
      failed: "Invalid email or password."
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
      noGrades: "No grades yet."
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
      failed: "البريد الإلكتروني أو كلمة المرور غير صحيحة."
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
      noGrades: "لا توجد درجات بعد."
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
