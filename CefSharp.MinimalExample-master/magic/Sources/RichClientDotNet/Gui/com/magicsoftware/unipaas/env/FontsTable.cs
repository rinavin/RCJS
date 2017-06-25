using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using com.magicsoftware.unipaas.gui.low;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.unipaas.env
{
   /// <summary>
   ///   data for <fonttable value = ...>
   /// </summary>
   public class FontsTable
   {
      public const int ENV_FONT_TOOLTIP = 22; 
      private readonly MgFont _defaultFont;
      private readonly List<MgFont> _fontTable;

      /// <summary>
      ///   FONT
      /// </summary>
      internal FontsTable()
      {
         _fontTable = new List<MgFont>();

         //init the default font
         _defaultFont = new MgFont(0, "MS Sans Serif", 8, 0, 0, 0);
      }

      /// <summary>
      ///   get font
      /// </summary>
      /// <param name = "idx">index of the font</param>
      internal MgFont getFont(int idx)
      {
         MgFont mgFont;

         lock (this)
         {
            if (idx < 1 || idx > _fontTable.Count)
               mgFont = _defaultFont;
            else
               mgFont = _fontTable[idx - 1];
         }

         return mgFont;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fontxml"></param>
      public void FillFrom(byte[] fontxml)
      {
         lock (this)
         {
            //Clear the font's table before filling in new values.
            if (_fontTable.Count > 0)
               _fontTable.Clear();

            try
            {
               if (fontxml != null)
                  new FontTableSaxHandler(this, fontxml);
            }
            catch (Exception ex)
            {
               Events.WriteExceptionToLog(ex.Message);
            }
         }
      }

      /// <summary>
      /// change the values on the font in the specified index
      /// </summary>
      /// <param name="index"></param>
      /// <param name="fontName"></param>
      /// <param name="size"></param>
      /// <param name="scriptCode"></param>
      /// <param name="orientation"></param>
      /// <param name="bold"></param>
      /// <param name="italic"></param>
      /// <param name="strike"></param>
      /// <param name="underline"></param>
      /// <returns></returns>
      public bool SetFont(int index, string fontName, int size, int scriptCode, int orientation, bool bold, bool italic, bool strike, bool underline)
      {
         if (index == 0 || index > _fontTable.Count)
            return false;

         FontAttributes fa = 0;
         if(bold)
            fa |= FontAttributes.FontAttributeBold;
         if(italic)
            fa |= FontAttributes.FontAttributeItalic;
         if(strike)
            fa |= FontAttributes.FontAttributeStrikethrough;
         if(underline)
            fa |= FontAttributes.FontAttributeUnderline;

         _fontTable[index - 1].SetValues(fontName, size, fa, orientation, scriptCode);

         return true;
      }

      #region Nested type: FontTableSaxHandler

      /// <summary>
      /// 
      /// </summary>
      internal class FontTableSaxHandler : MgSAXHandlerInterface
      {
         private readonly FontsTable _enclosingInstance;

         internal FontTableSaxHandler(FontsTable enclosingInstance, byte[] fontxml)
         {
            _enclosingInstance = enclosingInstance;
            MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
            mgSAXHandler.parse(fontxml);
         }

         public void endElement(String elementName, String elementValue, NameValueCollection attributes)
         {
            if (elementName == XMLConstants.MG_TAG_FONT_ENTRY)
            {
               String styleName;
               int index = 0;
               int height = 0;
               FontAttributes style = 0;
               String typeFace = null;
               int charSet = 0;
               int orientation = 0;

               IEnumerator enumerator = attributes.GetEnumerator();
               while (enumerator.MoveNext())
               {
                  String attr = (String) enumerator.Current;
                  switch (attr)
                  {
                     case XMLConstants.MG_ATTR_ID:
                        index = Int32.Parse(attributes[attr]);
                        break;

                     case XMLConstants.MG_ATTR_HEIGHT:
                        height = Int32.Parse(attributes[attr]);
                        break;

                     case XMLConstants.MG_TYPE_FACE:
                        typeFace = attributes[attr];
                        break;
                     case XMLConstants.MG_ATTR_CHAR_SET:
                        charSet = Int32.Parse(attributes[attr]);
                        break;

                     case XMLConstants.MG_ATTR_STYLE:
                        styleName = attributes[attr];
                        if (styleName.IndexOf('B') > -1) // Check if styleName contains "B"
                           style |= FontAttributes.FontAttributeBold;
                        if (styleName.IndexOf('I') > -1) // Check if styleName contains "I"
                           style |= FontAttributes.FontAttributeItalic;
                        if (styleName.IndexOf('U') > -1) // Check if styleName contains "U"
                           style |= FontAttributes.FontAttributeUnderline;
                        if (styleName.IndexOf('S') > -1) // Check if styleName contains "S"
                           style |= FontAttributes.FontAttributeStrikethrough;
                        break;

                     case XMLConstants.MG_ATTR_ORIENTATION:
                        orientation = Int32.Parse(attributes[attr]);
                        break;

                     default:
                        Events.WriteExceptionToLog(
                           "There is no such tag in MgFont class. Insert case to FontTable.endElement() for: " + attr);
                        break;
                  }
               }

               MgFont font = new MgFont(index, typeFace, height, style, orientation, charSet);
               lock (_enclosingInstance)
               {
                  _enclosingInstance._fontTable.Add(font);
               }
            }
               // It should come in this function only for FontTable tag or for Font tag..For other tags we should write error to log
            else if (elementName != XMLConstants.MG_TAG_FONTTABLE)
               Events.WriteExceptionToLog("There is no such tag in FontTable.endElement(): " +
                                                               elementName);
         }
      }

      #endregion
   }
}
