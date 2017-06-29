using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace com.magicsoftware.unipaas
{ 
   /// <summary>
   /// this class will be used for communication with JS
   /// it will hold delegates with callbacks for the JS 
   /// </summary>
   public class JSBridge
   {
      JSBridge()
      {

      }
      public delegate void RefreshUIDelegate(string UIDesctiption);
      public delegate string GetControlValueDelegate(string controlName);

      public RefreshUIDelegate refreshUIDelegate;
      public GetControlValueDelegate getControlValueDelegate;
      public void RefreshUI(string UIDesctiption)
      {
         if (refreshUIDelegate != null)
            refreshUIDelegate(UIDesctiption);
      }

      public string GetControlValue(string controlName)
      {
         if (getControlValueDelegate != null)
            return getControlValueDelegate(controlName);
         return "";
      }

      private static JSBridge instance;
      public static JSBridge Instance
      {
         get
         {
            if (instance == null)
            {
               lock (typeof(JSBridge))
               {
                  if (instance == null)
                     instance = new JSBridge();
               }
            }
            return instance;
         }

         private set
         {
            instance = value;
         }
      }
   }
}
