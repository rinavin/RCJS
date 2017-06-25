using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Render the expanders (as the expanders shown in property grid)
   /// Based on VisualStyleElement.ExplorerTreeView
   /// 
   /// </summary>
   public class ExpanderRenderer
   {
      /// <summary>
      ///  Name of the class used to draw the element 
      ///  this name is taken from class -'VisualStyleElement.ExplorerTreeView' which is used by framework to draw property grid expanders
      /// </summary>
      private static string CLASS_NAME = "Explorer::TreeView";

      /// <summary>
      ///  Part used by VisualStyleElement.ExplorerTreeView
      /// </summary>
      private static int PART = 2;

      /// <summary>
      /// Renderer for opened / expanded state
      /// </summary>
      private static VisualStyleRenderer expandedStateRenderer = null;
      private static VisualStyleRenderer ExpandedStateRenderer
      {
         get
         {
            if (expandedStateRenderer == null)
               expandedStateRenderer = new VisualStyleRenderer(CLASS_NAME, PART, 2);

            return expandedStateRenderer;
         }
      }

      /// <summary>
      /// renderer for closed / collapsed state 
      /// </summary>
      private static VisualStyleRenderer collapsedStateRenderer = null;
      private static VisualStyleRenderer CollapsedStateRenderer
      {
         get
         {
            if (collapsedStateRenderer == null)
               collapsedStateRenderer = new VisualStyleRenderer(CLASS_NAME, PART, 1);

            return collapsedStateRenderer;
         }
      }

      private static double EXPANDER_SIZE = 9.0;

      /// <summary>
      /// Render the expander
      /// </summary>
      /// <param name="graphics"></param>
      /// <param name="bounds"></param>
      /// <param name="isExpanded"></param>
      public static void RenderExpander(Graphics graphics, Rectangle bounds, bool isExpanded, Color backColor, Color foreColor)
      {
         if (Application.RenderWithVisualStyles)
         {
            if (isExpanded)
               ExpandedStateRenderer.DrawBackground(graphics, bounds);
            else
               CollapsedStateRenderer.DrawBackground(graphics, bounds);
         }
         else
         {
            // Draw expander for classic theme
            RenderExpanderWithClassicStyle(graphics, bounds, isExpanded, backColor, foreColor);
         }
      }

      /// <summary>
      /// render classic style expander (+ /- )
      /// </summary>
      /// <param name="g"></param>
      /// <param name="bounds"></param>
      /// <param name="isExpanded"></param>
      /// <param name="backColor"></param>
      /// <param name="foreColor"></param>
      public static void RenderExpanderWithClassicStyle(Graphics g, Rectangle bounds, bool isExpanded, Color backColor, Color foreColor)
      {
         Rectangle rect = new Rectangle((int) System.Math.Ceiling((bounds.Width  - EXPANDER_SIZE) /2), (int)System.Math.Ceiling((bounds.Height - EXPANDER_SIZE )/ 2) + bounds.Y, (int)EXPANDER_SIZE, (int)EXPANDER_SIZE);

         if (!rect.IsEmpty)
         { 
            Brush backgroundBrush = SolidBrushCache.GetInstance().Get(backColor);
            Pen pen = PensCache.GetInstance().Get(foreColor);

            g.FillRectangle(backgroundBrush, rect); // Fill rect
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1); // draw border
            
            int num = 2;
            // draw horizontal line
            g.DrawLine(pen, (int)(rect.X + num), (int)(rect.Y + (rect.Height / 2)), (int)(((rect.X + rect.Width) - num) - 1), (int)(rect.Y + (rect.Height / 2)));

            if (!isExpanded)
            {
               // draw vertical line
               g.DrawLine(pen, (int)(rect.X + (rect.Width / 2)), (int)(rect.Y + num), (int)(rect.X + (rect.Width / 2)), (int)(((rect.Y + rect.Height) - num) - 1));
            }
         }
      }
   }
}