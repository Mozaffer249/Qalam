using Qalam.Data.Commons;

namespace Qalam.Data.Entity.Teacher;

public class TeacherContentFolder : AuditableEntity
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public int? ParentFolderId { get; set; }
    public string Name { get; set; } = default!;

    public Teacher Teacher { get; set; } = null!;
    public TeacherContentFolder? ParentFolder { get; set; }
    public ICollection<TeacherContentFolder> ChildFolders { get; set; } = new List<TeacherContentFolder>();
    public ICollection<TeacherContentItem> Items { get; set; } = new List<TeacherContentItem>();
}
