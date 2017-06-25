#if PocketPC
using GroupBox = OpenNETCF.Windows.Forms.GroupBox;
#endif

namespace com.magicsoftware.richclient.http
{
   partial class SessionStatisticsForm
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
         this.groupBoxStatistics = new GroupBox();
         this.label23 = new System.Windows.Forms.Label();
         this.label24 = new System.Windows.Forms.Label();
         this.NetworkPercentageLabel = new System.Windows.Forms.Label();
         this.NetworkTimeLabel = new System.Windows.Forms.Label();
         this.label27 = new System.Windows.Forms.Label();
         this.label22 = new System.Windows.Forms.Label();
         this.label21 = new System.Windows.Forms.Label();
         this.label20 = new System.Windows.Forms.Label();
         this.ServerPercentageLabel = new System.Windows.Forms.Label();
         this.MiddlewarePercentageLabel = new System.Windows.Forms.Label();
         this.ExternalPercentageLabel = new System.Windows.Forms.Label();
         this.groupBox1 = new GroupBox();
         this.label13 = new System.Windows.Forms.Label();
         this.label11 = new System.Windows.Forms.Label();
         this.labelDownloadedCompressionRatio = new System.Windows.Forms.Label();
         this.labelUploadedCompressionRatio = new System.Windows.Forms.Label();
         this.label10 = new System.Windows.Forms.Label();
         this.label8 = new System.Windows.Forms.Label();
         this.label2 = new System.Windows.Forms.Label();
         this.labelUploadedKB = new System.Windows.Forms.Label();
         this.label14 = new System.Windows.Forms.Label();
         this.labelDownloadedKB = new System.Windows.Forms.Label();
         this.ExternalTimeLabel = new System.Windows.Forms.Label();
         this.label9 = new System.Windows.Forms.Label();
         this.MiddlewareTimeLabel = new System.Windows.Forms.Label();
         this.label5 = new System.Windows.Forms.Label();
         this.ServerTimeLabel = new System.Windows.Forms.Label();
         this.label12 = new System.Windows.Forms.Label();
         this.SessionTimeLabel = new System.Windows.Forms.Label();
         this.label6 = new System.Windows.Forms.Label();
         this.labelRequests = new System.Windows.Forms.Label();
         this.label1 = new System.Windows.Forms.Label();
         this.groupBoxExecutionProperties = new GroupBox();
         this.labelInternalLogFile = new System.Windows.Forms.Label();
         this.label7 = new System.Windows.Forms.Label();
         this.labelInternalLogLevel = new System.Windows.Forms.Label();
         this.label4 = new System.Windows.Forms.Label();
         this.labelProgram = new System.Windows.Forms.Label();
         this.label3 = new System.Windows.Forms.Label();
         this.labelApplication = new System.Windows.Forms.Label();
         this.label16 = new System.Windows.Forms.Label();
         this.labelServerURL = new System.Windows.Forms.Label();
         this.label18 = new System.Windows.Forms.Label();
         this.groupBoxStatistics.SuspendLayout();
         this.groupBoxExecutionProperties.SuspendLayout();
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxStatistics
         // 
         this.groupBoxStatistics.Controls.Add(this.label23);
         this.groupBoxStatistics.Controls.Add(this.label24);
         this.groupBoxStatistics.Controls.Add(this.NetworkPercentageLabel);
         this.groupBoxStatistics.Controls.Add(this.NetworkTimeLabel);
         this.groupBoxStatistics.Controls.Add(this.label27);
         this.groupBoxStatistics.Controls.Add(this.label22);
         this.groupBoxStatistics.Controls.Add(this.label21);
         this.groupBoxStatistics.Controls.Add(this.label20);
         this.groupBoxStatistics.Controls.Add(this.ServerPercentageLabel);
         this.groupBoxStatistics.Controls.Add(this.MiddlewarePercentageLabel);
         this.groupBoxStatistics.Controls.Add(this.ExternalPercentageLabel);
         this.groupBoxStatistics.Controls.Add(this.groupBox1);
         this.groupBoxStatistics.Controls.Add(this.ExternalTimeLabel);
         this.groupBoxStatistics.Controls.Add(this.label9);
         this.groupBoxStatistics.Controls.Add(this.MiddlewareTimeLabel);
         this.groupBoxStatistics.Controls.Add(this.label5);
         this.groupBoxStatistics.Controls.Add(this.ServerTimeLabel);
         this.groupBoxStatistics.Controls.Add(this.label12);
         this.groupBoxStatistics.Controls.Add(this.SessionTimeLabel);
         this.groupBoxStatistics.Controls.Add(this.label6);
         this.groupBoxStatistics.Controls.Add(this.labelRequests);
         this.groupBoxStatistics.Controls.Add(this.label1);
         this.groupBoxStatistics.Location = new System.Drawing.Point(12, 221);
         this.groupBoxStatistics.Name = "groupBoxStatistics";
         this.groupBoxStatistics.Size = new System.Drawing.Size(492, 286);
         this.groupBoxStatistics.TabIndex = 12;
         this.groupBoxStatistics.TabStop = false;
         this.groupBoxStatistics.Text = "Statistics";
         // 
         // label2
         // 
         this.label2.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label2.Location = new System.Drawing.Point(19, 22);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(82, 17);
         this.label2.Text = "Uploaded:";
         // 
         // label11
         // 
         this.label11.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label11.Location = new System.Drawing.Point(207, 53);
         this.label11.Name = "label11";
         this.label11.Size = new System.Drawing.Size(28, 18);
         this.label11.Text = "KB";
         // 
         // labelNetwork
         // 
         this.ExternalTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.ExternalTimeLabel.Location = new System.Drawing.Point(151, 150);
         this.ExternalTimeLabel.Name = "labelNetwork";
         this.ExternalTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.ExternalTimeLabel.Text = "00:00:00.000";
         // 
         // label8
         // 
         this.label8.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label8.Location = new System.Drawing.Point(146, 22);
         this.label8.Name = "label8";
         this.label8.Size = new System.Drawing.Size(60, 18);
         this.label8.Text = "000,000";
         // 
         // label10
         // 
         this.label10.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label10.Location = new System.Drawing.Point(208, 22);
         this.label10.Name = "label10";
         this.label10.Size = new System.Drawing.Size(28, 18);
         this.label10.Text = "KB";
         // 
         // label9
         // 
         this.label9.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label9.Location = new System.Drawing.Point(22, 150);
         this.label9.Name = "label9";
         this.label9.Size = new System.Drawing.Size(79, 20);
         this.label9.Text = "Network:";
         // 
         // label14
         // 
         this.label14.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label14.Location = new System.Drawing.Point(19, 52);
         this.label14.Name = "label14";
         this.label14.Size = new System.Drawing.Size(101, 17);
         this.label14.Text = "Downloaded:";
         // 
         // MiddlewareTimeLabel
         // 
         this.MiddlewareTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.MiddlewareTimeLabel.Location = new System.Drawing.Point(152, 186);
         this.MiddlewareTimeLabel.Name = "MiddlewareTimeLabel";
         this.MiddlewareTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.MiddlewareTimeLabel.Text = "00:00:00.000";
         // 
         // label13
         // 
         this.label13.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label13.Location = new System.Drawing.Point(146, 52);
         this.label13.Name = "label13";
         this.label13.Size = new System.Drawing.Size(60, 18);
         this.label13.Text = "000,000";
         // 
         // label5
         // 
         this.label5.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label5.Location = new System.Drawing.Point(23, 186);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(67, 20);
         this.label5.Text = "Middleware:";
         // 
         // ServerTimeLabel
         // 
         this.ServerTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.ServerTimeLabel.Location = new System.Drawing.Point(152, 222);
         this.ServerTimeLabel.Name = "ServerTimeLabel";
         this.ServerTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.ServerTimeLabel.Text = "00:00:00.000";
         // 
         // label12
         // 
         this.label12.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label12.Location = new System.Drawing.Point(22, 222);
         this.label12.Name = "label12";
         this.label12.Size = new System.Drawing.Size(66, 20);
         this.label12.Text = "Server:";
         // 
         // SessionTimeLabel
         // 
         this.SessionTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.SessionTimeLabel.Location = new System.Drawing.Point(152, 256);
         this.SessionTimeLabel.Name = "SessionTimeLabel";
         this.SessionTimeLabel.Size = new System.Drawing.Size(116, 20);
         this.SessionTimeLabel.Text = "00:00:00.000";
         // 
         // label6
         // 
         this.label6.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label6.Location = new System.Drawing.Point(23, 256);
         this.label6.Name = "label6";
         this.label6.Size = new System.Drawing.Size(78, 20);
         this.label6.Text = "Session:";
         // 
         // labelRequests
         // 
         this.labelRequests.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelRequests.Location = new System.Drawing.Point(152, 32);
         this.labelRequests.Name = "labelRequests";
         this.labelRequests.Size = new System.Drawing.Size(89, 20);
         this.labelRequests.Text = "000,000,000";
         // 
         // label1
         // 
         this.label1.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label1.Location = new System.Drawing.Point(22, 32);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(91, 20);
         this.label1.Text = "Requests:";
         // 
         // groupBoxExecutionProperties
         // 
         this.groupBoxExecutionProperties.Controls.Add(this.labelInternalLogFile);
         this.groupBoxExecutionProperties.Controls.Add(this.label7);
         this.groupBoxExecutionProperties.Controls.Add(this.labelInternalLogLevel);
         this.groupBoxExecutionProperties.Controls.Add(this.label4);
         this.groupBoxExecutionProperties.Controls.Add(this.labelProgram);
         this.groupBoxExecutionProperties.Controls.Add(this.label3);
         this.groupBoxExecutionProperties.Controls.Add(this.labelApplication);
         this.groupBoxExecutionProperties.Controls.Add(this.label16);
         this.groupBoxExecutionProperties.Controls.Add(this.labelServerURL);
         this.groupBoxExecutionProperties.Controls.Add(this.label18);
         this.groupBoxExecutionProperties.Location = new System.Drawing.Point(12, 12);
         this.groupBoxExecutionProperties.Name = "groupBoxExecutionProperties";
         this.groupBoxExecutionProperties.Size = new System.Drawing.Size(492, 203);
         this.groupBoxExecutionProperties.TabIndex = 23;
         this.groupBoxExecutionProperties.TabStop = false;
         this.groupBoxExecutionProperties.Text = "Execution Properties";
         // 
         // label23
         // 
         this.label23.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label23.Location = new System.Drawing.Point(321, 223);
         this.label23.Name = "label23";
         this.label23.Size = new System.Drawing.Size(146, 20);
         this.label23.Text = "out of External time";
         // 
         // label24
         // 
         this.label24.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label24.Location = new System.Drawing.Point(294, 223);
         this.label24.Name = "label24";
         this.label24.Size = new System.Drawing.Size(21, 18);
         this.label24.Text = "%";
         // 
         // NetworkPercentageLabel
         // 
         this.NetworkPercentageLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.NetworkPercentageLabel.Location = new System.Drawing.Point(265, 224);
         this.NetworkPercentageLabel.Name = "NetworkPercentageLabel";
         this.NetworkPercentageLabel.Size = new System.Drawing.Size(35, 18);
         this.NetworkPercentageLabel.Text = "000";
         this.NetworkPercentageLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // NetworkTimeLabel
         // 
         this.NetworkTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.NetworkTimeLabel.Location = new System.Drawing.Point(153, 223);
         this.NetworkTimeLabel.Name = "NetworkTimeLabel";
         this.NetworkTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.NetworkTimeLabel.Text = "00:00:00.000";
         // 
         // label27
         // 
         this.label27.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label27.Location = new System.Drawing.Point(23, 222);
         this.label27.Name = "label27";
         this.label27.Size = new System.Drawing.Size(79, 20);
         this.label27.Text = "Network:";
         // 
         // label22
         // 
         this.label22.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label22.Location = new System.Drawing.Point(321, 287);
         this.label22.Name = "label22";
         this.label22.Size = new System.Drawing.Size(146, 20);
         this.label22.Text = "out of External time";
         // 
         // label21
         // 
         this.label21.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label21.Location = new System.Drawing.Point(321, 255);
         this.label21.Name = "label21";
         this.label21.Size = new System.Drawing.Size(146, 20);
         this.label21.Text = "out of External time";
         // 
         // label20
         // 
         this.label20.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label20.Location = new System.Drawing.Point(321, 190);
         this.label20.Name = "label20";
         this.label20.Size = new System.Drawing.Size(145, 20);
         this.label20.Text = "out of Session time";
         // 
         // labelInternalLogFile
         // 
         this.labelInternalLogFile.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelInternalLogFile.Location = new System.Drawing.Point(125, 148);
         this.labelInternalLogFile.Name = "labelInternalLogFile";
         this.labelInternalLogFile.Size = new System.Drawing.Size(360, 49);
         this.labelInternalLogFile.Text = "C:\\Documents and Settings\\MyUser\\Desktop\\MgxpaRIA_YYYY_MM_DD.log";
         // 
         // label7
         // 
         this.label7.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label7.Location = new System.Drawing.Point(23, 148);
         this.label7.Name = "label7";
         this.label7.Size = new System.Drawing.Size(78, 20);
         this.label7.Text = "Log File:";
         // 
         // labelInternalLogLevel
         // 
         this.labelInternalLogLevel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelInternalLogLevel.Location = new System.Drawing.Point(125, 118);
         this.labelInternalLogLevel.Name = "labelInternalLogLevel";
         this.labelInternalLogLevel.Size = new System.Drawing.Size(360, 30);
         this.labelInternalLogLevel.Text = "Server | Support | Development | Gui";
         // 
         // label4
         // 
         this.label4.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label4.Location = new System.Drawing.Point(23, 118);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(91, 20);
         this.label4.Text = "Log Level:";
         // 
         // labelProgram
         // 
         this.labelProgram.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelProgram.Location = new System.Drawing.Point(125, 88);
         this.labelProgram.Name = "labelProgram";
         this.labelProgram.Size = new System.Drawing.Size(360, 20);
         this.labelProgram.Text = "MyProgram";
         // 
         // label3
         // 
         this.label3.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label3.Location = new System.Drawing.Point(23, 88);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(81, 20);
         this.label3.Text = "Program:";
         // 
         // labelApplication
         // 
         this.labelApplication.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelApplication.Location = new System.Drawing.Point(125, 58);
         this.labelApplication.Name = "labelApplication";
         this.labelApplication.Size = new System.Drawing.Size(360, 20);
         this.labelApplication.Text = "MyApplication";
         // 
         // label16
         // 
         this.label16.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label16.Location = new System.Drawing.Point(22, 58);
         this.label16.Name = "label16";
         this.label16.Size = new System.Drawing.Size(103, 20);
         this.label16.Text = "Application:";
         // 
         // labelServerURL
         // 
         this.labelServerURL.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelServerURL.Location = new System.Drawing.Point(125, 28);
         this.labelServerURL.Name = "labelServerURL";
         this.labelServerURL.Size = new System.Drawing.Size(360, 20);
         this.labelServerURL.Text = "http://MyServer.MyDomain/MagicScripts/mgrqispi.dll";
         // 
         // label18
         // 
         this.label18.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label18.Location = new System.Drawing.Point(22, 28);
         this.label18.Name = "label18";
         this.label18.Size = new System.Drawing.Size(66, 20);
         this.label18.Text = "Server:";
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.label2);
         this.groupBox1.Controls.Add(this.label13);
         this.groupBox1.Controls.Add(this.label11);
         this.groupBox1.Controls.Add(this.label14);
         this.groupBox1.Controls.Add(this.label10);
         this.groupBox1.Controls.Add(this.label8);
         this.groupBox1.Location = new System.Drawing.Point(4, 55);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(482, 82);
         this.groupBox1.TabStop = false;
         // 
         // SessionStatistics
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
         this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
         this.ClientSize = new System.Drawing.Size(516, 519);
         this.Controls.Add(this.groupBoxExecutionProperties);
         this.Controls.Add(this.groupBoxStatistics);
         this.Name = "SessionStatistics";
         this.Text = "Session Statistics";
         this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SessionStatistics_KeyUp);
         this.groupBoxStatistics.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private GroupBox groupBoxStatistics;
      private System.Windows.Forms.Label ServerTimeLabel;
      private System.Windows.Forms.Label label12;
      private System.Windows.Forms.Label ExternalTimeLabel;
      private System.Windows.Forms.Label label9;
      private System.Windows.Forms.Label SessionTimeLabel;
      private System.Windows.Forms.Label label6;
      private System.Windows.Forms.Label labelRequests;
      private System.Windows.Forms.Label label1;
      private GroupBox groupBoxExecutionProperties;
      private System.Windows.Forms.Label labelApplication;
      private System.Windows.Forms.Label label16;
      private System.Windows.Forms.Label labelServerURL;
      private System.Windows.Forms.Label label18;
      private System.Windows.Forms.Label labelProgram;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.Label labelInternalLogFile;
      private System.Windows.Forms.Label label7;
      private System.Windows.Forms.Label labelInternalLogLevel;
      private System.Windows.Forms.Label label4;
      private System.Windows.Forms.Label MiddlewareTimeLabel;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.Label label10;
      private System.Windows.Forms.Label label8;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.Label label11;
      private System.Windows.Forms.Label label13;
      private System.Windows.Forms.Label label14;
      private GroupBox groupBox1; 
      private System.Windows.Forms.Label labelDownloadedCompressionRatio;
      private System.Windows.Forms.Label labelUploadedCompressionRatio;
      private System.Windows.Forms.Label labelDownloadedKB;
      private System.Windows.Forms.Label labelUploadedKB;
      private System.Windows.Forms.Label ServerPercentageLabel;
      private System.Windows.Forms.Label MiddlewarePercentageLabel;
      private System.Windows.Forms.Label ExternalPercentageLabel;
      private System.Windows.Forms.Label label23;
      private System.Windows.Forms.Label label24;
      private System.Windows.Forms.Label NetworkPercentageLabel;
      private System.Windows.Forms.Label NetworkTimeLabel;
      private System.Windows.Forms.Label label27;
      private System.Windows.Forms.Label label22;
      private System.Windows.Forms.Label label21;
      private System.Windows.Forms.Label label20;
   }
}
