using System;
using System.Windows.Forms;

namespace com.magicsoftware.richclient.http.client
{
   internal partial class ApplicationUpdateForm : Form
   {
      /// <summary>
      /// </summary>
      /// <param name="serverName"></param>
      /// <param name="applicationName"></param>
      /// <param name="currentVersion"></param>
      /// <param name="newVersion"></param>
      public ApplicationUpdateForm(String serverName, String applicationName, String currentVersion, String newVersion)
      {
         InitializeComponent();
         _serverName.Text = serverName;
         _applicationName.Text = applicationName;
         _fromVersion.Text = currentVersion;
         _toVersion.Text = newVersion;
      }
   }
}