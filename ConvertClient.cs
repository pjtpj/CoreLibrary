using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Web;

namespace Core
{
	public class ConvertClient
	{
		public ConvertClient(string convertHost, string username, string password)
		{
			_ConvertHost = convertHost;
			_Username    = username;
			_Password    = password;
		}

		protected string _ConvertHost;
		public string ConvertHost { get { return _ConvertHost; } set { _ConvertHost = value; } }

		protected string _Username;
		public string Username { get { return _Username; } set { _Username = value; } }

		protected string _Password;
		public string Password { get { return _Password; } set { _Password = value; } }

		protected string _Response;
		public string Response { get { return _Response; } set { _Response = value; } }

		protected int _MaxConvertAttempts = 2;
		public int MaxConvertAttempts { get { return _MaxConvertAttempts; } set { _MaxConvertAttempts = value; } }

		protected string _ResponseCode;
		public string ResponseCode { get { return _ResponseCode; } set { _ResponseCode = value; } }

		protected static string _szBoundary    = "SEPARATORSTRINGTEZTECHDOTCOM1";
		protected static string _szBoundary2   = "\r\n--SEPARATORSTRINGTEZTECHDOTCOM1\r\n";
		protected static string _szBoundary3   = "\r\n--SEPARATORSTRINGTEZTECHDOTCOM1--";
		protected static string _szFileSizeHdr = "Content-Disposition: form-data; name=\"MAX_FILE_SIZE\"\r\n\r\n";
		protected static string _szFileHdrFmt  = "Content-Disposition: form-data; name=\"InputFile\"; filename=\"{0}\"\r\nContent-Type: application/octet-stream\r\n\r\n";

		public byte[] ConvertToPdf(byte[] inputFileBytes, string inputFileName, string outputFileName)
		{
			string url = string.Format("http://{0}/{1}?Username={2}&Password={3}", ConvertHost, outputFileName, Username, Password);

			// Calculate upload data size

			string szFileSizeData = string.Format("{0}", inputFileBytes.Length + 50000);
			string szFileHdr      = string.Format(_szFileHdrFmt, inputFileName);

			ASCIIEncoding ascii = new ASCIIEncoding(); // At this time, file names must be ascii
			List<byte> header = new List<byte>(); 
			List<byte> footer = new List<byte>(); 

			header.AddRange(ascii.GetBytes(_szBoundary2));	  // MAX_FILE_SIZE field
			header.AddRange(ascii.GetBytes(_szFileSizeHdr));
			header.AddRange(ascii.GetBytes(szFileSizeData));
			header.AddRange(ascii.GetBytes(_szBoundary2));	  // userfile field
			header.AddRange(ascii.GetBytes(szFileHdr));

			footer.AddRange(ascii.GetBytes(_szBoundary3));
			
			int cbContent = header.Count + inputFileBytes.Length + footer.Count;

			int attempts = 0;

			while (true)
			{
				try
				{
					attempts++;

					HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
					webRequest.Method        = "POST";
					webRequest.ContentType   = string.Format("multipart/form-data; boundary={0}\r\n", _szBoundary);
					webRequest.ContentLength = cbContent;

					using (Stream request = webRequest.GetRequestStream())
					{
						request.Write(header.ToArray(), 0, header.Count);
						request.Write(inputFileBytes, 0, inputFileBytes.Length);
						request.Write(footer.ToArray(), 0, footer.Count);
					}

					using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
					{
						using (BinaryReader reader = new BinaryReader(webResponse.GetResponseStream()))
						{
							byte[] pdfBytes = reader.ReadBytes((int)webResponse.ContentLength);
							return pdfBytes;
						}
					}
				}
				catch
				{
					if (attempts >= MaxConvertAttempts)
						throw;
				}
			}
		}
	}
}
