using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.local.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   /// <summary>
   /// AddRangeCommand
   /// </summary>
   class AddRangeCommand : ClientTargetedCommandBase
   {
      internal UserRange UserRange { get; private set; }

      public override bool ShouldExecute
      {
         get
         {
            MGDataCollection mgDataTab = MGDataCollection.Instance;
            Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
            return task.isStarted();
         }
      }
      /// <summary>
      /// Execute AddRange command.
      /// </summary>
      /// <param name="res"></param>
      public override void Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)mgDataTab.GetTaskByID(TaskTag);
         FieldDef fieldDef = task.DataView.getField((int)UserRange.veeIdx - 1);
         int parsedLen;

         AddUserRangeDataviewCommand command = CommandFactory.CreateAddUserRangeDataviewCommand(TaskTag, UserRange);
         if (!UserRange.nullMin)
            command.Range.min = RecordUtils.deSerializeItemVal(UserRange.min, fieldDef.getType(), fieldDef.getSize(), true, fieldDef.getType(), out parsedLen);
         if (!UserRange.nullMax)
            command.Range.max = RecordUtils.deSerializeItemVal(UserRange.max, fieldDef.getType(), fieldDef.getSize(), true, fieldDef.getType(), out parsedLen);

         task.DataviewManager.Execute(command);
      }

      /// <summary>
      /// Handle Attribute
      /// </summary>
      /// <param name="attribute"></param>
      /// <param name="value"></param>
      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_FIELD_INDEX:
               if (UserRange == null)
                  UserRange = new UserRange() { nullMin = true, nullMax = true, discardMin = true, discardMax = true };
               UserRange.veeIdx = XmlParser.getInt(value);
               break;

            case ConstInterface.MG_ATTR_MIN_VALUE:
               UserRange.min = value;
               UserRange.nullMin = false;
               UserRange.discardMin = false;
               break;

            case ConstInterface.MG_ATTR_NULL_MIN_VALUE:
               UserRange.nullMin =true;
               UserRange.discardMin = false;
               break;

            case ConstInterface.MG_ATTR_NULL_MAX_VALUE:
               UserRange.nullMax = true;
               UserRange.discardMax = false;
               break;

            case ConstInterface.MG_ATTR_MAX_VALUE:
               UserRange.max = value;
               UserRange.discardMax = false;
               UserRange.nullMax = false;
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }
      }
   }
}
