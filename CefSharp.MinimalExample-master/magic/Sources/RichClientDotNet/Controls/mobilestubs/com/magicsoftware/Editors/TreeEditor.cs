using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.controls;
using System.Windows.Forms;
using System.Drawing;
#if PocketPC
using LayoutEventArgs = com.magicsoftware.mobilestubs.LayoutEventArgs;
#endif

namespace com.magicsoftware.editors
{
    /// <summary>
    ///  implement editor for tree
    /// </summary>
    public class TreeEditor : Editor
    {
        public TreeEditor(TreeView treeView, TreeNode node)
            : base(treeView)
        {
        }

        void TreeView_Layout(object sender, LayoutEventArgs e)
        {
        }

        /// <summary>
        /// set node to editor
        /// </summary>
        public TreeNode Node
        {
            get { return null; }
            set
            {
            }
        }


        /// <summary>
        /// hide editor
        /// </summary>
        public override void Hide()
        {
        }

        /// <summary>
        /// return true if comtrol is hidden
        /// </summary>
        /// <returns></returns>
        public override bool isHidden()
        {
            return false;
        }

        /// <summary>
        /// calculate editor bounds
        /// </summary>
        /// <returns></returns>
        public override Rectangle Bounds()
        {
            return new Rectangle();

        }
    }
}
