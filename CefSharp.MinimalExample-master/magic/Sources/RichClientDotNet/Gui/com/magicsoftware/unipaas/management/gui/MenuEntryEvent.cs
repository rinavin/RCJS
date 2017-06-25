using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.gui
{
   public class MenuEntryEvent : MenuEntry
   {
      // internal event data
      private int _internalEvent;
      public int InternalEvent
      {
         get { return _internalEvent; }
         internal set
         {
            _internalEvent = value;
            if ((_internalEvent < InternalInterface.MG_ACT_USER_ACTION_1) ||
                (_internalEvent > InternalInterface.MG_ACT_USER_ACTION_20))
               setEnabled(false, false, false);
         }
      }

      // system event data
      public KeyboardItem KbdEvent { get; internal set; } // kbdItm data
      public List<String> MainProgVars { get; internal set; } // argument from the main program
      internal String Prompt { get; set; } // prompt

      // user event data
      public int UserEvtCompIndex { get; internal set; } //Index of the component
      public int UserEvtIdx { get; internal set; } // index of the user event in the tasks user events table
      internal String UserEvtTaskId { get; set; } // ID of the task where the user event is defined
      public String DestinationContext { get; internal set; } //dest. Context

      /// <summary>
      /// 
      /// </summary>
      /// <param name="mgMenu"></param>
      internal MenuEntryEvent(MgMenu mgMenu) : base(0, mgMenu)
      {
      }

      public MgMenu getMgMenu()
      {
         return getParentMgMenu();
      }
   }
}