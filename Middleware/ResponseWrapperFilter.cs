using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using Pm.DTOs.Common;
using Pm.Helper;

namespace Pm.Middleware
{
    public class ResponseWrapperFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult objectResult)
            {
                var statusCode = objectResult.StatusCode ?? 200;
                var originalValue = objectResult.Value;

                object? data = null;
                object? meta = null;
                string message = GetDefaultMessageForStatusCode(statusCode);

                // Cek apakah ini hasil dari ApiResponse (yaitu objek dengan properti 'data')
                var originalType = originalValue?.GetType();
                if (originalType != null && originalValue is not string)
                {
                    var props = originalType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    var dataProp = props.FirstOrDefault(p => p.Name.Equals("data", StringComparison.OrdinalIgnoreCase));
                    var messageProp = props.FirstOrDefault(p => p.Name.Equals("message", StringComparison.OrdinalIgnoreCase));
                    var metaProp = props.FirstOrDefault(p => p.Name.Equals("meta", StringComparison.OrdinalIgnoreCase));

                    // Ambil message jika ada
                    if (messageProp?.GetValue(originalValue) is string msg)
                        message = msg;

                    // Cek apakah 'data' adalah PagedResultDto<T>
                    if (dataProp != null)
                    {
                        var dataValue = dataProp.GetValue(originalValue);
                        if (dataValue != null)
                        {
                            var dataType = dataValue.GetType();
                            if (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(PagedResultDto<>))
                            {
                                // Extract Data dan Meta dari PagedResultDto
                                var actualData = dataType.GetProperty("Data")?.GetValue(dataValue);
                                var actualMeta = dataType.GetProperty("Meta")?.GetValue(dataValue);

                                data = actualData;
                                meta = actualMeta;
                            }
                            else
                            {
                                // Bukan PagedResultDto, gunakan langsung
                                data = dataValue;
                                // Jika ada meta di dalam original object, ambil juga
                                if (metaProp != null)
                                    meta = metaProp.GetValue(originalValue);
                            }
                        }
                        else
                        {
                            data = null;
                        }
                    }
                    else
                    {
                        // Tidak ada properti 'data' → anggap seluruh originalValue adalah data
                        data = originalValue;

                        // Tapi cek apakah ini PagedResultDto langsung (tanpa dibungkus ApiResponse)
                        if (originalType.IsGenericType && originalType.GetGenericTypeDefinition() == typeof(PagedResultDto<>))
                        {
                            data = originalType.GetProperty("Data")?.GetValue(originalValue);
                            meta = originalType.GetProperty("Meta")?.GetValue(originalValue);
                        }
                        else if (metaProp != null)
                        {
                            meta = metaProp.GetValue(originalValue);
                        }
                    }
                }
                else
                {
                    // Nilai primitif atau string (misal error message)
                    if (originalValue is string str && statusCode >= 400)
                        message = str;
                    data = originalValue;
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
                    message = "No Content",
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

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Tidak ada aksi sebelum action dieksekusi
        }
    }
}