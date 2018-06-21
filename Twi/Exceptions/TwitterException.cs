using System;
using Twi.Objects;

namespace Twi.Exceptions
{
	public class TwitterException : Exception
	{
		public TwitterException(TwitterErrors errors, string message) : base(message)
		{
			Errors = errors;
		}

		public TwitterException(TwitterErrors errors, string message, Exception innerException) : base(message, innerException)
		{
			Errors = errors;
		}

		public TwitterErrors Errors { get; set; }
	}
}
