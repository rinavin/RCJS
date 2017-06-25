using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// Maintains a list of xml tags, which together comprise an xml path.
   /// This class is used to keep track of the current xml parser's path as it reads
   /// through an xml stream.<para/>
   /// The class allows either appending a tag to the end of the path or removing the
   /// last element from the path. Doing so, it is able to tell the current tag depth and
   /// serialize the path into a readable string format.
   /// </summary>
   class XmlParserPath
   {
      List<XmlParserTagInfo> _path = new List<XmlParserTagInfo>();

      /// <summary>
      /// Appends the tag as the last path entry.
      /// </summary>
      /// <param name="entry"></param>
      public void Append(XmlParserTagInfo entry)
      {
         _path.Add(entry);
      }

      /// <summary>
      /// Removes the last entry from the path.
      /// </summary>
      /// <returns></returns>
      public XmlParserTagInfo RemoveLastEntry()
      {
         XmlParserTagInfo lastEntry = _path[_path.Count - 1];
         _path.RemoveAt(_path.Count - 1);
         return lastEntry;
      }

      /// <summary>
      /// Gets the path's depth - i.e. number of path entries.
      /// </summary>
      public int Depth { get { return _path.Count; } }

      /// <summary>
      /// Gets the path entries as an array of string. Each string
      /// in the array is the name of tag in that position in the path.
      /// </summary>
      /// <returns>A string array, where the first element (0) is the root xml node name.</returns>
      public string[] ToStringArray()
      {
         string[] pathArray = new string[_path.Count];
         int i = 0;
         foreach (var pathEntry in _path)
         {
            pathArray[i] = pathEntry.Name;
            i++;
         }
         return pathArray;
      }

      /// <summary>
      /// Creates a string describing the path. Each entry is preceded with
      /// a '/'. So the path of tag1, tag2, tag3 will look like so:
      /// /tag1/tag2/tag3
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         StringBuilder pathString = new StringBuilder();
         foreach (var pathEntry in _path)
         {
            pathString.Append("/").Append(pathEntry.Name);
         }
         return pathString.ToString();
      }

      /// <summary>
      /// Deep copy of the path entries, so changes made to the entries of
      /// the original or cloned paths do not affect the other path.
      /// </summary>
      /// <returns></returns>
      public XmlParserPath Clone()
      {
         XmlParserPath clone = new XmlParserPath();
         foreach (var pathEntry in _path)
         {
            clone.Append(pathEntry.Clone());
         }
         return clone;
      }

      /// <summary>
      /// Removes all elements from the path.
      /// </summary>
      internal void Clear()
      {
         _path.Clear();
      }
   }
}
