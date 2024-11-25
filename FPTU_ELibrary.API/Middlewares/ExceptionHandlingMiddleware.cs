using FPTU_ELibrary.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;
using System.Net;

namespace FPTU_ELibrary.API.Middlewares
{
    //  Summary:
    //      This act as exception handling middleware, where it will 
    //      catch all exceptions invoked within the application and
    //      continue to process response with [Problem details] for to client
	public class ExceptionHandlingMiddleware
	{
		// Func that can process HTTP request
        private readonly RequestDelegate _next;
        
		/// Factory to produce <see cref="ProblemDetails" />
		private readonly ProblemDetailsFactory _problemDetailsFactory;

		// Contructors
		public ExceptionHandlingMiddleware(
            RequestDelegate next,
			ProblemDetailsFactory problemDetailsFactory)
        {
            _next = next;
			_problemDetailsFactory = problemDetailsFactory;
		}

		// Invoke when HTTP request mainstream is progressing and reach the exception middleware
		public async Task InvokeAsync(HttpContext httpContext)
		{
			try
			{
				// Nothing happen, keep on progressing request 
				await _next(httpContext);
			}
			catch (Exception ex) // Exception invoke 
			{
				// Handle exception with Problem details for HTTP APIs
				await HandleExceptionAsync(httpContext, ex);
			}
		}

		// Handle Exception invoke
		private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
		{
			// Initialize problem details
			// This is a machine-readable format for specifying errors in HTTP API responses based on
			var problem = new ProblemDetails();
			// Assign exception detail
			problem.Detail = ex.Message;
			// Assign instance, where the exception invoke
			problem.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";

			// Define problem status based on specific exception type
			switch (ex)
			{
				// NotFoundException
				case NotFoundException notFoundException:
					problem.Status = (int)HttpStatusCode.NotFound;
					break;
				// BadRequestException
				case BadRequestException badRequestException:
					problem.Status = (int)HttpStatusCode.BadRequest;
					break;
				// UnprocessableEntityException
				case UnprocessableEntityException unprocessableEntityException:
					problem.Status = (int)HttpStatusCode.UnprocessableEntity;
					foreach(var validationResult in unprocessableEntityException.Errors)
					{
						problem.Extensions.Add(validationResult.Key, validationResult.Value);
					}
					break;
				// UnauthorizedException
				case UnauthorizedException unauthorizedException:
					problem.Status = (int)HttpStatusCode.Unauthorized;
					break;
				// ForbiddenException
				case ForbiddenException forbiddenException:
					problem.Status = (int)HttpStatusCode.Forbidden;
					break;
				// Default 
				default:
					problem.Status = (int)HttpStatusCode.InternalServerError;
					break;
			}

			// Add title and type for problem details
			if (_problemDetailsFactory != null)
			{
				// Creates a ProblemDetails instance that configures defaults based on values specified
				var tempProbDetail = _problemDetailsFactory.CreateProblemDetails(
					httpContext, statusCode: problem.Status);
				// Assign title and type 
				problem.Type = tempProbDetail.Type;
				problem.Title = tempProbDetail.Title;
			}

			// Initiate ObjectResult
			var result = new ObjectResult(problem)
			{
				StatusCode = problem.Status
			};

			// Serialize result object to str
			var response = JsonConvert.SerializeObject(result.Value);
			// Response Status Code
			httpContext.Response.StatusCode = (int)result.StatusCode;
			// Response Content-Type
			httpContext.Response.ContentType = "application/problem+json";
			// Write to response body
			await httpContext.Response.WriteAsync(response);
		}
	}
}
