using System;
using System.Windows.Forms;
using com.magicsoftware.controls;
using System.Drawing;

namespace com.magicsoftware.unipaas.gui.low
{

   /// <summary> 
   /// Radio Button handler
   /// </summary>
   /// <author>  rinav</author>
   internal class RadioButtonHandler : HandlerBase
   {
      // singleton
      private static RadioButtonHandler _instance;
      internal static RadioButtonHandler getInstance()
      {
         if (_instance == null)
            _instance = new RadioButtonHandler();
         return _instance;
      }
      private RadioButtonHandler() {}

      /// <summary>
      /// 
      /// </summary>
      /// <param name="radioButton"></param>
      internal void addHandler(MgRadioButton radioButton)
      {
         radioButton.GotFocus          += GotFocusHandler;
         //radioButton.Click += ClickHandler;
         radioButton.KeyDown           += KeyDownHandler;
         radioButton.KeyPress          += KeyPressHandler;
#if !PocketPC
         radioButton.MouseDown         += MouseDownHandler;
         radioButton.MouseUp           += MouseUpHandler;
         radioButton.MouseMove         += MouseMoveHandler;
         radioButton.MouseEnter        += MouseEnterHandler;
         radioButton.MouseHover        += MouseHoverHandler;
         radioButton.MouseLeave        += MouseLeaveHandler;
         radioButton.MouseWheel        += MouseWheelHandler;
         radioButton.MouseDoubleClick  += MouseDoubleClickHandler;
         radioButton.PreviewKeyDown    += PreviewKeyDownHandler;
         radioButton.DragOver          += DragOverHandler;
         radioButton.DragDrop          += DragDropHandler;
         radioButton.GiveFeedback      += GiveFeedBackHandler;
#endif
         radioButton.Disposed          += DisposedHandler;
      }

      /// <summary> </summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         Control control = (Control)sender;
         RadioButton radioButton = (RadioButton)sender;
         MgRadioPanel mgRadioPanel = (MgRadioPanel)radioButton.Parent;
         MapData mapData = controlsMap.getMapData(mgRadioPanel);
         if (mapData == null)
            return;

         GuiMgControl guiMgControl = mapData.getControl();
         Type senderType = sender.GetType();
         bool leftClickWasPressed = false;

         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgControl));
         try
         {
            switch (type)
            {
               case EventType.MOUSE_DOWN:
                  //fixed bug #435168 , saveing the widget that we made MouseDown on the comosite control             
                  ((TagData)mgRadioPanel.Tag).MouseDownOnControl = radioButton;
                  GuiUtils.checkAndCloseTreeEditorOnClick(control);
                  MouseEventArgs mouseEvtArgs = (MouseEventArgs)e;
                  GuiUtils.SaveLastClickInfo(mapData, (Control)sender, new Point(mouseEvtArgs.X, mouseEvtArgs.Y));
#if !PocketPC //tmp
                  GuiUtils.setTooltip(control, "");
#endif
                  String Value = GuiUtils.GetRadioButtonIndex(radioButton);
                  GuiUtils.setSuggestedValueOfChoiceControlOnTagData(mgRadioPanel, Value);
                  Events.OnSelection(Value, guiMgControl, mapData.getIdx(), true);
                  //the right click isn't move the focus to the control, only on left click.
                  leftClickWasPressed = (((MouseEventArgs)e).Button == MouseButtons.Left);

                  if (leftClickWasPressed)
                  {
                     Events.OnMouseDown(null, guiMgControl, null, leftClickWasPressed, mapData.getIdx(), false, true);
#if !PocketPC
                     GuiUtils.AssessDrag(control, (MouseEventArgs)e, mapData);
#endif
                  }
                  else
                     control.Focus();
                  return;

               case EventType.KEY_DOWN:
                  ((TagData)mgRadioPanel.Tag).MouseDownOnControl = null;
                  break;
            }
         }
         finally 
         {
            contextIDGuard.Dispose();
         }

         DefaultHandler.getInstance().handleEvent(type, sender, e, mapData);
      }
   }
}
