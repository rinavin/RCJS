using System;

namespace com.magicsoftware.util
{
   /// <summary>
   ///   JPN: Japanese date picture support
   ///   Utility Class for Japanese date
   /// </summary>
   /// <author>  Toshiro Nakayoshi (MSJ)
   /// </author>
   public sealed class UtilDateJpn
   {
      private static UtilDateJpn _instance;

      // ---- gengo (the name of an era)  ---------------------------------------
      private const int MAX_GENGO = 5;
      private int MaxGengo = MAX_GENGO;

      private const int IDX_UPPERCASE_ALPHA = 0;
      private const int IDX_LOWERCASE_ALPHA = 1;
      private const int IDX_KANJI = 2;

      private const int IDX_YEAR = 0;
      private const int IDX_MONTH = 1;
      private const int IDX_DAY = 2;
      private const int IDX_DOY = 3;

      private static readonly String[] JweekStr = new[]
                                                      {
                                                         "\u0020\u0020\u0020", // space * 3
                                                         "\u65e5\u66dc\u65e5", // Sunday    in Japanese
                                                         "\u6708\u66dc\u65e5", // Monday    in Japanese
                                                         "\u706b\u66dc\u65e5", // Tuesday   in Japanese
                                                         "\u6c34\u66dc\u65e5", // Wednesday in Japanese
                                                         "\u6728\u66dc\u65e5", // Thursday  in Japanese
                                                         "\u91d1\u66dc\u65e5", // Friday    in Japanese
                                                         "\u571f\u66dc\u65e5"  // Saturday  in Japanese
                                                      };

      private static readonly String[] JmonthStr = new[]
                                                      {
                                                         "",
                                                         " 1\u6708", " 2\u6708", " 3\u6708", " 4\u6708",
                                                         " 5\u6708", " 6\u6708", " 7\u6708", " 8\u6708",
                                                         " 9\u6708", "10\u6708", "11\u6708", "12\u6708"
                                                      };

      private static String[][] GengoStr = new[]
                                                      {
                                                         new[] {"?", "?", "????"},
                                                         new[] {"M", "m", "\u660e\u6cbb"}, // the Meiji  era
                                                         new[] {"T", "t", "\u5927\u6b63"}, // the Taisyo era
                                                         new[] {"S", "s", "\u662d\u548c"}, // the Syouwa era
                                                         new[] {"H", "h", "\u5e73\u6210"}, // the Heisei era
                                                         new[] {"?", "?", "\uff1f\uff1f"}, // Fullwidth question mark (reserve)
                                                         new[] {"?", "?", "\uff1f\uff1f"},
                                                         new[] {"?", "?", "\uff1f\uff1f"}
                                                      };

      private static int[][] StartDayOfGengo = new[]
                                                      {
                                                         // { year, month, day, doy }
                                                         new[] {1, 1, 1, 1}, // 01/Jan/0001
                                                         new[] {1868, 9, 8, 252}, // 08/Sep/1868
                                                         new[] {1912, 6, 30, 212}, // 30/Jul/1912
                                                         new[] {1926, 12, 25, 359}, // 25/Dec/1926
                                                         new[] {1989, 1, 8, 8}, // 08/Jan/1989
                                                         new[] {0, 0, 0, 0},
                                                         new[] {0, 0, 0, 0},
                                                         new[] {0, 0, 0, 0}
                                                      };

      public static UtilDateJpn getInstance()
      {
         if (_instance == null)
         {
            lock (typeof(UtilDateJpn))
            {
               if (_instance == null)
                  _instance = new UtilDateJpn();
            }
         }
         return _instance;
      }
      
      public static String[] getArrayDow()
      {
         return JweekStr;
      }

      public static String getStrDow(int intIdx)
      {
         if (intIdx < 0 || 7 < intIdx)
            intIdx = 0;

         return JweekStr[intIdx];
      }

      public static String convertStrMonth(int month)
      {
         if (month < 0 || 12 < month)
            month = 0;

         return JmonthStr[month];
      }

      /// <summary>
      ///   Convert a year (A.D.) into Japanese year of an era
      ///   This method is modeled after "date_jpn_year_ofs" function in 
      ///   "\mglocal\jpn\jpndate_jpn.cpp".
      /// </summary>
      /// <param name = "intYear:">year (A.D.)
      /// </param>
      /// <param name = "intDoy:">DOY
      /// </param>
      /// <returns> year of an era.
      ///   if either param is invalid, it returns 0.
      /// </returns>
      public int date_jpn_year_ofs(int intYear, int intDoy)
      {
         int intIdxNow;

         if ((intYear < 1) || (intDoy < 1))
            return (0);

         intIdxNow = MaxGengo - 1;

         while (intYear < StartDayOfGengo[intIdxNow][IDX_YEAR])
            intIdxNow--;

         if ((intYear == StartDayOfGengo[intIdxNow][IDX_YEAR]) && (intDoy < StartDayOfGengo[intIdxNow][IDX_DOY]))
            intIdxNow--;

         return (intYear - StartDayOfGengo[intIdxNow][IDX_YEAR] + 1);
      }

      /// <summary>
      ///   Convert a year (A.D.) into a name of a Japanese era
      ///   This method is modeled after "date_jpn_yr_2_a" function in 
      ///   "\mglocal\jpn\jpndate_jpn.cpp".
      /// </summary>
      /// <param name = "intYear:">year (A.D.)
      /// </param>
      /// <param name = "intDoy:">DOY
      /// </param>
      /// <param name = "isKanji:">return a full name (true) or the first letter (false).
      /// </param>
      /// <returns> name of an era
      ///   if either param is invalid, it returns "?".
      /// </returns>
      public String date_jpn_yr_2_a(int intYear, int intDoy, bool isKanji)
      {
         int intIdxNow;
         String strRet;

         if ((intYear < 1) || (intDoy < 1))
            intIdxNow = 0;
         else
         {
            intIdxNow = MaxGengo - 1;

            while (intYear < StartDayOfGengo[intIdxNow][IDX_YEAR])
               intIdxNow--;

            if ((intYear == StartDayOfGengo[intIdxNow][IDX_YEAR]) && (intDoy < StartDayOfGengo[intIdxNow][IDX_DOY]))
               intIdxNow--;
         }

         if (isKanji)
            strRet = GengoStr[intIdxNow][IDX_KANJI];
         else
            strRet = GengoStr[intIdxNow][IDX_UPPERCASE_ALPHA];

         return strRet;
      }

      /// <summary>
      ///   Get the first year (A.D.) of a specified Japanese era
      ///   This method is modeled after "date_jpn_yr_4_a" function in 
      ///   "\mglocal\jpn\jpndate_jpn.cpp".
      /// </summary>
      /// <param name = "ucp_str:">name of a specified Japanese era
      /// </param>
      /// <param name = "s_len:">length (the number of bytes) of ucp_str
      /// </param>
      /// <returns> year (A.D.)
      /// </returns>
      private int date_jpn_yr_4_a(String ucp_str, int s_len)
      {
         int s_maxgen = MaxGengo - 1;
         String GengoStrWork;

         if (s_len > 0)
            // if s_len == 0, s_maxgen is max.
         {
            if (s_len > 1)
               ucp_str = UtilStrByteMode.leftB(ucp_str, s_len);

            for (; s_maxgen > 0; s_maxgen--)
            {
               if (s_len == 1)
               {
                  if (ucp_str.Equals(GengoStr[s_maxgen][IDX_UPPERCASE_ALPHA]) ||
                      ucp_str.Equals(GengoStr[s_maxgen][IDX_LOWERCASE_ALPHA]))
                     break;
               }
               else if (s_len > 1)
               {
                  GengoStrWork = UtilStrByteMode.leftB(GengoStr[s_maxgen][IDX_KANJI], s_len);
                  if (ucp_str.Equals(GengoStrWork))
                     break;
               }
            }
         }

         if (s_maxgen > 0)
            return (StartDayOfGengo[s_maxgen][IDX_YEAR]);
         else
         {
            GengoStrWork = ucp_str.Substring(0, 1);
            if (GengoStrWork.Equals(" ") || GengoStrWork.Equals("?"))
               return (1);
            else
               return (0);
         }
      }

      /// <summary>
      ///   Get the name of an era in date string
      /// </summary>
      /// <param name = "strDate:">string of input strDate
      /// </param>
      /// <param name = "strPicture:">string of picture
      /// </param>
      /// <param name = "intStartPos:">start position to search
      /// </param>
      /// <returns> name of an era
      /// </returns>
      private static String getEraNameStrInDate(String strDate, String strPicture, int intStartPos)
      {
         String strBuff = null;
         int intLetters = 0;

         // A DBCS character in "strDate" consumes two characters in "strPicture".
         // If "strDate" contains DBCS, the position of "strPicture" has to skip next index.
         // This variable indicates the number of skipped indexes.
         int intPicIdxOfs = 0;

         for (int i = intStartPos; i + intPicIdxOfs < strPicture.Length; i++)
         {
            if (strPicture[i + intPicIdxOfs] == PICInterface.PIC_JY1)
               intLetters = 1;
               // the number of charaters, not bytes
            else if (strPicture[i + intPicIdxOfs] == PICInterface.PIC_JY2)
               intLetters = 1;
               // the number of charaters, not bytes
            else if (strPicture[i + intPicIdxOfs] == PICInterface.PIC_JY4)
               intLetters = 2;
               // the number of charaters, not bytes
            else
            {
               if (i < strDate.Length)
               {
                  // If "strDate" contains DBCS, the position of "strPicture" has to skip next index.
                  if (UtilStrByteMode.isHalfWidth(strDate[i]) == false &&
                      UtilStrByteMode.isHalfWidth(strPicture[i + intPicIdxOfs]))
                     intPicIdxOfs++;
               }
               continue;
            }

            //strBuff = utilStrByteMode.midB(strDate, i, intLetters);
            strBuff = strDate.Substring(i, intLetters);
            break; // exit loop
         }

         return strBuff;
      }

      /// <summary>
      ///   Get the length of the name of an era in picture
      /// </summary>
      /// <param name = "strPicture">string of picture
      /// </param>
      /// <param name = "intStartPos">start position to search
      /// </param>
      /// <returns> length of the name (the number of bytes)
      /// </returns>
      private static int getEraNameLenInPicture(String strPicture, int intStartPos)
      {
         int intLetters = 0;

         for (int i = intStartPos; i < strPicture.Length; i++)
         {
            if (strPicture[i] == PICInterface.PIC_JY1)
               intLetters = 1;
            else if (strPicture[i] == PICInterface.PIC_JY2)
               intLetters = 2;
            else if (strPicture[i] == PICInterface.PIC_JY4)
               intLetters = 4;

            if (intLetters > 0)
               break;
         }

         return intLetters;
      }

      /// <summary>
      ///   Get the start year of an era in picture
      /// </summary>
      /// <param name = "strDate:">string of input strDate
      /// </param>
      /// <param name = "strPicture:">string of picture
      /// </param>
      /// <returns> start year of the era
      /// </returns>
      public int getStartYearOfEra(String strDate, String strPicture)
      {
         String strEra;
         int intEraLen = 0;
         int intStartYearOfEra = 0;

         // convert year of the Japanese era to year A.D.
         // get the name of an era
         strEra = getEraNameStrInDate(strDate, strPicture, 0);
         if (strEra == null)
            return 0;

         intEraLen = getEraNameLenInPicture(strPicture, 0);
         if (intEraLen == 0)
            return 0;

         // get the start year of the era
         intStartYearOfEra = date_jpn_yr_4_a(strEra, intEraLen);

         return intStartYearOfEra;
      }

      /// <summary> Add extra Gengo data into the Gengo tables</summary>
      /// <param name="strExtraGengo:">
      /// </param>
      /// <returns>
      /// </returns>
      public void addExtraGengo(String strExtraGengo)
      {
         String[] strGengoInfo;
         String[] strTok;
         String[] strDate;
         //String token = null;
         int i;

         // e.g. strExtraGengo = "2012/04/01,092,AaABCD;2013/04/01,091,WwWXYZ;"
         strGengoInfo = strExtraGengo.Split(';');

         for (i = 0; i <= strGengoInfo.GetUpperBound(0); i++)
         {
            if (strGengoInfo[i].Length > 0)
            {
               strTok = strGengoInfo[i].Split(',');
               if (strTok.GetUpperBound(0) == 2 && strTok[0].Length > 0 && strTok[1].Length > 0 && strTok[2].Length > 0)
               {
                  strDate = strTok[0].Split('/');
                  if (strDate.GetUpperBound(0) == 2 && strDate[0].Length > 0 && strDate[1].Length > 0 && strDate[2].Length > 0)
                  {
                     GengoStr[MAX_GENGO + i][0] = strTok[2].Substring(0, 1);     // symbol name (upper case): A
                     GengoStr[MAX_GENGO + i][1] = strTok[2].Substring(1, 1);     // symbol name (lower case): a
                     GengoStr[MAX_GENGO + i][2] = strTok[2].Substring(2);        // gengo name: ABCD

                     StartDayOfGengo[MAX_GENGO + i][0] = int.Parse(strDate[0]);  // start year: 2012
                     StartDayOfGengo[MAX_GENGO + i][1] = int.Parse(strDate[1]);  // start month: 4
                     StartDayOfGengo[MAX_GENGO + i][2] = int.Parse(strDate[2]);  // start day: 1
                     StartDayOfGengo[MAX_GENGO + i][3] = int.Parse(strTok[1]);   // days since January 1: 92
                     MaxGengo++;
                  }
               }
            }
         }
      }
   }
}