using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Web;

namespace Core
{
	// A simple client that traps redirects so we can pass along cookies 
	public class HttpClient
	{
		public const string IEUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.0.3705)";
		public const string SafariUserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; ru) AppleWebKit/522.11.3 (KHTML, like Gecko) Version/3.0 Safari/522.11.3";
		public CookieContainer Cookies = new CookieContainer();
		public string UserAgent = IEUserAgent;
		public string RedirectUri;
		public HttpRequestCachePolicy CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
		public bool KeepAlive = false;
		public int  Timeout = 100*1000;  // Use system default of 100 seconds (100000 ms)
		public int  MaxUrlAttempts = 3;  // Includes redirects, connection failures and timeouts
		public ICredentials Credentials = null;
		public bool ForceBasicAuthentication = false; // Required by GoogleCheckout - See http://groups.google.com/group/microsoft.public.dotnet.general/browse_thread/thread/c8b05c4a2c650487/7a4e73e3824d75ef%237a4e73e3824d75ef
        public string SOAPActionHeader = "";
		
		public HttpClient()
		{
		}

		// Form actions and redirects are often give to use as relative URLs
		public string FindRequestUri(Uri responseUri, string requestItem)
		{
			if (!requestItem.StartsWith("http:") && !requestItem.StartsWith("https:"))
			{
				Uri temp = new Uri(responseUri, requestItem);
				return temp.ToString();
			}

			return requestItem;
		}

		public HttpWebResponse GetHttpWebResponse(string url)
		{
			while(true)
			{
				int urlAttempts = 0;
				RedirectUri = url;

				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
				webRequest.ServicePoint.ConnectionLimit = 1000;
				webRequest.CachePolicy = CachePolicy;
				webRequest.KeepAlive = KeepAlive;
				webRequest.Timeout = Timeout;
				webRequest.Method = "GET";
				webRequest.Accept = "*/*";
				webRequest.AllowAutoRedirect = false;
				webRequest.UserAgent = UserAgent;
				webRequest.CookieContainer = Cookies;
				if (SOAPActionHeader != "")
					webRequest.Headers.Add("SOAPAction: " + SOAPActionHeader);
                
				if (Credentials != null)
				{
					if (ForceBasicAuthentication)
					{
						NetworkCredential networkCredential = Credentials.GetCredential(webRequest.RequestUri, "Basic");
						string credentials   = string.Format("{0}:{1}", networkCredential.UserName, networkCredential.Password);
						string authorization = string.Format("Basic {0}", Convert.ToBase64String(new System.Text.UTF8Encoding().GetBytes(credentials)));
						webRequest.Headers["Authorization"] = authorization;
					}
					else
					{
						webRequest.Credentials = Credentials;
						webRequest.PreAuthenticate = true;
					}
				}

			RetryUrl:

				HttpWebResponse webResponse = null;

				try
				{
					urlAttempts++;
					webResponse = (HttpWebResponse)webRequest.GetResponse();

					// Look for redirects
					if (webResponse.StatusCode == HttpStatusCode.Found ||
						webResponse.StatusCode == HttpStatusCode.Redirect ||
						webResponse.StatusCode == HttpStatusCode.Moved ||
						webResponse.StatusCode == HttpStatusCode.MovedPermanently)
					{
						// Get new location and continue
						WebHeaderCollection headers = webResponse.Headers;
						url = FindRequestUri(webResponse.ResponseUri, headers["location"]);
						webResponse.Close();
						webResponse = null;
						continue;
					}

					// IE only copies over cookies on successful responses
					foreach (Cookie retCookie in webResponse.Cookies)
					{
						bool cookieFound = false;
						foreach (Cookie oldCookie in Cookies.GetCookies(new Uri(url)))
						{
							// Same cookie, different domain seems like a dumb idea to me...
							if (retCookie.Domain.Equals(oldCookie.Domain) && retCookie.Name.Equals(oldCookie.Name))
							{
								oldCookie.Value = retCookie.Value;
								cookieFound = true;
							}
						}
						if (!cookieFound)
							Cookies.Add(retCookie);
					}
				}
				catch (WebException webex)
				{
					if (webResponse != null)
					{
						webResponse.Close();
						webResponse = null;
					}

					if ((webex.Status == WebExceptionStatus.ConnectFailure || webex.Status == WebExceptionStatus.Timeout) && urlAttempts < MaxUrlAttempts)
						goto RetryUrl;

					throw;
				}

				return webResponse;
			}
		}

		public HttpWebResponse PostHttpWebResponse(string url, string post)
		{
			int urlAttempts = 0;
			RedirectUri = url;

			HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(url);
			webRequest.ServicePoint.ConnectionLimit = 1000;
			webRequest.CachePolicy = CachePolicy;
			webRequest.KeepAlive = KeepAlive;
			webRequest.Timeout = Timeout;
			webRequest.Method = "POST";
			webRequest.Accept = "*/*";
			webRequest.AllowAutoRedirect = false;
			webRequest.UserAgent = UserAgent;
			webRequest.CookieContainer = Cookies;
			if (SOAPActionHeader != "")
				webRequest.Headers.Add("SOAPAction: " + SOAPActionHeader);

			if (Credentials != null)
			{
				if (ForceBasicAuthentication)
				{
					NetworkCredential networkCredential = Credentials.GetCredential(webRequest.RequestUri, "Basic");
					string credentials   = string.Format("{0}:{1}", networkCredential.UserName, networkCredential.Password);
					string authorization = string.Format("Basic {0}", Convert.ToBase64String(new System.Text.UTF8Encoding().GetBytes(credentials)));
					webRequest.Headers["Authorization"] = authorization;
				}
				else
				{
					webRequest.Credentials = Credentials;
					webRequest.PreAuthenticate = true;
				}
			}

			webRequest.ContentType = "application/x-www-form-urlencoded";
			// webRequest.ContentLength = post.Length;
			StreamWriter writer = new StreamWriter(webRequest.GetRequestStream());
			writer.Write(post);
			writer.Close();

		RetryUrl:

			HttpWebResponse webResponse = null;

			try
			{
				urlAttempts++;
				webResponse = (HttpWebResponse)webRequest.GetResponse();

				// Look for redirects
				if (webResponse.StatusCode == HttpStatusCode.Found ||
					webResponse.StatusCode == HttpStatusCode.Redirect ||
					webResponse.StatusCode == HttpStatusCode.Moved ||
					webResponse.StatusCode == HttpStatusCode.MovedPermanently)
				{
					// Get new location and continue
					WebHeaderCollection headers = webResponse.Headers;
					url = FindRequestUri(webResponse.ResponseUri, headers["location"]);
					webResponse.Close();
					webResponse = null;
					return GetHttpWebResponse(url);
				}

				// IE only copies over cookies on successful responses
				foreach (Cookie retCookie in webResponse.Cookies)
				{
					bool cookieFound = false;
					foreach (Cookie oldCookie in Cookies.GetCookies(new Uri(url)))
					{
						// Same cookie, different domain seems like a dumb idea to me...
						if (retCookie.Domain.Equals(oldCookie.Domain) && retCookie.Name.Equals(oldCookie.Name))
						{
							oldCookie.Value = retCookie.Value;
							cookieFound = true;
						}
					}
					if (!cookieFound)
						Cookies.Add(retCookie);
				}
			}
			catch (WebException webex)
			{
				if (webResponse != null)
				{
					webResponse.Close();
					webResponse = null;
				}

				if ((webex.Status == WebExceptionStatus.ConnectFailure || webex.Status == WebExceptionStatus.Timeout) && urlAttempts < MaxUrlAttempts)
					goto RetryUrl;

				throw;
			}


			return webResponse;
		}
	}
}
