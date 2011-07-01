using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Core
{
	public class JsonParser
	{
		public Dictionary<string, object> ParseObject(StringReader reader)
		{
			_reader = reader;

			Dictionary<string, object> jsonObject = new Dictionary<string,object>();

			ReadJsonToken('{');

			while (true)
			{
				string name = ParseString();
				ReadJsonToken(':');
				object val  = ParseValue();

				jsonObject[name] = val;

				if(PeekJsonToken() == '}')
					break;

				ReadJsonToken(',');
			}

			ReadJsonToken('}');

			return jsonObject;
		}

		public List<object> ParseArray(StringReader reader)
		{
			_reader = reader;

			List<object> jsonArray = new List<object>();

			ReadJsonToken('[');

			if (PeekJsonToken() != ']')
			{
				while (true)
				{
					object val = ParseValue();

					jsonArray.Add(val);

					if (PeekJsonToken() == ']')
						break;

					ReadJsonToken(',');
				}
			}

			ReadJsonToken(']');

			return jsonArray;
		}

		protected StringReader _reader;

		protected void SkipWhiteSpace()
		{
			while(Char.IsWhiteSpace((char)_reader.Peek())) 
				_reader.Read();
		}

		protected int PeekJsonToken()
		{
			SkipWhiteSpace();
			return _reader.Peek();
		}

		protected void ReadJsonToken(char token)
		{
			SkipWhiteSpace();
			int tokenRead = _reader.Read();
			if(tokenRead == -1)
				throw new ApplicationException(string.Format("Input Error: Unexpected end of data when Json parser expected token '{0}'", token));
			if(tokenRead != token)
				throw new ApplicationException(string.Format("Input Error: Json parser expected token '{0}', but read '{1}'", token, tokenRead));
		}

		protected object ParseValue()
		{
			switch (PeekJsonToken())
			{
				case '"':
					return ParseString();
                case '-':
                case '+':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
					return ParseNumber();
				case '{':
					return ParseObject(_reader);
				case '[':
					return ParseArray(_reader);
                case 'f':
                case 'F':
                case 't':
                case 'T':
					return ParseBool();
                case 'n':
                case 'N':
					return ParseNull();
				default:
					throw new ApplicationException(string.Format("Input Error: Json parser encountered unexpected input: {0}", _reader.ReadLine()));
			}
		}

        protected string ParseString() 
		{
			ReadJsonToken('"');

            StringBuilder sb = new StringBuilder();

			while(true)
			{
				int ch = _reader.Read();
				if(ch == -1)
					throw new ApplicationException("Input Error: Unexpected end of data in Json parser while parsing string token");
				if(ch == '"')
					break;
				if(ch != '\\')
					sb.Append((char)ch);
				else
				{
					ch = _reader.Read();
					switch (ch)
					{
						case -1:
							throw new ApplicationException("Input Error: Unexpected end of data in Json parser while parsing string escape token");
						case '"':
							sb.Append('"');
							break;
						case '/':
							sb.Append('/');
							break;
						case '\\':
							sb.Append('\\');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'u':
							sb.Append(ParseUnicode());
							break;
						default:
							throw new ApplicationException(string.Format("Input Error: Json parser encountered unexpected character '{0}' while parsing string escape token", ch));
					}
				}
			}

			return sb.ToString();
        }

        protected char ParseUnicode() 
		{
            int ch1 = _reader.Read();
            int ch2 = _reader.Read();
            int ch3 = _reader.Read();
            int ch4 = _reader.Read();

			return (char)(FromHex(ch1) << 12 | FromHex(ch2) << 8 | FromHex(ch3) << 4 | FromHex((ch4)));
        }

        protected int FromHex(int ch) 
		{
			if(ch == -1)
				throw new ApplicationException("Input Error: Unexpected end of data in Json parser while parsing string escape hex token");

            if(ch >= '0' && ch <= '9')
                return ch - '0';
            if(ch >= 'a' && ch <= 'f')
                return (ch - 'a') + 10;
            if(ch >= 'A' && ch <= 'F')
                return (ch - 'A') + 10;

			throw new ApplicationException(string.Format("Input Error: Json parser encountered unexpected character '{0}' while parsing string escape hex token", ch));
        }

        protected double ParseNumber() 
		{
            StringBuilder sb = new StringBuilder();

			int ch;
            while((ch = _reader.Peek()) != -1 && IsNumberComponent(ch))
                sb.Append((char)_reader.Read());

			double val = double.MaxValue;
            if(double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                return val;

			throw new ApplicationException(string.Format("Input Error: Json parser could not parse the input text as a number: {0}", sb.ToString()));
        }

        protected bool IsNumberComponent(int ch) 
		{
            return (ch >= '0' && ch <= '9') || ch == '-' || ch == '+' || ch == '.' || ch == 'e' || ch == 'E';
        }

		protected bool ParseBool()
		{
            StringBuilder sb = new StringBuilder();

			int ch;
            while((ch = _reader.Peek()) != -1 && Char.IsLetter((char)ch))
                sb.Append((char)_reader.Read());

			string val = sb.ToString().ToLower();
			if(val == "true")
				return true;
			if(val == "false")
				return false;

			throw new ApplicationException(string.Format("Input Error: Json parser could not parse the input text as a bool: {0}", sb.ToString()));
		}

		protected object ParseNull()
		{
            StringBuilder sb = new StringBuilder();

			int ch;
            while((ch = _reader.Peek()) != -1 && Char.IsLetter((char)ch))
                sb.Append((char)_reader.Read());

			string val = sb.ToString().ToLower();
			if(val == "null")
				return null;

			throw new ApplicationException(string.Format("Input Error: Json parser could not parse the input text as a null: {0}", sb.ToString()));
		}
	}
}
