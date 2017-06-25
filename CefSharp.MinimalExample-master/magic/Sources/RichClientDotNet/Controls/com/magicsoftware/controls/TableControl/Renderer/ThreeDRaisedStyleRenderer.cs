using com.magicsoftware.controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Controls.com.magicsoftware.controls.Renderer
{
   /// <summary>
   /// class for rendering threeD- raised table
   /// </summary>
   internal class ThreeDRaisedStyleRenderer : TableStyleRendererBase
   {
      public static Pen HighLightPen = PensCache.GetInstance().Get(SystemColors.ButtonHighlight);
      public static Pen ShadowPen = PensCache.GetInstance().Get(SystemColors.ButtonShadow);
      public static Pen BackGroundPen = PensCache.GetInstance().Get(SystemColors.ButtonFace);

      public ThreeDRaisedStyleRenderer(TableControl tableControl)
         : base(tableControl)
      {
         ColumnDividerRenderer = new ThreeDRaisedStyleColumnDividerRenderer();
         RowDividerRenderer = new ThreeDRaiseStyleRowDividerRenderer();
         BorderRenderer = new ThreeDRaisedBorderRenderer();
      }

      /// <summary>
      ///  get border size
      /// </summary>
      /// <param name="tableControl"></param>
      /// <returns></returns>
      internal override int GetBorderSize()
      {
         return SystemInformation.Border3DSize.Height; // 3-d border height
      }

      internal override Color GetCellColor(int row, int colIndex)
      {
         return SystemColors.ButtonFace; // back color is ButtonFace for 3-d raised table
      }

      internal override void PaintDividers(Header header, Graphics g, Pen pen)
      {
         // For 3d-raised style , we have dividers in 2 colors . 
         // So when we first paint column and then paint line we see partitions at the point of intersections .
         // Hence reverse the order of painting , first paint row dividers and then paint column dividers.

         if (tableControl.ShowLineDividers)
            DrawLineDividers(header, g, pen);
         if (tableControl.ShowColumnDividers)
            DrawColumnDividers(header, g, pen);
      }

      internal override int GetColumnWidthFromSectionWidth(int i, Header _header)
      {
         if (i == 0)
            return _header.Sections[i].Width - 1;
         else
            return base.GetColumnWidthFromSectionWidth(i, _header);
      }

      /// <summary>
      /// set section width 
      /// </summary>
      /// <param name="headerSection"></param>
      /// <param name="index"></param>
      /// <param name="width"></param>
      public override int GetHeaderSectionWidthFromColumnWidth(int index, int width)
      {
         if (index == 0)
            return width + 1;
         else
            return base.GetHeaderSectionWidthFromColumnWidth(index, width);
      }

      protected override void AdjustColumnWidth(int col, ref Rectangle rect)
      {
         if (tableControl.RightToLeftLayout)
         {
            rect.X -= TableControl.Factor;  // InflateRect (&Rect, -factor, -factor); after painting first border
            rect.X -= TableControl.Factor;  // InflateRect (&Rect, -factor, -factor); after painting second border

            //   if(Ctrl->Style & CTRL_STYLE_HEBREW)
            //Tmp = MAX(cRect.left + factor, Tmp);
            if (col == TableControl.TAIL_COLUMN_IDX)
               rect.Width = Math.Max(GetClientRectangle().Right - rect.X , rect.Width);
         }
      }
   }
}