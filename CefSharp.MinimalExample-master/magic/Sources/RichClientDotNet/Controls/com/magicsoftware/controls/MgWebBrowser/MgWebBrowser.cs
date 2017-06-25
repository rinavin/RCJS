using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using System.Drawing;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// This class inherits WebBrowser control and adds the ability to execute host methods through
   /// external events
   /// </summary>
#if !PocketPC
   [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
   [ToolboxBitmap(typeof(WebBrowser))]
#endif
   [ComVisible(true)]
   public class MgWebBrowser : WebBrowser, IHostMethods, ISetSpecificControlPropertiesForFormDesigner
   {
      public MgWebBrowser()
         : base()
      {
#if !PocketPC
         ObjectForScripting = this as IHostMethods;
#endif
      }

      [ComVisible(true)]
      public delegate void ExternalEventHandler(object sender, ExternalEventArgs e);

      public event ExternalEventHandler ExternalEvent;

      /// <summary>
      /// Raises the com.magicsoftware.controls.ExternalEvent event.
      /// </summary>
      /// <param name="e">A com.magicsoftware.controls.ExternalEventArgs that contains the event data.</param>
      protected void OnExternalEvent(ExternalEventArgs e)
      {
         if (ExternalEvent != null)
            ExternalEvent(this, e);
      }

      #region IHostMethods Members

      /// <summary>
      /// Raises an ExternalEvent event. This method should be called by a script in the HTML page
      /// by calling window.external.MGExternalEvent(string param)
      /// </summary>
      /// <param name="param">The string parameter to be passed to the event</param>
      public void MGExternalEvent(string param)
      {
         ExternalEventArgs e = new ExternalEventArgs();

         e.Param = param;
         OnExternalEvent(e);
      }

      #endregion

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fromControl"></param>
      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         this.Url = ((MgWebBrowser)fromControl).Url;
      }
   }

   /// <summary>
   /// contains the External Event data
   /// </summary>
   public class ExternalEventArgs : EventArgs
   {
      public string Param { get; set; }
   }
}
