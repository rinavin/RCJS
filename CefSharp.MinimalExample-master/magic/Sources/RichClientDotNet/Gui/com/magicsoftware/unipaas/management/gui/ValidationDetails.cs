using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
   public class ValidationDetails
   {
      // ATTENTION !!! when you add/modify/delete a member variable don't forget to take care
      // about the copy constructor: ValidationDetails(ValidationDetails vd)

      private readonly MgControlBase _control;
      private readonly FieldValidator _fieldValidator;
      private readonly PIC _picData;
      private readonly StringBuilder _pictureEnable; // for inner (of Numeric type) use only, String of 1's -> possible to write ; 0's -> impossible to write ; for '\' using
      private readonly StringBuilder _pictureReal; // for inner (of Numeric type) use only
      private List<String> _continuousRangeValues; //can't use List<T> - uses Clone()
      private List<String> _discreteRangeValues; //can't use List<T> - uses Clone()
      private bool _isNull;
      private String _oldvalue; // old value of the control
      private String _range; // 'compressed' string of the range
      private String _val; // new value, inserted into control
      private bool _validationFailed;

      /// <summary>
      ///   Constructor, which sets all member values
      /// </summary>
      public ValidationDetails(String oldvalue, String val, String range, PIC pic, MgControlBase control)
         : this()
      {
         _picData = pic;
         _oldvalue = oldvalue; // old value of the field
         _val = val; // new  inserted value of the field
         _range = range;
         _control = control;

         switch (pic.getAttr())
         {
            case StorageAttribute.NUMERIC:
               _pictureReal = new StringBuilder(_picData.getMaskSize());
               _pictureEnable = new StringBuilder(_picData.getMaskSize());
               getRealNumericPicture();
               break;

            case StorageAttribute.BOOLEAN:
               if (_range == null)
                  _range = "True,False"; // default range value for Logical
               goto case StorageAttribute.DATE;

            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
            case StorageAttribute.ALPHA:
            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
            case StorageAttribute.UNICODE:
               break;

            default:
               Events.WriteExceptionToLog("ValidationDetails.ValidationDetails: There is no type " +
                                      _picData.getAttr());
               break;
         }
         // get Range for the picture:
         if (_range != null)
            fillRange();

         // free memory
         range = null;
      }

      /// <summary>
      ///   constructor for range only, make parsing of  Range and fill
      ///   DiscreteRangeValues and ContinuousRangeValues
      ///   (used in DisplayConventor)
      /// </summary>
      /// <param name = "range">string</param>
      public ValidationDetails(String rangeStr) : this()
      {
         _range = rangeStr;
         if (_range != null)
            fillRange();
      }

      /// <summary>
      ///   Make copy of validation details, 
      ///   copy constructor
      /// </summary>
      public ValidationDetails(ValidationDetails vd) : this()
      {
         _oldvalue = vd.getOldValue();
         _val = vd._val;
         _range = vd.getRange();
         _control = vd.getControl();
         _validationFailed = false;
         if (vd._pictureReal != null)
            _pictureReal = new StringBuilder(vd._pictureReal.ToString());
         if (vd._pictureEnable != null)
            _pictureEnable = new StringBuilder(vd._pictureEnable.ToString());
         if (vd.getDiscreteRangeValues() != null)
            _discreteRangeValues = vd.CloneDiscreteRangeValues();
         if (vd.getContinuousRangeValues() != null)
            _continuousRangeValues = vd.CloneContinuousRangeValues();
         _picData = vd.getPIC();
         _isNull = vd.getIsNull();
      }

      /// <summary>
      ///   for inner class using only
      /// </summary>
      private ValidationDetails()
      {
         _fieldValidator = new FieldValidator();
      }

      public bool ValidationFailed
      {
         get { return _validationFailed; }
      }

      /// <summary>
      ///   ***********************Numeric*****************************************
      /// </summary>
      private void getRealNumericPicture()
      {
         String mask = _picData.getMask();
         int dec_ = _picData.getDec();
         int whole_ = _picData.getWholes();
         bool decimal_ = _picData.withDecimal();
         bool dec_pnt_in_first = _picData.decInFirstPos();
         bool isNegative = _picData.isNegative();
         bool isFirstPic_N = true;
         bool isPointInserted = false;

         int currChar;

         for (int i = 0; i < _picData.getMaskSize(); i++)
         {
            currChar = mask[i]; // it's ASCII of the char
            switch (currChar)
            {
               case PICInterface.PIC_N:
                  if (isFirstPic_N)
                  {
                     if (isNegative)
                        setPictures('-', '0');

                     if (dec_pnt_in_first)
                     {
                        setPictures('.', '1');
                        isPointInserted = true;
                     }
                     else if (whole_ > 0)
                     {
                        whole_--;
                        setPictures('#', '0');
                     }
                     isFirstPic_N = false;
                     break;
                  }

                  if (decimal_)
                  {
                     if (whole_ > 0)
                     {
                        whole_--;
                        setPictures('#', '0');
                     }
                     else if (whole_ == 0 && !isPointInserted)
                     {
                        setPictures('.', '1');
                        isPointInserted = true;
                     }
                     else if (dec_ > 0)
                     {
                        dec_--;
                        setPictures('#', '0');
                     }
                  }
                  else
                  {
                     if (whole_ > 0)
                     {
                        whole_--;
                        setPictures('#', '0');
                     }
                  }
                  break;

               default: // const string in Alpha
                  setPictures((char) currChar, '1');
                  break;
            }
         }
         Events.WriteDevToLog(_pictureReal.ToString());
         Events.WriteDevToLog(_pictureEnable.ToString());
      }

      /// <summary>
      ///   ***********************Range*****************************************
      /// </summary>
      /// <summary>
      ///   Fill Range structures in the class
      /// </summary>
      private void fillRange()
      {
         _range = StrUtil.makePrintableTokens(_range, StrUtil.SEQ_2_HTML);
         // find delimiters ',' which has not '\,' => RealDelimeters
         int from = 0, prev = 0;
         while ((from = findDelimeter(from, ",", _range)) != - 1 && prev < _range.Length)
         {
            rangeForms(_range.Substring(prev, (from) - (prev)));
            prev = ++from;
         }
         // last element:
         if (prev < _range.Length)
            rangeForms(_range.Substring(prev));
         // range=null; // don't delete it
         if (_discreteRangeValues != null)
         {
            StrUtil.makePrintableTokens(_discreteRangeValues, StrUtil.HTML_2_STR);
            Events.WriteDevToLog(_discreteRangeValues.ToString());
         }
         if (_continuousRangeValues != null)
         {
            StrUtil.makePrintableTokens(_continuousRangeValues, StrUtil.HTML_2_STR);
            Events.WriteDevToLog(Misc.CollectionToString(_continuousRangeValues));
         }
      }

      /// <summary>
      ///   Fill Vectors of DiscreterangeValues and ContinuousRangeValues
      ///   DiscreteRangeValues: every member is discrete value of the range
      ///   ContinuousRangeValues: [LOW border, TOP border, ...]
      /// </summary>
      /// <param name = "found">- String to insert to DiscreteRangeValues, or parse to ContinuousRangeValues</param>
      private void rangeForms(String found)
      {
         int minus = 0;
         if ((minus = findDelimeter(0, "-", found)) == - 1)
            // not found continuous range: num1-num2
         {
            found = found.TrimStart(null);
            int todel = 0;
            while (todel < found.Length && (todel = findDelimeter(todel, "\\", found)) != - 1)
               // to delete '\' and replace '\\' with '\'
            {
               found = found.Substring(0, todel) + found.Substring(++todel);
               todel++;
            }
            if (_discreteRangeValues == null)
               _discreteRangeValues = new List<String>();
            _discreteRangeValues.Add(found);
         }
         else
         {
            // index of minus found in if statement
            String val;
            for (int i = 0; i < 2; i++)
            {
               if (i == 0)
                  val = found.Substring(0, minus);
               else
               {
                  minus++;
                  // skip blanks between '-' and the value. Heading blanks cause to wrong values.
                  while (minus < found.Length && found[minus] == ' ')
                     minus++;

                  val = found.Substring(minus);
               }
               val = deleteChar("\\", val);
               if (_continuousRangeValues == null)
                  _continuousRangeValues = new List<String>();
               _continuousRangeValues.Add(val); // can be number(float) for numeric or string for Alpha
            }
         }
      }

      /// <summary>
      ///   delete String/char from the string
      /// </summary>
      /// <param name = "delete">String to delete</param>
      /// <param name = "String">delete from the String</param>
      private String deleteChar(String delete, String from)
      {
         int index = 0;
         while ((index = from.IndexOf(delete, index)) != - 1)
            from = from.Substring(0, index) + from.Substring(index + delete.Length);
         return from;
      }

      /// <summary>
      ///   Find real delimiter, there is no symbol '\' before REAL delimiter
      /// </summary>
      /// <param name = "start">of string, looking in</param>
      /// <param name = "delim">to found</param>
      /// <returns> place of found real delimiter, or -1 -> not found</returns>
      private int findDelimeter(int start, String delim, String str)
      {
         int found = str.IndexOf(delim, start);
         while (found < str.Length && found > 0)
            // => found!=-1(not found); found!=0(not first letter)
         {
            if (str[found - 1] == '\\')
               // go to next char
            {
               found = str.IndexOf(delim, found + 1);
            }
            else
               break;
         }
         return found;
      }

      /// <summary>
      ///   add chars to Real and Enable String Buffers
      /// </summary>
      /// <param name = "real">to Real</param>
      /// <param name = "Enable">to Enable</param>
      private void setPictures(char Real, char Enable)
      {
         _pictureEnable.Append(Enable);
         _pictureReal.Append(Real);
      }

      public void setValue(String val)
      {
         _val = val;
      }

      public void setOldValue(String oldvalue)
      {
         _oldvalue = oldvalue;
      }

      protected internal void setRange(String range)
      {
         _range = range;
      }

      /// <summary>
      ///   set the "validation failed" flag
      /// </summary>
      /// <param name = "val">the value of the flag</param>
      protected internal void setValidationFailed(bool val)
      {
         _validationFailed = val;
      }

      /// <summary>
      ///   set isNull flag of the ValDetails
      /// </summary>
      protected internal void setNull(bool isNull_)
      {
         _isNull = isNull_;
      }

      /// <summary>
      ///   returns the valid display value (with mask)
      /// </summary>
      public String getDispValue()
      {
         int minimumValueLength = _picData.getMinimumValueLength();

         // RTrim the value beyond this index
         if (_val.Length > minimumValueLength)
         {
            if (minimumValueLength > 0 || StrUtil.rtrim(_val).Length != 0)
            {
               String str = _val.Substring(minimumValueLength);
               _val = _val.Substring(0, minimumValueLength);
               _val = String.Concat(_val, StrUtil.rtrim(str));
            }
         }
         return _val;
      }

      protected internal String getOldValue()
      {
         return _oldvalue;
      }

      protected internal StorageAttribute getType()
      {
         return _picData.getAttr();
      }

      protected internal String getPictureReal()
      {
         return _pictureReal.ToString();
      }

      protected internal String getPictureEnable()
      {
         return _pictureEnable.ToString();
      }

      // for Numeric
      protected internal bool getIsNegative()
      {
         return _picData.isNegative();
      }

      // for Numeric
      protected internal bool getIsPadFill()
      {
         return _picData.padFill();
      }

      // for Numeric
      protected internal char getPadFillChar()
      {
         return _picData.getPad();
      }

      // for Numeric, Date, Time
      protected internal bool getIsZeroFill()
      {
         return _picData.zeroFill();
      }

      // for Numeric, Date, Time
      protected internal char getZeroFillChar()
      {
         return _picData.getZeroPad();
      }

      // for Numeric
      protected internal String getNegativeSignPref()
      {
         return _picData.getNegPref_();
      }

      public List<String> getDiscreteRangeValues()
      {
         return _discreteRangeValues;
      }

      public List<String> getContinuousRangeValues()
      {
         return _continuousRangeValues;
      }

      protected internal String getRange()
      {
         return _range;
      }

      protected internal MgControlBase getControl()
      {
         return _control;
      }

      protected internal PIC getPIC()
      {
         return _picData;
      }

      public bool getIsNull()
      {
         return _isNull;
      }

      /// <summary>
      ///   Make validation of the field
      /// </summary>
      public ValidationDetails evaluate()
      {
         return _fieldValidator.checkVal(this);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      private List<String> CloneDiscreteRangeValues()
      {
         List<String> clone = new List<String>();
         clone.AddRange(_discreteRangeValues);
         return clone;
      }

      private List<String> CloneContinuousRangeValues()
      {
         List<String> clone = new List<String>();
         clone.AddRange(_continuousRangeValues);
         return clone;
      }
   }
}
