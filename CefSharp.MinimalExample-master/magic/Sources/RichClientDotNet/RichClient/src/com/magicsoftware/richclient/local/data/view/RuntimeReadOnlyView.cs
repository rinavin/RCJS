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
using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.local.data.view.Boundaries;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;

namespace com.magicsoftware.richclient.local.data.view
{

   /// <summary>
   /// runtime read only view 
   /// </summary>
   internal class RuntimeReadOnlyView : RuntimeViewBase
   {
      /// <summary>
      /// Datasource definition
      /// </summary>
      internal IDataSourceViewDefinition DataSourceViewDefinition { get; set; }

      /// <summary>
      /// cursor builder
      /// </summary>
      internal CursorBuilder CursorBuilder { get; set; }
 
      /// <summary>
      /// runtime cursor
      /// </summary>
      internal RuntimeCursor defaultCursor;

      internal RuntimeCursor CurrentCursor { get ; set; }
      internal IRangeDataBuilder RangeBuilder { get; set; }

      internal override FieldValues OldValues
      {
         get
         {
            return CurrentCursor.RuntimeCursorData.OldValues;
         }
      }

      internal override FieldValues CurrentValues
      {
         get
         {
            return CurrentCursor.RuntimeCursorData.CurrentValues;
         }
      }


      /// <summary>
      /// current position
      /// </summary>
      internal DbPos CurrentPosition
      {
         get
         {
            return CurrentCursor.CursorDefinition.CurrentPosition;
         }
      }
      
      /// <summary>
      /// builder for the cursor
      /// </summary>
      public void BuildCursor()
      {
         CurrentCursor = defaultCursor = CursorBuilder.Build(this);
      }




      /// <summary>
      /// prepare cursor
      /// </summary>
      internal virtual GatewayResult Prepare()
      {
         CurrentCursor.CursorDefinition.Key = DataSourceViewDefinition.DbKey;


         //TODO:Range 
         //tsk_crsr_fill_sql_range (vew_main->main.crsr);
         //if (CTX->ctl_tsk_.tskr->SQLRange_exp != 0 && !AS400File)
         //   tsk_crsr_fill_magic_sql_range(vew_main->main.crsr);

         //TODO
         CurrentCursor.CursorDefinition.StartPosition = new DbPos(true);
         CurrentCursor.CursorDefinition.CurrentPosition = new DbPos(true); ;//set to new position ?

         //create fm PrepareCursor operation
         GatewayCommandPrepare cursorCommand = GatewayCommandsFactory.CreateCursorPrepareCommand(CurrentCursor, LocalDataviewManager.LocalManager);   
         return cursorCommand.Execute();
      }

      /// <summary>
      /// release cursor
      /// </summary>
      internal virtual GatewayResult ReleaseCursor()
      {
         GatewayCommandBase cursorCommand = GatewayCommandsFactory.CreateCursorReleaseCommand(CurrentCursor, LocalDataviewManager.LocalManager);
         return cursorCommand.Execute();
      }

      /// <summary>
      /// open cursor
      /// </summary>
      /// <param name="reverse"></param>
      /// <param name="startPosition"></param>
      /// <param name="useLocate">If this is the 1st access to the table, locate values should be considered. 
      /// if not - use only ranges</param>
      /// <returns></returns>
      internal GatewayResult OpenCursor(bool reverse, DbPos startPosition, BoudariesFlags boundariesFlag)
      {
         CurrentCursor.CursorDefinition.SetFlagValue(CursorProperties.DirReversed, reverse);
         CurrentCursor.CursorDefinition.StartPosition = startPosition;

         // build the RangeData list
         CurrentCursor.RuntimeCursorData.Ranges = RangeBuilder.Build(boundariesFlag);

         GatewayCommandBase cursorCommand = GatewayCommandsFactory.CreateCursorOpenCommand(CurrentCursor, LocalDataviewManager.LocalManager);
         //String r = CurrentCursor.Serialize();

         return cursorCommand.Execute();
      }

      /// <summary>
      /// get the required start position, for use when there's a "locate" command
      /// </summary>
      /// <param name="reverse"></param>
      /// <returns></returns>
      internal DbPos SetCursorOnPosition(bool reverse, DbPos position, BoudariesFlags cursorPositionFlag)
      {
         OpenCursor(reverse, position, cursorPositionFlag);
         CursorFetch();
         CloseCursor();
         return CurrentCursor.CursorDefinition.CurrentPosition;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal GatewayResult CloseCursor()
      {
         GatewayCommandBase cursorCommand = GatewayCommandsFactory.CreateCursorCloseCommand(CurrentCursor, LocalDataviewManager.LocalManager);
         return cursorCommand.Execute();
      }

      /// <summary>
      /// Fetch 
      /// </summary>
      /// <returns></returns>
      internal GatewayResult CursorFetch()
      {
         //vew_fetch_rng
         GatewayCommandBase cursorCommand = GatewayCommandsFactory.CreateCursorFetchCommand(CurrentCursor, LocalDataviewManager.LocalManager);
         return cursorCommand.Execute();
      }

      /// <summary>
      /// get dbField for the index
      /// </summary>
      /// <param name="indexInView"></param>
      /// <returns></returns>
      internal DBField GetDbField(int indexInView)
      {
         return DataSourceViewDefinition.DbFields[indexInView];
      }

      internal FieldValue GetFieldValue(DBField field)
      {
         int fieldIndex = IndexOf(field);
         if (fieldIndex >= 0)
            return CurrentValues.Fields[fieldIndex];
         else
            return null;
      }

      int IndexOf(DBField field)
      {
         int index = 0;
         foreach (var currentField in DataSourceViewDefinition.DbFields)
         {
            if (currentField.Isn == field.Isn)
               return index;
            ++index;
         }
         return -1; // Not found
      }
      
      internal override GatewayResult Fetch(IRecord record)
      {
         GatewayResult result = new GatewayResult();
         result = CursorFetch();
         if (result.Success)
         {
            //TODO: handle records with same position
            CopyValues(record);
            base.Fetch(record);
         }
         return result;
      }


      internal GatewayResult OpenDataSource()
      {
         var dataSourceRef = DataSourceViewDefinition.TaskDataSource;
         string fileName = dataSourceRef.DataSourceDefinition.Name;

         var command = GatewayCommandsFactory.CreateFileOpenCommand(fileName, dataSourceRef.DataSourceDefinition, dataSourceRef.Access, this.LocalDataviewManager.LocalManager);
         var retVal = command.Execute();

         return retVal;
      }

      internal GatewayResult CloseDataSource()
      {
         var dataSourceRef = DataSourceViewDefinition.TaskDataSource;
         string fileName = dataSourceRef.DataSourceDefinition.Name;

         var command = GatewayCommandsFactory.CreateFileCloseCommand(dataSourceRef.DataSourceDefinition, this.LocalDataviewManager.LocalManager);
         var retVal = command.Execute();
         return retVal;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="field"></param>
      /// <param name="indexInRecordView"></param>
      internal override void MapFieldDefinition(IFieldView field, int indexInRecordView)
      {
         base.MapFieldDefinition(field, indexInRecordView);

         //add ranges of the field
         ViewBoundaries.AddFieldBoundaries(field, fieldIndexInViewByIndexInRecord[indexInRecordView]);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="viewDefinition"></param>
      /// <param name="localDataviewManager"></param>
      public void Initialize(IDataSourceViewDefinition viewDefinition, LocalDataviewManager localDataviewManager)
      {
         DataSourceViewDefinition = viewDefinition;
         LocalDataviewManager = localDataviewManager;
         RangeBuilder = new ViewRangeDataBuilder(ViewBoundaries, ViewBoundaries.RuntimeViewBase.LocalDataviewManager);
      }

      internal override GatewayResult FetchCurrent(IRecord record)
      {
         return base.Fetch(record);
      }
   }
}
