using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Qalam.Data.DTOs.Auth;
using Qalam.Data.Entity.Identity;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class UserProfileService : IUserProfileService
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorage;
    private readonly IStudentRepository _studentRepository;
    private readonly IGuardianRepository _guardianRepository;
    private readonly IMediaUrlResolver _mediaUrlResolver;

    public UserProfileService(
        UserManager<User> userManager,
        IFileStorageService fileStorage,
        IStudentRepository studentRepository,
        IGuardianRepository guardianRepository,
        IMediaUrlResolver mediaUrlResolver)
    {
        _userManager = userManager;
        _fileStorage = fileStorage;
        _studentRepository = studentRepository;
        _guardianRepository = guardianRepository;
        _mediaUrlResolver = mediaUrlResolver;
    }

    public async Task<UserProfilePictureUpdateResult> UpdateProfilePictureAsync(
        int userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return UserProfilePictureUpdateResult.FailNotFound("User not found.");

        if (file == null || file.Length == 0)
            return UserProfilePictureUpdateResult.Fail("Profile picture file is required.");

        var valid = await _fileStorage.ValidateFileAsync(file, AllowedImageExtensions, MaxImageBytes);
        if (!valid)
            return UserProfilePictureUpdateResult.Fail(
                "Invalid image. Use jpg, jpeg, png, or webp up to 5 MB.");

        var previousUrl = user.ProfilePictureUrl;
        await _fileStorage.QueueProfilePicUploadAsync(file, userId, previousUrl);

        return UserProfilePictureUpdateResult.Ok(
            _mediaUrlResolver.ToPublicUrl(user.ProfilePictureUrl));
    }

    public async Task<RelatedAccountsDto> GetRelatedAccountsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var dto = new RelatedAccountsDto();

        var selfStudent = await _studentRepository.GetTableNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (selfStudent != null)
        {
            dto.HasGuardian = selfStudent.GuardianId.HasValue;
            dto.SelfStudent = new RelatedSelfStudentDto
            {
                StudentId = selfStudent.Id,
                FullName = FormatFullName(selfStudent.User),
                IsMinor = selfStudent.IsMinor,
            };
        }

        var guardian = await _guardianRepository.GetByUserIdAsync(userId);
        if (guardian == null)
            return dto;

        var children = await _studentRepository.GetChildrenByGuardianIdAsync(guardian.Id);

        dto.Children = children
            .Select(c => new RelatedChildAccountDto
            {
                StudentId = c.Id,
                FullName = FormatFullName(c.User),
                ProfilePictureUrl = _mediaUrlResolver.ToPublicUrl(c.User?.ProfilePictureUrl),
            })
            .ToList();

        return dto;
    }

    private static string FormatFullName(User? user)
    {
        if (user == null)
            return string.Empty;

        return string.Join(
            " ",
            new[]
            {
                (user.FirstName ?? string.Empty).Trim(),
                (user.LastName ?? string.Empty).Trim(),
            }.Where(s => !string.IsNullOrEmpty(s)));
    }
}
