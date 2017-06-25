using com.magicsoftware.controls;
namespace ContolsTest.TableTest
{
   partial class TableTestFrm
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
#if !PocketPC
          this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
#endif
          this.tableControl1 = new com.magicsoftware.controls.TableControlUnlimitedItems();
          this.SuspendLayout();
          // 
          // propertyGrid1
          // 
#if !PocketPC
          this.propertyGrid1.Location = new System.Drawing.Point(12, 54);
          this.propertyGrid1.Name = "propertyGrid1";
          this.propertyGrid1.SelectedObject = this.tableControl1;
          this.propertyGrid1.Size = new System.Drawing.Size(346, 589);
          this.propertyGrid1.TabIndex = 5;
#endif
          // 
          // tableControl1
          // 
          this.tableControl1.AllowColumnResize = true;
          this.tableControl1.AllowPaint = true;
          this.tableControl1.BackColor = System.Drawing.Color.White;
          this.tableControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
#if !PocketPC
          this.tableControl1.Cursor = System.Windows.Forms.Cursors.Default;
#endif
          this.tableControl1.Dock = System.Windows.Forms.DockStyle.Right;
          this.tableControl1.Location = new System.Drawing.Point(380, 0);
          this.tableControl1.Name = "tableControl1";
          this.tableControl1.RowHeight = 30;
          this.tableControl1.ShowColumnDividers = true;
          this.tableControl1.ShowLineDividers = true;
#if !PocketPC
          this.tableControl1.Size = new System.Drawing.Size(292, 643);
#else
          this.tableControl1.Size = new System.Drawing.Size(220, 270);
          this.tableControl1.Font = this.Font;
#endif
          this.tableControl1.TabIndex = 0;
          this.tableControl1.TitleHeight = 30;
          this.tableControl1.TopIndex = 0;
          this.tableControl1.VerticalScrollBar = true;
          this.tableControl1.VirtualItemsCount = 15;
#if !PocketPC
          this.tableControl1.Load += new System.EventHandler(this.tableControl1_Load);
          this.tableControl1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.tableControl1_MouseDoubleClick);
#endif
          // 
          // TableTestFrm
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
#if !PocketPC
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.ClientSize = new System.Drawing.Size(672, 643);
          this.Controls.Add(this.propertyGrid1);
#else
          this.ClientSize = new System.Drawing.Size(230, 280);
#endif
          this.Controls.Add(this.tableControl1);
          this.Name = "TableTestFrm";
          this.Text = "TableTestFrm";
          this.ResumeLayout(false);

      }

      #endregion

      private com.magicsoftware.controls.TableControl tableControl1;
#if !PocketPC
      private System.Windows.Forms.PropertyGrid propertyGrid1;
#endif
   }
}