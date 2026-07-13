namespace Qalam.Data.Entity.Common.Enums;

public enum TeacherContentItemKind
{
    File = 1,
    Homework = 2
}

public enum TeacherContentFileType
{
    Pdf = 1,
    Image = 2,
    Video = 3,
    Doc = 4,
    Other = 5
}

public enum TeacherContentUploadStatus
{
    Pending = 1,
    Ready = 2,
    Failed = 3
}
