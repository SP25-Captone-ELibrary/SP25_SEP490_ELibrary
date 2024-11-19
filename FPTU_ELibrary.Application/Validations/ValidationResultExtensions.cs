using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

using FluentValidationResult = FluentValidation.Results.ValidationResult;

namespace FPTU_ELibrary.Application.Validations
{
	public static class ValidationResultExtensions
	{
		// Config ValidationResult to ValidationProblemDetails instance
		public static ValidationProblemDetails ToProblemDetails(this FluentValidationResult result)
		{
			// Init validation problem details
			ValidationProblemDetails validationProblemDetails = new()
			{
			};

			// Each ValidationResult.Errors is ValidationFailure
			// Contains pair <key,value> (Property, ErrorMessage)
			foreach (ValidationFailure failure in result.Errors)
			{
				// If failure already exist
				if (validationProblemDetails.Errors.ContainsKey(failure.PropertyName))
				{
					// Concat old error with new error
					validationProblemDetails.Errors[failure.PropertyName] =
						// Current arr of error
						validationProblemDetails.Errors[failure.PropertyName]
						// Concat with new error
						.Concat(new[] { failure.ErrorMessage }).ToArray();

				}
				else
				{ // failure is not exist yet
				  // Add errors
					validationProblemDetails.Errors.Add(new KeyValuePair<string, string[]>(
						failure.PropertyName,
						new[] { failure.ErrorMessage }));
				}
			}

			return validationProblemDetails;
		}
	}
}
