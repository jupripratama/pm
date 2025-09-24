using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Pm.Middleware
{
    public class ResponseWrapperFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Tidak ada aksi sebelum action dieksekusi
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? 200;
                var originalValue = objectResult.Value;

                object? data = null;
                object? meta = null;

                // Ambil message dari HttpContext.Items jika tersedia
                string message;
                if (context.HttpContext.Items.ContainsKey("message"))
                {
                    message = context.HttpContext.Items["message"]?.ToString()
                        ?? GetDefaultMessageForStatusCode(statusCode);
                }
                else
                {
                    var type = originalValue?.GetType();
                    var props = type?.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    var messageProp = props?.FirstOrDefault(p =>
                        p.Name.Equals("message", StringComparison.OrdinalIgnoreCase));
                    message = messageProp?.GetValue(originalValue)?.ToString()
                        ?? GetDefaultMessageForStatusCode(statusCode);
                }

                if (originalValue != null)
                {
                    if (originalValue is string errorString && statusCode >= 400)
                    {
                        message = errorString;
                        data = null;
                    }
                    else
                    {
                        var type = originalValue.GetType();
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                        var metaProp = props.FirstOrDefault(p => p.Name.Equals("meta", StringComparison.OrdinalIgnoreCase));
                        if (metaProp != null)
                            meta = metaProp.GetValue(originalValue);

                        var dataProp = props.FirstOrDefault(p => p.Name.Equals("data", StringComparison.OrdinalIgnoreCase));
                        if (dataProp != null)
                            data = dataProp.GetValue(originalValue);

                        if (data == null)
                            data = originalValue;
                    }
                }

                context.Result = new JsonResult(new
                {
                    statusCode,
                    message,
                    data,
                    meta
                })
                {
                    StatusCode = statusCode
                };
            }
            else if (context.Result is EmptyResult)
            {
                context.Result = new JsonResult(new
                {
                    statusCode = 204,
                    message = GetDefaultMessageForStatusCode(204),
                    data = new { },
                    meta = (object?)null
                })
                {
                    StatusCode = 204
                };
            }
        }

        private string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                200 => "Success",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Error"
            };
        }
    }
}