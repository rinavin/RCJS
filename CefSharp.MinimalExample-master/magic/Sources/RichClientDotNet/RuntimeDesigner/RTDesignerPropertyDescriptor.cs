using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using com.magicsoftware.support;
using RuntimeDesigner.RuntimeDesignerStrategies;
using System.Collections.Generic;

namespace RuntimeDesigner
{
   /// <summary>
   /// property descriptor for the runtime designer - wraps the original property descriptor
   /// </summary>
   class RTDesignerPropertyDescriptor : PropertyDescriptor
   {
      PropertyDescriptor originalPropertyDescriptor;

      internal object DefaultValue { get; private set; } // the studio value of the property

      AttributeCollection attributes;

      internal ISetPropertyData SetDataStrategy { set; get; }

      internal ICanResetStrategy CanResetStrategy { get; set; }

      ITranslator translator = null;

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="component"></param>
      /// <param name="originalPropertyDescriptor"></param>
      internal RTDesignerPropertyDescriptor(object component, PropertyDescriptor originalPropertyDescriptor, object studioValue, string name, Attribute[] attr = null)
         : base(name, attr)
      {
         this.originalPropertyDescriptor = originalPropertyDescriptor;

         // recalc the attribute
         List<Attribute> l = new List<Attribute>() { new MergablePropertyAttribute(true), new CategoryAttribute(originalPropertyDescriptor.Category) };
         if (attr != null)
            l.AddRange(attr);
         attributes = new AttributeCollection(l.ToArray());

         DefaultValue = studioValue;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="translator"></param>
      internal void SetTranslator(ITranslator translator)
      {
         this.translator = translator;
      }

      #region PropertyDescriptor overrides
      /// <summary>
      /// 
      /// </summary>
      public override AttributeCollection Attributes
      {
         get
         {
            return attributes;
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="component"></param>
      /// <returns></returns>
      public override bool CanResetValue(object component)
      {
         object value = GetValue(component);

         if (CanResetStrategy != null)
            return CanResetStrategy.CanResetData(DefaultValue, value);

         return (value == null && DefaultValue != null) ||
            (value != null && !value.Equals(DefaultValue));
      }

      /// <summary>
      /// 
      /// </summary>
      public override Type ComponentType
      {
         get { return originalPropertyDescriptor.ComponentType; }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="component"></param>
      /// <returns></returns>
      public override object GetValue(object component)
      {
         object val = originalPropertyDescriptor.GetValue(component);

         return translator != null ? translator.AdjustedGetValue(val) : val;
      }

      /// <summary>
      /// 
      /// </summary>
      public override bool IsReadOnly
      {
         get { return false; }
      }

      /// <summary>
      /// 
      /// </summary>
      public override Type PropertyType
      {
         get { return originalPropertyDescriptor.PropertyType; }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="component"></param>
      public override void ResetValue(object component)
      {
         object value = DefaultValue;

         if (SetDataStrategy != null)
            value = SetDataStrategy.AdjustResettedValue(value);

         SetValue(component, value);

         // Do other stuff which might be required when reseting a value
         RuntimeHostSurface surface = (RuntimeHostSurface)((Component)component).Site.GetService(typeof(DesignSurface));
         surface.HandleControlValueReset(component as Control, Name);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="component"></param>
      /// <param name="value"></param>
      public override void SetValue(object component, object value)
      {
         PropertyDescriptorCollection originalPropDescriptors = TypeDescriptor.GetProvider(component).GetTypeDescriptor(component).GetProperties();
         ControlDesignerInfo controlDesignerInfo = ((ControlDesignerInfo)((Control)component).Tag);

         if (SetDataStrategy != null)
         {
            SetDataStrategy.SetData(originalPropertyDescriptor.GetValue(component), ref value);
         }

         if (translator != null)
            value = translator.AdjustSetValue(value);



         originalPropertyDescriptor.SetValue(component, value);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="component"></param>
      /// <returns></returns>
      public override bool ShouldSerializeValue(object component)
      {
         return CanResetValue(component);
      }

      /// <summary>
      /// 
      /// </summary>
      public override string Category
      {
         get
         {
            return originalPropertyDescriptor.Category;
         }
      }
      #endregion PropertyDescriptor overrides
   }
}
