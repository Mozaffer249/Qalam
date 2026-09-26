using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Profile.Commands.UploadSampleLessonMedia;

public class UploadSampleLessonMediaCommand
    : IRequest<Response<SampleLessonMediaUploadResultDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public IFormFile File { get; set; } = null!;
}
