using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Twi
{
	internal class OAuthRequester
	{
		private Random Rand { get; set; }

		private HttpClient Http { get; set; }

		public OAuthRequester(HttpClient http)
		{
			Rand = new Random();
			Http = http;
		}

		/// <summary>
		/// RFC3986の非予約文字を除く全ての文字をパーセントエンコードします
		/// </summary>
		private string UrlEncode(string value)
		{
			var unreserved = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
			var sb = new StringBuilder();
			foreach (var c in Encoding.UTF8.GetBytes(value))
			{
				var isUnreserved = c < 0x80 && unreserved.IndexOf((char)c) != -1;
				sb.Append(isUnreserved ? ((char)c).ToString() : $"%{(int)c:X2}");
			}

			return sb.ToString();
		}

		/// <summary>
		/// パラメータをクエリ文字列として組み立てます。キーと値はURLエンコードされます。
		/// ※任意のセパレータと囲い文字を指定可能。
		/// </summary>
		private string BuildQueryString(IDictionary<string, string> parameters, string sep = "&", string bracket = "")
		{
			var pairs = parameters
				.Select(p => $"{UrlEncode(p.Key)}={bracket}{UrlEncode(p.Value)}{bracket}");
			var queryString = string.Join(sep, pairs);

			return queryString;
		}

		private string GenerateMAC(string key, string data)
		{
			var mac = new HMACSHA1(Encoding.ASCII.GetBytes(key));
			var hash = Convert.ToBase64String(mac.ComputeHash(Encoding.ASCII.GetBytes(data)));

			return hash;
		}

		private string GenerateTimestamp()
		{
			var time = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			var timestamp = Convert.ToInt64(time.TotalSeconds).ToString();

			return timestamp;
		}

		private string GenerateNonce()
		{
			var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

			var sb = new StringBuilder();
			foreach (var i in Enumerable.Range(0, 32))
				sb.Append(chars[Rand.Next(chars.Length)]);

			return sb.ToString();
		}

		/// <summary>
		/// シグネチャを生成します
		/// </summary>
		private string GenerateSignature(string consumerSecret, string tokenSecret, HttpMethod httpMethod, string url, SortedDictionary<string, string> parameters)
		{
			var uri = new Uri(url);
			var nonQueryStringUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
			var queryStringParameters = BuildQueryString(parameters);
			var key = UrlEncode(consumerSecret) + '&' + UrlEncode(tokenSecret ?? "");
			// 注: dataに渡す クエリ文字列についても UrlEncode します
			// 注: dataに渡す URLは「?」以降のクエリ文字列は含めません
			var data = httpMethod.ToString() + '&' + UrlEncode(nonQueryStringUrl) + '&' + UrlEncode(queryStringParameters);
			var mac = GenerateMAC(key, data);

			return mac;
		}

		/// <summary>
		/// 認可ヘッダを構築します
		/// </summary>
		private AuthenticationHeaderValue BuildAuthorizationHeader(string consumerKey, string consumerSecret, string token, string tokenSecret, HttpMethod method, string url, IDictionary<string, string> parameters = null)
		{
			var authParameters = new SortedDictionary<string, string> {
				{ "oauth_consumer_key", consumerKey },
				{ "oauth_timestamp", GenerateTimestamp() },
				{ "oauth_nonce", GenerateNonce() },
				{ "oauth_version", "1.0" },
				{ "oauth_signature_method", "HMAC-SHA1" },
			};

			if (!string.IsNullOrEmpty(token))
				authParameters.Add("oauth_token", token);

			if (parameters != null)
			{
				foreach (var kvp in parameters)
				{
					authParameters.Add(kvp.Key, kvp.Value);
				}
			}

			authParameters.Add("oauth_signature", GenerateSignature(consumerSecret, tokenSecret, method, url, authParameters));
			var authHeader = new AuthenticationHeaderValue("OAuth", BuildQueryString(authParameters, ",", "\""));

			return authHeader;
		}

		/// <summary>
		/// OAuth プロトコルに従ってリクエストをします
		/// </summary>
		public async Task<HttpResponseMessage> Request(string consumerKey, string consumerSecret, string token, string tokenSecret, HttpMethod method, string url, IDictionary<string, string> parameters = null)
		{
			if (consumerKey == null || consumerSecret == null || method == null || url == null)
				throw new ArgumentNullException();

			if (method == HttpMethod.Get && parameters != null)
				url += $"?{BuildQueryString(parameters)}";

			var req = new HttpRequestMessage(method, url);

			if (method == HttpMethod.Post && parameters != null)
				req.Content = new FormUrlEncodedContent(parameters);

			req.Headers.ExpectContinue = false;
			req.Headers.Authorization = BuildAuthorizationHeader(consumerKey, consumerSecret, token, tokenSecret, method, url, parameters);
			var res = await Http.SendAsync(req);

			return res;
		}

		private MultipartFormDataContent BuildMediaContent(IEnumerable<byte> mediaData, string fileName)
		{
			var MultipartContent = new MultipartFormDataContent();
			var mediaContent = new ByteArrayContent(mediaData.ToArray());
			mediaContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
			mediaContent.Headers.ContentDisposition.Name = "media";
			mediaContent.Headers.ContentDisposition.FileName = fileName;
			MultipartContent.Add(mediaContent);

			return MultipartContent;
		}

		/// <summary>
		/// OAuth プロトコルに従ってメディアをアップロードします
		/// </summary>
		public async Task<HttpResponseMessage> UploadMedia(string consumerKey, string consumerSecret, string token, string tokenSecret, IEnumerable<byte> mediaData, string fileName)
		{
			if (consumerKey == null || consumerSecret == null)
				throw new ArgumentNullException();

			var url = "https://upload.twitter.com/1.1/media/upload.json";
			var req = new HttpRequestMessage(HttpMethod.Post, url);
			req.Content = BuildMediaContent(mediaData, fileName);
			req.Headers.ExpectContinue = false;
			req.Headers.Authorization = BuildAuthorizationHeader(consumerKey, consumerSecret, token, tokenSecret, HttpMethod.Post, url);
			var res = await Http.SendAsync(req);

			return res;
		}
	}
}
