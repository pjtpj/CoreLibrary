using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
	public class Template
	{
        public string OpenMarker   = "<!-- ";
        public string CloseMarker  = " -->";
		public string TemplateText = "";

		public Template(Template template)
		{
			TemplateText = template.TemplateText;
		}

		public Template(string templateText)
		{
			TemplateText = templateText;
		}

		public void Insert(int startIndex, string value)
		{
			TemplateText = TemplateText.Insert(startIndex, value);
		}

		public void Replace(string oldValue, string newValue)
		{
			TemplateText = TemplateText.Replace(oldValue, newValue);
		}

		public void ReplaceOne(string oldValue, string newValue)
		{
			int iOldValue = TemplateText.IndexOf(oldValue);
			if (iOldValue < 0)
				return;

			TemplateText = TemplateText.Remove(iOldValue, oldValue.Length);
			TemplateText = TemplateText.Insert(iOldValue, newValue);
		}

		public void Replace(string marker, Template newValue)
		{
			ExtractMarkedRegion(marker, true);
			if (newValue.MarkedRegionExists(marker))
			{
				Template newValue2 = newValue.ExtractMarkedRegion(marker, false);
				Replace(marker, newValue2.TemplateText);
			}
		}

		public void AppendMarkedRegion(string marker, string content)
		{
            string beginRow = string.Format("{0}BEGIN {1}{2}", OpenMarker, marker, CloseMarker);
            string endRow   = string.Format("{0}END {1}{2}",   OpenMarker, marker, CloseMarker);

			TemplateText = TemplateText + string.Format("{0}\r\n{1}\r\n{2}", beginRow, content, endRow);
		}

		public bool MarkedRegionExists(string marker)
		{
            string beginRow = string.Format("{0}BEGIN {1}{2}", OpenMarker, marker, CloseMarker);
			return TemplateText.IndexOf(beginRow) >= 0;
		}

		public Template ExtractMarkedRegion(string marker, bool leaveMarker)
		{
			return ExtractMarkedRegion(marker, leaveMarker, false);
		}

		public Template ExtractMarkedRegion(string marker, bool leaveMarker, bool leaveExtractedMarkers)
		{
            string beginRow = string.Format("{0}BEGIN {1}{2}", OpenMarker, marker, CloseMarker);
            string endRow   = string.Format("{0}END {1}{2}",   OpenMarker, marker, CloseMarker);

			string rowTemplate = "";

			while(true)
			{
				int pos1 = TemplateText.IndexOf(beginRow);
				if (pos1 == -1)
					break;

				int pos2 = TemplateText.IndexOf(endRow);
				if (pos2 == -1)
					break;

				rowTemplate += TemplateText.Substring(pos1, pos2 - pos1 + endRow.Length);

				if (leaveMarker)
					TemplateText = TemplateText.Replace(rowTemplate, marker);
				else
					TemplateText = TemplateText.Replace(rowTemplate, "");

				break; // Only one marked region at a time for now
			}

			if (!leaveExtractedMarkers)
			{
				rowTemplate = rowTemplate.Replace(beginRow, "");
				rowTemplate = rowTemplate.Replace(endRow, "");
			}

			return new Template(rowTemplate);
		}
	}
}
