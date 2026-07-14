using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public interface ITeacherContentService
{
    Task<List<TeacherContentFolderDto>> ListFoldersAsync(int teacherId, int? parentFolderId, CancellationToken ct);
    Task<TeacherContentFolderDto?> CreateFolderAsync(int teacherId, CreateTeacherContentFolderDto dto, CancellationToken ct);
    Task<TeacherContentFolderDto?> UpdateFolderAsync(int teacherId, int folderId, UpdateTeacherContentFolderDto dto, CancellationToken ct);
    Task<bool> DeleteFolderAsync(int teacherId, int folderId, CancellationToken ct);
    Task<List<TeacherContentItemDto>> ListItemsAsync(int teacherId, int? folderId, TeacherContentItemKind? kind, string? search, int pageNumber, int pageSize, CancellationToken ct);
    Task<TeacherContentItemDto?> GetItemAsync(int teacherId, int itemId, CancellationToken ct);
    Task<TeacherContentItemDto?> UploadFileAsync(int teacherId, IFormFile file, int? folderId, string? title, string? description, List<string>? tags, CancellationToken ct);
    Task<TeacherContentItemDto?> CreateHomeworkAsync(int teacherId, CreateHomeworkTemplateDto dto, CancellationToken ct);
    Task<TeacherContentItemDto?> UpdateItemAsync(int teacherId, int itemId, UpdateTeacherContentItemDto dto, CancellationToken ct);
    Task<bool> DeleteItemAsync(int teacherId, int itemId, CancellationToken ct);
    Task<List<TeacherSessionContentLinkDto>> ListSessionContentAsync(int teacherId, int scheduleId, CancellationToken ct);
    Task<TeacherSessionContentLinkDto?> LinkSessionContentAsync(int teacherId, int scheduleId, int contentItemId, CancellationToken ct);
    Task<List<TeacherSessionContentLinkDto>> LinkSessionContentBulkAsync(int teacherId, int scheduleId, LinkSessionContentBulkDto dto, CancellationToken ct);
    Task<bool> UnlinkSessionContentAsync(int teacherId, int scheduleId, int linkId, CancellationToken ct);
    Task<List<TeacherSessionContentLinkDto>> GetContentLinksForSessionAsync(int scheduleId, CancellationToken ct);
    Task<List<TeacherSessionHomeworkDto>> ListSessionHomeworkAsync(int teacherId, int scheduleId, CancellationToken ct);
    Task<TeacherSessionHomeworkDto?> CreateSessionHomeworkAsync(int teacherId, int scheduleId, CreateSessionHomeworkDto dto, CancellationToken ct);
    Task<TeacherSessionHomeworkDto?> UpdateSessionHomeworkAsync(int teacherId, int scheduleId, int assignmentId, UpdateSessionHomeworkDto dto, CancellationToken ct);
    Task<bool> DeleteSessionHomeworkAsync(int teacherId, int scheduleId, int assignmentId, CancellationToken ct);
    Task<List<TeacherSessionHomeworkDto>> GetHomeworkForSessionAsync(int scheduleId, CancellationToken ct);
}

public class TeacherContentService : ITeacherContentService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg", ".mp4" };

    private readonly ApplicationDBContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;

    public TeacherContentService(ApplicationDBContext db, IFileStorageService fileStorage, IConfiguration configuration)
    {
        _db = db;
        _fileStorage = fileStorage;
        _configuration = configuration;
    }

    public async Task<List<TeacherContentFolderDto>> ListFoldersAsync(int teacherId, int? parentFolderId, CancellationToken ct)
    {
        var folders = await _db.TeacherContentFolders
            .AsNoTracking()
            .Where(f => f.TeacherId == teacherId && f.ParentFolderId == parentFolderId)
            .OrderBy(f => f.Name)
            .Select(f => new TeacherContentFolderDto
            {
                Id = f.Id,
                Name = f.Name,
                ParentFolderId = f.ParentFolderId,
                ItemCount = f.Items.Count,
                SubfolderCount = f.ChildFolders.Count,
            })
            .ToListAsync(ct);

        return folders;
    }

    public async Task<TeacherContentFolderDto?> CreateFolderAsync(int teacherId, CreateTeacherContentFolderDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return null;

        if (dto.ParentFolderId.HasValue)
        {
            var parentExists = await _db.TeacherContentFolders.AnyAsync(
                f => f.Id == dto.ParentFolderId && f.TeacherId == teacherId, ct);
            if (!parentExists) return null;
        }

        var name = dto.Name.Trim();
        var duplicate = await _db.TeacherContentFolders.AnyAsync(
            f => f.TeacherId == teacherId && f.ParentFolderId == dto.ParentFolderId && f.Name == name, ct);
        if (duplicate) return null;

        var folder = new TeacherContentFolder
        {
            TeacherId = teacherId,
            ParentFolderId = dto.ParentFolderId,
            Name = name,
        };
        _db.TeacherContentFolders.Add(folder);
        await _db.SaveChangesAsync(ct);

        return new TeacherContentFolderDto
        {
            Id = folder.Id,
            Name = folder.Name,
            ParentFolderId = folder.ParentFolderId,
            ItemCount = 0,
            SubfolderCount = 0,
        };
    }

    public async Task<TeacherContentFolderDto?> UpdateFolderAsync(int teacherId, int folderId, UpdateTeacherContentFolderDto dto, CancellationToken ct)
    {
        var folder = await _db.TeacherContentFolders.FirstOrDefaultAsync(
            f => f.Id == folderId && f.TeacherId == teacherId, ct);
        if (folder == null) return null;

        if (dto.ParentFolderId.HasValue && dto.ParentFolderId == folderId) return null;

        if (dto.ParentFolderId.HasValue)
        {
            var parentOk = await _db.TeacherContentFolders.AnyAsync(
                f => f.Id == dto.ParentFolderId && f.TeacherId == teacherId, ct);
            if (!parentOk) return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
            folder.Name = dto.Name.Trim();

        if (dto.ParentFolderId.HasValue || dto.ParentFolderId == null)
            folder.ParentFolderId = dto.ParentFolderId;

        folder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await MapFolderAsync(folder.Id, ct);
    }

    public async Task<bool> DeleteFolderAsync(int teacherId, int folderId, CancellationToken ct)
    {
        var folder = await _db.TeacherContentFolders
            .Include(f => f.Items)
            .Include(f => f.ChildFolders)
            .FirstOrDefaultAsync(f => f.Id == folderId && f.TeacherId == teacherId, ct);

        if (folder == null) return false;
        if (folder.Items.Count > 0 || folder.ChildFolders.Count > 0) return false;

        _db.TeacherContentFolders.Remove(folder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<TeacherContentItemDto>> ListItemsAsync(
        int teacherId, int? folderId, TeacherContentItemKind? kind, string? search,
        int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = _db.TeacherContentItems.AsNoTracking().Where(i => i.TeacherId == teacherId);

        if (folderId.HasValue)
            query = query.Where(i => i.FolderId == folderId);
        else
            query = query.Where(i => i.FolderId == null);

        if (kind.HasValue)
            query = query.Where(i => i.Kind == kind.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Title.Contains(term) || (i.Description != null && i.Description.Contains(term)));
        }

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                Item = i,
                LinkCount = i.SessionLinks.Count,
            })
            .ToListAsync(ct);

        return items.Select(x => MapItem(x.Item, x.LinkCount)).ToList();
    }

    public async Task<TeacherContentItemDto?> GetItemAsync(int teacherId, int itemId, CancellationToken ct)
    {
        var item = await _db.TeacherContentItems.AsNoTracking()
            .Include(i => i.SessionLinks)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TeacherId == teacherId, ct);
        return item == null ? null : MapItem(item, item.SessionLinks.Count);
    }

    public async Task<TeacherContentItemDto?> UploadFileAsync(
        int teacherId, IFormFile file, int? folderId, string? title, string? description, List<string>? tags, CancellationToken ct)
    {
        if (!await _fileStorage.ValidateFileAsync(file, AllowedExtensions, MaxFileSizeBytes))
            return null;

        if (folderId.HasValue)
        {
            var folderOk = await _db.TeacherContentFolders.AnyAsync(
                f => f.Id == folderId && f.TeacherId == teacherId, ct);
            if (!folderOk) return null;
        }

        var item = new TeacherContentItem
        {
            TeacherId = teacherId,
            FolderId = folderId,
            Kind = TeacherContentItemKind.File,
            Title = string.IsNullOrWhiteSpace(title) ? file.FileName : title.Trim(),
            Description = description,
            FileType = InferFileType(file.FileName),
            SizeBytes = file.Length,
            TagsJson = SerializeTags(tags),
            UploadStatus = TeacherContentUploadStatus.Pending,
        };

        _db.TeacherContentItems.Add(item);
        await _db.SaveChangesAsync(ct);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storageKey = $"teachers/{teacherId}/content/{item.Id}{ext}";
        var ossPublicBase = _configuration["OssSettings:LearningPublicBaseUrl"]
                          ?? _configuration["OSS_LEARNING_PUBLIC_BASE_URL"]
                          ?? _configuration["OssSettings:PublicBaseUrl"]
                          ?? _configuration["OSS_PUBLIC_BASE_URL"]
                          ?? string.Empty;
        item.StorageKey = storageKey;
        item.PublicUrl = string.IsNullOrEmpty(ossPublicBase)
            ? null
            : $"{ossPublicBase.TrimEnd('/')}/{storageKey}";
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        try
        {
            await _fileStorage.QueueTeacherContentFileUploadAsync(file, teacherId, item.Id, storageKey);
        }
        catch
        {
            _db.TeacherContentItems.Remove(item);
            await _db.SaveChangesAsync(ct);
            throw;
        }

        return MapItem(item, 0);
    }

    public async Task<TeacherContentItemDto?> CreateHomeworkAsync(int teacherId, CreateHomeworkTemplateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return null;

        if (dto.FolderId.HasValue)
        {
            var folderOk = await _db.TeacherContentFolders.AnyAsync(
                f => f.Id == dto.FolderId && f.TeacherId == teacherId, ct);
            if (!folderOk) return null;
        }

        var item = new TeacherContentItem
        {
            TeacherId = teacherId,
            FolderId = dto.FolderId,
            Kind = TeacherContentItemKind.Homework,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            TagsJson = SerializeTags(dto.Tags),
        };

        _db.TeacherContentItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return MapItem(item, 0);
    }

    public async Task<TeacherContentItemDto?> UpdateItemAsync(int teacherId, int itemId, UpdateTeacherContentItemDto dto, CancellationToken ct)
    {
        var item = await _db.TeacherContentItems
            .Include(i => i.SessionLinks)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TeacherId == teacherId, ct);
        if (item == null) return null;

        if (dto.FolderId.HasValue)
        {
            var folderOk = await _db.TeacherContentFolders.AnyAsync(
                f => f.Id == dto.FolderId && f.TeacherId == teacherId, ct);
            if (!folderOk) return null;
            item.FolderId = dto.FolderId;
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
            item.Title = dto.Title.Trim();
        if (dto.Description != null)
            item.Description = dto.Description;
        if (dto.Tags != null)
            item.TagsJson = SerializeTags(dto.Tags);

        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapItem(item, item.SessionLinks.Count);
    }

    public async Task<bool> DeleteItemAsync(int teacherId, int itemId, CancellationToken ct)
    {
        var item = await _db.TeacherContentItems
            .Include(i => i.SessionLinks)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TeacherId == teacherId, ct);

        if (item == null) return false;
        if (item.UploadStatus == TeacherContentUploadStatus.Pending) return false;
        if (item.SessionLinks.Count > 0) return false;

        if (!string.IsNullOrEmpty(item.StorageKey))
            await _fileStorage.DeleteFileAsync(item.StorageKey);

        _db.TeacherContentItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<TeacherSessionContentLinkDto>> ListSessionContentAsync(int teacherId, int scheduleId, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return new List<TeacherSessionContentLinkDto>();
        return await GetContentLinksForSessionAsync(scheduleId, ct);
    }

    public async Task<TeacherSessionContentLinkDto?> LinkSessionContentAsync(int teacherId, int scheduleId, int contentItemId, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return null;

        var item = await _db.TeacherContentItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == contentItemId && i.TeacherId == teacherId, ct);
        if (item == null) return null;
        if (item.Kind != TeacherContentItemKind.File) return null;
        if (item.UploadStatus != TeacherContentUploadStatus.Ready) return null;

        var exists = await _db.SessionContentLinks.AnyAsync(
            l => l.CourseScheduleId == scheduleId && l.ContentItemId == contentItemId, ct);
        if (exists) return null;

        var link = new SessionContentLink
        {
            CourseScheduleId = scheduleId,
            ContentItemId = contentItemId,
            LinkedAt = DateTime.UtcNow,
        };
        _db.SessionContentLinks.Add(link);
        await _db.SaveChangesAsync(ct);

        return MapLink(link.Id, item);
    }

    public async Task<List<TeacherSessionContentLinkDto>> LinkSessionContentBulkAsync(
        int teacherId, int scheduleId, LinkSessionContentBulkDto dto, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return new List<TeacherSessionContentLinkDto>();
        if (dto.ContentItemIds == null || dto.ContentItemIds.Count == 0) return new List<TeacherSessionContentLinkDto>();

        var distinctIds = dto.ContentItemIds.Distinct().ToList();
        var items = await _db.TeacherContentItems.AsNoTracking()
            .Where(i => distinctIds.Contains(i.Id) && i.TeacherId == teacherId)
            .ToListAsync(ct);

        if (items.Count != distinctIds.Count) return new List<TeacherSessionContentLinkDto>();

        var existingIds = await _db.SessionContentLinks.AsNoTracking()
            .Where(l => l.CourseScheduleId == scheduleId)
            .Select(l => l.ContentItemId)
            .ToListAsync(ct);
        var existingSet = existingIds.ToHashSet();

        var created = new List<TeacherSessionContentLinkDto>();
        foreach (var item in items)
        {
            if (item.Kind != TeacherContentItemKind.File) continue;
            if (item.UploadStatus != TeacherContentUploadStatus.Ready) continue;
            if (existingSet.Contains(item.Id)) continue;

            var link = new SessionContentLink
            {
                CourseScheduleId = scheduleId,
                ContentItemId = item.Id,
                LinkedAt = DateTime.UtcNow,
            };
            _db.SessionContentLinks.Add(link);
            await _db.SaveChangesAsync(ct);
            existingSet.Add(item.Id);
            created.Add(MapLink(link.Id, item));
        }

        return created;
    }

    public async Task<bool> UnlinkSessionContentAsync(int teacherId, int scheduleId, int linkId, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return false;

        var link = await _db.SessionContentLinks.FirstOrDefaultAsync(
            l => l.Id == linkId && l.CourseScheduleId == scheduleId, ct);
        if (link == null) return false;

        _db.SessionContentLinks.Remove(link);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<TeacherSessionContentLinkDto>> GetContentLinksForSessionAsync(int scheduleId, CancellationToken ct)
    {
        return await _db.SessionContentLinks.AsNoTracking()
            .Where(l => l.CourseScheduleId == scheduleId)
            .Include(l => l.ContentItem)
            .OrderBy(l => l.LinkedAt)
            .Select(l => new TeacherSessionContentLinkDto
            {
                Id = l.Id,
                ContentItemId = l.ContentItemId,
                Kind = l.ContentItem.Kind,
                Title = l.ContentItem.Title,
                Description = l.ContentItem.Description,
                FileType = l.ContentItem.FileType,
                PublicUrl = l.ContentItem.PublicUrl,
            })
            .ToListAsync(ct);
    }

    public async Task<List<TeacherSessionHomeworkDto>> ListSessionHomeworkAsync(int teacherId, int scheduleId, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return new List<TeacherSessionHomeworkDto>();
        return await GetHomeworkForSessionAsync(scheduleId, ct);
    }

    public async Task<TeacherSessionHomeworkDto?> CreateSessionHomeworkAsync(
        int teacherId, int scheduleId, CreateSessionHomeworkDto dto, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return null;
        if (string.IsNullOrWhiteSpace(dto.Title)) return null;

        var fileItems = await ResolveReadyFileItemsAsync(teacherId, dto.ContentItemIds, ct);
        if (dto.ContentItemIds?.Count > 0 && fileItems.Count != dto.ContentItemIds.Distinct().Count())
            return null;

        var assignment = new SessionHomeworkAssignment
        {
            CourseScheduleId = scheduleId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            DueAt = dto.DueAt,
            AssignedAt = DateTime.UtcNow,
            FileLinks = fileItems.Select(item => new SessionHomeworkFileLink
            {
                ContentItemId = item.Id,
                LinkedAt = DateTime.UtcNow,
            }).ToList(),
        };

        _db.SessionHomeworkAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return await MapHomeworkAsync(assignment.Id, scheduleId, ct);
    }

    public async Task<TeacherSessionHomeworkDto?> UpdateSessionHomeworkAsync(
        int teacherId, int scheduleId, int assignmentId, UpdateSessionHomeworkDto dto, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return null;

        var assignment = await _db.SessionHomeworkAssignments
            .Include(a => a.FileLinks)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseScheduleId == scheduleId, ct);
        if (assignment == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title))
            assignment.Title = dto.Title.Trim();
        if (dto.Description != null)
            assignment.Description = dto.Description;
        if (dto.DueAt.HasValue || dto.DueAt == null)
            assignment.DueAt = dto.DueAt;

        if (dto.ContentItemIds != null)
        {
            var fileItems = await ResolveReadyFileItemsAsync(teacherId, dto.ContentItemIds, ct);
            if (dto.ContentItemIds.Distinct().Count() != fileItems.Count)
                return null;

            _db.SessionHomeworkFileLinks.RemoveRange(assignment.FileLinks);
            assignment.FileLinks = fileItems.Select(item => new SessionHomeworkFileLink
            {
                SessionHomeworkAssignmentId = assignment.Id,
                ContentItemId = item.Id,
                LinkedAt = DateTime.UtcNow,
            }).ToList();
        }

        await _db.SaveChangesAsync(ct);
        return await MapHomeworkAsync(assignment.Id, scheduleId, ct);
    }

    public async Task<bool> DeleteSessionHomeworkAsync(int teacherId, int scheduleId, int assignmentId, CancellationToken ct)
    {
        if (!await OwnsScheduleAsync(teacherId, scheduleId, ct)) return false;

        var assignment = await _db.SessionHomeworkAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.CourseScheduleId == scheduleId, ct);
        if (assignment == null) return false;

        _db.SessionHomeworkAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<TeacherSessionHomeworkDto>> GetHomeworkForSessionAsync(int scheduleId, CancellationToken ct)
    {
        var totalStudents = await CountEnrollmentStudentsAsync(scheduleId, ct);
        var assignments = await _db.SessionHomeworkAssignments.AsNoTracking()
            .Where(a => a.CourseScheduleId == scheduleId)
            .Include(a => a.FileLinks)
            .ThenInclude(f => f.ContentItem)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);

        return assignments.Select(a => MapHomework(a, totalStudents)).ToList();
    }

    private async Task<List<TeacherContentItem>> ResolveReadyFileItemsAsync(
        int teacherId, List<int>? contentItemIds, CancellationToken ct)
    {
        if (contentItemIds == null || contentItemIds.Count == 0)
            return new List<TeacherContentItem>();

        var distinctIds = contentItemIds.Distinct().ToList();
        var items = await _db.TeacherContentItems.AsNoTracking()
            .Where(i => distinctIds.Contains(i.Id) && i.TeacherId == teacherId)
            .ToListAsync(ct);

        if (items.Count != distinctIds.Count) return new List<TeacherContentItem>();
        if (items.Any(i => i.Kind != TeacherContentItemKind.File || i.UploadStatus != TeacherContentUploadStatus.Ready))
            return new List<TeacherContentItem>();

        return items;
    }

    private async Task<TeacherSessionHomeworkDto?> MapHomeworkAsync(int assignmentId, int scheduleId, CancellationToken ct)
    {
        var totalStudents = await CountEnrollmentStudentsAsync(scheduleId, ct);
        var assignment = await _db.SessionHomeworkAssignments.AsNoTracking()
            .Include(a => a.FileLinks)
            .ThenInclude(f => f.ContentItem)
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);
        return assignment == null ? null : MapHomework(assignment, totalStudents);
    }

    private async Task<int> CountEnrollmentStudentsAsync(int scheduleId, CancellationToken ct)
    {
        return await _db.CourseSchedules.AsNoTracking()
            .Where(s => s.Id == scheduleId)
            .Select(s => s.Enrollment.Participants.Count)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<bool> OwnsScheduleAsync(int teacherId, int scheduleId, CancellationToken ct)
    {
        return await _db.CourseSchedules.AsNoTracking()
            .AnyAsync(s => s.Id == scheduleId &&
                (s.Enrollment.ApprovedByTeacherId == teacherId ||
                 (s.Enrollment.Course != null && s.Enrollment.Course.TeacherId == teacherId)), ct);
    }

    private async Task<TeacherContentFolderDto?> MapFolderAsync(int folderId, CancellationToken ct)
    {
        return await _db.TeacherContentFolders.AsNoTracking()
            .Where(f => f.Id == folderId)
            .Select(f => new TeacherContentFolderDto
            {
                Id = f.Id,
                Name = f.Name,
                ParentFolderId = f.ParentFolderId,
                ItemCount = f.Items.Count,
                SubfolderCount = f.ChildFolders.Count,
            })
            .FirstOrDefaultAsync(ct);
    }

    private static TeacherContentItemDto MapItem(TeacherContentItem item, int linkCount) => new()
    {
        Id = item.Id,
        FolderId = item.FolderId,
        Kind = item.Kind,
        Title = item.Title,
        Description = item.Description,
        FileType = item.FileType,
        SizeBytes = item.SizeBytes,
        PublicUrl = item.PublicUrl,
        Tags = DeserializeTags(item.TagsJson),
        UsedInSessionsCount = linkCount,
        UploadedAt = item.CreatedAt,
        UploadStatus = item.UploadStatus,
    };

    private static TeacherSessionContentLinkDto MapLink(int linkId, TeacherContentItem item) => new()
    {
        Id = linkId,
        ContentItemId = item.Id,
        Kind = item.Kind,
        Title = item.Title,
        Description = item.Description,
        FileType = item.FileType,
        PublicUrl = item.PublicUrl,
    };

    private static TeacherSessionHomeworkDto MapHomework(SessionHomeworkAssignment assignment, int totalStudents) => new()
    {
        Id = assignment.Id,
        Title = assignment.Title,
        Description = assignment.Description,
        DueAt = assignment.DueAt,
        SubmittedCount = 0,
        TotalStudents = totalStudents,
        Files = assignment.FileLinks
            .OrderBy(f => f.LinkedAt)
            .Select(f => new TeacherSessionHomeworkFileDto
            {
                ContentItemId = f.ContentItemId,
                Title = f.ContentItem.Title,
                FileType = f.ContentItem.FileType,
                PublicUrl = f.ContentItem.PublicUrl,
            })
            .ToList(),
    };

    private static TeacherContentFileType InferFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => TeacherContentFileType.Pdf,
            ".png" or ".jpg" or ".jpeg" => TeacherContentFileType.Image,
            ".mp4" => TeacherContentFileType.Video,
            ".doc" or ".docx" => TeacherContentFileType.Doc,
            _ => TeacherContentFileType.Other,
        };
    }

    private static string SerializeTags(List<string>? tags) =>
        JsonSerializer.Serialize(tags ?? new List<string>());

    private static List<string> DeserializeTags(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
