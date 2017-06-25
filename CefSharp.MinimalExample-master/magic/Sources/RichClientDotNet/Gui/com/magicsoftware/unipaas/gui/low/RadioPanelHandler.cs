using System;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   Handler for composite control of tab's item
   /// </summary>
   /// <author>  rinav </author>
   internal class RadioPanelHandler : HandlerBase, ICloneable
   {
      private static RadioPanelHandler _instance;
      private RadioPanelHandler()
      {
      }

      #region ICloneable Members

      public Object Clone()
      {
         throw new Exception("CloneNotSupportedException");
      }

      #endregion

      internal static RadioPanelHandler getInstance()
      {
         if (_instance == null)
            _instance = new RadioPanelHandler();
         return _instance;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="panel"></param>
      internal void addHandler(Panel panel)
      {
#if !PocketPC
         panel.MouseHover += MouseHoverHandler;
         panel.MouseMove += MouseMoveHandler;
         panel.MouseLeave += MouseLeaveHandler;
#endif
         panel.Disposed += DisposedHandler;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }
   }
}