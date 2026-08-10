using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Commands.AddChild;

/// <summary>
/// Parent adds a child (Student record linked to Guardian).
/// JSON body: <c>{ "child": { ... } }</c>.
/// Multipart: nested <c>Child.*</c> fields + optional <c>file</c> profile picture.
/// </summary>
public class AddChildCommand : IRequest<Response<int>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
    public AddChildDto Child { get; set; } = null!;

    /// <summary>Optional profile picture when using multipart/form-data (field name: file).</summary>
    public IFormFile? File { get; set; }
}
