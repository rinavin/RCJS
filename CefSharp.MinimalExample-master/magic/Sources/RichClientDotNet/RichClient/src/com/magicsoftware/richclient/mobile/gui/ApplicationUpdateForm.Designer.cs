namespace com.magicsoftware.richclient.http.client
{
   partial class ApplicationUpdateForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;
      private System.Windows.Forms.MainMenu mainMenu1;

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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApplicationUpdateForm));
         this.mainMenu1 = new System.Windows.Forms.MainMenu();
         this.label1 = new System.Windows.Forms.Label();
         _serverName = new System.Windows.Forms.TextBox();
         this.label2 = new System.Windows.Forms.Label();
         _applicationName = new System.Windows.Forms.TextBox();
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         this.label5 = new System.Windows.Forms.Label();
         this.button_Cancel = new System.Windows.Forms.Button();
         _fromVersion = new System.Windows.Forms.TextBox();
         this.label3 = new System.Windows.Forms.Label();
         _toVersion = new System.Windows.Forms.TextBox();
         this.label4 = new System.Windows.Forms.Label();
         this.label6 = new System.Windows.Forms.Label();
         this.label7 = new System.Windows.Forms.Label();
         this.SuspendLayout();
         // 
         // label1
         // 
         this.label1.Location = new System.Drawing.Point(8, 31);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(59, 20);
         this.label1.Text = "Server :";
         // 
         // ServerName
         // 
         _serverName.BackColor = System.Drawing.Color.White;
         _serverName.BorderStyle = System.Windows.Forms.BorderStyle.None;
         _serverName.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
         _serverName.Location = new System.Drawing.Point(94, 31);
         _serverName.Name = "_serverName";
         _serverName.Size = new System.Drawing.Size(133, 21);
         _serverName.TabIndex = 1;
         _serverName.TabStop = false;
         // 
         // label2
         // 
         this.label2.Location = new System.Drawing.Point(8, 70);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(76, 20);
         this.label2.Text = "Application :";
         // 
         // ApplicationName
         // 
         _applicationName.BackColor = System.Drawing.Color.White;
         _applicationName.BorderStyle = System.Windows.Forms.BorderStyle.None;
         _applicationName.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
         _applicationName.Location = new System.Drawing.Point(94, 70);
         _applicationName.Name = "_applicationName";
         _applicationName.Size = new System.Drawing.Size(133, 21);
         _applicationName.TabIndex = 2;
         _applicationName.TabStop = false;
         // 
         // pictureBox1
         // 
         this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
         this.pictureBox1.Location = new System.Drawing.Point(19, 218);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(48, 22);
         // 
         // label5
         // 
         this.label5.BackColor = System.Drawing.SystemColors.InactiveBorder;
         this.label5.ForeColor = System.Drawing.SystemColors.ControlDark;
         this.label5.Location = new System.Drawing.Point(0, 194);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(240, 1);
         // 
         // button_Cancel
         // 
         this.button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.button_Cancel.Location = new System.Drawing.Point(155, 220);
         this.button_Cancel.Name = "button_Cancel";
         this.button_Cancel.Size = new System.Drawing.Size(72, 20);
         this.button_Cancel.TabIndex = 5;
         this.button_Cancel.Text = "Cancel";
         // 
         // FromVersion
         // 
         _fromVersion.AcceptsReturn = true;
         _fromVersion.BackColor = System.Drawing.Color.White;
         _fromVersion.BorderStyle = System.Windows.Forms.BorderStyle.None;
         _fromVersion.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
         _fromVersion.Location = new System.Drawing.Point(94, 109);
         _fromVersion.Name = "_fromVersion";
         _fromVersion.Size = new System.Drawing.Size(133, 21);
         _fromVersion.TabIndex = 3;
         _fromVersion.TabStop = false;
         // 
         // label3
         // 
         this.label3.Location = new System.Drawing.Point(8, 109);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(88, 20);
         this.label3.Text = "From Version :";
         // 
         // ToVersion
         // 
         _toVersion.BackColor = System.Drawing.Color.White;
         _toVersion.BorderStyle = System.Windows.Forms.BorderStyle.None;
         _toVersion.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
         _toVersion.Location = new System.Drawing.Point(94, 148);
         _toVersion.Name = "_toVersion";
         _toVersion.Size = new System.Drawing.Size(133, 21);
         _toVersion.TabIndex = 4;
         _toVersion.TabStop = false;
         // 
         // label4
         // 
         this.label4.Location = new System.Drawing.Point(8, 148);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(88, 20);
         this.label4.Text = "To Version :";
         // 
         // label6
         // 
         this.label6.BackColor = System.Drawing.SystemColors.InactiveBorder;
         this.label6.Location = new System.Drawing.Point(0, 258);
         this.label6.Name = "label6";
         this.label6.Size = new System.Drawing.Size(240, 1);
         // 
         // label7
         // 
         this.label7.BackColor = System.Drawing.SystemColors.InactiveBorder;
         this.label7.Location = new System.Drawing.Point(0, 9);
         this.label7.Name = "label7";
         this.label7.Size = new System.Drawing.Size(240, 1);
         // 
         // ApplicationUpdateForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
         this.AutoScroll = true;
         this.BackColor = System.Drawing.Color.White;
         this.ClientSize = new System.Drawing.Size(240, 268);
         this.Controls.Add(this.label7);
         this.Controls.Add(this.label6);
         this.Controls.Add(_toVersion);
         this.Controls.Add(this.label4);
         this.Controls.Add(_fromVersion);
         this.Controls.Add(this.label3);
         this.Controls.Add(this.button_Cancel);
         this.Controls.Add(this.label5);
         this.Controls.Add(this.pictureBox1);
         this.Controls.Add(_applicationName);
         this.Controls.Add(this.label2);
         this.Controls.Add(_serverName);
         this.Controls.Add(this.label1);
         this.Menu = this.mainMenu1;
         this.Name = "ApplicationUpdateForm";
         this.Text = "Application Version Change";
         this.TopMost = true;
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.TextBox _serverName;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.TextBox _applicationName;
      private System.Windows.Forms.PictureBox pictureBox1;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.Button button_Cancel;
      private System.Windows.Forms.TextBox _fromVersion;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.TextBox _toVersion;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.Label label6;
      private System.Windows.Forms.Label label7;
   }
}