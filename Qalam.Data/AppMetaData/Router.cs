namespace Qalam.Data.AppMetaData
{
    public static class Router
    {
        public const string SignleRoute = "/{id}";

        public const string root = "Api";
        public const string version = "V1";
        public const string Rule = root + "/" + version + "/";

        #region Authentication
        public const string Authentication = Rule + "Authentication";
        public const string AuthenticationRegister = Authentication + "/Register";
        public const string AuthenticationLogin = Authentication + "/Login";
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
        public const string UserById = Users + SignleRoute;
        #endregion

        #region Education
        public const string Education = Rule + "Education";
        
        // Domains
        public const string EducationDomains = Education + "/Domains";
        public const string EducationDomainById = EducationDomains + SignleRoute;
        
        // Levels
        public const string EducationLevels = Education + "/Levels";
        public const string EducationLevelById = EducationLevels + SignleRoute;
        
        // Grades
        public const string EducationGrades = Education + "/Grades";
        public const string EducationGradeById = EducationGrades + SignleRoute;
        
        // Terms
        public const string EducationTerms = Education + "/Terms";
        public const string EducationTermById = EducationTerms + SignleRoute;
        #endregion

        #region Curriculum
        public const string Curriculum = Rule + "Curriculum";
        public const string CurriculumById = Curriculum + SignleRoute;
        public const string CurriculumLevels = Curriculum + "/{id}/Levels";
        #endregion

        #region Subjects
        public const string Subjects = Rule + "Subjects";
        public const string SubjectById = Subjects + SignleRoute;
        public const string SubjectsByGrade = Subjects + "/Grade/{gradeId}";
        public const string SubjectsByDomain = Subjects + "/Domain/{domainId}";
        #endregion

        #region Content
        public const string Content = Rule + "Content";
        
        // Content Units
        public const string ContentUnits = Content + "/Units";
        public const string ContentUnitById = ContentUnits + SignleRoute;
        
        // Lessons
        public const string ContentLessons = Content + "/Lessons";
        public const string ContentLessonById = ContentLessons + SignleRoute;
        #endregion

        #region Quran
        public const string Quran = Rule + "Quran";
        
        // Quran Levels
        public const string QuranLevels = Quran + "/Levels";
        public const string QuranLevelById = QuranLevels + SignleRoute;
        
        // Quran Parts
        public const string QuranParts = Quran + "/Parts";
        public const string QuranPartByNumber = QuranParts + "/{partNumber}";
        
        // Quran Surahs
        public const string QuranSurahs = Quran + "/Surahs";
        public const string QuranSurahByNumber = QuranSurahs + "/{surahNumber}";
        
        // Content Types
        public const string QuranContentTypes = Quran + "/ContentTypes";
        #endregion

        #region Teaching
        public const string Teaching = Rule + "Teaching";
        
        // Teaching Modes
        public const string TeachingModes = Teaching + "/Modes";
        public const string TeachingModeById = TeachingModes + SignleRoute;
        
        // Session Types
        public const string SessionTypes = Teaching + "/SessionTypes";
        public const string SessionTypeById = SessionTypes + SignleRoute;
        
        // Time Slots
        public const string TimeSlots = Teaching + "/TimeSlots";
        public const string TimeSlotById = TimeSlots + SignleRoute;
        #endregion
    }
}

