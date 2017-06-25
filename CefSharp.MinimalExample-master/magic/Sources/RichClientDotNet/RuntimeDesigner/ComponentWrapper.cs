using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using com.magicsoftware.support;
using com.magicsoftware.util;

namespace RuntimeDesigner
{
   /// <summary>
   /// wraps a component and exposes the wanted property descriptors
   /// </summary>
   public class ComponentWrapper : ICustomTypeDescriptor
   {
      /// <summary>
      /// wrapped component
      /// </summary>
      object component;

      /// <summary>
      /// collection of exposed property descriptors for the supplied component
      /// </summary>
      PropertyDescriptorCollection propertiesDescriptors = null;
      public PropertyDescriptorCollection PropertiesDescriptors { get { return propertiesDescriptors; } }
      
      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="component"></param>
      internal ComponentWrapper(object component, bool adminMode)
      {
         this.component = component;
         ProcessOriginalProperties(adminMode);
      }

      #region ICustomTypeDescriptor
      public AttributeCollection GetAttributes()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetAttributes();
      }

      public string GetClassName()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetClassName();
      }

      public string GetComponentName()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetComponentName();
      }

      public TypeConverter GetConverter()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetConverter();
      }

      public EventDescriptor GetDefaultEvent()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetDefaultEvent();
      }

      public PropertyDescriptor GetDefaultProperty()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetDefaultProperty();
      }

      public object GetEditor(Type editorBaseType)
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetEditor(editorBaseType);
      }

      public EventDescriptorCollection GetEvents(Attribute[] attributes)
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetEvents(attributes);
      }

      public EventDescriptorCollection GetEvents()
      {
         return TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetEvents();
      }

      public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
      {
         return GetProperties();
      }

      public PropertyDescriptorCollection GetProperties()
      {
         return propertiesDescriptors;
      }

      public object GetPropertyOwner(PropertyDescriptor pd)
      {
         return component;
      }

      #endregion ICustomTypeDescriptor

      /// <summary>
      /// init the properties descriptors collection
      /// </summary>
      void ProcessOriginalProperties(bool adminMode)
      {
         // list of property descriptors for this object's properties collection
         List<PropertyDescriptor> descriptorsToCreate = new List<PropertyDescriptor>();

         if (((Control)component).Tag != null)
         {
            // Get default property descriptors
            PropertyDescriptorCollection originalPropDescriptors = TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetProperties();

            Dictionary<string, DesignerPropertyInfo> properties = ((ControlDesignerInfo)((Control)component).Tag).Properties;
            ControlDesignerInfo controlDesignerInfo = ((ControlDesignerInfo)((Control)component).Tag);

            foreach (var keyProp in properties)
               descriptorsToCreate.Add(ComponentWrapperPropertyFactory.CreatePropertyDescriptor(originalPropDescriptors, keyProp, controlDesignerInfo, component, adminMode));
    
            // create the new PropertyDescriptorCollection
            propertiesDescriptors = new PropertyDescriptorCollection(descriptorsToCreate.ToArray());
         }
      }

      /// <summary>
      /// return true if property is coordinate
      /// </summary>
      /// <param name="prop"></param>
      /// <returns></returns>
      internal static bool IsCoordinateProperty(string prop)
      {
         List<string> coordiantesProperties = new List<string> { Constants.WinPropLeft, Constants.WinPropTop, Constants.WinPropWidth, 
            Constants.WinPropHeight, Constants.WinPropX1, Constants.WinPropX2, Constants.WinPropY1, Constants.WinPropY2, "ClientSize"};
         return coordiantesProperties.Contains(prop);
      }

      /// <summary>
      /// 
      /// </summary>
      internal void ResetAllProperties(bool adminMode)
      {
         foreach (PropertyDescriptor item in propertiesDescriptors)
         {
            // don't reset the "visible" property 
            if(adminMode || !item.Name.Equals(Constants.WinPropVisible))
               item.ResetValue(component);
         }
      }

      /// <summary>
      /// get the corrected offset for calculating the tab order
      /// </summary>
      /// <param name="prop"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public int GetTabOrderOffset(string prop, int value)
      {
         Control parent = ((Control)component).Parent;
         // if the control is on a tabcontrol, shift by the display rectangle size
         if (parent is Panel && parent.Parent is TabPage)
         {
            switch (prop)
            {
               case Constants.WinPropTop:
                  value += ((TabControl)parent.Parent.Parent).DisplayRectangle.Y;
                  break;
               case Constants.WinPropLeft:
                  value += ((TabControl)parent.Parent.Parent).DisplayRectangle.X;
                  break;
            }
         }

         return value;
      }

   }
}
