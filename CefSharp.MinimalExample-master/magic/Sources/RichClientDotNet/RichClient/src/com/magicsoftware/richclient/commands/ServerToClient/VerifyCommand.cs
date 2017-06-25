using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.commands.ClientToServer;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;
using com.magicsoftware.richclient.data;

namespace com.magicsoftware.richclient.commands.ServerToClient
{
   class VerifyCommand : ClientTargetedCommandBase
   {
      String _callingTaskTag;    // is the id of the task that called the new window
      protected String _title;   // title of verify opr
      bool _errLogAppend;        // append to error log for verify opr
      String _text;              // the verify message
      char _display = (char)(0);
      char _mode = (char)(0);
      bool _sendAck = true;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="exp"></param>
      public override void Execute(rt.IResultValue res)
      {
         MGDataCollection mgDataTab = MGDataCollection.Instance;

         Task task = (Task)(mgDataTab.GetTaskByID(TaskTag) ?? mgDataTab.GetTaskByID(_callingTaskTag));
         if (task == null)
            task = ClientManager.Instance.getLastFocusedTask();

         // In order to keep the behavior same as in Online, verify operation warning messages 
         // will be written as error messages in Client log (QCR #915122)
         if (_errLogAppend)
            Logger.Instance.WriteExceptionToLog(_text + ", program : " + ClientManager.Instance.getPrgName());

         //Blank Message will not be shown, same as in Online
         if (!String.IsNullOrEmpty(_text))
         {
            if (_display == ConstInterface.DISPLAY_BOX)
            {
               MgForm currForm;
               int style = 0;

               // on verify box, show translated value. (status is handled in property).
               String mlsTransText = ClientManager.Instance.getLanguageData().translate(_text);
               String mlsTransTitle = String.Empty;

               if (task != null && task.isStarted())
                  currForm = (MgForm)task.getTopMostForm();
               else
                  currForm = null;

               PrepareMessageBoxForDisplay(task, ref mlsTransTitle, ref style);

               int returnValue = Commands.messageBox(currForm, mlsTransTitle, mlsTransText, style);

               ProcessMessageBoxResponse(task, returnValue);
            }
            // display message on status only if we have a task
            //Blank Message will not be shown, same as in Online
            else if (_display == ConstInterface.DISPLAY_STATUS && task != null)
            {
               task.DisplayMessageToStatusBar(_text);
            }
         }

         if (_sendAck)
            task.CommandsProcessor.Execute(CommandsProcessorBase.SendingInstruction.NO_TASKS_OR_COMMANDS);
      }

      /// <summary>
      /// Prepares the message box's style and title for display.
      /// </summary>
      /// <param name="task"></param>
      /// <param name="mlsTransTitle"></param>
      /// <param name="style"></param>
      protected virtual void PrepareMessageBoxForDisplay(Task task, ref String mlsTransTitle, ref int style)
      {
            String options = ClientManager.Instance.getMessageString(MsgInterface.BRKTAB_STOP_MODE_TITLE);
            _title = ConstUtils.getStringOfOption(options, "EW", _mode);
            mlsTransTitle = ClientManager.Instance.getLanguageData().translate(_title);
            //add the icon according to the mode :is Error \ Warning
            style = Styles.MSGBOX_BUTTON_OK |
                    ((_mode == 'E')
                        ? Styles.MSGBOX_ICON_ERROR
                        : Styles.MSGBOX_ICON_WARNING);
      }

      /// <summary>
      /// Handles the response received from the message box.
      /// </summary>
      /// <param name="task"></param>
      /// <param name="returnValue"></param>
      protected virtual void ProcessMessageBoxResponse(Task task, int returnValue)
      {
         // intentionally left empty.
      }

      public override void HandleAttribute(string attribute, string value)
      {
         switch (attribute)
         {
            case ConstInterface.MG_ATTR_TITLE:
               _title = value;
               break;

            case ConstInterface.MG_ATTR_CALLINGTASK:
               _callingTaskTag = value;
               break;

            case ConstInterface.MG_ATTR_ERR_LOG_APPEND:
               _errLogAppend = XmlParser.getBoolean(value);
               break;

            case ConstInterface.MG_ATTR_TEXT:
               _text = XmlParser.unescape(value);
               break;

            case ConstInterface.MG_ATTR_DISPLAY:
               _display = value[0];
               break;

            case ConstInterface.MG_ATTR_MODE:
               _mode = value[0];
               break;

            case ConstInterface.MG_ATTR_ACK:
               _sendAck = (XmlParser.getInt(value) != 0);
               break;

            default:
               base.HandleAttribute(attribute, value);
               break;
         }
      }
   }
}
