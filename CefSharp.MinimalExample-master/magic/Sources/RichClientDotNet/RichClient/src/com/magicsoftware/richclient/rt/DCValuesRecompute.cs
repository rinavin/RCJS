using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.rt
{
   /// <summary>
   /// Denotes the recompute operation of a DC values. An instance of this class
   /// will be created when the server notifies the client that a field is a dependency
   /// of a data control values range.<para/>
   /// When 'Recompute' class is executing the recompute sequence, it will call this type's
   /// 'Recompute' method to recompute the DC values.<para/>
   /// </summary>
   class DCValuesRecompute
   {
      RecomputeId unitId;
      MgControlBase control;

      public DCValuesRecompute(Task task, int ditIndex)
      {
         control = task.getForm().getCtrl(ditIndex);
         unitId = RecomputeIdFactory.GetRecomputeId(control);
      }

      public bool Recompute(Task task, Record record)
      {
         IClientCommand dataViewCommand = CommandFactory.CreateRecomputeUnitDataViewCommand(task.getTaskTag(), unitId, record.getId());
         ReturnResult result = task.DataviewManager.Execute(dataViewCommand);
         if (result.Success)
            control.RefreshDisplay();
         return result.Success;
      }
   }
}
