using Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public sealed class ResponseModelStatusCodeFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult { Value: IResponseModel response } result)
                result.StatusCode = response.StatusCode;
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}