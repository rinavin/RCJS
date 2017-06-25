using System;
using System.Windows.Forms;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.util;
using com.magicsoftware.unipaas;
using System.Collections.Generic;

namespace com.magicsoftware.unipaas.util
{
   /// <summary> Authentication dialog: username (label, text), password (label, text), OK button, CANCEL button;</summary>
   public sealed class AuthenticationDialogHandler : DialogHandler
   {
      private UsernamePasswordCredentials _credentials = new UsernamePasswordCredentials();
      public bool ChangePasswordRequested { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      public AuthenticationDialogHandler()
         : this(null, null, null)
      {
      }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="username"></param>
      public AuthenticationDialogHandler(String username)
         : this(username, null, null)
      {
      }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="username">set as the username in the dialog.</param>
      /// <param name="windowTitle">append to the form's title.</param>
      /// <param name="groupTitle">if not null - set as the group's title inside the form.</param>
      public AuthenticationDialogHandler(String username, String windowTitle, String groupTitle)
      {
         String windowTitleStr = Events.GetMessageString(MsgInterface.STR_LOGON_CAPTION) + " - " + windowTitle;
         String msgCaption = GuiConstants.ENTER_UID_TTL;

         // prepare parameters to be sent to Authentication form constructor
         Object[] parameters = new Object[] { this, username, windowTitleStr, groupTitle, msgCaption };

         this.createDialog(typeof(AuthenticationForm), parameters);
      }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="username">last typed username</param>
      /// <param name="logonDialogExecProps">the customized logon dialog execution props</param>
      public AuthenticationDialogHandler(String username, Dictionary<string, string> logonDialogExecProps)
      {
         Object[] parameters = { this, username, logonDialogExecProps };
         this.createDialog(typeof(AuthenticationForm), parameters);
      }

      /// <summary> display a dialog; store input values in 'credentials'.</summary>
      public override DialogResult openDialog()
      {
         DialogResult result = base.openDialog();
         if (result == DialogResult.Cancel)
            _credentials = null;

         return result;
      }

      /// <summary> dispose(and close) the dialog</summary>
      public override void closeDialog()
      {
         base.closeDialog();
      }

      /// <summary> set current credentials with new credentials </summary>
      public void setCredentials(UsernamePasswordCredentials newCredentials)
      {
         _credentials.Username = newCredentials.Username;
         _credentials.Password = newCredentials.Password;
      }

      /// <returns>
      public UsernamePasswordCredentials getCredentials()
      {
         return _credentials;
      }
   }
}
