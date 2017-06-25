using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using com.magicsoftware.richclient.http;
using com.magicsoftware.util;

namespace com.magicsoftware.richclient.mobile.gui
{
   partial class MainForm
   {
      static long startTime = Misc.getSystemMilliseconds();

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
          this.pictureBox1 = new System.Windows.Forms.PictureBox();
          this.progressBar1 = new System.Windows.Forms.ProgressBar();
          this.SuspendLayout();
          // 
          // pictureBox1
          // 
          this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
          this.pictureBox1.Location = new System.Drawing.Point(0, 0);
          this.pictureBox1.Name = "pictureBox1";
          this.pictureBox1.Size = new System.Drawing.Size(240, 294);
          this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
          // 
          // progressBar1
          // 
          this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
          this.progressBar1.Location = new System.Drawing.Point(39, 121);
          this.progressBar1.Maximum = 10;
          this.progressBar1.Name = "progressBar1";
          this.progressBar1.Size = new System.Drawing.Size(164, 31);
          //In case user selects not to show progress bar (in execution.properties)
          //then hide and disable the progress bar.
          bool showProgerssBar = ClientManager.Instance.getMobileShowProgressBar();
          this.progressBar1.Enabled = showProgerssBar;
          this.progressBar1.Visible = showProgerssBar;
          // 
          // MainForm
          // 
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
          this.ClientSize = new System.Drawing.Size(240, 294);
          this.Controls.Add(this.progressBar1);
          this.Controls.Add(this.pictureBox1);
          this.Name = "MainForm";
          this.LostFocus += new System.EventHandler(MainForm_LostFocus);
          this.ResumeLayout(false);

      }

      void MainForm_LostFocus(object sender, System.EventArgs e)
      {
         Hide();
      }

      #endregion

      delegate void UpdateProgressBar(string msg);
      internal void updateProgressBar(string msg)
      {
         ushort timeElapsedSeconds = (ushort)((Misc.getSystemMilliseconds() - startTime) / 1000);
         if (timeElapsedSeconds <= 20) //TODO: 
         {
            if (progressBar1.InvokeRequired)
            {
               UpdateProgressBar d = new UpdateProgressBar(updateProgressBar);
               Invoke(d, new object[] { msg });
            }
            else
            {
               progressBar1.Value = (int)timeElapsedSeconds % progressBar1.Maximum;
            }
         }
      }
      internal void updateProgressBar()
      {
         updateProgressBar("");
      }

      private PictureBox pictureBox1;
      private ProgressBar progressBar1;
   }
}