using System;
using System.Text;
using System.Collections;

namespace com.magicsoftware.util
{
	/// <summary>
	///  This class handles the processing for Rtf Document
	/// </summary>
	/// <author>  Sushant Raut </author>
	public class Rtf
	{
		private long _group;
		private long _cbBin;
		private long _lParam;
		private bool _skipDestIfUnk;
		private bool _outputOnce;
		private bool _processCrlfSpecial;
		private RDS _destState;
		private RIS _internalState;
		private readonly Stack _stack;
		private int _index;
		private int _fontNum;
		private readonly Hashtable _charsetTable = new Hashtable();
		private readonly Hashtable _codePageTable = new Hashtable();      
		
		private const String RTF_PREFIX = "{\\rtf";
		private const String CHAR_PAR = "par";

      private enum RtfChar
      {
         CR = (char)(0x0d),
         LF = (char)(0x0a),
         TAB = (char)(0x09),
         BULLET = (char)(0x95),
         TILDA = (char)(0xa0),
         DASH = (char)(0xad),
         DASH_CHAR = (char)('-'),
         QUOTE = (char)('\''),
         DBLQUOTE = (char)('"'),
         OPENINGBRACE = (char)('{'),
         CLOSINGBRACE = (char)('}'),
         BACKSLASH    = (char)('\\')
      }
		
		public class ErrorRtf
		{
			public static readonly ErrorRtf OK = new ErrorRtf(); /* Everything's fine! */
			public static readonly ErrorRtf STACK_UNDERFLOW = new ErrorRtf(); /* Unmatched '}' */
			public static readonly ErrorRtf STACK_OVERFLOW = new ErrorRtf(); /* Too many '{' -- memory exhausted */
			public static readonly ErrorRtf UNMATCHED_BRACE = new ErrorRtf(); /* RTF ended during an open group. */
			public static readonly ErrorRtf INVALID_HEX = new ErrorRtf(); /* invalid hex character found in data */
			public static readonly ErrorRtf BAD_TABLE = new ErrorRtf(); /* RTF table (sym or prop) invalid */
			public static readonly ErrorRtf ASSERTION = new ErrorRtf(); /* Assertion failure */
			public static readonly ErrorRtf END_OF_FILE = new ErrorRtf(); /* End of file reached while reading */
			public static readonly ErrorRtf BUFFER_TOO_SMALL = new ErrorRtf(); /* Output buffer is too small */
			
			internal ErrorRtf()
			{
			}
		}
		
		/* Rtf Destination State */
		private class RDS
		{
			public static readonly RDS NORM = new RDS();
			public static readonly RDS COLOR = new RDS();
			public static readonly RDS SKIP = new RDS();
			public static readonly RDS NEW = new RDS();
			
			internal RDS()
			{
			}
			
			public static RDS getObj(RDS copyFrom)
			{
				if (copyFrom == NORM)
					return NORM;
				else if (copyFrom == COLOR)
					return COLOR;
				else if (copyFrom == SKIP)
					return SKIP;
				else if (copyFrom == NEW)
					return NEW;
				else
					return null;
			}
		}
		
		/* types of properties */
		private class IPROP
		{
			public static readonly IPROP BOLD = new IPROP(0);
			public static readonly IPROP ITALIC = new IPROP(1);
			public static readonly IPROP UNDERLINE = new IPROP(2);
			public static readonly IPROP FONT = new IPROP(3);
			public static readonly IPROP SIZE = new IPROP(4);
			
			public static readonly IPROP COLOR = new IPROP(5);
			public static readonly IPROP RED = new IPROP(6);
			public static readonly IPROP GREEN = new IPROP(7);
			public static readonly IPROP BLUE = new IPROP(8);
			
			public static readonly IPROP LEFT_IND = new IPROP(9);
			public static readonly IPROP RIGHT_IND = new IPROP(10);
			public static readonly IPROP FIRST_IND = new IPROP(11);
			
			public static readonly IPROP COLS = new IPROP(12);
			public static readonly IPROP PGN_X = new IPROP(13);
			public static readonly IPROP PGN_Y = new IPROP(14);
			
			public static readonly IPROP XA_PAGE = new IPROP(15);
			public static readonly IPROP YA_PAGE = new IPROP(16);
			public static readonly IPROP XA_LEFT = new IPROP(17);
			public static readonly IPROP XA_RIGHT = new IPROP(18);
			public static readonly IPROP YA_TOP = new IPROP(19);
			public static readonly IPROP YA_BOTTOM = new IPROP(20);
			public static readonly IPROP PGN_START = new IPROP(21);
			
			public static readonly IPROP SBK = new IPROP(22);
			public static readonly IPROP PGN_FORMAT = new IPROP(23);
			
			public static readonly IPROP FACING_P = new IPROP(24);
			public static readonly IPROP LANDSCAPE = new IPROP(25);
			
			public static readonly IPROP JUST = new IPROP(26);
			
			public static readonly IPROP PARD = new IPROP(27);
			public static readonly IPROP PLAIN = new IPROP(28);
			public static readonly IPROP SECTD = new IPROP(29);
			
			public static readonly IPROP BULLET = new IPROP(30);
			public static readonly IPROP XA_BULLET = new IPROP(31);
			
			public static readonly IPROP MAX = new IPROP(32);
			
			internal IPROP(int val)
			{
				_val = val;
			}
			
			public int toInt()
			{
				return _val;
			}
			
			private readonly int _val;
		}
		
		private class IDEST
		{
			public static readonly IDEST PICT = new IDEST();
			public static readonly IDEST COLOR = new IDEST();
			public static readonly IDEST SKIP = new IDEST();
			
			internal IDEST()
			{
			}
		}
		
		private class IPFN
		{
			public static readonly IPFN BIN = new IPFN();
			public static readonly IPFN HEX = new IPFN();
			public static readonly IPFN SKIP_DEST = new IPFN();
			public static readonly IPFN BREAK = new IPFN();
			public static readonly IPFN NEW = new IPFN();
			public static readonly IPFN FONT = new IPFN();
			public static readonly IPFN CHARSET = new IPFN();
			public static readonly IPFN UNICODE = new IPFN();
			
			internal IPFN()
			{
			}
		}
		
		private class ACTN
		{
			public static readonly ACTN SPEC = new ACTN();
			public static readonly ACTN BYTE = new ACTN();
			public static readonly ACTN WORD = new ACTN();
			
			internal ACTN()
			{
			}
		}
		
		private class PROPTYPE
		{
			public static readonly PROPTYPE CHP = new PROPTYPE();
			public static readonly PROPTYPE PAP = new PROPTYPE();
			public static readonly PROPTYPE SEP = new PROPTYPE();
			public static readonly PROPTYPE DOP = new PROPTYPE();
			
			internal PROPTYPE()
			{
			}
		}
		
		private class KWD
		{
			public static readonly KWD CHAR = new KWD();
			public static readonly KWD DEST = new KWD();
			public static readonly KWD PROP = new KWD();
			public static readonly KWD SPEC = new KWD();
			
			internal KWD()
			{
			}
		}
		
		private class RIS
		{
			public static readonly RIS NORM = new RIS();
			public static readonly RIS BIN = new RIS();
			public static readonly RIS HEX = new RIS();
			public static readonly RIS UNICODE = new RIS();
			
			internal RIS()
			{
			}
			
			public static RIS getObj(RIS copyFrom)
			{
				if (copyFrom == NORM)
					return NORM;
				else if (copyFrom == BIN)
					return BIN;
				else if (copyFrom == HEX)
					return HEX;
				else
					return null;
			}
		}
		
		private class StackSave
		{
			public StackSave()
			{
			}
			internal RDS rds;
			internal RIS ris;
		}
		
		private sealed class PROP
		{
			internal ACTN actn; /* size of value */
			internal PROPTYPE prop; /* structure containing value */
			
			public PROP(ACTN actn, PROPTYPE prop)
			{
				this.actn = actn;
				this.prop = prop;
			}
		}
		
		private sealed class SYMBOL
		{
			internal String szKeyword; /* RTF keyword */
			internal KWD kwd; /* base action to take */
			internal Object idxInRgprop; /* index into property table if kwd == kwdProp */
			
			/* index into destination table if kwd == kwdDest  */
			/* character to print if kwd == kwdChar            */
			public SYMBOL(String keyWord, KWD kwd, Object idxInRgprop)
			{
				this.szKeyword = keyWord;
				this.kwd = kwd;
				this.idxInRgprop = idxInRgprop;
			}
		}
		
		/* Property descriptions */
		private static readonly PROP[] rgprop = new PROP[]{
			new PROP(ACTN.BYTE, PROPTYPE.CHP), new PROP(ACTN.BYTE, PROPTYPE.CHP), new PROP(ACTN.BYTE, PROPTYPE.CHP), 
			new PROP(ACTN.BYTE, PROPTYPE.CHP), new PROP(ACTN.BYTE, PROPTYPE.CHP), new PROP(ACTN.BYTE, PROPTYPE.CHP), 
			new PROP(ACTN.BYTE, PROPTYPE.CHP), new PROP(ACTN.BYTE, PROPTYPE.CHP), new PROP(ACTN.BYTE, PROPTYPE.CHP), 
			new PROP(ACTN.WORD, PROPTYPE.PAP), new PROP(ACTN.WORD, PROPTYPE.PAP), new PROP(ACTN.WORD, PROPTYPE.PAP), 
			new PROP(ACTN.WORD, PROPTYPE.SEP), new PROP(ACTN.WORD, PROPTYPE.SEP), new PROP(ACTN.WORD, PROPTYPE.SEP), 
			new PROP(ACTN.WORD, PROPTYPE.DOP), new PROP(ACTN.WORD, PROPTYPE.DOP), new PROP(ACTN.WORD, PROPTYPE.DOP), 
			new PROP(ACTN.WORD, PROPTYPE.DOP), new PROP(ACTN.WORD, PROPTYPE.DOP), new PROP(ACTN.WORD, PROPTYPE.DOP), 
			new PROP(ACTN.WORD, PROPTYPE.DOP), new PROP(ACTN.BYTE, PROPTYPE.SEP), new PROP(ACTN.BYTE, PROPTYPE.SEP), 
			new PROP(ACTN.BYTE, PROPTYPE.DOP), new PROP(ACTN.BYTE, PROPTYPE.DOP), new PROP(ACTN.BYTE, PROPTYPE.PAP), 
			new PROP(ACTN.SPEC, PROPTYPE.PAP), new PROP(ACTN.SPEC, PROPTYPE.CHP), new PROP(ACTN.SPEC, PROPTYPE.SEP)};
		
		/* Keyword descriptions */
		private static readonly SYMBOL[] rgsymRtf = new SYMBOL[]{
			new SYMBOL("b", KWD.PROP, IPROP.BOLD),                   new SYMBOL("ul", KWD.PROP, IPROP.UNDERLINE), 
			new SYMBOL("i", KWD.PROP, IPROP.ITALIC),                 new SYMBOL("li", KWD.PROP, IPROP.LEFT_IND), 
			new SYMBOL("ri", KWD.PROP, IPROP.RIGHT_IND),             new SYMBOL("fi", KWD.PROP, IPROP.FIRST_IND), 
			new SYMBOL("cols", KWD.PROP, IPROP.COLS),                new SYMBOL("sbknone", KWD.PROP, IPROP.SBK), 
			new SYMBOL("sbkcol", KWD.PROP, IPROP.SBK),               new SYMBOL("sbkeven", KWD.PROP, IPROP.SBK), 
			new SYMBOL("sbkodd", KWD.PROP, IPROP.SBK),               new SYMBOL("sbkpage", KWD.PROP, IPROP.SBK), 
			new SYMBOL("pgnx", KWD.PROP, IPROP.PGN_X),               new SYMBOL("pgny", KWD.PROP, IPROP.PGN_Y), 
			new SYMBOL("pgndec", KWD.PROP, IPROP.PGN_FORMAT),        new SYMBOL("pgnucrm", KWD.PROP, IPROP.PGN_FORMAT), 
			new SYMBOL("pgnlcrm", KWD.PROP, IPROP.PGN_FORMAT),       new SYMBOL("pgnucltr", KWD.PROP, IPROP.PGN_FORMAT), 
			new SYMBOL("pgnlcltr", KWD.PROP, IPROP.PGN_FORMAT),      new SYMBOL("qc", KWD.PROP, IPROP.JUST), 
			new SYMBOL("ql", KWD.PROP, IPROP.JUST),                  new SYMBOL("qr", KWD.PROP, IPROP.JUST), 
			new SYMBOL("qj", KWD.PROP, IPROP.JUST),                  new SYMBOL("paperw", KWD.PROP, IPROP.XA_PAGE), 
			new SYMBOL("paperh", KWD.PROP, IPROP.YA_PAGE),           new SYMBOL("margl", KWD.PROP, IPROP.XA_LEFT), 
			new SYMBOL("margr", KWD.PROP, IPROP.XA_RIGHT),           new SYMBOL("margt", KWD.PROP, IPROP.YA_TOP), 
			new SYMBOL("margb", KWD.PROP, IPROP.YA_BOTTOM),          new SYMBOL("pgnstart", KWD.PROP, IPROP.PGN_START), 
			new SYMBOL("facingp", KWD.PROP, IPROP.FACING_P),         new SYMBOL("landscape", KWD.PROP, IPROP.LANDSCAPE), 

			new SYMBOL("par", KWD.CHAR, (Object) RtfChar.LF),        new SYMBOL("\x0000x0a", KWD.CHAR, (Object) RtfChar.LF), 
			new SYMBOL("\x0000x0d", KWD.CHAR, (Object) RtfChar.LF),  new SYMBOL("tab", KWD.CHAR, (Object) RtfChar.TAB), 
			new SYMBOL("ldblquote", KWD.CHAR, (Object) RtfChar.DBLQUOTE), new SYMBOL("rdblquote", KWD.CHAR, (Object) RtfChar.DBLQUOTE), 
			new SYMBOL("lquote", KWD.CHAR, (Object) RtfChar.QUOTE),       new SYMBOL("rquote", KWD.CHAR, (Object) RtfChar.QUOTE), 
			new SYMBOL("bullet", KWD.CHAR, (Object) RtfChar.BULLET), new SYMBOL("endash", KWD.CHAR, (Object)RtfChar.DASH_CHAR), 
			new SYMBOL("emdash", KWD.CHAR, (Object) RtfChar.DASH_CHAR), new SYMBOL("~", KWD.CHAR, (Object) RtfChar.TILDA), 
			new SYMBOL("-", KWD.CHAR, (Object) RtfChar.DASH),        new SYMBOL("{", KWD.CHAR, (Object) RtfChar.OPENINGBRACE), 
			new SYMBOL("}", KWD.CHAR, (Object) RtfChar.CLOSINGBRACE),new SYMBOL("\\", KWD.CHAR,(Object) RtfChar.BACKSLASH), 

			new SYMBOL("bin", KWD.SPEC, IPFN.BIN),                   new SYMBOL("*", KWD.SPEC, IPFN.SKIP_DEST), 
			new SYMBOL("'", KWD.SPEC, IPFN.HEX),                     new SYMBOL("f", KWD.SPEC, IPFN.FONT),
			new SYMBOL("fcharset", KWD.SPEC, IPFN.CHARSET),          new SYMBOL("u", KWD.SPEC, IPFN.UNICODE),

			new SYMBOL("author", KWD.DEST, IDEST.SKIP),              new SYMBOL("buptim", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("colortbl", KWD.DEST, IDEST.SKIP),            new SYMBOL("comment", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("creatim", KWD.DEST, IDEST.SKIP),             new SYMBOL("doccomm", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("fonttbl", KWD.DEST, IDEST.SKIP),             new SYMBOL("footer", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("footerf", KWD.DEST, IDEST.SKIP),             new SYMBOL("footerl", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("footerr", KWD.DEST, IDEST.SKIP),             new SYMBOL("footnote", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("ftncn", KWD.DEST, IDEST.SKIP),               new SYMBOL("ftnsep", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("ftnsepc", KWD.DEST, IDEST.SKIP),             new SYMBOL("header", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("headerf", KWD.DEST, IDEST.SKIP),             new SYMBOL("headerl", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("headerr", KWD.DEST, IDEST.SKIP),             new SYMBOL("info", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("keywords", KWD.DEST, IDEST.SKIP),            new SYMBOL("operator", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("pict", KWD.DEST, IDEST.SKIP),                new SYMBOL("printim", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("private1", KWD.DEST, IDEST.SKIP),            new SYMBOL("revtim", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("rxe", KWD.DEST, IDEST.SKIP),                 new SYMBOL("stylesheet", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("subject", KWD.DEST, IDEST.SKIP),             new SYMBOL("tc", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("title", KWD.DEST, IDEST.SKIP),               new SYMBOL("txe", KWD.DEST, IDEST.SKIP), 
			new SYMBOL("xe", KWD.DEST, IDEST.SKIP)};
		
		/// <summary> Constructor
		/// 
		/// </summary>
		public Rtf()
		{
			_stack = new Stack();
			_group = 0;
			_cbBin = 0;
			_lParam = 0;
			_outputOnce = false;
			_skipDestIfUnk = false;
			_processCrlfSpecial = false;
			_destState = RDS.NORM;
			_internalState = RIS.NORM;
			_fontNum = 0;
			if (UtilStrByteMode.isLocaleDefLangDBCS())
				setCodePageTable();
		}
		
		/// <summary> Checks if the blob has a Rtf data or not
		/// 
		/// </summary>
		/// <param name="str">
		/// </param>
		/// <returns>
		/// </returns>
		public static bool isRtf(String str)
		{
			bool isRtf = false;
			
			if (str != null && str.StartsWith(RTF_PREFIX))
				isRtf = true;
			
			return isRtf;
		}
		
		/// <summary> Converts Rtf Text to Plain Text
		/// Step 1: Isolate RTF keywords and send them to ParseKeyword; Push and pop state at the start and end of
		/// RTF groups Send text to ParseChar for further processing.
		/// 
		/// </summary>
		/// <param name="rtfTxt">
		/// </param>
		/// <param name="outputTxt">
		/// </param>
		/// <returns>
		/// </returns>
		public ErrorRtf toTxt(String rtfTxt, StringBuilder outputTxt)
		{
			long cNibble = 2;
			long b = 0;
			int currPos = 0;
			bool skipNewline = false;
			long blobStrLen;
			char blobChar;
			ErrorRtf ec;
			byte[] dbcsBytes = new byte[2];
			bool skipParseChar = false;
			int charset = 0;
			int codePage = 0;
			
			_outputOnce = false;
			_processCrlfSpecial = false;
			blobStrLen = rtfTxt.Length;
			_index = 0;
			_destState = RDS.NORM;
			
			if (rtfTxt == null || blobStrLen == 0 || !isRtf(rtfTxt))
				return ErrorRtf.OK;
			
			while (_index < blobStrLen)
			{
				blobChar = rtfTxt[_index];
				_index++;
				
				if (_group < 0)
					return ErrorRtf.STACK_UNDERFLOW;
				
				/* if we're parsing binary data, handle it directly */
				if (_internalState == RIS.BIN)
				{
					if ((ec = ParseChar(blobChar, outputTxt)) != ErrorRtf.OK)
						return ec;
				}
				else
				{
					switch (blobChar)
					{
						
						case '{': 
							skipNewline = false;
							if ((ec = PushState()) != ErrorRtf.OK)
								return ec;
							break;
						
						
						case '}': 
							skipNewline = true;
							if ((ec = PopState()) != ErrorRtf.OK)
								return ec;
							break;
						
						
						case '\\': 
							skipNewline = false;
							if ((ec = ParseKeyword(rtfTxt, outputTxt)) != ErrorRtf.OK)
								return ec;
							break;
						
						
						case (char)RtfChar.LF:
                  case (char)RtfChar.CR:  /* cr and lf are noise characters... */
							if (_processCrlfSpecial)
							{
								/* Once we reach the 0x0a while ProcessCRLFSpecial_, reset the ProcessCRLFSpecial_ */
                        if (blobChar == (char)RtfChar.LF)
									_processCrlfSpecial = false;
							}
							else
							{
								/*---------------------------------------------------------------*/
								/* skip new lines coming only from the RTF header 1/1/98 - #2390 */
								/*---------------------------------------------------------------*/
								/* Skip the LF (0x0a) if we are not in the ProcessCRLFSpecial_ */
                        if (blobChar == (char)RtfChar.LF || (blobChar == (char)RtfChar.CR && skipNewline && !_outputOnce))
									break;
							}
							goto default;
						
						
						default:
                     if (blobChar != (char)RtfChar.CR)
								skipNewline = false;
							if (_internalState == RIS.NORM)
							{
								if ((ec = ParseChar(blobChar, outputTxt)) != ErrorRtf.OK)
									return ec;
							}
							else if (_internalState == RIS.UNICODE)
							{
								if ((ec = ParseChar((char)_lParam, outputTxt)) != ErrorRtf.OK)
									return ec;
								_internalState = RIS.NORM;
							}
							else
							{
								/* parsing hex data */
								if (_internalState != RIS.HEX)
									return ErrorRtf.ASSERTION;
								b = b << 4;
                                if (Char.IsDigit(blobChar))
									b += (int) blobChar - (int) '0';
								else
								{
                                    if (Char.IsLower(blobChar))
									{
										if (blobChar < 'a' || blobChar > 'f')
											return ErrorRtf.INVALID_HEX;
										b += 10 + (int) blobChar - (int) 'a';
									}
									else
									{
										if (blobChar < 'A' || blobChar > 'F')
											return ErrorRtf.INVALID_HEX;
										b += 10 + (int) blobChar - (int) 'A';
									}
								}
								cNibble--;
								if (cNibble == 0)
								{
									if (UtilStrByteMode.isLocaleDefLangDBCS())
									{
										charset = getCharset(_fontNum);

										if (!skipParseChar && is1stByte(b, charset))    // leading byte of a double-byte character
										{
											dbcsBytes[0] = (byte)b;
											dbcsBytes[1] = (byte)0;
											skipParseChar = true;
										}
										else
										{
											if (skipParseChar && is2ndByte(b, charset))  // trailing byte of a double-byte character
												dbcsBytes[1] = (byte)b;
											else                                         // single-byte character
											{
												dbcsBytes[0] = (byte)b;
												dbcsBytes[1] = (byte)0;
											}

											// convert DBCS to Unicode
											codePage = getCodePage(charset);
											string workStr = Encoding.GetEncoding(codePage).GetString(dbcsBytes, 0, 2);
											char[] workChar = workStr.ToCharArray();
											b = (long)workChar[0];
											skipParseChar = false;
										}
									}

									if (!skipParseChar)
									{
										if ((ec = ParseChar((char) b, outputTxt)) != ErrorRtf.OK)
											return ec;
									}

									cNibble = 2;
									b = 0;
									_internalState = RIS.NORM;
								}
							} /* end else (ris != risNorm) */
							break;
						
					} /* switch */
				} /* else (ris != risBin) */
			} /* while */
			
			if (_group < 0L)
				return ErrorRtf.STACK_UNDERFLOW;
			if (_group > 0L)
				return ErrorRtf.UNMATCHED_BRACE;
			
			/*-------------------------------------------------------------------*/
			/* Eliminate suffix of carrige return + line feed                    */
			/* (Check last characters - just in case format is not the expected) */
			/*-------------------------------------------------------------------*/
			currPos = outputTxt.Length;
         if (currPos >= 3 && (outputTxt[currPos - 3] == (char)RtfChar.CR && outputTxt[currPos - 2] == (char)RtfChar.LF && outputTxt[currPos - 1] == (char)RtfChar.CR || outputTxt[currPos - 3] == (char)RtfChar.LF && outputTxt[currPos - 2] == (char)RtfChar.CR && outputTxt[currPos - 1] == (char)RtfChar.CR))
            outputTxt.Remove(currPos - 3, 3);
			
			return ErrorRtf.OK;
		}
		
		/// <summary> Route the character to the appropriate destination stream.
		/// 
		/// </summary>
		/// <param name="ch">
		/// </param>
		/// <param name="outputTxt">
		/// </param>
		/// <returns>
		/// </returns>
		private ErrorRtf ParseChar(char ch, StringBuilder outputTxt)
		{
			ErrorRtf ret = ErrorRtf.OK;
			
			if (_internalState == RIS.BIN && --_cbBin <= 0)
				_internalState = RIS.NORM;
			
			if (_destState == RDS.SKIP)
			{
			}
			/* Toss this character. */
			else if (_destState == RDS.NORM)
			/* Output a character. Properties are valid at this point. */
				ret = PrintChar(ch, outputTxt);
			else
			{
			} /* handle other destinations.... */
			
			return ret;
		}
		
		/// <summary> Send a character to the output file.
		/// 
		/// </summary>
		/// <param name="ch">
		/// </param>
		/// <param name="outputTxt">
		/// </param>
		/// <returns>
		/// </returns>
		private ErrorRtf PrintChar(char ch, StringBuilder outputTxt)
		{
			/* Allow carrige return + line feed in text, but remove bullet sign */
			/*------------------------------------------------------------------*/
         if ((ch >= ' ' || ch == (char)RtfChar.CR || ch == (char)RtfChar.LF) && ((long)ch != 183))
			// TODO (b7) is a valid JPN character
				outputTxt.Append(ch);
			
			if (ch >= ' ')
				_outputOnce = true;
			
			return ErrorRtf.OK;
		}
		
		/// <summary> Save relevant info on a linked list of SAVE structures.
		/// 
		/// </summary>
		/// <returns>
		/// </returns>
		private ErrorRtf PushState()
		{
			StackSave saveNew = new StackSave();
			
			if (saveNew == null)
				return ErrorRtf.STACK_OVERFLOW;
			
			saveNew.rds = RDS.getObj(_destState);
			saveNew.ris = RIS.getObj(_internalState);
			_internalState = RIS.NORM;
			
			_stack.Push(saveNew);
			_group++;
			
			return ErrorRtf.OK;
		}
		
		/// <summary> Always restore relevant info from the top of the SAVE list.
		/// 
		/// </summary>
		/// <returns>
		/// </returns>
		private ErrorRtf PopState()
		{
            StackSave savedPop = (StackSave)_stack.Pop();
			
			if (savedPop == null)
				return ErrorRtf.STACK_UNDERFLOW;
			
			_destState = RDS.getObj(savedPop.rds);
			_internalState = RIS.getObj(savedPop.ris);
			
			_group--;
			
			return ErrorRtf.OK;
		}
		
		/// <summary> Step 2: get a control word (and its associated value) and call TranslateKeyword to dispatch the control.
		/// 
		/// </summary>
		/// <param name="rtfTxt">
		/// </param>
		/// <param name="outputTxt">
		/// </param>
		/// <returns>
		/// </returns>
		private ErrorRtf ParseKeyword(String rtfTxt, StringBuilder outputTxt)
		{
			char ch;
			bool fNeg = false;
			String szKeyword = "";
			String szParameter = "";
			
			if ((ch = rtfTxt[_index++]) == '\x0000')
				return ErrorRtf.END_OF_FILE;
			
			if (!Char.IsLetter(ch))
			/* a control symbol; no delimiter. */
			{
				szKeyword = String.Concat(szKeyword, System.Convert.ToString(ch));
				
				return TranslateKeyword(szKeyword, outputTxt);
			}
			
			for (; Char.IsLetter(ch); ch = rtfTxt[_index++])
				szKeyword = String.Concat(szKeyword, System.Convert.ToString(ch));
			
			if (ch == '-')
			{
				fNeg = true;
				if ((ch = rtfTxt[_index++]) == '\x0000')
					return ErrorRtf.END_OF_FILE;
			}
			
			if (Char.IsDigit(ch))
			{
				for (; Char.IsDigit(ch); ch = rtfTxt[_index++])
					szParameter = String.Concat(szParameter, System.Convert.ToString(ch));
				
				_lParam = Int64.Parse(szParameter);
				
				if (fNeg)
					_lParam = - _lParam;
			}
			
			if (ch != ' ')
				_index--;
			
			if (String.CompareOrdinal(szKeyword, CHAR_PAR) == 0)
			{
				/* if we get a RTF sequence of \par[0xd][0xa], ie a \par kwd followed   */
				/* immidiately by the CR and LF, then ignore the \par kwd. otherwise    */
				/* we will translate the \par - which translates to a LF (0xa) and also */
				/* the following 0x0d is translated to 0x0a, thus resulting in TWO LF's */
				/* being inserted instead of just one LF. So by skipping [\par] and     */
				/* translating only the [0xd 0xa] will result in only one LF appearing  */
				/* - which is the desired behaviour                                     */
            if (rtfTxt[_index] == (char)RtfChar.CR && rtfTxt[_index + 1] == (char)RtfChar.LF)
					_processCrlfSpecial = true;
			}
			
			if (_processCrlfSpecial)
				return ErrorRtf.OK;
			else
				return TranslateKeyword(szKeyword, outputTxt);
		}
		
		/// <summary> Step 3. Search rgsymRtf for szKeyword and evaluate it appropriately.
		/// 
		/// </summary>
		/// <param name="szKeyword">
		/// </param>
		/// <param name="outputTxt">
		/// </param>
		/// <returns>
		/// </returns>
		private ErrorRtf TranslateKeyword(String szKeyword, StringBuilder outputTxt)
		{
			int isym;
			ErrorRtf ret = ErrorRtf.OK;
			
			/* search for szKeyword in rgsymRtf */
			for (isym = 0; isym < rgsymRtf.Length; isym++)
			{
				if (String.CompareOrdinal(szKeyword, rgsymRtf[isym].szKeyword) == 0)
					break;
			}
			
			if (isym == rgsymRtf.Length)
			/* control word not found */
			{
				if (_skipDestIfUnk)
				/* if this is a new destination */
					_destState = RDS.SKIP; /* skip the destination */
				
				/* else just discard it */
				_skipDestIfUnk = false;
			}
			else
			{
				ret = ErrorRtf.BAD_TABLE;
				/* found it! use kwd and idxInRgprop to determine what to do with it. */
				_skipDestIfUnk = false;
				
				if (rgsymRtf[isym].kwd == KWD.PROP)
					ret = validateProp((IPROP) rgsymRtf[isym].idxInRgprop);
				else if (rgsymRtf[isym].kwd == KWD.CHAR)
					ret = ParseChar(((char)((RtfChar) rgsymRtf[isym].idxInRgprop)), outputTxt);
				else if (rgsymRtf[isym].kwd == KWD.DEST)
					ret = changeDestState();
				else if (rgsymRtf[isym].kwd == KWD.SPEC)
					ret = ParseSpecialKeyword((IPFN) rgsymRtf[isym].idxInRgprop);
			}
			
			return ret;
		}
		
		/// <summary> Validate the property identified by _iprop_ to the value _val_.
		/// previously called Applypropchange
		/// </summary>
		/// <param name="iprop">
		/// </param>
		/// <returns>
		/// </returns>
		private ErrorRtf validateProp(IPROP iprop)
		{
			ErrorRtf ret = ErrorRtf.OK;
			
			if (_destState == RDS.SKIP)
			/* If we're skipping text, */
				return ret; /* don't do anything. */
			
			/* validate prop */
			if (rgprop[iprop.toInt()].prop != PROPTYPE.DOP && rgprop[iprop.toInt()].prop != PROPTYPE.SEP && rgprop[iprop.toInt()].prop != PROPTYPE.PAP && rgprop[iprop.toInt()].prop != PROPTYPE.CHP && rgprop[iprop.toInt()].actn != ACTN.SPEC)
				ret = ErrorRtf.BAD_TABLE;
			
			if (rgprop[iprop.toInt()].actn != ACTN.BYTE && rgprop[iprop.toInt()].actn != ACTN.WORD && rgprop[iprop.toInt()].actn != ACTN.SPEC)
				ret = ErrorRtf.BAD_TABLE;
			
			return ret;
		}
		
		/// <summary> Change to the destination state.
		/// previously called ChangeDest
		/// </summary>
		/// <returns>
		/// </returns>
		private ErrorRtf changeDestState()
		{
			if (_destState == RDS.SKIP)
			/* if we're skipping text, */
				return ErrorRtf.OK; /* don't do anything */
			
			_destState = RDS.SKIP; /* when in doubt, skip it... */
			
			return ErrorRtf.OK;
		}
		
		/// <summary> Evaluate an RTF control that needs special processing.
		/// 
		/// </summary>
		/// <param name="ipfn">
		/// </param>
		/// <returns>
		/// </returns>
		private ErrorRtf ParseSpecialKeyword(IPFN ipfn)
		{
			ErrorRtf ret = ErrorRtf.OK;
			
			if (!UtilStrByteMode.isLocaleDefLangDBCS())
			{
				if (_destState == RDS.SKIP && ipfn != IPFN.BIN)
				/* if we're skipping, and it's not */
					return ret; /* the \bin keyword, ignore it. */
			
				if (ipfn == IPFN.FONT || ipfn == IPFN.CHARSET || ipfn == IPFN.UNICODE)
					return ret;
			}
			else
			{
				if (_destState == RDS.SKIP && ipfn != IPFN.BIN &&
					ipfn != IPFN.FONT && ipfn != IPFN.CHARSET && ipfn != IPFN.UNICODE)
					return ret;
			}
			
			if (ipfn == IPFN.BIN)
			{
				_internalState = RIS.BIN;
				_cbBin = _lParam;
			}
			else if (ipfn == IPFN.SKIP_DEST)
				_skipDestIfUnk = true;
			else if (ipfn == IPFN.HEX)
				_internalState = RIS.HEX;
			else if (ipfn == IPFN.FONT)
				_fontNum = (int)_lParam;
			else if (ipfn == IPFN.CHARSET)
				_charsetTable[_fontNum] = (int)_lParam;
			else if (ipfn == IPFN.UNICODE)
				_internalState = RIS.UNICODE;
			else
				ret = ErrorRtf.BAD_TABLE;
			
			return ret;
		}

      /// <summary> Checks if the byte is within the leading byte range.
      /// 
      /// </summary>
      /// <param name="dbcsBytes">
      /// </param>
      /// <param name="charset">
      /// </param>
      /// <returns>
      /// </returns>
      private static bool is1stByte(long dbcsBytes, int charset)
      {
         bool ret = false;

         if (dbcsBytes > 0xff)
            return ret;
            
         switch (charset)
         {
            case 128:   // SHIFTJIS_CHARSET
               ret = (0x81 <= dbcsBytes && dbcsBytes <= 0x9f) || (0xe0 <= dbcsBytes && dbcsBytes <= 0xfe);
               break;

            case 129:   // HANGUL_CHARSET
            case 134:   // GB2312_CHARSET
            case 136:   // CHINESEBIG5_CHARSET
               ret = (0x81 <= dbcsBytes);
               break;

            default:    // SBCS
               break;
         }

         return ret;
      }

      /// <summary> Checks if the byte is within the trailing byte range.
      /// 
      /// </summary>
      /// <param name="dbcsBytes">
      /// </param>
      /// <param name="charset">
      /// </param>
      /// <returns>
      /// </returns>
      private static bool is2ndByte(long dbcsBytes, long charset)
      {
         bool ret = false;

         if (dbcsBytes > 0xff)
            return ret;

         switch (charset)
         {
            case 128:   // SHIFTJIS_CHARSET
               ret = (dbcsBytes != 0x7f) && (0x40 <= dbcsBytes && dbcsBytes <= 0xfc);
               break;

            case 129:   // HANGUL_CHARSET
            case 134:   // GB2312_CHARSET
            case 136:   // CHINESEBIG5_CHARSET
               ret = (0x40 <= dbcsBytes);
               break;

            default:    // SBCS
               break;
         }

         return ret;
      }

      /// <summary> Create a hashtable of codepage associated with charset.
      /// 
      /// </summary>
      /// <param>
      /// <returns>
      /// </returns>
      private void setCodePageTable()
      {
         // add elements with key (charset) and value (codepage) into the table.
         _codePageTable[0] = 1252;   // ANSI_CHARSET            
         _codePageTable[128] = 932;  // SHIFTJIS_CHARSET
         _codePageTable[129] = 949;  // HANGUL_CHARSET
         _codePageTable[134] = 936;  // GB2312_CHARSET
         _codePageTable[136] = 950;  // CHINESEBIG5_CHARSET
         _codePageTable[161] = 1253; // GREEK_CHARSET
         _codePageTable[162] = 1254; // TURKISH_CHARSET
         _codePageTable[177] = 1255; // HEBREW_CHARSET
         _codePageTable[178] = 1256; // ARABIC _CHARSET
         _codePageTable[186] = 1257; // BALTIC_CHARSET
         _codePageTable[204] = 1251; // RUSSIAN_CHARSET
         _codePageTable[222] = 874;  // THAI_CHARSET
         _codePageTable[238] = 1250; // EASTEUROPE_CHARSET
      }

      /// <summary> Get codepage corresponding to the specified charset.
      /// 
      /// </summary>
      /// <param name="charset">
      /// </param>
      /// <returns>
      /// </returns>
      private int getCodePage(int charset)
      {
         int codePage = 0;

          if (_codePageTable.ContainsKey(charset))
             codePage = (int)_codePageTable[charset];

         return codePage;
      }

      /// <summary> Get charset corresponding to the specified font index.
      /// 
      /// </summary>
      /// <param name="font">
      /// </param>
      /// <returns>
      /// </returns>
      private int getCharset(int font)
      {
         int charset = 0;

          if (_charsetTable.ContainsKey(font))
             charset = (int)_charsetTable[font];

         return charset;
      }
   }
}