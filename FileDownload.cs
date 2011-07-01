using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace Core
{
	public class FileDownload
	{
		public FileDownload()
		{
		}

		public class ProgressEventArgs : EventArgs
		{
			private string _message;
			public string Message
			{
				get { return _message;}
				set { _message = value;}
			}
	
			private long _bytesCompleted;
			public long BytesCompleted
			{
				get { return _bytesCompleted; }
				set { _bytesCompleted = value; }
			}

			private long _bytesRemaining;
			public long BytesRemaining
			{
				get { return _bytesRemaining;}
				set { _bytesRemaining = value;}
			}

			public ProgressEventArgs()
			{
			}
		}

		private ProgressEventArgs _progress = new ProgressEventArgs();
		public ProgressEventArgs Progress
		{
			get { return _progress; }
			set { _progress = value; }
		}

		public delegate void ProgressHandler(object sender, ProgressEventArgs e);
		public ProgressHandler ProgressEvent;

		protected bool _cancel;
		protected WebRequest _webRequest;

		public void Download(Uri uri, string file)
		{
			Download(uri, file, long.MaxValue);
		}

		public void Download(Uri uri, string file, long maxLength)
		{
			// Defeat overly simplistic resume
			if (File.Exists(file))
				File.Delete(file);

#if !WEBREQUESTFTPCLIENT
			if (uri.Scheme == "ftp")
			{
				DownloadFtp(uri, file, maxLength);
				return;
			}
#endif
			_cancel = false;
			_webRequest = WebRequest.Create(uri);
			_progress.Message = string.Format("Connecting to host {0}", uri.Host);
			_progress.BytesCompleted = 0;
			_progress.BytesRemaining = -1;
			if(ProgressEvent != null) ProgressEvent(this, _progress);

			WebResponse webResponse = null;
			FileStream output = null;
			Stream response = null;

			try
			{
				webResponse = _webRequest.GetResponse();
				output      = new FileStream(file, FileMode.Create);

				long contentLength = webResponse.ContentLength;
				if (contentLength < 0 && _webRequest is FtpWebRequest)
				{
					_progress.Message = string.Format("Retreiving file size for '{0}'", uri.AbsolutePath);
					if(ProgressEvent != null) ProgressEvent(this, _progress);

					try
					{

						FtpWebRequest sizeRequest = (FtpWebRequest)WebRequest.Create(_webRequest.RequestUri);
						sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
						using(FtpWebResponse sizeResponse = (FtpWebResponse)sizeRequest.GetResponse())
						{
							if (sizeResponse.StatusCode == FtpStatusCode.FileStatus)
							{
								string[] temp = sizeResponse.StatusDescription.Split(null, 2);
								long tempLength = long.Parse(temp[1]);
								contentLength = tempLength;
							}
						}
					}
					catch
					{
					}
				}

				_progress.Message = string.Format("Downloading '{0}'", uri.AbsolutePath);
				_progress.BytesCompleted = 0;
				_progress.BytesRemaining = contentLength;
				if(ProgressEvent != null) ProgressEvent(this, _progress);

				long totalLength = 0;

				response = webResponse.GetResponseStream();
				byte[] buffer = new byte[1024];
				for (;;)
				{
					if(_cancel)
						throw new WebException("Download canceled");

					if(totalLength >= maxLength)
						break;

					int requestBytes = Math.Min(buffer.Length, (int)Math.Min(int.MaxValue, maxLength));
					int bytesRead = response.Read(buffer, 0, requestBytes);
					if (bytesRead > 0)
					{
						output.Write(buffer, 0, bytesRead);
						totalLength += bytesRead;

						_progress.BytesCompleted += bytesRead;
						if (_progress.BytesRemaining >= bytesRead)
							_progress.BytesRemaining -= bytesRead;
						if(ProgressEvent != null) ProgressEvent(this, _progress);
					}
					else // bytesRead <= 0
					{
						break;
					}
				}
			}
			finally
			{
				if (webResponse != null) webResponse.Close();
				if (output != null) output.Close();
				if (response != null) response.Close();
			}
		}

#if !WEBREQUESTFTPCLIENT
		public void DownloadFtp(Uri uri, string file, long maxLength)
		{
			FTPLib.FTP ftp = FtpClient.CreateFTP(uri);

			_cancel = false;
			_progress.Message = string.Format("Connecting to host {0}", uri.Host);
			_progress.BytesCompleted = 0;
			_progress.BytesRemaining = -1;
			if(ProgressEvent != null) ProgressEvent(this, _progress);

			try
			{
				long contentLength = -1;
				long totalLength   = 0;

				ftp.ChangeDir(HttpUtility.UrlDecode(FtpClient.GetFolder(uri, true)));

				ftp.OpenDownload(HttpUtility.UrlDecode(FtpClient.GetFileName(uri)), file);

				contentLength = ftp.FileSize > 0 ? ftp.FileSize : -1;

				_progress.Message = string.Format("Downloading '{0}'", uri.AbsolutePath);
				_progress.BytesCompleted = 0;
				_progress.BytesRemaining = contentLength;
				if(ProgressEvent != null) ProgressEvent(this, _progress);

				while(ftp.DoDownload() > 0)
				{
					if(_cancel)
						throw new WebException("Download canceled");

					totalLength = ftp.BytesTotal;

					if(totalLength >= maxLength)
						break;

					_progress.BytesCompleted = totalLength;
					_progress.BytesRemaining = contentLength > 0 && contentLength >= totalLength ? contentLength - totalLength : -1;
					if(ProgressEvent != null) ProgressEvent(this, _progress);
				}
			}
			finally
			{
				ftp.Disconnect();
			}
		}
#endif // #if !WEBREQUESTFTPCLIENT

		public void Cancel()
		{
			try
			{
				_cancel = true;
				_webRequest.Abort();
			}
			catch
			{
			}
		}
	}
}
