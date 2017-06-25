using System;
using System.Text;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using Field = com.magicsoftware.richclient.data.Field;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   internal class Argument
   {
      private static readonly int _tempTableKey = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);
      // reference key for DotNet object

      private readonly int _dnObjectCollectionKey;
      private Expression _exp;
      private Field _fld;
      private bool _skip;
      private char _type; // Field|Expression|Value
      private String _val;
      private StorageAttribute _valueAttr; // true is argument is of type ARG_TYPE_VALUE and
      private bool _valueIsNull;

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal Argument()
      {
         _type = (char)(0);
         _fld = null;
         _exp = null;
         _skip = false;
      }

      /// <summary>
      /// CTOR - create an argument from a field
      /// </summary>
      public Argument(Field field)
      {
         _fld = field;
         _type = ConstInterface.ARG_TYPE_FIELD;
         _exp = null;
         _skip = false;
      }

      /// <summary>
      ///   CTOR that creates a "Value" type argument from a given Expression Value
      /// </summary>
      /// <param name = "expVal">the source expression value</param>
      protected internal Argument(GuiExpressionEvaluator.ExpVal expVal)
      {
         _type = ConstInterface.ARG_TYPE_VALUE;
         _fld = null;
         _exp = null;
         _skip = false;

         if (expVal.IsNull)
            _val = null;
         else
         {
            _val = expVal.ToMgVal();
            if (expVal.Attr == StorageAttribute.DOTNET &&
                (String.IsNullOrEmpty(_val) || !BlobType.isValidDotNetBlob(_val)))
            {
               _dnObjectCollectionKey = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);
               _val = BlobType.createDotNetBlobPrefix(_dnObjectCollectionKey);

               if (expVal.DnMemberInfo != null)
                  DNManager.getInstance().DNObjectsCollection.Update(_dnObjectCollectionKey,
                                                                     expVal.DnMemberInfo.value);

               // add this key into 'tempDNObjectsCollectionKeys', so that it can be freed later.
               ClientManager.Instance.getTempDNObjectCollectionKeys().Add(_dnObjectCollectionKey);
            }
         }

         _valueIsNull = (_val == null);
         _valueAttr = expVal.Attr;
      }

      /// <summary>
      ///   CTOR that creates a "Value" type argument from a given Field or Expression type
      ///   argument
      /// </summary>
      /// <param name = "srcArg">the source argument</param>
      protected internal Argument(Argument srcArg)
      {
         Expression.ReturnValue retVal;
         _type = ConstInterface.ARG_TYPE_VALUE;

         switch (srcArg._type)
         {
            case ConstInterface.ARG_TYPE_FIELD:
               _val = srcArg._fld.getValue(true);
               _valueIsNull = srcArg._fld.isNull();
               _valueAttr = srcArg._fld.getType();
               break;

            case ConstInterface.ARG_TYPE_EXP:
               // TODO: EHUD: find a way to know in advance the expected type and size

               retVal = srcArg._exp.evaluate(255);
               _val = retVal.mgVal;

               _valueAttr = retVal.type;

               _valueIsNull = (_val == null);
               if (_valueIsNull)
                  _val = getEmptyValue((retVal.type == StorageAttribute.BLOB ||
                                        retVal.type == StorageAttribute.BLOB_VECTOR));
               break;

            case ConstInterface.ARG_TYPE_SKIP:
               _skip = true;
               break;

            default:
               throw new ApplicationException("in Argument.Argument(): illegal source Argument type!");
         }
      }

      /// <summary>
      ///   Parsing Argument string and fill inner objects
      /// </summary>
      /// <param name = "arg">string to parse </param>
      /// <param name = "srcTask">of the argument to find expression argument </param>
      protected internal void fillData(String arg, Task srcTask)
      {
         _type = arg[0];
         String argElements = arg.Substring(2);

         switch (_type)
         {
            case ConstInterface.ARG_TYPE_FIELD:
               {
                  string[] fieldId = argElements.Split(',');
                  int parent = Int32.Parse(fieldId[0]);
                  int fldIdx = Int32.Parse(fieldId[1]);
                  _fld = (Field)srcTask.getField(parent, fldIdx);
               }
               break;

            case ConstInterface.ARG_TYPE_EXP:
               int expNum = Int32.Parse(argElements);
               _exp = srcTask.getExpById(expNum);
               break;

            case ConstInterface.ARG_TYPE_SKIP:
               _skip = true;
               break;

            default:
               Logger.Instance.WriteExceptionToLog("in Argument.FillData() illegal type: " + arg);
               break;
         }
      }

      /// <summary>
      ///   build the XML string for the argument
      /// </summary>
      /// <param name = "message">the XML string to append the argument to</param>
      protected internal void buildXML(StringBuilder message)
      {
         switch (_type)
         {
            case ConstInterface.ARG_TYPE_FIELD:
               message.Append("F:" + _fld.getTask().getTaskTag() + "," + _fld.getId());
               break;

            case ConstInterface.ARG_TYPE_EXP:
               message.Append("E:" + _exp.getId());
               break;

            case ConstInterface.ARG_TYPE_SKIP:
               message.Append("X:0");
               break;

            default:
               Logger.Instance.WriteExceptionToLog("in Argument.buildXML() illegal type: " + _type);
               break;
         }
      }

      /// <summary>
      ///   returns the type of the argument
      /// </summary>
      internal char getType()
      {
         return _type;
      }

      /// <summary>
      ///   for field type arguments returns a reference to the field
      /// </summary>
      internal Field getField()
      {
         if (_type == ConstInterface.ARG_TYPE_FIELD)
            return _fld;
         return null;
      }

      /// <summary>
      ///   for expression type arguments returns a reference to the expression
      /// </summary>
      protected internal Expression getExp()
      {
         if (_type == ConstInterface.ARG_TYPE_EXP)
            return _exp;
         return null;
      }

      /// <summary>
      ///   set the value of this argument to a given field
      /// </summary>
      /// <param name = "destFld">the destination field
      /// </param>
      internal void setValueToField(Field destFld)
      {
         String val;
         bool isNull;

         switch (_type)
         {
            case ConstInterface.ARG_TYPE_FIELD:
               val = _fld.getValue(true);
               val = convertArgs(val, _fld.getType(), destFld.getType());
               isNull = _fld.isNull() || val == null;
               break;

            case ConstInterface.ARG_TYPE_EXP:
               val = _exp.evaluate(destFld.getType(), destFld.getSize());
               isNull = (val == null);
               if (isNull)
                  val = getEmptyValue((destFld.getType() == StorageAttribute.BLOB ||
                                       destFld.getType() == StorageAttribute.BLOB_VECTOR));
               break;

            case ConstInterface.ARG_TYPE_VALUE:
               val = convertArgs(_val, _valueAttr, destFld.getType());
               isNull = _valueIsNull || val == null;
               break;

            default:
               return;
         }

         // Update destination field's _isNULL with the value from record. This is needed to identify
         // if the variable is modified.
         destFld.takeValFromRec();

         destFld.setValueAndStartRecompute(val, isNull, ((Task)destFld.getTask()).DataViewWasRetrieved, false, true);
      }

      /// <summary>
      /// converts Alpha/Unicode values to Blob and vice versa.
      /// </summary>
      /// <param name="value"></param>
      /// <param name="srcAttr"></param>
      /// <param name="expectedType"></param>
      /// <returns></returns>
      internal static String convertArgs(String value, StorageAttribute srcAttr, StorageAttribute expectedType)
      {
         int key;
         object dotNetObj = null;
         bool invalidArg = false;

         if (srcAttr != StorageAttribute.DOTNET &&
             expectedType == StorageAttribute.DOTNET)
         {
            //Convert Magic To DotNet
            key = _tempTableKey;
            dotNetObj = DNConvert.convertMagicToDotNet(value, srcAttr,
                                                       DNConvert.getDefaultDotNetTypeForMagicType(value, srcAttr));

            DNManager.getInstance().DNObjectsCollection.Update(key, dotNetObj);
            value = BlobType.createDotNetBlobPrefix(key);
         }
         else if (srcAttr == StorageAttribute.DOTNET &&
                  expectedType != StorageAttribute.DOTNET &&
                  expectedType != StorageAttribute.NONE)
         {
            //Convert DotNet to Magic
            key = BlobType.getKey(value);

            if (key != 0)
            {
               dotNetObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);
               value = DNConvert.convertDotNetToMagic(dotNetObj, expectedType);
            }
         }
         else
         {
            switch (expectedType)
            {
               case StorageAttribute.ALPHA :
               case StorageAttribute.UNICODE:
                  if (srcAttr == StorageAttribute.BLOB)
                  {
                     if (BlobType.isValidBlob(value))
                        value = BlobType.getString(value);
                  }
                  else if (!StorageAttributeCheck.IsTypeAlphaOrUnicode(srcAttr))
                     invalidArg = true;
                  break;

               case StorageAttribute.NUMERIC :
               case StorageAttribute.DATE :
               case StorageAttribute.TIME :
                  if (!StorageAttributeCheck.isTypeNumeric(srcAttr))
                     invalidArg = true;
                  break;

               case StorageAttribute.BLOB:
                  if (StorageAttributeCheck.IsTypeAlphaOrUnicode(srcAttr))
                  {
                     char contentType = srcAttr == StorageAttribute.ALPHA
                                           ? BlobType.CONTENT_TYPE_ANSI
                                           : BlobType.CONTENT_TYPE_UNICODE;
                     value = BlobType.createFromString(value, contentType);
                  }
                  else if (!StorageAttributeCheck.isTypeBlob(srcAttr))
                     invalidArg = true;
                  break;
            }

            //If there is mismatch in attribute, take default value of expectd argument.
            if (invalidArg)
               value = FieldDef.getMagicDefaultValue(expectedType);

         }
         
         return value;
      }

      /// <summary>
      ///   get value of Argument
      /// </summary>
      /// <param name = "expType">type of expected type of evaluation</param>
      /// <param name = "expSize">size of expected string from evaluation</param>
      /// <returns> value of the Argument</returns>
      protected internal String getValue(StorageAttribute expType, int expSize)
      {
         switch (_type)
         {
            case ConstInterface.ARG_TYPE_EXP:
               _val = _exp.evaluate(expType, expSize);
               if (_val == null)
                  _val = getEmptyValue(expType == StorageAttribute.BLOB ||
                                       expType == StorageAttribute.BLOB_VECTOR);
               break;

            case ConstInterface.ARG_TYPE_FIELD:
               _val = _fld.getValue(true);
               break;

            case ConstInterface.ARG_TYPE_VALUE:
               break;

            default:
               return null;
         }
         return _val;
      }

      /// <returns> translation of this argument into Magic URL style (e.g. -Aalpha or -N17 etc.)</returns>
      protected internal void toURL(StringBuilder htmlArgs, bool makePrintable)
      {
         if (!skipArg())
         {
            String argValue = null, rangeStr = null;
            StorageAttribute attribute = StorageAttribute.NONE;
            bool isNull = false;
            PIC pic = null;
            NUM_TYPE num1, num2;
            Expression.ReturnValue retVal;
            int compIdx = 0;

            // Get the value and attribute and set the "is null" flag according
            // to the argument type
            switch (_type)
            {
               case ConstInterface.ARG_TYPE_VALUE:
                  isNull = _valueIsNull;
                  attribute = _valueAttr;
                  argValue = _val;
                  break;

               case ConstInterface.ARG_TYPE_EXP:
                  retVal = _exp.evaluate(2000);
                  argValue = retVal.mgVal;
                  if (argValue == null)
                     isNull = true;
                  else
                     attribute = retVal.type;
                  compIdx = _exp.getTask().getCompIdx();
                  break;

               case ConstInterface.ARG_TYPE_FIELD:
                  if (_fld.isNull())
                     isNull = true;
                  else
                  {
                     argValue = _fld.getValue(false);
                     attribute = _fld.getType();
                  }
                  compIdx = _fld.getTask().getCompIdx();
                  break;

               case ConstInterface.ARG_TYPE_SKIP:
                  isNull = true; //#919535 If argument is skipped then pass NULL it will handled in server.
                  break;
            }

            // Create the argument string
            if (isNull)
               htmlArgs.Append(ConstInterface.REQ_ARG_NULL);
            else
            {
               switch (attribute)
               {
                  case StorageAttribute.NUMERIC:
                     num1 = new NUM_TYPE(argValue);
                     num2 = new NUM_TYPE(argValue);
                     num1.round(0);
                     if (NUM_TYPE.num_cmp(num1, num2) == 0)
                     {
                        pic = new PIC("" + UtilStrByteMode.lenB(argValue),
                                      StorageAttribute.ALPHA, compIdx);
                        String numDispVal = num2.to_a(pic).Trim();
                        if (numDispVal.Length <= 9)
                           htmlArgs.Append(ConstInterface.REQ_ARG_NUMERIC + num2.to_a(pic).Trim());
                        else
                           htmlArgs.Append(ConstInterface.REQ_ARG_DOUBLE + num2.to_double());
                     }
                     else
                        htmlArgs.Append(ConstInterface.REQ_ARG_DOUBLE + num2.to_double());
                     break;

                  case StorageAttribute.DATE:
                     pic = new PIC(Manager.GetDefaultDateFormat(), attribute, compIdx);
                     rangeStr = "";
                     htmlArgs.Append(ConstInterface.REQ_ARG_ALPHA);
                     break;

                  case StorageAttribute.TIME:
                     pic = new PIC(Manager.GetDefaultTimeFormat(), attribute, compIdx);
                     rangeStr = "";
                     htmlArgs.Append(ConstInterface.REQ_ARG_ALPHA);
                     break;

                  case StorageAttribute.ALPHA:
                  // alpha strings are kept internally as Unicode, so fall through to unicode case...

                  case StorageAttribute.UNICODE:
                     pic = new PIC("" + UtilStrByteMode.lenB(argValue), attribute, compIdx);
                     rangeStr = "";
                     /* TODO: Kaushal. this should be "-U".
                     * "-U" is currently used for null value.
                     */
                     htmlArgs.Append(ConstInterface.REQ_ARG_UNICODE);
                     break;

                  case StorageAttribute.BLOB:
                     pic = new PIC("", attribute, compIdx);
                     rangeStr = "";

                     char contentType = BlobType.getContentType(argValue);
                     // ANSI blobs are later translated to Unicode
                     if (contentType == BlobType.CONTENT_TYPE_UNICODE || contentType == BlobType.CONTENT_TYPE_ANSI)
                        htmlArgs.Append(ConstInterface.REQ_ARG_UNICODE);
                     else
                        htmlArgs.Append(ConstInterface.REQ_ARG_ALPHA);
                     break;

                  case StorageAttribute.BLOB_VECTOR:
                     pic = new PIC("", attribute, compIdx);
                     rangeStr = "";
                     htmlArgs.Append(ConstInterface.REQ_ARG_ALPHA);
                     //QCR 970794 appending eye catcher for vectors passed as arguments on hyperlink
                     argValue = ConstInterface.MG_HYPER_ARGS + BlobType.removeBlobPrefix(argValue) +
                                ConstInterface.MG_HYPER_ARGS;
                     break;

                  case StorageAttribute.BOOLEAN:
                     pic = new PIC("5", attribute, compIdx);
                     rangeStr = "TRUE,FALSE";
                     htmlArgs.Append(ConstInterface.REQ_ARG_LOGICAL);
                     break;
               }

               if (attribute != StorageAttribute.NUMERIC)
               {
                  string finalValue = StrUtil.rtrim(DisplayConvertor.Instance.mg2disp(argValue, rangeStr, pic, compIdx, false));

                  //QCR 970794 converting the url to a legal format
                  htmlArgs.Append(makePrintable
                                    ? GUIManager.Instance.makeURLPrintable(finalValue)
                                    : finalValue);
               }
            }
         }
      }

      /// <returns> value of the skip flag </returns>
      internal bool skipArg()
      {
         return _skip;
      }

      /// <summary>
      ///   This method fills the argument data from the mainProgVar strings
      /// </summary>
      /// <param name = "mainProgVar">- a vector of strings of main program variables</param>
      /// <param name = "mainProgTask">- the main program task</param>
      internal void fillDataByMainProgVars(String mainProgVar, Task mainProgTask)
      {
         if (mainProgVar.Equals("Skip"))
         {
            _skip = true;
         }
         else
         {
            int fldId = Int32.Parse(mainProgVar) - 1;
            _type = ConstInterface.ARG_TYPE_VALUE;
            _fld = (Field)mainProgTask.getField(fldId);
            _val = _fld.getValue(true);
            _valueIsNull = _fld.isNull();
            _valueAttr = _fld.getType();
         }
      }

      /// <summary>
      ///   fill an argument data from a parameter data
      /// </summary>
      /// <param name = "paramValueAttr"></param>
      /// <param name = "paramValue"></param>
      /// <param name = "paramNull"></param>
      protected internal void fillDataByParams(StorageAttribute paramValueAttr, String paramValue, bool paramNull)
      {
         _valueAttr = paramValueAttr;
         _val = paramValue;
         _valueIsNull = paramNull;

         // The server indicates skip on a parameter by sending DATA_TYPE_SKIP instead of the real attribute.
         if (_valueAttr == StorageAttribute.SKIP)
         {
            _type = ConstInterface.ARG_TYPE_SKIP;
            _skip = true;
         }
         else
            _type = ConstInterface.ARG_TYPE_VALUE;
      }

      /// <summary> returns empty value</summary>
      /// <param name="isBlob">- true is value is for blob</param>
      private static String getEmptyValue(bool isBlob)
      {
         return isBlob
                   ? BlobType.getEmptyBlobPrefix(((char)0)) + ";"
                   : "";
      }

      /// <summary>
      /// fill the argument from a string
      /// </summary>
      /// <param name="argStr"></param>
      internal void FillFromString(string argStr)
      {
         // type is always a value
         _type = ConstInterface.ARG_TYPE_VALUE;
         
         string argType = null;
         // If string is shorter than 3, assume it does not have an attribute identifier
         if(argStr.Length > 2)
            argType = argStr.Substring(0, 2);

         switch(argType)
         {
            case ConstInterface.REQ_ARG_ALPHA:
               _valueAttr = StorageAttribute.ALPHA;
               break;

            case ConstInterface.REQ_ARG_UNICODE:
               _valueAttr = StorageAttribute.UNICODE;
               break;
            
            case ConstInterface.REQ_ARG_NUMERIC:
               _valueAttr = StorageAttribute.NUMERIC;
               break;
            
            case ConstInterface.REQ_ARG_DOUBLE:
               _valueAttr = StorageAttribute.NUMERIC;
               break;
            
            case ConstInterface.REQ_ARG_LOGICAL:
               _valueAttr = StorageAttribute.BOOLEAN;
               break;
            
            case ConstInterface.REQ_ARG_NULL:
               _valueAttr = StorageAttribute.NONE;
               break;
            
            default:
               // if storage type is not defined, assume alpha string and the entire string is the value 
               _valueAttr = StorageAttribute.ALPHA;
               _val = argStr;
               return;
         }

         // the case the string is too short was dealt with earlier - it will get to the default case in the switch 
         // and return from there
         _val = argStr.Substring(2);
      }
   }
}
