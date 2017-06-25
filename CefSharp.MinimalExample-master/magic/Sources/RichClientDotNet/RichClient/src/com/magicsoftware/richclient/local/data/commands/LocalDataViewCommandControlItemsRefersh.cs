using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.commands;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.util;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.gatewaytypes.data;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// LocalDataViewCommandControlItemsRefersh
   /// </summary>
   class LocalDataViewCommandControlItemsRefersh : LocalDataViewCommandBase
   {
      MgControlBase control;

      public LocalDataViewCommandControlItemsRefersh(ControlItemsRefreshCommand command)
         :base(command)
      {
         control = command.Control;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         LocalDataviewManager localDataViewManager = Task.DataviewManager.LocalDataviewManager;

         RuntimeReadOnlyView dataControlView = localDataViewManager.TaskViews.GetDataControlViewByBoundControlId(control.getDitIdx());

         DataSourceId dataSourceId = dataControlView.DataSourceViewDefinition.TaskDataSource.DataSourceDefinition.Id;

         DataControlRangeDataCollection rangeData = new DataControlRangeDataCollection(dataSourceId, dataControlView.RangeBuilder, control.getDitIdx());

         //remove old DataControlRangeDataCollection entry from map.
         localDataViewManager.rangeToDcValuesMap.Remove(rangeData);

         //build new DC values for control
         LocallyComputedDcValuesBuilder dcValuesBuilder = new LocallyComputedDcValuesBuilder(dataControlView);
         var dataControlValues = dcValuesBuilder.Build();

         localDataViewManager.DataviewSynchronizer.ApplyDCValuesAndRefreshControl(dataControlValues, rangeData, control);

         return new ReturnResult();
      }
   }
}
