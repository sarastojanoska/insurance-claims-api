using System.Net;

namespace Application.Models
{
    public class ResponseModel<T> : IResponseModel
    {
        public int StatusCode { get; private set; }
        public bool Success { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public T? Data { get; private set; }
        public List<string> Errors { get; private set; } = [];

        public static ResponseModel<T> SuccessResult(T? data, string message = "Success", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ResponseModel<T>
            {
                StatusCode = (int)statusCode,
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ResponseModel<T> FailureResult(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        {
            return new ResponseModel<T>
            {
                StatusCode = (int)statusCode,
                Success = false,
                Message = message,
                Errors = errors ?? []
            };
        }
    }
}
