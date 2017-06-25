using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.rt;
using System.Diagnostics;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.local.data.gateways;

namespace com.magicsoftware.richclient.local.data.view
{
   enum InsertMode { Before = 'B', After = 'A' };

   /// <summary>
   /// this class is responsible for updating Dataview
   /// </summary>
   class DataviewSynchronizer
   {
      /// <summary>
      /// local dataview manager
      /// </summary>
      internal LocalDataviewManager DataviewManager { get; set; }

      /// <summary>
      /// Task
      /// </summary>
      protected Task Task { get { return DataviewManager.Task; } }

      /// <summary>
      /// View Composite
      /// </summary>
      protected TaskViewsCollection TaskViews { get { return DataviewManager.TaskViews; } }

      /// <summary>
      /// position Cache
      /// </summary>
      protected PositionCache PositionCache { get { return DataviewManager.PositionCache; } }

      DataView Dataview { get { return (DataView)Task.DataView; } }

      MgForm Form { get { return Task.getForm() as MgForm; } }

      /// <summary>
      /// remove record
      /// </summary>
      /// <param name="useFirstRecord"></param>
      /// <returns></returns>
      internal IRecord GetRecord(bool useFirstRecord)
      {
         IRecord record;

         if (useFirstRecord)
         {
            record = Dataview.getRecByIdx(0);
            DataviewManager.ClientIdGenerator.GenerateId();
         }
         else
         {
            record = CreateRecord();
            InsertRecord(record);
         }
         return record;
      }

      internal void RemoveRecord(IRecord record)
      {
         Record currRec = ((Record)record).getPrevRec();
         if (currRec == null)
            currRec = ((Record)record).getNextRec();

         if (currRec != null)
            SetCurrentRecord(currRec.getId());
         else
            Dataview.reset();
         Dataview.RemoveRecord((Record)record);
         TaskViews.ClearRecord(record);
         UpdateTableChildrenArraysSize();

      }

      /// <summary>
      /// update arrays of table children controls
      /// </summary>
      private void UpdateTableChildrenArraysSize()
      {
         if (Form != null)
            Form.UpdateTableChildrenArraysSize(Dataview.getSize());

      }

      internal IRecord CreateRecord()
      {
         int id = DataviewManager.ClientIdGenerator.GenerateId();
         Record record = new Record(id, Dataview, true);
         record.setId(id);
         return record;
      }

      internal void SetupDataview(bool reverse)
      {
         //do invalidate if needed
         //backup current record or copy values of virtuals

         Dataview.InsertAt = reverse ? (char)InsertMode.Before : (char)InsertMode.After;
         Dataview.setEmptyDataview(false);

      }

      internal void InsertRecord(IRecord record)
      {
         Dataview.InsertSingleRecord((Record)record);
         UpdateTableChildrenArraysSize();
      }

      /// <summary>
      /// update dataview after fetch of the record
      /// </summary>
      /// <param name="curRecId"></param>
      internal ReturnResultBase UpdateAfterFetch(int curRecId)
      {
          
         UpdateIncludes();

         if (Dataview.getSize() == 0)
         {
            if (Task.getMode() == Constants.TASK_MODE_CREATE)
               Dataview.addRecord(false, false);
            else
               Dataview.addFirstRecord();
         }

         if (Form != null)
            Form.SetTableItemsCount(false);

         SetCurrentRecord(curRecId);
         ReturnResultBase result = UpdateEmptyDataview();
         ComputeRecord();
         Task.SetRefreshType(Constants.TASK_REFRESH_TABLE);
         return result;
      }

      /// <summary>
      /// update top index on the form
      /// </summary>
      internal void UpdateFormTopIndex()
      {
         if (Form != null)
            Form.SetTableTopIndex();
      }

      /// <summary>
      /// compute record if it is new
      /// </summary>
      private void ComputeRecord()
      {
         Record record = Dataview.getCurrRec();
         if (record != null && record.getMode() == DataModificationTypes.Insert)
         {
            record.setComputed(false);
            Dataview.currRecCompute(false);
         }
         else if (Task.DataviewManager.HasLocalData)
            Dataview.currRecCompute(false);
      }



      /// <summary>
      /// update include first/include last according to the Position cache
      /// </summary>
      internal void UpdateIncludes()
      {
         Dataview.SetIncludesFirst(PositionCache.IncludesFirst);
         Dataview.SetIncludesLast(PositionCache.IncludesLast);
      }


      /// <summary>
      /// reset is the first time the d.v is load
      /// </summary>
      internal void ResetFirstDv()
      {
         Dataview.ResetFirstDv();
      }
      /// <summary>
      //  make sure that a new record created by the server is in 'insert' mode
      //  and mark it as a 'computed' record
      //  the same we doing for RecordsTable.cs method fillData()
      /// </summary>
      internal void SetInsertModeToCurrentRecord()
      {
         Debug.Assert(Task.getMode() == Constants.TASK_MODE_CREATE);

         Record record = Dataview.getCurrRec();
         record.setMode(DataModificationTypes.Insert);
         Dataview.currRecCompute(false);
      }

      internal ReturnResultBase UpdateEmptyDataview()
      {
          ReturnResultBase result = new ReturnResult();
         if (PositionCache.Count == 0 && ((DataView)Task.DataView).HasMainTable)
         {

            Dataview.MoveToEmptyDataviewIfAllowed();

            if (!Dataview.isEmptyDataview())
            {
                if (Task.checkProp(PropInterface.PROP_TYPE_ALLOW_CREATE, true))
                {
                    // change to create mode
                    if (Task.getMode() != Constants.TASK_MODE_CREATE)
                    {
                        Task.setOriginalTaskMode(Constants.TASK_MODE_CREATE);
                        Task.setMode(Constants.TASK_MODE_CREATE);
                        Dataview.getCurrRec().setMode(DataModificationTypes.Insert);
                        Dataview.getCurrRec().setNewRec();
                        //ComputeRecord();
                    }

                    // MG_ACT_CRELINE may be disabled .i.e. PROP_TYPE_ALLOW_CREATE expression can be changed
                    (Task).enableCreateActs(true);
                }
                // exit from the ownerTask
                else
                {
                    result = new ReturnResult(MsgInterface.RT_STR_NO_RECS_IN_RNG);
                    //ClientManager.Instance.EventsManager.handleInternalEvent(Task, InternalInterface.MG_ACT_EXIT);
                }
            }

         }
         else
            Dataview.setEmptyDataview(false);
         return result;
      }


      internal void UpdateTopIndex(int topIndex)
      {
         Dataview.setTopRecIdx(topIndex);
      }

      /// <summary>
      /// invalidate dataview
      /// </summary>
      internal void Invalidate()
      {
         Dataview.clear();
         DataviewManager.Reset();
      }

      /// <summary>
      /// invalidate lines on the form
      /// </summary>
      internal void InvalidateView()
      {
         if (Form != null)
            Form.SetTableItemsCount(0, true);
      }

      /// <summary>
      /// set current record to the specified record id
      /// </summary>
      /// <param name="id"></param>
      internal void SetCurrentRecord(int id)
      {
         int idx = Dataview.getRecIdx(id);
         if (idx != -1)
            SetCurrentRecordByIdx(idx);
         else
         {
            if (Dataview.getSize() > 0)
               SetCurrentRecordByIdx(0);
         }
      }

      /// <summary>
      /// return the current record
      /// </summary>
      /// <returns></returns>
      internal IRecord GetCurrentRecord()
      {
         return Dataview.getCurrRec();
      }

      /// <summary>
      /// set current record to the specified record index
      /// </summary>
      /// <param name="idx"></param>
      internal void SetCurrentRecordByIdx(int idx)
      {
         if (idx >= 0)
            Dataview.setCurrRecByIdx(idx, false, true, false, DataView.SET_DISPLAYLINE_BY_DV);
      }
      /// <summary>
      /// set link's return value
      /// </summary>
      /// <param name="dataviewHeader"></param>
      /// <param name="record"></param>
      /// <param name="linkSuccess"></param>
      internal void SetLinkReturnValue(IDataviewHeader dataviewHeader, IRecord record, bool linkSuccess, bool recompute)
      {
         ((LocalDataviewHeader)dataviewHeader).SetReturnValue(record, linkSuccess, recompute);
      }

      /// <summary>
      /// init link fields
      /// </summary>
      /// <param name="dataviewHeader"></param>
      /// <param name="record"></param>
      internal void InitLinkFields(IDataviewHeader dataviewHeader, IRecord record)
      {
         ((LocalDataviewHeader)dataviewHeader).InitLinkFields(record);
      }

      /// <summary>
      /// set record as computed
      /// </summary>
      /// <param name="record"></param>
      internal void SetComputed(IRecord record)
      {
         ((Record)record).setComputed(true);
      }

      /// <summary>
      /// get modified record
      /// </summary>
      /// <returns></returns>
      internal Record GetModifiedRecord()
      {
         return Dataview.GetModifiedRecord();
      }

      /// <summary>
      /// update after modifications
      /// </summary>
      /// <param name="success"></param>
      internal void UpdateDataviewAfterModification(bool success)
      {
         IRecordsTable modRecordTbl = Dataview.ModifiedRecordsTab;
         Record record = this.GetModifiedRecord();
         
         if (success)
         {
            record.clearMode();
            record.clearFlagsHistory();
            record.clearFlagsForRealOnly(Record.FLAG_MODIFIED_ATLEAST_ONCE);
            Task.setTransactionFailed(false);
         }
         // remove all records   
         modRecordTbl.RemoveAll();

         // if not success then call RecoveryRetryToLocalTask
         if (!success)
            Dataview.HandleLocalDataError();
      }


      /// <summary>
      /// prepare for modifications
      /// </summary>
      internal void PrepareForModification()
      {
         // reset the StopExecution 
         ClientManager.Instance.EventsManager.setStopExecution(false);
         // reset setTransactionFailed
         Task.setTransactionFailed(false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal IRecord GetFirstRecord()
      {
         return Dataview.getRecByIdx(0);
      }

      /// <summary>
      /// update Task member InLocalDataviewCommand
      /// </summary>
      public bool InLocalDataviewCommand
      {
         get
         {
            return Dataview.InLocalDataviewCommand;
         }
         set
         {
            Dataview.InLocalDataviewCommand = value;
         }
      }

      /// <summary>
      /// update db view size
      /// </summary>
      internal void UpdateDBViewSize()
      {
         Dataview.DBViewSize = Dataview.getSize();
      }

      /// <summary>
      /// Adds the dcValues object to the data view's DC values collection.
      /// </summary>
      /// <param name="dcValues">Object to be added to the data view.</param>
      internal void AddDcValues(DcValues dcValues, DataControlRangeDataCollection dcValuesRange)
      {
         Dataview.AddDcValues(dcValues);
         DataviewManager.MapDcValues(dcValuesRange, dcValues.getId());
      }

      /// <summary>
      /// Gets the DC values object, whose identifier is 'id', from the data view.
      /// </summary>
      /// <param name="id">The identifier of the DC values object.</param>
      /// <returns>Returns the DC values, whose identifier is 'id', if found. 
      /// If 'id' is not found - the method returns null</returns>
      internal DcValues GetDcValues(DataControlRangeDataCollection dcValuesRange)
      {
         int id;
         if (DataviewManager.TryGetDcValuesIsByRange(dcValuesRange, out id))
            return Dataview.getDcValues(id);
         else
            return null;
      }

      /// <summary>
      /// Binds a DC values object to the record and the control.
      /// </summary>
      /// <param name="dcValues">The bound DC values object.</param>
      /// <param name="record">The record holding the DC values references.</param>
      /// <param name="controlId">The identifier of the control that should be bound to the DC values object.</param>
      internal void ApplyDcValues(DcValues dcValues, IRecord record, int controlId)
      {
         record.AddDcValuesReference(controlId, dcValues.getId());
         Task.SetDataControlValuesReference(controlId, dcValues.getId());
      }

      /// <summary>
      /// Add new dcValues , set dcValues reference to control and refresh the control.
      /// </summary>
      /// <param name="dcValues"></param>
      /// <param name="rangeData"></param>
      /// <param name="control"></param>
      internal void ApplyDCValuesAndRefreshControl(DcValues dcValues, DataControlRangeDataCollection rangeData, MgControlBase control)
      {
         //add new DC values collection 
         DataviewManager.DataviewSynchronizer.AddDcValues(dcValues, rangeData);

         //set dc values reference
         Task.SetDataControlValuesReference(control.getDitIdx(), dcValues.getId());

         //Update DCValRef of every record after fetching the dataControl values.
         for (int i = 0; i < ((DataView)Task.DataView).getSize(); i++)
            ((DataView)Task.DataView).getRecByIdx(i).AddDcValuesReference(control.getDitIdx(), dcValues.getId());

         //Update DCValRef of original record.
         ((DataView)Task.DataView).getOriginalRec().AddDcValuesReference(control.getDitIdx(), dcValues.getId());

         control.RefreshDisplay();
      }
   }
}
