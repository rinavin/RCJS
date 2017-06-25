using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.env;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.unipaas.management.gui
{
   public class DisplayConvertor
   {
      private readonly IEnvironment _environment;
      private int DATECHAR;

      private static DisplayConvertor _instance;
      public static DisplayConvertor Instance
      {
         get
         {
            if (_instance == null)
            {
               lock (typeof(DisplayConvertor))
               {
                  if (_instance == null)
                     _instance = new DisplayConvertor();
               }
            }
            return _instance;
         }
      }

      private DisplayConvertor()
      {
         _environment = Manager.Environment;
      }

      public DateBreakParams getNewDateBreakParams()
      {
         return new DateBreakParams();
      }

      public TimeBreakParams getNewTimeBreakParams()
      {
         return new TimeBreakParams();
      }

      /// <summary>
      ///   Build a display string of a value from its picture format
      /// </summary>
      /// <param name = "mgValue">  the internal value (as sent in the XML)
      /// </param>
      /// <param name = "rangeStr"> value range
      /// </param>
      /// <param name = "pic">      the picture format
      /// </param>
      /// <returns> string to display
      /// </returns>
      public String mg2disp(String mgValue, String rangeStr, PIC pic, int compIdx, bool time_date_pic_Z_edt)
      {
         return mg2disp(mgValue, rangeStr, pic, false, compIdx, time_date_pic_Z_edt);
      }

      public String mg2disp(String mgValue, String rangeStr, PIC pic, bool alwaysFill, int compIdx,
                            bool time_date_pic_Z_edt)
      {
         return mg2disp(mgValue, rangeStr, pic, alwaysFill, compIdx, false, time_date_pic_Z_edt);
      }

      /// <summary>
      ///   Build a display string of a value from its picture format
      /// </summary>
      /// <param name = "mgValue">the internal value (as sent in the XML)
      /// </param>
      /// <param name = "rangeStr">value range
      /// </param>
      /// <param name = "pic">the picture format
      /// </param>
      /// <param name = "alwaysFill">Always auto fill alpha over its picture (used for select controls)
      /// </param>
      /// <returns> string to display
      /// </returns>
      public String mg2disp(String mgValue, String rangeStr, PIC pic, bool alwaysFill, int compIdx, bool convertCase,
                            bool time_date_num_pic_Z_edt)
      {
         String str = "";
         String tmpRange;

         tmpRange = (rangeStr == null ? ("") : rangeStr);

         switch (pic.getAttr())
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               /*
               * TODO: Kaushal. Since the same function is used for Alpha as well as Unicode values, the name
               * should be changed.
               */
               str = fromAlpha(mgValue, pic, tmpRange, alwaysFill, convertCase);
               break;


            case StorageAttribute.NUMERIC:
               str = fromNum(mgValue, pic);
               //Value should be trimmed when we are displaying control in order to display alignment properly. But while entering control, it should not be trimmed
               //in order to read it properly.
               if ((pic.getMaskChars() == 0) || !time_date_num_pic_Z_edt)
                  str = StrUtil.ltrim(str);
               break;


            case StorageAttribute.DATE:
               str = fromDate(mgValue, pic, compIdx, time_date_num_pic_Z_edt);
               break;


            case StorageAttribute.TIME:
               str = fromTime(mgValue, pic, time_date_num_pic_Z_edt);
               break;


            case StorageAttribute.BOOLEAN:
               str = fromLogical(mgValue, pic, tmpRange);
               break;


            case StorageAttribute.BLOB:
               str = BlobType.getString(mgValue);
               break;


            case StorageAttribute.BLOB_VECTOR:
               str = mgValue;
               break;
         }

         return str;
      }

      /// <summary>
      ///   Build an internal representation of a display value from its picture format
      /// </summary>
      /// <param name = "dispValue">the value as entered by user
      /// </param>
      /// <param name = "rangeStr">value range
      /// </param>
      /// <param name = "pic">the picture format
      /// </param>
      /// <param name = "blobContentType">if the attribute is blob, its content type
      /// </param>
      /// <returns> internal form of data (as used in the XML)
      /// </returns>
      public String disp2mg(String dispValue, String rangeStr, PIC pic, int compIdx, char blobContentType)
      {
         String str = "";

         switch (pic.getAttr())
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               /* TODO: Kaushal. Since the same function is used for
               * Alpha as well as Unicode values, the name should 
               * be changed.
               */
               str = toAlpha(dispValue, pic);
               break;


            case StorageAttribute.NUMERIC:
               {
                  //Remove the mask from display value. 
                  int length = dispValue.Length;
                  while (length > 0)
                  {
                     if (!pic.picIsMask(length - 1) && dispValue[length - 1] != (' '))
                        break;
                     length--;
                  }
                  if (dispValue.Length > length)
                     dispValue = dispValue.Remove(length, (dispValue.Length - length));

                  if (pic.isNegative() && dispValue.Trim().StartsWith("-"))
                     dispValue = StrUtil.ltrim(dispValue);

                  str = toNum(dispValue, pic, compIdx);
               }
               break;


            case StorageAttribute.DATE:
               str = toDate(dispValue, pic, compIdx);
               break;


            case StorageAttribute.TIME:
               str = toTime(dispValue, pic);
               break;


            case StorageAttribute.BOOLEAN:
               str = toLogical(dispValue, pic, rangeStr);
               break;


            case StorageAttribute.BLOB:
               str = BlobType.createFromString(dispValue, blobContentType);
               break;
         }
         return str;
      }

      //--------------------------------------------------------------------------
      // Methods for coverting INTERNAL ---> DIPLAY
      //--------------------------------------------------------------------------

      /// <summary>
      ///   Handles magic number value
      /// </summary>
      /// <param name = "mgNum">   A Magic Number</param>
      /// <param name = "picStr">  The picture to use</param>
      /// <returns> the string to be displayed</returns>
      private String fromNum(String mgValue, PIC pic)
      {
         NUM_TYPE mgNum;
         String str;

         mgNum = new NUM_TYPE(mgValue);
         str = mgNum.toDisplayValue(pic);
         return str;
      }

      /// <summary>
      ///   Build a display string for an alpha value according to the picture
      /// </summary>
      /// <param name = "dataStr">   The source data
      /// </param>
      /// <param name = "picStr">    The picture to use for the value to display
      /// </param>
      /// <param name = "rangeStr">  The range of values to check
      /// </param>
      /// <param name = "alwaysFill">Always auto fill alpha over its picture (used for select controls)
      /// </param>
      /// <returns> the string to be displayed
      /// </returns>
      private String fromAlpha(String dataStr, PIC pic, String rangeStr, bool alwaysFill, bool convertCase)
      {
         int len;
         int min_len;
         String maskStr;
         String resStr = "";
         List<String> continuousRangeValues, discreteRangeValues;

         len = pic.getMaskSize();
         min_len = len;
         maskStr = pic.getMask();

         if (pic.getMaskChars() > 0)
            resStr = win_data_cpy(maskStr.ToCharArray(), len, dataStr.ToCharArray(), pic.isAttrAlpha(), convertCase);
         else
         {
            if (UtilStrByteMode.isLocaleDefLangDBCS() && pic.isAttrAlpha())
            // JPN: DBCS support
            {
               // substring by the number of bytes
               int intDataStrLenB = UtilStrByteMode.lenB(dataStr);
               if (min_len > intDataStrLenB)
                  min_len = intDataStrLenB;
               resStr = UtilStrByteMode.leftB(dataStr, min_len);
            }
            else
            {
               if (min_len > dataStr.Length)
                  min_len = dataStr.Length;

               if (convertCase)
                  resStr = win_data_cpy(maskStr.ToCharArray(), len, dataStr.ToCharArray(), pic.isAttrAlpha(),
                                        convertCase);
               else
                  resStr = dataStr.Substring(0, min_len);
            }
         }

         // We do not fill Alpha filed over control's picture length QCR # 295408
         if (alwaysFill || (rangeStr != null && rangeStr.Length > 0 && resStr.Length < len))
         {
            ValidationDetails vd = new ValidationDetails(rangeStr);
            continuousRangeValues = vd.getContinuousRangeValues();
            discreteRangeValues = vd.getDiscreteRangeValues();
            if (continuousRangeValues != null || discreteRangeValues != null)
               resStr = fillAlphaByRange(continuousRangeValues, discreteRangeValues, resStr);
         }
         return resStr;
      }

      private char caseConvertedChar(char data, char mask)
      {
         char ch;

         if (mask == PICInterface.PIC_U)
            ch = Char.ToUpper(data);
         else if (mask == PICInterface.PIC_L)
            ch = Char.ToLower(data);
         else
            ch = data;

         return ch;
      }

      private String win_data_cpy(char[] maskStr, int maskLen, char[] dataStr, bool isAttrAlpha)
      {
         return win_data_cpy(maskStr, maskLen, dataStr, isAttrAlpha, false);
      }

      /// <summary>
      ///   Handles alpha value
      /// </summary>
      /// <param name = "maskStr">   The mask to be used in order to build the string
      /// </param>
      /// <param name = "picStr">    The picture to use
      /// </param>
      /// <returns> the string to be displayed
      /// </returns>
      private String win_data_cpy(char[] maskStr, int maskLen, char[] dataStr, bool isAttrAlpha, bool convertCase)
      {
         int maskIdx = 0;
         int dataIdx = 0;
         char[] resStr = new char[maskLen];

         if (isAttrAlpha && UtilStrByteMode.isLocaleDefLangDBCS())
         // JPN: DBCS support
         {
            // a DBCS character in "dataStr" consumes two characters in "maskStr"
            String strDataStrOneChar;
            int resIdx = 0;

            while (maskIdx < maskLen && resIdx < maskLen)
            {
               if (maskStr[maskIdx] <= PICInterface.PIC_MAX_OP)
               {
                  if (dataIdx < dataStr.Length)
                  {
                     strDataStrOneChar = new String(dataStr, dataIdx, 1);

                     if (UtilStrByteMode.lenB(strDataStrOneChar) == 2)
                     // if DBCS
                     {
                        if (maskIdx + 1 < maskLen)
                        {
                           // a DBCS needs two consecutive alpha positional directives
                           if (maskStr[maskIdx + 1] <= PICInterface.PIC_MAX_OP)
                           {
                              if (convertCase)
                                 resStr[resIdx++] = caseConvertedChar(dataStr[dataIdx++], maskStr[maskIdx + 1]);
                              else
                                 resStr[resIdx++] = dataStr[dataIdx++];
                              maskIdx++;
                           }
                           // only one (not enough for DBCS)
                           else
                           {
                              resStr[resIdx++] = ' ';
                           }
                        }
                        // end of the mask
                        else
                        {
                           resStr[resIdx++] = ' ';
                        }
                     }
                     // if SBCS
                     else
                     {
                        if (convertCase)
                           resStr[resIdx++] = caseConvertedChar(dataStr[dataIdx++], maskStr[maskIdx + 1]);
                        else
                           resStr[resIdx++] = dataStr[dataIdx++];
                     }
                  }
                  else
                  {
                     resStr[resIdx++] = ' ';
                  }
               }
               else
               {
                  resStr[resIdx++] = maskStr[maskIdx];
               }

               maskIdx++;
            }

            return new String(resStr).Substring(0, resIdx);
         }

         while (maskIdx < maskLen)
         {
            if (maskStr[maskIdx] <= PICInterface.PIC_MAX_OP)
            {
               if (dataIdx < dataStr.Length)
               {
                  resStr[maskIdx] = dataStr[dataIdx];
                  if (convertCase)
                     resStr[maskIdx] = caseConvertedChar(dataStr[dataIdx], maskStr[maskIdx]);
                  else
                     resStr[maskIdx] = dataStr[dataIdx];
                  dataIdx++;
               }
               else
                  resStr[maskIdx] = ' ';
            }
            else
               resStr[maskIdx] = maskStr[maskIdx];
            maskIdx++;
         }

         return new String(resStr);
      }

      /// <summary>
      ///   Build a display string for an date value according to the picture
      /// </summary>
      /// <param name = "dataStr">   The source data</param>
      /// <param name = "picStr">    The picture to use for the value to display</param>
      /// <returns> the string to be displayed</returns>
      public String fromDate(String dataStr, PIC pic, int compIdx, bool time_date_pic_Z_edt)
      {
         int maskLen;
         String maskStr;
         String resStr = "";
         int date;

         maskStr = pic.getMask();
         maskLen = pic.getMaskSize();
         date = (new NUM_TYPE(dataStr)).NUM_2_ULONG();

         resStr = to_a_pic_datemode(maskStr, maskLen, date, pic, time_date_pic_Z_edt, false, compIdx);
         return resStr;
      }

      /// <summary>
      ///   Handle date value
      /// </summary>
      /// <param name = "outStr">          The mask to be used
      /// </param>
      /// <param name = "out_len">         Mask length
      /// </param>
      /// <param name = "pic">             The picure information
      /// </param>
      /// <param name = "date_pic_Z_edt">
      /// </param>
      /// <param name = "ignore_dt_fmt">
      /// </param>
      /// <returns> the string to be displayed
      /// </returns>
      protected internal String to_a_pic_datemode(String outStr, int out_len, int date, PIC pic, bool date_pic_Z_edt,
                                                  bool ignore_dt_fmt, int compIdx)
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int len;
         int day = 0;
         int doy = 0;
         int dow = 0;
         int year = 0;
         int month = 0;
         int hday = 0;
         int hyear = 0;
         int hmonth = 0;
         Boolean leap = false; // Used only for hebrew
         int i;
         char[] p;

         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         char[] outVal = outStr.ToCharArray();
         DateBreakParams breakParams;
         DateNameParams dateNameParams;
         int outIdx = 0;

         //-----------------------------------------------------------------------
         // If result overflows return buffer, fill with asterixs and bail out
         //-----------------------------------------------------------------------
         if (pic.getMaskSize() > out_len)
         {
            // memset (out, '*', out_len); // Original Line
            for (i = 0; i < out_len; i++)
               outVal[i] = '*';
            return new String(outVal);
         }

         //-----------------------------------------------------------------------
         // If zero fill requested and date value is zero, fill it and bail out
         //-----------------------------------------------------------------------
         out_len = len = pic.getMaskSize();

         if (pic.zeroFill() && date == 0 && !date_pic_Z_edt)
         {
            // memset (out, pic->zero, out_len); // Original Line
            for (i = 0; i < out_len; i++)
               outVal[i] = pic.getZeroPad();
            return new String(outVal);
         }

         if (pic.isHebrew() && (date == 0 || date == 1000000000L))
         {
            // memset (out, ' ', out_len);  // Original Line
            for (i = 0; i < out_len; i++)
               outVal[i] = ' ';
            return new String(outVal);
         }

         //-----------------------------------------------------------------------
         // Break data into its components
         //-----------------------------------------------------------------------
         breakParams = new DateBreakParams(year, month, day, doy, dow);
         date_break_datemode(breakParams, date, ignore_dt_fmt, compIdx);
         if (pic.isHebrew())
            leap = HebrewDate.dateheb_4_d(date, out hday, out hmonth, out hyear);

         year = breakParams.year;
         month = breakParams.month;
         day = breakParams.day;
         doy = breakParams.doy;
         dow = breakParams.dow;
         breakParams = null;
         
         //-----------------------------------------------------------------------
         // Loop on the picture mask
         //-----------------------------------------------------------------------
         while (len > 0)
         {
            switch (outVal[outIdx])
            {
               case (char)(PICInterface.PIC_YY):
                  if (pic.zeroFill() && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 2);
                  else
                     int_2_a(outVal, outIdx, 2, year, '0');
                  outIdx += 2;
                  len -= 2;
                  break;

               case (char)(PICInterface.PIC_HYYYYY):
                  i = HebrewDate.dateheb_2_str(outVal, outIdx, len, ref out_len, 5, hyear, pic.isTrimed(), true);
                  outIdx += i;
                  len -= i;
                  break;

               case (char)(PICInterface.PIC_HL):
                  i = HebrewDate.dateheb_i_2_h((hyear / 1000) % 10, outVal, outIdx, false, false);
                  outIdx++;
                  len--;
                  break;

               case (char)(PICInterface.PIC_HDD):
                  i = HebrewDate.dateheb_2_str (outVal, outIdx, len, ref out_len, 2, hday, pic.isTrimed(), false);
                  outIdx += i;
                  len -= i;
                  break;

               case (char)(PICInterface.PIC_YYYY):
                  if (pic.zeroFill() && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 4);
                  else
                     int_2_a(outVal, outIdx, 4, year, '0');
                  outIdx += 4;
                  len -= 4;
                  break;

               case (char)(PICInterface.PIC_MMD):
                  if (pic.zeroFill() && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 2);
                  else
                     int_2_a(outVal, outIdx, 2, month, '0');
                  outIdx += 2;
                  len -= 2;
                  break;

               case (char)(PICInterface.PIC_MMM):
                  if (pic.isHebrew())
                  {
                     if (leap && hmonth == 6)
                        hmonth = 14;
                     String[] monthNames = HebrewDate.GetLocalMonths();
                     p = monthNames[hmonth].ToCharArray();
                  }
                  else
                  {
                     String monthStr = Events.GetMessageString(MsgInterface.MONTHS_PTR);
                     String[] monthNames = DateUtil.getLocalMonths(monthStr);
                     p = monthNames[month].ToCharArray();
                  }
                  dateNameParams = new DateNameParams(outVal, outIdx, len);
                  out_len -= (int)date_i_2_nm(dateNameParams, p, pic.isTrimed());
                  outIdx = dateNameParams.outIdx;
                  len = dateNameParams.len;
                  dateNameParams = null;
                  break;

               case (char)(PICInterface.PIC_DD):
                  bool isZero = pic.zeroFill();
                  if (isZero && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 2);
                  else
                     int_2_a(outVal, outIdx, 2, day, '0');
                  outIdx += 2;
                  len -= 2;
                  break;

               case (char)(PICInterface.PIC_DDD):
                  if (pic.zeroFill() && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 3);
                  else
                     int_2_a(outVal, outIdx, 3, doy, '0');
                  len -= 3;
                  outIdx += 3;
                  break;

               case (char)(PICInterface.PIC_DDDD):
                  i = date_2_DDDD(day, outVal, outIdx, len, pic.isTrimed());
                  outIdx += i;
                  out_len += i - 4;
                  len -= 4;
                  break;


               case (char)(PICInterface.PIC_W):
                  if (pic.zeroFill() && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 1);
                  else
                     int_2_a(outVal, outIdx, 1, dow, '0');
                  len--;
                  outIdx++;
                  break;

               case (char)(PICInterface.PIC_WWW):
                  if (pic.isHebrew())
                  {
                     String[] dowsNames = HebrewDate.GetLocalDows();
                     p = dowsNames[dow].ToCharArray();
                  }
                  else
                  {
                     String dayStr = Events.GetMessageString(MsgInterface.DAYS_PTR);
                     String[] dayNames = DateUtil.getLocalDays(dayStr);
                     p = dayNames[dow].ToCharArray();
                  }
                  dateNameParams = new DateNameParams(outVal, outIdx, len);
                  out_len -= (int)date_i_2_nm(dateNameParams, p, pic.isTrimed());
                  outIdx = dateNameParams.outIdx;
                  len = dateNameParams.len;
                  dateNameParams = null;
                  break;


               case (char)(PICInterface.PIC_BB): // JPN: Japanese date picture support

                  // Japanese a day of the week
                  p = UtilDateJpn.getStrDow(dow).ToCharArray();
                  String strOut = null;

                  dateNameParams = new DateNameParams(outVal, outIdx, len);

                  strOut = date_i_2_nm_bytemode(strOut, dateNameParams, new String(p), out_len);

                  outVal = null;
                  outVal = new char[strOut.Length];
                  strOut.CopyTo(0, outVal, 0, strOut.Length);

                  outIdx = dateNameParams.outIdx;
                  len = dateNameParams.len;

                  strOut = null;
                  dateNameParams = null;

                  break;


               case (char)(PICInterface.PIC_JY1):
               // JPN: Japanese date picture support
               case (char)(PICInterface.PIC_JY2):
               case (char)(PICInterface.PIC_JY4):

                  // Japanese the name of an era
                  if (outVal[outIdx] == PICInterface.PIC_JY1)
                     p = UtilDateJpn.getInstance().date_jpn_yr_2_a(year, doy, false).ToCharArray();
                  else
                     p = UtilDateJpn.getInstance().date_jpn_yr_2_a(year, doy, true).ToCharArray();
                  strOut = null;

                  dateNameParams = new DateNameParams(outVal, outIdx, len);

                  strOut = date_i_2_nm_bytemode(strOut, dateNameParams, new String(p), out_len);

                  outVal = null;
                  outVal = new char[strOut.Length];
                  strOut.CopyTo(0, outVal, 0, strOut.Length);

                  outIdx = dateNameParams.outIdx;
                  len = dateNameParams.len;

                  strOut = null;
                  dateNameParams = null;
                  break;


               case (char)(PICInterface.PIC_YJ): // JPN: Japanese date picture support

                  // Japanese a year of the era
                  if (pic.zeroFill() && date == 0 && date_pic_Z_edt)
                     char_memset(outVal, outIdx, pic.getZeroPad(), 2);
                  else
                     int_2_a(outVal, outIdx, 2, UtilDateJpn.getInstance().date_jpn_year_ofs(year, doy), '0');
                  outIdx += 2;
                  len -= 2;
                  break;


               default:
                  outIdx++;
                  len--;
                  break;
            }
         }

         // Qcr #774363 : if out_len is shorter then the rest is junk. same as online ...
         // if (len < StrLen)
         //    memset_U (Str + len, CHAR_U(' '), (StrLen - len));
         if (out_len < outVal.Length)
            char_memset(outVal, out_len, ' ', outVal.Length - out_len);

         return new String(outVal);
      }


      /// <summary>
      ///   Break a date into Year, Month, Day in month, Day in year and Day in week
      /// </summary>
      public void date_break_datemode(DateBreakParams breakParams, int date, bool ignore_dt_fmt, int compIdx)
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int cent4;
         int cent;
         int year4;
         int leapyear;
         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         int year = breakParams.year;
         int month = breakParams.month;
         int day = breakParams.day;
         int doy = breakParams.doy;
         int dow = breakParams.dow;

         if (date <= 0)
         {
            breakParams.day = 0;
            breakParams.doy = 0;
            breakParams.dow = 0;
            breakParams.year = 0;
            breakParams.month = 0;
            return;
         }
         dow = ((date % 7) + 1);
         date--;
         cent4 = (date / PICInterface.DAYSINFOURCENT);
         date = date - (cent4 * PICInterface.DAYSINFOURCENT);
         cent = (date / PICInterface.DAYSINCENTURY);
         if (cent > 3)
            cent = 3;
         date = date - (cent * PICInterface.DAYSINCENTURY);
         year4 = (date / PICInterface.DAYSINFOURYEAR);
         date = date - (year4 * PICInterface.DAYSINFOURYEAR);
         year = (date / PICInterface.DAYSINYEAR);
         if (year > 3)
            year = 3;
         date = date - (year * PICInterface.DAYSINYEAR);
         year = (cent4 * 400) + (cent * 100) + (year4 * 4) + year + 1;
         leapyear = 0;
         if ((year % 4) == 0)
            if ((year % 100) != 0 || (year % 400) == 0)
               leapyear = 1;
         if (!ignore_dt_fmt && (_environment.GetDateMode(compIdx) == 'B'))
            year += PICInterface.DATE_BUDDHIST_GAP;
         month = ((date / PICInterface.DAYSINMONTH) + 1);
         day = PICInterface.date_day_tab[month];
         if (leapyear != 0)
            if (month > 1)
               (day)++; /* 04-11-89 */
         if (date >= day)
            (month)++;
         else
         {
            day = PICInterface.date_day_tab[month - 1];
            if (leapyear != 0)
               if (month > 2)
                  (day)++;
         }
         day = date - day + 1;
         doy = date + 1;

         breakParams.year = year;
         breakParams.month = month;
         breakParams.day = day;
         breakParams.doy = doy;
         breakParams.dow = dow;
      }

      /// <summary>
      ///   get alpha string from int value
      /// </summary>
      /// <param name = "str">     the output string (as an char array)
      /// </param>
      /// <param name = "strPos">  starting position
      /// </param>
      /// <param name = "len">     string length
      /// </param>
      /// <param name = "n">       the number to convert
      /// </param>
      /// <param name = "lead">    leading character
      /// </param>
      /// <returns> the result string length
      /// </returns>
      private int int_2_a(char[] str, int strPos, int len, int n, char lead)
      {
         int pos;
         bool neg = (n < 0);

         if (len <= 0)
            return 0;

         n = Math.Abs(n);
         pos = len;
         do
         {
            str[strPos + (--pos)] = (char)('0' + (n % 10));
            n /= 10;
         } while (pos > 0 && n != 0);
         if (neg && pos > 0)
            str[strPos + (--pos)] = '-';
         return (lib_a_fill(str, strPos, len, pos, lead));
      }

      /// <summary>
      ///   Fill the char array for the converting of int->string
      /// </summary>
      /// <param name = "str">     the output string (as an char array)
      /// </param>
      /// <param name = "strPos">  starting position
      /// </param>
      /// <param name = "len">     string length
      /// </param>
      /// <param name = "lead">    leading character
      /// </param>
      /// <returns> the result string length
      /// </returns>
      private int lib_a_fill(char[] str, int strPos, int len, int pos, int lead)
      {
         if (lead == 0)
         {
            len -= pos;
            if (len > 0 && pos > 0)
            {
               char_memcpy(str, strPos, str, pos, len);
               char_memset(str, strPos + len, ' ', pos);
            }
         }
         else
         {
            if (pos > 0)
               char_memset(str, strPos, (char)lead, pos);
         }
         return (len);
      }

      /// <summary>
      ///   Convert index to Name
      /// </summary>
      /// <param name = "dateNameParams">  output sting details
      /// </param>
      /// <param name = "nm">              name
      /// </param>
      /// <param name = "trim">            output string should be trimed
      /// </param>
      /// <returns> the length of the string
      /// </returns>
      private long date_i_2_nm(DateNameParams dateNameParams, char[] nm, bool trim)
      {
         int n;
         int l;

         n = date_msk_cnt(dateNameParams.outVal, dateNameParams.outIdx, dateNameParams.len);
         if (trim)
            l = mem_trim(nm, 0, n);
         else
            l = n;

         l = Math.Min(l, dateNameParams.len);

         char_memcpy(dateNameParams.outVal, dateNameParams.outIdx, nm, 0, l);
         dateNameParams.outIdx += l;
         dateNameParams.len -= n;

         //-----------------------------------------------------------------------
         // Trimed ? Jump other trimed mask : nothing
         //-----------------------------------------------------------------------
         n -= l;
         if (n != 0)
            // memcpy (*out, ((*out) + n), *len); // Original Line
            char_memcpy(dateNameParams.outVal, dateNameParams.outIdx, dateNameParams.outVal, dateNameParams.outIdx + n,
                        dateNameParams.len);

         return (n);
      }

      /// <summary>
      ///   JPN: Japanese date picture support
      ///   Convert index to Name
      /// </summary>
      /// <param name = "strOut">          output sting (original)
      /// </param>
      /// <param name = "dateNameParams">  output sting details
      /// </param>
      /// <param name = "strNm">           name which will be embedded in output string
      /// </param>
      /// <param name = "intOutLen">       the remaining length of the output string
      /// </param>
      /// <returns>                 output sting (processed)
      /// </returns>
      private String date_i_2_nm_bytemode(String strOut, DateNameParams dateNameParams, String strNm, int intOutLen)
      {
         int intMskCnt = date_msk_cnt(dateNameParams.outVal, dateNameParams.outIdx, dateNameParams.len);

         String strWork = new String(dateNameParams.outVal);
         strWork = strWork.Substring(0, dateNameParams.outIdx);

         int intOfs = UtilStrByteMode.lenB(strWork);

         strNm = UtilStrByteMode.leftB(strNm, intMskCnt);

         strOut = UtilStrByteMode.repB(new String(dateNameParams.outVal), strNm, intOfs + 1, intMskCnt);

         dateNameParams.outIdx += strNm.Length;
         dateNameParams.len -= intMskCnt;

         intOutLen -= intMskCnt;
         return (strOut);
      }

      /// <summary>
      ///   Return N of consecutive Mask characters
      /// </summary>
      /// <param name = "msk">     the mask to check
      /// </param>
      /// <param name = "mskPos">  index of first char to check
      /// </param>
      /// <param name = "len">     length of the mask
      /// </param>
      /// <returns> number of consecutive Mask characters
      /// </returns>
      private int date_msk_cnt(char[] msk, int mskPos, long len)
      {
         int i;
         char c;

         c = msk[mskPos++];
         for (i = 1; i < len; mskPos++, i++)
            if (msk[mskPos] != c)
               break;
         return (i);
      }

      /// <summary>
      ///   Compute Day name
      /// </summary>
      private int date_2_DDDD(int day, char[] outVal, int outIdx, int len, bool trim)
      {
         int ret = 4;
         char[] ext;

         if (day > 9)
         {
            int_2_a(outVal, outIdx, 2, day, (char)0);
            outIdx += 2;
         }
         else
         {
            if (trim)
            {
               char_memcpy(outVal, outIdx, outVal, outIdx + 1, len - 1); /*17-May-90*/
               ret--;
            }
            else
            {
               // *(out++) = ' '; // Original Line
               outVal[outIdx++] = ' ';
            }
            int_2_a(outVal, outIdx, 1, day, (char)0);
            outIdx++;
         }
         switch (day)
         {
            case 1:
            case 21:
            case 31:
               ext = ("st").ToCharArray();
               break;

            case 2:
            case 22:
               ext = ("nd").ToCharArray();
               break;

            case 3:
            case 23:
               ext = ("rd").ToCharArray();
               break;

            default:
               ext = ("th").ToCharArray();
               break;
         }
         outVal[outIdx++] = ext[0];
         outVal[outIdx] = ext[1];

         return (ret);
      }

      /// <summary>
      ///   Build a display string for an time value according to the picture
      /// </summary>
      /// <param name = "dataStr">   The source data</param>
      /// <param name = "picStr">    The picture to use for the value to display</param>
      /// <returns> the string to be displayed</returns>
      public String fromTime(String dataStr, PIC pic, bool time_date_pic_Z_edt)
      {
         int maskLen;
         String maskStr;
         String resStr = "";
         int time;

         maskStr = pic.getMask();
         maskLen = pic.getMaskSize();
         time = (new NUM_TYPE(dataStr)).NUM_2_ULONG();

         resStr = time_2_a_pic(maskStr, maskLen, time, pic, time_date_pic_Z_edt, false);
         return resStr;
      }

      /// <summary>
      ///   Handle time value
      /// </summary>
      /// <param name = "AsciiStr">        The mask to be used</param>
      /// <param name = "AsciiL">          Mask length</param>
      /// <param name = "pic">             The picure information</param>
      /// <param name = "time_pic_Z_edt"></param>
      /// <param name = "seconds">/ milliSeconds flag</param>
      /// <returns> the string to be displayed
      /// </returns>
      private String time_2_a_pic(String AsciiStr, int AsciiL, int time, PIC pic, bool time_pic_Z_edt, bool milliSeconds)
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int am; // Display time in AM format
         int len; // Length of picture mask
         int hour = 0;
         int minute = 0;
         int second = 0;
         int millisec = 0;
         int timeSec = 0;
         int HourI; // Location of hour value in the picture mask
         int I;
         int inx;
         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         char[] Ascii = AsciiStr.ToCharArray();
         TimeBreakParams breakParams;


         breakParams = new TimeBreakParams(hour, minute, second);
         if (milliSeconds)
         {
            timeSec = time / 1000;
            millisec = time - timeSec * 1000;
         }
         else
            timeSec = time;
         time_break(breakParams, timeSec);
         hour = breakParams.hour;
         minute = breakParams.minute;
         second = breakParams.second;

         //-----------------------------------------------------------------------
         // If ascii representation overflows output buffer
         // fill with '*' and bail out
         //-----------------------------------------------------------------------
         if (pic.getMaskSize() > AsciiL)
         {
            char_memset(Ascii, 0, '*', AsciiL);
            return new String(Ascii);
         }

         //-----------------------------------------------------------------------
         // If zero fill specified and time is zero, fill in zero char instead of
         // data
         //-----------------------------------------------------------------------
         if (pic.zeroFill())
            if (timeSec == 0 && (!milliSeconds || millisec == 0))
               if (time_pic_Z_edt)
               {
                  for (inx = 0; inx < pic.getMaskSize(); inx++)
                     if (Ascii[inx] <= PICInterface.PIC_MAX_OP)
                        Ascii[inx] = pic.getZeroPad();

                  return new String(Ascii);
               }
               else
               {
                  char_memset(Ascii, 0, pic.getZeroPad(), pic.getMaskSize());
                  return new String(Ascii);
               }
         //-----------------------------------------------------------------------
         // Intialize counters and flags
         //-----------------------------------------------------------------------
         I = 0;
         am = 0;
         len = pic.getMaskSize();
         HourI = -1;

         while (I < len)
         {
            switch (Ascii[I])
            {
               case (char)(PICInterface.PIC_HH):
                  HourI = I;
                  I += 2;
                  break;


               case (char)(PICInterface.PIC_MMT):
                  int_2_a(Ascii, I, 2, minute, '0');
                  I += 2;
                  break;

               case (char)(PICInterface.PIC_SS):
                  int_2_a(Ascii, I, 2, second, '0');
                  I += 2;
                  break;

               case (char)(PICInterface.PIC_MS):
                  int_2_a(Ascii, I, 3, millisec, '0');
                  I += 3;
                  break;

               case (char)(PICInterface.PIC_PM):
                  am = 1;
                  int tmpHour = hour % 24;
                  if (tmpHour < 12 || tmpHour == 0)
                     char_memcpy(Ascii, I, "am".ToCharArray(), 0, 2);
                  else
                     char_memcpy(Ascii, I, "pm".ToCharArray(), 0, 2);
                  I += 2;
                  break;

               default:
                  I++;
                  break;
            }
         }

         //-----------------------------------------------------------------------
         // Embed hour value
         //-----------------------------------------------------------------------
         if (HourI >= 0)
         {
            if (am != 0)
            {
               hour %= 24;
               if (hour == 0)
                  hour = 12;
               else
               {
                  hour %= 12;
                  if (hour == 0)
                     hour = 12;
               }
            }
            int_2_a(Ascii, HourI, 2, hour, (am != 0 && hour < 10 ? ' ' : '0'));
         }

         return new String(Ascii);
      }

      /// <summary>
      ///   Break time into its components
      /// </summary>
      public static void time_break(TimeBreakParams breakParams, int time)
      {
         if (time <= 0)
         {
            breakParams.second = 0;
            breakParams.minute = 0;
            breakParams.hour = 0;
         }
         else
         {
            breakParams.hour = (time / 3600);
            time = time - (breakParams.hour * 3600);
            breakParams.minute = (time / 60);
            breakParams.second = time - breakParams.minute * 60;
         }
      }

      /// <summary>
      ///   Build a display string for a logical value according to the picture
      /// </summary>
      /// <param name = "dataStr">   "0" for false or "1" for true
      /// </param>
      /// <param name = "picStr">    The picture to use for the value to display
      /// </param>
      /// <param name = "rangeStr">  The range of values to check
      /// </param>
      /// <returns> the string to be displayed
      /// </returns>
      private String fromLogical(String dataStr, PIC pic, String rangeStr)
      {
         int len;
         char[] Str;
         char[] data;
         String resStr;

         Str = pic.getMask().ToCharArray();

         //-----------------------------------------------------------------------
         // If picture overflows string, reset with '*'
         //-----------------------------------------------------------------------
         if (pic.getMaskSize() > Str.Length)
         {
            char_memset(Str, 0, '*', Str.Length);
            return new String(Str);
         }

         //-----------------------------------------------------------------------
         // Crack range and copy the data
         //-----------------------------------------------------------------------
         data = new char[50];
         win_rng_bool_sub(data, data.Length, (dataStr[0] == '1'), rangeStr);
         len = pic.getMaskSize();
         resStr = win_data_cpy(Str, len, data, pic.isAttrAlpha());
         // Original Code - not relevant in java because the result is allways the mask len
         // if (len < StrLen)
         // char_memset (Str, len, ' ', StrLen - len);

         return resStr;
      }

      /// <summary>
      ///   Get a boolean value from the range specified
      /// </summary>
      /// <param name = "Sub">      the result string</param>
      /// <param name = "SubSiz">   the result string length</param>
      /// <param name = "BoolVal">  '1' if true or '0' if false</param>
      /// <param name = "rng">      range of values</param>
      private void win_rng_bool_sub(char[] Sub, int SubSiz, bool BoolVal, String rng)
      {
         int len;
         int s;
         char[] crk = new char[PICInterface.DB_STR_MAX + 2];
         char[] gap = new char[PICInterface.DB_STR_MAX + 2];
         int i = 0;

         //-----------------------------------------------------------------------
         // Set the default range if none is specified
         //-----------------------------------------------------------------------
         rng = win_rng_bool(rng);

         //-----------------------------------------------------------------------
         // Crack range and choose the first element as True value
         // or the second element as the False value.
         //-----------------------------------------------------------------------
         win_rng_crk(crk, gap, rng);
         if (BoolVal)
            s = 0;
         else
         {
            while (crk[i] != 0)
               i++;
            s = i + 1;
         }
         len = s;
         while (crk[len] != 0)
            len++;
         len = len - s;
         char_memcpy(Sub, 0, crk, s, Math.Min(len, SubSiz));
         if (len < SubSiz)
            char_memset(Sub, len, ' ', SubSiz - len);
      }

      /// <summary>
      ///   Get the default range for logical values
      /// </summary>
      /// <param name = "rng">  current range</param>
      /// <returns> the default range if the input range is empty</returns>
      private String win_rng_bool(String rng)
      {
         if (rng == null || rng.Length == 0)
         {
            /*if (heb)
            rng = "True, False";
            else*/
            rng = "True, False";
         }
         return rng;
      }

      /// <summary>
      ///   Crack a 'range' string into its components
      /// </summary>
      private void win_rng_crk(char[] crk, char[] gap, String range)
      {
         int rngLen = range.Length;
         char[] rng = range.ToCharArray();
         int crkPos = 0;
         int rngPos = 0;
         int gapPos = 0;

         while (rngPos < rngLen)
         {
            if (rng[rngPos] == ',' || rng[rngPos] == '-')
            {
               gap[gapPos++] = rng[rngPos++];
               crk[crkPos++] = (char)(0);
               if ((rngPos < rngLen))
                while (rng[rngPos] == ' ')
                    rngPos++;
            }
            else
            {
               //-----------------------------------------------------------------
               // 05/10/95 - Mr. Takada :
               // Japanese char may have '\' code of its second byte.
               // str_chr_1_byt needs mglocal.h.
               //according to Mr. Maruoka:
               //-----------------------------------------------------------------
               if (rng[rngPos] == '\\')
                  //&& str_chr_1_byt (org_rng, (long)(rng-org_rng)))
                  rngPos++;

               crk[crkPos] = rng[rngPos++];
               crkPos++;
            }
         }
         //-----------------------------------------------------------------------
         // JPN: 01/11/95 Yama*/
         // Upper function is implemented in str.c and
         // it is also overrided by mglocal function.
         // So MAGIC needs to call this function
         // instead of CST_hdr.uppr_tab[].
         //-----------------------------------------------------------------------
         // if (up)
         // 	str_upper (org_crk, (long)(crk-org_crk));

         gap[gapPos] = (char)(0);
         crk[crkPos++] = (char)(0);
         crk[crkPos] = (char)(0);
      }


      //--------------------------------------------------------------------------
      // Methods for coverting DIPLAY ---> INTERNAL
      //--------------------------------------------------------------------------

      /// <summary>
      ///   Translates a string number to magic number
      /// </summary>
      /// <param name = "dispValue">   A string number</param>
      /// <param name = "picStr">      The picture to use</param>
      /// <returns> the magic number</returns>
      public String toNum(String dispValue, PIC pic, int compIdx)
      {
         NUM_TYPE mgNum;
         String str;

         mgNum = new NUM_TYPE(dispValue, pic, compIdx);
         str = mgNum.toXMLrecord();

         return str;
      }

      /// <summary>
      ///   Translates a string date to magic number
      /// </summary>
      /// <param name = "dispValue">   A string value</param>
      /// <param name = "pic">         The picture to use</param>
      /// <returns> the magic number</returns>
      public String toDate(String dispValue, PIC pic, int compIdx)
      {
         String maskStr;
         int numVal;
         NUM_TYPE mgNum = new NUM_TYPE();
         String str, tmp;


         if (UtilStrByteMode.isLocaleDefLangJPN())
            dispValue = dispValue.TrimEnd(null);
         else
            dispValue = dispValue.Trim();

         //we are adding a little empty space to the variable, because we might later
         //want to add some data to the date ( for example move 2-digit year to 4-digit year)
         //and we assume that we have enough space for it
         tmp = String.Concat(dispValue, "       ");

         maskStr = pic.getMask();

         numVal = a_2_date_pic_datemode(tmp.ToCharArray(), dispValue.Length, pic, maskStr.ToCharArray(), compIdx);
         mgNum.NUM_4_LONG(numVal);
         str = mgNum.toXMLrecord();
         return str;
      }

      /// <summary>
      ///   builds the date magic number from the string value
      /// </summary>
      /// <param name = "str">            the date string</param>
      /// <param name = "str_len">        length of string</param>
      /// <param name = "pic">            the picture info</param>
      /// <param name = "mask">           the mask of the string</param>
      /// <param name = "ignore_dt_fmt"></param>
      /// <returns> the magic number</returns>
      public int a_2_date_pic_datemode(char[] str, int str_len, PIC pic, char[] mask, int compIdx)
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int pos = 0;
         bool usedoy;
         int len;
         int day;
         int doy;
         int year;
         int month;
         int i;
         int YearDigitsNo;
         int century_year;
         bool year_in_pic = false;
         bool month_in_pic = false;
         bool day_in_pic = false;
         bool inp = false;
         bool bYearIsZero = false;
         int era_year = 0; // should be set to as at first.
         int quotes;
         int millenium = 0;

         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         IntRef intRef = new IntRef(0);
         DateBreakParams dateParams;

         // A DBCS character in "str" consumes two characters in "mask".
         // If "str" contains DBCS, the position of "mask" has to skip next index.
         // This variable indicates the number of skipped indexes.
         int intPicIdxOfs = 0; // JPN: DBCS support

         len = Math.Min(str.Length, pic.getMaskSize());

#if PocketPC
         //added the define, so that TeamCity will be satisfied - since the PocketPC part failed.
#else
         if (str.Length < mask.Length)
            Array.Resize(ref str, mask.Length);
#endif

         year = 0;
         month = 1;
         day = 1;
         doy = 1;
         usedoy = false;

         for (i = 0; i < str_len; i++)
            if ((str[i] != '0') && (str[i] != ' '))
               break;
         if (i == str_len)
            return 0;

         while (pos < str_len && pos + intPicIdxOfs < len)
         {
            switch (mask[pos + intPicIdxOfs])
            {
               case (char)(PICInterface.PIC_YY):
                  year_in_pic = true;
                  inp = true;
                  year = date_a_2_i(str, 2, pos);
                  pos += 2;
                  // Hen 16/11/97 : Fix #768442
                  if (year == 0)
                     bYearIsZero = true;
                  century_year = _environment.GetCentury(compIdx) % 100; // DEF_century
                  if (year < century_year)
                     year += 100;
                  year += _environment.GetCentury(compIdx) - century_year; // DEF_century
                  if (_environment.GetDateMode(compIdx) == 'B')
                     // DEF_date_mode
                     year -= PICInterface.DATE_BUDDHIST_GAP;
                  break;


               case (char)(PICInterface.PIC_YYYY):
                  year_in_pic = true;
                  inp = true;
                  //--------------------------------------------------------------
                  // Make sure we have 4 year digit. If not, adjust it
                  // according to the 'century year'
                  //--------------------------------------------------------------
                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     for (YearDigitsNo = 0; YearDigitsNo < 4 && pos + YearDigitsNo < str.Length && UtilStrByteMode.isDigit(str[pos + YearDigitsNo]); YearDigitsNo++)
                     { }
                     i = YearDigitsNo;
                  }
                  else
                  {
                     for (i = 0; pos + i < str.Length && str[pos + i] != DATECHAR && i < 4; i++)
                     { }

                     for (YearDigitsNo = 0;
                          pos + YearDigitsNo < str.Length && UtilStrByteMode.isDigit(str[pos + YearDigitsNo]) &&
                          YearDigitsNo < 4;
                          YearDigitsNo++)
                     { }
                  }

                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     if (i < 4 && str.Length < mask.Length)
                        move_date(str, pos, 4 - i, str.Length);
                  }
                  else
                  {
                     if (i < 4 && len < mask.Length)
                     {
                        move_date(str, pos, 4 - i, len);
                        len = len + 4 - i;
                     }
                  }

                  year = date_a_2_i(str, 4, pos);
                  pos += 4;

                  // Hen 16/11/97 : Fix #768442
                  if (year == 0)
                     bYearIsZero = true;

                  if (YearDigitsNo <= 2)
                  {
                     century_year = _environment.GetCentury(compIdx) % 100; // DEF_century
                     if (year < century_year)
                        year += 100;
                     year += _environment.GetCentury(compIdx) - century_year; // DEF_century
                  }

                  if (_environment.GetDateMode(compIdx) == 'B')
                     // DEF_date_mode
                     year -= PICInterface.DATE_BUDDHIST_GAP;
                  break;


               case (char)(PICInterface.PIC_HYYYYY):
                  inp = true;
                  quotes = 0;
                  if (len > pos + 5 && mask[pos + 5] == (char)(PICInterface.PIC_HYYYYY))
                     quotes = 1;
                  year = HebrewDate.dateheb_h_2_i (str, 5 + quotes, pos);
                  pos += 5 + quotes;
                  if (year == 0)
                     bYearIsZero = true;
                  break;

               case (char)(PICInterface.PIC_HL):
                  millenium = HebrewDate.dateheb_h_2_i(str, 1, pos);
                  pos++;
                  break;

               case (char)(PICInterface.PIC_HDD):
                  inp = true;
                  quotes = 0;
                  if (len > pos + 2 && mask[pos + 2] == (char)(PICInterface.PIC_HDD))
                     quotes = 1;
                  day = HebrewDate.dateheb_h_2_i(str, 2 + quotes, pos);
                  pos += 2 + quotes;
                  break;

               case (char)(PICInterface.PIC_MMD):
                  inp = true;
                  month_in_pic = true;
                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     if ((str.Length == pos + 1 || str[pos + 1] == DATECHAR || !UtilStrByteMode.isDigit(str[pos + 1])) && str.Length < mask.Length)
                        move_date(str, pos, 1, str.Length);
                  }
                  else
                  {
                     if (str[pos + 1] == DATECHAR && len < mask.Length)
                     {
                        move_date(str, pos, 1, len);
                        len += 1;
                     }
                  }
                  month = date_a_2_i(str, 2, pos);
                  pos += 2;
                  break;


               case (char)(PICInterface.PIC_MMM):
                  inp = true;
                  month_in_pic = true;
                  intRef.val = pos;
                  month = date_MMM_2_m(str, mask, intRef, len, pic.isHebrew());
                  pos = intRef.val;
                  if (pic.isHebrew() && month == 14)
                     month = 6;
                  break;


               case (char)(PICInterface.PIC_DD):
                  inp = true;
                  day_in_pic = true;
                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     if ((str.Length == pos + 1 || str[pos + 1] == DATECHAR || !UtilStrByteMode.isDigit(str[pos + 1])) && str.Length < mask.Length)
                        move_date(str, pos, 1, str.Length);
                  }
                  else
                  {
                     if (str[pos + 1] == DATECHAR && len < mask.Length)
                     {
                        move_date(str, pos, 1, len);
                        len += 1;
                     }
                  }
                  day = date_a_2_i(str, 2, pos);
                  pos += 2;
                  break;


               case (char)(PICInterface.PIC_DDD):
                  inp = true;
                  usedoy = true;
                  day_in_pic = true;
                  doy = date_a_2_i(str, 3, pos);
                  pos += 3;
                  break;


               case (char)(PICInterface.PIC_DDDD):
                  inp = true;
                  day_in_pic = true;
                  day = date_a_2_i(str, 2, pos);
                  pos += 2 + 2;
                  break;


               case (char)(PICInterface.PIC_YJ): // JPN: Japanese date picture support
                  year_in_pic = true;
                  inp = true;
                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     if ((str.Length == pos + 1 || str[pos + 1] == DATECHAR || !UtilStrByteMode.isDigit(str[pos + 1])) && str.Length < mask.Length)
                        move_date(str, pos, 1, str.Length);
                  }
                  year = date_a_2_i(str, 2, pos);
                  pos += 2;

                  era_year = UtilDateJpn.getInstance().getStartYearOfEra(new String(str), pic.getMask());
                  if (era_year == 0)
                     return 0;
                  year += era_year - 1;

                  if (year == 0)
                     bYearIsZero = true;
                  break;


               default:
                  if (UtilStrByteMode.isLocaleDefLangDBCS())
                  {
                     // if "str[pos]" contains DBCS, the next index of "mask" has to be skipped.
                     if (UtilStrByteMode.isHalfWidth(str[pos]) == false &&
                         UtilStrByteMode.isHalfWidth(mask[pos + intPicIdxOfs]))
                        intPicIdxOfs++;
                  }

                  pos += 1;
                  break;
            }
         }

         if (bYearIsZero)
         {
            if (!month_in_pic)
               month = 0;
            if (!day_in_pic && month == 0)
               day = 0;
         }

         //-----------------------------------------------------------------------
         // Hen 16/11/97 : Fix #768442
         // Zero date is valid and should return 0L. This check must be here
         // because the year value may be changed because of century year.
         //-----------------------------------------------------------------------
         if (day == 0 && month == 0 && (bYearIsZero || year == 0))
            return (0);

         if (!inp)
            return (0);

         //-----------------------------------------------------------------------
         // Hebrew Year
         //-----------------------------------------------------------------------
         if (pic.isHebrew())
         {
            if (year > 0)
            {
               if (millenium == 0)
                  millenium = 5;
               year += millenium * 1000;
            }
            return (HebrewDate.dateheb_2_d (year, month, day));
         }

         //-----------------------------------------------------------------------
         // the next 2 lines support 4.xx DD/MM pictures
         //-----------------------------------------------------------------------
         if ((year == 0) && !year_in_pic)
         {
            dateParams = date_sys();
            year = dateParams.year;
         }
         return (date_4_calender(year, month, day, doy, usedoy));
      }

      /// <summary>
      ///   Decode digits from a date string
      /// </summary>
      private int date_a_2_i(char[] s, int len, int pos)
      {
         int i;
         char[] tmp = new char[len];
         NUM_TYPE mgNum = new NUM_TYPE();

         for (i = 0; i < len; i++)
            tmp[i] = s[pos + i];
         return (mgNum.a_2_long(new String(tmp)));
      }

      /// <summary>
      ///   moving part of the date string
      /// </summary>
      private void move_date(char[] str, int pos, int moveby, int len)
      {
         char_memmove(str, pos + moveby, str, pos, len - (pos + moveby) + 1);
         char_memset(str, pos, '0', moveby);
      }

      /// <summary>
      ///   Translate a month name -> month index
      /// </summary>
      /// <param name = "string">to evaluate
      /// </param>
      /// <param name = "picture">mask
      /// </param>
      /// <param name = "pos">current position in the string the intRef not exactly the same reference to integer , like in C++,
      ///   so the global pos MUST be updated by the pos.value after the function returns.
      /// </param>
      private int date_MMM_2_m(char[] str, char[] mask, IntRef pos, int len, bool isHebrew)
      {
         int i;
         int l;
         int strPos = 0;
         String monthStr;

         if (UtilStrByteMode.isLocaleDefLangJPN())
         {
            int maskPos = UtilStrByteMode.convPos(new String(str), new String(mask), pos.val, false);
            l = date_msk_cnt(mask, maskPos, len - maskPos);
         }
         else
         {
            l = date_msk_cnt(mask, pos.val, len - pos.val);
         }

         strPos = pos.val;
         pos.val += l;

         len = mem_trim(str, strPos, l);
         if (len == 0)
            return (0);

         //-----------------------------------------------------------------------
         // the first month is blank
         //-----------------------------------------------------------------------
         monthStr = new String(str, strPos, len);
         String months;
         String[] monthNames;

         if (isHebrew)
         {
            monthNames = HebrewDate.GetLocalMonths();
         }
         else
         {
            months = Events.GetMessageString(MsgInterface.MONTHS_PTR);
            monthNames = DateUtil.getLocalMonths(months);
         }
         for (i = 1; i < monthNames.Length; i++)
            if (String.Compare(monthNames[i].Substring(0, len), monthStr, true) == 0)
               return (i);
         return (0);
      }

      /// <summary>
      ///   Get the curret daye seperated to year, month, day
      /// </summary>
      protected internal DateBreakParams date_sys()
      {
         DateBreakParams date = new DateBreakParams();
         DateTime CurrDate = DateTime.Now;

         date.year = CurrDate.Year;
         date.month = CurrDate.Month;
         date.day = CurrDate.Day;

         return date;
      }

      /// <summary>
      ///   to get current date in magic representation Query Magic's Date
      /// </summary>
      /// <returns> the 'Magic' current date
      /// </returns>
      public int date_magic(bool utcDate)
      {
         int year, month, day;
         DateTime CurrDate;

         if (utcDate)
            CurrDate = DateTime.UtcNow;
         else
            CurrDate = DateTime.Now;

         year = CurrDate.Year;
         month = CurrDate.Month;
         day = CurrDate.Day;

         return date_4_calender(year, month, day, 1, false);
      }

      /// <summary>
      ///   to get  the system time
      /// </summary>
      /// <returns> the 'Magic' current time
      /// </returns>
      public int time_magic(bool utcTime)
      {
         int TotalSec;
         TimeSpan ts;

         if (utcTime)
            ts = DateTime.UtcNow.TimeOfDay;
         else
            ts = DateTime.Now.TimeOfDay;         

         TotalSec = (int)ts.TotalSeconds;
         return TotalSec;
      }

      /// <summary>
      ///   return  number of milliseconds 
      ///   form midnight to current time
      /// </summary>
      public int mtime_magic(bool utcTime)
      {
         int TotalMiliSec;
         TimeSpan ts;

         if (utcTime)
            ts = DateTime.UtcNow.TimeOfDay;
         else
            ts = DateTime.Now.TimeOfDay; 

         TotalMiliSec = (int)ts.TotalMilliseconds;
         return TotalMiliSec;
      }

      /// <summary>
      ///   Compute MAGIC date
      /// </summary>
      public int date_4_calender(int year, int month, int day, int doy, bool usedoy)
      {
         int Cent;
         int Cent4;
         int Year4;
         bool LeapYear;
         int PrevDays;
         int CurrDays;


         LeapYear = (year % 4 == 0) && ((year % 100 != 0) || (year % 400 == 0));

         //-----------------------------------------------------------------------
         // 3.11.97 - Hen (Fixed bug #764265 and #8186) :
         // If the date is 0 then return 0L (zero value is valid), else if the date
         // is illegal then return 1000000000L (wrong date).
         //-----------------------------------------------------------------------
         if (usedoy)
         {
            if (doy == 0)
               return (0);
            else if ((doy > 366) || ((!LeapYear) && (doy > 365)))
               return (1000000000);
         }
         else
         {
            if (day == 0 && month == 0 && year == 0)
               return (0);
            else if (day == 0 || month == 0 || year == 0 || month > 12)
               return (1000000000);
         }

         year--;
         Cent4 = year / 400;
         year = year - (Cent4 * 400);
         Cent = year / 100;
         year = year - (Cent * 100);
         Year4 = year / 4;
         year = year - (Year4 * 4);

         if (!usedoy)
         {
            PrevDays = PICInterface.date_day_tab[month - 1];
            CurrDays = PICInterface.date_day_tab[month];
            if (LeapYear)
               if (month > 1)
               {
                  CurrDays++;
                  if (month > 2)
                     PrevDays++;
               }
            if (day > CurrDays - PrevDays)
               return (1000000000);
            doy = PrevDays + day;
         }
         return (Cent4 * PICInterface.DAYSINFOURCENT + Cent * PICInterface.DAYSINCENTURY + Year4 * PICInterface.DAYSINFOURYEAR +
                 year * PICInterface.DAYSINYEAR + doy);
      }

      /// <summary>
      ///   Translates a string time to magic number
      /// </summary>
      /// <param name = "dispValue">   A string value</param>
      /// <param name = "picStr">      The picture to use</param>
      /// <returns> the magic number</returns>
      public String toTime(String dispValue, PIC pic)
      {
         int numVal;
         NUM_TYPE mgNum = new NUM_TYPE();
         String str;

         dispValue = dispValue.Trim();

         numVal = a_2_time(dispValue, pic, false);

         mgNum.NUM_4_LONG(numVal);
         str = mgNum.toXMLrecord();

         return str;
      }

      /// <summary>
      ///   Convert Alpha into a Time according to a given picture
      /// </summary>
      public int a_2_time(String Ascii, PIC pic, bool milliSeconds)
      {
         return a_2_time(Ascii.ToCharArray(), Ascii.Length, pic.getMask().ToCharArray(), milliSeconds);
      }

      /// <summary>
      ///   Convert Alpha into a Time according to a given picture
      /// </summary>
      protected internal int a_2_time(char[] Ascii, int AsciiL, char[] mask, bool milliSeconds)
      {
         int len; // Length of mask or string to scan
         int ampm; // AM/PM indicator
         int hour;
         int minute;
         int second;
         int millisecond;
         char c0;
         char c1;
         int I;
         NUM_TYPE mgNum = new NUM_TYPE();

         //-----------------------------------------------------------------------
         // Initialize counters and flags
         //-----------------------------------------------------------------------
         I = 0;
         len = mask.Length;
         if (len > AsciiL)
            len = AsciiL;
         ampm = 0;
         hour = 0;
         minute = 0;
         second = 0;
         millisecond = 0;

         //-----------------------------------------------------------------------
         // Loop on mask and on Ascii string
         //-----------------------------------------------------------------------
         while (I < len)
         {
            int maskLength;
            int maxMaskLen;

            //To get the maximum possible length of the constants in the mask
            switch (mask[I])
            {
               case (char)(PICInterface.PIC_HH):
               case (char)(PICInterface.PIC_MMT):
               case (char)(PICInterface.PIC_SS):
               case (char)(PICInterface.PIC_PM):
                  maxMaskLen = 2;
                  break;

               case (char)(PICInterface.PIC_MS):
                  maxMaskLen = 3;
                  break;

               default:
                  maxMaskLen = 1;
                  break;
            }

            /* To get the minimum of maxMaskLen & number of characters remained in string
            because if the Ascii is shorter(has less characters)then create String only
            of that no. of characters (maskLength), not of maxMaskLength */
            maskLength = Math.Min(maxMaskLen, len - I);

            switch (mask[I])
            {
               case (char)(PICInterface.PIC_HH):
                  hour = mgNum.a_2_long(new String(Ascii, I, maskLength));
                  break;

               case (char)(PICInterface.PIC_MMT):
                  minute = mgNum.a_2_long(new String(Ascii, I, maskLength));
                  break;

               case (char)(PICInterface.PIC_SS):
                  second = mgNum.a_2_long(new String(Ascii, I, maskLength));
                  break;

               case (char)(PICInterface.PIC_MS):
                  millisecond = mgNum.a_2_long(new String(Ascii, I, maskLength));
                  break;

               case (char)(PICInterface.PIC_PM):
                  ampm = 0;
                  c0 = Ascii[I];
                  c0 = Char.ToUpper(c0);
                  if (I + 1 < len)
                  {
                     c1 = Ascii[I + 1];
                     c1 = Char.ToUpper(c1);

                     if (c1 == 'M')
                     {
                        if (c0 == 'A')
                           ampm = -1;
                        if (c0 == 'P')
                           ampm = 1;
                     }
                  }
                  break;

               default:
                  break;
            }
            I += maskLength;
         }

         //-----------------------------------------------------------------------
         // Check validity of minutes and seconds
         //-----------------------------------------------------------------------
         if (second > 59 || minute > 59)
            return (0);

         //-----------------------------------------------------------------------
         // Convert hour to PM if specified as AM
         //-----------------------------------------------------------------------
         if (ampm != 0)
            if (ampm == -1)
            //AM
            {
               if (hour == 12)
                  hour = 0;
            }
            else if (hour < 12)
               //PM
               hour += 12;
         if (milliSeconds)
            return time_2_int(hour, minute, second) * 1000 + millisecond;
         else
            return time_2_int(hour, minute, second);
      }

      public int time_2_int(int hour, int minute, int second)
      {
         return (hour * 3600 + minute * 60 + second);
      }

      //--------------------------------------------------------------------------
      // General Methods
      //--------------------------------------------------------------------------
      /// <summary>
      ///   Convert "0"/"1" to false/true
      /// </summary>
      /// <param name = "boolStr">  "0"/"1"
      /// </param>
      /// <returns> true if "1" or false if others
      /// </returns>
      public static bool toBoolean(String boolStr)
      {
         return boolStr != null && boolStr.Equals("1");
      }

      /// <summary>
      ///   Convert '0'/'1' to false/true
      /// </summary>
      /// <param name = "boolChar">  '0'/'1'
      /// </param>
      /// <returns> true if '1' or false if others
      /// </returns>
      protected internal static bool toBoolean(char boolChar)
      {
         return boolChar == '1';
      }


      /// <summary>
      ///   converts string like "x,y,dx,dy" to MgRectangle
      /// </summary>
      /// <param name = "rectStr">
      /// </param>
      /// <returns>
      /// </returns>
      public static MgRectangle toRect(String val)
      {
         String[] strTok;
         int i;
         int[] array = new[] { 0, 0, 0, 0 };

         strTok = StrUtil.tokenize(val, ",");

         for (i = 0; i < strTok.Length; i++)
         {
            Debug.Assert(i < 4); // rectangle has 4 values
            array[i] = Int32.Parse(strTok[i]);
         }

         Debug.Assert(array.Length == 4);
         return new MgRectangle(array[0], array[1], array[2], array[3]);
      }

      /// <summary>
      ///   Java version of memset function to char[]
      /// </summary>
      /// <param name = "charArray">  A character array
      /// </param>
      /// <param name = "pos">        Position to start
      /// </param>
      /// <param name = "charToSet">  Character to set
      /// </param>
      /// <param name = "len">        number of Characters to set
      /// </param>
      public static void char_memset(char[] charArray, int pos, char charToSet, int len)
      {
         int i;

         if (len <= 0)
            return;
         for (i = 0; i < len; i++)
            charArray[pos + i] = charToSet;
      }

      /// <summary>
      ///   Java version of memcopy function to char[]
      /// </summary>
      /// <param name = "to">    destination array
      /// </param>
      /// <param name = "pos1">  Position to start in destination
      /// </param>
      /// <param name = "from">  source array
      /// </param>
      /// <param name = "pos2">  Position to start in source
      /// </param>
      /// <param name = "len">   number of Characters to copy
      /// </param>
      public static void char_memcpy(char[] to, int pos1, char[] from, int pos2, int len)
      {
         int i;

         if (len <= 0)
            return;
         for (i = 0; i < len; i++)
            to[pos1 + i] = from[pos2 + i];
      }

      /// <summary>
      ///   Java version of memmove function to char[]
      /// </summary>
      /// <param name = "to">    destination array
      /// </param>
      /// <param name = "pos1">  Position to start in destination
      /// </param>
      /// <param name = "from">  source array
      /// </param>
      /// <param name = "pos2">  Position to start in source
      /// </param>
      /// <param name = "len">   number of Characters to move
      /// </param>
      private static void char_memmove(char[] to, int pos1, char[] from, int pos2, int len)
      {
         int i;
         char[] tmp;


         if (len <= 0)
            return;

         tmp = new char[len];
         for (i = 0; i < len; i++)
            tmp[i] = from[pos2 + i];
         for (i = 0; i < len; i++)
            to[pos1 + i] = tmp[i];
         tmp = null;
      }

      /*------------------------------------------------------------------------*/
      /* calculate length of memory area withing blanks                         */
      /*------------------------------------------------------------------------*/

      /// <summary>
      ///   Magic's mem_trim function
      /// </summary>
      private int mem_trim(char[] s, int sPos, int len)
      {
         int i;

         for (i = sPos + len - 1; len > 0 && i < s.Length; i--, len--)
            if (s[i] != ' ' && s[i] != '\x0000')
               break;
         return len;
      }

      /*------------------------------------------------------------------------*/
      /* DATE functions                                                         */
      /*------------------------------------------------------------------------*/

      /// <summary>
      ///   String to Date function
      /// </summary>
      public int a_2_date(String str, String Format, int compIdx)
      {
         return (a_2_date_datemode(str, Format, compIdx));
      }

      public int a_2_date_pic(String str, PIC pic, String mask, int compIdx)
      {
         if (str == null)
            str = "";
         return (a_2_date_pic_datemode(str.ToCharArray(), str.Length, pic, mask.ToCharArray(), compIdx));
      }

      public String to_a(String outVal, int out_len, int date, String Format, int compIdx)
      {
         return (to_a_datemode(outVal, out_len, date, Format, false, compIdx));
      }

      /// <summary>
      ///   Convert string -> date using a format string
      /// </summary>
      private int a_2_date_datemode(String str, String Format, int compIdx)
      {
         PIC pic;

         pic = new PIC(Format, StorageAttribute.DATE, compIdx);
         if (str == null)
            str = "";
         return (a_2_date_pic_datemode(str.ToCharArray(), str.Length, pic, pic.getMask().ToCharArray(), compIdx));
      }

      /// <summary>
      ///   String to a datemode
      /// </summary>
      private String to_a_datemode(String outVal, int out_len, int date, String Format, bool ignore_dt_fmt, int compIdx)
      {
         PIC pic;

         /* Parse the picture */
         /* ----------------- */
         pic = new PIC(Format, StorageAttribute.DATE, compIdx);

         /* Convert the  date -> string */
         /* --------------------------- */
         if (outVal == null)
            outVal = pic.getMask();

         return (to_a_pic_datemode(outVal, out_len, date, pic, true, ignore_dt_fmt, compIdx));
      }

      /// <summary>
      ///   Decode digits from a date string
      /// </summary>
      private int date_a_2_i(String s, int len, int pos)
      {
         // don't forget to include 'pos+=len' after the using of the function
         String str;
         NUM_TYPE mgNum = new NUM_TYPE();

         try
         {
            str = s.Substring(pos, len);
         }
         catch (ArgumentOutOfRangeException)
         {
            if (pos + len < s.Length)
               str = s.Substring(pos);
            else
               str = "0";
         }
         return mgNum.a_2_long(str);
      }

      private String move_date(String str, int pos, int moveby, int len)
      {
         /* 19.11.97 - Hen : Fix #774600 */
         /* The moved len must be *len- pos - moveby, otherwise there will be memory leak */
         str = StrUtil.memmove(str, pos + moveby, str, pos, len - pos);
         str = StrUtil.memset(str, pos, '0', moveby);
         // dont't forget add : len += moveby;
         return str;
      }

      public String time_2_a(String Ascii, int AsciiL, int time, String Format, int compIdx, bool milliSeconds)
      {
         PIC pic;

         if (StrUtil.rtrim(Format).Length == 0)
            Format = "HH:MM:SS"; // Default time format

         pic = new PIC(Format, StorageAttribute.TIME, compIdx);
         if (Ascii == null)
            Ascii = pic.getMask();
         return (time_2_a_pic(Ascii, AsciiL, time, pic, false, milliSeconds));
      }

      /// <summary>
      ///   Translates a string to magic logical
      /// </summary>
      /// <param name = "dispValue">   A string value
      /// </param>
      /// <param name = "pic">         The picture to use
      /// </param>
      /// <param name = "rangeStr">    The range
      /// </param>
      /// <returns> the logical value
      /// </returns>
      public String toLogical(String dispValue, PIC pic, String rangeStr)
      {
         String str = null;
         dispValue = dispValue.Trim();
         if (dispValue == null || dispValue.Length == 0)
            dispValue = "False";
         str = disp_2_logical(dispValue, pic.getMask(), rangeStr);
         return str;
      }

      /// <summary>
      ///   translate the displayed value to a magic logical value
      /// </summary>
      /// <param name = "dispValue">the displayed value </param>
      /// <param name = "mask">the mask of the displayed value </param>
      /// <param name = "rangeStr">the range string </param>
      private String disp_2_logical(String dispValue, String mask, String rangeStr)
      {
         StringBuilder strippedValue = new StringBuilder(dispValue.Length);
         int i;
         String val, trueStr;

         rangeStr = win_rng_bool(rangeStr);
         rangeStr = StrUtil.makePrintableTokens(rangeStr, StrUtil.SEQ_2_HTML);
         i = rangeStr.IndexOf(',');
         trueStr = rangeStr.Substring(0, i);
         trueStr = StrUtil.makePrintableTokens(trueStr, StrUtil.HTML_2_STR);

         for (i = 0; i < dispValue.Length && i < mask.Length; i++)
         {
            if (mask[i] == PICInterface.PIC_X)
               // isLogicPositionalDirective
               strippedValue.Append(dispValue[i]);
         }
         val = strippedValue.ToString();
         if (trueStr.Length < val.Length)
            val = val.Substring(0, trueStr.Length);
         else if (val.Length < trueStr.Length)
            trueStr = trueStr.Substring(0, val.Length);
         return (val.ToUpper().Equals(trueStr.ToUpper()) ? "1" : "0");
      }

      /// <summary>
      ///   converting the displayed value of a string to internal
      ///   representation must remove the masking characters from it
      /// </summary>
      private String toAlpha(String dispValue, PIC pic)
      {
         int i, currPicture;
         String picture = pic.getMask();
         StringBuilder buffer = new StringBuilder(pic.getMaskSize());
         String strDspValOneChar; // JPN: DBCS support
         bool IsAlphaDBCS = (UtilStrByteMode.isLocaleDefLangDBCS() && pic.isAttrAlpha());

         for (currPicture = i = 0; i < dispValue.Length && currPicture < picture.Length; i++, currPicture++)
         {
            if (FieldValidator.isAlphaPositionalDirective(picture[currPicture]))
            {
               buffer.Append(dispValue[i]);

               if (IsAlphaDBCS)
               // JPN: DBCS support
               {
                  // a DBCS character in "dispValue" consumes two characters in "picture"
                  strDspValOneChar = dispValue.Substring(i, 1);
                  if (UtilStrByteMode.lenB(strDspValOneChar) == 2)
                     // if DBCS
                     currPicture++;
                  strDspValOneChar = null;
               }
            }
         }
         return buffer.ToString();
      }

      /// <summary>
      ///   Comparative newValue with possible Range in ContinuousRangeValues/discreteRangeValues
      /// </summary>
      /// <returns> newValue if it possible, old value owervise
      /// </returns>
      private String fillAlphaByRange(List<String> ContinuousRangeValues, List<String> discreteRangeValues,
                                      String newValue)
      {
         String tmpBuffer;

         tmpBuffer = fillAlphaByDiscreteRangeValues(discreteRangeValues, newValue);
         if (tmpBuffer != null)
            return tmpBuffer;

         tmpBuffer = fillAlphaByContinuousRangeValues(ContinuousRangeValues, newValue);
         if (tmpBuffer != null)
            return tmpBuffer;

         if (discreteRangeValues != null)
         {
            tmpBuffer = completeAlphaByRange(discreteRangeValues, newValue);
            if (tmpBuffer != null)
               return tmpBuffer;
         }

         // unpossible to find it in the range return not changed oldvalue
         return newValue;
      }

      /// <summary>
      ///   fill Alpha string by discrete range
      /// </summary>
      /// <param name = "discreteRangeValues">vector of discrete range values
      /// </param>
      /// <param name = "newValue">value, try found in the vector of discrete ranges
      /// </param>
      /// <returns> found value or null(not found)
      /// </returns>
      public String fillAlphaByDiscreteRangeValues(List<String> discreteRangeValues, String newValue)
      {
         int size;
         int i;
         String discreteValue, truncatedValue;

         if (discreteRangeValues != null)
         {
            size = discreteRangeValues.Count;
            for (i = 0; i < size; i++)
            {
               discreteValue = discreteRangeValues[i];
               truncatedValue = discreteValue.Substring(0, Math.Min(newValue.Length, discreteValue.Length));
               if (newValue.ToUpper().Equals(truncatedValue.ToUpper()))
                  return discreteValue;
            }
         }
         return null;
      }

      /// <summary>
      ///   fill Alpha string by continuous range
      /// </summary>
      /// <param name = "ContinuousRangeValues">vector of continuous range values
      /// </param>
      /// <param name = "newValue">value, try found in the vector of continuous ranges
      /// </param>
      /// <returns> found value or null(not found)
      /// </returns>
      public String fillAlphaByContinuousRangeValues(List<String> ContinuousRangeValues, String newValue)
      {
         String from, to;

         newValue = StrUtil.rtrim(newValue);
         if (ContinuousRangeValues != null)
            for (int i = 0; i < ContinuousRangeValues.Count; i++)
            {
               from = ContinuousRangeValues[i];
               to = ContinuousRangeValues[++i];
               if (String.CompareOrdinal(newValue, from) >= 0 && String.CompareOrdinal(newValue, to) <= 0)
                  return newValue;
            }
         return null;
      }

      /// <summary>
      ///   Try to complete the newValue using the Range
      ///   first try to comlete the value for case meaning and after that for ignore case
      /// </summary>
      /// <param name = "vector">of ranges
      /// </param>
      /// <param name = "checked">value
      /// </param>
      /// <returns> value completed by range
      /// </returns>
      public String completeAlphaByRange(List<String> discreteRangeValues, String newValue)
      {
         int currCoincide;
         int[] maxCoincide = new[] { 0, 0 };
         int i, j;
         String rangeItem, lowerValue;
         String[] bestItem = new String[] { null, null };

         int caseLetters;
         int CHECK_CASE = 0;
         int IGNORE_CASE = 1;
         bool wrongLetter;

         caseLetters = CHECK_CASE;
         while (caseLetters == CHECK_CASE || caseLetters == IGNORE_CASE)
         {
            if (caseLetters == CHECK_CASE)
               lowerValue = newValue;
            // caseLetters == IGNORE_CASE
            else
               lowerValue = newValue.ToLower();

            lowerValue = StrUtil.rtrim(lowerValue);
            // Not taking in account right spaces while completing Alpha by Range


            for (i = 0; i < discreteRangeValues.Count; i++)
            {
               wrongLetter = false;
               currCoincide = 0;
               int lowLength = 0;
               int rangeLength = 0;

               if (caseLetters == CHECK_CASE)
                  rangeItem = discreteRangeValues[i];
               // caseLetters == IGNORE_CASE
               else
                  rangeItem = discreteRangeValues[i].ToLower();

               lowLength = lowerValue.Length;
               rangeLength = rangeItem.Length;
               if (lowLength < rangeLength)
                  for (j = 0; j < lowLength; j++)
                  {
                     if (lowerValue[j] == rangeItem[j])
                        currCoincide++;
                     else
                     {
                        wrongLetter = true;
                        break;
                     }
                  }
               else
                  wrongLetter = true;


               if (currCoincide > maxCoincide[caseLetters] && !wrongLetter)
               {
                  bestItem[caseLetters] = discreteRangeValues[i];
                  maxCoincide[caseLetters] = currCoincide;
               }
            }

            caseLetters++; // IGNORE_CASE
         } //while , loop from 0-1 -> 2 cicles: first for all cases, second - ignore case

         if (bestItem[CHECK_CASE] != null)
            return bestItem[CHECK_CASE];
         else if (bestItem[IGNORE_CASE] != null)
            return bestItem[IGNORE_CASE];

         return null;
      }

      public void setDateChar(int dateChar)
      {
         DATECHAR = dateChar;
      }

      public int getDateChar()
      {
         return DATECHAR;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="value"></param>
      /// <param name="storageAttribute"></param>
      /// <returns></returns>
      public static object StringValueToMgValue(String value, StorageAttribute storageAttribute, char filler, int length)
      {
         switch (storageAttribute)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               {
                  String newVal = value;
                  newVal = newVal.PadRight(length, filler);
                  return newVal;
               }
            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               return new NUM_TYPE(value);
            default:
               return value;
         }
      }

      #region Nested type: DateBreakParams

      /// <summary>
      ///   Being used to send parameters to the 'date_break_datemode' method
      ///   it's struct
      /// </summary>
      public class DateBreakParams
      {
         public int day;
         public int dow;
         public int doy;
         public int month;
         public int year;

         public DateBreakParams()
         {
         }

         public DateBreakParams(int year_, int month_, int day_, int doy_, int dow_)
         {
            year = year_;
            month = month_;
            day = day_;
            doy = doy_;
            dow = dow_;
         }
      }

      #endregion

      #region Nested type: DateNameParams

      /// <summary>
      ///   Being used to send parameters to the 'date_i_2_nm' method
      /// </summary>
      private class DateNameParams
      {
         internal readonly char[] outVal;
         internal int len;
         internal int outIdx;

         internal DateNameParams(char[] out_, int outIdx_, int len_)
         {
            outVal = out_;
            outIdx = outIdx_;
            len = len_;
         }
      }

      #endregion

      #region Nested type: IntRef

      /// <summary>
      ///   Being used to send int by Ref
      /// </summary>
      internal class IntRef
      {
         internal int val;

         internal IntRef(int val_)
         {
            val = val_;
         }
      }

      #endregion

      #region Nested type: TimeBreakParams

      /// <summary>
      ///   Being used to send parameters to the 'time_break' method
      /// </summary>
      public class TimeBreakParams
      {
         public int hour;
         public int minute;
         public int second;

         public TimeBreakParams()
         {
         }

         public TimeBreakParams(int hour_, int minute_, int second_)
         {
            hour = hour_;
            minute = minute_;
            second = second_;
         }
      }

      #endregion
   }
}
