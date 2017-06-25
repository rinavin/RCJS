using System;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.util
{
   /// <summary>
   /// SAX handler to parse an <objectRef ...> tag. The handler expects
   /// two attributes: ctl_idx and isn, and uses them to create an ObjectReference
   /// instance.<br/>
   /// The created instance is kept in 'ParsedReference' property.
   /// </summary>
   class ObjectReferenceSAXHandler : MgSAXHandler
   {
      public ObjectReference ParsedReference { get; private set; }

      public override void startElement(string elementName, System.Collections.Specialized.NameValueCollection attributes)
      {
         base.startElement(elementName, attributes);
         int ctlIndex = int.Parse(attributes["ctl_idx"]);
         int objectIsn = int.Parse(attributes["isn"]);
         ParsedReference = new ObjectReference(ctlIndex, objectIsn);
      }
   }
}
