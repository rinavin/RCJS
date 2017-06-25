using System.Collections.Generic;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.local.data.view.RecordCompute;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using System.Diagnostics;
using com.magicsoftware.richclient.local.data.gateways.commands;
using util.com.magicsoftware.util;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks.sort;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// views of the Task
   /// </summary>
   /// 
   internal class TaskViewsCollection
   {

      /// <summary>
      /// fields in the view
      /// </summary>
      internal List<IFieldView> Fields { get; private set; }

      /// <summary>
      /// view of main source
      /// </summary>
      internal RuntimeRealView ViewMain { get; private set; }

      /// <summary>
      /// all links (NOT including main program)
      /// </summary>
      internal Dictionary<int, LinkView> LinkViews { get; private set; }

      /// <summary>
      /// view of virtuals
      /// </summary>
      internal VirtualsView VirtualView { get; private set; }

      /// <summary>
      /// view of remote variables
      /// </summary>
      internal RemoteView RemoteRuntimeView { get; private set; }

      internal Task Task { get; set; }

      internal List<RuntimeViewBase> RecordViews { get; private set; }

      //internal List<RuntimeView> JoinView { get; private set;}
      //internal RuntimeView SortView;
      internal List<RuntimeReadOnlyView> DataControlViews { get; private set; }

      internal RecordComputer RecordComputer { get; private set; }

      internal RecordComputer LocateExpressionRecordComputer { get; private set; }

      /// <summary>
      /// Should a "locate" instruction be used with this task.
      /// On non-interactive tasks, the locate instructions are ignored – the task goes over all records, and does not locate a specific one.
      /// Locate on a link has a different function – it practically defines a range.
      /// </summary>
      internal bool UseTaskLocate
      {
         get
         {
            return ((ViewMain != null && ViewMain.HasLocate) ||
               (VirtualView != null && VirtualView.HasLocate) ||
               Task.checkIfExistProp(PropInterface.PROP_TYPE_TASK_PROPERTIES_LOCATE) ||
               Task.UseTaskLocateDirection) &&
               Task.IsInteractive;
         }
      }

      /// <summary>
      /// If user locates are present then this flag indicates whether to use user locates or not. 
      /// This flag is set in view refersh command and reset after calculating start poition according to locates.
      /// </summary>
      internal bool UseUserLocates { get; set; }

      internal DbPos CurrentPosition
      {
         get
         {
            if (ViewMain != null)
               return ViewMain.CurrentPosition;
            return new DbPos(true);
         }
      }

      internal bool HasMainTable { get { return ViewMain != null; } }



      public TaskViewsCollection()
      {
         Fields = new List<IFieldView>();
         RecordViews = new List<RuntimeViewBase>();
         LinkViews = new Dictionary<int, LinkView>();
         DataControlViews = new List<RuntimeReadOnlyView>();

      }



      /// <summary>
      /// map all fields to the views they belong to
      /// </summary>
      internal void MapRecordFields()
      {

         for (int i = 0; i < Fields.Count; i++)
         {
            IFieldView field = Fields[i];
            RuntimeViewBase view = GetView(field);
            view.MapFieldDefinition(field, i);
         }
      }



      /// <summary>
      /// return view that own this field
      /// </summary>
      /// <param name="field"></param>
      /// <returns></returns>
      RuntimeViewBase GetView(IFieldView field)
      {
         RuntimeViewBase view = null;
         if (field.IsVirtual)
            view = VirtualView;
         else
         {
            int linkId = field.DataviewHeaderId;
            if (ViewMain != null && ((IDataviewHeader)ViewMain.DataSourceViewDefinition).Id == linkId)
               view = ViewMain;
            else if (LinkViews.ContainsKey(linkId))
               view = LinkViews[linkId];
            else
               view = RemoteRuntimeView;
         }
         return view;
      }

      /// <summary>
      /// prepare cursors
      /// If any prepare action fails, the method will stop executing and return the error
      /// </summary>
      internal GatewayResult Prepare()
      {
         GatewayResult gatewayResult;

         ApplyUserRangesAndLocates();

         foreach (var view in RecordViews)
         {
            if (view is RuntimeReadOnlyView)
            {
               gatewayResult = ((RuntimeReadOnlyView)view).Prepare();

               if (!gatewayResult.Success)
                  return gatewayResult;
            }
         }

         foreach (var view in DataControlViews)
         {
            gatewayResult = view.Prepare();

            if (!gatewayResult.Success)
               return gatewayResult;
         }

         return new GatewayResult();
      }

      /// <summary>
      /// release cursors
      /// </summary>
      internal void ReleaseCursors()
      {
         foreach (var view in RecordViews)
         {
            if (view is RuntimeReadOnlyView)
               ((RuntimeReadOnlyView)view).ReleaseCursor();
         }

         foreach (var view in DataControlViews)
         {
            view.ReleaseCursor();
         }
      }

      /// <summary>
      /// Close tables
      /// </summary>
      internal void CloseTables()
      {
         foreach (var view in DataControlViews)
         {
            view.CloseDataSource();
         }
      }

      /// <summary>
      /// open cursors
      /// </summary>
      /// <param name="reverse"> direction reverse</param>
      /// <param name="startPosition"> start position</param>
      /// <returns></returns>

      internal GatewayResult OpenCursors(bool reverse, DbPos startPosition)
      {
         ///vew_crsr_open
         ///

         GatewayResult result = new GatewayResult();
         if (ViewMain != null)
         {
            SortCollection userSortCollection = GetPrimaryView().LocalDataviewManager.UserSorts;

            //user sorts may have added in task prefix. So, apply them and use the sort key.
            if (userSortCollection != null && userSortCollection.getSize() > 0)
            {
               ApplySort(userSortCollection);
               ViewMain.CurrentCursor.CursorDefinition.Key = ViewMain.DataSourceViewDefinition.DbKey;
            }

            result = ViewMain.OpenCursor(reverse, startPosition, BoudariesFlags.Range);

         }

         return result;
      }

      /// <summary>
      /// close cursor
      /// </summary>
      internal void CloseMainCursor()
      {
         ///vew_crsr_close
         ///
         if (ViewMain != null)
            ViewMain.CloseCursor();

      }


      internal GatewayResult FetchMain(com.magicsoftware.unipaas.management.data.IRecord record)
      {
         GatewayResult result = new GatewayResult();
         if (ViewMain != null)
         {
            result = ViewMain.CursorFetch();
            if (result.Success) //check result
            {
               //TODO: handle records with same position
               ViewMain.CopyValues(record);
            }
         }
         return result;
      }


      /// <summary>
      /// Brings Main View or Virtual view
      /// </summary>
      /// <returns></returns>
      internal RuntimeViewBase GetPrimaryView()
      {
         return ViewMain != null ? (RuntimeViewBase)ViewMain : VirtualView;
      }


      /// <summary>
      /// remove records from position cache
      /// </summary>
      /// <param name="record"></param>
      internal void ClearRecord(IRecord record)
      {
         foreach (var item in RecordViews)
         {
            if (item is RuntimeRealView)
               ((RuntimeRealView)item).ClearRecord(record);
         }
      }

      /// <summary>
      /// update all links
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      private GatewayResult UpdateLinks(IRecord record)
      {
         GatewayResult result = new GatewayResult();
         for (int i = 0; i < LinkViews.Count && result.Success; i++)
            result = LinkViews[i].ModifyRecord(record);
         return result;
      }

      /// <summary>
      /// apply modifications
      /// </summary>
      /// <param name="record"></param>
      /// <returns></returns>
      internal GatewayResult ApplyModifications(IRecord record)
      {
         GatewayResult result = new GatewayResult();
         if (record.getMode() == DataModificationTypes.Delete)
            result = UpdateLinks(record);
         for (int i = 0; i < RecordViews.Count && result.Success; i++)
         {
            if (RecordViews[i] is RuntimeRealView)
               result = ((RuntimeRealView)RecordViews[i]).ApplyModifications(record);
         }
         return result;
      }

      /// <summary>
      /// apply runtime sort on Main View.
      /// </summary>
      /// <param name="sortCollection"></param>
      /// <returns>bool</returns> 
      public bool ApplySort(SortCollection sortCollection)
      {
         bool sortKeySet = false;
         if (ViewMain != null)
            sortKeySet = ViewMain.ApplySort(sortCollection);
         return sortKeySet;
      }


      /// <summary>
      /// Apply user ranges and locates on main view.
      /// </summary>
      /// <param name="mainView"></param>
      public void ApplyUserRangesAndLocates()
      {
         if (ViewMain != null)
         {
            ViewMain.ApplyUserRangesAndLocates();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="controlId"></param>
      /// <returns></returns>
      public RuntimeReadOnlyView GetDataControlViewByBoundControlId(int controlId)
      {
         RuntimeReadOnlyView dataControlView = null;
         foreach (var view in DataControlViews)
         {
            if (((DataControlSourceViewDefinition)view.DataSourceViewDefinition).BoundControlId == controlId)
            {
               dataControlView = view;
               break;
            }
         }

         return dataControlView;
      }

      #region Setters
      public void SetMainView(RuntimeRealView mainView)
      {
         // This method should be called once only.
         Debug.Assert(ViewMain == null);
         ViewMain = mainView;
         RecordViews.Add(mainView);
      }

      public void AddLinkView(int viewId, LinkView linkView)
      {
         LinkViews.Add(viewId, linkView);
         RecordViews.Add(linkView);
      }

      public void SetVirtualView(VirtualsView virtualsView)
      {
         // This method should be called once only.
         Debug.Assert(VirtualView == null);
         VirtualView = virtualsView;
         RecordViews.Add(virtualsView);
      }

      public GatewayResult AddDataControlView(RuntimeReadOnlyView view, DcValuesBuilderBase dcValuesBuilder)
      {
         GatewayResult result = null;

         DataControlViews.Add(view);
         int boundControlId = ((DataControlSourceViewDefinition)view.DataSourceViewDefinition).BoundControlId;
         var dsId = view.DataSourceViewDefinition.TaskDataSource.DataSourceDefinition.Id;
         var strategy = new DataControlValuesComputeStrategy(boundControlId, dcValuesBuilder, view.RangeBuilder, dsId);
         var unitId = RecomputeIdFactory.GetRecomputeId(typeof(DcValues), boundControlId);
         RecordComputer.Add(unitId, strategy);
         result = view.OpenDataSource();
         view.BuildCursor();

         if (result.Success)
            view.Prepare();

         return result;
      }

      internal void SetRemoveView(RemoteView remoteView)
      {
         // This method should be called once only.
         Debug.Assert(RemoteRuntimeView == null);
         RemoteRuntimeView = remoteView;
      }

      internal void SetRecordsComputer(RecordComputer recordComputer)
      {
         // This method should be called once only.
         Debug.Assert(RecordComputer == null);
         RecordComputer = recordComputer;
      }

      internal void SetLocateExpressionRecordComputer(RecordComputer recordComputer)
      {
         // This method should be called once only.
         Debug.Assert(LocateExpressionRecordComputer == null);
         LocateExpressionRecordComputer = recordComputer;
      }

      #endregion
   }





   //TODO : Move to other classes ?
   internal class RemoteView : RuntimeViewBase
   {
   }




}
