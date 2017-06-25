namespace ControlsTest.ShapeControlTest
{
   partial class ShapeTestForm
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
         mgShape1.PropertyChanged -= mgShape1_PropertyChanged;
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
         this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
         this.mgShape1 = new Controls.com.magicsoftware.controls.MgShape.MgShape();
         this.SuspendLayout();
         // 
         // propertyGrid1
         // 
         this.propertyGrid1.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
         this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Left;
         this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
         this.propertyGrid1.Name = "propertyGrid1";
         this.propertyGrid1.Size = new System.Drawing.Size(256, 367);
         this.propertyGrid1.TabIndex = 1;
         // 
         // mgShape1
         // 
         this.mgShape1.BackColor = System.Drawing.Color.Transparent;
         this.mgShape1.ControlStyle = com.magicsoftware.util.ControlStyle.TwoD;
         this.mgShape1.FillColor = System.Drawing.Color.Transparent;
         this.mgShape1.FontOrientation = 0;
         this.mgShape1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
         this.mgShape1.LineType = com.magicsoftware.util.CtrlLineType.Normal;
         this.mgShape1.LineWidth = 1;
         this.mgShape1.Location = new System.Drawing.Point(291, 36);
         this.mgShape1.Margin = new System.Windows.Forms.Padding(0);
         this.mgShape1.Multiline = false;
         this.mgShape1.Name = "mgShape1";
         this.mgShape1.Shapes = com.magicsoftware.util.ShapeTypes.Ellipse;
         this.mgShape1.Size = new System.Drawing.Size(275, 173);
         this.mgShape1.TabIndex = 0;
         this.mgShape1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
         this.mgShape1.PropertyChanged += mgShape1_PropertyChanged;

         // 
         // ShapeTestForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.BackColor = System.Drawing.SystemColors.Window;
         this.ClientSize = new System.Drawing.Size(575, 367);
         this.Controls.Add(this.propertyGrid1);
         this.Controls.Add(this.mgShape1);
         this.Name = "ShapeTestForm";
         this.Text = "dfgdf";
         this.ResumeLayout(false);

      }

      void mgShape1_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         mgShape1.Invalidate();
      }

      #endregion

      private Controls.com.magicsoftware.controls.MgShape.MgShape mgShape1;
      private System.Windows.Forms.PropertyGrid propertyGrid1;

   }
}