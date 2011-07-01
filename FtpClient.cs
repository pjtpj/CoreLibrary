using System;
using System.IO;
using System.Net;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace Core
{
	public class FtpClient
	{
#if !WEBREQUESTFTPCLIENT
		public static FTPLib.FTP CreateFTP(Uri ftpUri)
		{
			string   host     = ftpUri.DnsSafeHost;
			string[] userInfo = ftpUri.UserInfo.Split(new Char[] {':'});
			string   username = HttpUtility.UrlDecode(userInfo[0]);
			string   password = userInfo.Length > 1 ? HttpUtility.UrlDecode(userInfo[1]) : "";

			return new FTPLib.FTP(host, username, password);
		}

		public static string GetFolder(Uri uri, bool pathHasFileName)
		{
			UriBuilder builder = new UriBuilder(uri);

			string path = builder.Path;

			if (!pathHasFileName)
				return path;

			// 0123457890
			// /as/df.html
			int lastSlash = path.LastIndexOf('/');
			if (lastSlash != -1)
			{
				path = path.Substring(0, lastSlash);
				return path == "" ? "/" : path;
			}

			return path;
		}

		public static string GetFileName(Uri uri)
		{
			UriBuilder builder = new UriBuilder(uri);

			string path = builder.Path;

			// 0123457890
			// /as/df.html
			int lastSlash = path.LastIndexOf('/');
			if (lastSlash != -1)
				return path.Substring(lastSlash + 1, path.Length - (lastSlash + 1));

			return path;
		}
#endif


		public static List<string> ListFiles(Uri ftpFolderUri)
		{
#if WEBREQUESTFTPCLIENT
			FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(ftpFolderUri);
			listRequest.Method = WebRequestMethods.Ftp.ListDirectory;
			using (FtpWebResponse listResponse = (FtpWebResponse)listRequest.GetResponse())
			{
				using (Stream listStream = listResponse.GetResponseStream())
				{
					using (StreamReader listReader = new StreamReader(listStream))
					{
						string list = listReader.ReadToEnd();
						string[] files = Regex.Split(list, "\r\n|\n");
						List<string> returnFiles = new List<string>();

						foreach (string file in files)
						{
							string fileName = file.Trim();
							if (fileName != "")
							{
								returnFiles.Add(fileName);
							}
						}

						return returnFiles;
					}
				}
			}
#else
			FTPLib.FTP ftp = CreateFTP(ftpFolderUri);

			try
			{
				List<string> returnFiles = new List<string>();

				ftp.ChangeDir(HttpUtility.UrlDecode(GetFolder(ftpFolderUri, false)));

				foreach (string f in ftp.Nlst())
					returnFiles.Add(f);

				return returnFiles;
			}
			finally
			{
				ftp.Disconnect();
			}
#endif
		}

		// BUGBUG: For now, we swallow all errors, but that should be an option
		public static long GetFileLength(Uri ftpFileUri)
		{
#if WEBREQUESTFTPCLIENT
			long fileLength = -1;

			try
			{
				FtpWebRequest sizeRequest = (FtpWebRequest)WebRequest.Create(ftpFileUri);
				sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
				using (FtpWebResponse sizeResponse = (FtpWebResponse)sizeRequest.GetResponse())
				{
					if (sizeResponse.StatusCode == FtpStatusCode.FileStatus)
					{
						string[] temp = sizeResponse.StatusDescription.Split(null, 2);
						long tempLength = long.Parse(temp[1]);
						fileLength = tempLength;
					}

				}
			}
			catch
			{
			}

			return fileLength;
#else
			FTPLib.FTP ftp = null;

			try
			{
				ftp  = CreateFTP(ftpFileUri);

				ftp.ChangeDir(HttpUtility.UrlDecode(GetFolder(ftpFileUri, true)));

				return ftp.GetFileSize(HttpUtility.UrlDecode(GetFileName(ftpFileUri)));
			}
			finally
			{
				if (ftp != null)
					ftp.Disconnect();
			}

#endif
		}
	}
}
