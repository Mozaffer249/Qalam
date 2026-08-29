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
        public const string AuthenticationConfig = Authentication + "/Config";
        public const string AuthenticationRegister = Authentication + "/Register";
        public const string AdminLogin = Authentication + "/Admin/Login";
        public const string AdminSendResetPasswordCode = Authentication + "/Admin/SendResetPasswordCode";
        public const string AdminResetPassword = Authentication + "/Admin/ResetPassword";
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
        public const string TeacherRegistrationRequirements = Authentication + "/Teacher/RegistrationRequirements";
        public const string TeacherSubmitRegistrationRequirements = Authentication + "/Teacher/SubmitRegistrationRequirements";
        public const string TeacherAccountStatus = Authentication + "/Teacher/AccountStatus";
        public const string TeacherAcceptTerms = Authentication + "/Teacher/AcceptTerms";

        public const string AdminTeacherRegistrationRequirements = Rule + "Admin/TeacherRegistrationRequirements";
        public const string AdminTeacherDomainQuestions = Rule + "Admin/TeacherDomainQuestions";
        public const string AdminNationalities = Rule + "Admin/Nationalities";
        public const string AdminPricing = Rule + "Admin/Pricing";
        public const string AdminRefunds = Rule + "Admin/Refunds";
        public const string AdminRefundById = AdminRefunds + "/{id}";
        public const string AdminEnrollments = Rule + "Admin/Enrollments";
        public const string AdminEnrollmentById = AdminEnrollments + "/{id}";
        public const string AdminSessions = Rule + "Admin/Sessions";
        public const string AdminSessionById = AdminSessions + "/{id}";
        public const string AdminSessionAttendance = AdminSessionById + "/Attendance";
        public const string AdminSessionCancel = AdminSessionById + "/Cancel";
        public const string AdminSessionRefund = AdminSessionById + "/Refund";
        public const string AdminSessionEarningHold = AdminSessionById + "/Earning/Hold";
        public const string AdminSessionEarningRelease = AdminSessionById + "/Earning/Release";
        public const string AdminSessionEarningVoid = AdminSessionById + "/Earning/Void";
        public const string AdminSessionWarnTeacher = AdminSessionById + "/WarnTeacher";
        public const string AdminSessionBlockTeacher = AdminSessionById + "/BlockTeacher";
        public const string AdminSessionComplaintAssign = AdminSessionById + "/Complaints/{complaintId}/Assign";
        public const string AdminSessionComplaintRequestTeacher = AdminSessionById + "/Complaints/{complaintId}/RequestTeacherResponse";
        public const string AdminSessionComplaintResolve = AdminSessionById + "/Complaints/{complaintId}/Resolve";
        public const string AdminSessionComplaintResolvePreview = AdminSessionById + "/Complaints/{complaintId}/ResolvePreview";
        public const string AdminPayouts = Rule + "Admin/Payouts";
        public const string AdminPayoutById = AdminPayouts + "/{id}";
        public const string AdminPayoutApprove = AdminPayoutById + "/Approve";
        public const string AdminPayoutMarkPaid = AdminPayoutById + "/MarkPaid";
        public const string AdminPayoutPendingEarnings = AdminPayouts + "/PendingEarnings";
        public const string AdminEmailSuppressionsSeed = Rule + "Admin/EmailSuppressions/Seed";
        public const string AdminEmailFailedContacts = Rule + "Admin/Email/FailedContacts";
        public const string AdminEmailSuppressions = Rule + "Admin/Email/Suppressions";
        public const string Nationalities = Rule + "Nationalities";
        public const string CommonPricing = Rule + "Common/Pricing";
        public const string CommonPricingMarkets = CommonPricing + "/markets";
        public const string CommonPricingMyMarket = CommonPricing + "/my-market";
        public const string Contact = Rule + "Contact";
        public const string AdminContactMessages = Rule + "Admin/ContactMessages";
        public const string AdminContactMessageById = Rule + "Admin/ContactMessages/{id}";
        public const string AdminContactMessageClose = Rule + "Admin/ContactMessages/{id}/close";
        public const string AdminContactMessageReopen = Rule + "Admin/ContactMessages/{id}/reopen";
        public const string AdminContactMessageInProgress = Rule + "Admin/ContactMessages/{id}/in-progress";

        public const string AdminStudentManagement = Rule + "Admin/StudentManagement";
        public const string AdminStudents = AdminStudentManagement + "/Students";
        public const string AdminStudentById = AdminStudents + "/{studentId}";
        public const string AdminStudentFreeTrialConsumptions = AdminStudentById + "/FreeTrialConsumptions";
        public const string AdminTeacherInterviewUnlocks = Rule + "Admin/Teachers/{teacherId}/InterviewUnlocks";

        #region Legal Documents
        public const string AdminLegalDocuments = Rule + "Admin/LegalDocuments";
        public const string AdminLegalDocumentById = Rule + "Admin/LegalDocuments/{id}";
        public const string AdminLegalDocumentVersions = Rule + "Admin/LegalDocuments/{id}/versions";
        public const string AdminLegalDocumentVersionById = Rule + "Admin/LegalDocuments/versions/{versionId}";
        public const string AdminLegalDocumentVersionPublish = Rule + "Admin/LegalDocuments/versions/{versionId}/publish";
        public const string AdminLegalDocumentVersionUnpublish = Rule + "Admin/LegalDocuments/versions/{versionId}/unpublish";
        public const string AdminLegalDocumentVersionSections = Rule + "Admin/LegalDocuments/versions/{versionId}/sections";
        public const string AdminLegalDocumentVersionSectionsReorder = Rule + "Admin/LegalDocuments/versions/{versionId}/sections/reorder";
        public const string AdminLegalDocumentSectionById = Rule + "Admin/LegalDocuments/sections/{id}";
        public const string LegalDocuments = Rule + "Legal/Documents";
        public const string LegalDocumentByCode = Rule + "Legal/Documents/{code}";
        public const string LegalConsentsPending = Rule + "Legal/Consents/Pending";
        public const string LegalConsents = Rule + "Legal/Consents";
        #endregion

        public const string TeacherDomainQuestionsSubmit = Rule + "Teacher/DomainQuestions/submit";

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
        public const string AccountUpdateProfilePicture = Authentication + "/Profile/Picture";
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

        // University institution hierarchy
        public const string EducationUniversities = Education + "/Universities";
        public const string EducationUniversityById = EducationUniversities + SingleRoute;
        public const string EducationColleges = Education + "/Colleges";
        public const string EducationCollegeById = EducationColleges + SingleRoute;
        public const string EducationDepartments = Education + "/Departments";
        public const string EducationDepartmentById = EducationDepartments + SingleRoute;
        public const string EducationAcademicPrograms = Education + "/AcademicPrograms";
        public const string EducationAcademicProgramById = EducationAcademicPrograms + SingleRoute;

        public const string EducationWritableFilterValues = Education + "/WritableFilterValues";
        public const string EducationWritableFilterValueById = EducationWritableFilterValues + SingleRoute;
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
        /// <summary>Publish draft course: Api/V1/Teacher/TeacherCourse/{id}/publish</summary>
        public const string TeacherCoursePublish = TeacherCourseById + "/publish";
        /// <summary>Pause published course: Api/V1/Teacher/TeacherCourse/{id}/pause</summary>
        public const string TeacherCoursePause = TeacherCourseById + "/pause";
        /// <summary>Reactivate paused course: Api/V1/Teacher/TeacherCourse/{id}/reactivate</summary>
        public const string TeacherCourseReactivate = TeacherCourseById + "/reactivate";
        /// <summary>Replace a session's unit/lesson coverage: Api/V1/Teacher/TeacherCourse/{courseId}/Sessions/{sessionId}/Units</summary>
        public const string TeacherCourseSessionUnits = TeacherCourse + "/{courseId:int}/Sessions/{sessionId:int}/Units";
        /// <summary>Fixed course session library content: Api/V1/Teacher/TeacherCourse/{courseId}/Sessions/{sessionId}/Content</summary>
        public const string TeacherCourseSessionContent = TeacherCourse + "/{courseId:int}/Sessions/{sessionId:int}/Content";
        public const string TeacherCourseSessionContentByLinkId = TeacherCourseSessionContent + "/{linkId:int}";

        /// <summary>Teacher enrollment requests: Api/V1/Teacher/EnrollmentRequests</summary>
        public const string TeacherEnrollmentRequests = Rule + "Teacher/EnrollmentRequests";
        /// <summary>Teacher enrollment request by id: Api/V1/Teacher/EnrollmentRequests/{id}</summary>
        public const string TeacherEnrollmentRequestById = TeacherEnrollmentRequests + "/{id}";
        /// <summary>Approve enrollment request: Api/V1/Teacher/EnrollmentRequests/{id}/Approve</summary>
        public const string TeacherEnrollmentRequestApprove = TeacherEnrollmentRequestById + "/Approve";
        /// <summary>Reject enrollment request: Api/V1/Teacher/EnrollmentRequests/{id}/Reject</summary>
        public const string TeacherEnrollmentRequestReject = TeacherEnrollmentRequestById + "/Reject";

        /// <summary>Enrollments per course (unified — kind tells individual vs group): Api/V1/Teacher/Courses/{courseId}/Enrollments</summary>
        public const string TeacherCourseEnrollments = Rule + "Teacher/Courses/{courseId}/Enrollments";
        /// <summary>All enrollments for the signed-in teacher: Api/V1/Teacher/Enrollments</summary>
        public const string TeacherEnrollments = Rule + "Teacher/Enrollments";
        /// <summary>Enrollment detail (unified — individual or group): Api/V1/Teacher/Enrollments/{id}</summary>
        public const string TeacherEnrollmentById = TeacherEnrollments + "/{id}";
        /// <summary>Remind unpaid participants: Api/V1/Teacher/Enrollments/{id}/Remind</summary>
        public const string TeacherEnrollmentRemind = TeacherEnrollmentById + "/Remind";
        /// <summary>Invoice metadata for enrollment: Api/V1/Teacher/Enrollments/{id}/Invoice</summary>
        public const string TeacherEnrollmentInvoice = TeacherEnrollmentById + "/Invoice";
        /// <summary>Find-or-create enrollment chat (teacher): Api/V1/Teacher/Enrollments/{id}/Conversation</summary>
        public const string TeacherEnrollmentConversation = TeacherEnrollmentById + "/Conversation";

        /// <summary>Teacher profile: Api/V1/Teacher/Profile/me</summary>
        public const string TeacherProfileMe = Rule + "Teacher/Profile/me";

        /// <summary>Teacher teaching preferences: Api/V1/Teacher/TeachingPreferences</summary>
        public const string TeacherTeachingPreferences = Rule + "Teacher/TeachingPreferences";

        /// <summary>Teacher service areas: Api/V1/Teacher/TeacherArea</summary>
        public const string TeacherArea = Rule + "Teacher/TeacherArea";
        public const string TeacherAreaById = TeacherArea + "/{id:int}";

        /// <summary>Teacher scheduled sessions: Api/V1/Teacher/MySessions</summary>
        public const string TeacherMySessions = Rule + "Teacher/MySessions";
        public const string TeacherMySessionById = TeacherMySessions + "/{id:int}";
        public const string TeacherMySessionContent = TeacherMySessionById + "/Content";
        public const string TeacherMySessionContentByLinkId = TeacherMySessionContent + "/{linkId:int}";
        public const string TeacherMySessionHomework = TeacherMySessionById + "/Homework";
        public const string TeacherMySessionHomeworkById = TeacherMySessionHomework + "/{assignmentId:int}";
        public const string TeacherMySessionStart = TeacherMySessionById + "/Start";
        public const string TeacherMySessionComplete = TeacherMySessionById + "/Complete";
        public const string TeacherMySessionCancel = TeacherMySessionById + "/Cancel";
        public const string TeacherMySessionReschedule = TeacherMySessionById + "/Reschedule";
        public const string TeacherMySessionAttendance = TeacherMySessionById + "/Attendance";
        public const string TeacherMySessionNotes = TeacherMySessionById + "/Notes";
        public const string TeacherMySessionJoin = TeacherMySessionById + "/Join";
        public const string TeacherMySessionLeave = TeacherMySessionById + "/Leave";
        public const string TeacherMySessionLiveToken = TeacherMySessionById + "/LiveToken";
        public const string TeacherMySessionReviews = TeacherMySessionById + "/Reviews";

        /// <summary>Teacher content library folders: Api/V1/Teacher/Content/Folders</summary>
        public const string TeacherContentFolders = Rule + "Teacher/Content/Folders";
        public const string TeacherContentFolderById = TeacherContentFolders + "/{id:int}";
        /// <summary>Teacher content library items: Api/V1/Teacher/Content/Items</summary>
        public const string TeacherContentItems = Rule + "Teacher/Content/Items";
        public const string TeacherContentItemById = TeacherContentItems + "/{id:int}";
        public const string TeacherContentItemUpload = TeacherContentItems + "/Upload";
        public const string TeacherContentItemHomework = TeacherContentItems + "/Homework";

        /// <summary>Teacher finance: Api/V1/Teacher/Finance/Summary</summary>
        public const string TeacherFinanceSummary = Rule + "Teacher/Finance/Summary";
        public const string TeacherFinanceTransactions = Rule + "Teacher/Finance/Transactions";
        public const string TeacherFinanceTransactionById = Rule + "Teacher/Finance/Transactions/{id}";

        /// <summary>Teacher course hourly rate preview: Api/V1/Teacher/Pricing/course-hourly-rate</summary>
        public const string TeacherPricingCourseHourlyRate = Rule + "Teacher/Pricing/course-hourly-rate";

        /// <summary>Teacher read-only domain pricing: Api/V1/Teacher/Pricing/my-domain-pricings</summary>
        public const string TeacherPricingMyDomainPricings = Rule + "Teacher/Pricing/my-domain-pricings";

        /// <summary>Teacher in-app notifications</summary>
        public const string TeacherNotifications = Rule + "Teacher/Notifications";
        public const string TeacherNotificationRead = TeacherNotifications + "/{id:int}/read";
        public const string TeacherNotificationsReadAll = TeacherNotifications + "/read-all";
        #endregion

        #region Teacher Open Session Requests (Scenario 2)
        /// <summary>Inbox of matched requests: Api/V1/Teacher/AvailableRequests</summary>
        public const string TeacherAvailableRequests = Rule + "Teacher/AvailableRequests";
        /// <summary>Inbox tab counts: Api/V1/Teacher/AvailableRequests/summary</summary>
        public const string TeacherAvailableRequestsSummary = TeacherAvailableRequests + "/summary";
        /// <summary>Available request detail (side-effect: marks Viewed): Api/V1/Teacher/AvailableRequests/{id}</summary>
        public const string TeacherAvailableRequestById = TeacherAvailableRequests + "/{id:int}";
        /// <summary>Mark as viewed without fetching detail: Api/V1/Teacher/AvailableRequests/{id}/mark-viewed</summary>
        public const string TeacherAvailableRequestMarkViewed = TeacherAvailableRequestById + "/mark-viewed";
        /// <summary>Dismiss from inbox: Api/V1/Teacher/AvailableRequests/{id}/dismiss</summary>
        public const string TeacherAvailableRequestDismiss = TeacherAvailableRequestById + "/dismiss";
        /// <summary>Availability + conflict match per session: Api/V1/Teacher/AvailableRequests/{id}/availability-match</summary>
        public const string TeacherAvailableRequestAvailabilityMatch = TeacherAvailableRequestById + "/availability-match";

        /// <summary>Create / list endpoint for teacher offers: Api/V1/Teacher/Offers</summary>
        public const string TeacherSessionOffers = Rule + "Teacher/Offers";
        /// <summary>Single offer by id: Api/V1/Teacher/Offers/{id}</summary>
        public const string TeacherSessionOfferById = TeacherSessionOffers + "/{id:int}";
        /// <summary>My offers paginated list: Api/V1/Teacher/Offers/my</summary>
        public const string TeacherSessionOffersMy = TeacherSessionOffers + "/my";
        /// <summary>Withdraw an offer: Api/V1/Teacher/Offers/{id}/withdraw</summary>
        public const string TeacherSessionOfferWithdraw = TeacherSessionOfferById + "/withdraw";

        /// <summary>Conversations namespace (shared between teacher + student): Api/V1/Conversations</summary>
        public const string OfferConversations = Rule + "Conversations";
        /// <summary>
        /// Find-or-create the chat for a (request, teacher) pair — targeted OSR only.
        /// Broadcast requests must use OfferConversationByOffer.
        /// </summary>
        public const string OfferConversationByRequest = OfferConversations + "/by-request/{requestId:int}/teacher/{teacherId:int}";
        /// <summary>Find-or-create by offer (broadcast; also resolves targeted request thread): Api/V1/Conversations/by-offer/{offerId}</summary>
        public const string OfferConversationByOffer = OfferConversations + "/by-offer/{offerId:int}";
        /// <summary>Cursor-paginated messages: Api/V1/Conversations/{conversationId}/messages</summary>
        public const string OfferConversationMessages = OfferConversations + "/{conversationId:int}/messages";
        /// <summary>Mark messages as read: Api/V1/Conversations/{conversationId}/read</summary>
        public const string OfferConversationMarkRead = OfferConversations + "/{conversationId:int}/read";

        /// <summary>Enrollment conversations (shared namespace): Api/V1/EnrollmentConversations</summary>
        public const string EnrollmentConversations = Rule + "EnrollmentConversations";
        /// <summary>Find-or-create by enrollment: Api/V1/EnrollmentConversations/by-enrollment/{enrollmentId}</summary>
        public const string EnrollmentConversationByEnrollment = EnrollmentConversations + "/by-enrollment/{enrollmentId:int}";
        /// <summary>Cursor-paginated messages: Api/V1/EnrollmentConversations/{conversationId}/messages</summary>
        public const string EnrollmentConversationMessages = EnrollmentConversations + "/{conversationId:int}/messages";
        /// <summary>Mark messages as read: Api/V1/EnrollmentConversations/{conversationId}/read</summary>
        public const string EnrollmentConversationMarkRead = EnrollmentConversations + "/{conversationId:int}/read";

        /// <summary>LiveKit webhook receiver: Api/V1/Live/Webhooks/LiveKit</summary>
        public const string LiveKitWebhook = Rule + "Live/Webhooks/LiveKit";
        #endregion

        #region Student
        /// <summary>Student course catalog: Api/V1/Student/Courses</summary>
        public const string StudentCourses = Rule + "Student/Courses";
        /// <summary>Student course by id: Api/V1/Student/Courses/{id}</summary>
        public const string StudentCourseById = StudentCourses + "/{id}";
        /// <summary>Student recommended courses (4 items): Api/V1/Student/Courses/Recommended</summary>
        public const string StudentRecommendedCourses = StudentCourses + "/Recommended";
        /// <summary>Guardian's children list: Api/V1/Student/MyChildren</summary>
        public const string StudentMyChildren = Rule + "Student/MyChildren";
        /// <summary>Update child profile: Api/V1/Student/Children/{studentId}</summary>
        public const string StudentChildById = Rule + "Student/Children/{studentId}";
        /// <summary>Update child profile picture: Api/V1/Student/Children/{studentId}/ProfilePicture</summary>
        public const string StudentChildProfilePicture = StudentChildById + "/ProfilePicture";
        /// <summary>Child file detail (attendance + upcoming + documents): Api/V1/Student/Children/{studentId}/File</summary>
        public const string StudentChildFile = StudentChildById + "/File";
        /// <summary>Student enrollment requests: Api/V1/Student/EnrollmentRequests</summary>
        public const string StudentEnrollmentRequests = Rule + "Student/EnrollmentRequests";
        /// <summary>Student enrollment request by id: Api/V1/Student/EnrollmentRequests/{id}</summary>
        public const string StudentEnrollmentRequestById = StudentEnrollmentRequests + "/{id}";
        /// <summary>Respond to group enrollment invite: Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/Response</summary>
        public const string StudentEnrollmentRequestMemberResponse = StudentEnrollmentRequests + "/{enrollmentRequestId}/Members/Response";
        /// <summary>Owner cancel pending invite: Api/V1/Student/EnrollmentRequests/{enrollmentRequestId}/Members/{studentId}/Cancel</summary>
        public const string StudentEnrollmentRequestCancelInvite = StudentEnrollmentRequests + "/{enrollmentRequestId}/Members/{studentId}/Cancel";
        /// <summary>Owner cancel whole request before pay: Api/V1/Student/EnrollmentRequests/{id}/Cancel</summary>
        public const string StudentEnrollmentRequestCancel = StudentEnrollmentRequests + "/{id}/Cancel";

        // ---- Scenario 2: Open Session Requests (student posts, multiple teachers offer) ----
        /// <summary>Student open-session requests root: Api/V1/Student/OpenSessionRequests</summary>
        public const string StudentOpenSessionRequests = Rule + "Student/OpenSessionRequests";
        /// <summary>Open-session request by id: Api/V1/Student/OpenSessionRequests/{id}</summary>
        public const string StudentOpenSessionRequestById = StudentOpenSessionRequests + "/{id}";
        /// <summary>My open-session requests list: Api/V1/Student/OpenSessionRequests/my</summary>
        public const string StudentOpenSessionRequestsMy = StudentOpenSessionRequests + "/my";
        /// <summary>Cancel: Api/V1/Student/OpenSessionRequests/{id}/Cancel</summary>
        public const string StudentOpenSessionRequestCancel = StudentOpenSessionRequests + "/{id}/Cancel";
        /// <summary>Invitee response: Api/V1/Student/OpenSessionRequests/{openSessionRequestId}/Members/Response</summary>
        public const string StudentOpenSessionRequestMemberResponse = StudentOpenSessionRequests + "/{openSessionRequestId}/Members/Response";
        /// <summary>Attachments: Api/V1/Student/OpenSessionRequests/{id}/Attachments</summary>
        public const string StudentOpenSessionRequestAttachments = StudentOpenSessionRequests + "/{id}/Attachments";
        /// <summary>Attachment by id: Api/V1/Student/OpenSessionRequests/{id}/Attachments/{attachmentId}</summary>
        public const string StudentOpenSessionRequestAttachmentById = StudentOpenSessionRequestAttachments + "/{attachmentId}";
        /// <summary>Update draft: Api/V1/Student/OpenSessionRequests/{id}</summary>
        public const string StudentOpenSessionRequestUpdateDraft = StudentOpenSessionRequestById;
        /// <summary>Publish draft: Api/V1/Student/OpenSessionRequests/{id}/Publish</summary>
        public const string StudentOpenSessionRequestPublish = StudentOpenSessionRequestById + "/Publish";
        /// <summary>List offers on a request: Api/V1/Student/OpenSessionRequests/{id}/Offers</summary>
        public const string StudentOpenSessionRequestOffers = StudentOpenSessionRequestById + "/Offers";
        /// <summary>Offer detail: Api/V1/Student/OpenSessionRequests/{id}/Offers/{offerId}</summary>
        public const string StudentOpenSessionRequestOfferById = StudentOpenSessionRequestOffers + "/{offerId}";
        /// <summary>Accept offer: Api/V1/Student/OpenSessionRequests/Offers/{offerId}/Accept</summary>
        public const string StudentOpenSessionOfferAccept = StudentOpenSessionRequests + "/Offers/{offerId}/Accept";
        /// <summary>Reject offer: Api/V1/Student/OpenSessionRequests/Offers/{offerId}/Reject</summary>
        public const string StudentOpenSessionOfferReject = StudentOpenSessionRequests + "/Offers/{offerId}/Reject";
        /// <summary>Availability pre-check: Api/V1/Student/OpenSessionRequests/Offers/{offerId}/availability-check</summary>
        public const string StudentOpenSessionOfferAvailabilityCheck =
            StudentOpenSessionRequests + "/Offers/{offerId}/availability-check";

        /// <summary>Student enrollments (my enrollments): Api/V1/Student/Enrollments</summary>
        public const string StudentEnrollments = Rule + "Student/Enrollments";
        /// <summary>Student enrollment by id: Api/V1/Student/Enrollments/{id}</summary>
        public const string StudentEnrollmentById = StudentEnrollments + "/{id}";
        /// <summary>Owner cancel PendingPayment enrollment: Api/V1/Student/Enrollments/{id}/Cancel</summary>
        public const string StudentEnrollmentCancel = StudentEnrollments + "/{id}/Cancel";
        /// <summary>Search students for group enrollment: Api/V1/Student/Students/Search</summary>
        public const string StudentSearchForGroup = Rule + "Student/Students/Search";
        /// <summary>Search students by name or email: Api/V1/Student/Search</summary>
        public const string StudentSearch = Rule + "Student/Search";
        /// <summary>Student pending invitations: Api/V1/Student/Invitations</summary>
        public const string StudentInvitations = Rule + "Student/Invitations";
        /// <summary>Invitation detail (S1 or OSR): Api/V1/Student/Invitations/{invitationKey}</summary>
        public const string StudentInvitationById = StudentInvitations + "/{invitationKey}";

        /// <summary>Pay one participant of an enrollment (individual = the only participant; group = one member). Api/V1/Student/Payments/Participants</summary>
        public const string StudentPayEnrollmentParticipant = Rule + "Student/Payments/Participants";
        /// <summary>Unified enrollment payment summary: Api/V1/Student/Payments/Enrollments/{enrollmentId}/Summary</summary>
        public const string StudentEnrollmentPaymentSummary = Rule + "Student/Payments/Enrollments/{enrollmentId}/Summary";

        /// <summary>Student course schedules (join / review): Api/V1/Student/Sessions</summary>
        public const string StudentSessions = Rule + "Student/Sessions";
        public const string StudentSessionById = StudentSessions + "/{id:int}";
        public const string StudentSessionComplaints = StudentSessionById + "/Complaints";
        public const string StudentSessionComplaintById = StudentSessions + "/Complaints/{complaintId:int}";
        public const string TeacherMySessionComplaintRespond = TeacherMySessionById + "/Complaints/{complaintId:int}/Respond";
        public const string StudentSessionJoin = StudentSessionById + "/Join";
        public const string StudentSessionLiveToken = StudentSessionById + "/LiveToken";
        public const string StudentSessionReview = StudentSessionById + "/Review";

        /// <summary>Teacher availability for a date range (calendar view): Api/V1/Student/Teachers/{teacherId}/Availability</summary>
        public const string StudentTeacherAvailability = Rule + "Student/Teachers/{teacherId}/Availability";

        /// <summary>Paginated browse of teachers with filters: Api/V1/Student/Teachers</summary>
        public const string StudentTeachers = Rule + "Student/Teachers";
        /// <summary>Top-N recommended teachers based on the student's profile: Api/V1/Student/Teachers/Recommended</summary>
        public const string StudentRecommendedTeachers = StudentTeachers + "/Recommended";

        /// <summary>Student teacher profile: Api/V1/Student/Teachers/{teacherId}</summary>
        public const string StudentTeacherById = StudentTeachers + "/{teacherId:int}";
        /// <summary>Approved subjects + units: Api/V1/Student/Teachers/{teacherId}/Subjects</summary>
        public const string StudentTeacherSubjects = StudentTeacherById + "/Subjects";
        /// <summary>Repertoire units for a teacher subject: Api/V1/Student/Teachers/{teacherId}/Subjects/{teacherSubjectId}/Units</summary>
        public const string StudentTeacherSubjectUnits = StudentTeacherSubjects + "/{teacherSubjectId:int}/Units";
        /// <summary>Approved reviews: Api/V1/Student/Teachers/{teacherId}/Reviews</summary>
        public const string StudentTeacherReviews = StudentTeacherById + "/Reviews";
        /// <summary>Approved certificates: Api/V1/Student/Teachers/{teacherId}/Certificates</summary>
        public const string StudentTeacherCertificates = StudentTeacherById + "/Certificates";
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
