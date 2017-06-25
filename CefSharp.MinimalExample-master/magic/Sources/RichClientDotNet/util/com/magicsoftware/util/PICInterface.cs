using System;

namespace com.magicsoftware.util
{
   /// <summary>
   ///   An interface to define the constantes used by the Picture mechanism.
   /// </summary>
   public class PICInterface
   {
      //--------------------------------------------------------------------------
      // TEMP!
      //--------------------------------------------------------------------------

      public const int PIC_X = 1;
      public const int PIC_U = 2;
      public const int PIC_L = 3;
      public const int PIC_N = 4;
      public const int PIC_YY = 5;
      public const int PIC_YYYY = 6;
      public const int PIC_MMD = 7;
      public const int PIC_MMM = 8;
      public const int PIC_DD = 9;
      public const int PIC_DDD = 10;
      public const int PIC_DDDD = 11;
      public const int PIC_W = 12;
      public const int PIC_WWW = 13;
      public const int PIC_HH = 14;
      public const int PIC_MMT = 15;
      public const int PIC_SS = 16;
      public const int PIC_PM = 17;
      public const int PIC_HYYYYY = 18; // Hebrew year
      public const int PIC_HL = 19; // Hebrew thousand year
      public const int PIC_HDD = 20; // Hebrew day of month
      public const int PIC_MS = 21; // Milliseconds
      public const int PIC_LOCAL = 23; // the space between PIC_LOCAL and PIC_MAX_OP
      public const int PIC_MAX_MSK_LEN = 100;
      // JPN: Japanese date picture support
      public const int PIC_JY1 = PICInterface.PIC_LOCAL + 0; // the name of an era (1 byte)
      public const int PIC_JY2 = PICInterface.PIC_LOCAL + 1; // the name of an era (2 bytes)
      public const int PIC_JY4 = PICInterface.PIC_LOCAL + 2; // the name of an era (4 bytes)
      public const int PIC_YJ = PICInterface.PIC_LOCAL + 3; // a year of an era
      public const int PIC_BB = PICInterface.PIC_LOCAL + 4; // a day of the week (2, 4 or 6 bytes)
      // DBCS pictures for iSeries
      public const int PIC_J = PICInterface.PIC_LOCAL + 5; // DBCS only (with SO/SI)
      public const int PIC_T = PICInterface.PIC_LOCAL + 6; // All SBCS or All DBCS (with SO/SI)
      public const int PIC_G = PICInterface.PIC_LOCAL + 7; // DBCS only (without SO/SI)
      public const int PIC_S = PICInterface.PIC_LOCAL + 8; // SBCS only
      public const int PIC_MAX_OP = 31; // is reserved for DLL's pictures
      public const int NULL_CHAR = -1;
      public const int DB_STR_MAX = 255;
      public const int DAYSINFOURCENT = 146097; // ((365*4+1)*25-1)*4+1 */
      public const int DAYSINCENTURY = 36524; // (365*4+1)*25-1 */
      public const int DAYSINFOURYEAR = 1461; // 365*4+1 */
      public const int DAYSINYEAR = 365;
      public const int DAYSINMONTH = 31;
      public const int DATE_BUDDHIST_GAP = 543; // years above the gregorian date
      public const String DEFAULT_DATE = "693961";
      public const String DEFAULT_TIME = "0";
      public static readonly int[] date_day_tab = new[] {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};

      public static readonly String[] date_month_str = new[]
                                                          {
                                                             "          ", "January   ", "February  ", "March     ",
                                                             "April     ", "May       ", "June      ", "July      ",
                                                             "August    ", "September ", "October   ", "November  ",
                                                             "December  "
                                                          };

      public static readonly String[] date_dow_str = new[]
                                                        {
                                                           "          ", "Sunday    ", "Monday    ", "Tuesday   ",
                                                           "Wednesday ", "Thursday  ", "Friday    ", "Saturday  "
                                                        };

      //public final static int    DEF_century       = 1920; 
      //public final static char   DEF_date_mode     = 'E'; 
      // vec of pictures that can be given a numeric char only 
      public static readonly int[] NumDirective = new[]
                                                     {
                                                        PIC_N, PIC_YY, PIC_YYYY, PIC_MMD, PIC_DD, PIC_DDD, PIC_DDDD, PIC_W
                                                        , PIC_HH, PIC_MMT, PIC_SS
                                                     };
   }
}