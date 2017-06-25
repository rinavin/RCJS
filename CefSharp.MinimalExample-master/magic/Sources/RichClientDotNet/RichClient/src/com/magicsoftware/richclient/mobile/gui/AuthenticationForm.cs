using System;
using System.Windows.Forms;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;

namespace com.magicsoftware.unipaas.util
{
   internal partial class AuthenticationForm : Form
   {
      private readonly AuthenticationDialogHandler _owner;

      /// <summary>
      /// </summary>
      /// <param name="owner"></param>
      /// <param name="username"></param>
      /// <param name="dialogCaption"></param>
      /// <param name="dialogSubCaption"></param>
      /// <param name="instructions"></param>
      public AuthenticationForm(AuthenticationDialogHandler owner, String username, String dialogCaption,
                                String dialogSubCaption, String instructions)
      {
         InitializeComponent();
         _owner = owner;
         Text = dialogCaption;
         labelInstructions.Text = instructions;
         textBoxUserName.Text = username;
      }

      /// <summary>
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void buttonOK_Click(object sender, EventArgs e)
      {
         _owner.getCredentials().Username = textBoxUserName.Text;
         _owner.getCredentials().Password = textBoxPassword.Text;
         textBoxUserName.Focus();
         Hide();
      }

      /// <summary>
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void buttonCancel_Click(object sender, EventArgs e)
      {
         Hide();
      }

      /// <summary>
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void MobileAuthenticationForm_Load(object sender, EventArgs e)
      {
         GuiUtils.FindForm(((Control)sender)).Activate();
      }

      /// <summary>
      /// Select all text inside textbox control whenever it receives focus
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void textBox_GotFocus(object sender, EventArgs e)
      {
         ((TextBox)sender).SelectAll();
      }

      /// <summary>
      /// Traverse through all the parkable controls in forward and backward direction for Down and Up arrow keys 
      /// respectively.
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Down)
         {
            this.SelectNextControl((Control)sender, true, true, true, true);
         }
         else if (e.KeyCode == Keys.Up)
         {
            this.SelectNextControl((Control)sender, false, true, true, true);
         }
      }

   }
}