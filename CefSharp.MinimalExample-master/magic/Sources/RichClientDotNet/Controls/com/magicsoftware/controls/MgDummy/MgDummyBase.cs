using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Controls.com.magicsoftware.controls.MgDummy
{
   /// <summary>
   /// Defines dummy control .It uses the base sub form\browser ...
   /// </summary>
   public class MgDummyBase : UserControl
   {     
      protected Label DummyInfoLabel;

      //public static readonly System.Drawing.Color ControlBackGroundColor = System.Drawing.Color.FromArgb(232, 232, 232);
   

      #region IDesignerControl Members
            public MgDummyBase()
      {
         CreateLableControl();
         // add label control to Subform control
         this.Controls.Add(DummyInfoLabel);
      }

      protected virtual void CreateLableControl()
      {
         DummyInfoLabel = new Label();
         DummyInfoLabel.Text = string.Empty;
         DummyInfoLabel.Dock = DockStyle.Fill;
#if !PocketPC
         DummyInfoLabel.AutoSize = false;
         DummyInfoLabel.TextAlign = ContentAlignment.MiddleCenter;
         DummyInfoLabel.ForeColor = SystemColors.ControlText;
         DummyInfoLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         // Sets background color 
         BackColor = SystemColors.Control;
#endif
      }

      #endregion
          
   }
}

