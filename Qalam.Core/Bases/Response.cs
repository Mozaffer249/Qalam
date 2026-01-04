using System.Net;

namespace Qalam.Core.Bases
{
    public class Response<T>
    {
        public Response()
        {
        }

        public Response(T data, string message = null!)
        {
            Succeeded = true;
            Message = message;
            Data = data;
        }

        public Response(string message)
        {
            Succeeded = false;
            Message = message;
        }

        public Response(string message, bool succeeded)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public HttpStatusCode StatusCode { get; set; }
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public T Data { get; set; } = default!;
        public List<string>? Errors { get; set; }
        public object? Meta { get; set; }


    }
}

