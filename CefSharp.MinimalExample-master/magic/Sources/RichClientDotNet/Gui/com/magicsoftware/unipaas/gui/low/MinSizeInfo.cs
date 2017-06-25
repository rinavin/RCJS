using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> save information about the minimum size of object</summary>
   /// <author>  rinat</author>
   class MinSizeInfo
   {
      private readonly Hashtable _childrenMinSizeInfo; // Reference for all MinSizeInfo of the children;
      private readonly int _orientation; // the orientation of the frame set (for frame it is not relevant)
      internal int MinHeight { set; get; } // minimum Height
      internal int MinWidth { set; get; } // minimum width

      /// <param name="setOrientation"></param>
      internal MinSizeInfo(int setOrientation)
      {
         _orientation = setOrientation;
         MinHeight = 1;
         MinWidth = 1;
         _childrenMinSizeInfo = new Hashtable();
      }

      /// <summary> add minimum size info for a child.
      /// 
      /// </summary>
      /// <param name="control">
      /// </param>
      /// <param name="minSizeInfoChild">
      /// </param>
      internal void addChildMinSizeInfo(Control control, MinSizeInfo minSizeInfoChild)
      {
         if (_childrenMinSizeInfo[control] == null)
            _childrenMinSizeInfo[control] = minSizeInfoChild;
      }

      /// <summary> remove minimum size info for a child.
      /// 
      /// </summary>
      /// <param name="control">
      /// </param>
      /// <param name="minSizeInfoChild">
      /// </param>
      internal void deleteChildMinSizeInfo(Control control)
      {
         _childrenMinSizeInfo.Remove(control);
      }

      /// <summary> calculate the minimum height.</summary>
      /// <originalMinSize:>  if to return the original min size(without the decrease the Spliter width) </originalMinSize:>
      /// <returns>
      /// </returns>
      private int calcMinimumHeight(bool originalMinSize)
      {
         int retMinHeight = 0;
         IEnumerator valuesIterator = _childrenMinSizeInfo.Values.GetEnumerator();
         while (valuesIterator.MoveNext())
         {
            MinSizeInfo minSizeInfo = (MinSizeInfo)valuesIterator.Current;
            if (getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
               retMinHeight = Math.Max(minSizeInfo.calcMinimumHeight(originalMinSize), retMinHeight);
            else
               retMinHeight += minSizeInfo.calcMinimumHeight(originalMinSize);
         }

         int size = MinHeight;
         if (originalMinSize && MinHeight > 0)
         {
            Point pt = new Point(GuiConstants.DEFAULT_VALUE_INT, MinHeight);
            GuiUtils.updateFrameSize(ref pt, false);
            size = pt.Y;
         }

         retMinHeight = Math.Max(retMinHeight, size);

         return retMinHeight;
      }

      /// <summary> calculate the minimum width.</summary>
      /// <originalMinSize:>  if to return the original min size(without the decrease the Spliter width) </originalMinSize:>
      /// <returns>
      /// </returns>
      private int calcMinimumWidth(bool originalMinSize)
      {
         int retMinWidth = 0;
         IEnumerator valuesIterator = _childrenMinSizeInfo.Values.GetEnumerator();
         while (valuesIterator.MoveNext())
         {
            MinSizeInfo minSizeInfo = (MinSizeInfo)valuesIterator.Current;
            if (getOrientation() == MgSplitContainer.SPLITTER_STYLE_HORIZONTAL)
               retMinWidth += minSizeInfo.calcMinimumWidth(originalMinSize);
            else
               retMinWidth = Math.Max(minSizeInfo.calcMinimumWidth(originalMinSize), retMinWidth);
         }

         int size = MinWidth;

         if (originalMinSize && MinWidth > 0)
         {
            Point pt = new Point(MinWidth, GuiConstants.DEFAULT_VALUE_INT);
            GuiUtils.updateFrameSize(ref pt, false);
            size = pt.X;
         }

         retMinWidth = Math.Max(retMinWidth, size);

         return retMinWidth;
      }

      /// <summary> get minimum size
      /// 
      /// </summary>
      /// <returns>
      /// </returns>
      internal Point getMinSize(bool originalMinSize)
      {
         Point pt = new Point(calcMinimumWidth(originalMinSize), calcMinimumHeight(originalMinSize));
         return pt;
      }

      /// <summary> get the Orientation according to the Spliter style
      /// 
      /// </summary>
      /// <param name="SpliterStyle">
      /// </param>
      /// <returns>
      /// </returns>
      internal int getOrientation()
      {
         return _orientation;
      }
   }
}