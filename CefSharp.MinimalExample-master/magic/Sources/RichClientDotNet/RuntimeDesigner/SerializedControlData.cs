using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;

namespace RuntimeDesigner
{


   /// <summary>
   /// Summary description for CopyControl.
   /// </summary>
   [Serializable()]
   class SerializedControlData
   {
      private Hashtable propertyList = new Hashtable();

      static SerializedControlData()
      {
      }

      internal string ControlName { get; set; }
      internal string PartialName { get; set; }

      internal Hashtable PropertyList
      {
         get
         {
            return propertyList;
         }

      }


      internal SerializedControlData()
      {

      }

      internal SerializedControlData(Control ctrl)
      {
         ControlName = ctrl.GetType().Name;
         PartialName = ctrl.GetType().Namespace;

         PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(ctrl);

         foreach (PropertyDescriptor myProperty in properties)
         {
            try
            {
               if (myProperty.PropertyType.IsSerializable)
                  propertyList.Add(myProperty.Name, myProperty.GetValue(ctrl));
            }
            catch (Exception ex)
            {
               System.Diagnostics.Trace.WriteLine(ex.Message);
               //do nothing, just continue
            }

         }

      }


   }
}

