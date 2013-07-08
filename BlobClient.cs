using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Web;

namespace Core
{
	public class BlobClient
	{
		public BlobClient(string blobHost, string blobPassword)
		{
			_BlobHost     = blobHost;
			_BlobPassword = blobPassword;
		}

		protected string _BlobHost;
		public string BlobHost { get { return _BlobHost; } set { _BlobHost = value; } }

		protected string _BlobPassword;
		public string BlobPassword { get { return _BlobPassword; } set { _BlobPassword = value; } }

		protected string _Response;
		public string Response { get { return _Response; } set { _Response = value; } }

		protected string _ResponseCode;
		public string ResponseCode { get { return _ResponseCode; } set { _ResponseCode = value; } }

		public bool FileExists(string folder, string file)
		{
			string url = string.Format("http://{0}/post.php?Password={1}&Action=Status&Folder={2}&File={3}", BlobHost, BlobPassword, folder, file);

			HttpClient client = new HttpClient();
			using (HttpWebResponse webResponse = client.GetHttpWebResponse(url))
			{
				using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
				{
					Response = reader.ReadToEnd();
					ResponseCode = Response.Substring(0, 3);
					if (ResponseCode == "200")
						return true;
				}
			}

			return false;
		}

		public void DeleteFile(string folder, string file)
		{
			string url = string.Format("http://{0}/post.php?Password={1}&Action=Delete&Folder={2}&File={3}", BlobHost, BlobPassword, folder, file);

			HttpClient client = new HttpClient();
			using (HttpWebResponse webResponse = client.GetHttpWebResponse(url))
			{
				using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
				{
					Response = reader.ReadToEnd();
					ResponseCode = Response.Substring(0, 3);
					if (ResponseCode != "200")
						throw new ApplicationException(string.Format("Blob delete file failed: {0}", Response));
				}
			}
		}

		public void RenameFile(string folder, string oldFile, string newFile)
		{
			string url = string.Format("http://{0}/post.php?Password={1}&Action=Rename&Folder={2}&File={3}&NewFile={4}", BlobHost, BlobPassword, folder, oldFile, newFile);

			HttpClient client = new HttpClient();
			using (HttpWebResponse webResponse = client.GetHttpWebResponse(url))
			{
				using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
				{
					Response = reader.ReadToEnd();
					ResponseCode = Response.Substring(0, 3);
					if (ResponseCode != "200")
						throw new ApplicationException(string.Format("Blob rename file failed: {0}", Response));
				}
			}
		}

		protected static string _szBoundary    = "SEPARATORSTRINGTEZTECHDOTCOM1";
		protected static string _szBoundary2   = "\r\n--SEPARATORSTRINGTEZTECHDOTCOM1\r\n";
		protected static string _szBoundary3   = "\r\n--SEPARATORSTRINGTEZTECHDOTCOM1--";
		protected static string _szFileSizeHdr = "Content-Disposition: form-data; name=\"MAX_FILE_SIZE\"\r\n\r\n";
		protected static string _szFileHdrFmt  = "Content-Disposition: form-data; name=\"userfile\"; filename=\"{0}\"\r\nContent-Type: application/octet-stream\r\n\r\n";

		public void UploadBlob(string folder, string file, byte[] bytes)
		{
			string url = string.Format("http://{0}/post.php?Password={1}&Action=Update&Folder={2}&File={3}", BlobHost, BlobPassword, folder, file);

			// Calculate upload data size

			string szFileSizeData = string.Format("{0}", bytes.Length + 50000);
			string szFileHdr      = string.Format(_szFileHdrFmt, file);

			ASCIIEncoding ascii = new ASCIIEncoding(); // At this time, file names must be ascii
			List<byte> header = new List<byte>(); 
			List<byte> footer = new List<byte>(); 

			header.AddRange(ascii.GetBytes(_szBoundary2));	  // MAX_FILE_SIZE field
			header.AddRange(ascii.GetBytes(_szFileSizeHdr));
			header.AddRange(ascii.GetBytes(szFileSizeData));
			header.AddRange(ascii.GetBytes(_szBoundary2));	  // userfile field
			header.AddRange(ascii.GetBytes(szFileHdr));

			footer.AddRange(ascii.GetBytes(_szBoundary3));
			
			int cbContent = header.Count + bytes.Length + footer.Count;

			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
			webRequest.Method        = "POST";
			webRequest.ContentType   = string.Format("multipart/form-data; boundary={0}\r\n", _szBoundary);
			webRequest.ContentLength = cbContent;

			using (Stream request = webRequest.GetRequestStream())
			{
				request.Write(header.ToArray(), 0, header.Count);
				request.Write(bytes, 0, bytes.Length);
				request.Write(footer.ToArray(), 0, footer.Count);
			}

			using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
			{
				using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
				{
					Response = reader.ReadToEnd();
					ResponseCode = Response.Substring(0, 3);
					if (ResponseCode != "200")
						throw new ApplicationException(string.Format("Blob upload file failed: {0}", Response));
				}
			}
		}

		public Set<string> ListFiles(string folder)
		{
			string url = string.Format("http://{0}/post.php?Password={1}&Action=ListFiles&Folder={2}", BlobHost, BlobPassword, folder);

			Set<string> files = new Set<string>();

			HttpClient client = new HttpClient();
			// See also:
			// php.ini max_execution_time = 240
			// IIS FastCGI PHP Request Timeout = 240
			client.Timeout = 1000 * 1000;
			using (HttpWebResponse webResponse = client.GetHttpWebResponse(url))
			{
				using (StreamReader reader = new StreamReader(webResponse.GetResponseStream()))
				{
					while (true)
					{
						string line = reader.ReadLine();
						if (line == null)
							break;
						if (line.StartsWith(string.Format("200 {0} files listed", files.Count)))
							break;

						files.Add(line);
					}
				}
			}

			return files;
		}
	}
}
