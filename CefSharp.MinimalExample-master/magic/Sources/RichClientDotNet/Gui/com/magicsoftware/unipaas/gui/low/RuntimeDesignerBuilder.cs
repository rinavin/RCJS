using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.support;
using com.magicsoftware.unipaas.env;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.util;
using Controls.com.magicsoftware;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// manages the creation of the runtime designer
   /// </summary>
   class RuntimeDesignerBuilder
   {
      /// <summary>
      /// dictionary of objects and their info
      /// </summary>
      Dictionary<object, ControlDesignerInfo> controlDesignerInfoDictionary;
      public bool adminMode { get; set; }
      public String ControlsPersistencyPath { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="controlDesignerInfoDictionary"></param>
      public RuntimeDesignerBuilder(Dictionary<object, ControlDesignerInfo> controlDesignerInfoDictionary, bool adminMode, String controlsPersistencyPath)
      {
         this.controlDesignerInfoDictionary = controlDesignerInfoDictionary;
         this.adminMode = adminMode;
         this.ControlsPersistencyPath = controlsPersistencyPath;

         conversionDictionary = new Dictionary<string, ConvertPropertyValueDelegate>()
         {
            {Constants.WinPropBackColor, ConvertBackColorProp},
            {Constants.WinPropForeColor, ConvertForeColorProp},
            {Constants.WinPropIsTransparent, ConvertIsTransparentProp},
            {Constants.WinPropFont, ConvertFontProp},
         };

         ConvertPropertyValuesToGuiValues();
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="form"></param>
      public Form Build(Form form)
      {
         RuntimeDesigner.MainShell s = new RuntimeDesigner.MainShell(GetTranslateString, form.Icon, adminMode);
         s.AddDesigner(form, CreateAllOwnerDrawControls, GetControlDesignerInfo);
         return s;
      }

      /// <summary>
      ///  TranslateStringDelegate
      /// </summary>
      /// <param name="str"></param>
      /// <returns></returns>
      string GetTranslateString(String str)
      {
         return Events.Translate(str);
      }
      /// <summary>
      /// callback - create owner draw controls as regular controls
      /// </summary>
      /// <param name="control"></param>
      /// <returns></returns>
      public Dictionary<Control, bool> CreateAllOwnerDrawControls(Control control)
      {
         Dictionary<Control, bool> list = new Dictionary<Control, bool>();
         if (GuiUtils.IsContainerManager(control))
         {
            ContainerManager c = GuiUtils.getContainerManager(control);
            if (c is BasicControlsManager)
               return ((BasicControlsManager)c).CreateAllControlsForFormDesigner();
         }

         foreach (Control item in control.Controls)
         {
            // Fixed defect #:128197
            // while create child window it created form for child window
            // the runtime designer is for the current form only 
            bool itIsForm = item is Form;
            if (!itIsForm)
               list[item] = false;
         }
         return list;
      }

      /// <summary>
      /// callback - get the ControlDesignerInfo for the requested object
      /// </summary>
      /// <param name="component"></param>
      /// <returns></returns>
      ControlDesignerInfo GetControlDesignerInfo(object component)
      {
         object mgObject = ControlsMap.getInstance().GetObjectFromWidget(component);

         if (mgObject == null || !controlDesignerInfoDictionary.ContainsKey(mgObject)) //TMP - prevent subform exception yoel
            return null;

         if (mgObject != null)
         {
            controlDesignerInfoDictionary[mgObject].PreviousPlacementBounds = EditorSupportingPlacementLayout.GetPlacementDifRect(component);
         }
         return mgObject != null ? controlDesignerInfoDictionary[mgObject] : null;
      }


      delegate object ConvertPropertyValueDelegate(object mgObject, object value);

      Dictionary<string, ConvertPropertyValueDelegate> conversionDictionary;

      /// <summary>
      /// 
      /// </summary>
      private void ConvertPropertyValuesToGuiValues()
      {
         foreach (KeyValuePair<object, ControlDesignerInfo> pair in controlDesignerInfoDictionary)
         {
            if (pair.Value.Properties == null)
               continue;

            Object obj = ControlsMap.getInstance().object2Widget(pair.Key, 0);
            ConvertCoordinates(obj, pair.Value.Properties);
            foreach (var item in pair.Value.Properties)
            {
               if (conversionDictionary.ContainsKey(item.Key))
                  item.Value.Value = conversionDictionary[item.Key](obj, item.Value.Value);
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="Properties"></param>
      void ConvertCoordinates(object obj, Dictionary<string, DesignerPropertyInfo> Properties)
      {
         Form form = GuiUtils.getForm(obj);
         if (form != null)
         {
            //ConvertFormCoordinates(form, Properties);
            return;
         }

         string[] coordinatesProps;
         if (!(obj is Line))
         {
            coordinatesProps = new string[] {
               Constants.WinPropLeft,
               Constants.WinPropTop,
               Constants.WinPropWidth,
               Constants.WinPropHeight };
         }

         else // line control
         {
            coordinatesProps = new string[] {
               Constants.WinPropX1,
               Constants.WinPropY1,
               Constants.WinPropX2,
               Constants.WinPropY2 };
         }

         Rectangle rect = new Rectangle();

         if (!NavigationPropertiesExists(ref Properties, coordinatesProps))
            return;

         rect.X = (int)Properties[coordinatesProps[0]].Value;
         rect.Y = (int)Properties[coordinatesProps[1]].Value;
         rect.Width = (int)Properties[coordinatesProps[2]].Value;
         rect.Height = (int)Properties[coordinatesProps[3]].Value;

         Control container = null;
         if (obj is LogicalControl)
            container = ((LogicalControl)obj).GetContainerControl();
         else
            container = ((Control)obj).Parent;

         if (container is Panel)
         {
            container = container.Parent;
            if (container is TabPage)
               container = container.Parent;
         }

         if (container is MgTabControl)
         {
            rect.X -= ((MgTabControl)container).DisplayRectangle.X;
            rect.Y -= ((MgTabControl)container).DisplayRectangle.Y;
         }

         Properties[coordinatesProps[0]].Value = rect.X;
         Properties[coordinatesProps[1]].Value = rect.Y;
         Properties[coordinatesProps[2]].Value = rect.Width;
         Properties[coordinatesProps[3]].Value = rect.Height;
      }
      /// <summary>
      /// return true if all navigation properties exists in the properties 
      /// it will return false for controls that doesn't have those properties (as Container control)
      /// </summary>
      /// <param name="Properties"></param>
      /// <param name="coordinatesProps"></param>
      private bool NavigationPropertiesExists(ref Dictionary<string, DesignerPropertyInfo> Properties, string[] coordinatesProps)
      {
         bool navigationPropertiesExists = (Properties.ContainsKey(coordinatesProps[0]) &&
                Properties.ContainsKey(coordinatesProps[1]) &&
                Properties.ContainsKey(coordinatesProps[2]) &&
                Properties.ContainsKey(coordinatesProps[3]));

         return navigationPropertiesExists;
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="Properties"></param>
      void ConvertFormCoordinates(object obj, Dictionary<string, DesignerPropertyInfo> Properties)
      {
         Form form = GuiUtils.getForm(obj);

         Size sz = (Size)Properties["ClientSize"].Value;

         Size clientSize = GuiUtils.computeClientSize((Form)obj, sz);

         Properties["ClientSize"].Value = clientSize;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      object ConvertBackColorProp(object obj, object value)
      {
         Color backgroundColor = Color.Empty;
         MgColor mgColor = (MgColor)value;

         // if the color is transparent then set the background color to null
         if (mgColor != null)
         {
            //For logical text, we support alpha values.
            bool supportsAlpha = obj is LgText || GuiUtils.supportTransparency(obj);
            backgroundColor = ControlUtils.MgColor2Color(mgColor, GuiUtils.supportTransparency(obj), supportsAlpha);
         }
         return backgroundColor;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      object ConvertIsTransparentProp(object obj, object value)
      {
         return ((MgColor)value).IsTransparent;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      object ConvertForeColorProp(object obj, object value)
      {
         Color foregroundColor = value != null ? ControlUtils.MgColor2Color((MgColor)value, false, false) : Color.Empty;

         return foregroundColor;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      object ConvertFontProp(object obj, object value)
      {
         Font font = value != null ? FontsCache.GetInstance().Get((MgFont)value) : null;

         return font;
      }
   }
}
