using System;

namespace Twi
{
	public class TwitterException : Exception
	{
		public TwitterException(ErrorsObject errors, string message) : base(message)
		{
			Errors = errors;
		}

		public TwitterException(ErrorsObject errors, string message, Exception innerException) : base(message, innerException)
		{
			Errors = errors;
		}

		public ErrorsObject Errors { get; set; }
	}
}
