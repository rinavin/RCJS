using System;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> Class implements handlers for the text controls, this class is a singleton
   /// 
   /// </summary>
   /// <author>  rinav</author>
   internal class BrowserHandler : HandlerBase
   {
      private static BrowserHandler _instance;
      internal static BrowserHandler getInstance()
      {
         if (_instance == null)
            _instance = new BrowserHandler();
         return _instance;
      }

      private BrowserHandler()
      {
      }

      /// <summary> 
      /// add events for Browser Control
      /// </summary>
      /// <param name="webBrowser"></param>
      internal void addHandler(MgWebBrowser webBrowser)
      {
         // TODO: Not yet handled
         // SWT.KeyDown and SWT.Traverse
         webBrowser.GotFocus += GotFocusHandler;
         webBrowser.Disposed += DisposedHandler;
#if !PocketPC
         webBrowser.StatusTextChanged += StatusTextChangedHandler;
#endif
         webBrowser.ExternalEvent += ExternalEventHandler;
      }

      /// <summary>
      /// </summary>
      /// <param name="type"></param>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      internal override void handleEvent(EventType type, Object sender, EventArgs e)
      {
         ControlsMap controlsMap = ControlsMap.getInstance();
         MgWebBrowser webBroswer = (MgWebBrowser)sender;

         MapData mapData = controlsMap.getMapData(webBroswer);
         if (mapData == null)
            return;

         GuiMgControl guiMgCtrl = mapData.getControl();
         var contextIDGuard = new Manager.ContextIDGuard(Manager.GetContextID(guiMgCtrl));
         try
         {
            switch (type)
            {

               case EventType.GOT_FOCUS:
                  Events.OnFocus(guiMgCtrl, 0, false, false);
                  return;

               case EventType.DISPOSED:
                  break;
#if !PocketPC
               case EventType.STATUS_TEXT_CHANGED:
                  String statusText = webBroswer.StatusText.Trim();
                  String previousStatusText = ((TagData)webBroswer.Tag).BrowserControlStatusText;
                  if (statusText.Length > 0 && !statusText.Equals(previousStatusText))
                  {
                     ((TagData)webBroswer.Tag).BrowserControlStatusText = statusText;
                     Events.OnBrowserStatusTxtChange(guiMgCtrl, statusText);
                  }
                  break;
#endif

               case EventType.EXTERNAL_EVENT:
                  ExternalEventArgs args = (ExternalEventArgs)e;
                  Events.OnBrowserExternalEvent(guiMgCtrl, args.Param);
                  break;
            }
         }
         finally
         {
            contextIDGuard.Dispose();
         }
         DefaultHandler.getInstance().handleEvent(type, sender, e);
      }
   }
}