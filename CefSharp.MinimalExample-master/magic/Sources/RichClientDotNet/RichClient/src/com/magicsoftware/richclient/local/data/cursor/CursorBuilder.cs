using System.Collections.Generic;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.util;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.gatewaytypes.data;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.local.data.cursor
{
   /// <summary>
   /// builds cursor
   /// </summary>
   internal class CursorBuilder
   {
      IDataSourceViewDefinition DataSourceViewDefinition { get {return view.DataSourceViewDefinition;}}
      RuntimeReadOnlyView view;

      public CursorBuilder(RuntimeReadOnlyView view)
      {
         this.view = view;
      }

      /// <summary>
      /// Gets the cursor's data source.
      /// </summary>
      protected DataSourceReference DataSourceReference
      {
         get
         {
            return DataSourceViewDefinition.TaskDataSource;
         }
      }
      static IdGenerator idGenerator = new IdGenerator();

      /// <summary>
      /// build cursor using CursorDefinition
      /// </summary>
      /// <param name="cursorDefinition"></param>
      /// <returns></returns>
      private RuntimeCursor Build(CursorDefinition cursorDefinition)
      {
         RuntimeCursor runtimeCursor = new RuntimeCursor();
         runtimeCursor.ID = idGenerator.GenerateId();
         runtimeCursor.CursorDefinition = cursorDefinition;
         runtimeCursor.RuntimeCursorData = BuildRuntimeCursorData(runtimeCursor.CursorDefinition);
         return runtimeCursor;
      }

      /// <summary>
      /// build cursor
      /// </summary>
      /// <param name="link"></param>
      /// <returns></returns>
      internal RuntimeCursor Build(RuntimeReadOnlyView view)
      {
         CursorDefinition cursorDefinition = BuildCursorDefinition();
         return Build(cursorDefinition);
      }

      /// <summary>
      /// build cursor using DataSourceDefinition
      /// </summary>
      /// <param name="dataSourceDefinition"></param>
      /// <returns></returns>
      internal RuntimeCursor Build(DataSourceDefinition dataSourceDefinition, Access access)
      {
         CursorDefinition cursorDefinition = BuildCursorDefinition(dataSourceDefinition, null, Order.Ascending, false, dataSourceDefinition.Fields, access);
         return Build(cursorDefinition);
      }

      /// <summary>
      /// <summary>
      /// build cursor for locate operations - override the defined range direction
      /// </summary>
      /// <param name="view"></param>
      /// <param name="direction"></param>
      /// <returns></returns>
      internal RuntimeCursor Build(RuntimeReadOnlyView view, Order direction)
      {
         RuntimeCursor runtimeCursor = Build(view);
         runtimeCursor.CursorDefinition.Direction = direction;
         return runtimeCursor;
      }

      /// <summary>
      /// build runtime cursor data
      /// </summary>
      /// <param name="cursorDefinition"></param>
      /// <returns></returns>
      private RuntimeCursorData BuildRuntimeCursorData(CursorDefinition cursorDefinition)
      {
         RuntimeCursorData cursorData = new RuntimeCursorData();
         cursorData.CurrentValues = new FieldValues();
         cursorData.OldValues = new FieldValues();

         for (int i = 0; i < cursorDefinition.FieldsDefinition.Count; i++)
         {
            //TODO: set storage type
            cursorData.CurrentValues.Add(new FieldValue());
            cursorData.OldValues.Add(new FieldValue());
         }
         //TODO: locates

         return cursorData;
         
      }

      /// <summary>
      /// Build cursor definition.
      /// </summary>
      /// <param name="link"></param>
      /// <returns></returns>
      internal CursorDefinition BuildCursorDefinition()
      {
         CursorDefinition cursorDefinition = BuildCursorDefinition(DataSourceReference.DataSourceDefinition,
                                                                   DataSourceViewDefinition.DbKey, DataSourceViewDefinition.RecordsOrder, true, BuildFields(DataSourceViewDefinition.DbKey), 
                                                                   DataSourceViewDefinition.TaskDataSource.Access);
         return cursorDefinition;
      }

      /// <summary>
      /// Build cursor definition.
      /// </summary>
      /// <param name="link"></param>
      /// <returns></returns>
      internal CursorDefinition BuildCursorDefinition(DataSourceDefinition dataSourceDefinition, DBKey dbKey, Order order, bool keyCheckNeeded, List<DBField>dbFields, Access access)
      {
         CursorDefinition cursorDefinition = new CursorDefinition();
         cursorDefinition.DataSourceDefinition = dataSourceDefinition;
         cursorDefinition.Key = dbKey;
         cursorDefinition.FieldsDefinition = dbFields;
         cursorDefinition.IsFieldUpdated = new List<bool>();
         cursorDefinition.DifferentialUpdate = new List<bool>();

         for (int i = 0; i < cursorDefinition.FieldsDefinition.Count; i++)
         {
            //rt_prpr_fldlist_in_vew - different from C++ but it seems more logical
             cursorDefinition.IsFieldUpdated.Add(false);
             cursorDefinition.DifferentialUpdate.Add(false);
         }
         cursorDefinition.Direction = order;
         cursorDefinition.CursorMode = CursorMode.Online;
         //check for non interactive
         // : CursorMode.Batch;
         if (keyCheckNeeded)
            cursorDefinition.SetFlag(CursorProperties.KeyCheck);
         //TODO:batch tsk_crsr_prpr
         cursorDefinition.SetFlag(CursorProperties.StartPos);
         //TODO:locate tsk_crsr_prpr
         //crsr->dir = CTX->ctl_tsk_.tskr->range_dir;

         //TODO:
         //if CTX->ctl_tsk_.tsk_rt->TransMode != TRANS_MODE_NONE
         if (access == Access.Write) 
         {
            CalculateInsertFlag(cursorDefinition);
            ClaculateDeleteFlag(cursorDefinition);
            cursorDefinition.SetFlag(CursorProperties.Update);
         }

          return cursorDefinition;
      }

      /// <summary>
      /// calculate insert flag
      /// </summary>
      /// <param name="cursorDefinition"></param>
      protected virtual void CalculateInsertFlag(CursorDefinition cursorDefinition)
      {
         if (DataSourceViewDefinition.CanInsert)
            cursorDefinition.SetFlag(CursorProperties.Insert);
         else
            cursorDefinition.ClearFlag(CursorProperties.Insert);

      }

      /// <summary>
      /// calculate delete flag
      /// </summary>
      /// <param name="cursorDefinition"></param>
      protected virtual void ClaculateDeleteFlag(CursorDefinition cursorDefinition)
      {
         cursorDefinition.ClearFlag(CursorProperties.Delete);         
      }

      protected virtual void CalculateLinkFlag(CursorDefinition cursorDefinition)
      {
         cursorDefinition.SetFlag(CursorProperties.CursorLink);
      }

      private List<DBField> BuildFields(DBKey key)
      {
         List<DBField> fields = new List<DBField>(DataSourceViewDefinition.DbFields);
         //check that all the fields of the key are in the field's list

         //if (key != null)
         //   foreach (var item in key.Segments)
         //   {
         //      if (!fields.Contains(item.Field))
         //         fields.Add(item.Field);
         //   }
         return fields;
      }

      
   }

}
