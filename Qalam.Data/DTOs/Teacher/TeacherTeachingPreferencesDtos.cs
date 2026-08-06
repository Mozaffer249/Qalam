namespace Qalam.Data.DTOs.Teacher;

public class TeacherTeachingPreferencesDto
{
    public bool OffersOnline { get; set; }
    public bool OffersInPerson { get; set; }
    public bool OffersIndividual { get; set; }
    public bool OffersGroup { get; set; }
    public string? JobTitle { get; set; }
    public int YearsOfExperience { get; set; }
}

public class UpdateTeacherTeachingPreferencesDto
{
    public bool OffersOnline { get; set; }
    public bool OffersInPerson { get; set; }
    public bool OffersIndividual { get; set; }
    public bool OffersGroup { get; set; }
    public string? JobTitle { get; set; }
    public int YearsOfExperience { get; set; }
}
