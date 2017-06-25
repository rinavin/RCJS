using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   /// 
   /// </summary>
   internal class LgLinkLabel : LogicalControl
   {
      private Color? _hoveringBgColor;
      private Color? _hoveringFgColor;
      private bool _visited;
      private Color? _visitedBgColor;
      private Color? _visitedFgColor;

      public int MgHoveringColorIndex { get; set; }
      public int MgVisitedColorIndex { get; set; }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="guiMgControl"></param>
      /// <param name="containerControl"></param>
      internal LgLinkLabel(GuiMgControl guiMgControl, Control containerControl) :
         base(guiMgControl, containerControl)
      {
      }

      internal bool Visited
      {
         get { return _visited; }
         set
         {
            _visited = value;
            _coordinator.Refresh(true);
         }
      }

      internal Color? HoveringFGColor
      {
         get { return _hoveringFgColor; }
         set
         {
            _hoveringFgColor = value;
            _coordinator.Refresh(true);
         }
      }

      internal Color? HoveringBGColor
      {
         get { return _hoveringBgColor; }
         set
         {
            _hoveringBgColor = value;
            _coordinator.Refresh(true);
         }
      }

      internal Color? VisitedFGColor
      {
         get { return _visitedFgColor; }
         set
         {
            _visitedFgColor = value;
            _coordinator.Refresh(true);
         }
      }

      internal Color? VisitedBGColor
      {
         get { return _visitedBgColor; }
         set
         {
            _visitedBgColor = value;
            _coordinator.Refresh(true);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="control"></param>
      internal override void setSpecificControlProperties(Control control)
      {
         MgControlType type = GuiMgControl.Type;
         ControlUtils.SetContentAlignment(control, ContentAlignment);
         if (Text != null)
            GuiUtilsBase.setText(control, Text);
         if (GuiMgControl.IsHyperTextButton())
         {
            MgLinkLabel mgLinkLabel = ((MgLinkLabel)control);

            mgLinkLabel.SetHoveringColor(HoveringFGColor, HoveringBGColor);
            mgLinkLabel.SetVisitedColor(VisitedFGColor, VisitedBGColor);
            mgLinkLabel.LinkVisited = Visited;
            mgLinkLabel.RefreshLinkColor();
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="refreshNow"></param>
      internal override void RecalculateColors(bool refreshNow)
      {
         if (MgHoveringColorIndex != 0)
         {
            HoveringBGColor = ColorIndexToColor(MgHoveringColorIndex, true);
            HoveringFGColor = ColorIndexToColor(MgHoveringColorIndex, false);
         }

         if(MgVisitedColorIndex != 0)
         {
            VisitedBGColor = ColorIndexToColor(MgVisitedColorIndex, true);
            VisitedFGColor = ColorIndexToColor(MgVisitedColorIndex, false);
         }

         base.RecalculateColors(refreshNow);
      }
   }
}