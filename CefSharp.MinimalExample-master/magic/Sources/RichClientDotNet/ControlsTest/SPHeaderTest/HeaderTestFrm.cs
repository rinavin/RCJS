using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using com.magicsoftware.controls;
#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using LeftRightAlignment = com.magicsoftware.mobilestubs.LeftRightAlignment;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using GroupBox = com.magicsoftware.mobilestubs.GroupBox;
using Enum = OpenNETCF.Enum2;
using FlatStyle = com.magicsoftware.mobilestubs.FlatStyle;
using TextBox = com.magicsoftware.controls.MgTextBox;
#endif

namespace ListApp
{
	/// <summary>
	/// Summary description for MainFrm.
	/// </summary>
	public class HeaderTest : System.Windows.Forms.Form
	{
      private MgPushButton btnUpdateItem;
      private MgPushButton btnDeleteItem;
      private MgPushButton btnInserItem;
      private MgPushButton btnAddItem;
      private Label label1;
      private Label lblBitmapMargine;
      private CheckBox cbFullDrag;
      private CheckBox cbAllowDrag;
      private CheckBox cbFlat;
      private CheckBox cbHotTrack;
      private CheckBox cbClickable;
      private MgPushButton btnRefreshItem;
      private Button btnRefreshCtrl;
      private Label lblText;
      private System.Windows.Forms.TextBox textText;
      private Label lblWidth;
      private System.Windows.Forms.ComboBox comboCurrentItem;
      private System.Windows.Forms.TextBox textWidth;
      private System.Windows.Forms.TextBox textBmpMargin;
      private System.Windows.Forms.ComboBox comboImageIndex;
      private Label lblImageIndex;
      private Label label2;
      private System.Windows.Forms.ComboBox comboBitmap;
      private Label label3;
      private Label label4;
      private Label label5;
      private System.Windows.Forms.ComboBox comboContentAlign;
      private System.Windows.Forms.ComboBox comboImageAlign;
      private System.Windows.Forms.ComboBox comboSortMark;
      private Label label6;
      private TextBox textEvents;
      private MgPushButton btnClearNotifications;
      private System.Windows.Forms.ComboBox comboRightToLeft;
#if !PocketPC
      private GroupBox gbHeaderItemProperties;
		private GroupBox gpHeaderProperties;
      private GroupBox gbNotifications;
#else
      private TabControl tab;
      private TabPage gbHeaderItemProperties;
      private TabPage gpHeaderProperties;
      private TabPage gbNotifications;
#endif
      private System.Windows.Forms.ImageList ilCards;
      private System.Windows.Forms.ComboBox comboImageList;
      private Label label7;
      private Button btnUpdate;

      private com.magicsoftware.controls.Header header;

      private System.ComponentModel.IContainer components;

		public HeaderTest()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.header = new Header();
         this.header.Location = new System.Drawing.Point(8, 8);
         this.header.Size = new Size(100, 20);
         this.header.Dock = DockStyle.Top;

			//      this.header.TabIndex = 0;
			//      this.header.Height = this.Font.Height + SystemInformation.Border3DSize.Height * 2;

			//      this.header.Clickable = true;
			//      this.header.HotTrack = false;
			//      this.header.FullDrag = true;
			//      this.header.AllowDrag = true;

			this.header.SectionClick += new HeaderSectionEventHandler(this.OnHeaderSectionClick);
			this.header.SectionDblClick += new HeaderSectionEventHandler(this.OnHeaderSectionDblClick);
			this.header.DividerDblClick += new HeaderSectionEventHandler(this.OnHeaderSectionDividerDblClick);
			this.header.BeforeSectionTrack += new HeaderSectionWidthConformableEventHandler(this.OnBeforeHeaderSectionTrack);
			this.header.SectionTracking += new HeaderSectionWidthConformableEventHandler(this.OnHeaderSectionTracking);
			this.header.AfterSectionTrack += new HeaderSectionWidthEventHandler(this.OnAfterHeaderSectionTrack);
			this.header.BeforeSectionDrag += new HeaderSectionOrderConformableEventHandler(this.OnBeforeHeaderSectionDrag);
			this.header.AfterSectionDrag += new HeaderSectionOrderConformableEventHandler(this.OnAfterHeaderSectionDrag);

         this.header.AllowDragSections = true;
         this.header.Clickable = true;
         this.header.FullDragSections = true;
         this.header.HotTrack = true;
#if PocketPC
         this.Controls.Add(this.header);
         header.Init();
#endif
         int idx;
         idx = this.header.Sections.Add(new HeaderSection("Card \n 1", 50));
         header.Sections[idx].Color = Color.Blue;
         header.Sections[idx].ContentAlignment = ContentAlignment.TopCenter;
         idx = this.header.Sections.Add(new HeaderSection("Card 2", 75));
         header.Sections[idx].Color = Color.Red;
         header.Sections[idx].ContentAlignment = ContentAlignment.MiddleCenter;
         header.Sections[idx].SortMark = HeaderSectionSortMarks.Up;
         idx = this.header.Sections.Add(new HeaderSection("Card 3", 100));
         header.Sections[idx].Color = Color.Purple;
         header.Sections[idx].ContentAlignment = ContentAlignment.BottomRight;
         header.Sections[idx].SortMark = HeaderSectionSortMarks.Down;

         
         idx = this.header.Sections.Add(new HeaderSection("Card 4", 125));
         header.Sections[idx].Color = Color.Green;
         header.Sections[idx].Font = new Font("Miriam", 16, FontStyle.Regular);
         /*this.header.Sections.Add(new HeaderSection("", 50));
         this.header.Sections.Add(new HeaderSection("", 75));
         this.header.Sections.Add(new HeaderSection("", 100));
         this.header.Sections.Add(new HeaderSection("", 125));*/

			this.comboImageList.Items.Add("Cards");

			this.comboCurrentItem.Items.Clear();
			for ( int i = 0; i < this.header.Sections.Count; i++ )
			{
				this.comboCurrentItem.Items.Add(i);
			}

			this.comboImageIndex.Items.Clear();
			for ( int i = 0; i < this.ilCards.Images.Count; i++ )
			{
				this.comboImageIndex.Items.Add(i);
			}

#if !PocketPC
			this.comboRightToLeft.Items.AddRange(Enum.GetNames(typeof(RightToLeft)));
         this.comboContentAlign.Items.AddRange(Enum.GetNames(typeof(ContentAlignment)));
         this.comboImageAlign.Items.AddRange(Enum.GetNames(typeof(LeftRightAlignment)));
         this.comboSortMark.Items.AddRange(Enum.GetNames(typeof(HeaderSectionSortMarks)));
#else
         foreach (Object item in Enum.GetNames(typeof(RightToLeft)))
            this.comboRightToLeft.Items.Add(item);

         foreach (Object item in Enum.GetNames(typeof(ContentAlignment)))
            this.comboContentAlign.Items.Add(item);

         foreach (Object item in Enum.GetNames(typeof(LeftRightAlignment)))
            this.comboImageAlign.Items.Add(item);

         foreach (Object item in Enum.GetNames(typeof(HeaderSectionSortMarks)))
            this.comboSortMark.Items.Add(item);
#endif

#if !PocketPC
			this.Controls.Add(this.header);
#endif

         RetrieveControlProperties();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
         this.components = new System.ComponentModel.Container();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HeaderTest));
#if PocketPC
         this.tab = new TabControl();
         tab.Bounds = new Rectangle(0, 0, 200, 250);
         tab.Bounds = new Rectangle(0, 0, 100, 270);
#endif
#if !PocketPC
         this.gbHeaderItemProperties = new GroupBox();
#else
         this.gbHeaderItemProperties = new TabPage();
#endif
         this.comboRightToLeft = new ComboBox();
         this.label6 = new Label();
         this.comboSortMark = new ComboBox();
         this.label5 = new Label();
         this.comboImageAlign = new ComboBox();
         this.label4 = new Label();
         this.comboContentAlign = new ComboBox();
         this.label3 = new Label();
         this.label2 = new Label();
         this.comboBitmap = new ComboBox();
         this.lblImageIndex = new Label();
         this.comboImageIndex = new ComboBox();
         this.textWidth = new System.Windows.Forms.TextBox();
         this.lblWidth = new Label();
         this.lblText = new Label();
         this.textText = new System.Windows.Forms.TextBox();
         this.btnRefreshItem = new MgPushButton();
         this.label1 = new Label();
         this.btnUpdateItem = new MgPushButton();
         this.btnDeleteItem = new MgPushButton();
         this.btnInserItem = new MgPushButton();
         this.btnAddItem = new MgPushButton();
         this.comboCurrentItem = new ComboBox();
#if !PocketPC
         this.gpHeaderProperties = new GroupBox();
#else
         this.gpHeaderProperties = new TabPage();
#endif
         this.btnUpdate = new Button();
         this.label7 = new Label();
         this.comboImageList = new ComboBox();
         this.textBmpMargin = new System.Windows.Forms.TextBox();
         this.btnRefreshCtrl = new Button();
         this.lblBitmapMargine = new Label();
         this.cbFullDrag = new CheckBox();
         this.cbAllowDrag = new CheckBox();
         this.cbFlat = new CheckBox();
         this.cbHotTrack = new CheckBox();
         this.cbClickable = new CheckBox();
#if !PocketPC
         this.gbNotifications = new GroupBox();
#else
         this.gbNotifications = new TabPage();
#endif
         this.btnClearNotifications = new MgPushButton();
         this.textEvents = new TextBox();
#if !PocketPC
         this.ilCards = new System.Windows.Forms.ImageList(this.components);
#else
         this.ilCards = new System.Windows.Forms.ImageList();
#endif
         this.gbHeaderItemProperties.SuspendLayout();
         this.gpHeaderProperties.SuspendLayout();
         this.gbNotifications.SuspendLayout();
         this.SuspendLayout();
         // 
         // gbHeaderItemProperties
         // 
#if !PocketPC
         this.gbHeaderItemProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
#endif
         this.gbHeaderItemProperties.Controls.Add(this.comboRightToLeft);
         this.gbHeaderItemProperties.Controls.Add(this.label6);
         this.gbHeaderItemProperties.Controls.Add(this.comboSortMark);
         this.gbHeaderItemProperties.Controls.Add(this.label5);
         this.gbHeaderItemProperties.Controls.Add(this.comboImageAlign);
         this.gbHeaderItemProperties.Controls.Add(this.label4);
         this.gbHeaderItemProperties.Controls.Add(this.comboContentAlign);
         this.gbHeaderItemProperties.Controls.Add(this.label3);
         this.gbHeaderItemProperties.Controls.Add(this.label2);
         this.gbHeaderItemProperties.Controls.Add(this.comboBitmap);
         this.gbHeaderItemProperties.Controls.Add(this.lblImageIndex);
         this.gbHeaderItemProperties.Controls.Add(this.comboImageIndex);
         this.gbHeaderItemProperties.Controls.Add(this.textWidth);
         this.gbHeaderItemProperties.Controls.Add(this.lblWidth);
         this.gbHeaderItemProperties.Controls.Add(this.lblText);
         this.gbHeaderItemProperties.Controls.Add(this.textText);
         this.gbHeaderItemProperties.Controls.Add(this.btnRefreshItem);
         this.gbHeaderItemProperties.Controls.Add(this.label1);
         this.gbHeaderItemProperties.Controls.Add(this.btnUpdateItem);
         this.gbHeaderItemProperties.Controls.Add(this.btnDeleteItem);
         this.gbHeaderItemProperties.Controls.Add(this.btnInserItem);
         this.gbHeaderItemProperties.Controls.Add(this.btnAddItem);
         this.gbHeaderItemProperties.Controls.Add(this.comboCurrentItem);
#if !PocketPC
         this.gbHeaderItemProperties.FlatStyle = System.Windows.Forms.FlatStyle.System;
#endif
         this.gbHeaderItemProperties.Location = new System.Drawing.Point(200, 48);
         this.gbHeaderItemProperties.Name = "gbHeaderItemProperties";
         this.gbHeaderItemProperties.Size = new System.Drawing.Size(352, 288);
         this.gbHeaderItemProperties.TabIndex = 2;
         this.gbHeaderItemProperties.TabStop = false;
         this.gbHeaderItemProperties.Text = "&Item Properties";
#if !PocketPC
         this.gbHeaderItemProperties.Enter += new System.EventHandler(this.gbHeaderItemProperties_Enter);
#endif
         // 
         // comboRightToLeft
         // 
         this.comboRightToLeft.Location = new System.Drawing.Point(96, 152);
         this.comboRightToLeft.Name = "comboRightToLeft";
         this.comboRightToLeft.Size = new System.Drawing.Size(96, 21);
         this.comboRightToLeft.TabIndex = 12;
         // 
         // label6
         // 
         this.label6.FlatStyle = FlatStyle.System;
         this.label6.Location = new System.Drawing.Point(8, 152);
         this.label6.Name = "label6";
         this.label6.Size = new System.Drawing.Size(72, 23);
         this.label6.TabIndex = 11;
         this.label6.Text = "Right To Left:";
         // 
         // comboSortMark
         // 
         this.comboSortMark.Location = new System.Drawing.Point(96, 224);
         this.comboSortMark.Name = "comboSortMark";
         this.comboSortMark.Size = new System.Drawing.Size(96, 21);
         this.comboSortMark.TabIndex = 18;
         // 
         // label5
         // 
         this.label5.FlatStyle = FlatStyle.System;
         this.label5.Location = new System.Drawing.Point(8, 224);
         this.label5.Name = "label5";
         this.label5.Size = new System.Drawing.Size(64, 16);
         this.label5.TabIndex = 17;
         this.label5.Text = "Sort mark:";
         // 
         // comboImageAlign
         // 
         this.comboImageAlign.Location = new System.Drawing.Point(96, 200);
         this.comboImageAlign.Name = "comboImageAlign";
         this.comboImageAlign.Size = new System.Drawing.Size(96, 21);
         this.comboImageAlign.TabIndex = 16;
         // 
         // label4
         // 
         this.label4.FlatStyle = FlatStyle.System;
         this.label4.Location = new System.Drawing.Point(8, 200);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(72, 16);
         this.label4.TabIndex = 15;
         this.label4.Text = "Image Align:";
         // 
         // comboContentAlign
         // 
         this.comboContentAlign.Location = new System.Drawing.Point(96, 176);
         this.comboContentAlign.Name = "comboContentAlign";
         this.comboContentAlign.Size = new System.Drawing.Size(96, 21);
         this.comboContentAlign.TabIndex = 14;
         // 
         // label3
         // 
         this.label3.FlatStyle = FlatStyle.System;
         this.label3.Location = new System.Drawing.Point(8, 176);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(80, 16);
         this.label3.TabIndex = 13;
         this.label3.Text = "Content Align:";
         // 
         // label2
         // 
         this.label2.FlatStyle = FlatStyle.System;
         this.label2.Location = new System.Drawing.Point(8, 128);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(40, 16);
         this.label2.TabIndex = 9;
         this.label2.Text = "Bitmap:";
         // 
         // comboBitmap
         // 
         this.comboBitmap.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.comboBitmap.Location = new System.Drawing.Point(96, 128);
         this.comboBitmap.Name = "comboBitmap";
         this.comboBitmap.Size = new System.Drawing.Size(248, 21);
         this.comboBitmap.TabIndex = 10;
         // 
         // lblImageIndex
         // 
         this.lblImageIndex.FlatStyle = FlatStyle.System;
         this.lblImageIndex.Location = new System.Drawing.Point(8, 104);
         this.lblImageIndex.Name = "lblImageIndex";
         this.lblImageIndex.Size = new System.Drawing.Size(72, 16);
         this.lblImageIndex.TabIndex = 7;
         this.lblImageIndex.Text = "Image index:";
         // 
         // comboImageIndex
         // 
         this.comboImageIndex.Location = new System.Drawing.Point(96, 104);
         this.comboImageIndex.Name = "comboImageIndex";
         this.comboImageIndex.Size = new System.Drawing.Size(72, 21);
         this.comboImageIndex.TabIndex = 8;
         // 
         // textWidth
         // 
         this.textWidth.Location = new System.Drawing.Point(96, 80);
         this.textWidth.Name = "textWidth";
         this.textWidth.Size = new System.Drawing.Size(72, 20);
         this.textWidth.TabIndex = 6;
         this.textWidth.Text = "100";
         this.textWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
         // 
         // lblWidth
         // 
         this.lblWidth.FlatStyle = FlatStyle.System;
         this.lblWidth.Location = new System.Drawing.Point(8, 80);
         this.lblWidth.Name = "lblWidth";
         this.lblWidth.Size = new System.Drawing.Size(40, 23);
         this.lblWidth.TabIndex = 5;
         this.lblWidth.Text = "Width";
         // 
         // lblText
         // 
         this.lblText.FlatStyle = FlatStyle.System;
         this.lblText.Location = new System.Drawing.Point(8, 56);
         this.lblText.Name = "lblText";
         this.lblText.Size = new System.Drawing.Size(40, 16);
         this.lblText.TabIndex = 3;
         this.lblText.Text = "Text";
         // 
         // textText
         // 
         this.textText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.textText.Location = new System.Drawing.Point(96, 56);
         this.textText.Name = "textText";
         this.textText.Size = new System.Drawing.Size(248, 20);
         this.textText.TabIndex = 4;
         this.textText.Text = "Item";
         // 
         // btnRefreshItem
         // 
         this.btnRefreshItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnRefreshItem.FlatStyle = FlatStyle.System;
         this.btnRefreshItem.Location = new System.Drawing.Point(8, 256);
         this.btnRefreshItem.Name = "btnRefreshItem";
         this.btnRefreshItem.Size = new System.Drawing.Size(72, 23);
         this.btnRefreshItem.TabIndex = 19;
         this.btnRefreshItem.Text = "Re&fresh";
         this.btnRefreshItem.Click += new System.EventHandler(this.btnRefreshItem_Click);
         // 
         // label1
         // 
         this.label1.FlatStyle = FlatStyle.System;
         this.label1.Location = new System.Drawing.Point(8, 24);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(48, 16);
         this.label1.TabIndex = 1;
         this.label1.Text = "Current:";
         // 
         // btnUpdateItem
         // 
         this.btnUpdateItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.btnUpdateItem.FlatStyle = FlatStyle.System;
         this.btnUpdateItem.Location = new System.Drawing.Point(96, 256);
         this.btnUpdateItem.Name = "btnUpdateItem";
         this.btnUpdateItem.Size = new System.Drawing.Size(56, 23);
         this.btnUpdateItem.TabIndex = 20;
         this.btnUpdateItem.Text = "Up&date";
         this.btnUpdateItem.Click += new System.EventHandler(this.btnUpdateItem_Click);
         // 
         // btnDeleteItem
         // 
         this.btnDeleteItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.btnDeleteItem.FlatStyle = FlatStyle.System;
         this.btnDeleteItem.Location = new System.Drawing.Point(288, 256);
         this.btnDeleteItem.Name = "btnDeleteItem";
         this.btnDeleteItem.Size = new System.Drawing.Size(56, 23);
         this.btnDeleteItem.TabIndex = 23;
         this.btnDeleteItem.Text = "&Delete";
         this.btnDeleteItem.Click += new System.EventHandler(this.btnDeleteItem_Click);
         // 
         // btnInserItem
         // 
         this.btnInserItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.btnInserItem.FlatStyle = FlatStyle.System;
         this.btnInserItem.Location = new System.Drawing.Point(224, 256);
         this.btnInserItem.Name = "btnInserItem";
         this.btnInserItem.Size = new System.Drawing.Size(56, 23);
         this.btnInserItem.TabIndex = 22;
         this.btnInserItem.Text = "&Insert";
         this.btnInserItem.Click += new System.EventHandler(this.btnInserItem_Click);
         // 
         // btnAddItem
         // 
         this.btnAddItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.btnAddItem.FlatStyle = FlatStyle.System;
         this.btnAddItem.Location = new System.Drawing.Point(160, 256);
         this.btnAddItem.Name = "btnAddItem";
         this.btnAddItem.Size = new System.Drawing.Size(56, 23);
         this.btnAddItem.TabIndex = 21;
         this.btnAddItem.Text = "&Add";
         this.btnAddItem.Click += new System.EventHandler(this.btnAddItem_Click);
         // 
         // comboCurrentItem
         // 
         this.comboCurrentItem.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.comboCurrentItem.Location = new System.Drawing.Point(96, 24);
         this.comboCurrentItem.Name = "comboCurrentItem";
         this.comboCurrentItem.Size = new System.Drawing.Size(160, 21);
         this.comboCurrentItem.TabIndex = 2;
         this.comboCurrentItem.SelectedIndexChanged += new System.EventHandler(this.comboCurrentItem_SelectedIndexChanged);
         // 
         // gpHeaderProperties
         // 
         this.gpHeaderProperties.Controls.Add(this.btnUpdate);
         this.gpHeaderProperties.Controls.Add(this.label7);
         this.gpHeaderProperties.Controls.Add(this.comboImageList);
         this.gpHeaderProperties.Controls.Add(this.textBmpMargin);
         this.gpHeaderProperties.Controls.Add(this.btnRefreshCtrl);
         this.gpHeaderProperties.Controls.Add(this.lblBitmapMargine);
         this.gpHeaderProperties.Controls.Add(this.cbFullDrag);
         this.gpHeaderProperties.Controls.Add(this.cbAllowDrag);
         this.gpHeaderProperties.Controls.Add(this.cbFlat);
         this.gpHeaderProperties.Controls.Add(this.cbHotTrack);
         this.gpHeaderProperties.Controls.Add(this.cbClickable);
#if !PocketPC
         this.gpHeaderProperties.FlatStyle = System.Windows.Forms.FlatStyle.System;
#endif
         this.gpHeaderProperties.Location = new System.Drawing.Point(8, 48);
         this.gpHeaderProperties.Name = "gpHeaderProperties";
         this.gpHeaderProperties.Size = new System.Drawing.Size(184, 288);
         this.gpHeaderProperties.TabIndex = 1;
         this.gpHeaderProperties.TabStop = false;
         this.gpHeaderProperties.Text = "Control &Properties";
#if !PocketPC
         this.gpHeaderProperties.Enter += new System.EventHandler(this.gpHeaderProperties_Enter);
#endif
         // 
         // btnUpdate
         // 
         this.btnUpdate.FlatStyle = FlatStyle.System;
         this.btnUpdate.Location = new System.Drawing.Point(104, 256);
         this.btnUpdate.Name = "btnUpdate";
         this.btnUpdate.Size = new System.Drawing.Size(72, 24);
         this.btnUpdate.TabIndex = 11;
         this.btnUpdate.Text = "&Update";
         this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
         // 
         // label7
         // 
         this.label7.FlatStyle = FlatStyle.System;
         this.label7.Location = new System.Drawing.Point(8, 184);
         this.label7.Name = "label7";
         this.label7.Size = new System.Drawing.Size(56, 16);
         this.label7.TabIndex = 10;
         this.label7.Text = "Image List:";
         // 
         // comboImageList
         // 
         this.comboImageList.Location = new System.Drawing.Point(96, 184);
         this.comboImageList.Name = "comboImageList";
         this.comboImageList.Size = new System.Drawing.Size(80, 21);
         this.comboImageList.TabIndex = 9;
         // 
         // textBmpMargin
         // 
         this.textBmpMargin.Location = new System.Drawing.Point(96, 160);
         this.textBmpMargin.Name = "textBmpMargin";
         this.textBmpMargin.Size = new System.Drawing.Size(56, 20);
         this.textBmpMargin.TabIndex = 7;
         this.textBmpMargin.Text = "0";
         this.textBmpMargin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
         // 
         // btnRefreshCtrl
         // 
         this.btnRefreshCtrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.btnRefreshCtrl.FlatStyle = FlatStyle.System;
         this.btnRefreshCtrl.Location = new System.Drawing.Point(8, 256);
         this.btnRefreshCtrl.Name = "btnRefreshCtrl";
         this.btnRefreshCtrl.Size = new System.Drawing.Size(72, 24);
         this.btnRefreshCtrl.TabIndex = 8;
         this.btnRefreshCtrl.Text = "&Refresh";
         this.btnRefreshCtrl.Click += new System.EventHandler(this.btnRefreshCtrl_Click);
         // 
         // lblBitmapMargine
         // 
         this.lblBitmapMargine.FlatStyle = FlatStyle.System;
         this.lblBitmapMargine.Location = new System.Drawing.Point(8, 160);
         this.lblBitmapMargine.Name = "lblBitmapMargine";
         this.lblBitmapMargine.Size = new System.Drawing.Size(88, 16);
         this.lblBitmapMargine.TabIndex = 6;
         this.lblBitmapMargine.Text = "Bitmap margine:";
         // 
         // cbFullDrag
         // 
         this.cbFullDrag.FlatStyle = FlatStyle.System;
         this.cbFullDrag.Location = new System.Drawing.Point(8, 120);
         this.cbFullDrag.Name = "cbFullDrag";
         this.cbFullDrag.Size = new System.Drawing.Size(72, 24);
         this.cbFullDrag.TabIndex = 5;
         this.cbFullDrag.Text = "Full Drag";
         // 
         // cbAllowDrag
         // 
         this.cbAllowDrag.FlatStyle = FlatStyle.System;
         this.cbAllowDrag.Location = new System.Drawing.Point(8, 96);
         this.cbAllowDrag.Name = "cbAllowDrag";
         this.cbAllowDrag.Size = new System.Drawing.Size(72, 24);
         this.cbAllowDrag.TabIndex = 4;
         this.cbAllowDrag.Text = "Allow Drag";
         // 
         // cbFlat
         // 
         this.cbFlat.FlatStyle = FlatStyle.System;
         this.cbFlat.Location = new System.Drawing.Point(8, 72);
         this.cbFlat.Name = "cbFlat";
         this.cbFlat.Size = new System.Drawing.Size(72, 24);
         this.cbFlat.TabIndex = 3;
         this.cbFlat.Text = "Flat";
         // 
         // cbHotTrack
         // 
         this.cbHotTrack.FlatStyle = FlatStyle.System;
         this.cbHotTrack.Location = new System.Drawing.Point(8, 48);
         this.cbHotTrack.Name = "cbHotTrack";
         this.cbHotTrack.Size = new System.Drawing.Size(72, 24);
         this.cbHotTrack.TabIndex = 2;
         this.cbHotTrack.Text = "Hot Track";
         // 
         // cbClickable
         // 
         this.cbClickable.FlatStyle = FlatStyle.System;
         this.cbClickable.Location = new System.Drawing.Point(8, 24);
         this.cbClickable.Name = "cbClickable";
         this.cbClickable.Size = new System.Drawing.Size(72, 24);
         this.cbClickable.TabIndex = 1;
         this.cbClickable.Text = "Clickable";
         // 
         // gbNotifications
         // 
#if !PocketPC
         this.gbNotifications.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
#endif
         this.gbNotifications.Controls.Add(this.btnClearNotifications);
         this.gbNotifications.Controls.Add(this.textEvents);
#if !PocketPC
         this.gbNotifications.FlatStyle = System.Windows.Forms.FlatStyle.System;
#endif
         this.gbNotifications.Location = new System.Drawing.Point(8, 352);
         this.gbNotifications.Name = "gbNotifications";
         this.gbNotifications.Size = new System.Drawing.Size(540, 88);
         this.gbNotifications.TabIndex = 3;
         this.gbNotifications.TabStop = false;
         this.gbNotifications.Text = "&Notifications";
         // 
         // btnClearNotifications
         // 
         this.btnClearNotifications.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.btnClearNotifications.BackColor = System.Drawing.SystemColors.Control;
         this.btnClearNotifications.FlatStyle = FlatStyle.System;
         this.btnClearNotifications.Location = new System.Drawing.Point(460, 16);
         this.btnClearNotifications.Name = "btnClearNotifications";
         this.btnClearNotifications.Size = new System.Drawing.Size(75, 23);
         this.btnClearNotifications.TabIndex = 2;
         this.btnClearNotifications.Text = "&Clear";
         this.btnClearNotifications.UseVisualStyleBackColor = false;
         this.btnClearNotifications.Click += new System.EventHandler(this.btnClearNotifications_Click);
         // 
         // textEvents
         // 
         this.textEvents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                     | System.Windows.Forms.AnchorStyles.Left)
                     | System.Windows.Forms.AnchorStyles.Right)));
         this.textEvents.Location = new System.Drawing.Point(8, 16);
         this.textEvents.Multiline = true;
         this.textEvents.Name = "textEvents";
         this.textEvents.ReadOnly = true;
         this.textEvents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
         this.textEvents.Size = new System.Drawing.Size(444, 64);
         this.textEvents.TabIndex = 1;
         // 
         // ilCards
         // 
#if !PocketPC
         this.ilCards.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilCards.ImageStream")));
         this.ilCards.TransparentColor = System.Drawing.Color.Transparent;
         this.ilCards.Images.SetKeyName(0, "");
         this.ilCards.Images.SetKeyName(1, "");
         this.ilCards.Images.SetKeyName(2, "");
         this.ilCards.Images.SetKeyName(3, "");
#endif
         // 
         // HeaderTest
         // 
#if !PocketPC
         this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
#endif
         this.ClientSize = new System.Drawing.Size(556, 453);
#if !PocketPC
         this.Controls.Add(this.gbNotifications);
         this.Controls.Add(this.gpHeaderProperties);
         this.Controls.Add(this.gbHeaderItemProperties);
         this.MinimumSize = new System.Drawing.Size(564, 480);
#else
         //////////////////////////////////////////////////////////////////////
         // Size and location adjustments
         //1st tab
         this.comboRightToLeft.Location = new System.Drawing.Point(88, 128);
         this.comboRightToLeft.Size = new System.Drawing.Size(96, 21);
         this.label6.Location = new System.Drawing.Point(8, 128);
         this.label6.Size = new System.Drawing.Size(80, 23);
         this.comboSortMark.Location = new System.Drawing.Point(88, 200);
         this.comboSortMark.Size = new System.Drawing.Size(96, 21);
         this.label5.Location = new System.Drawing.Point(8, 200);
         this.label5.Size = new System.Drawing.Size(64, 16);
         this.comboImageAlign.Location = new System.Drawing.Point(88, 176);
         this.comboImageAlign.Size = new System.Drawing.Size(96, 21);
         this.label4.Location = new System.Drawing.Point(8, 176);
         this.label4.Size = new System.Drawing.Size(72, 16);
         this.comboContentAlign.Size = new System.Drawing.Size(96, 21);
         this.comboContentAlign.Location = new System.Drawing.Point(88, 152);
         this.label3.Location = new System.Drawing.Point(8, 152);
         this.label3.Size = new System.Drawing.Size(84, 16);
         this.label2.Location = new System.Drawing.Point(8, 104);
         this.label2.Size = new System.Drawing.Size(40, 16);
         this.comboBitmap.Location = new System.Drawing.Point(88, 104);
         this.comboBitmap.Size = new System.Drawing.Size(10, 21);
         this.lblImageIndex.Location = new System.Drawing.Point(8, 80);
         this.lblImageIndex.Size = new System.Drawing.Size(76, 16);
         this.comboImageIndex.Location = new System.Drawing.Point(88, 80);
         this.comboImageIndex.Size = new System.Drawing.Size(72, 21);
         this.textWidth.Location = new System.Drawing.Point(88, 56);
         this.textWidth.Size = new System.Drawing.Size(72, 20);
         this.lblWidth.Location = new System.Drawing.Point(8, 56);
         this.lblWidth.Size = new System.Drawing.Size(40, 23);
         this.lblText.Location = new System.Drawing.Point(8, 32);
         this.lblText.Size = new System.Drawing.Size(40, 16);
         this.textText.Location = new System.Drawing.Point(88, 32);
         this.textText.Size = new System.Drawing.Size(10, 20);
         this.label1.Location = new System.Drawing.Point(8, 8);
         this.label1.Size = new System.Drawing.Size(48, 16);
         this.comboCurrentItem.Location = new System.Drawing.Point(88, 8);
         this.comboCurrentItem.Size = new System.Drawing.Size(10, 21);

         this.btnRefreshItem.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnUpdateItem.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnAddItem.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnInserItem.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnDeleteItem.Anchor = AnchorStyles.Top | AnchorStyles.Left;

         this.btnRefreshItem.Location = new System.Drawing.Point(8, 224);
         this.btnRefreshItem.Size =  new System.Drawing.Size(40, 20);
         this.btnUpdateItem.Location =  new System.Drawing.Point(53, 224);
         this.btnUpdateItem.Size =   new System.Drawing.Size(40, 20);
         this.btnAddItem.Location =     new System.Drawing.Point(98, 224);
         this.btnAddItem.Size =      new System.Drawing.Size(40, 20);
         this.btnInserItem.Location =   new System.Drawing.Point(143, 224);
         this.btnInserItem.Size =    new System.Drawing.Size(40, 20);
         this.btnDeleteItem.Location =  new System.Drawing.Point(188, 224);
         this.btnDeleteItem.Size =   new System.Drawing.Size(40, 20);

         //2nd tab
         this.btnRefreshCtrl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnUpdate.Location = new System.Drawing.Point(80, 220);
         this.btnUpdate.Size = new System.Drawing.Size(72, 24);
         this.btnRefreshCtrl.Location = new System.Drawing.Point(2, 220);
         this.btnRefreshCtrl.Size = new System.Drawing.Size(72, 24);
         this.comboImageList.Location = new System.Drawing.Point(100, 164);
         this.comboImageList.Size = new System.Drawing.Size(80, 21);
         this.label7.Location = new System.Drawing.Point(8, 164);
         this.label7.Size = new System.Drawing.Size(86, 16);
         this.textBmpMargin.Location = new System.Drawing.Point(100, 140);
         this.textBmpMargin.Size = new System.Drawing.Size(56, 20);
         this.lblBitmapMargine.Location = new System.Drawing.Point(8, 140);
         this.lblBitmapMargine.Size = new System.Drawing.Size(108, 16);
         this.cbFullDrag.Location = new System.Drawing.Point(8, 104);
         this.cbFullDrag.Size = new System.Drawing.Size(92, 24);
         this.cbAllowDrag.Location = new System.Drawing.Point(8, 80);
         this.cbAllowDrag.Size = new System.Drawing.Size(92, 24);
         this.cbFlat.Location = new System.Drawing.Point(8, 56);
         this.cbFlat.Size = new System.Drawing.Size(92, 24);
         this.cbHotTrack.Location = new System.Drawing.Point(8, 32);
         this.cbHotTrack.Size = new System.Drawing.Size(92, 24);
         this.cbClickable.Location = new System.Drawing.Point(8, 8);
         this.cbClickable.Size = new System.Drawing.Size(92, 24);

         //1st tab
         this.btnClearNotifications.Anchor = AnchorStyles.Top | AnchorStyles.Left;
         this.btnClearNotifications.Location = new System.Drawing.Point(88, 220);
         this.btnClearNotifications.Size = new System.Drawing.Size(72, 24);
         this.textEvents.Location = new System.Drawing.Point(8, 16);
         this.textEvents.Size = new System.Drawing.Size(80, 202);
         // End of size and location adjustments
         //////////////////////////////////////////////////////////////////////
         this.tab.Controls.Add(gbNotifications);
         this.tab.Controls.Add(gpHeaderProperties);
         this.tab.Controls.Add(gbHeaderItemProperties);
         this.Controls.Add(tab);
#endif
         this.Name = "HeaderTest";
         this.Text = "Header Control Demo";
         this.gbHeaderItemProperties.ResumeLayout(false);
#if !PocketPC
         this.gbHeaderItemProperties.PerformLayout();
#endif
         this.gpHeaderProperties.ResumeLayout(false);
#if !PocketPC
         this.gpHeaderProperties.PerformLayout();
#endif
         this.gbNotifications.ResumeLayout(false);
#if !PocketPC
         this.gbNotifications.PerformLayout();
#endif
         this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/*[STAThread]
		static void MainHeaderTest() 
		{
         Application.EnableVisualStyles();

         Application.Run(new MainFrm());
      }*/

		/// <summary>
		/// Header Notifications
		/// </summary>
		private void OnHeaderSectionClick(object sender, HeaderSectionEventArgs ea)
		{
			string sEventInfo = "HeaderSectionClick: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
				"Button: " + ea.Button.ToString() + "]" + "\r\n";

         this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnHeaderSectionDblClick(object sender, HeaderSectionEventArgs ea)
		{
			string sEventInfo = "HeaderSectionDblClick: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
            "Button: " + ea.Button.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnHeaderSectionDividerDblClick(object sender, HeaderSectionEventArgs ea)
		{
			string sEventInfo = "HeaderSectionDividerDblClick: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
            "Button: " + ea.Button.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnBeforeHeaderSectionTrack(object sender, 
			HeaderSectionWidthConformableEventArgs ea)
		{
			string sEventInfo = "BeforeItemTrack: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
				"Button: " + ea.Button.ToString() + "; " +
            "Width: " + ea.Width.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnHeaderSectionTracking(object sender, 
			HeaderSectionWidthConformableEventArgs ea)
		{
			string sEventInfo = "ItemTracking: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
				"Button: " + ea.Button.ToString() + "; " +
            "Width: " + ea.Width.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnAfterHeaderSectionTrack(object sender, HeaderSectionWidthEventArgs ea)
		{
			string sEventInfo = "AfterItemTrack: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
				"Button: " + ea.Button.ToString() + "; " +
            "Width: " + ea.Width.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnBeforeHeaderSectionDrag(object sender, HeaderSectionOrderConformableEventArgs ea)
		{
			string sEventInfo = "BeforeItemDrag: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
				"Button: " + ea.Button.ToString() + "; " +
            "NewOrder: " + ea.Order.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		private void OnAfterHeaderSectionDrag(object sender, HeaderSectionOrderConformableEventArgs ea)
		{
			string sEventInfo = "AfterItemDrag: [" +  
				"Order: " + this.header.Sections.IndexOf(ea.Item).ToString() + "; " +
				"Item: " + ea.Item.ToString() + "; " +
				"Button: " + ea.Button.ToString() + "; " +
            "NewOrder: " + ea.Order.ToString() + "]" + "\r\n";

			this.textEvents.AppendText(sEventInfo);
			this.textEvents.Select(this.textEvents.Text.Length, 0);
			this.textEvents.ScrollToCaret();
		}   

		/// <summary>
		/// Helpers
		/// </summary>
		private void RetrieveControlProperties()
		{
			this.cbClickable.Checked = this.header.Clickable;
			this.cbHotTrack.Checked = this.header.HotTrack;
			this.cbFlat.Checked = this.header.Flat;
			this.cbAllowDrag.Checked = this.header.AllowDragSections;
			this.cbFullDrag.Checked = this.header.FullDragSections;
			this.textBmpMargin.Text = this.header.BitmapMargin.ToString();

			if ( this.header.ImageList == this.ilCards )
			{
				this.comboImageList.Text = "Cards";
			}
			else
			{
				this.comboImageList.Text = "";
			}
		}

		private void UpdateControlProperties()
		{
			try
			{
				this.header.Clickable = this.cbClickable.Checked;
				this.header.HotTrack = this.cbHotTrack.Checked;   
				this.header.Flat = this.cbFlat.Checked;  
				this.header.AllowDragSections = this.cbAllowDrag.Checked;
				this.header.FullDragSections = this.cbFullDrag.Checked;    
				this.header.BitmapMargin = int.Parse(this.textBmpMargin.Text);

				switch ( this.comboImageList.Text )
				{
					case "Cards":
						this.header.ImageList = this.ilCards;
						break;

					default:
						this.header.ImageList = null;
						break;
				}        

				this.header.Update();
			}
			catch ( ArgumentNullException e )
			{
#if !PocketPC
            MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
         }
			catch ( FormatException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( OverflowException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( ArgumentOutOfRangeException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
         }
		}

		private void RetrieveItemProperties(int index)
		{
			HeaderSection item = this.header.Sections[index];

			this.textText.Text = item.Text;
			this.textWidth.Text = item.Width.ToString();
			this.comboImageIndex.Text = item.ImageIndex.ToString();
			//        this.comboBitmap.Text = item.ImageIndex.ToString();
			this.comboRightToLeft.Text = item.RightToLeft.ToString();
			this.comboContentAlign.Text = item.ContentAlignment.ToString();
			this.comboImageAlign.Text = item.ImageAlign.ToString();
			this.comboSortMark.Text = item.SortMark.ToString();
		}

		private HeaderSection GenerateItemFromProperties()
		{
			string sText = this.textText.Text;
        
			int cxWidth = int.Parse(this.textWidth.Text);
        
			int iImageIndex = this.comboImageIndex.Text != "" 
				? int.Parse(this.comboImageIndex.Text) 
				: -1;
        
			// comboBitmap

			RightToLeft enRightToLeft = (this.comboRightToLeft.Text != "")
				? (RightToLeft)Enum.Parse(typeof(RightToLeft), this.comboRightToLeft.Text)
				: RightToLeft.No;

			ContentAlignment enContentAlign = (this.comboContentAlign.Text != "")
            ? (ContentAlignment)Enum.Parse(typeof(ContentAlignment), 
				this.comboContentAlign.Text)
            : ContentAlignment.MiddleLeft;

			LeftRightAlignment enImageAlign = (this.comboImageAlign.Text != "")
				? (LeftRightAlignment)Enum.Parse(typeof(LeftRightAlignment), 
				this.comboImageAlign.Text)
				: LeftRightAlignment.Left;

			HeaderSectionSortMarks enSortMark = (this.comboSortMark.Text != "")
				? (HeaderSectionSortMarks)Enum.Parse(typeof(HeaderSectionSortMarks), 
				this.comboSortMark.Text)
				: HeaderSectionSortMarks.Non;
               

			return new HeaderSection(sText, cxWidth, iImageIndex, null, 
				enRightToLeft, enContentAlign, enImageAlign, 
				enSortMark, null);
		}

		/// <summary>
		/// Command handlers
		/// </summary>
		private void btnRefreshCtrl_Click(object sender, System.EventArgs e)
		{
			RetrieveControlProperties();
		}

		private void btnUpdate_Click(object sender, System.EventArgs ea)
		{
			UpdateControlProperties();
		}

		private void btnRefreshItem_Click(object sender, System.EventArgs ea)
		{
			try
			{
				int index = int.Parse(this.comboCurrentItem.Text);

				RetrieveItemProperties(index);
			}
			catch ( ArgumentNullException e )
			{
#if !PocketPC
            MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( ArgumentOutOfRangeException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( FormatException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( OverflowException e )
         {
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
         }
		}

		private void btnUpdateItem_Click(object sender, System.EventArgs ea)
		{
			try
			{
				int index = int.Parse(this.comboCurrentItem.Text);

				HeaderSection item = GenerateItemFromProperties();
				this.header.Sections[index] = item;
			}
			catch ( ArgumentNullException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( ArgumentOutOfRangeException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( FormatException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( OverflowException e )
         {
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
		}

		private void btnAddItem_Click(object sender, System.EventArgs ea)
		{
			try
			{
				HeaderSection item = GenerateItemFromProperties();

				this.header.Sections.Add(item);

				this.comboCurrentItem.Items.Add(this.header.Sections.Count - 1);
			}
			catch ( FormatException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid Width value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid Width value.\r\n" + e.Message);
#endif
         }
			catch ( OverflowException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid Width value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid Width value.\r\n" + e.Message);
#endif
			}
			catch ( ArgumentOutOfRangeException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid Width value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid Width value.\r\n" + e.Message);
#endif
         }
		}

		private void btnInserItem_Click(object sender, System.EventArgs ea)
		{
			try
			{
				int index = int.Parse(this.comboCurrentItem.Text);        

				HeaderSection item = GenerateItemFromProperties();

				this.header.Sections.Insert(index, item);

				this.comboCurrentItem.Items.Add(this.header.Sections.Count - 1);
			}
			catch ( ArgumentNullException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( FormatException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( OverflowException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
			catch ( ArgumentOutOfRangeException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid parameter value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid parameter value.\r\n" + e.Message);
#endif
			}
		}

		private void btnDeleteItem_Click(object sender, System.EventArgs ea)
		{
			try
			{
				int index = int.Parse(this.comboCurrentItem.Text);        

				this.header.Sections.RemoveAt(index);

				this.comboCurrentItem.Items.RemoveAt(this.comboCurrentItem.Items.Count - 1);
			}
			catch ( ArgumentNullException e )
         {
#if !PocketPC
				MessageBox.Show(this, "Invalid index value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid index value.\r\n" + e.Message);
#endif
         }
			catch ( FormatException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid index value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid index value.\r\n" + e.Message);
#endif
			}
			catch ( OverflowException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid index value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid index value.\r\n" + e.Message);
#endif
			}
			catch ( ArgumentOutOfRangeException e )
			{
#if !PocketPC
				MessageBox.Show(this, "Invalid index value.\r\n" + e.Message);
#else
            MessageBox.Show("Invalid index value.\r\n" + e.Message);
#endif
			}
		}

		private void btnClearNotifications_Click(object sender, System.EventArgs e)
		{
			this.textEvents.Clear();
		}

      private void gpHeaderProperties_Enter(object sender, EventArgs e)
      {

      }

      private void comboCurrentItem_SelectedIndexChanged(object sender, EventArgs e)
      {

      }

      private void gbHeaderItemProperties_Enter(object sender, EventArgs e)
      {

      }
	}
}
