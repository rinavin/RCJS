using System;
using System.Collections.Generic;
using com.magicsoftware.richclient.rt;
using GuiMisc = com.magicsoftware.util.Misc;
using com.magicsoftware.unipaas.gui;
using RunTimeEvent = com.magicsoftware.richclient.events.RunTimeEvent;
using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.richclient.tasks;

namespace com.magicsoftware.richclient
{
   internal class MenuManager
   {

      /// <summary>
      ///   according to shell return the last control that was in focus
      ///   it can be on subform \ sub sub form....
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <returns></returns>
      private static MgControl getLastFocusedControl(MgForm mgForm)
      {
         int windowIndex = getLastFocusedTask(mgForm).getMGData().GetId();
         return GUIManager.getLastFocusedControl(windowIndex);
      }

      /// <summary>
      ///   according to shell return the last task that was in focus
      ///   it can be on subform \ sub sub form....    *
      /// </summary>
      /// <param name = "mgForm"></param>
      /// <returns></returns>
      private static Task getLastFocusedTask(MgForm mgForm)
      {
         int windowIndex = ((Task)mgForm.getTask()).getMGData().GetId();
         return ClientManager.Instance.getLastFocusedTask(windowIndex);
      }

      /// <summary>
      ///   This method is activated when a program menu was selected. It performs the needed operations in order to
      ///   translate the selected program menu into the matching operation
      /// </summary>
      /// <param name="contextID">active/target context (irelevant for RC)</param>
      /// <param name="menuEntryProgram">the selected menu \ bar menuEntryProgram object</param>
      /// <param name="activeForm"></param>
      /// <param name="ActivatedFromMDIFrame"></param>
      /// <returns></returns>
      internal static void onProgramMenuSelection(Int64 contextID, MenuEntryProgram menuEntryProgram, MgForm activeForm, bool ActivatedFromMDIFrame)
      {
         Task menuTask = getLastFocusedTask(activeForm);

         ClientManager.Instance.RuntimeCtx.LastClickedMenuUid = menuEntryProgram.menuUid();
         RunTimeEvent programMenuEvt = new RunTimeEvent(menuEntryProgram, menuTask, ActivatedFromMDIFrame);
         ClientManager.Instance.EventsManager.addToTail(programMenuEvt);
      }

      /// <summary>
      ///   This method is activated when an Event menu was selected. It performs the needed operations in order to
      ///   translate the selected event menu into the matching operation
      /// </summary>
      /// <param name = "menuEntryEvent">the selected menu \ bar menuEntryEvent object</param>
      /// <param name = "activeForm">last active Form</param>
      /// <param name = "ctlIdx">the index of the ctl which the menu is attached to in toolkit</param>
      internal static void onEventMenuSelection(MenuEntryEvent menuEntryEvent, MgForm activeForm, int ctlIdx)
      {

         MgControl lastFocusedControl = getLastFocusedControl(activeForm);
         Task task = getLastFocusedTask(activeForm);

         RunTimeEvent aRtEvt = new RunTimeEvent(menuEntryEvent, task, lastFocusedControl, ctlIdx);
         aRtEvt.setPublicName();
         aRtEvt.setMainPrgCreator(null);

         // build the argument list from the mainProgVars
         List<String> mainProgVars = menuEntryEvent.MainProgVars;
         if (mainProgVars != null && mainProgVars.Count > 0)
         {
            ArgumentsList argList = new ArgumentsList();
            argList.fillListByMainProgVars(mainProgVars, task.getCtlIdx());
            aRtEvt.setArgList(argList);
            aRtEvt.setTask(null);
         }

         ClientManager.Instance.EventsManager.addToTail(aRtEvt);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="contextID">active/target context (irelevant for RC)</param>
      /// <param name="osCommandMenuEntry"></param>
      /// <param name="lastFocusedCtrlTask">
      ///the task of the last focused control. This is required because even if there is no 
      ///last focused control, then also we have task and hence expression/function will be executed properly.
      ///Previously task was obtained from the control and when there was no control,task could not be obtained.
      /// </param>
      /// <returns></returns>
      internal static void onOSMenuSelection(Int64 contextID, MenuEntryOSCommand osCommandMenuEntry, MgForm activeForm)
      {
         Task lastFocusedCtrlTask = getLastFocusedTask(activeForm);

         RunTimeEvent osMenuEvent = new RunTimeEvent(osCommandMenuEntry, lastFocusedCtrlTask);

         if (osCommandMenuEntry.Wait)
            ClientManager.Instance.EventsManager.handleEvent(osMenuEvent, false);
         else
            ClientManager.Instance.EventsManager.addToTail(osMenuEvent);
      }

      /// <summary>
      /// get menu path
      /// </summary>
      /// <param name="task">
      /// <returns>string</returns>
      internal static string GetMenuPath(Task task)
      {
            Task mainProg = MGDataCollection.Instance.GetMainProgByCtlIdx((task).getCtlIdx());
            Task currTsk = ClientManager.Instance.EventsManager.getCurrTask() ?? (Task)task.GetContextTask();

            // fixed bug#919779, the MenuUid is save on the parent.
            // MenuUid is saved on the parent. bacouse we don't have menu on the current form.
            int menuUid = currTsk.MenuUid;
            while (menuUid == 0 && currTsk != null)
            {
               currTsk = currTsk.getParent();
               if (currTsk != null)
                  menuUid = currTsk.MenuUid;
            }

            //as we doing for online: if it is not program that was called, then get the last click menu id from the top most form.
            if (menuUid == 0)
            {
               currTsk = ClientManager.Instance.EventsManager.getCurrTask() ?? (Task)task.GetContextTask();
               menuUid = currTsk.getTopMostForm().LastClickedMenuUid;
            }

            string menuPath = Manager.MenuManager.GetMenuPath(mainProg, menuUid);
            return menuPath;

       }
   }
}
