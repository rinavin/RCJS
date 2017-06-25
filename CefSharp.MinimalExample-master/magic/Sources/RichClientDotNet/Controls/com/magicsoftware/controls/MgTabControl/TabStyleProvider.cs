/*
 * This code is provided under the Code Project Open Licence (CPOL)
 * See http://www.codeproject.com/info/cpol10.aspx for details
 * http://www.codeproject.com/Articles/91387/Painting-Your-Own-Tabs-Second-Edition
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Provides the tabRect, border to the Renderer
   /// </summary>
   internal abstract class TabStyleProvider 
   {
      #region Constructor

      protected internal TabStyleProvider(MgTabControl tabControl)
      {
         this.tabControl = tabControl;
      }

      #endregion Constructor

      #region Factory Methods

      internal static TabStyleProvider CreateProvider(MgTabControl tabControl, TabStyle tabStyle)
      {
         TabStyleProvider provider;

         //	Depending on the display style of the tabControl generate an appropriate provider.
         switch (tabStyle)
         {
            //case TabStyle.None:
            //   provider = new TabStyleNoneProvider(tabControl);
            //   break;

            case TabStyle.Default:
               provider = new TabStyleDefaultProvider(tabControl);
               break;

            //case TabStyle.Angled:
            //   provider = new TabStyleAngledProvider(tabControl);
            //   break;

            //case TabStyle.Rounded:
            //   provider = new TabStyleRoundedProvider(tabControl);
            //   break;

            //case TabStyle.VisualStudio:
            //   provider = new TabStyleVisualStudioProvider(tabControl);
            //   break;

            //case TabStyle.Chrome:
            //   provider = new TabStyleChromeProvider(tabControl);
            //   break;

            //case TabStyle.IE8:
            //   provider = new TabStyleIE8Provider(tabControl);
            //   break;

            //case TabStyle.VS2010:
            //   provider = new TabStyleVS2010Provider(tabControl);
            //   break;

            default:
               provider = new TabStyleDefaultProvider(tabControl);
               Debug.Assert(false, "Invalid tab style.");
               break;
         }

         return provider;
      }

      #endregion Factory Methods

      #region	Protected variables

      protected internal MgTabControl tabControl;

      #endregion

      #region overridable Methods

      internal abstract void AddTabBorder(GraphicsPath path, Rectangle tabBounds);

      internal virtual Rectangle GetTabRect(int index)
      {
         if (index < 0)
            return new Rectangle();

         Rectangle tabBounds = this.tabControl.GetTabRect(index);

         int TabUpDownX = tabControl.TabUpDownRect.X;

         //If the tab has part under scroller we shouldn't draw that part so reduce the tab width in this case
         if (TabUpDownX > 0 && TabUpDownX >= tabBounds.X && TabUpDownX < tabBounds.Right)
            tabBounds.Width -= tabBounds.Right - TabUpDownX;

         return tabBounds;
      }

      #endregion

      #region Painting

      internal void DrawTabFocusIndicator(int index, Graphics graphics)
      {
         if (this.tabControl.Focused && index == this.tabControl.SelectedIndex)
         {
            Rectangle focusRectangle = this.GetTabRect(index);
            focusRectangle.Inflate(-3, -3);
            focusRectangle.Width += 1;
            ControlPaint.DrawFocusRectangle(graphics, focusRectangle);
         }
      }

      #endregion

      #region Tab border and rect

      internal GraphicsPath GetTabBorder(int index)
      {
         GraphicsPath path = new GraphicsPath();
         Rectangle tabBounds = this.GetTabRect(index);

         this.AddTabBorder(path, tabBounds);

         path.CloseFigure();
         return path;
      }

      #endregion
   }
}