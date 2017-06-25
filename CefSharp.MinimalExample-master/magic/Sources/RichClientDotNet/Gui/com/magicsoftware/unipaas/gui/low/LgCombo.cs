using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using com.magicsoftware.controls;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;

#if PocketPC
using PointF = com.magicsoftware.mobilestubs.PointF;
#endif
namespace com.magicsoftware.unipaas.gui.low
{
   internal class LgCombo : LogicalControl
   {
      private int _selectionIdx;
      private int _visibleItemsCount;
      private String[] _itemList;
      private bool _refreshItemList; //if true - refresh items list in editor

      //Returns the non-client height of a ComboBox control.
      static private int ncHeight = -1;
      private int NCHeight
      {
         get
         {
            if (ncHeight == -1)
            {
               //Identify the NCHeight of a ComboBox by creating a temporary control.
               MgComboBox comboBox = new MgComboBox();
               comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

               Form form = GuiUtilsBase.FindForm(_containerControl);
               PointF fontMetrics = Utils.GetFontMetricsByFont(form, comboBox.Font);
               ncHeight = (int)(comboBox.Height - fontMetrics.Y);
            }

            return ncHeight;
         }
      }

      /// <summary> set font </summary>
      internal override Font Font
      {
         get { return base.Font; }
         set
         {
            base.Font = value;

            if (Style == ControlStyle.Windows)
            {
               //Height of the actual ComboBox control depends on the font.
               //So, whenever the font is set, calculate the new height of the logical control as well.
               Form form = GuiUtilsBase.FindForm(_containerControl);
               PointF fontMetrics = Utils.GetFontMetricsByFont(form, value);
               Height = (int)fontMetrics.Y + NCHeight;
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="containerControl"></param>
      internal LgCombo(GuiMgControl guiMgControl, Control containerControl) :
         base(guiMgControl, containerControl)
      {
         _selectionIdx = -1;
         _visibleItemsCount = -1;
      }

      /// <summary> set refreshItemList</summary>
      /// <param name="val"></param>
      internal void setRefreshItemList(bool val)
      {
         _refreshItemList = val;
      }

      /// <summary>
      /// </summary>
      /// <param name="g"></param>
      /// <param name="rect"></param>
      /// <param name="fgColor"></param>
      internal override void paintForeground(Graphics g, Rectangle rect,  Color fgColor)
      {
         String str = null;
         if (_selectionIdx >= 0 && _itemList != null)
         {
            Debug.Assert(_itemList.Length > _selectionIdx);
            str = _itemList[_selectionIdx];
         }
         
         if (!String.IsNullOrEmpty(str))
            printText(g, rect, fgColor, str);
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal override string getSpecificControlValue()
      {
         return _selectionIdx.ToString();
      }

      /// <summary> 
      /// set selection Idx
      /// </summary>
      /// <param name="selectionIdx"></param>
      internal void setSelectionIdx(int selectionIdx)
      {
         _selectionIdx = selectionIdx;
         Refresh(true);
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal int getSelectionIdx()
      {
         return _selectionIdx;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="visibleItemsCount"></param>
      internal void setVisibleItemsCount(int visibleItemsCount)
      {
         _visibleItemsCount = visibleItemsCount;
         Refresh(true);
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal int getVisibleItemsCount()
      {
         return _visibleItemsCount;
      }

      /// <summary>
      /// </summary>
      /// <param name="itemList"></param>
      internal void setItemList(String[] itemList)
      {
         _itemList = itemList;
         _refreshItemList = true;
         Refresh(true);
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal String[] getItemList()
      {
         return _itemList;
      }

      /// <summary>
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlProperties(Control control)
      {
         MgComboBox combo = ((MgComboBox)control);
         if (_refreshItemList)
         {
            // Defect# 130023: Initially when itemList is attempted to be set for table header controls, itemList is not
            // initialized; when called while updating control visibility. Hence, it internally throws exception, which 
            // results in this problem. So check for null.
            if (_itemList != null)
               GuiUtils.setItemsList(control, _itemList);
            _refreshItemList = false;
         }

         GuiUtils.setSelect(combo, _selectionIdx);
         // The visible ItemCount need to update only if the user was update it.
         // otherwise the Visible line will be 0 and no line will be show
         if (_visibleItemsCount > -1)
            GuiUtils.setVisibleLines(combo, _visibleItemsCount, Font);

         ControlUtils.SetStyle3D(control, Style);
      }
   }
}
