using Microsoft.EntityFrameworkCore;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Repositories;

public class TeacherDomainQuestionSubmissionRepository
    : GenericRepositoryAsync<TeacherDomainQuestionSubmission>, ITeacherDomainQuestionSubmissionRepository
{
    private readonly DbSet<TeacherDomainQuestionSubmission> _set;

    public TeacherDomainQuestionSubmissionRepository(ApplicationDBContext context) : base(context)
    {
        _set = context.Set<TeacherDomainQuestionSubmission>();
    }

    public Task<List<TeacherDomainQuestionSubmission>> GetByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Where(s => s.TeacherId == teacherId)
            .ToListAsync(cancellationToken);

    public Task<List<TeacherDomainQuestionSubmission>> GetByTeacherIdWithQuestionsAsync(int teacherId, CancellationToken cancellationToken = default) =>
        _set.AsNoTracking()
            .Include(s => s.Question)
            .ThenInclude(q => q.Domain)
            .Where(s => s.TeacherId == teacherId)
            .ToListAsync(cancellationToken);

    public Task<TeacherDomainQuestionSubmission?> GetByIdWithQuestionAsync(int submissionId, CancellationToken cancellationToken = default) =>
        _set.Include(s => s.Question)
            .ThenInclude(q => q.Domain)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

    public Task<bool> ExistsForTeacherAndQuestionAsync(int teacherId, int questionId, CancellationToken cancellationToken = default) =>
        _set.AnyAsync(s => s.TeacherId == teacherId && s.QuestionId == questionId, cancellationToken);

    public Task<TeacherDomainQuestionSubmission?> GetByTeacherDocumentIdAsync(int teacherDocumentId, CancellationToken cancellationToken = default) =>
        _set.FirstOrDefaultAsync(s => s.TeacherDocumentId == teacherDocumentId, cancellationToken);
}
