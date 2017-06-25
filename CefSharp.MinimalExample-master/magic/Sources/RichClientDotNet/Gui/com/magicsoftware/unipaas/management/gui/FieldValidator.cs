using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.env;
using com.magicsoftware.unipaas.management.data;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary> Has the following services:
   /// 1. check validity of value according to the type+picture+range
   /// 2. auto complete of value using the range+picture
   /// </summary>
   internal class FieldValidator
   {
      private String _newvalue;
      private String _oldvalue;
      private String _pictureReal;
      private String _pictureEnable;
      private ValidationDetails _valDet;
      private bool _isPm; // for Time
      private String _picture;
      private bool hebrew; // Hebrew ?
      private readonly String _decimal;
      private readonly IEnvironment _environment;

      /// <summary> constructor</summary>
      protected internal FieldValidator()
      {
         _environment = Manager.Environment;         
         _decimal = "" + _environment.GetDecimal();
      }

      /// <summary> the main method of this class</summary>
      /// <param name="valDet">a ValidationDetails object to use when validating</param>
      protected internal ValidationDetails checkVal(ValidationDetails valDet)
      {
         MgControlBase ctrl;

         // Check if new value suitable to picture
         _newvalue = _oldvalue = _pictureReal = _pictureEnable = null;
         init(valDet);
         valDet.setValidationFailed(false); // focus not back in start of every chacking validation
         ctrl = valDet.getControl();
         bool modInQueuy = (ctrl.checkProp(PropInterface.PROP_TYPE_MODIFY_IN_QUERY, false, ctrl.getDisplayLine(false)));

         if ((ctrl.getForm().getTask().getMode() == Constants.TASK_MODE_QUERY && !modInQueuy))
         {
            // do not fail the validation, but return the old value
            valDet.setValue(_oldvalue);
            return valDet;
         }

         // Filter the Validation of controls of type Radio/CheckBox/Select
         if (ctrl.Type == MgControlType.CTRL_TYPE_RADIO || ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX || ctrl.Type == MgControlType.CTRL_TYPE_TAB || ctrl.isSelectionCtrl())
         {
            try
            {
               if (ctrl.Type == MgControlType.CTRL_TYPE_CHECKBOX)
                  valDet.setNull(ctrl.isNullValue(_newvalue));
               if (_newvalue != null && ctrl.isSelectionCtrl())
               {
                  int idx = Int32.Parse(_newvalue);
                  if (ctrl.isChoiceNull(idx))
                     valDet.setNull(true);
               }
            }
            catch (Exception)
            {
               //protect, in case newvalue is empty
            }

            valDet.setValue(_newvalue);
            return valDet;
         }

         // space|nothing was inserted  -> use default value
         if (ctrl.DataType != StorageAttribute.ALPHA && ctrl.DataType != StorageAttribute.UNICODE && ctrl.DataType != StorageAttribute.BLOB)
         {
            if (_newvalue.Trim().Length == 0 || (ctrl.DataType == StorageAttribute.DATE && (ctrl.isAllBlanks(_newvalue) || isNullDisplayVal() )))
            {
               Field field = ctrl.getField();
               String defValue = field.getDefaultValue(); // inner value inserted
               String dispDefValue = null;

               if (field.isNullDefault())
               {
                  valDet.setNull(true);
                  dispDefValue = field.getNullDisplay();
               
                  //QCR # 278557 : For date if Null display is not set then we should use default value 
                  //as zero date is not allowed for some databases.
                  if (string.IsNullOrEmpty(dispDefValue) && ctrl.DataType == StorageAttribute.DATE)
                     dispDefValue = DisplayConvertor.Instance.mg2disp(defValue, valDet.getRange(), valDet.getPIC(), field.getTask().getCompIdx(), false);
               }
               if (dispDefValue == null)
                  dispDefValue = DisplayConvertor.Instance.mg2disp(defValue, valDet.getRange(), valDet.getPIC(), field.getTask().getCompIdx(), false);

               // If range is specified to field, the range validation should be performed ,even though blank value is entered.
               // But this is not applicable for Null Default value.
               if (!field.isNullDefault() && (_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null))
               {
                  _newvalue = dispDefValue;
               }
               else
               {
                  valDet.setValue(dispDefValue);
                  return valDet;
               }
            }
         }
         if (!StrUtil.rtrim(_newvalue).Equals(StrUtil.rtrim(_oldvalue)) || ctrl.ModifiedByUser)
         {
            try
            {
               switch (valDet.getType())
               {

                  case StorageAttribute.ALPHA:
                  case StorageAttribute.UNICODE:
                     valDet.setValue(checkAlphaField());
                     break;

                  case StorageAttribute.NUMERIC:
                     _pictureReal = valDet.getPictureReal(); // only numeric type need another picture format
                     _pictureEnable = valDet.getPictureEnable();
                     checkNumericPicture();
                     valDet.setValue(checkNumericField());
                     break;

                  case StorageAttribute.BOOLEAN:
                     valDet.setValue(checkLogicalField());
                     break;

                  case StorageAttribute.DATE:
                     valDet.setValue(checkDateField());
                     break;

                  case StorageAttribute.TIME:
                     valDet.setValue(checkTimeField());
                     break;

                  case StorageAttribute.BLOB:
                  //there is no real picture to BLOB, do nothing
                  case StorageAttribute.BLOB_VECTOR:
                     valDet.setValue(_newvalue);
                     break;

                  case StorageAttribute.NONE:
                  default:
                     Events.WriteExceptionToLog("FieldValidator.checkVal: Type of the field " + valDet.getType());
                     break;
               }
            }
            catch (WrongFormatException goBack)
            {
               // need change to old value, set focus back and write Error Message
               valDet.setValue(_oldvalue);
               setValidationFailed(true);
               if (goBack.getType() != MsgInterface.STR_RNG_TXT)
               {
                  printMessage(goBack.getType(), false);
                  setValidationFailed(true);
               }
               else
                  printMessage(MsgInterface.STR_RNG_TXT, true);
            }
         }
         return valDet;
      }

      /// <summary>**************************Alpha*********************************</summary>

      /// <summary>Check & validate Alpha field by his old/new value and 2 pictures:
      /// Real and Enable
      /// 1.check if newvalue complete with pictures, otherwise return oldvalue
      /// 2.check if newvalue complete with range
      /// 2.1 complete value, if possible, otherwise  return oldvalue
      /// </summary>
      /// <returns> String - new value of validated field</returns>
      private String checkAlphaField()
      {
         MgControlBase ctrl = _valDet.getControl(); ;
         MgControlType ctrlType;
         bool IsAttrAlpha = (_valDet.getType() == StorageAttribute.ALPHA);

         if (_newvalue.Length == 0 && _valDet.getContinuousRangeValues() == null && _valDet.getDiscreteRangeValues() == null)
         {
            setValidationFailed(false);
            return _newvalue;
         }

         // count the number of bytes, not characters (JPN: DBCS support)
         if (IsAttrAlpha && UtilStrByteMode.isLocaleDefLangDBCS())
         {
            int picLenB = UtilStrByteMode.lenB(_picture);
            if (UtilStrByteMode.lenB(_newvalue) > picLenB)
            {
               _newvalue = UtilStrByteMode.leftB(_newvalue, picLenB);
            }
         }
         else
         {
            if (_newvalue.Length > _picture.Length)
               _newvalue = _newvalue.Substring(0, _picture.Length);
         }

         int i, currPicture;
         StringBuilder newbuffer = new StringBuilder();

         String strNewValOneChar; // JPN: DBCS support

         for (currPicture = i = 0; i < _newvalue.Length && currPicture < _picture.Length; )
         {
            if (!isAlphaPositionalDirective(_picture[currPicture]) && _newvalue[i] == _picture[currPicture])
            {
               newbuffer.Append(_picture[currPicture]);
               i++;
               currPicture++;
            }
            else if (!isAlphaPositionalDirective(_picture[currPicture]) && _newvalue[i] != _picture[currPicture])
            {
               newbuffer.Append(_picture[currPicture]);
               currPicture++;
            }
            else if (isAlphaPositionalDirective(_picture[currPicture]) && isPossibleAlphaLetter(i, currPicture))
            {
               if (UtilStrByteMode.isLocaleDefLangDBCS())
               // JPN: DBCS support
               {
                  // a DBCS character in "newvalue" consumes two characters in "picture"
                  strNewValOneChar = _newvalue.Substring(i, 1);
                  if (UtilStrByteMode.lenB(strNewValOneChar) == 2)
                  // if DBCS
                  {
                     if (IsAttrAlpha)
                     {
                        if (currPicture + 1 < _picture.Length)
                        {
                           // a DBCS needs two consecutive alpha positional directives
                           if (isAlphaPositionalDirective(_picture[currPicture + 1]) && isPossibleAlphaLetter(i, currPicture + 1))
                           {
                              newbuffer.Append(_newvalue[i]);
                              i++;
                              currPicture += 2;
                           }
                           // only one (not enough for DBCS)
                           else
                           {
                              newbuffer.Append(' ');
                              currPicture++;
                           }
                        }
                        // end of the picture
                        else
                        {
                           newbuffer.Append(' ');
                           currPicture++;
                        }
                     }
                     else
                     {
                        newbuffer.Append(_newvalue[i]);
                        i++;
                        currPicture++;
                     }
                  }
                  // if SBCS
                  else
                  {
                     newbuffer.Append(_newvalue[i]);
                     i++;
                     currPicture++;
                  }
                  strNewValOneChar = null;
               }
               else
               {
                  newbuffer.Append(_newvalue[i]);
                  i++;
                  currPicture++;
               }
            }
            else
            {
               newbuffer.Append(' ');
               currPicture++;
               if (_valDet.getControl() != null)
               {
                  ctrlType = ctrl.Type;
                  if (ctrlType != MgControlType.CTRL_TYPE_BUTTON)
                  {
#if PocketPC
                     if (UtilStrByteMode.isLocaleDefLangJPN())
                        throw new WrongFormatException(MsgInterface.STR_ERR_NUM);
                     else
#endif
                        printMessage(MsgInterface.STR_ERR_NUM, false);
                  }
               }
            }
         } // end of checkloop  i==newvalue.length()
         _newvalue = newbuffer.ToString(); // change newvalue with changed value of buffer

         if (_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null)
            _newvalue = fillAlphaByRange(_valDet.getContinuousRangeValues(), _valDet.getDiscreteRangeValues());

         return _newvalue;
      }

      /// <summary>check if char in Alpha compleate with Directive Character for the letter</summary>
      /// <param name="index">of directive in string to check</param>
      /// <returns> true if complete, false otherwise</returns>
      private bool isPossibleAlphaLetter(int indexInput, int indexPicture)
      {
         switch ((int)_picture[indexPicture])
         {
            case PICInterface.PIC_X:
            case PICInterface.PIC_J:
            case PICInterface.PIC_G:
            case PICInterface.PIC_S:
            case PICInterface.PIC_T:
               return true;

            case PICInterface.PIC_U:
               char upper = Char.ToUpper(_newvalue[indexInput]);
               if (upper != _newvalue[indexInput])
                  changeStringInside("" + upper, indexInput);
               return true;

            case PICInterface.PIC_L:
               char lower = Char.ToLower(_newvalue[indexInput]);
               if (lower != _newvalue[indexInput])
                  changeStringInside("" + lower, indexInput);
               return true;

            case PICInterface.PIC_N:
               char number = _newvalue[indexInput];
               if (UtilStrByteMode.isDigit(number) || /*Character.isSpaceChar(number) ||*/ Char.IsWhiteSpace(number) || UtilStrByteMode.asNumeric(number))
                  return true;
               else
               {
                  Events.WriteErrorToLog("Bad Alpha value. Should be number only on place Num. " + indexInput);
                  return false;
               }

            default:
               Events.WriteErrorToLog("FieldValidator.isPossibleAlphaLetter: illegal Char Directive: " + _picture.IndexOf((Char)indexPicture) + " for " + _newvalue[indexInput]);
               return false;
         }
      }

      /// <summary>if new value wasn't finished and stay unwritable peaces of the Real picture - add it
      /// to the end of the newvalue
      /// </summary>
      private String fillAllAlpha()
      {
         StringBuilder buffer = new StringBuilder(_picture.Length);
         buffer.Append(_newvalue);

         if (UtilStrByteMode.isLocaleDefLangDBCS() && _valDet.getType() == StorageAttribute.ALPHA)
         // JPN: DBCS support
         {
            // calculate the size (bytes) of "newvalue"
            int intNewvalueLenB = UtilStrByteMode.lenB(_newvalue);

            // cut "picture" at the same size (bytes) of "newvalue"
            String strBuiedPicture = UtilStrByteMode.leftB(_picture, intNewvalueLenB);

            for (int i = strBuiedPicture.Length; i < _picture.Length; i++)
            {
               if (isAlphaPositionalDirective(_picture[i]))
                  buffer.Append(' ');
               else
                  buffer.Append(_picture[i]);
            }
         }
         else
         {
            for (int i = _newvalue.Length - 1; i < _picture.Length && i >= 0; i++)
            // i=-1 for empty string and for empty strings needn't insert to the loop
            {
               if (isAlphaPositionalDirective(_picture[i]))
                  buffer.Append(' ');
               else
                  buffer.Append(_picture[i]);
            }
         }
         return buffer.ToString();
      }

      /// <summary>Comparative newvalue with possible Range in ContinuousRangeValues/DiscreteRangeValues</summary>
      /// <returns> newvalue if it possible, oldvalue otherwise</returns>
      private String fillAlphaByRange(List<String> ContinuousRangeValues, List<String> DiscreteRangeValues)
      {
         String tmpBuffer;

         tmpBuffer = DisplayConvertor.Instance.fillAlphaByDiscreteRangeValues(DiscreteRangeValues, _newvalue);
         if (tmpBuffer != null)
            return tmpBuffer;
         tmpBuffer = DisplayConvertor.Instance.fillAlphaByContinuousRangeValues(ContinuousRangeValues, _newvalue);
         if (tmpBuffer != null)
            return tmpBuffer;

         if (DiscreteRangeValues != null)
            return CompleteAlphaByRange(DiscreteRangeValues);

         // here set focus back and from the point impossibles to set it to false
         printMessage(MsgInterface.STR_RNG_TXT, true);

         return _oldvalue;
      }

      /// <summary>Try to complete the newvalue by Range</summary>
      /// <returns> completed value if it possible, otherwise oldvalue</returns>
      private String CompleteAlphaByRange(List<String> DiscreteRangeValues)
      {
         String tmpNewValue;
         tmpNewValue = DisplayConvertor.Instance.completeAlphaByRange(DiscreteRangeValues, _newvalue);
         if (tmpNewValue != null)
            return tmpNewValue;
         // focus back
         printMessage(MsgInterface.STR_RNG_TXT, true);
         return _oldvalue;
      }

      /// <summary>**************************Logical*********************************</summary>

      /// <summary>check & validate Logical field by his old/new value and 2 pictures:
      /// Real and Enable
      /// 1.check if newvalue complete with pictures, otherwise return oldvalue
      /// 2.check if newvalue complete with range
      /// 2.1 complete value, if possible, otherwise  return oldvalue
      /// </summary>
      /// <returns> String - new value of validated field</returns>
      private String checkLogicalField()
      {
         if (_newvalue.Length == 0 && _valDet.getContinuousRangeValues() == null && _valDet.getDiscreteRangeValues() == null)
         {
            throw new WrongFormatException(MsgInterface.STR_RNG_TXT);
         }
         setValidationFailed(false);

         int i;
         StringBuilder newbuffer = new StringBuilder();

         // Drag & Drop : prevent crash when length of Dragged Content > picture length
         if (_newvalue.Length > _picture.Length)
            _newvalue = _newvalue.Substring(0, _picture.Length);

         for (i = 0; i < _newvalue.Length; i++)
         {
            if (!isLogicPositionalDirective(_picture[i]))
            {
               if (_newvalue[i] != _picture[i])
               {
                  printMessage(MsgInterface.STR_RNG_TXT, true);
                  return _oldvalue;
               }
               else
                  continue;
            }
         } // end of checkloop  i==newvalue.length()

         // try to fill value:
         if (i < _picture.Length && indexOfPositionalNonDirective(_picture.Substring(i)) != -1)
            _newvalue = fillAllAlpha();

         for (i = 0; i < _newvalue.Length; i++)
            if (isLogicPositionalDirective(_picture[i]))
               newbuffer.Append(_newvalue[i]);

         _newvalue = newbuffer.ToString().Trim();

         // fill by range can take a partial value like "*Tr   *" and return True.
         if (_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null)
            _newvalue = fillAlphaByRange(_valDet.getContinuousRangeValues(), _valDet.getDiscreteRangeValues());

         if (_newvalue.Length > _picture.Length)
            // cut the string
            _newvalue = _newvalue.Substring(0, _picture.Length);

         // do not try to unstrip the value, since it was not changed.
         if (!_newvalue.Equals(_oldvalue))
         {
            // unstrip (add masking chars) to the value.
            // later on when this value is used, the masked value is expected, not the stripped one.
            StringBuilder dispvalue = new StringBuilder();
            int j = 0;
            for (i = 0; i < _picture.Length; i++)
            {
               if (isLogicPositionalDirective(_picture[i]))
               {
                  if (j < _newvalue.Length)
                     dispvalue.Append(_newvalue[j]);
                  else
                     dispvalue.Append(' ');

                  j++;
               }
               else
                  dispvalue.Append(_picture[i]);
            }

            _newvalue = dispvalue.ToString().Trim();
         }

         return _newvalue;
      }

      /// <summary>**************************Numeric*********************************</summary>

      /// <summary>check & validate Numeric field by his old/new value and 2 pictures:
      /// Real and Enable
      /// 1.check if newvalue complete with pictures, otherwise return oldvalue
      /// 2.check if newvalue complete with range
      /// 2.1 complete value, if possible, otherwise  return oldvalue
      /// </summary>
      /// <returns> String - new value of validated field</returns>
      private String checkNumericField()
      {
         if (_newvalue.Length == 0)
            _newvalue = "0";

         int delimeterRealIndex = _newvalue.IndexOf(_decimal);
         int delimeterPictIndex = _pictureReal.IndexOf(_decimal);

         String evaluated = "";
         String evaluatedLeft = "";
         String evaluatedRight = "";

         setValidationFailed(false);

         // check if the format of newvalue matches the numeric mask.
         if (!checkNumericInDecFormat())
            throw new WrongFormatException(MsgInterface.EDT_ERR_STR_1);

         if (delimeterPictIndex != -1 && delimeterRealIndex != -1)
         {
            // there is decimal value & decimal picture
            evaluatedLeft = checkNumericLeft(delimeterPictIndex, delimeterRealIndex);
            evaluatedRight = checkNumericRight(delimeterPictIndex, delimeterRealIndex);
            evaluated = evaluatedLeft + _decimal + evaluatedRight;
         }
         else if (delimeterPictIndex != -1 && delimeterRealIndex == -1)
         {
            // picture is decimal and inserted whole number
            // Change in arguments due need to cover cases like : mask #.### input 123
            // Where input is shorter then mask and mask includes separating caracter.
            evaluatedLeft = checkNumericLeft(delimeterPictIndex, Math.Min(delimeterPictIndex, _newvalue.Length));
            evaluatedRight = checkNumericRight(delimeterPictIndex, delimeterPictIndex - 1);
            evaluated = evaluatedLeft + _decimal + evaluatedRight; // add strings without digits
         }
         else if (delimeterPictIndex == -1 && delimeterRealIndex == -1)
         {
            // there are whole picture and inserted number
            evaluated = checkNumericRight(-1, -1);
            evaluatedLeft = evaluated;
         }
         // delimeterPictIndex == -1 && delimeterRealIndex != -1
         else
         {
            // there whole picture and inserted decimal number
            if (delimeterRealIndex > 0)
               _newvalue = _newvalue.Substring(0, delimeterRealIndex);
            // delimeterRealIndex==0
            else
               _newvalue = "0";
            evaluated = checkNumericRight(-1, -1);
            evaluatedLeft = evaluated;
         }

         if ((evaluatedRight.Length == 0 && evaluatedLeft.Length == 0) || (evaluated.Length == 0 && _newvalue.Length > 0))
         {
            // space|nothing was inserted  -> use default value
            MgControlBase ctrl;
            ctrl = _valDet.getControl();

            Field field = ctrl.getField();
            String defValue = field.getDefaultValue(); // inner value inserted
            String dispDefValue = null;

            if (field.isNullDefault())
            {
               _valDet.setNull(true);
               dispDefValue = field.getNullDisplay();
            }
            if (dispDefValue == null)
               dispDefValue = DisplayConvertor.Instance.mg2disp(defValue, _valDet.getRange(), _valDet.getPIC(), field.getTask().getCompIdx(), false);

         }

         // check if the digit before the delimiter do not exceed the pic wholes.
         int wholeDigitdiff = digitsBeforeDelimeter(evaluated, 0, _decimal) - _valDet.getPIC().getWholes();
         // if they do, check if the exceeding chars are the pad chars. if so, there is no error.
         if (wholeDigitdiff > 0)
         {
            bool padCharOnly = false;
            // is there a pad, check it. no pad is a certain error.
            if (_valDet.getIsPadFill())
            {
               padCharOnly = true;
               for (int i = 0; i < wholeDigitdiff && padCharOnly; i++)
                  // if the char is not the pad, then this is an error.
                  if (evaluated[i] != _valDet.getPadFillChar())
                     padCharOnly = false;
            }

            if (!padCharOnly)
               throw new WrongFormatException(MsgInterface.EDT_ERR_STR_1);
         }

         if (_valDet.getIsNegative() && (_newvalue.Trim().StartsWith("-") || (!(_valDet.getNegativeSignPref().Length == 0) && _newvalue.Trim().StartsWith(_valDet.getNegativeSignPref()))))
            evaluated = "-" + evaluated; // add '-'

         // from this point have evaluated string - the value (without mask) of the control
         // needed to be inserted to the picture
         evaluated = DisplayConvertor.Instance.disp2mg(_newvalue, "", _valDet.getPIC(), _valDet.getControl().getForm().getTask().getCompIdx(), BlobType.CONTENT_TYPE_UNKNOWN);
         evaluated = DisplayConvertor.Instance.mg2disp(evaluated, "", _valDet.getPIC(), _valDet.getControl().getForm().getTask().getCompIdx(), true);

         // in order check if user has entered a blank value , trim and then check it's length.
         string tempDispVal = StrUtil.ltrim(evaluated);
         if (tempDispVal.Length == 0 && _valDet.getPIC().zeroFill() && _valDet.getPIC().getZeroPad() == ' ')
            evaluated = "0";

         // Insert the number to range
         if (_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null)
         {
            _newvalue = evaluated;
            evaluated = fillNumericByRange();
         }

         return evaluated;
      }

      /// <summary>Check validity of numerical picture from found 'delimeter' to left.
      /// <-.
      /// </summary>
      /// <param name="delimeterPictIndex">found delimeter in the picture</param>
      /// <param name="delimeterRealIndex">found delimeter in the inserted by user new value</param>
      private String checkNumericLeft(int delimeterPictIndex, int delimeterRealIndex)
      {
         StringBuilder buffer = new StringBuilder();
         int RealIndex = delimeterRealIndex - 1;
         int PictIndex;
         bool isFound;

         for (PictIndex = delimeterPictIndex - 1; PictIndex >= 0 && RealIndex >= 0; PictIndex--)
         {
            if (_pictureEnable[PictIndex] == '1')
            {
               int tmpstorage = RealIndex;
               isFound = false;

               for (; RealIndex >= 0; RealIndex--)
               {
                  if (_pictureReal[PictIndex] == _newvalue[RealIndex])
                  {
                     isFound = true;
                     RealIndex--;
                     break;
                  }
               }

               if (!isFound)
                  RealIndex = tmpstorage;

               buffer.Append(_pictureReal[PictIndex]);
            }
            // can insert only number
            else
            {
               for (; RealIndex >= 0; RealIndex--)
               {
                  if (UtilStrByteMode.isDigit(_newvalue[RealIndex]))
                  {
                     buffer.Append(_newvalue[RealIndex]);
                     RealIndex--;
                     break;
                  }
               }
            }
         }

         if (RealIndex < 0 && PictIndex >= 0)
            // try to add unwritable
            for (; PictIndex >= 0; PictIndex--)
            {
               if (_pictureEnable[PictIndex] == '1')
                  buffer.Append(_pictureReal[PictIndex]);
               else
               {
                  // only from left need PadFill
                  if (_valDet.getIsPadFill())
                     buffer.Append(_valDet.getPadFillChar());
               }
            }

         return StrUtil.ReverseString(buffer).ToString();
      }

      /// <summary>Check validity of numerical picture from found 'delimeter' to right.
      /// .->
      /// </summary>
      /// <param name="delimeterPictIndex">found delimeter in the picture</param>
      /// <param name="delimeterRealIndex">found delimeter in the inserted by user new value</param>
      private String checkNumericRight(int delimeterPictIndex, int delimeterRealIndex)
      {
         // if (delimeterRealIndex==newvalue.length() || delimeterPictIndex==pictureEnable.length())
         //    return "";
         StringBuilder buffer = new StringBuilder();
         int RealIndex = delimeterRealIndex + 1;
         int PictIndex;
         PIC pic = _valDet.getPIC();

         for (PictIndex = delimeterPictIndex + 1; PictIndex < _pictureEnable.Length && RealIndex < _newvalue.Length; PictIndex++)
         {
            if (_pictureEnable[PictIndex] == '1')
            {
               if (RealIndex < PictIndex)
                  RealIndex = PictIndex;

               buffer.Append(_pictureReal[PictIndex]);
            }
            // can insert only number
            else
            {
               for (; RealIndex < _newvalue.Length; RealIndex++)
               {
                  // Append the digit of actual input data. If it is mask char, skip it.
                  if (UtilStrByteMode.isDigit(_newvalue[RealIndex]) && !pic.picIsMask(RealIndex))
                  {
                     buffer.Append(_newvalue[RealIndex]);
                     RealIndex++;
                     break;
                  }
               }
            }
         }

         //if right part has any input data, then only append unwritable chars 
         if (buffer.Length > 0)
         {
            if (RealIndex >= _newvalue.Length && PictIndex < _pictureReal.Length)
               // try to add unwritable
               for (; PictIndex < _pictureReal.Length; PictIndex++)
               {
                  if (_pictureEnable[PictIndex] == '1')
                     buffer.Append(_pictureReal[PictIndex]);
                  // else Do nothing
               }
         }
         return buffer.ToString();
      }

      /// <summary>BUG: 442788
      /// Changing decimal separator in thew pictureReal to value got from Environment
      /// </summary>
      private void checkNumericPicture()
      {
         int indexOfDec, lastDec, maxLength, currIndx;
         String POINT_DEC = "."; // used in PICTURE for decimal separator
         PIC pic = _valDet.getPIC();

         if (_pictureReal == null || _pictureEnable == null || pic.getAttr() != StorageAttribute.NUMERIC || !pic.withDecimal())
            return;

         lastDec = _pictureReal.LastIndexOf(POINT_DEC);
         maxLength = _pictureEnable.Length;
         currIndx = 0;
         while (currIndx < lastDec)
         {
            indexOfDec = _pictureEnable.IndexOf("1", currIndx);

            if (indexOfDec == -1)
               // not found
               return;
            else
            {
               if (indexOfDec == _pictureReal.IndexOf(POINT_DEC, currIndx))
               // it's real decimal point and not \. into the picture
               {
                  String first, second;
                  first = second = "";
                  if (indexOfDec == 0)
                  // the point on the first place .###
                  {
                     if (indexOfDec + 1 < maxLength)
                        // only 1 .
                        second = _pictureReal.Substring(indexOfDec + 1);
                  }
                  else if (indexOfDec == maxLength)
                  // the point on the last place ###.
                  {
                     first = _pictureReal.Substring(0, indexOfDec);
                  }
                  else
                  {
                     first = _pictureReal.Substring(0, indexOfDec);
                     second = _pictureReal.Substring(indexOfDec + 1);
                  }
                  _pictureReal = first + _decimal + second;
                  break;
               }
               else
               {
                  currIndx = ++indexOfDec;
               }
            }
         }
      }

      /// <summary> Compare newvalue with the range</summary>
      /// <returns> newvalue if it's in the range, oldvalue otherwise</returns>
      private String fillNumericByRange()
      {
         List<String> continuousRangeValues, discreteRangeValues;
         int i;
         String checkedStr;
         PIC controlPic = _valDet.getControl().getPIC();
         NUM_TYPE rangeItemValue1, rangeItemValue2;
         NUM_TYPE checkedNum;

         continuousRangeValues = _valDet.getContinuousRangeValues();
         discreteRangeValues = _valDet.getDiscreteRangeValues();

         setValidationFailed(false);
         checkedStr = setNumericValueWithoutPicture(_newvalue);

         if (checkedStr.Length == 0)
         {
            printMessage(MsgInterface.STR_RNG_TXT, true);
            return _oldvalue;
         }

         checkedNum = new NUM_TYPE(_newvalue, _valDet.getControl().getPIC(), 0);

         if (discreteRangeValues != null)
         {
            for (i = 0; i < discreteRangeValues.Count; i++)
            {
               rangeItemValue1 = new NUM_TYPE(discreteRangeValues[i], controlPic, 0);

               try
               {
                  if (NUM_TYPE.num_cmp(checkedNum, rangeItemValue1) == 0)
                     return _newvalue;
               }
               catch (FormatException)
               {
                  // skip non-numeric range values
               }
            }
         }

         if (continuousRangeValues != null)
         {
            for (i = 0; i < continuousRangeValues.Count; i++)
            {
               rangeItemValue1 = new NUM_TYPE ();
               rangeItemValue2 = new NUM_TYPE ();

               rangeItemValue1.num_4_a_std (continuousRangeValues[i++]);
               rangeItemValue2.num_4_a_std (continuousRangeValues[i]);
               try
               {
                  if (NUM_TYPE.num_cmp(checkedNum, rangeItemValue1) != -1 && NUM_TYPE.num_cmp(rangeItemValue2, checkedNum) != -1)
                     return _newvalue;
               }
               catch (FormatException)
               {
                  // skip non-numeric range values
               }
            }
         }

         throw new WrongFormatException();
      }

      /// <summary> returns a numeric value extracted from the beginning of a string
      /// on failure returns NEGATIVE_INFINITY
      /// </summary>
      /// <param name="val">the string to parse</param>
      private double getDoubleVal(String val)
      {
         int i;
         char c;
         bool dotFound = false;
         bool minusFound = false;
         bool digitFound = false;
         bool endOfNum = false;

         // find the delemeting non numeric character
         for (i = 0; i < val.Length && !endOfNum; i++)
         {
            c = val[i];
            if (c >= '0' && c <= '9')
               digitFound = true;
            else
            {
               switch (c)
               {
                  case '-':
                     if (digitFound || dotFound || minusFound)
                        endOfNum = true;
                     minusFound = true;
                     break;

                  case '.':
                     if (dotFound || minusFound && !digitFound)
                        endOfNum = true;
                     dotFound = true;
                     break;

                  case ' ':
                     if (digitFound || dotFound || minusFound)
                        endOfNum = true;
                     break;

                  default:
                     endOfNum = true;
                     break;
               }
            }
         }
         if (endOfNum)
            i--;
         if (i > 0)
            return System.Double.Parse(val.Substring(0, i));
         return System.Double.NegativeInfinity;
      }

      /// <summary>set Numeric value Without Picture, filter input from user</summary>
      /// <returns> the pure number, without picture mask</returns>
      private String setNumericValueWithoutPicture(String val)
      {
         StringBuilder checkedStr = new StringBuilder(val.Length);
         bool isFirstDecimal = false;
         char currChar;
         for (int i = 0; i < _pictureReal.Length && i < val.Length; i++)
         {
            currChar = val[i];
            if (UtilStrByteMode.isDigit(currChar))
               checkedStr.Append(currChar);
            else if (currChar == _environment.GetDecimal() && !isFirstDecimal)
            {
               checkedStr.Append(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.ToCharArray());
               isFirstDecimal = true;
            }
            else if (currChar == '-' && _valDet.getIsNegative())
                checkedStr.Append(NumberFormatInfo.CurrentInfo.NegativeSign.ToCharArray());
         }
         if (checkedStr.ToString().Length == 0)
         {
            printMessage(MsgInterface.STR_ERR_NUM, false);
            setValidationFailed(true);
         }
         return checkedStr.ToString();
      }

      /// <summary> check if newvalue format matches the numeric pic format.</summary>
      /// <returns></returns>
      private bool checkNumericInDecFormat()
      {
         PIC pic = _valDet.getPIC();
         int i;
         int Dec;
         int Whole;
         int DecPos;
         char decimalChar = _environment.GetDecimal();
         // Count number of digits and position of decimal point 
         Dec = 0;
         Whole = 0;
         DecPos = -1;
         for (i = 0; i < _newvalue.Length; i++)
            if (pic.isNumeric(i))
               if (UtilStrByteMode.isDigit(_newvalue[i]))
                  // when dec pos not found, count wholes, if found, count dec.
                  if (DecPos >= 0)
                     Dec++;
                  else
                     Whole++;
               else if (_newvalue[i] == decimalChar)
                  DecPos = i;

         // did we exceed the mask ?
         if (pic.getWholes() < Whole || pic.getDec() < Dec)
            return false;
         else
            return true;
      }

      /// <summary>********************DATE*************************************</summary>

      /// <summary>check & validate Date field by his old/new value and 2 pictures:
      /// Real and Enable
      /// 1.check if newvalue complete with pictures, otherwise return oldvalue
      /// 2.check if newvalue complete with range
      /// 2.1 complete value, if possible, otherwise  return oldvalue
      /// </summary>
      /// <returns> String - new value of validated field</returns>
      private String checkDateField()
      {
         StringBuilder buffer = new StringBuilder(_picture.Length);
         int currValue = 0;
         int letters = 0; // number of letters in the block
         bool isDay = false; // number inserted to D need be checked after inserting the date
         bool isWeek = false; // number inserted to W need be checked after inserting the date
         bool isDoy = false; // day has picture DDD -> day of year
         bool isJpnEraYear = false; // number inserted to J and YY need be checked after
         // inserting the date (JPN: Japanese date picture support)
         int i;

         setValidationFailed(false);
         for (i = 0; i < _newvalue.Length; i++)
         {
            if (UtilStrByteMode.isLocaleDefLangJPN())
            {
              if (UtilStrByteMode.isDigit(_newvalue[i]))
                 break;
            }
            else
            {
               if ((UtilStrByteMode.isDigit(_newvalue[i])) || (_newvalue[i] != ' '))
                  break;
            }
         }

         if ((_newvalue.Trim().Length == 0 && _valDet.getIsZeroFill()) || _valDet.getControl().isAllBlanks(_newvalue))
         {
            // get default value of the field
            _newvalue = _valDet.getControl().getField().getDefaultValue();
            if (_newvalue == null)
            // MUST to be default default value: Application Properties -> Def./Nulls
            {
               throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
            }

            _newvalue = DisplayConvertor.Instance.mg2disp(_newvalue, "", _valDet.getPIC(), _valDet.getControl().getForm().getTask().getCompIdx(), false);
         }
         else if (i == _newvalue.Length)
         {
            // not found any digit in the new value - nothing to evaluate
            throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
         }

         // used to be : only date with Year part of it's picture can have date: Day-0, Month-0, Year 0
         // date does not need to have year part in order for all zeros to be valid. This is compatible with online.
         if (isNumberAllowedInDate() && isAllNumbers0())
         // && isYearPartOfDate())
         {
            // all date is 0 and need to be zero filled
            // there is no Range or the 0 value is in Range
            if (((_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null) && isDateLegal4Range(_valDet.getContinuousRangeValues(), _valDet.getDiscreteRangeValues(), 0)) || (_valDet.getContinuousRangeValues() == null && _valDet.getDiscreteRangeValues() == null))
            {
               if (_valDet.getIsZeroFill() || isAllNumbers0())
               {
                  char add = '0'; // isAllNumbers0(), default
                  if (_valDet.getIsZeroFill())
                     add = _valDet.getZeroFillChar();

                  buffer.Remove(0, buffer.Length);

						//QCR:982741.
						//Fix:Removed the function 'UtilStrByteMode.isLocaleDefLangJPN()' from if(), this function is specific to japanese language and hence 
						//was returning false in non-japanese environement.Since this function was returning false the formatting of the date field
						//was not happening correctly hence improper values were observed by the user.     
                  if (!_valDet.getIsZeroFill())
						{
                     for (i = 0; i < _picture.Length; i++)
                     {
                        if (isDataPositionalDirective(_picture[i]))
                           buffer.Append(add);
                        else
                           buffer.Append(_picture[i]);
                     }
                  }
                  return buffer.ToString();
               }
            }
         }

         int minLen = 0;
         if (UtilStrByteMode.isLocaleDefLangJPN())
#if PocketPC
            minLen = _picture.Length;
#else
            minLen = UtilStrByteMode.getMinLenPicture(_newvalue.TrimEnd(), _picture);
#endif
         else
            minLen = Math.Min(_newvalue.TrimEnd().Length, _picture.Length);

         for (i = 0; i < minLen && (!hebrew || currValue < minLen); i++)
         {
            if (isDataPositionalDirective(_picture[i]))
            {
               switch ((int)_picture[i])
               {
                  case PICInterface.PIC_DD:
                     currValue = getEndOfNumericBlock(currValue);
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_DD);
                     isDay = true;
                     i++;
                     break;

                  case PICInterface.PIC_DDD:
                     currValue = getEndOfNumericBlock(currValue);
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_DDD);
                     isDay = true;
                     isDoy = true;
                     i += 2;
                     break;

                  case PICInterface.PIC_DDDD:
                     currValue = getEndOfNumericBlock(currValue);
                     StringBuilder bufferDate = new StringBuilder();
                     currValue = add2Date(currValue, bufferDate, PICInterface.PIC_DDDD);
                     bufferDate.Append(addDateSuffix(bufferDate.ToString()));
                     int count = 4 - bufferDate.Length;
                     while (--count > 0)
                        buffer.Append(' ');
                     buffer.Append(bufferDate.ToString());
                     isDay = true;
                     i += 3;
                     break;

                  case PICInterface.PIC_MMD:
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_MMD);
                     i++;
                     break;

                  case PICInterface.PIC_MMM:
                     // get size of PIC_MMM block
                     letters = getSizeOfBlock(i, PICInterface.PIC_MMM);
                     currValue = addM2Date(currValue, letters, buffer, PICInterface.PIC_MMM);
                     i += letters - 1;
                     break;

                  case PICInterface.PIC_YY:
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_YY);
                     i++;
                     break;

                  case PICInterface.PIC_YYYY:
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_YYYY);
                     i += 3;
                     break;

                  case PICInterface.PIC_W:
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_W);
                     isWeek = true;
                     break;

                  case PICInterface.PIC_WWW:
                     letters = getSizeOfBlock(i, PICInterface.PIC_WWW);
                     currValue = addM2Date(currValue, letters, buffer, PICInterface.PIC_WWW);
                     isWeek = true;
                     i += letters - 1;
                     break;

                  case PICInterface.PIC_HYYYYY:
                  case PICInterface.PIC_HL:
                  case PICInterface.PIC_HDD:
                     letters = getSizeOfBlock(i, (int)_picture[i]);
                     currValue = HebrewDate.add2Date(_newvalue, currValue, letters, buffer);
                     i += letters - 1;
                     break;

                  case PICInterface.PIC_BB:  // JPN: Japanese date picture support
                     letters = getSizeOfBlock(i, PICInterface.PIC_BB);
                     currValue = addM2Date(currValue, letters / 2, buffer, PICInterface.PIC_BB);
                     // "/ 2" means a number of characters in newvalue, not a number of bytes

                     isWeek = true;
                     i += letters - 1;
                     break;

                  case PICInterface.PIC_JY1:  // JPN: Japanese date picture support
                     letters = 1; // a number of characters in newvalue, not a number of bytes
                     currValue = addM2Date(currValue, letters, buffer, PICInterface.PIC_JY1);
                     isJpnEraYear = true;
                     break;

                  case PICInterface.PIC_JY2:  // JPN: Japanese date picture support
                     letters = 1; // a number of characters in newvalue, not a number of bytes
                     currValue = addM2Date(currValue, letters, buffer, PICInterface.PIC_JY2);
                     isJpnEraYear = true;
                     i++;
                     break;

                  case PICInterface.PIC_JY4:  // JPN: Japanese date picture support
                     letters = 2; // a number of characters in newvalue, not a number of bytes
                     currValue = addM2Date(currValue, letters, buffer, PICInterface.PIC_JY4);
                     isJpnEraYear = true;
                     i += 3;
                     break;

                  case PICInterface.PIC_YJ:  // JPN: Japanese date picture support
                     currValue = add2Date(currValue, buffer, PICInterface.PIC_YJ);
                     isJpnEraYear = true;
                     i++;
                     break;
               }
            }
            else
            {
               buffer.Append(_picture[i]);
               if (_newvalue.IndexOf("" + _picture[i], currValue) != -1)
               // try to find the letter in newvalue
               {
                  currValue = _newvalue.IndexOf("" + _picture[i], currValue) + 1;
               }
            }
         }

         // range date
         if (_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null)
            buffer = fillDateByRange(_valDet.getContinuousRangeValues(), _valDet.getDiscreteRangeValues(), buffer.ToString());

         if (isJpnEraYear)
            // JPN: Japanese date picture support
            buffer = checkJpnEraYear(buffer.ToString());

         if (isDay)
            // D - day value in picture need be checked, if it exists
            buffer = checkDay(buffer.ToString(), isDoy);
         if (isWeek)
            // W - week value in picture need be checked, if it exists
            buffer = checkWeek(buffer.ToString());

         // BUG: 908424
         // check for wrong dates like 0000/01/02
         // for this reason the vlue must be evaluated
         String evaluatedValue = buffer.ToString();
         // currentlly used picture
         PIC pic = _valDet.getPIC();
         int innerValue = DisplayConvertor.Instance.a_2_date_pic_datemode(evaluatedValue.ToCharArray(), evaluatedValue.Length, pic, pic.getMask().ToCharArray(), _valDet.getControl().getForm().getTask().getCompIdx());
         if (innerValue == 1000000000)
            // has got error code
            throw new WrongFormatException(MsgInterface.STR_ERR_DATE);

         return evaluatedValue;
      }

      /// <summary>Add found in newvalue numbers(2 or 3 letters) to buffer and return
      /// last found number index
      /// </summary>
      /// <param name="currValue">in newvalue</param>
      /// <param name="buffer"></param>
      /// <param name="type">- which type of positional directive, for checking validity</param>
      /// <returns> currValue in newvalue</returns>
      private int add2Date(int currValue, StringBuilder buffer, int type)
      {
         int start = currValue;
         int i;
         StringBuilder currAdd = new StringBuilder(_newvalue.Length);
         int letters = 0;
         int add;

         switch (type)
         {
            case PICInterface.PIC_DD:
            case PICInterface.PIC_DDDD:
            case PICInterface.PIC_MMD:
            case PICInterface.PIC_YY:
               letters = 2;
               break;

            case PICInterface.PIC_DDD:
               letters = 3;
               break;

            case PICInterface.PIC_YYYY:
               letters = 4;
               break;

            case PICInterface.PIC_W:
               letters = 1;
               break;

            case PICInterface.PIC_YJ:  // JPN: Japanese date picture support
               letters = 2;
               type = PICInterface.PIC_YY; // PIC_YJ will be handled as PIC_YY in this method.
               break;
         }

         while (start < _newvalue.Length && !UtilStrByteMode.isDigit(_newvalue[start]))
            start++; // find first digit
         if (start == _newvalue.Length && (_newvalue.Length == 0 || _newvalue.Equals("0")))
         // if nothing inserted to the current block of field => inserted 0
         {
            for (int nul = 0; nul < letters; nul++)
               buffer.Append('0');
            return currValue;
         }
         else if (start == _newvalue.Length)
            _newvalue += '0';

         for (i = start; i < start + letters && i < _newvalue.Length; i++)
         {
            if (UtilStrByteMode.isDigit(_newvalue[i]))
               currAdd.Append(_newvalue[i]);
            else
            {
               // if (i != start)
               //   i--;
               break;
            }
         }
         if (currAdd.Length == 0)
            currAdd.Append('0');

         add = Int32.Parse(currAdd.ToString());

         // boundary conditions cases:
         if ((add < 1 && (type != PICInterface.PIC_YYYY && type != PICInterface.PIC_YY)) || (add > 31 && (type == PICInterface.PIC_DD || type == PICInterface.PIC_DDDD)) || (add > 12 && type == PICInterface.PIC_MMD) || (add > 7 && type == PICInterface.PIC_W))
         {
            throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
         }

         // than picture is YYYY and inserted 0-2 numbers, use CENTURE value, otherwise add zeros before the year
         if (type == PICInterface.PIC_YYYY && _environment.GetCentury(_valDet.getControl().getForm().getTask().getCompIdx()) > 999 && currAdd.Length < 3)
            currAdd = checkCentury(add);

         for (int diff = 0; diff < letters - currAdd.Length; diff++)
            buffer.Append('0');

         buffer.Append(currAdd.ToString());
         return i;
      }

      /// <summary>Add found in newvalue letters to buffer and return
      /// last found number index
      /// </summary>
      /// <param name="currValue">in newvalue</param>
      /// <param name="letters">amount of letters to insert to buffer</param>
      /// <param name="buffer"></param>
      /// <returns> currValue in newvalue</returns>
      private int addM2Date(int currValue, int letters, StringBuilder buffer, int type)
      {
         String[] array;
         if (type == PICInterface.PIC_MMM || type == PICInterface.PIC_MMD)
         // Month
         {
            if (hebrew)
               array = HebrewDate.GetLocalMonths();
            else
            {
               String monthStr = Events.GetMessageString(MsgInterface.MONTHS_PTR);
               array = DateUtil.getLocalMonths(monthStr);
            }
         }
         else if (type == PICInterface.PIC_WWW || type == PICInterface.PIC_W)
         // Week
         {
            if (hebrew)
               array = HebrewDate.GetLocalDows();
            else
            {
               String dayStr = Events.GetMessageString(MsgInterface.DAYS_PTR);
               array = DateUtil.getLocalDays(dayStr);
            }
         }
         else if (type == PICInterface.PIC_BB)
         // JPN: Japanese date picture support
         {
            array = UtilDateJpn.getArrayDow();
         }
         else if (type == PICInterface.PIC_JY1 || type == PICInterface.PIC_JY2 || type == PICInterface.PIC_JY4)
         {
            // JPN: Japanese date picture support
            if (_newvalue.Length < currValue + letters)
               throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
            
            String strJpnEraYear = _newvalue.Substring(currValue, letters);
            buffer.Append(strJpnEraYear);
            return currValue + strJpnEraYear.Length;
         }
         else
         {
            return currValue + letters;
         }
         if (_newvalue.Length < currValue)
            currValue = _newvalue.Length;
         int max = getMonthOrWeek(_newvalue.Substring(currValue), letters, type); // number of found month

         if (array[max].Trim().Length >= letters)
            buffer.Append(array[max].Trim().Substring(0, letters));
         // insert ' ' to added places
         else
         {
            buffer.Append(array[max].Trim());
            for (int j = letters - array[max].Trim().Length; j > 0; j--)
               buffer.Append(' ');
         }
         return currValue + letters;
      }

      /// <summary>get end index of every numeric block in the mask from mask[currValue]</summary>
      /// <returns> end of numeric block in newValue</returns>
      private int getEndOfNumericBlock(int currValue)
      {
         while (currValue < _newvalue.Length && !UtilStrByteMode.isDigit(_newvalue[currValue]))
            currValue++;
         return currValue;
      }

      /// <summary>added suffix to relevant number of the day</summary>
      private String addDateSuffix(String last)
      {
         int thenumber = Int32.Parse(last);
         if (thenumber == 1 || thenumber == 21 || thenumber == 31)
            return "st";
         else if (thenumber == 2 || thenumber == 22)
            return "nd";
         else if (thenumber == 3 || thenumber == 23)
            return "rd";
         return "th";
      }

      /// <summary>get size of block letters of type 'type', which start at 'start'</summary>
      private int getSizeOfBlock(int start, int type)
      {
         int letters = start + 1;
         while (letters < _picture.Length && (int)_picture[letters] == type)
            letters++;
         return letters - start;
      }


      /// <summary>get String part/full name of month/day of week</summary>
      /// <param name="month">part/full name of month/day of week</param>
      /// <param name="max">of possible letters in month/day of week, size of block in picture of the control</param>
      /// <param name="type:">PIC_  week|month</param>
      /// <returns> number of month 1-12, or day of week 1-7</returns>
      private int getMonthOrWeek(String month_str, int letters, int type)
      {
         String[] array;
         if (type == PICInterface.PIC_MMM || type == PICInterface.PIC_MMD)
         // Month
         {
            if (hebrew)
               array = HebrewDate.GetLocalMonths();
            else
            {
               String monthStr = Events.GetMessageString(MsgInterface.MONTHS_PTR);
               array = DateUtil.getLocalMonths(monthStr);
            }
         }
         else if (type == PICInterface.PIC_WWW || type == PICInterface.PIC_W)
         // Week
         {
            if (hebrew)
               array = HebrewDate.GetLocalDows();
            else
            {
               String dayStr = Events.GetMessageString(MsgInterface.DAYS_PTR);
               array = DateUtil.getLocalDays(dayStr);
            }
         }
         else if (type == PICInterface.PIC_BB)
         // JPN: Japanese date picture support
         {
            array = UtilDateJpn.getArrayDow();
         }
         else
         {
            return 1; // only week or month possible -> diffault 1.
         }

         if (letters > month_str.Length)
            // cut letter if it's  end of string
            letters = month_str.Length;

         if (hebrew)
         {
            String hstr = month_str.TrimStart(' ').Substring(0, letters).Trim();
            for (int i = 1; i < array.Length; i++)
            // start checking from 1st member, because 0 = ""
            {
               if (array[i].Trim().Equals(hstr))
               {
                  if (i == 14)
                     i = 6;
                  return i;
               }
            }
            return 0;
         }

         try
         // the number inserted and not string -> find mounth/day of week by number 1-12|1-7
         {
            String str = month_str.Substring(0, letters).Trim();
            StringBuilder numBuffer = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
               if (UtilStrByteMode.isDigit(str[i]))
                  numBuffer.Append(str[i]);
               else
                  break;
            }

            int theNumber = Int32.Parse(numBuffer.ToString());
            if (theNumber < 1)
               return 1;
            if (type == PICInterface.PIC_MMM || type == PICInterface.PIC_MMD)
            {
               if (theNumber > 12)
                  return 12;
            }
            else if (type == PICInterface.PIC_WWW || type == PICInterface.PIC_W || type == PICInterface.PIC_BB)
            {
               // JPN: Japanese date picture support
               if (theNumber > 7)
                  return 7;
            }
            return theNumber;
         }
         catch (FormatException)
         {
            //doNothing_StringInserted
         }

         // inserted string - name of month|day of week ->try identyfy it
         int[] found = new int[array.Length];
         int from;
         int max = -1; // number of found month/day of week

         for (int tmp = 1; tmp < array.Length; tmp++)
         // start checking from 1st member, because 0 = ""
         {
            found[tmp] = 0;
            from = 0;
            for (int i = 0; i < array[tmp].Length && from < month_str.Length && i < letters; i++, from++)
            {
               if (array[tmp].ToLower()[i] == month_str.ToLower()[from])
                  found[tmp]++;
               else
                  break;
            }

            if (from == letters || from == array[tmp].Length)
            // all letters found
            {
               max = tmp;
               break;
            }
         }

         if (max == -1)
            max = findMax(found);
         return max;
      }

      /// <summary>is numeric data allowed in date
      /// return true if it is allowed, otherwise false</summary>
      private bool isNumberAllowedInDate ()
      {
         for (int i = 0; i < _picture.Length; i++)
         {
            if (isDatePositionalNumericDirective(_picture[i]))
               return true;
         }
         return false;
      }

      

      /// <summary>find if all numbers of the new user inserted value are 0
      /// return true if all numbers in the input are 0, otherwise true</summary>
      private bool isAllNumbers0()
      {
         char currChr;
         for (int i = 0; i < _newvalue.Length; i++)
         {
            currChr = _newvalue[i];

            if (UtilStrByteMode.isDigit(currChr) && currChr != '0')
               return false;
         }

         return true;
      }

      /// <summary>check validity of Year for YYYY part of the mask</summary>
      /// <param name="yearIn">inserted by user</param>
      /// <returns> calculated by environment.century right value</returns>
      private StringBuilder checkCentury(int yearIn)
      {
         Int32 outVal;
         int start_century = _environment.GetCentury(_valDet.getControl().getForm().getTask().getCompIdx());
         int century = start_century / 100;
         int years = start_century % 100;

         if (yearIn >= years && yearIn != 0)
            outVal = (century * 100 + yearIn);
         else
            outVal = ((++century) * 100 + yearIn);

         return new StringBuilder(outVal.ToString());
      }

      /// <summary>check if the date legal for picture range, used for validation date 0</summary>
      /// <param name="ContinuousRange"></param>
      /// <param name="DiscreteRange"></param>
      /// <param name="date">integer - magic date number of days from 01.01.0000</param>
      private bool isDateLegal4Range(List<String> ContinuousRange, List<String> DiscreteRange, int currDate)
      {
         PIC pic = _valDet.getPIC();
         int i;
         int downDate, topDate;
         if (ContinuousRange != null)
         {
            for (i = 0; i < ContinuousRange.Count - 1; i++)
            {
               downDate = DisplayConvertor.Instance.a_2_date_pic(ContinuousRange[i], pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx());
               topDate = DisplayConvertor.Instance.a_2_date_pic(ContinuousRange[++i], pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx());
               if (currDate >= downDate && currDate <= topDate)
                  return true;
            }
         }

         if (DiscreteRange != null)
         {
            for (i = 0; i < DiscreteRange.Count; i++)
            {
               if (currDate == DisplayConvertor.Instance.a_2_date_pic(DiscreteRange[i], pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx()))
                  return true;
            }
         }
         // the date is not in the range
         // return false;
         throw new WrongFormatException();
      }

      /// <summary>check and fill DATE type by range</summary>
      private StringBuilder fillDateByRange(List<String> ContinuousRange, List<String> DiscreteRange, String buffer)
      {
         PIC pic = _valDet.getPIC();
         int i;
         int downDate, topDate;
         int currDate = DisplayConvertor.Instance.a_2_date_pic(buffer, pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx());

         setValidationFailed(false);
         if (ContinuousRange != null)
         {
            for (i = 0; i < ContinuousRange.Count - 1; i++)
            {
               downDate = DisplayConvertor.Instance.a_2_date_pic(ContinuousRange[i], pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx());
               topDate = DisplayConvertor.Instance.a_2_date_pic(ContinuousRange[++i], pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx());
               if (currDate >= downDate && currDate <= topDate)
                  return new StringBuilder(buffer);
            }
         }

         if (DiscreteRange != null)
         {
            for (i = 0; i < DiscreteRange.Count; i++)
            {
               if (currDate == DisplayConvertor.Instance.a_2_date_pic(DiscreteRange[i], pic, pic.getMask(), _valDet.getControl().getForm().getTask().getCompIdx()))
                  return new StringBuilder(buffer);
            }
         }
         // the date is not in the range
         throw new WrongFormatException();
      }

      /// <summary>check if day suitable (not bigger than max) for Year and Month</summary>
      /// <param name="date">user inserted string</param>
      /// <param name="isDoy">is picture of needed day is DDD - day of year</param>
      private StringBuilder checkDay(String date, bool isDoy)
      {
         int day = getDateElement(date, DateElement.DAY);
         if (day < 29 && !isDoy)
            return new StringBuilder(date);

         int year = getDateElement(date, DateElement.YEAR);

         if (!isDoy)
         {

            if (year.ToString().Length < 3)
               year = Int32.Parse(checkCentury(year).ToString());

            if (_environment.GetDateMode(_valDet.getControl().getForm().getTask().getCompIdx()) == 'B')
               year -= PICInterface.DATE_BUDDHIST_GAP;

            int month = getDateElement(date, DateElement.MONTH);

            // if day not consistained to year & month - change day
            int dayMax = getMaxDayInMonth(year, month);
            if (day > dayMax)
            {
               throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
            }
         }
         else
         {
            // if month not consistained to day(DDD) - change month
            int monthReal = getMonthByDay(year, day);

            if (monthReal < 1 || monthReal > 12)
               throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
         }

         return new StringBuilder(date);
      }

      /// <summary>check if day of week suitable (right) for Year, Month and Day</summary>
      /// <param name="date">user inserted string</param>
      private StringBuilder checkWeek(String date)
      {
         int dow = getDateElement(date, DateElement.DOW);
         int dowReal;

         DisplayConvertor.DateBreakParams breakParams;
         breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
         int dateMagic = DisplayConvertor.Instance.a_2_date(date, _picture, _valDet.getControl().getForm().getTask().getCompIdx());
         DisplayConvertor.Instance.date_break_datemode(breakParams, dateMagic, true, _valDet.getControl().getForm().getTask().getCompIdx());
         dowReal = breakParams.dow;
         if (dowReal != dow)
            date = changeDow(date, dowReal);

         return new StringBuilder(date);
      }

      // JPN: Japanese date picture support
      /// <summary>check if the combination of Japanese era and year is valid.</summary>
      /// <param name="strDate">user inserted string</param>
      /// <returns> date string of properly Japanese era</returns>
      private StringBuilder checkJpnEraYear(String strDate)
      {
         int intYearAD;
         int intDoy;
         int dateMagic;
         int intYearJpnEra;
         DisplayConvertor.DateBreakParams breakParams;

         // convert date string to magic date, and get DOY
         breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
         dateMagic = DisplayConvertor.Instance.a_2_date(strDate, _picture, _valDet.getControl().getForm().getTask().getCompIdx());
         DisplayConvertor.Instance.date_break_datemode(breakParams, dateMagic, true, _valDet.getControl().getForm().getTask().getCompIdx());
         intYearAD = breakParams.year;
         intDoy = breakParams.doy;

         // get the name and a year of the era
         String strEraAlpha;
         String strEraKanji;
         strEraAlpha = UtilDateJpn.getInstance().date_jpn_yr_2_a(intYearAD, intDoy, false);
         strEraKanji = UtilDateJpn.getInstance().date_jpn_yr_2_a(intYearAD, intDoy, true);

         // convert a year (A.D.) to a year of the era
         StringBuilder strYearJpnEra = new StringBuilder();

         intYearJpnEra = UtilDateJpn.getInstance().date_jpn_year_ofs(intYearAD, intDoy);
         if (intYearJpnEra <= 0)
            throw new WrongFormatException(MsgInterface.STR_ERR_DATE);
         else if (intYearJpnEra <= 9)
            strYearJpnEra.Append("0");

         strYearJpnEra.Append(System.Convert.ToString(intYearJpnEra));

         // build a properly year of the era

         // A DBCS character in "str" consumes two characters in "mask".
         // If "str" contains DBCS, the position of "mask" has to skip next index.
         // This variable indicates the number of skipped indexes.
         int intPicIdxOfs = 0;

         for (int intCnt = 0; intCnt + intPicIdxOfs < _picture.Length; intCnt++)
         {
            switch (_picture[intCnt + intPicIdxOfs])
            {
               case (char)(PICInterface.PIC_JY1):
                  strDate = UtilStrByteMode.repC(strDate, strEraAlpha, intCnt + 1, 1);
                  break;

               case (char)(PICInterface.PIC_JY2):
                  strDate = UtilStrByteMode.repC(strDate, strEraKanji, intCnt + 1, 1);
                  intPicIdxOfs++; // PIC_JY2 consumes 2 characters of picture (1 more than strDate)
                  break;

               case (char)(PICInterface.PIC_JY4):
                  strDate = UtilStrByteMode.repC(strDate, strEraKanji, intCnt + 1, 2);
                  intCnt++; // PIC_JY4 consumes 2 characters of strDate
                  intPicIdxOfs += 2; // PIC_JY4 consumes 4 characters of picture (2 more than strDate)
                  break;

               case (char)(PICInterface.PIC_YJ):
                  strDate = UtilStrByteMode.repC(strDate, strYearJpnEra.ToString(), intCnt + 1, 2);
                  intCnt++; // PIC_YJ consumes 2 characters of strDate
                  break;

               default:
                  if (intCnt < strDate.Length)
                  {
                     // if "strDate" contains DBCS, the next index of "picture" has to be skipped.
                     if (UtilStrByteMode.isHalfWidth(strDate[intCnt]) == false && UtilStrByteMode.isHalfWidth(_picture[intCnt + intPicIdxOfs]) == true)
                        intPicIdxOfs++;
                  }
                  break;

            }
         }

         return new StringBuilder(strDate);
      }

      /// <summary>get max number of day in month</summary>
      /// <param name="year"></param>
      /// <param name="month"></param>
      /// <returns> max days [28-31] number</returns>
      private int getMaxDayInMonth(int year, int month)
      {
         bool LeapYear;
         int[] date_day_max = new int[] { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
         LeapYear = (year % 4 == 0) && ((year % 100 != 0) || (year % 400 == 0));
         if (LeapYear && month == 2)
            return date_day_max[month] + 1;
         return date_day_max[month];
      }

      /// <summary>get month for day, than day have mask DDD</summary>
      /// <param name="year"></param>
      /// <param name="day">[0-365]</param>
      /// <returns> number of month</returns>
      private int getMonthByDay(int year, int day)
      {
         int i;
         bool LeapYear;
         LeapYear = (year % 4 == 0) && ((year % 100 != 0) || (year % 400 == 0));

         for (i = 0; i < PICInterface.date_day_tab.Length; i++)
         {
            if (!LeapYear)
            {
               if (PICInterface.date_day_tab[i] >= day)
                  return i;
            }
            // is Leap Year
            else
            {
               if (i < 3)
               {
                  if (PICInterface.date_day_tab[i] >= day)
                     return i;
               }
               // add 1 day after second month
               else
               {
                  if (PICInterface.date_day_tab[i] + 1 >= day)
                     return i;
               }
            }
         } // for loop
         return i; // the function cann't came to the statement anytime
      }

      /// </summary>get Year|Month|Day|Dow of date
      /// <param name="date">date</param>
      /// <param name="element">required date element (year/month/day/dow) </param>
      /// <returns>Year|Month|Day|Dow of date</returns>
      private int getDateElement(String date, DateElement element)
      {
         String buffer = null;
         int letters = 0;
         bool exitLoop = false;
         int picWeekType = PICInterface.PIC_WWW; // JPN: Japanese date picture support
         bool isJpnEraYear = false; // JPN: Japanese date picture support
         int era_year = 0;

         // A DBCS character in "date" consumes two characters in "picture".
         // If "date" contains DBCS, the position of "picture" has to skip next index.
         // This variable indicates the number of skipped indexes.
         int intPicIdxOfs = 0; // JPN: Japanese date picture support

         int minLen = 0;
         if (UtilStrByteMode.isLocaleDefLangJPN())
            minLen = UtilStrByteMode.getMinLenPicture(date, _picture);
         else
            minLen = Math.Min(date.Length, _picture.Length);

         for (int i = 0; i + intPicIdxOfs < minLen && !exitLoop; i++)
         {
            if (isDataPositionalDirective(_picture[i + intPicIdxOfs]))
            {
               switch ((int)_picture[i + intPicIdxOfs])
               {

                  case PICInterface.PIC_DD:
                     if (element == DateElement.DAY)
                     {
                        buffer = date.Substring(i, 2);
                        exitLoop = true;
                        break;
                     }
                     i++;
                     break;

                  case PICInterface.PIC_DDD:
                     if (element == DateElement.DAY)
                     {
                        buffer = date.Substring(i, 3);
                        exitLoop = true;
                        break;
                     }
                     i += 2;
                     break;

                  case PICInterface.PIC_DDDD:
                     if (element == DateElement.DAY)
                     {
                        buffer = date.Substring(i, 2); // not +4 need not suffix of 'nd', 'rd', 'st'
                        exitLoop = true;
                     }
                     i += 3;
                     break;

                  case PICInterface.PIC_MMD:
                     if (element == DateElement.MONTH)
                     {
                        buffer = date.Substring(i, 2);
                        exitLoop = true;
                     }
                     i++;
                     break;

                  case PICInterface.PIC_MMM:
                     // get size of PIC_MMM block
                     letters = getSizeOfBlock(i + intPicIdxOfs, PICInterface.PIC_MMM);
                     if (element == DateElement.MONTH)
                     {
                        buffer = date.Substring(i, letters);
                        exitLoop = true;
                        break;
                     }
                     i += letters - 1;
                     letters = 0;
                     break;

                  case PICInterface.PIC_YY:
                     if (element == DateElement.YEAR)
                     {
                        buffer = date.Substring(i, 2);
                        exitLoop = true;
                     }
                     i++;
                     break;

                  case PICInterface.PIC_YYYY:
                     if (element == DateElement.YEAR)
                     {
                        buffer = date.Substring(i, 4);
                        exitLoop = true;
                     }
                     i += 3;
                     break;

                  case PICInterface.PIC_W:
                     if (element == DateElement.DOW)
                     {
                        buffer = date.Substring(i, 1);
                        exitLoop = true;
                     }
                     break;

                  case PICInterface.PIC_WWW:
                     letters = getSizeOfBlock(i + intPicIdxOfs, PICInterface.PIC_WWW);
                     if (element == DateElement.DOW)
                     {
                        buffer = date.Substring(i, letters);
                        exitLoop = true;
                        break;
                     }
                     i += letters - 1;
                     letters = 0;
                     break;

                  case PICInterface.PIC_HYYYYY:
                  case PICInterface.PIC_HL:
                  case PICInterface.PIC_HDD:
                     // not implemented for hebrew yet
                     break;

                  case PICInterface.PIC_BB:  // JPN: Japanese date picture support
                     letters = getSizeOfBlock(i + intPicIdxOfs, PICInterface.PIC_BB);

                     if (i + (letters / 2) < date.Length)
                     {
                        buffer = date.Substring(i, (letters / 2));
                        // if date contains DBCS, some indexes of picture have to be skipped.
                        intPicIdxOfs += letters - buffer.Length;
                     }

                     if (element == DateElement.DOW)
                     {
                        exitLoop = true;
                        picWeekType = PICInterface.PIC_BB;
                     }
                     else
                     {
                        buffer = null;
                     }

                     i += letters - 1;
                     break;

                  case PICInterface.PIC_JY1:  // JPN: Japanese date picture support
                     isJpnEraYear = true;
                     break;

                  case PICInterface.PIC_JY2:  // JPN: Japanese date picture support
                     isJpnEraYear = true;
                     intPicIdxOfs++;
                     break;

                  case PICInterface.PIC_JY4:  // JPN: Japanese date picture support
                     isJpnEraYear = true;
                     intPicIdxOfs += 2;
                     i++;
                     break;

                  case PICInterface.PIC_YJ:  // JPN: Japanese date picture support
                     if (element == DateElement.YEAR)
                     {
                        buffer = date.Substring(i, 2);
                        exitLoop = true;
                     }
                     i++;
                     break;
               } // switch of positional directives
            } // if of positional directives
         } // for loop in picture

         if (buffer == null)
         {
            if (element == DateElement.YEAR)
               return DateTime.Now.Year;
            else
               return 1;
         }

         buffer = buffer.Trim();

         if (letters > 0)
         // it's PIC_WWW or PIC_MMM
         {
            if (element == DateElement.MONTH)
               letters = getMonthOrWeek(buffer, letters, PICInterface.PIC_MMM);
            else if (element == DateElement.DOW)
               letters = getMonthOrWeek(buffer, letters, picWeekType);
         }
         else
         {
            while (buffer.StartsWith("0") || buffer.StartsWith(" "))
               buffer = buffer.Substring(1);
            if (buffer.Length == 0)
               letters = 0;
            else
               letters = Int32.Parse(buffer);
         }

         if (element == DateElement.YEAR && isJpnEraYear)
         {
            // get the start year of the era
            era_year = UtilDateJpn.getInstance().getStartYearOfEra(date, _picture);
            if (era_year == 0)
               return 0;
            letters += era_year - 1;
         }

         return letters;
      }

      /// <summary>change day number in date field to dayMax</summary>
      /// <param name="date">to change day into</param>
      /// <param name="dayMax">the new day value</param>
      private String changeDow(String date, int dow)
      {
         bool isDate = false;
         int letters;
         String startPart, finishPart, number;
         number = "" + dow; // " "

         // A DBCS character in "date" consumes two characters in "picture".
         // If "date" contains DBCS, the position of "picture" has to skip next index.
         // This variable indicates the number of skipped indexes. (JPN: DBCS support)
         int intPicIdxOfs = 0;

         for (int i = 0; i + intPicIdxOfs < _picture.Length && !isDate; i++)
         {
            if (isDataPositionalDirective(_picture[i + intPicIdxOfs]))
            {
               switch ((int)_picture[i + intPicIdxOfs])
               {

                  case PICInterface.PIC_W:
                     startPart = date.Substring(0, i);
                     finishPart = date.Substring(i + 1);
                     date = startPart + number + finishPart;
                     isDate = true;
                     break;

                  case PICInterface.PIC_WWW:
                     letters = getSizeOfBlock(i + intPicIdxOfs, PICInterface.PIC_WWW);
                     startPart = date.Substring(0, i);
                     finishPart = date.Substring(i + letters);
                     String dayStr = Events.GetMessageString(MsgInterface.DAYS_PTR);
                     String[] dayNames = DateUtil.getLocalDays(dayStr);
                     number = dayNames[dow];

                     if (number.Length > letters)
                        number = number.Substring(0, letters);
                     else if (number.Length < letters)
                        while (number.Length < letters)
                           number = number + ' ';
                     date = startPart + number + finishPart;
                     isDate = true;
                     break;

                  case PICInterface.PIC_BB:  // JPN: Japanese date picture support
                     letters = getSizeOfBlock(i + intPicIdxOfs, PICInterface.PIC_BB);
                     // "letters" means a number of characters in "picture"

                     startPart = date.Substring(0, i);
                     finishPart = date.Substring(i + (letters / 2));
                     // "letters / 2" means a number of characters in "date"

                     number = UtilDateJpn.getStrDow(dow);

                     if (UtilStrByteMode.lenB(number) > letters)
                        number = UtilStrByteMode.leftB(number, letters);
                     else if (UtilStrByteMode.lenB(number) < letters)
                        while (UtilStrByteMode.lenB(number) < letters)
                           number = number + ' ';
                     date = startPart + number + finishPart;
                     isDate = true;
                     break;

                  default:
                     if (UtilStrByteMode.isLocaleDefLangDBCS())
                     // JPN: DBCS support
                     {
                        if (i < date.Length)
                        {
                           // if "date" contains DBCS, the next index of "picture" has to be skipped.
                           if (UtilStrByteMode.isHalfWidth(date[i]) == false && UtilStrByteMode.isHalfWidth(_picture[i + intPicIdxOfs]) == true)
                              intPicIdxOfs++;
                        }
                     }
                     break;

               } // switch
            } // if of positional directives
         }
         return date;
      }

      /// <summary>********************TIME*************************************</summary>

      /// <summary>check & validate Time field by his old/new value and 2 pictures:
      /// Real and Enable
      /// 1.check if newvalue complete with pictures, otherwise return oldvalue
      /// 2.check if newvalue complete with range
      /// 2.1 complete value, if possible, otherwise  return oldvalue
      /// </summary>
      /// <returns> String - new value of validated field</returns>
      private String checkTimeField()
      {
         int hour = 0;
         StringBuilder buffer = new StringBuilder();
         int currValue = 0;

         for (int i = 0; i < _picture.Length; i++)
         {
            if (isTimePositionalDirective(_picture[i]))
            {
               switch ((int)_picture[i])
               {
                  case PICInterface.PIC_HH:
                     currValue = add2Time(currValue, buffer, PICInterface.PIC_HH);
                     hour = Int32.Parse(buffer.ToString().Substring(buffer.Length - 2));
                     break;

                  case PICInterface.PIC_MMT:
                     currValue = add2Time(currValue, buffer, PICInterface.PIC_MMT);
                     break;

                  case PICInterface.PIC_SS:
                     currValue = add2Time(currValue, buffer, PICInterface.PIC_SS);
                     break;

                  case PICInterface.PIC_MS:
                     currValue = add2Time(currValue, buffer, PICInterface.PIC_MS);
                     break;

                  case PICInterface.PIC_PM:
                     currValue = addPM2Time(currValue, buffer);
                     if (buffer.ToString().Substring(buffer.Length - 2).ToUpper().Equals("pm".ToUpper()))
                        hour += 12;
                     break;
               }
               i++; // every one of the types can occupate 2 letters ONLY
            }
            else
            {
               buffer.Append(_picture[i]);
               if (_newvalue.IndexOf("" + _picture[i], currValue) != -1)
               // try to find the letter in newvalue
               {
                  currValue = _newvalue.IndexOf("" + _picture[i], currValue) + 1;
               } // if Ineed +1 ? Alex
            }
         }

         // range of the time
         if (_valDet.getContinuousRangeValues() != null || _valDet.getDiscreteRangeValues() != null)
            buffer = fillTimeByRange(_valDet.getContinuousRangeValues(), _valDet.getDiscreteRangeValues(), buffer.ToString());

         return buffer.ToString();
      }

      /// <summary>Add found in newvalue numbers(2(or 1) letters) to buffer and return
      /// last found number index
      /// </summary>
      /// <param name="currValue">in newvalue</param>
      /// <param name="buffer">/// </param>
      /// <param name="type">which type of positional directive, for checking validity</param>
      /// <returns> currValue in newvalue</returns>
      private int add2Time(int currValue, StringBuilder buffer, int type)
      {
         int start = currValue;
         int i;
         String STR_00 = "00";
         StringBuilder currAdd = new StringBuilder();

         while (start < _newvalue.Length && !UtilStrByteMode.isDigit(_newvalue[start]))
         {
            start++; // find first digit
            // non-digits were found in input string
            // we do not throw STR_ERR_TIME exception to prevent input digits lost
            // on non-digits place STR_00 will be added
            // Since Mask Editing . This error is irrelevant. Time now, always contains non digits like : and might contain ' ' for unfilled chars.
            // This is a valid case and we do not want an error popping about it. 
            //  guiManager.writeToMessagePane(valDet.getControl().getForm().getTask(), ClientManager.Instance.getMessageString(STR_ERR_TIME), StatusBarMessageType.SB_RUNTIME_MSG);
            //  setValidationFailed(true);
         }

         if (start == _newvalue.Length)
         // not found more digits
         {
            buffer.Append(STR_00);
            return currValue;
         }

         for (i = start; i < start + 2 && i < _newvalue.Length; i++)
         {
            if (UtilStrByteMode.isDigit(_newvalue[i]))
               currAdd.Append(_newvalue[i]);
            else
            {
               currAdd.Insert(0, new char[] { '0' }); // only 1 digit found
               break;
            }
         }

         if (i == _newvalue.Length && i == start + 1)
            currAdd.Insert(0, new char[] { '0' });
         // only 1 digit found
         else if (i == _newvalue.Length && i == start)
            currAdd.Append(STR_00);

         int add = Int32.Parse(currAdd.ToString());

         /*if (add < 0)
         currAdd=new StringBuffer("00");
         else if (add > 59 && type==PIC_MMT)
         currAdd=new StringBuffer("59");
         else if (add > 59 && type==PIC_SS)
         currAdd=new StringBuffer("59");*/
         if (add < 0 || (add > 59 && type == PICInterface.PIC_MMT) || (add > 59 && type == PICInterface.PIC_SS))
            throw new WrongFormatException(MsgInterface.STR_ERR_TIME);
         else if (type == PICInterface.PIC_HH)
         {
            bool PM = getIsPM();
            String newStr = null;
            int dif;

            if ((add >= 12 || add == 0) && PM)
            {
               int addTmp = add % 24;

               _isPm = (addTmp >= 12 && addTmp < 24);

               if (addTmp > 12)
                  dif = addTmp - 12;
               else if (addTmp == 0)
                  dif = 12;
               else
                  dif = addTmp;

               newStr = ((dif > 9 ? "" : "0") + dif);
            }
            else if (add > 99 && !PM)
               newStr = "99";

            if (newStr != null)
            {
               currAdd.Remove(0, currAdd.Length);
               currAdd.Append(newStr);
            }
         }

         buffer.Append(currAdd.ToString());
         return i;
      }

      /// <summary>Append PM or AM to time</summary>
      private int addPM2Time(int currValue, StringBuilder buffer)
      {
         int i;

         if ((i = _newvalue.ToUpper().IndexOf("AM", currValue)) != -1)
         {
            buffer.Append("am");
            return i + 2;
         }
         else
         {
            if ((i = _newvalue.ToUpper().IndexOf("PM", currValue)) != -1)
            {
               buffer.Append("pm");
               return i + 2;
            }
            else if (_isPm)
            {
               buffer.Append("pm");
               return currValue;
            }
            else
               buffer.Append("am");
         }
         return currValue;
      }

      /// <summary>Check if where is 'PM' positional directive in the Time Picture</summary>
      /// <returns> true - there is PM, else false</returns>
      private bool getIsPM()
      {
         for (int curr = 0; curr < _picture.Length; curr++)
         {
            if ((int)_picture[curr] == PICInterface.PIC_PM)
               return true;
         }
         return false;
      }

      private StringBuilder fillTimeByRange(List<String> ContinuousRange, List<String> DiscreteRange, String buffer)
      {
         PIC pic = _valDet.getPIC();
         int currTime = DisplayConvertor.Instance.a_2_time(buffer, pic, false);
         int downTime, topTime;
         int i;

         setValidationFailed(false);
         if (ContinuousRange != null)
         {
            for (i = 0; i < ContinuousRange.Count - 1; i++)
            {
               downTime = DisplayConvertor.Instance.a_2_time(ContinuousRange[i], pic, false);
               topTime = DisplayConvertor.Instance.a_2_time(ContinuousRange[++i], pic, false);
               if (currTime >= downTime && currTime <= topTime)
                  return new StringBuilder(buffer);
            }
         }

         if (DiscreteRange != null)
         {
            for (i = 0; i < DiscreteRange.Count; i++)
            {
               if (currTime == DisplayConvertor.Instance.a_2_time(DiscreteRange[i], pic, false))
                  return new StringBuilder(buffer);
            }
         }
         // the time is not in the range
         throw new WrongFormatException();
      }

      /// <summary>*************************************************************
      /// SERVICE functions
      /// ***************************************************************
      /// </summary>

      /// <summary> print ERROR prompt message about range to status bar</summary>
      /// <param name="msgId">of error Message</param>
      /// <param name="setValidationFaild">validation failed</param>
      private void printMessage(string msgId, bool setValidationFaild)
      {
         StringBuilder message = new StringBuilder("");
         MgControlType ctrlType;

         if (msgId.Equals(MsgInterface.EDT_ERR_STR_1))
         {
            PIC pic = _valDet.getPIC();
            String token = "%d";
            String messageString = Events.GetMessageString(msgId);
            messageString = StrUtil.replaceStringTokens(messageString, token, 1, System.Convert.ToString(pic.getWholes()));
            messageString = StrUtil.replaceStringTokens(messageString, token, 1, System.Convert.ToString(pic.getDec()));
            message.Append(messageString);
         }
         else
         {
            message.Append(Events.GetMessageString(msgId));
            if (msgId == MsgInterface.STR_RNG_TXT && _valDet.getRange() != null)
               message.Append(StrUtil.makePrintableTokens(_valDet.getRange(), StrUtil.HTML_2_STR));
         }

         if (_valDet.getControl() != null)
         {
            ctrlType = _valDet.getControl().Type;
            if (ctrlType != MgControlType.CTRL_TYPE_BUTTON && ctrlType != MgControlType.CTRL_TYPE_RADIO && !_valDet.getControl().isSelectionCtrl())
            {
                String msgString = message.ToString();
                Manager.WriteToMessagePane(_valDet.getControl().getForm().getTask(), StrUtil.makePrintable2(msgString), true);
               _valDet.getControl().getForm().ErrorOccured = !String.IsNullOrEmpty(msgString);
            }
         }
         if (setValidationFaild)
            // from this point the focus send back to control and setValidationFailed(true) can not set it to true
            setValidationFailed(true);
      }

      /// <summary>Change inside 'newvalue' substring on position 'index' to string 'to'</summary>
      /// <param name="newvalue">to change inside</param>
      /// <param name="to">string to change by him</param>
      /// <param name="index">the substring to change on the position</param>
      private void changeStringInside(String to, int index)
      {
         _newvalue = _newvalue.Substring(0, index) + to + _newvalue.Substring(index + to.Length);
      }

      /// <summary> Initialize newvalue/oldvalue members</summary>
      private void init(ValidationDetails validationDetails)
      {
         PIC pic;

         _valDet = validationDetails;
         _valDet.setNull(false);
         _isPm = false;
         pic = _valDet.getPIC();
         _oldvalue = _valDet.getOldValue();
         _newvalue = _valDet.getDispValue();
         if (_newvalue == null)
            _newvalue = "";
         if (_oldvalue == null)
            _oldvalue = "";
         if (pic == null || pic.getAttr() == StorageAttribute.BLOB || pic.getAttr() == StorageAttribute.BLOB_VECTOR)
            return;
         _picture = pic.getMask();
         hebrew = pic.isHebrew();
         _picture = _picture.Substring(0, pic.getMaskSize());
         //if (valDet.getType() == 'L' && newvalue.Length > picture.Length)
         //   newvalue = newvalue.Substring(0, picture.Length);
      }

      /// <summary>Return index of Max member in array</summary>
      private int findMax(int[] array)
      {
         int index = 0;
         int max = array[index];
         for (int i = 1; i < array.Length; i++)
         {
            if (array[i] > max)
            {
               max = array[i];
               index = i;
            }
         }
         return index;
      }

      /// <summary>is checked char Alpha Positional Directive</summary>
      internal static bool isAlphaPositionalDirective(char toCheck)
      {
         int ascii = (int)toCheck;
         return (ascii == PICInterface.PIC_X || ascii == PICInterface.PIC_U || ascii == PICInterface.PIC_L || ascii == PICInterface.PIC_N || ascii == PICInterface.PIC_J || ascii == PICInterface.PIC_G || ascii == PICInterface.PIC_S || ascii == PICInterface.PIC_T);
      }

      /// <summary>is checked char Data Positional Directive</summary>
      private bool isDataPositionalDirective(char toCheck)
      {
         int ascii = (int)toCheck;
         return (ascii == PICInterface.PIC_YY || ascii == PICInterface.PIC_YYYY || ascii == PICInterface.PIC_MMD || ascii == PICInterface.PIC_MMM || ascii == PICInterface.PIC_DD || ascii == PICInterface.PIC_DDD || ascii == PICInterface.PIC_DDDD || ascii == PICInterface.PIC_W || ascii == PICInterface.PIC_WWW || ascii == PICInterface.PIC_HYYYYY || ascii == PICInterface.PIC_HL || ascii == PICInterface.PIC_HDD || ascii == PICInterface.PIC_YJ || ascii == PICInterface.PIC_JY1 || ascii == PICInterface.PIC_JY2 || ascii == PICInterface.PIC_JY4 || ascii == PICInterface.PIC_BB); // JPN: Japanese date picture support
      }

      /// <summary>check if Numeric Data Positional Directive exists</summary>
      private bool isDatePositionalNumericDirective(char toCheck)
      {
         int ascii = (int)toCheck;
         return (ascii == PICInterface.PIC_YY || ascii == PICInterface.PIC_YYYY || ascii == PICInterface.PIC_MMD || ascii == PICInterface.PIC_DD || ascii == PICInterface.PIC_DDD || ascii == PICInterface.PIC_DDDD || ascii == PICInterface.PIC_W || ascii == PICInterface.PIC_HYYYYY || ascii == PICInterface.PIC_HL || ascii == PICInterface.PIC_HDD || ascii == PICInterface.PIC_YJ || ascii == PICInterface.PIC_JY1 || ascii == PICInterface.PIC_JY2 || ascii == PICInterface.PIC_JY4 || ascii == PICInterface.PIC_BB); // JPN: Japanese date picture support
      }

      /// <summary>is checked char Numeric Positional Directive</summary>
      private bool isNumericPositionalDirective(char toCheck)
      {
         int ascii = (int)toCheck;
         return (ascii == PICInterface.PIC_N);
      }

      /// <summary>is checked char Logic Positional Directive</summary>
      private bool isLogicPositionalDirective(char toCheck)
      {
         int ascii = (int)toCheck;
         return (ascii == PICInterface.PIC_X);
      }

      /// <summary>Find if the letter at index is Positional Date Directive</summary>
      /// <param name="char">to check in picture</param>
      /// <returns> true if Positional Date Directive; false otherwise</returns>
      private bool isTimePositionalDirective(char toCheck)
      {
         int ascii = (int)toCheck;
         return (ascii == PICInterface.PIC_HH || ascii == PICInterface.PIC_MMT || ascii == PICInterface.PIC_SS || ascii == PICInterface.PIC_PM || ascii == PICInterface.PIC_MS);
      }

      /// <summary>is checked string Positional Directive of any type</summary>
      /// <param name="full/part">of picture
      /// </param>
      /// <returns> index of directive position or -1 if not found</returns>
      private int indexOfPositionalNonDirective(String str)
      {
         char tmp;

         for (int i = 0; i < str.Length; i++)
         {
            tmp = str[i];
            if (isAlphaPositionalDirective(tmp) || isDataPositionalDirective(tmp) || isNumericPositionalDirective(tmp) || isLogicPositionalDirective(tmp) || isTimePositionalDirective(tmp))
               continue;
            return i;
         }
         return -1;
      }

      /// <summary> set the "validation failed" flag</summary>
      /// <param name="val">value to use</param>
      private void setValidationFailed(bool val)
      {
         if (_valDet.ValidationFailed)
            return;
         else
            _valDet.setValidationFailed(val);
      }

      /// <summary> Count digits in string before delimeter</summary>
      /// <param name="inputString">- string to look into for delimeter</param>
      /// <param name="searchStart">- initial index position for search in inputString</param>
      /// <param name="delimeter">we are looking for</param>
      private int digitsBeforeDelimeter(String inputString, int searchStart, String delimeter)
      {
         int counter = 0;
         int searchEndPos = inputString.IndexOf(delimeter, searchStart);

         if (searchEndPos < 0)
            searchEndPos = inputString.Length;

         for (int i = searchStart; i < searchEndPos; i++)
            if (Char.IsDigit(inputString[i]))
               counter++;

         return counter;
      }


      /// <summary> by Ctrl+U (Set to Null), null display value is set to ctrl. In this case we need following checking , in order to set field to null.</summary>
      /// <param name="val">- entered value to value to check</param>
      private bool isNullDisplayVal ()
      {
         bool isNullDisplayVal = false;
         Field field = _valDet.getControl().getField();

         if (field.NullAllowed && field.hasNullDisplayValue())
         {
            String dispDefValue = field.getNullDisplay();
            isNullDisplayVal = dispDefValue.Equals(_newvalue);
         }

         return isNullDisplayVal;
      }


      #region Nested type: DateElement

      private enum DateElement
      {
         YEAR = 'Y',
         MONTH = 'M',
         DAY = 'D',
         DOW = 'W' // day of the week
      }

      #endregion
   }
}
