using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.unipaas.management.gui;
using System.Drawing;
using com.magicsoftware.win32;
using com.magicsoftware.util;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// sets the runtime designer values on the controls
   /// </summary>
   class DesignerValuesSetters
   {
      delegate void SetValueDelegate(object obj, object value);

      static Dictionary<string, SetValueDelegate> settersDict = new Dictionary<string, SetValueDelegate>()
         {
            { Constants.WinPropLeft, SetLeft},
            { Constants.WinPropTop, SetTop},
            { Constants.WinPropWidth, SetWidth},
            { Constants.WinPropHeight, SetHeight},
            { Constants.WinPropBackColor, SetBackgroundColor},
            { Constants.WinPropForeColor, SetForegroundColor},
            { Constants.WinPropFont, SetFont},
            { Constants.WinPropText, SetText},
            { Constants.WinPropX1, SetLeft},
            { Constants.WinPropX2, SetWidth},
            { Constants.WinPropY1, SetTop},
            { Constants.WinPropY2, SetHeight},
            { Constants.WinPropGradientStyle, SetGradientStyle},
         };

      /// <summary>
      /// entry point
      /// </summary>
      /// <param name="dict"></param>
      public static void SetDesignerValues(Dictionary<MgControlBase, Dictionary<string, object>> dict)
      {
         foreach (var keyValue in dict) // key - control, value - properties dictionary
         {
            object obj = ControlsMap.getInstance().object2Widget(keyValue.Key);
            foreach (var propKeyValue in keyValue.Value) // key - property name, value - property value
            {
               if (settersDict.ContainsKey(propKeyValue.Key))
               {
                  //Defect 132495 - when height is set in the designer file, the style of the combo should be 2D. 
                  // This will not cause problems if the style is not 2D because after that, we set the real style (in case it neeeded)
                  if (obj is MgComboBox && propKeyValue.Key == "Height")
                     ((MgComboBox)obj).DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
                  settersDict[propKeyValue.Key](obj, propKeyValue.Value);
               }
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetLeft(object obj, object value)
      {
         GuiUtils.setBounds(obj, (int)value, GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetTop(object obj, object value)
      {
         GuiUtils.setBounds(obj, GuiConstants.DEFAULT_VALUE_INT, (int)value, GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetWidth(object obj, object value)
      {
         GuiUtils.setBounds(obj, GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, (int)value, GuiConstants.DEFAULT_VALUE_INT, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetHeight(object obj, object value)
      {
         GuiUtils.setBounds(obj, GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, GuiConstants.DEFAULT_VALUE_INT, (int)value, false);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetBackgroundColor(object obj, object value)
      {
         GuiUtils.setBackgroundColor(obj, (Color)value, null, value.Equals(Color.Transparent), value.Equals(Color.Transparent), 0);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetForegroundColor(object obj, object value)
      {
         GuiUtils.setForegroundColor(obj, (Color)value);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetGradientStyle(object obj, object value)
      {
         GuiUtils.setGradientStyle(obj, (GradientStyle)value);
      }
      

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetFont(object obj, object value)
      {
         Font font = (Font)value;
         NativeWindowCommon.LOGFONT logfont = new NativeWindowCommon.LOGFONT();
         font.ToLogFont(logfont);

         GuiUtils.setFont(obj, font, logfont.lfOrientation, 0);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      static void SetText(object obj, object value)
      {
         GuiUtils.setText(obj, (string)value);
      }
   }
}
