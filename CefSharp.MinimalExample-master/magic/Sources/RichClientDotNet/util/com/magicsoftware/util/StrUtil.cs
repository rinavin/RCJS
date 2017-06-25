using System;
using System.Text;
using System.Collections.Generic;

namespace com.magicsoftware.util
{
   public class StrUtil
   {
      private static String _paddingSpaces = null;

      private const String HTML_BACKSLASH = "&#092;";
      private const String HTML_COMMA = "&#044;";
      private const String HTML_HYPHEN = "&#045;";
      public const char STR_2_HTML = (char)(1);
      public const char SEQ_2_HTML = (char)(2);
      public const char HTML_2_STR = (char)(3);
      public const char HTML_2_SEQ = (char)(4);
      public const char SEQ_2_STR = (char)(5);
		
		/// <summary> trim the end of the string</summary>
		public static int mem_trim(String str, int len)
		{
			if (len > 0)
			{
				if (len > str.Length)
					return - 1;
				
				while (len > 0 && str[len - 1] == ' ')
					len--;
			}
			
			return len;
		}

      public static String memmove(String dest, int destCount, String src, int srcCount, int len)
      {
         StringBuilder outVal = new StringBuilder(dest.Length + len);
         if (UtilStrByteMode.isLocaleDefLangJPN() && dest.Length < destCount)
            outVal.Append(new String(' ', destCount));
         else
            outVal.Append(dest.Substring(0, destCount));
         outVal.Append(src.Substring(srcCount, len));
         try
         {
            outVal.Append(dest.Substring(outVal.Length));
         }
         catch (ArgumentOutOfRangeException)
         {
            //doNothing
         }
         return outVal.ToString();
      }

      /// <summary>
      ///   copy part of string into another string, like memcpy of C, but 4 string only
      /// </summary>
      /// <param name = "dest">string</param>
      /// <param name = "destCount">of counter start from in destignation string</param>
      /// <param name = "src">string</param>
      /// <param name = "scrCount">of counter start from in source string</param>
      /// <param name = "count"></param>
      /// <returns> new value of destignation string</returns>
      public static String memcpy(String dest, int destCount, String src, int scrCount, int count)
      {
         var first = new StringBuilder(dest.Substring(0, destCount));
         if (scrCount + count < src.Length)
            first.Append(src.Substring(scrCount, (count) - (scrCount)));
         else
            first.Append(src.Substring(scrCount));
         int size = dest.Length - destCount - count;
         if (size > 0)
            first.Append(dest.Substring(destCount + count));
         return first.ToString();
      }

      public static void memcpy(char[] dest, int destCount, char[] src, int srcCount, int count)
      {
         for (; count > 0 && destCount < dest.Length && srcCount < src.Length; count--)
            dest[destCount++] = src[srcCount++];
      }

      /// <summary>
      ///   insert to string chars n times
      /// </summary>
      /// <param name = "dest">string</param>
      /// <param name = "destCount">of counter start from in destignation string to start insertion of char from</param>
      /// <param name = "inVal">2 insert</param>
      /// <param name = "counter">- number of times to insert the char</param>
      /// <returns> new value of destignation string</returns>
      public static String memset(String dest, int destCount, char inVal, int counter)
      {
         var first = new StringBuilder(dest.Substring(0, destCount));
         while (counter > 0)
         {
            first.Append(inVal);
            counter--;
         }

         if (first.Length < dest.Length)
            first.Append(dest.Substring(first.Length));
         return first.ToString();
      }

      public static void memset(char[] dest, int destCount, char inVal, int counter)
      {
         for (; counter > 0 && destCount < dest.Length; counter--)
            dest[destCount++] = inVal;
      }

      public static String strstr(String str, String substr)
      {
         int from = str.IndexOf(substr);
         if (from < 0)
            return null;
         return str.Substring(from);
      }

      /*******************************/
      /// <summary>
      /// Reverses string values.
      /// </summary>
      /// <param name="text">The StringBuilder object containing the string to be reversed.</param>
      /// <returns>The reversed string contained in a StringBuilder object.</returns>
      public static StringBuilder ReverseString(StringBuilder text)
      {
         char[] tmpChar = text.ToString().ToCharArray();
         System.Array.Reverse(tmpChar);
         return new StringBuilder(new String(tmpChar));
      }

      /// <summary> remove spaces from the right side of string</summary>
      /// <param name="str">the string to trim
      /// </param>
      public static String rtrim(String str)
      {
         return rtrimWithNull(str, false);
      }

      /// <summary> remove spaces and/or Null chars from the right side of string</summary>
      /// <param name="str">the string to trim
      /// </param>
      /// <param name="trimNullChars">Whether to remove NULL characters or not
      /// </param>
      public static String rtrimWithNull(String str, Boolean trimNullChars)
      {
         int idx;

         if (str == null || str.Length == 0)
            return str;

         idx = str.Length - 1;

         if (trimNullChars)
         {
            while (idx >= 0 && (str[idx] == ' ' || str[idx] == '\0'))
               idx--;
         }
         else
         {
            while (idx >= 0 && str[idx] == ' ')
               idx--;
         }
         idx++; // point to the first blank after the last non-blank character
         if (idx < str.Length)
            return str.Substring(0, idx);
         return str;
      }

      /// <summary> remove spaces from the left side of string</summary>
      /// <param name="str">the string to trim
      /// </param>
      public static String ltrim(String str)
      {
         int len = str.Length;
         int i = 0;

         if (str == null || len == 0)
            return str;

         while ((i < len) && (str[i] == ' '))
            i++;

         if (i > 0)
            str = str.Substring(i);

         return str;
      }

      /// <summary>This function for Deleting String from end & start of input
      /// String
      /// </summary>
      /// <param name="str">String , which can include strToDelete spaces on input
      /// </param>
      /// <param name="strToDelete">need delete this String from start/end of str.
      /// </param>
      /// <returns> String without strToDelete on end & start,
      /// or 'null' if Sting hasn't not  characters inside
      /// </returns>
      public static String DeleteStringsFromEnds(String str, String strToDelete)
      {
         if (str.StartsWith(strToDelete))
            str = str.Substring(strToDelete.Length);
         if (str.EndsWith(strToDelete))
            str = str.Substring(0, (str.Length - strToDelete.Length));
         if (str.Length == 0)
            return null;
         return str;
      }

      /// <summary> pad a string with trailing spaces up to the given length</summary>
      /// <param name="str">the string to pad
      /// </param>
      /// <param name="len">the expected length after padding
      /// </param>
      public static String padStr(String str, int len)
      {
         int padLen;
         StringBuilder paddingBuffer = null;

         padLen = len - str.Length;
         if (padLen > 0)
         {
            // if the allocated String is too short then allocate a new one
            // and fill it with spaces
            if (_paddingSpaces == null || _paddingSpaces.Length < padLen)
               _paddingSpaces = new String(' ', padLen);

            // Performance: allocate the final size in advance in order to prevent reallocations
            paddingBuffer = new StringBuilder(len);
            paddingBuffer.Append(str);
            paddingBuffer.Append(_paddingSpaces, 0, padLen);
            str = paddingBuffer.ToString();
         }
         return str;
      }

      /// <summary> this method will serve as a string tokenizer instead of using the c# split method
      /// since there are diffrences btween java tokenizer and c# split
      /// the implimentation given by the conversion tool is not Sufficient
      /// </summary>
      /// <param name="source">- the source string to be converted
      /// </param>
      /// <param name="delim">- the string of delimiters used to split the string (each character in the String is a delimiter
      /// </param>
      /// <returns> array of token according which is the same as string tokenizer in java
      /// </returns>
      public static String[] tokenize(String source, String delim)
      {
         // It is mentioned in the comment that we should not use String.Split()
         // because its behavior is different than Java's tokenizer.
         // So, we were suppose to use our own implementation (the commented code below). 
         // But all these years, we were calling XmlParser.getToken() which was actually 
         // using String.Split(). And we didn't face any problem.
         // So, it seems that we do not have problem in using String.Split().
         // But now, we can improve the performance here...
         // XmlParser.getTokens() was getting a String[] using String.Split().
         // It was then creating a List<String> from this String[] and was returning it to 
         // tokenize().
         // tokenize() was again converting this List<String> back to String[].
         // So why not call String.Split() directly?
         return source.Split(delim.ToCharArray());

         /*
         String [] tokens = null;
			
         char [] delimArry = delim.toCharArray();
			
         //since java discards delimiters from the start and end of the string and c# does not 
         //we need to remove them manually
         //       source = source.TrimEnd(delimArry);
         //     source = source.TrimStart(delimArry);
         source = source.trim();
			
         //now that we have remove starting and ending delimiters we can split
         tokens = source.Split(delimArry);
			
         /*
         * only one problem: if we have two Subsequent delimiters for example : 
         * the delimiter is ';' and the string is: "first;;second;third"
         * then in java String tokenizer will give us only 3 tokens :first,second and third
         * while is c# split wethod will return 4 tokens: first,empty string,second and third
         * we need to deal with that
         */
         /*
         List res  = new List();
         for (int i = 0 ; i < tokens.length; i++)
         {
         if (tokens[i] != "" )
         res.addItem(tokens[i]);
         }
			
         return (String [])(res.getAllItems (String.class));*/
      }

      /// <summary>
      ///   translate from string to hexa dump char by char
      /// </summary>
      /// <param name = "string">to translate it to the byte stream</param>
      /// <param name = "minLength">the minimal length of hexa digits for each char</param>
      /// <returns> the byte stream in form of string</returns>
      public static String stringToHexaDump(String str, int minLength)
      {
         StringBuilder result = new StringBuilder(str.Length * minLength);
         int currInt;
         String hexStr;

         for (int indx = 0; indx < str.Length; indx++)
         {
            currInt = str[indx];
            hexStr = Convert.ToString(currInt, 16);
            while (hexStr.Length < minLength)
               hexStr = '0' + hexStr;
            result.Append(hexStr);
         }

         return result.ToString().ToUpper();
      }

      /// <summary> replace every appearance of 'from' in 'str' with 'to'</summary>
      /// <param name="str">the working base source string </param>
      /// <param name="from">the string to replace </param>
      /// <param name="to">the string use instead 'from' </param>
      /// <returns> modified String </returns>
      public static String searchAndReplace(String str, String from, String to)
      {
         int startSubStr = -1;
         int lastSubStr = 0;

         if ((startSubStr = str.IndexOf(from)) == -1)
            return str;

         StringBuilder tmpBuf = new StringBuilder(str.Length);
         while (startSubStr != -1)
         {
            tmpBuf.Append(str.Substring(lastSubStr, (startSubStr) - (lastSubStr)) + to);
            startSubStr += from.Length;
            lastSubStr = startSubStr;
            startSubStr = str.IndexOf(from, lastSubStr);
         }
         tmpBuf.Append(str.Substring(lastSubStr)); // last part of the String

         return (tmpBuf.ToString());
      }

      /// <summary> replace every appearance of strings of 'from' in 'str' with the according string in 'to'</summary>
      /// <param name="str">the working base source string </param>
      /// <param name="from">the string to replace </param>
      /// <param name="to">the string use instead 'from' </param>
      /// <returns> modified String </returns>
      public static String searchAndReplace(String str, String[] from, String[] to)
      {
         int startSubStr = -1;
         int lastSubStr = 0;

         int SARindex = 0;
         string[] fromCopy = new String[from.Length];
         from.CopyTo(fromCopy, 0);
         if ((startSubStr = indexOf(str, fromCopy, lastSubStr, ref SARindex)) == -1)
            return str;

         StringBuilder tmpBuf = new StringBuilder(str.Length);
         while (startSubStr != -1)
         {
            tmpBuf.Append(str.Substring(lastSubStr, (startSubStr) - (lastSubStr)) + to[SARindex]);
            startSubStr += fromCopy[SARindex].Length;
            lastSubStr = startSubStr;
            startSubStr = indexOf(str, fromCopy, lastSubStr, ref SARindex);
         }
         tmpBuf.Append(str.Substring(lastSubStr)); // last part of the String

         return (tmpBuf.ToString());
      }

      /// <summary> this functions is for use by the searchAndReplace() function -
      /// searches the offset of the strings from the array in the given string
      /// and returns the minimum offset found and sets the index of the found string
      /// to SARindex
      /// </summary>
      /// <param name="str">the string to search in </param>
      /// <param name="strings">an array of strings to search for </param>
      /// <param name="offset">where to start the search </param>
      private static int indexOf(String str, String[] strings, int offset, ref int SARindex)
      {
         int i;
         int minOffset = -1;

         for (i = 0; i < strings.Length; i++)
         {
            if (strings[i] == null)
               continue;
            int resultOffset = str.IndexOf(strings[i], offset);
            if (resultOffset == -1)
               strings[i] = null;
            else if (resultOffset < minOffset || minOffset == -1)
            {
               minOffset = resultOffset;
               SARindex = i;
            }
         }
         if (minOffset > -1)
            return minOffset;
         SARindex = -1;
         return -1;
      }

      /// <summary> replace tokens in user string by vector values </summary>
      /// <param name="userString">- user buffer like "User %d, %d string" 
      /// </param>
      /// <param name="token">- token used in user string - i.e. "%d"
      /// </param>
      /// <param name="occurrence">- number of token where replace will take part (1 for first occurrence)
      /// </param>
      /// <param name="value">- value to be inserted insted of token
      /// </param>
      public static String replaceStringTokens(String userString, String token, int occurrence, String val)
      {
         int tokenLen = token.Length;
         int currPosition = 0;
         String newString = userString;


         if (val != null)
         {
            for (int i = 0; i < occurrence && currPosition != -1; i++)
            {
               currPosition = userString.IndexOf(token, currPosition + (i == 0 ? 0 : tokenLen));
            }

            if (currPosition != -1)
               newString = userString.Substring(0, currPosition) + val + userString.Substring(currPosition + tokenLen, (userString.Length) - (currPosition + tokenLen));
         }

         return newString;
      }

      /// <summary>
      ///   converts special characters in a token to a printable format
      /// </summary>
      /// <param name = "source">a token </param>
      /// <param name = "type">type of conversion: STR_2_HTML, SEQ_2_HTML, HTML_2_SEQ, HTML_2_STR, SEQ_2_STR </param>
      /// <returns> token with converted special characters </returns>
      public static String makePrintableTokens(String source, char type)
      {
         String[] escStr = new[] { "\\", "-", "," };
         String[] escSeq = new[] { "\\\\", "\\-", "\\," };
         String[] escHtm = new[] { HTML_BACKSLASH, HTML_HYPHEN, HTML_COMMA };
         String result;

         switch (type)
         {
            case STR_2_HTML:
               result = StrUtil.searchAndReplace(source, escStr, escHtm);
               break;

            case SEQ_2_HTML:
               result = StrUtil.searchAndReplace(source, escSeq, escHtm);
               break;

            case HTML_2_SEQ:
               result = StrUtil.searchAndReplace(source, escHtm, escSeq);
               break;

            case HTML_2_STR:
               result = StrUtil.searchAndReplace(source, escHtm, escStr);
               break;

            case SEQ_2_STR:
               result = StrUtil.searchAndReplace(source, escSeq, escStr);
               break;

            default:
               result = source;
               break;
         }

         return result;
      }

      /// <summary>
      ///   converts special characters in a tokens collection to a printable format
      /// </summary>
      /// <param name = "source">vector of strings before tokenaizer </param>
      /// <param name = "type">type of conversion: STR_2_HTML, SEQ_2_HTML, HTML_2_SEQ, HTML_2_STR </param>
      public static void makePrintableTokens(List<String> source, char type)
      {
         if (source != null)
         {
            int length = source.Count;
            for (int i = 0; i < length; i++)
            {
               string currElm = source[i];
               source[i] = makePrintableTokens(currElm, type);
            }
         }
      }

      /// <summary>
      ///   change non-printable characters like "new line" and "line feed" to their
      ///   printable representation
      /// </summary>
      /// <param name = "source">is the string with non-printable characters </param>
      /// <returns> the new string where all the non-printable characters are converted </returns>
      public static String makePrintable(String source)
      {
         String[] from = new[] { "\n", "\r", "\'", "\\", "\"", "\x0000" };
         String[] to = new[] { "\\n", "\\r", "\\'", "\\\\", "\\\"", "\\0" };

         string result = searchAndReplace(source, from, to);
         return result;
      }

      /// <summary>
      ///   change non-printable characters like "new line" and "line feed" to their
      ///   printable representation (simplified version for range error message)
      /// </summary>
      /// <param name = "source">is the string with non-printable characters </param>
      /// <returns> the new string where all the non-printable characters are converted </returns>
      public static String makePrintable2(String source)
      {
          String[] from = new[] { "\n", "\r", "\x0000" };
          String[] to = new[] { "\\n", "\\r", "\\0" };

          string result = searchAndReplace(source, from, to);
          return result;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="s"></param>
      /// <param name="len"></param>
      /// <returns></returns>
      public static String ZstringMake(String s, int len)
      {
         len = StrUtil.mem_trim(s, len);
         return (s.Substring(0, len));
      }

      /// <summary>(public)
      /// returns plain text from rtf text
      /// </summary>
      /// <param name="rtfText">refer to the summary</param>
      /// <returns>refer to the summary</returns>
      public static String GetPlainTextfromRtf(String rtfText)
      {
         if (Rtf.isRtf(rtfText))
         {
            Rtf rtf = new Rtf();
            StringBuilder outputTxt = new StringBuilder("");
            rtf.toTxt(rtfText, outputTxt);
            rtfText = outputTxt.ToString();
         }
         return rtfText;
      }

      /// <summary>
      /// Returns true if the string arrays str1 & str2 are equal
      /// </summary>
      /// <param name="str1"></param>
      /// <param name="str2"></param>
      /// <returns></returns>
      public static bool StringsArraysEqual(string[] str1, string[] str2)
      {
         if (str1 == null && str2 == null)
            return true;
         else if (str1 == null || str2 == null)
            return false;
         else
         {
            if (str1.Length != str2.Length)
               return false;

            for (long index = 0; index < str1.Length; index++)
               if (str1[index] != str2[index])
                  return false;

            return true;
         }
      }


      /// <summary>
      /// The code is copied from tsk_open_bnd_wild and SearchAndReplaceWildChars 
      /// The refactoring is not performed for backwards compatibility
      /// The code replaces special charachters :* ? with recieved filler
      /// </summary>
      /// <returns></returns>
      public static String SearchAndReplaceWildChars(string buf, int len, char filler)
      {
         buf = buf.PadRight(len);

         bool escChar = false;
         bool stopSearch = false;
         StringBuilder tmpBuf = new StringBuilder(len);


         for (int i = 0; i < len; i++)
         {
            switch (buf[i])
            {
               case ('\\'):
                  {
                     bool isNextCharWild = true;
                     //If next char is not wild , then copy '\', if this is first char.
                     if ((i + 1 < len) && (buf[i + 1] != '*' && buf[i + 1] != '\\'))
                        isNextCharWild = false;

                     if (escChar || !isNextCharWild)
                        tmpBuf.Append(buf[i]);
                     escChar = !escChar;
                  }
                  break;

               case ('*'):
                  if (escChar)
                     tmpBuf.Append(buf[i]);
                  else
                  {
                     tmpBuf.Append(filler, len - tmpBuf.Length);
                     //if (attr == STORAGE_ATTR_UNICODE)
                     //   WMEMSET((Wchar*)(&tmpBuf[j]), len - j, fill, len - j);
                     //else
                     //   MEMSET((char*)(&tmpBuf[j]), len - j, fill, len - j);
                     stopSearch = true;
                  }
                  escChar = false;
                  break;

               case '?':
                  tmpBuf.Append(filler);
                  escChar = false;
                  break;

               default:
                  tmpBuf.Append(buf[i]);
                  escChar = false;
                  break;
            }
            if (stopSearch)
               break;
         }
         String res = tmpBuf.ToString();
         res = res.TrimEnd('\0');

         return res;
      }
   }
}
