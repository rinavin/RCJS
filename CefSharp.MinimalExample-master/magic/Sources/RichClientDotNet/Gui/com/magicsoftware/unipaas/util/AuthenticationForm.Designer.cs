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
           this.textBoxUsername = new System.Windows.Forms.TextBox();
           this.buttonOK = new System.Windows.Forms.Button();
           this.textBoxPassword = new System.Windows.Forms.TextBox();
           this.labelUsername = new System.Windows.Forms.Label();
           this.labelPassword = new System.Windows.Forms.Label();
           this.buttonCancel = new System.Windows.Forms.Button();
           this.groupBox1 = new System.Windows.Forms.GroupBox();
           this.pictureBox1 = new System.Windows.Forms.PictureBox();
           this.labelInstructions = new System.Windows.Forms.Label();
           this.buttonChangePassword = new System.Windows.Forms.Button();
           this.groupBox1.SuspendLayout();
           ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
           this.SuspendLayout();
           // 
           // textBoxUsername
           // 
           this.textBoxUsername.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.textBoxUsername.Location = new System.Drawing.Point(128, 70);
           this.textBoxUsername.MaxLength = 127;
           this.textBoxUsername.Name = "textBoxUsername";
           this.textBoxUsername.Size = new System.Drawing.Size(234, 20);
           this.textBoxUsername.TabIndex = 0;
           this.textBoxUsername.GotFocus += new System.EventHandler(this.textBox_GotFocus);
           this.textBoxUsername.TextChanged += new System.EventHandler(this.textBoxUsername_textChanged);
           // 
           // buttonOK
           // 
           this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
           this.buttonOK.Location = new System.Drawing.Point(218, 165);
           this.buttonOK.Name = "buttonOK";
           this.buttonOK.Size = new System.Drawing.Size(75, 23);
           this.buttonOK.TabIndex = 4;
           this.buttonOK.Text = "&OK";
           this.buttonOK.UseVisualStyleBackColor = true;
           this.buttonOK.Enabled = false;
           this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
           // 
           // textBoxPassword
           // 
           this.textBoxPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.textBoxPassword.Location = new System.Drawing.Point(128, 103);
           this.textBoxPassword.MaxLength = 20;
           this.textBoxPassword.Name = "textBoxPassword";
           this.textBoxPassword.PasswordChar = '*';
           this.textBoxPassword.Size = new System.Drawing.Size(234, 20);
           this.textBoxPassword.TabIndex = 1;
           this.textBoxPassword.GotFocus += new System.EventHandler(this.textBox_GotFocus);
           // 
           // labelUsername
           // 
           this.labelUsername.AutoSize = false;
           this.labelUsername.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.labelUsername.Location = new System.Drawing.Point(50, 73);
           this.labelUsername.Name = "labelUsername";
           this.labelUsername.Size = new System.Drawing.Size(75, 13);
           this.labelUsername.TabIndex = 3;
           this.labelUsername.Text = "User ID:";
           // 
           // labelPassword
           // 
           this.labelPassword.AutoSize = false;
           this.labelPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.labelPassword.Location = new System.Drawing.Point(50, 106);
           this.labelPassword.Name = "labelPassword";
           this.labelPassword.Size = new System.Drawing.Size(75, 13);
           this.labelPassword.TabIndex = 4;
           this.labelPassword.Text = "Password:";
           // 
           // buttonCancel
           // 
           this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
           this.buttonCancel.Location = new System.Drawing.Point(303, 165);
           this.buttonCancel.Name = "buttonCancel";
           this.buttonCancel.Size = new System.Drawing.Size(75, 23);
           this.buttonCancel.TabIndex = 5;
           this.buttonCancel.Text = "&Cancel";
           this.buttonCancel.UseVisualStyleBackColor = true;
           this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
           // 
           // groupBox1
           // 
           this.groupBox1.Controls.Add(this.pictureBox1);
           this.groupBox1.Controls.Add(this.labelInstructions);
           this.groupBox1.Controls.Add(this.labelUsername);
           this.groupBox1.Controls.Add(this.textBoxUsername);
           this.groupBox1.Controls.Add(this.labelPassword);
           this.groupBox1.Controls.Add(this.textBoxPassword);
           this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.groupBox1.ForeColor = System.Drawing.Color.Black;
           this.groupBox1.Location = new System.Drawing.Point(7, 8);
           this.groupBox1.Name = "groupBox1";
           this.groupBox1.Size = new System.Drawing.Size(371, 151);
           this.groupBox1.TabIndex = 2;
           this.groupBox1.TabStop = false;
           this.groupBox1.Text = "Logon Parameters";
           // 
           // pictureBox1
           // 
           this.pictureBox1.Image = global::com.magicsoftware.unipaas.gui.Properties.Resources.LoginIcon;
           this.pictureBox1.Location = new System.Drawing.Point(6, 25);
           this.pictureBox1.Name = "pictureBox1";
           this.pictureBox1.Size = new System.Drawing.Size(32, 32);
           this.pictureBox1.TabIndex = 6;
           this.pictureBox1.TabStop = false;
           // 
           // labelInstructions
           // 
           this.labelInstructions.AutoSize = false;
           this.labelInstructions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.labelInstructions.Location = new System.Drawing.Point(50, 34);
           this.labelInstructions.Name = "labelInstructions";
           this.labelInstructions.Size = new System.Drawing.Size(312, 13);
           this.labelInstructions.TabIndex = 5;
           this.labelInstructions.Text = "Instructions";
           // 
           // buttonChangePassword
           // 
           this.buttonChangePassword.DialogResult = System.Windows.Forms.DialogResult.Ignore;
           this.buttonChangePassword.Location = new System.Drawing.Point(13, 165);
           this.buttonChangePassword.Name = "buttonChangePassword";
           this.buttonChangePassword.Size = new System.Drawing.Size(106, 23);
           this.buttonChangePassword.TabIndex = 3;
           this.buttonChangePassword.Text = "Change &Password";
           this.buttonChangePassword.UseVisualStyleBackColor = true;
           this.buttonChangePassword.Visible = false;
           this.buttonChangePassword.Click += new System.EventHandler(this.buttonChangePassword_Click);
           // 
           // AuthenticationForm
           // 
           this.AcceptButton = this.buttonOK;
           this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
           this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
           this.CancelButton = this.buttonCancel;
           this.ClientSize = new System.Drawing.Size(384, 193);
           this.Controls.Add(this.buttonChangePassword);
           this.Controls.Add(this.groupBox1);
           this.Controls.Add(this.buttonCancel);
           this.Controls.Add(this.buttonOK);
           this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
           this.MaximizeBox = false;
           this.MinimizeBox = false;
           this.Name = "AuthenticationForm";
           this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
           this.Text = "AuthenticationForm";
           this.TopMost = true;
           this.Load += new System.EventHandler(this.AuthenticationForm_Load);
           this.groupBox1.ResumeLayout(false);
           this.groupBox1.PerformLayout();
           ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
           this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxUsername;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label labelUsername;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label labelInstructions;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button buttonChangePassword;
    }
}
