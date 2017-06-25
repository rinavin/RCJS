using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using System.Diagnostics;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// column sort command
   /// </summary>
   class LocalDataViewCommandSort : LocalDataViewCommandBase
   {
      SortCollection Sorts;
      LocalDataViewCommandViewRefresh localDataViewCommandViewRefresh;

      public LocalDataViewCommandSort(ColumnSortEventCommand command)
         : base(command)
      {
         //Prepare Sort collection.
         Sorts = new SortCollection();
         Sort sort = new Sort();
         sort.fldIdx = command.FldId;
         sort.dir = (command.Direction == 0) ? true : false;
         Sorts.add(sort);

         RefreshEventCommand refreshCommand = CommandFactory.CreateRealRefreshCommand(command.TaskTag, InternalInterface.MG_ACT_RT_REFRESH_VIEW, 0, null, command.ClientRecId);

         localDataViewCommandViewRefresh = new LocalDataViewCommandViewRefresh(refreshCommand);
         localDataViewCommandViewRefresh.RefreshMode = ViewRefreshMode.CurrentLocation;
         localDataViewCommandViewRefresh.KeepUserSort = true;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         GatewayResult result = new GatewayResult();

         if (!TaskViews.Task.isAborting () && TaskViews.ApplySort(Sorts))
         {
            localDataViewCommandViewRefresh.DataviewManager = DataviewManager;
            localDataViewCommandViewRefresh.LocalManager = LocalManager;
            localDataViewCommandViewRefresh.Execute();
         }
         
         return result;
      }
   }
}

