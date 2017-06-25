using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.Drawing;
using com.magicsoftware.util;
using Controls.com.magicsoftware;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// Renderer for Table children
   /// </summary>
   internal class TableChildRenderer : TableChildRendererBase
   {
      public delegate int GetGuiRowIndex(int mgIndex);
      public event GetGuiRowIndex GuiRowIndex;


      public delegate bool IsInMultimarkDelegate();
      /// <summary>
      /// returns true if table is in MM state
      /// </summary>
      public event IsInMultimarkDelegate IsInMultimark;

      public TableChildRenderer(TableControl tableControl)
         : base(tableControl)
      {
       

      }

      public bool IsInMultimarkState()
      {
         if (IsInMultimark != null)
            return IsInMultimark();
         return false;
      }



      /// <summary>
      /// row highliting style
      /// </summary>
      public override RowHighlightType RowHighlitingStyle
      {
         get
         {
            if (IsInMultimarkState())
               return RowHighlightType.BackgroundControls;
            return base.RowHighlitingStyle;
         }
         set
         {
            base.RowHighlitingStyle = value;
         }
      }
      public int OnGetGuiRowIndex(int mgIndex)
      {
         if (GuiRowIndex != null)
            return GuiRowIndex(mgIndex);

         return 0;
      }

      public override void PaintControl(PlacementDrivenLogicalControl lg, Rectangle cellRect, Graphics g, bool isRowMarked, bool isSelected, ColumnsManager columnsManager)
      {
         base.PaintControl(lg, cellRect, g, isRowMarked, isSelected, columnsManager);

         LogicalControl logicalControl = (LogicalControl)lg;
         // paint controls
         if (GuiUtilsBase.isOwnerDrawControl(logicalControl.GuiMgControl))
         {
            var tableCoordinator = (TableCoordinator)lg.Coordinator;

            ILogicalColumn lgColumn = columnsManager.getLgColumnByMagicIdx(lg._mgColumn);
            if (lg.Visible && lgColumn.Visible && tableCoordinator.getEditorControl() == null)
            {
               bool keepColor;
               //calculate background color
               Color controlBgColor = computeControlBgColor((LogicalControl)lg, columnsManager, null, isRowMarked, false,
                  OnGetGuiRowIndex(lg._mgRow), true, out keepColor);

               Rectangle displayRect = tableCoordinator.getDisplayRect(cellRect, false);

               //calculate foreground color
               Color fgColor = computeControlFgColor(isSelected, isRowMarked, (LogicalControl)lg);

               //paint the control
               logicalControl.paint(g, displayRect, controlBgColor, fgColor, keepColor);
            }
         }

      }

      /// <summary>
      /// Gets logical control cell rect in table
      /// </summary>
      /// <param name="lg"></param>
      /// <returns></returns>
      public override Rectangle GetCellRect(PlacementDrivenLogicalControl lg)
      {
         LogicalControl logicalControl = (LogicalControl)lg;
         TableCoordinator tableCoordinator = (TableCoordinator)lg.Coordinator;

         return tableCoordinator.GetCellRect(lg._mgColumn, logicalControl._mgRow);
      }

      protected override bool IsButtonControl(PlacementDrivenLogicalControl logicalControl)
      {
         return ((LogicalControl)logicalControl).GuiMgControl.IsButtonPushButton();
      }

      protected override bool IsComboControl(PlacementDrivenLogicalControl logicalControl)
      {
         return ((LogicalControl)logicalControl).GuiMgControl.Type == MgControlType.CTRL_TYPE_COMBO;
      }

      protected override Color GetBackgroundColor(PlacementDrivenLogicalControl logicalControl, bool ownerDraw)
      {
         return ((LogicalControl)logicalControl).getBackgroundColor(ownerDraw);
      }

      protected override Color GetForegroundColor(PlacementDrivenLogicalControl logicalControl)
      {
         return ((LogicalControl)logicalControl).FgColor;
      }

      protected override bool IsControlModifiable(PlacementDrivenLogicalControl logicalControl)
      {
         return ((LogicalControl)logicalControl).Modifable;
      }

      protected override bool IsControlEnable(PlacementDrivenLogicalControl logicalControl)
      {
         return ((LogicalControl)logicalControl).Enabled;
      }

      protected override Color GetColumnBackgroundColor(PlacementDrivenLogicalControl logicalControl, ColumnsManager columnsManager)
      {
         return ((LgColumn)columnsManager.getLgColumnByMagicIdx(((TableCoordinator)logicalControl.Coordinator).MgColumn)).BgColor;
      }
   }
}
