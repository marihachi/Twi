using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Twi.Exceptions;
using Twi.Objects;

namespace Twi
{
	/// <summary>
	/// Twitter向けの認可リクエストやAPIリクエストの機能を提供します
	/// </summary>
	public class TwiClient
	{
		private const string RequestTokenUrl = "https://twitter.com/oauth/request_token";
		private const string AccessTokenUrl = "https://twitter.com/oauth/access_token";
		private const string AuthorizationUrl = "https://twitter.com/oauth/authorize";

		public string ConsumerKey { get; private set; }
		public string ConsumerSecret { get; private set; }
		public string RequestToken { get; private set; }
		public string RequestTokenSecret { get; private set; }
		public string AccessToken { get; private set; }
		public string AccessTokenSecret { get; private set; }

		private OAuthRequester Requester { get; set; }

		/// <summary>
		/// TwiClient クラスの新しいインスタンスを初期化します
		/// </summary>
		/// <param name="http"></param>
		/// <param name="consumerKey"></param>
		/// <param name="consumerSecret"></param>
		/// <param name="accessToken"></param>
		/// <param name="accessTokenSecret"></param>
		public TwiClient(HttpClient http, string consumerKey, string consumerSecret, string accessToken = null, string accessTokenSecret = null)
		{
			Requester = new OAuthRequester(http);
			ConsumerKey = consumerKey;
			ConsumerSecret = consumerSecret;
			AccessToken = accessToken;
			AccessTokenSecret = accessTokenSecret;
		}

		private static Dictionary<string, string> ParseQueryString(string queryString)
		{
			return queryString.Split('&')
				.Select(i => {
					var kv = i.Split('=');
					return new { Key = kv[0], Value = kv[1] };
				})
				.ToDictionary(i => i.Key, i => i.Value);
		}

		/// <summary>
		/// 認可をリクエストし、認可URLを取得します
		/// </summary>
		/// <exception cref="TwitterException" />
		/// <exception cref="Exception" />
		public async Task<Uri> GetAuthorizationUrl()
		{
			var res = await Requester.Request(ConsumerKey, ConsumerSecret, null, null, HttpMethod.Get, RequestTokenUrl);
			var resStr = await res.Content.ReadAsStringAsync();

			try
			{
				var parsed = ParseQueryString(resStr);
				RequestToken = parsed["oauth_token"];
				RequestTokenSecret = parsed["oauth_token_secret"];

				return new Uri($"{AuthorizationUrl}?oauth_token={RequestToken}");
			}
			catch(Exception ex)
			{
				var t = JsonConvert.DeserializeObject<JToken>(resStr);
				if (t["errors"] != null)
				{
					var es = JsonConvert.DeserializeObject<ErrorsObject>(resStr);
					throw new TwitterException(es, "認可URLの取得に失敗しました。", ex);
				}
				else
				{
					Debug.WriteLine($"GetAuthorizationUrlに失敗: {resStr}");
					throw new Exception("認可URLの取得に失敗しました。", ex);
				}
			}
		}

		/// <summary>
		/// PINコードを渡してアクセストークンをリクエストします
		/// </summary>
		/// <exception cref="InvalidOperationException" />
		/// <exception cref="TwitterException" />
		/// <exception cref="Exception" />
		public async Task Authorize(string pin)
		{
			if (RequestToken == null || RequestTokenSecret == null)
				throw new InvalidOperationException("この操作は GetAuthorizationUrl メソッドを呼び出すまでは無効です");

			var parameters = new Dictionary<string, string> { { "oauth_verifier", pin } };
			var res = await Requester.Request(ConsumerKey, ConsumerSecret, RequestToken, RequestTokenSecret, HttpMethod.Post, AccessTokenUrl, parameters);
			var resStr = await res.Content.ReadAsStringAsync();

			try
			{
				var parsed = ParseQueryString(resStr);
				AccessToken = parsed["oauth_token"];
				AccessTokenSecret = parsed["oauth_token_secret"];
			}
			catch (Exception ex)
			{
				var t = JsonConvert.DeserializeObject<JToken>(resStr);
				if (t["errors"] != null)
				{
					var es = JsonConvert.DeserializeObject<ErrorsObject>(resStr);
					throw new TwitterException(es, "AccessTokenの取得に失敗しました。", ex);
				}
				else
				{
					Debug.WriteLine($"Authorizeに失敗: {resStr}");
					throw new Exception("AccessTokenの取得に失敗しました。", ex);
				}
			}
		}

		private void CheckError(string json)
		{
			var t = JsonConvert.DeserializeObject<JToken>(json);
			if (t["errors"] != null)
			{
				var es = JsonConvert.DeserializeObject<ErrorsObject>(json);
				throw new TwitterException(es, "APIからエラーが返却されました。");
			}
		}

		/// <summary>
		/// Twitter APIを呼び出します
		/// </summary>
		/// <exception cref="InvalidOperationException" />
		/// <exception cref="TwitterException" />
		/// <returns>JSONデータ</returns>
		public async Task<string> Request(HttpMethod method, string url, IDictionary<string, string> parameters = null)
		{
			if (AccessToken == null || AccessTokenSecret == null)
				throw new InvalidOperationException("AccessTokenが見つかりません");

			var res = await Requester.Request(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret, method, url, parameters);
			var resStr = await res.Content.ReadAsStringAsync();
			CheckError(resStr);

			return resStr;
		}

		/// <summary>
		/// Twitter にファイルをアップロードします
		/// </summary>
		/// <exception cref="InvalidOperationException" />
		/// <exception cref="TwitterException" />
		/// <returns>Media ID</returns>
		public async Task<long> UploadMedia(IEnumerable<byte> fileData, string fileName)
		{
			if (AccessToken == null || AccessTokenSecret == null)
				throw new InvalidOperationException("AccessTokenが見つかりません");

			var res = await Requester.UploadMedia(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret, fileData, fileName);
			var resStr = await res.Content.ReadAsStringAsync();
			CheckError(resStr);

			return JsonConvert.DeserializeObject<MediaObject>(resStr).Id;
		}
	}
}
