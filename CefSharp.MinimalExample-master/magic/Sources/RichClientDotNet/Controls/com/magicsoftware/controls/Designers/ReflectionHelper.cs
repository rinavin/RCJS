using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace com.magicsoftware.controls.designers
{

   /// <summary>
   /// simple reflection utils for designer purpuses
   /// </summary>
   internal static class ReflecionDesignHelper
   {
      static internal Assembly DesignAssembly = System.Reflection.Assembly.Load("System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
      static internal Assembly FormsAssembly = System.Reflection.Assembly.Load("System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089");

      /// <summary>
      /// get type by name
      /// </summary>
      /// <param name="typeName"></param>
      /// <param name="assembly"></param>
      /// <returns></returns>
      internal static Type GetType(string typeName, Assembly assembly)
      {
         return DesignAssembly.GetType(typeName);
      }

      /// <summary>
      /// simple method invoke
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="methodName"></param>
      /// <param name="parameters"></param>
      /// <returns></returns>
      internal static object InvokeMethod(object obj, string methodName, params object[] parameters)
      {
         System.Reflection.MethodInfo method = obj.GetType().GetMethod(methodName);
         return method.Invoke(obj, parameters);
      }


      /// <summary>
      /// simple property invoke
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="propertyName"></param>
      /// <returns></returns>
      internal static object InvokeProperty(object obj, string propertyName)
      {
         System.Reflection.PropertyInfo p = obj.GetType().GetProperty(propertyName);
         return p.GetValue(obj, null);
      }


      /// <summary>
      /// returns true if object o can be assigned to type destTypeName
      /// </summary>
      /// <param name="o"></param>
      /// <param name="destTypeName"></param>
      /// <param name="assembly"></param>
      /// <returns></returns>
      internal static bool IsAssignableFrom(object o, string destTypeName, Assembly assembly)
      {
         if (o != null)
         {
            Type type1 = o.GetType();
            Type type2 = GetType(destTypeName, assembly);
            return type2.IsAssignableFrom(type1);
         }
         return false;
      }

   }
}
