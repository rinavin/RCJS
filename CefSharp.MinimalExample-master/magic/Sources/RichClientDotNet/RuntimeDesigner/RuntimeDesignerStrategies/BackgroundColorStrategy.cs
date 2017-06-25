using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.controls;
using System.Drawing;

namespace RuntimeDesigner.RuntimeDesignerStrategies
{
   /// <summary>
   /// strategy for changing the background color
   /// </summary>
   class BackgroundColorStrategy : ISetPropertyData
   {
      Control control;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="control"></param>
      internal BackgroundColorStrategy(Control control)
      {
         this.control = control;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="oldValue"></param>
      /// <param name="newValue"></param>
      public void SetData(object oldValue, ref object newValue)
      {
         if (control is MgTabControl)
            HandelBackColorForTabControl(newValue);
         if (control is MgRadioPanel)
            HandelBackColorForRadioControl(newValue);

         ResetGradientStyle(control);
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
               item.BackColor = (Color)newValue;
         }    
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="newValue"></param>
      private void HandelBackColorForTabControl(object newValue)
      {
         // for tab control, find the panel and set its background color to the same value as the tab control
         MgPanel panel = GetMgPanelOfTabControl(control);
         if (panel != null)
            panel.BackColor = (Color)newValue;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal static MgPanel GetMgPanelOfTabControl(object control)
      {
         MgPanel panel = null;
         if (control is MgTabControl)
         {
            Control mgTabcontrol = control as Control;
            // for tab control, find the panel and set its background color to the same value as the tab control            
            foreach (Control item in mgTabcontrol.Controls)
            {
               if (item.Controls.Count > 0)
               {
                  panel = (MgPanel)item.Controls[0];
                  break;
               }
            }           
         }

            return panel;
      }

      private void ResetGradientStyle(Control control)
      {
         // Fixed defect #127250 : For all controls that support Gradient, while color is set in runtime designer 
         //                        it's mean the Gradient color will be reset to NONE
         // Note in Reset command: * The solid color will be seen on the control.
         //                        * The Gradient color will be show while re execute the form again.
         //                        * It is because we don't have Gradient color property on property grid, 
         //                        * PM deiced to go with that way because if it will be impotent we will add the property 
         //                          to property grid but for now we will not do it 
         if (control is IGradientColorProperty)
         {
            IGradientColorProperty gradientColorProperty = control as IGradientColorProperty;
            gradientColorProperty.GradientStyle = com.magicsoftware.util.GradientStyle.None;

            MgPanel panel = GetMgPanelOfTabControl(control);
            if (panel != null)
               ResetGradientStyle(panel);
         }
      }

      public object AdjustResettedValue(object value)
      {
         return value;
      }
   }
}
