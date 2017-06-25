using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class EnhancedVerifyCommand : VerifyCommand
   {
      char _buttonsID;           // buttons of verify opr
      int _defaultButton;        // default button of verify opr
      char _image;               // image of verify opr
      String _returnValStr;
      Field _returnVal;          // return value of verify opr

      protected override void ProcessMessageBoxResponse(Task task, int returnValue)
      {
         if (task != null)
            Operation.setoperVerifyReturnValue(returnValue, _returnVal);
      }

      protected override void PrepareMessageBoxForDisplay(tasks.Task task, ref string mlsTransTitle, ref int style)
      {
         mlsTransTitle = ClientManager.Instance.getLanguageData().translate(_title);
         style = Operation.getButtons(_buttonsID);
         style |= Operation.getImage(_image);
         style |= Operation.getDefaultButton(_defaultButton);
         if (task != null)
         {
            // Verify command return value can only be used with main program fields
            if (Task.isMainProgramField(_returnValStr))
               _returnVal = Operation.InitField(_returnValStr, task);
         }
      }

      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_IMAGE:
               _image = value[0];
               break;

            case ConstInterface.MG_ATTR_BUTTONS:
               _buttonsID = value[0];
               break;

            case ConstInterface.MG_ATTR_DEFAULT_BUTTON:
               _defaultButton = XmlParser.getInt(value);
               break;

            case ConstInterface.MG_ATTR_RETURN_VAL:
               _returnValStr = value;
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }
      }
   }
}
