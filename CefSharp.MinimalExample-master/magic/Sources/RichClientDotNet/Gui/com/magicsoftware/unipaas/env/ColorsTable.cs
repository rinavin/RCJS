using System;
using com.magicsoftware.unipaas.util;
using System.Collections.Specialized;
using System.Collections;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.env;
using System.Diagnostics;
using util.com.magicsoftware.util;

namespace com.magicsoftware.unipaas.env
{
   /// <summary> data for <colortable value=...></summary>
   public class ColorsTable
   {
      internal int ID { get; private set; }
      private readonly MgArrayList _colors;
      private readonly MgArrayList _bgColors;
      private readonly MgArrayList _fgColors;

      private const int COLOR_LENGTH = 9; // color represented in 9 bytes: first 8 for color hex representation, and 1 byte Y/N for transparency

      /// <summary>
      /// Get the total number if color items present in the color table.
      /// </summary>
      public int Count
      {
         get
         {
            return _colors.Count;
         }
      }

      /// <summary> CTOR</summary>
      public ColorsTable()
      {
         _colors = new MgArrayList();
         _bgColors = new MgArrayList();
         _fgColors = new MgArrayList();
      }

      /// <summary> get BackGround color</summary>
      /// <param name="idx">index of the color</param>
      /// <returns> String BackGround color in __RRGGBB format</returns>
      private String getBgStr(int idx, out bool isDefaultColor)
      {
         int start = COLOR_LENGTH;
         String BGcolor;

         BGcolor = getColorStr(idx, start);
         if (BGcolor == null)
         {
            int defIdx = Manager.Environment.GetDefaultColor();
            if (idx == defIdx)
               BGcolor = "000000000N";
            else
               BGcolor = getBgStr(defIdx, out isDefaultColor);
            isDefaultColor = true;
         }
         else
         {
            isDefaultColor = false;
         }

         return BGcolor;
      }

      /// <summary> Retruns the background Color object</summary>
      /// <param name="idx">index of the color</param>
      /// <returns> MgColor object that represents the background color</returns>
      internal MgColor getBGColor(int idx)
      {
         MgColor mgColor = null;

         Debug.Assert(idx > 0);

         lock (this)
         {
            if (idx < _bgColors.Count)
               mgColor = (MgColor)_bgColors[idx];

            if (mgColor == null)
            {
               bool isDefaultColor;
               String colorStr = getBgStr(idx, out isDefaultColor);
               mgColor = new MgColor(colorStr);

               //If default color is used then no need to add it in bgColor table.
               if (!isDefaultColor)
               {
                  if (idx >= _bgColors.Count)
                     _bgColors.SetSize(idx + 1);
                  _bgColors[idx] = mgColor;
               }
            }
         }

         return mgColor;
      }

      /// <summary> 
      /// Returns the foreground Color object
      /// </summary>
      /// <param name="idx">index of the color</param>
      /// <returns> MgColor object that represents the foreground color</returns>
      internal MgColor getFGColor(int idx)
      {
         MgColor mgColor = null;

         Debug.Assert(idx > 0);

         lock (this)
         {
            if (idx < _fgColors.Count)
               mgColor = (MgColor)_fgColors[idx];

            if (mgColor == null)
            {
               bool isDefaultColor;
               string colorStr = getFgStr(idx, out isDefaultColor);
               mgColor = new MgColor(colorStr);

               //If default color is used then no need to add it in fgColor table.
               if (!isDefaultColor)
               {
                  if (idx >= _fgColors.Count)
                     _fgColors.SetSize(idx + 1);
                  _fgColors[idx] = mgColor;
               }
            }
         }

         return mgColor;
      }

      /// <summary> 
      /// get Foreground color string
      /// </summary>
      /// <param name="idx">index of the color</param>
      /// <returns> String Foreground color in __RRGGBB format</returns>
      private String getFgStr(int idx, out bool isDefaultColor)
      {
         string fGcolor = getColorStr(idx, 0);
         if (fGcolor == null)
         {
            int defIdx = Manager.Environment.GetDefaultColor();
            if (idx == defIdx)
               fGcolor = "00FFFFFF  N";
            else
               fGcolor = getFgStr(defIdx, out isDefaultColor);

            isDefaultColor = true;
         }
         else
         {
            isDefaultColor = false;
         }

         return fGcolor;
      }

      /// <summary> 
      /// get FG or BG color
      /// </summary>
      /// <param name="start">index of the color</param>
      /// <returns> String color in RRGGBB format</returns>
      private String getColorStr(int index, int start)
      {
         String colorStr = null;
         int end = start + COLOR_LENGTH;

         lock (this)
         {
            if (index > 0 && index < _colors.Count)
               colorStr = ((String)_colors[index]).Substring(start, end - start);

            // fixed Defect 130709: the error of Illegal index will not be display 
            //else
            //   Events.WriteExceptionToLog(string.Format("Illegal index: {0}", index));
         }

         return colorStr;
      }

      /// <summary>
      /// </summary>
      /// <param name="colorxml">XML formatted content of a colors table.</param>
      public void FillFrom(byte[] colorxml)
      {
         lock (this)
         {
            //Clear the color's table before filling in new values.
            _colors.Clear();
            _bgColors.Clear();
            _fgColors.Clear();

            try
            {
               if (colorxml != null)
                  new ColorTableSaxHandler(this, colorxml);
            }
            catch (Exception ex)
            {
               Events.WriteExceptionToLog(ex);
            }
         }
      }

      /// <summary>
      /// change the values on the color in the specified index
      /// </summary>
      /// <param name="index"></param>
      /// <param name="foreColor"></param>
      /// <param name="backColor"></param>
      /// <returns></returns>
      public bool SetColor(int index, int foreColor, int backColor)
      {
         if (index == 0 || index > Count)
            return false;

         int alpha = (byte)(foreColor >> 24);
         int red = (byte)(foreColor >> 16);
         int green = (byte)(foreColor >> 8);
         int blue = (byte)(foreColor);

         if (index >= _fgColors.Count)
            _fgColors.SetSize(index + 1);
         _fgColors[index] = new MgColor(alpha, red, green, blue, MagicSystemColor.Undefined, false);

         alpha = (byte)(backColor >> 24);
         red = (byte)(backColor >> 16);
         green = (byte)(backColor >> 8);
         blue = (byte)(backColor);

         if (index >= _bgColors.Count)
            _bgColors.SetSize(index + 1);
         _bgColors[index] = new MgColor(alpha, red, green, blue, MagicSystemColor.Undefined, false);

         return true;
      }

      #region Nested type: ColorTableSaxHandler

      /// <summary>
      /// 
      /// </summary>
      internal class ColorTableSaxHandler : MgSAXHandler
      {
         private readonly ColorsTable _enclosingInstance;

         public ColorTableSaxHandler(ColorsTable enclosingInstance, byte[] colorxml)
         {
            _enclosingInstance = enclosingInstance;
            parse(colorxml);
         }

         /// <summary>
         /// 
         /// </summary>
         /// <param name="elementName"></param>
         /// <param name="attributes"></param>
         public override void startElement(String elementName, NameValueCollection attributes)
         {
            if (elementName == XMLConstants.MG_TAG_COLOR_ENTRY)
            {
               int index = 0;
               String colorStr = "";
               IEnumerator enumerator = attributes.GetEnumerator();
               while (enumerator.MoveNext())
               {
                  String attr = (String)enumerator.Current;
                  switch (attr)
                  {
                     case XMLConstants.MG_ATTR_VALUE:
                        colorStr = attributes[attr];
                        break;

                     case XMLConstants.MG_ATTR_ID:
                        index = Int32.Parse(attributes[attr]);
                        break;

                     default:
                        Events.WriteExceptionToLog("in ColorTable.endElement(): unknown  attribute: " + attr);
                        break;
                  }
               }
               
               lock (_enclosingInstance)
               {

                  if (index >= _enclosingInstance._colors.Count)
                     _enclosingInstance._colors.SetSize(index + 1);
                  _enclosingInstance._colors[index] = colorStr;
               }
            }
            else if (elementName.Equals(XMLConstants.MG_TAG_COLORTABLE))
            {
               IEnumerator enumerator = attributes.GetEnumerator();
               while (enumerator.MoveNext())
               {
                  String attr = (String)enumerator.Current;
                  switch (attr)
                  {
                     case XMLConstants.MG_TAG_COLORTABLE_ID:
                        _enclosingInstance.ID = Int32.Parse(attributes[attr]);
                        break;
                  }
               }
            }
            // It should come in this function only for ColorTableTag ...For other tags we should write error to log
            else
               Events.WriteExceptionToLog("There is no such tag in ColorTable.endElement(): " + elementName);
         }
      }

      #endregion
   }
}
