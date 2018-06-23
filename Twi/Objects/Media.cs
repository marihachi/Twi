using Newtonsoft.Json;

namespace Twi.Objects
{
	[JsonObject]
	public class Media
	{
		[JsonProperty("media_id")]
		public long Id { get; set; }

		[JsonProperty("media_id_string")]
		public string IdString { get; set; }

		[JsonProperty("size")]
		public int FileSize { get; set; }

		[JsonProperty("expires_after_secs")]
		public int ExpiresSec { get; set; }

		public Image Image { get; set; }
	}

	[JsonObject]
	public class Image
	{
		[JsonProperty("image_type")]
		public string Type { get; set; }

		[JsonProperty("h")]
		public int Height { get; set; }

		[JsonProperty("w")]
		public int Width { get; set; }
	}
}
