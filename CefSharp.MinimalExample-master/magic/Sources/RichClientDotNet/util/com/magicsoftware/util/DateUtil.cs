using System;

namespace com.magicsoftware.util
{
   public class DateUtil
   {
      private const int DATE_MONTH_LEN = 10;
      private const int DATE_DOW_LEN = 10;

      private static readonly String[] _localMonths = new String[13];
      private static readonly String[] _localDays = new String[8];

      /// <summary>
      ///  extract the vector which contains the names of the months, as specified by the 
      ///  language CAB
      /// </summary>
      public static String[] getLocalMonths(String names)
      {
         int i;
         int monthLen = DATE_MONTH_LEN;

         // if it's the first time then access the language CAB and take the values
         if (_localMonths[0] == null)
         {
            //cut the string into separate values
            if (names != null)
            {
               _localMonths[0] = PICInterface.date_month_str[0];
               for (i = 1; i < _localMonths.Length; i++)
                  if (i * monthLen >= names.Length)
                  {
                     _localMonths[i] = names.Substring((i - 1) * monthLen);
                     while (monthLen - _localMonths[i].Length > 0)
                        _localMonths[i] = String.Concat(_localMonths[i], " ");
                  }
                  else
                     _localMonths[i] = names.Substring((i - 1) * monthLen, (i * monthLen) - ((i - 1) * monthLen));
            }
            else
            {
               for (i = 0; i < _localMonths.Length; i++)
                  _localMonths[i] = PICInterface.date_month_str[i];
            }
         }

         return _localMonths;
      }

      /// <summary>
      ///   extract the vector which contains the names of the days, as specified by the 
      ///   language CAB
      /// </summary>
      public static String[] getLocalDays(String names)
      {
         int i;
         int dowLen = DATE_DOW_LEN;

         // if it's the first time then access the language CAB and take the values
         if (_localDays[0] == null)
         {
            //cut the string into separate values
            if (names != null)
            {
               _localDays[0] = PICInterface.date_dow_str[0];
               for (i = 1; i < _localDays.Length; i++)
                  if (i * dowLen >= names.Length)
                  {
                     _localDays[i] = names.Substring((i - 1) * dowLen);
                     while (dowLen - _localDays[i].Length > 0)
                        _localDays[i] = String.Concat(_localDays[i], " ");
                  }
                  else
                     _localDays[i] = names.Substring((i - 1) * dowLen, (i * dowLen) - ((i - 1) * dowLen));
            }
            else
            {
               for (i = 0; i < _localMonths.Length; i++)
                  _localMonths[i] = PICInterface.date_dow_str[i];
            }
         }

         return _localDays;
      }
   }
}
