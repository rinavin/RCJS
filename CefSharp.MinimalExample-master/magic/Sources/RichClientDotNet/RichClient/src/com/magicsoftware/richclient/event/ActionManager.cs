using Task = com.magicsoftware.richclient.tasks.Task;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.management.events;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;

namespace com.magicsoftware.richclient.events
{
   /// <author>  marina
   /// 
   /// </author>
   internal class ActionManager : IActionManager
   {
      internal static readonly int[] actEnabled = new[]
                                                     {
                                                        InternalInterface.MG_ACT_CLOSE, InternalInterface.MG_ACT_EXIT,
                                                        InternalInterface.MG_ACT_RTO_MODIFY,
                                                        InternalInterface.MG_ACT_RTO_CREATE,
                                                        InternalInterface.MG_ACT_RTO_QUERY, InternalInterface.MG_ACT_OK,
                                                        InternalInterface.MG_ACT_CANCEL,
                                                        InternalInterface.MG_ACT_TBL_PRVFLD,
                                                        InternalInterface.MG_ACT_TBL_NXTFLD,
                                                        InternalInterface.MG_ACT_TBL_BEGLINE,
                                                        InternalInterface.MG_ACT_TBL_ENDLINE,
                                                        InternalInterface.MG_ACT_TBL_BEGPAGE,
                                                        InternalInterface.MG_ACT_TAB_NEXT,
                                                        InternalInterface.MG_ACT_TAB_PREV,
                                                        InternalInterface.MG_ACT_TBL_ENDPAGE,
                                                        InternalInterface.MG_ACT_USING_HELP,
                                                        InternalInterface.MG_ACT_HELP, InternalInterface.MG_ACT_ABOUT,
                                                        InternalInterface.MG_ACT_CONTEXT_MENU,
                                                        InternalInterface.MG_ACT_CTRL_HIT, InternalInterface.MG_ACT_HIT,
                                                        InternalInterface.MG_ACT_WINSIZE,
                                                        InternalInterface.MG_ACT_WINMOVE,
                                                        InternalInterface.MG_ACT_WEB_ON_DBLICK,
                                                        InternalInterface.MG_ACT_WEB_CLICK,
                                                        InternalInterface.MG_ACT_WEB_MOUSE_OUT,
                                                        InternalInterface.MG_ACT_WEB_MOUSE_OVER,
                                                        InternalInterface.MG_ACT_RT_REFRESH_RECORD,
                                                        InternalInterface.MG_ACT_RT_REFRESH_SCREEN,
                                                        InternalInterface.MG_ACT_RT_REFRESH_VIEW,
                                                        InternalInterface.MG_ACT_RT_QUIT,
                                                        InternalInterface.MG_ACT_BEGIN_DRAG,
                                                        InternalInterface.MG_ACT_BEGIN_DROP,
                                                        InternalInterface.MG_ACT_SERVER_TERMINATION,
                                                        InternalInterface.MG_ACT_SUBFORM_REFRESH,
                                                        InternalInterface.MG_ACT_TBL_REORDER,
                                                        InternalInterface.MG_ACT_EXIT_SYSTEM,
                                                        InternalInterface.MG_ACT_SUBFORM_OPEN,
                                                        InternalInterface.MG_ACT_COL_SORT,
                                                        InternalInterface.MG_ACT_CTRL_MODIFY,
                                                        InternalInterface.MG_ACT_ROLLBACK,
                                                        InternalInterface.MG_ACT_BROWSER_STS_TEXT_CHANGE,
                                                        InternalInterface.MG_ACT_EXT_EVENT,
                                                        InternalInterface.MG_ACT_COL_CLICK,
                                                        InternalInterface.MG_ACT_UPDATE_DN_CONTROL_VALUE,
                                                        InternalInterface.MG_ACT_PRESS,
                                                        InternalInterface.MG_ACT_SWITCH_TO_OFFLINE,
                                                        InternalInterface.MG_ACT_UNAVAILABLE_SERVER,
                                                        InternalInterface.MG_ACT_ENABLE_EVENTS,
                                                        InternalInterface.MG_ACT_OPEN_FORM_DESIGNER,
                                                        InternalInterface.MG_ACT_COL_FILTER,
                                                        InternalInterface.MG_ACT_NEXT_RT_WINDOW,
                                                        InternalInterface.MG_ACT_PREV_RT_WINDOW,
                                                        InternalInterface.MG_ACT_INDEX_CHANGE
                                                     };

      internal static readonly int[] actMDIFrameEnabled = new[]
                                                             {
                                                                InternalInterface.MG_ACT_CLOSE,
                                                                InternalInterface.MG_ACT_EXIT,
                                                                InternalInterface.MG_ACT_HELP,
                                                                InternalInterface.MG_ACT_WINSIZE,
                                                                InternalInterface.MG_ACT_WINMOVE,
                                                                InternalInterface.MG_ACT_EXIT_SYSTEM,
                                                                InternalInterface.MG_ACT_CONTEXT_MENU,
                                                                InternalInterface.MG_ACT_UNAVAILABLE_SERVER,
                                                                InternalInterface.MG_ACT_ENABLE_EVENTS
                                                             };

      private static readonly int[] actEditing = new[]
                                                    {
                                                       InternalInterface.MG_ACT_CHAR,
                                                       InternalInterface.MG_ACT_EDT_MARKPRVCH,
                                                       InternalInterface.MG_ACT_EDT_MARKNXTCH,
                                                       InternalInterface.MG_ACT_EDT_MARKTOBEG,
                                                       InternalInterface.MG_ACT_EDT_MARKTOEND,
                                                       InternalInterface.MG_ACT_EDT_BEGFLD,
                                                       InternalInterface.MG_ACT_EDT_ENDFLD,
                                                       InternalInterface.MG_ACT_EDT_PRVCHAR,
                                                       InternalInterface.MG_ACT_EDT_NXTCHAR,
                                                       InternalInterface.MG_ACT_EDT_PRVWORD,
                                                       InternalInterface.MG_ACT_EDT_NXTWORD,
                                                       InternalInterface.MG_ACT_EDT_DELCURCH,
                                                       InternalInterface.MG_ACT_EDT_DELPRVCH,
                                                       InternalInterface.MG_ACT_EDT_UNDO,
                                                       InternalInterface.MG_ACT_EDT_MARKALL
                                                    };

      private static readonly int[] actMLE = new[]
                                                {
                                                   InternalInterface.MG_ACT_EDT_PRVLINE,
                                                   InternalInterface.MG_ACT_EDT_NXTLINE,
                                                   InternalInterface.MG_ACT_EDT_PRVPAGE,
                                                   InternalInterface.MG_ACT_EDT_NXTPAGE,
                                                   InternalInterface.MG_ACT_EDT_BEGLINE,
                                                   InternalInterface.MG_ACT_EDT_ENDLINE,
                                                   InternalInterface.MG_ACT_EDT_BEGPAGE,
                                                   InternalInterface.MG_ACT_EDT_ENDPAGE,
                                                   InternalInterface.MG_ACT_EDT_BEGFORM,
                                                   InternalInterface.MG_ACT_EDT_ENDFORM,
                                                   InternalInterface.MG_ACT_EDT_BEGNXTLINE,
                                                   InternalInterface.MG_ACT_EDT_MARKPRVLINE
                                                };

      private static readonly int[] actRichEdit = {
                                                     InternalInterface.MG_ACT_ALIGN_LEFT,
                                                     InternalInterface.MG_ACT_ALIGN_RIGHT,
                                                     InternalInterface.MG_ACT_CENTER
                                                     , InternalInterface.MG_ACT_BULLET, InternalInterface.MG_ACT_INDENT,
                                                     InternalInterface.MG_ACT_UNINDENT,
                                                     InternalInterface.MG_ACT_CHANGE_COLOR,
                                                     InternalInterface.MG_ACT_CHANGE_FONT
                                                  };

      private static readonly int[] actTree = new[]
                                                 {
                                                    InternalInterface.MG_ACT_TREE_EXPAND,
                                                    InternalInterface.MG_ACT_TREE_COLLAPSE,
                                                    InternalInterface.MG_ACT_TREE_MOVETO_PARENT,
                                                    InternalInterface.MG_ACT_TREE_MOVETO_FIRSTCHILD,
                                                    InternalInterface.MG_ACT_TREE_MOVETO_PREVSIBLING,
                                                    InternalInterface.MG_ACT_TREE_MOVETO_NEXTSIBLING
                                                 };

      private static readonly int[] actNavigation = new[]
                                                       {
                                                          InternalInterface.MG_ACT_TBL_PRVLINE,
                                                          InternalInterface.MG_ACT_TBL_PRVPAGE,
                                                          InternalInterface.MG_ACT_TBL_BEGTBL,
                                                          InternalInterface.MG_ACT_TBL_NXTLINE,
                                                          InternalInterface.MG_ACT_TBL_NXTPAGE,
                                                          InternalInterface.MG_ACT_TBL_ENDTBL
                                                       };

      private static readonly int[] _actPaste = new[] {InternalInterface.MG_ACT_CLIP_PASTE};
      private readonly int[] _actCount; //to know which action was last enabled
      private readonly bool[] _actState; //action state array for all 526 actions
      private readonly Task _parentTask;
      private int _numerator; //generator for actCount

      /// <summary>
      ///   Constructor
      /// </summary>
      internal ActionManager(Task parent)
      {
         _parentTask = parent;
         _numerator = 0;
         _actState = new bool[InternalInterface.MG_ACT_TOT_CNT];
         _actCount = new int[InternalInterface.MG_ACT_TOT_CNT];
      }

      /// <summary>
      ///   sets action enabled or disabled
      /// </summary>
      /// <param name = "act"></param>
      /// <param name = "enable"></param>
      public void enable(int act, bool enable)
      {
         //If Zoom/Wide is enabled/disabled, update it's status on status bar.
         if (act == InternalInterface.MG_ACT_ZOOM || act == InternalInterface.MG_ACT_WIDE)
         {
            MgForm form = (MgForm)_parentTask.getForm();
            if (form != null)
            {
               if (act == InternalInterface.MG_ACT_ZOOM)
                  form.UpdateStatusBar(GuiConstants.SB_ZOOMOPTION_PANE_LAYER, 
                                       form.GetMessgaeForStatusBar(GuiConstants.SB_ZOOMOPTION_PANE_LAYER, enable), false);
               else
                  form.UpdateStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, 
                                       form.GetMessgaeForStatusBar(GuiConstants.SB_WIDEMODE_PANE_LAYER, enable), false);
            }
         }

         bool valueChanged = (_actState[act] != enable);
         _actState[act] = enable;
         if (enable)
         {
            _numerator++;
            _actCount[act] = _numerator;
         }
         if (valueChanged)
            _parentTask.enableActionMenu(act, enable);
      }

      /// <param name = "act"></param>
      /// <returns> true if the action is enabled or false if it is not</returns>
      public bool isEnabled(int act)
      {
         return (_actState[act]);
      }

      /// <param name = "act"></param>
      /// <returns> actCount for the specific action</returns>
      public int getActCount(int act)
      {
         return (_actCount[act]);
      }

      /// <summary>
      ///   sets action list enabled or disabled.
      ///   onlyIfChanged was added in order to avoid many updates to menus (like in cut/copy),
      ///   especially when the request comes as an internal act from the gui thread.
      /// </summary>
      /// <param name = "act">array of actions</param>
      /// <param name = "enable"></param>
      /// <param name = "onlyIfChanged">: call enable only if the state has changed.</param>
      public void enableList(int[] act, bool enable, bool onlyIfChanged)
      {
         for (int i = 0;
              i < act.Length;
              i++)
         {
            int iAct = act[i];

            if (!onlyIfChanged || _actState[iAct] != enable)
               this.enable(iAct, enable);
         }
      }

      /// <summary>
      ///   enable or disable actions for ACT_STT_EDT_EDITING state
      /// </summary>
      /// <param name = "enable"></param>
      public void enableEditingActions(bool enable)
      {
         enableList(actEditing, enable, false);
      }

      /// <summary>
      ///   enable or disable actions for Multi-line edit
      /// </summary>
      /// <param name = "enable"></param>
      public void enableMLEActions(bool enable)
      {
         enableList(actMLE, enable, false);
      }

      /// <summary>
      ///   enable or disable actions for rich edit
      /// </summary>
      /// <param name = "enable"></param>
      public void enableRichEditActions(bool enable)
      {
         enableList(actRichEdit, enable, false);
      }

      /// <summary>
      ///   enable or disable actions for tree
      /// </summary>
      /// <param name = "enable"></param>
      public void enableTreeActions(bool enable)
      {
         enableList(actTree, enable, false);
      }

      /// <summary>
      ///   enable or disable actions for navigation
      /// </summary>
      /// <param name = "enable"></param>
      public void enableNavigationActions(bool enable)
      {
         enableList(actNavigation, enable, false);
      }

      /// <summary>
      ///   This is the work thread method to check if to enable/disable the paste action.
      ///   It is equivalent to the GuiUtils.checkPasteEnable (used by the gui thread).
      /// </summary>
      /// <param name = "ctrl"></param>
      public void checkPasteEnable(MgControlBase ctrl)
      {
         bool enable = false;

         if (ctrl != null && ctrl.isTextOrTreeEdit() && ctrl.isModifiable() &&
             Manager.ClipboardRead() != null)
            enable = true;

         enableList(_actPaste, enable, true);
      }
   }
}
