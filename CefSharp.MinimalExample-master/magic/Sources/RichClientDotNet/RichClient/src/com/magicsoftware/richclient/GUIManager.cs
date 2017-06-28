using System;
using System.Text;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using Manager = com.magicsoftware.unipaas.Manager;
using com.magicsoftware.richclient.gui;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.richclient
{
   /// <summary> 
   /// </summary>
   internal class GUIManager
   {
      private static readonly ControlTable _lastFocusedControls = new ControlTable(); // last focused controls PER WINDOW
      internal static int LastFocusMgdID { get; private set; } //the last MGDATA for which setFocus was performed

      private BrowserWindow _browserWindow;
      internal BrowserWindow BrowserWindow
      {
         get
         {
            if (_browserWindow == null)
            {
               // default dimensions
               MgRectangle formRect = new MgRectangle(0, 0, 640, 480);
               _browserWindow = new BrowserWindow(formRect);
            }
            return _browserWindow;
         }
      }


      ///   singleton
      /// </summary>
      private static GUIManager _instance; // the single instance of the GUIManager
      internal static GUIManager Instance
      {
         get
         {
            if (_instance == null)
            {
               lock (typeof(GUIManager))
               {
                  if (_instance == null)
                     _instance = new GUIManager();
               }
            }
            return _instance;
         }
      }

      /// <summary>
      ///   private CTOR to prevent instantiating this class
      /// </summary>
      private GUIManager()
      {
         _browserWindow = null;
      }

      /// <summary>
      ///   execute the commands in the queue
      /// </summary>
      internal void execGuiCommandQueue()
      {
         Commands.beginInvoke();
      }

      /// <summary>
      ///   Shows contents of the document located by URL inside a browser control on a separate form
      /// </summary>
      /// <param name = "url">document path</param>
      /// <param name = "openAsApplicationModal">open form modally</param>
      internal void showContentFromURL(String url, bool openAsApplicationModal)
      {
         if (string.IsNullOrEmpty(url))
            return;

         BrowserWindow.ShowContentFromURL(url, openAsApplicationModal);
      }


      ///   Show contents inside a browser control on a separate form
      /// </summary>
      /// <param name = "contents">contents to show </param>
      internal void showContent(String contents)
      {
         BrowserWindow.ShowContent(contents);
      }

      /// <summary>
      ///   start a timer
      /// </summary>
      /// <param name = "mgd"> MgData </param>
      /// <param name = "seconds">interval in full seconds to invoke the function (from current time)</param>
      /// <param name = "isIdleTimer">true if this timer is the idle timer</param>
      internal void startTimer(MGData mgData, int seconds, bool isIdleTimer)
      {
         if (seconds > 0)
         {
            //RCTimer/MgTimer works on milliseconds so convert seconds to milliseconds
            RCTimer objRCTimer = new RCTimer(mgData, seconds * 1000, isIdleTimer);
            Commands.addAsync(CommandType.START_TIMER, objRCTimer);

            // fixed bug 284566, while we have command in he queue, execute them 
            Commands.beginInvoke();
         }
      }

      /// <summary>
      ///   stop the timer
      /// </summary>
      /// <param name = "MgData"> MgData</param>
      /// <param name = "seconds">interval in full seconds to invoke the function (from current time)</param>
      /// <param name = "isIdleTimer">true if this timer is the idle timer</param>
      internal void stopTimer(MGData mgData, int seconds, bool isIdleTimer)
      {
         if (seconds > 0)
         {
            //RCTimer/MgTimer works on milliseconds so convert seconds to milliseconds
            RCTimer.StopTimer(mgData, seconds * 1000, isIdleTimer);
         }
      }

      /// <summary>
      ///  close the task, if the wide is open, then close is before close the task
      /// </summary>
      /// <param name = "form"> </param>
      internal void abort(MgForm form)
      {
         if (form != null)
         {
            Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx(form.getTask().getCtlIdx());
            Manager.Abort(form, mainProg);
         }
      }

      /// <summary>
      ///   process ABORT command.Close all shells. Last shell closed, will close the display.
      /// </summary>
      protected internal void abort()
      {
         Commands.disposeAllForms();
      }

      /// <summary>
      ///   Open message box with title id and message id that will take from MGBCL*ENG.xml (created by mgconst)
      /// </summary>
      /// <param name = "form"></param>
      /// <param name = "titleId">message from from MGBCL*ENG.xml</param>
      /// <param name = "msgId">message from from MGBCL*ENG.xml</param>
      /// <param name = "style">the style of the message box can be flags of Styles.MSGBOX_xxx </param>
      /// <returns> message box style from Styles.MSGBOX_XXX</returns>
      internal int writeToMessageBox(MgForm form, string titleId, string msgId, int style)
      {
         String title = ClientManager.Instance.getMessageString(titleId);
         String msg = ClientManager.Instance.getMessageString(msgId);

         return (Commands.messageBox(form.getTopMostForm(), title, msg, style));
      }

      /// <summary>
      ///   display a Message Box (confirm) with 2 buttons : yes & no and icon question.
      /// </summary>
      /// <param name = "form">the parent form </param>
      /// <param name = "msgId">message number (from MsgInterface.java) that will be display on the content of the Message Box
      ///   return true if the button YES was pressed
      /// </param>
      internal bool confirm(MgForm form, string msgId)
      {
         int retResult = confirm(form, msgId,
                                 Styles.MSGBOX_ICON_QUESTION | Styles.MSGBOX_BUTTON_YES_NO |
                                 Styles.MSGBOX_DEFAULT_BUTTON_2);
         return (retResult == Styles.MSGBOX_RESULT_YES);
      }

      /// <summary>
      ///   display a Message Box (confirm) with 2 buttons .
      /// </summary>
      /// <param name = "form">the parent form </param>
      /// <param name = "msgId">message number (from MsgInterface.java) that will be display on the content of the Message Box </param>
      /// <param name = "style">the icon and the button that will be display on the confirmation box </param>
      /// <returns> the button that was pressed (Styles.MSGBOX_BUTTON_xxx) </returns>
      internal int confirm(MgForm form, string msgId, int style)
      {
         return writeToMessageBox(form, MsgInterface.CONFIRM_STR_WINDOW_TITLE, msgId, style);
      }

      /// <summary>
      ///   Set the current cursor
      /// </summary>
      /// <param name = "shape">code of mouse cursor: "wait", "hand"...</param>
      internal bool setCurrentCursor(MgCursors shape)
      {
         bool ret = true;
         if (Manager.IsInitialized() && !ClientManager.Instance.IsHidden)
         // !ClientManager.isHidden: Topic #13 (MAGIC version 1.8\SP1 for WIN) RC mobile - improve performance: spec, section 4.3
         {
            Commands.addAsync(CommandType.SET_CURRENT_CURSOR, shape);
            Commands.beginInvoke();
         }
         return ret;
      }

      /// <summary>
      ///   changes all illegal characters or special characters used for parsing to their legal format
      ///   the parsing is performed at several places first when passing the url to IE by IE
      ///   second in the requester (rqhttp.cpp) when parsing the url for arguments 
      ///   thirst in the magic engine (mgrqmrg.cpp - copy_in_args)
      ///   we must distinguish between characters that are reserved for special use by the URL RFC and
      ///   between characters used internally by magic for special use
      /// </summary>
      /// <param name = "source">url to be converted </param>
      /// <returns> the converted url   </returns>
      internal String makeURLPrintable(String source)
      {
         String[] from = new[] { "\\", "\x0000", "," };
         String[] to = new[] { "\\\\", "\\0", "\\," };

         string result = StrUtil.searchAndReplace(source, from, to);
         return result;
      }

      /// <summary>
      ///   The main message loop that reads messages from the queue and dispatches them.
      /// </summary>
      internal void messageLoop()
      {
        // Manager.MessageLoop();
      }

      /// <summary>
      ///   get the last control that had the focus in the current mgdata
      /// </summary>
      /// <returns> reference to the last focused control</returns>
      internal static MgControl getLastFocusedControl()
      {
         return (MgControl)_lastFocusedControls.getCtrl(MGDataCollection.Instance.currMgdID);
      }

      /// <summary>
      ///   get the last control that had the focus in the current mgdata
      /// </summary>
      /// <param name = "mgdID">the mgdata ID for which to return the last focused control</param>
      /// <returns> reference to the last focused control</returns>
      internal static MgControl getLastFocusedControl(int mgdID)
      {
         return (MgControl)_lastFocusedControls.getCtrl(mgdID);
      }

      /// <summary>
      /// get the control at 'index'
      /// </summary>
      /// <param name="index"></param>
      internal static void deleteLastFocusedControlAt(int index)
      {
         if (_lastFocusedControls != null)
            _lastFocusedControls.deleteControlAt(index);
      }

      /// <summary>
      /// set the control at 'index'
      /// </summary>
      /// <param name="ctrl"></param>
      /// <param name="currMgdID"></param>
      internal static void setLastFocusedControlAt(MgControlBase ctrl, int currMgdID)
      {
         _lastFocusedControls.setControlAt(ctrl, currMgdID);
      }

      /// <summary>
      /// set the last focused control and the task for current window.
      /// </summary>
      /// <param name="task"></param>
      /// <param name="MgControlBase"></param>
      internal static void setLastFocusedControl(Task task, MgControlBase mgControl)
      {
         int currMgdID;

         currMgdID = task.getMgdID();

         Debug.Assert(mgControl == null || task == mgControl.getForm().getTask());

         LastFocusMgdID = currMgdID;
         setLastFocusedControlAt(mgControl, currMgdID);
         ClientManager.Instance.setLastFocusedTask(task);
      }

#if PocketPC
         /// <summary> Add the event for context hibernation
         /// </summary>
         internal void hibernateContext()
         {
            GUIMain.getInstance().MainForm.NeedResume = true;
            ClientManager.Instance.EventsManager.addGuiTriggeredEvent(InternalInterface.MG_ACT_HIBERNATE_CTX);
         }

         /// <summary> Add the event for context resume - if successful, will show and activate the forms
         /// </summary>
         internal void restoreHiddenForms()
         {
            GUIMain.getInstance().MainForm.NeedResume = false;
            ClientManager.Instance.EventsManager.addGuiTriggeredEvent(InternalInterface.MG_ACT_VERIFY_RESUMED_CTX);
         }
#endif
   }
}
