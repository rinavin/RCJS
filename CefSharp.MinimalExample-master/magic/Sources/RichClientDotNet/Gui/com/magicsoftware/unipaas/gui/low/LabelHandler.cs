using System;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.controls;
using com.magicsoftware.util;
using com.magicsoftware.controls.utils;
#if PocketPC
using LinkLabelLinkClickedEventArgs = OpenNETCF.Windows.Forms.LinkLabel2LinkClickedEventArgs;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> Handler for label</summary>
   /// <author>  rinav </author>
   internal class LabelHandler : HandlerBase
   {
      private static LabelHandler _instance;
      internal static LabelHandler getInstance()
      {
         if (_instance == null)
            _instance = new LabelHandler();
         return _instance;
      }

      private LabelHandler()
      {
      }

      /// <summary> add handler for the list</summary>
      /// <param name="label"></param>
      internal void addHandler(Control label)
      {
         label.MouseMove += MouseMoveHandler;
#if !PocketPC //?
         label.MouseEnter += MouseEnterHandler;
         label.MouseHover += MouseHoverHandler;
         label.MouseLeave += MouseLeaveHandler;
         label.DragOver += DragOverHandler;
         label.DragDrop += DragDropHandler;
         label.GiveFeedback += GiveFeedBackHandler;
#endif
         label.Disposed += DisposedHandler;
         label.MouseDown += MouseDownHandler;
         label.MouseUp += MouseUpHandler;
         if (label is MgLinkLabel)
         {
#if !PocketPC
            label.MouseWheel += MouseWheelHandler;
            label.PreviewKeyDown += PreviewKeyDownHandler;
#endif
            ((MgLinkLabel)label).LinkClicked += LinkClicked;
            label.MouseDown += MouseDownHandler;
            label.GotFocus += GotFocusHandler;
            label.KeyDown += KeyDownHandler;
         }
      }

      /// <summary></summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="evtArgs"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs evtArgs)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         Control ctrl = (Control)sender;
         MapData mapData = controlsMap.getMapData(ctrl);
         if (mapData == null)
            return;
     
         GuiMgControl mgControl = mapData.getControl();
         MgLinkLabel linkLabel = ctrl as MgLinkLabel;

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(mgControl));
         try
         {
            if (linkLabel != null)
            {
               switch (type)
               {
                  case EventType.LINK_CLICKED:
                     LinkLabelLinkClickedEventArgs args = (LinkLabelLinkClickedEventArgs)evtArgs;
#if !PocketPC
                     if (args.Button == MouseButtons.Left)
#endif
                        // Mobile: we get here only with a left button click
                        OnLinkClicked(linkLabel, controlsMap, mapData, mgControl, true);
                     return;

                  case EventType.GOT_FOCUS:
                  case EventType.MOUSE_UP:
                     break;

                  case EventType.MOUSE_DOWN:
                     if (!linkLabel.Focused)
                        GuiUtils.saveFocusingControl(GuiUtils.FindForm(linkLabel), mapData);

                     break;

                  case EventType.MOUSE_ENTER:
                     linkLabel.OnHovering = true;
                     break;

                  case EventType.MOUSE_LEAVE:
                     linkLabel.OnHovering = false;
                     break;

                  case EventType.KEY_DOWN:
                     KeyEventArgs keyEventArgs = (KeyEventArgs)evtArgs;

                     if (KbdConvertor.isModifier(keyEventArgs.KeyCode))
                        return;

                     if (keyEventArgs.Modifiers == Keys.None && keyEventArgs.KeyCode == Keys.Space)
                     {
                        OnLinkClicked(linkLabel, controlsMap, mapData, mgControl, false);
                        return;
                     }
                     break;

                  case EventType.PRESS:
                     if(!linkLabel.Focused)
                        GuiUtils.saveFocusingControl(GuiUtils.FindForm(linkLabel), mapData);
                     break;

                  default:
                     break;

               }
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }


         DefaultHandler.getInstance().handleEvent(type, sender, evtArgs);
      }

      /// <summary>on click</summary>
      /// <param name="controlsMap"></param>
      /// <param name="ctrl"></param>
      /// <param name="mapData"></param>
      /// <param name="mgControl"></param>
      /// <param name="linkLabel"></param>
      private static void OnLinkClicked(MgLinkLabel linkLabel, ControlsMap controlsMap, MapData mapData, GuiMgControl mgControl, bool produceClick)
      {
         Events.OnSelection(GuiUtils.getValue(linkLabel), mgControl, mapData.getIdx(), produceClick);
      }
   }
}
