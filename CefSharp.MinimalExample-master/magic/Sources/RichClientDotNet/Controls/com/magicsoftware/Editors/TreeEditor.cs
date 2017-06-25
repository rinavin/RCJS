using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.Windows.Forms;
using System.Drawing;

namespace com.magicsoftware.editors
{
    /// <summary>
    ///  implement editor for tree
    /// </summary>
    public class TreeEditor : Editor
    {
        private TreeNode node;

        public TreeEditor(TreeView treeView, TreeNode node)
            : base(treeView)
        {
            Node = node;
        }

        void TreeView_Layout(object sender, LayoutEventArgs e)
        {
            Layout();
        }

        /// <summary>
        /// set node to editor
        /// </summary>
        public TreeNode Node
        {
            get { return node; }
            set
            {
                if (node != null)
                    node.TreeView.Layout -= TreeView_Layout;
                this.node = value;
                if (node != null)
                    node.TreeView.Layout += TreeView_Layout;


            }
        }


        /// <summary>
        /// hide editor
        /// </summary>
        public override void Hide()
        {
            Node = null;
            base.Hide();
        }

        /// <summary>
        /// return true if comtrol is hidden
        /// </summary>
        /// <returns></returns>
        public override bool isHidden()
        {
            return node == null;
        }

        /// <summary>
        /// calculate editor bounds
        /// </summary>
        /// <returns></returns>
        public override Rectangle Bounds()
        {
            Rectangle rect = node.Bounds;
            rect.Width = parentControl.ClientRectangle.Width - rect.X;
            rect.Height = ((TreeView)parentControl).ItemHeight + SystemInformation.BorderSize.Height * 2;
            return rect;

        }




    }
}
