using Microsoft.AspNetCore.Mvc;

namespace Pm.Helper
{
    public static class ApiResponse
    {
        public static ObjectResult Success(object? data, string? message = null)
        {
            return new ObjectResult(new
            {
                data,
                message
            })
            {
                StatusCode = 200
            };
        }

        public static ObjectResult Created(object? data, string? message = null)
        {
            return new ObjectResult(new
            {
                data,
                message
            })
            {
                StatusCode = 201
            };
        }

        public static ObjectResult BadRequest(string objectName, string[] errorMessages)
        {
            var errors = new Dictionary<string, string[]>
            {
                {objectName, errorMessages}
            };

            return new ObjectResult(new
            {
                data = errors
            })
            {
                StatusCode = 400
            };
        }

        public static ObjectResult BadRequest(string objectName, string errorMessage)
        {
            return BadRequest(objectName, new[] { errorMessage });
        }

        public static ObjectResult NotFound(string detailMessage)
        {
            return new ObjectResult(new
            {
                data = new { message = detailMessage }
            })
            {
                StatusCode = 404
            };
        }

        public static ObjectResult InternalServerError(string details)
        {
            return new ObjectResult(new
            {
                data = new
                {
                    details
                },
                message = "Terjadi kesalahan internal server"
            })
            {
                StatusCode = 500
            };
        }

        public static ObjectResult Unauthorized()
        {
            return new ObjectResult(new
            {
                statusCode = StatusCodes.Status401Unauthorized,
                message = "Unauthorized",
                data = new
                {
                    message = "Anda perlu login untuk mengakses halaman ini"
                },
                meta = (object?)null
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        public static ObjectResult Forbidden()
        {
            return new ObjectResult(new
            {
                statusCode = StatusCodes.Status403Forbidden,
                message = "Forbidden",
                data = new
                {
                    message = "Role anda tidak memiliki akses untuk halaman ini"
                },
                meta = (object?)null
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}