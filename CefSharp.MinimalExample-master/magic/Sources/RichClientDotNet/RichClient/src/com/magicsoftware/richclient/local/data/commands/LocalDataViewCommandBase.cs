using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.util;
using System;
using com.magicsoftware.richclient.util;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// local dataview command
   /// </summary>

   internal abstract class LocalDataViewCommandBase : DataViewCommandBase
   {
      /// <summary>
      /// Task
      /// </summary>
      virtual protected Task Task { get { return DataviewManager.Task; } }

      /// <summary>
      /// local dataview manager
      /// </summary>
      internal LocalDataviewManager LocalDataviewManager { get { return DataviewManager as LocalDataviewManager; } }

      /// <summary>
      /// local data manager
      /// </summary>
      internal LocalManager LocalManager { get; set; }

      /// <summary>
      /// View Composite
      /// </summary>
      protected TaskViewsCollection TaskViews { get { return LocalDataviewManager.TaskViews; } }

      /// <summary>
      /// position Cache
      /// </summary>
      protected PositionCache PositionCache { get { return LocalDataviewManager.PositionCache; } }

      /// <summary>
      /// Dataview synchronizer
      /// </summary>
      protected DataviewSynchronizer DataviewSynchronizer { get { return LocalDataviewManager.DataviewSynchronizer; } }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="command"></param>
      ///
      internal LocalDataViewCommandBase(IClientCommand command)
      {
      }

      /// <summary>
      /// Sets task view when the task go to create mode or task is opened in create mode
      /// </summary>
      /// <returns></returns>
      internal ReturnResultBase SetViewForCreateMode()
      {
         PositionCache.IncludesFirst = true;
         PositionCache.IncludesLast = true;
         DataviewSynchronizer.InvalidateView();

         Task.setOriginalTaskMode(Constants.TASK_MODE_CREATE);
         Task.setMode(Constants.TASK_MODE_CREATE);
         return (DataviewSynchronizer.UpdateAfterFetch(Int32.MinValue));

      }
   }
}
