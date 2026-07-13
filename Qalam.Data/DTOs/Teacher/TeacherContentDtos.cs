using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Data.DTOs.Teacher;

public class TeacherContentFolderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int? ParentFolderId { get; set; }
    public int ItemCount { get; set; }
    public int SubfolderCount { get; set; }
}

public class TeacherContentItemDto
{
    public int Id { get; set; }
    public int? FolderId { get; set; }
    public TeacherContentItemKind Kind { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TeacherContentFileType? FileType { get; set; }
    public long? SizeBytes { get; set; }
    public string? PublicUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public int UsedInSessionsCount { get; set; }
    public DateTime UploadedAt { get; set; }
    public TeacherContentUploadStatus UploadStatus { get; set; }
}

public class TeacherSessionContentLinkDto
{
    public int Id { get; set; }
    public int ContentItemId { get; set; }
    public TeacherContentItemKind Kind { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TeacherContentFileType? FileType { get; set; }
    public string? PublicUrl { get; set; }
}

public class TeacherSessionHomeworkFileDto
{
    public int ContentItemId { get; set; }
    public string Title { get; set; } = default!;
    public TeacherContentFileType? FileType { get; set; }
    public string? PublicUrl { get; set; }
}

public class TeacherSessionHomeworkDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public int SubmittedCount { get; set; }
    public int TotalStudents { get; set; }
    public List<TeacherSessionHomeworkFileDto> Files { get; set; } = new();
}

public class CreateSessionHomeworkDto
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public List<int>? ContentItemIds { get; set; }
}

public class UpdateSessionHomeworkDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueAt { get; set; }
    public List<int>? ContentItemIds { get; set; }
}

public class CreateTeacherContentFolderDto
{
    public string Name { get; set; } = default!;
    public int? ParentFolderId { get; set; }
}

public class UpdateTeacherContentFolderDto
{
    public string? Name { get; set; }
    public int? ParentFolderId { get; set; }
}

public class CreateHomeworkTemplateDto
{
    public int? FolderId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
}

public class UpdateTeacherContentItemDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? FolderId { get; set; }
    public List<string>? Tags { get; set; }
}

public class LinkSessionContentDto
{
    public int ContentItemId { get; set; }
}

public class LinkSessionContentBulkDto
{
    public List<int> ContentItemIds { get; set; } = new();
}
