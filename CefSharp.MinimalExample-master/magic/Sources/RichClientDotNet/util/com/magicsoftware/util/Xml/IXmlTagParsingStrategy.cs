using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   public interface IXmlTagParsingStrategy
   {
      bool FindEndOfTag(string xml, XmlParserCursor cursor);
   }
}
