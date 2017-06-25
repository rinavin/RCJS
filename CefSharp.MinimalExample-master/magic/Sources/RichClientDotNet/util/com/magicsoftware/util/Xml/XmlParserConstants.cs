using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   static class XmlParserConstants
   {
      static readonly string Whitespace = " \t\r\n";
      static readonly string EndOfTag = "/>";

      public static readonly char[] WhitespaceChars = Whitespace.ToCharArray();
      public static readonly char[] EndOfTagNameChars = (Whitespace + EndOfTag).ToCharArray();
   }
}
