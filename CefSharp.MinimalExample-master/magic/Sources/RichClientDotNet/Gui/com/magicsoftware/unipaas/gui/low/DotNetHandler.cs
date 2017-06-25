using System;
using System.Collections.Generic;
using System.Windows.Forms;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.unipaas.util;
#if PocketPC
using System.ComponentModel;
using System.Diagnostics;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>This class will recieve all handled events for the .NET magic objects and handle them</summary>
   public class DotNetHandler : HandlerBase
   {
      private static DotNetHandler _instance;
      internal static DotNetHandler getInstance()
      {
         if (_instance == null)
            _instance = new DotNetHandler();
         return _instance;
      }

      private DotNetHandler()
      {
      }

      /// <summary> adds events for control</summary>
      /// <param name="control"></param>
      /// <param name="dnEventsNames">comma-delimited string of events that should be raised for the object</param>
      internal void addHandler(Control control, List<String> dnEventsNames)
      {
         DNObjectEventsCollection.ObjectEvents objectEvents = DNManager.getInstance().DNObjectEventsCollection.checkAndCreateObjectEvents(control);

         foreach (string item in dnEventsNames)
            ReflectionServices.addHandler(item, control, objectEvents, true);

         ICollection<String> standartEvents = DNControlEvents.getStandardEventsNames();
         foreach (string item in standartEvents)
            if (!dnEventsNames.Contains(item))
               ReflectionServices.addHandler(item, control, objectEvents, false);
      }

      /// <summary>this is an entry point for all objects handlers</summary>
      /// <param name="objectHashCode"> hash code of the object</param>
      /// <param name="eventName"> event name</param>
      /// <param name="parameters">event parameters</param>
      /// <returns></returns>
      public static void handleDotNetEvent(int objectHashCode, String eventName, Object[] parameters)
      {
         Object invokedObject = DNManager.getInstance().DNObjectEventsCollection.getObject(objectHashCode);
         GuiMgControl mgControl = null;
         bool raiseRuntimeEvent = true;
         Manager.ContextIDGuard contextIDGuard = null;

         // If Object is a control and it is 'our' control (not created dynamically, decided by .Tag)
         if (invokedObject is Control && ((Control)invokedObject).Tag != null)
         {
            ControlsMap controlsMap = ControlsMap.getInstance();
            MapData mapData = controlsMap.getControlMapData((Control) invokedObject);
            if (mapData == null)
               return;

            mgControl = mapData.getControl();

            contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(mgControl));
            //first execute default magic handling of event
            EventType type = DNControlEvents.getStandardEvent(eventName);
            if (type != EventType.NONE && parameters.Length == 2)
               DefaultHandler.getInstance().handleEvent(type, invokedObject, (EventArgs)parameters[1]);

            if (eventName == "KeyDown") //QCR #736734 KeyDown is handled from Filter.cs on WM_KEYDOWN - do not duplicate the event
               raiseRuntimeEvent = false;
            else if (eventName == "Disposed")
               // a Disposed event for a control can't be handled as a runtime-event after the task/form are closed
               raiseRuntimeEvent = false;
            else
            {
               // raise .NET runtime event only if hooked from the application (i.e. have a handler in the task)
               List<String> applicationHookedDNeventsNames = ((TagData)((Control)invokedObject).Tag).ApplicationHookedDNeventsNames;
               if (applicationHookedDNeventsNames == null || !applicationHookedDNeventsNames.Contains(eventName))
                  raiseRuntimeEvent = false;
            }
         }

         // raise .NET event
         if (raiseRuntimeEvent)
            Events.OnDotNetEvent(invokedObject, mgControl, eventName, parameters);

         if (contextIDGuard != null)
            contextIDGuard.Dispose();
      }

      /// <summary>handle event for standard magic internal/keyboard events</summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, object sender, EventArgs e)
      {
         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }

      /// <summary>handle dotnet event of .NET control</summary>
      /// <param name="eventName"></param>
      /// <param name="sender"></param>
      /// <param name="eventArgs"></param>
      /// <param name="mapData"></param>
      internal void handleEvent(String eventName, object sender, EventArgs eventArgs, MapData mapData)
      {
         GuiMgControl mgControl = null;
         if (mapData != null)
            mgControl = mapData.getControl();

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(mgControl));
         Events.OnDotNetEvent(sender, mgControl, eventName, new Object[] { sender, eventArgs });
         contextIDGuard.Dispose();
      }

      /// <summary>
      /// This function handles DN control value changed event.
      /// </summary>
      /// <param name="objectHashCode">object hash code.</param>
      public static void HandleDNControlValueChanged(int objectHashCode)
      {
         Object invokedObject = DNManager.getInstance().DNObjectEventsCollection.getObject(objectHashCode);
         
         // Get the gui control from the control's map.
         if (invokedObject is Control && ((Control)invokedObject).Tag != null)
         {
            ControlsMap controlsMap = ControlsMap.getInstance();
            MapData mapData = controlsMap.getControlMapData((Control)invokedObject);
            if (mapData != null)
               //Raise the event.
               Events.OnDNControlValueChanged(mapData.getControl(), mapData.getIdx());
         }
      }

#if PocketPC
      //Standard events  
      internal void handleMouseLeaveEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("MouseLeave", sender, e);
      }
      internal void handleMouseEnterEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("MouseEnter", sender, e);
      }
      internal void handleMouseMoveEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("MouseMove", sender, e);
      }
      internal void handleMouseWheelEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("MouseWheel", sender, e);
      }
      internal void handleMouseDownEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("MouseDown", sender, e);
      }
      internal void handleMouseUpEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("MouseUp", sender, e);
      }
      internal void handleDisposedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("Disposed", sender, e);
      }

      //Common event handler
      internal void handleValueChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("ValueChanged", sender, e);
      }
      internal void handleGotFocusEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("GotFocus", sender, e);
      }
      internal void handleLostFocusEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("LostFocus", sender, e);
      }
      internal void handleKeyDownEvent(object sender, KeyEventArgs e)
      {
         handlePredefinedEvent("KeyDown", sender, e);
      }
      internal void handleKeyPressEvent(object sender, KeyPressEventArgs e)
      {
         handlePredefinedEvent("KeyPress", sender, e);
      }
      internal void handleKeyUpEvent(object sender, KeyEventArgs e)
      {
         handlePredefinedEvent("KeyUp", sender, e);
      }
      internal void handleValidatedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("Validated", sender, e);
      }
      internal void handleValidatingEvent(object sender, CancelEventArgs e)
      {
         handlePredefinedEvent("Validating", sender, e);
      }
      internal void handleEnabledChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("EnabledChanged", sender, e);
      }

      // Parent Changed Event will be supported in future.
      internal void handleParentChangedEvent(object sender, EventArgs e)
      {
         Events.WriteErrorToLog("ParentChanged Event - Not Implemented Yet");

         //handlePredefinedEvent("ParentChanged", sender, e);
      }

      // Form event handler
      internal void handleLoadEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("Load", sender, e);
      }

      // MonthCalender event handler
      internal void handleDateChangedEvent(object sender, DateRangeEventArgs e)
      {
         handlePredefinedEvent("DateChanged", sender, e);
      }

      // RadioButton event handlers
      internal void handleClickEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("Click", sender, e);
      }
      internal void handleCheckedChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("CheckedChanged", sender, e);
      }
      internal void handleTextChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("TextChanged", sender, e);
      }

      // Treeview event handlers
      internal void handleAfterCheckEvent(object sender, TreeViewEventArgs e)
      {
         handlePredefinedEvent("AfterCheck", sender, e);
      }
      internal void handleAfterCollapseEvent(object sender, TreeViewEventArgs e)
      {
         handlePredefinedEvent("AfterCollapse", sender, e);
      }
      internal void handleAfterExpandEvent(object sender, TreeViewEventArgs e)
      {
         handlePredefinedEvent("AfterExpand", sender, e);
      }
      internal void handleAfterSelectEvent(object sender, TreeViewEventArgs e)
      {
         handlePredefinedEvent("AfterSelect", sender, e);
      }
      internal void handleBeforeCollapseEvent(object sender, TreeViewCancelEventArgs e)
      {
         handlePredefinedEvent("BeforeCollapse", sender, e);
      }
      internal void handleBeforeExpandEvent(object sender, TreeViewCancelEventArgs e)
      {
         handlePredefinedEvent("BeforeExpand", sender, e);
      }

      // Splitter control event handlers
      internal void handlePaintEvent(object sender, PaintEventArgs e)
      {
         handlePredefinedEvent("Paint", sender, e);
      }
      internal void handleResizeEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("Resize", sender, e);
      }
      internal void handleHelpRequestedEvent(object sender, HelpEventArgs e)
      {
         handlePredefinedEvent("HelpRequested", sender, e);
      }

      // Listview event handlers
      internal void handleColumnClickEvent(object sender, ColumnClickEventArgs e)
      {
         handlePredefinedEvent("ColumnClick", sender, e);
      }
      internal void handleItemActivateEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("ItemActivate", sender, e);
      }
      internal void handleItemCheckEvent(object sender, ItemCheckEventArgs e)
      {
         handlePredefinedEvent("ItemCheck", sender, e);
      }
      internal void handleSelectedIndexChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("SelectedIndexChanged", sender, e);
      }

      //CheckBox events handlers
      internal void handleCheckStateChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("CheckStateChanged", sender, e);
      }

      //ComboBox events handlers
      internal void handleDataSourceChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("DataSourceChanged", sender, e);
      }
      internal void handleDisplayMemberChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("DisplayMemberChanged", sender, e);
      }
      internal void handleSelectedValueChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("SelectedValueChanged", sender, e);
      }

      //PictureBox events handlers
      internal void handleDoubleClickEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("DoubleClick", sender, e);
      }

      //WebBrowser events handlers
      internal void handleCanGoBackChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("CanGoBackChanged", sender, e);
      }
      internal void handleCanGoForwardChangedEvent(object sender, EventArgs e)
      {
         handlePredefinedEvent("CanGoForwardChanged", sender, e);
      }
      internal void handleDocumentCompletedEvent(object sender, WebBrowserDocumentCompletedEventArgs e)
      {
         handlePredefinedEvent("DocumentCompleted", sender, e);
      }
      internal void handleNavigatedEvent(object sender, WebBrowserNavigatedEventArgs e)
      {
         handlePredefinedEvent("Navigated", sender, e);
      }
      internal void handleNavigatingEvent(object sender, WebBrowserNavigatingEventArgs e)
      {
         handlePredefinedEvent("Navigating", sender, e);
      }
      private void handlePredefinedEvent(string eventName, object sender, EventArgs e)
      {
         GuiMgControl guiMgCtrl = null;
         Debug.Assert(sender is Control && DNControlEvents.isPredefinedEvent(eventName));
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData = controlsMap.getMapData(sender);
         if (mapData != null)
         guiMgCtrl = mapData.getControl();
         Events.OnDotNetEvent(sender, guiMgCtrl, eventName, new[] {sender, e});
      }
      #endif
   }
}
