using System.Drawing;
namespace RuntimeDesigner
{
    partial class MainShell
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
         this.components = new System.ComponentModel.Container();
         this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
         this.fileMenuItem = new System.Windows.Forms.MenuItem();
         this.saveMenuItem = new System.Windows.Forms.MenuItem();
         this.separator1 = new System.Windows.Forms.MenuItem();
         this.resetToDefault = new System.Windows.Forms.MenuItem();
         this.separator2 = new System.Windows.Forms.MenuItem();
         this.exitMenuItem = new System.Windows.Forms.MenuItem();
         this.alignoLoaderMenuItem = new System.Windows.Forms.MenuItem();
         this.leMenuItem = new System.Windows.Forms.MenuItem();
         this.ceMenuItem = new System.Windows.Forms.MenuItem();
         this.rightsMenuItem = new System.Windows.Forms.MenuItem();
         this.tMenuItem = new System.Windows.Forms.MenuItem();
         this.miMenuItem = new System.Windows.Forms.MenuItem();
         this.bMenuItem = new System.Windows.Forms.MenuItem();
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.splitContainer2 = new System.Windows.Forms.SplitContainer();
         this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
         this.label1 = new System.Windows.Forms.Label();
         this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
         this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
         this.label2 = new System.Windows.Forms.Label();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.splitContainer2.Panel1.SuspendLayout();
         this.splitContainer2.Panel2.SuspendLayout();
         this.splitContainer2.SuspendLayout();
         this.tableLayoutPanel1.SuspendLayout();
         this.tableLayoutPanel2.SuspendLayout();
         this.SuspendLayout();
         // 
         // mainMenu1
         // 
         this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.fileMenuItem,
            this.alignoLoaderMenuItem});
         // 
         // fileMenuItem
         // 
         this.fileMenuItem.Index = 0;
         this.fileMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.saveMenuItem,
            this.separator1,
            this.resetToDefault,
            this.separator2,
            this.exitMenuItem});
         this.fileMenuItem.Text = "&File";
         // 
         // saveMenuItem
         // 
         this.saveMenuItem.Index = 0;
         this.saveMenuItem.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
         this.saveMenuItem.Text = "&Save";
         this.saveMenuItem.Click += new System.EventHandler(this.saveMenuItem_Click);
         // 
         // separator1
         // 
         this.separator1.Index = 1;
         this.separator1.Text = "-";
         // 
         // resetToDefault
         // 
         this.resetToDefault.Index = 2;
         this.resetToDefault.Text = "Reset to default";
         this.resetToDefault.Click += new System.EventHandler(this.resetToDefault_Click);
         // 
         // separator2
         // 
         this.separator2.Index = 3;
         this.separator2.Text = "-";
         // 
         // exitMenuItem
         // 
         this.exitMenuItem.Index = 4;
         this.exitMenuItem.Shortcut = System.Windows.Forms.Shortcut.AltF4;
         this.exitMenuItem.Text = "E&xit";
         this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
         // 
         // alignoLoaderMenuItem
         // 
         this.alignoLoaderMenuItem.Index = 1;
         this.alignoLoaderMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.leMenuItem,
            this.ceMenuItem,
            this.rightsMenuItem,
            this.tMenuItem,
            this.miMenuItem,
            this.bMenuItem});
         this.alignoLoaderMenuItem.Text = "&Align";
         // 
         // leMenuItem
         // 
         this.leMenuItem.Index = 0;
         this.leMenuItem.Text = "&Lefts";
         this.leMenuItem.Click += new System.EventHandler(this.ActionClick);
         // 
         // ceMenuItem
         // 
         this.ceMenuItem.Index = 1;
         this.ceMenuItem.Text = "&Centers";
         this.ceMenuItem.Click += new System.EventHandler(this.ActionClick);
         // 
         // rightsMenuItem
         // 
         this.rightsMenuItem.Index = 2;
         this.rightsMenuItem.Text = "&Rights";
         this.rightsMenuItem.Click += new System.EventHandler(this.ActionClick);
         // 
         // tMenuItem
         // 
         this.tMenuItem.Index = 3;
         this.tMenuItem.Text = "&Tops";
         this.tMenuItem.Click += new System.EventHandler(this.ActionClick);
         // 
         // miMenuItem
         // 
         this.miMenuItem.Index = 4;
         this.miMenuItem.Text = "&Middles";
         this.miMenuItem.Click += new System.EventHandler(this.ActionClick);
         // 
         // bMenuItem
         // 
         this.bMenuItem.Index = 5;
         this.bMenuItem.Text = "&Bottoms";
         this.bMenuItem.Click += new System.EventHandler(this.ActionClick);
         // 
         // splitContainer1
         // 
         this.splitContainer1.BackColor = System.Drawing.SystemColors.InactiveCaption;
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.Location = new System.Drawing.Point(0, 0);
         this.splitContainer1.Name = "splitContainer1";
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
         this.splitContainer1.Size = new System.Drawing.Size(760, 413);
         this.splitContainer1.SplitterDistance = 546;
         this.splitContainer1.TabIndex = 2;
         this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
         this.splitContainer1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.splitContainer1_MouseUp);
         // 
         // splitContainer2
         // 
         this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer2.Location = new System.Drawing.Point(0, 0);
         this.splitContainer2.Margin = new System.Windows.Forms.Padding(0);
         this.splitContainer2.Name = "splitContainer2";
         this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer2.Panel1
         // 
         this.splitContainer2.Panel1.Controls.Add(this.tableLayoutPanel1);
         // 
         // splitContainer2.Panel2
         // 
         this.splitContainer2.Panel2.Controls.Add(this.tableLayoutPanel2);
         this.splitContainer2.Size = new System.Drawing.Size(210, 413);
         this.splitContainer2.SplitterDistance = 271;
         this.splitContainer2.TabIndex = 0;
         this.splitContainer2.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer2_SplitterMoved);
         this.splitContainer2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.splitContainer2_MouseUp);
         // 
         // tableLayoutPanel1
         // 
         this.tableLayoutPanel1.ColumnCount = 1;
         this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
         this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
         this.tableLayoutPanel1.Controls.Add(this.propertyGrid1, 0, 1);
         this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
         this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
         this.tableLayoutPanel1.Name = "tableLayoutPanel1";
         this.tableLayoutPanel1.RowCount = 2;
         this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
         this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
         this.tableLayoutPanel1.Size = new System.Drawing.Size(210, 271);
         this.tableLayoutPanel1.TabIndex = 0;
         // 
         // label1
         // 
         this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.label1.Location = new System.Drawing.Point(0, 0);
         this.label1.Margin = new System.Windows.Forms.Padding(0);
         this.label1.Name = "label1";
         this.label1.Padding = new System.Windows.Forms.Padding(2);
         this.label1.Size = new System.Drawing.Size(210, 20);
         this.label1.TabIndex = 1;
         this.label1.Text = "Properties";
         // 
         // propertyGrid1
         // 
         this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.propertyGrid1.Location = new System.Drawing.Point(0, 20);
         this.propertyGrid1.Margin = new System.Windows.Forms.Padding(0);
         this.propertyGrid1.Name = "propertyGrid1";
         this.propertyGrid1.Size = new System.Drawing.Size(210, 251);
         this.propertyGrid1.TabIndex = 0;
         // 
         // tableLayoutPanel2
         // 
         this.tableLayoutPanel2.ColumnCount = 1;
         this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
         this.tableLayoutPanel2.Controls.Add(this.label2, 0, 0);
         this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
         this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
         this.tableLayoutPanel2.Name = "tableLayoutPanel2";
         this.tableLayoutPanel2.RowCount = 2;
         this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
         this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
         this.tableLayoutPanel2.Size = new System.Drawing.Size(210, 138);
         this.tableLayoutPanel2.TabIndex = 0;
         // 
         // label2
         // 
         this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.label2.Location = new System.Drawing.Point(0, 0);
         this.label2.Margin = new System.Windows.Forms.Padding(0);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(210, 20);
         this.label2.TabIndex = 0;
         this.label2.Text = "Hidden Controls";
         // 
         // MainShell
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(760, 413);
         this.Controls.Add(this.splitContainer1);
         this.Menu = this.mainMenu1;
         this.Name = "MainShell";
         this.Text = "Form Designer";
         this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
         this.splitContainer1.Panel2.ResumeLayout(false);
         this.splitContainer1.ResumeLayout(false);
         this.splitContainer2.Panel1.ResumeLayout(false);
         this.splitContainer2.Panel2.ResumeLayout(false);
         this.splitContainer2.ResumeLayout(false);
         this.tableLayoutPanel1.ResumeLayout(false);
         this.tableLayoutPanel2.ResumeLayout(false);
         this.ResumeLayout(false);

        }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        void MainShell_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
           // Exit behavior for form designer : possible to close the runtime designer by Escape
           if (e.KeyCode == System.Windows.Forms.Keys.Escape)              
              this.Close();
        }

        #endregion

        private System.Windows.Forms.MainMenu mainMenu1;
        private System.Windows.Forms.MenuItem fileMenuItem;
        private System.Windows.Forms.MenuItem saveMenuItem;
        private System.Windows.Forms.MenuItem exitMenuItem;
        private System.Windows.Forms.MenuItem alignoLoaderMenuItem;
        private System.Windows.Forms.MenuItem leMenuItem;
        private System.Windows.Forms.MenuItem ceMenuItem;
        private System.Windows.Forms.MenuItem rightsMenuItem;
        private System.Windows.Forms.MenuItem tMenuItem;
        private System.Windows.Forms.MenuItem miMenuItem;
        private System.Windows.Forms.MenuItem bMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.MenuItem separator1;
        private System.Windows.Forms.MenuItem resetToDefault;
        private System.Windows.Forms.MenuItem separator2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label2;
    }
}
