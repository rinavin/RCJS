using System;
using System.Text;
using System.Globalization;

namespace util.com.magicsoftware.util
{
   public static class DateTimeUtils
   {
      // create a DateTime reference from a given string, formatted: "dd/MM/yyyy HH:mm:ss" (HH - 24 hours clock)
      public static DateTime Parse(string dateTimeString)
      {
         DateTime dateTime;

         try
         {
            dateTime = DateTime.ParseExact(dateTimeString, XMLConstants.CACHED_DATE_TIME_FORMAT, CultureInfo.InvariantCulture);
         }
         catch
         {
            String[] parsed = dateTimeString.Split(new[] { '/', ' ', ':', '-' });
            dateTime = new DateTime(Convert.ToInt32(parsed[2]), Convert.ToInt32(parsed[1]), 
                                    Convert.ToInt32(parsed[0]), Convert.ToInt32(parsed[3]), Convert.ToInt32(parsed[4]), 
                                    Convert.ToInt32(parsed[5]));
         }

         return dateTime;
      }

      /// <summary> returns the number in a 2 digit string
      /// </summary>
      private static String int2str(int n)
      {
         return n > 9 ? n.ToString() : "0" + n;
      }

      // format a string from a given DateTime reference.
      // For now only the formats used are supported. If a new format is used, it's treatment
      // should be added.
      // possible format: 
      //    (1) "dd/MM/yyyy HH:mm:ss" (HH - 24 hours clock)
      public static string ToString(DateTime dateTime, string format)
      {
         string dateTimeString;

         try
         {
            dateTimeString = dateTime.ToString(format, CultureInfo.InvariantCulture);
         }
         catch (Exception ex)
         {
            if (format == XMLConstants.CACHED_DATE_TIME_FORMAT)
            {
               dateTimeString = int2str(dateTime.Day) + "/" + int2str(dateTime.Month) + "/" + 
                  dateTime.Year + " " + int2str(dateTime.Hour) + ":" + int2str(dateTime.Minute) + ":" + 
                  int2str(dateTime.Second);
            }
            else if (format == XMLConstants.ERROR_LOG_TIME_FORMAT)
            {
               dateTimeString = int2str(dateTime.Hour) + ":" + int2str(dateTime.Minute) + ":" + 
                  int2str(dateTime.Second) + ".";

               if (dateTime.Millisecond%100 > 50)
                  dateTimeString += (dateTime.Millisecond / 100 + 1);
               else
                  dateTimeString += (dateTime.Millisecond / 100);
            }
            else if (format == XMLConstants.ERROR_LOG_DATE_FORMAT)
            {
               dateTimeString = int2str(dateTime.Day) + "/" + int2str(dateTime.Month) + "/" + dateTime.Year;
            }
            else if (format == XMLConstants.HTTP_ERROR_TIME_FORMAT)
            {
               dateTimeString = int2str(dateTime.Hour) + ":" + int2str(dateTime.Minute) + ":" +
                  int2str(dateTime.Second) + ".";
            }
            else
            {
               Console.WriteLine(ex.Message);

               StringBuilder dateTimeTmpString = new StringBuilder();

               //..

               dateTimeString = dateTimeTmpString.ToString();
            }
         }

         return dateTimeString;
      }

      // format a string from a given DateTime reference.
      // possible format: 
      //    (1) "dd/MM/yyyy HH:mm:ss" (HH - 24 hours clock)
      public static string ToString(DateTime dateTime, System.Globalization.DateTimeFormatInfo formatter)
      {
         string dateTimeString;

         try
         {
            dateTimeString = dateTime.ToString(formatter);
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);

            StringBuilder dateTimeTmpString = new StringBuilder();

            //..

            dateTimeString = dateTimeTmpString.ToString();
         }

         return dateTimeString;
      }
   }
}
