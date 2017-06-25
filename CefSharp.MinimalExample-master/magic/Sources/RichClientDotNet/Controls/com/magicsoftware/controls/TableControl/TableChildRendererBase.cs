using System.Drawing;
using com.magicsoftware.util;
using com.magicsoftware.controls.utils;
using Controls.com.magicsoftware;
using LgList = System.Collections.Generic.List<Controls.com.magicsoftware.PlacementDrivenLogicalControl>;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Renderer for Table children
   /// </summary>
   public class TableChildRendererBase
   {
      protected readonly TableControl _tableControl;
      Color _hightlightBgColor = SystemColors.Highlight;
      Color _hightlightFgColor = SystemColors.HighlightText;

      /// <summary>
      ///  background color, used for highlighting
      /// </summary>
      public Color HightlightBgColor
      {
         get
         {
            return ShouldUseActiveColor ? _hightlightBgColor : InactiveHightlightBgColor;
         }
         set
         {
            _hightlightBgColor = value;

         }
      }
      /// <summary>
      /// foreground color, used for highlighting
      /// </summary>
      public Color HightlightFgColor
      {
         get
         {
            return ShouldUseActiveColor ? _hightlightFgColor : InactiveHightlightFgColor;
         }
         set
         {
            _hightlightFgColor = value;

         }
      }

      /// <summary>
      /// background color, used for highlighting, when subform is not active
      /// </summary>
      public Color InactiveHightlightBgColor { get; set; } = Color.Empty;// background color, used for highlighting inactive row
      /// <summary>
      /// foreground color, used for highlighting, when subform is not active
      /// </summary>
      public Color InactiveHightlightFgColor { get; set; } = Color.Empty;// foreground color, used for highlighting inactive row
      /// <summary>
      /// true when subform is active
      /// </summary>
      public bool IsActive { get; set; } = true; //is table active ( on the active task)

      public Color DisabledTextColor { get; set; }// foreground core for disabled text
      RowHighlightType rowHighlitingStyle;

      public virtual RowHighlightType RowHighlitingStyle
      {
         get { return rowHighlitingStyle; }
         set { rowHighlitingStyle = value; }
      }

      public TableChildRendererBase(TableControl tableControl)
      {
         _tableControl = tableControl;
         DisabledTextColor = SystemColors.GrayText;
         
         //row highlighting type default
         RowHighlitingStyle = RowHighlightType.BackgroundControls;
      }


      public virtual Rectangle GetCellRect(PlacementDrivenLogicalControl lg)
      {
         return new Rectangle();
      }

      /// <summary>
      /// Paints logical control
      /// </summary>
      /// <param name="lg"></param>
      /// <param name="cellRect"></param>
      /// <param name="g"></param>
      /// <param name="isRowMarked"></param>
      /// <param name="isSelected"></param>
      /// <param name="columnsManager"></param>
      public virtual void PaintControl(PlacementDrivenLogicalControl lg, Rectangle cellRect, Graphics g, bool isRowMarked, bool isSelected, ColumnsManager columnsManager)
      {

      }

      /// <summary> compute control background color
      /// </summary>
      /// <param name="child">table child</param>
      /// <param name="control">editor control, can be null for painting control without editor</param>
      /// <param name="isRowMarked">is row marked</param>
      /// <param name="recievingFocus">TODO</param>
      /// <param name="guiRow">gui row number</param>
      /// <returns></returns>
      public Color computeControlBgColor(PlacementDrivenLogicalControl child, ColumnsManager columnsManager, Control control, bool isRowMarked, bool isFocusedControl,
                                           int guiRow, bool ownerDraw, out bool keepColor)
      {
         Color bgColor;
         keepColor = false;

         //QCR #320284. PushButton is a special case. For all other controls, alternate and 
         //highlight (if RowHighlightStyle=BG & Controls) has precedence over control's 
         //own color. But for PushButtons, it is the other way round.
         if (IsButtonControl(child))
            return GetBackgroundColor(child, ownerDraw);

         if (isRowMarked)
         {
            if (RowHighlitingStyle == RowHighlightType.BackgroundControls)
            {
               if (control != null)
               {
                  // if the control is in the selected row
                  // check if controls is in focus, we do not use display.getfocusedContol, cause sometimes
                  // it takes time for display.getfocusedContol() to be set and wrong results are recieved
                  //bool isFocusedControl = false;// TODO: ((getFocusedControl(table.getDisplay()) == control) || recievingFocus);
                  // if control is modifable and is focused take the BG color of the control
                  if ((IsControlModifiable(child) && isFocusedControl) || IsComboControl(child))
                  {
                     bgColor = GetBackgroundColor(child, ownerDraw);
                     keepColor = true;
                  }
                  else
                     bgColor = HightlightBgColor;
               }
               else
                  bgColor = HightlightBgColor;
            }
            else if (_tableControl.HasAlternateColor && !(IsControlModifiable(child) && isFocusedControl))
            {
               bgColor = _tableControl.GetColorbyRow(guiRow);
            }
            else
            {
               bgColor = GetBackgroundColor(child, ownerDraw);
               keepColor = true;
            }
         }
         else if (_tableControl.HasAlternateColor)
         {
            bgColor = _tableControl.GetColorbyRow(guiRow);
         }
         else
         {
            bgColor = GetBackgroundColor(child, ownerDraw);
            if (_tableControl.ColorBy == TableColorBy.Column && bgColor == null)
               //transparent control, should have column color
               bgColor = GetColumnBackgroundColor(child, columnsManager);
            else
               keepColor = true;
         }
         return bgColor;
      }


      /// <summary>
      /// returns true  - if we should usual highlight row color 
      ///         false - if we should paint with inactive row highlightt color
      /// </summary>
      public bool ShouldUseActiveColor
      {
         get { return IsActive || InactiveHightlightBgColor == Color.Empty; }
      }

      /// <summary>
      /// calculate foreground color
      /// </summary>
      /// <param name="isSelected"></param><param name="lg"></param>
      /// <returns></returns>
      protected Color computeControlFgColor(bool isSelected, bool isRowMarked, PlacementDrivenLogicalControl lg)
      {
         Color fgColor;
         if (!IsControlEnable(lg))
            fgColor = DisabledTextColor;
         else if ((isSelected || isRowMarked )&& RowHighlitingStyle == RowHighlightType.BackgroundControls)
         {
            if (isRowMarked)
               fgColor = HightlightFgColor;
            else
               fgColor = GetForegroundColor(lg);
         }
        
         else
            fgColor = GetForegroundColor(lg);
         return fgColor;
      }

      #region abstract methods

      protected virtual bool IsButtonControl(PlacementDrivenLogicalControl logicalControl) { return false; }
      protected virtual bool IsComboControl(PlacementDrivenLogicalControl logicalControl) { return false; }

      protected virtual Color GetBackgroundColor(PlacementDrivenLogicalControl logicalControl, bool ownerDraw) { return Color.Empty; }
      protected virtual Color GetForegroundColor(PlacementDrivenLogicalControl logicalControl) { return Color.Empty; }

      protected virtual bool IsControlModifiable(PlacementDrivenLogicalControl logicalControl) { return true; }
      protected virtual bool IsControlEnable(PlacementDrivenLogicalControl logicalControl) { return true; }

      protected virtual Color GetColumnBackgroundColor(PlacementDrivenLogicalControl logicalControl, ColumnsManager columnsManager) { return Color.Empty; }

      #endregion

   }
}
