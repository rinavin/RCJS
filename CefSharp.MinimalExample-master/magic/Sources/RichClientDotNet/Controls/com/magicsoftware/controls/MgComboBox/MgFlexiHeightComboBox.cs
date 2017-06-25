using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls.designers;

namespace com.magicsoftware.controls
{
   [Designer(typeof(ListControlDesigner))]
   [ToolboxBitmap(typeof(ComboBox))]
   public partial class MgFlexiHeightComboBox : UserControl, IChoiceControl, ISetSpecificControlPropertiesForFormDesigner
   {
      public MgComboBox MgComboBox { get; private set; }


      public MgFlexiHeightComboBox()
      {
         //this.BorderStyle = BorderStyle.None;
         this.BackColor = Color.Transparent;
         
         MgComboBox = new MgComboBox();
         MgComboBox.Dock = DockStyle.Fill;
         MgComboBox.SelectedIndexChanged += MgComboBox_SelectedIndexChanged;
         this.Controls.Add(MgComboBox);
      }

      public override Color BackColor
      {
         get
         {
            return base.BackColor;
         }
         set
         {
            base.BackColor = value;
            if (MgComboBox != null)
               MgComboBox.BackColor = value;
         }
      }

      public override Color ForeColor
      {
         get
         {
            return base.ForeColor;
         }
         set
         {
            base.ForeColor = value;
            if (MgComboBox != null)
               MgComboBox.ForeColor = value;
         }
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void MgComboBox_SelectedIndexChanged(object sender, EventArgs e)
      {
         SelectedIndex = MgComboBox.SelectedIndex;
      }

      #region IChoiceControl Members

      public event EventHandler SelectedIndexChanged;

      private int selectedIndex;
      public int SelectedIndex
      {
         get
         {
            return MgComboBox.SelectedIndex;
         }
         set
         {
            if (value != selectedIndex)
            {
               selectedIndex = value;
               OnSelectdIndexChanged();
            }
         }
      }

      /// <summary>
      /// Raises SelectedIndexChanged event
      /// </summary>
      void OnSelectdIndexChanged()
      {
         if (SelectedIndexChanged != null)
            SelectedIndexChanged(this, EventArgs.Empty);
      }
      #endregion

      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         MgComboBox.setSpecificControlPropertiesForFormDesigner(fromControl);
      }
   }
}
