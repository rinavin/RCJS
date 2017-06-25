using Microsoft.WindowsCE.Forms;
using com.magicsoftware.win32;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace com.magicsoftware.unipaas.util
{
   partial class AuthenticationForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AuthenticationForm));
         this.mainMenu1 = new System.Windows.Forms.MainMenu();
         this.pictureBox2 = new System.Windows.Forms.PictureBox();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         this.labelInstructions = new System.Windows.Forms.Label();
         this.labelPassword = new System.Windows.Forms.Label();
         this.labelUsername = new System.Windows.Forms.Label();
         this.textBoxPassword = new System.Windows.Forms.TextBox();
         this.textBoxUserName = new System.Windows.Forms.TextBox();
         inputPanel = new InputPanel();
         this.SuspendLayout();
         // 
         // pictureBox2
         // 
         this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
         this.pictureBox2.Location = new System.Drawing.Point(0, 0);
         this.pictureBox2.Name = "pictureBox2";
         this.pictureBox2.Size = new System.Drawing.Size(240, 268);
         this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
         // 
         // buttonCancel
         // 
         this.buttonCancel.BackColor = System.Drawing.Color.White;
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
         this.buttonCancel.Location = new System.Drawing.Point(187, 220);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(50, 20);
         this.buttonCancel.TabIndex = 21;
         this.buttonCancel.Text = "&Cancel";
         this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
         this.buttonCancel.Anchor = AnchorStyles.Bottom;
         // 
         // buttonOK
         // 
         this.buttonOK.BackColor = System.Drawing.Color.White;
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular);
         this.buttonOK.Location = new System.Drawing.Point(123, 220);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(50, 20);
         this.buttonOK.TabIndex = 20;
         this.buttonOK.Text = "&OK";
         this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
         this.buttonOK.Anchor = AnchorStyles.Bottom;
         // 
         // pictureBox1
         // 
         this.pictureBox1.BackColor = System.Drawing.Color.White;
         this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
         this.pictureBox1.Location = new System.Drawing.Point(39, 109);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(35, 32);
         // 
         // labelInstructions
         // 
         this.labelInstructions.BackColor = System.Drawing.Color.Transparent;
         this.labelInstructions.Location = new System.Drawing.Point(80, 120);
         this.labelInstructions.Name = "labelInstructions";
         this.labelInstructions.Size = new System.Drawing.Size(157, 20);
         this.labelInstructions.Text = "Instructions";
         this.labelInstructions.Anchor = AnchorStyles.Bottom;
         // 
         // labelPassword
         // 
         this.labelPassword.BackColor = System.Drawing.Color.Transparent;
         this.labelPassword.Location = new System.Drawing.Point(90, 181);
         this.labelPassword.Name = "labelPassword";
         this.labelPassword.Size = new System.Drawing.Size(60, 20);
         this.labelPassword.Text = "Password:";
         this.labelPassword.Anchor = AnchorStyles.Bottom;
         // 
         // labelUsername
         // 
         this.labelUsername.BackColor = System.Drawing.Color.Transparent;
         this.labelUsername.Location = new System.Drawing.Point(90, 154);
         this.labelUsername.Name = "labelUsername";
         this.labelUsername.Size = new System.Drawing.Size(55, 20);
         this.labelUsername.Text = "User ID:";
         this.labelUsername.Anchor = AnchorStyles.Bottom;
         // 
         // textBoxPassword
         // 
         this.textBoxPassword.Location = new System.Drawing.Point(160, 181);
         this.textBoxPassword.MaxLength = 20;
         this.textBoxPassword.Name = "textBoxPassword";
         this.textBoxPassword.PasswordChar = '*';
         this.textBoxPassword.Size = new System.Drawing.Size(77, 21);
         this.textBoxPassword.TabIndex = 19;
         this.textBoxPassword.GotFocus += new System.EventHandler(this.textBox_GotFocus);
         this.textBoxPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyDown);
         this.textBoxPassword.Anchor = AnchorStyles.Bottom;
         // 
         // textBoxUserName
         // 
         this.textBoxUserName.Location = new System.Drawing.Point(160, 154);
         this.textBoxUserName.MaxLength = 127;
         this.textBoxUserName.Name = "textBoxUserName";
         this.textBoxUserName.Size = new System.Drawing.Size(77, 21);
         this.textBoxUserName.TabIndex = 18;
         this.textBoxUserName.GotFocus += new System.EventHandler(this.textBox_GotFocus);
         this.textBoxUserName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyDown);
         this.textBoxUserName.Anchor = AnchorStyles.Bottom;
         // input panel
         inputPanel.EnabledChanged += new System.EventHandler(inputPanel_EnabledChanged);
         // 
         // AuthenticationForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
         this.BackColor = System.Drawing.Color.White;
         this.ClientSize = new System.Drawing.Size(240, 268);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.pictureBox1);
         this.Controls.Add(this.labelInstructions);
         this.Controls.Add(this.labelPassword);
         this.Controls.Add(this.labelUsername);
         this.Controls.Add(this.textBoxPassword);
         this.Controls.Add(this.textBoxUserName);
         this.Controls.Add(this.pictureBox2);
         this.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular);
         this.Menu = this.mainMenu1;
         this.MinimizeBox = false;
         this.Name = "AuthenticationForm";
         this.Text = "AuthenticationForm";
         this.TopMost = true;
         this.Load += new System.EventHandler(this.MobileAuthenticationForm_Load);
         this.ResumeLayout(false);

      }

      void inputPanel_EnabledChanged(object sender, System.EventArgs e)
      {
         Size changeFormSizeTo;

         if (Bounds != inputPanel.VisibleDesktop)
         {
            if (inputPanel.Enabled || previousSize == null)
            {
               // remember the form's size before the soft keyboard was opened
               previousSize = Size;
               changeFormSizeTo = inputPanel.VisibleDesktop.Size;
            }
            else
            {
               // use the size from before the soft keyboard was opened - the VisibleDesktop ignores
               // the menu bar size
               changeFormSizeTo = previousSize;
            }

            // Set the new size. The .Net way of changing the bounds does not work in this case,
            // so lets do it via win32 functions
            NativeWindowCommon.SetWindowPos(Handle, IntPtr.Zero,
               0, 0, changeFormSizeTo.Width, changeFormSizeTo.Height,
               NativeWindowCommon.SWP_NOMOVE | NativeWindowCommon.SWP_NOZORDER);
         }
      }

      #endregion

      private System.Windows.Forms.MainMenu mainMenu1;
      private System.Windows.Forms.PictureBox pictureBox2;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.PictureBox pictureBox1;
      private System.Windows.Forms.Label labelInstructions;
      private System.Windows.Forms.Label labelPassword;
      private System.Windows.Forms.Label labelUsername;
      private System.Windows.Forms.TextBox textBoxPassword;
      private System.Windows.Forms.TextBox textBoxUserName;

      private InputPanel inputPanel;
      private Size previousSize;     // form size before the soft keyboard opened

   }
}