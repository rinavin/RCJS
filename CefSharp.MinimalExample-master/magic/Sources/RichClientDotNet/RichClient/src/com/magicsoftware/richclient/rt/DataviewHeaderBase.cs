using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using System.Collections.Specialized;
using System.Collections;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient.rt
{

  
   /// <summary>
   ///   Summary description for Link.
   /// </summary>
   internal abstract class DataviewHeaderBase 
   {
      protected readonly YesNoExp _cond;
                                //will be generated or from the link condition or from the link condition value tag

      protected readonly Task _task;
      internal List<Boundary> Loc;
      protected char _dir;
      protected int _id; //the id of the link in the task
      protected int _keyIdx; //the key (from the tableCache) that this 

    
 
      private string _retVal = null;
      /// <summary>
      /// do not use directly
      /// </summary>
      private Field returnfield;

      /// <summary>
      /// return return field for the link
      /// </summary>
      internal Field ReturnField
      {
         get
         {
            if (returnfield == null && _retVal != null)
               returnfield = (Field)Task.getFieldByValueStr(_retVal);

            return returnfield;
         }
      }
      protected LnkEval_Cond _linkEvalCondition;

      /// <summary>
      /// link Mode
      /// </summary>
      public LnkMode Mode { get; private set; }
      public int LinkStartAfterField { get; private set; }
      public int KeyExpression { get; private set; }

      /// <summary>
      /// true if this link represents Main source
      /// </summary>
      public bool IsMainSource
      {
         get
         {
            return _id == -1;
         }
      }

      /// <summary>
      /// Task
      /// </summary>
      internal Task Task
      {
         get
         {
            return _task;
         }
      }

      /// <summary>
      /// Fields
      /// </summary>
      internal List<Field> Fields
      {
         get
         {
            return ((FieldsTable)Task.DataView.GetFieldsTab()).getLinkFields(_id);
         }
      }

 
 
      /// <summary>
      ///   initialize the link for a given task
      /// </summary>
      internal DataviewHeaderBase(Task task)
      {
         _task = task;
         _keyIdx = - 1;
         _cond = new YesNoExp(true);
      }

     
      /// <summary>
      /// 
      /// </summary>
      /// <param name="attributes"></param>
      internal void SetAttributes(NameValueCollection attributes)
      {        
         IEnumerator enumerator = attributes.GetEnumerator();
         while (enumerator.MoveNext())
         {
            String attr = (String)enumerator.Current;
            setAttribute(attr, attributes[attr]);
         }

      }

      /// <summary>
      ///   parse the link XML tag
      /// </summary>
      /// <param name = "tokensVector">the vector of attributes and their values</param>
      protected virtual void setAttribute(string attribute, string valueStr)
      {

         switch (attribute)
         {
            case XMLConstants.MG_ATTR_ID:
               _id = XmlParser.getInt(valueStr);
               break;
           
            case ConstInterface.MG_ATTR_KEY:
               _keyIdx = XmlParser.getInt(valueStr);
               break;

            case ConstInterface.MG_ATTR_KEY_EXP:
               KeyExpression = XmlParser.getInt(valueStr);
               break;
               
            case ConstInterface.MG_ATTR_DIR:
               _dir = valueStr[0];
               break;
            case ConstInterface.MG_ATTR_COND:
            case ConstInterface.MG_ATTR_COND_RES:
               _cond.setVal(_task, valueStr);
               break;
            case ConstInterface.MG_ATTR_RET_VAL:
               _retVal = valueStr;
               
               break;
            case ConstInterface.MG_ATTR_LINK_EVAL_CONDITION:
               _linkEvalCondition = (LnkEval_Cond)valueStr[0];
               break;

            case ConstInterface.MG_ATTR_LINK_MODE:
               Mode = (LnkMode)valueStr[0];
               break;

            case ConstInterface.MG_ATTR_LINK_START:
               LinkStartAfterField = XmlParser.getInt(valueStr);
               break;

            default:
               Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
               break;

         }
      }

      /// <summary>
      /// update link result
      /// </summary>
      /// <param name="curRec">current record</param>
      /// <param name="ret">link success</param>
      public void SetReturnValue(com.magicsoftware.unipaas.management.data.IRecord curRec, bool ret, bool recompute)
      {
         Field retFld = ReturnField;
         
         if (retFld != null)
         {
            bool enforceValiableChange = retFld.getTask() != Task;

            //if we are fetching first chunk we didn't updated the executuion stack yet
            ClientManager.Instance.EventsManager.pushNewExecStacks();
            //same as NonRecomputeVarChangeEvent on server fetch.cpp

            //if the return field is numeric, then convert result value toNum() before passing to setValueAndStartRecompute().
            string result = ret ? "1" : "0";
            if (retFld.getType() == StorageAttribute.NUMERIC)
               result = DisplayConvertor.Instance.toNum(result, new PIC(retFld.getPicture(), StorageAttribute.NUMERIC, 0), 0);

            retFld.setValueAndStartRecompute(result, false, recompute, false, false, enforceValiableChange);

            ClientManager.Instance.EventsManager.popNewExecStacks();

            if (!recompute)
               retFld.setModified();

            retFld.invalidate(true, false);

            // If field is from another task, need to update the display as well
            if (retFld.getTask() != Task)
               retFld.updateDisplay();
         }
      }


      /// <summary>
      ///   returns the id of the link
      /// </summary>
      public int Id
      {
         get { return _id; }
      }

      public LnkEval_Cond LinkEvaluateCondition
      {
         get { return _linkEvalCondition; }
      }

      public bool EvaluateLinkCondition()
      {
         return _cond.getVal(); 
      }

      internal abstract bool getLinkedRecord(Record curRec);

      /// <summary>
      /// initialize link fields
      /// </summary>
      /// <param name="currRec"></param>
      public void InitLinkFields(com.magicsoftware.unipaas.management.data.IRecord currRec)
      {
          //init the cureent rec fields that belong to the link
          foreach (Field field in Fields)
          {
              bool isNull = field.isNull();
              String result = field.getValue(false); ;
              field.getInitExpVal(ref result, ref isNull);
              //this function knows to take either the null value or the defualt value 
              ((Record)currRec).SetFieldValue(field.getId(), isNull, result);
              field.invalidate(true, false);
          }
      }

   }
}
