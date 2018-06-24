using System;
using System.Collections.Generic;

namespace Twi
{
	internal static class InternalUtil
	{
		public static Dictionary<string, string> ParseQueryString(string queryString)
		{
			try
			{
				var parsed = new Dictionary<string, string>();
				foreach (var item in queryString.Split('&'))
				{
					var keyvalue = item.Split('=');
					parsed.Add(keyvalue[0], keyvalue[1]);
				}

				return parsed;
			}
			catch (Exception ex)
			{
				throw new ApplicationException("クエリ文字列のパースに失敗しました", ex);
			}
		}
	}
}
