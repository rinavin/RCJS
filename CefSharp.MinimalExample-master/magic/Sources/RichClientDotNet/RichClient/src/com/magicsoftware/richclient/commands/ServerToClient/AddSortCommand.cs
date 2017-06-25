using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.tasks.sort;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// AddSortCommand
   /// </summary>
   class AddSortCommand : ClientTargetedCommandBase
   {
      internal Sort UserSort { get; private set; }

      /// <summary>
      /// Execute AddSortCommand
      /// </summary>
      /// <param name="res"></param>
      public override void  Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;
         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
         AddUserSortDataViewCommand command = CommandFactory.CreateAddUserSortDataviewCommand(TaskTag, UserSort);
         task.DataviewManager.Execute(command);
      }

      /// <summary>
      /// Handle Attributes of the command.
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="value"></param>
      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_FIELD_INDEX:
               if (UserSort == null)
                  UserSort = new Sort();
               UserSort.fldIdx = XmlParser.getInt(value);
               break;

            case ConstInterface.MG_ATTR_DIR:
               UserSort.dir = XmlParser.getInt(value) == 1 ? true : false; 
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }
      }
   }
}
