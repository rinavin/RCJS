using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.remote;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   this class represents the dataview of a task
   /// </summary>
   internal class DataView : DataViewBase
   {
      // CONSTANTS
      private const string INVOKED_FROM_OFFLINE_TASK = "-99999";

      private const char CHUNK_CACHE_NEXT = 'N';
      private const char CHUNK_CACHE_PREV = 'P';
      private const char CHUNK_DV_BOTTOM = 'B';
      private const char CHUNK_DV_TOP = 'T';
      private const char COMPUTE_FLUSH_UPDATES = 'H';

      private const char COMPUTE_NEWREC_ON_CLIENT = 'C';
      private const char COMPUTE_NEWREC_ON_SERVER = 'S';
      private const String END_DV_TAG = "</" + ConstInterface.MG_TAG_DATAVIEW + ">";
      private const char RECOVERY_ACT_BEGIN_SCREEN = 'S';

      // recovery action taken by the client in response to recovery type
      private const char RECOVERY_ACT_BEGIN_TABLE = 'T';
      private const char RECOVERY_ACT_CANCEL = 'C';
      private const char RECOVERY_ACT_MOVE_DIRECTION_BEGIN = 'B';
      private const char RECOVERY_ACT_NONE = 'N';
      private const char TRANS_STAT_CLOSED = 'C';
      private const char TRANS_STAT_OPENED = 'O';

      private const char UNKNOWN_RCMPS_FOUND = 'Y';
      private const char UNKNOWN_RCMPS_NOT_INITED = 'M';

      internal const int SET_DISPLAYLINE_BY_DV = Int32.MinValue;

      private long _cacheLruTimeStamp;
      private bool _changed;
      private int _chunkSize = 30;
      private int _chunkSizeExpression;
      private char _computeBy; // tells the client to execute the compute of new records on the client or on the server
      private int _currRecId;
      private int _currRecIdx;
      private long _dvPosValue;
      private bool _firstDv = true;
      private bool _flushUpdates; // TRUE means that whenever a record is changed we must send it to the server
      private bool _hasMainTable = true;
      private char _insertAt = ' ';
      /// <summary>
      /// insert at
      /// </summary>
      internal char InsertAt
      {
         get { return _insertAt; }
         set { _insertAt = value; }
      }

      /// <summary>
      /// true, if we are executing local dataview command
      /// </summary>
      internal bool InLocalDataviewCommand { get; set; }
      private int _lastCreatedRecId; // the record id of the last created record id
      private long _lastSessionCounter = Int64.MinValue;
      private int _locateFirstRec = -1;
      private RecordsTable _modifiedRecordsTab; // after sending these records to the server the list is cleared
      internal IRecordsTable ModifiedRecordsTab { get { return _modifiedRecordsTab; } }

      private int _oldTopRecIdx = Int32.MinValue;
      private Record _original;
      private char _pendingRecovery = RECOVERY_ACT_NONE; // when different than NONE, means the server order for recovery was not performed (yet).
      private Record _prevCurrRec;
      private RecordsTable _recordsTab; // <dataview> ...</dataview> tag
      private char _recovery = ConstInterface.RECOVERY_NONE; // recovery action request, received from the server
      private int _rmIdx; // the index of the first field in the record main
      private int _rmSize; // number of "select"s in the record main
      private bool _skipParsing;
      private int _topRecId;
      private int _topRecIdx = Int32.MinValue;
      private bool _topRecIdxModified; //TODO: temporary fix for QCR# 304516. DO NOT USE THIS FLAG ELSEWHERE.
      private bool _transCleared; // If true, _task is the owner of a Rec level transaction and the _task is in _task suffix
      private char _unknownRcmp = UNKNOWN_RCMPS_NOT_INITED; // signals if a virtual field without init expression has server rcmp action

      internal bool IsOneWayKey { get; private set; }

      protected bool _includesFirst;
      protected bool _includesLast;
      protected MgTreeBase _mgTree; //Tree for cache; TODO: re-design 

      private Record _currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER;
      private Record CurrRec
      {
         get
         {
            return _currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER;
         }
         set
         {
            // reset the prev dcValIds in mgcontrol
            if (_currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER != null && value == null)
               _currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER.resetDcValueId();

            _currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER = value;

            // set the dcValIds in mgcontrol
            if (_currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER != null)
               _currRec_DO_NOT_USE_DIRECTLY_USE_SETTER_GETTER.SetDcValueId();
         }
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      internal DataView(Task task)
      {
         _task = task;
         _fieldsTab = new FieldsTable();
         _recordsTab = new RecordsTable(true);
         // Records inserted through the records-tab are kept as a linked list
         _modifiedRecordsTab = new RecordsTable(false);
         // No linked list here. We use the order maintained by recordsTab
         init();
      }

      internal bool FlushUpdates
      {
         get
         {
            return _flushUpdates;
         }
      }

      internal bool HasMainTable
      {
         get
         {
            return _hasMainTable;
         }
      }

      internal int DBViewSize
      {
         get;
         set;
      }

      internal char taskModeFromCache { get; set; }

      internal int CurrentRecId { get { return _currRecId; } }

      /// <summary>
      ///   checks if the first row of data is exists in the client's data view
      /// </summary>
      /// <returns> true is the first row of data is included in the client </returns>
      internal bool IncludesFirst()
      {
         return _includesFirst;
      }

      /// <summary>
      ///   checks if the last row of data is included in the client's data view
      /// </summary>
      /// <returns> true if the last row of data is included in the client's data view </returns>
      internal bool IncludesLast()
      {
         return _includesLast;
      }

      /// <summary>
      /// </summary>
      /// <param name = "mgTree"></param>
      /// <returns></returns>
      internal void setMgTree(MgTreeBase mgTree)
      {
         _mgTree = mgTree;
      }

      internal int Dvcount  //Holds dataview vars count of previos task
      {
         get;
         set;
      }

      /// <summary>
      /// Total records count for absolute scrollbar 
      /// </summary>
      internal int TotalRecordsCount { get; set; }

      /// <summary>
      /// Indicates no of records before existing records set which are not added into recordstable yet.
      /// </summary>
      internal int RecordsBeforeCurrentView { get; set; }

      /// <summary>
      ///   parse the DVHEADER tag
      /// </summary>
      public override void fillHeaderData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initHeaderInnerObjects(parser, parser.getNextTag()))
         {
         }

         Dvcount = ((Task)_task).ctl_itm_4_parent_vee(0, 1);
      }

      /// <summary>
      ///   parse the DVHEADER inner objects
      /// </summary>
      /// <param name = "foundTagName">  name of tag, of object, which need be allocated</param>
      /// <returns> is finished inner tag</returns>
      private bool initHeaderInnerObjects(XmlParser parser, String foundTagName)
      {
         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(XMLConstants.MG_TAG_DVHEADER))
         {
            Logger.Instance.WriteDevToLog("dvheader found");
            getHeaderAttributes(parser);
            _fieldsTab.fillData(this);
            ((FieldsTable) _fieldsTab).setRMPos(_rmSize, _rmIdx);
         }
         else if (foundTagName.Equals('/' + XMLConstants.MG_TAG_DVHEADER))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
            return false;

         return true;
      }

      /// <summary>
      ///   get the attributes of the DVHEADER tag
      /// </summary>
      private void getHeaderAttributes(XmlParser parser)
      {
         List<String> tokensVector;
         String tag, attribute, valueStr;

         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(XMLConstants.MG_TAG_DVHEADER) + XMLConstants.MG_TAG_DVHEADER.Length);
            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               attribute = (tokensVector[j]);
               valueStr = (tokensVector[j + 1]);

               switch (attribute)
               {
                  case ConstInterface.MG_ATTR_RMPOS:
                     {
                        int i = valueStr.IndexOf(",");

                        if (i > -1)
                        {
                           _rmSize = Int32.Parse(valueStr.Substring(0, i));
                           _rmIdx = Int32.Parse(valueStr.Substring(i + 1));
                        }
                        break;
                     }

                  case ConstInterface.MG_ATTR_COMPUTE_BY:
                     _computeBy = valueStr[0];

                     if (_computeBy == COMPUTE_FLUSH_UPDATES)
                     {
                        _computeBy = COMPUTE_NEWREC_ON_SERVER;
                        _flushUpdates = true;
                     }
                     else
                        _flushUpdates = false;

                     break;

                  case ConstInterface.MG_ATTR_HAS_MAIN_TBL:
                     _hasMainTable = XmlParser.getBoolean(valueStr);
                     break;

                  case ConstInterface.MG_ATTR_CHUNK_SIZE:
                     _chunkSize = XmlParser.getInt(valueStr);
                     break;

                  case ConstInterface.MG_ATTR_CHUNK_SIZE_EXPRESSION:
                     _chunkSizeExpression = XmlParser.getInt(valueStr);
                     break;

                  default:
                     Logger.Instance.WriteExceptionToLog("Unknown attribute for <" + XMLConstants.MG_TAG_DVHEADER +
                                                   "> tag: " +
                                                   attribute);
                     break;
               }
            }
            parser.setCurrIndex(++endContext); // to delete ">" too
            return;
         }
         Logger.Instance.WriteExceptionToLog("in DataView.getHeaderAttributes(): out of bounds");
      }

      /// <summary>
      ///   parse the DATAVIEW tag
      /// </summary>
      internal void fillData()
      {
         _skipParsing = false;
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         while (initInnerObjects(parser, parser.getNextTag()))
         {
         }
         ResetFirstDv();
      }

      /// <summary>
      /// get transaction begin of the task
      /// </summary>
      /// <returns></returns>
      private char GetTransactionBegin()
      {
         char  transBegin = (char) (0);
         Property transBeginProp = _task.getProp(PropInterface.PROP_TYPE_TRASACTION_BEGIN);
         if (transBeginProp != null)
            transBegin = transBeginProp.getValue()[0];

         return transBegin;
      }
      /// <summary>
      ///   parse the DATAVIEW inner objects
      /// </summary>
      /// <param name = "foundTagName">  name of tag, of object, which need be allocated</param>
      /// <returns> is finished inner tag</returns>
      private bool initInnerObjects(XmlParser parser, String foundTagName)
      {         
         
         MgForm form = (MgForm)_task.getForm();

         if (foundTagName == null)
            return false;

         if (foundTagName.Equals(ConstInterface.MG_TAG_DATAVIEW))
         {
            bool invalidate = getAttributes(parser);

            if (_skipParsing)
            {
               int endContext = parser.getXMLdata().IndexOf(END_DV_TAG, parser.getCurrIndex());
               parser.setCurrIndex(endContext + END_DV_TAG.Length);
            }
            else
            {
               // parse the exec stack entries values
               fillExecStack(parser);

               // parse the data controls values
               fillDataDc(parser);
               if (invalidate && form != null)
                  form.SetTableItemsCount(0, true);

               char taskRefreshType = _recordsTab.fillData(this, _insertAt);

               if (form != null)
               {
                  if (_insertAt == 'B' && (invalidate || _recordsTab.InsertedRecordsCount > 0))
                     form.SetTableItemsCount(0, true);
                  form.SetTableItemsCount(false);
                  if (invalidate && _mgTree != null)
                     _mgTree.clean();
               }
               if (CurrRec != null && CurrRec.InRecompute && taskRefreshType == Constants.TASK_REFRESH_NONE)
                  taskRefreshType = Constants.TASK_REFRESH_CURR_REC;

               if (invalidate)
               {
                  taskRefreshType = Constants.TASK_REFRESH_FORM;
                  if (_task.IsSubForm)
                     getTask().DoSubformPrefixSuffix = true;
               }

               getTask().SetRefreshType(taskRefreshType);
               UpdateDataviewAfterInsert();
               /*
               * the records table might set a new current record. It
               * actually becomes the current record by calling setCurrRec()
               */
               int newRecId = _recordsTab.getInitialCurrRecId();

               if (newRecId != Int32.MinValue && (_recovery == ConstInterface.RECOVERY_NONE || _firstDv))
               {
                  // Make sure the client performs the first record prefix
                  if (_firstDv && _recovery == ConstInterface.RECOVERY_NONE &&
                      (_task.getLevel() == 0 || _task.getLevel() == Constants.TASK_LEVEL_TASK))
                     _task.setLevel(Constants.TASK_LEVEL_TASK);

                  // If the server sent us an updated currRec then invalidate all current values.
                  // this will force all new values to be copied from the currRec to the dataview,
                  // especially virtual fields with no init expression.
                  ((FieldsTable) _fieldsTab).invalidate(true, Field.LEAVE_FLAGS);

                  if (newRecId != _currRecId)
                  {
                     setCurrRec(newRecId, !_firstDv);
                     _original = CurrRec.replicate(); // Defect 115374

                     // fix bug #779858: force compute for the first record in a screen mode _task
                     if (_firstDv)
                        takeFldValsFromCurrRec();
                     // QCR #981320: make sure that virtuals with no init or control are initialized
                     if (_task.getForm() != null)
                       ((MgForm)(_task.getForm())).updateDisplayLineByDV();
                  }
                  else
                     takeFldValsFromCurrRec();
               }
               else if (invalidate && _recordsTab.getSize() > 0)
               {
                  if (form == null || form.isScreenMode())
                  {
                     try
                     {
                        setCurrRecByIdx(0, false, true, false, 0);
                     }
                     catch (RecordOutOfDataViewException)
                     {
                     }
                  }
                  else
                  {
                     // QCR #431630: For forms in line mode there was a situation in which
                     // after invalidation, the current record was set to be the first of
                     // the dataview but the current line of the form was greater than 0.
                     // It caused saving an original record which is different than the
                     // original of the current form line. To solve this problem we first
                     // make sure that the cursor parks on the first row of the dataview.
                     ((FieldsTable) _fieldsTab).invalidate(true, Field.LEAVE_FLAGS);
                     setTopRecIdx(0);
                     if (_task.getForm() != null)
                        ((MgForm)_task.getForm()).SetTableTopIndex();
                     try
                     {
                        setCurrRecByIdx(0, false, true, true, 0);
                        if (_task.getForm() != null && _mgTree == null)
                        //to update tree we need to load nodes first
                           ((MgForm)(_task.getForm())).updateDisplayLineByDV();
                     }
                     catch (RecordOutOfDataViewException)
                     {
                     }

                     if (_task.getLevel() == Constants.TASK_LEVEL_CONTROL && _task.getLastParkedCtrl() != null &&
                         _mgTree == null)
                        _task.getLastParkedCtrl().resetPrevVal();
                  }
                  saveOriginal(); // After invalidation, we must save the original record too
                  ((FieldsTable) _fieldsTab).invalidate(true, Field.CLEAR_FLAGS);
               }

               // QCR #761848 prevCurrRec should set the first time the dataview is initialized
               if (_firstDv)
                  setPrevCurrRec();

               // Make sure that the sub forms are not unnecessarily refreshed:
               // After setting the current record and before executing record prefix
               // we save the current record as the previous record so when the record prefix
               // is executed it will skip refreshing the sub forms. We do that because
               // the server already passes back to the client all the dataviews of the
               // sub forms in a single iteration.
               if (invalidate & _task.IsSubForm)
                  _task.getForm().getSubFormCtrl().RefreshOnVisible = false;

               if (_recovery == ConstInterface.RECOVERY_NONE)
               {
                  // No need to perform record prefix after a record level transaction retry
                  if (getTask().getAfterRetry(ConstInterface.RECOVERY_RETRY) &&
                      !getTask().IsAfterRetryBeforeBuildXML)
                     _task.setLevel(Constants.TASK_LEVEL_RECORD);
               }
               else
               {
                  var transBegin = (char)GetTransactionBegin();

                  bool stopExecution = true;

                  switch (_recovery)
                  {
                     case ConstInterface.RECOVERY_ROLLBACK:
                        getTask().TaskTransactionManager.CreateAndExecuteRollbackLocalTransaction(RollbackEventCommand.RollbackType.ROLLBACK);

                        if (transBegin == ConstInterface.TRANS_RECORD_PREFIX)
                        {
                           // QCR #747316 can't rollback a lost record
                           // Also, in case of non interactive _task, skip to the next record.
                           if (_task.getMode() == Constants.TASK_MODE_DELETE ||
                               !_task.IsInteractive)
                              stopExecution = false;
                           else
                           {
                              getTask().setAfterRetry(ConstInterface.RECOVERY_ROLLBACK);
                              CurrRec.restart(CurrRec.getMode());
                              CurrRec.resetModified();
                              _original = CurrRec.replicate(); //QCR #
                              _pendingRecovery = RECOVERY_ACT_CANCEL;
                           }
                        }
                        else if (transBegin != ConstInterface.TRANS_NONE)
                        {
                           _task.setLevel(Constants.TASK_LEVEL_TASK);
                           _pendingRecovery = RECOVERY_ACT_BEGIN_TABLE;
                           getTask().setAfterRetry(ConstInterface.RECOVERY_ROLLBACK);
                           // ensure _task close action is stopped (taskEnd)
                        }

                        break;

                     case ConstInterface.RECOVERY_RETRY:
                        RecoveryRetry(transBegin);
                        break;
                  }

                 if (stopExecution)
                 {
                    ClientManager.Instance.EventsManager.setStopExecution(true);
                    // in case the task suffix was already executed, need to set this flag to false in order to enable it to execute TS again.
                    getTask().TaskSuffixExecuted = false;
                 }
               }
            }
         }
         else if (foundTagName.Equals('/' + ConstInterface.MG_TAG_DATAVIEW))
         {
            if (_includesFirst && _includesLast && isEmpty())
            {
               _currRecId = Int32.MinValue;
               _currRecIdx = Int32.MinValue;
               CurrRec = null;
               setTopRecIdx(Int32.MinValue);
            }
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
            return false;

         return true;
      }

      /// <summary>
      /// 
      /// </summary>
      internal void HandleLocalDataError()
      {
         if (getTask().IsInteractive)
         {            
            var transBegin = getTask().TaskTransactionManager.GetTransactionBeginValue(true);
            
            getTask().TaskTransactionManager.HandelTransactionErrorHandlingsRetry(ref transBegin);

            RecoveryRetry(transBegin);

            ClientManager.Instance.EventsManager.setStopExecution(true);
            ClientManager.Instance.ProcessRecovery();
         }
         else
         {
            // while we get error in the local data we need to exit the task.
            getTask().CancelAndRefreshCurrentRecordAfterRollback();
            // fixed bug#:432806, while we on endtask don't execute again end task.
            if (!getTask().InEndTask)
               ClientManager.Instance.EventsManager.handleInternalEvent(getTask(), InternalInterface.MG_ACT_EXIT, EventSubType.ExitDueToError);
         }
      }    

      /// <summary>
      /// 
      /// </summary>
      /// <param name="transBegin"></param>
      private void RecoveryRetry(char transBegin)
      {
         if (transBegin == ConstInterface.TRANS_RECORD_PREFIX)
         {
            // No need to re-perform record prefix during retry.
            _task.setLevel(Constants.TASK_LEVEL_RECORD);

            // ensure that the record will be passed to the server after correction
            getTask().setTransactionFailed(true);

            // ensure record prefix is not executed for all tasks which participate in the transaction
            getTask().setAfterRetry(ConstInterface.RECOVERY_RETRY);

            // QCR #479686: no need to add the curr rec to the list anymore
            // because the insert/modify may be canceled and a rollback event
            // would be sent to the server
            CurrRec.restart(CurrRec.getMode());

            // An error when initializing the _task needs no movement (error in rec-prefix)
            if (!_firstDv)
            {
               takeFldValsFromCurrRec();
               _pendingRecovery = RECOVERY_ACT_MOVE_DIRECTION_BEGIN;
            }
         }
         else if (transBegin != ConstInterface.TRANS_NONE)
         {
            _task.setLevel(Constants.TASK_LEVEL_TASK);
            _pendingRecovery = RECOVERY_ACT_BEGIN_SCREEN;
            getTask().setAfterRetry(ConstInterface.RECOVERY_RETRY);
            // ensure _task close action is stopped (taskEnd)
            getTask().setTransactionFailed(true);
         }
      }

      /// <summary>
      /// insert single record
      /// </summary>
      /// <param name="record"></param>
      internal void InsertSingleRecord(Record record)
      {
         _recordsTab.InsertedRecordsCount = 0;
         _recordsTab.InsertRecord(_insertAt, record);
         UpdateDataviewAfterInsert();
      }

      /// <summary>
      /// update Dataview After insert

      /// </summary>
      private void UpdateDataviewAfterInsert()
      {
         if (_insertAt == 'B')
         {
            int newRecs = _recordsTab.InsertedRecordsCount;
            if (_currRecIdx != Int32.MinValue)
               _currRecIdx += newRecs;
            if (_topRecIdx != Int32.MinValue)
               _topRecIdx += newRecs;
            if (_oldTopRecIdx != Int32.MinValue)
               _oldTopRecIdx += newRecs;
         }
      }

      /// <summary>
      ///   get the attributes of the DATAVIEW tag
      /// </summary>
      private bool getAttributes(XmlParser parser)
      {
         List<String> tokensVector;
         String tag;
         bool boolVal;
         IClientCommand dataViewCommand = null;

         _recovery = ConstInterface.RECOVERY_NONE;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_DATAVIEW) + ConstInterface.MG_TAG_DATAVIEW.Length);
            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            _insertAt = ' ';
            // find and execute invalidate immediately
            bool invalidate = peekInvalidate(tokensVector);

            // [MH] A rather bad solution for the problem of QCR 293064: The 'empty data view' flag should not be
            // changed for local data. This solution relies on the fact that the task can have only local data or
            // remote data. However, when we support 'mixed' mode, we'd have to refine this condition.
            if (!this.getTask().DataviewManager.HasLocalData)
               setEmptyDataview(false);

            // locateFirstRec is sent by server only if records are located for the subform.
            // So, it should be initialized to 0.
            _locateFirstRec = 0;

            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               string attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);

               switch (attribute)
               {
                  case ConstInterface.MG_ATTR_INSERT_AT:
                     _insertAt = valueStr[0];
                     break;
                  case ConstInterface.MG_ATTR_INCLUDE_FIRST:
                     boolVal = XmlParser.getBoolean(valueStr);
                     SetIncludesFirst(boolVal);
                     break;
                  case ConstInterface.MG_ATTR_INCLUDE_LAST:
                     boolVal = XmlParser.getBoolean(valueStr);
                     SetIncludesLast(boolVal);
                     break;
                  case ConstInterface.MG_ATTR_IS_ONEWAY_KEY:
                     boolVal = XmlParser.getBoolean(valueStr);
                     SetOneWayKey(boolVal);
                    break;
                  case ConstInterface.MG_ATTR_DBVIEWSIZE:
                    DBViewSize = XmlParser.getInt(valueStr);
                     break;
                  case ConstInterface.MG_ATTR_INVALIDATE:
                     {
                        // invalidate command found - it was  executed by peekInvalidate
                     }
                     break;
                  case ConstInterface.MG_ATTR_TOP_REC_ID:
                     _topRecId = Int32.Parse(valueStr);
                     break;
                  case ConstInterface.MG_ATTR_TASKMODE:
                     if (((Task)_task).getOriginalTaskMode() == Constants.TASK_MODE_NONE || invalidate)
                        ((Task)_task).setOriginalTaskMode(valueStr);
                     _task.setProp(PropInterface.PROP_TYPE_TASK_MODE, valueStr);
                     break;
                  case ConstInterface.MG_ATTR_EMPTY_DATAVIEW:
                     setEmptyDataview(true);
                     break;
                  case XMLConstants.MG_ATTR_TASKID:
                     continue;
                     // do nothing
                  case ConstInterface.MG_ATTR_LOW_ID:
                     _lastCreatedRecId = Int32.Parse(valueStr);
                     break;
                  case ConstInterface.MG_ATTR_TRANS_STAT:
                     char transStat = valueStr[0];
                     switch (transStat)
                     {
                        case TRANS_STAT_OPENED:
                           dataViewCommand = CommandFactory.CreateSetTransactionStateDataviewCommand(getTask().getTaskTag(), true);
                           getTask().DataviewManager.RemoteDataviewManager.Execute(dataViewCommand);          
                           break;
                        case TRANS_STAT_CLOSED:
                           dataViewCommand = CommandFactory.CreateSetTransactionStateDataviewCommand(getTask().getTaskTag(), false);
                           getTask().DataviewManager.RemoteDataviewManager.Execute(dataViewCommand);          
                           getTask().setTransactionFailed(false);
                           break;
                        default:
                           break;
                     }
                     break;
                  case ConstInterface.MG_ATTR_RECOVERY:
                     if (((Task)_task).IsTryingToStop)
                        getTask().setTryingToStop(false);
                     getTask().resetExecEndTask();
                     _recovery = valueStr[0];
                     break;
                  case ConstInterface.MG_ATTR_DVPOS_VALUE:
                     _dvPosValue = Int64.Parse(valueStr); // initiate the dvPosValue
                     break;
                  case ConstInterface.MG_ATTR_LOCATE_FIRST_ID:
                     _locateFirstRec = Int32.Parse(valueStr);
                     break;
                  case ConstInterface.MG_ATTR_OFFSET:
                     getTask().locateQuery.Offset = Int32.Parse(valueStr);
                     break;
                  case ConstInterface.MG_ATTR_USER_SORT:
                     //QCR # 923380:Getting the value of userSort from the dataview header.If true then sort sign will be removed or else shown.
                     MgForm form = (MgForm)_task.getForm();
                     form.clearTableColumnSortMark(XmlParser.getBoolean(valueStr));
                     break;
                  case ConstInterface.MG_ATTR_VAL_TOTAL_RECORDS_COUNT:
                     TotalRecordsCount = XmlParser.getInt(valueStr);
                     break;
                  case ConstInterface.MG_ATTR_VAL_RECORDS_BEFORE_CURRENT_VIEW:
                     RecordsBeforeCurrentView = XmlParser.getInt(valueStr);
                     break;
                  default:
                     Logger.Instance.WriteExceptionToLog("Unknown attribute for <" + ConstInterface.MG_TAG_DATAVIEW +
                                                   "> tag: " + attribute);
                     break;
               }
            }
            parser.setCurrIndex(++endContext); // to delete ">" too
            return invalidate;
         }
         Logger.Instance.WriteExceptionToLog("in DataView.getAttributes(): out of bounds");
         return false;
      }

      internal void SetOneWayKey(bool val)
      {
         IsOneWayKey = val;
      }

      internal void SetIncludesLast(bool val)
      {
         _includesLast = val;
      }

      internal void SetIncludesFirst(bool val)
      {
         bool prevIncludeFirst = _includesFirst;
         _includesFirst = val;
         if (_includesFirst != prevIncludeFirst)
         {
            MgFormBase form = _task.getForm();
            if(form != null)
               form.SetTableItemsCount(0, true);
         }
      }

      /// <summary>
      ///   parse and fill inner data for all <dc_val> tags are contained into the <dataview>
      /// </summary>
      private void fillDataDc(XmlParser parser)
      {
         // Create a DC values builder.
         XMLBasedDcValuesBuilder dcvBuilder = new XMLBasedDcValuesBuilder();

         // Loop through all "dc_vals" tags.
         String foundTagName = parser.getNextTag();

         while (foundTagName != null && foundTagName.Equals(ConstInterface.MG_TAG_DC_VALS))
         {
            // Save current index for future reference, in case of an error.
            int dcValsPosition = parser.getCurrIndex();

            dcvBuilder.SerializedDCVals = parser.ReadToEndOfCurrentElement();
            DcValues dcv = dcvBuilder.Build();

            if (dcv != null)
               AddDcValues(dcv);
            else
               Logger.Instance.WriteExceptionToLog("Error while parsing DC values at position " + dcValsPosition);
            foundTagName = parser.getNextTag();
         }
      }

      public void AddDcValues(DcValues dcv)
      {
         _dcValsCollection.Add(dcv.getId(), dcv);
      }

      /// <summary>
      ///   parse and fill inner data for all <execstackentry> tags are contained into the <dataview>
      /// </summary>
      private void fillExecStack(XmlParser parser)
      {
         String foundTagName = parser.getNextTag();
         bool execStackTagExists = false;

         while (foundTagName != null && foundTagName.Equals(ConstInterface.MG_TAG_EXEC_STACK_ENTRY))
         {
            fillInnerExecStack(parser);
            foundTagName = parser.getNextTag();
            execStackTagExists = true;
         }
         if (execStackTagExists)
         {
            ClientManager.Instance.EventsManager.CopyOfflineTasksToServerExecStack();

            ClientManager.Instance.EventsManager.reverseServerExecStack();
         }
      }

      /// <summary>
      ///   fill inner objects of <execstackentry> tag
      /// </summary>
      private void fillInnerExecStack(XmlParser parser)
      {
         List<String> tokensVector;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         String attribute;
         String taskId = "";
         String handlerId = "";
         int operIdx = 0;

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_EXEC_STACK_ENTRY) +
                                     ConstInterface.MG_TAG_EXEC_STACK_ENTRY.Length);

            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);

            for (int j = 0; j < tokensVector.Count; j += 2)
            {
               attribute = (tokensVector[j]);
               string valueStr = (tokensVector[j + 1]);

               switch (attribute)
               {
                  case XMLConstants.MG_ATTR_TASKID:
                     taskId = valueStr;
                     break;

                  case ConstInterface.MG_ATTR_HANDLERID:
                     handlerId = valueStr;
                     break;

                  case ConstInterface.MG_ATTR_OPER_IDX:
                     operIdx = XmlParser.getInt(valueStr);
                     break;
               }
            }
            ClientManager.Instance.EventsManager.pushServerExecStack(taskId, handlerId, operIdx);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length);
         }
         else
            Logger.Instance.WriteExceptionToLog("in DataView.fillInnerExecStack() out of bounds");
      }

      /// <summary>
      ///   search for the invalidate attribute and if it is true then it also clears the dataview
      ///   execute this method BEFORE parsing the records
      /// </summary>
      /// <param name = "tokensVector">of all attributes and values</param>
      private bool peekInvalidate(IList tokensVector)
      {
         long sessionCounter = ((Task)_task).CommandsProcessor.GetSessionCounter();
         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            var attribute = ((String) tokensVector[j]);
            if (attribute.Equals(ConstInterface.MG_ATTR_INVALIDATE))
            {
               var valueStr = ((String) tokensVector[j + 1]);
               if (XmlParser.getBoolean(valueStr))
               {
                  // QCR #2863: When executing RECURSIVE calls to the server there were cases
                  // in which an older dataview superseds a new one because its parsing comes
                  // after the parsing of the new dataview. The solution is to skip the
                  // parsing of the older dataview if the newer dataview has invalidate=true
                  if (sessionCounter < _lastSessionCounter)
                     _skipParsing = true;
                  else
                  {
                     _lastSessionCounter = sessionCounter;
                     clear();
                     return true;
                  }
               }
               return false;
            }
         }
         return false;
      }

      /// <summary>
      ///   get a field by its name
      /// </summary>
      /// <param name = "fldName">of the field </param>
      /// <returns> required field (or null if not found) </returns>
      internal Field getField(String fldName)
      {
         if (_fieldsTab != null)
            return (Field) _fieldsTab.getField(fldName);

         Logger.Instance.WriteExceptionToLog("in DataView.getField(String): There is no fieldsTab object");
         return null;
      }

      /// <summary>
      ///   build the XML string for the dataview
      /// </summary>
      /// <param name = "message">the message buffer
      /// </param>
      internal void buildXML(StringBuilder message)
      {
         DcValues dcv;
         String taskTag = _task.getTaskTag();
         int brkLevelIndex = _task.getBrkLevelIndex();
         bool currRecDeleted = false;
         String serverParent;

         Task contextTask = getTask().GetContextTask() as Task;
         
         String invokingTaskTag = contextTask.IsOffline ? INVOKED_FROM_OFFLINE_TASK : contextTask.getTaskTag();

         // put the dataview tag even if there are no modified records just to let
         // the server know the current record
         message.Append("\n      <" + ConstInterface.MG_TAG_DATAVIEW + " " + XMLConstants.MG_ATTR_TASKID + "=\"" +
                        taskTag + "\"");

         // QCR 423064 puts the current executing handler index
         message.Append(" " + ConstInterface.MG_ATTR_TASKLEVEL + "=\"" + brkLevelIndex + "\"");

         //If the trigger task is an Offline program, the parent on the server should be 
         //the Main Program of the current CTL.
         serverParent = "0";
         Task triggeringTask = getTask().getTriggeringTask();
         if (triggeringTask != null)
         {
            if (triggeringTask.IsOffline)
            {
               int ctlIdx = triggeringTask.getCtlIdx();
               Task mainPrg = MGDataCollection.Instance.GetMainProgByCtlIdx(ctlIdx);
               serverParent = mainPrg.getTaskTag();
            }
            else
               serverParent = triggeringTask.getTaskTag();
         }

         if (_currRecId > Int32.MinValue)
         {
            if (CurrRec != null)
               message.Append(" " + ConstInterface.MG_ATTR_CURR_REC + "=\"" + _currRecId + "\"");
            message.Append(" " + ConstInterface.MG_ATTR_TASKMODE + "=\"" + _task.getMode() + "\"");

            if (_transCleared)
            {
               message.Append(" " + ConstInterface.MG_ATTR_TRANS_CLEARED + "=\"1\"");
               _transCleared = false;
            }

            // QCR 779982 puts the invoking _task id and the direct parent _task id
            message.Append(" " + ConstInterface.MG_ATTR_INVOKER_ID + "=\"" + invokingTaskTag + "," + serverParent + "\"");

            // put the current ((DataView)dataView) dvPos value
            if (getTask().isCached())
               message.Append(" " + ConstInterface.MG_ATTR_DVPOS_VALUE + "=\"" + _dvPosValue + "\"");

            message.Append(" " + ConstInterface.MG_ATTR_LOOP_COUNTER + "=\"" + getTask().getLoopCounter() + "\"");

            //send subform visibility only if the subform with property "RefreshWhenHidden=N" and it is not visible,
            //i.e. not needed refresh from the server
            if (_task.IsSubForm)
            {
               MgControl subformCtrl = (MgControl)_task.getForm().getSubFormCtrl();
               if (!subformCtrl.isVisible() &&
                   !subformCtrl.checkProp(PropInterface.PROP_TYPE_REFRESH_WHEN_HIDDEN, false))
                  message.Append(" " + ConstInterface.MG_ATTR_SUBFORM_VISIBLE + "=\"0\"");
            }

            if (!_task.IsInteractive)
               message.Append(" " + ConstInterface.MG_ATTR_TASK_COUNTER + "=\"" + getTask().getCounter() + "\"");

            //QCR#591973: lastRecid and oldFirstRecID should be sent by client.
            //In case of mouse scrolling, we park on the same record (don't leave the current record). So on server we get curr record on which 
            //we were parked before scrolling. So we can not use this index to fetch the record. 
            //In this case, we need to send the last fetched record's idx from client in case of scroll down
            //and oldFirstRecID (first record of last sent chunk) in case of scroll up, from which we need to fetch the chunk of records.
            if (_task != null && _task.checkProp(PropInterface.PROP_TYPE_PRELOAD_VIEW, false))
            {
               message.Append(" " + ConstInterface.MG_ATTR_LAST_REC_ID + "=\"" +
                              (_recordsTab.getRecByIdx(getSize() - 1).getId()) + "\"");
               message.Append(" " + ConstInterface.MG_ATTR_OLD_FIRST_REC_ID + "=\"" + _recordsTab.getRecByIdx(0).getId() +
                              "\"");
            }

            // send the current position of the current rec in the forms table.
            // it can be used by the server to keep the record in this position after the view refresh.
            if (_task.IsInteractive)
            {
               MgForm form = (MgForm)_task.getForm();
               if (form != null && form.getRowsInPage() > 1)
               {
                  message.Append(" " + ConstInterface.MG_ATTR_CURR_REC_POS_IN_FORM + "=\"" +
                              form.getCurrRecPosInForm() + "\"");
               }
            }

            message.Append(XMLConstants.TAG_CLOSE);

            // Add User Ranges
            if (getTask().ResetRange || getTask().UserRngs != null)
            {
               message.Append(XMLConstants.START_TAG + ConstInterface.USER_RANGES);
               if (getTask().ResetRange)
               {
                  message.Append(" " + ConstInterface.CLEAR_RANGE + "=\"1\"");
                  getTask().ResetRange = false;
               }
               if (getTask().UserRngs != null)
               {
                  getTask().buildXMLForRngs(message, getTask().UserRngs, false);
                  getTask().UserRngs.Clear();
                  getTask().UserRngs = null;
               }
               else
                  message.Append(XMLConstants.TAG_TERM);
            }

            // Add User Locates
            if (getTask().ResetLocate || getTask().UserLocs != null)
            {
               message.Append(XMLConstants.START_TAG + ConstInterface.USER_LOCATES);
               if (getTask().ResetLocate)
               {
                  message.Append(" " + ConstInterface.CLEAR_LOCATES + "=\"1\"");
                  getTask().ResetLocate = false;
               }
               if (getTask().UserLocs != null)
               {
                  getTask().buildXMLForRngs(message, getTask().UserLocs, true);
                  getTask().UserLocs.Clear();
                  getTask().UserLocs = null;
               }
               else
                  message.Append(XMLConstants.TAG_TERM);
            }

            // Add User Sorts
            if (getTask().ResetSort || getTask().UserSorts != null)
            {
               message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_SORTS);
               if (getTask().ResetSort)
               {
                  message.Append(" " + ConstInterface.CLEAR_SORTS + "=\"1\"");
                  getTask().ResetSort = false;
               }
               if (getTask().UserSorts != null)
                  getTask().buildXMLForSorts(message);
               else
                  message.Append(XMLConstants.TAG_TERM);
            }

            // send deleted cache entries
            if (getTask().isCached())
            {
               String delList = getTask().getTaskCache().getDeletedListToXML();
               if (!delList.Equals(""))
               {
                  message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_DEL_LIST);
                  message.Append(" " + XMLConstants.MG_ATTR_ID + "=\"" + _task.getTaskTag() + "\"");
                  message.Append(" " + ConstInterface.MG_ATTR_REMOVE + "=\"" + delList + "\"");
                  message.Append(XMLConstants.TAG_TERM);

                  // if the current active dataview is in the deltede list mark it as un-valid so
                  // it wont enter the cache on return from server
                  if (getTask().getTaskCache().isDeleted(_dvPosValue))
                     setChanged(true);

                  getTask().getTaskCache().clearDeletedList();
               }
            }

            // remove unnecessary data control values
            var dcValsArray = new DcValues[_dcValsCollection.Count];
            _dcValsCollection.Values.CopyTo(dcValsArray, 0);
            for (int dcValIdx = 0; dcValIdx < dcValsArray.Length; dcValIdx++)
            {
               dcv = dcValsArray[dcValIdx];
               if (!dcv.HasReferences)
               {
                  message.Append(XMLConstants.START_TAG + ConstInterface.MG_TAG_DC_VALS);
                  message.Append(" " + XMLConstants.MG_ATTR_ID + "=\"" + dcv.getId() + "\"");
                  message.Append(" " + ConstInterface.MG_ATTR_REMOVE + "=\"1\"");
                  message.Append(XMLConstants.TAG_TERM);

                  _dcValsCollection.Remove(dcv.getId());
               }
            }

            if (CurrRec != null)
               currRecDeleted = (CurrRec.getMode() == DataModificationTypes.Delete &&
                                 _modifiedRecordsTab.getRecord(CurrRec.getId()) != null && CurrRec.SendToServer);

            // If the current user event being handled has Force exit = 'Pre Record Update', then
            // do not add current record to the modified records list. We anyway send current
            // record separately.
            bool skipCurrRec = false;

            if (CurrRec != null && _modifiedRecordsTab.getRecord(CurrRec.getId()) != null)
               skipCurrRec = ClientManager.Instance.EventsManager.isForceExitPreRecordUpdate(this.getTask());

            _modifiedRecordsTab.buildXML((FieldsTable) _fieldsTab, message, skipCurrRec, CurrRec.getId());

            if (_original != null)
               _original.clearMode();

            for (int i = _modifiedRecordsTab.getSize() - 1; i >= 0; i--)
            {
               if (skipCurrRec && _modifiedRecordsTab.getRecByIdx(i).getId() == CurrRec.getId())
                  continue;

               _modifiedRecordsTab.getRecByIdx(i).clearFlagsHistory();
               _modifiedRecordsTab.removeRecord(i);
            }

            if (CurrRec != null && !currRecDeleted)
            {
               // When we reconnect to the server once it is disconnected, server will create a new context and
               // therefore MP variables are with their intial (empty) value and not with their real values.
               // So, we must always send Main program's field's latest  values to the server.
               bool forceBuild = (_task.isMainProg() && ClientManager.Instance.RuntimeCtx.ContextID == RemoteCommandsProcessor.RC_NO_CONTEXT_ID);

               CurrRec.buildXML(message, true, forceBuild);
            }
         }
         // QCR 779982 if we have main prog with no select's send the invoker and parent _task
         else
         {
            // only for tree and only when the dataview is empty, we send an indication to the server.
            // we need it in case the user deleted the root of the tree. for the server, 1 rec was deleted, but for the client it means the dataview is empty now.
            if (_mgTree != null && isEmptyDataview())
               message.Append(" " + ConstInterface.MG_ATTR_EMPTY_DATAVIEW + "=\"1\"");

            message.Append(" " + ConstInterface.MG_ATTR_INVOKER_ID + "=\"" + invokingTaskTag + "," + serverParent + "\"" +
                           XMLConstants.TAG_CLOSE);
         }
         message.Append("\n      </" + ConstInterface.MG_TAG_DATAVIEW + XMLConstants.TAG_CLOSE);
      }

      /// <summary>
      ///   get a reference to the current record
      /// </summary>
      /// <returns> Record is the reference to the current record </returns>
      internal Record getCurrRec()
      {
         if (CurrRec == null && _currRecId > Int32.MinValue)
         {
            if (!setCurrRec(_currRecId, true))
               Logger.Instance.WriteExceptionToLog("DataView.getCurrRec(): record " + _currRecId + " not found");
         }
         return CurrRec;
      }

      /// <summary>
      ///   get a reference to the previous current record
      /// </summary>
      /// <returns> Record is the reference to the previous current record </returns>
      internal Record getPrevCurrRec()
      {
         return _prevCurrRec;
      }

      /// <summary>
      ///   get a reference to the original record
      /// </summary>
      /// <returns> Record is the reference to the original record
      /// </returns>
      internal Record getOriginalRec()
      {
         if (_original == null || _original.getId() != CurrRec.getId())
            return CurrRec;
         return _original;
      }

      /// <summary>
      ///   set the current record - returns true on success
      /// </summary>
      /// <param name = "id">the record id
      /// </param>
      /// <param name = "compute">if true then execute compute for the new record
      /// </param>
      internal bool setCurrRec(int id, bool compute)
      {
         int oldRecId = _currRecId;

         if (id > Int32.MinValue)
         {
            int newIdx = _recordsTab.getRecIdx(id);
            if (newIdx == -1)
               _original = null;
            else
            {
               try
               {
                  bool ignoreCurrRec = (_currRecId == Int32.MinValue);
                  setCurrRecByIdx(newIdx, !ignoreCurrRec, ignoreCurrRec, compute, SET_DISPLAYLINE_BY_DV);
                  if (id != oldRecId)
                     saveOriginal();
                  return true;
               }
               catch (RecordOutOfDataViewException)
               {
               }
            }
         }
         else
         {
            // do not point to any current record
            CurrRec = null;
            _original = null;
            _currRecId = Int32.MinValue;
         }
         return false;
      }

       /// <summary>
      ///   Set the current record index and returns true for success
      ///   Use the MG_DATAVIEW_FIRST_RECORD and MG_DATAVIEW_LAST_RECORD constants
      ///   to retrieve the first or the last record respectively
      /// </summary>
      /// <param name = "newIdx">the new index </param>
      /// <param name = "doSuffix">if true then do the record suffix for the previous record when the record was changed </param>
      /// <param name = "ignoreCurrRec">if true all the operations are done is if there is no current record </param>
      /// <param name = "compute">if true then execute compute for the new record </param>
      /// <param name = "newDisplayLine">to update form </param>
      internal void setCurrRecByIdx(int newIdx, bool doSuffix, bool ignoreCurrRec, bool compute,
                                           int newDisplayLine)
      {
         Record oldFirstRec, nextRec;
         int lastRecIdx = _recordsTab.getSize() - 1;

         // QCR #984960: the variable recordSuff is used to recognize if we have pressed
         // ctrl+home or ctrl+end in these cases we must initiate record suffix
         // the correct way to do this is using the doSuffix parameter but since it is used
         // in a lot of cases sometime not in a correct way, forcing record suffix
         // using doSuffix will cause many changes in the code.
         bool recordSuff = false;

         if (newIdx == Constants.MG_DATAVIEW_FIRST_RECORD)
         {
            //As moving to first record in the view, recordsBeforeCurrentView should be 0.
            if (getTask().isTableWithAbsolutesScrollbar())
               RecordsBeforeCurrentView = 0;

            if (_includesFirst)
            {
               // check if the current record is already the first record of the data view
               if (_currRecIdx >= 0 || _currRecIdx == Int32.MinValue)
               {
                  newIdx = 0;

                  // ctrl+home wase pressed
                  recordSuff = true;
               }
               else
                  throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.TOP);
            }
            else
            {
               fetchChunkFromServer(CHUNK_DV_TOP, doSuffix);
               setCurrRecByIdx(newIdx, false, ignoreCurrRec, compute, SET_DISPLAYLINE_BY_DV);
               return;
            }
         }
         else if (newIdx == Constants.MG_DATAVIEW_LAST_RECORD)
         {
            if (_includesLast)
            {
               // check if the current record is already the last record of the data view
               if (_currRecIdx <= lastRecIdx)
               {
                  newIdx = lastRecIdx;
                  // ctrl+end was pressed
                  recordSuff = true;
               }
               else
                  throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.BOTTOM);
            }
            else
            {
               //If last record is not in the view, and being fetched from the server, RecordsBeforeCurrentView = TotalRecordsCount. And as this chunk get added
               //into records table, the RecordsBeforeCurrentView will be decremented by no of rows in view.
               //This is because, we open the cursor with reverse direction for which do not bring the counts from gateway.
               if (getTask().isTableWithAbsolutesScrollbar())
                  RecordsBeforeCurrentView = TotalRecordsCount;

               fetchChunkFromServer(CHUNK_DV_BOTTOM, doSuffix);

               if (!_includesLast)
                  throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.BOTTOM);
               MgForm form = (MgForm)_task.getForm();
               if (form != null)
               {
                  while (!_includesFirst && form.getRowsInPage() >= getSize())
                  {
                     fetchChunkFromServer(CHUNK_CACHE_PREV, false);
                  }
               }
               setCurrRecByIdx(newIdx, false, ignoreCurrRec, compute, SET_DISPLAYLINE_BY_DV);
               return;
            }
         }
         // check whether the record was not found beyond the TOP end
         else if (newIdx < 0)
         {
            if (_includesFirst)
               throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.TOP);
            oldFirstRec = _recordsTab.getRecByIdx(0);
            if (_recordsTab.getSize() > 0 || IncludesLast())
               fetchChunkFromServer(CHUNK_CACHE_PREV, doSuffix);
            // if no records and its first simulate start table
            else if (_recordsTab.getSize() == 0 && IncludesFirst())
               fetchChunkFromServer(CHUNK_DV_TOP, doSuffix);
            else
               Logger.Instance.WriteExceptionToLog("in DataView.fetchChunkFromServer() wrong option");
            // get the new index of the record which was first before the fetch
            int oldFirstNewIdx = oldFirstRec != null
                                 ? _recordsTab.getRecIdx(oldFirstRec.getId())
                                 : _recordsTab.getSize();
            if (oldFirstNewIdx != 0)
               setCurrRecByIdx(oldFirstNewIdx + newIdx, false, ignoreCurrRec, compute, SET_DISPLAYLINE_BY_DV);
            return;
         }
         // check whether the record was not found beyond the BOTTOM end
         else if (newIdx > lastRecIdx)
         {
            if (_includesLast)
               throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.BOTTOM);
            fetchChunkFromServer(CHUNK_CACHE_NEXT, doSuffix);
            setCurrRecByIdx(newIdx, false, ignoreCurrRec, compute, SET_DISPLAYLINE_BY_DV);
            return;
         }

         // QCR # 984960 - see documentation for the variable recordSuff in the top of this method
         if (recordSuff || newIdx != _currRecIdx || ignoreCurrRec && compute)
         {
            if (!ignoreCurrRec && doSuffix && CurrRec != null)
            {
               int oldRecTabSize = _recordsTab.getSize();

               nextRec = _recordsTab.getRecByIdx(newIdx);
               if (nextRec.isNewRec())
               {
                  if (_task.getMode() != Constants.TASK_MODE_CREATE &&
                      !_task.checkProp(PropInterface.PROP_TYPE_ALLOW_CREATE, true))
                     ClientManager.Instance.EventsManager.setStopExecution(true);
               }

               if (!ClientManager.Instance.EventsManager.GetStopExecutionFlag())
               {
                  int orgTopRecIdx = 0;
                  //TODO: temporary fix for QCR# 304516. DO NOT USE THIS FLAG ELSEWHERE.
                  if (_topRecIdxModified)
                  {
                     orgTopRecIdx = getTopRecIdx();
                     restoreTopRecIdx();
                  }
                  ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_REC_SUFFIX);
                  if (_topRecIdxModified)
                     setTopRecIdx(orgTopRecIdx);
               }
               if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
                  throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.REC_SUFFIX_FAILED);

               // QCR #2046: when the next record is different than the current one we should
               // allow the execution of the record prefix by setting the _task level to TASK.
               if (nextRec != CurrRec && getTask().getAfterRetry())
                  _task.setLevel(Constants.TASK_LEVEL_TASK);

               // if the records table size was decreased during handling the
               // record suffix (e.g. cancel edit) and the user tries to move to
               // a record with a higher index then we should also decrease the
               // newIdx to reflect the change
               if (oldRecTabSize - 1 == _recordsTab.getSize() && _currRecIdx < newIdx)
                  newIdx--;

               // the new curr rec may be already set by the events chain triggered by the
               // record suffix above - in that case there is no need to continue
               if (newIdx == _currRecIdx)
               {
                  // QCR 918983 changes to the _task mode must be done before recompute
                  if (_task.getMode() == Constants.TASK_MODE_CREATE && !CurrRec.isNewRec() &&
                      !_task.getForm().inRefreshDisplay())
                     _task.setMode(Constants.TASK_MODE_MODIFY);
                  return;
               }
            }
            CurrRec = _recordsTab.getRecByIdx(newIdx);
            if (CurrRec == null)
               throw new ApplicationException("in DataView.setCurrRecByIdx() current record not found!");
            _currRecId = CurrRec.getId();
            _currRecIdx = newIdx;
            if (_task.getForm() != null)
            {
               //QCR #107926, we must update display line here, since currRecCompute which is activated later
               //may go to server and cause refresh display.
               //In case of adding new node to the tree, the node must be actually added to the tree.
               if (newDisplayLine == SET_DISPLAYLINE_BY_DV)
               {
                   if (_mgTree != null && ((MgTree)_mgTree).hasNodeInCreation())
                       _task.getForm().DisplayLine = ((MgTree)_mgTree).addNodeInCreation(_currRecId);
                  else
                     ((MgForm)(_task.getForm())).updateDisplayLineByDV();
               }
               else
                  _task.getForm().DisplayLine = newDisplayLine;
            }
            // QCR 918983 changes to the _task mode must be done before recompute
            if (_task.getMode() == Constants.TASK_MODE_CREATE && !CurrRec.isNewRec() &&
                !_task.getForm().inRefreshDisplay())
               _task.setMode(Constants.TASK_MODE_MODIFY);
            if (compute)
               currRecCompute(false);
         }
      }

      /// <summary>
      ///   recompute ALL the init expressions of the current record - call this method when
      ///   entering a record
      /// </summary>
      /// <param name = "forceClientCompute">if TRUE forces the client to compute the record even if it is flagged
      ///   as "server computed".
      /// </param>
      internal void currRecCompute(bool forceClientCompute)
      {
         ((FieldsTable) _fieldsTab).invalidate(false, Field.CLEAR_FLAGS);
         compute(false, forceClientCompute);
      }

      /// <summary>
      ///   saves a replication of the current record
      /// </summary>
      internal void saveOriginal()
      {
         if (!CurrRec.Modified)
            _original = CurrRec.replicate();
      }

      /// <summary>
      ///   fetch a chunk of records from the server
      /// </summary>
      internal void fetchChunkFromServer(char chunkId, bool doSuffix)
      {
         CommandsTable cmdsToServer = getTask().getMGData().CmdsToServer;

         // QCR #430134: if the server sent us records fewer than the number that fits into a table
         // then we go back to the server and must refresh the screen.
         ((MgForm)_task.getForm()).FormRefreshed = true;

         if (doSuffix && CurrRec != null)
         {
            ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_REC_SUFFIX);
            if (ClientManager.Instance.EventsManager.GetStopExecutionFlag() || _task.isAborting())
               throw new RecordOutOfDataViewException(RecordOutOfDataViewException.ExceptionType.REC_SUFFIX_FAILED);
            ((MgForm)_task.getForm()).setSuffixDone(); // QCR 758639
         }

         int eventCode;
         int clientRecId = _currRecId;
         switch (chunkId)
         {
            case CHUNK_DV_TOP:
               eventCode = InternalInterface.MG_ACT_DATAVIEW_TOP;
               break;
            case CHUNK_DV_BOTTOM:
               eventCode = InternalInterface.MG_ACT_DATAVIEW_BOTTOM;
               break;
            case CHUNK_CACHE_PREV:
               clientRecId = FirstRecord == null ? 0 : FirstRecord.getId();
               eventCode = InternalInterface.MG_ACT_CACHE_PREV;
               break;
            case CHUNK_CACHE_NEXT:
               eventCode = InternalInterface.MG_ACT_CACHE_NEXT;
               clientRecId = LastRecord == null ? 0 : LastRecord.getId();
               break;
            default:
               Logger.Instance.WriteExceptionToLog("in DataView.fetchChunkFromServer() unknown chunk id: " + chunkId);
               return;
         }
         EventCommand cmd = CommandFactory.CreateEventCommand(_task.getTaskTag(), eventCode);
         cmd.ClientRecId = clientRecId;
         getTask().DataviewManager.Execute(cmd);

      }

      /// <summary>
      ///   returns true if and only if a record with the given index exist
      /// </summary>
      /// <param name = "recIdx">index to check </param>
      internal bool recExists(int recIdx)
      {
         int lastRecIdx = _recordsTab.getSize() - 1;
         if (recIdx >= 0 && recIdx <= lastRecIdx)
            return true;

         return false;
      }

      /// <summary>
      ///   checks if the if the current first record exist
      ///   in case there is no locate check include first
      /// </summary>
      internal bool checkFirst(int locateFirst)
      {
         return (_recordsTab.getRecord(locateFirst) != null);
      }

      /// <summary>
      ///   get the current record index
      /// </summary>
      /// <returns> the record index </returns>
      internal int getCurrRecIdx()
      {
         if (_currRecIdx == Int32.MinValue)
         {
            if (!setCurrRec(_currRecId, true))
            {
               if (!getTask().HasMDIFrame)
                  Logger.Instance.WriteWarningToLog("DataView.getCurrRecIdx(): _task " +
                                                  (getTask()).PreviouslyActiveTaskId +
                                                  ", record " + _currRecId + " not found");
            }
         }
         return _currRecIdx;
      }

      /// <summary>
      /// return the dbviewrowid for the current record
      /// </summary>
      internal int getCurrDBViewRowIdx()
      {
         return CurrRec.getDBViewRowIdx();
      }

      /// <summary>
      ///   set the top record index and saves the old value
      /// </summary>
      /// <param name = "newTopRecIdx">the new value for the top record index </param>
      internal void setTopRecIdx(int newTopRecIdx)
      {
         _oldTopRecIdx = getTopRecIdx();
         _topRecIdx = newTopRecIdx;
      }

      /// <summary>
      ///   get the top record index
      /// </summary>
      internal int getTopRecIdx()
      {
         return _topRecIdx;
      }

      /// <summary>
      ///   set the top record index modified
      /// </summary>
      /// <param name = "val">the new value for topRecIdxModified </param>
      internal void setTopRecIdxModified(bool val)
      {
         //TODO: temporary fix for QCR# 304516. DO NOT USE THIS FLAG ELSEWHERE.
         _topRecIdxModified = val;
      }

      /// <summary>
      ///   get the chunk size index
      /// </summary>
      protected internal int getChunkSize()
      {
         return _chunkSize;
      }

      /// <summary>
      ///   restore the old saved value of the top record index
      /// </summary>
      internal void restoreTopRecIdx()
      {
         _topRecIdx = _oldTopRecIdx;
      }

      /// <summary>
      ///   removes all the records from the Dataview
      /// </summary>
      internal void clear()
      {
         _recordsTab.removeAll();
         _original = null;
         MgForm form = (MgForm)_task.getForm();
         if (form != null)
            _task.getForm().SetTableItemsCount(0, true);
         _modifiedRecordsTab.removeAll();
         init();
      }

      /// <summary>
      ///   initialize the class members
      /// </summary>
      private void init()
      {
         _currRecId = Int32.MinValue;
         _currRecIdx = Int32.MinValue;
         CurrRec = null;
         _prevCurrRec = null;
         _includesFirst = false;
         _includesLast = false;
         IsOneWayKey = false;
         setTopRecIdx(Int32.MinValue);
         ((FieldsTable) _fieldsTab).invalidate(false, Field.CLEAR_FLAGS);
      }

      /// <summary>
      ///   refreshes the subForms
      /// </summary>
      internal void computeSubForms()
      {
         // we do not need to go to the server due to updates
         if (getTask().prepareCache(true))
         {
            // one of the needed d.v was not found in the cache
            if (!getTask().testAndSet(true))
            {
               compute(true, false);
            }
         }
         else
            compute(true, false);
      }

      /// <summary>
      ///   compute the values of the fields without updating the display
      /// </summary>
      /// <param name = "subForms">if true the a subForms compute is to be done</param>
      /// <param name = "forceClientCompute">if TRUE forces the client to compute the record even if it is flagged
      ///   as "server computed".
      /// </param>
      private void compute(bool subForms, bool forceClientCompute)
      {
         Field fld;
         Record tmpRec = null;
         bool computeByServer = !computeByClient();
         char orgComputeBy = _computeBy;

         if (forceClientCompute)
            _computeBy = COMPUTE_NEWREC_ON_CLIENT;

         try
         {
            CurrRec.setInCompute(true);

            if (!subForms)
            {
               if (CurrRec.isNewRec())
               {
                  if (_task.getMode() != Constants.TASK_MODE_CREATE && !isEmptyDataview())
                     _task.setMode(Constants.TASK_MODE_CREATE);
               }
               else if (_task.getMode() == Constants.TASK_MODE_CREATE)
                  _task.setMode(Constants.TASK_MODE_MODIFY);
            }

            //TODO : improve this condition
            bool getRecordData = computeByServer || ((Task)_task).ForceLocalCompute;
            // for newly created records - go to the server (if they were not computed already)
            if (subForms || getRecordData && !CurrRec.isComputed() && !forceClientCompute)
            {
               //TODO : refactor after local subforms implementation
               IClientCommand cmd = CommandFactory.CreateComputeEventCommand(_task.getTaskTag(), subForms, _currRecId); // the 99999 means compute the subforms
               ((Task)_task).DataviewManager.Execute(cmd);
            }
            else
            {
               // QCR #921475 - relink during compute destroys values of virtual fields with no init exp.
               // thus we save them before the compute and restore them during the loop.
               if (getRecordData)
                  tmpRec = new Record(CurrRec.getId(), this, true);

               ((FieldsTable) _fieldsTab).setServerRcmp(false);
               int from = ((FieldsTable) _fieldsTab).getRMIdx();
               int size = ((FieldsTable) _fieldsTab).getRMSize();

               for (int i = from; i < from + size; i++)
               {
                  fld = (Field) _fieldsTab.getField(i);
                  if (getRecordData && ((FieldsTable)_fieldsTab).serverRcmpDone() && fld.IsVirtual && !fld.VirAsReal &&
                      !fld.hasInitExp())
                  {
                     // TODO: (ilan) what if the field's original value was null, and changed to not-null
                     Record orgCurrRec = CurrRec;
                     CurrRec = tmpRec;
                     try
                     {
                        fld.takeValFromRec();
                     }
                     finally
                     {
                        CurrRec = orgCurrRec;
                     }
                  }

                  fld.compute(false);
               }
               if (_task != null && _task.getForm() != null)
                  ((MgForm)_task.getForm()).RecomputeTabbingOrder(true);
            }
         }
         finally
         {
            CurrRec.setInCompute(false);
            CurrRec.setLateCompute(false);
            _computeBy = orgComputeBy;
         }
      }

      /// <summary>
      /// add first record
      /// </summary>
      internal void addFirstRecord()
      {
         DataModificationTypes recordMode = getTask().getMode() == Constants.TASK_MODE_CREATE ? DataModificationTypes.Insert : DataModificationTypes.None;
         addRecord(false, true, true, 0, recordMode);
      }
      /// <summary>
      ///   create and add a record to the dataview and make it the current record
      /// </summary>
      /// <param name = "doSuffix">if true then do the suffix of the previous record </param>
      /// <param name = "ignoreCurrRec">if true then do the suffix of the previous record</param>
      /// <returns> the new rec index </returns>
      internal int addRecord(bool doSuffix, bool ignoreCurrRec)
      {
         int newCurrRecId = (--_lastCreatedRecId);
         DataModificationTypes mode = DataModificationTypes.Insert;
         return addRecord(doSuffix, ignoreCurrRec, false, newCurrRecId, mode);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="doSuffix">if true then do the suffix of the previous record</param>
      /// <param name="ignoreCurrRec">if true then do the suffix of the previous record</param>
      /// <param name="isFirstRecord"> is first record</param>
      /// <param name="newCurrRecId"> new record id</param>
      /// <param name="mode"> operation mode</param>
      /// <returns>the new rec index</returns>
      internal int addRecord(bool doSuffix, bool ignoreCurrRec, bool isFirstRecord, int newCurrRecId, DataModificationTypes mode)
      {
         // give the new record a negative id

         var rec = new Record(newCurrRecId, this, false, isFirstRecord);
         
         int newCurrRecIdx = (_currRecIdx != Int32.MinValue
                              ? _currRecIdx + 1
                              : 0);
         _recordsTab.insertRecord(rec, newCurrRecIdx);
         rec.setMode(mode);
         try
         {
            bool compute = isFirstRecord ? false : true;
            setCurrRecByIdx(newCurrRecIdx, doSuffix, ignoreCurrRec, compute, SET_DISPLAYLINE_BY_DV);

            //while we in create mode don't set reset to _newRec
            if (isFirstRecord &&  getTask().getMode() != Constants.TASK_MODE_CREATE)
               rec.setOldRec();
         }
         catch (RecordOutOfDataViewException)
         {
            if (ClientManager.Instance.EventsManager.GetStopExecutionFlag())
               newCurrRecIdx = -1;
            else
               throw new ApplicationException("in DataView.addRecord() invalid exception");
         }

         return newCurrRecIdx;
      }

      /// <summary>
      ///   remove the current record from the dataview
      /// </summary>
      internal void removeCurrRec()
      {
         bool onDelete; // true if we are removing the record due to delete. false if due to cancel on insert

         MgForm form = (MgForm)_task.getForm();
         if (CurrRec.getMode() == DataModificationTypes.Insert &&
             !getTask().transactionFailed(ConstInterface.TRANS_RECORD_PREFIX))
         {
            // the record was created by the client so it has to be removed from the
            // modified records table (if it is already there) because the server
            // doesn't have to know anything about it
            _modifiedRecordsTab.removeRecord(CurrRec);
            onDelete = false;
         }
         else
         {
            // move the current record from the records table to the modified table
            CurrRec.clearMode();
            CurrRec.setMode(DataModificationTypes.Delete);
            onDelete = true;
         }
         CurrRec.removeRecFromDc();
         _recordsTab.removeRecord(_currRecIdx);
         setChanged(true);
         if (isEmpty() && !((Task)_task).IsTryingToStop)
         {
            MoveToEmptyDataviewIfAllowed();
         }
         int newDisplayLine = calcLineToMove(onDelete);
         if (_mgTree != null)
         //remove node from tree
         {
            _mgTree.delete(form.DisplayLine);
            //no more records in the tree
            if (newDisplayLine == MgTreeBase.NODE_NOT_FOUND)
               reset();
         }
         // set the new display line && current record
         try
         {
            if (form.isScreenMode())
               setCurrRecByIdx(newDisplayLine, false, false, true, SET_DISPLAYLINE_BY_DV);
            else
               form.setCurrRowByDisplayLine(newDisplayLine, false, true);
         }
         catch (RecordOutOfDataViewException e)
         {
            if (_mgTree != null && e.noRecord())
                //node has no record
                ((MgTree)_mgTree).handleNoRecordException(newDisplayLine);
            else
            {
               // if the current index is beyond the bottom end of the dataview
               // try to set it to the preceding record
               try
               {
                  if (!isEmptyDataview())
                     newDisplayLine = form.getPrevLine(newDisplayLine);
                  if (form.isScreenMode())
                     setCurrRecByIdx(newDisplayLine, false, false, true, SET_DISPLAYLINE_BY_DV);
                  else
                     form.setCurrRowByDisplayLine(newDisplayLine, false, true);
               }
               catch (RecordOutOfDataViewException)
               {
                  // no more records in the dataview
                  //or no nodes in the tree
                  reset();
               }
            }
         }
      }

      /// <summary>
      /// remove record
      /// </summary>
      /// <param name="record"></param>
      internal void RemoveRecord( Record record)
      {
         int recIdx = _recordsTab.getRecIdx(record.getId());
         if (_topRecIdx > recIdx)
            _topRecIdx--;
         if (_oldTopRecIdx > recIdx)
            _oldTopRecIdx--;
         if (_currRecIdx > recIdx)
            _currRecIdx--;
         _recordsTab.removeRecord(record);
      }

      /// <summary>
      /// move to empty dataview if allowed
      /// </summary>
      internal void MoveToEmptyDataviewIfAllowed()
      {
         if (!isEmptyDataview() && ((Task)_task).getMode() != Constants.TASK_MODE_CREATE)
         {
            if (_task.checkProp(PropInterface.PROP_TYPE_ALLOW_EMPTY_DATAVIEW, true))
               setEmptyDataview(true);
         }
         else if (((Task)_task).getMode() == Constants.TASK_MODE_CREATE && isEmptyDataview())
         {
            setEmptyDataview(false);
         }
      }

      /// <summary>
      ///   returns display line to move after delete
      /// </summary>
      /// <param name = "onDelete">true if we are removing the record due to delete. false if due to cancel on insert
      /// </param>
      /// <returns>
      /// </returns>
      private int calcLineToMove(bool onDelete)
      {
         int newDisplayLine;

         // if the record is removed due to its deletion, we need to got to the next record in the view - 
         // now that we deleted the current record, the index of the next record is the same index that was
         // of the record that was deleted.    

         if (_mgTree != null)
            newDisplayLine = _mgTree.calcLineForRemove(onDelete);
         else if (onDelete)
            newDisplayLine = _currRecIdx;
         else
         {
            // #750190 Shaya
            // move to the PREV record after cancel
            if (_currRecIdx > 0)
               newDisplayLine = _currRecIdx - 1;
            else
               newDisplayLine = _currRecIdx;
         }
         return newDisplayLine;
      }

      /// <summary>
      ///   reset values if there is no current record
      /// </summary>
      internal void reset()
      {
         _currRecId = Int32.MinValue;
         _currRecIdx = Int32.MinValue;
         CurrRec = null;
         setTopRecIdx(Int32.MinValue);
         SetIncludesFirst(true);
         SetIncludesLast(true);
     }

      /// <summary>
      ///   cancel the editing of the current record and restore the old values of
      ///   the fields
      /// </summary>
      /// <param name = "onlyReal">if true only restores the values of the real fields. </param>
      /// <param name = "recomputeUnknowns">re-perform unknown recomputes. </param>
      internal bool cancelEdit(bool onlyReal, bool recomputeUnknowns)
      {
         MgControl currCtrl;
         Record tmpCurrRec = null;
         bool recRemoved = false;

         // QCR #362903 - At _task suffix, we want to preserve the last values of the virtuals.
         if (((Task)_task).InEndTask)
            onlyReal = true;

         if (CurrRec.getMode() == DataModificationTypes.Insert && CurrRec.isNewRec())
         {
            if (_includesFirst && _includesLast && (_recordsTab == null || _recordsTab.getSize() == 1) || !HasMainTable)
            {
               // defect 121968. When from the parent task "ViewRefresh" or "Cancel" are done and the subform is in Create node
               // and the subform has no records, _original is null. So pass it.
               if (_original != null)
                  CurrRec.setSameAs(_original, onlyReal);
            }
            else
            {
               removeCurrRec();
               recRemoved = true;
               _original = CurrRec.replicate(); // must copy again to prevent altering the original values
            }
         }
         // if (currRec.modified())
         else
         {
            // still on the same control
            if (!CurrRec.Modified)
            {
               currCtrl = (MgControl)_task.getLastParkedCtrl();
               if (currCtrl != null)
                  currCtrl.resetPrevVal();
            }

            if (recomputeUnknowns)
               tmpCurrRec = CurrRec.replicate();

            // ATTENTION !!!
            // we don't step over the currRec reference with the original record
            // to assure that if this record is both in the records table and in
            // the modified table these tables will keep referencing the same instance
            if (_original != null)
               CurrRec.setSameAs(_original, onlyReal);

            if (_original != null && recomputeUnknowns && !CurrRec.isSameRecData(tmpCurrRec, false, false))
               execUnknownRcmps(_original);

            takeFldValsFromCurrRec();
            _original = CurrRec.replicate(); // must copy again to prevent altering the original values
         }

         return recRemoved;
      }

      /// <summary>
      ///   check if there are no records in the dataview
      /// </summary>
      internal bool isEmpty()
      {
         return (_includesFirst && _includesLast && (_recordsTab == null || _recordsTab.getSize() == 0));
      }

      /// <summary>
      ///   add the current record to the modified records table - if it is not already there
      /// </summary>
      internal void addCurrToModified()
      {
         addCurrToModified(true);
      }

      /// <summary>
      ///   add the current record to the modified records table - if it is not already there
      /// </summary>
      /// <param name="isSetOldRec"></param>
      internal void addCurrToModified(bool setRecToOld)
      {
         _modifiedRecordsTab.addRecord(CurrRec);
         if (setRecToOld)
            CurrRec.setOldRec();

         IClientCommand dataViewCommand = CommandFactory.CreateSetTransactionStateDataviewCommand(getTask().getTaskTag(), true);
         getTask().DataviewManager.RemoteDataviewManager.Execute(dataViewCommand);          

      }


      /// <summary>
      ///   returns the value of the field from the previous record
      /// </summary>
      /// <param name = "fld">a reference to the field descriptor </param>
      /// <returns> a String array in which:
      ///   cell[1] is the real value of the field
      ///   cell[0] is a null flag ("1" means a null value)
      /// </returns>
      internal String[] getDitto(Field fld)
      {
         var fldVal = new String[2];

         // if the current record is the first in the cache don't bother to get the ditto
         if (_currRecIdx <= 0)
            return null;
         try
         {
            if (_task.getForm() != null)
            {
               MgForm form = (MgForm)_task.getForm();
               int displayLine = form.DisplayLine;
               // switch the current record to the previous record
               setCurrRecByIdx(_currRecIdx - 1, false, false, true, SET_DISPLAYLINE_BY_DV);
               fld.compute(false);
               fldVal[0] = fld.isNull()
                           ? "1"
                           : "0";
               fldVal[1] = fld.getValue(false);
               // restore the current record
               setCurrRecByIdx(_currRecIdx + 1, false, false, true, SET_DISPLAYLINE_BY_DV);
            }
         }
         catch (RecordOutOfDataViewException)
         {
            return null;
         }
         return fldVal;
      }

      /// <summary>
      ///   take the field values from the current record
      /// </summary>
      internal void takeFldValsFromCurrRec()
      {
         ((FieldsTable) _fieldsTab).takeValsFromRec();
      }

      /// <summary>
      ///   compare a selected fields values between the current record and the previous record
      ///   and return true if their values are equal
      /// </summary>
      /// <param name = "fldList">the list of field indexes to compare
      /// </param>
      internal bool currEqualsPrev(int[] fldList)
      {
         if (CurrRec != null && _prevCurrRec != null)
         {
            if (CurrRec.Equals(_prevCurrRec))
               return true;

            for (int i = 0; i < fldList.Length; i++)
            {
               if (!CurrRec.fldValsEqual(_prevCurrRec, fldList[i]))
                  return false;
            }
         }
         else if (!CurrRec.Equals(_prevCurrRec))
            return false;
         return true;
      }

      /// <summary>
      ///   return a reference to the _task of this dataview
      /// </summary>
      internal new Task getTask()
      {
         Debug.Assert(_task is Task);
         return ((Task)_task);
      }

      /// <summary>
      ///   return a reference to the form for _task of this dataview
      /// </summary>
      protected internal MgForm getForm()
      {
         return (MgForm)_task.getForm();
      }

      /// <summary>
      ///   return the number of records in the modified records table
      /// </summary>
      internal int modifiedRecordsNumber()
      {
         return _modifiedRecordsTab.getSize();
      }

      /// <summary>
      ///   return the number of records in the dataview
      /// </summary>
      internal int getSize()
      {
         return _recordsTab.getSize();
      }

      /// <summary>
      ///   returns true if the client knows how to init a new record.
      /// </summary>
      protected internal bool computeByClient()
      {
         return _computeBy == COMPUTE_NEWREC_ON_CLIENT;
      }

      /// <summary>
      ///   is Record exists in the DataView
      /// </summary>
      /// <param name = "id">of needed record </param>
      /// <returns> true if the record exists </returns>
      internal bool recExistsById(int id)
      {
         return (_recordsTab.getRecord(id) != null);
      }


      /// <summary>
      ///   check if there are params in the specified range
      /// </summary>
      /// <param name = "iPos">position of first field</param>
      /// <param name = "iLen">number of fields in the range</param>
      /// <returns> true = there are parameters in the range</returns>
      internal bool ParametersExist(int iPos, int iLen)
      {
         for (int j = iPos; j < iPos + iLen; j++)
         {
            if (((Field) getField(j)).isParam())
               return true;
         }

         return false;
      }

      /// <summary>
      /// check if there are params in Dataview tab
      /// </summary>
      /// <returns></returns>
      internal bool ParametersExist()
      {
         return ParametersExist(_rmIdx, _rmSize);
      }

      /// <summary>
      ///   returns true if a record is in the modified records table
      /// </summary>
      /// <param name = "id">the id of the record</param>
      internal bool recInModifiedTab(int id)
      {
         Record rec = _modifiedRecordsTab.getRecord(id);
         return (rec != null);
      }

      /// <summary>
      ///   saves the current record as the previous current record
      /// </summary>
      internal void setPrevCurrRec()
      {
         _prevCurrRec = CurrRec;
      }

      internal void setPrevCurrRec(Record rec)
      {
         _prevCurrRec = rec;
      }

      internal bool isPrevCurrRecNull()
      {
         return (_prevCurrRec == null);
      }

      /// <summary>
      ///   recompute all virtuals with no init expression which have server recompute defined for them.
      /// </summary>
      /// <param name = "orgRec">contains the virtual's original value </param>
      private void execUnknownRcmps(Record orgRec)
      {
         int i;
         int from = ((FieldsTable) _fieldsTab).getRMIdx();
         int size = ((FieldsTable) _fieldsTab).getRMSize();

         var rec = (Record) getCurrRec();

         // First time - initialize unknownRcmp value
         if (_unknownRcmp == UNKNOWN_RCMPS_NOT_INITED)
         {
            // Do we have a "non regular" recompute?
            for (i = from;
                 i < from + size && _unknownRcmp == UNKNOWN_RCMPS_NOT_INITED;
                 i++)
            {
               var fld = (Field) _fieldsTab.getField(i);
               if (fld.IsVirtual && !fld.hasInitExp() && fld.isServerRcmp())
                  _unknownRcmp = UNKNOWN_RCMPS_FOUND;
            }
         }

         if (_unknownRcmp == UNKNOWN_RCMPS_FOUND && rec != null)
         {
            for (i = from; i < from + size; i++)
            {
               var fld = (Field) _fieldsTab.getField(i);
               if (fld.IsVirtual && !fld.hasInitExp() && fld.isServerRcmp() && !rec.fldValsEqual(orgRec, i))
               {
                  string val = rec.GetFieldValue(i);
                  bool isNull = rec.IsNull(i);
                  rec.setFieldValue(i, orgRec.GetFieldValue(i), false);
                  fld.setValueAndStartRecompute(val, isNull, false, false, false);
               }
            }
         }
      }

      /// <summary>
      ///   set the transCleared flag
      /// </summary>
      internal void setTransCleared()
      {
         _transCleared = true;
      }

      /// <summary>
      ///   perform any data error recovery action
      /// </summary>
      internal void processRecovery()
      {
         if (!_task.isAborting())
         {
            char orgAction = _pendingRecovery;
            bool orgStopExecution = ClientManager.Instance.EventsManager.GetStopExecutionFlag();

            _pendingRecovery = RECOVERY_ACT_NONE;
            if (orgStopExecution)
               ClientManager.Instance.EventsManager.setStopExecution(false);

            bool temporaryResetInCtrlPrefix = ((Task)_task).InCtrlPrefix;
            
            // if the error from which we recover happened while in ctrl prefix, reset it for the action to be executed here,
            // so that entering a new control will work (Defect 122387).
            if (temporaryResetInCtrlPrefix)
               ((Task)_task).InCtrlPrefix = false;

            switch (orgAction)
            {
               case RECOVERY_ACT_CANCEL:
                  ((DataView) _task.DataView).getCurrRec().resetUpdated();
                  // QCR #773979 (updated record cant be canceled)

                  // clear the cache
                  getTask().getTaskCache().clearCache();

                  // qcr #776952. create mode and only 1 rec on dv. The server has ordered
                  // a rollback. Here, we reset original rec and curr rec to insert mode,
                  // otherwise, next time the client will set update mode for the rec instead of insert.
                  if (getSize() == 1 && _task.getMode() == Constants.TASK_MODE_CREATE)
                  {
                     CurrRec.resetModified();
                     CurrRec.clearMode();
                     CurrRec.setMode(DataModificationTypes.Insert);
                     CurrRec.setNewRec();
                     _original.setSameAs(CurrRec, false);
                  }

                  // try and set the last parked ctrl on the _task being cancled. But when calling to getFirstParkableCtrl
                  // do not search into subforms since we cannot have _task A having lastparked ctrl from _task B.
                  if (_task.getLastParkedCtrl() == null)
                  {
                     MgControl FirstParkableCtrl = ((MgForm)_task.getForm()).getFirstParkableCtrl(false);

                     // same reason we sent fasle to getFirstParkableCtrl. Do not set last parked of a different _task.
                     // What the server wants after all is to rollback a specific _task.
                     if (FirstParkableCtrl != null && FirstParkableCtrl.getForm().getTask() == _task)
                        _task.setLastParkedCtrl(((MgForm)_task.getForm()).getFirstParkableCtrl(false));
                  }

                  // The cancel handled here will not caused a rollback since it was raised by rollback. (rollb already done in server).
                  ClientManager.Instance.EventsManager.handleInternalEvent(getTask(), InternalInterface.MG_ACT_CANCEL,
                                                                           EventSubType.CancelWithNoRollback);
                  break;

               case RECOVERY_ACT_MOVE_DIRECTION_BEGIN:
                  ((MgForm)_task.getForm()).moveInRow(null, Constants.MOVE_DIRECTION_BEGIN);
                  break;

               case RECOVERY_ACT_BEGIN_SCREEN:
                  getTask().setPreventRecordSuffix(true);
                  ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_TBL_BEGPAGE);
                  getTask().setPreventRecordSuffix(false);
                  break;

               case RECOVERY_ACT_BEGIN_TABLE:
                  // make sure the action is enabled no matter our location in the table. otherwise, rec prefix will not be executed after rollback.
                  _task.ActionManager.enable(InternalInterface.MG_ACT_TBL_BEGTBL, true);
                  ClientManager.Instance.EventsManager.handleInternalEvent(_task, InternalInterface.MG_ACT_TBL_BEGTBL);

                  // clear the cache
                  getTask().getTaskCache().clearCache();
                  break;

               default:
                  ClientManager.Instance.EventsManager.setStopExecution(orgStopExecution);
                  break;
            }

            if (temporaryResetInCtrlPrefix)
               ((Task)_task).InCtrlPrefix = true;

            if (orgAction != RECOVERY_ACT_NONE)
               ClientManager.Instance.EventsManager.setStopExecution(true, orgAction != RECOVERY_ACT_BEGIN_TABLE);
         }
      }

      /// <summary>
      ///   repleciates the current dataview
      /// </summary>
      internal DataView replicate()
      {
         var rep = (DataView) MemberwiseClone();
         rep._recordsTab = _recordsTab.replicate();
         rep._currRecId = rep._recordsTab.getRecByIdx(0).getId();
         rep._currRecIdx = 0;
         rep.CurrRec = rep._recordsTab.getRecByIdx(0);
         rep._prevCurrRec = rep._recordsTab.getRecByIdx(0);
         rep._original = rep._recordsTab.getRecByIdx(0);
         rep.setTopRecIdx(Int32.MinValue);
         if (_mgTree != null)
             rep._mgTree = ((MgTree)_mgTree).replicate();
         // we must check dcVals if it need to be replicated as well currenty we think not
         return rep;
      }

      /// <summary>
      ///   returns the dvPos value non hashed
      /// </summary>
      internal long getDvPosValue()
      {
         return _dvPosValue;
      }

      /// <summary>
      ///   check is it is the first time the d.v is parsed
      /// </summary>
      internal bool getFirstDv()
      {
         return _firstDv;
      }
      
      /// <summary>
      ///   reset is the first time the d.v is parsed
      /// </summary>
      internal void ResetFirstDv()
      {
         _firstDv = false;
      }
      /// <summary>
      ///   get this dataview cache time stamp
      /// </summary>
      protected internal long getCacheLRU()
      {
         return _cacheLruTimeStamp;
      }

      /// <summary>
      ///   sets the time stamp of the dataview
      ///   used when inserting dataview in cache
      /// </summary>
      protected internal void setCacheLRU()
      {
         _cacheLruTimeStamp = Misc.getSystemMilliseconds();
      }

      /// <summary>
      ///   news to take the values from the given dv and to set them as the current values
      ///   the given dv is usually created via dataview.replicate()
      ///   this method does not take the values of  lastSessionCount and cacheLRUTimeStamp
      ///   from the given dv since they should be propagated from one current dv to the other
      /// </summary>
      internal void setSameAs(DataView newDv)
      {
         _rmSize = newDv._rmSize;
         _rmIdx = newDv._rmIdx;
         _computeBy = newDv._computeBy;
         _flushUpdates = newDv._flushUpdates;
         _currRecId = newDv._currRecId;
         _currRecIdx = newDv._currRecIdx;
         _topRecId = newDv._topRecId;
         CurrRec = newDv.CurrRec;
         _original = newDv._original;
         _prevCurrRec = null;
         _fieldsTab = newDv._fieldsTab;
         _recordsTab = newDv._recordsTab;
         _modifiedRecordsTab = newDv._modifiedRecordsTab;
         _includesFirst = newDv._includesFirst;
         _includesLast = newDv._includesLast;
         _insertAt = newDv._insertAt;
         _lastCreatedRecId = newDv._lastCreatedRecId;
         _task = newDv._task;
         _dcValsCollection = newDv._dcValsCollection;
         _recovery = newDv._recovery;
         _pendingRecovery = newDv._pendingRecovery;
         _hasMainTable = newDv._hasMainTable;
         _chunkSize = newDv._chunkSize;
         _firstDv = newDv._firstDv;
         _lastSessionCounter = newDv._lastSessionCounter;
         _skipParsing = newDv._skipParsing;
         _unknownRcmp = newDv._unknownRcmp;
         _transCleared = newDv._transCleared;
         _dvPosValue = newDv._dvPosValue;
         _cacheLruTimeStamp = newDv._cacheLruTimeStamp;
         _changed = newDv._changed;
         _locateFirstRec = newDv._locateFirstRec;
         _mgTree = newDv._mgTree;
         setEmptyDataview(newDv.isEmptyDataview());
         _task.setMode(newDv.taskModeFromCache);

         // UPDATE THE TABLE DISPLAY
         try
         {
            if (getTask().HasLoacte())
            {
               setCurrRec(_locateFirstRec, true);
               setTopRecIdx(_recordsTab.getRecIdx(_locateFirstRec));
            }
            else
            {
               getTask().setPreventRecordSuffix(true);
               setCurrRecByIdx(Constants.MG_DATAVIEW_FIRST_RECORD, true, false, true, SET_DISPLAYLINE_BY_DV);
               getTask().setPreventRecordSuffix(false);

               setTopRecIdx(0);
            }

            if (!_task.getForm().isScreenMode())
               setCurrRecByIdx(_topRecIdx, true, false, true, SET_DISPLAYLINE_BY_DV);

            if (_task.getForm() != null)
               ((MgForm)(_task.getForm())).updateDisplayLineByDV();
         }
         catch (RecordOutOfDataViewException e)
         {
            Logger.Instance.WriteExceptionToLog("wrong top record idx in DataView.setSameAs" + e.Message);
         }
      }

      internal void setChanged(bool val)
      {
         _changed = val;
      }

      internal bool getChanged()
      {
         return _changed;
      }

      internal int getLocateFirstRec()
      {
         return _locateFirstRec;
      }

      internal void setLocateFirstRec(int newFirst)
      {
         _locateFirstRec = newFirst;
      }

      /// <summary>
      ///   returns a record by its idx
      /// </summary>
      internal Record getRecByIdx(int idx)
      {
         return _recordsTab.getRecByIdx(idx);
      }

      protected internal Record getServerCurrRec()
      {
         return _recordsTab.getServerCurrRec();
      }

      protected internal void zeroServerCurrRec()
      {
         _recordsTab.zeroServerCurrRec();
      }

      protected internal bool inRollback()
      {
         bool inRollback = false;

         if (_recovery == ConstInterface.RECOVERY_ROLLBACK && getTask().isTransactionOwner())
            inRollback = true;

         return inRollback;
      }

      /// <summary>
      /// </summary>
      /// <param name = "id"> </param>
      /// <returns> </returns>
      internal int getRecIdx(int id)
      {
         return _recordsTab.getRecIdx(id);
      }

      internal MgTreeBase getMgTree()
      {
         return _mgTree;
      }

      /// <summary>
      ///   backup current record
      /// </summary>
      internal Record backupCurrent()
      {
         return (CurrRec.replicate());
      }

      /// <summary>
      ///   restore current record from backup
      /// </summary>
      /// <param name = "recBackup">backup of previous current record </param>
      internal void restoreCurrent(Record recBackup)
      {
         Debug.Assert(recBackup is Record);
         _currRecId = ((Record)recBackup).getId();
         _currRecIdx = _recordsTab.getRecIdx(_currRecId);
         CurrRec = _recordsTab.getRecByIdx(_currRecIdx);
         CurrRec.setSameAs((Record)recBackup, false);
         takeFldValsFromCurrRec();
      }

      /// <summary>
      /// calculate chunk size
      /// </summary>
      internal void CalculateChunkSizeExp()
      {
         const int DEFAULT_CHUNK_SIZE = 30;
         const int MAX_CHUNK_SIZE = 999;
         Task task = (Task)_task;

         bool wasEvaluated;

         _chunkSize = 1;
         if (_chunkSizeExpression != 0)
         {

            String val = task.EvaluateExpression(_chunkSizeExpression, StorageAttribute.NUMERIC, 0, false, StorageAttribute.NONE, true, out wasEvaluated);
            val = DisplayConvertor.Instance.mg2disp(val, "", LocalDataviewManager.NUMERIC_PIC, 0, false).Trim();
            _chunkSize = Int32.Parse(val) ;

            if (_chunkSize <= 0)
               _chunkSize = DEFAULT_CHUNK_SIZE;
            else if (_chunkSize > MAX_CHUNK_SIZE)
               _chunkSize = MAX_CHUNK_SIZE;
         }
         else
         {
            if (task.getForm() != null)
               if (task.getForm().isScreenMode() )
                  _chunkSize = 1;
               else
                  _chunkSize = DEFAULT_CHUNK_SIZE;
         }

      }

      /// <summary>
      /// first Dataview Record
      /// </summary>
      internal IRecord FirstRecord
      {
         get
         {
            return _recordsTab.getRecByIdx(0);
         }
      }

      /// <summary>
      /// last dataview record
      /// </summary>
      internal IRecord LastRecord
      {
         get
         {
            return _recordsTab.getRecByIdx(_recordsTab.getSize() - 1);
         }
      }

      internal IRecord GetRecordById(int id)
      {
         return _recordsTab.getRecord(id);
      }


      /// <summary>
      /// returns modified record of the task - null if no record was modified
      /// </summary>
      /// <returns></returns>
      internal Record GetModifiedRecord()
      {
         Record record = null;
         if (_modifiedRecordsTab.getSize() > 0)
         {
            // in modRecordTbl we need to have only 1 record 
            Debug.Assert(_modifiedRecordsTab.getSize() == 1);
            record = _modifiedRecordsTab.getRecByIdx(0);
         }
         return record;
      }

      /// <summary>
      /// return the id of the current record
      /// </summary>
      /// <returns></returns>
      internal int GetCurrentRecId()
      {
         return _currRecId;
      }

      /// <summary>
      /// dataview boundaries are changed
      /// </summary>
      /// <param name="orgIncludesFirst"></param>
      /// <param name="orgIncludesLast"></param>
      /// <returns></returns>
      internal bool DataviewBoundriesAreChanged(bool orgIncludesFirst, bool orgIncludesLast)
      {
         return (orgIncludesFirst != IncludesFirst() || orgIncludesLast != IncludesLast());
      }

      /// <summary>
      /// Returns index of first field in DataView Tab
      /// </summary>
      /// <returns></returns>
      internal int GetRecordMainIdx()
      {
         return _rmIdx;
      }

      /// <summary>
      /// Returns number of fields in DataView Tab
      /// </summary>
      /// <returns></returns>
      internal int GetRecordMainSize()
      {
         return _rmSize;
      }

   }
}
