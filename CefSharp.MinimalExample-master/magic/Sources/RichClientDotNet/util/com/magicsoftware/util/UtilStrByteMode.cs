using System;
using System.Text;
using CultureInfo = System.Globalization.CultureInfo;

namespace com.magicsoftware.util
{
   /// <summary>JPN: DBCS support
   /// Utility Class for String
   /// In this class, considering DBCS, strings are counted by the number
   /// of bytes, not the number of characters.
   /// </summary>
   /// <author>  Toshiro Nakayoshi (MSJ) </author>
   public static class UtilStrByteMode
   {
      private static readonly bool _bLocaleDefLangJPN;
      private static readonly bool _bLocaleDefLangCHN;
      private static readonly bool _bLocaleDefLangKOR;

      /// <summary> static block to initialize static members </summary>
      static UtilStrByteMode()
      {
         String strLocaleDefLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

         _bLocaleDefLangJPN = (strLocaleDefLang == "ja");
         _bLocaleDefLangCHN = (strLocaleDefLang == "zh");
         _bLocaleDefLangKOR = (strLocaleDefLang == "ko");
      }

      /// <summary> Checks the environment whether it is running on DBCS environment
      /// Returns true if the language code for the current default Locale is
      /// DBCS language (in non-Unicode encoding).
      /// 
      /// </summary>
      /// <returns> true if DBCS, or false if SBCS
      /// </returns>
      public static bool isLocaleDefLangDBCS()
      {
         return (_bLocaleDefLangJPN || _bLocaleDefLangCHN || _bLocaleDefLangKOR);
      }

      /// <summary> Checks whether the language code for the current default Locale
      /// is JAPANESE.
      /// 
      /// </summary>
      /// <returns> true if JAPANESE, or false if not
      /// </returns>
      public static bool isLocaleDefLangJPN()
      {
         return _bLocaleDefLangJPN;
      }

      /// <summary> Checks whether the language code for the current default Locale
      /// is KOREAN.
      /// 
      /// </summary>
      /// <returns> true if KOREAN, or false if not
      /// </returns>
      public static bool isLocaleDefLangKOR()
      {
         return _bLocaleDefLangKOR;
      }

      public static bool isKoreanCharacter(char c)
      {
         return
            (0xAC00 <= c && c <= 0xD7A3) ||
            (0x1100 <= c && c <= 0x11FF) ||
            (0x3130 <= c && c <= 0x318F) ||
            (0xA960 <= c && c <= 0xA97F) ||
            (0xD7B0 <= c && c <= 0xD7FF);
      }

      /// <summary> Length of String
      /// Returns the number of bytes (in default encoding).
      /// 
      /// </summary>
      /// <param name="strVal:">string (in Unicode)
      /// </param>
      /// <returns> the number of bytes (in default encoding)
      /// 
      /// Example: lenB("abXYc")
      /// Where 'a', 'b' and  'c' are SBCS, and 'X' and 'Y' are DBCS, it returns
      /// 7.
      /// </returns>
      public static int lenB(String strVal)
      {
         // convert to byte[] by default-encoding
         return Encoding.Default.GetByteCount(strVal);
      }

      /// <summary> Substring of String
      /// Extracts a specified number of characters (a substring) from a string.
      /// If a DBCS character is divided in two, it will be replace to a space.
      /// 
      /// </summary>
      /// <param name="strVal:">string (in Unicode)
      /// </param>
      /// <param name="ofs:">starting position (byte) of the substring
      /// </param>
      /// <param name="len:">number of bytes to be extracted (i.e. bytes of substring)
      /// </param>
      /// <returns> substring
      /// 
      /// Example: midB("abXYc", 2, 4)
      /// Where 'a', 'b' and  'c' are SBCS, and 'X' and 'Y' are DBCS, it returns
      /// "bX ".
      /// </returns>
      public static String midB(String strVal, int ofs, int len)
      {
         int LenMax;
         int intByteLength;
         StringBuilder strbufAddingBuf;
         String strOneChar = null;
         String strRet = null;
         int intIndex; // index of strVal
         int intValidMinIndex = -1; // param #1 of substring
         int intValidMaxIndex = -1; // param #2 of substring
         bool bHeadSpace = false;   // flag: need to add space
         bool bEndSpace = false;    // flag: need to add space

         // check and modify ofs & len
         if (len <= 0)
            return "";

         if (ofs <= 0)
         {
            ofs = 0;
            intValidMinIndex = 0;
            bHeadSpace = false;
         }

         intByteLength = lenB(strVal);

         if (intByteLength < ofs)
            return "";

         LenMax = intByteLength - ofs;
         if (LenMax < len)
            len = LenMax;

         // set MinIndex and MaxIndex for substring
         intByteLength = 0;

         for (intIndex = 0; intIndex < strVal.Length; intIndex++)
         {
            strOneChar = strVal.Substring(intIndex, 1);
            intByteLength += Encoding.Default.GetByteCount(strOneChar);

            if (intValidMinIndex == -1)
            {
               if (intByteLength == ofs)
               {
                  intValidMinIndex = intIndex + 1;
                  bHeadSpace = false;
               }
               else if (intByteLength > ofs)
               {
                  intValidMinIndex = intIndex + 1;
                  bHeadSpace = true;
               }
            }
            if (intValidMaxIndex == -1)
            {
               if (intByteLength == ofs + len)
               {
                  intValidMaxIndex = intIndex;
                  bEndSpace = false;
                  break;
               }
               else if (intByteLength > ofs + len)
               {
                  intValidMaxIndex = intIndex - 1;
                  bEndSpace = true;
                  break;
               }
            }
         }

         // prepare for substring
         strbufAddingBuf = null;
         strbufAddingBuf = new StringBuilder(len);

         // execute Mid
         if (bHeadSpace)
            strbufAddingBuf.Append(' ');

         if (intValidMinIndex <= intValidMaxIndex)
            strbufAddingBuf.Append(strVal.Substring(intValidMinIndex, (intValidMaxIndex + 1) - (intValidMinIndex)));

         if (bEndSpace)
            strbufAddingBuf.Append(' ');

         strRet = strbufAddingBuf.ToString();
         strbufAddingBuf = null;

         return strRet;
      }

      /// <summary> Get Characters from Left of String
      /// Returns a specified number of bytes from the left side of a string.
      /// If a DBCS character is divided in two, it will be replace to a space.
      /// 
      /// </summary>
      /// <param name="strVal:">string (in Unicode)
      /// </param>
      /// <param name="len:">number of bytes to be retured
      /// </param>
      /// <returns> output string
      /// 
      /// Example: leftB("abXYc", 4)
      /// Where 'a', 'b' and  'c' are SBCS, and 'X' and 'Y' are DBCS, it returns
      /// "abX".
      /// </returns>
      public static String leftB(String strVal, int len)
      {
         return midB(strVal, 0, len);
      }

      /// <summary> Get Characters from Right of String
      /// Returns a specified number of bytes from the right side of a string.
      /// If a DBCS character is divided in two, it will be replace to a space.
      /// 
      /// </summary>
      /// <param name="strVal:">string (in Unicode)
      /// </param>
      /// <param name="len:">number of bytes to be retured
      /// </param>
      /// <returns> output string
      /// 
      /// Example: rightB("abXYc", 4)
      /// Where 'a', 'b' and  'c' are SBCS, and 'X' and 'Y' are DBCS, it returns
      /// " Yc".
      /// </returns>
      public static String rightB(String strVal, int len)
      {
         int byteFldsValLen;
         int ofs;

         byteFldsValLen = lenB(strVal);

         if (len < 0)
            len = 0;

         ofs = byteFldsValLen - len;

         if (ofs < 0)
            ofs = 0;

         return midB(strVal, ofs, len);
      }

      /// <summary> Insert String
      /// Inserts one string into another.
      /// If a DBCS character is divided in two, it will be replace to a space.
      /// 
      /// </summary>
      /// <param name="strTarget:">A string that represents the target string.
      /// </param>
      /// <param name="strSource:">A string that represents the source string.
      /// </param>
      /// <param name="ofs:">A number that represents the starting position (byte) in
      /// the target.
      /// </param>
      /// <param name="len:">A number that represents the number of bytes from the
      /// source that will be inserted into the target.
      /// </param>
      /// <returns> output string
      /// 
      /// Example: insB("abXYc", "de", 4, 1)
      /// Where 'a', 'b', 'c', 'd' and 'e' are SBCS, and 'X' and 'Y' are DBCS,
      /// it returns "ab d Yc".
      /// </returns>
      public static String insB(String strTarget, String strSource, int ofs, int len)
      {
         int intTargetLenB;
         int intSourceLenB;
         int intAddSpaceLen;
         StringBuilder strbufRetVal;
         String strRet = null;

         if (ofs < 0)
            ofs = 0;
         else if (ofs >= 1)
            ofs--;

         if (len < 0)
            len = 0;

         intTargetLenB = lenB(strTarget);
         intSourceLenB = lenB(strSource);

         strbufRetVal = new StringBuilder(ofs + len);

         strbufRetVal.Append(leftB(strTarget, ofs));

         // add blanks between strTarget and strSource
         intAddSpaceLen = ofs - intTargetLenB;
         for (; intAddSpaceLen > 0; intAddSpaceLen--)
            strbufRetVal.Append(' ');

         strbufRetVal.Append(leftB(strSource, len));

         // add blanks to the end
         intAddSpaceLen = len - intSourceLenB;
         for (; intAddSpaceLen > 0; intAddSpaceLen--)
            strbufRetVal.Append(' ');

         strbufRetVal.Append(rightB(strTarget, intTargetLenB - ofs));

         strRet = strbufRetVal.ToString();
         strbufRetVal = null;

         return strRet;
      }

      /// <summary> Delete Characters
      /// Delete characters from a string.
      /// If a DBCS character is divided in two, it will be replace to a space.
      /// 
      /// </summary>
      /// <param name="strVal:">string (in Unicode)
      /// </param>
      /// <param name="ofs:">The position (byte) of the first character to be deleted.
      /// </param>
      /// <param name="len:">The number of characters to be deleted, beginning with
      /// position start and proceeding rightward.
      /// </param>
      /// <returns> output string
      /// 
      /// Example: delB("abXYc", 2, 4)
      /// Where 'a', 'b' and  'c' are SBCS, and 'X' and 'Y' are DBCS, it returns
      /// "a c".
      /// </returns>
      public static String delB(String strVal, int ofs, int len)
      {
         int intValLenB;
         int intRightSideLenB;
         StringBuilder strbufRetVal;
         String strRet = null;

         if (ofs < 0)
            ofs = 0;
         else if (ofs >= 1)
            ofs--;

         intValLenB = lenB(strVal);
         if (ofs + len > intValLenB)
            len = intValLenB - ofs;
         if (len <= 0)
            return strVal;

         intRightSideLenB = intValLenB - ofs - len;
         if (intRightSideLenB < 0)
            return strVal;

         strbufRetVal = new StringBuilder(ofs + intRightSideLenB);

         strbufRetVal.Append(leftB(strVal, ofs));
         strbufRetVal.Append(rightB(strVal, intRightSideLenB));

         strRet = strbufRetVal.ToString();
         strbufRetVal = null;

         return strRet;
      }

      /// <summary> In-String Search
      /// Returns a number that represents the first position (byte) of a
      /// substring within a string.
      /// 
      /// </summary>
      /// <param name="strTarget:">string (in Unicode)
      /// </param>
      /// <param name="strSearch:">string which will be the search argument in string
      /// </param>
      /// <returns> number, 0 if not found
      /// 
      /// Example: instrB("abXYc", "Y")
      /// Where 'a', 'b' and  'c' are SBCS, and 'X' and 'Y' are DBCS, it returns
      /// 5.
      /// </returns>
      public static int instrB(String strTarget, String strSearch)
      {
         int ofs = 0;

         if (strSearch.Length == 0)
            // nothing to look for
            return 0;

         ofs = strTarget.IndexOf(strSearch);

         if (ofs < 0)
            // not found
            return 0;

         return lenB(strTarget.Substring(0, ofs)) + 1;
      }

      /// <summary> Replace Substring Within a String (Byte Mode)
      /// Replaces a substring within a string with another substring.
      /// If a DBCS character is divided in two, it will be replace to a space.
      /// 
      /// </summary>
      /// <param name="strTarget:">target string where the replacement will take place.
      /// </param>
      /// <param name="strOrigin:">string that provides the substring to be copied to
      /// target.
      /// </param>
      /// <param name="ofs:">the first position (byte) in the target string that will
      /// receive the substring from origin.
      /// </param>
      /// <param name="len:">the number of bytes that will be moved from origin to
      /// target, starting from the leftmost character of origin.
      /// </param>
      /// <returns> string containing modified target string
      /// 
      /// Example: repB("abXYc", "de", 4, 2)
      /// Where 'a', 'b', 'c', 'd' and 'e' are SBCS, and 'X' and 'Y' are DBCS,
      /// it returns "ab de c".
      /// </returns>
      public static String repB(String strTarget, String strOrigin, int ofs, int len)
      {
         StringBuilder strbufAddingBuf = new StringBuilder();
         int intRightLen;
         int intAddSpaceLen;

         if (ofs < 0)
            ofs = 0;
         else if (ofs >= 1)
            ofs--;

         if (len < 0)
            len = 0;

         strbufAddingBuf.Append(leftB(strTarget, ofs));

         // add blanks between strTarget and strOrigin
         intAddSpaceLen = ofs - lenB(strTarget);
         for (; intAddSpaceLen > 0; intAddSpaceLen--)
            strbufAddingBuf.Append(' ');

         strbufAddingBuf.Append(leftB(strOrigin, len));

         intRightLen = lenB(strTarget) - (ofs + len);
         if (intRightLen > 0)
            strbufAddingBuf.Append(rightB(strTarget, intRightLen));

         // add blanks to the end
         intAddSpaceLen = len - lenB(strOrigin);
         for (; intAddSpaceLen > 0; intAddSpaceLen--)
            strbufAddingBuf.Append(' ');

         return strbufAddingBuf.ToString();
      }

      /// <summary> Replace Substring Within a String (Character Mode)
      /// Replaces a substring within a string with another substring.
      /// 
      /// </summary>
      /// <param name="strTarget:">target string where the replacement will take place.
      /// </param>
      /// <param name="strOrigin:">string that provides the substring to be copied to
      /// target.
      /// </param>
      /// <param name="ofs:">the first position (character) in the target string that
      /// will receive the substring from origin.
      /// </param>
      /// <param name="len:">the number of characters that will be moved from origin
      /// to target, starting from the leftmost character of origin.
      /// </param>
      /// <returns> string containing modified target string
      /// 
      /// Example: repB("abXYc", "de", 4, 2)
      /// Whether each character is SBCS or DBCS, it returns "abXde".
      /// </returns>
      public static String repC(String strTarget, String strOrigin, int ofs, int len)
      {
         StringBuilder strbufAddingBuf = new StringBuilder();
         int intRightLen;
         int intAddSpaceLen;

         if (ofs < 0)
            ofs = 0;
         else if (ofs >= 1)
            ofs--;

         if (len < 0)
            len = 0;

         strbufAddingBuf.Append(strTarget.Substring(0, ofs));

         // add blanks between strTarget and strOrigin
         intAddSpaceLen = ofs - strTarget.Length;
         for (; intAddSpaceLen > 0; intAddSpaceLen--)
            strbufAddingBuf.Append(' ');

         strbufAddingBuf.Append(strOrigin.Substring(0, len));

         intRightLen = strTarget.Length - (ofs + len);
         if (intRightLen > 0)
            strbufAddingBuf.Append(strTarget.Substring(ofs + len));

         // add blanks to the end
         intAddSpaceLen = len - strOrigin.Length;
         for (; intAddSpaceLen > 0; intAddSpaceLen--)
            strbufAddingBuf.Append(' ');

         return strbufAddingBuf.ToString();
      }

      /// <summary> Checks whether a character is 1 byte (halfwidth) or not (fullwidth)
      /// Returns true if the character is represented by 1 byte in non-Unicode
      /// encoding.
      /// </summary>
      /// <param name="letter:">a character to be checked.
      /// </param>
      /// <returns> true if the character is halfwidth (SBCS), or false if it is
      /// fullwidth (DBCS).
      /// </returns>
      public static bool isHalfWidth(char letter)
      {
         if (0x0020 <= letter && letter <= 0x007e)
            return true;

         char[] szLetter = new char[] { letter };
         int len = lenB(new String(szLetter));
         if (len == 1)
            return true;
         else
            return false;
      }

      /// <summary> Checks whether a character is halfwidth digit letter
      /// Do not use "Character.isDigit" which cannot distinguish between
      /// halfwidth digit letter(SBCS) and fullwidth difit letter(DBCS).
      /// </summary>
      /// <param name="letter:">a character to be checked.
      /// </param>
      /// <returns> true if the character is halfwidth digit letter, or
      /// false if it is DBCS or not digit letter.
      /// </returns>
      public static bool isDigit(char letter)
      {
         if (0x0030 <= letter && letter <= 0x0039)
            return true;
         else
            return false;
      }

      /// <summary>Checks whether a character is one of those supported for # Alpha Mask</summary>
      /// <param name="letter:">a character to be checked.
      /// </param>
      /// <returns> true if the character is halfwidth digit letter, or
      /// false if it is DBCS or not digit letter.
      /// </returns>
      public static bool asNumeric(char letter)
      {
         switch (letter)
         {
            case ',':
            case '-':
            case '.':
            case '*':
            case '+':
            case '/':
               return true;

            default:
               return false;
         }
      }

      /// <summary> Converts a position for the 1st string (Source) to a position for
      /// the 2nd string (Dest).
      /// If a double byte character exists in the strings, the position for the
      /// Source could be different from the position for the Dest.
      /// (DBCS Support)
      /// 
      /// </summary>
      /// <param name="strSource:">Source string
      /// </param>
      /// <param name="strDest:">Dest string
      /// </param>
      /// <param name="pos:">position in the Source string
      /// </param>
      /// <param name="isAdvance:">advance or retreat the ret pos if a DBCS char is split
      /// </param>
      /// <returns> position in the Dest string
      /// 
      /// Example: convPos("abcYZ", "YZabc", 4)
      /// It returns 4, if the all characters in the strings are SBCS.
      /// 
      /// If 'a', 'b' and 'c' are SBCS, and 'Y' and 'Z' are DBCS, it
      /// returns 3.
      /// pos
      /// Unicode index  0 1 2  3  [4]
      /// +-------------+
      /// Source string |a|b|c| Y | Z |
      /// +-------------+
      /// ANSI index     0 1 2 3 4[5]6
      /// +-------------+
      /// Dest string   | Y | Z |a|b|c|
      /// +-------------+
      /// Unicode index   0   1  2[3]4
      /// ret
      /// </returns>
      public static int convPos(String strSource, String strDest, int pos, bool isAdvance)
      {
         byte[] byteSource;
         String strLeftB;
         int retPos;
         int diffLen;

         if (pos < 0)
            return 0;

         if (pos > strSource.Length)
            pos = strSource.Length;

         // add blanks to the Dest string if it is shorter than the Src string
         diffLen = lenB(strSource) - lenB(strDest);
         if (diffLen > 0)
         {
            StringBuilder strbufDest = new StringBuilder(strDest);
            for (; diffLen > 0; diffLen--)
               strbufDest.Append(' ');
            strDest = strbufDest.ToString();
         }

         // convert to byte[] by default-encoding
         byteSource = Encoding.Default.GetBytes(strSource.Substring(0, pos));

         strLeftB = leftB(strDest, byteSource.Length);
         retPos = strLeftB.Length;

         // if a full-width character is split, a blank is embedded and retPos is advanced. 
         if (!isAdvance && retPos > 0 && strLeftB[retPos - 1] == ' ' && strDest[retPos - 1] != ' ')
            retPos--;

         return retPos;
      }

      /// <summary> return the number of characters of picture which corresponds to
      /// given string.
      /// </summary>
      /// <param name="str:">given string
      /// </param>
      /// <param name="picture:">picture
      /// </param>
      /// <returns> minimal length of picture
      /// Example: getMinLenPicture("ZZ20/11/", "JJJJYY/MM/DD") [ZZ is DBCS]
      /// It returns 10.
      /// </returns>
      /// (DBCS Support)
      public static int getMinLenPicture(String str, String picture)
      {
         int len = 0;

         if (lenB(picture) - lenB(str) > 0)
            len = convPos(str, picture, str.Length, false);
         else
            len = picture.Length;

         return len;
      }

      /// <summary> 
      /// </summary> Compares two specified strings in the DBCS sort order and returns an integer
      /// that indicates their relative position.
      /// <param name="str1:">The first string to compare. 
      /// </param>
      /// <param name="str2:">The second string to compare. 
      /// </param>
      /// <returns>an integer that indicates the lexical relationship between the two strings.
      ///  -1: str1 is less than str2.
      ///   0: str1 equals str2.
      ///   1: str1 is greater than str2. 
      /// </returns>
      public static int strcmp(String str1, String str2)
      {
         byte[] array1 = Encoding.Default.GetBytes(str1);
         byte[] array2 = Encoding.Default.GetBytes(str2);
         
         for (int i = 0; i < array1.Length && i < array2.Length; i++)
         {
            if (array1[i] > array2[i])
               return 1;
            else if (array1[i] < array2[i])
               return -1;
         }

         if (array1.Length > array2.Length)
            return 1;
         else if (array1.Length < array2.Length)
            return -1;
         else
            return 0;
      }
   }
}