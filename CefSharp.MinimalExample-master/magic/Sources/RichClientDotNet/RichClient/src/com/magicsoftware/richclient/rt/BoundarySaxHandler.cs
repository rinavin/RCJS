using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.rt
{
   class BoundarySaxHandler : MgSAXHandler
   {
      List<BoundaryFactory> boundaryFactories = new List<BoundaryFactory>();

      public IEnumerable<BoundaryFactory> BoundaryFactories { get { return boundaryFactories; } }

      public override void startElement(string elementName, System.Collections.Specialized.NameValueCollection attributes)
      {
         if (elementName == XMLConstants.MG_TAG_BOUNDARY)
         {
            var factory = new BoundaryFactory()
            {
               BoundaryFieldIndex = Int32.Parse(attributes[XMLConstants.MG_ATTR_FLD]),
               MinExpressionId = Int32.Parse(attributes[XMLConstants.MG_ATTR_MIN]),
               MaxExpressionId = Int32.Parse(attributes[XMLConstants.MG_ATTR_MAX])
            };
            boundaryFactories.Add(factory);
         }
      }
   }
}
