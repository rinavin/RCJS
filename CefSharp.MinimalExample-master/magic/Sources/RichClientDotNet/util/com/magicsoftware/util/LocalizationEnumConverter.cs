using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Resources;

namespace com.magicsoftware.util
{
   public delegate string GetLocalizedDescriptionDelegate(FieldInfo fieldInfo, object value); 

   /// <summary>   
   /// This class is a converter for enumerations.  
   /// Purpose is to localize enum values.   
   /// </summary>   
   public class LocalizationEnumConverter : EnumConverter
   {
      public static GetLocalizedDescriptionDelegate GetLocalizedDescriptionDelegateRef;
      Dictionary<CultureInfo, Dictionary<string, object>> _lookupTables;
      IList partialList = null;

      static LocalizationEnumConverter()
      {
         // initialize default GetLocalizedDescription.
         // It simply returns the enum value as string.
         GetLocalizedDescriptionDelegateRef = delegate(FieldInfo fieldInfo, object value)
         {
            return value.ToString();
         };
      }

      /// <summary>  
      /// Instantiate a new Enum Converter  
      /// </summary>  
      /// <param name="type">Type of the enum to convert</param>  
      public LocalizationEnumConverter(Type type)
         : base(type)
      {
         _lookupTables = new Dictionary<CultureInfo, Dictionary<string, object>>();
      }

      /// <summary>  
      /// Instantiate a new Enum Converter with a partial list of values
      /// </summary>  
      /// <param name="type">Type of the enum to convert</param>  
      public LocalizationEnumConverter(Type type, IList partialList)
         : base(type)
      {
         _lookupTables = new Dictionary<CultureInfo, Dictionary<string, object>>();
         this.partialList = partialList;
      }


      /// <summary> 
      /// The lookup table holds the references between the original values and the localized values.       
      /// </summary>  
      /// <param name="culture">Culture for which the localization pairs must be fetched (or created)</param>  
      /// <returns>Dictionary</returns>   
      private Dictionary<string, object> GetLookupTable(CultureInfo culture)
      {
         Dictionary<string, object> result = null;
         if (culture == null)

            culture = CultureInfo.CurrentCulture;
         if (!_lookupTables.TryGetValue(culture, out result))
         {
            result = new Dictionary<string, object>();
            foreach (object value in GetValues())
            {
               string text = GetString(culture, value);

               if (text != null)
               {
                  result.Add(text, value);
               }
            }
            _lookupTables.Add(culture, result);
         }
         return result;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="culture"></param>
      /// <param name="value"></param>
      /// <returns></returns>
      public virtual string GetString(CultureInfo culture, object value)
      {
         return ConvertToString(null, culture, value);
      }
      /// <summary>  
      /// Convert the localized value to enum-value  
      /// </summary> 
      /// <param name="context"></param> 
      /// <param name="culture"></param>  
      /// <param name="value"></param>  
      /// <returns></returns>  
      public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
      {
         if (value is string)
         {
            Dictionary<string, object> lookupTable = GetLookupTable(culture);
            object result = null;

            if (!lookupTable.TryGetValue(value as string, out result))
               throw new FormatException("Value \"" + value + "\" is not allowed");
            
            return result;
         }
         else
         {
            return base.ConvertFrom(context, culture, value);
         }
      }

      /// <summary>  
      /// Convert the enum value to a localized value 
      /// </summary>       
      /// <param name="context"></param>  
      /// <param name="culture"></param>  
      /// <param name="value"></param>  
      /// <param name="destinationType"></param> 
      /// <returns></returns>  
      public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
      {

         if (value != null && destinationType == typeof(string))
         {
            if (value is string)
               return value;

            Type type = value.GetType();

            FieldInfo f = type.GetField(value.ToString());
            if (f != null)
               return GetLocalizedDescriptionDelegateRef(f, value);
            else
               return "";
         }
         else
         {
            return base.ConvertTo(context, culture, value, destinationType);
         }
      }


      public StandardValuesCollection GetValues()
      {
         if (partialList != null)
         {
            return new StandardValuesCollection(partialList);
         }
         else
            return base.GetStandardValues(null);
      }

      /// <summary>
      /// If a  partial list was defined, use it as standard values collection
      /// </summary>
      /// <param name="context"></param>
      /// <returns></returns>
      public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
      {
         Dictionary<string, object> lookup = GetLookupTable(Thread.CurrentThread.CurrentCulture);
         string[] keys = new string[lookup.Keys.Count];
         lookup.Keys.CopyTo(keys, 0);
         Array.Sort(keys);

         List<object> valuesList = new List<object>();

         foreach(string key in keys)
            valuesList.Add(lookup[key]);

         return new StandardValuesCollection(valuesList);
        
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="context"></param>
      /// <returns></returns>
      public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
      {
         return true;
      }
   }

}
