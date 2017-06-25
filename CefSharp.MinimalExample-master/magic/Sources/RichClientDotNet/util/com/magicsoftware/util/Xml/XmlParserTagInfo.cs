using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// Holds xml tag information. This class is used by the various xml parser components to 
   /// relay tag information from one to the other.
   /// </summary>
   public class XmlParserTagInfo
   {
      public static readonly XmlParserTagInfo NullTag = new XmlParserTagInfo(new XmlParserCursor(), "");

      XmlParserCursor cursor;

      /// <summary>
      /// Gets the position within the xml string, where the tag starts.
      /// </summary>
      public int StartPosition { get {return cursor.StartPosition;} }

      /// <summary>
      /// Gets the position within the xml string, where the tag ends.
      /// </summary>
      public int EndPosition { get {return cursor.EndPosition;} }

      /// <summary>
      /// Gets the name of the tag.
      /// </summary>
      public string Name { get; private set;}

      /// <summary>
      /// Gets or sets a value denoting whether the tag represents and empty element, which
      /// is an element without sub-elements (e.g. &lt;tag /&gt;).
      /// </summary>
      public bool IsEmptyElement { get; set; }

      /// <summary>
      /// Gets or sets a value denoting whether the tag is a the last tag of an element
      /// ('&lt;/tagname ...').
      /// </summary>
      public bool IsElementClosingTag { get; set; }

      /// <summary>
      /// Gets or sets a value denoting whether the element is used as a boundary.
      /// </summary>
      public bool IsBoundaryElement { get; set; }

      public XmlParserTagInfo(XmlParserCursor cursor, string name)
      {
         this.cursor = cursor.Clone();
         this.Name = name;
         IsEmptyElement = false;
         IsElementClosingTag = false;
         IsBoundaryElement = false;
      }

      public override string ToString()
      {
         return String.Format("Tag: {2}{0}{3}, {1}", Name, cursor, IsElementClosingTag ? "/" : "", IsEmptyElement ? "/" : "");
      }

      public XmlParserTagInfo Clone()
      {
         XmlParserTagInfo clone = this.MemberwiseClone() as XmlParserTagInfo;
         clone.cursor = cursor.Clone();
         return clone;
      }

      public override int GetHashCode()
      {
         HashCodeBuilder hashBuilder = new HashCodeBuilder();
         hashBuilder.Append(StartPosition).Append(EndPosition).Append(Name);
         return hashBuilder.HashCode;
      }

      public override bool Equals(object obj)
      {
         XmlParserTagInfo other = obj as XmlParserTagInfo;
         if (other == null)
            return false;

         return (StartPosition == other.StartPosition) && (EndPosition == other.EndPosition) && (Name == other.Name);
      }
   }
}
