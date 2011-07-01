using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
	[Serializable]
	public class CreditCard
	{
		string _Name = "";
		public string Name { get { return _Name; } set { _Name = value; } }
		string _RegExp = "";
		public string RegExp { get { return _RegExp; } set { _RegExp = value; } }

		public CreditCard() {}

		public CreditCard(string name)
		{
			_Name  = name;
		}

		public CreditCard(string name, string regExp)
		{
			_Name  = name;
			_RegExp = regExp;
		}
	}
}
