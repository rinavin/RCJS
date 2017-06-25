using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Controls.com.magicsoftware
{
   /// <summary>
   /// Container containing information of logicalControls contained in it
   /// </summary>
   public class LogicalControlsContainer
   {
      readonly internal Control mainControl; // container control, can be panel of form or subform, groupbox, inner panel of tab, table or tree
      private List<PlacementDrivenLogicalControl> _logicalControls;
      /// <summary>
      /// list of logical controls on the container
      /// </summary>
      public List<PlacementDrivenLogicalControl> LogicalControls
      {
         get { return _logicalControls; }
      }

      private Point _sizeChange = new Point();

      /// <summary>
      /// when controls recieve negative location (possible during hebrew placement)
      /// we must move all the control so they will have positive locations,
      /// like it is done in online
      /// </summary>
      public int NegativeOffset { get; set; }

      /// <summary>change of size caused by placement</summary>
      public Point SizeChange
      {
         get { return _sizeChange; }
         set { _sizeChange = value; }
      }

      public LogicalControlsContainer(Control container)
      {
         mainControl = container;
      }

      public void Add(PlacementDrivenLogicalControl logicalControl)
      {
         if (_logicalControls == null)
            _logicalControls = new List<PlacementDrivenLogicalControl>();

         _logicalControls.Add(logicalControl);
      }

      /// <summary>compute logical size of container</summary>
      /// <returns></returns>
      public Size computeSize()
      {
         Size size = new Size();
         if (LogicalControls != null)
         {
            foreach (PlacementDrivenLogicalControl lg in LogicalControls)
            {
               if (lg.Visible)
               {
                  Rectangle rect = lg.getRectangle();
                  int right = (rect.Width > 0 ? rect.Right : rect.Left);
                  int bottom = (rect.Height > 0 ? rect.Bottom : rect.Top);

                  if (right > size.Width)
                     size.Width = right;

                  if (bottom > size.Height)
                     size.Height = bottom;
               }
            }
         }
         return size;
      }

      /// <summary>save change in size of container
      /// /add change to previous changes</summary>
      /// <param name="diff"></param>
      public void addChange(Point diff)
      {
         _sizeChange.Offset(diff.X, diff.Y);
      }
   }
}
