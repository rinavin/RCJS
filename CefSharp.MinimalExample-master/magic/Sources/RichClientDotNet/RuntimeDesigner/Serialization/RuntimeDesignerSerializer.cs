using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using com.magicsoftware.support;
using System.Windows.Forms;
using System.IO;
using com.magicsoftware.util;
using System.Xml;
using System.Drawing;

namespace RuntimeDesigner.Serialization
{
   /// <summary>
   /// 
   /// </summary>
   public class RuntimeDesignerSerializer
   {

      // Construct an instance of the XmlSerializer with the type of object that is being de serialized.
      static System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<ControlItem>));

      /// <summary>
      /// save the info into file:
      /// Note each subform is saved in another file.
      /// </summary>
      internal static void SerializeToFiles(Dictionary<object, ComponentWrapper> componentWrapperList)
      {
         Dictionary<String, Dictionary<object, ComponentWrapper>> dictPerFileName = new Dictionary<String, Dictionary<object, ComponentWrapper>>();
         CreateDictionaryPerForm(dictPerFileName, componentWrapperList);

         // create file for each form or subform
         foreach (var filename in dictPerFileName.Keys)
            RuntimeDesignerSerializer.SerializeToFile(filename, dictPerFileName[filename]);
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="dictPerFileName"></param>
      /// <param name="componentWrapperList"></param>
      internal static void CreateDictionaryPerForm(Dictionary<String, Dictionary<object, ComponentWrapper>> dictPerFileName, 
                                   Dictionary<object, ComponentWrapper> componentWrapperList)
      {         
         List<ControlItem> serializeObjectList = new List<ControlItem>();

         foreach (var key in componentWrapperList.Keys)
         {
            // get the file name that this control need to be save
            ControlDesignerInfo cdf = ((Control)key).Tag as ControlDesignerInfo;
            Dictionary<object, ComponentWrapper> dictControlsForForm = null;
                        
            if (cdf != null && !String.IsNullOrEmpty(cdf.FileName))
            {
               // if we don't have dict for this file name , create Dictionary and add it to the dictPerFileName
               if (!dictPerFileName.ContainsKey(cdf.FileName))
               {
                  dictControlsForForm = new Dictionary<object, ComponentWrapper>();
                  dictPerFileName.Add(cdf.FileName, dictControlsForForm);
               }
               else
                  dictControlsForForm = dictPerFileName[cdf.FileName];

               // add the control to the relevant Dictionary
               dictControlsForForm.Add((Control)key, componentWrapperList[(Control)key]);
            }
         }
      }

      internal static bool SerializeToFile(String fileName, List<ControlItem> serializeObjectList)
      {

         bool success = true;
         try
         {
            String XmlizedString = SerializeToString(serializeObjectList);

            if ((!String.IsNullOrEmpty(fileName)) && File.Exists(fileName))
               File.Delete(fileName);

            if (!String.IsNullOrEmpty(XmlizedString))
            {
               if (CheckAndCreatePathIfNeeded(fileName))
                  SaveToFile(fileName, XmlizedString);
            }
         }
         catch
         {
            success = false;
            MessageBox.Show("Error while try to save form designer file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
         }

         return success;
      }
      /// <summary>
      /// SerializeToFile the all list !!!! 
      /// </summary>
      /// <param name="componentWrapperList"></param>
      static void SerializeToFile(String fileName, Dictionary<object, ComponentWrapper> componentWrapperList)
      {

         try
         {
            String XmlizedString = SerializeToString(componentWrapperList);

            if ((!String.IsNullOrEmpty(fileName)) && File.Exists(fileName))
               File.Delete(fileName);

            if (!String.IsNullOrEmpty(XmlizedString))
            {
               if (CheckAndCreatePathIfNeeded(fileName))
                  SaveToFile(fileName, XmlizedString);
            }
         }
         catch
         {
            MessageBox.Show("Error while try to save form designer file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);            
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      /// <param name="XmlizedString"></param>
      private static void SaveToFile(String fileName, String XmlizedString)
      {
         XmlTextWriter streamWriter = new XmlTextWriter(fileName, Encoding.Unicode);
         streamWriter.WriteRaw(XmlizedString);
         streamWriter.Close();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      private static bool CheckAndCreatePathIfNeeded(String fileName)
      {
         String directoryName = Path.GetDirectoryName(fileName);
         bool pathExist = true;
         if ((!String.IsNullOrEmpty(directoryName)) && !Directory.Exists(directoryName))
         {
            if (Directory.CreateDirectory(directoryName) == null)
               pathExist = false;
         }
         return pathExist;
      }

      /// <summary>
      /// De SerializeToFile the all list !!!! 
      /// </summary>
      /// <param name="componentWrapperList"></param>
      public static List<ControlItem> DeSerializeFromFile(String fileName)
      {
         //Load the fileName into this object 
         List<ControlItem> deSerializeObject = null;

         try
         {
            // check if file exist
            if (File.Exists(fileName))
            {
               String xmlString = LoadFromFile(fileName);
               
               deSerializeObject = DeSerializeFromString(xmlString);
            }
         }
         catch
         {
            MessageBox.Show("Error while try to load form designer file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
         }

         return deSerializeObject;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      internal static String LoadFromFile(String fileName)
      {
         String retString = string.Empty;
         if (File.Exists(fileName))
            retString =  File.ReadAllText(fileName);

         return retString;
      }

      /// <summary>
      /// https://social.msdn.microsoft.com/Forums/en-US/5d08bc28-5b61-4c5a-8c4b-4665b1c929ea/serialize-object-to-string?forum=csharplanguage
      /// </summary>
      /// <param name="componentWrapperList"></param>
      /// <returns></returns>
      internal static string SerializeToString(Dictionary<object, ComponentWrapper> componentWrapperList)
      {

         List<ControlItem> serializeObjectList = BuildComponentWrapperList(componentWrapperList);

         return SerializeToString(serializeObjectList);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="serializeObjectList"></param>
      /// <returns></returns>
      internal static String SerializeToString(List<ControlItem> serializeObjectList)
      {
         String XmlizedString = String.Empty;

         // Serialize only if there is items to Serialize
         if (serializeObjectList.Count > 0)
         {
            using (StringWriter writer = new StringWriter())
            {
               serializer.Serialize(writer, serializeObjectList);

               XmlizedString = writer.ToString();
            }
         }

         return XmlizedString;
      }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      /// <returns></returns>
      internal static List<ControlItem> DeSerializeFromString(string stringXml)
      {        
         using (StringReader reader = new StringReader(stringXml))
         {
            return (List<ControlItem>)serializer.Deserialize(reader);
         }      
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="componentWrapperList"></param>
      /// <returns></returns>
      internal static List<ControlItem> BuildComponentWrapperList(Dictionary<object, ComponentWrapper> componentWrapperList)
      {
         var comp = componentWrapperList;
         List<ControlItem> serializeObjectList = new List<ControlItem>();

         foreach (var key in componentWrapperList.Keys)
         {
            ControlItem serializeObject = BuildControlItemFromComponentWrapper((Control)key, componentWrapperList[key]);
            // serialize the control SerializableControlItem only while we have properties that need to be serialize 
            if (serializeObject != null && serializeObject.Properties != null && serializeObject.Properties.Count > 0)
               serializeObjectList.Add(serializeObject);
         }

         return serializeObjectList;
      }

      /// <summary>
      /// return Should Serialize Control
      /// </summary>
      /// <param name="cdf"></param>
      /// <returns></returns>
      static bool ShouldSerializeControl(ControlDesignerInfo cdf)
      {
         bool shouldSerializeControl = true;
         //don't serilize frame control
         if (cdf == null || cdf.IsFrame)
            shouldSerializeControl = false;

         return shouldSerializeControl;

      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="key"></param>
      /// <param name="componentWrapper"></param>
      /// <returns></returns>
      static ControlItem BuildControlItemFromComponentWrapper(Control key, ComponentWrapper componentWrapper)
      {
         ControlItem serializeObject = null;

         if (componentWrapper != null)
         {
            // get the isn of the control
            ControlDesignerInfo cdf = ((Control)key).Tag as ControlDesignerInfo;
            // don't save items of the frame
            if (ShouldSerializeControl(cdf))
            {
               serializeObject = new ControlItem();
               serializeObject.Isn = cdf.Isn;
               // the control type 
               serializeObject.ControlType = cdf.ControlType.ToString();               

               serializeObject.Properties = BuildProperties(componentWrapper, cdf);
            }
         }

         return serializeObject;
      }

      /// <summary>
      /// bild properties to serialize
      /// </summary>
      /// <param name="componentWrapper"></param>
      /// <param name="previousPlacementBounds"></param>
      /// <returns></returns>
      static List<PropertyItem> BuildProperties(ComponentWrapper componentWrapper, ControlDesignerInfo controlDesignerInfo)
      {
         List<PropertyItem> list = new List<PropertyItem>();

         foreach (PropertyDescriptor item in componentWrapper.PropertiesDescriptors)
         {
            Object component = componentWrapper.GetPropertyOwner(item);
            if (item.ShouldSerializeValue(component))
            {
               object valueItem = item.GetValue(component);
               object offsetValueItem = valueItem; // value to hold offset for runtime tab-order calculations

               if (valueItem != null)
               {
                  if (ComponentWrapper.IsCoordinateProperty(item.Name))
                  {
                     valueItem = ((int)valueItem) - controlDesignerInfo.GetPlacementForProp(item.Name);
                     offsetValueItem = componentWrapper.GetTabOrderOffset(item.Name, (int)valueItem);
                  }
                  TypeConverter converter = TypeDescriptor.GetConverter(valueItem.GetType());
                  String serString = converter.ConvertToString(valueItem);
                  
                  if(valueItem == offsetValueItem)
                     list.Add(new PropertyItem(item.Name, serString, valueItem.GetType()));
                  else
                     // we have an offset for tab-order calculation
                     list.Add(new TabOrderOffsetPropertyItem(item.Name, serString, valueItem.GetType(), converter.ConvertToString(offsetValueItem)));
               }
            }

         }
         return list;
      }
   
      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileName"></param>
      public static bool ControlsPersistencyClearPropertiesAndSaveVisiblePropertyOnly(String fileName)
      {
         bool success = true;

         if (File.Exists(fileName))
         {
            List<ControlItem> orgControlItems = RuntimeDesignerSerializer.DeSerializeFromFile(fileName);
            List<ControlItem> newControlItems = new List<ControlItem>();

            //for each control item save the Visible property and add it to the newProperties
            foreach (ControlItem controlItem in orgControlItems)
            {
               List<PropertyItem> newProperties = new List<PropertyItem>();

               foreach (PropertyItem propertyItem in controlItem.Properties)
               {
                  if (propertyItem.Key.Equals(Constants.WinPropVisible))
                     newProperties.Add(propertyItem);
               }

               //only if there is property visible we need to add it to the newControlItems
               if (newProperties.Count > 0)
               {
                  controlItem.Properties = newProperties;
                  newControlItems.Add(controlItem);
               }
            }

            // serialize newControlItems to file 
            success = RuntimeDesignerSerializer.SerializeToFile(fileName, newControlItems);
         }

         return success;
      }
   }
}
