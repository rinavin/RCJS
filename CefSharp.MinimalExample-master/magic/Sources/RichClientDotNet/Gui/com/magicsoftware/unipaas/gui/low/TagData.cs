using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.editors;
using System.Collections.Generic;
using com.magicsoftware.util;
using Controls.com.magicsoftware;
#if !PocketPC
using System.Collections;
#else
using Panel = com.magicsoftware.controls.MgPanel;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   class TagData
   {
#if !PocketPC
      internal ArrayList TableControls { get; set; }
      internal int SelectingIdx { get; set; }
#endif
      internal Rectangle? Bounds { get; set; }          // BOUNDS_KEY           = "BOUNDS_KEY";
      internal Image Image { get; set; }
      internal CtrlImageStyle ImageStyle { get; set; }  // IMAGE_STYLE_KEY = "IMAGE_Style_Key";
      internal MinSizeInfo MinSizeInfo { get; set; }    // MININUM_SIZE_INFO_KEY = "MININUM_SIZE_INFO_KEY";
#if PocketPC
      internal ToolBar toolBar { get; set; }   
#else
      internal ToolStrip toolBar { get; set; } 
      internal TableManager TableManager
      {
         get
         {
            return (TableManager)ContainerManager;
         }
      }
      internal TreeManager TreeManager
      {
         get
         {
            return (TreeManager)ContainerManager;
         }
      }
#endif
      internal ContainerManager ContainerManager { get; set; }
      internal LgColumn ColumnManager { get; set; } // COLUMN_MANAGER_KEY = "COLUMN_MANAGER_KEY";
      internal MgSplitContainerData MgSplitContainerData { get; set; }//for splitter
      internal EditorSupportingPlacementLayout PlacementLayout { get; set; }
      internal Boolean Visible { get; set; }            // VISIBLE_KEY = "VISIBLE_KEY";
      internal Boolean Enabled { get; set; }                  
      internal int ImeMode { get; set; } // IME_MODE_KEY = "IME_MODE_KEY";
      internal string ListControlOriginalValue { get; set; } // COMBO_BOX_ORGINAL_VALUE = "COMBO_BOX_ORGINAL_VALUE";
      internal Boolean ContextCanOpen { get; set; } // MENU_BY_CLICK = "MENU_BY_CLICK";
      internal MenuStyle MenuStyle { get; set; } // MGMENU_STYLE_KEY = "MGMENU_STYLE_Key";
      internal GuiMgMenu GuiMgMenu { get; set; } // menu
      internal GuiMenuEntry guiMenuEntry { get; set; } // menuEntry
      internal bool CheckAutoWide { get; set; } // CHECK_AUTO_WIDE_KEY = "CHECK_AUTO_WIDE_KEY";
      internal WindowType WindowType { get; set; } // WINDOW_TYPE_KEY = "WINDOW_TYPE_Key";
      internal string Tooltip { get; set; } // TOOLTIP_KEY = "TOOLTIP_Key";
      internal Object LastBounds { get; set; } // LAST_BOUNDS = "LAST_BOUNDS";
      internal MapData MapData { get; set; }                // Mapdata
      internal Control StatusBarControl { get; set; } // STATUS_BAR_WIDGET_KEY = "STATUS_BAR_WIDGET_KEY";
      internal Form ActiveChildWindow { get; set; } // the active window with WindowType=Child.
      internal MapData InFocusControl { get; set; } // IN_FOCUS_CONTROL = "IN_FOCUS_CONTROL";
      private Control _lastFocusedControl_DoNotUseDirectly;

      //====================================================================================================================
      //                                        Fixed defect #81498: 
      //==========================
      //Some explanation:
      //==========================
      //                 we get a lot of  EventType.SELECTED_INDEX_CHANGED, on comboHandler.cs
      //                 we need to know if it is real or we need to ignore, so Current mechanism 
      //                 in GuiCommandQueue::setSelection() we check the prev value on the 
      //                 MgControlBase(that send to the GuiCommandQueue) and the Control.SelectedIndex
      //                 on the control: If those two values are not identical then we not set the selection to the comboBox
      //==========================
      //Related to the defect: 
      //==========================
      //                 while we on "SetToDefaultValue" in OL , we set all control to be as default,
      //                 and all properties are refresh, and also the property ItemList, in GuiCommandQueue.SetItemsList() ->
      //                 set the items to be with one item and the Control.SelectedIndex is set to -1: 
      //                 And the value on the gui and the value on the control are not identical so next select isn't set.
      //==========================
      //The solution: 
      //==========================
      //                1. On mgcontrolBase save flag :"InSetToDefaultValue", and send this value to the command PROP_SET_ITEMS_LIST,

      //                2. in command SetItemsList if we on "know" situation (:"InSetToDefaultValue" = true ) that we will check if the
      //                   selectedIndex was set to new value and set TagData.WorkerThreadAndGuiThreadValueAreNotIdentical  = true;

      //                3. in command PROP_SET_SELECTION, in method GuiCommandQueue:setSelection() , check if this flag is true
      //                   then force set although Worker thread value and Gui thread value are not identical  
      public bool  WorkerThreadAndGuiThreadValueAreNotIdentical { get; set; }
      //====================================================================================================================

      internal Control LastFocusedControl 
      { 
         get { return _lastFocusedControl_DoNotUseDirectly; }
         set
         {
            _lastFocusedControl_DoNotUseDirectly = value;
#if !PocketPC
            Form form = GuiUtils.FindForm(value);
            //Since the focus moves to a control of this window, child window is no more active.
            ((TagData) form.Tag).ActiveChildWindow = null;

            //If the current form is a child window, set it as the active child form of 
            //the non-child parent form.
            if (((TagData)form.Tag).WindowType == WindowType.ChildWindow)
            {
               Form parentForm = form.ParentForm;
               while (((TagData)parentForm.Tag).WindowType == WindowType.ChildWindow)
                  parentForm = parentForm.ParentForm;

               ((TagData)parentForm.Tag).ActiveChildWindow = form;
            }
#endif
         }
      } // LAST_FOCUSED_WIDGET = "LAST_FOCUSED_WIDGET";
      internal MapData LastFocusedMapData { get; set; }                // Mapdata
      internal Control MouseDownOnControl { get; set; } // for radio button group MOUSE_DOWN_PRESS_ON_CONTROL
      internal Boolean IgnoreClick { get; set; }// for ignore the first selection IGNORE_SELECTION_KEY
      internal Boolean IgnoreTwiceClick { get; set; }// for ignore the first selection IGNORE_SELECTION_KEY
      internal String  IgnoreTwiceClickWhenFromValueIs { get; set; }// for ignore the first selection IGNORE_SELECTION_KEY
      internal String IgnoreTwiceClickWhenToValueIs { get; set; }// for ignore the first selection IGNORE_SELECTION_KEY

      internal Boolean ClickOnComboDropDownList { get; set; }
      internal Boolean HandleOnDropDownClosed{ get; set; }

      internal GuiForm MDIClientForm { get; set; } // Main program's form containing controls
      internal Boolean IsMDIClientForm { get; set; } // Main program's form containing controls
      internal Boolean IsClientPanel { get; set; } //client panel of form
      internal Boolean IsInnerPanel { get; set; } //subform or client panel of form 
      internal Control ClientPanel { get; set; }
      internal Panel TabControlPanel { get; set; }
      internal MgTabControl ContainerTabControl { get; set; }
      internal String BrowserControlStatusText { get; set; }

      //for MgSplitContainer
      internal Rectangle? RepaintRect { get; set; } // SPLIT_CONTAINER_REPAINT_RECT_KEY = "SPLIT_CONTAINER_REPAINT_RECT_KEY";
      internal Rectangle? Splitter { get; set; } // SPLITTER_REPAINT_RECT_KEY = "SPLITTER_REPAINT_RECT_KEY";
      internal bool IgnoreKeyDown { get; set; } // KEYDOWN_IGNORE_KEY = "KEYDOWN_IGNORE_key";
      internal bool IgnoreKeyPress { get; set; } // for keys posted by richedit control (char keys for key press event).
      internal Editor TmpEditor { get; set; }
      internal bool IsEditor { get; set; }
      internal PlacementData PlacementData { get; set; }

      //for Image Button
      internal bool OnHovering { get; set; }

      internal bool OnMouseDown { get; set; }

      public String SuggestedValueOfChoiceControl { get; set; }
      public bool GetSuggestedValueOfChoiceControl { get; set; }

      internal MapData LastMouseOver { get; set; }

      internal bool DrawAsDefaultButton { get; set; }

      internal bool IsDotNetControl { get; set; }
      internal List<String> ApplicationHookedDNeventsNames { get; set; }
      internal bool Minimized { get; set; }
      internal bool IsShown { get; set; }
      internal bool ContainsSubFormOrFrame { get; set; }

      // Allow Drag & Drop : will be used to check whether the drag/drop is allowed on control or not.
      internal bool AllowDrop { get; set; }
      internal bool AllowDrag { get; set; }
      internal Rectangle DragBoxFromMouseDown { get; set; }

      internal Rectangle DeskTopBounds { get; set; }
      internal FormWindowState LastWindowState { get; set; }
      internal int VisibleLines { get; set; }
      internal Boolean ShowTitleBar { get; set; }
      internal Boolean ShowSystemMenu { get; set; }
      internal String TextToDisplay { get; set; }
      internal Boolean IgnoreWindowResizeAndMove { get; set; }

      internal string RtfValueBeforeEnteringControl { get; set; }

      // save info for Focus color
      internal Color? OrgFocusBGColor { get; set; }
      internal Color? OrgFocusFGColor { get; set; }
      internal Color? FocusBGColor { get; set; }
      internal Color? FocusFGColor { get; set; }
      internal Boolean HasCaret { get; set; }

      /// <summary>
      /// 
      /// </summary>
      internal TagData()
      {
         Bounds = null;
         PlacementData = new PlacementData(new Rectangle()); //Placement Data should always be initialized, since RTL placement occurs even if no placement is set on the control.
         ShowTitleBar = true;
         ShowSystemMenu = true;
         ApplicationHookedDNeventsNames = null;
         HasCaret = true;
#if !PocketPC
         TableControls = new ArrayList();
         SelectingIdx = Int32.MinValue;
#endif
      }
   }
}
