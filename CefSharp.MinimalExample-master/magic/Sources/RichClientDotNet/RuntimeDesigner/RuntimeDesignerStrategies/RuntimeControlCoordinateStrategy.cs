using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   class RuntimeControlCoordinateStrategy : ISetPropertyData
   {
      Control control;
      string propName;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="control"></param>
      internal RuntimeControlCoordinateStrategy(Control control, string propName)
      {
         this.control = control;
         this.propName = propName;
      }

      public object AdjustResettedValue(object value)
      {
         if (propName == "Top")
         {
            // If the control is on a Tab, its location may have to be shifted to compensate for
            // the original height of tab headers
            if (control.Parent is Panel && control.Parent.Parent is TabPage)
            {
               MgTabControl tabControl = (MgTabControl)control.Parent.Parent.Parent;
               int diff = tabControl.DisplayRectangle.Top - tabControl.InitialDisplayRectangle.Top;
               value = ((int)value) + diff;
            }
         }

         return value;
      }

      public void SetData(object oldValue, ref object newValue)
      {
         newValue = Math.Max((int)newValue, 0);

         //In case of 2D combobox, the height should be item height.
         MgFlexiHeightComboBox combo = control as MgFlexiHeightComboBox;
         if ((combo != null) && (propName == "Height") && (combo.MgComboBox.DrawMode == DrawMode.OwnerDrawFixed))
         {
            combo.MgComboBox.SetItemHeight((int)newValue);
            combo.Height = (int)newValue;
         }
      }
   }
}
