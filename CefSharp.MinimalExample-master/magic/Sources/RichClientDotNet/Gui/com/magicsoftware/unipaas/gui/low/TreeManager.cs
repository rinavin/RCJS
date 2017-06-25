using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using CultureInfo = System.Globalization.CultureInfo;
using com.magicsoftware.editors;
using com.magicsoftware.controls.utils;
using com.magicsoftware.controls;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// pure GUI functionality (GUI thread).
   /// </summary>
   class TreeManager : ItemsManager
   {
      private TreeView _treeControl; // tree widget
      private GuiMgControl _mgTreeControl; // magic tree control
      private Form _form;
      private TreeNode _selectedTreeNode; // save selected item
      private TreeNode _prevSelectedTreeNode; // previously selected item
      private TreeEditor _tmpEditor; // temporary editor for tree
      private DateTime _focusTime = DateTime.Now;
      internal bool IsLabelEditAllowed { private set; get; }
      internal bool InExpand {  set; get; }

      internal TreeManager(TreeView treeControl, GuiMgControl mgTreeControl)
         : base(treeControl)
      {
         Debug.Assert(Misc.IsGuiThread());
         _treeControl = treeControl;
         _mgTreeControl = mgTreeControl;
         _rowsInPage = -1;
         _tmpEditor = createEditor();
         treeControl.LabelEdit = true;
         treeControl.HideSelection = false;
         treeControl.ShowLines = true;
         treeControl.ShowPlusMinus = true;
         treeControl.ShowRootLines = true;
      }

      /// <summary> 
      /// create parent row
      /// </summary>
      /// <param name="parentIdx">index of parent</param>
      /// <param name="afterSibling">add new node after this sibling, if equals 0, add node as first child</param>
      /// <param name="mgRow">idx of new node</param>
      internal void createNode(int parentIdx, int afterSiblingIdx, int mgRow)
      {
         Debug.Assert(Misc.IsGuiThread());
         TreeNode item = null;
         Object parent = controlsMap.object2Widget(_mgTreeControl, parentIdx);
         Debug.Assert(parent != null);
         int idx = 0;
         TreeChild afterSibling = null;
         if (afterSiblingIdx > 0)
         {
            afterSibling = (TreeChild)controlsMap.object2Widget(_mgTreeControl, afterSiblingIdx);
            idx = afterSibling.getIndex() + 1;
         }

         if (parent is TreeView)
         {
            item = ((TreeView)parent).Nodes.Insert(idx, "");
            // ensure that the root is visible even if the tree is not visible.
            // if not ensured, set expand might not work on nodes and when tree is visible
            // it will be collapsed.
            item.EnsureVisible();
         }
         else if (parent is TreeChild)
         {
            ((TreeChild)parent).removeDummyItem();
            item = (((TreeChild)parent).getTreeNode()).Nodes.Insert(idx, "");
         }
         else
            Debug.Assert(false);

         TreeChild treeChild = new TreeChild(this, _mgTreeControl, 0, mgRow, item);
         // check that we do not add node twice
         Debug.Assert(controlsMap.object2Widget(_mgTreeControl, mgRow) == null);
         // add treechild to madding
         controlsMap.add(_mgTreeControl, mgRow, treeChild);
         // set map data on tree item
         item.Tag = new TagData();
         controlsMap.setMapData(_mgTreeControl, mgRow, item);
      }

      /// <summary>
      /// move node under parent to a location after 'afterSiblingIdx'.
      /// </summary>
      /// <param name="parentIdx">The parent node under which we are moving the node</param>
      /// <param name="afterSiblingIdx">The new location is after that sibling</param>
      /// <param name="nodeId">The node to move</param>      
      internal void moveNode(int parentIdx, int afterSiblingIdx, int nodeId)
      {
         Debug.Assert(Misc.IsGuiThread());
         Object parent = controlsMap.object2Widget(_mgTreeControl, parentIdx);
         Debug.Assert(parent != null);
         int idx = 0;
         TreeChild afterSibling = null;
         TreeChild nodeToMove = null;

         //get the treeChild of the node to move
         nodeToMove = (TreeChild)controlsMap.object2Widget(_mgTreeControl, nodeId);
         
         // get the new index under the parent
         if (afterSiblingIdx > 0)
         {
            afterSibling = (TreeChild)controlsMap.object2Widget(_mgTreeControl, afterSiblingIdx);
            idx = afterSibling.getIndex() + 1;
         }

         //inserting an open node, raises the before expand. avoid it.
         _treeControl.BeforeExpand -= TreeHandler.getInstance().BeforeExpandHandler;

         // remove from the nodes collection and add at the new location.
         if (parent is TreeView)
         {
            //remove and insert of a root (The parent is the tree itself)
            ((TreeView)parent).Nodes.Remove(nodeToMove.getTreeNode());
            ((TreeView)parent).Nodes.Insert(idx, nodeToMove.getTreeNode());
         }
         else if (parent is TreeChild)
         {
            //remove and insert of a node with another node as a parent.
            (((TreeChild)parent).getTreeNode()).Nodes.Remove(nodeToMove.getTreeNode());
            (((TreeChild)parent).getTreeNode()).Nodes.Insert(idx, nodeToMove.getTreeNode());
         }
         else
            Debug.Assert(false);

         // resume handling 'Before expand'.
         _treeControl.BeforeExpand += TreeHandler.getInstance().BeforeExpandHandler;
      }

      /// <summary>
      /// see ItemsManager#setSelectionIndex(int)
      /// </summary>
      /// <param name="number"></param>
      internal override void setSelectionIndex(int number)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (number != GuiConstants.NO_ROW_SELECTED)
         {
            TreeChild treeChild = (TreeChild)controlsMap.object2Widget(_mgTreeControl, number);

            if (_prevSelectedTreeNode != null && _prevSelectedTreeNode.TreeView != null /* isDisposed()*/)
            {
               // workaround for SWT bug. QCR #729438
               Rectangle rect = _prevSelectedTreeNode.Bounds;
               _treeControl.Invalidate(new Rectangle(rect.X, rect.Y, rect.Width + 10, rect.Height), true);
            }

            _treeControl.BeforeSelect -= TreeHandler.getInstance().TreeNodeBeforeSelect;

            _treeControl.SelectedNode = treeChild.getTreeNode();
            _selectedTreeNode = treeChild.getTreeNode();
            _prevSelectedTreeNode = _selectedTreeNode;
            showSelection();

            _treeControl.BeforeSelect += TreeHandler.getInstance().TreeNodeBeforeSelect;
         }
         else
         {
            _treeControl.SelectedNode = null;
            _prevSelectedTreeNode = null;
         }
      }

      /// <summary>
      /// get map data of treeNode depending on point
      /// </summary>
      /// <param name="pt"></param> point on tree
      /// <param name="findExact">click anywhere in the line of an item except plus/minus sign 
      /// is considered as click on item</param>
      /// <returns></returns>
      internal MapData pointToMapData(Point pt, bool findExact)
      {
         Debug.Assert(Misc.IsGuiThread());
         TreeViewHitTestInfo info = _treeControl.HitTest(pt);
         if (findExact)
         {
            if (info != null)
            {
               switch (info.Location)
               {
                  case TreeViewHitTestLocations.Image:
                  case TreeViewHitTestLocations.Indent:
                  case TreeViewHitTestLocations.Label:
                  case TreeViewHitTestLocations.RightOfLabel:
                  case TreeViewHitTestLocations.StateImage:
                     return (controlsMap.getMapData(info.Node));
                  default:
                     break;
               }
            }
            return null;
         }
         else if (info != null && info.Node != null)
            return (controlsMap.getMapData(info.Node));
         else
            return (controlsMap.getMapData(_treeControl));
      }

      /// <summary> 
      /// tree resize
      /// </summary>
      internal void resize()
      {
         Debug.Assert(Misc.IsGuiThread());
         int newRowsInPage = _treeControl.ClientRectangle.Height / _treeControl.ItemHeight;
         if (newRowsInPage != _rowsInPage)
         {
            _rowsInPage = newRowsInPage;
            Events.OnTableResize(_mgTreeControl, _rowsInPage);
         }
      }

      /// <summary> return tree top index</summary>
      internal override int getTopIndex()
      {
         Debug.Assert(Misc.IsGuiThread());
         TreeNode treeNode = _treeControl.TopNode;
         if (treeNode != null)
            return controlsMap.getMapData(treeNode).getIdx();
         return 0;
      }

      /// <summary> 
      /// creates temporary editor for a child
      /// </summary>
      /// <param name="child"></param>
      private TreeEditor createEditor()
      {
         Debug.Assert(Misc.IsGuiThread());
         TreeEditor editor = new TreeEditor(_treeControl, null);
         return editor;
      }

      /// <summary> open temporary editor for tree child create a text control for the editor
      /// </summary>
      /// <param name="child"></param>
      /// <returns></returns>
      internal Control showTmpEditor(TreeChild child)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (_form == null)
            _form = GuiUtils.FindForm(_treeControl);
         GuiUtils.SetTmpEditorOnTagData(_form, _tmpEditor);
         TextBox text = (_tmpEditor.Control == null ? (TextBox)toControl(_mgTreeControl, CommandType.CREATE_EDIT) :
         (TextBox)_tmpEditor.Control);

         text.Show(); //fixed bug #:310473, control is created as hiden we need to show the created editor.

         ControlUtils.SetBGColor(text, Color.White);
         text.BorderStyle = BorderStyle.FixedSingle;
         ControlsMap.getInstance().setMapData(_mgTreeControl, child._mgRow, text);

         if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja") // JPN: IME support
            child.Modifable = true;

         child.setProperties(text);
         _tmpEditor.Control = text;
         _tmpEditor.Node = child.getTreeNode();
         _tmpEditor.Layout();
         ((TagData)_form.Tag).LastFocusedControl = text;

         GuiUtils.setFocus(text, true, false);
         return text;
      }

      /// <summary> return temporary editor
      /// </summary>
      /// <returns></returns>
      internal TreeEditor getTmpEditor()
      {
         Debug.Assert(Misc.IsGuiThread());
         return _tmpEditor;
      }

      //<summary> get tree child from tree item</summary>
      //<param name="treeNode"></param>
      //<returns></returns>
      internal static TreeChild getTreeChild(TreeNode treeNode)
      {
         Debug.Assert(Misc.IsGuiThread());
         ControlsMap controlsMap = ControlsMap.getInstance();
         MapData mapdata = controlsMap.getMapData(treeNode);
         TreeChild child = (TreeChild)controlsMap.object2Widget(mapdata.getControl(), mapdata.getIdx());
         return child;
      }

      /// <summary> show tree selection
      /// 
      /// </summary>
      internal void showSelection()
      {
         Debug.Assert(Misc.IsGuiThread());
         if (_treeControl.SelectedNode != null)
            _treeControl.SelectedNode.EnsureVisible();
      }

      /// <summary>
      /// allow update of the tree
      /// </summary>
      /// <param name="val"></param>
      internal void allowUpdate(bool val)
      {
         Debug.Assert(Misc.IsGuiThread());
         if (val)
            _treeControl.EndUpdate();
         else
            _treeControl.BeginUpdate();
      }

      /// <summary>
      /// set label edit property
      /// </summary>
      /// <param name="val"></param>
      internal void labelEdit(bool val)
      {
         Debug.Assert(Misc.IsGuiThread());
         _treeControl.LabelEdit = val;
      }

      /// <summary>
      /// set focus time
      /// </summary>
      internal void setFocusTime()
      {
         Debug.Assert(Misc.IsGuiThread());
         _focusTime = DateTime.Now;
      }

      /// <summary>
      /// set focus time
      /// </summary>
      internal void setMouseDownTime()
      {
         Debug.Assert(Misc.IsGuiThread());
         TimeSpan ts = DateTime.Now - _focusTime;
         IsLabelEditAllowed = (ts.TotalMilliseconds > SystemInformation.DoubleClickTime);
      }

      /// <summary>
      /// implement HitTest
      /// </summary>
      /// <param name="pt"></param>
      /// <param name="findExact"></param> not relevant for tree
      /// <param name="checkEnabled"></param> not relevant for tree
      /// <returns></returns>
      internal override MapData HitTest(Point pt, bool findExact, bool checkEnabled)
      {
         Debug.Assert(Misc.IsGuiThread());
         return pointToMapData(pt, findExact);
      }

      internal override void Dispose()
      {
         Debug.Assert(Misc.IsGuiThread());
         ControlsMap controlsMap = ControlsMap.getInstance();
         controlsMap.removeFromIdx(_mgTreeControl, 1);
         _tmpEditor.Dispose();
      }

      internal override Editor getEditor()
      {
         Debug.Assert(Misc.IsGuiThread());
         return _tmpEditor;
      }
   }
}