using System;
using System.Drawing;
using System.Windows.Forms;
using com.magicsoftware.editors;
using com.magicsoftware.controls.utils;
using com.magicsoftware.controls;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary>
   ///   class represents tree item
   /// </summary>
   /// <author>rinav</author>
   internal class TreeChild : LogicalControl
   {
      private readonly TreeManager _treeManager;
      private readonly TreeNode _treeNode;
      private bool _childrenRetrieved;
      private int _collapsedImageIdx;
      private int _expandedImageIdx;
      private int _parkedCollapsedImageIdx;
      private int _parkedExpanedImageIdx;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="treeManager"></param>
      /// <param name="ctrl"></param>
      /// <param name="mgColumn"></param>
      /// <param name="mgRow"></param>
      /// <param name="treeNode"></param>
      internal TreeChild(TreeManager treeManager, GuiMgControl ctrl, int mgColumn, int mgRow, TreeNode treeNode)
         : base(ctrl, treeManager.mainControl)
      {
         _mgRow = mgRow;
         _treeManager = treeManager;
         _treeNode = treeNode;
         _childrenRetrieved = true;
         if (!Utils.IsXPStylesActive())
            treeNode.BackColor = treeNode.TreeView.BackColor;
         Visible = true;
         TextSizeLimit = -1; // -1 to later indicate that the limit was not set.
      }

      internal TreeManager TreeManager
      {
         get { return _treeManager; }
      }

      /// <summary>
      ///   expanded image index
      /// </summary>
      internal int ExpandedImageIdx
      {
         get { return _expandedImageIdx; }
         set
         {
            _expandedImageIdx = value;
            updateImages();
         }
      }

      /// <summary>
      ///   collapsed image index
      /// </summary>
      internal int CollapsedImageIdx
      {
         get { return _collapsedImageIdx; }
         set
         {
            _collapsedImageIdx = value;
            updateImages();
         }
      }

      /// <summary>
      ///   collapsed selected image index
      /// </summary>
      internal int ParkedCollapsedImageIdx
      {
         get
         {
            if (_parkedCollapsedImageIdx > 0)
               return _parkedCollapsedImageIdx;
            else
               return _collapsedImageIdx;
         }
         set
         {
            _parkedCollapsedImageIdx = value;
            updateImages();
         }
      }

      /// <summary>
      ///   expanded selected image index
      /// </summary>
      internal int ParkedExpanedImageIdx
      {
         get
         {
            if (_parkedExpanedImageIdx > 0)
               return _parkedExpanedImageIdx;
            else
               return _expandedImageIdx;
         }
         set
         {
            _parkedExpanedImageIdx = value;
            updateImages();
         }
      }

      internal override string Text
      {
         get { return base.Text; }
         set
         {
            bool changed = Text != value;
            
            base.Text = value;
            if (changed)
            {      
               if (_treeNode == _treeNode.TreeView.SelectedNode)
                  // don't do the work around for the selected node. if this node is a new node 
                  // going to be edited, removing and inserting the node will change the selection.
                  _treeNode.Text = Text;
               else
               {
                  // performance work around. updating a node's text while it is attached to the tree
                  // triggers a lot of win msgs. A much faster way would be to detach the node, 
                  // update the text and then reattach it in the same location.
                  int index = getIndex();  
                  TreeNodeCollection parentNodeCollection;

                  if (_treeNode.Parent != null)
                     parentNodeCollection = _treeNode.Parent.Nodes;
                  else
                     parentNodeCollection = _treeNode.TreeView.Nodes;

                  // removing and inserting will trigger the BeforeExapanding event. 
                  // we do not want to get expanded messages for this.
                  _treeNode.TreeView.BeforeExpand -= TreeHandler.getInstance().BeforeExpandHandler;
                  parentNodeCollection.Remove(_treeNode); 
                  _treeNode.Text = Text;
                  
                   parentNodeCollection.Insert(index , _treeNode);
                  _treeNode.TreeView.BeforeExpand += TreeHandler.getInstance().BeforeExpandHandler; 
               }
            }
            Control control = getEditorControl();
            if (control != null)
               GuiUtilsBase.setText(control, Text);
         }
      }

      /// <summary>
      ///   if childrenRetrieved becomes false, that create dummy item to hava effect of parent node if childrenRetrieved becomes true, remove the dummy
      ///   item
      /// </summary>
      /// <param name = "childrenRetrieved"></param>
      internal void setChildrenRetrieved(bool childrenRetrieved)
      {
         if (_childrenRetrieved != childrenRetrieved)
         {
            if (childrenRetrieved)
               removeDummyItem();
            else
               addDummyItem();
            _childrenRetrieved = childrenRetrieved;
         }
      }

      /// <summary>
      ///   check if item is dummy item
      /// </summary>
      /// <param name = "treeItem"></param>
      /// <returns></returns>
      internal static bool isDummyItem(TreeNode treeItem)
      {
         return (ControlsMap.getInstance().getMapData(treeItem) == null);
      }

      /// <summary>
      ///   removes dummy item
      /// </summary>
      internal void removeDummyItem()
      {
         if (_treeNode.Nodes.Count > 0)
         {
            TreeNode firstItem = _treeNode.Nodes[0];
            if (isDummyItem(firstItem))
            {
               _treeNode.TreeView.BeforeSelect -= TreeHandler.getInstance().TreeNodeBeforeSelect;
               firstItem.Remove();
               _treeNode.TreeView.BeforeSelect += TreeHandler.getInstance().TreeNodeBeforeSelect;
            }
         }
      }

      /// <summary>
      ///   adds dummy item
      /// </summary>
      private void addDummyItem()
      {
         //check that iw was not already created
         if (_treeNode.Nodes.Count > 0)
         {
            TreeNode firstItem = _treeNode.Nodes[0];
            if (isDummyItem(firstItem))
               return;
         }
         _treeNode.Nodes.Insert(0, "");
      }

      /// <summary>
      ///   add dummy item only if node is not preloaded and it has no children
      /// </summary>
      internal void checkAndAddDummyItem()
      {
         if (_treeNode.Nodes.Count == 0 && !_childrenRetrieved)
            addDummyItem();
      }

      /// <summary>
      ///   set node expand
      /// </summary>
      /// <param name = "expand"></param>
      internal void setExpand(bool expand)
      {
         if (expand)
         {
            _treeNode.TreeView.BeforeExpand -= TreeHandler.getInstance().BeforeExpandHandler;
             removeDummyItem();
            _treeNode.Expand();
            _treeNode.TreeView.BeforeExpand += TreeHandler.getInstance().BeforeExpandHandler;
         }
         else
         {
            _treeNode.TreeView.BeforeCollapse -= TreeHandler.getInstance().BeforeCollapseHandler;
            _treeNode.Collapse(true);
            _treeNode.TreeView.BeforeCollapse += TreeHandler.getInstance().BeforeCollapseHandler;
         }
         updateImages();
      }

      /// <summary>
      ///   update images according to image style
      /// </summary>
      private void updateImages()
      {
         int imageIndex;
         int selectedImage;

         if (_treeNode.IsExpanded)         
         {
            imageIndex = ExpandedImageIdx - 1;
            selectedImage = ParkedExpanedImageIdx - 1;            
         }
         else
         {
            imageIndex = CollapsedImageIdx - 1;
            selectedImage = ParkedCollapsedImageIdx - 1;
         }

         //only if there is an image list and this node has no image selection.
         // -1 means no image, but treeView will show image 0 (default image)
         // to show blank image, we need to set to -2.
         if (_treeNode.TreeView.ImageList != null)
         {
            if (imageIndex == -1)
               imageIndex = -2;

            if (selectedImage == -1)
               selectedImage = -2;
         }

         _treeNode.ImageIndex = imageIndex;
         _treeNode.SelectedImageIndex = selectedImage;
      }

      /// <summary>
      ///   return tree item
      /// </summary>
      /// <returns>
      /// </returns>
      internal TreeNode getTreeNode()
      {
         return _treeNode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      internal override string getSpecificControlValue()
      {
         return _treeNode.Text;
      }

      /// <summary>
      ///   return index of treeItem
      /// </summary>
      /// <returns></returns>
      internal int getIndex()
      {
         TreeNode parentItem = _treeNode.Parent;
         if (parentItem != null)
            return parentItem.Nodes.IndexOf(_treeNode);
         else
            return _treeNode.TreeView.Nodes.IndexOf(_treeNode);
      }

      /// <summary>
      ///   dispose treeItrm
      /// </summary>
      internal override void Dispose()
      {
         Object parentItem = _treeNode.Parent;
         TreeView treeview = _treeNode.TreeView;
         base.Dispose();
         TreeEditor treeEditor = _treeManager.getTmpEditor();
         if (treeEditor.Node == _treeNode)
            treeEditor.Node = null;

         // remove might change selection. we dont want to catch it. 
         treeview.BeforeSelect -= TreeHandler.getInstance().TreeNodeBeforeSelect;

         _treeNode.Remove();

         treeview.BeforeSelect += TreeHandler.getInstance().TreeNodeBeforeSelect;

         if (parentItem is TreeNode)
         {
            TreeChild parent = TreeManager.getTreeChild((TreeNode) parentItem);
            parent.checkAndAddDummyItem();
         }
      }

      /// <summary>
      ///   copy child properties to the editor
      /// </summary>
      /// <param name = "control"></param>
      internal void setProperties(TextBox control)
      {
         //GuiUtils.setTooltip(control, tooltip);
         Font font = _treeNode.TreeView.Font;
         // fixed bug #:942320, the setFont is closed the combo box, need to do it only
         // if the font was changed
         if (!font.Equals(control.Font))
            ControlUtils.SetFont(control, font);
         setTextProperties(control, _treeNode.Text);
         GuiUtils.setRightToLeft(control, _treeNode.TreeView.RightToLeftLayout);
      }

      /// <summary>
      ///   get editor control of child
      /// </summary>
      /// <returns></returns>
      public override Control getEditorControl()
      {
         TreeEditor treeEditor = _treeManager.getTmpEditor();
         Control control = treeEditor.Control;
         if (control != null)
         {
            MapData mapData = ControlsMap.getInstance().getMapData(control);
            if (mapData != null && _mgRow == mapData.getIdx())
               // this child has temporary editor
               return control;
         }
         return null;
      }

      /// <summary>
      /// returns rectangle of the control according to its container
      /// </summary>
      /// <returns></returns>
      public override Rectangle getRectangle()
      {
         return _treeNode.Bounds;
      }
   }
}