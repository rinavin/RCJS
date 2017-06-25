/*
 * This code is provided under the Code Project Open Licence (CPOL)
 * See http://www.codeproject.com/info/cpol10.aspx for details
 * http://www.codeproject.com/Articles/91387/Painting-Your-Own-Tabs-Second-Edition
 */

using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Provides the tabRect and paints the border for tab with default style(similar to system rendered tab)
   /// </summary>
   internal class TabStyleDefaultProvider : TabStyleProvider
   {
      internal TabStyleDefaultProvider(MgTabControl tabControl)
         : base(tabControl)
      {
      }

      /// <summary>
      /// Paints the border for tabs
      /// </summary>
      /// <param name="path"></param>
      /// <param name="tabBounds"></param>
      internal override void AddTabBorder(System.Drawing.Drawing2D.GraphicsPath path, System.Drawing.Rectangle tabBounds)
      {
         switch (MgTabControlRenderer.GetRealTabAlignment(tabControl))
         {
            case TabAlignment.Top:
               path.AddLine(tabBounds.X, tabBounds.Bottom, tabBounds.X, tabBounds.Y);
               path.AddLine(tabBounds.X, tabBounds.Y, tabBounds.Right, tabBounds.Y);
               path.AddLine(tabBounds.Right, tabBounds.Y, tabBounds.Right, tabBounds.Bottom);
               break;

            case TabAlignment.Bottom:
               path.AddLine(tabBounds.Right, tabBounds.Y, tabBounds.Right, tabBounds.Bottom);
               path.AddLine(tabBounds.Right, tabBounds.Bottom, tabBounds.X, tabBounds.Bottom);
               path.AddLine(tabBounds.X, tabBounds.Bottom, tabBounds.X, tabBounds.Y);
               break;

            case TabAlignment.Left:
               path.AddLine(tabBounds.Right, tabBounds.Bottom, tabBounds.X, tabBounds.Bottom);
               path.AddLine(tabBounds.X, tabBounds.Bottom, tabBounds.X, tabBounds.Y);
               path.AddLine(tabBounds.X, tabBounds.Y, tabBounds.Right, tabBounds.Y);
               break;

            case TabAlignment.Right:
               path.AddLine(tabBounds.X, tabBounds.Y, tabBounds.Right, tabBounds.Y);
               path.AddLine(tabBounds.Right, tabBounds.Y, tabBounds.Right, tabBounds.Bottom);
               path.AddLine(tabBounds.Right, tabBounds.Bottom, tabBounds.X, tabBounds.Bottom);
               break;
         }
      }

      /// <summary>
      /// Return the tab rect for selected and non selected tabs
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      internal override Rectangle GetTabRect(int index)
      {
         if (index < 0)
            return new Rectangle();

         Rectangle tabBounds = base.GetTabRect(index);

         if (index == this.tabControl.SelectedIndex)
         {
            if (this.tabControl.Alignment <= TabAlignment.Bottom)
            {
               // Increase the width of selected tab
               tabBounds.X -= 2;
               tabBounds.Width += 3;

               if (this.tabControl.Alignment == TabAlignment.Top)
                  tabBounds.Inflate(0, 2); // Shift tab upwards
            }
            else  // left and right
            {
               // increase the height of tab
               tabBounds.Y -= 2;
               tabBounds.Height += 3;

               // when tab is aligned left shift tab towards left
               if (MgTabControlRenderer.GetRealTabAlignment(tabControl) == TabAlignment.Left)
                  tabBounds.Inflate(2, 0);
            }
         }
         else // non selected tabs
         {
            // When tab is bottom aligned decrease the top of non selected tabs.
            if (tabControl.Alignment == TabAlignment.Bottom)
               tabBounds.Y -= 2;

            // When tab are aligned to right , decrease the width of non selected tabs
            else if (MgTabControlRenderer.GetRealTabAlignment(tabControl) == TabAlignment.Right)
            {
               tabBounds.X -= 1;
               tabBounds.Width -= 1;
            }
         }

         return tabBounds;
      }
   }
}