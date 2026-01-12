namespace Qalam.Data.AppMetaData
{
    public static class Roles
    {
        // Role Names
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string Teacher = "Teacher";
        public const string Student = "Student";
        public const string Parent = "Parent";
        public const string Customer = "Customer";
        public const string User = "User";

        // Combined Roles
        public const string AdminOrStaff = "SuperAdmin,Staff";
        public const string TeacherOrAdmin = "Teacher,Admin,SuperAdmin";
        public const string EducationStaff = "Teacher,Admin,SuperAdmin,Staff";

        // Policy Names
        public const string AdminPolicy = "AdminPolicy";
        public const string SuperAdminPolicy = "SuperAdminPolicy";
        public const string TeacherPolicy = "TeacherPolicy";
        public const string TeacherOrAdminPolicy = "TeacherOrAdminPolicy";
    }
}

