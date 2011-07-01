using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Core;

namespace ConvertTest
{
	class Program
	{
		static void Main(string[] args)
		{
			string convertHost     = "convert.t3city.com";
			string convertUsername = "listings";
			string convertPassword = "conv2020list";
			ConvertClient convert = new ConvertClient(convertHost, convertUsername, convertPassword);

			byte[] inputBytes  = File.ReadAllBytes(@"C:\temp\samples-Final\sample.docx");
			byte[] outputBytes = convert.ConvertToPdf(inputBytes, "sample.docx", "sample.pdf");
			File.WriteAllBytes(@"C:\temp\samples-Final\sample.pdf", outputBytes);
		}
	}
}
