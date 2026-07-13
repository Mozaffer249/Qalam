using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Course;

namespace Qalam.Core.Features.Teacher.CourseManagement.Commands.UploadCourseImage;

public class UploadCourseImageCommand : IRequest<Response<CourseImageUploadResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public IFormFile File { get; set; } = null!;
}
