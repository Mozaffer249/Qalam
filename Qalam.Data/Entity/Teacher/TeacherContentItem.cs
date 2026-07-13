using Qalam.Data.Commons;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.Entity.Teacher;

public class TeacherContentItem : AuditableEntity
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public int? FolderId { get; set; }
    public TeacherContentItemKind Kind { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TeacherContentFileType? FileType { get; set; }
    public string? StorageKey { get; set; }
    public string? PublicUrl { get; set; }
    public long? SizeBytes { get; set; }
    public string TagsJson { get; set; } = "[]";
    public TeacherContentUploadStatus UploadStatus { get; set; } = TeacherContentUploadStatus.Ready;

    public Teacher Teacher { get; set; } = null!;
    public TeacherContentFolder? Folder { get; set; }
    public ICollection<SessionContentLink> SessionLinks { get; set; } = new List<SessionContentLink>();
}
