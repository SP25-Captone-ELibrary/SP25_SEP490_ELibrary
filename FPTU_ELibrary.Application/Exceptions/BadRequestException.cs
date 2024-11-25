namespace FPTU_ELibrary.Application.Exceptions
{
	[Serializable]
	public class BadRequestException : Exception
	{
        public BadRequestException() { }
       
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
