using System;
using System.Collections;
using System.Collections.Generic;
using com.magicsoftware.richclient.events;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.dotnet;
using System.Diagnostics;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   data for <fldh ...>
   /// </summary>
   internal class Field : unipaas.management.data.Field
   {
      internal const bool CLEAR_FLAGS = true;
      internal const bool LEAVE_FLAGS = false;

      private static NUM_TYPE NUM0;
      private static NUM_TYPE NUM1;

      private readonly YesNoExp _linkExp;
      private Recompute _recompute;
      private int _argFldKey;
      internal int CacheTableFldIdx { get; private set; }
      private bool _causeTableInvalidation; //change in this field will cause invalidation of all rows in table
      private MgForm _form;
      private bool _hasChangeEvent; //if TRUE, there is a handler to run when this field is updated
      private bool _hasZoomHandler; //if TRUE, there is a zoom handler for this field
      private bool _inEvalProcess;
      private Expression _initExp;
      private bool _invalidValue = true;
      private bool _isLinkFld; //if TRUE, it means that the field belongs to a link
      private bool _isVirtual;
      private int _dataviewHeaderId = -1; //members related to table cache
      private bool _linkCreate; // If true then it's a real field which belongs to link create.
      internal Boundary Locate { get; private set; }
      internal Boundary Range { get; private set; }
      private bool _isParam;
      private bool _prevIsNull;
      private String _prevValue;
      private String _tableName;
      private int _indexInTable = -1;

      private bool _virAsReal; // If true then a virtual field is computed as real (needed for link ret value)
      protected String _val;
      private bool modifiedAtLeastOnce;
      private bool _isNull;
      private bool _clearVectorType = true; // do we need to update the vector object
      private VectorType _vectorType; // the vector representation
      internal bool IsEventHandlerField { get; set; }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "dataview">a reference to the dataview</param>
      /// <param name = "id">counter of the field in FieldTable</param>
      protected internal Field(DataView dataview, int id)
         : base(id)
      {
         if (NUM0 == null)
         {
            NUM0 = new NUM_TYPE();
            NUM1 = new NUM_TYPE();
            NUM0.NUM_4_LONG(0);
            NUM1.NUM_4_LONG(1);
         }

         _dataview = dataview;
         _linkExp = new YesNoExp(false);
         _default_date = DisplayConvertor.Instance.disp2mg(PICInterface.DEFAULT_DATE, null,
                                                                new PIC("6", StorageAttribute.NUMERIC,
                                                                        getTask().getCompIdx()),
                                                                getTask().getCompIdx(),
                                                                BlobType.CONTENT_TYPE_UNKNOWN);
      }

      protected internal bool VirAsReal
      {
         get { return _virAsReal; }
      }

      internal bool IsVirtual
      {
         get { return _isVirtual; }
      }

      /// <summary>
      /// index in table
      /// </summary>
      public int IndexInTable
      {
         get { return _indexInTable; }
      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every foundelement</param>
      public override void initElements(List<String> tokensVector)
      {
         base.initElements(tokensVector);

         // if type is DotNet field, add an empty entry into DNObjectsCollection and create a blob using key
         if (_type == StorageAttribute.DOTNET)
         {
            int key = DNManager.getInstance().DNObjectsCollection.CreateEntry(DNType);

            DNManager.getInstance().DNObjectFieldCollection.createEntry(key, this);

            _val = BlobType.createDotNetBlobPrefix(key);
         }
      }

      /// <summary>
      ///   set the field attribute
      /// </summary>
      protected override bool setAttribute(string attribute, string valueStr)
      {
         bool isTagProcessed = base.setAttribute(attribute, valueStr);
         if (!isTagProcessed)
         {
            String[] data = StrUtil.tokenize(valueStr, ",");

            isTagProcessed = true;
            switch (attribute)
            {
               case ConstInterface.MG_ATTR_VIRTUAL:
                  _isVirtual = (XmlParser.getInt(valueStr) != 0);
                  break;
               case ConstInterface.MG_ATTR_PARAM:
                  _isParam = (XmlParser.getInt(valueStr) != 0);
                  if (!_isVirtual && _isParam)
                     // possible error - we rely on the order of XML attributes
                     throw new ApplicationException(
                        "in Field.initElements(): non virtual field is defined as a parameter");
                  break;
               case ConstInterface.MG_ATTR_VIR_AS_REAL:
                  _virAsReal = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_LNK_CREATE:
                  _linkCreate = XmlParser.getBoolean(valueStr);
                  break;
               case ConstInterface.MG_ATTR_LNKEXP:
                  _linkExp.setVal((Task)getTask(), valueStr);
                  break;
               case ConstInterface.MG_ATTR_TABLE_NAME:
                  _tableName = XmlParser.unescape(valueStr);
                  break;
               case ConstInterface.MG_ATTR_INDEX_IN_TABLE:
                  _indexInTable = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_INIT:
                  int expNum = XmlParser.getInt(valueStr);
                  _initExp = ((Task)getTask()).getExpById(expNum);
                  break;
               case XMLConstants.MG_ATTR_NAME:
                  {
                     /*TODO: delete case*/
                  }
                  break;
               case ConstInterface.MG_ATTR_CHACHED_FLD_ID:
                  //MG_ATTR_CHACHED_FLD is in the format of cachedTableId,fld
                  CacheTableFldIdx = Int32.Parse(data[1]);
                  break;
               case ConstInterface.MG_ATTR_LOCATE:
                  //locate is in the format of max,min expression num (-1 if non exists)
                  Locate = new Boundary((Task)this.getTask(), Int32.Parse(data[1]), Int32.Parse(data[0]), this.getType(), this.getSize(), CacheTableFldIdx);
                  break;
               case ConstInterface.MG_ATTR_RANGE:
                  //range is in the format of max,min expression num (-1 if non exists)
                  Range = new Boundary((Task)this.getTask(), Int32.Parse(data[1]), Int32.Parse(data[0]), this.getType(), this.getSize(), CacheTableFldIdx);
                  break;

               case ConstInterface.MG_ATTR_LINK:
                  _dataviewHeaderId = Int32.Parse(valueStr);
                  break;
               case ConstInterface.MG_ATTR_IS_LINK_FIELD:
                  _isLinkFld = XmlParser.getBoolean(valueStr);
                  break;
               default:
                  isTagProcessed = false;
                  Logger.Instance.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
                  break;
            }
         }

         return isTagProcessed;
      }

      /// <summary>
      ///   set a reference to the Recompute Object of this field
      /// </summary>
      /// <param name = "recompRef">reference to Recompute Object</param>
      internal void setRecompute(Recompute recompRef)
      {
         _recompute = recompRef;
      }

      /// <summary>
      /// Removes the specific subform task from the recompute
      /// </summary>
      /// <param name="subformTask"></param>
      internal void RemoveSubformFromRecompute(Task subformTask)
      {
         if (_recompute != null)
            _recompute.RemoveSubform(subformTask);
      }

      /// <summary>
      /// Insert the specific subform task into the field recompute
      /// </summary>
      /// <param name="subformTask"></param>
      internal void AddSubformRecompute(Task subformTask)
      {
         if (_recompute == null)
         {
            _recompute = new Recompute();
            _recompute.Task = (Task)getTask();
            _recompute.OwnerFld = this;
            _recompute.RcmpMode = Recompute.RcmpBy.CLIENT;
         }
         _recompute.AddSubform(subformTask);
      }

      /// <summary>
      ///   set a reference to a control attached to this field
      /// </summary>
      /// <param name = "ctrl">the control which is attached to this field</param>
      public override void SetControl(MgControlBase ctrl)
      {
         base.SetControl(ctrl);

         if (_isVirtual && ctrl.IsRepeatable && _initExp == null)
            _causeTableInvalidation = true;
      }

      /// <summary>
      ///   compute the field init expression or get its value from the dataview, set
      ///   the value of the field (and update the display)
      /// </summary>
      /// <param name = "recompute">tells the compute to start the recompute chain and recompute
      ///   the expression of this field (if it is real)
      /// </param>
      internal void compute(bool recompute)
      {
         String result = getValue(false);
         bool isNullFld = _isNull;
         var rec = ((DataView)_dataview).getCurrRec();
         var task = (Task)getTask();

         bool zeroReal = _linkCreate && rec.InCompute && !recompute && task.getMode() == Constants.TASK_MODE_CREATE;
         //QCR #999024 - zero lnk create values during compute

         if (_form == null)
            _form = (MgForm)task.getForm();

         // no need to zero reals if we are recovering a record.
         if ((_form != null && _form.InRestore) || ClientManager.Instance.EventsManager.GetStopExecutionFlag())
            zeroReal = false;

         RunTimeEvent rtEvt = ClientManager.Instance.EventsManager.getLastRtEvent();
         bool computeOnCancelEvent = false;
         if (rtEvt != null)
            computeOnCancelEvent = rtEvt.getInternalCode() == InternalInterface.MG_ACT_CANCEL;

         // virtual as real is especially for fields which are used as a "return value"
         // of a link. Their value is returned by the link result and not by the init expression.
         // when the form is restoring the curr row we shouldn't compute the init expression
         // but to use the value from the record (by getValue)
         // QCR#926758: When Task's mode is Create, init for real field should be evaluated only when it comes for cancel event(last runtime event is cancel event). 
         //While creating a new record, init will be evaluated because of condition rec.isNewRec(). (It is the refix of QCR#994072)
         if (!_virAsReal && (_form == null || !_form.InRestore) &&
             ((_isVirtual || (task.getMode() == Constants.TASK_MODE_CREATE && computeOnCancelEvent)) ||  recompute ||
              zeroReal || rec.isNewRec() && !rec.isComputed()))
         {
            if (!task.DataView.isEmptyDataview() || (task.DataView.isEmptyDataview() && !PartOfDataview))
            {
               // QCR #775508: for dataviews, which are computed by the server, prevent
               // computing the init expression while in compute of the record because the
               // server have already done it
               if ((_isVirtual || task.getMode() == Constants.TASK_MODE_CREATE) || ((DataView)_dataview).computeByClient() ||
                   !rec.InCompute || zeroReal || rec.lateCompute())
               {
                  if (_initExp != null)
                  {
                      EvaluateInitExpression(ref result, ref isNullFld);
                  }
                  else if (zeroReal)
                     result = getDefaultValue();
               }
            }
         }

         if (_invalidValue)
         {
            _prevValue = result;
            _prevIsNull = isNullFld;
         }
         setValue(result, isNullFld, recompute, false, recompute && !_isVirtual, false, false);
      }

      /// <summary>
      /// evaluate init expression
      /// </summary>
      /// <param name="result"></param>
      /// <param name="isNullFld"></param>
      internal void EvaluateInitExpression(ref String result, ref bool isNullFld)
      {
          Debug.Assert(_initExp != null);
          try
          {
              _inEvalProcess = true;
              result = _initExp.evaluate(_type, _size);
          }
          finally
          {
              _inEvalProcess = false;
          }

          isNullFld = (result == null);
          if (isNullFld && NullAllowed)
          {
              result = getValue(false);
          }
      }


      ///// <summary>
      ///// 
      ///// </summary>
      ///// <param name="recompute"></param>
      ///// <returns></returns>
      //bool ShouldRecompute(bool recompute)
      //{
      //   if (recompute)
      //   {
      //      bool computedByFetch = !IsVirtual || VirAsReal;
      //      if (computedByFetch)
      //      {
      //         Task task = (Task)getTask();
      //         DataviewHeaderBase header = task.getDataviewHeaders().getDataviewHeaderById(_dataviewHeaderId);
      //         //local datavase already computed the record as server does
      //         if (header is LocalDataviewHeader)
      //         {
      //             var rec = ((DataView)_dataview).getCurrRec();
      //             return !rec.InCompute;
      //         }
      //      }
      //   }
      //   return recompute;
      //}


      /// <summary>
      ///   get the value of the field from the current record
      /// </summary>
      /// <param name = "checkNullArithmetic">if true then the null arithmetic environment
      ///   setting is used to return null when "nullify" computation is needed
      /// </param>
      internal String getValue(bool checkNullArithmetic)
      {
         // if we need to return the original value of the field
         if (((Task)getTask()).getEvalOldValues())
            return getOriginalValue();
         if (_invalidValue)
            takeValFromRec();
         if (_isNull)
            _val = getValueForNull(checkNullArithmetic);
         return _val;
      }

      /// <summary>
      /// </summary>
      /// <param name = "newValue"></param>
      /// <param name = "isNull"></param>
      /// <returns></returns>
      private bool isChanged(String newValue, bool isNull)
      {
         bool isChanged = (_prevValue != null && (!_prevValue.Equals(newValue) || (_prevIsNull != isNull)));
         if (isChanged)
            return true;
         return false;
      }

      /// <summary>
      ///   set the value of the field by taking it from the current record
      /// </summary>
      protected internal void takeValFromRec()
      {
         Record rec;

         rec = (Record)(((DataView)_dataview).getCurrRec());
         if (rec != null)
         {
            // skip if type is dotnet field
            if (_type != StorageAttribute.DOTNET)
            {
               _val = rec.GetFieldValue(_id);
               _isNull = rec.IsNull(_id);
            }
            else
            {
               // no need to check isParam() or isVirtual, for parameters as we do not have real dotnet field.
               // If Server has sent a valid blob prefix, set field to this value.
               String valFrmServer = rec.GetFieldValue(_id);
               bool srcIsDnObj = false;
               bool srcIsMagicVal = false;
               int keyInRec = 0;

               if (BlobType.isValidDotNetBlob(valFrmServer))
               {
                  keyInRec = BlobType.getKey(valFrmServer);

                  // In case of parameters only the keys (client & server ) are different and update will be done.
                  // But for main program, keyInRec and current key represents two different objects (server & client resp.)
                  // for single .net field. Since keys are isolated no update will be performed.
                  if (keyInRec != 0 && !((Task)getTask()).isMainProg())
                  {
                     if (keyInRec != BlobType.getKey(_val))
                     {
                        srcIsDnObj = true;

                        // copy the object at blobPrefix key from server onto this field key.
                        UpdateDNObjectFrom(keyInRec, true);

                        // save this key. This is a parameter, so we must copy the object back into parent field
                        _argFldKey = keyInRec;
                     }
                  }
               }

               // if src is not DnObj, check if it is a magic value.
               if (!srcIsDnObj)
               {
                  String magicVal = BlobType.getString(valFrmServer);

                  // blob from server has some magic data in suffix
                  if (!string.IsNullOrEmpty(magicVal))
                  {
                     srcIsMagicVal = true;

                     Object objVal = null;

                     // convert the magic data in dotnet blob into 'DotNetType' object
                     if (DNType != null)
                        objVal = DNConvert.convertMagicToDotNet(magicVal,
                                                                DNConvert.getDefaultMagicTypeForDotNetType(DNType),
                                                                DNType);

                     // copy the object 'objVal' onto this field key.
                     UpdateDNObjectFrom(objVal, true);
                  }
               }

               // if src is neither DnObj nor Magic val and key==0, init with default.
               if (keyInRec == 0 && !srcIsMagicVal)
               {
                  // if DotNetType is a structure and not inited from record, default construct it.
                  if (DNType != null && !DNType.IsClass && !DNType.IsAbstract)
                  {
                     Object obj = ReflectionServices.CreateInstance(DNType, null, null);
                     DNManager.getInstance().DNObjectsCollection.Update(BlobType.getKey(_val), obj);
                  }
               }
            }
         }

         if (_invalidValue)
         {
            if (rec != null && _causeTableInvalidation && isChanged(_val, _isNull) && _form != null)
               rec.setCauseInvalidation(true);


            _prevValue = _val;
            _prevIsNull = _isNull;
            _invalidValue = false;
         }
         // ZERO THE VECTOR OBJECT
         _vectorType = null;
      }

      /// <summary>
      ///   get the original value of the field without updating the field value
      /// </summary>
      internal String getOriginalValue()
      {
         Record originalRec = ((DataView)_dataview).getOriginalRec();

         if (originalRec.IsNull(_id))
            return getMagicDefaultValue();

         return originalRec.GetFieldValue(_id);
      }

      /// <summary>
      ///   is original value of the field was null
      /// </summary>
      internal bool isOriginalValueNull()
      {
         Record rec = ((DataView)_dataview).getOriginalRec();
         return rec.IsNull(_id);
      }

      /// <summary>
      ///   get value by rec idx
      /// </summary>
      /// <param name = "idx">record idx</param>
      /// <returns></returns>
      internal String getValueByRecIdx(int idx)
      {
         var rec = (Record)(((DataView)_dataview).getRecByIdx(idx));
         String val = rec.GetFieldValue(_id);
         bool isNullFld = rec.IsNull(_id);
         return (isNullFld
                    ? getValueForNull(false)
                    : val);
      }

      /// <summary>
      ///   get modifiedAtLeastOnce flag 
      /// </summary>
      /// <returns></returns>
      internal bool getModifiedAtLeastOnce()
      {
         return modifiedAtLeastOnce;
      }

      /// <summary>
      ///   return null if value of field for the record is null
      /// </summary>
      /// <param name = "idx">record idx</param>
      /// <returns></returns>
      internal bool isNullByRecIdx(int idx)
      {
         var rec = (Record)(((DataView)_dataview).getRecByIdx(idx));
         bool isNullFld = rec.IsNull(_id);
         return isNullFld;
      }

      /// <summary>
      ///   returns true if field's value for record idx recidx equals to mgValue
      /// </summary>
      /// <param name="mgValue">value to compare  </param>
      /// <param name="isNullFld"></param>
      /// <param name="type">type of value</param>
      /// <param name="recIdx">recIdx of the record</param>
      /// <returns></returns>
      internal bool isEqual(String mgValue, bool isNullFld, StorageAttribute type, int recIdx)
      {
         var rec = (Record)(((DataView)_dataview).getRecByIdx(recIdx));
         bool valsEqual = false;
         if (rec != null)
         {
            String fieldValue = rec.GetFieldValue(_id);
            bool fielsIsNull = rec.IsNull(_id);
            valsEqual = ExpressionEvaluator.mgValsEqual(fieldValue, fielsIsNull, _type, mgValue, isNullFld, type);
         }
         return valsEqual;
      }

      /// <summary>
      ///   returns the value of this field which should be inserted into a new rec, when it
      ///   is created. This will usually be the field's default value, unless there is a
      ///   virtual field without an init expression. In this case, the current value will be
      ///   returned.
      /// </summary>
      /// <param name="clobberedOnly">clobberedOnly is true if we want to only take the values of the virtuals with no init expression</param>
      protected internal String getNewRecValue(bool clobberedOnly)
      {
         if (_isVirtual && _initExp == null)
            return _val;
         return !clobberedOnly
                   ? getDefaultValue()
                   : null;
      }


       /// <summary>
      ///   call the set value function from the "outer" world. This will trigger the recompute
      ///   chain. When recompute is done - refresh controls which are expression dependent.
      /// </summary>
      /// <param name="val">the new value of the field</param>
      /// <param name="isNullFld">if true then the value is null</param>
      /// <param name="recompute">states whether to execute recompute when setting the value</param>
      /// <param name="recomputeOnlyWhenUpdateValue">: do the recompute only when the value was update</param>
      /// <param name="setRecordUpdated">tells whether to define the record as updated</param>
      /// <param name="isArgUpdate">true if called for handler argument-parameter copy</param>
      /// <param name="enforceVariableChange">execute vaiable change even when recompute is false</param>
      internal void setValueAndStartRecompute(String val, bool isNullFld, bool recompute,
                                              bool setRecordUpdated, bool isArgUpdate)
      {
         setValueAndStartRecompute(val, isNullFld, recompute, setRecordUpdated, isArgUpdate, false);
      }

      /// <summary>
      ///   call the set value function from the "outer" world. This will trigger the recompute
      ///   chain. When recompute is done - refresh controls which are expression dependent.
      /// </summary>
      /// <param name="val">the new value of the field</param>
      /// <param name="isNullFld">if true then the value is null</param>
      /// <param name="recompute">states whether to execute recompute when setting the value</param>
      /// <param name="recomputeOnlyWhenUpdateValue">: do the recompute only when the value was update</param>
      /// <param name="setRecordUpdated">tells whether to define the record as updated</param>
      /// <param name="isArgUpdate">true if called for handler argument-parameter copy</param>
      /// <param name="enforceVariableChange">execute vaiable change even when recompute is false</param>
      internal void setValueAndStartRecompute(String val, bool isNullFld, bool recompute,
                                              bool setRecordUpdated, bool isArgUpdate, bool enforceVariableChange)
      {
         var task = (Task)getTask();
         if (task != null)
            // to avoid recursions
            task.VewFirst++;

         bool valueWasUpdated = setValue(val, isNullFld, recompute, setRecordUpdated, !_isVirtual, isArgUpdate, enforceVariableChange);

         // A field that is an event handler field cannot appear as a property in gui. no need to refresh all
         // form+controls properties expressions for it.
         if (task != null && task.VewFirst == 1 && _form != null && !IsEventHandlerField)
         {
            if (recompute && valueWasUpdated)
               _form.refreshOnExpressions();

            _form.RecomputeTabbingOrder(true);
         }

         if (task != null)
            task.VewFirst--;
      }

      /// <summary>
      ///   set the value of the field in the current record and update the display
      ///   the field must be in a magic internal format
      /// </summary>
      /// <param name="newVal">the new value of the field</param>
      /// <param name="isNullFld">if true then the value is null</param>
      /// <param name="recompute">states whether to execute recompute when setting the value</param>
      /// <param name="setRecordModified">states whether to define the record as modified</param>
      /// <param name="setCrsrModified">states whether to define the cursor of the field as modified</param>
      /// <param name="isArgUpdate">true if called for handler argument-parameter copy</param>
      /// <returns>the return value was updated </returns>
      private bool setValue(String newVal, bool isNullFld, bool recompute, bool setRecordModified, bool setCrsrModified, bool isArgUpdate, bool enforceValiableChange)
      {
         Record rec = null;
         String recFldVal;
         bool valsEqual = false;
         int remainder;
         bool zeroReal;
         ArgumentsList args = null;
         var mainCtrl = getCtrl();
         int pendingEvents;
         ArrayList evts;
         bool forceUpdate = false;
         bool checkForVariableChange = recompute || enforceValiableChange;

         var task = (Task)getTask();
         if (_form == null)
            _form = (MgForm)task.getForm();

         if (isNullFld && !NullAllowed && _type != StorageAttribute.BLOB_VECTOR)
         {
            newVal = getDefaultValue();
            isNullFld = false;
         }

         //QCR#805364: For any field, we store the value in field as it is (without checking the picture).
         //But for Numeric field, if picture does not contain decimals then we should round off the value and then save this value in field
         //otherwise it will give wrong calculation. So always round off the value depending on picture's decimal.
         // check if newVal is "long" since there is no reason to round an already whole number
         if (_type == StorageAttribute.NUMERIC && newVal != null && !NUM_TYPE.numHexStrIsLong(newVal))
         {
            var numVal = new NUM_TYPE(newVal); 
            var pic = new PIC(_picture, _type, getTask().getCompIdx());
            int decs = pic.getDec();
            numVal.round(decs);
            newVal = numVal.toXMLrecord();
         }

         rec = ((DataView)_dataview).getCurrRec();
         if (rec == null)
            throw new ApplicationException(ClientManager.Instance.getMessageString(MsgInterface.RT_STR_NO_RECS_IN_RNG));

         // don't allow updating REAL fields, unless 'allow update in query mode' is set to TRUE
         if (!IsVirtual &&
             (task.getMode() == Constants.TASK_MODE_QUERY &&
              !ClientManager.Instance.getEnvironment().allowUpdateInQueryMode(task.getCompIdx()) ||
              rec.InForceUpdate && rec.InCompute))
            return !valsEqual;

         if (checkForVariableChange && getHasChangeEvent())
         {
            var argsList = new GuiExpressionEvaluator.ExpVal[2];
            argsList[0] = new GuiExpressionEvaluator.ExpVal(StorageAttribute.NUMERIC, false,
                                                            (mainCtrl != null && mainCtrl.IsInteractiveUpdate
                                                                ? NUM0.toXMLrecord()
                                                                : NUM1.toXMLrecord()));
            if (getType() == StorageAttribute.DOTNET)
            {
               // We must copy the object pointed by field into VarChangeDNObjectCollectionKey entry. Once field is updated by newVal,
               // the prev Object would be lost, and we cannot have prevObj in Var change.
               int key = ClientManager.Instance.getVarChangeDNObjectCollectionKey();
               Object objToCopy =
                  DNManager.getInstance().DNObjectsCollection.GetDNObj(BlobType.getKey(getValue(false)));
               DNManager.getInstance().DNObjectsCollection.Update(key, objToCopy);

               // Now, we prepare a DotNet blob with this key. This blob prefix is used to copy object into parameter.
               argsList[1] = new GuiExpressionEvaluator.ExpVal(getType(), isNull(), BlobType.createDotNetBlobPrefix(key));
            }
            else
               argsList[1] = new GuiExpressionEvaluator.ExpVal(getType(), isNull(), getValue(false));
            args = new ArgumentsList(argsList);
         }

         if (mainCtrl != null)
            mainCtrl.IsInteractiveUpdate = false;

         // update the field in the current record
         if (_type != StorageAttribute.UNICODE && UtilStrByteMode.isLocaleDefLangDBCS())
         {
            // count the number of bytes, not characters (JPN: DBCS support)
            if (newVal != null)
               remainder = _size - UtilStrByteMode.lenB(newVal);
            else
               remainder = 0;

            if (newVal != null && _type != StorageAttribute.BLOB && _type != StorageAttribute.BLOB_VECTOR &&
                remainder < 0
                && _type != StorageAttribute.DOTNET)
               _val = UtilStrByteMode.leftB(newVal, _size);
            else if (_type == StorageAttribute.ALPHA && remainder > 0)
               _val = newVal + new String(_spaces, 0, remainder);
            else if (_type == StorageAttribute.BLOB)
               _val = BlobType.copyBlob(_val, newVal);
            //else if (type == com.magicsoftware.richclient.util.MgDataType.BLOB_VECTOR && choiceControl)
            //    setCellVecValue(1, newVal, false);
            else if (_type == StorageAttribute.DOTNET)
            {
               int keyFrom = BlobType.getKey(newVal);

               valsEqual = UpdateDNObjectFrom(keyFrom, isArgUpdate);
            }
            else
               _val = newVal;
         }
         else
         {
            if (newVal != null)
               remainder = _size - newVal.Length;
            else
               remainder = 0;

            if (newVal != null && _type != StorageAttribute.BLOB && _type != StorageAttribute.BLOB_VECTOR &&
                remainder < 0
                && _type != StorageAttribute.DOTNET)
               _val = newVal.Substring(0, _size);
            else if ((_type == StorageAttribute.ALPHA || _type == StorageAttribute.UNICODE) && remainder > 0)
               _val = newVal + new String(_spaces, 0, remainder);
            else if (_type == StorageAttribute.BLOB)
            {
               // Ensure _val is not null, so that the blob copy will be 
               // done correctly, according to the field's blob type.
               if (_val == null)
                  _val = this.getMagicDefaultValue();
               _val = BlobType.copyBlob(_val, newVal);
            }
            //else if (type == com.magicsoftware.richclient.util.MgDataType.BLOB_VECTOR && choiceControl)
            //    setCellVecValue(1, newVal, false);
            else if (_type == StorageAttribute.DOTNET)
            {
               int keyFrom = BlobType.getKey(newVal);
               valsEqual = UpdateDNObjectFrom(keyFrom, isArgUpdate);
            }
            else
               _val = newVal;
         }

         // If dotnet var is updated with new blobprefix, valsEqual is false, we should not check in record.
         if (_type != StorageAttribute.DOTNET)
         {
            recFldVal = rec.GetFieldValue(_id);

            //In order to compare, If virtual's old value is taken from record, then old isNull value also should be taken from record. 
            //This should be done only if not in re-compute / not argument / no initExp.) 
            //Mainly , this is needed, when cursor is parked on one record and virtual is modified from null to nonnull or viceversa and
            //now if cursor moves from one record to next record, We need to  get the old null value from record.
            bool oldNullVal = _isNull;
            if (!recompute && !isArgUpdate && _initExp == null)
               oldNullVal = rec.IsNull(_id);

            
            try
            {
               valsEqual = ExpressionEvaluator.mgValsEqual(_val, isNullFld, _type, recFldVal, oldNullVal, _type);
            }
            catch (FormatException)
            {
               _val = getMagicDefaultValue();
               valsEqual = false;
            }
            catch (ArgumentOutOfRangeException)
            {
               _val = getMagicDefaultValue();
               valsEqual = false;
            }
         }

         //zero the vector
         if (_clearVectorType)
            _vectorType = null;

         if (_invalidValue)
         {
            _invalidValue = false;
            rec.clearFlag(_id, Record.FLAG_UPDATED);
            rec.clearFlag(_id, Record.FLAG_MODIFIED);
         }

         // during compute, we should zero fields which belong to link create. 
         zeroReal = _linkCreate && rec.InCompute && !recompute && (_form == null || !_form.InRestore) &&
                    !ClientManager.Instance.EventsManager.GetStopExecutionFlag();

         // Virtuals without init expression are copied from one record to another, even at
         // compute time (no recompute). Nevertheless, in case their value changes, we must
         // perform recompute.
         if ((_isVirtual || zeroReal) && !rec.isNewRec() && !valsEqual)
            forceUpdate = true;

         // if the recompute is false it means that the user is setting a value
         // that its source is the record, thus, we don't have to update the record.
         // if the recompute is true, go ahead and update the record.
         if (checkForVariableChange || forceUpdate || rec.isNewRec())
         {
            UpdateNull(isNullFld, rec);

            if (setCrsrModified)
               rec.setFlag(_id, Record.FLAG_CRSR_MODIFIED);
            rec.setFieldValue(_id, _val, setRecordModified);
            evts = ClientManager.Instance.getPendingVarChangeEvents();
            pendingEvents = evts.Count;
            if (checkForVariableChange && getHasChangeEvent() && !evts.Contains(this) && !valsEqual)
            {
               //if there is a variable change handler - save value and prepare params
               var rtEvt = new RunTimeEvent(this);
               rtEvt.setInternal(InternalInterface.MG_ACT_VARIABLE);
               rtEvt.setArgList(args);
               evts.Add(this);
               evts.Add(rtEvt);
            }

            // check for recompute and execute it
            //QCR #299609, if we are in local dataview manager, we can only compute.
            //Recompute is not allowed, since it will be executed multiple times 
            if (_recompute != null && (recompute || forceUpdate) && !((DataView)_dataview).InLocalDataviewCommand)
            {
               try
               {
                  //if field caused table invalidation and it's value was changed
                  if (forceUpdate)
                     rec.InForceUpdate = true;
                  _recompute.execute(rec);
               }
               finally
               {
                  if (forceUpdate)
                     rec.InForceUpdate = false;
               }
            }

            if (_causeTableInvalidation && isChanged(_val, isNullFld))
               rec.setCauseInvalidation(true);

            // Don't update controls while fetching records from local database. If the task has a table control, then actual controls on the 
            // rows are created after no. of rows is known (and this is after the data is avaialable). So, while fetching records, controls in 
            // table are not yet created and hence there is an assert while trying to update the controls. After skipping this update, the controls 
            // are updated from other places (refreshTable, commonHandlerBefore, handleRowDataCurPage etc like it happens for server data)
            if ((recompute || forceUpdate) && !((DataView)_dataview).InLocalDataviewCommand && mainCtrl != null)
            {
               // Unlike other controls, .NET control's display is not updated from MgControl.validateAndSetValue() because
               // the field itself is updated after receiving MG_ACT_UPDATE_DN_CONTROL_VALUE. 
               if (mainCtrl.IsDotNetControl())
                  updateDisplay();
               else if (mainCtrl.isChoiceControl())
                  updateDisplay();
            }

            //Right after we exit the topmost update - execute all variable change events.
            //note that during this execution, new ones may be created.
            if (pendingEvents == 0 && checkForVariableChange && getHasChangeEvent())
            {
               while (evts.Count > 0)
               {
                  var evt = (RunTimeEvent)evts[1];
                  evts.RemoveAt(0);
                  evts.RemoveAt(0);
                  if (ClientManager.Instance.getLastFocusedTask() == null)
                     evt.setTask((Task)getTask());
                  else
                  {
                     Task taskRef = ClientManager.Instance.getLastFocusedTask();
                     // If we are in variable change event and TaskSuffix is executed, it means we are here from 
                     // update arguments while closing the task. In such case, we should set the event task as PathParentTask
                     // and not the task itself.
                     if (taskRef.TaskSuffixExecuted)
                     {
                        taskRef = (taskRef.PathParentTask != null) ? (Task)taskRef.PathParentTask : taskRef;
                     }
                     evt.setTask(taskRef);
                  }
                  evt.setCtrl(GUIManager.getLastFocusedControl());
                  ClientManager.Instance.EventsManager.handleEvent(evt, false);
               }
            }
         }

         // #776031 - the field should be marked modified only if val are not equal. Otherwise, even for null field paramter passing 
         // or same value update, it is marked as modified and RS is executed.
         if (recompute && !valsEqual)
            setModified();

         return !valsEqual;
      }

      internal void UpdateNull(bool isNullFld, Record rec)
      {
         if (isNullFld)
            rec.setFlag(_id, Record.FLAG_NULL);
         else
            rec.clearFlag(_id, Record.FLAG_NULL);
         _isNull = isNullFld;
      }

      /// <summary>
      ///   set a vec cell 
      ///   can not be executed without the logic from eval_op_vecGet
      /// </summary>
      /// <param name = "idx">the cell index in the vector strats from 1</param>
      /// <param name = "newVal">the new value</param>
      /// <param name = "valIsNull">is the bew value null</param>
      internal bool setCellVecValue(int idx, String newVal, bool valIsNull)
      {
         bool res = false;
         if (_type == StorageAttribute.BLOB_VECTOR)
         {
            // initialized the vector object if needed
            if (_vectorType == null)
            {
               _vectorType = isNull()
                         ? new VectorType(_vecCellsType, _vecCellsContentType, DefaultValue, isNullDefault(),
                                          NullAllowed, _vecCellsSize)
                         : new VectorType(_val);
            }

            // if vecCellsType is BLOB, convert data into blob
            if (_vecCellsType == StorageAttribute.BLOB)
            {
               String tmpNewVal = getCellDefualtValue();
               newVal = BlobType.copyBlob(tmpNewVal, newVal);
            }

            res = _vectorType.setVecCell(idx, newVal, valIsNull);

            //start recompute
            if (res)
            {
               _clearVectorType = false;
               //QCR 984563 do not set the record as changed 
               setValueAndStartRecompute(_vectorType.ToString(), false, true, false, false);
               _clearVectorType = true;
            }
         }

         return res;
      }

      /// <summary>
      ///   returns the cell value of a given cell in the vector
      /// </summary>
      /// <returns> the cell's value as string</returns>
      internal String getVecCellValue(int idx)
      {
         if (_type == StorageAttribute.BLOB_VECTOR)
         {
            // if we are here than the filed must have none null value of a vector
            if (_vectorType == null)
               _vectorType = new VectorType(_val);

            return _vectorType.getVecCell(idx);
         }

         return null;
      }

      /// <summary>
      ///   return true if this field contains reference to the object in the 
      ///   key entrance of the object table
      /// </summary>
      /// <param name = "o"></param>
      /// <returns></returns>
      internal bool hasDotNetObject(Object o)
      {
         if (getType() == StorageAttribute.DOTNET)
         {
            String blobStr = getValue(false);
            if (!string.IsNullOrEmpty(blobStr))
            {
               int key = BlobType.getKey(blobStr);
               if (key != 0)
               {
                  Object fieldObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);
                  return fieldObj == o;
               }
            }
         }
         return false;
      }

      /// <summary>
      /// Updates the DotNet object in field with object pointed by 'keyFrom'
      /// </summary>
      /// <param name="keyFrom"></param>
      /// <param name="isArgUpdate">true if called for handler argument-parameter copy</param>
      /// <returns></returns>
      private bool UpdateDNObjectFrom(int keyFrom, bool isArgUpdate)
      {
         bool valsEqual = true;

         // field is a dotnet type
         if (_type == StorageAttribute.DOTNET)
         {
            int fieldDNObjectCollectionKey = BlobType.getKey(_val);

            if (keyFrom != fieldDNObjectCollectionKey)
            {
               Object newObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(keyFrom);

               // update with 'newObj'
               valsEqual = UpdateDNObjectFrom(newObj, isArgUpdate);
            }
         }

         return valsEqual;
      }

      /// <summary>
      /// Updates the DotNet object in field with object 'newObj'
      /// </summary>
      /// <param name="newObj"></param>
      /// <param name="isArgUpdate">true if called for handler argument-parameter copy</param>
      /// <returns></returns>
      private bool UpdateDNObjectFrom(Object newObj, bool isArgUpdate)
      {
         bool valsEqual = true;
         int fieldDNObjectCollectionKey = BlobType.getKey(_val);
         Object obj = DNManager.getInstance().DNObjectsCollection.GetDNObj(fieldDNObjectCollectionKey);

         // perform a cast into dotnet var type
         try
         {
            newObj = DNConvert.doCast(newObj, DNType);
         }
         catch (Exception)
         {
            // suppress exception, continue with 'newObj' as null
            newObj = null;
         }

         if (newObj != null)
            valsEqual = obj != null && newObj.Equals(obj);
         else
            valsEqual = (obj == null);

         if (!valsEqual)
         {
            // if it is a structure, shallow copy it. For structure passed in handler var, or in call operation,
            // we donot want the changes to be updated immediate. 
            if (isArgUpdate && newObj != null && !newObj.GetType().IsClass)
               newObj = DNConvert.doShallowCopy(newObj);

            // clear the events to 'obj'
            removeDNEventHandlers();

            DNManager.getInstance().DNObjectsCollection.Update(fieldDNObjectCollectionKey, newObj);

            addDNEventHandlers(newObj);
         }

         return valsEqual;
      }

      /// <summary>
      ///   add handlers to the .net objects events
      /// </summary>
      /// <param name = "obj"></param>
      private void addDNEventHandlers(Object obj)
      {
         if (obj != null && ControlToFocus == null) //this field has a control and its events will be handled with controls
         {
            //get list of events to listen to
            List<String> dnEventsNames = ((Task)getTask()).GetDNEvents(this, DNType, false);
            String[] eventsArray = null;

            if (dnEventsNames.Count > 0)
            {
               eventsArray = new String[dnEventsNames.Count];
               dnEventsNames.CopyTo(eventsArray);
            }

            DNManager.getInstance().DNObjectEventsCollection.addEvents(obj, eventsArray);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal bool refersDNControl()
      {
         MgFormBase form = ((Task)_dataview.getTask()).getForm();
         int noOfControls = form.CtrlTab.getSize();

         for (int idx = 0; idx < noOfControls; idx++)
         {
            MgControlBase mgControl = form.CtrlTab.getCtrl(idx);
            if (mgControl != null && mgControl.DNObjectReferenceField == this)
               return true;
         }

         return false;
      }

      /// <summary>
      ///   garbage collection for dotnet event handlers
      /// </summary>
      internal void removeDNEventHandlers()
      {
         if (getType() == StorageAttribute.DOTNET)
         {
            Object obj = DNManager.getInstance().DNObjectsCollection.GetDNObj(BlobType.getKey(_val));
            DNManager.getInstance().DNObjectEventsCollection.removeEvents(obj);
         }
      }

      /// <summary>
      ///   set the recomputed variable to true
      /// </summary>
      internal void setRecomputed()
      {
         //recomputed = true;
      }

      /// <summary>
      ///   invalidate the value of the field. An invalid value forces us to re-calculate the field's
      ///   value by copying it from the dataview.
      ///   used when switching to another record
      /// </summary>
      /// <param name = "forceInvalidate">- Dont check the field's type and make it invalid (usually Virtuals
      ///   without init expression are never invalidated, since they retain their value when moving
      ///   between records).
      /// </param>
      /// <param name="clearFlags"></param>
      internal void invalidate(bool forceInvalidate, bool clearFlags)
      {
         var rec = ((DataView)_dataview).getCurrRec();

         if (!_isVirtual || _virAsReal || _initExp != null || forceInvalidate)
            _invalidValue = true;
         if (clearFlags && null != rec)
         {
            rec.clearFlag(_id, Record.FLAG_UPDATED);
            rec.clearFlag(_id, Record.FLAG_MODIFIED);
         }
      }

      /// <summary>
      ///   get Name of table
      /// </summary>
      internal String getTableName()
      {
         if (_tableName == null)
            return "";
         return _tableName;
      }

      /// <summary>
      ///   for get VARNAME function use
      ///   A string containing the table name where the variable originates,
      ///   concatenated with '.' and the variable description of the variable in that table.
      ///   If the variable is a virtual one, then the table name would indicate 'Virtual'.
      /// </summary>
      public override String getName()
      {
         if (_isParam)
            return "Parameter." + getVarName();
         else if (_isVirtual)
            return "Virtual." + getVarName();
         else
            return getTableName() + "." + getVarName();
      }

      /// <summary>
      ///   test for a nullity
      /// </summary>
      public bool isNull()
      {
         if (((Task)getTask()).getEvalOldValues())
            return isOriginalValueNull();
         else if (_invalidValue)
            takeValFromRec();
         return _isNull;
      }

      /// <summary>
      /// </summary>
      internal bool PrevIsNull()
      {
         return _prevIsNull;
      }

      /// <summary>
      ///   test for an invalid link
      /// </summary>
      internal bool isLinkInvalid()
      {
         var rec = ((DataView)_dataview).getCurrRec();
         return rec.isLinkInvalid(_id);
      }

      /// <summary>
      ///   test for an modified flag
      /// </summary>
      internal bool isModified()
      {
         var rec = ((DataView)_dataview).getCurrRec();
         return rec.isFldModified(_id);
      }

      /// <summary>
      ///   test for an modified flag
      /// </summary>
      internal bool IsModifiedAtLeastOnce()
      {
         var rec = ((DataView)_dataview).getCurrRec();
         // QCR #298014. After deleting of the last record with AllowEmptyDV=No, the current record is null.
         // So take values from the previous current record that was the last deleted record.
         if (rec == null)
            rec = ((DataView)_dataview).getPrevCurrRec();
         if (rec == null)
            return false;
         else
            return rec.IsFldModifiedAtLeastOnce(_id);
      }

      /// <summary>
      ///   test for an updated flag
      /// </summary>
      protected internal bool isUpdated()
      {
         var rec = ((DataView)_dataview).getCurrRec();
         return rec.isFldUpdated(_id);
      }

      /// <summary>
      ///   set the modified flag
      ///   please note that clearing the flag is done explicitly
      /// </summary>
      protected internal void setModified()
      {
         var rec = ((DataView)_dataview).getCurrRec();
         rec.setFlag(_id, Record.FLAG_MODIFIED);

         // This flag will be set as FLAG_MODIFIED and never cleared. Qcr #926815
         rec.setFlag(_id, Record.FLAG_MODIFIED_ATLEAST_ONCE);
         if (IsVirtual)
            modifiedAtLeastOnce = true;
         // the setting of the record modified flag is done explicitly
      }

      /// <summary>
      ///   set the updated flag
      ///   please note that clearing the flag is done explicitly
      /// </summary>
      internal void setUpdated()
      {
         var rec = ((DataView)_dataview).getCurrRec();
         rec.setFlag(_id, Record.FLAG_UPDATED);
         rec.setUpdated();
      }

      /// <summary>
      ///   returns true if this field is a parameter
      /// </summary>
      internal bool isParam()
      {
         return _isParam;
      }

      /// <summary>
      ///   returns if the field in evaluation process
      /// </summary>
      internal bool isInEvalProcess()
      {
         return _inEvalProcess;
      }

      /// <summary>
      ///   returns true if the field has an init expression
      /// </summary>
      internal bool hasInitExp()
      {
         return (_initExp != null);
      }

      /// <summary>
      ///   returns true if the server recomputes this field
      /// </summary>
      protected internal bool isServerRcmp()
      {
         return (_recompute != null && _recompute.isServerRcmp());
      }

      /// <summary>
      ///   returns the picture of the field as a string
      /// </summary>
      internal String getPicture()
      {
         return _picture;
      }

      /// <summary>
      ///   returns the the id of the link this field belong to
      ///   if does not belong to any link return -1
      /// </summary>
      protected internal int getDataviewHeaderId()
      {
         return _dataviewHeaderId;
      }

      /// <summary>
      ///   returns whether the field belongs to a link or not
      /// </summary>
      internal bool IsLinkField
      {
         get { return _isLinkFld; }
      }

      /// <summary>
      ///   evaluates and returns the init expression value of this field (null if there is non)
      /// </summary>
      internal void getInitExpVal(ref String res, ref bool isNull)
      {
          if (_initExp != null)
              EvaluateInitExpression(ref res, ref isNull);
          else
              res = getDefaultValue();
       }

      /// <summary>
      ///   sets the hasChangeEvent flag to TRUE
      /// </summary>
      internal void setHasChangeEvent()
      {
         _hasChangeEvent = true;
      }

      /// <summary>
      ///   retrieves the hasChangeEvent flag
      /// </summary>
      private bool getHasChangeEvent()
      {
         return _hasChangeEvent;
      }

      /// <summary>
      ///   set hasZoomHandler to true
      /// </summary>
      internal void setHasZoomHandler()
      {
         _hasZoomHandler = true;
      }

      /// <returns> the hasZoomHandler
      /// </returns>
      internal bool getHasZoomHandler()
      {
         return _hasZoomHandler;
      }

      /// <summary>
      ///   returns true if in this field will cause invalidation of all rows in table
      /// </summary>
      /// <returns></returns>
      internal bool isCauseTableInvalidation()
      {
         return _causeTableInvalidation;
      }

      /// <summary>
      ///   set cause Table Invalidation member
      /// </summary>
      /// <param name = "causeTableInvalidation"></param>
      internal void causeTableInvalidation(bool causeTableInvalidation)
      {
         _causeTableInvalidation = causeTableInvalidation;
      }

      /// <summary>
      ///   copy the object back to original field passed as argument
      /// </summary>
      internal void updateDNArgFld()
      {
         if (getType() != StorageAttribute.DOTNET)
            return;

         int key = BlobType.getKey(_val);
         if (key != 0 && _argFldKey != 0)
         {
            var argFld = (Field)DNManager.getInstance().DNObjectFieldCollection.get(_argFldKey);

            // set the value
            if (argFld != null)
               argFld.setValueAndStartRecompute(_val, false, true, true, false);
         }
      }

      /// <summary>
      ///   when value is null, get value that represent null
      /// </summary>
      /// <param name = "checkNullArithmetic"></param>
      /// <returns></returns>
      protected String getValueForNull(bool checkNullArithmetic)
      {
         String val;
         if (NullAllowed)
         {
            if (checkNullArithmetic && getTask().getNullArithmetic() == Constants.NULL_ARITH_NULLIFY)
               val = null;
            else
            {
               if (_nullValue == null)
                  _nullValue = getMagicDefaultValue();
               val = _nullValue;
            }
         }
         else
            val = getMagicDefaultValue();
         return val;
      }

      /// <summary>
      ///   returns the control which is attached to the field and resides on the same task
      ///   of of the field
      /// </summary>
      internal MgControl getCtrl()
      {
         MgControl ctrl;

         if (_controls != null)
         {
            for (int i = 0;
                 i < _controls.getSize();
                 i++)
            {
               ctrl = (MgControl)_controls.getCtrl(i);
               if (ctrl.getForm().getTask() == getTask())
                  return ctrl;
            }
         }
         return null;
      }

      /// <summary>
      ///   update the display
      /// </summary>
      internal void updateDisplay()
      {
         updateDisplay(getDispValue(), isNull(), false);
      }

      /// <summary>
      ///   return the value to display
      /// </summary>
      internal String getDispValue()
      {
         if (isNull())
         {
            if (_nullDisplay != null)
               return getNullDisplay();
            else
               return getMagicDefaultValue();
         }
         else
            return getValue(false);
      }

      /// <summary>
      ///   return key of the .NET field
      /// </summary>
      /// <returns></returns>
      internal int GetDNKey()
      {
         Debug.Assert(_type == StorageAttribute.DOTNET);
         return BlobType.getKey(_val);
      }

      /// <summary>
      /// Updates field value with dot net object value.
      /// </summary>
      /// <param name="dnObjectVal">new value, to be updated on field.</param>
      internal void UpdateWithDNObject(Object dnObjectVal, bool refreshControl)
      {
         if (getType() == StorageAttribute.DOTNET)
         {
            // create temporary entry in DNObjectsCollection and add object      
            // to it.Call set value to set the value to fld (from this temp entry) and do recompute.
            int key = DNManager.getInstance().DNObjectsCollection.CreateEntry(null);

            // add obj into this entry
            DNManager.getInstance().DNObjectsCollection.Update(key, dnObjectVal);

            // set the value
            setValueAndStartRecompute(BlobType.createDotNetBlobPrefix(key), false, true, true, false);

            // delete the temp. entry
            DNManager.getInstance().DNObjectsCollection.Remove(key);
         }
         else
         {
            String strVal = DNConvert.convertDotNetToMagic(dnObjectVal, getType());

            // set the value
            setValueAndStartRecompute(strVal, false, true, true, false);
         }

         if (refreshControl)
            updateDisplay();
      }

      /// <summary>
      /// true, if range condition should be applied to the ield during compute
      /// </summary>
      public bool ShouldCheckRangeInCompute
      {
         get
         {
            return IsLinkField || (IsVirtual && ((DataView)_dataview).HasMainTable);
         }
      }

      internal bool IsForArgument(bool taskHasParameters)
      {
         // If we need a parameter and the field is not one
         if (taskHasParameters)
         {
            if (!isParam())
               return false;
         }
         // if there are no parameters, we need a virtual
         else
         {
            if (!IsVirtual)
               return false;
         }

         return true;
      }

   }
}
