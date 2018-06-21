using System.Collections.Generic;

namespace Twi.Objects
{
	public class TwitterError
	{
		public int Code { get; set; }

		public string Message { get; set; }
	}

	public class TwitterErrors
	{
		public List<TwitterError> Errors { get; set; }
	}
}
