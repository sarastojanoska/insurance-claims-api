using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public class ServiceResponseModel<T>
    {
        public bool Success { get; private set; }
        public T? Data { get; private set; }
        public string? Error { get; private set; }

        public static ServiceResponseModel<T> SuccessResult(T data)
            => new()
            {
                Success = true,
                Data = data
            };

        public static ServiceResponseModel<T> FailureResult(string error)
            => new()
            {
                Success = false,
                Error = error
            };
    }
}
