using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// AddLocateCommand
   /// </summary>
   class AddLocateCommand : AddRangeCommand
   {
      /// <summary>
      /// Execute AddLocateCommand
      /// </summary>
      /// <param name="exp"></param>
      public override void  Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
         FieldDef fieldDef = task.DataView.getField((int)UserRange.veeIdx - 1);
         int parsedLen;

         AddUserLocateDataViewCommand command = CommandFactory.CreateAddUserLocateDataviewCommand(TaskTag, UserRange);
         if (!UserRange.nullMin)
            command.Range.min = RecordUtils.deSerializeItemVal(UserRange.min, fieldDef.getType(), fieldDef.getSize(), true, fieldDef.getType(), out parsedLen);
         if (!UserRange.nullMax)
            command.Range.max = RecordUtils.deSerializeItemVal(UserRange.max, fieldDef.getType(), fieldDef.getSize(), true, fieldDef.getType(), out parsedLen);

         task.DataviewManager.Execute(command);
      }
   }
}
