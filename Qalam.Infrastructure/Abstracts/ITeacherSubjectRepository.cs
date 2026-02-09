using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherSubjectRepository : IGenericRepositoryAsync<TeacherSubject>
{
    /// <summary>
    /// Get all teacher subjects with their units
    /// </summary>
    Task<List<TeacherSubject>> GetTeacherSubjectsWithUnitsAsync(int teacherId);
    
    /// <summary>
    /// Get a specific teacher subject with units
    /// </summary>
    Task<TeacherSubject?> GetTeacherSubjectWithUnitsAsync(int teacherSubjectId);
    
    /// <summary>
    /// Save teacher subjects (replaces existing)
    /// </summary>
    Task<List<TeacherSubject>> SaveTeacherSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjects);
    
    /// <summary>
    /// Check if teacher has a specific subject
    /// </summary>
    Task<bool> TeacherHasSubjectAsync(int teacherId, int subjectId);
    
    /// <summary>
    /// Check if teacher has any subjects (optimized - doesn't retrieve data)
    /// </summary>
    Task<bool> HasAnySubjectsAsync(int teacherId);
    
    /// <summary>
    /// Remove all subjects for a teacher
    /// </summary>
    Task RemoveAllTeacherSubjectsAsync(int teacherId);
    
    /// <summary>
    /// Get only SubjectIds for a teacher (optimized - no full data)
    /// </summary>
    Task<HashSet<int>> GetExistingSubjectIdsAsync(int teacherId);
    
    /// <summary>
    /// Add only new subjects (skip existing ones)
    /// </summary>
    Task<List<TeacherSubject>> AddNewSubjectsAsync(int teacherId, List<TeacherSubjectItemDto> subjects);
}
