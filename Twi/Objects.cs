using Newtonsoft.Json;
using System.Collections.Generic;

namespace Twi.Objects
{
	public class ErrorsObject
	{
		public List<Error> Errors { get; set; }

		public class Error
		{
			public int Code { get; set; }

			public string Message { get; set; }
		}
	}

	internal class MediaObject
	{
		[JsonProperty("media_id")]
		public long Id { get; set; }
	}
}
