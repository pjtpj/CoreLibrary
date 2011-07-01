using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace Core
{
	/// <summary>
	/// Summary description for ExceptionMessage
	/// </summary>
	public class ExceptionMessage
	{
		public static string GetMessage(Exception ex)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(ExceptionToStringPrivate(ex, true));

			try
			{
				sb.Append(GetASPSettings());
			}
			catch(Exception e)
			{
				sb.AppendLine(e.Message);
			}

			return sb.ToString();

		}

		const string _strViewstateKey    = "__VIEWSTATE";
		const string _strRootException   = "System.Web.HttpUnhandledException";
		const string _strRootWsException = "System.Web.Services.Protocols.SoapException";
		const string _strDefaultLogName  = "UnhandledExceptionLog.txt";

		protected static string ExceptionToStringPrivate(Exception ex, bool blnIncludeSysInfo)
		{
			StringBuilder sb = new StringBuilder();

			if (ex.InnerException != null)
			{
				// sometimes the original exception is wrapped in a more relevant outer exception
				// the detail exception is the "inner" exception
				// see http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnbda/html/exceptdotnet.asp

				// don't return the outer root ASP exception; it is redundant.
				if (ex.GetType().ToString() == _strRootException || ex.GetType().ToString() == _strRootWsException)
				{
					return ExceptionToStringPrivate(ex.InnerException, true);
				}
				else
				{
					sb.AppendLine(ExceptionToStringPrivate(ex.InnerException, false));
					sb.AppendLine("(Outer Exception)");
				}
			}

			// get general system and app information
			// we only really want to do this on the outermost exception in the stack
			if (blnIncludeSysInfo)
			{
				try
				{
					sb.Append(SysInfoToString(false));
				}
				catch(Exception e)
				{
					sb.AppendLine(e.Message);
				}

				try
				{
					sb.Append(AssemblyInfoToString(ex));
				}
				catch(Exception e)
				{
					sb.AppendLine(e.Message);
				}

				sb.AppendLine();
			}

			// get exception-specific information

			sb.Append("Exception Type:        ");
			try 
			{ 
				sb.AppendLine(ex.GetType().FullName);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Exception Message:     ");
			try 
			{ 
				sb.AppendLine(ex.Message);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Exception Source:      ");
			try 
			{ 
				sb.AppendLine(ex.Source);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Exception Target Site: ");
			try 
			{ 
				sb.AppendLine(ex.TargetSite.Name);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			try 
			{ 
				sb.AppendLine(EnhancedStackTrace(ex, ""));
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			return sb.ToString();
		}

		protected static string GetASPSettings()
		{
			StringBuilder sb = new StringBuilder();

			const string strSuppressKeyPattern = "^ALL_HTTP|^ALL_RAW|VSDEBUGGER";

			sb.Append("---- ASP.NET Collections ----");
			sb.AppendLine();
			sb.AppendLine();
			sb.Append(HttpVarsToString(HttpContext.Current.Request.QueryString, "QueryString", false, ""));
			sb.Append(HttpVarsToString(HttpContext.Current.Request.Form, "Form", false, ""));
			sb.Append(HttpVarsToString(HttpContext.Current.Request.Cookies));
			sb.Append(HttpVarsToString(HttpContext.Current.Session));
			sb.Append(HttpVarsToString(HttpContext.Current.Cache));
			sb.Append(HttpVarsToString(HttpContext.Current.Application));
			sb.Append(HttpVarsToString(HttpContext.Current.Request.ServerVariables, "ServerVariables", true, strSuppressKeyPattern));

			NameValueCollection extraVars = new NameValueCollection();
			extraVars["OriginalAbsoluteUri"] = HttpContext.Current.Items.Contains("OriginalAbsoluteUri") ? (string)HttpContext.Current.Items["OriginalAbsoluteUri"] : "";
			extraVars["Referrer"]            = HttpContext.Current.Request.UrlReferrer != null ? HttpContext.Current.Request.UrlReferrer.AbsoluteUri : "";
			sb.Append(HttpVarsToString(extraVars, "URLs", false, ""));


			return sb.ToString();
		}

		protected static void AppendLine(StringBuilder sb, string key, object val)
		{
			string strValue;

			if (val == null)
			{
				strValue = "(Nothing)";
			}
			else
			{
				try
				{
					strValue = val.ToString();
				}
				catch
				{
					strValue = "(" + val.GetType().ToString() + ")";
				}
			}

			AppendLine(sb, key, strValue);
		}

		protected static void AppendLine(StringBuilder sb, string key, string val)
		{
			sb.AppendLine(string.Format("    {0, -30}{1}", key, val));
		}

		protected static string HttpVarsToString(HttpCookieCollection c)
		{
			if (c.Count == 0)
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append("Cookies");
			sb.AppendLine();
			sb.AppendLine();

			foreach (string strKey in c)
				AppendLine(sb, strKey, c[strKey].Value);

			sb.AppendLine();
			return sb.ToString();
		}

		protected static string HttpVarsToString(HttpApplicationState c)
		{
			if (c.Count == 0)
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append("Application");
			sb.AppendLine();
			sb.AppendLine();

			foreach (string strKey in c)
				AppendLine(sb, strKey, c[strKey]);

			sb.AppendLine();
			return sb.ToString();
		}

		protected static string HttpVarsToString(System.Web.Caching.Cache c)
		{
			if (c.Count == 0)
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append("Cache");
			sb.AppendLine();
			sb.AppendLine();

			foreach (DictionaryEntry de in c)
				AppendLine(sb, Convert.ToString(de.Key), de.Value);

			sb.AppendLine();
			return sb.ToString();
		}

		protected static string HttpVarsToString(System.Web.SessionState.HttpSessionState c)
		{
			if (c == null || c.Count == 0)
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append("Session");
			sb.AppendLine();
			sb.AppendLine();

			foreach (string strKey in c)
				AppendLine(sb, strKey, c[strKey]);

			sb.AppendLine();
			return sb.ToString();
		}

		protected static string HttpVarsToString(System.Collections.Specialized.NameValueCollection nvc, string strTitle,
			bool blnSuppressEmpty, string strSuppressKeyPattern)
		{
			if (!nvc.HasKeys())
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append(strTitle);
			sb.AppendLine();
			sb.AppendLine();

			foreach (string strKey in nvc)
			{
				bool blnDisplay = true;

				if (blnSuppressEmpty)
					blnDisplay = nvc[strKey] != "";

	#if SAVE_VIEWSTATE
				if (strKey == _strViewstateKey)
				{
					_strViewstate = nvc[strKey];
					blnDisplay = false;
				}
	#endif

				if (blnDisplay && strSuppressKeyPattern != "")
					blnDisplay = !Regex.IsMatch(strKey, strSuppressKeyPattern);

				if (blnDisplay)
					AppendLine(sb, strKey, nvc[strKey]);
			}

			sb.AppendLine();
			return sb.ToString();
		}

		protected static string SysInfoToString(bool blnIncludeStackTrace)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("Date and Time:         ");
			sb.Append(DateTime.Now);
			sb.AppendLine();

			sb.Append("Machine Name:          ");
			try
			{
				sb.AppendLine(Environment.MachineName);
			}
			catch(Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Process User:          ");
			sb.Append(ProcessIdentity());
			sb.AppendLine();

			sb.Append("Remote User:           ");
			sb.Append(HttpContext.Current.Request.ServerVariables["REMOTE_USER"]);
			sb.AppendLine();

			sb.Append("Remote Address:        ");
			sb.Append(HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
			sb.AppendLine();

			sb.Append("Remote Host:           ");
			sb.Append(HttpContext.Current.Request.ServerVariables["REMOTE_HOST"]);
			sb.AppendLine();

			sb.Append("URL:                   ");
			sb.Append(WebCurrentUrl());
			sb.AppendLine();
			sb.AppendLine();

			sb.Append("NET Runtime version:   ");
			sb.Append(System.Environment.Version.ToString());
			sb.AppendLine();

			sb.Append("Application Domain:    ");
			try
			{
				sb.AppendLine(System.AppDomain.CurrentDomain.FriendlyName);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			if (blnIncludeStackTrace)
				sb.Append(EnhancedStackTrace());

			return sb.ToString();
		}

		protected static string CurrentWindowsIdentity()
		{
			try
			{
				return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
			}
			catch
			{
				return "";
			}
		}

		protected static string CurrentEnvironmentIdentity()
		{
			try
			{
				return System.Environment.UserDomainName + "\\" + System.Environment.UserName;
			}
			catch
			{
				return "";
			}
		}

		protected static string ProcessIdentity()
		{
			string strTemp = CurrentWindowsIdentity();
			return strTemp == "" ? CurrentEnvironmentIdentity() : strTemp;
		}

		protected static string WebCurrentUrl()
		{
			NameValueCollection sv = HttpContext.Current.Request.ServerVariables;

			string strUrl = "http://" + sv["server_name"];
			if (sv["server_port"] != "80")
				strUrl += ":" + sv["server_port"];

			strUrl += sv["url"];
			if (sv["query_string"] != null && sv["query_string"].Length > 0)
				strUrl += "?" + sv["query_string"];

			return strUrl;
		}

		protected static string AssemblyInfoToString(Exception ex)
		{
			// ex.source USUALLY contains the name of the assembly that generated the exception
			// at least, according to the MSDN documentation..
			System.Reflection.Assembly a = GetAssemblyFromName(ex.Source);
			return a == null ? AllAssemblyDetailsToString() : AssemblyDetailsToString(a);
		}

		protected static Assembly GetAssemblyFromName(string strAssemblyName)
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (a.GetName().Name == strAssemblyName)
					return a;
			}
			
			return null;
		}

		protected static string AllAssemblyDetailsToString()
		{
			StringBuilder sb = new StringBuilder();
			const string strLineFormat = "    {0, -30} {1, -15} {2}";

			sb.AppendLine();
			sb.Append(string.Format(strLineFormat, "Assembly", "Version", "BuildDate"));
			sb.AppendLine();
			sb.Append(string.Format(strLineFormat, "--------", "-------", "---------"));
			sb.AppendLine();

			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				NameValueCollection nvc = AssemblyAttribs(a);
				// assemblies without versions are weird (dynamic?)
				if (nvc["Version"] != "0.0.0.0")
				{
					sb.Append(string.Format(strLineFormat, System.IO.Path.GetFileName(nvc["CodeBase"]), nvc["Version"], nvc["BuildDate"]));
					sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		protected static string AssemblyDetailsToString(Assembly a)
		{
			StringBuilder sb = new StringBuilder();
			NameValueCollection nvc = AssemblyAttribs(a);

			sb.Append("Assembly Codebase:     ");
			try
			{
				sb.AppendLine(nvc["CodeBase"]);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Assembly Full Name:    ");
			try
			{
				sb.AppendLine(nvc["FullName"]);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Assembly Version:      ");
			try
			{
				sb.AppendLine(nvc["Version"]);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			sb.Append("Assembly Build Date:   ");
			try
			{
				sb.AppendLine(nvc["BuildDate"]);
			}
			catch (Exception e)
			{
				sb.AppendLine(e.Message);
			}

			return sb.ToString();
		}

		// <summary>
		// returns string name / string value pair of all attribs for the specified assembly
		// </summary>
		// <remarks>
		// note that Assembly* values are pulled from AssemblyInfo file in project folder
		//
		// Trademark       = AssemblyTrademark string
		// Debuggable      = True
		// GUID            = 7FDF68D5-8C6F-44C9-B391-117B5AFB5467
		// CLSCompliant    = True
		// Product         = AssemblyProduct string
		// Copyright       = AssemblyCopyright string
		// Company         = AssemblyCompany string
		// Description     = AssemblyDescription string
		// Title           = AssemblyTitle string
		// </remarks>
		protected static NameValueCollection AssemblyAttribs(Assembly a)
		{
			NameValueCollection nvc = new NameValueCollection();

			foreach (object attrib in a.GetCustomAttributes(false))
			{
				string Name = attrib.GetType().ToString();
				string Value = "";

				switch (Name)
				{
					case "System.Diagnostics.DebuggableAttribute":
						Name = "Debuggable";
						Value = ((DebuggableAttribute)attrib).IsJITTrackingEnabled.ToString();
						break;
					case "System.CLSCompliantAttribute":
						Name = "CLSCompliant";
						Value = ((CLSCompliantAttribute)attrib).IsCompliant.ToString();
						break;
					case "System.Runtime.InteropServices.GuidAttribute":
						Name = "GUID";
						Value = ((System.Runtime.InteropServices.GuidAttribute)attrib).Value.ToString();
						break;
					case "System.Reflection.AssemblyTrademarkAttribute":
						Name = "Trademark";
						Value = ((AssemblyTrademarkAttribute)attrib).Trademark.ToString();
						break;
					case "System.Reflection.AssemblyProductAttribute":
						Name = "Product";
						Value = ((AssemblyProductAttribute)attrib).Product.ToString();
						break;
					case "System.Reflection.AssemblyCopyrightAttribute":
						Name = "Copyright";
						Value = ((AssemblyCopyrightAttribute)attrib).Copyright.ToString();
						break;
					case "System.Reflection.AssemblyCompanyAttribute":
						Name = "Company";
						Value = ((AssemblyCompanyAttribute)attrib).Company.ToString();
						break;
					case "System.Reflection.AssemblyTitleAttribute":
						Name = "Title";
						Value = ((AssemblyTitleAttribute)attrib).Title.ToString();
						break;
					case "System.Reflection.AssemblyDescriptionAttribute":
						Name = "Description";
						Value = ((AssemblyDescriptionAttribute)attrib).Description.ToString();
						break;
					default:
						// Console.WriteLine(Name)
						break;
				}

				if (Value != "")
					if (nvc[Name] == "")
						nvc.Add(Name, Value);
			}

			// add some extra values that are not in the AssemblyInfo, but nice to have
			nvc.Add("CodeBase", a.CodeBase.Replace("file:///", ""));
			nvc.Add("BuildDate", AssemblyBuildDate(a, false).ToString());
			nvc.Add("Version", a.GetName().Version.ToString());
			nvc.Add("FullName", a.FullName);

			return nvc;
		}

		protected static DateTime AssemblyBuildDate(Assembly a, bool blnForceFileDate)
		{
			System.Version v = a.GetName().Version;
			DateTime dt;

			if (blnForceFileDate)
				dt = AssemblyLastWriteTime(a);
			else
			{
				dt = (new DateTime(2000, 1, 1)).AddDays(v.Build). AddSeconds(v.Revision * 2);
				if (TimeZone.IsDaylightSavingTime(dt, TimeZone.CurrentTimeZone.GetDaylightChanges(dt.Year)))
					dt = dt.AddHours(1);
				if (dt > DateTime.Now || v.Build < 730 || v.Revision == 0)
					dt = AssemblyLastWriteTime(a);
			}

			return dt;
		}

		protected static DateTime AssemblyLastWriteTime(Assembly a)
		{
			try
			{
				return System.IO.File.GetLastWriteTime(a.Location);
			}
			catch
			{
				return DateTime.MaxValue;
			}
		}

		protected static string EnhancedStackTrace(Exception ex, string strSkipClassName)
		{
			StackTrace    st = ex != null ? new StackTrace(ex, true) : new StackTrace(true);
			StringBuilder sb = new StringBuilder();

			sb.AppendLine();
			sb.Append("---- Stack Trace ----");
			sb.AppendLine();
			sb.AppendLine();

			for (int intFrame = 0; intFrame < st.FrameCount; intFrame++)
			{
				StackFrame sf = st.GetFrame(intFrame);
				MemberInfo mi = sf.GetMethod();

				if (strSkipClassName == "" || mi.DeclaringType.Name.IndexOf(strSkipClassName) == -1)
					sb.Append(StackFrameToString(sf));
			}

			return sb.ToString();
		}

		protected static string EnhancedStackTrace()
		{
			return EnhancedStackTrace(null, "ASPUnhandledException");
		}

		protected static string StackFrameToString(StackFrame sf)
		{
			StringBuilder sb = new StringBuilder();
			MemberInfo    mi = sf.GetMethod();

			// build method name
			sb.Append("   ");
			sb.Append(mi.DeclaringType.Namespace);
			sb.Append(".");
			sb.Append(mi.DeclaringType.Name);
			sb.Append(".");
			sb.Append(mi.Name);

			// build method params
			sb.Append("(");
			int intParam = 0;
			foreach (ParameterInfo param in sf.GetMethod().GetParameters())
			{
				intParam += 1;
				if (intParam > 1) sb.Append(", ");
				sb.Append(param.ParameterType.Name);
				sb.Append(" ");
				sb.Append(param.Name);
			}
			sb.Append(")");
			sb.AppendLine();

			// if source code is available, append location info
			sb.Append("       ");
			if (sf.GetFileName() == null || sf.GetFileName().Length == 0)
			{
				sb.Append("(unknown file)");
				// native code offset is always available
				sb.Append(": N ");
				sb.Append(string.Format("{0:#00000}", sf.GetNativeOffset()));
			}
			else
			{
				sb.Append(System.IO.Path.GetFileName(sf.GetFileName()));
				sb.Append(": line ");
				sb.Append(string.Format("{0:#0000}", sf.GetFileLineNumber()));
				sb.Append(", col ");
				sb.Append(string.Format("{0:#00}", sf.GetFileColumnNumber()));
				// if IL is available, append IL location info
				if (sf.GetILOffset() != StackFrame.OFFSET_UNKNOWN)
				{
					sb.Append(", IL ");
					sb.Append(String.Format("{0:#0000}", sf.GetILOffset()));
				}
			}
			sb.AppendLine();

			return sb.ToString();
		}
	}
}