using System;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   internal class StatusHandler : HandlerBase
   {
      private static StatusHandler _instance;
      internal static StatusHandler getInstance()
      {
         if (_instance == null)
            _instance = new StatusHandler();
         return _instance;
      }

      private StatusHandler()
      {
      }

      /// <summary> 
      /// adds events for control
      /// </summary>
      /// <param name="control"> </param>
      internal void addHandler(Control control)
      {
         control.Disposed += DisposedHandler;
      }

      /// <summary> </summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }
   }

   /// <summary>
   /// 
   /// </summary>
   internal class StatusPaneHandler : HandlerBase
   {
      private static StatusPaneHandler _instance;
      internal static StatusPaneHandler getInstance()
      {
         if (_instance == null)
            _instance = new StatusPaneHandler();
         return _instance;
      }

      private StatusPaneHandler()
      {
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="toolStripItem"></param>
      internal void addHandler(ToolStripItem toolStripItem)
      {
         toolStripItem.Disposed += DisposedHandler;
      }

      /// <summary> </summary>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         if (type == EventType.DISPOSED)
         {
            ControlsMap controlsMap = ControlsMap.getInstance();
            ToolStripItem toolStripItem = (ToolStripItem)sender;
            MapData mapData = controlsMap.getMapData(sender);
            if (mapData != null)
            {
               GuiMgControl guiMgControl = mapData.getControl();
               toolStripItem.Tag = null;
               controlsMap.remove(guiMgControl, mapData.getIdx());
            }
         }
      }
   }
}
