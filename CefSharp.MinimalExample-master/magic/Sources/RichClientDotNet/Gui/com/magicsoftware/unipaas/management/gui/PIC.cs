using System;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.env;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   The class handles the process of converting internal data to displayed data
   ///   by using a specific format.
   ///   The class implements the PIC structure of Magic, and all its related operations.
   /// </summary>
   public class PIC
   {
      //--------------------------------------------------------------------------
      // Original members of the Magic PIC structure
      //--------------------------------------------------------------------------
      private readonly int _mskLen; // The mask len
      private readonly StringBuilder _negPref = new StringBuilder();
      private readonly StringBuilder _negSuff = new StringBuilder();
      private readonly StorageAttribute _picAttr;
      private readonly StringBuilder _posPref = new StringBuilder();
      private readonly StringBuilder _posSuff = new StringBuilder();
      private bool _autoSkip_; // Autoskip ?
      private bool _padFill; // Pad fill ?
      private bool _zeroFill; // Zero fill ?
      private bool _comma; // Display numbers with commas ?
      private int _decimalDigits; // Decimal digits
      private bool _decPointIsFirst;
      private bool _decimal; // Decimals ?
      private bool _embeded; // Embeded characters ?
      private int _formatIdx; // Current char processed in format_
      private char[] _format; // The format processed
      private bool hebrew; // Hebrew ?
      private int _imeMode = -1; // JPN: IME support
      private bool _left; // Left justified ?
      private int _maskLength;
      private int _maskChars; // Mask chars count
      private int _maskSize; // Size of picture i/o mask
      private bool _mixed; // Mixed language ?

      private static readonly IEnvironment _environment;

      //--------------------------------------------------------------------------
      // Added variables
      //--------------------------------------------------------------------------
      private char[] _msk; // The mask for the specified picture format
      private bool _needLength; //get length of the picture after parsing
      private bool _negative; // Negative values allowed ?
      private char _pad = (char) (0); // Pad character
      private int _prefLen;
      private int _size; // Size of Alpha data  without mask characters
      private int _suffLen;
      private bool _termFlag; // are terminators specified ?
      private bool _trim; // Trim ?
      private int _whole; // Whole digits
      private char _zero = (char) (0); // Zero character

      /// <summary> static block to initialize static members </summary>
      static PIC()
      {
         _environment = Manager.Environment;
      }

      /// <summary>
      ///   Builds a PIC class from picture format and attribute
      /// </summary>
      /// <param name = "picStr">The picture format string</param>
      /// <param name = "attr">The data attribute (N/A/D/T/...)</param>
      public PIC(String picStr, StorageAttribute attr, int compIdx)
      {
         _picAttr = attr;

         if (_picAttr == StorageAttribute.BLOB ||
             _picAttr == StorageAttribute.BLOB_VECTOR)
         {
            if (picStr.Trim().Length != 0)
               Events.WriteDevToLog("PIC.PIC the picture of BLOB must be empty string");
            return;
         }

         _format = picStr.ToCharArray();
         _formatIdx = 0;

         _mskLen = getMaskLen(compIdx);
         prs_all(compIdx);
      }

      /// <summary>
      /// Get information methods
      /// </summary>
      /// <returns></returns>
      public bool isHebrew()
      {
         return hebrew;
      }

      protected internal bool isMixed()
      {
         return _mixed;
      }

      protected internal bool embededChars()
      {
         return _embeded;
      }

      protected internal bool isNegative()
      {
         return _negative;
      }

      protected internal bool withComa()
      {
         return _comma;
      }

      protected internal bool isLeft()
      {
         return _left;
      }

      protected internal bool padFill()
      {
         return _padFill;
      }

      protected internal char getPad()
      {
         return _pad;
      }

      internal bool zeroFill()
      {
         return _zeroFill;
      }

      internal char getZeroPad()
      {
         return _zero;
      }

      internal bool isTrimed()
      {
         return _trim;
      }

      public bool autoSkip()
      {
         return _autoSkip_;
      }

      public bool withDecimal()
      {
         return _decimal;
      }

      protected internal bool withTerm()
      {
         return _termFlag;
      }

      internal int getMaskChars()
      {
         return _maskChars;
      }

      protected internal int getWholes()
      {
         return _whole;
      }

      public int getDec()
      {
         return _decimalDigits;
      }

      public int getSize()
      {
         return _size;
      }

      public int getMaskSize()
      {
         return _maskSize;
      }

      public int getMaskLength()
      {
         return _maskLength;
      }

      protected internal bool decInFirstPos()
      {
         return _decPointIsFirst;
      }

      protected internal String getPosPref_()
      {
         return _posPref.ToString();
      }

      protected internal String getPosSuff_()
      {
         return _posSuff.ToString();
      }

      protected internal String getNegPref_()
      {
         return _negPref.ToString();
      }

      protected internal String getNegSuff_()
      {
         return _negSuff.ToString();
      }

      public String getMask()
      {
         return new String(_msk);
      }

      public String getFormat()
      {
         return new String(_format);
      }

      internal StorageAttribute getAttr()
      {
         return _picAttr;
      }

      internal int getImeMode()
      {
         return _imeMode;
      }

      /// <summary>
      ///   Parse the picture format by the attribute
      /// </summary>
      private void prs_all(int compIdx)
      {
         if (!_needLength)
            _msk = new char[_mskLen];

         switch (_picAttr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               alpha_prs();
               break;

            case StorageAttribute.NUMERIC:
               num_prs();
               break;

            case StorageAttribute.DATE:
               date_prs(compIdx);
               break;

            case StorageAttribute.TIME:
               time_prs();
               break;

            case StorageAttribute.BOOLEAN:
               bool_prs();
               break;

            case StorageAttribute.BLOB:
               // do nothing
            case StorageAttribute.BLOB_VECTOR:
               break;
         }
      }
           
      /// <summary>
      ///   Parse an alpha picture format
      /// </summary>
      private void alpha_prs()
      {
         char currChr;
         int drv;
         int count;

         while (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            switch (currChr)
            {
               case 'H':
                  _formatIdx++;
                  setHebrew();
                  break;

               case 'A':
                  _autoSkip_ = true;
                  _formatIdx++;
                  break;

               case '#':
               case 'L':
               case 'U':
               case 'X':
                  drv = 0;
                  switch (currChr)
                  {
                     case '#':
                        drv = PICInterface.PIC_N;
                        break;

                     case 'L':
                        drv = PICInterface.PIC_L;
                        break;

                     case 'U':
                        drv = PICInterface.PIC_U;
                        break;

                     case 'X':
                        drv = PICInterface.PIC_X;
                        break;
                  }
                  _formatIdx++;
                  count = pik_count();
                  pik_drv_fill(drv, count);
                  break;

               default:
                  if (UtilStrByteMode.isLocaleDefLangDBCS()) // JPN: DBCS support
                  {
                     // JPN: IME support
                     bool useImeJpn = UtilStrByteMode.isLocaleDefLangJPN();
                     if (currChr == 'K' && useImeJpn)
                     {
                        if (pic_kanji())
                           break;
                     }

                     if (_environment.GetLocalAs400Set())
                     {
                        drv = 0;

                        if (currChr == 'J')
                           drv = PICInterface.PIC_J;
                        else if (currChr == 'T')
                           drv = PICInterface.PIC_T;
                        else if (currChr == 'G')
                           drv = PICInterface.PIC_G;
                        else if (currChr == 'S')
                           drv = PICInterface.PIC_S;

                        if (drv != 0)
                        {
                           // JPN: IME support
                           // calculate IME mode from AS/400 pictures if Kanji parameter isn't specified.
                           if (_imeMode == -1 && useImeJpn)
                           {
                              if (drv == PICInterface.PIC_J || drv == PICInterface.PIC_G)
                                 _imeMode = UtilImeJpn.IME_ZEN_HIRAGANA_ROMAN;
                              else if (drv == PICInterface.PIC_S)
                                 _imeMode = UtilImeJpn.IME_FORCE_OFF;
                           }
                           _formatIdx++;
                           count = pik_count();
                           pik_drv_fill(drv, count);
                           break;
                        }
                     }
                  }

                  if (UtilStrByteMode.isDigit(currChr))
                  {
                     count = pik_count();
                     pik_drv_fill(PICInterface.PIC_X, count);
                  }
                  else
                     pik_mask();
                  break;
            }
         }

         // In case that no picture is specified, assume all is Xs
         if (_maskSize == 0)
            pik_drv_fill(PICInterface.PIC_X, _mskLen);

         // Compute mask size without the mask characters
         _size = _maskSize;
         if (!_embeded)
            _size -= _maskChars;
      }

      /// <summary>
      ///   Parse a numeric picture format
      /// </summary>
      private void num_prs()
      {
         int i;
         int j;
         char currChr;
         int count;
         int tmp;
         int tmps;
         int tmp_c = 0;
         bool first = true;

         _zero = ' ';
         _pad = ' ';
         _negPref.Append('-');

         // Check if Comma exists
         for (i = 0;
              i < _format.Length;
              i++)
         {
            if (_format[i] == '+' || _format[i] == '-')
               break;
            if ((_format[i] == '\\') || (_format[i] == 'P'))
            {
               i++;
               continue;
            }
            if (_format[i] == 'C')
               _comma = true;
         }

         while (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            switch (currChr)
            {
               case 'N':
                  _formatIdx++;
                  _negative = true;
                  break;

               case 'C':
                  _formatIdx++;
                  _comma = true;
                  break;

               case 'L':
                  _formatIdx++;
                  _left = true;
                  break;

               case 'P':
                  _formatIdx++;
                  _padFill = true;
                  if (_formatIdx < _format.Length)
                     _pad = _format[_formatIdx++];
                  break;

               case 'Z':
                  _formatIdx++;
                  _zeroFill = true;
                  if (_formatIdx < _format.Length)
                     _zero = _format[_formatIdx++];
                  break;

               case 'A':
                  _formatIdx++;
                  _autoSkip_ = true;
                  break;

               case '.':
                  _formatIdx++;
                  if (_needLength)
                     _maskLength++;
                  _decimal = true;
                  if (_maskSize < _mskLen)
                     _msk[_maskSize] = (char) (PICInterface.PIC_N);
                  //--------------------------------------------------------------
                  // 26/11/97 Shay Z. Bug #770526 - Mark that
                  // Decimal Point is in First Position: 
                  //--------------------------------------------------------------
                  if (_maskSize == 0)
                     _decPointIsFirst = true;
                  _maskSize++;
                  break;

               case '+':
               case '-':
                  pik_sign();
                  break;

               default:
                  if (UtilStrByteMode.isDigit(currChr) || currChr == '#')
                  {
                     tmp = _formatIdx;
                     if (currChr == '#')
                        _formatIdx++;
                     count = pik_count();
                     pik_drv_fill(PICInterface.PIC_N, count);
                     if (_format[tmp] == '#')
                     {
                        if (count == 1)
                        {
                           tmps = _formatIdx;
                           _formatIdx = tmp;
                           tmp_c = pik_dup();
                           _formatIdx = tmps;
                        }
                        if (first && (tmp_c > 0 || count > 1))
                        {
                           if (count > 1)
                              tmp_c = count;
                           // add commas befor places
                           if (_comma && !_decimal)
                              pik_drv_fill(PICInterface.PIC_N, (tmp_c - 1)/3);
                           first = false;
                        }
                     }
                     // add commas befor places
                     else if (_comma && !_decimal)
                        pik_drv_fill(PICInterface.PIC_N, (count - 1)/3);

                     if (_decimal)
                        _decimalDigits += count;
                     else
                        _whole += count;
                  }
                  else
                     pik_mask();
                  break;
            }
         }

         // Store the maximal length of sign prefix and suffix between negative and positive
         _prefLen = _posPref.Length;
         _suffLen = _posSuff.Length;
         if (_negative)
         {
            if (_negPref.Length > _prefLen)
               _prefLen = _negPref.Length;
            if (_negSuff.Length > _suffLen)
               _suffLen = _negSuff.Length;
         }

         // Check for a format without digits
         if (_whole == 0 && _decimalDigits == 0)
         {
            count = PICInterface.PIC_MAX_MSK_LEN - _prefLen - _suffLen;
            if (_maskSize < count)
            {
               _whole = count - _maskSize;
               if (_comma)
                  _whole -= (_whole/4);
               pik_drv_fill(PICInterface.PIC_N, _whole);
            }
         }

         // add extra for pref/suff and commas
         if (_comma)
         {
            count = (_whole - 1)/3;
            pik_drv_fill(PICInterface.PIC_N, count);
            // remove commas places
            _maskSize -= count;
         }

         i = _prefLen;
         if (i > 0 && _msk != null)
         {
            if ((_maskSize + i) <= _mskLen)
            {
               for (j = _maskSize - 1;
                    j >= 0;
                    j--)
                  _msk[j + i] = _msk[j];
               // memset (this->msk, PIC_N, i); // Original Line
               for (j = 0;
                    j < i;
                    j++)
                  if (_msk != null && j < _msk.Length)
                     _msk[j] = (char) (PICInterface.PIC_N);
            }
            _maskSize += i;
         }
         i = _suffLen;
         if (i > 0 && _msk != null)
         {
            if (_maskSize + _suffLen <= _mskLen)
               // memset (&Msk[this->pic->mask_size], PIC_N, this->pic->suff_len); // Original Line
               for (j = 0;
                    j < _suffLen;
                    j++)
                  _msk[_maskSize + j] = (char) (PICInterface.PIC_N);
            _maskSize += i;
         }
      }

      /// <summary>
      ///   Parse a date picture format
      /// </summary>
      private void date_prs(int compIdx)
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int i;
         int ocr;
         int count;
         int drv = 0;
         bool SwitchAndExpandDate = false;
         bool SWExpandDateChecked = false;

         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         char currChr;
         bool isJpnEraYear = false; // JPN: Japanese date picture support
         _zero = ' ';
         ocr = 0;

         if (_format.Length == 0 || _format[0] == 0)
            _format = getDefaultDateMask(_environment.GetDateMode(compIdx)).ToCharArray();

         for (int j = 0; j < _format.Length; j++)
            if (_format[j] == 'H')
               setHebrew();

         while (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            switch (currChr)
            {
               case 'H':
                  _formatIdx++;
                  break;

               case 'Z':
                  _formatIdx++;
                  _zeroFill = true;
                  if (_formatIdx < _format.Length)
                     _zero = _format[_formatIdx++];
                  break;

               case 'T':
                  _formatIdx++;
                  _trim = true;
                  break;

               case 'A':
                  _formatIdx++;
                  _autoSkip_ = true;
                  break;

               case 'L':
                  if (hebrew)
                  {
                     _formatIdx++;
                     pik_drv_fill(PICInterface.PIC_HL, 1);
                  }
                  else
                     pik_mask();
                  break;

               case 'Y':
                  count = pik_dup();
                  drv = 0;
                  i = count;
                  switch (count)
                  {
                     case 1:
                        break;

                     case 2:
                        if (!hebrew)
                           drv = PICInterface.PIC_YY;
                        break;

                     case 4:
                        if (!hebrew)
                           drv = PICInterface.PIC_YYYY;
                        else
                           drv = PICInterface.PIC_HYYYYY;
                        break;

                     case 5:
                     case 6:
                        if (hebrew)
                           drv = PICInterface.PIC_HYYYYY;
                        break;
                  }
                  if (drv != 0)
                  {
                     _formatIdx += count;
                     pik_drv_fill(drv, i);
                  }
                  else
                     pik_mask();
                  break;

               case 'M':
                  count = pik_dup();
                  drv = 0;
                  if (count == 2)
                     drv = PICInterface.PIC_MMD;
                  else if (count > 2)
                  {
                     drv = PICInterface.PIC_MMM;
                     if (count > 10)
                        drv = 0;
                  }
                  if (drv != 0)
                  {
                     _formatIdx += count;
                     pik_drv_fill(drv, count);
                  }
                  else
                     pik_mask();
                  break;

               case 'D':
                  count = pik_dup();
                  drv = 0;
                  switch (count)
                  {
                     case 2:
                        if (!hebrew)
                        {
                           drv = PICInterface.PIC_DD;
                           break;
                        }
                        /* falls through! */
                        goto case 3;

                     case 3:
                        if (!hebrew)
                           drv = PICInterface.PIC_DDD;
                        else
                           drv = PICInterface.PIC_HDD;
                        break;

                     case 4:
                        if (!hebrew)
                           drv = PICInterface.PIC_DDDD;
                        else
                           pik_mask();
                        break;

                     default:
                        pik_mask();
                        break;
                  }
                  if (drv != 0)
                  {
                     pik_drv_fill(drv, count);
                     _formatIdx += count;
                  }
                  break;

               case '#':
                  count = pik_dup();
                  if (count == 2)
                  {
                     drv = 0;
                     ocr++;
                     if (!hebrew)
                        switch (_environment.GetDateMode(compIdx))
                        {
                           case 'B':
                           case 'E':
                              switch (ocr)
                              {
                                 case 1:
                                    drv = PICInterface.PIC_DD;
                                    break;

                                 case 2:
                                    drv = PICInterface.PIC_MMD;
                                    break;

                                 case 3:
                                    drv = PICInterface.PIC_YY;
                                    break;
                              }
                              break;

                           case 'A':
                              switch (ocr)
                              {
                                 case 1:
                                    drv = PICInterface.PIC_MMD;
                                    break;

                                 case 2:
                                    drv = PICInterface.PIC_DD;
                                    break;

                                 case 3:
                                    drv = PICInterface.PIC_YY;
                                    break;
                              }
                              break;

                           case 'J':
                           case 'S':
                              switch (ocr)
                              {
                                 case 1:
                                    if (!SWExpandDateChecked)
                                    {
                                       // Check if format_ contains "####"
                                       SwitchAndExpandDate = (new String(_format).IndexOf("####") > -1);
                                       SWExpandDateChecked = true;
                                    }
                                    if (SwitchAndExpandDate)
                                       pik_drv_fill(PICInterface.PIC_YYYY, 4);
                                    else
                                       drv = PICInterface.PIC_YY;
                                    break;

                                 case 2:
                                    drv = PICInterface.PIC_MMD;
                                    break;

                                 case 3:
                                    drv = PICInterface.PIC_DD;
                                    break;
                              }
                              break;
                        }
                     if (drv != 0)
                        pik_drv_fill(drv, 2);
                  }
                  else if (count == 4)
                  {
                     drv = 0;
                     ocr++;
                     if (!hebrew)
                        switch (_environment.GetDateMode(compIdx))
                        {
                           case 'B':
                           case 'E':
                           case 'A':
                              if (ocr == 3)
                                 drv = PICInterface.PIC_YYYY;
                              break;

                           case 'J':
                           case 'S':
                              if (ocr == 1)
                                 drv = PICInterface.PIC_YYYY;
                              else
                              {
                                 if (!SWExpandDateChecked)
                                 {
                                    // Check if format_ contains "####"
                                    SwitchAndExpandDate = (new String(_format).IndexOf("####") > -1);
                                    SWExpandDateChecked = true;
                                 }
                                 if (SwitchAndExpandDate && ocr == 3)
                                    pik_drv_fill(PICInterface.PIC_DD, 2);
                              }

                              break;
                        }

                     if (drv != 0)
                        pik_drv_fill(drv, 4);
                  }

                  _formatIdx += count;
                  break;


               case 'W':
                  count = pik_dup();
                  i = count;
                  if (count == 1)
                     drv = PICInterface.PIC_W;
                  else if (hebrew)
                     if (count == 5)
                        drv = PICInterface.PIC_WWW;
                     else
                        i = 0;
                  else if (count > 2 && count <= 10)
                     drv = PICInterface.PIC_WWW;
                  else
                     i = 0;
                  _formatIdx += count;
                  pik_drv_fill(drv, i);
                  break;

               case '/':
                  _formatIdx++;
                  pik_drv_fill(_environment.GetDate(), 1);
                  break;

               case 'S': // JPN: Japanese date picture support

                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     count = pik_dup();
                     i = count;

                     if (count != 2 && count != 4 && count != 6)
                        i = 0;

                     drv = PICInterface.PIC_BB;
                     _formatIdx += count;
                     pik_drv_fill(drv, i);
                  }
                  else
                  {
                     pik_mask();
                  }
                  break;

               case 'J': // JPN: Japanese date picture support

                  if (UtilStrByteMode.isLocaleDefLangJPN())
                  {
                     count = pik_dup();
                     i = count;
                     isJpnEraYear = true;

                     switch (count)
                     {
                        case 1:
                           drv = PICInterface.PIC_JY1;
                           break;

                        case 2:
                           drv = PICInterface.PIC_JY2;
                           break;

                        case 4:
                           drv = PICInterface.PIC_JY4;
                           break;

                        default:
                           i = 0;
                           isJpnEraYear = false;
                           break;
                     }

                     _formatIdx += count;
                     pik_drv_fill(drv, i);
                  }
                  else
                  {
                     pik_mask();
                  }
                  break;


               default:
                  pik_mask();
                  break;
            }
         }

         if (!_needLength && isJpnEraYear)
            // JPN: Japanese date picture support
         {
            for (i = 0;
                 i < _msk.Length;
                 i++)
            {
               if (_msk[i] == PICInterface.PIC_YY)
                  _msk[i] = (char) (PICInterface.PIC_YJ);
               else if (_msk[i] == PICInterface.PIC_YYYY)
                  _msk[i] = 'Y';
            }
         }
      }

      /// <summary>
      ///   Parse a numeric picture format
      /// </summary>
      private void time_prs()
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int drv;
         String NoFormat;
         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         char currChr;

         _zero = ' ';

         //-----------------------------------------------------------------------
         // Use a default format, if one is not specified
         //-----------------------------------------------------------------------
         if (_format.Length == 0 || _format[0] == 0)
         {
            NoFormat = "HH:MM:SS";
            if (_mskLen < 8)
               NoFormat = NoFormat.Substring(0, _mskLen); // NoFormat[this->msk_len] = 0; // Original Line
            _format = NoFormat.ToCharArray();
         }

         while (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            int maskLength;
            switch (currChr)
            {
               case 'H':
               case 'M':
               case 'S':
                  maskLength = 2;
                  /* we parse 'H/M/S' only if it has one similar character after it (i.e. 'HH'/'MM'/'SS') */
                  if (_formatIdx + 1 < _format.Length && _format[_formatIdx] == _format[_formatIdx + 1])
                  {
                     switch (currChr)
                     {
                        case 'H':
                           drv = PICInterface.PIC_HH;
                           break;

                        case 'M':
                           drv = PICInterface.PIC_MMT;
                           break;

                        case 'S':
                           drv = PICInterface.PIC_SS;
                           break;

                        default:
                           drv = PICInterface.NULL_CHAR;
                           break;
                     }
                     _formatIdx += maskLength;
                     pik_drv_fill(drv, maskLength);
                  }
                  else
                     pik_mask();
                  break;

               case 'm':
                  maskLength = 3;
                  /* we parse 'm' only if it has 2 similar characters after it (i.e. 'mmm') */
                  if (_formatIdx + 2 < _format.Length &&
                      (_format[_formatIdx] == _format[_formatIdx + 1] &&
                       _format[_formatIdx + 1] == _format[_formatIdx + 2]))
                  {
                     drv = PICInterface.PIC_MS;
                     _formatIdx += maskLength;
                     pik_drv_fill(drv, maskLength);
                  }
                  else
                     pik_mask();
                  break;

               case ':':
                  _formatIdx++;
                  pik_drv_fill(_environment.GetTime(), 1);
                  break;

               case 'Z':
                  _formatIdx++;
                  _zeroFill = true;
                  if (_formatIdx < _format.Length)
                     _zero = _format[_formatIdx++];
                  break;

               case 'A':
                  _formatIdx++;
                  _autoSkip_ = true;
                  break;

               case 'P':
                  if (_format[_formatIdx + 1] == 'M')
                     // changed from format_[1] -> format_[formatIdx_+1] 
                     // TODO :check in cpp if same thing wromng ??? 'time_prs.cpp'
                  {
                     _formatIdx += 2;
                     pik_drv_fill(PICInterface.PIC_PM, 2);
                  }
                  else
                     pik_mask();
                  break;

               default:
                  pik_mask();
                  break;
            }
         }
      }

      /// <summary>
      ///   Build Picture for the variable of the 'type' with 'value'
      /// </summary>
      /// <param name = "type">of the picture</param>
      /// <param name = "value">is needed to be evaluated with the picture</param>
      /// <returns> PICture of the needed 'type'</returns>
      public static PIC buildPicture(StorageAttribute type, String val, int compIdx, bool useDecimal)
      {
         PIC pic = null;
         StringBuilder mask = new StringBuilder(10);
         char decimalSeparator, thousands;
         char currChar;
         int counter;
         int length;
         int beforeDec, afterDec;
         bool decimalDelimFound;
         bool isThousandsDelim, isNegative;

         switch (type)
         {
            case StorageAttribute.NUMERIC:
               decimalSeparator = _environment.GetDecimal();
               thousands = _environment.GetThousands();
               beforeDec = afterDec = 0;
               decimalDelimFound = false;
               isThousandsDelim = false;
               isNegative = false;

               if (val == null || val.Length == 0)
                  beforeDec = afterDec = 1;
               else
               {
                  length = val.Length;
                  counter = 0;

                  while (counter < length)
                  {
                     currChar = val[counter];
                     switch (currChar)
                     {
                        case '+':
                        case '-':
                           isNegative = true;
                           break;


                        default:
                           if (useDecimal && currChar == decimalSeparator)
                              decimalDelimFound = true;
                           else if (currChar == thousands)
                              isThousandsDelim = true;
                           else if (decimalDelimFound)
                              afterDec++;
                           else
                              beforeDec++;
                           break;
                     }
                     counter++;
                  } //end of while for looking in the value

                  mask.Append("" + beforeDec + decimalSeparator + afterDec);
                  if (isNegative)
                     mask.Append('N');
                  if (isThousandsDelim)
                     mask.Append('C');
               }
               break;


            case StorageAttribute.DATE:
               mask.Append(getDefaultDateMask(_environment.GetDateMode(compIdx)));
               break;


            case StorageAttribute.TIME:
               mask.Append("HH" + _environment.GetTime() + "MM" + _environment.GetTime() + "SS");

               break;


            default:
               Events.WriteDevToLog("Event.buildPicture() there is no PICTURE building for type :" + type +
                                           " and value :" + val);
               return pic; //null, the PIC didn't build
         }

         pic = new PIC(mask.ToString(), type, compIdx);
         return pic;
      }

      /// <summary>
      ///   set Hebrew to true
      /// </summary>
      internal void setHebrew()
      {
         // Defect 116258. Set hebrew flag only for hebrew mgconst.
         if (Manager.Environment.Language == 'H')
            hebrew = true;
      }

      /// <summary>
      ///   get default mask for Date type
      /// </summary>
      /// <param name = "dataMode">- data mode (J,S,B,E,A)</param>
      /// <returns> mask of default date</returns>
      protected internal static String getDefaultDateMask(char dataMode)
      {
         String frmt;
         switch (dataMode)
         {
            case 'J':
            case 'S':
               frmt = "####/##/##";
               break;

            case 'B':
            case 'E':
            case 'A':
            default:
               frmt = "##/##/####";
               break;
         }
         return frmt;
      }

      /// <summary>
      ///   Parse a numeric picture format
      /// </summary>
      private void bool_prs()
      {
         //-----------------------------------------------------------------------
         // Original vars
         //-----------------------------------------------------------------------
         int count; // Count of direvtive or mask character
         //-----------------------------------------------------------------------
         // Added vars
         //-----------------------------------------------------------------------
         char currChr;

         while (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            switch (currChr)
            {
               case 'H':
                  _formatIdx++;
                  setHebrew();
                  break;


               case 'A':
                  _formatIdx++;
                  _autoSkip_ = true;
                  break;


               case 'X':
                  _formatIdx++;
                  count = pik_count();
                  pik_drv_fill(PICInterface.PIC_X, count);
                  break;


               default:
                  if (UtilStrByteMode.isDigit(currChr))
                  {
                     count = pik_count();
                     pik_drv_fill(PICInterface.PIC_X, count);
                  }
                  else
                     pik_mask();
                  break;
            }
         }

         //-----------------------------------------------------------------------
         // Fill in an empty format
         //-----------------------------------------------------------------------
         if (_maskSize == 0)
            pik_drv_fill(PICInterface.PIC_X, _mskLen);
      }

      /// <summary>
      ///   Parse the sign prefix/suffix from the format
      /// </summary>
      private void pik_sign()
      {
         bool posSign;
         StringBuilder signStr;
         char currChr;

         posSign = _format[_formatIdx] == '+'
                      ? true
                      : false;
         _formatIdx++;

         if (posSign)
            signStr = _posPref;
         else
         {
            _negPref.Remove(0, _negPref.Length);
            signStr = _negPref;
         }

         while (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            switch (currChr)
            {
               case ';':
                  _formatIdx++;
                  return;


               case ',':
                  _formatIdx++;
                  if (posSign)
                     signStr = _posSuff;
                  else
                     signStr = _negSuff;
                  break;


               case '\\':
                  _formatIdx++;
                  if (_formatIdx >= _format.Length)
                     return;
                  currChr = _format[_formatIdx];
                  goto default;

               default:
                  signStr.Append(currChr);
                  _formatIdx++;
                  break;
            }
         }
      }

      /// <summary>
      ///   Parse a number which is a counter of specific char in the format
      /// </summary>
      private int pik_count()
      {
         long val;

         val = 0;

         while (_formatIdx < _format.Length && UtilStrByteMode.isDigit(_format[_formatIdx]))
         {
            val *= 10;
            val += _format[_formatIdx] - '0';
            _formatIdx++;
         }

         if (val > 32000)
            val = 32000;
         else if (val <= 0)
            val = 1;

         if (val > Int32.MaxValue)
            throw new ApplicationException(Events.GetMessageString(MsgInterface.STR_ERR_MAX_VAR_SIZE));

         return (int) val;
      }

      /// <summary>
      ///   Fill a specific mask character in the mask string
      /// </summary>
      /// <param name = "drv">    The mask character to fill</param>
      /// <param name = "count">  Number of times for the mask character to apear</param>
      private void pik_drv_fill(int drv, int count)
      {
         int MskZ;
         int i;

         if (_needLength)
         {
            _maskLength += count;
            return;
         }

         MskZ = _maskSize;

         _maskSize += count;
         if (MskZ + count > _mskLen)
            count = _mskLen - MskZ;
         if (count > 0)
            // memset (&this->msk[MskZ], (Uchar) drv, count); // Original Line
            for (i = 0;
                 i < count;
                 i++)
               _msk[MskZ + i] = (char) drv;
         return;
      }

      /// <summary>
      ///   Parse a general mask character
      /// </summary>
      private void pik_mask()
      {
         char currChr;
         int count;

         if (_format[_formatIdx] == '\\')
            _formatIdx++;
         if (_formatIdx < _format.Length)
         {
            currChr = _format[_formatIdx];
            _formatIdx++;
            count = pik_count();
            pik_drv_fill(currChr, count);
            _maskChars += count;
         }
      }

      /// <summary>
      ///   Count continues occurrences of a mask character in the format string
      /// </summary>
      private int pik_dup()
      {
         int dupCount = 1;
         char c;
         int tmpIdx = _formatIdx;

         c = _format[tmpIdx];
         tmpIdx++;
         while (tmpIdx < _format.Length && c == _format[tmpIdx])
         {
            dupCount++;
            tmpIdx++;
         }

         return dupCount;
      }

      /// <summary>
      ///   set IME mode depends on K picture (K0 - K9)
      ///   (JPN: IME support)
      /// </summary>
      private bool pic_kanji()
      {
         int count;

         if (_formatIdx + 1 >= _format.Length)
            return false;

         if (!UtilStrByteMode.isDigit(_format[_formatIdx + 1]))
            return false;

         _formatIdx++;

         if (_format[_formatIdx] == '0')  // K0
         {
            _imeMode = UtilImeJpn.IME_FORCE_OFF;
            _formatIdx++;
         }
         else
         {
            count = pik_count();
            if (1 <= count && count <= 9) // K1 - K9
               _imeMode = count;
         }
         return true;
      }

      /// <summary>
      ///   Find length of the picture after parsing the picture
      /// </summary>
      /// <returns> length of the picture</returns>
      private int getMaskLen(int compIdx)
      {
         _needLength = true;
         _maskLength = 0;
         _formatIdx = 0;

         prs_all(compIdx);

         //add prefix and suffix length to the mask
         if (_picAttr == StorageAttribute.NUMERIC && (_prefLen > 0 || _suffLen > 0))
            _maskLength += (_prefLen + _suffLen);

         _needLength = false;
         //init for next running :
         _formatIdx = _maskChars = _whole = _decimalDigits = _size = _maskSize = _prefLen = _suffLen = 0;
         _pad = _zero = (char) (0);
         hebrew = _mixed = _embeded = _negative = _comma = _left = _padFill = _zeroFill = _trim = _autoSkip_ = _decimal = _termFlag = _decPointIsFirst = false;
         _posPref.Remove(0, _posPref.Length);
         _posSuff.Remove(0, _posSuff.Length);
         _negPref.Remove(0, _negPref.Length);
         _negSuff.Remove(0, _negSuff.Length);
         return _maskLength;
      }

      /// <summary>
      ///   returns true if there is an embeded char in the mask in a givven position.
      /// </summary>
      /// <param name = "pos"></param>
      /// <returns> true if pic is mask</returns>
      public bool picIsMask(int pos)
      {
         if (pos < 0 || pos >= _maskLength)
            pos = 0;
         return _msk[pos] > PICInterface.PIC_MAX_OP;
      }

      /// <summary>
      ///   returns the number of chars till the end of the sequence.
      /// </summary>
      /// <param name = "pos"></param>
      /// <returns> number of chars till the end of the sequence</returns>
      public int getDirectiveLen(int pos)
      {
         char drv = _msk[pos];
         int currPos = pos;
         // till the end of the mask, if mask[pos] has same directive as ours, this is the same sequence.
         while (currPos < _maskSize && drv == _msk[currPos])
            currPos++;

         // return only how many chars to the end, including our pos.
         return (currPos - pos);
      }

      /// <summary>
      ///   returns the number of chars till the end of the sequence.
      /// </summary>
      /// <param name = "mskPos"></param>
      /// <param name = "bufPos"></param>
      /// <param name = "buf"></param>
      /// <returns></returns>
      public int getDirectiveLen(int mskPos, int bufPos, String buf)
      {
         char drv = _msk[mskPos];
         int currMskPos = mskPos;
         int currBufPos = bufPos;

         // till the end of the mask, if mask[pos] has same directive as ours, this is the same sequence.
         while (currMskPos < _maskSize && drv == _msk[currMskPos])
         {
            // Ignore the change of sequence if it is in-between a DBCS char.
            // If the current char is a Full Width char, then the next mask bit 
            // is actually not used. So, ignore that bit and move on.
            if (currBufPos < buf.Length && UtilStrByteMode.isLocaleDefLangDBCS() && isAttrAlphaOrDate())
            {
               if (!UtilStrByteMode.isHalfWidth(buf[currBufPos++]))
                  currMskPos++;
            }

            currMskPos++;
         }

         // return only how many chars to the end, including our pos.
         return (currMskPos - mskPos);
      }

      /// <summary>
      ///   check if char is valid in the given mask position
      /// </summary>
      /// <param name = "charToValidate">
      /// </param>
      /// <param name = "pos">of directive mask to check
      ///   The returned char might be different then the char that was checked.
      ///   The reason is that 'U' and 'L' directive might change it, and in numeric,
      ///   decimal separator and thousand separator might be changed as well while typing.
      /// </param>
      /// <returns> 0 if not valid or a char if valid.</returns>
      public char validateChar(char charToValidate, int pos)
      {
         char charReturned = charToValidate;
         if (pos >= getMaskSize())
            return charReturned;

         // we need to skip any characters which are embeded into the picture
         while (picIsMask(pos))
            pos++;

         switch (_msk[pos])
         {
            case (char) (PICInterface.PIC_U):
               charReturned = Char.ToUpper(charToValidate);
               break;

            case (char) (PICInterface.PIC_L):
               charReturned = Char.ToLower(charToValidate);
               goto default;

            default:
               if (!UtilStrByteMode.isDigit(charToValidate))
                  for (int i = 0;
                       i < PICInterface.NumDirective.Length;
                       i++)
                     if (_msk[pos] == PICInterface.NumDirective[i])
                     {
                        charReturned = isValidInNum(charToValidate);
                        break;
                     }
               break;
         }
         return charReturned;
      }

      /// <summary>
      ///   Checks whether a non digit character is valid in a numeric mask
      /// </summary>
      /// <param name = "letter:">a character to be checked.</param>
      /// <returns> 0 if not valid. char if valid. char might be different then letter.</returns>
      internal char isValidInNum(char letter)
      {
         char charRet = (char) (0);
         char decimalChar = _environment.GetDecimal();

         /***********************************************************************/
         /*  replace '.' with a different char for numeric seperator only if
         *  SpecialModifiedDecimalSeparator = Y*/
         // for numeric, if '.' was entered, change it to the env seperator.
         // #931784: If SpecialModifiedDecimalSeparator = Y then replace '.' by decimal separator.
         if (_environment.CanReplaceDecimalSeparator() && isAttrNumeric())
            if (letter == '.')
               letter = decimalChar;

         switch (letter)
         {
            case ' ':
            case '-':
            case '*':
            case '+':
            case '/':
               charRet = letter;
               break;

            default:
               // the env decimal seperator and thousands seperator are also valid
               if (letter == decimalChar || letter == _environment.GetThousands())
                  charRet = letter;
               break;
         }

         return charRet;
      }

      /// <summary>
      ///   check if the attribute is logical
      /// </summary>
      /// <returns></returns>
      public bool isAttrLogical()
      {
         return (_picAttr == StorageAttribute.BOOLEAN);
      }

      /// <summary>
      ///   check if the attribute is numeric
      /// </summary>
      /// <returns></returns>
      public bool isAttrNumeric()
      {
         return (_picAttr == StorageAttribute.NUMERIC);
      }

      /// <summary>
      ///   check if the attribute is alpha
      /// </summary>
      /// <returns></returns>
      public bool isAttrAlpha()
      {
         return (_picAttr == StorageAttribute.ALPHA);
      }

      /// <summary>
      ///   check if the attribute is unicode
      /// </summary>
      /// <returns></returns>
      public bool isAttrUnicode()
      {
         return (_picAttr == StorageAttribute.UNICODE);
      }

      /// <summary>
      ///   check if the attribute is alpha or date
      /// </summary>
      /// <returns></returns>
      public bool isAttrAlphaOrDate()
      {
         return (_picAttr == StorageAttribute.ALPHA || _picAttr == StorageAttribute.DATE);
      }

      /// <summary>
      ///   check if the attribute is time or date
      /// </summary>
      /// <returns></returns>
      public bool isAttrDateOrTime()
      {
         return (_picAttr == StorageAttribute.TIME || _picAttr == StorageAttribute.DATE);
      }

      /// <summary>
      ///   check if the attribute is blob
      /// </summary>
      /// <returns></returns>
      public bool isAttrBlob()
      {
         return (_picAttr == StorageAttribute.BLOB ||
                 _picAttr == StorageAttribute.BLOB_VECTOR);
      }

      /// <summary>
      ///   check if the mask at a given position is numeric
      /// </summary>
      /// <param name = "pos"></param>
      /// <returns></returns>
      public bool isNumeric(int pos)
      {
         return _msk[pos] == PICInterface.PIC_N;
      }

      /// <summary>
      ///   check if the mask is all X
      /// </summary>
      /// <returns></returns>
      public bool isAllX()
      {
         return (_msk[0] == PICInterface.PIC_X && getDirectiveLen(0) == _maskSize);
      }

      /// <summary>
      ///   check is mask at position pos is same as parameter maskPic
      /// </summary>
      /// <param name = "pos"></param>
      /// <param name = "maskPic"></param>
      /// <returns></returns>
      public bool isMaskPicEq(int pos, int maskPic)
      {
         return (_msk[pos] == maskPic);
      }

      /// <summary>
      ///   check if the input char is valid for local AS400 (system i) pictures.
      ///   (DBCS Support)
      /// </summary>
      /// <param name = "charToValidate"></param>
      /// <param name = "firstChar"></param>
      /// <param name = "pos"></param>
      /// <returns> 0 if not valid or a char if valid.</returns>
      public bool isValidChar_as400(char charToValidate, char firstChar, int pos)
      {
         bool ret = true;

         switch (_msk[pos])
         {
            case (char) (PICInterface.PIC_J):
               // only full-width characters are allowed
            case (char) (PICInterface.PIC_G):
               ret = UtilStrByteMode.isHalfWidth(charToValidate)
                        ? false
                        : true;
               break;

            case (char) (PICInterface.PIC_T): // mixture of full-width and half-width is not allowed
               if (firstChar == '\x0000')
                  ret = true;
               else if (UtilStrByteMode.isHalfWidth(firstChar))
                  ret = UtilStrByteMode.isHalfWidth(charToValidate)
                           ? true
                           : false;
               else
                  ret = UtilStrByteMode.isHalfWidth(charToValidate)
                           ? false
                           : true;
               break;

            case (char) (PICInterface.PIC_S): // only half-width characters are allowed
               ret = UtilStrByteMode.isHalfWidth(charToValidate)
                        ? true
                        : false;
               break;

            default:
               break;
         }
         return ret;
      }

      /// <summary>
      ///   returns the minimum value length
      /// </summary>
      internal int getMinimumValueLength()
      {
         int minLength = getMaskLength();

         // traverse from right till a valid mask char is encountered
         while (minLength > 0 && !picIsMask(minLength - 1))
            minLength--;

         return minLength;
      }

      public override string ToString()
      {
         return "{Picture: " + (new String(_format)) + "}";
      }
   }
}
