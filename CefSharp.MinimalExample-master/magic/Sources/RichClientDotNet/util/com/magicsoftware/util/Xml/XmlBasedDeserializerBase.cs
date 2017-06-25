using System;
using System.Collections.Generic;
using System.Text;
using util.com.magicsoftware.util;

namespace com.magicsoftware.util.Xml
{
   public abstract class XmlBasedDeserializerBase
   {
      public bool Deserialize(EnhancedXmlParser parser)
      {
         using (parser.ElementBoundary())
         {
            if (!EnterElement(parser))
               return false;

            while (parser.MoveToNextTag(GetTagParsingStrategy()))
            {
               if (!parser.IsOnElementClosingTag)
               {
                  if (!ProcessInnerElement(parser))
                  {
                     Logger.Instance.WriteErrorToLog("An error occurred while deserializing an object. See previous messages for more details.");
                     return false;
                  }
               }
            }

            if (!LeaveElement(parser))
               return false;
         }
         return true;
      }

      protected virtual bool EnterElement(EnhancedXmlParser parser)
      {
         return true;
      }

      protected virtual bool LeaveElement(EnhancedXmlParser parser)
      {
         return true;
      }

      protected virtual bool ProcessInnerElement(EnhancedXmlParser parser)
      {
         return true;
      }

      protected virtual IXmlTagParsingStrategy GetTagParsingStrategy()
      {
         return DefaultTagParsingStrategy.Instance;
      }
   }
}
