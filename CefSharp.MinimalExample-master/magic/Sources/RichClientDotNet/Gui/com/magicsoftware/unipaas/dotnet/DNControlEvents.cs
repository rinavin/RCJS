using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui.low;

#if PocketPC
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
#endif

namespace com.magicsoftware.unipaas.dotnet
{
   /// <summary>
   /// This class maintains a list of control events for a control-type Dotnet variable
   /// </summary>
   public static class DNControlEvents
   {
      /// <summary>
      ///   standard events for control
      ///   This is an array of  events to which we must subscribe on any control
      /// </summary>
      private static readonly Dictionary<String, HandlerBase.EventType> _standardControlEvents =
         new Dictionary<string, HandlerBase.EventType>();

#if PocketPC
      /// <summary>predefined events for control (RC mobile)
      ///  This is an array of  events to which we must subscribe on any control</summary>
      static readonly Dictionary<String, Delegate> _predefinedControlEvents = new Dictionary<string, Delegate>();
#endif

      static DNControlEvents()
      {
         //initialize standard events
         _standardControlEvents.Add("GotFocus", HandlerBase.EventType.GOT_FOCUS);
         _standardControlEvents.Add("MouseDown", HandlerBase.EventType.MOUSE_DOWN);
         _standardControlEvents.Add("MouseUp", HandlerBase.EventType.MOUSE_UP);
         //keyDown will be handled through filter
         //_standardControlEvents.Add("PreviewKeyDown", (PreviewKeyDownEventHandler)DotNetHandler.getInstance().PreviewKeyDownHandler);
         //_standardControlEvents.Add("KeyDown", (KeyEventHandler)DotNetHandler.getInstance().KeyDownHandler);
         _standardControlEvents.Add("Disposed", HandlerBase.EventType.DISPOSED);

#if !PocketPC
         _standardControlEvents.Add("MouseLeave", HandlerBase.EventType.MOUSE_LEAVE);
         _standardControlEvents.Add("MouseEnter", HandlerBase.EventType.MOUSE_ENTER);
         _standardControlEvents.Add("MouseMove", HandlerBase.EventType.MOUSE_MOVE);
         _standardControlEvents.Add("MouseWheel", HandlerBase.EventType.MOUSE_WHEEL);
         _standardControlEvents.Add("DragOver", HandlerBase.EventType.DRAG_OVER);
         _standardControlEvents.Add("DragDrop", HandlerBase.EventType.DRAG_DROP);
#else
         //initialize predefined events
         // standard events
         _predefinedControlEvents.Add("MouseLeave", (EventHandler)DotNetHandler.getInstance().handleMouseLeaveEvent);
         _predefinedControlEvents.Add("MouseEnter", (EventHandler)DotNetHandler.getInstance().handleMouseEnterEvent);
         _predefinedControlEvents.Add("MouseMove", (MouseEventHandler)DotNetHandler.getInstance().handleMouseMoveEvent);
         _predefinedControlEvents.Add("MouseWheel", (EventHandler)DotNetHandler.getInstance().handleMouseWheelEvent);
         _predefinedControlEvents.Add("MouseDown", (MouseEventHandler)DotNetHandler.getInstance().handleMouseDownEvent);
         _predefinedControlEvents.Add("MouseUp", (MouseEventHandler)DotNetHandler.getInstance().handleMouseUpEvent);
         _predefinedControlEvents.Add("Disposed", (EventHandler)DotNetHandler.getInstance().handleDisposedEvent);

         //Common events
         _predefinedControlEvents.Add("ValueChanged", (EventHandler)DotNetHandler.getInstance().handleValueChangedEvent);
         _predefinedControlEvents.Add("GotFocus", (EventHandler)DotNetHandler.getInstance().handleGotFocusEvent);
         _predefinedControlEvents.Add("LostFocus", (EventHandler)DotNetHandler.getInstance().handleLostFocusEvent);
         _predefinedControlEvents.Add("KeyDown", (KeyEventHandler)DotNetHandler.getInstance().handleKeyDownEvent);
         _predefinedControlEvents.Add("KeyPress", (KeyPressEventHandler)DotNetHandler.getInstance().handleKeyPressEvent);
         _predefinedControlEvents.Add("KeyUp", (KeyEventHandler)DotNetHandler.getInstance().handleKeyUpEvent);
         _predefinedControlEvents.Add("Validated", (EventHandler)DotNetHandler.getInstance().handleValidatedEvent);
         _predefinedControlEvents.Add("Validating", (CancelEventHandler)DotNetHandler.getInstance().handleValidatingEvent);
         _predefinedControlEvents.Add("EnabledChanged", (EventHandler)DotNetHandler.getInstance().handleEnabledChangedEvent);
         _predefinedControlEvents.Add("ParentChanged", (EventHandler)DotNetHandler.getInstance().handleParentChangedEvent);

         //Form events
         _predefinedControlEvents.Add("Load", (EventHandler)DotNetHandler.getInstance().handleLoadEvent);

         //MonthCalender events
         _predefinedControlEvents.Add("DateChanged", (DateRangeEventHandler)DotNetHandler.getInstance().handleDateChangedEvent);

         //RadioButton events
         _predefinedControlEvents.Add("Click", (EventHandler)DotNetHandler.getInstance().handleClickEvent);
         _predefinedControlEvents.Add("CheckedChanged", (EventHandler)DotNetHandler.getInstance().handleCheckedChangedEvent);
         _predefinedControlEvents.Add("TextChanged", (EventHandler)DotNetHandler.getInstance().handleTextChangedEvent);

         //TreeView events
         _predefinedControlEvents.Add("AfterCheck", (TreeViewEventHandler)DotNetHandler.getInstance().handleAfterCheckEvent);
         _predefinedControlEvents.Add("AfterCollapse", (TreeViewEventHandler)DotNetHandler.getInstance().handleAfterCollapseEvent);
         _predefinedControlEvents.Add("AfterExpand", (TreeViewEventHandler)DotNetHandler.getInstance().handleAfterExpandEvent);
         _predefinedControlEvents.Add("AfterSelect", (TreeViewEventHandler)DotNetHandler.getInstance().handleAfterSelectEvent);
         _predefinedControlEvents.Add("BeforeCollapse", (TreeViewCancelEventHandler)DotNetHandler.getInstance().handleBeforeCollapseEvent);
         _predefinedControlEvents.Add("BeforeExpand", (TreeViewCancelEventHandler)DotNetHandler.getInstance().handleBeforeExpandEvent);

         //Splitter events
         _predefinedControlEvents.Add("Paint", (PaintEventHandler)DotNetHandler.getInstance().handlePaintEvent);
         _predefinedControlEvents.Add("Resize", (EventHandler)DotNetHandler.getInstance().handleResizeEvent);
         _predefinedControlEvents.Add("HelpRequested", (HelpEventHandler)DotNetHandler.getInstance().handleHelpRequestedEvent);

         //ListView events
         _predefinedControlEvents.Add("ColumnClick", (ColumnClickEventHandler)DotNetHandler.getInstance().handleColumnClickEvent);
         _predefinedControlEvents.Add("ItemActivate", (EventHandler)DotNetHandler.getInstance().handleItemActivateEvent);
         _predefinedControlEvents.Add("ItemCheck", (ItemCheckEventHandler)DotNetHandler.getInstance().handleItemCheckEvent);
         _predefinedControlEvents.Add("SelectedIndexChanged", (EventHandler)DotNetHandler.getInstance().handleSelectedIndexChangedEvent);

         //CheckBox events
         _predefinedControlEvents.Add("CheckStateChanged", (EventHandler)DotNetHandler.getInstance().handleCheckStateChangedEvent);

         //ComboBox events
         _predefinedControlEvents.Add("DataSourceChanged", (EventHandler)DotNetHandler.getInstance().handleDataSourceChangedEvent);
         _predefinedControlEvents.Add("DisplayMemberChanged", (EventHandler)DotNetHandler.getInstance().handleDisplayMemberChangedEvent);
         _predefinedControlEvents.Add("SelectedValueChanged", (EventHandler)DotNetHandler.getInstance().handleSelectedValueChangedEvent);

         //PictureBox events
         _predefinedControlEvents.Add("DoubleClick", (EventHandler)DotNetHandler.getInstance().handleDoubleClickEvent);


         //WebBrowser events
         _predefinedControlEvents.Add("CanGoBackChanged", (EventHandler)DotNetHandler.getInstance().handleCanGoBackChangedEvent);
         _predefinedControlEvents.Add("CanGoForwardChanged", (EventHandler)DotNetHandler.getInstance().handleCanGoForwardChangedEvent);
         _predefinedControlEvents.Add("DocumentCompleted", (WebBrowserDocumentCompletedEventHandler)DotNetHandler.getInstance().handleDocumentCompletedEvent);
         _predefinedControlEvents.Add("Navigated", (WebBrowserNavigatedEventHandler)DotNetHandler.getInstance().handleNavigatedEvent);
         _predefinedControlEvents.Add("Navigating", (WebBrowserNavigatingEventHandler)DotNetHandler.getInstance().handleNavigatingEvent);

         // Button, TextBox, Label, LinkLabel, ListBox don't have any more specific events

#endif
      }

      /// <summary>
      ///   return standard .NET events
      /// </summary>
      /// <returns></returns>
      internal static ICollection<String> getStandardEventsNames()
      {
         return _standardControlEvents.Keys;
      }

      /// <summary>
      ///   is this a standard event
      /// </summary>
      /// <param name = "eventName"></param>
      /// <returns></returns>
      internal static bool isStandardEvent(string eventName)
      {
         return _standardControlEvents.ContainsKey(eventName);
      }

      /// <summary>
      ///   return list of standard events
      /// </summary>
      /// <param name = "eventName"></param>
      /// <returns></returns>
      internal static HandlerBase.EventType getStandardEvent(string eventName)
      {
         HandlerBase.EventType eventHandlerType = HandlerBase.EventType.NONE;
         _standardControlEvents.TryGetValue(eventName, out eventHandlerType);
         return eventHandlerType;
      }

#if PocketPC
      /// <summary>is this a predefined event</summary>
      /// <param name="eventName"></param>
      /// <returns></returns>
      internal static bool isPredefinedEvent(string eventName)
      {
         return _predefinedControlEvents.ContainsKey(eventName);
      }

      /// <summary>System.Reflection.Emit isn't supported in the CF;
      /// temporarily, a predefined list of events will be supported.</summary>
      /// <param name="eventName">a predefined event name</param>
      /// <returns></returns>
      internal static Delegate getPredefinedEvent(string eventName)
      {
         Delegate retDelegate = null;

         foreach (var item in _predefinedControlEvents)
         {
            if (item.Key == eventName)
            {
               retDelegate = item.Value;
               break;
            }
         }

         Debug.Assert(retDelegate != null);
         return retDelegate;
      }
#endif
   }
}
