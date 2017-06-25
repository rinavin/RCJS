/*
 * This code is provided under the Code Project Open Licence (CPOL)
 * See http://www.codeproject.com/info/cpol10.aspx for details
 * The code is taken from : http://www.codeproject.com/Articles/91387/Painting-Your-Own-Tabs-Second-Edition
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using com.magicsoftware.controls.utils;
using Controls.com.magicsoftware.support;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// render the tab control
   /// </summary>
   internal class MgTabControlRenderer
   {
      internal static void CustomPaint(MgTabControl tabControl, Graphics screenGraphics)
      {
         //	We render into a bitmap that is then drawn in one shot rather than using
         //	double buffering built into the control as the built in buffering
         // 	messes up the background painting.
         //	Equally the .Net 2.0 BufferedGraphics object causes the background painting
         //	to mess up, which is why we use this .Net 1.1 buffering technique.

         //	Buffer code from Gil. Schmidt http://www.codeproject.com/KB/graphics/DoubleBuffering.aspx

         if (tabControl.Width > 0 && tabControl.Height > 0)
         {
            tabControl.PaintTransparentBackground(screenGraphics);

            if (tabControl.TabCount > 0)
            {
               //	Draw each tabpage from right to left.  We do it this way to handle
               //	the overlap correctly.

               for (int row = 0; row < tabControl.RowCount; row++)
               {
                  for (int index = tabControl.TabCount - 1; index >= 0; index--)
                  {
                     if (index != tabControl.SelectedIndex && GetTabRow(tabControl, index) == row)
                        DrawTabPage(tabControl, screenGraphics, index);
                  }
               }

               //	The selected tab must be drawn last so it appears on top.
               if (tabControl.SelectedIndex > -1)
                  DrawTabPage(tabControl, screenGraphics, tabControl.SelectedIndex);
            }
            else
            {
               // When there are no tabs paint only the border.
               Rectangle tabBorderRect = tabControl.ClientRectangle;
               tabBorderRect.Inflate(-2, -2);
               screenGraphics.DrawRectangle(new Pen(SolidBrushCache.GetInstance().Get(SystemColors.ControlDark)), tabBorderRect);
            }
         }
      }

      private static void DrawTabPage(MgTabControl tabControl, Graphics graphics, int index)
      {

         graphics.SmoothingMode = SmoothingMode.HighSpeed;

         //if the scroller is visible and the tab is fully placed under it, we don't need to draw such tab 
         // in this case we should only paint the border for selected tab 
         if (!tabControl.TabUpDownRect.IsEmpty && tabControl.GetTabRect(index).X >= tabControl.TabUpDownRect.X)
         {
            if (index == tabControl.SelectedIndex)
            {
               using (GraphicsPath tabBorderPath = GetTabPageBorder(tabControl, index))
               {
                  //	Paint the border for selected tab even if it is not visible
                  DrawTabBorder(tabControl, graphics, tabBorderPath, index);
               }
            }
            return;
         }

         //	Get TabPageBorder
         using (GraphicsPath tabBorderPath = GetTabPageBorder(tabControl, index))
         {
            //	Paint the tab background
            FillTab(tabControl, graphics, tabBorderPath, index);

            using (GraphicsPath tabRectPath = tabControl.DisplayStyleProvider.GetTabBorder(index))
            {
               Region originalClipRegion = graphics.Clip;

               // Set clip so that image is not painted outside tab bounds
               graphics.SetClip(tabControl.DisplayStyleProvider.GetTabRect(index));

               //	Draw any image
               DrawTabImage(tabControl, graphics, tabRectPath, index);

               //	Draw the text
               DrawTabText(tabControl, graphics, tabRectPath, index);

               // restore original clip
               graphics.SetClip(originalClipRegion, CombineMode.Replace);
            }

            //	Paint the border
            DrawTabBorder(tabControl, graphics, tabBorderPath, index);

            //	Paint a focus indication
            tabControl.DisplayStyleProvider.DrawTabFocusIndicator(index, graphics);
         }
      }

      /// <summary>
      /// Fill the background of Tab
      /// </summary>
      /// <param name="index"></param>
      /// <param name="graphics"></param>
      /// <param name="path"></param>
      private static void FillTab(MgTabControl tabControl, Graphics graphics, GraphicsPath path, int index)
      {
         Brush fillBrush = GetTabBackgroundBrush(tabControl, index);
         //	Paint the background
         graphics.FillPath(fillBrush, path);

         // Fill the selected tab with selected tab color
         if (tabControl.SelectedTabColor != Color.Empty && tabControl.SelectedIndex == index)
            graphics.FillRectangle(SolidBrushCache.GetInstance().Get(tabControl.SelectedTabColor), tabControl.DisplayStyleProvider.GetTabRect(index));
      }

      /// <summary>
      /// get the brush for filling background
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      private static Brush GetTabBackgroundBrush(MgTabControl tabControl, int index)
      {
         Color tabBackGroundColor;

         if (tabControl.SelectedIndex == index)
            tabBackGroundColor = tabControl.PageColor;
         else if (index == tabControl.ActiveIndex && tabControl.HotTrackColor != Color.Empty)
            tabBackGroundColor = tabControl.HotTrackColor; // Change the background of hot tracked tab for XP theme
         else
            tabBackGroundColor = tabControl.TitleColor;

         return SolidBrushCache.GetInstance().Get(tabBackGroundColor);
      }

      private static void DrawTabBorder(MgTabControl tabControl, Graphics graphics, GraphicsPath path, int index)
      {
         graphics.SmoothingMode = SmoothingMode.HighQuality;
         Color borderColor = SystemColors.ControlDark;

         // For hottracked item , border color must be the FG Color
         if (index == tabControl.ActiveIndex && index != tabControl.SelectedIndex && tabControl.HotTrackFgColor != Color.Empty)
            borderColor = tabControl.HotTrackFgColor;

         Pen borderPen = PensCache.GetInstance().Get(borderColor);
         graphics.DrawPath(borderPen, path);
      }

      private static void DrawTabText(MgTabControl tabControl, Graphics graphics, GraphicsPath tabPath, int index)
      {
        
         Rectangle tabBounds = GetTabTextRect(tabControl, tabPath,  index);

         Color textColor;

         // For selected tab 
         // When 'SelectedTabColor' is set TextColor is SelectedTabFgColor else it is Tab's ForeColor
         if (tabControl.SelectedIndex == index)
            textColor = tabControl.SelectedTabFgColor != Color.Empty ? tabControl.SelectedTabFgColor : tabControl.ForeColor;
         else
            textColor=  tabControl.TitleFgColor; // Text color it is Foreground of Title Color for non selected tabs


         int orientation = GetOrientation(tabControl);

         if (tabControl.RightToLeftLayout)
         {
            Rectangle clipRect = tabControl.GetTabRect(index);

            clipRect.X = tabControl.Width - clipRect.Right;

            // Set clip so that image is not painted outside tab bounds
            graphics.SetClip(clipRect);
         }

         FontDescription font = new FontDescription(tabControl.Font);
         ControlRenderer.PrintText(graphics, tabBounds, textColor, font, GetTabText(tabControl, orientation, index), false, ContentAlignment.MiddleCenter,
                           true, false, false, false, (tabControl.RightToLeft == RightToLeft.Yes), false, orientation, true);

      }

      private static void DrawTabImage(MgTabControl tabControl, Graphics graphics, GraphicsPath tabPath, int index)
      {
         Image tabImage = null;
         if (tabControl.TabPages[index].ImageIndex > -1 && tabControl.ImageList != null && tabControl.ImageList.Images.Count > tabControl.TabPages[index].ImageIndex)
         {
            tabImage = tabControl.ImageList.Images[tabControl.TabPages[index].ImageIndex];
         }

         if (tabImage != null)
         {
        
            Rectangle imageRect = GetTabImageRect(tabControl, tabPath, index);
            graphics.DrawImage(tabImage, imageRect);

         }
      }

      #region Tab borders and bounds properties

      private static GraphicsPath GetTabPageBorder(MgTabControl tabControl, int index)
      {
         GraphicsPath path = new GraphicsPath();
         Rectangle tabBounds = tabControl.DisplayStyleProvider.GetTabRect(index);
         tabControl.DisplayStyleProvider.AddTabBorder(path, tabBounds);

         if (tabControl.SelectedIndex == index)
         {
            Rectangle pageBounds = GetPageBounds(tabControl, index);
            AddPageBorder(path, pageBounds, tabBounds, tabControl);
         }

         path.CloseFigure();
         return path;
      }

      private static Rectangle GetPageBounds(MgTabControl tabControl, int index)
      {
         Rectangle pageBounds = tabControl.TabPages[index].Bounds;
         if (tabControl.Alignment <= TabAlignment.Bottom)
         {
            // Ideally the page bounds should be inflated by 4. But it does not work correctly .
            // In case of Top :  Y is increased by 2 , Bottom by 4  (so height increases by 6 )
            // In case of Bottom :  Y is increased by 4 , Bottom by 2 (so height increases by 6)

            pageBounds.Inflate(4, 0); // Adjust the width first

            if (tabControl.Alignment == TabAlignment.Top)
               pageBounds.Y -= 2;
            else //if (this.Alignment == TabAlignment.Bottom)
               pageBounds.Y -= 4;

            pageBounds.Height += (2 + 4); // Adjust the height,
         }
         else // for left and right
         {
            // Ideally the page bounds should be inflated by 4. But it does not work correctly .
            // In case of Left :  X is increased by 2 , Right by 4  (so width increases by 6 )
            // In case of Right : X  is increased by 4 , Right by 2 (so width increases by 6)

            pageBounds.Inflate(0, 4);

            bool tabsOnLeft = (tabControl.Alignment == TabAlignment.Left && !tabControl.RightToLeftLayout) ||
                              (tabControl.Alignment == TabAlignment.Right && tabControl.RightToLeftLayout);

            if (tabsOnLeft)
               pageBounds.X -= 2;
            else // Right
               pageBounds.X -= 4;

            pageBounds.Width += (2 + 4);

            if (tabControl.RightToLeftLayout)
            {
               if (tabControl.Alignment == TabAlignment.Left)
                  pageBounds.X = 0;  // page is aligned to left
               else
                  pageBounds.X = tabControl.Width - pageBounds.Width; // page is aligned to right
            }
         }

         // Since we cannot paint on controls bound reduce the width and height by 1 pixel
         pageBounds.Width -= 1;
         pageBounds.Height -= 1;

         return pageBounds;
      }

      /// <summary>
      /// Get the tab page text at specified index.
      /// </summary>
      /// <param name="tabControl"></param>
      /// <param name="orientation"></param>
      /// <param name="index"></param>
      /// <returns></returns>
      private static string GetTabText(MgTabControl tabControl, int orientation, int index)
      {
         if (orientation == 0)
            return ((MgTabPage)tabControl.TabPages[index]).TextWithAccelerator;

         return tabControl.TabPages[index].Text;
      }

      private static Rectangle GetTabTextRect(MgTabControl tabControl, GraphicsPath tabPath, int index)
      {
         TabPage tabPage = tabControl.TabPages[index];
         Rectangle textRect = new Rectangle();

         RectangleF tabBounds = tabPath.GetBounds();
         Size textExt = Utils.GetTextExt(tabControl.Font, tabPage.Text, tabPage);

         //TODO : image file does not exist
         if (tabControl.Alignment <= TabAlignment.Bottom) // Top and Bottom
            textRect = new Rectangle(0, 0, textExt.Width + (2 * tabControl.Padding.X), (int)tabBounds.Height);
         else
         {
            if (tabControl.SizeMode == TabSizeMode.Fixed && !(tabControl.FontOrientation == 900 || tabControl.FontOrientation == 2700))
            {
               // For right and left tab when tab size is Fixed , text is rendered in center , hence textRect is same as TabRect
               // textRect = new Rectangle((int)tabBounds.X, (int)tabBounds.Y, Math.Min((int)tabBounds.Height, textExt.Height), Math.Min((int)tabBounds.Width, textExt.Width));
               textRect = new Rectangle((int)tabBounds.X, (int)tabBounds.Y, (int)tabBounds.Width, (int)tabBounds.Height);
            }
            else
               textRect = new Rectangle(0, 0, (int)tabBounds.Height, (textExt.Width + (2 * tabControl.Padding.X)));  // Since the text is rotated in case of left and right, we treat the textExt.Width as text's Height and vice versa.
         }

         int leftMargin = ((int)tabBounds.Width - textRect.Width) / 2;
         int topMargin = ((int)tabBounds.Height - textRect.Height) / 2;

         if (tabControl.SizeMode == TabSizeMode.Normal && tabPage.ImageIndex == -1) // When Size mode is 'Fit to text' and there is no image, text is aligned to left
         {
            if (tabControl.Alignment <= TabAlignment.Bottom) // Top and Bottom
               leftMargin = 0;
            else
               topMargin = 0;
         }

         textRect.X = (int)tabBounds.X + leftMargin; //Align text to left

         textRect.Y = (int)tabBounds.Y + topMargin; // text must be centered vertically

         //	If there is an image allow for it
         if (tabControl.TabPages[index].ImageIndex > -1 && tabControl.ImageList != null)
         {
            Rectangle imageRect = GetTabImageRect(tabControl, tabPath, index);
            if (tabControl.Alignment <= TabAlignment.Bottom)
            {
               textRect.X = imageRect.Right; // text is on right of image
            }
            else
            {
               if (tabControl.Alignment == TabAlignment.Right)
               {
                  if (tabControl.SizeMode == TabSizeMode.Fixed && !(tabControl.FontOrientation == 900 || tabControl.FontOrientation == 2700))
                  {
                     // text should be rendered in center of the space below the image
                     textRect.Height = (int)tabBounds.Bottom - imageRect.Bottom;
                     textRect.Y = imageRect.Bottom;
                  }
                  else
                     textRect.Y = imageRect.Bottom; // text is below the image
               }
               else
               {
                  if (tabControl.SizeMode == TabSizeMode.Fixed && !(tabControl.FontOrientation == 900 || tabControl.FontOrientation == 2700))
                  {
                     // text should be rendered in center of the space above the image
                     textRect.Height = imageRect.Top - (int)tabBounds.Top;
                     textRect.Y = (int)tabBounds.Y;
                  }
                  else
                     textRect.Y = imageRect.Top - textRect.Height; // text is above the image
               }
            }

         }
         return textRect;
      }

      private static int GetTabRow(MgTabControl tabControl, int index)
      {
         //	All calculations will use this rect as the base point
         //	because the itemsize does not return the correct width.
         Rectangle rect = tabControl.GetTabRect(index);

         int row = -1;

         switch (tabControl.Alignment)
         {
            case TabAlignment.Top:
               row = (rect.Y - 2) / rect.Height;
               break;

            case TabAlignment.Bottom:
               row = ((tabControl.Height - rect.Y - 2) / rect.Height) - 1;
               break;

            case TabAlignment.Left:
               row = (rect.X - 2) / rect.Width;
               break;

            case TabAlignment.Right:
               row = ((tabControl.Width - rect.X - 2) / rect.Width) - 1;
               break;
         }
         return row;
      }

      private static void AddPageBorder(GraphicsPath path, Rectangle pageBounds, Rectangle tabBounds, MgTabControl tabControl)
      {
         switch (MgTabControlRenderer.GetRealTabAlignment(tabControl))
         {
            case TabAlignment.Top:
               path.AddLine(tabBounds.Right, pageBounds.Y, pageBounds.Right, pageBounds.Y);
               path.AddLine(pageBounds.Right, pageBounds.Y, pageBounds.Right, pageBounds.Bottom);
               path.AddLine(pageBounds.Right, pageBounds.Bottom, pageBounds.X, pageBounds.Bottom);
               path.AddLine(pageBounds.X, pageBounds.Bottom, pageBounds.X, pageBounds.Y);
               path.AddLine(pageBounds.X, pageBounds.Y, tabBounds.X, pageBounds.Y);
               break;

            case TabAlignment.Bottom:
               path.AddLine(tabBounds.X, pageBounds.Bottom, pageBounds.X, pageBounds.Bottom);
               path.AddLine(pageBounds.X, pageBounds.Bottom, pageBounds.X, pageBounds.Y);
               path.AddLine(pageBounds.X, pageBounds.Y, pageBounds.Right, pageBounds.Y);
               path.AddLine(pageBounds.Right, pageBounds.Y, pageBounds.Right, pageBounds.Bottom);
               path.AddLine(pageBounds.Right, pageBounds.Bottom, tabBounds.Right, pageBounds.Bottom);
               break;

            case TabAlignment.Left:
               path.AddLine(pageBounds.X, tabBounds.Y, pageBounds.X, pageBounds.Y);
               path.AddLine(pageBounds.X, pageBounds.Y, pageBounds.Right, pageBounds.Y);
               path.AddLine(pageBounds.Right, pageBounds.Y, pageBounds.Right, pageBounds.Bottom);
               path.AddLine(pageBounds.Right, pageBounds.Bottom, pageBounds.X, pageBounds.Bottom);
               path.AddLine(pageBounds.X, pageBounds.Bottom, pageBounds.X, tabBounds.Bottom);
               break;

            case TabAlignment.Right:
               path.AddLine(pageBounds.Right, tabBounds.Bottom, pageBounds.Right, pageBounds.Bottom);
               path.AddLine(pageBounds.Right, pageBounds.Bottom, pageBounds.X, pageBounds.Bottom);
               path.AddLine(pageBounds.X, pageBounds.Bottom, pageBounds.X, pageBounds.Y);
               path.AddLine(pageBounds.X, pageBounds.Y, pageBounds.Right, pageBounds.Y);
               path.AddLine(pageBounds.Right, pageBounds.Y, pageBounds.Right, tabBounds.Y);
               break;
         }
      }

      //private static Rectangle GetTabImageRect(MgTabControl tabControl, int index)
      //{
      //   using (GraphicsPath tabBorderPath = tabControl.DisplayStyleProvider.GetTabBorder(index))
      //   {
      //      return GetTabImageRect(tabControl, tabBorderPath, index);
      //   }
      //}

      private static Rectangle GetTabImageRect(MgTabControl tabControl, GraphicsPath tabBorderPath, int index)
      {
         TabPage tabPage = tabControl.TabPages[index];
         Size iconSize = tabControl.ImageList.ImageSize;

         RectangleF tabBounds = tabBorderPath.GetBounds();

         Size textExt = Utils.GetTextExt(tabControl.Font, tabPage.Text, tabPage);

         int widthForImageAndText = iconSize.Width + textExt.Width + tabControl.Padding.X;

         double leftmargin = 0;
         double topMargin = 0;

         Rectangle imageRect = new Rectangle(0, 0, iconSize.Width, iconSize.Height);

         if (tabControl.Alignment <= TabAlignment.Bottom)
         {
            leftmargin = (tabBounds.Width - widthForImageAndText) / 2;
            topMargin = Math.Round((double)((int)tabBounds.Height - iconSize.Height) / 2); // vertical center

            imageRect.Y = (int)tabBounds.Y + (int)topMargin; // Y is always centered

            imageRect.X = (int)tabBounds.X + (int)leftmargin; // aligned to left and centered vertically
         }
         else // left and right
         {
            leftmargin = Math.Round((double)((int)tabBounds.Width - iconSize.Width) / 2);
            topMargin = (tabBounds.Height - widthForImageAndText) / 2;  // vertical center

            imageRect.X = (int)tabBounds.X + (int)leftmargin; // X is always centered

            if (tabControl.Alignment == TabAlignment.Right)
               imageRect.Y = (int)tabBounds.Top + (int)topMargin; // aligned to Top and centered horizontally
            else
               imageRect.Y = (int)tabBounds.Bottom - ((int)Math.Round(topMargin, MidpointRounding.AwayFromZero) + iconSize.Height); // aligned to Bottom and centered horizontally
         }

         return imageRect;
      }

      #endregion Tab borders and bounds properties

      /// <summary>
      /// Get the real side of Tab
      /// </summary>
      internal static TabAlignment GetRealTabAlignment(MgTabControl tabControl)
      {
         switch (tabControl.Alignment)
         {
            case TabAlignment.Left:
               return (tabControl.RightToLeftLayout ? TabAlignment.Right : TabAlignment.Left);

            case TabAlignment.Right:
               return (tabControl.RightToLeftLayout ? TabAlignment.Left : TabAlignment.Right);

            default:
               return tabControl.Alignment;
         }
      }

      /// <summary>
      /// Return the orientation
      /// </summary>
      /// <param name="tabControl"></param>
      /// <returns></returns>
      private static int GetOrientation(MgTabControl tabControl)
      {
         int orientation = 0;

         if (tabControl.Alignment == TabAlignment.Left || tabControl.Alignment == TabAlignment.Right)
         {
            // For left and right, when tab size mode is Fixed , set the orientation of applied Font
            if (tabControl.SizeMode == TabSizeMode.Fixed)
               orientation = tabControl.FontOrientation;
            else // when size mode is not fixed orientation is 900 for left and 2700 for right
               orientation = tabControl.Alignment == TabAlignment.Left ? 900 : 2700;
         }
         // For other alignment orientation is always 0
         return orientation;
      }
   }
}
