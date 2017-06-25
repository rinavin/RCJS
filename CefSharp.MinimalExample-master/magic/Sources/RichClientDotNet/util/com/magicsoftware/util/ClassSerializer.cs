using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Diagnostics;

namespace util.com.magicsoftware.util
{
   /// <summary>
   /// class sereializer uses XMLSerializer to serialize the class
   /// </summary>
   public class ClassSerializer
   {
      /// <summary>
      /// file name to serialize data to
      /// </summary>
      public string FileName { get; set; }

      /// <summary>
      /// string to serialize data to
      /// </summary>
      public string XMLString { get; set; }
 
      
      /// <summary>
      /// serializes class to file
      /// </summary>
      /// <param name="o"></param>
      public void SerializeToFile(Object o)
      {
         XmlSerializer ser = GetSerializer(o.GetType());
         using (var stream = new FileStream(FileName, FileMode.Create))
         {
            ser.Serialize(stream, o);
         }
      }

      /// <summary>
      /// deserialises data from file
      /// </summary>
      /// <param name="type"></param>
      /// <returns></returns>
      public Object DeSerializeFromFile(Type type)
      {
         Object o;
         XmlSerializer ser = GetSerializer(type);
         using (FileStream stream = new FileStream(FileName, FileMode.Open))
         {
            o = ser.Deserialize(stream);
         }

         return o;
      }

      /// <summary>
      /// dictionary of the serialisers
      /// </summary>
      Dictionary<Type, XmlSerializer> serializers = new Dictionary<Type, XmlSerializer>();
      XmlSerializer GetSerializer(Type type)
      {
         if (!serializers.ContainsKey(type))
            serializers[type] = new XmlSerializer(type);
         return serializers[type];
      }


      /// <summary>
      /// serialize to string
      /// </summary>
      /// <param name="o"></param>
      /// <returns></returns>
      public String SerializeToString(Object o)
      {
         String result = null;
         try
         {
            XmlSerializer serializer = GetSerializer(o.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
               serializer.Serialize(stream, o);
               result = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
            }

         }
         catch (Exception e)
         {

            MessageBox.Show(e.ToString());
         }
         return result;
      }

      /// <summary>
      /// desiralize from string
      /// </summary>
      /// <param name="type"></param>
      /// <param name="text"></param>
      /// <returns></returns>
      public Object DeserializeFromString(Type type, String text)
      {
         Object o = null;
         using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
         {
            o = GetSerializer(type).Deserialize(stream);
         }
         return o;
      }


      public object Deserialize(Type type)
      {
         if (FileName != null)
            return DeSerializeFromFile(type);
         else if (XMLString != null)
            return DeserializeFromString(type, XMLString);
         Debug.Assert(false);
         return false;
      }
   }
}
