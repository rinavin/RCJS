using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls;
using System.Drawing;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   /// <summary>
   /// strategy for changing the fore color
   /// </summary>
   class ForegroundColorStrategy : ISetPropertyData
   {
      Control control;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="control"></param>
      internal ForegroundColorStrategy(Control control)
      {
         this.control = control;
      }

      public object AdjustResettedValue(object value)
      {
         return value;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="oldValue"></param>
      /// <param name="newValue"></param>
      public void SetData(object oldValue, ref object newValue)
      {       
         if (control is MgRadioPanel)
            HandelBackColorForRadioControl(newValue);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newValue"></param>
      private void HandelBackColorForRadioControl(object newValue)
      {
         foreach (Control item in ((MgRadioPanel)control).Controls)
         {
            if (item is RadioButton)
               item.ForeColor = (Color)newValue;
         }     
      }
   
   }
}
