using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
   internal class HebrewDate
   {
      private const int PARTS_PER_HOUR = 1080;
      private const int HOURS_PER_DAY = 24;
      private const int PARTS_PER_DAY = (HOURS_PER_DAY * PARTS_PER_HOUR);
      private const int MONTH_DAYS = 29;
      private const int MONTH_EXTRA_PARTS = (12 * PARTS_PER_HOUR + 793);
      private const int PARTS_PER_MONTH = (MONTH_DAYS * PARTS_PER_DAY + MONTH_EXTRA_PARTS);
      private const int MONTHS_PER_NORM_YEAR = 12;
      private const int MONTHS_PER_LEAP_YEAR = 13;
      private const int NORM_YEARS_PER_CYCLE = 12;
      private const int LEAP_YEARS_PER_CYCLE = 7;
      private const int YEARS_PER_CYCLE = (NORM_YEARS_PER_CYCLE + LEAP_YEARS_PER_CYCLE);
      private const int MONTHS_PER_CYCLE = (NORM_YEARS_PER_CYCLE * MONTHS_PER_NORM_YEAR + LEAP_YEARS_PER_CYCLE * MONTHS_PER_LEAP_YEAR);
      private const int PARTS_PER_NORM_YEAR = (MONTHS_PER_NORM_YEAR * PARTS_PER_MONTH);
      private const int DAYS_PER_NORM_YEAR = (PARTS_PER_NORM_YEAR / PARTS_PER_DAY);
      private const int PARTS_PER_LEAP_YEAR = (MONTHS_PER_LEAP_YEAR * PARTS_PER_MONTH);
      private const int DAYS_PER_LEAP_YEAR  = (PARTS_PER_LEAP_YEAR / PARTS_PER_DAY);
      private const int PARTS_PER_CYCLE = (MONTHS_PER_CYCLE * PARTS_PER_MONTH);
      private const int DAYS_PER_CYCLE = (PARTS_PER_CYCLE / PARTS_PER_DAY);
      private const int CREATION_DOW = 2;
      private const int CREATION_PARTS = (5 * PARTS_PER_HOUR + 204);
      private const int DAYS_TIL_JESUS = 1373428;

      private const int HESHVAN = 2;
      private const int KISLEV = 3;
      private const int ADAR = 6;
      private const int ADAR_B = 13;

      /*-----------------------------------------------------*/
      /* Hebrew numeric representation of each Hebrew letter */
      /* --------------------------------------------------- */
      private static int[] DATEHEB_letters = 
      {
         1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 20, 30, 40, 40,
         50, 50, 60, 70, 80, 80, 90, 90, 100, 200, 300, 400
      };

      private const char ALEF = 'א';
      private const char TAV = 'ת';

      /* ------------------------------------------------ */
      /* N of Months from beginning of 19 years cycle, to */
      /* the beginning of a year (in the range 0 - 18)    */
      /* ------------------------------------------------ */
      private static int[] DATEHEB_months_n = 
      {
         0,   12,  24,  37,  49,  61,  74,  86,  99,  111,
         123, 136, 148, 160, 173, 185, 197, 210, 222, 235
      };

      /* ------------------------------------------------------------------ */
      /* N of days from the beginning of a year to the beginning of a month */
      /* Tishrei, Heshvan, Kislev, Tevet, Shvat, Adar,                      */
      /* Nisan,   Iyar,    Sivan,  Tamuz, Av,    Elul                       */
      /* ------------------------------------------------------------------ */
      private static int[] DATEHEB_norm_days_in_month =
         {30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 0};
      private static int[] DATEHEB_leap_days_in_month =
         {30, 29, 30, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29};

      /// <summary>
      /// calculate if year is leap
      /// </summary>
      /// <param name="year"></param>
      /// <returns></returns>
      private static bool dateheb_year_is_leap(long year)
      {
         year %= (long)YEARS_PER_CYCLE;
         return (DATEHEB_months_n[year + 1] - DATEHEB_months_n[year] == 13);
      }

      /// <summary>
      /// calculate days in month
      /// </summary>
      /// <param name="month"></param>
      /// <param name="leap"></param>
      /// <param name="days_in_year"></param>
      /// <returns></returns>
      private static int dateheb_days_in_month(int month, bool leap, int days_in_year)
      {
         int days_in_month = days_in_month = (leap ? DATEHEB_leap_days_in_month[month - 1] : DATEHEB_norm_days_in_month[month - 1]);

         if (leap)
            days_in_year -= 30;
         
         switch (days_in_year)
         {
            case 353:
               if (month == KISLEV)
                  days_in_month = 29;
               break;
            case 355:
               if (month == HESHVAN)
                  days_in_month = 30;
               break;
         }
         return (days_in_month);
      }

      /// <summary>
      /// calculate days from creation till start of the year
      /// </summary>
      /// <param name="year"></param>
      /// <returns></returns>
      private static int dateheb_year_start(int year)
      {
         int cycle;
         int months;
         int days;
         int parts;
         int dow;

         /*---------------------------*/
         /* calculate "molad" of year */
         /*---------------------------*/
         year--;
         cycle = year / YEARS_PER_CYCLE;
         year %= YEARS_PER_CYCLE;

         /*---------------------------------------*/
         /* How many months passed since creation */
         /*---------------------------------------*/
         months = cycle * MONTHS_PER_CYCLE + DATEHEB_months_n[year];

         /*-------------------------------------*/
         /* How many days passed since creation */
         /*-------------------------------------*/
         days = months * MONTH_DAYS;

         /*--------------------------------------------*/
         /* How many extra parts passed since creation */
         /*--------------------------------------------*/
         parts = months * MONTH_EXTRA_PARTS + CREATION_PARTS;

         days += parts / PARTS_PER_DAY;
         parts %= PARTS_PER_DAY;

         /*---------------------------*/
         /* Reminder > 3/4 of a day ? */
         /*---------------------------*/
         if (parts >= PARTS_PER_DAY * 3L / 4L)
         {
            parts = 0;
            days++;
         }

         /*--------------------------------------------------*/
         /* verify day-of-week not Sunday, Wednesday, Friday */
         /*--------------------------------------------------*/
         dow = ((days - 1 + CREATION_DOW) % 7) + 1;
         if ((dow == 1) || (dow == 4) || (dow == 6))
         {
            parts = 0;
            days++;
         }

         /*-------------------------------*/
         /* verify if year too long/long */
         /*-------------------------------*/
         if (!dateheb_year_is_leap(year))
         {
            if (dow == 3 && parts >= (9L * PARTS_PER_HOUR + 204L))
               days += 2;
         }

         if (dateheb_year_is_leap(year + (long)YEARS_PER_CYCLE - 1))
         {
            if (dow == 2 && parts >= (15L * PARTS_PER_HOUR + 589L))
               days++;
         }

         return (days);
      }
      
      /// <summary>
      /// Convert a Magic internal Date to a Hebrew date (year, month, day)  
      /// Note that ADAR_B is represented by month = 13  
      /// </summary>
      /// <param name="date"></param>
      /// <param name="hday"></param>
      /// <param name="hmonth"></param>
      /// <param name="hyear"></param>
      /// <returns>TRUE if leap year</returns>
      internal static bool dateheb_4_d(int date, out int day, out int month, out int year)
      {
         int  cycle;
         int  days;
         int curr_year_start;
         int next_year_start;
         int  days_in_year;
         int  days_in_month;
         bool leap;
         int  i;

         /*--------------------*/
         /* Days from creation */
         /*--------------------*/
         date += DAYS_TIL_JESUS;

         /*--------------------------------*/
         /* Guess a year (<= correct year) */
         /*--------------------------------*/
         cycle = date / (DAYS_PER_CYCLE + 1);
         days = date % (DAYS_PER_CYCLE + 1);
         year = cycle * YEARS_PER_CYCLE;
         year += days / (DAYS_PER_LEAP_YEAR + 1);

         /*----------------------------*/
         /* Advance year until correct */
         /*----------------------------*/
         while (true)
         {
            next_year_start = dateheb_year_start (year + 1);
            if (next_year_start >= date)
               break;
            year++;
         }
         curr_year_start = dateheb_year_start (year);
         day = date - curr_year_start;
         days_in_year = next_year_start - curr_year_start;
         leap = (days_in_year > 355);

         /*------------*/
         /* Find Month */
         /*------------*/
         for (i = 1; i < 13; i++)
         {
            days_in_month = dateheb_days_in_month (i, leap, days_in_year);
            if (day <= days_in_month)
               break;
            day -= days_in_month;
         }

         if (!leap || i <= ADAR)
            month = i;
         else
            if (i == ADAR + 1)
               month = ADAR_B;
            else
               month = i - 1;

         return (leap);
      }

      /// <summary>
      /// Compute a Hebrew letter to add to string, representing a numeric value
      /// </summary>
      /// <param name="val"></param>
      /// <param name="str"></param>
      /// <param name="strPos"></param>
      /// <param name="use_ending"></param>
      /// <param name="quote"></param>
      /// <returns></returns>
      internal static int dateheb_i_2_h(int val, char[] str, int strPos, bool use_ending, bool quote)
      {
         int let_idx = 0;
         int letter = 0;
         int len = 0;

         while (val > 0)
         {
            for (let_idx = DATEHEB_letters.Length; let_idx > 0; let_idx--)
            {
               letter = DATEHEB_letters[let_idx - 1];
               if (val >= letter)
               {
                  val -= letter;
                  str[strPos++] = (char)(ALEF + let_idx - 1);
                  len++;
                  break;
               }
            }
         }

         if (len > 0)
         {
            if (len >= 2)
            {
               if (str[strPos - 2] == (short)1497)
                  if (str[strPos - 1] == (short)1492 || str[strPos - 1] == (short)1493)
                  {
                     str[strPos - 2]--;
                     str[strPos - 1]++;
                  }
            }
            

            // Final letters: only from 'מ'.
            if (let_idx > 10 && use_ending)
               if (DATEHEB_letters[let_idx - 1] == DATEHEB_letters[let_idx - 2])
                  str[strPos - 1]--;

            if (quote)
            {
               if (len == 1)
                  str[strPos] = '\'';
               else
               {
                  str[strPos] = str[strPos - 1];
                  str[strPos - 1] = '"';
               }
               len++;
            }
         }
         return (len);
      }

      /// <summary>
      /// Convert Hebrew year/day to a string
      /// </summary>
      /// <param name="outStr"></param>
      /// <param name="len"></param>
      /// <param name="full_len"></param>
      /// <param name="hyear"></param>
      /// <param name="trim"></param>
      /// <param name="use_ending"></param>
      /// <returns>length</returns>
      internal static int dateheb_2_str (char[] outStr, int strPos, int len, ref int out_len, int full_len, int hyear, bool trim, bool use_ending)
      {
         bool quote;
         int heb_len;
         int rem_len;

         quote = (len > full_len && outStr[strPos + full_len] == outStr[strPos]);
         if (quote)
            full_len++;
         heb_len = dateheb_i_2_h (hyear % 1000, outStr, strPos, use_ending, quote);

         rem_len = full_len - heb_len;
         if (rem_len > 0)
         {
            if (trim)
            {
               out_len -= rem_len;
               int tmp_out_len = len - full_len;
               if (tmp_out_len > 0)
                  DisplayConvertor.char_memcpy(outStr, strPos + heb_len, outStr, strPos + full_len, tmp_out_len);
               else
                  heb_len = full_len;
            }
            else
            {
               // Do not over the string length. It may be when YYYY in the end. (DDD MMMMM YYYY)
               if (rem_len > outStr.Length - strPos - heb_len)
                  rem_len = outStr.Length - strPos - heb_len;
               DisplayConvertor.char_memset(outStr, strPos + heb_len, ' ', rem_len);
               heb_len = full_len;
            }
         }

         return heb_len;
      }

      private const int MONTH_LEN = 5;
      private static readonly String[] _localMonths = new String[15];
      /// <summary>
      /// Cut the month string from 'const' into separate values
      /// </summary>
      /// <returns></returns>
      internal static string[] GetLocalMonths()
      {
         int i;
         int monthLen = MONTH_LEN;

         if (_localMonths[0] == null)
         {
            String monthStr = Events.GetMessageString(MsgInterface.DATEHEB_MONTH_STR);
            if (monthStr != null)
            {
               _localMonths[0] = "       ";
               for (i = 1; i < _localMonths.Length; i++)
                  _localMonths[i] = monthStr.Substring((i - 1) * monthLen, (i * monthLen) - ((i - 1) * monthLen));
            }
         }
         return _localMonths;
      }

      private const int DOW_LEN = 5;
      private static readonly String[] _localDows = new String[8];
      /// <summary>
      /// Cut the day of week string from 'const' into separate values
      /// </summary>
      /// <returns></returns>
      internal static string[] GetLocalDows()
      {
         int i;
         int dowLen = DOW_LEN;

         if (_localDows[0] == null)
         {
            String dowsStr = Events.GetMessageString(MsgInterface.DATEHEB_DOW_STR);
            if (dowsStr != null)
            {
               _localDows[0] = "     ";
               for (i = 1; i < _localDows.Length; i++)
                  _localDows[i] = dowsStr.Substring((i - 1) * dowLen, (i * dowLen) - ((i - 1) * dowLen));
            }
         }
         return _localDows;
      }

      /// <summary>
      /// Convert a hebrew string -> integer
      /// </summary>
      /// <param name="str"></param>
      /// <param name="len"></param>
      /// <param name="pos"></param>
      /// <returns>converted to integer</returns>
      internal static int dateheb_h_2_i(char[] str, int len, int pos)
      {
         int val;
         bool has_1_to_9 = false;
         bool has_10_to_90 = false;
         bool has_100_to_300 = false;
         int cnt_400 = 0;
         int last_val = 1000;
         int let_val;

         for (val = 0; len > 0 && pos < str.Length; pos++, len--)
            if (str[pos] >= ALEF && str[pos] <= TAV)  
            {
               let_val = DATEHEB_letters[str[pos] - ALEF];
               if (let_val > last_val)
                  return (0);

               if (let_val == 400)
               {
                  if (cnt_400 == 2)
                     return (0);
                  cnt_400++;
               }
               else if (let_val >= 100)
               {
                  if (has_100_to_300)
                     return (0);
                  has_100_to_300 = true;
               }
               else if (let_val >= 10)
               {
                  if (has_10_to_90)
                     return (0);
                  has_10_to_90 = true;
               }
               else
               {
                  if (has_1_to_9)
                  {
                     if (has_10_to_90 || last_val != 9)
                        return (0);
                     if (let_val != 6 && let_val != 7)
                        return (0);
                  }
                  has_1_to_9 = true;
               }
               val += let_val;
               last_val = let_val;
            }

         return (val);
      }

      /// <summary>
      /// Check whether the character is a Hebrew letter
      /// </summary>
      /// <param name="letter"></param>
      /// <returns></returns>
      internal static bool isHebrewLetter(char letter)
      {
         if (letter >= ALEF && letter <= TAV)
            return true;
         else
            return false;
      }

      /// <summary>Add found in newvalue numbers(2 or 3 letters) to buffer and return
      /// last found number index
      /// </summary>
      /// <param name="currValue">in newvalue</param>
      /// <param name="buffer"></param>
      /// <returns> currValue in newvalue</returns>
      internal static int add2Date(String newValue, int currValue, int letters, StringBuilder buffer)
      {
         int start = currValue;
         int i;
         StringBuilder currAdd = new StringBuilder(newValue.Length);

         while (start < newValue.Length && !isHebrewLetter(newValue[start]))
            start++; // find first hebrew letter

         for (i = start; i < start + letters && i < newValue.Length; i++)
         {
            if (isHebrewLetter(newValue[i]) || newValue[i] == ' ' || newValue[i] == '"' || newValue[i] == '\'')
               buffer.Append(newValue[i]);
            else
               break;
         }

         return i;
      }

      /// <summary>
      /// Convert a Hebrew date (year, month, day) to Magic internal date 
      /// </summary>
      /// <param name="year"></param>
      /// <param name="month"></param>
      /// <param name="day"></param>
      /// <returns></returns>
      internal static int dateheb_2_d(int year, int month, int day)
      {
         int days_from_creation;
         int days_in_year;
         bool leap;
         int i;

         if (day == 0 && month == 0 && year == 0)
            return (0);

         if (day == 0 || month == 0 || year == 0)
            return (1000000000);

         /*--------------------------------------------------------*/
         /* How many days from creation, to begining of the year ? */
         /*--------------------------------------------------------*/
         days_from_creation = dateheb_year_start(year);

         /*------------------------*/
         /* How days in the year ? */
         /*------------------------*/
         days_in_year = dateheb_year_start(year + 1) - days_from_creation;
         leap = (days_in_year > 355);

         /*-----------------*/
         /* Add month + day */
         /*-----------------*/
         if (leap && month > ADAR)
            if (month == ADAR_B)
               month = ADAR + 1;
            else
               month++;

         if (day > dateheb_days_in_month(month, leap, days_in_year))
            return (1000000000);

         for (i = 1; i < month; i++)
            days_from_creation += dateheb_days_in_month(i, leap, days_in_year);
         days_from_creation += day;

         /*-------------------------------*/
         /* Convert to Christian calender */
         /*-------------------------------*/
         return (Math.Max(days_from_creation - DAYS_TIL_JESUS, 0));
      }
   
   }
}
