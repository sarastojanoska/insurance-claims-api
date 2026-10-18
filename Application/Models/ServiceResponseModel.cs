using System.Net;

namespace Application.Models
{
    public class ServiceResponseModel<T>
    {
        public bool Success { get; private set; }
        public T? Data { get; private set; }
        public string? Error { get; private set; }
        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.OK;

        public static ServiceResponseModel<T> SuccessResult(T data) => new() { Success = true, Data = data };

        public static ServiceResponseModel<T> FailureResult(string error, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            => new() { Success = false, Error = error, StatusCode = statusCode };
    }
}
