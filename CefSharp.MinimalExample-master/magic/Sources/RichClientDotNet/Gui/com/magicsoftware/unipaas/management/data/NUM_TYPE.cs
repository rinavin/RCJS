using System;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.env;
using System.Xml.Serialization;

namespace com.magicsoftware.unipaas.management.data
{
   /// <summary>
   ///   This class handles a Magic Number the identical way it's represented in Magic.
   ///   Most of the methods in this class were copied from the C++ sources of Magic,
   ///   in order to keep the exact logic used in order to implement all the functionality
   ///   expected, which means that the exact methods names and variables names where used
   ///   in order to make future debugging more easyer.
   ///   Notice a long data type that is used in Magic C++ sources, is represented in java
   ///   by the int data type.
   /// </summary>
   /// <author>  Alon Bar-Nes
   /// </author>

   public class NUM_TYPE
   {
      //--------------------------------------------------------------------------
      // Constants
      //--------------------------------------------------------------------------
      protected internal const String INT_ZERO_HEX = "00000000";
      protected internal const String BYTE_ZERO_BIT = "00000000";
      protected internal const int NO_ROOM = -1;
      protected internal const int ZERO_FILL = -2;
      [XmlIgnore]
      public const int NUM_SIZE = 20;
      protected internal static sbyte NUM_LONG_TYPE = unchecked((sbyte)0xff);
      protected internal static sbyte EXP_BIAS = 0x40;
      protected internal static sbyte SIGN_MASK = unchecked((sbyte)0x80);

      private static readonly short[] _randtbl = new short[]
                                                   {
                                                      41, 179, 9, 40, 93, 224, 24, 94, 39, 110, 70, 85, 196, 129, 80, 11
                                                      ,
                                                      127, 42, 87, 8, 243, 96, 174, 153, 61, 192, 17, 20, 235, 7, 47,
                                                      222,
                                                      53, 216, 101, 112, 44, 139, 109, 233, 108, 57, 240, 229, 160, 219,
                                                      251, 15, 221, 245, 239, 104, 182, 33, 138, 59, 166, 247, 146, 190,
                                                      31, 125, 188, 114, 97, 86, 157, 106, 3, 142, 66, 69, 152, 177, 116
                                                      ,
                                                      50, 149, 242, 144, 28, 29, 95, 37, 2, 117, 246, 65, 183, 206, 241,
                                                      230, 131, 13, 145, 63, 98, 200, 176, 226, 5, 158, 189, 49, 18, 54,
                                                      30, 120, 227, 68, 48, 91, 89, 252, 210, 185, 203, 140, 171, 155,
                                                      56,
                                                      162, 249, 201, 134, 244, 67, 197, 148, 248, 82, 100, 34, 90, 236,
                                                      51
                                                      , 167, 178, 207, 193, 164, 225, 218, 147, 79, 71, 217, 151, 38,
                                                      198,
                                                      215, 212, 123, 156, 76, 121, 113, 253, 55, 255, 23, 25, 231, 187,
                                                      21
                                                      , 72, 209, 103, 81, 35, 12, 6, 122, 78, 32, 60, 92, 14, 202, 83,
                                                      128
                                                      , 195, 135, 223, 211, 181, 84, 0, 119, 173, 165, 234, 45, 208, 107
                                                      ,
                                                      136, 199, 126, 141, 163, 180, 99, 172, 220, 75, 115, 232, 111, 228
                                                      ,
                                                      204, 237, 73, 254, 62, 105, 132, 22, 36, 118, 143, 161, 133, 169,
                                                      10
                                                      , 52, 186, 184, 170, 77, 205, 137, 124, 238, 16, 159, 250, 191, 74
                                                      ,
                                                      27, 130, 4, 194, 1, 46, 58, 102, 64, 168, 150, 88, 214, 175, 213,
                                                      154, 26, 43, 19
                                                   };

      protected internal char COMMACHAR; // ','
      protected internal char DECIMALCHAR; // '.'
      private int SIGNIFICANT_NUM_SIZE; // should be initialized by the CTOR

      //--------------------------------------------------------------------------
      // protected data
      //--------------------------------------------------------------------------
      sbyte[] _data = new sbyte[NUM_SIZE];
      public sbyte[] Data
      {
         get { return _data; }
         set { _data = value; }
      }

      //--------------------------------------------------------------------------
      // protected structures
      //--------------------------------------------------------------------------

      //--------------------------------------------------------------------------
      // Constructor list
      //--------------------------------------------------------------------------

      /// <summary>
      ///   Default constructor. Set value to 0
      /// </summary>
      public NUM_TYPE()
      {
         initConst();
         NUM_ZERO();
      }

      /// <summary>
      ///   Creates a Magic Number from a string got from the XML record value. The
      ///   String is 40 characters long in an hexa representation.
      /// </summary>
      /// <param name = "recordHexStr">  Magic number in hex format, 40 characters long
      /// </param>
      public NUM_TYPE(String recordHexStr)
      {
         int i = 0;
         String twoDigits;
         initConst();

         try
         {
            for (i = 0;
                 i < SIGNIFICANT_NUM_SIZE;
                 i++)
            {
               twoDigits = recordHexStr.Substring(i * 2, 2);
               _data[i] = (sbyte)Convert.ToInt32(twoDigits, 16);
            }
         }
         catch (Exception ex)
         {
            Events.WriteWarningToLog (ex);

            NUM_ZERO();

         }
      }

      /// <summary>
      ///   Creates a Magic Number from array of bytes. Used from the expression evaluator
      ///   when a Magic Number value apears within an expression.
      /// </summary>
      /// <param name = "byteVal">  Array of bytes representing a Magic Number</param>
      /// <param name = "offset">   Index of the first byte </param>
      /// <param name = "length">   Number of bytes to use </param>
      public NUM_TYPE(sbyte[] byteVal, int offset, int length)
         : this()
      {
         for (int i = 0;
              i < length;
              i++)
            _data[i] = byteVal[i + offset];
      }

      /// <summary>
      ///   Creates a Magic Number from array of bytes
      /// </summary>
      public NUM_TYPE(sbyte[] byteVal)
         : this(byteVal, 0, byteVal.Length)
      {
      }

      /// <summary>
      ///   Creates a Magic Number from a decimal for number and its picture
      /// </summary>
      /// <param name = "decStr">  The number in a string decimal format </param>
      /// <param name = "pic">     The picture format to be used </param>
      public NUM_TYPE(String decStr, PIC pic, int compIdx)
         : this()
      {
         from_a(decStr, pic, compIdx);
      }

      /// <summary>
      ///   Creates a Magic Number from a Magic Number
      /// </summary>
      /// <param name = "numFrom">  The source Magic Number </param>
      public NUM_TYPE(NUM_TYPE numFrom)
      {
         initConst();
         _data = new sbyte[numFrom._data.Length];
         numFrom._data.CopyTo(_data, 0);
      }

      //--------------------------------------------------------------------------
      // Public Methods
      //--------------------------------------------------------------------------

      //--------------------------------------------------------------------------
      // protected Methods
      //--------------------------------------------------------------------------

      /// <summary>
      ///   Build a decimal string from the number stored in a Magic Number.
      ///   The string is build from the picture provided as a parameter.
      /// </summary>
      /// <param name = "pic">The picture to use for building the display string
      /// </param>
      /// <returns> The decimal form of the Magic Number in the specified format
      /// </returns>
      public String toDisplayValue(PIC pic)
      {
         return to_a(pic);
      }

      /// <summary>
      ///   init constants of the class
      /// </summary>
      private void initConst()
      {
         IEnvironment env = Manager.Environment;
         DECIMALCHAR = env.GetDecimal();
         COMMACHAR = env.GetThousands();
         SIGNIFICANT_NUM_SIZE = env.GetSignificantNumSize();
      }

      /// <summary>
      ///   Build an hexadecimal string from the Magic Number for the data record in
      ///   the XML tag (to be sent to the Magic Server). The length of this string
      ///   will allways be two times the length of the Magic Numbers (in bytes)
      ///   because every byte in the Magic Number will be translated to two
      ///   character hexadecimal string.
      /// </summary>
      /// <returns> the hexadecimal string
      /// </returns>
      public String toXMLrecord()
      {
         int i;
         int numSize;
         StringBuilder hexStr = new StringBuilder(SIGNIFICANT_NUM_SIZE * 2);
         String tmpStr;

         if (NUM_IS_LONG())
            numSize = 5;
         else
            numSize = SIGNIFICANT_NUM_SIZE;

         for (i = 0;
              i < SIGNIFICANT_NUM_SIZE;
              i++)
         {
            if (i < numSize)
            {
               tmpStr = Convert.ToString(_data[i], 16);
               if (_data[i] < 0)
                  tmpStr = tmpStr.Substring(tmpStr.Length - 2);
               else if (_data[i] < 16)
                  hexStr.Append('0');
               hexStr.Append(tmpStr);
            }
            else
               hexStr.Append("00");
         }

         return hexStr.ToString().ToUpper();
      }

      /// <summary>
      ///   Transforming a decimal string number to a Magic Number using the format
      ///   provided.
      /// </summary>
      /// <param name = "Alpha">  The number is decimal string form
      /// </param>
      /// <param name = "pic">    The picture fomat to use for the extracting the number
      /// </param>
      private void from_a(String Alpha, PIC pic, int compIdx)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         int i;
         int idx;
         bool dec;
         int decs;
         int digit;
         int len;
         bool no_pic;
         int Pos;
         bool SType;
         bool SPart;
         int Signed;
         String buf;
         String mask = "";
         char c;
         bool NgtvPrmt;
         String Scan = "";
         String Pbuf = "";
         bool is_negative;

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         String negStr;
         String negPref;
         String posPref;
         String negSuff;
         String posSuff;
         //-----------------------------------------------------------------------
         // Parse the picture format
         //-----------------------------------------------------------------------
         no_pic = (pic == null);
         len = 0;
         SType = false; // PIC_PSTV : true, PIC_NGTV: false
         SPart = false; // PIC_PREF : true, PIC_SUF : false

         if (!no_pic)
         {
            mask = pic.getMask();
            if (Alpha.Length > pic.getMaskSize())
               Alpha = Alpha.Substring(0, pic.getMaskSize());
            NgtvPrmt = pic.isNegative();
            if (NgtvPrmt)
            {
               negPref = pic.getNegPref_();
               posPref = pic.getPosPref_();
               negSuff = pic.getNegSuff_();
               posSuff = pic.getPosSuff_();

               if (negPref.Length == posPref.Length)
               {
                  if (negPref.Length == 0 || negPref.Equals(posPref))
                     SPart = false;
                  // PIC_SUF
                  else
                     SPart = true; // PIC_PREF
               }
               else
                  SPart = true; // PIC_PREF

               if (SPart)
                  negStr = negPref;
               else
                  negStr = negSuff;

               if (negStr.Length == 0)
                  SType = true;
               //PIC_POSITIVE
               else
                  SType = false; //PIC_NEGATIVE

               if (SType)
                  Scan = (SPart
                             ? posPref
                             : posSuff);
               else
                  Scan = (SPart
                             ? negPref
                             : negSuff);
               len = Scan.Length;
            }
         }
         else
         {
            pic = new PIC("", StorageAttribute.NUMERIC, compIdx);
            NgtvPrmt = false;
         }

         //-----------------------------------------------------------------------
         // Localize variables and flags
         //-----------------------------------------------------------------------
         dec = false;
         decs = 0;
         digit = 0;
         Signed = 0;

         //-----------------------------------------------------------------------
         // Loop on picture mask and decode the digits and the decimal point
         //-----------------------------------------------------------------------
         for (idx = 0;
              idx < Alpha.Length;
              )
         {
            if (no_pic || mask[idx] == PICInterface.PIC_N)
            {
               c = Alpha[idx];
               if (UtilStrByteMode.isDigit(c))
               {
                  digit++;
                  Pbuf += c;
                  if (dec)
                     decs++;
               }
               else if (c == DECIMALCHAR)
               {
                  Pbuf += DECIMALCHAR;
                  dec = true;
               }
               else if (c != COMMACHAR)
               {
                  if (NgtvPrmt && Signed == 0)
                  {
                     Signed = 1;
                     for (i = idx, Pos = 0;
                          i <= Alpha.Length && Pos < len;
                          i++)
                     {
                        if (no_pic || mask[i] == PICInterface.PIC_N)
                        {
                           c = Alpha[i];
                           if (c != Scan[Pos])
                           {
                              if (c == '-' && !SType)
                                 break;
                              if (c == '+' && SType)
                                 break;
                              Signed = -1;
                              break;
                           }
                           Pos++;
                        }
                     }
                     if (Pos > 0 && Pos < len)
                        Signed = -1;
                  }
                  else
                  {
                     // QCR # 309623 : If prefix/suffix strings contains digits in it, it is considered as part of data.
                     // In order to avoid it skip the prefix / suffix strings while extracting the data part to build NUM_TYPE.
                     if (idx == 0)
                     {
                        if (pic.getNegPref_().Length > 0)
                           if (Alpha.StartsWith(pic.getNegPref_()))
                           {
                              idx += pic.getNegPref_().Length;
                              continue;
                           }

                        if (pic.getPosPref_().Length > 0)
                           if (Alpha.StartsWith(pic.getPosPref_()))
                           {
                              idx += pic.getPosPref_().Length;
                              continue;
                           }

                     }
                     else
                     {
                        if (pic.getNegSuff_().Length > 0)
                           if (Alpha.EndsWith(pic.getNegSuff_()))
                           {
                              idx += pic.getNegSuff_().Length;
                              continue;
                           }

                        if (pic.getPosSuff_().Length > 0)
                           if (Alpha.EndsWith(pic.getPosSuff_()))
                           {
                              idx += pic.getPosSuff_().Length;
                              continue;
                           }
                     }
                  }
               }
            }
            idx++;
         }

         if (Signed == 0 && SType)
            Signed = 1;

         //-----------------------------------------------------------------------
         // Return result of decoded digits
         //-----------------------------------------------------------------------
         is_negative = (Signed == 1 && !SType) || (Signed != 1 && SType);
         if (decs > 0 || digit > 9)
         {
            buf = (is_negative
                      ? '-'
                      : ' ') + Pbuf;
            num_4_a_std(buf);
         }
         else
            NUM_4_LONG((is_negative
                           ? -1
                           : 1) * a_2_long(Pbuf));
      }

      /// <summary>
      ///   Extracting a non long number from a decimal string number (numbers with
      ///   decimal point, etc...)
      /// </summary>
      /// <param name = "str">  The decimal string number
      /// </param>
      public void num_4_a_std(String str)
      {
         int diglen;
         String digstr = "";
         int wholes;
         int decs;
         bool isdec;
         bool isminus;
         int pos;
         int numptr;
         char c;
         sbyte digit1;
         sbyte digit2;

         diglen = 0;
         wholes = 0;
         decs = 0;
         isdec = false;
         isminus = false;

         for (pos = 0;
              pos < str.Length;
              pos++)
         {
            c = str[pos];
            if (UtilStrByteMode.isDigit(c))
            {
               if ((diglen > 0) || (c != '0'))
               {
                  diglen++;
                  digstr += c;
                  if (!isdec)
                     wholes++;
               }
               else if (isdec)
                  decs++;
            }
            else if (c == DECIMALCHAR)
               isdec = true;
            else if (c == '-')
               isminus = true;
         }

         NUM_ZERO();
         if (diglen == 0)
            return;

         if (((wholes + decs) & 1) != 0)
         {
            _data[1] = (sbyte)(digstr[0] - '0');
            pos = 1;
         }
         else
            pos = 0;
         numptr = 1 + pos;
         diglen = Math.Min(diglen, (SIGNIFICANT_NUM_SIZE - 1) * 2 - pos);
         while (pos < diglen)
         {
            digit1 = toSByte(digstr[pos++] - '0');
            digit2 = toSByte(((pos < diglen)
                                 ? (digstr[pos++] - '0')
                                 : 0));
            _data[numptr++] = toSByte(toUByte(digit1) * 10 + toUByte(digit2));
         }
         if (wholes > 0)
            _data[0] = toSByte(EXP_BIAS + ((wholes + 1) >> 1));
         else
            _data[0] = toSByte(EXP_BIAS - (decs >> 1));
         if (isminus)
            _data[0] |= SIGN_MASK;
      }


      /// <summary>
      ///   Translating a long value into an internal Magic Number form
      /// </summary>
      /// <param name = "longVal">  The number to be kept
      /// </param>
      public void NUM_4_LONG(int longVal)
      {
         String hexStr;

         NUM_ZERO();
         if (longVal != 0)
         {
            hexStr = Convert.ToString(longVal, 16);
            if (longVal > 0)
               hexStr = INT_ZERO_HEX.Substring(hexStr.Length) + hexStr;

            _data[1] = toSByte(Convert.ToInt32(hexStr.Substring(6, (8) - (6)), 16));
            _data[2] = toSByte(Convert.ToInt32(hexStr.Substring(4, (6) - (4)), 16));
            _data[3] = toSByte(Convert.ToInt32(hexStr.Substring(2, (4) - (2)), 16));
            _data[4] = toSByte(Convert.ToInt32(hexStr.Substring(0, (2) - (0)), 16));
         }
      }

      /// <summary>
      ///   Getting a long value from a decimal string representing a long value
      /// </summary>
      /// <param name = "str">  the string representing a long value
      /// </param>
      /// <returns> the long number
      /// </returns>
      public int a_2_long(String str)
      {
         int pos;
         int n;

         n = 0;
         for (pos = 0;
              pos < str.Length;
              pos++)
            if (UtilStrByteMode.isDigit(str[pos]))
            {
               n *= 10;
               n += str[pos] - '0';
            }

         return (n);
      }

      /// <summary>
      ///   Activating the process of building a decimal string by the format indicated
      /// </summary>
      /// <param name = "pic">a numeric picture to use for building the display string
      /// </param>
      public String to_a(PIC pic)
      {
         String res;

         res = to_a_pic(pic);
         return res;
      }

      /// <summary>
      ///   Performs the process of building a decimal string by the picture provided
      /// </summary>
      /// <param name = "pic">  the picture class holding the information about the picture
      /// </param>
      /// <returns> the string form of the Magic Number
      /// </returns>
      protected internal String to_a_pic(PIC pic)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         char[] buf = new char[128];
         int remains;
         bool sign_n;
         int pref_len;
         int suff_len;
         String pref_str;
         String suff_str;
         int str_pos;
         int str_len;
         int out_pos;
         char[] out_buf;
         char[] tmp_out_buf;
         char pad;
         bool left;
         bool pfill;
         int mask_chars;

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         int len;
         char[] res;
         int i;
         bool isOut;
         char[] outVal;

         len = pic.getMaskSize();

         //-----------------------------------------------------------------------
         // Set signs of number
         //-----------------------------------------------------------------------
         sign_n = (pic.isNegative() && num_is_neg()
                      ? false
                      : true);
         // pic_sign = pic->sign[sign_n]; // Original line (not needed)
         pref_len = (sign_n
                        ? pic.getPosPref_().Length
                        : pic.getNegPref_().Length); // pic_sign[PIC_PREF].len;
         suff_len = (sign_n
                        ? pic.getPosSuff_().Length
                        : pic.getNegSuff_().Length); // pic_sign[PIC_SUFF].len;

         left = pic.isLeft();
         pfill = pic.padFill();
         mask_chars = pic.getMaskChars();
         outVal = pic.getMask().Substring(0, len).ToCharArray();
         out_buf = ((left || (mask_chars > 0))
                       ? buf
                       : outVal);
         isOut = ((left || (mask_chars > 0))
                     ? false
                     : true);

         //-----------------------------------------------------------------------
         // Convert to string
         //-----------------------------------------------------------------------
         str_pos = pref_len;
         str_len = len - pref_len - suff_len;

         tmp_out_buf = new String(out_buf, str_pos, out_buf.Length - str_pos).ToCharArray();
         if (NUM_IS_LONG())
            remains = num_l_2_str(NUM_LONG(), tmp_out_buf, str_len, pic);
         else
            remains = to_str(tmp_out_buf, str_len, pic);
         for (i = str_pos;
              i < out_buf.Length;
              i++)
            out_buf[i] = tmp_out_buf[i - str_pos];
         tmp_out_buf = null;

         if (remains < mask_chars)
         {
            // memset (out, (remains == ZERO_FILL ? pic->zero : '*'), len); // Original Line
            res = new char[len];
            for (i = 0;
                 i < len;
                 i++)
               res[i] = (remains == ZERO_FILL
                            ? pic.getZeroPad()
                            : '*');
            return new String(res);
         }

         if (mask_chars > 0)
         {
            remains -= mask_chars;
            str_pos += mask_chars;
            str_len -= mask_chars;
         }

         //-----------------------------------------------------------------------
         // take care of pad after and before number
         //-----------------------------------------------------------------------
         if (pfill)
         {
            pad = pic.getPad();
            if (left)
            {
               // memset (&out_buf[str_pos + str_len], pad, remains);  // Original Line
               for (i = 0;
                    i < remains;
                    i++)
                  out_buf[str_pos + str_len + i] = pad;
               str_pos += remains;
            }
            else
            {
               // memset (&out_buf[str_pos], pad, remains);  // Original Line
               for (i = 0;
                    i < remains;
                    i++)
                  out_buf[str_pos + i] = pad;
            }
         }
         else
         {
            pad = ' ';
            str_pos += remains;
            str_len -= remains;
         }

         //-----------------------------------------------------------------------
         // take care of suffix and prefix
         //-----------------------------------------------------------------------
         if (suff_len > 0)
         {
            suff_str = sign_n
                          ? pic.getPosSuff_()
                          : pic.getNegSuff_(); // [PIC_SUFF].str;
            if (suff_len == 1)
               out_buf[str_pos + str_len] = suff_str[0];
            else
            {
               // memcpy (&out_buf[str_pos + str_len], suff_str, suff_len); // Original Line
               for (i = 0;
                    i < suff_len;
                    i++)
                  out_buf[str_pos + str_len + i] = suff_str[i];
            }
            str_len += suff_len;
         }
         if (pref_len > 0)
         {
            str_pos -= pref_len;
            str_len += pref_len;
            pref_str = sign_n
                          ? pic.getPosPref_()
                          : pic.getNegPref_(); // pic_sign[PIC_PREF].str;
            if (pref_len == 1)
               out_buf[str_pos] = pref_str[0];
            else
            {
               // memcpy (&out_buf[str_pos], pref_str, pref_len); // Original Line
               for (i = 0;
                    i < pref_len;
                    i++)
                  out_buf[str_pos + i] = pref_str[i];
            }
         }

         //-----------------------------------------------------------------------
         // str_pos now points to beginning of result str
         // take care of final pad after and before number
         //-----------------------------------------------------------------------
         if (!pfill)
         {
            int outBufLen = out_buf.Length;

            if (left)
            {
               // memset (&out_buf[str_pos + str_len], pad, remains); // Original Line
               for (i = 0;
                    i < remains && (str_pos + str_len + i < outBufLen);
                    i++)
                  out_buf[str_pos + str_len + i] = pad;
            }
            else
            {
               str_pos -= remains;
               // memset (&out_buf[str_pos], pad, remains); // Original Line
               for (i = 0;
                    i < remains && (str_pos + i < outBufLen);
                    i++)
                  out_buf[str_pos + i] = pad;
            }
         }
         //-----------------------------------------------------------------------
         // result built, finish
         //-----------------------------------------------------------------------
         if (isOut)
            return new String(out_buf);

         //-----------------------------------------------------------------------
         // Copy result to output buffer
         //-----------------------------------------------------------------------
         if (mask_chars == 0)
         {
                // memcpy (out, &out_buf[str_pos], len); // Original Line
                if (str_pos + len > out_buf.Length)
                    len = out_buf.Length - str_pos;

                return new String(out_buf, str_pos, len);
         }

         for (out_pos = 0;
              out_pos < len && str_pos < out_buf.Length;
              out_pos++)
            if (outVal[out_pos] == PICInterface.PIC_N)
               outVal[out_pos] = out_buf[str_pos++];

         return new String(outVal);
      }

      /// <summary>
      ///   Bulding the basic decimal string number from a long value (without
      ///   the special picture features).
      /// </summary>
      /// <param name = "num">  The number to be translated
      /// </param>
      /// <param name = "str">  The mask to use for building the string number
      /// </param>
      /// <len>    len   the len to use in the str char array </len>
      /// <param name = "pic">  The picture class holding the information about the picture
      /// </param>
      /// <returns> the length of the result string
      /// </returns>
      protected internal int num_l_2_str(int num, char[] str, int len, PIC pic)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         bool commas;
         int digits;
         int i;
         int decs;

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         int j;

         if (num < 0)
            num = -num;
         commas = pic.withComa();
         decs = Math.Min(pic.getDec(), len - 1);
         if (decs > 0)
            len -= (decs + 1);
         i = len;

         //-----------------------------------------------------------------------
         // 26/11/97 Shay Z. Bug #770526 - If The decimal point is the first
         // in the picture's format - assume that the input value is legal !!
         //-----------------------------------------------------------------------
         if (num == 0 && !pic.decInFirstPos())
         {
            if (pic.zeroFill())
               return (ZERO_FILL);
            if (len == 0)
               return (NO_ROOM);

            if (i > str.Length)
               i = str.Length;

            str[--i] = '0';
         }

         if (decs > 0)
         {
            str[len] = DECIMALCHAR;
            // memset (&str [len + 1], '0', decs); // Original line
            for (j = 0;
                 j < decs;
                 j++)
               str[len + 1 + j] = '0';
         }

         digits = 0;
         for (;
            num > 0;
            num /= 10)
         {
            i--;
            if (commas)
            {
               if (digits == 3)
               {
                  if (i < 0)
                     return (NO_ROOM);
                  digits = 0;
                  str[i--] = COMMACHAR;
               }
               digits++;
            }
            if (i < 0)
               return (NO_ROOM);
            str[i] = (char)((num % 10) + '0');
         }

         return i;
      }

      /// <summary>
      ///   Bulding the basic decimal string number from a non long value (without
      ///   the special picture features).
      /// </summary>
      /// <param name = "str">  The mask to use for building the string number
      /// </param>
      /// <len>    len   the len to use in the str char array </len>
      /// <param name = "pic">  The picture class holding the information about the picture
      /// </param>
      /// <returns> the length of the result string
      /// </returns>
      protected internal int to_str(char[] str, int len, PIC pic)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         NUM_TYPE tmp;
         bool commas;
         int digits;
         int decpos;
         int last;
         int first;
         int j;
         int i;
         int decs;
         char num_char;

         commas = pic.withComa();
         decs = pic.getDec();
         if (decs >= len)
            decs = len - 1;
         tmp = new NUM_TYPE(this); // NUM_CPY (&tmp, num);
         //round(decs);
         tmp.round(decs);
         if (tmp.NUM_IS_LONG())
            return tmp.num_l_2_str(tmp.NUM_LONG(), str, len, pic);

         if (pic.zeroFill() && tmp._data[0] == 0)
            return (ZERO_FILL);

         i = len - 1;
         digits = 0;
         decpos = ((tmp._data[0] & ~SIGN_MASK) - EXP_BIAS + 1) * 2;
         last = decpos - 1 + decs;
         first = decpos - 1;
         if (first > 2)
            first = (tmp._data[1] < 10)
                       ? 3
                       : 2;

         for (j = last;
              j >= first;
              j--)
         {
            if (i < 0)
               return (NO_ROOM);
            if (commas && (j < decpos))
            {
               if (digits == 3)
               {
                  digits = 0;
                  str[i--] = COMMACHAR;
               }
               digits++;
            }

            //--------------------------------------------------------------------
            // JPN:15/08/1996 Yama (JpnID:ZA0815001)
            // When Format="N12C" and num is bigger than 12 digits.
            // MAGIC crushes. Because the var i becomes negative.
            //--------------------------------------------------------------------
            if (i < 0)
               return (NO_ROOM);
            if ((j < 2) || (j >= SIGNIFICANT_NUM_SIZE * 2))
               str[i--] = '0';
            else
            {
               //-----------------------------------------------------------------
               // Alex, 21.04.93, two lines
               //-----------------------------------------------------------------
               if (i < 0)
                  return (NO_ROOM);
               num_char = (char)tmp._data[j >> 1];
               str[i--] = (char)((((j & 1) != 0)
                                      ? (num_char % 10)
                                      : (num_char / 10)) + '0');
            }
            if (j == decpos)
            {
               if (i < 0)
                  return (NO_ROOM);
               str[i--] = DECIMALCHAR;

               //-----------------------------------------------------------------
               // 26/11/97 Shay Z. Bug #770526 - If The decimal point is the first
               // in the picture's format - assume that the input value is legal !
               //-----------------------------------------------------------------
               if (i < 0 && pic.decInFirstPos())
                  return (0);
            }
         }

         return (i + 1);
      }

      //--------------------------------------------------------------------------
      // Public operations methods
      //--------------------------------------------------------------------------

      /// <summary>
      ///   Performing an Add operation on two magic numbers
      /// </summary>
      /// <param name = "num1">  the first number
      /// </param>
      /// <param name = "num2">  the first number
      /// </param>
      /// <returns> the result
      /// </returns>
      public static NUM_TYPE add(NUM_TYPE num1, NUM_TYPE num2)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         sbyte sign1;
         sbyte sign2;
         int cmpval;
         NUM_TYPE tmpres;
         sbyte exp;
         int l;
         int l1;
         int l2;
         int SIGNIFICANT_NUM_SIZE;

         // null values
         if (num1 == null || num2 == null)
            return null;

         SIGNIFICANT_NUM_SIZE = Manager.Environment.GetSignificantNumSize();
         tmpres = new NUM_TYPE();
         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         NUM_TYPE res = new NUM_TYPE();
         OperData operData = new OperData();

         if (num1.NUM_IS_LONG())
         {
            if (num2.NUM_IS_LONG())
            {
               l = l1 = num1.NUM_LONG();
               if (l < 0)
                  l = -l;
               if (l < 0x40000000)
               {
                  l = l2 = num2.NUM_LONG();
                  if (l < 0)
                     l = -l;
                  if (l < 0x40000000)
                  {
                     l1 += l2;
                     res.NUM_4_LONG(l1);
                     return res;
                  }
               }
               num2.num_4_std_long();
            }
            num1.num_4_std_long();
         }
         else if (num2.NUM_IS_LONG())
            num2.num_4_std_long();

         sign1 = (sbyte)(num1._data[0] & SIGN_MASK);
         operData.NUM_Exp1_ = (sbyte)(num1._data[0] & ~SIGN_MASK);
         sign2 = (sbyte)(num2._data[0] & SIGN_MASK);
         operData.NUM_Exp2_ = (sbyte)(num2._data[0] & ~SIGN_MASK);
         operData.NUM_Diff_ = operData.NUM_Exp1_ - operData.NUM_Exp2_;

         cmpval = operData.NUM_Diff_;
         if (cmpval == 0)
         {
            cmpval = toUByte(num1._data[1]) - toUByte(num2._data[1]);
            if (cmpval == 0)
               cmpval = memcmp(num1, 2, num2, 2, SIGNIFICANT_NUM_SIZE - 2);
         }

         if (cmpval >= 0)
         {
            if (sign1 == sign2)
               tmpres = add_pos(num1, num2, operData);
            else
               tmpres = sub_pos(num1, num2, operData);
            if (tmpres._data[0] != 0)
               tmpres._data[0] = (sbyte)(tmpres._data[0] | sign1);
         }
         else
         {
            exp = operData.NUM_Exp1_;
            operData.NUM_Exp1_ = operData.NUM_Exp2_;
            operData.NUM_Exp2_ = exp;
            operData.NUM_Diff_ = -operData.NUM_Diff_;
            if (sign1 == sign2)
               tmpres = add_pos(num2, num1, operData);
            else
               tmpres = sub_pos(num2, num1, operData);
            if (tmpres._data[0] != 0)
               tmpres._data[0] |= sign2;
         }

         return tmpres;
      }

      /// <summary>
      ///   Performing a Subtract operation on two magic numbers
      /// </summary>
      /// <param name = "num1">  the first number
      /// </param>
      /// <param name = "num2">  the first number
      /// </param>
      /// <returns> the result
      /// </returns>
      public static NUM_TYPE sub(NUM_TYPE num1, NUM_TYPE num2)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         sbyte sign1;
         sbyte sign2;
         int cmpval;
         NUM_TYPE tmpres;
         sbyte exp;
         int l;
         int l1;
         int l2;

         // null values
         if (num1 == null || num2 == null)
            return null;

         tmpres = new NUM_TYPE();
         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         NUM_TYPE res = new NUM_TYPE();
         OperData operData = new OperData();

         if (num1.NUM_IS_LONG())
         {
            if (num2.NUM_IS_LONG())
            {
               l = l1 = num1.NUM_LONG();
               if (l < 0)
                  l = -l;
               if (l < 0x40000000)
               {
                  l = l2 = num2.NUM_LONG();
                  if (l < 0)
                     l = -l;
                  if (l < 0x40000000)
                  {
                     l1 -= l2;
                     res.NUM_4_LONG(l1);
                     return res;
                  }
               }
               num2.num_4_std_long();
            }
            num1.num_4_std_long();
         }
         else if (num2.NUM_IS_LONG())
            num2.num_4_std_long();

         sign1 = (sbyte)(num1._data[0] & SIGN_MASK);
         operData.NUM_Exp1_ = (sbyte)(num1._data[0] & ~SIGN_MASK);
         sign2 = (sbyte)(num2._data[0] & SIGN_MASK);
         operData.NUM_Exp2_ = (sbyte)(num2._data[0] & ~SIGN_MASK);
         operData.NUM_Diff_ = operData.NUM_Exp1_ - operData.NUM_Exp2_;

         cmpval = operData.NUM_Diff_;
         if (cmpval == 0)
         {
            cmpval = toUByte(num1._data[1]) - toUByte(num2._data[1]);
            if (cmpval == 0)
               cmpval = memcmp(num1, 2, num2, 2, Manager.Environment.GetSignificantNumSize() - 2);
         }

         if (cmpval >= 0)
         {
            if (sign1 == sign2)
               tmpres = sub_pos(num1, num2, operData);
            else
               tmpres = add_pos(num1, num2, operData);
         }
         else
         {
            exp = operData.NUM_Exp1_;
            operData.NUM_Exp1_ = operData.NUM_Exp2_;
            operData.NUM_Exp2_ = exp;
            operData.NUM_Diff_ = -operData.NUM_Diff_;
            if (sign1 == sign2)
            {
               tmpres = sub_pos(num2, num1, operData);
               sign1 ^= SIGN_MASK;
            }
            else
               tmpres = add_pos(num2, num1, operData);
         }
         if (tmpres._data[0] != 0)
            tmpres._data[0] |= sign1;

         return tmpres;
      }

      /// <summary>
      ///   Performing a Multiply operation on two magic numbers
      /// </summary>
      /// <param name = "num1">  the first number
      /// </param>
      /// <param name = "num2">  the first number
      /// </param>
      /// <returns> the result
      /// </returns>
      public static NUM_TYPE mul(NUM_TYPE num1, NUM_TYPE num2)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         sbyte[] fullres = new sbyte[(NUM_SIZE - 1) * 2];
         int pwr;
         int len1;
         int len2;
         int pos1;
         int pos2;
         int pos;
         sbyte digit1;
         int prod;
         int carry;
         int l;
         int l1;
         int l2;
         int SIGNIFICANT_NUM_SIZE;

         // null values
         if (num1 == null || num2 == null)
            return null;

         SIGNIFICANT_NUM_SIZE = Manager.Environment.GetSignificantNumSize();

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         NUM_TYPE res = new NUM_TYPE();
         OperData operData = new OperData();
         int i;
         sbyte tmpByte;

         if (num1.NUM_IS_LONG())
         {
            if (num2.NUM_IS_LONG())
            {
               l = l1 = num1.NUM_LONG();
               if (l < 0)
                  l = -l;
               if (l < 0xB000)
               {
                  l = l2 = num2.NUM_LONG();
                  if (l < 0)
                     l = -l;
                  if (l < 0xB000)
                  {
                     l1 *= l2;
                     res.NUM_4_LONG(l1);
                     return res;
                  }
               }
               num2.num_4_std_long();
            }
            num1.num_4_std_long();
         }
         else if (num2.NUM_IS_LONG())
            num2.num_4_std_long();

         operData.NUM_Exp1_ = (sbyte)(num1._data[0] & ~SIGN_MASK);
         operData.NUM_Exp2_ = (sbyte)(num2._data[0] & ~SIGN_MASK);
         if (operData.NUM_Exp1_ == 0 || operData.NUM_Exp2_ == 0)
         {
            res.NUM_ZERO();
            return res;
         }

         for (len1 = SIGNIFICANT_NUM_SIZE - 1;
              num1._data[len1] == 0;
              len1--)
         { }
         for (len2 = SIGNIFICANT_NUM_SIZE - 1;
              num2._data[len2] == 0;
              len2--)
         { }

         // memset (fullres, 0, (NUM_SIZE - 1) * 2); // Original Command
         for (i = 0;
              i < (NUM_SIZE - 1) * 2;
              i++)
            fullres[i] = 0;
         pos = 0;
         for (pos1 = len1;
              pos1 > 0;
              pos1--)
         {
            pos = pos1 + len2 - 1;
            digit1 = num1._data[pos1];
            carry = 0;
            for (pos2 = len2;
                 pos2 > 0;
                 pos2--)
            {
               prod = toUByte(digit1) * toUByte(num2._data[pos2]) + carry;
               carry = prod / 100;
               // if ((fullres[pos--] += (Uchar)(prod % 100)) >= 100) // Original Lines
               fullres[pos] = toSByte(toUByte(fullres[pos]) + (prod % 100));
               tmpByte = fullres[pos];
               pos--;
               if (toUByte(tmpByte) >= 100)
               {
                  fullres[pos + 1] = toSByte(toUByte(fullres[pos + 1]) - 100);
                  carry++;
               }
            }
            fullres[pos] = toSByte(carry);
         }

         pwr = (toUByte(operData.NUM_Exp1_) - EXP_BIAS) + (toUByte(operData.NUM_Exp2_) - EXP_BIAS);
         if (fullres[0] == 0)
         {
            pos++;
            pwr--;
         }
         if ((num1._data[0] & SIGN_MASK) == (num2._data[0] & SIGN_MASK))
            res._data[0] = 0;
         else
            res._data[0] = SIGN_MASK;
         res._data[0] |= toSByte(pwr + EXP_BIAS);
         // memcpy (&res->numfield[1], &fullres[pos], NUM_SIZE - 1); // Original Line
         for (i = 0;
              i < SIGNIFICANT_NUM_SIZE - 1;
              i++)
            res._data[1 + i] = fullres[pos + i];

         return res;
      }

      /// <summary>
      ///   Performing a Mod operation on two magic numbers
      /// </summary>
      /// <param name = "num1">  the first number
      /// </param>
      /// <param name = "num2">  the first number
      /// </param>
      /// <returns> the result
      /// </returns>
      public static NUM_TYPE mod(NUM_TYPE num1, NUM_TYPE num2)
      {
         // null values
         if (num1 == null || num2 == null)
            return null;

         NUM_TYPE res = new NUM_TYPE();

         if (num2.num_is_zero())
         {
            res.NUM_ZERO();
            return res;
         }

         if (num1.NUM_IS_LONG())
            num1.num_4_std_long();
         if (num2.NUM_IS_LONG())
            num2.num_4_std_long();
         res = div(num1, num2);
         res.num_trunc(0);
         res = mul(res, num2);
         res = sub(num1, res);
         return res;
      }

      /// <summary>
      ///   Performing a Divide operation on two magic numbers
      /// </summary>
      /// <param name = "num1">  the first number
      /// </param>
      /// <param name = "num2">  the first number
      /// </param>
      /// <returns> the result
      /// </returns>
      public static NUM_TYPE div(NUM_TYPE num1, NUM_TYPE num2)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         sbyte[] dividend = new sbyte[(NUM_SIZE - 1) * 2];
         sbyte[] divisor = new sbyte[NUM_SIZE];
         int pwr;
         int len1;
         int len2;
         int pos1;
         int pos2;
         int pos;
         int quot;
         int prod;
         int carry;
         int SIGNIFICANT_NUM_SIZE;
         // null values
         if (num1 == null || num2 == null)
            return null;

         SIGNIFICANT_NUM_SIZE = Manager.Environment.GetSignificantNumSize();

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         NUM_TYPE res = new NUM_TYPE();
         OperData operData = new OperData();
         int i;
         sbyte tmpByte;

         if (num1.NUM_IS_LONG())
            num1.num_4_std_long();
         if (num2.NUM_IS_LONG())
            num2.num_4_std_long();
         operData.NUM_Exp1_ = (sbyte)(num1._data[0] & ~SIGN_MASK);
         operData.NUM_Exp2_ = (sbyte)(num2._data[0] & ~SIGN_MASK);
         if (operData.NUM_Exp1_ == 0 || operData.NUM_Exp2_ == 0)
         {
            res.NUM_ZERO();
            return res;
         }
         for (len1 = SIGNIFICANT_NUM_SIZE - 1;
              num1._data[len1] == 0;
              len1--)
         { }
         for (len2 = SIGNIFICANT_NUM_SIZE - 1;
              num2._data[len2] == 0;
              len2--)
         { }

         //-----------------------------------------------------------------------
         // initialize variables
         //-----------------------------------------------------------------------
         pos = (memcmp(num1, 1, num2, 1, len2) < 0)
                  ? 0
                  : 1;
         // memset (dividend, 0, (NUM_SIZE - 1) * 2); // Original Line
         for (i = 0;
              i < (NUM_SIZE - 1) * 2;
              i++)
            dividend[i] = 0;
         // memcpy (&dividend[pos], &num1->numfield[1], len1); // Original Line
         for (i = 0;
              i < len1;
              i++)
            dividend[pos + i] = num1._data[1 + i];
         // memcpy (&divisor[1], &num2->numfield[1], len2); // Original Line
         for (i = 0;
              i < len2;
              i++)
            divisor[1 + i] = num2._data[1 + i];

         res.NUM_SET_ZERO();
         pwr = toUByte(operData.NUM_Exp1_) - toUByte(operData.NUM_Exp2_) + pos;
         res._data[0] = toSByte(pwr + EXP_BIAS);
         if ((num1._data[0] & SIGN_MASK) != (num2._data[0] & SIGN_MASK))
            res._data[0] |= SIGN_MASK;

         //-----------------------------------------------------------------------
         // normalize dividend & divisor
         //-----------------------------------------------------------------------
         quot = 100 / (toUByte(divisor[1]) + 1);
         if (quot > 1)
         {
            carry = 0;
            for (pos2 = len2;
                 pos2 > 0;
                 pos2--)
            {
               prod = quot * toUByte(divisor[pos2]) + carry;
               carry = prod / 100;
               divisor[pos2] = toSByte(prod % 100);
            }
            carry = 0;
            for (pos1 = len1 + pos - 1;
                 pos1 >= 0;
                 pos1--)
            {
               prod = quot * toUByte(dividend[pos1]) + carry;
               carry = prod / 100;
               dividend[pos1] = toSByte(prod % 100);
            }
         }

         //-----------------------------------------------------------------------
         // divide dividend by divisor
         //-----------------------------------------------------------------------
         for (pos1 = 1;
              pos1 < SIGNIFICANT_NUM_SIZE;
              pos1++)
         {
            quot = (toUByte(dividend[pos1 - 1]) * 100 + toUByte(dividend[pos1])) / toUByte(divisor[1]);
            if (quot >= 100)
               quot = 99;

            if (quot != 0)
            {
               //-----------------------------------------------------------------
               // multiply divisor by quotient, and subtract from dividend
               //-----------------------------------------------------------------
               pos = pos1 + len2 - 1;
               carry = 0;
               for (pos2 = len2;
                    pos2 > 0;
                    pos2--)
               {
                  prod = quot * toUByte(divisor[pos2]) + carry;
                  carry = prod / 100;
                  // if ((Schar)(dividend[pos--] -= (Uchar)(prod % 100)) < 0) // Original Line
                  dividend[pos] = toSByte(toUByte(dividend[pos]) - (prod % 100));
                  tmpByte = dividend[pos];
                  pos--;
                  if (tmpByte < 0)
                  {
                     dividend[pos + 1] = toSByte(toUByte(dividend[pos + 1]) + 100);
                     carry++;
                  }
               }
               dividend[pos] = toSByte(toUByte(dividend[pos]) - carry);

               //-----------------------------------------------------------------
               // decrement quotient, and add back divisor to dividend
               //-----------------------------------------------------------------
               while (dividend[pos] < 0)
               {
                  quot--;
                  pos = pos1 + len2 - 1;
                  for (pos2 = len2;
                       pos2 > 0;
                       pos2--)
                  {
                     // if ((dividend[pos--] += divisor[pos2]) >= 100) // Original Line
                     dividend[pos] = toSByte(toUByte(dividend[pos]) + toUByte(divisor[pos2]));
                     tmpByte = dividend[pos];
                     pos--;
                     if (toUByte(tmpByte) >= 100)
                     {
                        dividend[pos + 1] = toSByte(toUByte(dividend[pos + 1]) - 100);
                        dividend[pos] = toSByte(toUByte(dividend[pos]) + 1);
                     }
                  }
               }
            }
            res._data[pos1] = toSByte(quot);
         }

         return res;
      }

      /// <summary>
      ///   Compare two magic numbers
      /// </summary>
      /// <param name = "num1">  The first number
      /// </param>
      /// <param name = "num2">  The second number
      /// </param>
      /// <returns> 0 if num1=num2, 1 if num1>num2 or -1 if num1<num2
      /// </returns>
      public static int num_cmp(NUM_TYPE num1, NUM_TYPE num2)
      {
         sbyte sign1;
         sbyte sign2;
         int cmpval;
         int l1;
         int l2;
         int tmp;
         int SIGNIFICANT_NUM_SIZE;

         if (num1 == null || num2 == null)
            return Int32.MinValue;

         SIGNIFICANT_NUM_SIZE = Manager.Environment.GetSignificantNumSize();

         if (num1.NUM_IS_LONG())
         {
            if (num2.NUM_IS_LONG())
            {
               l1 = num1.NUM_LONG();
               l2 = num2.NUM_LONG();
               if (l1 >= 0 && l2 >= 0)
               {
                  tmp = l1 - l2;
                  if (tmp == 0)
                     return (0);
                  if (tmp > 0)
                     return (1);
                  return (-1);
               }
               num2.num_4_std_long();
            }
            num1.num_4_std_long();
         }
         else if (num2.NUM_IS_LONG())
            num2.num_4_std_long();
         sign1 = (sbyte)(num1._data[0] & SIGN_MASK);
         sign2 = (sbyte)(num2._data[0] & SIGN_MASK);
         cmpval = (sign1 == sign2)
                     ? memcmp(num1, 0, num2, 0, SIGNIFICANT_NUM_SIZE)
                     : 1;
         if (sign1 != 0)
            cmpval = -cmpval;
         return (cmpval);
      }

      /// <summary>
      ///   Check if the given hex string represents a long number.
      /// </summary>
      /// <param name = "numHexStr">  The hex string to check.
      /// </param>
      /// <returns> true, if the string represents a 'long' number in NUM_TYPE.
      /// </returns>
      public static Boolean numHexStrIsLong(string numHexStr)
      {
          return (numHexStr.Substring(0, 2) == "FF");
      }
      //--------------------------------------------------------------------------
      // General protected Methods
      //--------------------------------------------------------------------------

      /// <summary>
      ///   Zeroes the Magic Number as a long zero
      /// </summary>
      public void NUM_ZERO()
      {
         setZero(true);
      }

      /// <summary>
      ///   Zeroes the Magic Number (all bytes will set to 0)
      /// </summary>
      public void NUM_SET_ZERO()
      {
         setZero(false);
      }

      /// <summary>
      ///   Getting indication for the sign of the Magic Number
      /// </summary>
      /// <returns> true in the number is negative, false if positive
      /// </returns>
      public bool num_is_neg()
      {
         if (NUM_IS_LONG())
            return ((_data[4] & 0x80) != 0);
         return ((_data[0] & SIGN_MASK) != 0);
      }

      /// <summary>
      ///   Getting indication if the Magic Number is ZERO (all bytes equals to 0)
      /// </summary>
      /// <returns> true in the number is ZERO, false if positive
      /// </returns>
      public bool num_is_zero()
      {
         if (NUM_IS_LONG())
            return (NUM_LONG() == 0);
         return (_data[0] == 0);
      }

      /// <summary>
      ///   Changing sign of Magic Number
      /// </summary>
      public void num_neg()
      {
         int l;

         if (NUM_IS_LONG())
         {
            l = -NUM_LONG();
            NUM_4_LONG(l);
            return;
         }
         if (_data[0] != 0)
            _data[0] ^= SIGN_MASK;
      }

      /// <summary>
      ///   Is the number kept in in the NUM_TYPE is a long number?
      /// </summary>
      /// <returns> true in the number is long, false if not
      /// </returns>
      public bool NUM_IS_LONG()
      {
         return (_data[0] == NUM_LONG_TYPE)
                   ? true
                   : false;
      }

      /// <summary>
      ///   Get the long value from the NUM_TYPE.
      ///   THIS FUNCTION MUST BE WRITTEN DIFERENTLY!
      /// </summary>
      /// <returns> the long number
      /// </returns>
      protected internal int NUM_LONG()
      {
         int i;

         //-----------------------------------------------------------------------
         // Special case for the lowest number
         //-----------------------------------------------------------------------
         if (_data[1] == 0 && _data[2] == 0 && _data[3] == 0 && _data[4] == -128)
            return Int32.MinValue;

         i = BitConverter.ToInt32((byte[]) (Array)_data, 1);

         return i;
      }

      /// <summary>
      ///   Rounds the NUM_TYPE number
      /// </summary>
      /// <param name = "decs">  number of digits after to decimal point to round to
      /// </param>
      public void round(int decs)
      {
         NUM_TYPE addval;

         if (NUM_IS_LONG())
            return;
         addval = new NUM_TYPE();
         addval._data[0] = (sbyte)((_data[0] & SIGN_MASK) | (EXP_BIAS - (decs >> 1)));
         addval._data[1] = (sbyte)((decs & 1) != 0
                                       ? 5
                                       : 50);

         NUM_TYPE temp = add(this, addval);
         _data = new sbyte[temp._data.Length];
         temp._data.CopyTo(_data, 0);
         num_trunc(decs);
      }

      /// <summary>
      ///   Returns a numeric expression, rounded to the specified number of decimal places
      /// </summary>
      /// <param name = "whole">the number of decimal places to which the numeric expression is rounded
      /// </param>
      public void dbRound(int whole)
      {
         NUM_TYPE addval;
         int pwr;
         int num_diff;
         int i;

         if (NUM_IS_LONG())
            return;
         // Add 5 (in the corresponding to whole position)
         addval = new NUM_TYPE();
         addval._data[0] = (sbyte)((_data[0] & SIGN_MASK) | (EXP_BIAS + ((whole + 1) >> 1)));
         addval._data[1] = (sbyte)(((whole & 1) != 0)
                                       ? 5
                                       : 50);

         NUM_TYPE temp = add(this, addval);
         _data = new sbyte[temp._data.Length];
         temp._data.CopyTo(_data, 0);

         // Put 0 in the end of the number
         pwr = (_data[0] & ~SIGN_MASK) - EXP_BIAS;
         if ((num_diff = pwr - ((whole + 1) >> 1)) >= 0)
         {
            if ((whole & 1) != 0)
            {
               _data[1 + num_diff] = (sbyte)(_data[1 + num_diff] - _data[1 + num_diff] % 10);
               num_diff++;
            }
            for (i = 0;
                 i < SIGNIFICANT_NUM_SIZE - (1 + num_diff);
                 i++)
               _data[1 + num_diff + i] = 0;
            // memset (_data[1 + num_diff], 0, SIGNIFICANT_NUM_SIZE - (1 + num_diff));
         }
         else
            NUM_SET_ZERO();
      }


      /// <summary>
      ///   Truncates the NUM_TYPE number
      /// </summary>
      /// <param name = "decs">  number of digits after to decimal point to have
      /// </param>
      public void num_trunc(int decs)
      {
         int pwr;
         int num_diff;
         int i;

         if (NUM_IS_LONG())
            return;
         pwr = (_data[0] & ~SIGN_MASK) - EXP_BIAS;
         if ((num_diff = SIGNIFICANT_NUM_SIZE - 1 - pwr - ((decs + 1) >> 1)) < 0)
            return;
         if ((num_diff < SIGNIFICANT_NUM_SIZE - 1) && ((decs & 1) != 0))
         {
            _data[SIGNIFICANT_NUM_SIZE - 1 - num_diff] =
               (sbyte)(_data[SIGNIFICANT_NUM_SIZE - 1 - num_diff] - _data[SIGNIFICANT_NUM_SIZE - 1 - num_diff] % 10);
            if (_data[SIGNIFICANT_NUM_SIZE - 1 - num_diff] == 0)
               num_diff++;
         }
         if (num_diff >= SIGNIFICANT_NUM_SIZE - 1)
         {
            NUM_ZERO();
            return;
         }
         // memset (&num->numfield[NUM_SIZE - num_diff], 0, num_diff); // Original Line
         for (i = 0;
              i < num_diff;
              i++)
            _data[SIGNIFICANT_NUM_SIZE - num_diff + i] = 0;
      }

      /// <summary>
      ///   Converts a Magic number from std long
      /// </summary>
      /// <param name = "num">  The number to be translated
      /// </param>
      public void num_4_std_long()
      {
         int slong;

         slong = NUM_LONG();
         if (slong >= 0)
            num_4_ulong(slong);
         else
         {
            num_4_ulong(-slong);
            _data[0] |= SIGN_MASK;
         }
      }

      /// <summary>
      ///   Converts a Magic number from ulong
      /// </summary>
      /// <param name = "num">  The number to be translated
      /// </param>
      protected internal void num_4_ulong(long data)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         int pwr;
         int pos;

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         int i;

         NUM_SET_ZERO();
         pos = pwr = 5;
         while (data > 0)
         {
            _data[pos--] = (sbyte)(data % 100);
            data /= 100;
         }
         if (pos < pwr)
         {
            if (pos > 0)
            {
               // memcpy (&num->numfield[1], &num->numfield[1 + pos], pwr - pos); // Original Line
               for (i = 0;
                    i < pwr - pos;
                    i++)
                  _data[1 + i] = _data[1 + pos + i];
               // memset (&num->numfield[pwr - pos + 1], 0, pos);
               for (i = 0;
                    i < pos;
                    i++)
                  _data[pwr - pos + 1 + i] = 0;
            }
            _data[0] = (sbyte)(pwr - pos + EXP_BIAS);
         }
      }

      /// <summary>
      ///   Get the long value from a Magic Number
      /// </summary>
      /// <returns> the long value of the number
      /// </returns>
      public int NUM_2_LONG()
      {
         if (NUM_IS_LONG())
            return NUM_LONG();
         else
            return num_2_long();
      }

      /// <summary>
      ///   Get the Ulong value from a Magic Number
      /// </summary>
      /// <returns> the long value of the number
      /// </returns>
      public int NUM_2_ULONG()
      {
         if (NUM_IS_LONG())
            return NUM_LONG();
         else
            return (int)num_2_ulong();
      }

      /// <summary>
      ///   Get the long value from a non long Magic Number
      /// </summary>
      /// <returns> the long value of the number
      /// </returns>
      protected internal int num_2_long()
      {
         int slong;
         int sign;

         sign = _data[0] & SIGN_MASK;
         _data[0] &= (sbyte)(~SIGN_MASK);
         if (sign != 0)
         {
            if (memcmp(this, 0, MinLONG(), 0, SIGNIFICANT_NUM_SIZE) > 0)
               return 0;
         }
         else
         {
            if (memcmp(this, 0, MaxLONG(), 0, SIGNIFICANT_NUM_SIZE) > 0)
               return 0;
         }
         slong = (int)num_2_ulong();
         if (sign != 0)
         {
            slong = -slong;
            _data[0] |= (sbyte)(sign);
         }
         return (slong);
      }

      /// <summary>
      ///   Get the ulong value from a non long Magic Number
      /// </summary>
      /// <returns> the ulong value of the number
      /// </returns>
      protected internal long num_2_ulong()
      {
         long val;
         int pwr;
         int pos;
         bool last;

         pwr = _data[0] - EXP_BIAS;
         if (pwr > 5)
            return (0);
         last = (pwr == 5);
         if (last)
            pwr = 4;

         val = 0;
         for (pos = 1;
              pos <= pwr;
              pos++)
            val = val * 100 + toUByte(_data[pos]);
         if (last)
            if (val <= (0xFFFFFFFFL - toUByte(_data[5])) / 100L)
               val = val * 100 + toUByte(_data[5]);
            else
               val = 0;

         return (val);
      }

      /// <summary>
      ///   Perform the actuall add operation
      /// </summary>
      /// <param name = "num1">      The first number
      /// </param>
      /// <param name = "num2">      The second number
      /// </param>
      /// <param name = "operData">  Information for performing the operation
      /// </param>
      /// <returns> The two numbers added
      /// </returns>
      protected internal static NUM_TYPE add_pos(NUM_TYPE num1, NUM_TYPE num2, OperData operData)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         int len1;
         int len2;
         int len;
         int pos;
         int num1ptr;
         int resptr;
         int i;
         int SIGNIFICANT_NUM_SIZE;

         // null values
         if (num1 == null || num2 == null)
            return null;
         SIGNIFICANT_NUM_SIZE = Manager.Environment.GetSignificantNumSize();

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         NUM_TYPE res = new NUM_TYPE();

         if (operData.NUM_Exp2_ == 0 || (operData.NUM_Diff_ >= SIGNIFICANT_NUM_SIZE - 1))
         {
            res = new NUM_TYPE(num1);
            res._data[0] = operData.NUM_Exp1_;
            return res;
         }

         len1 = 1;
         for (i = SIGNIFICANT_NUM_SIZE - 2;
              i > 0;
              i -= 2)
         {
            if (num1.SHRT_IS_ZERO(i))
               continue;
            else
            {
               len1 = i + 1;
               break;
            }
         }

         if (num1._data[len1] == 0)
            len1--;

         len2 = 1;
         for (i = SIGNIFICANT_NUM_SIZE - 2;
              i > 0;
              i -= 2)
         {
            if (num2.SHRT_IS_ZERO(i))
               continue;
            else
            {
               len2 = i + 1;
               break;
            }
         }

         if (num2._data[len2] == 0)
            len2--;

         if ((len = Math.Max(len1, len2 + operData.NUM_Diff_)) > SIGNIFICANT_NUM_SIZE - 1)
            len = SIGNIFICANT_NUM_SIZE - 1;

         res.NUM_SET_ZERO();
         num1ptr = operData.NUM_Diff_;
         resptr = operData.NUM_Diff_;
         for (pos = len - operData.NUM_Diff_;
              pos > 0;
              pos--)
         {
            // if ((resptr[pos] += num1ptr[pos] + num2->numfield[pos]) >= 100) // Original Line
            res._data[resptr + pos] =
               toSByte(toUByte(res._data[resptr + pos]) + toUByte(num1._data[num1ptr + pos]) + toUByte(num2._data[pos]));
            if (toUByte(res._data[resptr + pos]) >= 100)
            {
               res._data[resptr + pos] = toSByte(toUByte(res._data[resptr + pos]) - 100);
               res._data[resptr + pos - 1] = 1;
            }
         }
         for (pos = operData.NUM_Diff_;
              pos > 0;
              pos--)
         {
            res._data[pos] = toSByte(toUByte(res._data[pos]) + toUByte(num1._data[pos]));
            if (toUByte(res._data[pos]) >= 100)
            {
               res._data[pos] = toSByte(toUByte(res._data[pos]) - 100);
               res._data[pos - 1] = 1;
            }
         }
         if (res._data[0] != 0)
         {
            for (pos = Math.Min(len, SIGNIFICANT_NUM_SIZE - 2);
                 pos >= 0;
                 pos--)
               res._data[pos + 1] = res._data[pos];
            operData.NUM_Exp1_++;
         }
         res._data[0] = operData.NUM_Exp1_;

         return res;
      }

      /// <summary>
      ///   Perform the actuall sub operation
      /// </summary>
      /// <param name = "num1">      The first number
      /// </param>
      /// <param name = "num2">      The second number
      /// </param>
      /// <param name = "operData">  Information for performing the operation
      /// </param>
      /// <returns> The result of the subtraction operation
      /// </returns>
      protected internal static NUM_TYPE sub_pos(NUM_TYPE num1, NUM_TYPE num2, OperData operData)
      {
         //-----------------------------------------------------------------------
         // Original variables
         //-----------------------------------------------------------------------
         int len1;
         int len2;
         int len;
         int pos;
         int num1ptr;
         int resptr;
         int i;
         int SIGNIFICANT_NUM_SIZE;

         // null values
         if (num1 == null || num2 == null)
            return null;

         SIGNIFICANT_NUM_SIZE = Manager.Environment.GetSignificantNumSize();

         //-----------------------------------------------------------------------
         // Added variables
         //-----------------------------------------------------------------------
         NUM_TYPE res = new NUM_TYPE();
         int j;

         if (operData.NUM_Exp2_ == 0 || (operData.NUM_Diff_ >= SIGNIFICANT_NUM_SIZE - 1))
         {
            res = new NUM_TYPE(num1);
            res._data[0] = operData.NUM_Exp1_;
            return res;
         }

         len1 = 1;
         for (i = SIGNIFICANT_NUM_SIZE - 2;
              i > 0;
              i -= 2)
         {
            if (num1.SHRT_IS_ZERO(i))
               continue;
            else
            {
               len1 = i + 1;
               break;
            }
         }

         if (num1._data[len1] == 0)
            len1--;

         len2 = 1;
         for (i = SIGNIFICANT_NUM_SIZE - 2;
              i > 0;
              i -= 2)
         {
            if (num2.SHRT_IS_ZERO(i))
               continue;
            else
            {
               len2 = i + 1;
               break;
            }
         }

         if (num2._data[len2] == 0)
            len2--;

         if ((len = Math.Max(len1, len2 + operData.NUM_Diff_)) > SIGNIFICANT_NUM_SIZE - 1)
            len = SIGNIFICANT_NUM_SIZE - 1;

         res.NUM_SET_ZERO();
         num1ptr = operData.NUM_Diff_;
         resptr = operData.NUM_Diff_;
         for (pos = len - operData.NUM_Diff_;
              pos > 0;
              pos--)
         {
            // if ((Schar)(resptr[pos] += num1ptr[pos] - num2->numfield[pos]) < 0) // Original Line
            res._data[resptr + pos] =
               toSByte(toUByte(res._data[resptr + pos]) + toUByte(num1._data[num1ptr + pos]) - toUByte(num2._data[pos]));
            if (res._data[resptr + pos] < 0)
            {
               res._data[resptr + pos] = toSByte(toUByte(res._data[resptr + pos]) + 100);
               res._data[resptr + pos - 1] = unchecked((sbyte)0xFF); // 255
            }
         }
         for (pos = operData.NUM_Diff_;
              pos > 0;
              pos--)
         {
            res._data[pos] = toSByte(toUByte(res._data[pos]) + toUByte(num1._data[pos]));
            if (res._data[pos] < 0)
            {
               res._data[pos] = toSByte(toUByte(res._data[pos]) + 100);
               res._data[pos - 1] = unchecked((sbyte)0xFF); // 255
            }
         }
         while ((++pos <= len) && res._data[pos] == 0)
         { }
         if (pos <= len)
         {
            operData.NUM_Diff_ = pos - 1;
            if (operData.NUM_Diff_ > 0)
            {
               // memcpy (&res->numfield[1], &res->numfield[pos], len - operData.NUM_Diff_); // Original Line
               for (j = 0;
                    j < len - operData.NUM_Diff_;
                    j++)
                  res._data[1 + j] = res._data[pos + j];
               // memset (&res->numfield[len - operData.NUM_Diff_ + 1], 0, operData.NUM_Diff_); // Original Line
               for (j = 0;
                    j < operData.NUM_Diff_;
                    j++)
                  res._data[len - operData.NUM_Diff_ + 1 + j] = 0;
            }
            res._data[0] = toSByte(toUByte(operData.NUM_Exp1_) - operData.NUM_Diff_);
         }

         return res;
      }

      /// <summary>
      ///   Perform the actual  FIX operation with whole part of the number
      /// </summary>
      /// <param name = "wholes">number of needed whole digits
      /// </param>
      public void num_fix(int wholes)
      {
         int pwr;
         int num_diff;

         if (NUM_IS_LONG())
            num_4_std_long();
         pwr = (_data[0] & ~SIGN_MASK) - EXP_BIAS;

         if ((num_diff = pwr - ((wholes + 1) >> 1)) < 0)
            return;
         if (num_diff < SIGNIFICANT_NUM_SIZE - 1)
         {
            if ((wholes & 1) == 1)
               _data[1 + num_diff] = (sbyte)(_data[1 + num_diff] % 10);
            while ((num_diff < SIGNIFICANT_NUM_SIZE - 1) && _data[1 + num_diff] == 0)
               num_diff++;
         }
         if (num_diff >= SIGNIFICANT_NUM_SIZE - 1)
         {
            NUM_ZERO();
            return;
         }
         if (num_diff > 0)
         {
            int i;
            // memcpy(_data[1], _data[1 + num_diff], NUM_SIZE - 1 - num_diff);
            for (i = 0;
                 i < SIGNIFICANT_NUM_SIZE - 1 - num_diff;
                 i++)
               _data[1 + i] = _data[1 + num_diff + i];

            // memset (_data[NUM_SIZE - num_diff], 0, num_diff);
            for (i = 0;
                 i < num_diff;
                 i++)
               _data[SIGNIFICANT_NUM_SIZE - num_diff + i] = 0;
            _data[0] = (sbyte)(_data[0] - (sbyte)num_diff);
         }
      }


      /// <summary>
      ///   check if byte on specific position is zero or 0xFF
      /// </summary>
      /// <param name = "position">in NUM to be checked
      /// </param>
      /// <returns> is the byte zero
      /// </returns>
      protected internal bool SHRT_IS_ZERO(int pos)
      {
         return ((_data[pos] == 0 && _data[pos + 1] == 0));
      }

      //--------------------------------------------------------------------------
      // Private Methods
      //--------------------------------------------------------------------------
      /// <summary>
      ///   Zero a Magic Number. If the number should contain a long zero, the first
      ///   byte is set to NUM_LONG_TYPE.
      /// </summary>
      /// <param name = "asLong">  set the Magic Number to contain a long zero value
      /// </param>
      private void setZero(bool asLong)
      {
         int i;

         for (i = 0;
              i < NUM_SIZE;
              i++)
            _data[i] = 0;

         if (asLong)
            _data[0] = NUM_LONG_TYPE;
      }

      /// <summary>
      ///   Compare two Magic Numbers
      /// </summary>
      /// <param name = "num1">  The first number
      /// </param>
      /// <param name = "pos1">  The position of num1 to compare from
      /// </param>
      /// <param name = "num2">  The second number
      /// </param>
      /// <param name = "pos3">  The position of num2 to compare from
      /// </param>
      /// <returns> 0 if identical, 1 if num1>num2, -1 if num1<num2
      /// </returns>
      private static int memcmp(NUM_TYPE num1, int pos1, NUM_TYPE num2, int pos2, int len)
      {
         int i = 0;

         while (i < len && num1._data[pos1] == num2._data[pos2] && pos1 < NUM_SIZE && pos2 < NUM_SIZE)
         {
            i++;
            pos1++;
            pos2++;
         }

         if (i == len)
            return 0;
         else if (toUByte(num1._data[pos1]) < toUByte(num2._data[pos2]))
            return -1;
         else
            return 1;
      }

      /// <summary>
      ///   Get a unsigned value from a byte
      /// </summary>
      /// <param name = "byteVal">  A signed byte value
      /// </param>
      /// <returns> the unsigned value of the byte
      /// </returns>
      protected internal static int toUByte(sbyte byteVal)
      {
         int val = byteVal;

         if (byteVal < 0)
            val = 256 + byteVal;

         return val;
      }

      /// <summary>
      ///   Get a signed byte value from a unsigned val
      /// </summary>
      /// <param name = "signedVal">  An unsigned byte value (as an integer)
      /// </param>
      /// <returns> the signed value as a byte
      /// </returns>
      public static sbyte toSByte(int unsignedVal)
      {
         sbyte val = 0;

         if (unsignedVal > SByte.MaxValue)
            val = (sbyte)(unsignedVal - 256);
         else
            val = (sbyte)unsignedVal;

         return val;
      }

      /// <summary>
      ///   Get the maximum long number in NUM_TYPE std format
      /// </summary>
      protected internal static NUM_TYPE MaxLONG()
      {
         NUM_TYPE num = new NUM_TYPE();

         num._data[0] = (sbyte)(EXP_BIAS + 5);
         num._data[1] = 21;
         num._data[2] = 47;
         num._data[3] = 48;
         num._data[4] = 36;
         num._data[5] = 47;
         return num;
      }

      /// <summary>
      ///   Get the minimum long number in NUM_TYPE std format
      /// </summary>
      protected internal static NUM_TYPE MinLONG()
      {
         NUM_TYPE num = new NUM_TYPE();

         num._data[0] = (sbyte)(EXP_BIAS + 5);
         num._data[1] = 21;
         num._data[2] = 47;
         num._data[3] = 48;
         num._data[4] = 36;
         num._data[5] = 48;
         return num;
      }

      /// <summary>
      ///   Get double number from Magic NUM_TYPE
      /// </summary>
      public double to_double()
      {
         return storage_mg_2_float(8); // lenght of double is 8 byte
      }

      /// <summary>
      ///   Get double number from Magic NUM_TYPE
      /// </summary>
      /// <param name = "len">of byte of needed number - USE 8
      /// </param>
      private double storage_mg_2_float(int len)
      {
         int sign_pos;
         int sign;
         NUM_TYPE compnum;
         NUM_TYPE tmpnum;
         NUM_TYPE divnum;
         int pwr;
         int bits;
         int exp;
         long long1 = 0;
         long long2 = 0;
         double fltOut = 0;
         sbyte[] cout = new sbyte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
         sbyte[] Num2Pwr23_ = new sbyte[] { (sbyte)(EXP_BIAS + 4), 8, 38, 86, 8 };
         sbyte[] Num2Pwr52_ = new sbyte[] { (sbyte)(EXP_BIAS + 8), 45, 3, 59, 96, 27, 37, 4, 96 };
         sbyte[][] base_Num16Pwrs = new[]
                                       {
                                          new sbyte[] {(sbyte) (EXP_BIAS + 1), 1},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 1), 16},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 2), 2, 56},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 2), 40, 96},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 3), 6, 55, 36},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 4), 1, 4, 85, 76},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 4), 16, 77, 72, 16},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 5), 2, 68, 43, 54, 56},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 5), 42, 94, 96, 72, 96},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 6), 6, 87, 19, 47, 67, 36},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 7), 1, 9, 95, 11, 62, 77, 76},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 7), 17, 59, 21, 86, 4, 44, 16},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 8), 2, 81, 47, 49, 76, 71, 6, 56},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 8), 45, 3, 59, 96, 27, 37, 4, 96},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 9), 7, 20, 57, 59, 40, 37, 92, 79, 36}
                                       };
         sbyte[][] base_Num2Pwrs = new[]
                                      {
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 1},
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 2},
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 4},
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 8}
                                      };

         if (NUM_IS_LONG())
            num_4_std_long();
         if (_data[0] == 0)
            return 0D;
         sign = toSByte(_data[0] & SIGN_MASK);
         _data[0] &= (sbyte)(~SIGN_MASK);
         if (len == 4)
         {
            bits = 24; // 1 + 23;
            compnum = new NUM_TYPE(Num2Pwr23_);
         }
         else
         {
            bits = 53; // 1 + 20 + 32;
            compnum = new NUM_TYPE(Num2Pwr52_);
         }
         pwr = (_data[0] - EXP_BIAS) << 1;
         if (pwr < 0)
            exp = pwr * 3 + (pwr + 1) / 3;
         else
            exp = pwr * 3 + (pwr + 2) / 3;
         pwr = bits - exp;
         if (pwr < 0)
         {
            pwr = -pwr;
            tmpnum = div(this, new NUM_TYPE(base_Num16Pwrs[pwr >> 2]));
            tmpnum = div(tmpnum, new NUM_TYPE(base_Num2Pwrs[pwr & 3]));
         }
         else
         {
            tmpnum = this;
            while (pwr >= 60)
            {
               tmpnum = mul(tmpnum, new NUM_TYPE(base_Num16Pwrs[14]));
               pwr -= 56;
            }
            tmpnum = mul(tmpnum, new NUM_TYPE(base_Num16Pwrs[pwr >> 2]));
            tmpnum = mul(tmpnum, new NUM_TYPE(base_Num2Pwrs[pwr & 3]));
         }
         while (num_cmp(tmpnum, compnum) < 0)
         {
            exp--;
            tmpnum = add(tmpnum, tmpnum);
         }

         if (len == 4)
         {
            long1 = tmpnum.num_2_ulong();
         }
         else
         {
            divnum = div(tmpnum, new NUM_TYPE(base_Num16Pwrs[8]));
            divnum.num_trunc(0);
            long1 = divnum.num_2_ulong();
            divnum = mul(divnum, new NUM_TYPE(base_Num16Pwrs[8]));
            divnum = sub(tmpnum, divnum);
            long2 = divnum.num_2_ulong();
         }

         sign_pos = len - 1;
         if (len == 4)
         {
            exp += 126;
            cout[3] = toSByte(exp >> 1);
            cout[2] = toSByte((LO_CHAR(HI_SHRT(long1)) & 0x7F) | ((exp & 0x01) << 7));
            cout[1] = toSByte(HI_CHAR(LO_SHRT(long1)));
            cout[0] = toSByte(LO_CHAR(LO_SHRT(long1)));
         }
         else
         {
            exp += 1022;
            cout[7] = toSByte(exp >> 4);

            cout[6] = toSByte((LO_CHAR(HI_SHRT(long1)) & 0x0F) | ((exp & 0x0F) << 4));

            cout[5] = toSByte(HI_CHAR(LO_SHRT(long1)));

            cout[4] = toSByte(LO_CHAR(LO_SHRT(long1)));
            cout[3] = toSByte(HI_CHAR(HI_SHRT(long2)));
            cout[2] = toSByte(LO_CHAR(HI_SHRT(long2)));
            cout[1] = toSByte(HI_CHAR(LO_SHRT(long2)));
            cout[0] = toSByte(LO_CHAR(LO_SHRT(long2)));
         }

         if (sign != 0)
         {
            cout[sign_pos] |= SIGN_MASK;
            _data[0] |= SIGN_MASK;
         }
         fltOut = sbyteArr_2_Double(cout);
         return fltOut;
      }

      /// <summary>
      ///   Bit_Wise mask : Leave last byte in number
      /// </summary>
      private int LO_CHAR(int n)
      {
         return (n & 0xff);
      }

      /// <summary>
      ///   Bit_Wise mask : Leave byte before last
      /// </summary>
      private int HI_CHAR(int n)
      {
         return ((n & 0xff00) >> 8);
      }

      /// <summary>
      ///   Bit_Wise mask : Leave 2 last bytes
      /// </summary>
      private int LO_SHRT(long n)
      {
         return (int)(n & 0xffffL);
      }

      /// <summary>
      ///   Bit_Wise mask
      /// </summary>
      private int HI_SHRT(long n)
      {
         return (int)((n & unchecked((int)0xffff0000L)) >> 16);
      }

      /// <summary>
      ///   Bit_Wise mask
      /// </summary>
      private static int MK_SHRT(int c1, int c2)
      {
         String strInt = Convert.ToString((c1 << 8) | c2);
         int result = Int32.Parse(strInt);

         return result;
      }

      /// <summary>
      ///   Bit_Wise mask
      /// </summary>
      private static long MK_LONG(int s1, int s2)
      {
         long l1 = s1;
         long l2 = s2;
         String strLng = Convert.ToString((l1 << 16) | l2);

         return Int64.Parse(strLng);
      }

      /// <summary>
      ///   get the double from array of bytes
      /// </summary>
      /// <param name = "array">of bytes, inner representation of the double
      /// </param>
      /// <returns> double
      /// </returns>
      private double sbyteArr_2_Double(sbyte[] array)
      {
         return BitConverter.ToDouble(Misc.ToByteArray(array), 0);
      }

      /// <summary>
      ///   get byte array for double (8 bytes in Java)
      /// </summary>
      /// <param name = "double">to get its byte representation
      /// </param>
      /// <returns> byte array
      /// </returns>
      private static sbyte[] double_2_sbyteArray(double d)
      {
         byte[] bytearray = BitConverter.GetBytes(d);

         return Misc.ToSByteArray(bytearray);
      }

      /// <summary>
      ///   Performing an Power operation on two magic numbers Val1^Val2
      /// </summary>
      /// <param name = "num1">  the first number - number
      /// </param>
      /// <param name = "num2">  the first number - power
      /// </param>
      /// <returns> the result
      /// </returns>
      public static NUM_TYPE eval_op_pwr(NUM_TYPE num1, NUM_TYPE num2)
      {
         double d0;
         double d1;
         // null values
         if (num1 == null || num2 == null)
            return null;

         // If second operator is an integer (which is so in most of the cases)
         // and it is not -ve, then calculate power using simple multiplication
         // There is a problem in from_double as it doesn't return exact 
         // values (for example, passing 1.002441 in num to from_double would
         // return 1.00244099999999). btw, this problem exists even in Visual studio's
         // watch window (try typing 1.002441 in Name column of watch window).
         if (num2.NUM_IS_LONG() && !num2.num_is_neg())
         {
            d1 = num2.to_double();
            NUM_TYPE result = from_double(1L);
            for (long i = 0; i < d1; i++)
               result = mul(result, num1);
            return result;
         }
         else
         {
            d0 = num1.to_double();
            d1 = num2.to_double();
            if (d0 < 0.0 && d1 != (long)d1)
               d0 = 0.0;
            /* pwr (0.0, 0.0) -> error */
            /* ----------------------- */
            else if (d0 != 0.0)
               d0 = Math.Pow(d0, d1);
         }

         return from_double(d0);
      }

      /// <summary>
      ///   translate double to NUM_TYPE
      /// </summary>
      /// <param name = "double">to translate
      /// </param>
      /// <returns> NUM_TYPE of the double
      /// </returns>
      public static NUM_TYPE from_double(double d0)
      {
         sbyte[] array = double_2_sbyteArray(d0);
         // len = 8;dec = 18;
         NUM_TYPE doubl = storage_mg_4_float(8, array);
         return doubl;
      }

      /// <summary>
      ///   translate byte array  to NUM_TYPE
      /// </summary>
      /// <param name = "len">of the needed number(8)
      /// </param>
      /// <param name = "byte">array mask
      /// </param>
      /// <returns> NUM_TYPE for the needed attributes
      /// </returns>
      private static NUM_TYPE storage_mg_4_float(int len, sbyte[] inp)
      {
         int[] cinp;
         int sign;
         int tmp1;
         long exp;
         long bias;
         long long1;
         long long2;
         // need 2 turn over the stuck  - work with C functions
         // after using the functions of C stopped turn over the stuck
         cinp = new int[inp.Length];
         for (tmp1 = 0;
              tmp1 < inp.Length;
              tmp1++)
            cinp[tmp1] = toUByte(inp[tmp1]);

         sign = cinp[len - 1];
         if (len == 4)
         {
            exp = (long)((cinp[2] >> 7) | ((cinp[3] & 0x7F) << 1));
            bias = 150; // 126 + (1 + 23);
            long1 = 0;
            long2 = MK_LONG(MK_SHRT(0, cinp[2] | 0x80), MK_SHRT(cinp[1], cinp[0]));
         }
         else
         {
            exp = (long)(((cinp[7] & 0x7F) << 4) | (cinp[6] >> 4));
            bias = 1075; // 1022 + (1 + 20 + 32);
            long1 = MK_LONG(MK_SHRT(0, (cinp[6] & 0x0F) | 0x10), MK_SHRT(cinp[5], cinp[4]));
            long2 = MK_LONG(MK_SHRT(cinp[3], cinp[2]), MK_SHRT(cinp[1], cinp[0]));
         }
         return storage_num_4_fld_flt(long1, long2, 18, sign, bias, exp);
      }

      /// <summary>
      ///   translate byte array  to NUM_TYPE (like in Magic Souce code).
      /// </summary>
      private static NUM_TYPE storage_num_4_fld_flt(long long1, long long2, int dec, int sign, long bias, long expr)
      {
         sbyte[][] base_Num16Pwrs = new[]
                                       {
                                          new sbyte[] {(sbyte) (EXP_BIAS + 1), 1},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 1), 16},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 2), 2, 56},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 2), 40, 96},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 3), 6, 55, 36},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 4), 1, 4, 85, 76},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 4), 16, 77, 72, 16},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 5), 2, 68, 43, 54, 56},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 5), 42, 94, 96, 72, 96},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 6), 6, 87, 19, 47, 67, 36},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 7), 1, 9, 95, 11, 62, 77, 76},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 7), 17, 59, 21, 86, 4, 44, 16},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 8), 2, 81, 47, 49, 76, 71, 6, 56},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 8), 45, 3, 59, 96, 27, 37, 4, 96},
                                          new sbyte[] {(sbyte) (EXP_BIAS + 9), 7, 20, 57, 59, 40, 37, 92, 79, 36}
                                       };
         sbyte[][] base_Num2Pwrs = new[]
                                      {
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 1},
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 2},
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 4},
                                         new sbyte[] {(sbyte) (EXP_BIAS + 1), 8}
                                      };
         NUM_TYPE tmpnum = new NUM_TYPE();
         NUM_TYPE outVal = new NUM_TYPE();
         int exp = (int)expr;

         if (exp == 0)
         {
            outVal.NUM_ZERO();
            return outVal;
         }
         outVal.num_4_ulong(long2);
         if (long1 != 0L)
         {
            tmpnum.num_4_ulong(long1);
            tmpnum = mul(tmpnum, new NUM_TYPE(base_Num16Pwrs[8]));
            outVal = add(tmpnum, outVal);
         }
         exp = (int)(exp - bias);
         if (exp < 0)
         {
            exp = -exp;
            while (exp >= 60)
            {
               outVal = div(outVal, new NUM_TYPE(base_Num16Pwrs[14]));
               exp -= 56;
            }
            tmpnum = mul(new NUM_TYPE(base_Num16Pwrs[exp >> 2]),
                         new NUM_TYPE(base_Num2Pwrs[exp & 3]));
            outVal = div(outVal, tmpnum);
         }
         else
         {
            if (exp >= 60)
            {
               outVal.NUM_ZERO();
               return outVal;
            }
            tmpnum = mul(new NUM_TYPE(base_Num16Pwrs[exp >> 2]),
                         new NUM_TYPE(base_Num2Pwrs[exp & 3]));
            outVal = mul(outVal, tmpnum);
         }
         if ((sign & 0x80) != 0)
            outVal._data[0] |= SIGN_MASK;
         outVal.round(dec);
         return outVal;
      }

      /// <summary>
      ///   Mathematical functions implementation
      /// </summary>
      public static NUM_TYPE eval_op_log(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();

         if (d > 0.0)
            resVal = from_double(Math.Log(d));
         else
            resVal.NUM_ZERO();
         return resVal;
      }

      public static NUM_TYPE eval_op_exp(NUM_TYPE val1)
      {
         NUM_TYPE resVal;

         if (val1 == null)
            return null;
         double d = val1.to_double();
         resVal = from_double(Math.Exp(d));
         return resVal;
      }

      public static NUM_TYPE eval_op_abs(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE(val1);
         resVal.num_abs();
         return resVal;
      }

      public static NUM_TYPE eval_op_sin(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();
         resVal = from_double(Math.Sin(d));
         return resVal;
      }

      public static NUM_TYPE eval_op_cos(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();
         resVal = from_double(Math.Cos(d));
         return resVal;
      }

      public static NUM_TYPE eval_op_tan(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();
         resVal = from_double(Math.Tan(d));
         return resVal;
      }

      public static NUM_TYPE eval_op_asin(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();
         if (d <= 1.0 && d >= -1.0)
            resVal = from_double(Math.Asin(d));
         else
            resVal = from_double(0.0);
         return resVal;
      }

      public static NUM_TYPE eval_op_acos(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();

         if (d <= 1.0 && d >= -1.0)
            resVal = from_double(Math.Acos(d));
         else
            resVal = from_double(0.0);
         return resVal;
      }

      public static NUM_TYPE eval_op_atan(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;
         NUM_TYPE resVal = new NUM_TYPE();
         double d = val1.to_double();
         resVal = from_double(Math.Atan(d));
         return resVal;
      }

      public void num_abs()
      {
         int l;
         if (NUM_IS_LONG())
         {
            l = NUM_LONG();
            if (l < 0)
               l = -l;
            NUM_4_LONG(l);
            return;
         }
         _data[0] &= (sbyte)(~SIGN_MASK);
      }

      /// <summary>
      ///   Randomal Math function
      /// </summary>
      public static NUM_TYPE eval_op_rand(NUM_TYPE val1)
      {
         if (val1 == null)
            return null;

         bool rand_initialized = Randomizer.get_initialized();
         NUM_TYPE rand_mod;
         NUM_TYPE rand_mul;
         NUM_TYPE rand_seed;

         NUM_TYPE tmp_num = new NUM_TYPE();

         if (!rand_initialized)
         {
            Randomizer.set_initialized(); // set to true
            rand_mod = new NUM_TYPE();
            rand_mul = new NUM_TYPE();
            rand_seed = new NUM_TYPE();
            rand_mod.num_4_a_std("100000007");
            rand_mul.num_4_a_std("75000007");
            rand_seed.num_4_a_std("12345678");

            Randomizer.set_mod(rand_mod.to_double());
            Randomizer.set_mul(rand_mul.to_double());
            Randomizer.set_seed(rand_seed.to_double());
         }
         else
         {
            rand_mod = NUM_TYPE.from_double(Randomizer.get_mod());
            rand_mul = NUM_TYPE.from_double(Randomizer.get_mul());
            rand_seed = NUM_TYPE.from_double(Randomizer.get_seed());
         }

         if (!val1.num_is_zero())
         {
            if (val1.num_is_neg())
               rand_seed.NUM_4_LONG(hash_rand());
            else
               rand_seed = new NUM_TYPE(val1);
         }
         else
         {
            if (rand_seed.num_is_neg())
            {
               tmp_num.NUM_4_LONG(-1);
               rand_seed = mul(rand_seed, tmp_num);
            }
         }

         rand_seed = mul(rand_seed, rand_mul);
         rand_seed = mod(rand_seed, rand_mod);
         tmp_num = div(rand_seed, rand_mod);
         // reset globals variables
         Randomizer.set_mod(rand_mod.to_double());
         Randomizer.set_mul(rand_mul.to_double());
         Randomizer.set_seed(rand_seed.to_double());
         return tmp_num;
      }

      /// <summary>
      ///   get a random value to use as the ctl encription  key
      /// </summary>
      private static int hash_rand()
      {
         byte[] empty = new byte[100];

         (new Random()).NextBytes(empty); // init random array
         return (int)(Misc.getSystemMilliseconds() ^ (hash_str_new(empty)));
      }

      public static long hash_str_new(byte[] strByte)
      {
         short[] hashval = new short[] { 0, 0, 0, 0 }; // long  hashval   = 0L;
         long lng = 0;
         int pos;
         short c;
         short f;
         short tmp;
         sbyte[] str = Misc.ToSByteArray(strByte);

         for (pos = 0;
              pos < str.Length;
              pos++)
         {
            short bpos = (short)(pos & 0x000000FF); //get signed byte from a int

            //now we dont have problems of the sign bit
            c = (short)(((str[pos]) ^ bpos) & 0x00FF);

            //eliminateing unused bits
            short c0 = (short)((c << 1) & 0x00FF);
            short c1 = (short)(Misc.URShift(c, 7));
            f = (short)(c0 | c1);

            tmp = _randtbl[(short)(f ^ hashval[0]) & 0x00FF];
            // always valid index because we only work on the first 8 bits
            hashval[0] = (short)((hashval[0] ^ tmp) & 0x00FF);

            c0 = (short)((c << 2) & 0x00FF);
            c1 = (short)(Misc.URShift(c, 6));
            f = (short)(c0 | c1);
            tmp = _randtbl[(short)(f ^ hashval[1]) & 0x00FF];
            // always valid index because we only work on the first 8 bits
            hashval[1] = (short)((hashval[1] ^ tmp) & 0x00FF);


            c0 = (short)((c << 3) & 0x00FF);
            c1 = (short)(Misc.URShift(c, 5));
            f = (short)(c0 | c1);
            tmp = _randtbl[(short)(f ^ hashval[2]) & 0x00FF];
            // always valid index because we only work on the first 8 bits
            hashval[2] = (short)((hashval[2] ^ tmp) & 0x00FF);

            c0 = (short)((c << 4) & 0x00FF);
            c1 = (short)(Misc.URShift(c, 4));
            f = (short)(c0 | c1);
            tmp = _randtbl[(short)(f ^ hashval[3]) & 0x00FF];
            // always valid index because we only work on the first 8 bits
            hashval[3] = (short)((hashval[3] ^ tmp) & 0x00FF);
         }

         // from bytes to long
         lng = hashval[3];
         for (int i = hashval.Length - 2;
              i >= 0;
              i--)
         {
            lng = lng << 8;
            lng = (long)((ulong)lng | (ushort)hashval[i]);
         }

         return lng;
      }

      // This algoritm of this hash function is better that hash_str_new       
      // Pay attention that in server there is the same function and it must   
      // return the same hash value.                                           
      public static long get_hash_code(byte[] str)
      {
         uint hashval;

         int hash1 = 5381;
         int hash2 = hash1;
         int c;

         for (int i = 0;
              i < str.Length;
              i += 2)
         {
            c = str[i];
            hash1 = ((hash1 << 5) + hash1) ^ c;
            if ((i + 1) == str.Length)
               break;
            c = str[i + 1];
            hash2 = ((hash2 << 5) + hash2) ^ c;
         }

         hashval = (uint)(hash1 + (hash2 * 1566083941));
         return (hashval);
      }

      #region Nested type: OperData

      protected internal class OperData
      {
         protected internal int NUM_Diff_;
         protected internal sbyte NUM_Exp1_;
         protected internal sbyte NUM_Exp2_;
      }

      #endregion
   }
}
