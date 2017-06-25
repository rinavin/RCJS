using com.magicsoftware.win32;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using com.magicsoftware.controls.utils;
using com.magicsoftware.controls.designers;

namespace com.magicsoftware.controls
{
   /// <summary>
   /// add scroll event to treeview
   /// </summary>
#if !PocketPC
   [Designer(typeof(TreeViewDesigner)), Docking(DockingBehavior.Never)]
   [ToolboxBitmap(typeof(TreeView))]
#endif
   public class MgTreeView : TreeView, IRightToLeftProperty, IBorderStyleProperty, ISetSpecificControlPropertiesForFormDesigner
   {
      public event ScrollEventHandler Scroll;

      #region IRightToLeftProperty Members

      public override RightToLeft RightToLeft
      {
         get
         {
            return base.RightToLeft;
         }
         set
         {
            base.RightToLeft = value;
            RightToLeftLayout = (value == RightToLeft.Yes ? true : false);
         }
      }

      #endregion

      public override Color BackColor
      {
         get
         {
            return base.BackColor;
         }
         set
         {
            base.BackColor = value;

            if (!Utils.IsXPStylesActive())
               foreach (TreeNode item in Nodes)
                  setBgColor(item, base.BackColor);
         }
      }

      /// <summary>
      /// set bg color to node and all it children
      /// </summary>
      /// <param name="node"></param>
      /// <param name="color"></param>
      void setBgColor(TreeNode node, Color color)
      {
         node.BackColor = color;
         foreach (TreeNode item in node.Nodes)
            setBgColor(item, color);
      }

      protected override void WndProc(ref Message m)
      {
         switch (m.Msg)
         {
            case NativeWindowCommon.WM_VSCROLL:
            case NativeWindowCommon.WM_HSCROLL:
               if (Scroll != null)
                  Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, 0));
               break;
         }
         base.WndProc(ref m);
      }

      public void setSpecificControlPropertiesForFormDesigner(Control fromControl)
      {
         TreeNode RootNode = null;
         TreeNode ExpandedNode = null;
         TreeNode CollapsedNode = null;

         ControlUtils.InitDefualtNodesForTree((TreeView)this, "Expanded Node", "Collapsed Node", "Leaf", true, 
                                                ref RootNode, ref ExpandedNode, ref CollapsedNode);
      }
   }
}
