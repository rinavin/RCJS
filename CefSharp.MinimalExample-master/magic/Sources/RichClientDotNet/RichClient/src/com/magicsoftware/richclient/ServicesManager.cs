using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.richclient
{
   /// <summary>
   /// 
   /// </summary>
   class ServicesManager
   {
      Dictionary<Type, object> services = new Dictionary<Type, object>();

      /// <summary>
      /// 
      /// </summary>
      /// <param name="serviceType"></param>
      /// <returns></returns>
      public object GetService(Type serviceType)
      {
         object value = null;

         services.TryGetValue(serviceType, out value);

         return value;

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="serviceType"></param>
      /// <param name="value"></param>
      public void SetService(Type serviceType, object value)
      {
         if (services.ContainsKey(serviceType))
            services.Remove(serviceType);

         services.Add(serviceType, value);
      }
   }
}
