using System;
using System.Collections.Generic;
using System.Text;

namespace com.magicsoftware.util.Xml
{
   /// <summary>
   /// Class for tracking the xml parser's current location. The location is
   /// expressed by two values: the current tag's start and end positions.<para/>
   /// </summary>
   public class XmlParserCursor
   {
      public static readonly XmlParserCursor Beginning = new XmlParserCursor();
      public static readonly XmlParserCursor End = new XmlParserCursor() { StartPosition = int.MaxValue };

      const int INVALID_POSITION = -1;

      int startPosition = 0;
      int endPosition = 0;

      public int StartPosition
      {
         get
         {
            ValidateCursor();
            return startPosition;
         }
         set
         {
            ValidateCursor();
            endPosition = Span + value;
            startPosition = value;
         }
      }

      public int EndPosition
      {
         get
         {
            ValidateCursor();
            return endPosition;
         }
         set
         {
            ValidateCursor();
            if (value < startPosition)
               throw new System.InvalidOperationException("EndPosition cannot be lesser than StartPosition.");
            endPosition = value;
         }
      }

      public int Span
      {
         get
         {
            ValidateCursor();
            return EndPosition - StartPosition;
         }

         set
         {
            ValidateCursor();
            endPosition = StartPosition + value;
         }
      }

      public bool IsValid { get { return startPosition >= 0; } }

      public void Invalidate()
      {
         startPosition = INVALID_POSITION;
         endPosition = INVALID_POSITION;
      }

      public void Reset()
      {
         startPosition = 0;
         endPosition = 0;
      }

      void ValidateCursor()
      {
         if (startPosition == INVALID_POSITION)
            throw new InvalidOperationException("The cursor was invalidated and thus cannot be used until reset.");
      }

      public XmlParserCursor Clone()
      {
         XmlParserCursor clone = new XmlParserCursor();
         clone.startPosition = startPosition;
         clone.endPosition = endPosition;
         return clone;
      }

      public void CloseGapForward()
      {
         startPosition = endPosition;
      }

      public override string ToString()
      {
         return "(" + startPosition + "-" + endPosition + ")";
      }

      public override bool Equals(object obj)
      {
         XmlParserCursor other = obj as XmlParserCursor;
         if (obj == null)
            return false;

         return (startPosition == other.startPosition) && (endPosition == other.endPosition);
      }

      public override int GetHashCode()
      {
         return base.GetHashCode();
      }
   }
}
