namespace Qalam.Data.AppMetaData
{
    public static class Router
    {
        public const string SingleRoute = "/{id}";

        public const string root = "Api";
        public const string version = "V1";
        public const string Rule = root + "/" + version + "/";

        #region Authentication
        public const string Authentication = Rule + "Authentication";
        public const string AuthenticationRegister = Authentication + "/Register";
        public const string AdminLogin = Authentication + "/Admin/Login";
        public const string AuthenticationLoginWithTwoFactor = Authentication + "/LoginWithTwoFactor";
        public const string AuthenticationLogout = Authentication + "/Logout";
        public const string AuthenticationRefreshToken = Authentication + "/RefreshToken";
        public const string AuthenticationChangePassword = Authentication + "/ChangePassword";
        public const string AuthenticationSendResetPasswordCode = Authentication + "/SendResetPasswordCode";
        public const string AuthenticationResetPassword = Authentication + "/ResetPassword";
        public const string AuthenticationConfirmEmail = Authentication + "/ConfirmEmail";
        public const string AuthenticationValidateToken = Authentication + "/ValidateToken";
        public const string AuthenticationEnableTwoFactor = Authentication + "/EnableTwoFactor";
        public const string AuthenticationVerifyTwoFactor = Authentication + "/VerifyTwoFactor";
        public const string AuthenticationDisableTwoFactor = Authentication + "/DisableTwoFactor";
        public const string AuthenticationGenerateRecoveryCodes = Authentication + "/GenerateRecoveryCodes";
        public const string AuthenticationGetTwoFactorStatus = Authentication + "/GetTwoFactorStatus";

        // Teacher Authentication & Registration
        public const string TeacherLoginOrRegister = Authentication + "/Teacher/LoginOrRegister";
        public const string TeacherVerifyOtp = Authentication + "/Teacher/VerifyOtp";
        public const string TeacherCompletePersonalInfo = Authentication + "/Teacher/CompletePersonalInfo";
        public const string TeacherUploadDocuments = Authentication + "/Teacher/UploadDocuments";

        // Student / Parent Authentication & Registration
        public const string StudentSendOtp = Authentication + "/Student/SendOttp";
        public const string StudentVerifyOtp = Authentication + "/Student/VerifyOp";
        public const string StudentSetAccountTypeAndUsage = Authentication + "/Student/SetAccountTypeAndUsage";
        public const string StudentCompleteProfile = Authentication + "/Student/CompleteProfile";
        public const string StudentAddChild = Authentication + "/Student/AddChild";

        // Enum endpoints
        public const string GetIdentityTypes = Authentication + "/IdentityTypes";
        public const string GetDocumentTypes = Authentication + "/DocumentTypes";

        // Account Management
        public const string AccountGetProfile = Authentication + "/Profile";
        public const string AccountUpdateProfile = Authentication + "/Profile/Update";
        public const string AccountChangeEmail = Authentication + "/ChangeEmail";
        public const string AccountConfirmEmailChange = Authentication + "/ConfirmEmailChange";
        public const string AccountGetSessions = Authentication + "/Sessions";
        public const string AccountTerminateSession = Authentication + "/Sessions/Terminate";
        public const string AccountTerminateAllSessions = Authentication + "/Sessions/TerminateAll";
        public const string AccountGetTrustedDevices = Authentication + "/TrustedDevices";
        public const string AccountTrustDevice = Authentication + "/TrustDevice";
        public const string AccountRemoveTrustedDevice = Authentication + "/TrustedDevices/Remove";
        public const string AccountGetSecurityEvents = Authentication + "/SecurityEvents";
        public const string AccountExportData = Authentication + "/ExportData";
        public const string AccountDelete = Authentication + "/Delete";
        #endregion

        #region Users
        public const string Users = Rule + "Users";
        public const string UserById = Users + SingleRoute;
        #endregion

        #region Education
        public const string Education = Rule + "Education";

        // Domains
        public const string EducationDomains = Education + "/Domains";
        public const string EducationDomainById = EducationDomains + SingleRoute;

        // Levels
        public const string EducationLevels = Education + "/Levels";
        public const string EducationLevelById = EducationLevels + SingleRoute;

        // Grades
        public const string EducationGrades = Education + "/Grades";
        public const string EducationGradeById = EducationGrades + SingleRoute;

        // Terms
        public const string EducationTerms = Education + "/Terms";
        public const string EducationTermById = EducationTerms + SingleRoute;
        #endregion

        #region Curriculum
        public const string Curriculum = Rule + "Curriculum";
        public const string CurriculumById = Curriculum + SingleRoute;
        public const string CurriculumLevels = Curriculum + "/{id}/Levels";
        #endregion

        #region Subjects
        public const string Subjects = Rule + "Subjects";
        public const string SubjectById = Subjects + SingleRoute;
        public const string SubjectsByGrade = Subjects + "/Grade/{gradeId}";
        public const string SubjectsByDomain = Subjects + "/Domain/{domainId}";
        #endregion

        #region Content
        public const string Content = Rule + "Content";

        // Content Units
        public const string ContentUnits = Content + "/Units";
        public const string ContentUnitById = ContentUnits + SingleRoute;

        // Lessons
        public const string ContentLessons = Content + "/Lessons";
        public const string ContentLessonById = ContentLessons + SingleRoute;
        #endregion

        #region Quran
        public const string Quran = Rule + "Quran";

        // Quran Levels
        public const string QuranLevels = Quran + "/Levels";
        public const string QuranLevelById = QuranLevels + SingleRoute;

        // Quran Parts
        public const string QuranParts = Quran + "/Parts";
        public const string QuranPartByNumber = QuranParts + "/{partNumber}";

        // Quran Surahs
        public const string QuranSurahs = Quran + "/Surahs";
        public const string QuranSurahByNumber = QuranSurahs + "/{surahNumber}";

        // Content Types
        public const string QuranContentTypes = Quran + "/ContentTypes";
        #endregion

        #region Course (Deprecated - Use Teacher routes)
        /// <summary>
        /// DEPRECATED: Course management has been moved to Teacher-specific routes.
        /// New route: /Api/V1/Teacher/TeacherCourse
        /// This constant is kept for backward compatibility only.
        /// </summary>
        [Obsolete("Use Teacher/TeacherCourse routes instead. This will be removed in a future version.")]
        public const string Courses = Rule + "Courses";
        [Obsolete("Use Teacher/TeacherCourse routes instead. This will be removed in a future version.")]
        public const string CourseById = Courses + SingleRoute;
        #endregion

        #region Teacher
        /// <summary>Base route for teacher course management: Api/V1/Teacher/TeacherCourse</summary>
        public const string TeacherCourse = Rule + "Teacher/TeacherCourse";
        /// <summary>Teacher course by id: Api/V1/Teacher/TeacherCourse/{id}</summary>
        public const string TeacherCourseById = TeacherCourse + "/{id}";

        /// <summary>Teacher enrollment requests: Api/V1/Teacher/EnrollmentRequests</summary>
        public const string TeacherEnrollmentRequests = Rule + "Teacher/EnrollmentRequests";
        /// <summary>Teacher enrollment request by id: Api/V1/Teacher/EnrollmentRequests/{id}</summary>
        public const string TeacherEnrollmentRequestById = TeacherEnrollmentRequests + "/{id}";
        /// <summary>Approve enrollment request: Api/V1/Teacher/EnrollmentRequests/{id}/Approve</summary>
        public const string TeacherEnrollmentRequestApprove = TeacherEnrollmentRequestById + "/Approve";
        /// <summary>Reject enrollment request: Api/V1/Teacher/EnrollmentRequests/{id}/Reject</summary>
        public const string TeacherEnrollmentRequestReject = TeacherEnrollmentRequestById + "/Reject";
        #endregion

        #region Student
        /// <summary>Student course catalog: Api/V1/Student/Courses</summary>
        public const string StudentCourses = Rule + "Student/Courses";
        /// <summary>Student course by id: Api/V1/Student/Courses/{id}</summary>
        public const string StudentCourseById = StudentCourses + "/{id}";
        /// <summary>Guardian's children list: Api/V1/Student/MyChildren</summary>
        public const string StudentMyChildren = Rule + "Student/MyChildren";
        /// <summary>Student enrollment requests: Api/V1/Student/EnrollmentRequests</summary>
        public const string StudentEnrollmentRequests = Rule + "Student/EnrollmentRequests";
        /// <summary>Student enrollment request by id: Api/V1/Student/EnrollmentRequests/{id}</summary>
        public const string StudentEnrollmentRequestById = StudentEnrollmentRequests + "/{id}";
        /// <summary>Respond to group enrollment invite: Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response</summary>
        public const string StudentEnrollmentRequestMemberResponse = StudentEnrollmentRequests + "/{enrollmentRequestId}/Members/Response";
        /// <summary>Student enrollments (my enrollments): Api/V1/Student/Enrollments</summary>
        public const string StudentEnrollments = Rule + "Student/Enrollments";
        /// <summary>Student enrollment by id: Api/V1/Student/Enrollments/{id}</summary>
        public const string StudentEnrollmentById = StudentEnrollments + "/{id}";
        /// <summary>Search students for group enrollment: Api/V1/Student/Students/Search</summary>
        public const string StudentSearchForGroup = Rule + "Student/Students/Search";
        /// <summary>Search students by name or email: Api/V1/Student/Search</summary>
        public const string StudentSearch = Rule + "Student/Search";
        /// <summary>Student pending invitations: Api/V1/Student/Invitations</summary>
        public const string StudentInvitations = Rule + "Student/Invitations";

        /// <summary>Pay an individual enrollment: Api/V1/Student/Payments/Enrollment</summary>
        public const string StudentPayEnrollment = Rule + "Student/Payments/Enrollment";
        /// <summary>Pay a group enrollment member: Api/V1/Student/Payments/GroupMember</summary>
        public const string StudentPayGroupMember = Rule + "Student/Payments/GroupMember";
        /// <summary>Individual payment summary: Api/V1/Student/Payments/Enrollment/{enrollmentId}/Summary</summary>
        public const string StudentEnrollmentPaymentSummary = Rule + "Student/Payments/Enrollment/{enrollmentId}/Summary";
        /// <summary>Group payment summary: Api/V1/Student/Payments/GroupEnrollment/{groupEnrollmentId}/Summary</summary>
        public const string StudentGroupEnrollmentPaymentSummary = Rule + "Student/Payments/GroupEnrollment/{groupEnrollmentId}/Summary";
        #endregion

        #region Teaching
        public const string Teaching = Rule + "Teaching";

        // Teaching Modes
        public const string TeachingModes = Teaching + "/Modes";
        public const string TeachingModeById = TeachingModes + SingleRoute;

        // Session Types
        public const string SessionTypes = Teaching + "/SessionTypes";
        public const string SessionTypeById = SessionTypes + SingleRoute;

        // Time Slots
        public const string TimeSlots = Teaching + "/TimeSlots";
        public const string TimeSlotById = TimeSlots + SingleRoute;

        // Days of Week
        public const string DaysOfWeek = Teaching + "/DaysOfWeek";
        public const string DayOfWeekById = DaysOfWeek + SingleRoute;
        #endregion

        #region Messaging
        public const string Messaging = Rule + "Messaging";
        public const string MessagingEmail = Messaging + "/Email";
        public const string MessagingEmailBulk = Messaging + "/Email/Bulk";
        public const string MessagingSms = Messaging + "/Sms";
        public const string MessagingSmsBulk = Messaging + "/Sms/Bulk";
        public const string MessagingPush = Messaging + "/Push";
        public const string MessagingPushBulk = Messaging + "/Push/Bulk";
        public const string MessagingStatus = Messaging + "/Status/{messageId}";
        public const string MessagingHistory = Messaging + "/History";
        public const string MessagingHealth = Messaging + "/Health";
        #endregion
    }
}
