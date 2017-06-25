using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.support;
using com.magicsoftware.util;
using Controls.com.magicsoftware;
using Controls.com.magicsoftware.support;
using RuntimeDesigner.RuntimeDesignerStrategies;

namespace RuntimeDesigner
{
   /// <summary>
   /// factory to create the PropertyDescriptors used by the COmponentWrapper of the Runtime Designer
   /// </summary>
   class ComponentWrapperPropertyFactory
   {
      /// <summary>
      /// 
      /// </summary>
      /// <param name="originalPropDescriptors"></param>
      /// <param name="keyProp"></param>
      /// <param name="controlDesignerInfo"></param>
      /// <param name="component"></param>
      /// <returns></returns>
      static internal PropertyDescriptor CreatePropertyDescriptor(PropertyDescriptorCollection originalPropDescriptors, KeyValuePair<string, DesignerPropertyInfo> keyProp, ControlDesignerInfo controlDesignerInfo, object component, bool adminMode)
      {
         string name = null;

         PropertyDescriptor nativeProp = GetNativePropDescriptor(originalPropDescriptors, keyProp.Key, component, ref name);


         if (nativeProp != null && keyProp.Value.IsNativeProperty)
         {
            return CreateDescriptorForRealProperty(keyProp, controlDesignerInfo, component, nativeProp, name);
         }
         else
         {
            return CreateDescriptorForFakeProperty(keyProp, component, adminMode );
         }
      }

      /// <summary>
      /// get attribute
      /// </summary>
      /// <param name="keyProp"></param>
      /// <returns></returns>
      private static Attribute[] GetAttribute(KeyValuePair<string, DesignerPropertyInfo> keyProp)
      {
         Attribute[] attrs = null;
         if (!keyProp.Value.VisibleInPropertyGrid)
            attrs = new Attribute[] { BrowsableAttribute.No };

         return attrs;
      }

      /// <summary>
      /// create a special property descriptor
      /// </summary>
      /// <param name="keyProp"></param>
      /// <param name="component"></param>
      /// <returns></returns>
      private static PropertyDescriptor CreateDescriptorForFakeProperty(KeyValuePair<string, DesignerPropertyInfo> keyProp, object component, bool adminMode)
      {
         Attribute[] attrs = GetAttribute(keyProp);

         MgPropertyDescriptor propertyDescriptor = new MgPropertyDescriptor((Component)component, keyProp.Key, keyProp.Value.Value, attrs);

         // for the visible property, set the default to "true"
         if (keyProp.Key.Equals(com.magicsoftware.util.Constants.WinPropVisible))
         {
            propertyDescriptor.SetDefaultValue(true);
            if (keyProp.Value.RuntimeValue is bool)
            {
               if (!adminMode && !(bool)keyProp.Value.RuntimeValue)
                  propertyDescriptor.SetValue(component, keyProp.Value.RuntimeValue);
            }

         }

         // for the layer property, set the right value from the runtime value
         if (keyProp.Key.Equals(com.magicsoftware.util.Constants.WinPropLayer))
            propertyDescriptor.SetValue(component, keyProp.Value.RuntimeValue);

         return propertyDescriptor;
      }

    
      /// <summary>
      /// Create a PropertyDescriptor which wraps a real property of the control
      /// </summary>
      /// <param name="keyProp"></param>
      /// <param name="controlDesignerInfo"></param>
      /// <param name="component"></param>
      /// <param name="nativeProp"></param>
      /// <returns></returns>
      private static PropertyDescriptor CreateDescriptorForRealProperty(KeyValuePair<string, DesignerPropertyInfo> keyProp, ControlDesignerInfo controlDesignerInfo, object component, PropertyDescriptor nativeProp, string name)
      {      
         object valueItem = keyProp.Value.Value;
         
         ISetPropertyData setPropertyData = GetSetDataStrategy(ref keyProp, ref valueItem, 
                                                          controlDesignerInfo, component);

         Attribute[] attrs = GetAttribute(keyProp);

         RTDesignerPropertyDescriptor propertyDescriptor = new RTDesignerPropertyDescriptor(component, nativeProp, valueItem, name ?? nativeProp.DisplayName, attrs) { SetDataStrategy = setPropertyData };

         propertyDescriptor.SetTranslator(GetTranslator(component, keyProp.Key));

         propertyDescriptor.CanResetStrategy = GetCanRestStrategy(component, keyProp.Key);

         return propertyDescriptor;
      }

      /// <summary>
      /// returns the SetPropertyData strategy for this control and property
      /// </summary>
      /// <param name="keyProp"></param>
      /// <param name="valueItem"></param>
      /// <param name="controlDesignerInfo"></param>
      /// <param name="setPropertyData"></param>
      /// <param name="component"></param>
      private static ISetPropertyData GetSetDataStrategy(ref KeyValuePair<string, DesignerPropertyInfo> keyProp, ref object valueItem, ControlDesignerInfo controlDesignerInfo, object component)
      {
         ISetPropertyData setPropertyData = null;

         if (ComponentWrapper.IsCoordinateProperty(keyProp.Key))
         {
            valueItem = ((int)valueItem) + controlDesignerInfo.GetPlacementForProp(keyProp.Key);
            setPropertyData = new RuntimeControlCoordinateStrategy((Control)component, keyProp.Key);
         }
         else if (keyProp.Key.Equals(Constants.WinPropBackColor))
            setPropertyData = new BackgroundColorStrategy((Control)component);
         else if (keyProp.Key.Equals(Constants.WinPropForeColor))
            setPropertyData = new ForegroundColorStrategy((Control)component);
         else if (keyProp.Key.Equals(Constants.WinPropFont))
            setPropertyData = new FontStrategy((Control)component);

         return setPropertyData;
      }

      /// <summary>
      /// returns the Canreset strategy for this control and property
      /// </summary>
      /// <param name="component"></param>
      /// <param name="prop"></param>
      /// <returns></returns>
      static private ICanResetStrategy GetCanRestStrategy(object component, string prop)
      {
         if (component is MgLinkLabel && prop.Equals(Constants.WinPropFont))
            return new CanResetFontStrategy();
         
         if (component is MgListBox && prop.Equals(Constants.WinPropHeight))
            return new CanResetListBoxHeightStrategy((MgListBox)component);

         return null;
      }

      /// <summary>
      /// get the native (i.e. the component's real) property descriptor do be wrapped by our property descriptor
      /// </summary>
      /// <param name="component"></param>
      /// <param name="propertyName"></param>
      /// <returns></returns>
      static private PropertyDescriptor GetNativePropDescriptor(PropertyDescriptorCollection originalPropDescriptors, string propertyName, object component, ref string name)
      {
         if (component is IDisplayInfo && propertyName.Equals(Constants.WinPropText) && !originalPropDescriptors["TextToDisplay"].IsReadOnly)
            return originalPropDescriptors["TextToDisplay"];

         if (component is MgTabControl && propertyName.Equals(Constants.WinPropBackColor))
         {
            name = Constants.WinPropBackColor;
            return originalPropDescriptors["PageColor"];
         }

         return originalPropDescriptors[propertyName];
      }

      /// <summary>
      /// Get the right translator for the property
      /// </summary>
      /// <param name="component"></param>
      /// <param name="propertyName"></param>
      /// <returns></returns>
      private static ITranslator GetTranslator(object component, string propertyName)
      {
         switch (propertyName)
         {
            case Constants.WinPropLeft:
               return new LocationTranslator((Control)component, Axe.X);
            case Constants.WinPropTop:
               return new LocationTranslator((Control)component, Axe.Y);
         }

         return null;
      }

   }
}
