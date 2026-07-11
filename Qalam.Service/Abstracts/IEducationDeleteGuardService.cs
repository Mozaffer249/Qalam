namespace Qalam.Service.Abstracts;

public interface IEducationDeleteGuardService
{
    Task AssertCanDeleteCurriculumAsync(int id);
    Task AssertCanDeleteLevelAsync(int id);
    Task AssertCanDeleteGradeAsync(int id);
    Task AssertCanDeleteSubjectAsync(int id);
    Task AssertCanDeleteTermAsync(int id);
    Task AssertCanDeleteContentUnitAsync(int id);
}
