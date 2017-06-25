using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;

namespace RuntimeDesigner.Serialization
{
   /// <summary>
   /// Serializable Control Item
   /// </summary>
      
   [Serializable]
   public class ControlItem
   {
      #region CTOR
      public ControlItem()
      {
      }
      

      #endregion

      #region properties
      [XmlElement(ElementName = "Property")]
      public List<PropertyItem> Properties { get; set; }

      [XmlAttribute]
      public int Isn { get; set; }

      [XmlAttribute]
      public String ControlType{ get; set; }
      #endregion
   }
}
