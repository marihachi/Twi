using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="http"></param>
		public OAuthRequester(HttpClient http)
		{
			Rand = new Random();
			Http = http;
		}

		private string UrlEncodeNormal(string source)
		{
			return WebUtility.UrlEncode(source).Replace("%20", "+");
		}

		private string BuildQueryStringNormal(IDictionary<string, string> parameters)
		{
			var pairs = from p in parameters select $"{UrlEncodeNormal(p.Key)}={UrlEncodeNormal(p.Value)}";
			var queryString = string.Join("&", pairs);

			return queryString;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="consumerKey"></param>
		/// <param name="consumerSecret"></param>
		/// <param name="token"></param>
		/// <param name="tokenSecret"></param>
		/// <param name="method"></param>
		/// <param name="url"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public async Task<HttpResponseMessage> Request(string consumerKey, string consumerSecret, string token, string tokenSecret, HttpMethod method, string url, IDictionary<string, string> parameters = null)
		{
			if (consumerKey == null || consumerSecret == null || method == null || url == null)
			{
				throw new ArgumentNullException();
			}
			Debug.WriteLine("== OAuthリクエスト 開始 ==");
			if (method == HttpMethod.Get && parameters != null)
			{
				url += $"?{BuildQueryString(parameters)}";
			}

			var req = new HttpRequestMessage(method, url);

			req.Headers.ExpectContinue = false;

			if (method == HttpMethod.Post && parameters != null)
			{
				req.Content = new FormUrlEncodedContent(parameters);
			}

			req.Headers.Authorization = BuildAuthorizationHeader(consumerKey, consumerSecret, token, tokenSecret, method, url, parameters);

			Debug.WriteLine("== リクエストヘッダ :");
			Debug.WriteLine(req.Headers.ToString());
			if (method == HttpMethod.Post)
				Debug.WriteLine(await req.Content.ReadAsStringAsync());

			var res = await Http.SendAsync(req);
			Debug.WriteLine("== OAuthリクエスト 完了 ==");

			return res;
		}

		public async Task<HttpResponseMessage> UploadMedia(string consumerKey, string consumerSecret, string token, string tokenSecret, string url, IEnumerable<byte> mediaData, string fileName)
		{
			if (consumerKey == null || consumerSecret == null || url == null)
			{
				throw new ArgumentNullException();
			}
			Debug.WriteLine("== OAuthリクエスト 開始 ==");

			var req = new HttpRequestMessage(HttpMethod.Post, url);

			req.Headers.ExpectContinue = false;

			var MultipartContent = new MultipartFormDataContent();
			var content = new ByteArrayContent(mediaData.ToArray());
			content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
			content.Headers.ContentDisposition.Name = "media";
			content.Headers.ContentDisposition.FileName = fileName;
			MultipartContent.Add(content);
			req.Content = MultipartContent;

			req.Headers.Authorization = BuildAuthorizationHeader(consumerKey, consumerSecret, token, tokenSecret, HttpMethod.Post, url);

			Debug.WriteLine("== リクエストヘッダ :");
			Debug.WriteLine(req.Headers.ToString());
			Debug.WriteLine(await req.Content.ReadAsStringAsync());

			var res = await Http.SendAsync(req);
			Debug.WriteLine("== OAuthリクエスト 完了 ==");

			return res;
		}

		private AuthenticationHeaderValue BuildAuthorizationHeader(string consumerKey, string consumerSecret, string token, string tokenSecret, HttpMethod method, string url, IDictionary<string, string> parameters = null)
		{
			Debug.WriteLine("== Authorizationヘッダー組み立て 開始 ==");
			var authParameters = new SortedDictionary<string, string> {
				{ "oauth_consumer_key", consumerKey },
				{ "oauth_timestamp", GenerateTimestamp() },
				{ "oauth_nonce", GenerateNonce() },
				{ "oauth_version", "1.0" },
				{ "oauth_signature_method", "HMAC-SHA1" },
			};

			if (!string.IsNullOrEmpty(token))
			{
				authParameters.Add("oauth_token", token);
			}

			if (parameters != null)
			{
				foreach (var kvp in parameters)
				{
					authParameters.Add(kvp.Key, kvp.Value);
				}
			}

			authParameters.Add("oauth_signature", GenerateSignature(consumerSecret, tokenSecret, method, url, authParameters));
			var authHeader = new AuthenticationHeaderValue("OAuth", BuildQueryString(authParameters, ",", "\""));
			Debug.WriteLine("== Authorizationヘッダー組み立て 完了 ==");

			return authHeader;
		}

		/// <summary>
		/// シグネチャを生成します
		/// </summary>
		private string GenerateSignature(string consumerSecret, string tokenSecret, HttpMethod httpMethod, string url, SortedDictionary<string, string> parameters)
		{
			Debug.WriteLine("== シグネチャ生成 開始 ==");
			var uri = new Uri(url);
			var nonQueryStringUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
			var queryStringParameters = BuildQueryString(parameters);
			var key = UrlEncode(consumerSecret) + '&' + UrlEncode(tokenSecret ?? "");
			// 注: dataに渡す クエリ文字列についても UrlEncode します
			// 注: dataに渡す URLは「?」以降のクエリ文字列は含めません
			var data = httpMethod.ToString() + '&' + UrlEncode(nonQueryStringUrl) + '&' + UrlEncode(queryStringParameters);
			var mac = GenerateMAC(key, data);
			Debug.WriteLine("== シグネチャ生成 完了 ==");

			return mac;
		}

		/// <summary>
		/// RFC3986の非予約文字を除く全ての文字をパーセントエンコードします
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
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
			var pairs = from p in parameters select $"{UrlEncode(p.Key)}={bracket}{UrlEncode(p.Value)}{bracket}";
			var queryString = string.Join(sep, pairs);

			return queryString;
		}

		private string GenerateTimestamp()
		{
			var time = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			var timestamp = Convert.ToInt64(time.TotalSeconds).ToString();

			return timestamp;
		}

		private string GenerateMAC(string key, string data)
		{
			var mac = new HMACSHA1(Encoding.ASCII.GetBytes(key));
			var hash = Convert.ToBase64String(mac.ComputeHash(Encoding.ASCII.GetBytes(data)));

			return hash;
		}

		private string GenerateNonce()
		{
			var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

			var sb = new StringBuilder();
			foreach (var i in Enumerable.Range(0, 32))
				sb.Append(chars[Rand.Next(chars.Length)]);

			return sb.ToString();
		}
	}
}
