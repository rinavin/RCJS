using System;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.local.data.gateways.commands;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using System.Collections.Generic;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;
using com.magicsoftware.richclient.data;


namespace com.magicsoftware.richclient.local.data.view
{
   internal delegate GatewayResult ModificationDelegate(IRecord record);

   /// <summary>
   /// runtime view  
   /// </summary>
   internal class RuntimeRealView : RuntimeReadOnlyView
   {
      /// <summary>
      /// id of the view
      /// </summary>
      internal int Id { get { return ((IDataviewHeader)DataSourceViewDefinition).Id; } }

      /// <summary>
      /// task position cache
      /// </summary>
      internal PositionCache PositionCache { get { return LocalDataviewManager.PositionCache; } }


      /// <summary>
      /// ignore position cache
      /// </summary>
      internal bool IgnorePositionCache { get; set; }

      internal List<bool> IsFieldUpdated
      {
         get
         {
            return CurrentCursor.CursorDefinition.IsFieldUpdated;
         }
      }

      Dictionary<DataModificationTypes, ModificationDelegate> modificationHandlers = new Dictionary<DataModificationTypes,ModificationDelegate>();

      internal RuntimeCursor LocateCursor; // cursor used for locate operations

      public RuntimeRealView()
      {
         modificationHandlers[DataModificationTypes.Insert] = OnInsert;
         modificationHandlers[DataModificationTypes.Update] = OnUpdate;
         modificationHandlers[DataModificationTypes.Delete] = OnDelete;
      }
      /// <summary>
      /// delete cursor
      /// </summary>
      /// <param name="pos"></param>
      /// <returns></returns>
      internal GatewayResult DeleteRecord(IRecord record)
      {
         GatewayResult result  = CheckTransactionValidation();
         if (result.Success)
         {

            DbPos pos = GetPosition(record);

            GatewayCommandBase cursorCommand = new GatewayCommandCursorDeleteRecord();
            cursorCommand.RuntimeCursor = CurrentCursor;
            CurrentCursor.CursorDefinition.CurrentPosition = pos;
            cursorCommand.LocalManager = LocalDataviewManager.LocalManager;
            result = cursorCommand.Execute();
            if (result.Success)
               ClearRecord(record);
         }
         return result;
      }

      /// <summary>
      /// clear record from position cache
      /// </summary>
      /// <param name="record"></param>
      internal void ClearRecord(IRecord record)
      {
         if (!IgnorePositionCache)
         {
            PositionId positionId = GetPositionId(record);
            PositionCache.Remove(positionId);
         }
      }

      /// <summary>
      /// check the transaction validation, if there is no transaction opend and use updated
      /// filed from the data source ,a GatewayResult error will be return 
      /// </summary>
      /// <returns></returns>
      private GatewayResult CheckTransactionValidation()
      {
         GatewayResult gatewayResult = new GatewayResult();
         if (!TaskTransactionManager.IsLocalTransactionOpned)
         {            
            gatewayResult.ErrorCode = GatewayErrorCode.ModifyWithinTransaction;
            gatewayResult.ErrorParams = new string[] { DataSourceViewDefinition.TaskDataSource.DataSourceDefinition.Name, ""};
         }
         return gatewayResult;
      }


      /// <summary>
      /// insert cursor
      /// </summary>
      /// <param name="pos"></param>
      /// <returns></returns>
      internal GatewayResult InsertRecord(IRecord record)
      {
         GatewayResult result = CheckTransactionValidation();
         if (result.Success)
         {
            UpdateRuntimeCursor(record, true);

            GatewayCommandCursorInsertRecord cursorCommand = GatewayCommandsFactory.CreateCursorInsertCommand(CurrentCursor, LocalDataviewManager.LocalManager);
            result = cursorCommand.Execute();
            if (result.Success && !IgnorePositionCache)
               PositionCache.Set(GetPositionId(record), CurrentPosition);
         }
         return result;
      }

      /// <summary>
      /// update record
      /// </summary>
      /// <param name="pos"></param>
      /// <returns></returns>
      internal virtual GatewayResult UpdateRecord(IRecord record)
      {
         GatewayResult gatewayResult = new GatewayResult();

         if (UpdateRuntimeCursor(record, false))
         {
             gatewayResult = CheckTransactionValidation();
             if (gatewayResult.Success)
             {

                GatewayCommandCursorUpdateRecord cursorCommand = new GatewayCommandCursorUpdateRecord();
                cursorCommand.RuntimeCursor = CurrentCursor;

                // set the current position to be on the update record.         
                CurrentCursor.CursorDefinition.CurrentPosition = GetPosition(record);

                // execute the command
                cursorCommand.LocalManager = LocalDataviewManager.LocalManager;
                gatewayResult = cursorCommand.Execute();
             }
         }

         return gatewayResult;
      }

      internal override GatewayResult Fetch(IRecord record)
      {
         GatewayResult result = base.Fetch(record);
         if (result.Success && !IgnorePositionCache)
            PositionCache.Set(GetPositionId(record), CurrentPosition);

         return result;
      }


      /// <summary>
      /// fm_flds_mg_2_db
      /// Converts a record from magic presentation to gateway 
      /// </summary>
      protected bool UpdateRuntimeCursor(IRecord record, bool fourceUpdated)
      {
         bool updated = false;
         for (int i = 0; i < CurrentValues.Count; i++)
         {

            int indexInRecord = fieldIndexInRecordByIndexInView[i];

            IsFieldUpdated[i] = false;
            if (fourceUpdated || record.IsFldModifiedAtLeastOnce(indexInRecord))
               updated = IsFieldUpdated[i] = true;

            CurrentValues[i].IsNull = record.IsNull(indexInRecord);
            CurrentValues[i].Value = CurrentValues[i].IsNull ? null : record.GetFieldValue(indexInRecord);
         }
         return updated;
      }

      /// <summary>
      /// get position id
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected PositionId GetPositionId(IRecord record)
      {
         PositionId positionId = new PositionId(record.getId(), ((IDataviewHeader)DataSourceViewDefinition).Id);
         return positionId;
      }


      /// <summary>
      /// get position
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      internal DbPos GetPosition(IRecord record)
      {
         DbPos pos = null;
         PositionId positionId = GetPositionId(record);
         PositionCache.TryGetValue(positionId, out pos);
         return pos;
      }


      /// <summary>
      /// get current  position
      /// </summary>
      /// <returns></returns>
      internal DbPos GetCurrentPosition()
      {
         DbPos pos = null;
         IRecord currRec = ((DataView)LocalDataviewManager.Task.DataView).getCurrRec();
         PositionId positionId = GetPositionId(currRec);
         PositionCache.TryGetValue(positionId, out pos);
         return pos;
      }

      /// <summary>
      /// override - build also the locate cursor
      /// </summary>
      public void BuildLocateCursor()
      {
         Order locateDirection = Order.Ascending;
         // get the locate order value
         if (LocalDataviewManager.Task.checkIfExistProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE_ORDER))
            locateDirection = (Order)LocalDataviewManager.Task.getProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE_ORDER).getValue()[0];

         LocateCursor = CursorBuilder.Build(this, locateDirection);
      }

      /// <summary>
      /// prepare cursor
      /// </summary>
      internal override GatewayResult Prepare()
      {
         GatewayResult gatewayResult = base.Prepare();

         if (gatewayResult.Success && LocateCursor != null)
         {
            // prepare the locate cursor
            CurrentCursor = LocateCursor;
            gatewayResult = base.Prepare();
            CurrentCursor = defaultCursor;
         }

         return gatewayResult;
      }

      /// <summary>
      /// release cursor
      /// </summary>
      internal override GatewayResult ReleaseCursor()
      {
         GatewayResult gatewayResult = base.ReleaseCursor();

         if (gatewayResult.Success && LocateCursor != null)
         {
            // prepare the locate cursor
            CurrentCursor = LocateCursor;
            gatewayResult = base.ReleaseCursor();
            CurrentCursor = defaultCursor;
         }

         return gatewayResult;
      }

      /// <summary>
      /// handle insert operation
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected virtual GatewayResult OnInsert(IRecord record)
      {
         return InsertRecord(record);
      }

      /// <summary>
      /// handle delete operation
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected virtual GatewayResult OnDelete(IRecord record)
      {
         return DeleteRecord(record);
      }


      /// <summary>
      /// handle update operation
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      protected virtual GatewayResult OnUpdate(IRecord record)
      {
         return UpdateRecord(record);
      }

      /// <summary>
      /// apply modifications
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      internal GatewayResult ApplyModifications(IRecord record)
      {
         return modificationHandlers[(DataModificationTypes)record.getMode()](record);
      }

      /// <summary>
      /// apply runtime sort on Main View.
      /// </summary>
      /// <param name="sortCollection"></param>
      /// <returns>bool</returns>
      public bool ApplySort(SortCollection sortCollection)
      {
         bool sortKeySet = false;
         if (sortCollection != null && sortCollection.getSize() > 0)
         {
            // Build SortKey
            SortKeyBuilder sortKeyBuilder = new SortKeyBuilder(this, sortCollection);
            DBKey sortKey = sortKeyBuilder.Build();

            //Set it on LocalDataviewHeader of main source
            if (sortKey != null)
            {
               //Get the dataviewHeader of Main Source.
               ((LocalDataviewHeader)this.DataSourceViewDefinition).SortKey = sortKey;
               sortKeySet = true;
            }
         }
         else
            ((LocalDataviewHeader)this.DataSourceViewDefinition).SortKey = null;

         return sortKeySet;
      }

      /// <summary>
      /// Apply user ranges and locates on Main View
      /// </summary>
      public void ApplyUserRangesAndLocates()
      {
         ((ViewRangeDataBuilder)RangeBuilder).userRanges = LocalDataviewManager.UserGatewayRanges;
         ((ViewRangeDataBuilder)RangeBuilder).userLocates = LocalDataviewManager.UserGatewayLocates;

         if (LocalDataviewManager.UserGatewayLocates != null && LocalDataviewManager.UserGatewayLocates.Count > 0)
            LocalDataviewManager.TaskViews.UseUserLocates = true;
      }
      
      /// <summary>
      /// fetch the current record from the gateway
      /// </summary>
      /// <param name="position"></param>
      /// <returns></returns>
      internal GatewayResult CursorGetCurrent(DbPos position)
      {
         GatewayCommandBase cursorCommand = new GatewayCommandCursorGetCurrent();
         CurrentCursor.CursorDefinition.CurrentPosition = position;
         cursorCommand.RuntimeCursor = CurrentCursor;
         cursorCommand.LocalManager = LocalDataviewManager.LocalManager;
         return cursorCommand.Execute();
      }

      /// <summary>
      /// fetch the current record
      /// </summary>
      /// <param name="record"></param>
      /// <param name="position"></param>
      /// <returns></returns>
      internal override GatewayResult FetchCurrent(IRecord record)
      {
         GatewayResult result = new GatewayResult();
         result.ErrorCode = GatewayErrorCode.LostRecord;

         DbPos position = GetPosition(record);
         if (position != null)
         {
            result = CursorGetCurrent(position);
            if (result.Success)
            {
               CopyValues(record);
               base.FetchCurrent(record);
            }
         }
         return result;
      }
   }
}
