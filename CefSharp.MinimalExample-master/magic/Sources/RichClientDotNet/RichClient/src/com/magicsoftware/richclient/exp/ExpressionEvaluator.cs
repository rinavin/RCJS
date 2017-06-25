using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.http;
using com.magicsoftware.httpclient;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.security;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using EventHandler = com.magicsoftware.richclient.events.EventHandler;
using Process = com.magicsoftware.richclient.util.Process;
using RunTimeEvent = com.magicsoftware.richclient.events.RunTimeEvent;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.richclient.tasks;
using Manager = com.magicsoftware.unipaas.Manager;
using Field = com.magicsoftware.richclient.data.Field;
using Record = com.magicsoftware.richclient.data.Record;
using Constants = com.magicsoftware.util.Constants;
using DataView = com.magicsoftware.richclient.data.DataView;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.exp;
using GuiField = com.magicsoftware.unipaas.management.data.Field;
using com.magicsoftware.richclient.gui;
using System.Globalization;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.richclient.communications;
using com.magicsoftware.richclient.tasks.sort;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.cache;

#if !PocketPC
using System.Threading;
using com.magicsoftware.gatewaytypes.data;
#else
using OSEnvironment = com.magicsoftware.richclient.mobile.util.OSEnvironment;
using Monitor = com.magicsoftware.richclient.mobile.util.Monitor;
using ThreadInterruptedException = com.magicsoftware.richclient.mobile.util.ThreadInterruptedException;
#endif

namespace com.magicsoftware.richclient.exp
{
   internal class ExpressionEvaluator : GuiExpressionEvaluator
   {
      private const char ASTERISK_CHR = (char)(1);
      private const char QUESTION_CHR = (char)(2);

      // Some items in polish are saved as short or long.
      // Following constants are used while reading the polished expression.
      private const int PARENT_LEN = 2; // 2 bytes
      private const int SHORT_OBJECT_LEN = 2; // 2 bytes
      private const int LONG_OBJECT_LEN = 4; // 4 bytes

      private static int _recursiveExpCalcCount;
      private readonly char[] _charsToTrim = { ' ', '\0' };
      private readonly ExpressionLocalJpn _expressionLocalJpn;
      private CommandsProcessorBase commandsProcessor { get { return ((Task)ExpTask).CommandsProcessor; } }

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal ExpressionEvaluator()
      {
         _expressionLocalJpn = UtilStrByteMode.isLocaleDefLangJPN()
                              ? new ExpressionLocalJpn(this)
                              : null;
      }

      /// <summary>
      ///   Get a basic value from the expression string
      /// </summary>
      /// <param name = "expStrTracker">the expression processed</param>
      /// <param name = "opCode">the opcode of the expression</param>
      /// <returns> the basic data value as an ExpVal instance</returns>
      private ExpVal getExpBasicValue(ExpStrTracker expStrTracker, int opCode)
      {
         var Val = new ExpVal();
         int len;
         int parent, vee;
         Field field;

         switch (opCode)
         {
            //--------------------------------------------------------------------
            // String Value
            //--------------------------------------------------------------------
            case ExpressionInterface.EXP_OP_A:
            case ExpressionInterface.EXP_OP_H:
               Val.Attr = StorageAttribute.UNICODE;
               len = expStrTracker.get4ByteNumber();
               Val.StrVal = expStrTracker.getString(len, true, true); // first value is unicode
               // since the server sends us both Unicode string Ansi string for 
               // each string in the expression, and the client uses only unicode,
               // we will diregard the Ansi string
               len = expStrTracker.get4ByteNumber();
               expStrTracker.getString(len, true, false); // second value is ansi
               break;

            case ExpressionInterface.EXP_OP_EXT_A:
               Val.Attr = StorageAttribute.ALPHA;
               len = expStrTracker.get4ByteNumber();
               expStrTracker.getString(len, true, true); // first value is unicode
               // since the server sends us both Unicode string Ansi string for 
               // each string in the expression, and the client uses only unicode,
               // we will diregard the Ansi string
               len = expStrTracker.get4ByteNumber();
               Val.StrVal = expStrTracker.getString(len, true, false);
               break;

            //--------------------------------------------------------------------
            // Magic Number value
            //--------------------------------------------------------------------
            case ExpressionInterface.EXP_OP_N:
            case ExpressionInterface.EXP_OP_T:
            case ExpressionInterface.EXP_OP_D:
            case ExpressionInterface.EXP_OP_E:
               if (opCode == ExpressionInterface.EXP_OP_D)
                  Val.Attr = StorageAttribute.DATE;
               else if (opCode == ExpressionInterface.EXP_OP_T)
                  Val.Attr = StorageAttribute.TIME;
               else
                  Val.Attr = StorageAttribute.NUMERIC;
               len = expStrTracker.get2ByteNumber();
               Val.MgNumVal = expStrTracker.getMagicNumber(len, true);
               break;

            //--------------------------------------------------------------------
            // Logical Value
            //--------------------------------------------------------------------
            case ExpressionInterface.EXP_OP_L:
               Val.Attr = StorageAttribute.BOOLEAN;
               Val.BoolVal = (expStrTracker.get2ByteNumber() == 1);
               break;

            //--------------------------------------------------------------------
            // Variable Value
            //--------------------------------------------------------------------
            case ExpressionInterface.EXP_OP_V:
               parent = expStrTracker.getVarIdx();
               vee = expStrTracker.get4ByteNumber();
               field = (Field)((Task)ExpTask).getField(parent, vee - 1);
               Val.Attr = field.getType();
               /* For Vector, null allowed is always true. If the field is a vector, the  */
               /* field.nullAllowed() actually returns the null allowed for the vec cell. */
               bool nullAllowed = (field.getType() == StorageAttribute.BLOB_VECTOR)
                                  ? true
                                  : field.NullAllowed;
               Val.IsNull = field.isNull() && nullAllowed
                             ? true
                             : false;
               Val.OriginalNull = Val.IsNull;
               string fldVal = field.getValue(true);

               if (fldVal != null && Val.IsNull &&
                   field.getTask().getNullArithmetic() == Constants.NULL_ARITH_USE_DEF)
                  Val.IsNull = false;

               switch (Val.Attr)
               {
                  case StorageAttribute.ALPHA:
                  case StorageAttribute.UNICODE:
                     Val.StrVal = fldVal;
                     break;

                  case StorageAttribute.BLOB_VECTOR:
                     Val.VectorField = field;
                     goto case StorageAttribute.BLOB;

                  case StorageAttribute.BLOB:
                     Val.StrVal = fldVal;

                     if (Val.StrVal == null)
                     {
                        Val.IsNull = true;
                        Val.IncludeBlobPrefix = false;
                     }
                     else
                        Val.IncludeBlobPrefix = true;
                     break;

                  case StorageAttribute.NUMERIC:
                  case StorageAttribute.DATE:
                  case StorageAttribute.TIME:
                     Val.MgNumVal = fldVal != null ? new NUM_TYPE(fldVal) : null;
                     break;

                  case StorageAttribute.BOOLEAN:
                     if (fldVal != null)
                        Val.BoolVal = DisplayConvertor.toBoolean(fldVal);
                     break;

                  case StorageAttribute.DOTNET:
                     if (!string.IsNullOrEmpty(fldVal))
                     {
                        int key = BlobType.getKey(fldVal);

                        Val.DnMemberInfo = DNManager.getInstance().CreateDNMemberInfo(key);
                        Val.IsNull = (Val.DnMemberInfo.value == null);
                     }
                     break;
               }
               break;

            case ExpressionInterface.EXP_OP_FORM:
               parent = expStrTracker.getVarIdx();
               int formDisplayIndexInTask = expStrTracker.get4ByteNumber();
               formDisplayIndexInTask = ((Task)ExpTask).GetRealMainDisplayIndexOnDepth(formDisplayIndexInTask);
               ConstructMagicNum(Val, formDisplayIndexInTask, StorageAttribute.NUMERIC);
               break;

            case ExpressionInterface.EXP_OP_VAR:
               parent = expStrTracker.getVarIdx();
               vee = expStrTracker.get4ByteNumber();
               int itm = ((Task)ExpTask).ctl_itm_4_parent_vee(parent, vee);
               ConstructMagicNum(Val, itm, StorageAttribute.NUMERIC);
               break;

            case ExpressionInterface.EXP_OP_RIGHT_LITERAL:
               len = expStrTracker.get2ByteNumber();
               Val.MgNumVal = expStrTracker.getMagicNumber(len, true);
               // Skip extra unused string stored after literal
               len = expStrTracker.get2ByteNumber();
               Val.Attr = StorageAttribute.NUMERIC;
               expStrTracker.getString(len, true, false);
               break;
         }

         return Val;
      }

      /// <summary>
      ///   Does the operator represents a basic data value?
      /// </summary>
      /// <param name = "opCode">  the current operation code</param>
      /// <returns> true if the operator indicates basic data item, false if not</returns>
      internal static bool isBasicItem(int opCode)
      {
         return (opCode <= ExpressionInterface.EXP_OP_L ||
                 opCode == ExpressionInterface.EXP_OP_EXT_A ||
                 opCode == ExpressionInterface.EXP_OP_RIGHT_LITERAL ||
                 opCode == ExpressionInterface.EXP_OP_E ||
                 opCode == ExpressionInterface.EXP_OP_FORM);
      }

      /// <summary>
      ///   Checks if the function has variable argument list
      ///   like in magic : EXP_OP_IS_VARARG(opr)
      /// </summary>
      private static bool isVarArgList(ExpressionDict.ExpDesc expDesc)
      {
         if (expDesc.ArgCount_ < 0 || expDesc.ArgAttr_.Length > expDesc.ArgCount_)
            return true;
         return false;
      }

      /// <summary>
      ///   Execute single operation
      /// </summary>
      /// <param name = "opCode">operation code to execute</param>
      /// <param name = "expStrTracker">the expression being processed</param>
      /// <param name = "valStack">stack of values that are the result of the operation</param>
      /// <param name = "addedOpers">stack of operation to execute as a result of this operator</param>
      /// <param name = "expectedType"></param>
      private void execOperation(int opCode, ExpStrTracker expStrTracker, Stack valStack,
                                 List<DynamicOperation> addedOpers,
                                 StorageAttribute expectedType)
      {
         ExpVal val1;
         ExpVal val2;
         ExpVal val3;
         ExpVal val4;
         ExpVal val5;
         ExpVal val6;
         ExpVal val7;
         ExpVal val8;
         ExpVal val9;
         var resVal = new ExpVal();
         ExpVal[] Exp_params;
         bool addResult = true;
         int nArgs;
         bool specialAnsiExpression = ClientManager.Instance.getEnvironment().getSpecialAnsiExpression();

         // temporary values:
         int ofs, len, LenMax, j = 0;
         PIC pic;

         // this part puts the number of arguments in the TOP of the stack
         // 4 variable argument list functions ONLY
         // don't forget pop this argument in the start of the cases of variable argument list functions
         ExpressionDict.ExpDesc expDesc = ExpressionDict.expDesc[opCode];

         // for function with NOT constant number of arguments ONLY
         // don't use this block for another cases, because the try-catch part insert unusable values to the expression queue
         if (isVarArgList(expDesc))
         {
            nArgs = expStrTracker.get1ByteNumber();

            for (j = 0; j < nArgs; j++)
            {
               try
               {
                  execExpression(expStrTracker, valStack, StorageAttribute.NONE);
               }
               catch (Exception exception)
               {
                  // if the exception is thrown by dotnet, we should immediately throw it
                  if (exception is DNException)
                     throw;
                  //getOutLoop
                  break;
               }
            }
            if (isVarArgList(expDesc) && j == nArgs) // the for_loop finished without exception
            {
               Object temp_object = nArgs;
               valStack.Push(temp_object);
            }
            else
               Logger.Instance.WriteExceptionToLog("ExpressionEvaluator.execOperation() there is problem with arguments of " +
                                             opCode + "(see ExpressionDict for name)");
         }

         switch (opCode)
         {
            case ExpressionInterface.EXP_OP_ADD:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.add(val1.MgNumVal, val2.MgNumVal);
               resVal.Attr = StorageAttribute.NUMERIC;
               resVal.IsNull = (resVal.MgNumVal == null);
               break;

            case ExpressionInterface.EXP_OP_SUB:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.sub(val1.MgNumVal, val2.MgNumVal);
               resVal.Attr = StorageAttribute.NUMERIC;
               resVal.IsNull = (resVal.MgNumVal == null);
               break;

            case ExpressionInterface.EXP_OP_MUL:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.mul(val1.MgNumVal, val2.MgNumVal);
               resVal.Attr = StorageAttribute.NUMERIC;
               resVal.IsNull = (resVal.MgNumVal == null);
               break;

            case ExpressionInterface.EXP_OP_DIV:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.div(val1.MgNumVal, val2.MgNumVal);
               resVal.Attr = StorageAttribute.NUMERIC;
               resVal.IsNull = (resVal.MgNumVal == null);
               break;

            case ExpressionInterface.EXP_OP_MOD:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.mod(val1.MgNumVal, val2.MgNumVal);
               resVal.Attr = StorageAttribute.NUMERIC;
               resVal.IsNull = (resVal.MgNumVal == null);
               break;

            case ExpressionInterface.EXP_OP_NEG:
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.NUMERIC;
               if (val1.MgNumVal == null)
               {
                  SetNULL(resVal, StorageAttribute.NUMERIC);
                  break;
               }

               val1.MgNumVal.num_neg();
               resVal.MgNumVal = new NUM_TYPE(val1.MgNumVal);
               break;

            case ExpressionInterface.EXP_OP_FIX:
               int whole, dec;
               val3 = (ExpVal)valStack.Pop(); // dec
               val2 = (ExpVal)valStack.Pop(); // whole
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.NUMERIC;
               if (val1.MgNumVal == null || val2.MgNumVal == null || val3.MgNumVal == null)
               {
                  SetNULL(resVal, StorageAttribute.NUMERIC);
                  break;
               }

               resVal.MgNumVal = new NUM_TYPE(val1.MgNumVal);
               whole = val2.MgNumVal.NUM_2_LONG();
               resVal.MgNumVal.num_fix(whole);
               dec = val3.MgNumVal.NUM_2_LONG();
               resVal.MgNumVal.num_trunc(dec);
               break;

            case ExpressionInterface.EXP_OP_ROUND:
               // int whole, dec;         needn't this variables, them defined in EXP_OP_FIX
               val3 = (ExpVal)valStack.Pop(); // dec
               val2 = (ExpVal)valStack.Pop(); // whole
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.NUMERIC;
               if (val1.MgNumVal == null || val2.MgNumVal == null || val3.MgNumVal == null)
               {
                  SetNULL(resVal, StorageAttribute.NUMERIC);
                  break;
               }

               resVal.MgNumVal = new NUM_TYPE(val1.MgNumVal);
               whole = val2.MgNumVal.NUM_2_LONG();
               resVal.MgNumVal.num_fix(whole);
               dec = val3.MgNumVal.NUM_2_LONG();
               resVal.MgNumVal.round(dec);

               break;

            case ExpressionInterface.EXP_OP_EQ:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.BOOLEAN;
               try
               {
                  resVal.BoolVal = (val_cmp_any(val1, val2, false) == 0);
               }
               catch (NullValueException)
               {
                  resVal.BoolVal = false;
               }
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_NE:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.BOOLEAN;
               try
               {
                  resVal.BoolVal = (val_cmp_any(val1, val2, false) != 0);
               }
               catch (NullValueException)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
               }
               break;

            case ExpressionInterface.EXP_OP_LE:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.BOOLEAN;
               try
               {
                  resVal.BoolVal = (val_cmp_any(val1, val2, true) <= 0);
               }
               catch (NullValueException)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
               }
               break;

            case ExpressionInterface.EXP_OP_LT:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.BOOLEAN;
               try
               {
                  resVal.BoolVal = (val_cmp_any(val1, val2, true) < 0);
               }
               catch (NullValueException)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
               }

               break;

            case ExpressionInterface.EXP_OP_GE:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.BOOLEAN;
               try
               {
                  resVal.BoolVal = (val_cmp_any(val1, val2, true) >= 0);
               }
               catch (NullValueException)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
               }

               break;

            case ExpressionInterface.EXP_OP_GT:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.BOOLEAN;
               try
               {
                  resVal.BoolVal = (val_cmp_any(val1, val2, true) > 0);
               }
               catch (NullValueException)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
               }
               break;

            case ExpressionInterface.EXP_OP_NOT:
               resVal = (ExpVal)valStack.Pop();
               resVal.BoolVal = !resVal.BoolVal;
               break;

            case ExpressionInterface.EXP_OP_OR:
               resVal = (ExpVal)valStack.Pop();
               var dynOper = new DynamicOperation { argCount_ = 1 };
               if (resVal.BoolVal)
               {
                  dynOper.opCode_ = ExpressionInterface.EXP_OP_IGNORE;
                  dynOper.argCount_ = 1;
                  addedOpers.Add(dynOper);
               }
               else
               {
                  dynOper.opCode_ = ExpressionInterface.EXP_OP_EVALX;
                  dynOper.argCount_ = 0;
                  addedOpers.Add(dynOper);
                  addResult = false;
               }
               break;

            case ExpressionInterface.EXP_OP_AND:
               resVal = (ExpVal)valStack.Pop();
               dynOper = new DynamicOperation();
               if (!resVal.BoolVal)
               {
                  dynOper.opCode_ = ExpressionInterface.EXP_OP_IGNORE;
                  dynOper.argCount_ = 1;
                  addedOpers.Add(dynOper);
               }
               else
               {
                  dynOper.opCode_ = ExpressionInterface.EXP_OP_EVALX;
                  dynOper.argCount_ = 0;
                  addedOpers.Add(dynOper);
                  addResult = false;
               }
               break;

            case ExpressionInterface.EXP_OP_IF:
               val1 = (ExpVal)valStack.Pop();
               if (val1.BoolVal)
               {
                  dynOper = new DynamicOperation { opCode_ = ExpressionInterface.EXP_OP_EVALX, argCount_ = 0 };
                  addedOpers.Add(dynOper);
                  dynOper = new DynamicOperation { opCode_ = ExpressionInterface.EXP_OP_IGNORE, argCount_ = 1 };
                  addedOpers.Add(dynOper);
               }
               else
               {
                  dynOper = new DynamicOperation { opCode_ = ExpressionInterface.EXP_OP_IGNORE, argCount_ = 1 };
                  addedOpers.Add(dynOper);
                  dynOper = new DynamicOperation { opCode_ = ExpressionInterface.EXP_OP_EVALX, argCount_ = 0 };
                  addedOpers.Add(dynOper);
               }
               addResult = false;
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_LEN:
               val1 = (ExpVal)valStack.Pop();

               resVal.Attr = StorageAttribute.NUMERIC;
               if (val1.StrVal == null)
               {
                  SetNULL(resVal, StorageAttribute.NUMERIC);
                  break;
               }
               resVal.MgNumVal = new NUM_TYPE();
               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression || val1.Attr != StorageAttribute.UNICODE)
                  resVal.MgNumVal.NUM_4_LONG(UtilStrByteMode.lenB(val1.StrVal));
               else
                  resVal.MgNumVal.NUM_4_LONG(val1.StrVal.Length);

               break;

            case ExpressionInterface.EXP_OP_CON:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.UNICODE;
               if (val1.Attr == StorageAttribute.UNICODE && val1.StrVal == null ||
                   val2.Attr == StorageAttribute.UNICODE && val2.StrVal == null)
                  SetNULL(resVal, StorageAttribute.UNICODE);
               else
                  resVal.StrVal = (val1.StrVal ?? "") + (val2.StrVal ?? "");
               break;

            case ExpressionInterface.EXP_OP_MID:
               val3 = (ExpVal)valStack.Pop(); // length
               val2 = (ExpVal)valStack.Pop(); // start, ofset
               val1 = (ExpVal)valStack.Pop(); // string

               resVal.Attr = val1.Attr;
               if (val2.MgNumVal == null || val3.MgNumVal == null || val1.StrVal == null)
               {
                  SetNULL(resVal, resVal.Attr);
                  break;
               }

               /* Compute offset and length of substring */
               ofs = val2.MgNumVal.NUM_2_LONG();
               ofs = (ofs > 1
                      ? ofs - 1
                      : 0); // in MID magic function start=0 && start=1 the same
               len = val3.MgNumVal.NUM_2_LONG();

               if (specialAnsiExpression || val1.Attr != StorageAttribute.UNICODE)
               {
                  resVal.Attr = StorageAttribute.ALPHA;
                  resVal.StrVal = UtilStrByteMode.midB(val1.StrVal, ofs, len);
               }
               else
               {
                  LenMax = val1.StrVal.Length - ofs;
                  if (LenMax < len)
                     len = LenMax;
                  if (len < 0)
                     len = 0;
                  try
                  {
                     resVal.StrVal = val1.StrVal.Substring(ofs, len);
                  }
                  catch (ArgumentOutOfRangeException)
                  {
                     if (ofs > val1.StrVal.Length)
                        resVal.StrVal = "";
                     else if (ofs + len > val1.StrVal.Length)
                        resVal.StrVal = val1.StrVal.Substring(ofs);
                  }
               }

               if (resVal.StrVal == null)
                  resVal.StrVal = "";

               break;

            case ExpressionInterface.EXP_OP_LEFT:
               val2 = (ExpVal)valStack.Pop(); // length
               val1 = (ExpVal)valStack.Pop(); // string

               resVal.Attr = val1.Attr;
               if (val2.MgNumVal == null || val1.StrVal == null)
               {
                  SetNULL(resVal, val1.Attr);
                  break;
               }
               len = val2.MgNumVal.NUM_2_LONG();

               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression || val1.Attr != StorageAttribute.UNICODE)
               {
                  resVal.Attr = StorageAttribute.ALPHA;
                  resVal.StrVal = UtilStrByteMode.leftB(val1.StrVal, len);
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               else
               {
                  if (len > val1.StrVal.Length)
                     len = val1.StrVal.Length;
                  if (len < 0)
                     len = 0;
                  try
                  {
                     resVal.StrVal = val1.StrVal.Substring(0, len);
                  }
                  catch (ArgumentOutOfRangeException)
                  {
                     resVal.StrVal = "";
                  }
               }
               break;

            case ExpressionInterface.EXP_OP_RIGHT:
               val2 = (ExpVal)valStack.Pop(); // length
               val1 = (ExpVal)valStack.Pop(); // string

               resVal.Attr = val1.Attr;
               if (val2.MgNumVal == null || val1.StrVal == null)
               {
                  SetNULL(resVal, val1.Attr);
                  break;
               }
               len = val2.MgNumVal.NUM_2_LONG();

               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression || val1.Attr != StorageAttribute.UNICODE)
               {
                  resVal.Attr = StorageAttribute.ALPHA;
                  resVal.StrVal = UtilStrByteMode.rightB(val1.StrVal, len);
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               else
               {
                  if (len > val1.StrVal.Length)
                     len = val1.StrVal.Length;
                  if (len < 0)
                     len = 0;
                  ofs = val1.StrVal.Length - len;
                  try
                  {
                     // memcpy (EXP_parms[0].ads, EXP_parms[0].ads + ofs, len);
                     resVal.StrVal = val1.StrVal.Substring(ofs);
                  }
                  catch (ArgumentOutOfRangeException)
                  {
                     resVal.StrVal = "";
                  }
               }
               break;

            case ExpressionInterface.EXP_OP_FILL:
               val2 = (ExpVal)valStack.Pop(); // times
               val1 = (ExpVal)valStack.Pop(); // string

               resVal.Attr = StorageAttribute.UNICODE;
               if (val2.MgNumVal == null || val1.StrVal == null)
               {
                  SetNULL(resVal, StorageAttribute.UNICODE);
                  break;
               }

               len = val1.StrVal.Length;
               j = val2.MgNumVal.NUM_2_LONG();
               if (j < 0)
                  j = 0;
               LenMax = len * j;
               if (LenMax > 0x7FFF)
                  // there is MAX lenght in Magic (actually it needn't in Java, but ...)
                  LenMax = (0x7FFF / len) * len;
               if (LenMax > 0)
               {
                  if (len <= 0)
                     resVal.StrVal = "";
                  else if (LenMax == 1)
                     resVal.StrVal = val1.StrVal;
                  else
                  {
                     var tmpBuffer = new StringBuilder(LenMax);
                     for (; LenMax > 0; LenMax -= len)
                        tmpBuffer.Append(val1.StrVal);
                     resVal.StrVal = tmpBuffer.ToString();
                  }
               }
               if (resVal.StrVal == null)
                  resVal.StrVal = "";
               break;

            case ExpressionInterface.EXP_OP_INSTR:
               val2 = (ExpVal)valStack.Pop(); // subStr
               val1 = (ExpVal)valStack.Pop(); // string

               ofs = 0;
               resVal.Attr = StorageAttribute.NUMERIC;
               if (val1.StrVal == null || val2.StrVal == null)
               {
                  SetNULL(resVal, StorageAttribute.NUMERIC);
                  break;
               }

               resVal.MgNumVal = new NUM_TYPE();
               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression ||
                   !(val1.Attr == StorageAttribute.UNICODE ||
                     val2.Attr == StorageAttribute.UNICODE))
               {
                  ofs = UtilStrByteMode.instrB(val1.StrVal, val2.StrVal);
               }
               else
               {
                  if (val2.StrVal.Length == 0)
                  // nothing 2 look for
                  {
                     resVal.MgNumVal.NUM_4_LONG(ofs);
                     break;
                  }
                  ofs = val1.StrVal.IndexOf(val2.StrVal);
                  if (ofs < 0)
                     // string in magic starts from 1, in java from 0.
                     ofs = 0;
                  else
                     ofs++;
               }

               resVal.MgNumVal.NUM_4_LONG(ofs);
               break;

            case ExpressionInterface.EXP_OP_TRIM:
            case ExpressionInterface.EXP_OP_LTRIM:
            case ExpressionInterface.EXP_OP_RTRIM:
               val1 = (ExpVal)valStack.Pop(); // string
               resVal.Attr = val1.Attr == StorageAttribute.ALPHA
                              ? StorageAttribute.ALPHA
                              : StorageAttribute.UNICODE;
               if (val1.StrVal == null)
               {
                  SetNULL(resVal, resVal.Attr);
                  break;
               }

               switch (opCode)
               {
                  case ExpressionInterface.EXP_OP_TRIM:
                     val1.StrVal = trimStr(val1.StrVal, 'B');
                     break;

                  case ExpressionInterface.EXP_OP_LTRIM:
                     val1.StrVal = trimStr(val1.StrVal, 'L');
                     break;

                  case ExpressionInterface.EXP_OP_RTRIM:
                     val1.StrVal = trimStr(val1.StrVal, 'R');
                     break;
               }
               resVal.StrVal = val1.StrVal;
               break;

            case ExpressionInterface.EXP_OP_STR:
               val2 = (ExpVal)valStack.Pop(); // picture format
               val1 = (ExpVal)valStack.Pop(); // Num invert to string
               resVal.Attr = StorageAttribute.UNICODE;
               if (val2.StrVal == null || val1.MgNumVal == null)
               {
                  SetNULL(resVal, StorageAttribute.UNICODE);
                  break;
               }

               // Max length of the picture is 100 characters, like in Magic
               pic = new PIC(set_a_pic(val2.StrVal), StorageAttribute.NUMERIC, ((Task)ExpTask).getCompIdx());
               resVal.StrVal = val1.MgNumVal.to_a(pic);
               break;

            case ExpressionInterface.EXP_OP_VAL:
               val2 = (ExpVal)valStack.Pop(); // picture format
               val1 = (ExpVal)valStack.Pop(); // string invert to Num
               resVal.Attr = StorageAttribute.NUMERIC;
               if (val2.StrVal == null || val1.StrVal == null)
               {
                  SetNULL(resVal, StorageAttribute.NUMERIC);
                  break;
               }
               pic = new PIC(set_a_pic(val2.StrVal), StorageAttribute.NUMERIC, ((Task)ExpTask).getCompIdx());
               resVal.MgNumVal = new NUM_TYPE(val1.StrVal, pic, ((Task)ExpTask).getCompIdx());
               break;

            case ExpressionInterface.EXP_OP_M:
               len = expStrTracker.get2ByteNumber();
               String codes = expStrTracker.getString(len, true, false);
               eval_op_m(resVal, codes);
               break;

            case ExpressionInterface.EXP_OP_K:
               resVal.Attr = StorageAttribute.NUMERIC;
               len = expStrTracker.get2ByteNumber();
               resVal.MgNumVal = expStrTracker.getMagicNumber(len, true);
               break;

            case ExpressionInterface.EXP_OP_F:
            case ExpressionInterface.EXP_OP_P:
               resVal.Attr = StorageAttribute.NUMERIC;
               len = expStrTracker.get2ByteNumber();
               resVal.MgNumVal = expStrTracker.getMagicNumber(len, true);
               //skip second number (the component idx)
               len = expStrTracker.get2ByteNumber();
               expStrTracker.getMagicNumber(len, true);
               break;

            case ExpressionInterface.EXP_OP_STAT:
               val2 = (ExpVal)valStack.Pop(); // Modes
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_stat(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_SUBFORM_EXEC_MODE:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_subformExecMode(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_USER:
               val1 = (ExpVal)valStack.Pop(); // number
               eval_op_user(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_SYS:
               eval_op_appname(resVal);
               break;

            case ExpressionInterface.EXP_OP_PROG:
               eval_op_prog(resVal);
               break;

            case ExpressionInterface.EXP_OP_LEVEL:
               val1 = (ExpVal)valStack.Pop();
               eval_op_level(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_COUNTER:
               val1 = (ExpVal)valStack.Pop();
               eval_op_counter(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_EMPTY_DATA_VIEW:
               val1 = (ExpVal)valStack.Pop();
               eval_op_emptyDataview(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_MAINLEVEL:
               val1 = (ExpVal)valStack.Pop();
               eval_op_mainlevel(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_MAINDISPLAY:
               val1 = (ExpVal)valStack.Pop();
               eval_op_maindisplay(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_ISFIRSTRECORDCYCLE:
               val1 = (ExpVal)valStack.Pop();
               eval_op_IsFirstRecordCycle(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_DATE:
            case ExpressionInterface.EXP_OP_UTCDATE:
               resVal.MgNumVal = new NUM_TYPE();
               resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.date_magic(opCode == ExpressionInterface.EXP_OP_UTCDATE));
               resVal.Attr = StorageAttribute.DATE; // DATE
               break;

            case ExpressionInterface.EXP_OP_ADDDT:
               val8 = (ExpVal)valStack.Pop();
               val7 = (ExpVal)valStack.Pop();
               val6 = (ExpVal)valStack.Pop(); // hours
               val5 = (ExpVal)valStack.Pop(); // days
               val4 = (ExpVal)valStack.Pop(); // months
               val3 = (ExpVal)valStack.Pop(); // years
               val2 = (ExpVal)valStack.Pop(); // Time
               val1 = (ExpVal)valStack.Pop(); // Date 
               eval_op_addDateTime(resVal, val1, val2, val3, val4, val5, val6, val7, val8);
               break;

            case ExpressionInterface.EXP_OP_DIFDT:
               val6 = (ExpVal)valStack.Pop(); // time diff
               val5 = (ExpVal)valStack.Pop(); // date diff
               val4 = (ExpVal)valStack.Pop(); // time 2
               val3 = (ExpVal)valStack.Pop(); // date 2
               val2 = (ExpVal)valStack.Pop(); // Time 1
               val1 = (ExpVal)valStack.Pop(); // Date 1
               eval_op_difdt(resVal, val1, val2, val3, val4, val5, val6);
               break;

            case ExpressionInterface.EXP_OP_VARPREV:
               val1 = (ExpVal)valStack.Pop();
               // exp_itm_2_vee ();
               exp_get_var(resVal, val1, true);
               break;

            case ExpressionInterface.EXP_OP_VARCURR:
               val1 = (ExpVal)valStack.Pop();
               exp_get_var(resVal, val1, false);
               break;

            case ExpressionInterface.EXP_OP_VARMOD:
               val1 = (ExpVal)valStack.Pop();
               eval_op_varmod(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_VARINP:
               val1 = (ExpVal)valStack.Pop();
               eval_op_varinp(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_VARNAME:
               val1 = (ExpVal)valStack.Pop();
               eval_op_varname(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_VARDISPLAYNAME:
               val1 = (ExpVal)valStack.Pop();
               eval_op_VarDisplayName(resVal, val1);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_VARCONTROLID:
               val1 = (ExpVal)valStack.Pop();
               eval_op_VarControlID(resVal, val1);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_CONTROLITEMSLIST:
               val1 = (ExpVal)valStack.Pop();
               eval_op_ControlItemsList(resVal, val1);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_CONTROLDISPLAYLIST:
               val1 = (ExpVal)valStack.Pop();
               eval_op_ControlDisplayList(resVal, val1);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_VIEWMOD:
               val1 = (ExpVal)valStack.Pop();
               eval_op_viewmod(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_TIME:
            case ExpressionInterface.EXP_OP_UTCTIME:
               resVal.MgNumVal = new NUM_TYPE();
               resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.time_magic(opCode == ExpressionInterface.EXP_OP_UTCTIME));
               resVal.Attr = StorageAttribute.TIME; // TIME
               break;

            case ExpressionInterface.EXP_OP_MTIME:
            case ExpressionInterface.EXP_OP_UTCMTIME:
               resVal.MgNumVal = new NUM_TYPE();
               resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.mtime_magic(opCode == ExpressionInterface.EXP_OP_UTCMTIME));
               resVal.Attr = StorageAttribute.TIME; // TIME
               break;

            case ExpressionInterface.EXP_OP_PWR:
               val2 = (ExpVal)valStack.Pop(); // power
               val1 = (ExpVal)valStack.Pop(); // number
               resVal.MgNumVal = new NUM_TYPE();
               resVal.MgNumVal = NUM_TYPE.eval_op_pwr(val1.MgNumVal, val2.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_LOG:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_log(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_EXP:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_exp(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_ABS:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_abs(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_SIN:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_sin(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_COS:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_cos(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;

               break;

            case ExpressionInterface.EXP_OP_TAN:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_tan(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_ASIN:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_asin(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_ACOS:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_acos(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_ATAN:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_atan(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_RAND:
               val1 = (ExpVal)valStack.Pop();
               resVal.MgNumVal = NUM_TYPE.eval_op_rand(val1.MgNumVal);
               resVal.IsNull = (resVal.MgNumVal == null);
               resVal.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_MIN:
            case ExpressionInterface.EXP_OP_MAX:
               nArgs = ((Int32)valStack.Pop());
               val_cpy((ExpVal)valStack.Pop(), resVal);
               try
               {
                  for (j = 1; j < nArgs; j++)
                  {
                     val1 = (ExpVal)valStack.Pop();
                     if (opCode == ExpressionInterface.EXP_OP_MIN)
                     {
                        if (val_cmp_any(val1, resVal, true) < 0)
                           val_cpy(val1, resVal);
                     }
                     //EXP_OP_MAX
                     else
                     {
                        if (val_cmp_any(val1, resVal, true) > 0)
                           val_cpy(val1, resVal);
                     }
                  }
               }
               catch (NullValueException oneOfValuesIsNull)
               {
                  for (; valStack.Count > 0 && j < nArgs; j++)
                     //clean queue
                     valStack.Pop();
                  resVal.IsNull = true;
                  resVal.Attr = oneOfValuesIsNull.getAttr();
               }
               break;

            case ExpressionInterface.EXP_OP_RANGE:
               val3 = (ExpVal)valStack.Pop(); // upper: A value that represents the upper limit of the range.
               val2 = (ExpVal)valStack.Pop(); // lower: A value that represents the lower limit of the range.
               val1 = (ExpVal)valStack.Pop(); // value: The value checked.
               eval_op_range(val1, val2, val3, resVal);
               break;

            case ExpressionInterface.EXP_OP_REP:
               val4 = (ExpVal)valStack.Pop();
               // len_origin: The number of characters that will be moved from origin to target, starting from the leftmost character of origin.
               val3 = (ExpVal)valStack.Pop();
               // pos_target: The first position in the target string that will receive the substring from origin.
               val2 = (ExpVal)valStack.Pop();
               // origin: The alpha string or expression that provides the substring to be copied to target.
               val1 = (ExpVal)valStack.Pop();
               // target: The target alpha string or expression where the replacement will take place.

               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression ||
                   !(val1.Attr == StorageAttribute.UNICODE ||
                     val2.Attr == StorageAttribute.UNICODE))
               {
                  resVal.Attr = StorageAttribute.ALPHA;
                  resVal.StrVal = UtilStrByteMode.repB(val1.StrVal, val2.StrVal, val3.MgNumVal.NUM_2_LONG(),
                                                        val4.MgNumVal.NUM_2_LONG());
               }
               else
               {
                  eval_op_rep_1(resVal, val1, val2, val3, val4);
               }

               break;

            case ExpressionInterface.EXP_OP_INS:
               val4 = (ExpVal)valStack.Pop();
               // length: A number that represents the number of characters from source that will be inserted into target.
               val3 = (ExpVal)valStack.Pop(); // position: A number that represents the starting position in target.
               val2 = (ExpVal)valStack.Pop(); // source: An alpha string that represents the source string.
               val1 = (ExpVal)valStack.Pop(); // target: An alpha string that represents the target string

               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression ||
                   !(val1.Attr == StorageAttribute.UNICODE ||
                     val2.Attr == StorageAttribute.UNICODE))
               {
                  resVal.Attr = StorageAttribute.ALPHA;
                  resVal.StrVal = UtilStrByteMode.insB(val1.StrVal, val2.StrVal, val3.MgNumVal.NUM_2_LONG(),
                                                        val4.MgNumVal.NUM_2_LONG());
               }
               else
               {
                  eval_op_ins(resVal, val1, val2, val3, val4);
               }

               break;

            case ExpressionInterface.EXP_OP_DEL:
               val3 = (ExpVal)valStack.Pop();
               // length: The number of characters to be deleted, beginning with position start and proceeding rightward.
               val2 = (ExpVal)valStack.Pop(); // start : The position of the first character to be deleted.
               val1 = (ExpVal)valStack.Pop(); // string: An alpha string or an alpha string expression.

               // count the number of bytes, not characters (JPN: DBCS support)
               if (specialAnsiExpression || val1.Attr != StorageAttribute.UNICODE)
               {
                  resVal.Attr = StorageAttribute.ALPHA;
                  resVal.StrVal = UtilStrByteMode.delB(val1.StrVal, val2.MgNumVal.NUM_2_LONG(),
                                                        val3.MgNumVal.NUM_2_LONG());
               }
               else
               {
                  eval_op_del(resVal, val1, val2, val3);
               }

               break;

            case ExpressionInterface.EXP_OP_FLIP:
               val1 = (ExpVal)valStack.Pop();
               eval_op_flip(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_UPPER:
               val1 = (ExpVal)valStack.Pop();
               eval_op_upper(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_LOWER:
               val1 = (ExpVal)valStack.Pop();
               eval_op_lower(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CRC:
               val2 = (ExpVal)valStack.Pop();
               // numeric: A number that represents the CRC algorithm. In this version of Magic use 0 to apply CRC-16.
               val1 = (ExpVal)valStack.Pop(); // string : An alpha string to which the CRC is applied.
               eval_op_crc(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_CHKDGT:
               val2 = (ExpVal)valStack.Pop(); // 0 or 1 (number) for Modulus 10 or Modulus 11, respectively.
               val1 = (ExpVal)valStack.Pop();
               // An alpha string that represents the number for which the check digit will be calculated
               eval_op_chkdgt(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_SOUNDX:
               val1 = (ExpVal)valStack.Pop();
               eval_op_soundx(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_HSTR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_hstr(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_HVAL:
               val1 = (ExpVal)valStack.Pop();
               eval_op_hval(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CHR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_chr(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_ASC:
               val1 = (ExpVal)valStack.Pop();
               eval_op_asc(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_MSTR:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_mstr(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_MVAL:
               val1 = (ExpVal)valStack.Pop();
               eval_op_mval(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_DSTR:
               val2 = (ExpVal)valStack.Pop(); // picture string
               val1 = (ExpVal)valStack.Pop(); // date string it's LOGICAL ??????
               eval_op_dstr(resVal, val1, val2, DisplayConvertor.Instance);
               break;

            case ExpressionInterface.EXP_OP_DVAL:
               val2 = (ExpVal)valStack.Pop(); // picture  format of date string output
               val1 = (ExpVal)valStack.Pop(); // date string
               eval_op_dval(resVal, val1, val2, DisplayConvertor.Instance);
               break;

            case ExpressionInterface.EXP_OP_TSTR:
               val2 = (ExpVal)valStack.Pop(); // picture format of time string output
               val1 = (ExpVal)valStack.Pop(); // time string
               eval_op_tstr(resVal, val1, val2, DisplayConvertor.Instance, false);
               break;

            case ExpressionInterface.EXP_OP_MTSTR:
               val2 = (ExpVal)valStack.Pop(); // picture format of time string output
               val1 = (ExpVal)valStack.Pop(); // time string
               eval_op_tstr(resVal, val1, val2, DisplayConvertor.Instance, true);
               break;

            case ExpressionInterface.EXP_OP_TVAL:
               val2 = (ExpVal)valStack.Pop(); // picture string
               val1 = (ExpVal)valStack.Pop(); // time string
               eval_op_tval(resVal, val1, val2, DisplayConvertor.Instance, false);
               break;

            case ExpressionInterface.EXP_OP_MTVAL:
               val2 = (ExpVal)valStack.Pop(); // picture string
               val1 = (ExpVal)valStack.Pop(); // time string
               eval_op_tval(resVal, val1, val2, DisplayConvertor.Instance, true);
               break;

            case ExpressionInterface.EXP_OP_DAY:
               val1 = (ExpVal)valStack.Pop();
               eval_op_day(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_MONTH:
               val1 = (ExpVal)valStack.Pop();
               eval_op_month(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_YEAR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_year(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_DOW:
               val1 = (ExpVal)valStack.Pop();
               eval_op_dow(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CDOW:
               val1 = (ExpVal)valStack.Pop();
               eval_op_cdow(resVal, val1.MgNumVal, DisplayConvertor.Instance);
               break;

            case ExpressionInterface.EXP_OP_CMONTH:
               val1 = (ExpVal)valStack.Pop();
               eval_op_cmonth(resVal, val1.MgNumVal, DisplayConvertor.Instance);
               break;

            case ExpressionInterface.EXP_OP_NDOW:
               val1 = (ExpVal)valStack.Pop(); // number or day of week
               eval_op_ndow(resVal, val1, DisplayConvertor.Instance);
               break;

            case ExpressionInterface.EXP_OP_NMONTH:
               val1 = (ExpVal)valStack.Pop(); // number or month of year
               eval_op_nmonth(resVal, val1, DisplayConvertor.Instance);
               break;

            case ExpressionInterface.EXP_OP_SECOND:
               val1 = (ExpVal)valStack.Pop();
               eval_op_second(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_MINUTE:
               val1 = (ExpVal)valStack.Pop();
               eval_op_minute(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_HOUR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_hour(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_DELAY:
               val1 = (ExpVal)valStack.Pop();
               // this function not wark, not checked
               eval_op_delay(val1);
               resVal.Attr = StorageAttribute.BOOLEAN;
               resVal.BoolVal = true;
               break;

            case ExpressionInterface.EXP_OP_IDLE:
               eval_op_idle(resVal);
               break;

            case ExpressionInterface.EXP_OP_FLOW:
               val1 = (ExpVal)valStack.Pop();
               if (val1.StrVal == null)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
                  break;
               }
               resVal.Attr = StorageAttribute.BOOLEAN;
               resVal.BoolVal = ((Task)ExpTask).checkFlowMode(val1.StrVal);
               break;

            case ExpressionInterface.EXP_OP_ADDDATE:
               val4 = (ExpVal)valStack.Pop(); // days: The number of days to add to date. May be zero.
               val3 = (ExpVal)valStack.Pop(); // months: The number of months to add to date. May be zero.
               val2 = (ExpVal)valStack.Pop(); // years : The number of years to add to date. May be zero.
               val1 = (ExpVal)valStack.Pop(); // date : A date.
               eval_op_adddate(resVal, val1, val2, val3, val4);
               break;

            case ExpressionInterface.EXP_OP_ADDTIME:
               val4 = (ExpVal)valStack.Pop(); // seconds: The number of seconds to add to time.
               val3 = (ExpVal)valStack.Pop(); // minutes: The number of minutes to add to time.
               val2 = (ExpVal)valStack.Pop(); // hours : The number of hours to add to time
               val1 = (ExpVal)valStack.Pop(); // time : A time value.
               eval_op_addtime(resVal, val1, val2, val3, val4);
               break;

            case ExpressionInterface.EXP_OP_OWNER:
               resVal.Attr = StorageAttribute.ALPHA;
               resVal.StrVal = ClientManager.Instance.getEnvironment().getOwner();
               break;

            case ExpressionInterface.EXP_OP_VARATTR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_varattr(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_VARPIC:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               //to add this method
               eval_op_varpic(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_DBROUND:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               //to add this method
               eval_op_dbround(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_NULL:
            case ExpressionInterface.EXP_OP_NULL_A:
            case ExpressionInterface.EXP_OP_NULL_N:
            case ExpressionInterface.EXP_OP_NULL_B:
            case ExpressionInterface.EXP_OP_NULL_D:
            case ExpressionInterface.EXP_OP_NULL_T:
            case ExpressionInterface.EXP_OP_NULL_U:
            case ExpressionInterface.EXP_OP_NULL_O:
               exp_null_val_get(expectedType, opCode, resVal);
               break;

            case ExpressionInterface.EXP_OP_ISNULL:
               val1 = (ExpVal)valStack.Pop();
               resVal.BoolVal = val1.IsNull;

               // identify a situation where a value was un-nullified due to "null arithmetic"
               // and still, make ISNULL() return TRUE although the value is flagged as non-null
               if (!val1.IsNull && val1.OriginalNull)
                  resVal.BoolVal = val1.OriginalNull;

               resVal.Attr = StorageAttribute.BOOLEAN;

               // null values accepted into the ISNULL() function do not nullify the
               // Whole expression, thus UNLESS SOMETHING ELSE nullified the expression,
               // we mark it as NON-NULL. We check if something else nullified the expression
               // by scanning the values currently on the parameters stack.
               if (expStrTracker.isNull())
               {
                  List<ExpVal> myArray = new List<ExpVal>();
                  bool prevNull = false;
                  int i;
                  while (!(valStack.Count == 0) && !prevNull)
                  {
                     myArray.Add((ExpVal)valStack.Pop());
                     if (myArray[myArray.Count - 1].IsNull)
                        prevNull = true;
                  }

                  for (i = myArray.Count - 1; i >= 0; i--)
                     valStack.Push(myArray[i]);

                  if (!prevNull)
                     expStrTracker.resetNullResult();
               }

               break;

            case ExpressionInterface.EXP_OP_BOM:
               val1 = (ExpVal)valStack.Pop();
               eval_op_bom(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_BOY:
               val1 = (ExpVal)valStack.Pop();
               eval_op_boy(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_EOM:
               val1 = (ExpVal)valStack.Pop();
               eval_op_eom(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_EOY:
               val1 = (ExpVal)valStack.Pop();
               eval_op_eoy(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_ROLLBACK:
               val2 = (ExpVal)valStack.Pop(); // show message 
               val1 = (ExpVal)valStack.Pop(); // generation (?)
               eval_op_rollback(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_VARSET:
               val2 = (ExpVal)valStack.Pop(); //value
               val1 = (ExpVal)valStack.Pop(); //number
               eval_op_varset(resVal, val2, val1);
               break;

            case ExpressionInterface.EXP_OP_CLICKWX:
            case ExpressionInterface.EXP_OP_CLICKWY:
            case ExpressionInterface.EXP_OP_CLICKCX:
            case ExpressionInterface.EXP_OP_CLICKCY:
               // case EXP_OP_HIT_ZORDER:
               eval_op_lastclick(resVal, opCode);
               break;

            case ExpressionInterface.EXP_OP_CTRL_NAME:
               eval_op_ctrl_name(resVal);
               break;

            case ExpressionInterface.EXP_OP_WIN_BOX:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_win_box(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_TDEPTH:
               //use the context task of the current task of expression
               Task currTsk = (Task)ExpTask.GetContextTask();
               len = currTsk.getTaskDepth(false) - 1; //start from First Task and not from main Task
               ConstructMagicNum(resVal, len, StorageAttribute.NUMERIC);
               break;

            case ExpressionInterface.EXP_OP_MINMAGIC:
            case ExpressionInterface.EXP_OP_MAXMAGIC:
            case ExpressionInterface.EXP_OP_RESMAGIC:
               resVal.BoolVal = SetWindowState(opCode);
               resVal.Attr = StorageAttribute.BOOLEAN;
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILEOPEN_DLG:
               val5 = (ExpVal)valStack.Pop(); //Multi select
               val4 = (ExpVal)valStack.Pop(); //Check exists
               val3 = (ExpVal)valStack.Pop(); //Filter
               val2 = (ExpVal)valStack.Pop(); //Dir
               val1 = (ExpVal)valStack.Pop(); //caption
               eval_op_fileopen_dlg(resVal, val1, val2, val3, val4, val5);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILESAVE_DLG:
               val5 = (ExpVal)valStack.Pop(); //Overwrite prompt
               val4 = (ExpVal)valStack.Pop(); //Default extension
               val3 = (ExpVal)valStack.Pop(); //Filter
               val2 = (ExpVal)valStack.Pop(); //Initial dir
               val1 = (ExpVal)valStack.Pop(); //Caption
               eval_op_filesave_dlg(resVal, val1, val2, val3, val4, val5);
               break;

            case ExpressionInterface.EXP_OP_ISDEFAULT:
               val1 = (ExpVal)valStack.Pop();
               exp_is_default(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_STRTOKEN:
               val3 = (ExpVal)valStack.Pop(); // Delimiters String - alpha string containing the delimiter string.
               val2 = (ExpVal)valStack.Pop(); // Token Index - requested token index (numeric).
               val1 = (ExpVal)valStack.Pop(); // Source String - delimited alpha string with tokens.
               eval_op_strtok(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_STRTOK_CNT:
               val2 = (ExpVal)valStack.Pop(); //the delimeters string
               val1 = (ExpVal)valStack.Pop(); // the source string
               eval_op_strTokenCnt(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_STRTOKEN_IDX:
               val3 = (ExpVal)valStack.Pop(); //the delimeters string
               val2 = (ExpVal)valStack.Pop(); //the token to be found
               val1 = (ExpVal)valStack.Pop(); // the source string
               eval_op_strTokenIdx(val1, val2, val3, resVal);
               break;

            case ExpressionInterface.EXP_OP_BLOBSIZE:
               val1 = (ExpVal)valStack.Pop(); // Blob
               eval_op_blobsize(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CTRL_CLIENT_CX:
            case ExpressionInterface.EXP_OP_CTRL_CLIENT_CY:
            case ExpressionInterface.EXP_OP_CTRL_LEFT:
            case ExpressionInterface.EXP_OP_CTRL_TOP:
            case ExpressionInterface.EXP_OP_CTRL_WIDTH:
            case ExpressionInterface.EXP_OP_CTRL_HEIGHT:
               val2 = (ExpVal)valStack.Pop(); //generation
               val1 = (ExpVal)valStack.Pop(); //control name
               GetCtrlSize(resVal, val1, val2, opCode);
               break;

            case ExpressionInterface.EXP_OP_SETCRSR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_setcrsr(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_LAST_CTRL_PARK:
               val1 = (ExpVal)valStack.Pop();
               eval_op_last_parked(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_WEB_REFERENCE:
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.ALPHA;
               resVal.StrVal = "%" + val1.StrVal + "%";
               break;

            case ExpressionInterface.EXP_OP_CURRROW:
               val1 = (ExpVal)valStack.Pop();
               eval_op_curr_row(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CASE:
               // the case not work with Date field, but not work in OnLine too.
               nArgs = ((Int32)valStack.Pop());
               Exp_params = new ExpVal[nArgs];
               for (j = 0; j < nArgs; j++)
                  Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();
               val_cpy(Exp_params[0], resVal); // get  case(A) -> 'A' value

               for (j = 1; j < nArgs; j += 2)
               {
                  val1 = Exp_params[j];
                  bool valueMatched;
                  try
                  {
                     valueMatched = (val_cmp_any(val1, resVal, false) == 0);
                  }
                  catch (NullValueException)
                  {
                     valueMatched = false;
                  }

                  if (valueMatched)
                  {
                     val_cpy(Exp_params[j + 1], resVal); // the case found
                     break;
                  }
                  if (j == (nArgs - 3))
                  // array starts from 0 ->nArgs-1; last is diffault value ->nArgs-1;
                  // looking one before diffault ->nArgs-1 =>-3
                  {
                     val_cpy(Exp_params[j + 2], resVal); // diffault argument found
                     break;
                  }
               } // end of for loop

               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_THIS:
               eval_op_this(resVal);
               break;

            case ExpressionInterface.EXP_OP_LIKE:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_like(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_REPSTR:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_repstr(val1, val2, val3, resVal);
               break;

            case ExpressionInterface.EXP_OP_EDITGET:
               eval_op_editget(resVal);
               break;

            case ExpressionInterface.EXP_OP_EDITSET:
               val1 = (ExpVal)valStack.Pop(); //set the edited value of the control that invoked the last handler
               eval_op_editset(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_VARCURRN:
               val1 = (ExpVal)valStack.Pop(); // string, name of control
               exp_get_var(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_VARINDEX:
               val1 = (ExpVal)valStack.Pop(); // string, name of control
               exp_get_indx(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_HAND_CTRL:
               eval_op_hand_ctrl_name(resVal);
               break;

            case ExpressionInterface.EXP_OP_JCDOW:
               if (_expressionLocalJpn == null)
                  SetNULL(resVal, StorageAttribute.ALPHA);
               else
               {
                  val1 = (ExpVal)valStack.Pop(); // date
                  _expressionLocalJpn.eval_op_jcdow(resVal, val1.MgNumVal, DisplayConvertor.Instance);
               }
               break;

            case ExpressionInterface.EXP_OP_JMONTH:
               if (_expressionLocalJpn == null)
                  SetNULL(resVal, StorageAttribute.ALPHA);
               else
               {
                  val1 = (ExpVal)valStack.Pop(); // number or month of year
                  _expressionLocalJpn.eval_op_jmonth(resVal, val1);
               }
               break;

            case ExpressionInterface.EXP_OP_JNDOW:
               if (_expressionLocalJpn == null)
                  SetNULL(resVal, StorageAttribute.ALPHA);
               else
               {
                  val1 = (ExpVal)valStack.Pop(); // number or day of week
                  _expressionLocalJpn.eval_op_jndow(resVal, val1, DisplayConvertor.Instance);
               }
               break;

            case ExpressionInterface.EXP_OP_JYEAR:
               if (_expressionLocalJpn == null)
                  SetNULL(resVal, StorageAttribute.ALPHA);
               else
               {
                  val1 = (ExpVal)valStack.Pop(); // date
                  _expressionLocalJpn.eval_op_jyear(resVal, val1);
               }
               break;

            case ExpressionInterface.EXP_OP_JGENGO:
               if (_expressionLocalJpn == null)
                  SetNULL(resVal, StorageAttribute.ALPHA);
               else
               {
                  val2 = (ExpVal)valStack.Pop(); // date
                  val1 = (ExpVal)valStack.Pop(); // mode
                  _expressionLocalJpn.eval_op_jgengo(resVal, val1.MgNumVal, val2.MgNumVal,
                                                    DisplayConvertor.Instance);
               }
               break;

            case ExpressionInterface.EXP_OP_HAN:
               val1 = (ExpVal)valStack.Pop(); // string
               resVal.Attr = val1.Attr;

               if (_expressionLocalJpn == null)
                  resVal.StrVal = val1.StrVal;
               else if (val1.StrVal == null)
                  SetNULL(resVal, val1.Attr);
               else
               {
                  resVal.StrVal = _expressionLocalJpn.eval_op_han(val1.StrVal);
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               break;

            case ExpressionInterface.EXP_OP_ZEN:
               val1 = (ExpVal)valStack.Pop(); // string
               resVal.Attr = val1.Attr;

               if (_expressionLocalJpn == null)
                  resVal.StrVal = val1.StrVal;
               else if (val1.StrVal == null)
                  SetNULL(resVal, val1.Attr);
               else
               {
                  resVal.StrVal = _expressionLocalJpn.eval_op_zens(val1.StrVal, 0);
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               break;

            case ExpressionInterface.EXP_OP_ZENS:
               val2 = (ExpVal)valStack.Pop(); // mode
               val1 = (ExpVal)valStack.Pop(); // string
               resVal.Attr = val1.Attr;

               if (_expressionLocalJpn == null)
                  resVal.StrVal = val1.StrVal;
               else if (val2.MgNumVal == null || val1.StrVal == null)
                  SetNULL(resVal, val1.Attr);
               else
               {
                  resVal.StrVal = _expressionLocalJpn.eval_op_zens(val1.StrVal, val2.MgNumVal.NUM_2_LONG());
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               break;

            case ExpressionInterface.EXP_OP_ZIMEREAD:
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.ALPHA;
               if (_expressionLocalJpn == null)
                  resVal.StrVal = val1.StrVal;
               else
               {
                  resVal.StrVal = _expressionLocalJpn.eval_op_zimeread(val1.MgNumVal.NUM_2_LONG());
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               break;

            case ExpressionInterface.EXP_OP_ZKANA:
               val2 = (ExpVal)valStack.Pop(); // mode
               val1 = (ExpVal)valStack.Pop(); // string
               resVal.Attr = val1.Attr;

               if (_expressionLocalJpn == null)
                  resVal.StrVal = val1.StrVal;
               else if (val2.MgNumVal == null || val1.StrVal == null)
                  SetNULL(resVal, val1.Attr);
               else
               {
                  resVal.StrVal = _expressionLocalJpn.eval_op_zkana(val1.StrVal, val2.MgNumVal.NUM_2_LONG());
                  if (resVal.StrVal == null)
                     resVal.StrVal = "";
               }
               break;

            case ExpressionInterface.EXP_OP_GOTO_CTRL:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_gotoCtrl(val1, val2, val3, resVal);
               break;

            case ExpressionInterface.EXP_OP_TRANSLATE:
               val1 = (ExpVal)valStack.Pop();
               eval_op_translate(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_ASTR:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_astr(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_TREELEVEL:
               eval_op_treeLevel(resVal);
               break;

            case ExpressionInterface.EXP_OP_TREEVALUE:
               val1 = (ExpVal)valStack.Pop();
               eval_op_treeValue(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_LOOPCOUNTER:
               ConstructMagicNum(resVal, ((Task)ExpTask).getLoopCounter(), StorageAttribute.NUMERIC);
               break;

            case ExpressionInterface.EXP_OP_VECCELLATTR:
               val1 = (ExpVal)valStack.Pop();
               eval_op_vecCellAttr(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_VECGET:
               val2 = (ExpVal)valStack.Pop(); // the cell index
               val1 = (ExpVal)valStack.Pop(); //the vector not a 'var' literal
               eval_op_vecGet(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_VECSET:
               val3 = (ExpVal)valStack.Pop(); // the new value of the cell
               val2 = (ExpVal)valStack.Pop(); //the cells index to be set
               val1 = (ExpVal)valStack.Pop(); //a 'var' literal representing the vector
               eval_op_vecSet(val1, val2, val3, resVal);
               resVal.IsNull = false;
               if (expStrTracker.isNull())
                  expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_VECSIZE:
               val1 = (ExpVal)valStack.Pop(); //the vector not a 'var' literal
               eval_op_vecSize(val1, resVal);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_TREENODEGOTO:
               val1 = (ExpVal)valStack.Pop();
               eval_op_treeNodeGoto(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_IN:
               nArgs = ((Int32)valStack.Pop());

               resVal.Attr = StorageAttribute.BOOLEAN;
               resVal.BoolVal = false;

               Exp_params = new ExpVal[nArgs];
               for (j = 0; j < nArgs; j++)
                  Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

               try
               {
                  for (j = 1; j < nArgs; j++)
                  {
                     if (val_cmp_any(Exp_params[0], Exp_params[j], false) == 0)
                     {
                        resVal.BoolVal = true;
                        break;
                     }
                  }
               }
               catch (NullValueException)
               {
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
               }

               break;

            case ExpressionInterface.EXP_OP_ISCOMPONENT:
               eval_op_iscomponent(resVal);
               break;

            case ExpressionInterface.EXP_OP_MARKTEXT:
               val2 = (ExpVal)valStack.Pop(); //# of chars to mark
               val1 = (ExpVal)valStack.Pop(); //start position (1 = first position)
               eval_op_markText(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_MARKEDTEXTSET:
               val1 = (ExpVal)valStack.Pop(); //string to set
               eval_op_markedTextSet(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_MARKEDTEXTGET:
               eval_op_markedTextGet(resVal);
               break;

            case ExpressionInterface.EXP_OP_CARETPOSGET:
               eval_op_caretPosGet(resVal);
               break;

            case ExpressionInterface.EXP_OP_MNU:
               resVal.Attr = StorageAttribute.NUMERIC;
               len = expStrTracker.get2ByteNumber();
               resVal.MgNumVal = expStrTracker.getMagicNumber(len, true);
               break;

            case ExpressionInterface.EXP_OP_USER_DEFINED_FUNC:
               nArgs = ((Int32)valStack.Pop());
               /* nArgs should atleast be 1 (for holding the function name) */
               if (nArgs > 0)
               {
                  nArgs--; // one of the arguments is the name of the function (it will be the 1st value)
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  String funcName = ((ExpVal)valStack.Pop()).StrVal;
                  eval_op_ExecUserDefinedFunc(funcName, Exp_params, resVal, expectedType);
                  expStrTracker.resetNullResult();
               }
               break;

            case ExpressionInterface.EXP_OP_UNICODEASC:
               //get a unicode string and return the unicode numeric code of its first char
               val1 = (ExpVal)valStack.Pop();
               resVal.Attr = StorageAttribute.NUMERIC;
               resVal.IsNull = false;

               if (!val1.IsNull && val1.StrVal.Length > 0)
               {
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(val1.StrVal[0]);
               }
               else
                  resVal.IsNull = true;
               break;

            case ExpressionInterface.EXP_OP_CLIPADD:
               nArgs = ((Int32)valStack.Pop());
               resVal.Attr = StorageAttribute.BOOLEAN;
               resVal.BoolVal = false;

               Exp_params = new ExpVal[nArgs];
               for (j = 0; j < nArgs; j++)
                  Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();
               resVal.BoolVal = eval_op_clipAdd(Exp_params);
               break;

            case ExpressionInterface.EXP_OP_CLIPREAD:
               eval_op_clipread(resVal);
               break;

            case ExpressionInterface.EXP_OP_CLIPWRITE:
               eval_op_clipwrite(resVal);
               break;

            case ExpressionInterface.EXP_OP_PUBLICNAME:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_publicName(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_TASKID:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_taskId(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_DBVIEWSIZE:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_dbviewsize(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_DBVIEWROWIDX:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_dbviewrowidx(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_PROJECTDIR:
               eval_op_projectdir(resVal);
               break;

            case ExpressionInterface.EXP_OP_BROWSER_SET_CONTENT:
               val1 = (ExpVal)valStack.Pop(); //the text to be set
               val2 = (ExpVal)valStack.Pop(); //the control name          
               eval_op_browserSetContent(resVal, val2, val1);
               break;

            case ExpressionInterface.EXP_OP_BROWSER_GET_CONTENT:
               val1 = (ExpVal)valStack.Pop(); //the control name 
               eval_op_browserGetContent(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_BROWSER_SCRIPT_EXECUTE:
               nArgs = ((Int32)valStack.Pop());
               eval_op_browserExecute(valStack, resVal, nArgs);
               break;

            case ExpressionInterface.EXP_OP_MLS_TRANS:
               val1 = (ExpVal)valStack.Pop(); //the control name 
               eval_op_MlsTrans(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_LOGICAL:
               val2 = ((ExpVal)valStack.Pop()); // Reverse the string ?
               val1 = ((ExpVal)valStack.Pop()); // Alpha string
               eval_op_logical(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_VISUAL:
               val2 = ((ExpVal)valStack.Pop()); //Left to Right or reverse?
               val1 = ((ExpVal)valStack.Pop()); // Alpha string
               eval_op_visual(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_STR_BUILD:
               nArgs = ((Int32)valStack.Pop());
               eval_op_StrBuild(valStack, resVal, nArgs);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_DIRDLG:
               val3 = (ExpVal)valStack.Pop(); //show new folder
               val2 = (ExpVal)valStack.Pop(); //init dir
               val1 = (ExpVal)valStack.Pop(); //caption
               eval_op_client_dir_dlg(val1, val2, val3, resVal);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILECOPY:
               val2 = (ExpVal)valStack.Pop(); // target file name
               val1 = (ExpVal)valStack.Pop(); // source file name
               eval_op_client_filecopy(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILEEXIST:
               val1 = (ExpVal)valStack.Pop(); // source file
               eval_op_client_file_exist(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILESIZE:
               val1 = (ExpVal)valStack.Pop(); // source file
               eval_op_client_file_size(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILEDEL:
               val1 = (ExpVal)valStack.Pop(); // source file
               eval_op_client_file_delete(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILEREN:
               val2 = (ExpVal)valStack.Pop(); // target file name
               val1 = (ExpVal)valStack.Pop(); // source file name
               eval_op_client_file_rename(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILE_LIST_GET:
               val3 = (ExpVal)valStack.Pop(); // Directory
               val2 = (ExpVal)valStack.Pop(); // Filter
               val1 = (ExpVal)valStack.Pop(); // Search subdirectory
               eval_op_client_file_list_get(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_OS_ENV_GET:
               val1 = (ExpVal)valStack.Pop(); //Env variable to get
               eval_op_client_os_env_get(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CHECK_MENU:
               val2 = (ExpVal)valStack.Pop(); // To check or uncheck
               val1 = (ExpVal)valStack.Pop(); //Menu name
               eval_op_menu_check(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_ENABLE_MENU:
               val2 = (ExpVal)valStack.Pop(); // To enable or disabled.
               val1 = (ExpVal)valStack.Pop(); // Menu name
               eval_op_menu_enable(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_MNUADD:
               val2 = (ExpVal)valStack.Pop(); // Path where the menu structure is to be merged
               val1 = (ExpVal)valStack.Pop(); // A numeric value for the number of the menu in the Menu repository
               eval_op_menu_add(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_MNUREMOVE:
               nArgs = ((Int32)valStack.Pop());
               if (nArgs > 1)
                  val2 = (ExpVal)valStack.Pop(); // Path from where the menu entried to be removed
               else
                  val2 = null;
               val1 = (ExpVal)valStack.Pop(); // A numeric value for the number of the menu in the Menu repository
               eval_op_menu_remove(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_MNURESET:
               eval_op_menu_reset(resVal);
               break;

            case ExpressionInterface.EXP_OP_MENU_NAME:
               val2 = (ExpVal)valStack.Pop(); // Entry Text
               val1 = (ExpVal)valStack.Pop(); // Entry name
               eval_op_menu_name(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_MENU:
               eval_op_menu(resVal);
               break;

            case ExpressionInterface.EXP_OP_SHOW_MENU:
               val2 = (ExpVal)valStack.Pop(); // To Show or hide
               val1 = (ExpVal)valStack.Pop(); // Menu Name
               eval_op_menu_show(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_MENU_IDX:
               val2 = (ExpVal)valStack.Pop(); // isPublic
               val1 = (ExpVal)valStack.Pop(); // Menu Name
               eval_op_menu_idx(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_GET_UNIQUE_MC_ID:
               eval_op_client_get_unique_machine_id(resVal);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILE2BLOB:
               val1 = (ExpVal)valStack.Pop(); //file name
               eval_op_client_file_2_blb(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_BLOB2FILE:
               val2 = (ExpVal)valStack.Pop(); //file name
               val1 = (ExpVal)valStack.Pop(); //blob variable
               eval_op_client_blb_2_file(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_STATUSBARSETTEXT:
               val1 = (ExpVal)valStack.Pop();
               eval_op_statusbar_set_text(resVal, val1);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_GETPARAM:
               val1 = (ExpVal)valStack.Pop(); // parameter name
               eval_op_getParam(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_SETPARAM:
               val2 = (ExpVal)valStack.Pop(); // parameter value
               val1 = (ExpVal)valStack.Pop(); // parameter name
               eval_op_setParam(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_INIPUT:
               val2 = (ExpVal)valStack.Pop(); // parameter value
               val1 = (ExpVal)valStack.Pop(); // parameter name
               eval_op_iniput(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_INIGET:
               val1 = (ExpVal)valStack.Pop(); // parameter name
               eval_op_iniget(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_INIGETLN:
               val2 = (ExpVal)valStack.Pop(); // parameter number
               val1 = (ExpVal)valStack.Pop(); // section name
               eval_op_inigetln(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILEINFO:
               val2 = (ExpVal)valStack.Pop(); // requested value
               val1 = (ExpVal)valStack.Pop(); // file name
               eval_op_client_fileinfo(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_RIGHTS:
               val1 = (ExpVal)valStack.Pop(); // index
               eval_op_rights(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_EXPCALC:
               val1 = (ExpVal)valStack.Pop(); // EXP
               eval_op_expcalc(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_REDIRECT:
               val4 = (ExpVal)valStack.Pop();
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_ClientRedirect(resVal, val1, val2, val3, val4);
               break;

            case ExpressionInterface.EXP_OP_IS_MOBILE_CLIENT:
               eval_op_IsMobileClient(resVal);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_SESSION_STATISTICS_GET:
               eval_op_ClientSessionStatisticsGet(resVal);
               break;

            case ExpressionInterface.EXP_OP_DN_MEMBER:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_member(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_DN_STATIC_MEMBER:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_static_member(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_DN_METHOD:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_dn_method(resVal, Exp_params);
               }
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_STATIC_METHOD:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_dn_static_method(resVal, Exp_params);
               }
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_CTOR:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_dn_ctor(resVal, Exp_params);
               }
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_ARRAY_CTOR:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_dn_array_ctor(resVal, Exp_params);
               }
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_ARRAY_ELEMENT:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_dn_array_element(resVal, Exp_params);
               }
               break;

            case ExpressionInterface.EXP_OP_DN_INDEXER:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_dn_indexer(resVal, Exp_params);
               }
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_PROP_GET:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_prop_get(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_DN_STATIC_PROP_GET:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_static_prop_get(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_DN_ENUM:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_enum(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_DN_CAST:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_cast(resVal, val1, val2);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_REF:
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_ref(resVal, val1);
               expStrTracker.resetNullResult();
               break;

            case ExpressionInterface.EXP_OP_DN_SET:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dn_set(resVal, val1, val2);
               expStrTracker.resetNullResult();
               break;
#if !PocketPC
            case ExpressionInterface.EXP_OP_DATAVIEW_TO_DN_DATATABLE:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dataview_to_dn_datatable(resVal, val1, val2, val3);
               expStrTracker.resetNullResult();
               break;
#endif
            case ExpressionInterface.EXP_OP_DNTYPE:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_dntype(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_DN_EXCEPTION:
               eval_op_dn_exception(resVal);
               break;

            case ExpressionInterface.EXP_OP_DN_EXCEPTION_OCCURED:
               eval_op_dn_exception_occured(resVal);
               break;

            case ExpressionInterface.EXP_OP_TASKTYPE:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_taskType(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_SERVER_FILE_TO_CLIENT:
               val1 = (ExpVal)valStack.Pop(); // filename
               eval_op_server_file_to_client(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_FILE_TO_SERVER:
               val2 = (ExpVal)valStack.Pop(); // client filename
               val1 = (ExpVal)valStack.Pop(); // server filename
               eval_op_client_file_to_server(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_RANGE_ADD:
               nArgs = ((Int32)valStack.Pop());

               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_range_add(resVal, Exp_params);
               }
               break;

            case ExpressionInterface.EXP_OP_RANGE_RESET:
               val1 = (ExpVal)valStack.Pop(); //generation
               eval_op_range_reset(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_LOCATE_ADD:
               nArgs = ((Int32)valStack.Pop());
               if (nArgs > 0)
               {
                  Exp_params = new ExpVal[nArgs];
                  for (j = 0; j < nArgs; j++)
                     Exp_params[nArgs - 1 - j] = (ExpVal)valStack.Pop();

                  eval_op_locate_add(resVal, Exp_params);
               }
               break;

            case ExpressionInterface.EXP_OP_LOCATE_RESET:
               val1 = (ExpVal)valStack.Pop(); //generation
               eval_op_locate_reset(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_SORT_ADD:
               val2 = (ExpVal)valStack.Pop(); //dir
               val1 = (ExpVal)valStack.Pop(); //varnum
               eval_op_sort_add(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_SORT_RESET:
               val1 = (ExpVal)valStack.Pop(); //generation
               eval_op_sort_reset(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_TSK_INSTANCE:
               val1 = (ExpVal)valStack.Pop(); //generation
               eval_op_tsk_instance(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_WIN_HWND:
               val1 = (ExpVal)valStack.Pop(); // Generation
               eval_op_formhandle(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_CTRLHWND:
               val1 = (ExpVal)valStack.Pop(); // Control name
               eval_op_ctrlhandle(resVal, val1);
               break;

            case ExpressionInterface.EXP_OP_TERM:
               eval_op_terminal(resVal);
               break;

            case ExpressionInterface.EXP_OP_FORMSTATECLEAR:
               val1 = (ExpVal)valStack.Pop(); // formName
               eval_op_formStateClear(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_CONTROLS_PERSISTENCY_CLEAR:
               val2 = (ExpVal)valStack.Pop(); // options
               val1 = (ExpVal)valStack.Pop(); // restore deleted controls
               eval_op_ControlsPersistencyClear(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_SERVER_LAST_ACCESS_STATUS:
               eval_op_serverLastAccessStatus(resVal);
               break;

            case ExpressionInterface.EXP_OP_CLIENTSESSION_SET:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();

               eval_op_ClientSessionSet(val1, val2, resVal);
               break;
#if !PocketPC
            case ExpressionInterface.EXP_OP_DRAG_SET_DATA:
               nArgs = (Int32)valStack.Pop();      // Get the nArgs.

               val3 = null;
               if (nArgs > 2)
                  val3 = (ExpVal)valStack.Pop();   // Get UserFormat string
               val2 = (ExpVal)valStack.Pop();      // Get Format
               val1 = (ExpVal)valStack.Pop();      // Get Data

               eval_op_DragSetData(resVal, val1, val2, val3);
               break;

            case ExpressionInterface.EXP_OP_DROP_FORMAT:
               nArgs = (Int32)valStack.Pop();      // Get the nArgs.

               val2 = null;
               if (nArgs > 1)
                  val2 = (ExpVal)valStack.Pop();   // Get UserFormat string
               val1 = (ExpVal)valStack.Pop();      // Get Format
               eval_op_DropFormat(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_GET_DROP_DATA:
               nArgs = (Int32)valStack.Pop();      // Get the nArgs.

               val2 = null;
               if (nArgs > 1)
                  val2 = (ExpVal)valStack.Pop();   // Get UserFormat string
               val1 = (ExpVal)valStack.Pop();      // Get Format

               eval_op_DropGetData(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_DRAG_SET_CURSOR:
               {
                  val2 = (ExpVal)valStack.Pop(); // Cursor FileName
                  val1 = (ExpVal)valStack.Pop(); // Cursor Type

                  String serverFileName = exp_build_ioname(val2);  // Evaluate logical names.
                  val2.StrVal = commandsProcessor.GetLocalFileName(serverFileName, (Task)ExpTask, true);  // Get client cache file path.
                  eval_op_DragSetCursor(resVal, val1, val2);
               }
               break;

            case ExpressionInterface.EXP_OP_DROP_GET_X:
               eval_op_GetDropMouseX(resVal);
               break;

            case ExpressionInterface.EXP_OP_DROP_GET_Y:
               eval_op_GetDropMouseY(resVal);
               break;
#endif

            case ExpressionInterface.EXP_OP_CND_RANGE:
               val2 = (ExpVal)valStack.Pop(); // range to be used
               val1 = (ExpVal)valStack.Pop(); // condition to use range

               eval_op_CndRange(resVal, val1, val2);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_DB_DISCONNECT:
               val1 = (ExpVal)valStack.Pop();
               exp_op_ClientDbDisconnect(val1, resVal);
               break;

            case ExpressionInterface.EXP_OP_DATAVIEW_TO_DATASOURCE:
               val5 = (ExpVal)valStack.Pop();
               val4 = (ExpVal)valStack.Pop();
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();

               if (val1.IsNull || val2.IsNull || val3.IsNull || val4.IsNull || val5.IsNull)
               {
                  resVal.BoolVal = false;
                  resVal.Attr = StorageAttribute.BOOLEAN;
               }
               else
               {
                  eval_op_dataview_to_datasource(resVal, val1, val2, val3, val4, val5);

                  if (!string.IsNullOrEmpty(ClientManager.Instance.ErrorToBeWrittenInServerLog))
                  {
                     ClientManager.Instance.EventsManager.WriteErrorMessageesToServerLog(ExpTask, ClientManager.Instance.ErrorToBeWrittenInServerLog);
                  }
               }
               break;

            case ExpressionInterface.EXP_OP_CLIENT_DB_DEL:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               exp_op_ClientDbDelete(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_CLIENT_NATIVE_CODE_EXECUTION:
               resVal.Attr = StorageAttribute.ALPHA;
               resVal.StrVal = string.Empty;
               break;

            case ExpressionInterface.EXP_OP_CONTROL_ITEMS_REFRESH:
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_control_items_refresh (val1, val2, resVal);
               break;
#if !PocketPC
            case ExpressionInterface.EXP_OP_CLIENT_SQL_EXECUTE:
               nArgs = ((Int32)valStack.Pop());
               ExpVal[] additionlVars = null;
               if (nArgs > 2)
               {
                  additionlVars = new ExpVal[nArgs - 2];
                  while (nArgs > 2)
                  {
                     additionlVars[nArgs - 3] = (ExpVal)valStack.Pop();
                     nArgs--;
                  }
               }
               else
               {
                  additionlVars = new ExpVal[0];
               }
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               exp_op_SQLExecute(val1, val2, resVal,additionlVars);
               break;
#endif
            case ExpressionInterface.EXP_OP_PIXELSTOFROMUNITS:
               val2 = (ExpVal)valStack.Pop(); 
               val1 = (ExpVal)valStack.Pop(); 
               eval_op_PixelsToFormUnits(val1, val2, resVal);
               break;
            case ExpressionInterface.EXP_OP_FORMUNITSTOPIXELS:
               val2 = (ExpVal)valStack.Pop(); 
               val1 = (ExpVal)valStack.Pop(); 
               eval_op_FormUnitsToPixels(val1, val2, resVal);
               break;

            case ExpressionInterface.EXP_OP_COLOR_SET:
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_ColorSet(val1, val2, val3, resVal);
               break;

            case ExpressionInterface.EXP_OP_FONT_SET:
               val9 = (ExpVal)valStack.Pop();
               val8 = (ExpVal)valStack.Pop();
               val7 = (ExpVal)valStack.Pop();
               val6 = (ExpVal)valStack.Pop();
               val5 = (ExpVal)valStack.Pop();
               val4 = (ExpVal)valStack.Pop();
               val3 = (ExpVal)valStack.Pop();
               val2 = (ExpVal)valStack.Pop();
               val1 = (ExpVal)valStack.Pop();
               eval_op_FontSet(val1, val2, val3, val4, val5, val6, val7, val8, val9, resVal);
               break;

            case ExpressionInterface.EXP_OP_CONTROL_SELECT_PROGRAM:
               val1 = (ExpVal)valStack.Pop();
               eval_op_control_select_program(val1, resVal);
               break;

            default:
               return;
         }
         if (addResult)
         {
            ConvertExpVal(resVal, expectedType);
            valStack.Push(resVal);
            /* check if we must nullify result, because one of the members of the expression is NULL
            */
            if (resVal.IsNull)
               expStrTracker.setNullResult();
         }
      }

      /// <summary> Returns the status of the last server access </summary>
      /// <param name="resVal"></param>
      /// <returns></returns>
      private void eval_op_serverLastAccessStatus(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG((int)RemoteCommandsProcessor.GetInstance().ServerLastAccessStatus);
      }

      /// <summary>
      /// Sets the client session with Key, Value pair provided.
      /// </summary>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      /// <param name="resVal"></param>
      private void eval_op_ClientSessionSet(ExpVal val1, ExpVal val2, ExpVal resVal)
      {
         resVal.BoolVal = false;
         resVal.Attr = StorageAttribute.BOOLEAN;

         if (!val1.isEmptyString())
         {
            switch (val1.StrVal)
            {
               case ConstInterface.ENABLE_COMMUNICATION_DIALOGS:
                  if (val2.Attr == StorageAttribute.BOOLEAN)
                  {
                     ICommunicationsFailureHandler communicationFailureHandler = (val2.BoolVal ? (ICommunicationsFailureHandler)(new InteractiveCommunicationsFailureHandler())
                                                                                               : (ICommunicationsFailureHandler)(new SilentCommunicationsFailureHandler()));
                     HttpManager.GetInstance().SetCommunicationsFailureHandler(communicationFailureHandler);
                     resVal.BoolVal = true;
                  }
                  else
                     Logger.Instance.WriteExceptionToLog("Invalid attribute for " + ConstInterface.ENABLE_COMMUNICATION_DIALOGS + " key in ClientSessionSet()");
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog("Invalid Key in ClientSessionSet()");
                  break;
            }
         }
      }

      /// <summary>
      ///   Execute the expression itself
      /// </summary>
      /// <param name = "expStrTracker">the expression</param>
      /// <param name = "valStack">stack of values to use while executing the expression</param>
      /// <param name = "expectedType"></param>
      private void execExpression(ExpStrTracker expStrTracker, Stack valStack, StorageAttribute expectedType)
      {
         ExpVal expVal;
         int i;
         List<DynamicOperation> addedOpers = new List<DynamicOperation>();
         DynamicOperation dynOper;

         int opCode = expStrTracker.getOpCode();

         // if a basic data item, just get it and push it to the stack
         if (isBasicItem(opCode))
         {
            expVal = getExpBasicValue(expStrTracker, opCode);

            ConvertExpVal(expVal, expectedType);
            valStack.Push(expVal);
            /* check if we must nullify result, because one of the members of the expression is NULL
            */
            if (expVal.IsNull)
               expStrTracker.setNullResult();

            return;
         }

         ExpressionDict.ExpDesc expDesc = ExpressionDict.expDesc[opCode];

         // Null result of a function depends only on return value its parameters
         // It doesn't depend on whether any parameter in the whole expression is NULL
         // For example in IF (VecSet ('A'Var, 1, NULL()), A, B)
         // IF should not return NULL because VecSet has a NULL parameter but it should
         // return NULL if either of the parameters (VecSet, A or B) is NULL
         bool nullArgs = false;
         if (expDesc.ArgEvalCount_ > 0)
         {
            for (i = 0; i < expDesc.ArgEvalCount_; i++)
            {
               expStrTracker.resetNullResult();
               execExpression(expStrTracker, valStack, (StorageAttribute)expDesc.ArgAttr_[i]);
               if (expStrTracker.isNull())
                  nullArgs = true;
            }
         }

         if (nullArgs)
            expStrTracker.setNullResult();

         // execute the current operator
         execOperation(opCode, expStrTracker, valStack, addedOpers, expectedType);

         // if there are side effect operation, execute them
         int nDynOpers = addedOpers.Count;
         if (nDynOpers > 0)
         {
            for (i = 0; i < nDynOpers; i++)
            {
               dynOper = addedOpers[0];
               addedOpers.RemoveAt(0);
               switch (dynOper.opCode_)
               {
                  case ExpressionInterface.EXP_OP_IGNORE:
                     int j;
                     for (j = 0; j < dynOper.argCount_; j++)
                        expStrTracker.skipOperator();
                     break;

                  case ExpressionInterface.EXP_OP_EVALX:
                     execExpression(expStrTracker, valStack, expectedType);
                     break;
               }
            }
         }
      }

      /// <summary>
      /// Calculate the value of the 1st CndRange parameter, and return true if the CndRange result should be discarded
      /// Similar to execExpression, but works only on CndRange, and returns the result of the CndRange 1st parameter
      /// </summary>
      /// <param name="expStrTracker"></param>
      /// <param name="valStack"></param>
      /// <param name="expectedType"></param>
      /// <returns>true if CndRange evaluated to false and the range should be discarded</returns>
      private bool DiscardCndRangeExpression(ExpStrTracker expStrTracker, Stack valStack)
      {
         ExpVal expVal;
         List<DynamicOperation> addedOpers = new List<DynamicOperation>();

         int opCode = expStrTracker.getOpCode();

         // if a basic data item, just get it and push it to the stack
         if (opCode != ExpressionInterface.EXP_OP_CND_RANGE)
         {
            return false;
         }

         ExpressionDict.ExpDesc expDesc = ExpressionDict.expDesc[opCode];

         execExpression(expStrTracker, valStack, (StorageAttribute)expDesc.ArgAttr_[0]);

         expVal = (ExpVal)valStack.Pop();
         return expVal.Attr == StorageAttribute.BOOLEAN && !expVal.BoolVal;
      }

      /// <summary>
      ///   Compare two expression values
      /// </summary>
      /// <param name="val1">  The first value</param>
      /// <param name="val2">  The second value</param>
      /// <returns> 0 if val1=val2, 1 if val1>val2 of -1 if num1<num2</returns>
      internal static int val_cmp_any(ExpVal val1, ExpVal val2, bool forceComparer)
      {
         int retval = 0;
         StorageAttribute attr1 = val1.Attr;
         StorageAttribute attr2 = val2.Attr;
         object Obj1 = null, Obj2 = null;
         bool compareObjects = false;
         ExpressionEvaluator expVal = new ExpressionEvaluator();

         if (val1.Attr != StorageAttribute.DOTNET &&
             val2.Attr != StorageAttribute.DOTNET)
         {
            if (val1.IsNull && val2.IsNull)
               return 0;

            if (val1.IsNull || val2.IsNull)
               throw new NullValueException(attr1);
         }

         if (StorageAttributeCheck.isTypeBlob(attr1))
         {
            if (val1.IncludeBlobPrefix && BlobType.getContentType(val1.StrVal) == BlobType.CONTENT_TYPE_BINARY)
            {
               val1.StrVal = BlobType.removeBlobPrefix(val1.StrVal);
               val1.Attr = StorageAttribute.ALPHA;
               val1.IncludeBlobPrefix = false;
            }
            else
               expVal.ConvertExpVal(val1, StorageAttribute.UNICODE);
         }
         if (StorageAttributeCheck.isTypeBlob(attr2))
         {
            if (val2.IncludeBlobPrefix && BlobType.getContentType(val2.StrVal) == BlobType.CONTENT_TYPE_BINARY)
            {
               val2.StrVal = BlobType.removeBlobPrefix(val2.StrVal);
               val2.Attr = StorageAttribute.ALPHA;
               val2.IncludeBlobPrefix = false;
            }
            else
               expVal.ConvertExpVal(val2, StorageAttribute.UNICODE);
         }

         /* If one val is Dotnet and the other is MagicType, convert the Dotnet val to Magic val before comparing. */
         if (val1.Attr == StorageAttribute.DOTNET &&
             val2.Attr != StorageAttribute.DOTNET)
            expVal.ConvertExpVal(val1, val2.Attr);
         else if (val2.Attr == StorageAttribute.DOTNET &&
                  val1.Attr != StorageAttribute.DOTNET)
            expVal.ConvertExpVal(val2, val1.Attr);

         attr1 = val1.Attr;
         attr2 = val2.Attr;

         if (attr1 != attr2)
         {
            if ((StorageAttributeCheck.isTypeNumeric(attr1) && StorageAttributeCheck.isTypeNumeric(attr2)) ||
                (StorageAttributeCheck.IsTypeAlphaOrUnicode(attr1) && StorageAttributeCheck.IsTypeAlphaOrUnicode(attr2)))
            {
               /* Do nothing : it's OK to compare these types */
            }
            else
               return 1;
         }

         //-----------------------------------------------------------------------
         // This code was taken from STORAGE::fld_cmp method in Magic
         //-----------------------------------------------------------------------
         switch (attr1)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
            case StorageAttribute.UNICODE:
               if (val1.StrVal == null && val2.StrVal == null)
                  return 0;

               if (val1.StrVal == null || val2.StrVal == null)
                  throw new NullValueException(attr1);

               string str1 = StrUtil.rtrim(val1.StrVal);
               string str2 = StrUtil.rtrim(val2.StrVal);

               if (ClientManager.Instance.getEnvironment().getSpecialAnsiExpression() ||
                   (UtilStrByteMode.isLocaleDefLangDBCS() &&
                   attr1 == StorageAttribute.ALPHA && attr2 == StorageAttribute.ALPHA))
                  retval = UtilStrByteMode.strcmp(str1, str2);
               else
                  retval = String.CompareOrdinal(str1, str2);
               break;


            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               if (val1.MgNumVal == null && val2.MgNumVal == null)
                  return 0;

               if (val1.MgNumVal == null || val2.MgNumVal == null)
                  throw new NullValueException(attr1);
               retval = NUM_TYPE.num_cmp(val1.MgNumVal, val2.MgNumVal);
               break;


            case StorageAttribute.BOOLEAN:
               retval = (val1.BoolVal
                            ? 1
                            : 0) - (val2.BoolVal
                                       ? 1
                                       : 0);
               break;

            case StorageAttribute.DOTNET:
               if (val1.StrVal != null && val2.StrVal != null)
               {
                  int key1 = BlobType.getKey(val1.StrVal);
                  int key2 = BlobType.getKey(val2.StrVal);
                  if (key1 == key2)
                     retval = 0;
                  else if (key1 == 0 || key2 == 0)
                     retval = 1;
                  else
                  {
                     Obj1 = DNManager.getInstance().DNObjectsCollection.GetDNObj(key1);
                     Obj2 = DNManager.getInstance().DNObjectsCollection.GetDNObj(key2);
                     compareObjects = true;
                  }
               }
               else if (val1.DnMemberInfo != null && val2.DnMemberInfo != null)
               {
                  Obj1 = val1.DnMemberInfo.value;
                  Obj2 = val2.DnMemberInfo.value;
                  compareObjects = true;
               }
               else
                  retval = 1;

               if (compareObjects)
               {
                  if (forceComparer)
                  {
                     var c = new Comparer(CultureInfo.CurrentCulture);
                     retval = c.Compare(Obj1, Obj2);
                  }
                  else
                  {
                     if (Obj1 != null)
                        retval = Obj1.Equals(Obj2)
                                    ? 0
                                    : 1;
                     else
                        retval = (Obj2 == null)
                                    ? 0
                                    : 1;
                  }
               }
               break;
         }
         return (retval);
      }

      /// <summary>
      ///   calculate the result of the expression
      /// </summary>
      /// <param name = "exp">is a reference to the expression string to be evaluated. this string
      ///   is in an expression in polish notation where every byte is represented by a
      ///   2-digit hex number</param>
      /// <param name = "expectedType">is the expected type of the expression. when the expressions type does
      ///   not match the expected type an exception should be thrown. a space (' ') here
      ///   means any expression. it is used for Evaluate operation.</param>
      /// <param name = "task">is a reference to the task in which the expression is defined. it is
      ///   used for getting a reference to fields for getting/setting their values</param>
      /// <returns> String is the result of the evaluation:
      ///   1. string value will be a string
      ///   2. numeric, date, time will be a Magic number as a string of hex digits
      ///   3. logical will be one digit: 0 for FALSE, 1 for TRUE
      ///   4. null value just return null (not an empty string!)</returns>
      internal static ExpVal eval(sbyte[] exp, StorageAttribute expectedType, Task task)
      {
         ExpressionEvaluator me;
         ExpVal res = null;
         var valStack = new Stack();
         ExpStrTracker expStrTracker;

         if (exp != null && exp.Length > 0)
         {
            me = new ExpressionEvaluator();
            expStrTracker = new ExpStrTracker(exp, task.getNullArithmetic() == Constants.NULL_ARITH_NULLIFY);
            me.ExpTask = task;
            me.execExpression(expStrTracker, valStack, expectedType);
            res = (ExpVal)valStack.Pop();
            if (expStrTracker.isNull())
               //null arithmetic == nullify and one of the members of the expression is null,
               //so the result must be NULL
               res.IsNull = true;
         }
         return res;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="exp"></param>
      /// <param name="expectedType"></param>
      /// <param name="task"></param>
      /// <returns></returns>
      internal static bool DiscardCndRangeResult(sbyte[] exp, Task task)
      {
         ExpressionEvaluator me;
         var valStack = new Stack();
         ExpStrTracker expStrTracker;

         if (exp != null && exp.Length > 0)
         {
            me = new ExpressionEvaluator();
            expStrTracker = new ExpStrTracker(exp, task.getNullArithmetic() == Constants.NULL_ARITH_NULLIFY);
            me.ExpTask = task;
            return me.DiscardCndRangeExpression(expStrTracker, valStack);
         }
         return false;
      }

      /// <summary>
      ///   Convert a string to a Null terminated Pic
      ///   max length of the mask is 100 characters
      /// </summary>
      private static String set_a_pic(String val)
      {
         int len = Math.Min(val.Length, 99);
         return (StrUtil.ZstringMake(val, len));
      }

      /// <summary>
      ///   Evaluate RANGE magic function
      /// </summary>
      private void eval_op_range(ExpVal val1, ExpVal val2, ExpVal val3, ExpVal resVal)
      {
         resVal.BoolVal = false;
         resVal.Attr = StorageAttribute.BOOLEAN;
         try
         {
            if (val_cmp_any(val1, val2, true) >= 0)
            {
               val2 = val3;
               if (val_cmp_any(val1, val2, true) <= 0)
                  resVal.BoolVal = true;
            }
         }
         catch (NullValueException nullFound)
         {
            SetNULL(resVal, nullFound.getAttr());
         }
      }

      /// <summary>
      ///   Evaluate REP magic function
      /// </summary>
      private void eval_op_rep_1(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3, ExpVal val4)
      {
         if (val1.StrVal == null || val2.StrVal == null || val3.MgNumVal == null || val4.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }

         val_s_cpy(val1, resVal);
         int i = val3.MgNumVal.NUM_2_LONG();
         if (i < 1)
            i = 1;
         int j = val4.MgNumVal.NUM_2_LONG();

         if (j > val2.StrVal.Length)
            j = val2.StrVal.Length;

         if (i + j - 1 > resVal.StrVal.Length)
            j = resVal.StrVal.Length - i + 1;

         if (j <= 0)
            return;

         string tmp_s = StrUtil.memcpy("", 0, resVal.StrVal, i + j - 1, resVal.StrVal.Length - i - j + 1);

         resVal.StrVal = StrUtil.memcpy(resVal.StrVal, i - 1, val2.StrVal, 0, j);
         resVal.StrVal = StrUtil.memcpy(resVal.StrVal, i - 1 + j, tmp_s, 0, resVal.StrVal.Length - i - j + 1);

         /* add blanks to the end*/
         if ((j - val2.StrVal.Length) > 0)
            resVal.StrVal = StrUtil.memset(resVal.StrVal, i + val2.StrVal.Length - 1, ' ', j - val2.StrVal.Length);
      }

      /// <summary>
      ///   Copy an Alpha value (analoge to magic function)
      /// </summary>
      private static void val_s_cpy(ExpVal val, ExpVal resVal)
      {
         resVal.Attr = val.Attr;
         resVal.StrVal = val.StrVal;
         resVal.IsNull = val.IsNull;
         resVal.IncludeBlobPrefix = val.IncludeBlobPrefix;
      }

      /// <summary>
      ///   copy val to resVal
      /// </summary>
      /// <param name = "val">source ExpVal
      /// </param>
      /// <param name="resVal">destination ExpVal
      /// </param>
      private void val_cpy(ExpVal val, ExpVal resVal)
      {
         switch (val.Attr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
               val_s_cpy(val, resVal);
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               if (! val.IsNull)
                  resVal.MgNumVal = new NUM_TYPE(val.MgNumVal);
               break;

            case StorageAttribute.BOOLEAN:
               resVal.BoolVal = val.BoolVal;
               break;

            case StorageAttribute.DOTNET:
               resVal.DnMemberInfo = val.DnMemberInfo;
               break;

            default:
               Logger.Instance.WriteExceptionToLog("Expression Evaluator.val_cpy no such type of attribute : " + val.Attr);
               break;
         }
         resVal.Attr = val.Attr;
         resVal.IsNull = val.IsNull;
      }

      /// <summary>
      ///   insert string into another string, INS magic function
      /// </summary>
      private void eval_op_ins(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3, ExpVal val4)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         if (val1.StrVal == null || val2.StrVal == null || val3.MgNumVal == null || val4.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }
         int i = val1.StrVal.Length;

         int ins_ofs = val3.MgNumVal.NUM_2_LONG() - 1;
         ins_ofs = Math.Max(ins_ofs, 0);
         ins_ofs = Math.Min(ins_ofs, i);

         int ins_len = val4.MgNumVal.NUM_2_LONG();
         ins_len = Math.Max(ins_len, 0);
         ins_len = Math.Min(ins_len, val2.StrVal.Length);

         resVal.StrVal = StrUtil.memcpy("", 0, val1.StrVal, 0, ins_ofs);
         resVal.StrVal = StrUtil.memcpy(resVal.StrVal, ins_ofs, val2.StrVal, 0, ins_len);
         resVal.StrVal = StrUtil.memcpy(resVal.StrVal, ins_ofs + ins_len, val1.StrVal, ins_ofs, i - ins_ofs);
      }

      /// <summary>
      ///   DEL magic function
      /// </summary>
      private void eval_op_del(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         if (val1.StrVal == null || val2.MgNumVal == null || val3.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }

         val_s_cpy(val1, resVal);
         int i = val2.MgNumVal.NUM_2_LONG() - 1;
         if (i < 0)
            i = 0;
         if (i > resVal.StrVal.Length)
            i = resVal.StrVal.Length;
         int j = val3.MgNumVal.NUM_2_LONG();
         if (i + j > resVal.StrVal.Length)
            j = resVal.StrVal.Length - i;
         if (j <= 0)
            return;
         // resVal.strVal_ = resVal.strVal_.substring(0, resVal.strVal_.length()-j);
         resVal.StrVal = StrUtil.memcpy(resVal.StrVal, i, resVal.StrVal, i + j, resVal.StrVal.Length - i);
      }

      /// <summary>
      ///   FLIP magic function (reverse)
      /// </summary>
      private void eval_op_flip(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }
         var rev_str = new StringBuilder(val1.StrVal);
         resVal.StrVal = StrUtil.ReverseString(rev_str).ToString();
         //resVal.strVal_ = XmlParser.reverse(val1.strVal_);
      }

      /// <summary>
      ///   UPPER magic function
      /// </summary>
      private void eval_op_upper(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }
         resVal.StrVal = val1.StrVal.ToUpper();
      }

      /// <summary>
      ///   LOWER magic function
      /// </summary>
      private void eval_op_lower(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }
         resVal.StrVal = val1.StrVal.ToLower();
      }

      /// <summary>
      ///   CRC magic function
      /// </summary>
      private void eval_op_crc(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         if (val1.StrVal == null || val2.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         int mode = val2.MgNumVal.NUM_2_LONG();
         short res = 0;
         switch (mode)
         {
            case 0:
               res = eval_crc_16(val1.StrVal);
               break;
         }
         // val_set_s_allc ((Uchar *) &res, 2);
         var left = (char)(res % 256);
         var right = (char)(res / 256);

         resVal.StrVal = "" + left + right;
      }

      private static short eval_crc_16(String buf)
      {
         var crc_16_table = new[]
         {
            (ushort) 0xA001, (ushort) 0xF001, (ushort) 0xD801, (ushort) 0xCC01, (ushort) 0xC601,
            (ushort) 0xC301, (ushort) 0xC181, (ushort) 0xC0C1
         };
         int buffer_idx = 0;
         int len = buf.Length;

         short crc = 0;
         for (; len > 0; len--)
         {
            var bt = (byte)buf[buffer_idx++];
            bt = (byte)(bt ^ LO_CHAR(crc));
            crc = LO_CHAR(MK_SHRT(0, HI_CHAR(crc)));
            byte mask;
            int tbl_idx;
            for (tbl_idx = 0, mask = (byte)LO_CHAR(0x80); tbl_idx < 8; tbl_idx++, mask = (byte)(LO_CHAR(mask) >> 1))
            {
               if ((byte)LO_CHAR((short)(bt & (byte)LO_CHAR(mask))) != 0)
                  crc = (short)(crc ^ crc_16_table[tbl_idx]);
            }
         }
         return (crc);
      }

      private static short LO_CHAR(short n)
      {
         return (short)(n & 0xff);
      }

      private static short HI_CHAR(short n)
      {
         return (short)((n & 0xff00) >> 8);
      }

      private static short MK_SHRT(short c1, short c2)
      {
         return (short)((ushort)(c1 << 8) | (ushort)c2);
      }

      private static int MK_LONG(int c1, int c2, int c3, int c4)
      {
         //      return (int) ((uint)(c1 << 24) | (uint)(c2 << 16) | (uint)(c3 << 8) | (uint) c4);
         return ((c1 << 24) | (c2 << 16) | (c3 << 8) | c4);
      }

      private void eval_op_chkdgt(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         var weight_vals = new[]
         {
            (char) (1), (char) (2), (char) (5), (char) (3), (char) (6), (char) (4), (char) (8),
            (char) (7), (char) (10), (char) (9)
         };
         int pos;
         int mul;
         String c_str;
         int digits;

         if (val1.StrVal == null || val2.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int mode = val2.MgNumVal.NUM_2_LONG();
         int res = 0;
         switch (mode)
         {
            case 0:
               mul = 2;
               for (pos = val1.StrVal.Length; pos >= 1; pos--)
               {
                  c_str = val1.StrVal.Substring(pos - 1);
                  digits = a_2_long(c_str, 1) * mul;
                  res += digits + (digits > 9
                                   ? 1
                                   : 0);
                  mul = 3 - mul;
               }
               res %= 10;
               if (res != 0)
                  res = 10 - res;
               break;

            case 1:
               for (pos = val1.StrVal.Length; pos >= 1; pos--)
               {
                  mul = weight_vals[(val1.StrVal.Length - pos) % 10];
                  char c_char = Char.ToUpper(val1.StrVal[pos - 1]);
                  c_str = val1.StrVal.Substring(pos - 1);
                  if (UtilStrByteMode.isDigit(c_char))
                     digits = a_2_long(c_str, 1) * mul;
                  else if (Char.IsUpper(c_str[0]))
                     digits = (c_str[0] - 'A' + 1) * mul;
                  else
                     digits = 0;
                  res += digits;
               }
               res %= 11;
               if (res != 0)
                  res = 11 - res;
               break;
         }
         ConstructMagicNum(resVal, res, StorageAttribute.NUMERIC);
      }

      private int a_2_long(String str, int len)
      {
         int pos;

         int n = 0;
         for (pos = 0; pos < len; pos++)
            if (UtilStrByteMode.isDigit(str[pos]))
            {
               n *= 10;
               n += str[pos] - '0';
            }
         return (n);
      }

      private void eval_op_soundx(ExpVal resVal, ExpVal val1)
      {
         var soundx_vals = new[]
         {
            '0', '1', '2', '3', '0', '1', '2', '0', '0', '2', '2', '4', '5', '5', '0', '1', '2',
            '6',
            '2', '3', '0', '1', '0', '2', '0', '2'
         };
         int inpos;

         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = "0000";
         char lastc = ' ';
         int outpos = 0;
         for (inpos = 0; inpos < val1.StrVal.Length; inpos++)
         {
            char inc = Char.ToUpper(val1.StrVal[inpos]);
            char outc = Char.IsUpper(inc)
                        ? soundx_vals[inc - 'A']
                        : '0';
            if (inpos == 0)
               resVal.StrVal = setAt(resVal.StrVal, outpos++, inc);
            else if (outc > '0' && outc != lastc)
            {
               resVal.StrVal = setAt(resVal.StrVal, outpos++, outc);
               if (outpos > 3)
                  break;
            }
            lastc = outc;
         }
      }

      /// <summary>
      ///   set character inside string on needed position
      /// </summary>
      /// <param name = "str">to insert char into</param>
      /// <param name = "pos">of character to insert</param>
      /// <param name = "ch">to be inserted</param>
      /// <returns> string with inserted char</returns>
      private static String setAt(String str, int pos, char ch)
      {
         var buffer = new StringBuilder(str);
         try
         {
            buffer[pos] = ch;
         }
         catch (ArgumentOutOfRangeException)
         {
            buffer.Append(ch);
         }
         return buffer.ToString();
      }

      private void eval_op_hstr(ExpVal resVal, ExpVal val1)
      {
         var num16 = new NUM_TYPE();
         NUM_TYPE newnum;
         int digit;
         var outstr = new char[30];
         var tmpOutStr = new StringBuilder(outstr.Length);
         bool negative = false;

         resVal.Attr = StorageAttribute.ALPHA;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }

         /* note: can be optimized by using larger divisor */
         num16.num_4_a_std("16");
         var orgnum = new NUM_TYPE(val1.MgNumVal);
         if (orgnum.num_is_neg())
         {
            negative = true;
            orgnum.num_abs();
         }
         int digits = 0;
         while (true)
         {
            newnum = NUM_TYPE.mod(orgnum, num16);
            orgnum = NUM_TYPE.div(orgnum, num16);
            orgnum.num_trunc(0);
            digit = newnum.NUM_2_LONG();
            digits++;
            int_2_hex(outstr, outstr.Length - digits, 1, digit, 0);
            if (orgnum.num_is_zero())
               break;
         }

         if (negative)
         {
            digits++;
            outstr[outstr.Length - digits] = '-';
         }
         for (digit = outstr.Length - digits; digit < outstr.Length; digit++)
            tmpOutStr.Append(outstr[digit]);
         resVal.StrVal = tmpOutStr.ToString();
      }

      private static void int_2_hex(char[] str, int strPos, int len, int n, int lead)
      {
         int pos = len;
         do
         {
            int digit = n % 16;
            if (digit < 10)
               str[--pos + strPos] = (char)('0' + digit);
            else
               str[--pos + strPos] = (char)('A' + digit - 10);
            n /= 16;
         } while (pos > 0 && n != 0);
         lib_a_fill(str, len, pos + strPos, lead);
         return;
      }

      private static void lib_a_fill(char[] str, int len, int pos, int lead)
      {
         if (lead == 0)
         {
            len -= pos;
            if (len > 0 && pos > 0)
            {
               StrUtil.memcpy(str, 0, str, pos, len);
               StrUtil.memset(str, len, ' ', pos);
            }
         }
         else
         {
            if (pos > 0)
               StrUtil.memset(str, 0, (char)lead, pos);
         }
         return;
      }

      private void eval_op_hval(ExpVal resVal, ExpVal val1)
      {
         var num16 = new NUM_TYPE();
         var num = new NUM_TYPE();
         int digits;
         int state = 0; // STATE_BEFORE_NUMBER = 0; STATE_IN_NUMBER = 1
         bool negative = false;

         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         /* note: can be optimized by using larger divisor */

         num16.num_4_a_std("16");
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_ZERO();
         for (digits = 0; digits < val1.StrVal.Length; digits++)
         {
            char digitc = val1.StrVal[digits];

            if (digitc == '-' && state == 0)
               negative = true;

            int digit = hex_2_long(val1.StrVal, digits, 1);

            if (digit == 0 && digitc != '0')
               continue;

            state = 1; // STATE_IN_NUMBER
            resVal.MgNumVal = NUM_TYPE.mul(resVal.MgNumVal, num16);
            num.NUM_4_LONG(digit);
            resVal.MgNumVal = NUM_TYPE.add(resVal.MgNumVal, num);
         }
         if (negative)
            resVal.MgNumVal.num_neg();
         resVal.Attr = StorageAttribute.NUMERIC;
      }

      private int hex_2_long(String str, int strCount, int len)
      {
         int pos;

         int n = 0;
         for (pos = strCount; pos < strCount + len; pos++)
         {
            char digit = Char.ToUpper(str[pos]);
            if (UtilStrByteMode.isDigit(digit))
            {
               n *= 16;
               n += digit - '0';
            }
            else if (digit >= 'A' && digit <= 'F')
            {
               n *= 16;
               n += digit - 'A' + 10;
            }
         }
         return n;
      }

      private void eval_op_chr(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         var c = Encoding.Default.GetChars(BitConverter.GetBytes(val1.MgNumVal.NUM_2_LONG()))[0];
         resVal.StrVal = "" + c;
      }

      private void eval_op_asc(ExpVal resVal, ExpVal val1)
      {
         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }
         int c = 0;
         if (val1.StrVal.Length != 0)
         {
            char[] chararray = { val1.StrVal[0] };
            c = Encoding.Default.GetBytes(chararray)[0];
         }

         ConstructMagicNum(resVal, c, StorageAttribute.NUMERIC);
      }

      private void eval_op_mstr(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         if (val1.MgNumVal == null && val2.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }

         var num = new NUM_TYPE(val1.MgNumVal);
         if (num.NUM_IS_LONG())
            num.num_4_std_long();
         int len = val2.MgNumVal.NUM_2_LONG();
         len = Math.Max(len, 2); // 2 -> 2 byte minimum -> 1 Java char
         var tmpArray = new sbyte[len];
         for (int i = 0; i < len; i++)
            tmpArray[i] = num.Data[i];

         byte[] tmpBytes = Misc.ToByteArray(tmpArray);
         resVal.StrVal = Encoding.Default.GetString(tmpBytes, 0, tmpBytes.Length);
      }

      private void eval_op_mval(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.NUMERIC;
         if (val1.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_SET_ZERO();
         int len = Math.Min(val1.StrVal.Length, NUM_TYPE.NUM_SIZE);
         resVal.MgNumVal = new NUM_TYPE(Misc.ToSByteArray(Encoding.Default.GetBytes(val1.StrVal)), 0, len);
      }

      /// <summary>
      ///   evaluation of Date to the Alfa string with special format
      /// </summary>
      private void eval_op_dstr(ExpVal resVal, ExpVal val1, ExpVal val2, DisplayConvertor displayConvertor)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         if (val1.MgNumVal == null || val2.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }
         resVal.StrVal = displayConvertor.to_a(resVal.StrVal, 100, val1.MgNumVal.NUM_2_ULONG(), val2.StrVal,
                                                ((Task)ExpTask).getCompIdx());
      }

      private void eval_op_tstr(ExpVal resVal, ExpVal val1, ExpVal val2, DisplayConvertor displayConvertor,
                                bool milliSeconds)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         if (val1.MgNumVal == null || val2.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         resVal.StrVal = displayConvertor.time_2_a(resVal.StrVal, 100, val1.MgNumVal.NUM_2_ULONG(), val2.StrVal,
                                                    ((Task)ExpTask).getCompIdx(), milliSeconds);
      }

      private void eval_op_dval(ExpVal resVal, ExpVal val1, ExpVal val2, DisplayConvertor displayConvertor)
      {
         // char pic[]=new char[100];
         if (val1.StrVal == null || val2.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.DATE);
            return;
         }

         /*--------------------------------------------------------------*/
         /* year 2000 bug - if the user inserts a date value with only 2 */
         /* year digits (i.e. DD/MM/YY), the a_2_date might insert extra */
         /* 2 century digits, and enlarge the input string. In order to  */
         /* aviod memory corruptions, we enlarge the date string input   */
         /* buffer - Ilan shiber                                         */
         /*--------------------------------------------------------------*/
         if (val1.Attr == StorageAttribute.UNICODE)
            if (val1.StrVal.Length + 2 <= NUM_TYPE.NUM_SIZE)
            {
            }
            else
            {
               var tmp = new char[val1.StrVal.Length + 2];
               StrUtil.memcpy(tmp, 0, val1.StrVal.ToCharArray(), 0, val1.StrVal.Length);
            }

         int l = displayConvertor.a_2_date(val1.StrVal, val2.StrVal, ((Task)ExpTask).getCompIdx());
         if (l >= 1000000000)
            l = 0;
         ConstructMagicNum(resVal, l, StorageAttribute.DATE);
      }

      private void eval_op_tval(ExpVal resVal, ExpVal val1, ExpVal val2, DisplayConvertor displayConvertor,
                                bool milliSeconds)
      {
         // Uchar pic[100];
         if (val1.StrVal == null || val2.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.TIME);
            return;
         }

         var pic = new PIC(val2.StrVal, StorageAttribute.TIME, ((Task)ExpTask).getCompIdx());
         int l = displayConvertor.a_2_time(val1.StrVal, pic, milliSeconds);

         if (l >= 1000000000)
            l = 0;
         ConstructMagicNum(resVal, l, StorageAttribute.TIME);
      }

      private void eval_op_day(ExpVal resVal, ExpVal val1)
      {
         eval_op_date_brk(resVal, val1.MgNumVal, 0);
      }

      protected internal void eval_op_month(ExpVal resVal, ExpVal val1)
      {
         eval_op_date_brk(resVal, val1.MgNumVal, 1);
      }

      private void eval_op_year(ExpVal resVal, ExpVal val1)
      {
         eval_op_date_brk(resVal, val1.MgNumVal, 2);
      }

      private void eval_op_dow(ExpVal resVal, ExpVal val1)
      {
         eval_op_date_brk(resVal, val1.MgNumVal, 3);
      }

      private void eval_op_second(ExpVal resVal, ExpVal val1)
      {
         eval_op_time_brk(resVal, val1.MgNumVal, 2);
      }

      private void eval_op_minute(ExpVal resVal, ExpVal val1)
      {
         eval_op_time_brk(resVal, val1.MgNumVal, 1);
      }

      private void eval_op_hour(ExpVal resVal, ExpVal val1)
      {
         eval_op_time_brk(resVal, val1.MgNumVal, 0);
      }

      protected internal void eval_op_date_brk(ExpVal resVal, NUM_TYPE val1, int typ)
      {
         if (val1 == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int d = val1.NUM_2_LONG();
         //-----------------------------------------------------------------------
         // Break data into its components
         //-----------------------------------------------------------------------
         DisplayConvertor.DateBreakParams breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
         DisplayConvertor.Instance.date_break_datemode(breakParams, d, false, ((Task)ExpTask).getCompIdx());
         int year = breakParams.year;
         int month = breakParams.month;
         int day = breakParams.day;
         int doy = breakParams.doy;
         int dow = breakParams.dow;
         switch (typ)
         {
            case 0:
               d = day;
               break;

            case 1:
               d = month;
               break;

            case 2:
               d = year;
               break;

            case 3:
               d = dow;
               break;

            case 4:
               d = UtilDateJpn.getInstance().date_jpn_year_ofs(year, doy);
               break;

            default:
               d = 0;
               break;
         }
         ConstructMagicNum(resVal, d, StorageAttribute.NUMERIC);
      }

      private void eval_op_time_brk(ExpVal resVal, NUM_TYPE val1, int typ)
      {
         if (val1 == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int d = val1.NUM_2_ULONG();
         //-----------------------------------------------------------------------
         // Breake data into its components
         //-----------------------------------------------------------------------
         DisplayConvertor.TimeBreakParams breakParams = DisplayConvertor.Instance.getNewTimeBreakParams();
         DisplayConvertor.time_break(breakParams, d);
         int hour = breakParams.hour;
         int minute = breakParams.minute;
         int second = breakParams.second;
         switch (typ)
         {
            case 0:
               d = hour;
               break;

            case 1:
               d = minute;
               break;

            case 2:
               d = second;
               break;

            default:
               d = 0;
               break;
         }
         ConstructMagicNum(resVal, d, StorageAttribute.NUMERIC);
      }

      private void eval_op_addDateTime(ExpVal resVal, ExpVal dateVal, ExpVal timeVal, ExpVal yearsVal,
                                       ExpVal monthsVal, ExpVal daysVal, ExpVal hoursVal, ExpVal minutesVal,
                                       ExpVal secondsVal)
      {
         var tmpVal = new ExpVal();

         // values check
         if (dateVal.MgNumVal == null || timeVal.MgNumVal == null)
         {
            resVal.Attr = StorageAttribute.BOOLEAN;
            resVal.BoolVal = false;
            return;
         }

         // get the time field
         Field fldTime = GetFieldOfContextTask(timeVal.MgNumVal.NUM_2_LONG());
         SetVal(tmpVal, fldTime.getType(), fldTime.getValue(true), null);

         // Add time
         eval_op_addtime(resVal, tmpVal, hoursVal, minutesVal, secondsVal);

         int time = resVal.MgNumVal.NUM_2_LONG();
         int date = time / (60 * 60 * 24);
         time = time % (60 * 60 * 24); // if diff is more then 24 hours, remove the full date changes
         // If time is negative, move back a day and change the time
         if (time < 0)
         {
            date--;
            time = (60 * 60 * 24) - (-time);
         }

         // Get the date field
         Field fldDate = GetFieldOfContextTask(dateVal.MgNumVal.NUM_2_LONG());
         SetVal(tmpVal, fldDate.getType(), fldDate.getValue(true), null);

         // Add the date
         eval_op_adddate(resVal, tmpVal, yearsVal, monthsVal, daysVal);
         // Add the result date to the date diff calculated earlier
         date += resVal.MgNumVal.NUM_2_LONG();

         // Set new time and date in fields
         tmpVal.MgNumVal.NUM_4_LONG(time);
         fldTime.setValueAndStartRecompute(tmpVal.ToMgVal(), false, true, false, false);
         fldTime.updateDisplay();

         tmpVal.MgNumVal.NUM_4_LONG(date);
         fldDate.setValueAndStartRecompute(tmpVal.ToMgVal(), false, true, false, false);
         fldDate.updateDisplay();

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = true;
      }

      private void eval_op_difdt(ExpVal resVal, ExpVal date1val, ExpVal time1val, ExpVal date2val,
                                 ExpVal time2val, ExpVal difDateVal, ExpVal difTimeVal)
      {
         var tmpVal = new ExpVal()
         {
            MgNumVal = new NUM_TYPE(),
            Attr = StorageAttribute.NUMERIC
         };

         // values check
         if (difDateVal.MgNumVal == null || difTimeVal.MgNumVal == null)
         {
            resVal.Attr = StorageAttribute.BOOLEAN;
            resVal.BoolVal = false;
            return;
         }

         // Calculate time difs
         int difDate = date1val.MgNumVal.NUM_2_LONG() - date2val.MgNumVal.NUM_2_LONG();
         int difTime = time1val.MgNumVal.NUM_2_LONG() - time2val.MgNumVal.NUM_2_LONG();

         if ((difTime < 0) && (difTime > -86400) && (difDate > 0))
         {
            difDate--;
            difTime = 86400 - (-difTime);
         }

         // get the fields
         Field fldDate = GetFieldOfContextTask(difDateVal.MgNumVal.NUM_2_LONG());
         Field fldTime = GetFieldOfContextTask(difTimeVal.MgNumVal.NUM_2_LONG());

         // Set the dif values
         tmpVal.MgNumVal.NUM_4_LONG(difDate);
         fldDate.setValueAndStartRecompute(tmpVal.ToMgVal(), false, true, false, false);
         fldDate.updateDisplay();

         tmpVal.MgNumVal.NUM_4_LONG(difTime);
         fldTime.setValueAndStartRecompute(tmpVal.ToMgVal(), false, true, false, false);
         fldTime.updateDisplay();

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = true;
      }

      private void eval_op_ndow(ExpVal resVal, ExpVal val1, DisplayConvertor displayConvertor)
      {
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }
         val1.MgNumVal = mul_add(val1.MgNumVal, 0, 6);
         eval_op_cdow(resVal, val1.MgNumVal, displayConvertor);
      }

      private void eval_op_nmonth(ExpVal resVal, ExpVal val1, DisplayConvertor displayConvertor)
      {
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }
         val1.MgNumVal = mul_add(val1.MgNumVal, 31, -30);
         eval_op_cmonth(resVal, val1.MgNumVal, displayConvertor);
      }

      /// <summary>
      ///   Multiply & add to numeric type
      /// </summary>
      protected internal NUM_TYPE mul_add(NUM_TYPE num, int mul, int add)
      {
         var tmp = new NUM_TYPE();

         if (num == null)
            return null;

         if (mul != 0)
         {
            tmp.NUM_4_LONG(mul);
            num = NUM_TYPE.mul(num, tmp);
         }
         if (add != 0)
         {
            tmp.NUM_4_LONG(add);
            num = NUM_TYPE.add(num, tmp);
         }
         return num;
      }

      private void eval_op_cdow(ExpVal resVal, NUM_TYPE val1, DisplayConvertor displayConvertor)
      {
         eval_op_date_str(resVal, val1, "WWWWWWWWWWT", displayConvertor);
      }

      private void eval_op_cmonth(ExpVal resVal, NUM_TYPE val1, DisplayConvertor displayConvertor)
      {
         eval_op_date_str(resVal, val1, "MMMMMMMMMMT", displayConvertor);
      }

      protected internal void eval_op_date_str(ExpVal resVal, NUM_TYPE val1, String format,
                                               DisplayConvertor displayConvertor)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         if (val1 == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         int dow = val1.NUM_2_ULONG();
         resVal.StrVal = displayConvertor.to_a(resVal.StrVal, 10, dow, format, ((Task)ExpTask).getCompIdx());
      }

      /// <summary>
      /// </summary>
      private void eval_op_delay(ExpVal val1)
      {
         if (val1.MgNumVal == null)
            return;
         int delay = val1.MgNumVal.NUM_2_ULONG() * 100;
         if (delay > 0)
         {
            Commands.beginInvoke();

            Object delayObject = Manager.GetDelayWait();
            ClientManager.Instance.setDelayInProgress(true);
#if !PocketPC
            // In case, GUI Commands are pending and if worker thread enter into  Monitor.Wait(), Worker Thread 
            // comes out without waiting for 'delay' duration due to GUI Events like Move/Resize (as checkAndStopDelay() 
            // locks the delay object again) . So, in order to finish the pending GUI commands, wait for 100 miliseconds
            // and substract the same from actual delay period.
            if (delay > ConstInterface.GUI_COMMAND_EXEC_WAIT_TIME)
            {
               Thread.Sleep(ConstInterface.GUI_COMMAND_EXEC_WAIT_TIME);
               delay -= ConstInterface.GUI_COMMAND_EXEC_WAIT_TIME;
            }
#endif
            Monitor.Enter(delayObject);
            try
            {
               // wait delay miliseconds
               Monitor.Wait(delayObject, TimeSpan.FromMilliseconds(delay));
            }
            catch (ThreadInterruptedException)
            {
            }
            finally
            {
               Monitor.Exit(delayObject);
            }
            ClientManager.Instance.setDelayInProgress(false);
         }
      }

      private void eval_op_idle(ExpVal resVal)
      {
         int n = 0;
         int idleTime = ClientManager.Instance.getEnvironment().getIdleTime(((Task)ExpTask).getCompIdx());
         if (idleTime > 0)
         {
            long CurrTimeMilli = Misc.getSystemMilliseconds();
            // act_idle ():
            n = ((int)(CurrTimeMilli - ClientManager.Instance.LastActionTime) / 1000) / idleTime;
         }
         resVal.MgNumVal = new NUM_TYPE();
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal.NUM_4_LONG(n * idleTime * 10);
      }

      private void eval_op_adddate(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3, ExpVal val4)
      {
         int tries;

         if (val1.MgNumVal == null || val2.MgNumVal == null || val3.MgNumVal == null || val4.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.DATE);
            return;
         }

         int date = val1.MgNumVal.NUM_2_LONG();
         //-----------------------------------------------------------------------
         // Breake data into its components
         //-----------------------------------------------------------------------
         DisplayConvertor.DateBreakParams breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
         DisplayConvertor.Instance.date_break_datemode(breakParams, date, false, ((Task)ExpTask).getCompIdx());
         int year = breakParams.year;
         int month = breakParams.month;
         int day = breakParams.day;

         /* 20.7.95 - Hen : Fix bug 1975 */
         if (ClientManager.Instance.getEnvironment().GetDateMode(((Task)ExpTask).getCompIdx()) == 'B')
            year = Math.Max(year - PICInterface.DATE_BUDDHIST_GAP, 0);
         year += val2.MgNumVal.NUM_2_LONG();
         month += val3.MgNumVal.NUM_2_LONG();
         int day1 = val4.MgNumVal.NUM_2_LONG();
         int add_day = (day == 0 && year != 0 && month != 0 && day1 != 0)
                       ? 1
                       : 0;
         int month1 = month + year * 12;
         year = (month1 - 1) / 12;
         month = (month1 - 1) % 12 + 1;
         for (tries = 0; tries < 4; tries++)
         {
            date = DisplayConvertor.Instance.date_4_calender(year, month, day + add_day, 0, false);
            if (date < 1000000000)
               break;
            day--;
         }

         /* FMI-264, 99/01/08, JPNID: MKP99010008                    */
         /* date_4_calender() returns 1000000000L as an invalid date */
         /* in which case date should be 0000/00/00.                 */
         if (date == 1000000000)
            date = 0;
         else
         {
            date += day1 - add_day;
            date = Math.Max(date, 0);
         }
         ConstructMagicNum(resVal, date, StorageAttribute.DATE);
      }

      private void eval_op_addtime(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3, ExpVal val4)
      {
         if (val1.MgNumVal == null || val2.MgNumVal == null || val3.MgNumVal == null || val4.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.TIME);
            return;
         }
         int time = val1.MgNumVal.NUM_2_ULONG() + val2.MgNumVal.NUM_2_LONG() * 3600 + val3.MgNumVal.NUM_2_LONG() * 60 +
                    val4.MgNumVal.NUM_2_LONG();
         ConstructMagicNum(resVal, time, StorageAttribute.TIME);
      }

      private void eval_op_bom(ExpVal resVal, ExpVal val1)
      {
         DisplayConvertor.DateBreakParams breakParams;
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.DATE);
            return;
         }
         int date = val1.MgNumVal.NUM_2_ULONG();
         if (date != 0)
         {
            breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
            DisplayConvertor.Instance.date_break_datemode(breakParams, date, false, ((Task)ExpTask).getCompIdx());
            int day = breakParams.day;
            date -= (day - 1);
         }
         ConstructMagicNum(resVal, date, StorageAttribute.DATE);
      }

      private void eval_op_boy(ExpVal resVal, ExpVal val1)
      {
         DisplayConvertor.DateBreakParams breakParams;
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.DATE);
            return;
         }
         int date = val1.MgNumVal.NUM_2_ULONG();
         if (date != 0)
         {
            breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
            DisplayConvertor.Instance.date_break_datemode(breakParams, date, false, ((Task)ExpTask).getCompIdx());
            int year = breakParams.year;
            const int day = 1;
            const int month = 1;
            if (ClientManager.Instance.getEnvironment().GetDateMode(((Task)ExpTask).getCompIdx()) == 'B')
               year = Math.Max(year - PICInterface.DATE_BUDDHIST_GAP, 0);
            date = DisplayConvertor.Instance.date_4_calender(year, month, day, 0, false);
         }
         ConstructMagicNum(resVal, date, StorageAttribute.DATE);
      }

      private void eval_op_eom(ExpVal resVal, ExpVal val1)
      {
         DisplayConvertor.DateBreakParams breakParams;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.DATE);
            return;
         }
         int date = val1.MgNumVal.NUM_2_ULONG();
         if (date != 0)
         {
            breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
            DisplayConvertor.Instance.date_break_datemode(breakParams, date, false, ((Task)ExpTask).getCompIdx());
            int year = breakParams.year;
            int month = breakParams.month;
            int day = 31;
            if (ClientManager.Instance.getEnvironment().GetDateMode(((Task)ExpTask).getCompIdx()) == 'B')
               year = Math.Max(year - PICInterface.DATE_BUDDHIST_GAP, 0);
            int tries;
            for (tries = 0; tries < 4; tries++)
            {
               date = DisplayConvertor.Instance.date_4_calender(year, month, day, 0, false);
               if (date < 1000000000)
                  break;
               day--;
            }
         }
         ConstructMagicNum(resVal, date, StorageAttribute.DATE);
      }

      private void eval_op_eoy(ExpVal resVal, ExpVal val1)
      {
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.DATE);
            return;
         }
         int date = val1.MgNumVal.NUM_2_ULONG();
         if (date != 0)
         {
            DisplayConvertor.DateBreakParams breakParams = DisplayConvertor.Instance.getNewDateBreakParams();
            DisplayConvertor.Instance.date_break_datemode(breakParams, date, false, ((Task)ExpTask).getCompIdx());
            int year = breakParams.year;
            const int month = 12;
            const int day = 31;
            if (ClientManager.Instance.getEnvironment().GetDateMode(((Task)ExpTask).getCompIdx()) == 'B')
               year = Math.Max(year - PICInterface.DATE_BUDDHIST_GAP, 0);
            date = DisplayConvertor.Instance.date_4_calender(year, month, day, 0, false);
         }
         ConstructMagicNum(resVal, date, StorageAttribute.DATE);
      }

      private void eval_op_strtok(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3)
      {
         StringBuilder tmp_s;
         String tmp_str;
         int idx;
         String delim;
         String ret_str = "";

         if (val1.StrVal == null || val2.MgNumVal == null || val3.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
         }

         if (!String.IsNullOrEmpty(val1.StrVal) && val3.StrVal.Length > 0)
         {
            tmp_s = new StringBuilder(val1.StrVal.Length + 2);

            idx = val2.MgNumVal.NUM_2_LONG();
            if (idx > 0)
            {
               delim = val3.StrVal.TrimEnd(_charsToTrim);
               tmp_s.Append(val1.StrVal);
               if (delim.Length == 0)
               {
                  if (idx == 1)
                     ret_str = tmp_s.ToString();
               }
               else
               {
                  tmp_str = tmp_s.ToString();
                  int i;
                  for (i = 0; i < idx; i++)
                  {
                     ret_str = StrUtil.strstr(tmp_str, delim);
                     if (ret_str == null)
                     {
                        if (i == idx - 1)
                           ret_str = tmp_str;
                        break;
                     }
                     ret_str = tmp_str.Substring(0, tmp_str.Length - ret_str.Length);
                     tmp_str = tmp_str.Substring(ret_str.Length + delim.Length);
                  }
               }
            }
         }
         if (ret_str != null)
         {
            resVal.Attr = StorageAttribute.UNICODE;
            resVal.StrVal = ret_str;
         }
         else
         {
            resVal.Attr = StorageAttribute.UNICODE;
            resVal.StrVal = "";
         }

         idx = val2.MgNumVal.NUM_2_LONG();
         if (!String.IsNullOrEmpty(val1.StrVal) && val3.StrVal.Length == 0 && idx == 1)
         {
            resVal.Attr = StorageAttribute.UNICODE;
            resVal.StrVal = val1.StrVal;
         }
      }

      private void eval_op_dbround(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         if (val1.MgNumVal == null || val2.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }
         resVal.MgNumVal = new NUM_TYPE(val1.MgNumVal);
         int whole = val2.MgNumVal.NUM_2_LONG();

         if (whole < 0)
            resVal.MgNumVal.dbRound(-whole);
         else
            resVal.MgNumVal.round(whole);
         resVal.Attr = StorageAttribute.NUMERIC;
      }

      private void eval_op_varpic(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         Field fld;
         MgControl ctrl;

         //validation checks on the expression arguments
         if (val2.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }

         int mode = val2.MgNumVal.NUM_2_LONG();
         int itm = val1.MgNumVal.NUM_2_LONG();
         resVal.Attr = StorageAttribute.UNICODE;

         try
         {
            fld = GetFieldOfContextTask(itm); // itm starts from A -> 1, but our array starts from 0 -> (itm-1)
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
            //makeNullString
            fld = null;
         }

         if (fld == null)
         {
            // val_pre_s (0) in Magic
            Logger.Instance.WriteExceptionToLog("ExpressionEvaluator.eval_op_varpic there is no control number " + itm);
            resVal.StrVal = "";
            return;
         }

         if (mode != 0)
         {
            //gets the control associated with the field
            ctrl = fld.getCtrl();

            if (ctrl != null)
            {
               // we dont need to check if the control format is an expression since
               // the PIC object always keep an up to date value of the expression resulte
               resVal.StrVal = ctrl.getPIC().getFormat();
               return;
            }
         }

         if (fld.getType() != StorageAttribute.BLOB && fld.getType() != StorageAttribute.BLOB_VECTOR)
         {
            resVal.StrVal = fld.getPicture();
            return;
         }
         resVal.StrVal = "";
         return;
      }

      private void eval_op_varattr(ExpVal resVal, ExpVal val1)
      {
         Field fld;

         resVal.Attr = StorageAttribute.ALPHA;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }

         int itm = val1.MgNumVal.NUM_2_LONG();
         try
         {
            fld = GetFieldOfContextTask(itm); // itm starts from A -> 1, but our array starts from 0 -> (itm-1)
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
            //makeNullString
            fld = null;
         }

         if (fld == null)
         {
            // val_pre_s (0) in Magic
            Logger.Instance.WriteExceptionToLog("ExpressionEvaluator.eval_op_varattr there is no control number " + itm);
            resVal.StrVal = "";
            return;
         }
         StorageAttribute attr = fld.getType();

         resVal.StrVal = "" + GetAttributeChar(attr);
      }

      private void exp_null_val_get(StorageAttribute exp_attr, int opcode, ExpVal null_parm)
      {
         String ptr = "";
         int num_val = 0;

         null_parm.IsNull = true;

         // If the The Attribute is Unknown - use the expected Attribute:
         if (opcode == ExpressionInterface.EXP_OP_NULL)
         {
            null_parm.Attr = exp_attr;
            if (exp_attr == StorageAttribute.ALPHA)
               ptr = "";

            switch (exp_attr)
            {
               case StorageAttribute.ALPHA:
               case StorageAttribute.BLOB:
               case StorageAttribute.BLOB_VECTOR:
               case StorageAttribute.MEMO:
                  opcode = ExpressionInterface.EXP_OP_NULL_A;
                  break;

               case StorageAttribute.NUMERIC:
                  opcode = ExpressionInterface.EXP_OP_NULL_N;
                  break;

               case StorageAttribute.BOOLEAN:
                  opcode = ExpressionInterface.EXP_OP_NULL_B;
                  break;

               case StorageAttribute.DATE:
                  opcode = ExpressionInterface.EXP_OP_NULL_D;
                  break;

               case StorageAttribute.TIME:
                  opcode = ExpressionInterface.EXP_OP_NULL_T;
                  break;

               case StorageAttribute.UNICODE:
                  opcode = ExpressionInterface.EXP_OP_NULL_U;
                  break;
            }
         }

         switch (opcode)
         {
            case ExpressionInterface.EXP_OP_NULL:
            case ExpressionInterface.EXP_OP_NULL_A:
               if (exp_attr == StorageAttribute.DOTNET)
                  null_parm.Attr = StorageAttribute.DOTNET;
               else if (exp_attr != StorageAttribute.BLOB &&
                        exp_attr != StorageAttribute.BLOB_VECTOR)
                  null_parm.Attr = StorageAttribute.ALPHA;
               ptr = "";
               break;

            case ExpressionInterface.EXP_OP_NULL_N:
               null_parm.Attr = StorageAttribute.NUMERIC;
               break;

            case ExpressionInterface.EXP_OP_NULL_B:
               null_parm.Attr = StorageAttribute.BOOLEAN;
               num_val = 0;
               break;

            case ExpressionInterface.EXP_OP_NULL_D:
               null_parm.Attr = StorageAttribute.DATE;
               num_val = Int32.Parse(PICInterface.DEFAULT_DATE);
               break;

            case ExpressionInterface.EXP_OP_NULL_T:
               null_parm.Attr = StorageAttribute.TIME;
               num_val = Int32.Parse(PICInterface.DEFAULT_TIME);
               break;

            case ExpressionInterface.EXP_OP_NULL_U:
               if (exp_attr != StorageAttribute.BLOB &&
                   exp_attr != StorageAttribute.BLOB_VECTOR)
                  null_parm.Attr = StorageAttribute.UNICODE;
               ptr = "";
               break;
            case ExpressionInterface.EXP_OP_NULL_O:
               null_parm.Attr = StorageAttribute.BLOB;
               break;
         }
         switch (null_parm.Attr)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
               null_parm.StrVal = ptr;
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               if (opcode == ExpressionInterface.EXP_OP_NULL_N)
               {
                  // init NULL Numeric number
                  var num_value = new sbyte[NUM_TYPE.NUM_SIZE];
                  for (int i = 0; i < num_value.Length; i++)
                     num_value[i] = 0;
                  null_parm.MgNumVal = new NUM_TYPE(num_value);
               }
               // Time & Data
               else
                  ConstructMagicNum(null_parm, num_val, null_parm.Attr);
               break;

            case StorageAttribute.UNICODE:
               null_parm.StrVal = ptr;
               break;

            case StorageAttribute.BOOLEAN:
               null_parm.BoolVal = false; // false is default 4 boolean
               break;

            case StorageAttribute.DOTNET:
               null_parm.DnMemberInfo = new DNMemberInfo(); // value as null
               break;
         }
      }

      private void exp_get_var(ExpVal resVal, ExpVal val1, bool is_previous)
      {
         Field fld = null;

         // evaluate fld from mgNumVal
         if (val1.MgNumVal != null)
         {
            int itm = val1.MgNumVal.NUM_2_LONG();
            fld = GetFieldOfContextTask(itm);
         }

         // if fld is null, set resVal to null and return
         if (fld == null)
         {
            SetNULL(resVal, StorageAttribute.NONE);
            return;
         }

         if (is_previous)
            // set the flag that will be used by fld.getValue
            ((Task)fld.getTask()).setEvalOldValues(true);

         // first get the value of the field and set it to the result.
         SetVal(resVal, fld.getType(), fld.getValue(true), null);

         // now for the null indication.
         if (is_previous)
         {
            // for previous, the null indication is not on the fld, we should get it from the original record.
            if (fld.isOriginalValueNull())
               //the original value of the record is NULL
               SetNULL(resVal, StorageAttribute.NONE);
         }
         // for current value, the null indication is already on the fld.
         else
            resVal.IsNull = fld.isNull();

         if (is_previous)
            // reset the flag.
            ((Task)fld.getTask()).setEvalOldValues(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void eval_op_varmod(ExpVal resVal, ExpVal val1)
      {
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.BOOLEAN);
            return;
         }
         int itm = val1.MgNumVal.NUM_2_LONG();
         Field fld = GetFieldOfContextTask(itm);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         //QCR 752270 if the the arguments does not represent a valid field return false
         if (fld != null)
         {
            int idx = fld.getId();
            DataView dv = (DataView)((Task)fld.getTask()).DataView;
            Record rec = (Record)dv.getCurrRec();
            resVal.BoolVal = !rec.fldValsEqual(dv.getOriginalRec(), idx);
         }
      }

      private void eval_op_varinp(ExpVal resVal, ExpVal val1)
      {
         int i = 0;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int expkern_parent = val1.MgNumVal.NUM_2_LONG();
         if ((expkern_parent >= 0 && expkern_parent < ((Task)ExpTask).getTaskDepth(false)) || expkern_parent == TRIGGER_TASK)
         {
            Task tsk = (Task)GetContextTask(expkern_parent);
            if (tsk != null)
               i = tsk.ctl_itm_4_parent_vee(0, tsk.getCurrFieldIdx() + 1);
         }
         ConstructMagicNum(resVal, i, StorageAttribute.NUMERIC);
      }

      private void eval_op_varname(ExpVal resVal, ExpVal val1)
      {
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }

         int itm = val1.MgNumVal.NUM_2_LONG();
         Field fld = GetFieldOfContextTask(itm);
         string buffer = fld != null
                         ? fld.getName()
                         : "";
         resVal.StrVal = buffer;
         resVal.Attr = StorageAttribute.ALPHA;
      }

      private void eval_op_VarDisplayName(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = "";

         if (val1.MgNumVal != null)
         {
            int itm = val1.MgNumVal.NUM_2_LONG();
            Field fld = GetFieldOfContextTask(itm);

            if (fld != null)
               resVal.StrVal = fld.VarDisplayName;
         }
      }

      /// <summary>
      /// Return the control ID of the control to which item (Data property literal) is attached.
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void eval_op_VarControlID(ExpVal resVal, ExpVal val1)
      {
         int ret = 0;

         if (val1.MgNumVal != null)
         {
            int itm = val1.MgNumVal.NUM_2_LONG();

            //if itm is trigger task, calculate the item from field.
            if (itm == TRIGGER_TASK)
               itm = ((Task)ExpTask.GetContextTask()).ctl_itm_4_parent_vee(0, ((Task)ExpTask.GetContextTask()).getCurrFieldIdx() + 1);

            ret = ((Task)ExpTask.GetContextTask()).GetControlIDFromVarItem(itm - 1);
         }

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG(ret);
      }

      /// <summary>
      /// Return the choice control's items list according to control ID.
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void eval_op_ControlItemsList(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = "";

         if (val1.MgNumVal != null)
         {
            int controlID = val1.MgNumVal.NUM_2_LONG();
            int parent = 0;
            MgControl mgControl = ((Task)ExpTask.GetContextTask()).GetControlFromControlID(controlID - 1, out parent);

            if (mgControl != null && mgControl.isChoiceControl())
               resVal.StrVal = mgControl.getForm().GetChoiceControlItemList(mgControl);
         }
      }

      /// <summary>
      /// Return the choice control's display list according to control ID.
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void eval_op_ControlDisplayList(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = "";

         if (val1.MgNumVal != null)
         {
            int controlID = val1.MgNumVal.NUM_2_LONG();
            int parent = 0;
            MgControl mgControl = ((Task)ExpTask.GetContextTask()).GetControlFromControlID(controlID - 1, out parent);

            if (mgControl != null && mgControl.isChoiceControl())
               resVal.StrVal = mgControl.getForm().GetChoiceControlDisplayList(mgControl);
         }
      }

      private void eval_op_viewmod(ExpVal resVal, ExpVal val1)
      {
         Task tsk;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.BOOLEAN);
            return;
         }
         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            if (!tsk.isMainProg())
            {
               resVal.BoolVal = ((DataView)tsk.DataView).getCurrRec().Modified;
               // record might be modified even if the flag is false (like when there is an update with force up = no
               // in the server the check is comparing all the fields in the view.
               // Here, if the modified is false, we will compare the current rec to the original current rec.
               if (!resVal.BoolVal)
               {
                  Record CurrRec = ((DataView)tsk.DataView).getCurrRec();
                  Record OriginalRec = ((DataView)tsk.DataView).getOriginalRec();
                  // compare fields between current record and original record. compare only fields that are part of data view.
                  resVal.BoolVal = !CurrRec.isSameRecData(OriginalRec, true, true);
               }
            }
            else
               resVal.BoolVal = false;
         }
         else
            resVal.BoolVal = false;
         resVal.Attr = StorageAttribute.BOOLEAN;
      }

      private void eval_op_level(ExpVal resVal, ExpVal val1)
      {
         Task tsk;
         String outstr = "";

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            outstr = tsk.getBrkLevel();

            /*
            * Check the level of the generation task.
            * A. If it is not RM, return the handler text.
            * B. If it is RM, then we have 3 different situations:
            * 1. If the generation task is Main Program, return "MP".
            * 2. If the generation task is a task whose immediate called task (in the runtime task tree) is a SubForm, return "SUBFORM".
            * 3. In any other case, return the handler text.
            */
            if (parent != TRIGGER_TASK && tsk != (Task)ExpTask)
            {
               if (outstr.ToUpper().Equals(ConstInterface.BRK_LEVEL_REC_MAIN.ToUpper()))
               {
                  if (tsk.isMainProg())
                     outstr = ConstInterface.BRK_LEVEL_MAIN_PROG;
                  else
                  {
                     var tskTree = new Task[((Task)ExpTask).getTaskDepth(false)];
                     ((Task)ExpTask).pathToRoot(tskTree, false);

                     if (parent > 0 && tskTree[parent - 1].isSubFormUnderFrameSet())
                        outstr = ConstInterface.BRK_LEVEL_FRAME;
                     else if (parent > 0 && tskTree[parent - 1].IsSubForm)
                        outstr = ConstInterface.BRK_LEVEL_SUBFORM;
                  }
               }
            }
         }
         resVal.StrVal = outstr;
         resVal.Attr = StorageAttribute.ALPHA;
      }

      // get the task's counter (by task generation).
      private void eval_op_counter(ExpVal resVal, ExpVal val1)
      {
         Task tsk;
         long ret = 0;

         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            ret = (tsk == null
                     ? 0
                     : tsk.getCounter());
         }
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG((int)ret);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void eval_op_emptyDataview(ExpVal resVal, ExpVal val1)
      {
         Task tsk;
         bool ret = false;

         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            if (tsk != null && tsk.DataView.isEmptyDataview())
               ret = true;
         }
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = ret;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void eval_op_mainlevel(ExpVal resVal, ExpVal val1)
      {
         Task tsk;
         String outstr = "";

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            outstr = tsk.getMainLevel();
         }
         resVal.StrVal = outstr;
         resVal.Attr = StorageAttribute.ALPHA;
      }

      private void eval_op_maindisplay(ExpVal resVal, ExpVal val1)
      {
         int mainDspIdx = 0;
         Task tsk;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            mainDspIdx = tsk.getProp(PropInterface.PROP_TYPE_MAIN_DISPLAY).getValueInt();
         }

         ConstructMagicNum(resVal, mainDspIdx, StorageAttribute.NUMERIC);
      }

      private void eval_op_IsFirstRecordCycle(ExpVal resVal, ExpVal val1)
      {
         Task tsk;
         bool isFirstRecCycle = false;

         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.ALPHA);
            return;
         }
         int parent = val1.MgNumVal.NUM_2_LONG();
         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            tsk = (Task)GetContextTask(parent);
            isFirstRecCycle = tsk.isFirstRecordCycle();
         }

         resVal.BoolVal = isFirstRecCycle;
         resVal.Attr = StorageAttribute.BOOLEAN;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      private void exp_is_default(ExpVal resVal, ExpVal val1)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         int itm = val1.MgNumVal.NUM_2_LONG();
         // itm starts from A->1 but the array starts from 0 -> itm-1
         Field fld = ((Task)ExpTask).ctl_itm_2_parent_vee(itm - 1);
         if (fld == null)
            return;

         //QCR 928308 - for vectors as vectors ( not per cell of the vector) the Behavior is the same as blobs
         // that is null is allowed and null is default
         if (fld.isNull() && (fld.isNullDefault() || fld.getType() == StorageAttribute.BLOB_VECTOR))
            resVal.BoolVal = true;

         string val = fld.getValue(false);
         string defVal = fld.getDefaultValue();
         StorageAttribute type = fld.getType();

         resVal.BoolVal = mgValsEqual(val, fld.isNull(), type, defVal, fld.isNullDefault(), type);
         // value not equivalent to default value function should return FALSE
      }

      private void eval_op_curr_row(ExpVal resVal, ExpVal val1)
      {
         Task Tsk;
         int Result = 0;
         MgForm Form;
         if (val1.MgNumVal == null)
         {
            SetNULL(resVal, StorageAttribute.NUMERIC);
            return;
         }

         int Parent = val1.MgNumVal.NUM_2_LONG();
         if ((Parent >= 0 && Parent < ((Task)ExpTask).getTaskDepth(false)) || Parent == TRIGGER_TASK)
         {
            Tsk = (Task)GetContextTask(Parent);
            if (Tsk != null && !Tsk.isMainProg())
            //there is no form & row in main program
            {
               Form = (MgForm)Tsk.getForm();

               if (Form.getTableCtrl() != null)
               {
                  if (((DataView)Tsk.DataView).getCurrRec().InCompute)
                     Result = Form.getDestinationRow() + 1;
                  // inner numbering starts from 0, but for user it
                  // starts from 1 -> +1
                  else
                  {
                     Form.getTopIndexFromGUI();
                     Result = Form.getVisibleLine() + 1; // inner numbering starts from 0, but for user it starts
                     // from 1 -> +1
                  }

                  if (Result < 0 || Result > Form.getRowsInPage() + 1)
                     Result = 0;
               }
            }
         }

         ConstructMagicNum(resVal, Result, StorageAttribute.NUMERIC);
      }

      private void eval_op_user(ExpVal resVal, ExpVal numVal)
      {
         int option = numVal.MgNumVal.NUM_2_LONG();
         resVal.Attr = StorageAttribute.ALPHA;

         switch (option)
         {
            case 0:
               resVal.StrVal = UserDetails.Instance.UserID;
               break;
            case 1:
               resVal.StrVal = UserDetails.Instance.UserName;
               break;
            case 2:
               resVal.StrVal = UserDetails.Instance.UserInfo;
               break;
         }
      }

      private void eval_op_appname(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = ClientManager.Instance.getAppName();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_prog(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = ((Task)ExpTask).queryTaskPath().ToString();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_this(ExpVal resVal)
      {
         Task triggerTask = (Task)ExpTask;
         int Result = 0;

         //EXPKERN_parent = EXPKERN_vee = 0;

         if (triggerTask != null)
         {
            //EXPKERN_parent = TRIGGER_TASK;
            Result = TRIGGER_TASK;
         }
         ConstructMagicNum(resVal, Result, StorageAttribute.NUMERIC);
      }

      /// <summary>
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "Parent"></param>
      /// <param name = "Modes">MODE literal given as mode parameter</param>
      private void eval_op_stat(ExpVal resVal, ExpVal Parent, ExpVal Modes)
      {
         Task Tsk;
         bool Ret = false;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            Tsk = (Task)GetContextTask(iParent);
            if (Tsk != null)
            {
               char tskMode = Char.ToUpper(Tsk.getMode());
               int i;
               for (i = 0; i < Modes.StrVal.Length; i++)
               {
                  char mode = Char.ToUpper(Modes.StrVal[i]);
                  char code = cst_code_trans_buf('I', "MCDEPLRKSONB", mode, MsgInterface.EXPTAB_TSK_MODE_RT);

                  // code not found, it might be English (same as in online).
                  if (code == '\0')
                  {
                     code = Char.ToUpper(mode);
                     if (code == 'Q')
                        code = 'E';
                  }

                  if (code == tskMode)
                  {
                     Ret = true;
                     break;
                  }
               }
            }
         }

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = Ret;
      }

      /// <summary>
      /// Returns subform execution mode for the task with the corresponding generation
      /// -1 ・The task is not executed as a subform
      ///  0 ・The task is executed by setting the focus on it
      ///  1 ・The subtask is executed for the first time
      ///  2 ・The task is executed because the Automatic Refresh property or the Subform Refresh event has been 
      ///      triggered.
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="generation"></param>
      private void eval_op_subformExecMode(ExpVal resVal, ExpVal generation)
      {
         Task task;
         SubformExecModeEnum subformExecMode = SubformExecModeEnum.NO_SUBFORM;

         int iParent = generation.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
            {
               if (task.IsSubForm)
                  subformExecMode = task.SubformExecMode;
            }
         }
         ConstructMagicNum(resVal, (int)subformExecMode, StorageAttribute.NUMERIC);
      }

      private void eval_op_varset(ExpVal resVal, ExpVal val, ExpVal num)
      {
         //init returned value
         resVal.BoolVal = true;
         resVal.Attr = StorageAttribute.BOOLEAN;

         if (num.MgNumVal == null)
         {
            //the varset function always returns true.
            //SetNULL(resVal,STORAGE_ATTR_BOOLEAN);
            return;
         }

         int itm = num.MgNumVal.NUM_2_LONG();

         Field fld = GetFieldOfContextTask(itm);

         if (fld == null)
         {
            resVal.BoolVal = false;
            return;
         }

         if (StorageAttributeCheck.StorageFldAlphaUnicodeOrBlob(fld.getType(), val.Attr))
            ConvertExpVal(val, fld.getType());

         String bufptr;
         if (StorageAttributeCheck.isTheSameType(fld.getType(), val.Attr))
         {
            switch (fld.getType())
            {
               case StorageAttribute.ALPHA:
               case StorageAttribute.UNICODE:
                  bufptr = val.StrVal;
                  break;

               case StorageAttribute.NUMERIC:
               case StorageAttribute.DATE:
               case StorageAttribute.TIME:
                  bufptr = val.MgNumVal.toXMLrecord();
                  break;

               case StorageAttribute.BOOLEAN:
                  bufptr = val.BoolVal
                           ? "1"
                           : "0";
                  break;

               case StorageAttribute.BLOB:
                  bufptr = val.ToMgVal();
                  break;

               case StorageAttribute.BLOB_VECTOR:
                  bufptr = val.ToMgVal();

                  //QCR 4486682
                  if (!val.IsNull)
                  {
                     //check for valid vector in the blob
                     if (val.Attr == StorageAttribute.BLOB)
                        if (!VectorType.validateBlobContents(val.StrVal))
                           bufptr = null;

                     if (bufptr != null)
                        bufptr = Operation.operUpdateVectors(fld, bufptr);

                     if (bufptr != null)
                        break;
                  }
                  goto default;

               default:
                  //cann't came to this point
                  SetNULL(resVal, StorageAttribute.BOOLEAN);
                  return;
            }
         }
         //it's not the same type of the field and value :
         else
         {
            //every field has to get it's default value
            bufptr = fld.getDefaultValue();
         }

         if (val.IsNull)
         {
            // get the null value or the magic default value
            bufptr = fld.getNullValue();
            if (bufptr == null)
               fld.getMagicDefaultValue();
         }
         //QCR 984563 var set in creat mode sould not creat a new record
         //#777700. The record should be marked as updated if the field's task and the expression task are different or
         //the field's task is in Record Suffix. This is same as OL (refer RT::PostVeeUpdate in vew.cpp).
         bool setRecordUpdated = (fld.getTask() != ExpTask || ((Task)fld.getTask()).getBrkLevel() == ConstInterface.BRK_LEVEL_REC_SUFFIX);
         fld.setValueAndStartRecompute(bufptr, val.IsNull, true, setRecordUpdated, false);
         fld.updateDisplay();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="showMessage"></param>
      /// <param name="generation"></param>
      private void eval_op_rollback(ExpVal resVal, ExpVal showMessage, ExpVal generation)
      {
         Task task = ((Task)ExpTask.GetContextTask() ?? (Task)ExpTask);
         
         // execute rollback command
         ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_ROLLBACK);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = true;// in the server we return alwes true
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="source"></param>
      /// <param name="maskOrg"></param>
      /// <param name="resVal"></param>
      private void eval_op_like(ExpVal source, ExpVal maskOrg, ExpVal resVal)
      {
         int i, j;
         String Source = source.StrVal;
         String MaskOrg = maskOrg.StrVal;
         int asteriskCnt = 0; //counter of '*' in the source string
         bool Same = true;
         bool esc_ch;

         if (Source == null || MaskOrg == null)
         {
            SetNULL(resVal, StorageAttribute.BOOLEAN);
            return;
         }

         int SourceLen = Source.Length;
         int MaskLen = MaskOrg.Length;
         var Mask = new char[MaskLen];

         var buffer = new StringBuilder(MaskLen);

         // Change in the mask '\\' -> '\', '\*' -> '*', '\?' -> '?',
         //                                 '*' -> ASTERISK_CHR, '?' -> QUESTION_CHR
         for (i = 0, j = 0, esc_ch = false; i < MaskLen; i++)
         {
            char currChr = MaskOrg[i];
            switch (currChr)
            {
               case '\\':
                  if (esc_ch)
                     Mask[j++] = currChr;
                  esc_ch = !esc_ch;
                  break;

               case '*':
                  if (esc_ch)
                     Mask[j++] = currChr;
                  else
                  {
                     Mask[j++] = ASTERISK_CHR;
                     asteriskCnt++;
                  }
                  esc_ch = false;
                  break;

               case '?':
                  Mask[j++] = esc_ch
                              ? currChr
                              : QUESTION_CHR;
                  esc_ch = false;
                  break;

               default:
                  Mask[j++] = currChr;
                  esc_ch = false;
                  break;
            }
         }
         MaskLen = j;
         MaskOrg = arrToStr(Mask, 0, Mask.Length);

         // 1. Find the last '*'
         int ast_last_ptr = MaskOrg.LastIndexOf(ASTERISK_CHR);

         if (ast_last_ptr == -1)
            // 2. In the case: there is not any '*'
            Same = op_like_cmp(Source, MaskOrg);
         else
         {
            // 2. In the case: there are one or more '*'
            // 2.1 Compare the piece of the string before the first '*'
            for (i = 0; Mask[i] != ASTERISK_CHR && Same; i++, MaskLen--, SourceLen--)
            {
               if (SourceLen == 0)
                  Same = false;
               else
                  Same = (Mask[i] == QUESTION_CHR
                          ? true
                          : Mask[i] == Source[i]);
            }
            // i - is index of first '*' in the mask
            Source = Source.Substring(i);
            Mask = cutArray(Mask, i);

            // 2.2 Compare the all pieces between the '*' and '*'
            while (Same && asteriskCnt != 1)
            {
               int ast_ptr;
               int tmp_len;
               for (ast_ptr = 1, tmp_len = 0; ast_ptr + i != ast_last_ptr; ast_ptr++, tmp_len++)
                  if (Mask[ast_ptr] == ASTERISK_CHR)
                     break;
               asteriskCnt--;
               SourceLen = Source.Length;
               //ast_ptr - index of next '*' (not first and not last)

               if (tmp_len != 0)
               //next index of '*' found
               {
                  if (SourceLen > 0)
                  //there is still members in source
                  {
                     buffer.Remove(0, buffer.Length);
                     buffer.Append(Source);
                     Same = op_like_map(buffer, arrToStr(Mask, 1, tmp_len + 1), false);
                     Source = buffer.ToString();
                  }
                  else
                     Same = false;
               }
               i += ast_ptr; //move 'pointer' to the next piece of string (between 2 asterisks)
               Mask = cutArray(Mask, ast_ptr);
            } //end of while

            if (Mask[0] == ASTERISK_CHR)
               //delete last '*'
               Mask = cutArray(Mask, 1);

            // 2.3 Compare the piece of the string after the last '*'
            if (Same && (Mask.Length > 0))
            {
               buffer.Remove(0, buffer.Length);
               buffer.Append(Source);
               Same = op_like_map(buffer, arrToStr(Mask), true);
            }
         }

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = Same;
      }

      /// <summary>
      ///   cut source array from special member
      /// </summary>
      /// <param name = "source">array, to cut it</param>
      /// <param name = "from">to cut all member before it in array</param>
      /// <returns> cutted array</returns>
      private static char[] cutArray(char[] source, int from)
      {
         int length = source.Length - from;
         var buffer = new char[length];
         for (int curr = 0; curr < length; curr++)
            buffer[curr] = source[from + curr];
         return buffer;
      }

      /// <summary>
      ///   implements RepStr
      /// </summary>
      private void eval_op_repstr(ExpVal source, ExpVal orgSubstr, ExpVal newSubstr, ExpVal resVal)
      {
         if (source.StrVal == null || orgSubstr.StrVal == null || newSubstr.StrVal == null ||
             !StorageAttributeCheck.IsTypeAlphaOrUnicode(source.Attr) || !StorageAttributeCheck.IsTypeAlphaOrUnicode(orgSubstr.Attr) ||
             !StorageAttributeCheck.IsTypeAlphaOrUnicode(newSubstr.Attr))
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }

         resVal.StrVal = source.StrVal.Replace(orgSubstr.StrVal, newSubstr.StrVal);
         resVal.Attr = StorageAttribute.UNICODE;
      }

      /// <summary>
      ///   Make string from array of characters
      /// </summary>
      /// <param name = "arr">to make string from</param>
      /// <returns> string contains all characters from the array</returns>
      private static String arrToStr(char[] arr)
      {
         return arrToStr(arr, 0, arr.Length);
      }

      /// <summary>
      ///   Make string from array of characters
      /// </summary>
      /// <param name = "arr">to make string from</param>
      /// <param name = "from">to start copy from array</param>
      /// <param name = "to">to finish copy from array</param>
      /// <returns> string contains all characters from the array</returns>
      private static String arrToStr(char[] arr, int from, int to)
      {
         var buffer = new StringBuilder(to - from);
         for (; from < to; from++)
            buffer.Append(arr[from]);
         return buffer.ToString();
      }

      /// <summary>
      ///   compile 2 strings , regular expression for '?' - any ONE character
      /// </summary>
      /// <param name = "Source">to be checked by pattern</param>
      /// <param name = "MaskOrg">for Regular expression</param>
      /// <returns> true if string muches for the pattern</returns>
      private static bool op_like_cmp(String Source, String MaskOrg)
      {
         bool Same = true;
         String Mask = MaskOrg;
         int SourceLen = Source.Length;
         int MaskLen = MaskOrg.Length;
         if (SourceLen < MaskLen)
            Same = false;
         else if (SourceLen > MaskLen)
            Mask = MaskOrg;

         //QCR 743187
         while (Mask.Length < SourceLen)
            Mask += " ";

         for (int i = 0; i < SourceLen && Same; i++)
            Same = (Mask[i] == QUESTION_CHR
                    ? true
                    : Mask[i] == Source[i]);

         return Same;
      }

      /// <summary>
      ///   compile 2 strings and move source
      /// </summary>
      /// <param name = "Source">source for checking, CHANGED in process of checking, must be changed
      ///   in accordance to changing after the using of function</param>
      /// <param name = "Mask">for Regular expression</param>
      /// <param name = "end">it the last asterisk in the pattern</param>
      /// <returns> true if string muches for the pattern</returns>
      private static bool op_like_map(StringBuilder source, String mask, bool end)
      {
         int j;
         bool same = false;
         String ptr = source.ToString();
         int i = 0;

         // mask is larger then the source. They do not match.
         if (!end && source.Length < mask.Length)
            return false;

         for (j = 0; j < source.Length && !same; j++)
         {
            same = true;
            if (end)
            {
               same = op_like_cmp(ptr.Substring(j), mask);
            }
            else
            {
               for (i = 0; i < mask.Length && same; i++)
               {
                  // If already reached at the end of source string, return false.
                  if (j + i == source.Length)
                     return false;

                  same = (mask[i] == QUESTION_CHR
                          ? true
                          : mask[i] == ptr[j + i]);
               }
            }
         }

         if (same)
         {
            source.Remove(0, source.Length);
            source.Append(ptr.Substring(j + i - 1));
         }

         return same;
      }

      /// <summary>
      ///   Get the name of the control being parked on event shooting.
      /// </summary>
      private void eval_op_hand_ctrl_name(ExpVal resVal)
      {
         RunTimeEvent rtEvt = ClientManager.Instance.EventsManager.getLastRtEvent();
         MgControl currCtrl = rtEvt.Control;
         String ctrlName;

         if (currCtrl == null ||
             (rtEvt.getType() == ConstInterface.EVENT_TYPE_INTERNAL && rtEvt.getInternalCode() > 1000 &&
              rtEvt.getInternalCode() != InternalInterface.MG_ACT_VARIABLE &&
              ((Task)currCtrl.getForm().getTask()).getLevel() != Constants.TASK_LEVEL_CONTROL))
            ctrlName = "";
         else
            ctrlName = currCtrl.Name;

         resVal.StrVal = ctrlName;
         resVal.Attr = StorageAttribute.ALPHA;
      }

      private void exp_get_var(ExpVal val1, ExpVal resVal)
      {
         string fldName = val1.StrVal;
         Field fld = ((Task)ExpTask).getFieldByName(fldName);

         if (fld == null)
         {
            SetNULL(resVal, StorageAttribute.NONE);
            return;
         }

         string fldValue = fld.getValue(true);
         resVal.IsNull = fld.isNull();
         //ctrl = fld.getControl();
         //pic = (ctrl == null) ? null : ctrl.getPIC();
         SetVal(resVal, fld.getType(), fldValue, null);
      }

      private void exp_get_indx(ExpVal val1, ExpVal resVal)
      {
         int index = ((Task)ExpTask.GetContextTask()).getIndexOfFieldByName(val1.StrVal);
         ConstructMagicNum(resVal, index, StorageAttribute.NUMERIC);
      }



      /// <summary>
      ///   get needed Field from var functions
      /// </summary>
      /// <param name = "itm">number of item
      /// </param>
      /// <returns> field  from which an event was triggered or field of itm index of the variable
      /// </returns>
      protected GuiField getField(int itm)
      {
         Field fld = itm != TRIGGER_TASK
                     ? ((Task)ExpTask).ctl_itm_2_parent_vee(itm - 1)
                     : ClientManager.Instance.EventsManager.getCurrField();
         return fld;
      }

      /// <summary>
      /// </summary>
      /// <param name="itm"></param>
      /// <returns></returns>
      private Field GetFieldOfContextTask(int itm)
      {
         return (itm != TRIGGER_TASK
                   ? ((Task)ExpTask.GetContextTask()).ctl_itm_2_parent_vee(itm - 1)
                   : ClientManager.Instance.EventsManager.getCurrField());
      }

      /// <summary>
      ///   remove spaces from the edges of the string
      ///   this is a high performance implementation that does not create unnecessary Strings
      /// </summary>
      /// <param name = "s">the source string</param>
      /// <param name = "type">Left, Right, Both</param>
      private static String trimStr(String s, char type)
      {
         int l = 0;

         if (string.IsNullOrEmpty(s))
            return s;

         int r = s.Length - 1;

         // trim the left side
         if (type != 'R')
         {
            while (l < s.Length && s[l] == ' ')
               l++;
         }

         // trim the right side
         if (type != 'L')
         {
            while (r >= l && s[r] == ' ')
               r--;
         }

         r++; // point the right bound of the string (exclusive)
         return r > l
                ? s.Substring(l, (r) - (l))
                : "";
      }

      /// <summary>
      ///   compare 2 magic values and return true if they are equal
      /// </summary>
      /// <param name = "aVal">the first value to compare</param>
      /// <param name = "aIsNull">true if the first value is null</param>
      /// <param name = "aDataType">the data type of the first value</param>
      /// <param name = "bVal">the second value to compare</param>
      /// <param name = "bIsNull">true if the second value is null</param>
      /// <param name = "bDataType">the data type of the second value</param>
      internal static bool mgValsEqual(String aVal, bool aIsNull, StorageAttribute aDataType, String bVal, bool bIsNull, StorageAttribute bDataType)
      {
         ExpVal a = null, b = null;
         bool result = false;

         // if one of aVal or bVal has a null Java value and has a boolean data type
         // then don't enter the "if block" because the val_cmp_any() method will
         // treat that value as false, so it might return a bad result
         if (aIsNull == bIsNull && (aIsNull || aVal != null && bVal != null))
         {
            try
            {
               a = new ExpVal(aDataType, aIsNull, aVal);
               b = new ExpVal(bDataType, bIsNull, bVal);
               result = (val_cmp_any(a, b, false) == 0);
            }
            catch (NullValueException)
            {
               //QCR 983332 if both values are null then they are equal
               if (a.IsNull && b.IsNull)
                  result = true;
            }
         }
         return result;
      }

      /// <summary>
      ///   compute tree level
      /// </summary>
      /// <param name = "resVal">
      /// </param>
      private void eval_op_treeLevel(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         int res = 0; // the result

         MgForm form = (MgForm)((Task)ExpTask).getForm();
         if (form != null)
         {
            MgTreeBase mgTree = form.getMgTree();
            if (mgTree != null)
               res = mgTree.getLevel(form.DisplayLine);
         }

         // return the res value
         resVal.MgNumVal.NUM_4_LONG(res);
      }

      /// <summary>
      ///   get tree value by level
      /// </summary>
      /// <param name = "val">tree level
      /// </param>
      /// <param name="resVal">
      /// </param>
      private void eval_op_treeValue(ExpVal val, ExpVal resVal)
      {
         MgForm form = (MgForm)((Task)ExpTask).getForm();
         int level = val.MgNumVal.NUM_2_LONG();

         String treeValue = null;
         bool isNull = true;
         MgControl treeControl = null;

         if (form != null)
         {
            MgTreeBase mgTree = form.getMgTree();
            treeControl = (MgControl)form.getTreeCtrl();

            if (mgTree != null)
            {
               int line = form.DisplayLine;
               while (level > 0 && mgTree.getParent(line) != MgTreeBase.NODE_NOT_FOUND)
               {
                  //go up #levels
                  line = mgTree.getParent(line);
                  level--;
               }

               if (level == 0)
               //get value
               {
                  treeValue = treeControl.getTreeValue(line);
                  isNull = ((MgControl)(form.getTreeCtrl())).isTreeNull(line);
               }
               else if (level == 1)
               //we r in the root and we need parent's value
               {
                  treeValue = treeControl.getTreeParentValue(line);
                  isNull = ((MgControl)(form.getTreeCtrl())).isTreeParentNull(line);
               }
            }
         }

         if (treeControl == null || treeValue == null)
         {
            SetNULL(resVal, StorageAttribute.NONE);
            return;
         }

         SetVal(resVal, treeControl.getNodeIdField().getType(), treeValue, null);
         resVal.IsNull = isNull;
      }

      /// <summary>
      ///   go to node
      /// </summary>
      /// <param name = "val">
      /// </param>
      /// <param name="resVal">
      /// </param>
      private void eval_op_treeNodeGoto(ExpVal val, ExpVal resVal)
      {
         MgForm form = (MgForm)((Task)ExpTask).getForm();
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         if (form != null)
         {
            MgTree mgTree = (MgTree)(form.getMgTree());
            MgControl treeControl = (MgControl)form.getTreeCtrl();
            if (mgTree != null && treeControl.IsParkable(false))
            {
               //find node to go to
               int newLine = mgTree.findNode(val.ToMgVal(), val.IsNull, val.Attr);
               if (newLine != MgTreeBase.NODE_NOT_FOUND)
               {
                  var rtEvt = new RunTimeEvent(treeControl, newLine);
                  rtEvt.setInternal(InternalInterface.MG_ACT_CTRL_PREFIX);
                  resVal.BoolVal = true;
                  ClientManager.Instance.EventsManager.addToTail(rtEvt);
               }
            }
         }
      }

      /// <summary>
      ///   funtion that get the logical name translate
      /// </summary>
      /// <param name = "str"></param>
      /// <param name = "resVal"></param>
      private void eval_op_translate(ExpVal str, ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = "";

         String name = str.StrVal;
         if (string.IsNullOrEmpty(name))
            SetNULL(resVal, StorageAttribute.ALPHA);
         else
            resVal.StrVal = ClientManager.Instance.getEnvParamsTable().translate(name);
      }

      /// <summary>
      ///   function that get a string and format
      ///   and change the string to be convert as the format
      /// </summary>
      private void eval_op_astr(ExpVal source, ExpVal format, ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.UNICODE;

         if (source.StrVal == null || format.StrVal == null)
         {
            SetNULL(resVal, StorageAttribute.UNICODE);
            return;
         }

         if (format.StrVal.Length > 0 && source.StrVal.Length > 0)
         {
            var pic = new PIC(set_a_pic(format.StrVal), StorageAttribute.UNICODE, ((Task)ExpTask).getCompIdx());
            resVal.StrVal = DisplayConvertor.Instance.mg2disp(source.ToMgVal(), null, pic, false, ((Task)ExpTask).getCompIdx(), true,
                                                      false);
         }
      }

      /// <summary>
      ///   calculates the attribute of a vector cells
      /// </summary>
      /// <param name = "vec">- the actuall vector not a 'var' literal </param>
      /// <param name = "res"></param>
      private static void eval_op_vecCellAttr(ExpVal vec, ExpVal res)
      {
         //return type is allway alpha
         res.Attr = StorageAttribute.ALPHA;
         res.IsNull = false;
         StorageAttribute attr = StorageAttribute.NONE;

         // if the vector is null take the type from the field
         //QCR 928308 && 426618 
         if (vec.IsNull && vec.VectorField != null)
         {
            attr = vec.VectorField.getCellsType();
         }
         else if (IsValidVector(vec))
         //if it a valid vector take from vector
         {
            attr = VectorType.getCellsAttr(vec.StrVal);
         }

         res.StrVal = "" + GetAttributeChar(attr);
      }

      /// <summary>
      ///   returns a cells value from a vector
      /// </summary>
      /// <param name = "vec">- the actuall vector not a 'var' literal</param>
      /// <param name = "cell">- the cell index to get </param>
      /// <param name = "res"></param>
      private void eval_op_vecGet(ExpVal vec, ExpVal cell, ExpVal res)
      {
         if (cell.MgNumVal == null || !IsValidVector(vec) || cell.MgNumVal.NUM_2_LONG() <= 0)
         {
            res.IsNull = true;
         }
         else
         {
            StorageAttribute cellAttr;
            string cellVal;
            if (vec.VectorField != null)
            {
               cellAttr = vec.VectorField.getCellsType();
               cellVal = ((Field)vec.VectorField).getVecCellValue(cell.MgNumVal.NUM_2_LONG());
            }
            else
            {
               var vector = new VectorType(vec.StrVal);
               cellVal = vector.getVecCell(cell.MgNumVal.NUM_2_LONG());
               cellAttr = vector.getCellsAttr();
            }

            //check the return value type type
            if (cellVal == null)
               res.IsNull = true;
            else
            {
               switch (cellAttr)
               {
                  case StorageAttribute.ALPHA:
                  case StorageAttribute.MEMO:
                     res.Attr = StorageAttribute.ALPHA;
                     res.StrVal = cellVal;
                     break;

                  case StorageAttribute.UNICODE:
                     res.Attr = cellAttr;
                     res.StrVal = cellVal;
                     break;

                  case StorageAttribute.BLOB:
                     res.Attr = cellAttr;
                     res.StrVal = cellVal;
                     res.IncludeBlobPrefix = true;
                     break;

                  case StorageAttribute.BLOB_VECTOR:
                     res.Attr = cellAttr;
                     res.StrVal = cellVal;
                     res.IncludeBlobPrefix = true;
                     break;

                  case StorageAttribute.NUMERIC:
                     res.Attr = cellAttr;
                     res.MgNumVal = new NUM_TYPE(cellVal);
                     break;

                  case StorageAttribute.DATE:
                     res.Attr = cellAttr;
                     res.MgNumVal = new NUM_TYPE(cellVal);
                     break;

                  case StorageAttribute.TIME:
                     res.Attr = cellAttr;
                     res.MgNumVal = new NUM_TYPE(cellVal);
                     break;

                  case StorageAttribute.BOOLEAN:
                     res.Attr = cellAttr;
                     res.BoolVal = DisplayConvertor.toBoolean(cellVal);
                     break;

                  default:
                     throw new ApplicationException("in ExpressionEvaluator.eval_op_vecGet unknowen storage type: " +
                                                    cellAttr);
               }
            }
         }
      }

      /// <summary>
      ///   returns the size of the vector
      /// </summary>
      /// <param name = "vec">- the actuall vector not a 'var' literal</param>
      /// <param name = "res"></param>
      private void eval_op_vecSize(ExpVal vec, ExpVal res)
      {
         //the resulte will always be numeric
         res.Attr = StorageAttribute.NUMERIC;
         res.IsNull = false;
         res.MgNumVal = new NUM_TYPE();

         res.MgNumVal.NUM_4_LONG(-1); //if there is a problem return -1
         //QCR 426618 
         if (IsValidVector(vec))
            res.MgNumVal.NUM_4_LONG(
               (int)(new VectorType(vec.StrVal).getVecSize()));
      }

      /// <summary>
      ///   sets the value of a given cell
      /// </summary>
      /// <param name = "vec">-  a 'var' literal pointing to the vector</param>
      /// <param name = "cell">- the cell index to set</param>
      /// <param name = "newVal">- the value to be set</param>
      /// <param name = "res"></param>
      private void eval_op_vecSet(ExpVal vec, ExpVal cell, ExpVal newVal, ExpVal res)
      {
         //result is logical
         res.Attr = StorageAttribute.BOOLEAN;
         res.BoolVal = false;

         if (vec.MgNumVal != null && cell.MgNumVal != null)
         {
            Field vecField;
            try
            {
               vecField = (Field)getField(vec.MgNumVal.NUM_2_LONG());
            }
            catch (Exception ex)
            {
               Logger.Instance.WriteExceptionToLog(ex);
               vecField = null;
            }

            if (StorageAttributeCheck.isTypeDotNet(newVal.Attr))
               ConvertExpVal(newVal, vecField.getCellsType());

            if ((StorageAttributeCheck.IsTypeAlphaOrUnicode(vecField.getCellsType()) && StorageAttributeCheck.IsTypeAlphaOrUnicode(newVal.Attr)) ||
                (vecField != null &&
                 (vecField.IsVirtual || vecField.getTask().getMode() == Constants.TASK_MODE_CREATE ||
                  vecField.DbModifiable) && vecField.getType() == StorageAttribute.BLOB_VECTOR &&
                 (StorageAttributeCheck.isTheSameType(vecField.getCellsType(), newVal.Attr) || newVal.IsNull ||
                  (StorageAttributeCheck.IsTypeAlphaOrUnicode(newVal.Attr) &&
                   vecField.getCellsType() == StorageAttribute.BLOB))))
            //allow alpha into blob rtf  
            {
               //QCR 745541
               //convert the alpha data into blob
               if (StorageAttributeCheck.IsTypeAlphaOrUnicode(newVal.Attr) &&
                   vecField.getCellsType() == StorageAttribute.BLOB)
               {
                  ConvertExpVal(newVal, StorageAttribute.BLOB);
               }

               res.BoolVal = vecField.setCellVecValue(cell.MgNumVal.NUM_2_LONG(), newVal.ToMgVal(), newVal.IsNull);
            }
         }
      }

      /// <summary>
      ///   returns the number of tokens in the string acourding to the given delimeters
      ///   If the SourceString is empty, return value is 0.
      ///   If the delimiter was not found  in the SourceString or was empty, return value is 1.
      /// </summary>
      /// <param name = "sourceString">- the data string</param>
      /// <param name = "delimiter">the delimeter</param>
      /// <param name = "resVal"></param>
      private void eval_op_strTokenCnt(ExpVal sourceString, ExpVal delimiter, ExpVal resVal)
      {
         // the return value is always numeric
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();

         //the result
         int res = 0;

         //continue only if the source string exists and is not empty 
         if (!sourceString.IsNull && sourceString.StrVal != null &&
             sourceString.StrVal.TrimEnd(_charsToTrim).Length != 0)
         {
            //if the delimiter is empty 
            if (delimiter.IsNull || delimiter.StrVal.TrimEnd(_charsToTrim).Length == 0)
               res = 1;
            //find the delimiter in the source string
            else
            {
               //if the delimiter was not found in the data
               int tokensSize = strTokenCount(sourceString.StrVal, delimiter.StrVal);
               if (tokensSize == 0)
                  res = 1;
               else
                  res = tokensSize;
            }
         }

         //return the res value
         resVal.MgNumVal.NUM_4_LONG(res);
      }

      /// <summary>
      ///   This function returns the index of the given token within the source
      ///   string or 0 if the token was not found.                             
      ///   If the SourceString or the token are empty, return value is 0.
      /// </summary>
      private void eval_op_strTokenIdx(ExpVal sourceString, ExpVal token, ExpVal delimiter, ExpVal resVal)
      {
         // the return value is always numeric
         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();

         //the result
         int res = 0;

         //continue only if the source string exists and is not empty 
         //and the token exist and not empty
         if (!sourceString.IsNull && sourceString.StrVal != null && !token.IsNull && token.StrVal != null &&
             sourceString.StrVal.TrimEnd(_charsToTrim).Length != 0 && token.StrVal.Trim().Length != 0)
         {
            //if the delimiter is empty check if the data equals to the token 
            if (!delimiter.IsNull && delimiter.StrVal.TrimEnd(_charsToTrim).Length != 0)
               res = strTokenIndex (sourceString.StrVal, delimiter.StrVal, token.StrVal);
            else if (sourceString.StrVal.Equals(token.StrVal))
               res = 1;
            else
               res = 0;
         }

         //return the res value
         resVal.MgNumVal.NUM_4_LONG(res);
      }

      /// <summary>
      /// Find a given token index in a given string (1 base)
      /// </summary>
      /// <param name="source">The string to search on</param>
      /// <param name="delimiter">The delimiter</param>
      /// <param name="token">The token to search</param>
      /// <returns></returns>
      private int strTokenIndex(String source, String delimiter, String token)
      {
         int    tokenIndex = 0;
         // add the delimiter at the front and end of the string and the token
         // this will help us if finding tokens that are at the beginning/end of the string.
         String trimDelim = delimiter.TrimEnd(_charsToTrim);
         String trimSource = trimDelim + source.TrimEnd(_charsToTrim) + trimDelim;
         String trimToken = (token != null
                             ? token.TrimEnd(_charsToTrim)
                             : null);

         if (trimToken == null)
            return 0;

         // surround the token with delimiters
         trimToken = trimDelim + trimToken + trimDelim;

         int tokenOffset = trimSource.IndexOf(trimToken);

         if (tokenOffset == -1)
            return 0;
         else
         {
            // The token count is the number of tokens till our token offset +1
            // since we added 1 token before the string, we will have to subtract 1
            tokenIndex = strTokenCount(trimSource.Substring(0, tokenOffset + trimDelim.Length), trimDelim);
            tokenIndex--;
         }

         return tokenIndex;
      }

      /// <summary>
      /// Count the tokens 
      /// </summary>
      /// <param name="source">The string to split</param>
      /// <param name="delimiter">The delimitrer</param>
      /// <returns></returns>
      private int strTokenCount(String source, String delimiter)
      {
         int counter = 1;
         String trimDelim = delimiter.TrimEnd(_charsToTrim);
         String trimSource = source.TrimEnd(_charsToTrim);

         int delimLength = trimDelim.Length;
         String data = trimSource;
         int fromOffset = 0;
         int delimiterOffset = 0;

         // empty string. no tokens.
         if (source == null || source.Length == 0)
            return 0;

         delimiterOffset = data.IndexOf(trimDelim, fromOffset);

         // find the number of tokens by counting the delimiters + 1
         while (delimiterOffset >= 0)
         { 
               counter++;

               // start next search after the delimiter
               fromOffset = delimiterOffset + delimLength;

               delimiterOffset = data.IndexOf(trimDelim, fromOffset);
         }

         return counter;
      }

      private void eval_op_blobsize(ExpVal resVal, ExpVal blobVal)
      {
         int size = 0;
         switch (blobVal.Attr)
         {
            case StorageAttribute.BLOB_VECTOR:
               size = (int)VectorType.getVecSize(blobVal.StrVal);
               break;
            case StorageAttribute.BLOB:
               size = BlobType.getBlobSize(blobVal.StrVal);
               break;
            default:
               break;
         }

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG(size);
      }

      private void eval_op_iscomponent(ExpVal resVal)
      {
         Task currTsk = ClientManager.Instance.EventsManager.getCurrTask() ?? (Task)ExpTask;

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = currTsk.getCompIdx() != 0;
      }

      /// <summary>
      ///   Find and Execute user defined function
      /// </summary>
      private void eval_op_ExecUserDefinedFunc(String funcName, ExpVal[] Exp_params, ExpVal resVal, StorageAttribute expectedType)
      {
         var rtEvt = new RunTimeEvent((Task)ExpTask);
         rtEvt.setType(ConstInterface.EVENT_TYPE_USER_FUNC);
         rtEvt.setUserDefinedFuncName(funcName);

         var evtHanPos = new EventHandlerPosition();
         evtHanPos.init(rtEvt);

         EventHandler handler = evtHanPos.getNext();

         /* TODO: Kaushal. May be we dont need to match the args */
         bool argsMatch = handler != null && handler.argsMatch(Exp_params);

         ExpVal val = null;
         bool valIniated = false;
         if (argsMatch)
         {
            // if there is a handler change the handler's context task to the event's task
            Task handlerContextTask = (Task)handler.getTask().GetContextTask();

            // If user-defined function is executed from handler which is invoked from  context task already,
            // then do not change the context task again.
            if (handler.getTask() == (Task)handler.getTask().GetContextTask())
               handler.getTask().SetContextTask(rtEvt.getTask());

            var argList = new ArgumentsList(Exp_params);

            rtEvt.setArgList(argList);
            // if the execution of the user defined function will go to the server, we don't want it to continue
            // any other execution on the server, like if we are in the middle of raise event or something
            // like that because the server cannot know on which operation we are currently on and which exprssion
            // and which part of the expression we are evaluating
            ClientManager.Instance.EventsManager.pushNewExecStacks();
            handler.execute(rtEvt, false, false);
            ClientManager.Instance.EventsManager.popNewExecStacks();

            // evaluate the return value of the user defined function
            Expression exp = handler.getTask().getExpById(handler.getEvent().getUserDefinedFuncRetExp());
            if (exp != null)
            {
               val = exp.evaluate(expectedType);
               valIniated = true;
            }

            // restoring the context
            handler.getTask().SetContextTask(handlerContextTask);
         }

         if (!valIniated)
         {
            if (expectedType == StorageAttribute.NONE)
               expectedType = StorageAttribute.ALPHA;
            val = new ExpVal(expectedType, true, null);
         }

         // copy contents from the Val helper variable to the resVal parameter.
         resVal.Copy(val);
      }

      /// <summary>(private)
      /// add values to clipboard
      /// </summary>
      /// <param name="vals">list values (val1, format1, val2, format2..)</param>
      /// <returns></returns>
      protected bool eval_op_clipAdd(ExpVal[] vals)
      {
         int i;

         for (i = 0; i < vals.Length - 1; i += 2)
         {
            if (!vals[i].IsNull && !vals[i + 1].IsNull &&
                (vals[i + 1].Attr == StorageAttribute.ALPHA ||
                 vals[i + 1].Attr == StorageAttribute.UNICODE))
            {
               if (i > 0)
                  Manager.ClipboardAdd("\t"); // a tab character should be present between values
               string mgVal = vals[i].ToMgVal();
               var pic = new PIC(vals[i + 1].StrVal, vals[i].Attr, (ExpTask).getCompIdx());
               string val = DisplayConvertor.Instance.mg2disp(mgVal, null, pic, false, (ExpTask).getCompIdx(),
                                                                   false);
               Manager.ClipboardAdd(val);
            }
         }
         Manager.ClipboardAdd("\r\n"); // new line placement at the end
         return true;
      }

      /// <summary>(private)
      /// places the buffer created by the ClipAdd() into the clipboard
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_clipwrite(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.IsNull = false;
         Task currTsk = (Task)ExpTask.GetContextTask();
         resVal.BoolVal = Manager.ClipboardWrite(currTsk);
      }

      /// <summary>(private)
      /// returns the contents from clipboard
      /// </summary>
      /// <param name="resVal">contents from clipboard</param>
      private void eval_op_clipread(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = Manager.ClipboardRead();
         if (string.IsNullOrEmpty(resVal.StrVal))
            resVal.IsNull = true;
      }

      /// <summary>
      ///   returns the internal name of the task
      /// </summary>
      private void eval_op_publicName(ExpVal resVal, ExpVal Parent)
      {
         String publicName = "NULL";
         Task task;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null && task.isProgram() && !task.isMainProg())
               publicName = task.getPublicName();
         }

         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = publicName;
      }

      /// <summary>
      ///   returns the external task ID of the task
      /// </summary>
      private void eval_op_taskId(ExpVal resVal, ExpVal Parent)
      {
         String taskId = "NULL";
         Task task;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
               taskId = task.getExternalTaskId();
         }

         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = taskId;
      }

      /// <summary>
      ///   returns the number of cached records
      /// </summary>
      private void eval_op_dbviewsize(ExpVal resVal, ExpVal Parent)
      {
         int size = 0;
         Task task;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null && ((DataView)task.DataView).HasMainTable)
            {
               if (task.checkProp(PropInterface.PROP_TYPE_PRELOAD_VIEW, false))
                  size = ((DataView)task.DataView).DBViewSize;
               else if (task.isTableWithAbsolutesScrollbar())
                  size = ((DataView)task.DataView).TotalRecordsCount;
            }
         }
         ConstructMagicNum(resVal, size, StorageAttribute.NUMERIC);
      }

      /// <summary>
      ///   returns the numeric value representing the sequential number 
      ///   of a record in a cached view
      /// </summary>
      private void eval_op_dbviewrowidx(ExpVal resVal, ExpVal Parent)
      {
         int idx = 0;
         Task task;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null && task.checkProp(PropInterface.PROP_TYPE_PRELOAD_VIEW, false))
            {
               /* QCR# 969723 - Do not return the row idx untill the record is commited.  */
               if (((DataView)task.DataView).getCurrRec().getMode() != DataModificationTypes.Insert)
               {
                  idx = task.DataviewManager.CurrentDataviewManager.GetDbViewRowIdx();
               }
            }
         }

         ConstructMagicNum(resVal, idx, StorageAttribute.NUMERIC);
      }

      /// <summary>
      ///   for Browser Control only: execute java script on on the browser Control
      /// </summary>
      protected void eval_op_browserExecute(Stack valStack, ExpVal resVal, int nArgs)
      {
         var Exp_parms = new ExpVal[nArgs];
         int i;

         // Read parameters 
         for (i = nArgs - 1; i >= 0; i--)
            Exp_parms[i] = (ExpVal)valStack.Pop();

         ExpVal controlName = Exp_parms[0]; //the control name
         ExpVal text = Exp_parms[1]; //the text to be set
         ExpVal sync = Exp_parms[2]; //sync
         String language = "JScript";

         if (nArgs == 4)
            language = Exp_parms[3].StrVal; //language

         eval_op_browserExecute_DO(resVal, controlName, text, sync, language);

      }

      /// <summary>
      ///   Call the Mls translation to return the translation of 'fromString'.
      /// </summary>
      private void eval_op_MlsTrans(ExpVal resVal, ExpVal fromString)
      {
         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = "";

         resVal.StrVal = ClientManager.Instance.getLanguageData().translate(fromString.StrVal.TrimEnd(_charsToTrim));
      }

      /// <summary> Check if input string has hebrew chars.</summary>
      private bool hasHebrewChars(StringBuilder str)
      {
         const long ALEF_RT = 128;
         const long TAF_RT = 155;
         byte[] srcBytes;

         srcBytes = Encoding.Default.GetBytes(str.ToString());

         for (int i = 0; i < srcBytes.Length; i++)
         {
            if (srcBytes[i] >= ALEF_RT && srcBytes[i] <= TAF_RT)
               return (true);
         }

         return (false);
      }

      /// <summary>
      /// Convert a string from visual representation to logical one.
      /// </summary>
      /// <param name="inString"></param>
      /// <param name="reverse"></param>
      /// <param name="resVal"></param>
      private void eval_op_logical(ExpVal inString, ExpVal reverse, ExpVal resVal)
      {
         String srcString = exp_build_string(inString);

         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = srcString;
#if !PocketPC
         if (ClientManager.Instance.getEnvironment().Language == 'H')
         {
            srcString = srcString.Trim();
            StringBuilder srcStr = new StringBuilder(srcString);
            int srcLen = srcStr.Length;
            if (srcLen > 0)
            {
               if (commandsProcessor.CopyToDefaultFolder(ConstInterface.V2L_DLL, (Task)ExpTask))
               {
                  bool bTrans = hasHebrewChars(srcStr);

                  //reverse srcStr
                  srcStr = StrUtil.ReverseString(srcStr);

                  Vis2Log.Vis_2_Log(srcStr, (UInt16)srcLen, srcStr, srcLen, !reverse.BoolVal, bTrans, bTrans, false);

                  resVal.StrVal = srcStr.ToString();
               }
            }
         }
#endif
      }

      /// <summary>
      /// Convert a string from logical format to visual presentation.
      /// </summary>
      /// <param name="inString"></param>
      /// <param name="rightToLeft"></param>
      /// <param name="resVal"></param>
      private void eval_op_visual(ExpVal inString, ExpVal rightToLeft, ExpVal resVal)
      {
         String srcString = exp_build_string(inString);

         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = srcString;

#if !PocketPC
         if (ClientManager.Instance.getEnvironment().Language == 'H')
         {
            srcString = srcString.Trim();
            StringBuilder srcStr = new StringBuilder(srcString);
            int srcLen = srcStr.Length;

            if (srcLen > 0)
            {
               if (commandsProcessor.CopyToDefaultFolder(ConstInterface.V2L_DLL, (Task)ExpTask))
               {
                  bool bTrans = hasHebrewChars(srcStr);

                  //reverse srcString
                  srcStr = StrUtil.ReverseString(srcStr);

                  Vis2Log.Log_2_Vis(srcStr, srcStr, srcLen, !rightToLeft.BoolVal, bTrans, bTrans);

                  resVal.StrVal = srcStr.ToString();
               }
            }
         }
#endif
      }
      /// <summary>
      ///   Implements StrBuild
      /// </summary>
      private void eval_op_StrBuild(Stack valStack, ExpVal resVal, int nArgs)
      {
         var Exp_parms = new ExpVal[nArgs];
         int i;

         // Read parameters 
         for (i = 0; i < nArgs; i++)
            Exp_parms[nArgs - 1 - i] = (ExpVal)valStack.Pop();
         val_cpy(Exp_parms[0], resVal);

         var resultStr = new StringBuilder(resVal.StrVal);

         // For each parameter, search "@n@" string in the given string.
         // If found, and if it is not preceded by '\'
         for (i = 1; i < nArgs; i++)
         {
            String toReplace = "@" + Convert.ToString(i).Trim() + "@";
            int indexFrom = 0;
            while (indexFrom != -1)
            {
               int nextIndex = resultStr.ToString().IndexOf(toReplace, indexFrom);
               if (nextIndex == -1)
                  break;

               bool precededBySlash = false;
               int shashIndex = resultStr.ToString().IndexOf("\\" + toReplace, indexFrom);
               if (shashIndex != -1)
                  precededBySlash = true;

               // if '\' is found check if, it is referring the current occurrance of @n@
               if ((precededBySlash && nextIndex != shashIndex + 1) || !precededBySlash)
               {
                  resultStr.Replace(resultStr.ToString(nextIndex, nextIndex + toReplace.Length - nextIndex),
                                    Exp_parms[i].StrVal.Trim(), nextIndex, nextIndex + toReplace.Length - nextIndex);
                  indexFrom = nextIndex + Exp_parms[i].StrVal.Trim().Length;
               }
               else
                  indexFrom = nextIndex + 1;
            }
         }

         resultStr.Replace("\\@", "@");
         resVal.StrVal = resultStr.ToString();
      }

      /// <summary>
      ///   ClientFileOpenDlg runtime function: Shows the File Dialog box and takes input from user
      /// </summary>
      /// <param name = "resVal">       Resultant value (string) from File Dialog Box</param>
      /// <param name = "titleVal">     caption of the dialog window</param>
      /// <param name = "dir">          initial directory path</param>
      /// <param name = "filterVal">    Filter String</param>
      /// <param name = "checkExists">  should check if file exists</param>
      /// <param name = "multiSelect">  should enable multiple selections.</param>
      private static void eval_op_fileopen_dlg(ExpVal resVal, ExpVal titleVal, ExpVal dir, ExpVal filterVal,
                                               ExpVal checkExists, ExpVal multiSelect)
      {
         String filter = exp_build_string(filterVal);
         String init_dir = exp_build_string(dir);

         //Translate logical name.
         init_dir = ClientManager.Instance.getEnvParamsTable().translate(dir.StrVal);

         String title = exp_build_string(titleVal);
         Boolean bCheckExists = checkExists.BoolVal;
         Boolean bMultiSelect = multiSelect.BoolVal;

         String directoryName;
         String fileName;
         HandleFiles.splitFileAndDir(init_dir, out directoryName, out fileName);

         // create a gui interactive object to interact with gui and get the result
         string finalResult = Commands.fileOpenDialogBox(title, directoryName, fileName, filter, bCheckExists, bMultiSelect);

         // Set result
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = finalResult;
      }

      /// <summary>
      ///   ClientFileSaveDlg runtime function: Shows the File Dialog box and takes input from user
      /// </summary>
      /// <param name = "resVal">       Resultant value (string) from File Dialog Box</param>
      /// <param name = "titleVal">     caption of the dialog window</param>
      /// <param name = "dir">          initial directory path</param>
      /// <param name = "filterVal">    Filter String</param>
      /// <param name = "defExt">       default file extension</param>
      /// <param name = "owPrompt">     should prompt before overwriting an existing file</param>
      private static void eval_op_filesave_dlg(ExpVal resVal, ExpVal titleVal, ExpVal dir, ExpVal filterVal,
                                               ExpVal defExt, ExpVal owPrompt)
      {
         String filter = exp_build_string(filterVal);
         String init_dir = exp_build_string(dir);
         String title = exp_build_string(titleVal);
         String defaultExtension = exp_build_string(defExt);
         Boolean overwritePrompt = owPrompt.BoolVal;

         String directoryName;
         String fileName;
         HandleFiles.splitFileAndDir(init_dir, out directoryName, out fileName);

         // create a gui interactive object to interact with gui and get the result
         string finalResult = Commands.fileSaveDialogBox(title, directoryName, fileName, filter, defaultExtension,
                                                         overwritePrompt);

         // Set result
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = finalResult;
      }

      /// <summary>
      ///   Copy the contents of a file to another file stored on client m/c.
      /// </summary>
      /// <param name = "resVal">Stores results. Will update BoolVal and Attr will be set to STORAGE_ATTR_BOOLEAN.    </param>
      /// <param name = "source">Expval containing path to a source file. </param>
      /// <param name = "target">Expval containing path to a target file.</param>
      private void eval_op_client_filecopy(ExpVal resVal, ExpVal source, ExpVal target)
      {
         //extract actual file names from ExpVal.
         string sourceFileName = exp_build_ioname(source);
         string targetFileName = exp_build_ioname(target);

         //Set result
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = HandleFiles.copy(sourceFileName, targetFileName, true, false);
      }

      /// <summary>
      ///   Check if given file  exists on client m/c.
      /// </summary>
      /// <param name = "resVal">Stores results. Will update BoolVal and Attr will be set to STORAGE_ATTR_BOOLEAN.</param>
      /// <param name = "source"> Expval containing path to a source file.</param>
      private void eval_op_client_file_exist(ExpVal resVal, ExpVal source)
      {
         // extract actual file names from ExpVal.
         string sourceFileName = exp_build_ioname(source);

         //check if file exists
         bool isSuccess = HandleFiles.isExists(sourceFileName);

         //Set result
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = isSuccess;
      }

      private void eval_op_client_fileinfo(ExpVal resVal, ExpVal name, ExpVal value)
      {
         Boolean retOK = false;
         // extract actual file names from ExpVal.
         string sourceFileName = exp_build_ioname(name);

         // Fail if there's a '\' at the end
         Boolean nameEndBackSlash = sourceFileName.EndsWith("\\");

         var f = new FileInfo(sourceFileName);
         var d = new DirectoryInfo(sourceFileName);

         if (f.Exists || (d.Exists && !nameEndBackSlash))
         {
            DateTime dt;
            var requestedInfo = (FileInfoType)value.MgNumVal.NUM_2_LONG();
            switch (requestedInfo)
            {
               case FileInfoType.FI_Name:
                  resVal.StrVal = f.Exists
                                   ? f.Name
                                   : d.Name;
                  resVal.Attr = StorageAttribute.ALPHA;
                  retOK = true;
                  break;
               case FileInfoType.FI_Path:
                  resVal.StrVal = f.DirectoryName.TrimEnd('\\');
                  resVal.Attr = StorageAttribute.ALPHA;
                  retOK = true;
                  break;
               case FileInfoType.FI_FullName:
                  resVal.StrVal = f.Exists
                                   ? f.FullName
                                   : sourceFileName.TrimEnd('\\');
                  resVal.Attr = StorageAttribute.ALPHA;
                  retOK = true;
                  break;
               case FileInfoType.FI_Attributes:
                  String attrs = null;
                  FileAttributes fa = f.Exists
                                      ? f.Attributes
                                      : d.Attributes;

                  if ((fa & FileAttributes.Directory) == FileAttributes.Directory)
                     attrs += "DIRECTORY";
                  else
                     attrs += "FILE";
                  if ((fa & FileAttributes.Archive) == FileAttributes.Archive)
                     attrs += ",ARCHIVE";
                  if ((fa & FileAttributes.Compressed) == FileAttributes.Compressed)
                     attrs += ",COMPRESSED";
                  if ((fa & FileAttributes.Encrypted) == FileAttributes.Encrypted)
                     attrs += ",ENCRYPTED";
                  if ((fa & FileAttributes.Hidden) == FileAttributes.Hidden)
                     attrs += ",HIDDEN";
                  if ((fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                     attrs += ",READONLY";
                  if ((fa & FileAttributes.System) == FileAttributes.System)
                     attrs += ",SYSTEM";

                  resVal.StrVal = attrs;
                  resVal.Attr = StorageAttribute.ALPHA;
                  retOK = true;
                  break;
               case FileInfoType.FI_Size:
                  resVal.Attr = StorageAttribute.NUMERIC;
                  resVal.MgNumVal = new NUM_TYPE();
                  if (f.Exists)
                     resVal.MgNumVal.NUM_4_LONG((int)f.Length);
                  else
                     resVal.MgNumVal.NUM_4_LONG(0);
                  retOK = true;
                  break;
               case FileInfoType.FI_CDate:
                  dt = f.Exists
                       ? f.CreationTime
                       : d.CreationTime;
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.date_4_calender(dt.Year, dt.Month,
                                                                                             dt.Day, 0, false));
                  resVal.Attr = StorageAttribute.DATE;
                  retOK = true;
                  break;
               case FileInfoType.FI_CTime:
                  dt = f.Exists
                       ? f.CreationTime
                       : d.CreationTime;
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.time_2_int(dt.Hour, dt.Minute,
                                                                                        dt.Second));
                  resVal.Attr = StorageAttribute.TIME;
                  retOK = true;
                  break;
               case FileInfoType.FI_MDate:
                  dt = f.Exists
                       ? f.LastWriteTime
                       : d.LastWriteTime;
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.date_4_calender(dt.Year, dt.Month,
                                                                                             dt.Day, 0, false));
                  resVal.Attr = StorageAttribute.DATE;
                  retOK = true;
                  break;
               case FileInfoType.FI_MTime:
                  dt = f.Exists
                       ? f.LastWriteTime
                       : d.LastWriteTime;
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.time_2_int(dt.Hour, dt.Minute,
                                                                                        dt.Second));
                  resVal.Attr = StorageAttribute.TIME;
                  retOK = true;
                  break;
               case FileInfoType.FI_ADate:
                  dt = f.Exists
                       ? f.LastAccessTime
                       : d.LastAccessTime;
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.date_4_calender(dt.Year, dt.Month,
                                                                                             dt.Day, 0, false));
                  resVal.Attr = StorageAttribute.DATE;
                  retOK = true;
                  break;
               case FileInfoType.FI_ATime:
                  dt = f.Exists
                       ? f.LastAccessTime
                       : d.LastAccessTime;
                  resVal.MgNumVal = new NUM_TYPE();
                  resVal.MgNumVal.NUM_4_LONG(DisplayConvertor.Instance.time_2_int(dt.Hour, dt.Minute,
                                                                                        dt.Second));
                  resVal.Attr = StorageAttribute.TIME;
                  retOK = true;
                  break;
            }
         }
         if (!retOK)
         {
            resVal.IsNull = true;
            resVal.Attr = StorageAttribute.ALPHA;
         }
      }

      /// <summary>
      ///   Returns size of given file from client m/c
      /// </summary>
      /// <param name = "resVal">Stores results. Will update MgNumVal and Attr will be set to STORAGE_ATTR_NUMERIC.</param>
      /// <param name = "source">Expval containing path to a file whose size is to be calculated.</param>
      private void eval_op_client_file_size(ExpVal resVal, ExpVal source)
      {
         // extract actual file names from ExpVal.
         string sourceFileName = exp_build_ioname(source);

         //check if file exists
         long size = HandleFiles.getFileSize(sourceFileName);

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG((int)size);
      }

      /// <summary>
      ///   Delete given file from client m/c.
      /// </summary>
      /// <param name = "resVal">Stores results. Will update BoolVal and Attr will be set to STORAGE_ATTR_BOOLEAN.</param>
      /// <param name = "source"> Expval containing path to a source file.</param>
      private void eval_op_client_file_delete(ExpVal resVal, ExpVal source)
      {
         // extract actual file names from ExpVal.
         string sourceFileName = exp_build_ioname(source);

         //check if file exists
         bool isSuccess = HandleFiles.deleteFile(sourceFileName);

         //Set result
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = isSuccess;
      }

      /// <summary>
      ///   Get the file(s) from server location to client cache folder and return this path in case of request for single file
      /// </summary>
      /// <param name = "resVal">containing location of the file after being cached</param>
      /// <param name = "source">containing server file path/folder/wildcards</param>
      private void eval_op_server_file_to_client(ExpVal resVal, ExpVal source)
      {
         resVal.Attr = StorageAttribute.ALPHA;

         RemoteCommandsProcessor server = RemoteCommandsProcessor.GetInstance();
         server.ServerFileToClientHelper = new ServerFileToClientExecutionHelper();

         // extract file name from ExpVal.
         String serverFilePath = exp_build_ioname(source);

         // Get file URLS
         bool refreshClientCopy = true;
         String serverCachedFileUrl = server.ServerFileToUrl(serverFilePath, (Task)ExpTask, refreshClientCopy);

         // Get contents of file(s)
         String localFileName = "";
         try
         {
            if (server.ServerFileToClientHelper.RequestedForFolderOrWildcard)
            {
               server.ServerFileToClientHelper.GetMultipleFilesFromServer();

               // Return client cache folder
               localFileName = CacheUtils.URLToLocalFileName(localFileName);
            }
            else if (!String.IsNullOrEmpty(serverCachedFileUrl))
            {
               // execute request to get the file from the server to local cache (if still not cached) without loading it into the memory
               server.DownloadContent(serverCachedFileUrl);

               // Get the file name in the local file system
               localFileName = CacheUtils.URLToLocalFileName(serverCachedFileUrl);

            }
         }
         catch (System.Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }

         resVal.StrVal = localFileName;
         server.ServerFileToClientHelper = null;
      }

      /// <summary>
      ///   Add user ranges on Task..
      /// </summary>
      /// <param name = "resVal">Will return BoolVal.</param>
      /// <param name="varnum"> varnum contains var num on which range is specified.</param>
      /// <param name="min"> min contains min range value.</param>
      /// <param name="max"> max contains max range value.</param>
      private void eval_op_range_add(ExpVal resVal, ExpVal[] Exp_params)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = add_rt_ranges(Exp_params, false);
      }

      /// <summary>
      ///   Free user ranges on Task..
      /// </summary>
      /// <param name = "resVal">Will return BoolVal.</param>
      /// <param name = "parent"> parent contains generation of the task.</param>
      private void eval_op_range_reset(ExpVal resVal, ExpVal parent)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         Task task;

         int iParent = parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
            {
               IClientCommand command = CommandFactory.CreateDataViewCommand(task.getTaskTag(), DataViewCommandType.ResetUserRange);
               task.DataviewManager.Execute(command);

               resVal.BoolVal = true;
            }
         }
      }

      /// <summary>
      ///   Add user locates on Task..
      /// </summary>
      /// <param name = "resVal">Will return BoolVal.</param>
      /// <param name="varnum"> varnum contains var num on which range is specified.</param>
      /// <param name="min"> min contains min range value.</param>
      /// <param name="max"> max contains max range value.</param>
      private void eval_op_locate_add(ExpVal resVal, ExpVal[] Exp_params)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = add_rt_ranges(Exp_params, true);
      }

      /// <summary>
      ///   Free user locates on Task..
      /// </summary>
      /// <param name = "resVal">Will return BoolVal.</param>
      /// <param name = "parent"> parent contains generation of the task.</param>
      private void eval_op_locate_reset(ExpVal resVal, ExpVal parent)
      {
         Task task;

         resVal.Attr = StorageAttribute.BOOLEAN;
         int iParent = parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
            {
               IClientCommand command = CommandFactory.CreateDataViewCommand(task.getTaskTag(), DataViewCommandType.ResetUserLocate);
               task.DataviewManager.Execute(command);
               resVal.BoolVal = true;
            }
         }
      }

      /// <summary>
      ///   Add user sorts on Task..
      /// </summary>
      /// <param name = "resVal">Will return BoolVal. Successful and Attr will be set to STORAGE_ATTR_BOOLEAN.</param>
      /// <param name="varnum"> val1 contains var num on which range is specified.</param>
      /// <param name="dir"> dir contains direction of sort.</param>
      private void eval_op_sort_add(ExpVal resVal, ExpVal varnum, ExpVal dir)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = add_sort(varnum, dir);
      }

      /// <summary>
      ///   Free user ranges on Task..
      /// </summary>
      /// <param name = "resVal">Will return BoolVal.</param>
      /// <param name="parent"> parent contains generation of task.</param>
      private void eval_op_sort_reset(ExpVal resVal, ExpVal parent)
      {
         Task task;

         resVal.Attr = StorageAttribute.BOOLEAN;
         int iParent = parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
            {
               IClientCommand command = CommandFactory.CreateDataViewCommand(task.getTaskTag(), DataViewCommandType.ResetUserSort);
               task.DataviewManager.Execute(command);
               resVal.BoolVal = true;
               
            }
         }
      }

      /// <summary>
      ///   This function returns current instance i.e. tag of the task
      /// </summary>
      /// <param name = "resVal">Will return BoolVal.</param>
      /// <param name = "Parent"> parent contains generation of task.</param>
      private void eval_op_tsk_instance(ExpVal resVal, ExpVal Parent)
      {
         Task task;
         uint tag = 0;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
            {
               tag = Convert.ToUInt32(task.getTaskTag());
            }
         }

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG((int)tag);
      }

      /// <summary>
      ///   This function adds User Sort on task.
      /// </summary>
      private bool add_sort(ExpVal varnum, ExpVal dir)
      {
         if (varnum.MgNumVal == null)
            return false;

         int itm = varnum.MgNumVal.NUM_2_LONG();

         if (itm == 0)
            return false;

         Field fld = GetFieldOfContextTask(itm);

         if (fld == null)
            return false;

         Task task = (Task)fld.getTask();
         int vee_idx = fld.getId() + 1;

         var sort = new Sort { fldIdx = vee_idx, dir = dir.BoolVal };

         IClientCommand command = CommandFactory.CreateAddUserSortDataviewCommand(task.getTaskTag(), sort);
         task.DataviewManager.Execute(command);

         return true;
      }

      /// <summary>
      ///   This function adds User Sort on task.
      /// </summary>
      private bool add_rt_ranges(ExpVal[] Exp_params, bool locate)
      {
         ExpVal varnum, min, max;

         varnum = Exp_params[0];
         min = Exp_params[1];

         if (varnum.MgNumVal == null)
            return false;

         int itm = varnum.MgNumVal.NUM_2_LONG();

         if (itm == 0)
            return false;

         Field fld = GetFieldOfContextTask(itm);

         if (fld == null)
            return false;

         Task task = (Task)fld.getTask();
         int vee_idx = fld.getId() + 1;

         var rng = new UserRange { veeIdx = vee_idx };

         if (min.IsNull)
            rng.nullMin = true;

         if (!rng.nullMin && (min.Attr == StorageAttribute.ALPHA ||
              min.Attr == StorageAttribute.UNICODE) && min.StrVal.Length == 0)
            rng.discardMin = true;
         else
         {
            if (!rng.nullMin)
            {
               if (!StorageAttributeCheck.isTheSameType(fld.getType(), min.Attr))
                  return false;

               if (StorageAttributeCheck.StorageFldAlphaUnicodeOrBlob(fld.getType(), min.Attr))
                  ConvertExpVal(min, fld.getType());

               rng.min = min.ToMgVal();
            }
         }

         if (Exp_params.Length == 3)
         {
            max = Exp_params[2];
            if (max.IsNull)
               rng.nullMax = true;

            if (!rng.nullMax && (max.Attr == StorageAttribute.ALPHA ||
                 max.Attr == StorageAttribute.UNICODE) && max.StrVal.Length == 0)
               rng.discardMax = true;
            else
            {
               if (!rng.nullMax)
               {
                  if (!StorageAttributeCheck.isTheSameType(fld.getType(), max.Attr))
                     return false;

                  if (StorageAttributeCheck.StorageFldAlphaUnicodeOrBlob(fld.getType(), max.Attr))
                     ConvertExpVal(max, fld.getType());

                  rng.max = max.ToMgVal();
               }
            }
         }
         else
            rng.discardMax = true;

         if (!rng.discardMin || !rng.discardMax)
         {
            if (locate)
            {
               IClientCommand command = CommandFactory.CreateAddUserLocateDataviewCommand(task.getTaskTag(), rng);
               task.DataviewManager.Execute(command);
            }
            else
            {
               IClientCommand command = CommandFactory.CreateAddUserRangeDataviewCommand(task.getTaskTag(), rng);
               task.DataviewManager.Execute(command);
            }
         }

         return true;
      }

      /// <summary>
      /// Refresh the items of data control.
      /// </summary>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      /// <param name="resVal"></param>
      private void eval_op_control_items_refresh(ExpVal val1, ExpVal val2, ExpVal resVal)
      {
         Task     task;
         Boolean  success = false;
         int      parent;

         resVal.Attr = StorageAttribute.BOOLEAN;
         parent = val2.MgNumVal.NUM_2_LONG();

         if ((parent >= 0 && parent < ((Task)ExpTask).getTaskDepth(false)) || parent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(parent);

            if (task != null)
            {
               MgControlBase control = task.getForm().GetCtrl(val1.StrVal);

               //This function is applicable only for Combo box, List box etc. i.e. for choice controls. Also it will refresh items only if Source table is attached to the data control.
               if (control != null && control.isChoiceControl() && control.isDataCtrl())
               {
                  IClientCommand command = CommandFactory.CreateControlItemsRefreshCommand(((Task)task).getTaskTag(), control);
                  task.DataviewManager.CurrentDataviewManager.Execute(command);
                  success = true;
               }
            }
         }

         resVal.BoolVal = success;
      }

      /// <summary>
      ///   Copies a file from client to server
      /// </summary>
      /// <param name = "resVal">contain error code</param>
      /// <param name = "val1">client file name</param>
      /// <param name = "val2">server file name</param>
      private void eval_op_client_file_to_server(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         // Error codes : 0-Success, 1-Source not found, 2-destination cannot be created, 3-problem uploading
         const int SOURCE_NOT_FOUND = 1;
         const int PROBLEM_UPLOADING = 3;
         int retcode = PROBLEM_UPLOADING;

         const String contentType = "application/octet-stream";

         String clientfilename = exp_build_ioname(val1);
         byte[] fileContent = HandleFiles.readToByteArray(clientfilename, "");
         if (fileContent != null)
         {
            String serverFilename = exp_build_ioname(val2);

            byte[] response = RemoteCommandsProcessor.GetInstance().UploadFileToServer(serverFilename, fileContent, contentType);
            if (response != null)
            {
               if (response.Length == 1)
                  retcode = response[0]; // sent from RequestsServer::saveContentToTargetFile()
               else
                  Logger.Instance.WriteExceptionToLog(Encoding.UTF8.GetString(response, 0, response.Length));
            }

         }
         else
            retcode = SOURCE_NOT_FOUND;

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG(retcode);
      }

      /// <summary>
      ///   Rename given file from client machine.
      /// </summary>
      /// <param name = "resVal">Stores results. Will update BoolVal and Attr will be set to STORAGE_ATTR_BOOLEAN.</param>
      /// <param name = "source">File to be renamed.</param>
      /// <param name = "target">New name for the file to be renamed.</param>
      private void eval_op_client_file_rename(ExpVal resVal, ExpVal source, ExpVal target)
      {
         // extract actual file names from ExpVal.
         string sourceFileName = exp_build_ioname(source);

         // extract actual file names from ExpVal.
         string targetFileName = exp_build_ioname(target);

         //check if file exists
         bool isSuccess = HandleFiles.renameFile(sourceFileName, targetFileName);

         //Set result
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = isSuccess;
      }

      /// <summary>
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "direcoty"></param>
      /// <param name = "filter"></param>
      /// <param name = "searchSubDir"></param>
      private void eval_op_client_file_list_get(ExpVal resVal, ExpVal direcoty, ExpVal filter, ExpVal searchSubDir)
      {
         String vecStr;

         //extract parameters from expval

         string strDirectory = exp_build_string(direcoty);
         string strFilter = exp_build_string(filter);
         bool bSearchSubDir = searchSubDir.BoolVal;

         List<String> fileList = HandleFiles.getFileList(strDirectory, strFilter, bSearchSubDir);

         if (fileList.Count > 0)
         {
            var vector = new VectorType(StorageAttribute.ALPHA, BlobType.CONTENT_TYPE_UNKNOWN, "", true,
                                        true, XMLConstants.FILE_NAME_SIZE);

            //loop through list of files.
            for (int i = 0; i < fileList.Count; i++)
            {
               // get the relative file name from starting directory
               String fileName = getRelativeFileName(strDirectory, fileList[i]);

               // Add it in vectorType
               vector.setVecCell(i + 1, fileName, (fileName == null
                                                   ? true
                                                   : false));
            }

            // Convert vectorType to string with prefix.
            vecStr = vector.ToString();
         }
         else
            vecStr = null;

         resVal.Attr = StorageAttribute.BLOB_VECTOR;
         resVal.StrVal = vecStr;

         if (vecStr == null)
            resVal.IsNull = true;
      }

      /// <summary>
      ///   Returns relative path of file from starting directory. e.g. for file c:\temp\d1\f1.txt if starting directory is 
      ///   c:\temp then this function will return d1\f1.txt
      /// </summary>
      /// <param name = "directory">Starting directory</param>
      /// <param name = "fullPath">File object</param>
      /// <returns>Relative filename from starting directory</returns>
      private static String getRelativeFileName(String directory, String fullPath)
      {
         String relativePath = null;

         if (fullPath != null)
         {
            if (directory != null && fullPath.IndexOf(directory) != -1)
            {
               int directorylen = directory.Length;

               //#785571. if search dir does not ends with \ or / then increase length by 1
               // so that (\ or /) will also be removed from path
               if (!directory.EndsWith("\\") && !directory.EndsWith("/"))
                  directorylen++;

               relativePath = fullPath.Substring(directorylen);
            }
         }
         return relativePath;
      }

      /// <summary>
      ///   Get environment variable from client machine.
      /// </summary>
      /// <param name = "resVal">Stores results. Will update BoolVal and Attr will be set to STORAGE_ATTR_BOOLEAN.</param>
      /// <param name = "envVar">The environment variable on the client machine to be retrieved.</param>
      private static void eval_op_client_os_env_get(ExpVal resVal, ExpVal envVar)
      {
         // extract the environment variable to get
         string environmentVariable = exp_build_string(envVar);

         // get the value of the environment variable
         string environmentVariableValue = OSEnvironment.get(environmentVariable);

         //Set result
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = environmentVariableValue;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_menu_check(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         bool retVal = false;

         string entryName = exp_build_string(val1);

         retVal = Manager.MenuManager.MenuCheckByName((Task)ExpTask.GetContextTask(), entryName, val2.BoolVal);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = retVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_menu_enable(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         string entryName = exp_build_string(val1);

         bool retVal = Manager.MenuManager.MenuEnableByName((Task)ExpTask.GetContextTask(), entryName, val2.BoolVal);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = retVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_menu_add(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;
         Boolean success = false;

         Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(((Task)ExpTask).getCtlIdx());

         String menuPath = val2.StrVal; // path where the menu is to be added
         int menuIndex = val1.MgNumVal.NUM_2_LONG(); // index of the menu inside Menu repository

         success = Manager.MenuManager.MenuAdd(mainProg, (Task)((Task)ExpTask).GetContextTask(), menuIndex, menuPath);

         resVal.BoolVal = success;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_menu_remove(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;
         Boolean success = false;

         Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(((Task)ExpTask).getCtlIdx());

         String menuPath = null;
         if (val2 != null) // path from where the menu entries is to be added
            menuPath = val2.StrVal;

         int menuIndex = val1.MgNumVal.NUM_2_LONG(); // index of the menu inside Menu repository

         success = Manager.MenuManager.MenuRemove(mainProg, (Task)((Task)ExpTask).GetContextTask(), menuIndex, menuPath);

         resVal.BoolVal = success;
      }

      /// <summary>
      /// Reset pulldown menu.
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_menu_reset(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(((Task)ExpTask).getCtlIdx());
         resVal.BoolVal = Manager.MenuManager.MenuReset(mainProg, (Task)((Task)ExpTask).GetContextTask());
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_menu_name(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         bool isNameSet = false;

         string entryName = exp_build_string(val1);
         string entryText = exp_build_string(val2);

         isNameSet = Manager.MenuManager.SetMenuName((Task)ExpTask.GetContextTask(), entryName, entryText);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = isNameSet;
      }

      /// <summary>
      ///   Returns the menu program path of the current program menu clicked.
      ///   The menu uid is stored on the menu manager and stored in the new task which is created 
      ///   when the program menu is clicked.
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_menu(ExpVal resVal)
      {
         String MenuPath = "";

         MenuPath = MenuManager.GetMenuPath((Task)ExpTask);

         resVal.Attr = StorageAttribute.UNICODE;
         resVal.StrVal = MenuPath;
      }

      /// <summary>
      ///   Creates/Deletes menu item on show/hide
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "val1">menu entry to be shown/hidden</param>
      /// <param name = "val2">visibility</param>
      private void eval_op_menu_show(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         MGData currMGData = MGDataCollection.Instance.getCurrMGData();

         String entryName = exp_build_string(val1);

         bool retVal = Manager.MenuManager.MenuShowByName((Task)ExpTask.GetContextTask(), entryName, val2.BoolVal);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = retVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_menu_idx(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         int index = 0;

         Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(((Task)ExpTask).getCtlIdx());
         String entryName = exp_build_string(val1);

         index = Manager.MenuManager.GetMenuIdx(mainProg, entryName, val2.BoolVal);

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();
         resVal.MgNumVal.NUM_4_LONG(index);
      }

      private static void eval_op_client_get_unique_machine_id(ExpVal resVal)
      {
         try
         {
            String uniqueID = UniqueIDUtils.GetUniqueMachineID();
            resVal.Attr = StorageAttribute.ALPHA;
            resVal.StrVal = uniqueID;
            resVal.IsNull = false;
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
            resVal.IsNull = true;
         }
      }

      /// <summary>
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "blbVar"></param>
      /// <param name = "fileName"></param>
      private void eval_op_client_blb_2_file(ExpVal resVal, ExpVal blbVar, ExpVal fileName)
      {
         var contentTypeBytes = new[] { (byte)0xFF, (byte)0xFE };
         bool isSuccess = true;
         bool isContentUnicode = false;

         // extract file name
         string FileName = exp_build_ioname(fileName);

         var fileinfo = new FileInfo(FileName);

         //QCR#:724750.Checking for blob variable nullability.If blob is not null,then only create the file and write the value in it.
         //This will avoid creation and writing of the file, in case of null value.
         try
         {
            if (!blbVar.IsNull)
            {
               isContentUnicode = (BlobType.getContentType(blbVar.StrVal) == BlobType.CONTENT_TYPE_UNICODE);

               // extract blob var
               string BlobVar = blbVar.ToMgVal();

               byte[] byteBuf = BlobType.getBytes(BlobVar);

               bool append = false;

               if (byteBuf != null)
               {
                  if (isContentUnicode)
                  {
                     isSuccess = HandleFiles.writeToFile(fileinfo, contentTypeBytes, false, false);
                     append = true;
                  }
                  if (isSuccess)
                     isSuccess = HandleFiles.writeToFile(fileinfo, byteBuf, append, false);
               }
            }
            else
            {
               //Same as online behaviour.If blob is null and the file exist, then delete the file.
               if (fileinfo.Exists)
                  fileinfo.Delete();
               isSuccess = false;
            }
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
            isSuccess = false;
         }

         // Set result
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = isSuccess;
      }

      /// <summary>
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "fileName"></param>
      private void eval_op_client_file_2_blb(ExpVal resVal, ExpVal fileName)
      {
         // extract file name
         string FileName = exp_build_ioname(fileName);

         String BlobVar;
         try
         {
            byte[] byteArray = HandleFiles.readToByteArray(FileName, "");

            char fileContentType = HandleFiles.GetFileContentType(FileName);

            char blobContentType;
            if (fileContentType == HandleFiles.CONTENT_TYPE_UTF8 ||
                fileContentType == HandleFiles.CONTENT_TYPE_BIG_ENDIAN ||
                fileContentType == HandleFiles.CONTENT_TYPE_SMALL_ENDIAN)
            {
               String Str = "";
               blobContentType = BlobType.CONTENT_TYPE_UNICODE;

               // Remove BOM char from bytArray and convert it to a string
               if (fileContentType == HandleFiles.CONTENT_TYPE_UTF8)
                  Str = Encoding.UTF8.GetString(byteArray, HandleFiles.utf8len, byteArray.Length - HandleFiles.utf8len);
               else if (fileContentType == HandleFiles.CONTENT_TYPE_BIG_ENDIAN)
                  Str = Encoding.GetEncoding("UTF-16BE").GetString(byteArray, HandleFiles.endianLen,
                                                                   byteArray.Length - HandleFiles.endianLen);
               else if (fileContentType == HandleFiles.CONTENT_TYPE_SMALL_ENDIAN)
                  Str = Encoding.Unicode.GetString(byteArray, HandleFiles.endianLen,
                                                   byteArray.Length - HandleFiles.endianLen);

               byteArray = Encoding.Unicode.GetBytes(Str);
            }
            else
               blobContentType = BlobType.CONTENT_TYPE_ANSI;

            BlobVar = BlobType.createFromBytes(byteArray, blobContentType);
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
            BlobVar = null;
         }

         // Set result
         resVal.StrVal = BlobVar;

         if (resVal.StrVal == null)
            resVal.IsNull = true;

         resVal.Attr = StorageAttribute.BLOB;
         resVal.IncludeBlobPrefix = true;
      }

      /// <summary>
      ///   get a context value (GetParam)
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "name"></param>
      private void eval_op_getParam(ExpVal resVal, ExpVal name)
      {
          Debug.Assert(!name.IsNull && name.StrVal != null);

          ExpVal retVal = ClientManager.Instance.getGlobalParamsTable().get(name.StrVal);
          if (retVal != null)
              resVal.Copy(retVal);
          else
              resVal.Nullify();
      }

      /// <summary>
      /// set a context value (SetParam)
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="name"></param>
      /// <param name="value"></param>
      private void eval_op_setParam(ExpVal resVal, ExpVal name, ExpVal value)
      {
          Debug.Assert(!name.IsNull && name.StrVal != null);

          resVal.Attr = StorageAttribute.BOOLEAN;
          GlobalParams globalParams = ClientManager.Instance.getGlobalParamsTable();

          globalParams.set(name.StrVal, value);
          resVal.BoolVal = true;
      }

      /// <summary>
      ///   set an environment variable (INIput)
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="value"></param>
      /// <param name="updateIni"></param>
      private void eval_op_iniput(ExpVal resVal, ExpVal value, ExpVal updateIni)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = ClientManager.Instance.getEnvParamsTable().set(value.StrVal, updateIni.BoolVal);
      }

      /// <summary>
      ///   get an environment variable value (INIget)
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="name"></param>
      private void eval_op_iniget(ExpVal resVal, ExpVal nameVal)
      {
         resVal.StrVal = ClientManager.Instance.getEnvParamsTable().get(nameVal.StrVal);
         resVal.Attr = StorageAttribute.ALPHA;
      }

      /// <summary>
      ///   get an environment variable value by section name and location
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="name"></param>
      private void eval_op_inigetln(ExpVal resVal, ExpVal sectionVal, ExpVal numberVal)
      {
         resVal.StrVal = ClientManager.Instance.getEnvParamsTable().getln(sectionVal.StrVal, numberVal.MgNumVal.NUM_2_LONG());
         resVal.Attr = StorageAttribute.ALPHA;
      }

      /// <summary>
      ///   Evaluates MODE literal
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "codes">codes string given in MODE literal</param>
      private void eval_op_m(ExpVal resVal, String codes)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = "";
         int i;

         for (i = 0; i < codes.Length; i++)
         {
            String mode =
            Convert.ToString(cst_code_trans_buf('O', "MCDELRKSON ", codes[i], MsgInterface.EXPTAB_TSK_MODE_RT));
            resVal.StrVal = resVal.StrVal + mode;
         }
      }

      /// <summary>
      ///   Translate codes according to given string and vice versa
      /// </summary>
      /// <param name = "opr">Input/Output</param>
      /// <param name = "intStr">List of internal values used by magic</param>
      /// <param name = "code">Internal/external code of magic</param>
      /// <param name = "strId">index of string</param>
      /// <returns></returns>
      private char cst_code_trans_buf(char opr, String intStr, char code, string strId)
      {
         int i;
         String constStr = ClientManager.Instance.getMessageString(strId);
         IEnumerator tokens = StrUtil.tokenize(constStr, ",").GetEnumerator();
         String token;
         var resVal = (char)(0);

         for (i = 0; i < intStr.Length && tokens.MoveNext(); i++)
         {
            token = (String)tokens.Current;
            int ofs = token.IndexOf('&');

            // if '&' is found, move to character next to '&'
            // else move to first character
            ofs++;

            char currCode = Char.ToUpper(token[ofs]);
            if (opr == 'I')
            {
               if (code == currCode)
               {
                  resVal = intStr[i];
                  break;
               }
            }
            // opr == 'O'
            else
            {
               if (code == intStr[i])
               {
                  resVal = currCode;
                  break;
               }
            }
         }

         return resVal;
      }

      /// <summary>
      /// return the user right
      /// </summary>
      /// <param name = "resVal"></param>
      /// <param name = "valIdx"></param>
      /// right index
      private void eval_op_rights(ExpVal resVal, ExpVal valIdx)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = ClientManager.Instance.getUserRights().getRight(((Task)ExpTask).getCtlIdx(), valIdx.MgNumVal.NUM_2_LONG());
      }

      /// <summary>
      /// implementing ExpCalc - get index of expression, find the expression and evaluate it
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="expVal"> expression index </param>
      private void eval_op_expcalc(ExpVal resVal, ExpVal expVal)
      {
         resVal.IsNull = true;
         // Avoid over 50 recursive calls
         if (_recursiveExpCalcCount < 50)
         {
            _recursiveExpCalcCount++;
            // get expression from idx and evaluate it. pass "none" for type, so we get whatever type the server sent back
            ExpVal retVal =
            ((Task)ExpTask).getExpById(expVal.MgNumVal.NUM_2_LONG()).evaluate(StorageAttribute.NONE);
            _recursiveExpCalcCount--;

            if (retVal != null)
               resVal.Copy(retVal);
         }
      }

      /// <summary>
      ///   ClientRedirectTo.
      ///   Note: If the function succeeds, it does not return, but exits the parent process
      /// </summary>
      /// <param name = "resVal">result</param>
      /// <param name = "val1">user ID</param>
      /// <param name = "val2">password</param>
      /// <param name = "val3">execution properties</param>
      /// <param name = "val4">parallel execution - the current context remains alive</param>
      private void eval_op_ClientRedirect(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3, ExpVal val4)
      {
         String userID = val1.StrVal;
         String password = val2.StrVal;
         String execProperties = val3.StrVal;
         bool parallelExecution = val4.BoolVal;

         // set return values for failure
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = false;

         // copy execution properties and overrun with new data
         MgProperties executionProps = ClientManager.Instance.copyExecutionProps();
         executionProps.loadFromXML(execProperties, Encoding.UTF8);
         executionProps.Remove(ConstInterface.DEBUGCLIENT);
         executionProps.Remove(ConstInterface.LOCALID);

         // set tenant ID, username & password
         executionProps[ConstInterface.MG_TAG_USERNAME] = userID;
         executionProps[ConstInterface.MG_TAG_PASSWORD] = password;
         if (String.IsNullOrEmpty(userID))
            executionProps.Remove(ConstInterface.REQ_SKIP_AUTHENTICATION);

         // arrange new execution properties
         var executionPropertiesXML = new StringBuilder();
         executionProps.storeToXML(executionPropertiesXML);
         String executionPropertiesEncoded = HttpUtility.UrlEncode(executionPropertiesXML.ToString(), Encoding.UTF8);

         try
         {
            //spawn new process
            Process.StartCurrentExecutable(executionPropertiesEncoded);

            if (!parallelExecution)
            {
               // Exit current process
               Task task = ((Task)ExpTask.GetContextTask() ?? (Task)ExpTask);
               ClientManager.Instance.EventsManager.handleInternalEvent(task, InternalInterface.MG_ACT_EXIT);
            }
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }
      }

      /// <summary>
      ///   The function will return TRUE if and only if the session was initiated from a mobile device
      /// </summary>
      /// <param name = "resVal"></param>
      private static void eval_op_IsMobileClient(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = ClientManager.IsMobile;
      }

      /// <summary>
      ///   The function will return: Requests Count,Network Time(ms),Server Time(ms),Middleware Time(ms)
      ///   A typical usage of this function will be to take several snapshots 
      ///   and compare the delta of requests or/and Network Time.
      /// </summary>
      /// <param name = "resVal"></param>
      private void eval_op_ClientSessionStatisticsGet(ExpVal resVal)
      {
         resVal.Attr = StorageAttribute.ALPHA;
         resVal.StrVal = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                                       Statistics.GetRequestsCnt(),
                                       Statistics.GetAccumulatedExternalTime(),
                                       Statistics.GetAccumulatedMiddlewareTime(),
                                       Statistics.GetAccumulatedServerTime(),
                                       Statistics.GetSessionTime(),
                                       Statistics.GetAccumulatedUploadedKB(),
                                       Statistics.GetAccumulatedDownloadedKB(),
                                       Statistics.GetUploadedCompressionRatio(),
                                       Statistics.GetDownloadedCompressionRatio());
         if (Logger.Instance.ShouldLogServerRelatedMessages() || ClientManager.Instance.getDisplayStatisticInfo())
            Logger.Instance.WriteToLog(string.Format("ClientSessionStatisticsGet() activated: {0}", resVal.StrVal), true);
      }

      /// <summary>
      /// implementation of CndRange function: If condition is true, use the 2nd parameter as range. if not - 
      /// mark the result as null
      /// </summary>
      /// <param name="resVal"></param>
      /// <param name="val1"></param>
      /// <param name="val2"></param>
      private void eval_op_CndRange(ExpVal resVal, ExpVal val1, ExpVal val2)
      {
         if (val1.BoolVal)
            resVal.Copy(val2);
         else
            resVal.IsNull = true;
      }

      /// <summary>
      ///   Finds the menu entry for the menu path
      /// </summary>
      /// <param name = "root">current pulldown menu structure</param>
      /// <param name = "menuPath">path where the menu structure is to be added</param>
      /// <param name = "menuPos">contains menu entry as out value, where the menu is to be added</param>
      /// <param name = "idx">contains position to add inside menuPos, as out value</param>
      /// <returns>true if the menuPos is found</returns>
      private static Boolean findMenuPath(Object root, String menuPath, ref Object menuPos, ref int idx)
      {
         Boolean subMenu = false;
         Boolean found = false;
         Object father = null;
         MenuEntry menuEntry = null;
         Boolean ret = false;
         int mnuPos = 0;

         //If MenuPath has trailing slash then add the Menu as SubMenu.
         if (menuPath.EndsWith("\\"))
            subMenu = true;

         String[] tokens = menuPath.Split('\\');
         IEnumerator iMenuEntry;

         foreach (String token in tokens)
         {
            if (String.IsNullOrEmpty(token))
               break;

            mnuPos = 0;

            if (root.GetType() == typeof(MgMenu))
               iMenuEntry = ((MgMenu)root).iterator();
            else if (root.GetType() == typeof(MenuEntryMenu))
               iMenuEntry = ((MenuEntryMenu)root).iterator();
            else
               break;

            while (iMenuEntry.MoveNext())
            {
               menuEntry = (MenuEntry)iMenuEntry.Current;
               if (menuEntry.getName() != null)
               {
                  if (menuEntry.getName().Equals(token))
                  {
                     father = root;
                     root = menuEntry;
                     found = true;
                     break;
                  }
               }
               else
                  found = false;

               mnuPos++;
            }

            if (found == false)
               break;
         }

         if (found)
         {
            if (menuEntry.menuType() == GuiMenuEntry.MenuType.MENU)
            {
               //If Menu type is Menu and have trailing slash in MenuPath
               if (subMenu)
               {
                  idx = 0;
                  menuPos = root;
               }
               // If Menu type is Menu and don't have trailing slash in MenuPath
               else
               {
                  idx = mnuPos + 1;
                  menuPos = father;
               }
               ret = true;
            }
            else
            {
               // If Menu type is not Menu and don't have trailing slash in MenuPath
               if (!subMenu)
               {
                  idx = mnuPos + 1;
                  menuPos = father;
                  ret = true;
               }
            }
         }

         return ret;
      }

      /// <summary>
      ///   returns the external task type of the task
      /// </summary>
      private void eval_op_taskType(ExpVal resVal, ExpVal Parent)
      {
         Task task;

         int iParent = Parent.MgNumVal.NUM_2_LONG();
         if ((iParent >= 0 && iParent < ((Task)ExpTask).getTaskDepth(false)) || iParent == TRIGGER_TASK)
         {
            task = (Task)GetContextTask(iParent);
            if (task != null)
            {
               resVal.StrVal = (task.isMainProg()
                                 ? "MC"
                                 : "C");
            }
            else
               resVal.StrVal = " ";
         }

         resVal.Attr = StorageAttribute.ALPHA;
      }

      /// <summary>
      ///   Function getting the terminal number.
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_terminal(ExpVal resVal)
      {
         int terminal = ClientManager.Instance.getEnvironment().getTerminal();

         ConstructMagicNum(resVal, terminal, StorageAttribute.NUMERIC);
      }

      /// <summary>
      ///   return the project's dir
      /// </summary>
      /// <param name="resVal"></param>
      private void eval_op_projectdir(ExpVal resVal)
      {
         resVal.StrVal = ClientManager.Instance.getEnvironment().getProjDir(ExpTask.getCompIdx());
         resVal.Attr = StorageAttribute.ALPHA;
      }

      /// <summary>
      ///   this method checks if the blob is valid vector i.e. if the blob is in vector's flatten format
      /// </summary>
      /// <param name = "vec">the blob to be checked</param>
      /// <returns> true if valid vector</returns>
      private static bool IsValidVector(ExpVal vec)
      {
         //TODO: do we need to check the blob data as well? Checking just the attribute isn't sufficient?
         return (vec != null && vec.Attr == StorageAttribute.BLOB_VECTOR && VectorType.validateBlobContents(vec.StrVal));
      }

      /// <summary>
      /// This function returns the attribute according to magic, for eg 'L' is returned for boolean.
      /// </summary>
      /// <param name="storageAttr"></param>
      /// <returns>attribute character corresponding to storage attribute.</returns>
      private static char GetAttributeChar(StorageAttribute storageAttr)
      {
         char attr = (char)storageAttr;

         switch (storageAttr)
         {
            case StorageAttribute.BLOB:
               attr = 'B';
               break;

            case StorageAttribute.BOOLEAN:
               attr = 'L';
               break;

            default:
               break;
         }
         return attr;
      }

      /// <summary>
      /// returns Val for the itmIdx
      /// </summary>
      /// <param name="itmIdx"></param>
      /// <returns></returns>
      protected override ExpVal GetItemVal(int itmIdx)
      {
         ExpVal expVal;
         Field fld = (Field)getField(itmIdx);

         if (fld.getType() == StorageAttribute.DOTNET)
         {
            // simulate exp_op_v
            int key = BlobType.getKey(fld.getValue(false));
            var dnMemberInfo = DNManager.getInstance().CreateDNMemberInfo(key);

            expVal = new ExpVal(dnMemberInfo);
         }
         else
            expVal = new ExpVal(fld.getType(), fld.isNull(), fld.getValue(false));

         return expVal;
      }

      /// <summary>
      /// sets the object on 'itmIdx' and recomputes
      /// </summary>
      /// <param name="itmIdx"></param>
      /// <param name="valueToSet"></param>
      protected override void SetItemVal(int itmIdx, Object valueToSet)
      {
         Field fld = (Field)getField(itmIdx);
         String newVal;
         int key = 0;

         if (fld.getType() == StorageAttribute.DOTNET)
         {
            // create temporary entry in DNObjectsCollection and add object to it. call setvalue to
            // set the value to fld (from this temp entry) and do recompute.
            key = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);
            DNManager.getInstance().DNObjectsCollection.Update(key, valueToSet);
            newVal = BlobType.createDotNetBlobPrefix(key);
         }
         else
            newVal = DNConvert.convertDotNetToMagic(valueToSet, fld.getType());

         fld.setValueAndStartRecompute(newVal, false, true, true, false);

         // remove the temporary entry created
         if (key != 0)
            DNManager.getInstance().DNObjectsCollection.Remove(key);
      }

      /// <summary>(protected)
      /// returns last focused task of context.
      /// If handler is executing then returns the task which is the context
      /// of the currently executing handler. This is achieved by calling getCurrTask().
      /// Although the function name sounds incorrect, it returns exactly what we need.
      /// </summary>
      /// <returns></returns>
      protected override ITask GetLastFocusedTask()
      {
         return (ClientManager.Instance.getCurrTask());
      }

      /// <summary>
      /// return all top level forms that exist
      /// </summary>
      /// <param name="contextId"></param>
      /// <returns></returns>
      protected override List<MgFormBase> GetTopMostForms(Int64 contextID)
      {
         return (MGDataCollection.Instance.GetTopMostForms());
      }

      /// <summary>
      /// Moves the caret to specified control
      /// </summary>
      /// <param name="ctrlTask"></param>
      /// <param name="ctrl"></param>
      /// <param name="rowNo"></param>
      /// <returns></returns>
      protected override bool HandleControlGoto(ITask ctrlTask, MgControlBase ctrl, int rowNo)
      {
         Task task = (Task)ctrlTask;
         if (ctrl != null)
         {
            DataView dv = (DataView)task.DataView;
            MgControl tCtrl = (MgControl)ctrl;
            int wantedLine = task.getForm().DisplayLine; // 0-based record index

            // validate specified row
            if (ctrl.IsRepeatable && rowNo > 0)
            {
               task.getForm().getTopIndexFromGUI();
               int top = dv.getTopRecIdx();
               wantedLine = top + rowNo - 1; // 0-based

               // check if the record is exist in the current page
               if ((wantedLine - top - 1) >= task.getForm().getRowsInPage())
                  return false;

               // check for invalid record index
               if (!task.getForm().IsValidRow(wantedLine))
                  return false;
            }

            if (tCtrl.isVisible())
            {
               bool noSubformTask = false;

               if (tCtrl.isSubform() || tCtrl.isFrameFormControl())
               {
                  MgForm subForm = null;
                  if (tCtrl.isSubform())
                     if (tCtrl.getSubformTask() == null)
                        noSubformTask = true;
                     else
                        subForm = (MgForm)tCtrl.getSubformTask().getForm();
                  else
                     subForm = (MgForm)tCtrl.getForm();

                  if (!noSubformTask)
                     tCtrl = subForm.getFirstParkableCtrl();
               }

               // jump to the control
               if (tCtrl != null && !noSubformTask)
               {
                  RunTimeEvent rtEvt;
                  rtEvt = new RunTimeEvent(tCtrl, wantedLine);
                  rtEvt.setInternal(InternalInterface.MG_ACT_CTRL_FOCUS);
                  ClientManager.Instance.EventsManager.addToTail(rtEvt);
                  return true;
               }
            }
         }
         return false;
      }

      /// <summary>
      /// translates the string containing logical name
      /// </summary>
      /// <param name="name">the string containing logical name</param>
      /// <returns>translated string </returns>
      protected override string Translate(string name)
      {
         return (ClientManager.Instance.getEnvParamsTable().translate(name));
      }

      /// <summary>(protected)
      /// this function implements "EditGet()" for RC
      /// 
      /// We have different implementation for Online and RC, because there is difference behavior
      /// OL: 1. In case of choice control, on change of selection, variable is updated immediately (as VC handler should get called)
      ///     2. Radio control - if two controls referring single variable, EditGet() executing inside control modify handler fails because the 
      ///        last parked control still points previous control. The focus is not moved. So using variable's value fixes this.
      /// RC: 1. on change of selection, variable is not updated immediately.
      ///     2. Radio control - if two controls referring single variable , on change of selection CS and CP handlers get executed.
      ///        So last parked control points to selected control. EditGet() returns correct value from CM event handler
      /// </summary>
      /// <param name="ctrl">control</param>
      /// <param name="resVal">out: resultant ExpVal</param>
      /// <returns></returns>
      protected override void EditGet(MgControlBase ctrl, ref ExpVal resVal)
      {
         GetValidatedMgValue(ctrl, ref resVal);
      }

      /// <summary>
      /// Disconnect the database connection of given database name.
      /// </summary>
      /// <param name="val"> Database name</param>
      /// <param name="resVal">Return status of disconnect operation</param>
      private void exp_op_ClientDbDisconnect(ExpVal val, ExpVal resVal)
      {
         string databaseName = val.StrVal;
         Task task = (Task)ExpTask;

         IClientCommand command = CommandFactory.CreateClientDbDisconnectCommand(databaseName);
         ReturnResult result = task.DataviewManager.LocalDataviewManager.Execute(command);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = result.Success;
      }

      /// <summary>
      /// Adds the dataview to destination datasource.
      /// </summary>
      /// <param name="resVal">Return result</param>
      /// <param name="val1">Generation</param>
      /// <param name="val2">task var names</param>
      /// <param name="val3">destination data source number</param>
      /// <param name="val4">destination data source name</param>
      /// <param name="val5">destination columns db names</param>
      private void eval_op_dataview_to_datasource(ExpVal resVal, ExpVal val1, ExpVal val2, ExpVal val3, ExpVal val4, ExpVal val5)
      {
         Task task = null;
         resVal.BoolVal = false;
         int iParent = val1.MgNumVal.NUM_2_LONG();
         resVal.Attr = StorageAttribute.BOOLEAN;
         string error = string.Empty;
         if (iParent >= 0)
         {
            task = (Task)GetContextTask(iParent);

            if (task != null)
            {
               if (!((DataView)task.DataView).HasMainTable)
               {
                  error = "DataViewToDataSource - Task doesn稚 have main data source.";
                  Logger.Instance.WriteExceptionToLog(error);
                  ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
                  return;
               }

               string taskVarName = val2.StrVal.Trim();

               //if taskVarName is empty, then add all the fields from dataview in the task var names list.
               if (string.IsNullOrEmpty(taskVarName))
               {
                  for (int fieldIndex = 0; fieldIndex < task.DataView.GetFieldsTab().getSize(); fieldIndex++)
                  {
                     if (fieldIndex != 0)
                     {
                        taskVarName += ",";
                     }
                     taskVarName += task.DataView.GetFieldsTab().getField(fieldIndex).getVarName();
                  }
               }

               int destinationDSNumber = val3.MgNumVal.NUM_2_LONG();
               string destinationDSName = val4.StrVal;

               string destinationColumns = val5.StrVal.Trim();

               //If destinationColumns is empty, then it will be same as task var list.
               if (string.IsNullOrEmpty(destinationColumns))
               {
                  destinationColumns = taskVarName;
               }

               IClientCommand command = CommandFactory.CreateDataViewToDataSourceCommand(ExpTask.getTaskTag(), iParent, taskVarName, destinationDSNumber, destinationDSName, destinationColumns);
               ReturnResult result = task.DataviewManager.Execute(command);


               if (result.Success)
                  resVal.BoolVal = true;
            }
         }
         if (task == null)
         {
            error = "DataViewToDataSource - Invalid generation specified.";
            Logger.Instance.WriteExceptionToLog(error);
            ClientManager.Instance.ErrorToBeWrittenInServerLog = error;
            return;
         }
      }

      /// <summary>
      /// Deletes a local DataSource
      /// </summary>
      /// <param name="val1">Data Source Number</param>
      /// <param name="val2">Data Source Name</param>
      /// <param name="resVal"></param>
      private void exp_op_ClientDbDelete(ExpVal val1, ExpVal val2, ExpVal resVal)
      {
         int DSNumber = val1.MgNumVal.NUM_2_LONG();
         string DSName = val2.StrVal.Trim();
         Task task = (Task)ExpTask;

         DSName = DSName.Trim();
         IClientCommand command = CommandFactory.CreateClientDbDeleteCommand(DSNumber, DSName);
         ReturnResult result = task.DataviewManager.LocalDataviewManager.Execute(command);

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = result.Success;
      }

      /// <summary>
      /// Executes an SQL statement
      /// </summary>
      /// <param name="val1">Data Source Name</param>
      /// <param name="val2">SQL statement</param>
      /// <param name="resVal"></param>
      /// <param name="addtionalVals">addtionalVals</param>
#if !PocketPC
      private void exp_op_SQLExecute(ExpVal val1, ExpVal val2, ExpVal resVal, params ExpVal[] addtionalVals)
      {
         string databaseName = val1.StrVal;
         string sqlStatement = val2.StrVal;
         Task task = (Task)ExpTask;

         Field[] fields = new Field[addtionalVals.Length];
         StorageAttribute[] storageAttributes = new StorageAttribute[addtionalVals.Length];
         DBField[] dbFields = new DBField[addtionalVals.Length];

         if (addtionalVals.Length > 0)
         {
            for (int i = 0; i < addtionalVals.Length; i++)
            {
               if (addtionalVals[i].MgNumVal == null)
               {
                  //the varset function always returns true.
                  //SetNULL(resVal,STORAGE_ATTR_BOOLEAN);
                  break;
               }

               int itm = addtionalVals[i].MgNumVal.NUM_2_LONG();
               fields[i] = GetFieldOfContextTask(itm);
               storageAttributes[i] = fields[i].getType();
               dbFields[i] = new DBField();
               dbFields[i].Picture = fields[i].GetPicture();
               dbFields[i].Length = fields[i].getSize();

               // We don't care about the actual value of Dec here. We just need to make sure it will have a value
               // larger than 0, so that the conversion from gateway to runtime field will be correct.
               if (dbFields[i].Picture != null && dbFields[i].Picture.Contains("."))
                  dbFields[i].Dec = 1;

               // Update blob content, if required
               switch (fields[i].Storage)
               {
                  case FldStorage.Blob:
                     dbFields[i].Storage = FldStorage.Blob;
                     dbFields[i].BlobContent = BlobContent.Binary;
                     break;
                  case FldStorage.AnsiBlob:
                     dbFields[i].Storage = FldStorage.AnsiBlob;
                     dbFields[i].BlobContent = BlobContent.Ansi;
                     break;
                  case FldStorage.UnicodeBlob:
                     dbFields[i].Storage = FldStorage.UnicodeBlob;
                     dbFields[i].BlobContent = BlobContent.Unicode;
                     break;
               }
            }
         }

         IClientCommand command = CommandFactory.CreateSQLExecuteCommand(databaseName, sqlStatement, storageAttributes, dbFields);
         ReturnResult result = task.DataviewManager.LocalDataviewManager.Execute(command);

         object[] returnedFieldValues = ((SQLExecuteCommand)command).statementReturnedValues;
         for (int i = 0; i < returnedFieldValues.Length; i++)
         {
            String value = returnedFieldValues[i] == null ? null : returnedFieldValues[i].ToString();
            bool setRecordUpdated = (fields[i].getTask() != ExpTask || ((Task)fields[i].getTask()).getBrkLevel() == ConstInterface.BRK_LEVEL_REC_SUFFIX);
            fields[i].setValueAndStartRecompute(value, returnedFieldValues[i] == null, true, setRecordUpdated, false);
            fields[i].updateDisplay();
         }

         resVal.Attr = StorageAttribute.BOOLEAN;
         resVal.BoolVal = result.Success;
      }
#endif
      /// <summary>
      /// Execute ControlSelectProgram() functon.
      /// </summary>
      /// <param name="expVal"></param>
      /// <param name="resVal"></param>
      protected void eval_op_control_select_program(ExpVal expVal, ExpVal resVal)
      {
         int controlID = expVal.MgNumVal.NUM_2_LONG();
         int parent = 0;
         MgControl mgControl = ((Task)ExpTask.GetContextTask()).GetControlFromControlID(controlID - 1, out parent);

         resVal.Attr = StorageAttribute.NUMERIC;
         resVal.MgNumVal = new NUM_TYPE();

         //If control has select program property.
         if (mgControl != null && mgControl.HasSelectProgram())
         {
            Property selectProgProp = mgControl.getProp(PropInterface.PROP_TYPE_SELECT_PROGRAM);
            int realIndex = Int32.Parse(selectProgProp.getValue());
            float programIndex = 0;
            if (realIndex > 0)
            {
               if (parent > 0)
                  programIndex = realIndex + ((float)parent / 100);
               else
                  programIndex = (float)realIndex;
            }

            resVal.MgNumVal =  NUM_TYPE.from_double(programIndex);
         }

      }

#region Nested type: DynamicOperation

      /// <summary>
      ///   The class is used for holding information on operators to perform
      ///   which are result of executing other operators.
      /// </summary>
      private class DynamicOperation
      {
         internal int argCount_;
         internal int opCode_ = ExpressionInterface.EXP_OP_NONE;
      }

#endregion

#region Nested type: ExpStrTracker

      /// <summary>
      ///   This class is used for tracking the current position in the current
      ///   executed expression.
      ///   expStr_ - is the current string of the expression being executed
      ///   posIdx_ - is the current position in the expression (while executing it)
      ///   The class also provides methods for extracting values/operator from
      ///   the expression being executed.
      /// </summary>
      private class ExpStrTracker
      {
         private readonly sbyte[] _expBytes; // Current string of the expression being executed
         private readonly bool _lowHigh = true;
         private readonly bool _nullArithmetic; //true, if task of the expression has null arithmetic == nullify;
         private bool _isNull; //is expression result NULL
         private int _posIdx; // The current position in the expression (while executing it)

         /// <summary>
         ///   initializing the class with the expression to be executed
         /// </summary>
         internal ExpStrTracker(sbyte[] expBytes, bool nullArithmetic)
         {
            _expBytes = new sbyte[expBytes.Length];
            expBytes.CopyTo(_expBytes, 0);
            _nullArithmetic = nullArithmetic;
            _lowHigh = ClientManager.Instance.getEnvironment().getLowHigh();
         }

         /// <summary>
         ///   set result to be NULL
         /// </summary>
         protected internal void setNullResult()
         {
            if (_nullArithmetic)
               _isNull = true;
         }

         /// <summary>
         ///   reset result to be NULL
         /// </summary>
         protected internal void resetNullResult()
         {
            if (_nullArithmetic)
               _isNull = false;
         }

         /// <summary>
         ///   check if the result must be null
         /// </summary>
         protected internal bool isNull()
         {
            return _isNull;
         }

         /// <summary>
         ///   Get a number represented in 1 byte from the expression
         /// </summary>
         /// <returns> the number
         /// </returns>
         internal int get1ByteNumber()
         {
            int num = (_expBytes[_posIdx] >= 0
                       ? _expBytes[_posIdx]
                       : 256 + _expBytes[_posIdx]);
            _posIdx += 1;

            return num;
         }

         /// <summary>
         ///   Get a number represented in 2 bytes from the expression
         /// </summary>
         /// <returns> the number
         /// </returns>
         internal int get2ByteNumber()
         {
            int c1 = (_expBytes[_posIdx] >= 0
                      ? _expBytes[_posIdx]
                      : 256 + _expBytes[_posIdx]);
            _posIdx += 1;
            int c2 = (_expBytes[_posIdx] >= 0
                      ? _expBytes[_posIdx]
                      : 256 + _expBytes[_posIdx]);
            _posIdx += 1;

            int num = _lowHigh
                      ? MK_SHRT((short)c2, (short)c1)
                      : MK_SHRT((short)c1, (short)c2);

            return num;
         }

         /// <summary>
         ///   Get a number represented in 4 byte from the expression
         /// </summary>
         /// <returns>the number</returns>
         internal int get4ByteNumber()
         {
            int c4 = (_expBytes[_posIdx] >= 0
                      ? _expBytes[_posIdx]
                      : 256 + _expBytes[_posIdx]);
            _posIdx += 1;
            int c3 = (_expBytes[_posIdx] >= 0
                      ? _expBytes[_posIdx]
                      : 256 + _expBytes[_posIdx]);
            _posIdx += 1;
            int c2 = (_expBytes[_posIdx] >= 0
                      ? _expBytes[_posIdx]
                      : 256 + _expBytes[_posIdx]);
            _posIdx += 1;
            int c1 = (_expBytes[_posIdx] >= 0
                      ? _expBytes[_posIdx]
                      : 256 + _expBytes[_posIdx]);
            _posIdx += 1;

            int num = _lowHigh
                      ? MK_LONG(c1, c2, c3, c4)
                      : MK_LONG(c4, c3, c2, c1);

            return num;
         }

         /// <summary>
         ///   Get alpha string from the expression by its length
         /// </summary>
         /// <param name = "len">        length of string to extract</param>
         /// <param name = "updateIdx">  the current position will be incremented</param>
         /// <param name = "isUnicode"></param>
         /// <returns> the number</returns>
         internal String getString(int len, bool updateIdx, bool isUnicode)
         {
            String str = "";
            int bytes;

            if (isUnicode == false)
            {
               bytes = len;
               var tmpChar = new char[_expBytes.Length];
               for (int i = 0; i < _expBytes.Length; i++)
                  tmpChar[i] = (char)_expBytes[i];

               str = new String(tmpChar, _posIdx, len);
            }
            else
            {
               bytes = len * ConstInterface.BYTES_IN_CHAR;

               var tmp = new byte[bytes];
               const int flip = 0;

               int increment = (!_lowHigh)
                               ? ConstInterface.BYTES_IN_CHAR
                               : 1;

               for (int i = _posIdx; i < _posIdx + bytes; i = i + increment)
               {
                  if (!_lowHigh)
                  {
                     tmp[i - _posIdx] = (byte)_expBytes[i + 1 + flip];
                     tmp[i + 1 - _posIdx] = (byte)_expBytes[i + flip];
                  }
                  else
                     tmp[i - _posIdx] = (byte)_expBytes[i + flip];
                  //tmp[i - posIdx_ + 1 ] = (byte)expBytes_[i + 1 - flip];
               }

               try
               {
                  str = Encoding.Unicode.GetString(tmp, 0, tmp.Length);
               }
               catch (Exception ex)
               {
                  Logger.Instance.WriteExceptionToLog(ex);
               }
            }
            if (updateIdx)
               _posIdx += bytes;
            return str;
         }

         /// To enhance the performance of the runtime expression evaluator, 
         /// the function pointer to the relevant entry in the EXP_OP_TAB is saved 
         /// in the polished expression instead of referencing it each time we need 
         /// it in runtime. But in Browser Client we do not (rather we cannot) use 
         /// this function pointer and so we have to skip it.
         /// <summary>
         ///   To enhance the performance of the runtime expression evaluator, 
         ///   the function pointer to the relevant entry in the EXP_OP_TAB is saved 
         ///   in the polished expression instead of referencing it each time we need 
         ///   it in runtime. But in Browser Client we do not (rather we cannot) use 
         ///   this function pointer and so we have to skip it.
         /// </summary>
         private void skipOpFunctionPtr()
         {
            _posIdx += ExpressionInterface.EXP_OPER_FUNC_PTR_LEN;
         }

         /// <summary>
         ///   Get an operator from the current position in the expression
         /// </summary>
         /// <returns> the current operator</returns>
         internal int getOpCode()
         {
            var tmp = new sbyte[2];

            tmp[0] = (_lowHigh
                      ? _expBytes[_posIdx]
                      : _expBytes[_posIdx + 1]);
            tmp[1] = (_lowHigh
                      ? _expBytes[_posIdx + 1]
                      : _expBytes[_posIdx]);

            // the HIGH byte is the most significant here
            int num = (tmp[1] >= 0
                       ? tmp[1]
                       : 256 + tmp[1]);
            num <<= 8;
            num |= (tmp[0] >= 0
                    ? tmp[0]
                    : 256 + tmp[0]);
            _posIdx += ExpressionInterface.EXP_OPER_LEN;

            skipOpFunctionPtr();

            return num;
         }

         /// <summary>
         ///   Get a variable idx from the current position in the expression
         /// </summary>
         /// <returns> the variable idx</returns>
         internal int getVarIdx()
         {
            int flip = 0;

            if (_lowHigh)
               flip = 1 - flip;

            // the first byte is the least significant here
            int num = (_expBytes[_posIdx + flip] >= 0
                       ? _expBytes[_posIdx + flip]
                       : 256 + _expBytes[_posIdx + flip]);
            num <<= 8;
            flip = 1 - flip;
            num |= (_expBytes[_posIdx + flip] >= 0
                    ? _expBytes[_posIdx + flip]
                    : 256 + _expBytes[_posIdx + flip]);
            _posIdx += ExpressionInterface.EXP_OPER_LEN;

            return num;
         }

         /// <summary>
         ///   Get a Magic Number from the expression
         /// </summary>
         /// <param name = "len"> length of the magic number within the expression</param>
         /// <param name = "updateIdx">  the current position will be incremented</param>
         /// <returns> the Magic Number</returns>
         internal NUM_TYPE getMagicNumber(int len, bool updateIdx)
         {
            var mgNum = new NUM_TYPE(_expBytes, _posIdx, len);
            if (updateIdx)
               _posIdx += len;
            return mgNum;
         }

         /// <summary>
         ///   Skips an operator within an expression from the current position
         /// </summary>
         internal void skipOperator()
         {
            int argsRemain = 1;
            ExpressionDict.ExpDesc expDesc;

            while (argsRemain > 0)
            {
               argsRemain--;
               int opCode = getOpCode();
               switch (opCode)
               {
                  //--------------------------------------------------------------
                  // Skip String value
                  //--------------------------------------------------------------
                  case ExpressionInterface.EXP_OP_A:
                  case ExpressionInterface.EXP_OP_H:
                     int len = get4ByteNumber();
                     _posIdx += (len * ConstInterface.BYTES_IN_CHAR);
                     // since the server sends us both Unicode string Ansi string for 
                     // each string in the expression, and the client uses only unicode,
                     // we will diregard the Ansi string
                     len = get4ByteNumber();
                     _posIdx += len;
                     break;

                  case ExpressionInterface.EXP_OP_EXT_A:
                     len = get2ByteNumber();
                     _posIdx += len;
                     break;

                  //--------------------------------------------------------------
                  // Skip Magic Number value
                  //--------------------------------------------------------------

                  case ExpressionInterface.EXP_OP_N:
                  case ExpressionInterface.EXP_OP_T:
                  case ExpressionInterface.EXP_OP_D:
                  case ExpressionInterface.EXP_OP_M:
                  case ExpressionInterface.EXP_OP_K:
                  case ExpressionInterface.EXP_OP_E:
                     len = get2ByteNumber();
                     _posIdx += len;
                     break;

                  //--------------------------------------------------------------
                  // Skip Prog and File literals which also include a component reference
                  //--------------------------------------------------------------

                  case ExpressionInterface.EXP_OP_F:
                  case ExpressionInterface.EXP_OP_P:
                     len = get2ByteNumber();
                     _posIdx += len;
                     len = get2ByteNumber();
                     _posIdx += len;
                     break;

                  //--------------------------------------------------------------
                  // Skip Logical Value
                  //--------------------------------------------------------------

                  case ExpressionInterface.EXP_OP_L:
                     _posIdx += 2;
                     break;

                  //--------------------------------------------------------------
                  // Skip variable
                  //--------------------------------------------------------------

                  case ExpressionInterface.EXP_OP_V:
                     //if something wrong with escape variable, change this number.
                     _posIdx += (PARENT_LEN + LONG_OBJECT_LEN);
                     break;

                  case ExpressionInterface.EXP_OP_FORM:
                     _posIdx += (PARENT_LEN + LONG_OBJECT_LEN);
                     break;

                  case ExpressionInterface.EXP_OP_VAR:
                     _posIdx += (PARENT_LEN + LONG_OBJECT_LEN);
                     break;

                  case ExpressionInterface.EXP_OP_MNU:
                     len = get2ByteNumber();
                     _posIdx += len;
                     break;

                  case ExpressionInterface.EXP_OP_RIGHT_LITERAL:
                     len = get2ByteNumber();
                     _posIdx += len;
                     // Skip extra unused string stored after literal
                     len = get2ByteNumber();
                     _posIdx += len;
                     break;

                  /* case EXP_OP_ACT:
                  posIdx_ +=2;
                  break;
                     
                  case EXP_OP_KBD:
                  posIdx_ ++;
                  break; */

                  //--------------------------------------------------------------
                  // Current operator is a function, so we just need to update
                  //  the number of arguments to skip
                  //--------------------------------------------------------------

                  default:
                     expDesc = ExpressionDict.expDesc[opCode];
                     if (expDesc.ArgCount_ < 0)
                        argsRemain += get1ByteNumber();
                     else
                        argsRemain += expDesc.ArgCount_;
                     break;
               }
            }
         }
      }

#endregion

#region Nested type: FileInfoType

      private enum FileInfoType
      {
         FI_Name = 1,
         FI_Path,
         FI_FullName,
         FI_Attributes,
         FI_Size,
         FI_CDate,
         FI_CTime,
         FI_MDate,
         FI_MTime,
         FI_ADate,
         FI_ATime
      } ;

#endregion

#region Nested type: NullValueException

      /// <summary>
      ///   This exception used when at least one of the operands is null
      /// </summary>
      internal class NullValueException : Exception
      {
         private readonly StorageAttribute _attr = StorageAttribute.NONE;

         internal NullValueException(StorageAttribute attr)
         {
            _attr = attr;
         }

         protected internal StorageAttribute getAttr()
         {
            return _attr;
         }
      }
#endregion
   }
}
