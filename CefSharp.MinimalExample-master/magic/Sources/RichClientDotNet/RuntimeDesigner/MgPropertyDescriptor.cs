using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace RuntimeDesigner
{
   /// <summary>
   /// property descriptor to show non-native read-only properties
   /// </summary>
   class MgPropertyDescriptor : PropertyDescriptor
   {
      Component component;
      object value, defaultValue;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="component"></param>
      /// <param name="name"></param>
      /// <param name="value"></param>
      internal MgPropertyDescriptor(Component component, string name, object value, Attribute[] attr)
         : base(name, attr)
      {
         this.component = component;
         this.value = defaultValue = value;
      }

      /// <summary>
      /// set the default value
      /// </summary>
      /// <param name="value"></param>
      internal void SetDefaultValue(object value)
      {
         defaultValue = value;
      }

      public override bool CanResetValue(object component)
      {
         return false;
      }

      public override Type ComponentType
      {
         get { return component.GetType(); }
      }

      public override object GetValue(object component)
      {
         return value;
      }

      public override bool IsReadOnly
      {
         get { return true; }
      }

      public override Type PropertyType
      {
         get { return typeof(string); }
      }

      public override void ResetValue(object component)
      {
         value = defaultValue;
      }

      public override void SetValue(object component, object value)
      {
         this.value = value;
      }

      public override bool ShouldSerializeValue(object component)
      {
         if (defaultValue == null)
            return value != null;
         return !defaultValue.Equals(value);
      }

      public override string Category
      {
         get
         {
            return "Misc";
         }
      }
   }
}
