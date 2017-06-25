using com.magicsoftware.controls;
using System;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// 
   /// </summary>
   internal class TabHandler : HandlerBase
   {
      private static TabHandler _instance;
      internal static TabHandler getInstance()
      {
         if (_instance == null)
            _instance = new TabHandler();
         return _instance;
      }

      private TabHandler()
      {
      }

      internal Object Clone()
      {
         throw new Exception("CloneNotSupportedException");
      }

      /// <summary> add handler for the text
      /// 
      /// </summary>
      /// <param name="text">text
      /// </param>

      internal void addHandler(MgTabControl Tab)
      {
#if !PocketPC
            Tab.MouseMove += MouseMoveHandler;
            Tab.MouseEnter += MouseEnterHandler;
            Tab.MouseHover += MouseHoverHandler;
            Tab.MouseLeave += MouseLeaveHandler;
            Tab.MouseDoubleClick += MouseDoubleClickHandler;
            Tab.MouseWheel += MouseWheelHandler;
            Tab.PreviewKeyDown += PreviewKeyDownHandler;
            Tab.Layout += LayoutHandler;
            Tab.Selecting += SelectingHandler;
            Tab.MouseDown += MouseDownHandler;
            Tab.MnemonicKeyPressed += MnemonicKeyPressedHandler;
#else
         Tab.EnabledChanged += EnabledChangedHandler;
#endif
         Tab.GotFocus += GotFocusHandler;
         Tab.LostFocus += LostFocusHandler;
         Tab.KeyDown += KeyDownHandler;
         Tab.KeyPress += KeyPressHandler;
         Tab.Disposed += DisposedHandler;
      }

      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         Control control = (Control)sender;
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapData = controlsMap.getMapData(control);
         if (mapData == null)
            return;

         GuiMgControl ctrl = mapData.getControl();

         switch (type)
         {
#if PocketPC
            case EventType.ENABLED_CHANGED:
               ((TagData)control.Tag).TabControlPanel.Invalidate();
               break;

#else
              case EventType.SELECTING:
                 ((TabControlCancelEventArgs)e).Cancel = true;
                 ((TagData)control.Tag).SelectingIdx = ((TabControl)control).SelectedIndex;
                 GuiUtils.setSuggestedValueOfChoiceControlOnTagData(control, "" + ((TabControl)control).SelectedIndex);
                 return;

            case EventType.MNEMONIC_KEY_PRESSED:
               Events.OnSelection(((MnemonicKeyPressedEventArgs)e).SelectedTabIndex.ToString(), ctrl, 0, false);
               return;

#endif
            case EventType.DISPOSED:
               if (control.Tag != null)
                  ((TagData)control.Tag).TabControlPanel.Dispose();
               return;
         }

         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }
   }
}
