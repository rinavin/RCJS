using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.local.data.cursor;
using com.magicsoftware.richclient.local.data.view.fields;
using com.magicsoftware.richclient.local.data.view.RecordCompute;

namespace com.magicsoftware.richclient.local.data.view
{
   /// <summary>
   /// factory that creates vies
   /// </summary>
   internal class TaskViewsCollectionBuilder
   {
      private Task task;
      private LocalDataviewManager LocalDataviewManager { get { return task.DataviewManager.LocalDataviewManager; } }
      internal FieldsBuilder FieldsBuilder { get; set; }

      internal TaskViewsCollection BuildViewsCollection(Task task)
      {
         TaskViewsCollection taskViews = new TaskViewsCollection();
         this.task = task;
         taskViews.Task = task;

         taskViews.Fields.AddRange(FieldsBuilder.Build(task));
         taskViews.SetVirtualView(CreateVirtualView());
         taskViews.SetRemoveView(CreateRemoteView());
         CreateRealRecordViews(taskViews);
         CreateRecordCursors(taskViews);
         taskViews.MapRecordFields();
         taskViews.SetRecordsComputer(BuildRecordComputer(taskViews));
         taskViews.SetLocateExpressionRecordComputer(BuildLocateExpressionRecordComputer(taskViews));

         if (taskViews.ViewMain == null)
         {
            LocalDataviewManager.PositionCache.IncludesFirst = true;
            LocalDataviewManager.PositionCache.IncludesLast = true;
         }

         return taskViews;
      }

      /// <summary>
      /// create record views
      /// </summary>
      void CreateRealRecordViews(TaskViewsCollection taskViews)
      {
         List<IDataviewHeader> dataviewHeaders = task.DataviewHeadersTable.FindAll(l => l is LocalDataviewHeader);
         foreach (IDataviewHeader dataviewHeader in dataviewHeaders)
         {
            if (dataviewHeader.IsMainSource)
               taskViews.SetMainView(CreateMainSourceView(dataviewHeader));
            else
               taskViews.AddLinkView(dataviewHeader.Id, (LinkView)CreateLinkView(dataviewHeader));
         }
      }

      void CreateRecordCursors(TaskViewsCollection views)
      {
         foreach (var view in views.RecordViews)
         {
            if (view is RuntimeRealView)
               ((RuntimeRealView)view).BuildCursor();
         }

         if (views.ViewMain != null)
            views.ViewMain.BuildLocateCursor();
      }

      /// <summary>
      /// Create views for data controls.
      /// </summary>
      /// <param name="dataControlSourceDefintions">The data source definitions of the data controls for which views should be created.
      /// The list can be prepared by "DataSourceDefintionsBuilder" for example.</param>
      internal void CreateDataControlViews(List<IDataSourceViewDefinition> dataControlSourceDefintions)
      {
         foreach (var dataSourceViewDefinition in dataControlSourceDefintions)
         {
            CreateDataControlSourceView(dataSourceViewDefinition);
         }
      }

      /// <summary>
      /// Creates a view for the task's main source.
      /// </summary>
      /// <param name="viewDefinition">The dataview header for the main source.</param>
      /// <returns></returns>
      internal RuntimeRealView CreateMainSourceView(IDataviewHeader viewDefinition)
      {
         RuntimeRealView view = new RuntimeRealView();
         view.CursorBuilder = new MainCursorBuilder(view);
         InitializeView(view, viewDefinition);
         return view;
      }

      /// <summary>
      /// Creates a view for a task's dataview link source.
      /// </summary>
      /// <param name="viewDefinition">The dataview header definition for the link</param>
      /// <returns></returns>
      internal RuntimeRealView CreateLinkView(IDataviewHeader viewDefinition)
      {
         RuntimeRealView view = new LinkView();
         view.CursorBuilder = new CursorBuilder(view);
         InitializeView(view, viewDefinition);
         return view;
      }

      /// <summary>
      /// Creates a view for a data control's data source.
      /// </summary>
      /// <param name="viewDefinition">The defintion of the data control source view.</param>
      /// <returns></returns>
      internal RuntimeReadOnlyView CreateDataControlSourceView(IDataSourceViewDefinition viewDefinition)
      {
         RuntimeReadOnlyView view = new RuntimeReadOnlyView();
         InitializeView(view, viewDefinition);
         view.CursorBuilder = new CursorBuilder(view);
         view.RangeBuilder = ((DataControlSourceViewDefinition)viewDefinition).RangeDataBuilder;
         return view;
      }

      /// <summary>
      /// Initializes a view according to provided view definition.
      /// </summary>
      /// <param name="view">The view to initialize</param>
      /// <param name="viewDefinition">The parameters for the initialized view.</param>
      private void InitializeView(RuntimeReadOnlyView view, IDataSourceViewDefinition viewDefinition)
      {
         view.Initialize(viewDefinition, LocalDataviewManager);
      }

      /// <summary>
      /// create view of virtuals
      /// </summary>
      /// <returns></returns>
      internal VirtualsView CreateVirtualView()
      {
         VirtualsView view = new VirtualsView();
         view.LocalDataviewManager = LocalDataviewManager;
         return view;

      }

      /// <summary>
      /// create remote view
      /// </summary>
      /// <returns></returns>
      internal RemoteView CreateRemoteView()
      {
         return new RemoteView();
      }

      /// <summary>
      /// build record computer
      /// </summary>
      /// <returns></returns>
      RecordComputer BuildRecordComputer(TaskViewsCollection taskViews)
      {
         RecordComputerBuilder recordComputerBuilder = new RecordComputerBuilder()
         {
            LocalDataviewManager = LocalDataviewManager,
            TaskViews = taskViews
         };
         return recordComputerBuilder.Build();

      }

      /// <summary>
      /// build record computer
      /// </summary>
      /// <returns></returns>
      RecordComputer BuildLocateExpressionRecordComputer(TaskViewsCollection taskViews)
      {
         RecordComputerBuilder recordComputerBuilder = new LocateExpressionRecordComputerBuilder()
         {
            LocalDataviewManager = LocalDataviewManager,
            TaskViews = taskViews
         };
         return recordComputerBuilder.Build();

      }

   }

}
