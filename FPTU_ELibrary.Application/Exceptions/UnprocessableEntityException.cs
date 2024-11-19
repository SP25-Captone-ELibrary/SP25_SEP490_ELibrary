using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Application.Exceptions
{
	[Serializable]
	public class UnprocessableEntityException : Exception
	{
        public UnprocessableEntityException(string? message, IDictionary<string, string[]> errors) : base(message)
        {
            Errors = errors;
        }

        public IDictionary<string, string[]> Errors { get; set; } = null!;
	}
}
