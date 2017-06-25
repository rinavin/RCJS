#if !PocketPC
using GroupBox = System.Windows.Forms.GroupBox;
#else
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
         this.groupBoxStatistics = new System.Windows.Forms.GroupBox();
         this.groupBox2 = new System.Windows.Forms.GroupBox();
         this.label23 = new System.Windows.Forms.Label();
         this.label24 = new System.Windows.Forms.Label();
         this.NetworkPercentageLabel = new System.Windows.Forms.Label();
         this.NetworkTimeLabel = new System.Windows.Forms.Label();
         this.label27 = new System.Windows.Forms.Label();
         this.label22 = new System.Windows.Forms.Label();
         this.label21 = new System.Windows.Forms.Label();
         this.label20 = new System.Windows.Forms.Label();
         this.label19 = new System.Windows.Forms.Label();
         this.label17 = new System.Windows.Forms.Label();
         this.label15 = new System.Windows.Forms.Label();
         this.ServerPercentageLabel = new System.Windows.Forms.Label();
         this.MiddlewarePercentageLabel = new System.Windows.Forms.Label();
         this.ExternalPercentageLabel = new System.Windows.Forms.Label();
         this.ExternalTimeLabel = new System.Windows.Forms.Label();
         this.label9 = new System.Windows.Forms.Label();
         this.MiddlewareTimeLabel = new System.Windows.Forms.Label();
         this.label5 = new System.Windows.Forms.Label();
         this.ServerTimeLabel = new System.Windows.Forms.Label();
         this.label12 = new System.Windows.Forms.Label();
         this.SessionTimeLabel = new System.Windows.Forms.Label();
         this.label6 = new System.Windows.Forms.Label();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
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
         this.labelRequests = new System.Windows.Forms.Label();
         this.label1 = new System.Windows.Forms.Label();
         this.groupBoxExecutionProperties = new System.Windows.Forms.GroupBox();
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
         this.groupBox2.SuspendLayout();
         this.groupBox1.SuspendLayout();
         this.groupBoxExecutionProperties.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxStatistics
         // 
         this.groupBoxStatistics.Controls.Add(this.groupBox2);
         this.groupBoxStatistics.Controls.Add(this.groupBox1);
         this.groupBoxStatistics.Controls.Add(this.labelRequests);
         this.groupBoxStatistics.Controls.Add(this.label1);
         this.groupBoxStatistics.Location = new System.Drawing.Point(12, 221);
         this.groupBoxStatistics.Name = "groupBoxStatistics";
         this.groupBoxStatistics.Size = new System.Drawing.Size(492, 334);
         this.groupBoxStatistics.TabIndex = 12;
         this.groupBoxStatistics.TabStop = false;
         this.groupBoxStatistics.Text = "Client Statistics";
         // 
         // groupBox2
         // 
         this.groupBox2.Controls.Add(this.label23);
         this.groupBox2.Controls.Add(this.label24);
         this.groupBox2.Controls.Add(this.NetworkPercentageLabel);
         this.groupBox2.Controls.Add(this.NetworkTimeLabel);
         this.groupBox2.Controls.Add(this.label27);
         this.groupBox2.Controls.Add(this.label22);
         this.groupBox2.Controls.Add(this.label21);
         this.groupBox2.Controls.Add(this.label20);
         this.groupBox2.Controls.Add(this.label19);
         this.groupBox2.Controls.Add(this.label17);
         this.groupBox2.Controls.Add(this.label15);
         this.groupBox2.Controls.Add(this.ServerPercentageLabel);
         this.groupBox2.Controls.Add(this.MiddlewarePercentageLabel);
         this.groupBox2.Controls.Add(this.ExternalPercentageLabel);
         this.groupBox2.Controls.Add(this.ExternalTimeLabel);
         this.groupBox2.Controls.Add(this.label9);
         this.groupBox2.Controls.Add(this.MiddlewareTimeLabel);
         this.groupBox2.Controls.Add(this.label5);
         this.groupBox2.Controls.Add(this.ServerTimeLabel);
         this.groupBox2.Controls.Add(this.label12);
         this.groupBox2.Controls.Add(this.SessionTimeLabel);
         this.groupBox2.Controls.Add(this.label6);
         this.groupBox2.Location = new System.Drawing.Point(5, 55);
         this.groupBox2.Name = "groupBox2";
         this.groupBox2.Size = new System.Drawing.Size(482, 175);
         this.groupBox2.TabIndex = 31;
         this.groupBox2.TabStop = false;
         // 
         // label23
         // 
         this.label23.AutoSize = true;
         this.label23.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label23.Location = new System.Drawing.Point(317, 81);
         this.label23.Name = "label23";
         this.label23.Size = new System.Drawing.Size(146, 20);
         this.label23.TabIndex = 67;
         this.label23.Text = "out of External time";
         // 
         // label24
         // 
         this.label24.AutoSize = true;
         this.label24.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label24.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label24.Location = new System.Drawing.Point(293, 81);
         this.label24.Name = "label24";
         this.label24.Size = new System.Drawing.Size(21, 18);
         this.label24.TabIndex = 66;
         this.label24.Text = "%";
         // 
         // NetworkPercentageLabel
         // 
         this.NetworkPercentageLabel.AutoSize = true;
         this.NetworkPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.NetworkPercentageLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.NetworkPercentageLabel.Location = new System.Drawing.Point(261, 82);
         this.NetworkPercentageLabel.Name = "NetworkPercentageLabel";
         this.NetworkPercentageLabel.Size = new System.Drawing.Size(35, 18);
         this.NetworkPercentageLabel.TabIndex = 65;
         this.NetworkPercentageLabel.Text = "000";
         this.NetworkPercentageLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // NetworkTimeLabel
         // 
         this.NetworkTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.NetworkTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.NetworkTimeLabel.Location = new System.Drawing.Point(149, 81);
         this.NetworkTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.NetworkTimeLabel.Name = "NetworkTimeLabel";
         this.NetworkTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.NetworkTimeLabel.TabIndex = 64;
         this.NetworkTimeLabel.Text = "00:00:00.000";
         // 
         // label27
         // 
         this.label27.AutoSize = true;
         this.label27.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label27.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label27.Location = new System.Drawing.Point(19, 80);
         this.label27.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label27.Name = "label27";
         this.label27.Size = new System.Drawing.Size(79, 20);
         this.label27.TabIndex = 63;
         this.label27.Text = "Network:";
         // 
         // label22
         // 
         this.label22.AutoSize = true;
         this.label22.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label22.Location = new System.Drawing.Point(317, 145);
         this.label22.Name = "label22";
         this.label22.Size = new System.Drawing.Size(146, 20);
         this.label22.TabIndex = 62;
         this.label22.Text = "out of External time";
         // 
         // label21
         // 
         this.label21.AutoSize = true;
         this.label21.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label21.Location = new System.Drawing.Point(317, 113);
         this.label21.Name = "label21";
         this.label21.Size = new System.Drawing.Size(146, 20);
         this.label21.TabIndex = 61;
         this.label21.Text = "out of External time";
         // 
         // label20
         // 
         this.label20.AutoSize = true;
         this.label20.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label20.Location = new System.Drawing.Point(317, 48);
         this.label20.Name = "label20";
         this.label20.Size = new System.Drawing.Size(145, 20);
         this.label20.TabIndex = 60;
         this.label20.Text = "out of Session time";
         // 
         // label19
         // 
         this.label19.AutoSize = true;
         this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label19.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label19.Location = new System.Drawing.Point(293, 145);
         this.label19.Name = "label19";
         this.label19.Size = new System.Drawing.Size(21, 18);
         this.label19.TabIndex = 59;
         this.label19.Text = "%";
         // 
         // label17
         // 
         this.label17.AutoSize = true;
         this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label17.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label17.Location = new System.Drawing.Point(293, 113);
         this.label17.Name = "label17";
         this.label17.Size = new System.Drawing.Size(21, 18);
         this.label17.TabIndex = 58;
         this.label17.Text = "%";
         // 
         // label15
         // 
         this.label15.AutoSize = true;
         this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label15.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label15.Location = new System.Drawing.Point(293, 48);
         this.label15.Name = "label15";
         this.label15.Size = new System.Drawing.Size(21, 18);
         this.label15.TabIndex = 57;
         this.label15.Text = "%";
         // 
         // ServerPercentageLabel
         // 
         this.ServerPercentageLabel.AutoSize = true;
         this.ServerPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.ServerPercentageLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.ServerPercentageLabel.Location = new System.Drawing.Point(261, 145);
         this.ServerPercentageLabel.Name = "ServerPercentageLabel";
         this.ServerPercentageLabel.Size = new System.Drawing.Size(35, 18);
         this.ServerPercentageLabel.TabIndex = 56;
         this.ServerPercentageLabel.Text = "000";
         this.ServerPercentageLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // MiddlewarePercentageLabel
         // 
         this.MiddlewarePercentageLabel.AutoSize = true;
         this.MiddlewarePercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.MiddlewarePercentageLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.MiddlewarePercentageLabel.Location = new System.Drawing.Point(261, 113);
         this.MiddlewarePercentageLabel.Name = "MiddlewarePercentageLabel";
         this.MiddlewarePercentageLabel.Size = new System.Drawing.Size(35, 18);
         this.MiddlewarePercentageLabel.TabIndex = 55;
         this.MiddlewarePercentageLabel.Text = "000";
         this.MiddlewarePercentageLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // ExternalPercentageLabel
         // 
         this.ExternalPercentageLabel.AutoSize = true;
         this.ExternalPercentageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.ExternalPercentageLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.ExternalPercentageLabel.Location = new System.Drawing.Point(261, 48);
         this.ExternalPercentageLabel.Name = "ExternalPercentageLabel";
         this.ExternalPercentageLabel.Size = new System.Drawing.Size(35, 18);
         this.ExternalPercentageLabel.TabIndex = 54;
         this.ExternalPercentageLabel.Text = "000";
         this.ExternalPercentageLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // ExternalTimeLabel
         // 
         this.ExternalTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.ExternalTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.ExternalTimeLabel.Location = new System.Drawing.Point(149, 48);
         this.ExternalTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.ExternalTimeLabel.Name = "ExternalTimeLabel";
         this.ExternalTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.ExternalTimeLabel.TabIndex = 49;
         this.ExternalTimeLabel.Text = "00:00:00.000";
         // 
         // label9
         // 
         this.label9.AutoSize = true;
         this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label9.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label9.Location = new System.Drawing.Point(19, 48);
         this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label9.Name = "label9";
         this.label9.Size = new System.Drawing.Size(80, 20);
         this.label9.TabIndex = 48;
         this.label9.Text = "External:";
         // 
         // MiddlewareTimeLabel
         // 
         this.MiddlewareTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.MiddlewareTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.MiddlewareTimeLabel.Location = new System.Drawing.Point(149, 113);
         this.MiddlewareTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.MiddlewareTimeLabel.Name = "MiddlewareTimeLabel";
         this.MiddlewareTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.MiddlewareTimeLabel.TabIndex = 53;
         this.MiddlewareTimeLabel.Text = "00:00:00.000";
         // 
         // label5
         // 
         this.label5.AutoSize = true;
         this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label5.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label5.Location = new System.Drawing.Point(19, 113);
         this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(104, 20);
         this.label5.TabIndex = 52;
         this.label5.Text = "Middleware:";
         // 
         // ServerTimeLabel
         // 
         this.ServerTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.ServerTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.ServerTimeLabel.Location = new System.Drawing.Point(149, 145);
         this.ServerTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.ServerTimeLabel.Name = "ServerTimeLabel";
         this.ServerTimeLabel.Size = new System.Drawing.Size(117, 20);
         this.ServerTimeLabel.TabIndex = 51;
         this.ServerTimeLabel.Text = "00:00:00.000";
         // 
         // label12
         // 
         this.label12.AutoSize = true;
         this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label12.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label12.Location = new System.Drawing.Point(19, 145);
         this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label12.Name = "label12";
         this.label12.Size = new System.Drawing.Size(66, 20);
         this.label12.TabIndex = 50;
         this.label12.Text = "Server:";
         // 
         // SessionTimeLabel
         // 
         this.SessionTimeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.SessionTimeLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.SessionTimeLabel.Location = new System.Drawing.Point(149, 18);
         this.SessionTimeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.SessionTimeLabel.Name = "SessionTimeLabel";
         this.SessionTimeLabel.Size = new System.Drawing.Size(116, 20);
         this.SessionTimeLabel.TabIndex = 47;
         this.SessionTimeLabel.Text = "00:00:00.000";
         // 
         // label6
         // 
         this.label6.AutoSize = true;
         this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label6.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label6.Location = new System.Drawing.Point(19, 17);
         this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label6.Name = "label6";
         this.label6.Size = new System.Drawing.Size(78, 20);
         this.label6.TabIndex = 46;
         this.label6.Text = "Session:";
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.label13);
         this.groupBox1.Controls.Add(this.label11);
         this.groupBox1.Controls.Add(this.labelDownloadedCompressionRatio);
         this.groupBox1.Controls.Add(this.labelUploadedCompressionRatio);
         this.groupBox1.Controls.Add(this.label10);
         this.groupBox1.Controls.Add(this.label8);
         this.groupBox1.Controls.Add(this.label2);
         this.groupBox1.Controls.Add(this.labelUploadedKB);
         this.groupBox1.Controls.Add(this.label14);
         this.groupBox1.Controls.Add(this.labelDownloadedKB);
         this.groupBox1.Location = new System.Drawing.Point(5, 232);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(482, 92);
         this.groupBox1.TabIndex = 30;
         this.groupBox1.TabStop = false;
         // 
         // label13
         // 
         this.label13.AutoSize = true;
         this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label13.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label13.Location = new System.Drawing.Point(426, 61);
         this.label13.Name = "label13";
         this.label13.Size = new System.Drawing.Size(21, 18);
         this.label13.TabIndex = 34;
         this.label13.Text = "%";
         // 
         // label11
         // 
         this.label11.AutoSize = true;
         this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label11.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label11.Location = new System.Drawing.Point(426, 26);
         this.label11.Name = "label11";
         this.label11.Size = new System.Drawing.Size(21, 18);
         this.label11.TabIndex = 33;
         this.label11.Text = "%";
         // 
         // labelDownloadedCompressionRatio
         // 
         this.labelDownloadedCompressionRatio.AutoSize = true;
         this.labelDownloadedCompressionRatio.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelDownloadedCompressionRatio.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelDownloadedCompressionRatio.Location = new System.Drawing.Point(397, 59);
         this.labelDownloadedCompressionRatio.Name = "labelDownloadedCompressionRatio";
         this.labelDownloadedCompressionRatio.Size = new System.Drawing.Size(32, 18);
         this.labelDownloadedCompressionRatio.TabIndex = 32;
         this.labelDownloadedCompressionRatio.Text = "000";
         this.labelDownloadedCompressionRatio.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // labelUploadedCompressionRatio
         // 
         this.labelUploadedCompressionRatio.AutoSize = true;
         this.labelUploadedCompressionRatio.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelUploadedCompressionRatio.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelUploadedCompressionRatio.Location = new System.Drawing.Point(397, 25);
         this.labelUploadedCompressionRatio.Name = "labelUploadedCompressionRatio";
         this.labelUploadedCompressionRatio.Size = new System.Drawing.Size(32, 18);
         this.labelUploadedCompressionRatio.TabIndex = 31;
         this.labelUploadedCompressionRatio.Text = "000";
         this.labelUploadedCompressionRatio.TextAlign = System.Drawing.ContentAlignment.TopRight;
         // 
         // label10
         // 
         this.label10.AutoSize = true;
         this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label10.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label10.Location = new System.Drawing.Point(231, 59);
         this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label10.Name = "label10";
         this.label10.Size = new System.Drawing.Size(159, 20);
         this.label10.TabIndex = 30;
         this.label10.Text = "Compression ratio:";
         // 
         // label8
         // 
         this.label8.AutoSize = true;
         this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label8.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label8.Location = new System.Drawing.Point(231, 23);
         this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label8.Name = "label8";
         this.label8.Size = new System.Drawing.Size(159, 20);
         this.label8.TabIndex = 29;
         this.label8.Text = "Compression ratio:";
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label2.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label2.Location = new System.Drawing.Point(22, 24);
         this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(91, 20);
         this.label2.TabIndex = 24;
         this.label2.Text = "Uploaded:";
         // 
         // labelUploadedKB
         // 
         this.labelUploadedKB.AutoSize = true;
         this.labelUploadedKB.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelUploadedKB.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelUploadedKB.Location = new System.Drawing.Point(149, 24);
         this.labelUploadedKB.Name = "labelUploadedKB";
         this.labelUploadedKB.Size = new System.Drawing.Size(60, 18);
         this.labelUploadedKB.TabIndex = 28;
         this.labelUploadedKB.Text = "000,000";
         // 
         // label14
         // 
         this.label14.AutoSize = true;
         this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label14.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label14.Location = new System.Drawing.Point(22, 59);
         this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label14.Name = "label14";
         this.label14.Size = new System.Drawing.Size(113, 20);
         this.label14.TabIndex = 27;
         this.label14.Text = "Downloaded:";
         // 
         // labelDownloadedKB
         // 
         this.labelDownloadedKB.AutoSize = true;
         this.labelDownloadedKB.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelDownloadedKB.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelDownloadedKB.Location = new System.Drawing.Point(149, 59);
         this.labelDownloadedKB.Name = "labelDownloadedKB";
         this.labelDownloadedKB.Size = new System.Drawing.Size(60, 18);
         this.labelDownloadedKB.TabIndex = 25;
         this.labelDownloadedKB.Text = "000,000";
         // 
         // labelRequests
         // 
         this.labelRequests.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelRequests.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelRequests.Location = new System.Drawing.Point(152, 32);
         this.labelRequests.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.labelRequests.Name = "labelRequests";
         this.labelRequests.Size = new System.Drawing.Size(89, 20);
         this.labelRequests.TabIndex = 13;
         this.labelRequests.Text = "000,000,000";
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label1.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label1.Location = new System.Drawing.Point(22, 32);
         this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(91, 20);
         this.label1.TabIndex = 12;
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
         // labelInternalLogFile
         // 
         this.labelInternalLogFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelInternalLogFile.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelInternalLogFile.Location = new System.Drawing.Point(125, 148);
         this.labelInternalLogFile.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.labelInternalLogFile.Name = "labelInternalLogFile";
         this.labelInternalLogFile.Size = new System.Drawing.Size(360, 49);
         this.labelInternalLogFile.TabIndex = 21;
         this.labelInternalLogFile.Text = "C:\\Documents and Settings\\MyUser\\Desktop\\MgxpaRIA_YYYY_MM_DD.log";
         // 
         // label7
         // 
         this.label7.AutoSize = true;
         this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label7.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label7.Location = new System.Drawing.Point(23, 148);
         this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label7.Name = "label7";
         this.label7.Size = new System.Drawing.Size(78, 20);
         this.label7.TabIndex = 20;
         this.label7.Text = "Log File:";
         // 
         // labelInternalLogLevel
         // 
         this.labelInternalLogLevel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelInternalLogLevel.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelInternalLogLevel.Location = new System.Drawing.Point(125, 118);
         this.labelInternalLogLevel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.labelInternalLogLevel.Name = "labelInternalLogLevel";
         this.labelInternalLogLevel.Size = new System.Drawing.Size(360, 30);
         this.labelInternalLogLevel.TabIndex = 19;
         this.labelInternalLogLevel.Text = "Server | Support | Development | Gui";
         // 
         // label4
         // 
         this.label4.AutoSize = true;
         this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label4.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label4.Location = new System.Drawing.Point(23, 118);
         this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(91, 20);
         this.label4.TabIndex = 18;
         this.label4.Text = "Log Level:";
         // 
         // labelProgram
         // 
         this.labelProgram.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelProgram.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelProgram.Location = new System.Drawing.Point(125, 88);
         this.labelProgram.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.labelProgram.Name = "labelProgram";
         this.labelProgram.Size = new System.Drawing.Size(360, 20);
         this.labelProgram.TabIndex = 17;
         this.labelProgram.Text = "MyProgram";
         // 
         // label3
         // 
         this.label3.AutoSize = true;
         this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label3.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label3.Location = new System.Drawing.Point(23, 88);
         this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(81, 20);
         this.label3.TabIndex = 16;
         this.label3.Text = "Program:";
         // 
         // labelApplication
         // 
         this.labelApplication.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelApplication.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelApplication.Location = new System.Drawing.Point(125, 58);
         this.labelApplication.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.labelApplication.Name = "labelApplication";
         this.labelApplication.Size = new System.Drawing.Size(360, 20);
         this.labelApplication.TabIndex = 15;
         this.labelApplication.Text = "MyApplication";
         // 
         // label16
         // 
         this.label16.AutoSize = true;
         this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label16.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label16.Location = new System.Drawing.Point(22, 58);
         this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label16.Name = "label16";
         this.label16.Size = new System.Drawing.Size(103, 20);
         this.label16.TabIndex = 14;
         this.label16.Text = "Application:";
         // 
         // labelServerURL
         // 
         this.labelServerURL.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.labelServerURL.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.labelServerURL.Location = new System.Drawing.Point(125, 28);
         this.labelServerURL.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.labelServerURL.Name = "labelServerURL";
         this.labelServerURL.Size = new System.Drawing.Size(360, 20);
         this.labelServerURL.TabIndex = 13;
         this.labelServerURL.Text = "http://MyServer.MyDomain/MagicScripts/mgrqispi.dll";
         // 
         // label18
         // 
         this.label18.AutoSize = true;
         this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.label18.ForeColor = System.Drawing.SystemColors.HotTrack;
         this.label18.Location = new System.Drawing.Point(22, 28);
         this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
         this.label18.Name = "label18";
         this.label18.Size = new System.Drawing.Size(66, 20);
         this.label18.TabIndex = 12;
         this.label18.Text = "Server:";
         // 
         // SessionStatisticsForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
         this.ClientSize = new System.Drawing.Size(516, 557);
         this.Controls.Add(this.groupBoxExecutionProperties);
         this.Controls.Add(this.groupBoxStatistics);
         this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
         this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
         this.Name = "SessionStatisticsForm";
         this.Text = "Session Summary";
         this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SessionStatistics_KeyUp);
         this.groupBoxStatistics.ResumeLayout(false);
         this.groupBoxStatistics.PerformLayout();
         this.groupBox2.ResumeLayout(false);
         this.groupBox2.PerformLayout();
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this.groupBoxExecutionProperties.ResumeLayout(false);
         this.groupBoxExecutionProperties.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private GroupBox groupBoxStatistics;
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
      private System.Windows.Forms.Label labelDownloadedKB;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.Label labelUploadedKB;
      private System.Windows.Forms.Label label14;
      private GroupBox groupBox1;
      private System.Windows.Forms.Label label10;
      private System.Windows.Forms.Label label8;
      private System.Windows.Forms.Label labelDownloadedCompressionRatio;
      private System.Windows.Forms.Label labelUploadedCompressionRatio;
      private System.Windows.Forms.Label label11;
      private System.Windows.Forms.Label label13;
      private GroupBox groupBox2;
      private System.Windows.Forms.Label label23;
      private System.Windows.Forms.Label label24;
      private System.Windows.Forms.Label NetworkPercentageLabel;
      private System.Windows.Forms.Label NetworkTimeLabel;
      private System.Windows.Forms.Label label27;
      private System.Windows.Forms.Label label22;
      private System.Windows.Forms.Label label21;
      private System.Windows.Forms.Label label20;
      private System.Windows.Forms.Label label19;
      private System.Windows.Forms.Label label17;
      private System.Windows.Forms.Label label15;
      private System.Windows.Forms.Label ServerPercentageLabel;
      private System.Windows.Forms.Label MiddlewarePercentageLabel;
      private System.Windows.Forms.Label ExternalPercentageLabel;
      private System.Windows.Forms.Label ExternalTimeLabel;
      private System.Windows.Forms.Label label9;
      private System.Windows.Forms.Label MiddlewareTimeLabel;
      private System.Windows.Forms.Label label5;
      private System.Windows.Forms.Label ServerTimeLabel;
      private System.Windows.Forms.Label label12;
      private System.Windows.Forms.Label SessionTimeLabel;
      private System.Windows.Forms.Label label6;
   }
}