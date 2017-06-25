using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.local.application.datasources;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.richclient.local.application;
using com.magicsoftware.gatewaytypes;
using System.Xml.Serialization;
using System.IO;
using com.magicsoftware.unipaas.management.data;


namespace com.magicsoftware.richclient.local.data.cursor
{
   /// <summary>
   /// class Runtime cursor
   /// </summary>
   [XmlInclude(typeof(NUM_TYPE))]
   public class RuntimeCursor 
   {
      public CursorDefinition CursorDefinition { get; set; }
      public RuntimeCursorData RuntimeCursorData { get; set; }
      [XmlAttribute]
      public int ID { get; set; }

      public override int GetHashCode()
      {
         return ID;
      }

      public override bool Equals(object obj)
      {
         if (obj != null && obj.GetHashCode() == GetHashCode())
            return true;
         return false;
      }

   }
}



