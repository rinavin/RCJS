using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Task = com.magicsoftware.richclient.tasks.Task;
using RunTimeEvent = com.magicsoftware.richclient.events.RunTimeEvent;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.dotnet;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.rt;

namespace com.magicsoftware.richclient.exp
{
   /// <summary>
   ///   data for <exp ...> tag
   /// </summary>
   internal class Expression : IResultValue
   {
      private IClientCommand _cmdToServer;
      private CommandsTable _cmdsToServer;
      private char _computeBy;
      private sbyte[] _expBytes;
      private int _id = -1;
      private int _dnObjectCollectionKey; // reference key for DotNet object in DNObjectsCollection
      private StorageAttribute _prevResType = StorageAttribute.NONE;
      private String _resultValue; // the result returned by the server
      private Task _task;
      private StorageAttribute _type = StorageAttribute.NONE; // the type of the result which is returned by the server

      /// <summary>
      ///   CTOR
      /// </summary>
      protected internal Expression()
      {
      }

      /// <summary>
      ///   evaluate the expression and return the result
      /// </summary>
      /// <param name = "resType">is the expected type </param>
      /// <param name = "length">of expected Alpha string </param>
      /// <returns> evaluated value or null if value evaluated to null (by ExpressionEvaluator) </returns>
      internal String evaluate(StorageAttribute resType, int length)
      {
         ExpressionEvaluator.ExpVal expVal = null;
         String retVal = null;

         if (computedByClient())
         {
            expVal = ExpressionEvaluator.eval(_expBytes, resType, _task);

            ConvertExpValForDotNet(expVal);

            if (expVal.IsNull)
            {
               // even if actual dotnet obj is null, we need to return blobPrefix
               if (expVal.Attr == StorageAttribute.DOTNET)
                  retVal = expVal.ToMgVal();
               else
                  retVal = null;
            }
            else if (resType == StorageAttribute.BLOB_VECTOR && expVal.Attr == StorageAttribute.BLOB)
            {
               if (VectorType.validateBlobContents(expVal.StrVal))
                  retVal = expVal.ToMgVal();
               else
                  retVal = null;
            }
            else if (expVal.Attr == StorageAttribute.BLOB_VECTOR &&
                     resType != StorageAttribute.BLOB_VECTOR && resType != StorageAttribute.BLOB)
               retVal = null;
            else
               retVal = expVal.ToMgVal();
         }
         else
         {
            RunTimeEvent rtEvt = ClientManager.Instance.EventsManager.getLastRtEvent();
            Task mprgCreator = null;

            if (rtEvt != null)
               mprgCreator = rtEvt.getMainPrgCreator();

            // create a new command object only when necessary
            if (resType != _prevResType)
               _cmdToServer = CommandFactory.CreateEvaluateCommand(_task.getTaskTag(), resType, _id, length, mprgCreator);
            ClientManager.Instance.execRequestWithSubformRecordCycle(_cmdsToServer, _cmdToServer, this, _task);

            if (resType != StorageAttribute.BLOB && resType != StorageAttribute.BLOB_VECTOR)
               retVal = _resultValue;
            else if (_resultValue != null && _resultValue.Equals(" "))
               retVal = "";
            else
               retVal = RecordUtils.byteStreamToString(_resultValue);
         }
         _prevResType = resType;
         return retVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resType"></param>
      /// <param name="length"></param>
      /// <returns></returns>
      internal bool DiscardCndRangeResult()
      {
         return ExpressionEvaluator.DiscardCndRangeResult(_expBytes, _task);       
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="resType"></param>
      /// <returns></returns>
      internal ExpressionEvaluator.ExpVal evaluate(StorageAttribute resType)
      {
         ExpressionEvaluator.ExpVal expVal = null;
         String retVal;
         bool isNull = false;

         if (computedByClient())
            expVal = ExpressionEvaluator.eval(_expBytes, resType, _task);
         else
         {
            RunTimeEvent rtEvt = ClientManager.Instance.EventsManager.getLastRtEvent();
            Task mprgCreator = null;

            if (rtEvt != null)
               mprgCreator = rtEvt.getMainPrgCreator();

            // create a new command object only when necessary
            if (resType != _prevResType)
               _cmdToServer = CommandFactory.CreateEvaluateCommand(_task.getTaskTag(), resType, _id, 0, mprgCreator);
            ClientManager.Instance.execRequestWithSubformRecordCycle(_cmdsToServer, _cmdToServer, this, _task);

            if (resType != StorageAttribute.BLOB && resType != StorageAttribute.BLOB_VECTOR)
               retVal = _resultValue;
            else if (_resultValue != null && _resultValue.Equals(" "))
               retVal = "";
            else
               retVal = RecordUtils.byteStreamToString(_resultValue);

            if (retVal == null)
               isNull = true;
            // If we don't know what result type we got, and want to keep it, as in ExpCalc
            if (resType == StorageAttribute.NONE)
               resType = _type;

            expVal = new ExpressionEvaluator.ExpVal(resType, isNull, retVal);
         }
         _prevResType = resType;
         return expVal;
      }

      /// <summary>
      ///   evaluate the expression and return the ReturnValue
      /// </summary>
      internal ReturnValue evaluate(int length)
      {
         ExpressionEvaluator.ExpVal expVal = null;
         StorageAttribute resType = StorageAttribute.NONE;
         ReturnValue retVal;
         String val = null;

         if (computedByClient())
         {
            expVal = ExpressionEvaluator.eval(_expBytes, resType, _task);

            ConvertExpValForDotNet(expVal);

            // even if actual dotnet obj is null, we need to return blobPrefix
            if (expVal.IsNull && expVal.Attr != StorageAttribute.DOTNET)
               val = null;
            else
               val = expVal.ToMgVal();
            resType = expVal.Attr;
         }
         else
         {
            val = evaluate(resType, length);
            resType = _type;
         }
         retVal = new ReturnValue(val, resType);
         return retVal;
      }

      /// <summary>
      ///   parse the expression
      /// </summary>
      /// <param name = "taskRef">a reference to the owner task</param>
      protected internal void fillData(Task taskRef)
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         List<String> tokensVector;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());

         if (_task == null)
            _task = taskRef;

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            //last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_EXP) + ConstInterface.MG_TAG_EXP.Length);

            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            initElements(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); //to delete "/>" too
         }
         else
            Logger.Instance.WriteExceptionToLog("in Command.FillData() out of string bounds");
      }

      /// <summary>
      ///   parse the expression XML tag
      /// </summary>
      /// <param name = "tokensVector">the vector of attributes and their values
      /// </param>
      // TODO: NEW JDK - tokensVector - use ArrayList instead of Vector	
      private void initElements(List<String> tokensVector)
      {
         String expStr;
         String attribute, valueStr;

         for (int j = 0;
              j < tokensVector.Count;
              j += 2)
         {
            attribute = (tokensVector[j]);
            valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case XMLConstants.MG_ATTR_VALUE:
                  //if we work in hex
                  if (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1)
                  {
                     expStr = valueStr;
                     buildByteArray(expStr);
                  }
                  else
                     _expBytes = Misc.ToSByteArray(Base64.decodeToByte(valueStr));
                  break;

               case XMLConstants.MG_ATTR_ID:
                  _id = XmlParser.getInt(valueStr);
                  break;

               case ConstInterface.MG_ATTR_COMPUTE_BY:
                  _computeBy = valueStr[0];
                  break;

               default:
                  Logger.Instance.WriteExceptionToLog(
                     "There is no such tag in Expression.initElements class. Insert case to Expression.initElements for " +
                     attribute);
                  break;
            }
         }
         if (!computedByClient())
            _cmdsToServer = _task.getMGData().CmdsToServer;

         // add an entry into DNObjectsCollection and store the key reference in ëDNObjectsCollectionKeyÅE
         _dnObjectCollectionKey = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);
      }

      /// <summary>
      ///   Build the expression as a byte array
      /// </summary>
      private void buildByteArray(String expStr)
      {
         String twoHexDigits;

         if (expStr == null || expStr.Length == 0 || expStr.Length%2 != 0)
         {
            Logger.Instance.WriteExceptionToLog("in Expression.buildByteArray() expStr cannot be changed " + expStr);
            return;
         }

         _expBytes = new sbyte[expStr.Length/2];
         for (int i = 0;
              i < expStr.Length;
              i += 2)
         {
            twoHexDigits = expStr.Substring(i, 2);
            _expBytes[i/2] = NUM_TYPE.toSByte(Convert.ToInt32(twoHexDigits, 16));
         }
      }

      /// <summary>
      ///   get ID of the Expression
      /// </summary>
      internal int getId()
      {
         return _id;
      }

      /// <summary>
      ///   returns true if the expression may be computed by the Client
      /// </summary>
      private bool computedByClient()
      {
         return (_computeBy != 'S'); // i.e, Client or Don't care
      }

      /// <summary>
      ///   returns true if the expression can be computed ONLY by the Client
      /// </summary>
      internal bool computedByClientOnly()
      {
         return (_computeBy == 'C');
      }

      /// <summary>
      ///   returns true if the expression can be computed ONLY by the Server
      /// </summary>
      protected internal bool computedByServerOnly()
      {
         return (_computeBy == 'S');
      }

      /// <summary>
      ///   returns the task
      /// </summary>
      internal Task getTask()
      {
         return _task;
      }

      /// <summary>
      ///   set result value by the Result command
      /// </summary>
      /// <param name = "result">the result computed by the server
      /// </param>
      /// <param name = "type_">the type of the result which was computed by the server
      /// </param>
      public void SetResultValue(String result, StorageAttribute type_)
      {
         _resultValue = result;
         _type = type_;
      }

      /// <summary>
      ///   gets the DNObjectsCollectionKey
      /// </summary>
      /// <returns></returns>
      internal int GetDNObjectCollectionKey()
      {
         return _dnObjectCollectionKey;
      }


      /// <summary>
      ///   update the result into expression DNObjectsCollection Entry and modifies Expval to contain the blob prefix
      /// </summary>
      /// <param name = "expVal"></param>
      private void ConvertExpValForDotNet(ExpressionEvaluator.ExpVal expVal)
      {
         if (expVal.Attr == StorageAttribute.DOTNET)
         {
            // update the value at DNObjectsCollection Key. This is updated into Expression DNObjectsCollection key, so no need to Cast.                     
            DNManager.getInstance().DNObjectsCollection.Update(_dnObjectCollectionKey, expVal.DnMemberInfo.value);

            bool isNull = (expVal.DnMemberInfo.value == null);

            expVal.Nullify();
            expVal.Init(StorageAttribute.DOTNET, isNull, BlobType.createDotNetBlobPrefix(_dnObjectCollectionKey));
         }
      }

      public override string ToString()
      {
         return String.Format("{{Expression #{0} of {1}}}", this._id, this._task);
      }

      #region Nested type: ReturnValue

      /// <summary>
      ///   represents the returned evaluated expression value and it's type (attribute)
      /// </summary>
      internal class ReturnValue
      {
         internal readonly String mgVal;
         internal readonly StorageAttribute type;

         /// <summary>
         ///   CTOR
         /// </summary>
         protected internal ReturnValue(String mgVal_, StorageAttribute type_)
         {
            mgVal = mgVal_;
            type = type_;
         }
      }

      #endregion
   }
}
