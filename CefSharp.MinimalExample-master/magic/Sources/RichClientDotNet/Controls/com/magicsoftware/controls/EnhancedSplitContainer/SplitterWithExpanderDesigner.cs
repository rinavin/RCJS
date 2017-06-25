using System.Drawing;
using System.Windows.Forms.Design;
using System.Windows.Forms.Design.Behavior;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// Area of HitTest for Splitter
   /// </summary>
   public enum SplitterHitTestArea
   {
      Expander,
      Splitter
   }

   /// <summary>
   /// Designer for SplitterWithExpander
   /// </summary>
   public class SplitterWithExpanderDesigner : ControlDesigner
   {
      protected override bool GetHitTest(Point point)
      {
         SplitterWithExpander splitter = this.Component as SplitterWithExpander;
         bool result = base.GetHitTest(point);

         point = splitter.PointToClient(point);
         SplitterHitTestArea hitTestResult = splitter.HitTest(point);
         if (hitTestResult == SplitterHitTestArea.Expander)
            return true;
         return false;
      }

      /// <summary>
      /// Splitter must not be selectable 
      /// </summary>
      public override SelectionRules SelectionRules
      {
         get
         {
            return SelectionRules.None;
         }
      }

      /// <summary>
      /// Splitter must not be selectable 
      /// </summary>
      public override GlyphCollection GetGlyphs(GlyphSelectionType selectionType)
      {
         return new GlyphCollection();;
      }
   }
}