using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.util
{
   /// <summary>
   /// Represents a reference to a magic object, in the form of
   /// ctl index and object isn.
   /// The reference makes no assumption on the type of the referenced object. The
   /// referenced object type is context dependent.
   /// </summary>
   public class ObjectReference
   {
      public int CtlIndex { get; private set; }
      public int ObjectISN { get; private set; }

      public ObjectReference(int ctlIndex, int objectIsn)
      {
         CtlIndex = ctlIndex;
         ObjectISN = objectIsn;
      }

      public override string ToString()
      {
         return "{Object Ref: " + CtlIndex + "," + ObjectISN + "}";
      }

      public static ObjectReference FromXML(string xmlData)
      {
         ObjectReferenceSAXHandler handler = new ObjectReferenceSAXHandler();
         MgSAXParser parser = new MgSAXParser(handler);
         parser.Parse(xmlData);
         return handler.ParsedReference;
      }
   }

}

