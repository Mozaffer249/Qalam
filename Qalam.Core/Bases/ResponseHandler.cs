using Microsoft.Extensions.Localization;
using Qalam.Core.Resources.Shared;

namespace Qalam.Core.Bases
{
    public class ResponseHandler
    {
        private readonly IStringLocalizer<SharedResources> _sharedLocalizer;

        public ResponseHandler(IStringLocalizer<SharedResources> sharedLocalizer)
        {
            _sharedLocalizer = sharedLocalizer;
        }

        // Constructor for handlers that use other resource types
        protected ResponseHandler(object localizer)
        {
            // This constructor is used when derived classes pass non-SharedResources localizers
            // They won't use the default response methods with localization
            _sharedLocalizer = null!;
        }

        public Response<T> Deleted<T>(string? Message = null)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = Message ?? _sharedLocalizer?[SharedResourcesKeys.Deleted] ?? "Deleted"
            };
        }
        public Response<T> Success<T>(string? Message = null, T? entity = default, object? Meta = null)
        {
            return new Response<T>()
            {
                Data = entity ?? default!,
                StatusCode = System.Net.HttpStatusCode.OK,
                Succeeded = true,
                Message = Message ?? _sharedLocalizer?[SharedResourcesKeys.Success] ?? "Success",
                Meta = Meta
            };
        }
        public Response<T> Unauthorized<T>(string? Message = null)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.Unauthorized,
                Succeeded = true,
                Message = Message ?? _sharedLocalizer?[SharedResourcesKeys.UnAuthorized] ?? "Unauthorized"
            };
        }
        public Response<T> BadRequest<T>(string? Message = null)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Succeeded = false,
                Message = Message ?? _sharedLocalizer?[SharedResourcesKeys.BadRequest] ?? "Bad Request"
            };
        }

        public Response<T> UnprocessableEntity<T>(string? Message = null)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.UnprocessableEntity,
                Succeeded = false,
                Message = Message ?? _sharedLocalizer?[SharedResourcesKeys.UnprocessableEntity] ?? "Unprocessable Entity"
            };
        }

        public Response<T> NotFound<T>(string? message = null)
        {
            return new Response<T>()
            {
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Succeeded = false,
                Message = message ?? _sharedLocalizer?[SharedResourcesKeys.NotFound] ?? "Not Found"
            };
        }

        public Response<T> Created<T>(string? Message = null, T? entity = default, object? Meta = null)
        {
            return new Response<T>()
            {
                Data = entity ?? default!,
                StatusCode = System.Net.HttpStatusCode.Created,
                Succeeded = true,
                Message = Message ?? _sharedLocalizer?[SharedResourcesKeys.Created] ?? "Created",
                Meta = Meta
            };
        }
    }


}

