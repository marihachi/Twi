﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Twi
{
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
		/// 
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

		/// <summary>
		/// 認可をリクエストし、認可URLを取得します
		/// </summary>
		public async Task<Uri> GetAuthorizationUrl()
		{
			var res = await Requester.Request(ConsumerKey, ConsumerSecret, null, null, HttpMethod.Get, RequestTokenUrl);
			var parsed = Utility.ParseQueryString(await res.Content.ReadAsStringAsync());
			RequestToken = parsed["oauth_token"];
			RequestTokenSecret = parsed["oauth_token_secret"];

			return new Uri($"{AuthorizationUrl}?oauth_token={RequestToken}");
		}

		/// <summary>
		/// PINコードを渡してアクセストークンをリクエストします
		/// </summary>
		public async Task Authorize(string pin)
		{
			if (RequestToken == null || RequestTokenSecret == null)
				throw new InvalidOperationException("この操作は GetAuthorizationUrl メソッドを呼び出すまでは無効です");

			var parameters = new Dictionary<string, string> { { "oauth_verifier", pin } };
			var res = await Requester.Request(ConsumerKey, ConsumerSecret, RequestToken, RequestTokenSecret, HttpMethod.Post, AccessTokenUrl, parameters);
			var parsed = Utility.ParseQueryString(await res.Content.ReadAsStringAsync());

			AccessToken = parsed["oauth_token"];
			AccessTokenSecret = parsed["oauth_token_secret"];
		}

		/// <summary>
		/// Twitter APIを呼び出します
		/// </summary>
		public async Task<string> Request(HttpMethod method, string url, IDictionary<string, string> parameters = null)
		{
			if (AccessToken == null || AccessTokenSecret == null)
				throw new InvalidOperationException("アクセストークンが見つかりません");

			var res = await Requester.Request(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret, method, url, parameters);
			return await res.Content.ReadAsStringAsync();
		}
	}
}
