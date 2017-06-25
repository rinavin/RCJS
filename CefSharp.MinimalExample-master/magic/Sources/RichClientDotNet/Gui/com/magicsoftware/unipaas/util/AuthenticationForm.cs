using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.unipaas;
using com.magicsoftware.util;
using MgEvents = com.magicsoftware.unipaas.Events;
using com.magicsoftware.unipaas.gui.low;
using System.Collections.Generic;

namespace com.magicsoftware.unipaas.util
{
   internal partial class AuthenticationForm : Form
   {
      private readonly AuthenticationDialogHandler _owner;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="owner"></param>
      /// <param name="lastTypedUserName"></param>
      /// <param name="windowTitle"></param>
      /// <param name="groupTitle">if not null - set as the group's title (inside the form).</param>
      /// <param name="msgCaption">if not null - set as a message inside the form.</param>
      public AuthenticationForm(AuthenticationDialogHandler owner, String lastTypedUserName, String windowTitle,
                                String groupTitle, String msgCaption)
      {
         InitializeComponent();
         _owner = owner;
         textBoxUsername.Text = lastTypedUserName;
         Text = windowTitle;
         if (groupTitle != null)
            groupBox1.Text = groupTitle;
         if (msgCaption != null)
            labelInstructions.Text = msgCaption;

         // logon window should be shown RightToLeft
         if (MgEvents.IsLogonRTL())
         {
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Layout of controls inside groupBox isn't changed if RTL is Yes. So change layout only for controls inside
            // group box. Rest controls layout will be handled automatically by form.
            ApplyRTL(this);

            if (MgEvents.IsSpecialEngLogon())
            {
               textBoxUsername.RightToLeft = RightToLeft.No;
               textBoxPassword.RightToLeft = RightToLeft.No;
            }
         }
         labelUsername.Text = MgEvents.GetMessageString(MsgInterface.STR_USER_ID);
         labelPassword.Text = MgEvents.GetMessageString(MsgInterface.STR_PASSWORD);
         labelInstructions.Text = MgEvents.GetMessageString(MsgInterface.STR_LOGON_INSTRUCTION);
         buttonOK.Text = MgEvents.GetMessageString(MsgInterface.GUI_OK_BUTTON);
         buttonCancel.Text = MgEvents.GetMessageString(MsgInterface.GUI_CANCEL_BUTTON);
      }

      /// <summary>
      /// CTOR
      /// This constructor would create an customized logon dialog for rich client
      /// from the execution property values for logon dialog.
      /// If the execution properties for logon dialog are not specified then logon dialog
      /// with default values for these properties would be created.
      /// </summary>
      /// <param name="owner"></param>
      /// <param name="lastTypedUserName"></param>
      /// <param name="logonDialogExecutionProps">the customized logon dialog execution props</param>
      public AuthenticationForm(AuthenticationDialogHandler owner, String lastTypedUserName,
                                Dictionary<string, string> logonDialogExecutionProps)
      {
         InitializeComponent();
         _owner = owner;
         textBoxUsername.Text = lastTypedUserName;

         //if the window title & group title execution properties are not specified
         // default values for these properties are already passed in logonDialogExecutionProps.
         Text = logonDialogExecutionProps[GuiConstants.STR_LOGON_WIN_TITLE];
         groupBox1.Text = logonDialogExecutionProps[GuiConstants.STR_LOGON_GROUP_TITLE];

         labelInstructions.Text = logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGON_MSG_CAPTION)
                                  ? logonDialogExecutionProps[GuiConstants.STR_LOGON_MSG_CAPTION]
                                  : GuiConstants.ENTER_UID_TTL;

         // logon window should be shown RightToLeft
         if (MgEvents.IsLogonRTL())
         {
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Layout of controls inside groupBox isn't changed if RTL is Yes. So change layout only for controls inside
            // group box. Rest controls layout will be handled automatically by form.
            ApplyRTL(this);
         }

         if (logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGO_WIN_ICON_URL))
         {
            Icon windowIcon = Manager.GetIcon(logonDialogExecutionProps[GuiConstants.STR_LOGO_WIN_ICON_URL]);
            if (windowIcon != null)
               this.Icon = windowIcon;
            else
               this.ShowIcon = false;
         }

         if (logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGON_IMAGE_URL))
         {
            String windowsImageUrlStr = logonDialogExecutionProps[GuiConstants.STR_LOGON_IMAGE_URL];
            Image windowImage = GuiUtils.getImageFromFile(windowsImageUrlStr);
            if (windowImage != null)
            {
               pictureBox1.Image = windowImage;
               pictureBox1.Visible = true;
            }
            else
               pictureBox1.Visible = false;
         }

         labelUsername.Text = logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGON_USER_ID_CAPTION)
                               ? logonDialogExecutionProps[GuiConstants.STR_LOGON_USER_ID_CAPTION]
                               : MgEvents.GetMessageString(MsgInterface.STR_USER_ID);

         labelPassword.Text = logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGON_PASS_CAPTION)
                              ? logonDialogExecutionProps[GuiConstants.STR_LOGON_PASS_CAPTION]
                              : MgEvents.GetMessageString(MsgInterface.STR_PASSWORD);

         buttonOK.Text = logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGON_OK_CAPTION)
                         ? logonDialogExecutionProps[GuiConstants.STR_LOGON_OK_CAPTION]
                         : MgEvents.GetMessageString(MsgInterface.GUI_OK_BUTTON);

         buttonCancel.Text = logonDialogExecutionProps.ContainsKey(GuiConstants.STR_LOGON_CANCEL_CAPTION)
                             ? logonDialogExecutionProps[GuiConstants.STR_LOGON_CANCEL_CAPTION]
                             : MgEvents.GetMessageString(MsgInterface.GUI_CANCEL_BUTTON);
      }

      private void buttonOK_Click(object sender, EventArgs e)
      {
         _owner.getCredentials().Username = textBoxUsername.Text;
         _owner.getCredentials().Password = textBoxPassword.Text;
         textBoxUsername.Focus();
         Hide();
      }

      private void buttonCancel_Click(object sender, EventArgs e)
      {
         Hide();
      }

      /// <summary> Load Image and show the username and password in the dialog </summary>
      private void AuthenticationForm_Load(object sender, EventArgs e)
      {
         var bmp = (Bitmap)pictureBox1.Image;
         bmp.MakeTransparent();

         Manager.FindForm(((Control)sender)).Activate();
      }

      /// <summary>Same as RightToLeftLayout property of form. This method will make RightToLeftLayout work for 
      /// controls inside GroupBoxes and Panels</summary>
      /// http://stackoverflow.com/questions/147657/how-to-make-righttoleftlayout-work-for-controls-inside-groupboxes-and-panels
      /// <param name="startControl"></param>
      private void ApplyRTL(Control startControl)
      {
         foreach (Control control in startControl.Controls)
         {
            if ((control is GroupBox) || (control is Panel))
            {
               foreach (Control innerControl in control.Controls)
               {
                  innerControl.Location = CalculateRTL(innerControl.Location, innerControl.Size, control.Size);
               }
            }
         }
      }

      /// <summary> Calculate location of control for RTL layout </summary>
      /// <param name="currentPoint"></param>
      /// <param name="parentSize"></param>
      /// <param name="currentSize"></param>
      /// <returns></returns>
      private Point CalculateRTL(Point currentPoint, Size currentSize, Size parentSize)
      {
         return new Point(parentSize.Width - currentSize.Width - currentPoint.X, currentPoint.Y);
      }

      /// <summary>Traverse through all the parkable controls in forward and backward direction for Down and Up arrow keys 
      /// respectively. 
      /// If any key is handled then return true to avoid its handling in base class.
      /// Note: Instead of this function, KeyDown event of Controls is not handled because in case of button controls 
      /// Arrowkeys dont raise KeyDown event.
      /// </summary>
      /// <param name="msg"></param>
      /// <param name="keyData"></param>
      /// <returns></returns>
      protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
      {
         Control ActiveCtrl = this.FindForm().ActiveControl;

         if (keyData == Keys.Down)
         {
            this.SelectNextControl(ActiveCtrl, true, true, true, true);
            return true;
         }
         else if (keyData == Keys.Up)
         {
            this.SelectNextControl(ActiveCtrl, false, true, true, true);
            return true;
         }

         return base.ProcessCmdKey(ref msg, keyData);
      }

      /// <summary>Select all text inside textbox control whenever it receives focus</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void textBox_GotFocus(object sender, EventArgs e)
      {
         ((TextBox)sender).SelectAll();
      }

      /// <summary>
      /// will enable or disable OK button based on the length of username.
      /// </summary>
      /// <param name="e"></param>
      /// <param name="sender"></param>
      private void textBoxUsername_textChanged(object sender, EventArgs e)
      {
         buttonOK.Enabled = textBoxUsername.TextLength > 0;
      }

      /// <summary>Update credentials</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      private void buttonChangePassword_Click(object sender, EventArgs e)
      {
         _owner.ChangePasswordRequested = true;
         _owner.getCredentials().Username = textBoxUsername.Text;
         _owner.getCredentials().Password = textBoxPassword.Text;
         Hide();
      }
   }
}
