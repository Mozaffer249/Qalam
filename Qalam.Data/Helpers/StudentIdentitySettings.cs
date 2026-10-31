namespace Qalam.Data.Helpers;

public class StudentIdentitySettings
{
    public List<StudentIdentityRule> Rules { get; set; } = new();
    public bool IdentityRequiredForEnrollment { get; set; }
}

public class StudentIdentityRule
{
    public string NationalityCode { get; set; } = "*";
    public List<int> AllowedIdentityTypes { get; set; } = new();
    public bool IssuingCountryRequired { get; set; }
}
