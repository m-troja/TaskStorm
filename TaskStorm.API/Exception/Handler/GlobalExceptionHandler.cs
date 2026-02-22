using System.Text.Json;
using TaskStorm.Exception.GptException;
using TaskStorm.Exception.IssueException;
using TaskStorm.Exception.LoginException;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Exception.Registration;
using TaskStorm.Exception.Tokens;
using TaskStorm.Exception.UserException;

namespace TaskStorm.Exception.Handler
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {
                    UserNotFoundException => StatusCodes.Status404NotFound,
                    UserDisabledException => StatusCodes.Status403Forbidden,
                    InvalidEmailOrPasswordException => StatusCodes.Status401Unauthorized,
                    RegisterEmailException => StatusCodes.Status400BadRequest,
                    UserAlreadyExistsException => StatusCodes.Status409Conflict,
                    IssueCreationException => StatusCodes.Status400BadRequest,
                    IssueNotFoundException => StatusCodes.Status400BadRequest,
                    ProjectNotFoundException => StatusCodes.Status404NotFound,
                    InvalidProjectData => StatusCodes.Status400BadRequest,
                    InvalidRefreshTokenException => StatusCodes.Status400BadRequest,
                    GptConnectionException => StatusCodes.Status408RequestTimeout,
                    BadRequestException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };
                var errorResponse = new TaskStorm.Exception.Error.ErrorResponse(
                    ex switch
                    {
                        UserNotFoundException => TaskStorm.Exception.Error.ErrorType.USER_NOT_FOUND,
                        UserDisabledException => TaskStorm.Exception.Error.ErrorType.USER_DISABLED,
                        InvalidEmailOrPasswordException => TaskStorm.Exception.Error.ErrorType.LOGIN_ERROR,
                        RegisterEmailException => TaskStorm.Exception.Error.ErrorType.REGISTRATION_ERROR,
                        UserAlreadyExistsException => TaskStorm.Exception.Error.ErrorType.USER_ALREADY_REGISTERED,
                        IssueCreationException => TaskStorm.Exception.Error.ErrorType.ISSUE_CREATION_ERROR,
                        IssueNotFoundException => TaskStorm.Exception.Error.ErrorType.ISSUE_NOT_FOUND,
                        ProjectNotFoundException => TaskStorm.Exception.Error.ErrorType.PROJECT_NOT_FOUND,
                        InvalidProjectData => TaskStorm.Exception.Error.ErrorType.INVALID_PROJECT_DATA,
                        InvalidRefreshTokenException => TaskStorm.Exception.Error.ErrorType.INVALID_REFRESH_TOKEN,
                        GptConnectionException => TaskStorm.Exception.Error.ErrorType.GPT_ERROR,
                        BadRequestException => TaskStorm.Exception.Error.ErrorType.BAD_REQUEST,
                        _ => TaskStorm.Exception.Error.ErrorType.SERVER_ERROR
                    },
                    ex.Message
                );
                var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });
                await context.Response.WriteAsync(jsonResponse);
            }
        }
    }
}
