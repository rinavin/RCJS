using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.remote;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.util;
using com.magicsoftware.richclient.data;
using com.magicsoftware.unipaas.management.tasks;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.remote
{
   /// <summary>
   /// RemoteControlItemsRefreshCommand
   /// </summary>
   class RemoteControlItemsRefreshCommand : RemoteDataViewCommandBase
   {
      MgControlBase control;

      public RemoteControlItemsRefreshCommand(ControlItemsRefreshCommand command)
         : base(command)
      {
         control = command.Control;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ResultValue res = new ResultValue();
         
         IClientCommand cmd = CommandFactory.CreatecFetchDataControlValuesCommand(Task.getTaskTag(), control.getName());
         Task.getMGData().CmdsToServer.Add(cmd);
         
         //Fetch data control values from server.
         RemoteCommandsProcessor.GetInstance().Execute(CommandsProcessorBase.SendingInstruction.TASKS_AND_COMMANDS, CommandsProcessorBase.SessionStage.NORMAL, res);

         //Update DCValRef of every record after fetching the dataControl values.
         for (int i = 0; i < ((DataView)Task.DataView).getSize(); i++)
            ((DataView)Task.DataView).getRecByIdx(i).AddDcValuesReference(control.getDitIdx(), control.getDcRef());

         //Update DCValRef of original record.
         ((DataView)Task.DataView).getOriginalRec().AddDcValuesReference(control.getDitIdx(), control.getDcRef());

         return new ReturnResult();
      }
   }
}
