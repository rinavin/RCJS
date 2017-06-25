using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace RuntimeDesigner
{
   /// <summary>
   /// wrapper around .Net controls, for the runtime designer
   /// </summary>
   class DotNetComponentWrapper : Control, ICustomTypeDescriptor
   {
      internal Control WrappedControl { get { return Controls.Count > 0 ? Controls[0] : null; } }

      PropertyDescriptorCollection propertyDescriptorCollection = null;

      #region ICustomTypeDescriptor
      public AttributeCollection GetAttributes()
      {
         return TypeDescriptor.GetAttributes(typeof(Control));
      }

      public string GetClassName()
      {
         return TypeDescriptor.GetClassName(WrappedControl);
      }

      public string GetComponentName()
      {
         return TypeDescriptor.GetComponentName(WrappedControl);
      }

      public TypeConverter GetConverter()
      {
         return TypeDescriptor.GetConverter(WrappedControl);
      }

      public EventDescriptor GetDefaultEvent()
      {
         return TypeDescriptor.GetDefaultEvent(WrappedControl);
      }

      public PropertyDescriptor GetDefaultProperty()
      {
         return TypeDescriptor.GetDefaultProperty(WrappedControl);
      }

      public object GetEditor(Type editorBaseType)
      {
         return TypeDescriptor.GetEditor(WrappedControl, editorBaseType);
      }

      public EventDescriptorCollection GetEvents(Attribute[] attributes)
      {
         return TypeDescriptor.GetEvents(WrappedControl);
      }

      public EventDescriptorCollection GetEvents()
      {
         return TypeDescriptor.GetEvents(WrappedControl);
      }

      public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
      {
         return GetProperties();
      }

      public PropertyDescriptorCollection GetProperties()
      {
         if (propertyDescriptorCollection == null)
            InitPropertyDescriptorCollection();
         return propertyDescriptorCollection;
      }

      public object GetPropertyOwner(PropertyDescriptor pd)
      {
         if(Controls.Count == 0 || wrapperPropertiesToUse.Contains(pd.Name))
            return this;
         return WrappedControl;
      }
      #endregion

      // properties which should be set on the wrapper, and not on the .Net control
      List<string> wrapperPropertiesToUse = new List<string>() { "Left", "Top", "Width", "Height", "Visible", "Bounds", "Dock" };

      /// <summary>
      /// Initialize the PropertyDescriptors collection for this object
      /// </summary>
      void InitPropertyDescriptorCollection()
      {
         ArrayList propDescriptors = new ArrayList();

         // get the props of the .Net control
         PropertyDescriptorCollection innerPdc = TypeDescriptor.GetProperties(WrappedControl);
         foreach (PropertyDescriptor item in innerPdc)
         {
            if (!wrapperPropertiesToUse.Contains(item.Name))
               propDescriptors.Add(item);
         }

         // get the props of the wrapper
         PropertyDescriptorCollection thisPdc = TypeDescriptor.GetProperties(typeof(Control));
         foreach (string item in wrapperPropertiesToUse)
         {
            propDescriptors.Add(thisPdc[item]);
         }

         propertyDescriptorCollection = new PropertyDescriptorCollection((PropertyDescriptor[])propDescriptors.ToArray(typeof(PropertyDescriptor)));
      }
   }
}
