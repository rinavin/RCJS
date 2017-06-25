using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.tasks;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   class for tree control
   /// </summary>
   public abstract class MgTreeBase
   {
      public const int NODE_NOT_FOUND = -1;
      public TaskBase Task {get; set;} // task of tree
      public NodeBase NodeInExpand {get; set;} // node expanded

      protected NodeBase _lastRootSibling; // used to improve performance while reading roots siblings
      protected NodeInCreation _nodeInCreation; //node which we r trying to create. TODO
      protected Dictionary<int,NodeBase> _allNodes; // all nodes. maps from a node id to a node.
      protected NodeBase _root; // tree root
      protected int _size; // size of tree, also used for new node id creation

      /// <summary>
      ///   tree constructor
      /// </summary>
      public MgTreeBase(TaskBase task)
      {
         Task = task;
         if (Task.getForm() != null)
            Task.getForm().setMgTree(this);

         _allNodes = new Dictionary<int, NodeBase>();
      }

      /// <summary>
      ///   create node
      /// </summary>
      /// <param name = "parent"></param>
      /// <param name = "recId"></param>
      /// <param name = "expanded"></param>
      /// <param name = "childrenRetrieved"></param>
      /// <returns></returns>
      public NodeBase add(NodeBase parent, bool expanded, bool childrenRetrieved, int nodeId)
      {
         NodeBase lastSibling = (parent == null
                                ? _lastRootSibling
                                : parent.LastChild);
         NodeBase node = add(parent, lastSibling, nodeId);
         node.ChildrenRetrieved = childrenRetrieved;
         node.Expanded = expanded;
         return node;
      }

      /// <summary>
      ///   add node to tree 
      /// </summary>
      /// <param name = "parent"></param>
      /// <param name = "afterSibling"></param>
      /// <param name = "recId"></param>
      /// <returns></returns>
      protected NodeBase add(NodeBase parent, NodeBase afterSibling)
      {
         return add(parent, afterSibling, NODE_NOT_FOUND);
      }

      /// <summary>
      ///   add node to tree, update inner structures
      /// </summary>
      /// <param name = "parent">nodss parent</param>
      /// <param name = "afterSibling">add node after this sibling</param>
      /// <param name = "recId">node's record id</param>
      /// <returns> id of new node</returns>
      private NodeBase add(NodeBase parent, NodeBase afterSibling, int nodeId)
      {
         NodeBase node = ConstructNode(parent, afterSibling);
         //NodeBase node = new NodeBase(this, parent, afterSibling);
         node.NodeId = (nodeId == NODE_NOT_FOUND
                           ? _size + 1
                           : nodeId);
         _size++;
         
         int nodeIdInt = node.NodeId;         

         _allNodes[nodeIdInt] = node;
         
         // TODO: update arrays
         return node;
      }    

      /// <summary>
      ///   allow update of the tree
      /// </summary>
      /// <param name = "allow"></param>
      public void allowUpdate(bool allow)
      {
         Commands.addAsync(CommandType.ALLOW_UPDATE, Task.getForm().getTreeCtrl(), allow);
      }

      /// <summary>
      ///   update expand collapse states of all nodes
      /// </summary>
      /// <param name = "startLine"></param>
      public void updateExpandStates(int startLine)
      {
         NodeBase startNode = getNode(startLine);
         if (startNode != null)
            startNode.doForAllNodes(new NodeExpandStateUpdater());
      }

      /// <summary>
      ///   return number of lines in tree
      /// </summary>
      /// <returns></returns>
      public int getSize()
      {
         return _size;
      }

      /// <summary>
      ///   update expand status of node
      /// </summary>
      /// <param name = "nodeId">node idparam>
      ///   <param name = "val">true for expand, false for collapse</param>
      ///   <param name = "applyNow">if true, send command to guicommand queue</param>
      public void setExpanded(int nodeId, bool val, bool applyNow)
      {
         NodeBase node = getNode(nodeId);
         if (node != null)
            node.setExpanded(val, applyNow);
      }

      /// <summary>
      ///   calculate line to move to
      /// </summary>
      /// <param name = "direction">direction</param>
      /// <param name = "unit">page, row or table</param>
      /// <returns></returns>
      public int calculateLine(char direction, char unit, int orgNodeId)
      {
         NodeBase node;

         if (orgNodeId == 0)
            node = getNode(Task.getForm().DisplayLine);
         else
            node = getNode(orgNodeId);

         // the tree is not empty
         if (node != null)
         {
            NodeBase saveNode = node;
            int rowsInPage = Task.getForm().getRowsInPage();

            if (unit == Constants.MOVE_UNIT_TREE_NODE)
            {
               switch (direction)
               {
                  case Constants.MOVE_DIRECTION_PARENT:
                     node = node.Parent;
                     break;

                  case Constants.MOVE_DIRECTION_FIRST_SON:
                     node = node.FirstChild;
                     break;

                  case Constants.MOVE_DIRECTION_NEXT_SIBLING:
                     node = node.NextSibling;
                     break;

                  case Constants.MOVE_DIRECTION_PREV_SIBLING:
                     node = node.PrevSibling;
                     break;

                  default:
                     Debug.Assert(false);
                     break;
               }
            }
            else if (unit == Constants.MOVE_UNIT_TABLE)
            {
               switch (direction)
               {
                  case Constants.MOVE_DIRECTION_BEGIN: // first tree node
                     if (_size > 0)
                        node = _root;
                     break;

                  case Constants.MOVE_DIRECTION_END: // last tree node
                     node = getLastNode();
                     break;
               }
            }
            else if (unit == Constants.MOVE_UNIT_PAGE)
            {
               int top = Task.getForm().getTopIndexFromGUI();
               int visibleLine = getVisibleLine(top);
               if (top > 0)
               {
                  switch (direction)
                  {
                     case Constants.MOVE_DIRECTION_BEGIN: // first node on page
                        node = getNode(top);
                        break;

                     case Constants.MOVE_DIRECTION_END: // last node on page
                        node = findNode(getNode(top), Constants.MOVE_DIRECTION_NEXT, rowsInPage - 1);
                        break;

                     case Constants.MOVE_DIRECTION_PREV: // previous page
                        if (visibleLine == 0)
                           node = findNode(node, direction, rowsInPage);
                        else
                           node = getNode(top);
                        break;

                     case Constants.MOVE_DIRECTION_NEXT: // next page
                        if (visibleLine == rowsInPage - 1)
                           node = findNode(node, direction, rowsInPage);
                        else
                           node = findNode(getNode(top), Constants.MOVE_DIRECTION_NEXT, rowsInPage - 1);
                        break;
                  }
               }
            }
            else if (unit == Constants.MOVE_UNIT_ROW)
            {
               switch (direction)
               {
                  case Constants.MOVE_DIRECTION_PREV: // previous row
                     node = node.getPrev();
                     break;

                  case Constants.MOVE_DIRECTION_NEXT: // next row
                     node = node.getNext();
                     break;
               }
            }
            if (node != null)
               return node.NodeId;
            else
               return saveNode.NodeId;
         }
         return NODE_NOT_FOUND;
      }

      /// <summary>
      ///   get node by ID
      /// </summary>
      /// <param name = "nodeId">1-based</param>
      /// <returns></returns>
      protected NodeBase getNode(int nodeId)
      {
         NodeBase retNode;
         _allNodes.TryGetValue(nodeId, out retNode);
         return retNode;
      }

      /// <summary>
      ///   get last tree node
      /// </summary>
      /// <returns></returns>
      private NodeBase getLastNode()
      {
         NodeBase node = _lastRootSibling;
         while (node != null && node.Expanded)
         {
            if (node.LastChild != null)
               node = node.LastChild;
            else
               break;
         }
         return node;
      }

      /// <summary>
      ///   find node
      /// </summary>
      /// <param name = "node">start node</param>
      /// <param name = "direction">direction</param>
      /// <param name = "count">number of nodes to move</param>
      /// <returns> target node</returns>
      protected NodeBase findNode(NodeBase node, char direction, int count)
      {
         NodeBase saveNode = node;
         for (int i = 0;
              i < count;
              i++)
         {
            if (node != null)
            {
               saveNode = node;
               if (direction == Constants.MOVE_DIRECTION_NEXT)
                  node = node.getNext();
               else if (direction == Constants.MOVE_DIRECTION_PREV)
                  node = node.getPrev();
               else
                  Debug.Assert(false);
            }
         }
         if (node == null)
            node = saveNode;
         return node;
      }

      /// <summary>
      ///   calc visible line number
      /// </summary>
      /// <param name = "top"></param>
      /// <returns></returns>
      private int getVisibleLine(int top)
      {
         NodeBase node = getNode(top);
         NodeBase currNode = getNode(Task.getForm().DisplayLine);
         for (int i = 0;
              i < Task.getForm().getRowsInPage() + 1;
              i++)
         {
            if (node == null)
               break;
            if (node == currNode)
               return i;
            node = node.getNext();
         }
         return NODE_NOT_FOUND;
      }

      /// <summary>
      ///   get tree level
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <returns></returns>
      public int getLevel(int nodeId)
      {
         NodeBase node = getNode(nodeId);
         int level = 0;
         while (node != null && node.Parent != null)
         {
            node = node.Parent;
            level++;
         }
         return level;
      }

      /// <summary>
      ///   check if node ancestorId is ancestor of node child
      /// </summary>
      /// <param name = "ancestorId">node id of ancestor</param>
      /// <param name = "childId">node id of child</param>
      /// <returns></returns>
      public bool isAncestor(int ancestorId, int childId)
      {
         NodeBase child = getNode(childId);
         while (child != null && child.Parent != null)
         {
            child = child.Parent;
            if (child.NodeId == ancestorId)
               return true;
         }

         return false;
      }

      /// <summary>
      ///   return children retrieved state
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <returns></returns>
      public bool isChildrenRetrieved(int nodeId)
      {
         NodeBase child = getNode(nodeId);
         return child.ChildrenRetrieved;
      }

      /// <summary>
      ///   set children retrieved state
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <param name = "value"></param>
      public void setChildrenRetrieved(int nodeId, bool val)
      {
         NodeBase node = getNode(nodeId);
         node.setChildrenRetrieved(val);
      }

      /// <summary>
      ///   get display line of Parent
      /// </summary>
      /// <returns></returns>
      public int getParent(int line)
      {
         NodeBase node = getNode(line);
         if (node != null && node.Parent != null)
            return node.Parent.NodeId;
         return NODE_NOT_FOUND;
      }

      /// <summary>
      /// returns the node id of the first child of a given node.
      /// if given node does not exist. first child will be the root of the tree.
      /// </summary>
      /// <param name="nodeId"></param>
      /// <returns></returns>
      public int GetTreeNodeFirstChild(int nodeId)
      {         
         NodeBase firstChildNode;
         int      firstChiltNodeId;
         NodeBase node = getNode(nodeId);
         
         if (node == null)
            // given child does not exist. return the root.
            firstChildNode = _root; 
         else
            // node exists. check its firs child.
            firstChildNode = node.FirstChild;

         if (firstChildNode == null)
            // there is no first child. return 0.
            firstChiltNodeId = 0;
         else
            firstChiltNodeId = firstChildNode.NodeId;

         return firstChiltNodeId;
      }

      /// <summary>
      /// return the nodeId of the the given node's sibling.
      /// </summary>
      /// <param name="nodeId"></param>
      /// <returns></returns>
      public int GetTreeNodeNextSibling (int nodeId)
      {
         NodeBase nextSiblingNode;
         int      nextSiblingNodeId = 0;
         NodeBase node = getNode(nodeId);
         
         if (node != null)
         {
            nextSiblingNode = node.NextSibling;
            if (nextSiblingNode != null)
               nextSiblingNodeId = nextSiblingNode.NodeId;
         }

         return nextSiblingNodeId;
      }


      /// <summary>
      /// Creates a single node in MgTree and send a command to create it in GUI.
      /// </summary>
      /// <param name = "parentLine">display line of parent</param>
      /// <param name = "prevLine">display line of previous sibling</param>
      /// <param name = "recId">record id of new node</param>
      /// <returns> new display line</returns>
      public NodeBase addAndSendToGui(int parentNodeId, int afterNodeId, bool addedByUser)
      {
          NodeBase parent = null;
          NodeBase afterSibling = null;

          if (parentNodeId > 0)
              parent = getNode(parentNodeId);

          if (afterNodeId > 0)
          {
             afterSibling = getNode(afterNodeId);
             Debug.Assert(afterSibling.Parent == parent);
          }
      
          NodeBase newNode = add(parent, afterSibling, NODE_NOT_FOUND);

          newNode.createNodeInGUI();

          if (addedByUser)
          {
              if (parent != null)
                 parent.setExpanded(true, true);

              newNode.setExpanded(false, true);
              
          }
          newNode.setChildrenRetrieved(true);

          updateArrays();
          return newNode;
      }

      /// <summary>
      ///   update arrays of tree controls
      /// </summary>
      protected void updateArrays()
      {
         Task.getForm().getTreeCtrl().updateArrays(_size + 1);
      }

      /// <summary>
      ///   delete node
      /// </summary>
      /// <param name = "nodeId"></param>
      public void delete(int nodeId)
      {
         NodeBase node = getNode(nodeId);
         if (node.FirstChild != null)
            node.FirstChild.doForAllNodes(new NodeCleaner());
         node.delete();
      }

      /// <summary>
      ///   move node under its parent to be after a different node.
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <param name = "newPreviousSiblingId">our new location is after this sibling</param>
      public void moveNode(int nodeId, int newPreviousSiblingId)
      {         
         NodeBase node = getNode(nodeId);
         int orgPreviousSiblingId = 0;

         if (node.PrevSibling != null)
            orgPreviousSiblingId = node.PrevSibling.NodeId;

         // if location did not change, return.
         if (newPreviousSiblingId == orgPreviousSiblingId)
            return;

         // our new location is after this node.
         NodeBase afterNode = getNode(newPreviousSiblingId);

         NodeBase parentNode = node.Parent;         
         int parentId = (parentNode == null
                               ? 0
                               : parentNode.NodeId);

         // In order to move a node within a tree we will detach it from its current location
         // and then attach it in the new location.
         node.detachFromTree();
         node.attachToTree(parentNode, afterNode);

         // send a command to gui to move the node in the TreeView as well.
         Commands.addAsync(CommandType.MOVE_TREE_NODE, Task.getForm().getTreeCtrl(), nodeId, parentId, 0, newPreviousSiblingId);
      }

      /// <summary>
      ///   get next node
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <returns></returns>
      public int getNext(int nodeId)
      {
         NodeBase node = getNode(nodeId);
         if (node != null)
         {
            NodeBase next = node.getNext();
            if (next != null)
               return next.NodeId;
            else
               return 0;
         }
         return 0;
      }

      /// <summary>
      ///   get previous node
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <returns></returns>
      public int getPrev(int nodeId)
      {
         NodeBase node = getNode(nodeId);
         if (node != null)
         {
            NodeBase prev = node.getPrev();
            if (prev != null)
               return prev.NodeId;
            else
               return 0;
         }
         else
            return 0;
      }

      /// <summary>
      ///   return true if node has children
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <returns></returns>
      public bool hasChildren(int nodeId)
      {
         NodeBase node = getNode(nodeId);
         return (node == null
                    ? false
                    : node.FirstChild != null);
      }

      /// <summary>
      ///   returns display line to move after deleting a given node.
      /// </summary>
      /// <param name = "onDelete">true if we are removing the record due to delete. false if due to cancel on insert</param>
      /// /// <param name = "nodeId">node to be deleted</param>
      /// <returns></returns>
      public int calcLineForRemove(bool onDelete, int nodeId)
      {
         NodeBase currNode = getNode(nodeId);
         NodeBase target;
         if (onDelete)
         // we are in F3
         {
            // try to move down in sub tree
            target = currNode.NextSibling;
            // try to move up in subTree
            if (target == null)
               target = currNode.PrevSibling;
            if (target == null)
               target = currNode.Parent;
         }
         // here we are in cancel edit
         else
         {
            // try to move up in subTree
            target = currNode.PrevSibling;
            if (target == null)
               target = currNode.Parent;
            // try to move down
            if (target == null)
               target = currNode.NextSibling;
         }
         if (target == null)
            return NODE_NOT_FOUND;
         else
            return target.NodeId;
      }

      /// <summary>
      ///   returns display line to move after deleting the current node.
      /// </summary>
      /// <param name = "onDelete">true if we are removing the record due to delete. false if due to cancel on insert</param>
      /// <returns></returns>
      public int calcLineForRemove(bool onDelete)
      {
         return calcLineForRemove(onDelete, Task.getForm().DisplayLine);
      }

      /// <summary>
      ///   set node for expand
      /// </summary>
      /// <param name = "displayLine"></param>
      public void setNodeInExpand(int displayLine)
      {
         NodeInExpand = getNode(displayLine);
      }

      /// <summary>
      ///   get expanded node
      /// </summary>
      /// <returns></returns>
      public NodeBase getNodeInExpand()
      {
         return NodeInExpand;
      }

      /// <summary>
      ///   recursively deletes children of the node
      /// </summary>
      /// <param name = "line"></param>
      public void deleteChildren(int line)
      {
         NodeBase node = getNode(line);
         if (node != null && node.FirstChild != null)
         {
            node.FirstChild.deleteSiblingsWithSubtree();
         }
      }

      /// <summary>
      ///   recursively deletes children of the node
      /// </summary>
      /// <param name = "line"></param>
      public void deleteWithChildren(int line)
      {
         NodeBase node = getNode(line);
         if (node != null)
            node.deleteSubTree();
      }

      

      /// <summary>
      ///   clean tree from nodes
      /// </summary>
      public void clean()
      {
         NodeBase node = _root;
         // in case there are multiple roots, there is 1 root and siblings to that root.
         // so when the 1st root is deleted, its next sibling becomes the new root.
         while (_root != null)
         {
            deleteWithChildren(_root.NodeId);            
         }
         _size = 0;
         updateArrays();
         _root = null;
         _lastRootSibling = null;
         Debug.Assert(NodeInExpand == null);
         NodeInExpand = null;
         Debug.Assert(_nodeInCreation == null);
         _nodeInCreation = null;
      }
       
      /// <summary>
      ///   expand all ancestors of the line
      /// </summary>
      /// <param name = "line"></param>
      public void expandAncestors(int line)
      {
         NodeBase node = getNode(line);
         while (node != null && node.Parent != null)
         {
            node = node.Parent;
            if (!node.Expanded)
               node.setExpanded(true, true);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="line"></param>
      /// <returns></returns>
      public bool isExpanded(int line)
      {
         NodeBase node = getNode(line);
         return node == null
                   ? false
                   : node.Expanded;
      }

      /// <summary>
      ///   return number of direct children of a given node.
      /// </summary>
      /// <param name = "nodeId"></param>
      /// <param name = "directChildsOnly">true means, count only direct nodes
      /// false, means : count all nodes under given node in all levels.</param>
      /// <returns></returns>
      public int GetNumberOfChildNodes(int nodeId, bool directChildsOnly)
      {
         NodeBase node = getNode(nodeId);
         int childNodes = 0;

         if (node != null)
         {
            NodeBase childNode = node.FirstChild;
            while (childNode != null)
            {
               childNodes++;
               if (!directChildsOnly)
                  childNodes += GetNumberOfChildNodes(childNode.NodeId, false);

               childNode = childNode.NextSibling;
            }
         }

         return childNodes;
      }

      // Used to create either RC node or RTE node.
      // Because in MgTreeBase we cannot create a new Node, just NodeBase, and we need Node.
      protected abstract NodeBase ConstructNode(NodeBase parent, NodeBase afterSibling);

      #region Nested type: NodeBase

      /// <summary>
      ///   Internal class that represents the nodes
      /// </summary>
      public class NodeBase
      {
         protected readonly MgTreeBase _enclosingInstance;
         public bool ChildrenRetrieved { get; set; } // true, if server tried to read nodes children
         public bool Expanded { get; set; } // true, if node is expanded

         public NodeBase FirstChild { get; private set; } // first child
         public NodeBase LastChild { get; private set; } // last child
         public NodeBase NextSibling { get; private set; } // next sibling

         // Data
         public int NodeId { get; set; } // node id (displayLine of node)
         public NodeBase Parent { get; private set; } // node parent
         public NodeBase PrevSibling { get; private set; } // previous sibling

         /// <summary>
         /// Attach a node to the tree in the given location, under 'parent' and after 'afterSibling'.
         /// </summary>
         /// <param name="parent">The new parent node to be attached to</param>
         /// <param name="afterSibling">The new location will be after that node</param>
         public void attachToTree(NodeBase parent, NodeBase afterSibling)
         {            
            NodeBase next;
            Parent = parent;
            if (parent != null)
            {
               if (parent.FirstChild == null)
                  // this is only child
                  parent.FirstChild = parent.LastChild = this;
               else
               {
                  Debug.Assert(parent.LastChild != null);
                  if (afterSibling == null)
                  // adding child as first sibling
                  {
                     next = parent.FirstChild;
                     parent.FirstChild = this;
                     setNextSibling(next);
                     PrevSibling = null;
                  }
                  else
                  {
                     // add child after existing sibling
                     next = afterSibling.NextSibling;
                     afterSibling.setNextSibling(this);
                     if (next != null)
                        // adding node in the middle of siblings
                        setNextSibling(next);
                     // adding last sibling
                     else
                        parent.LastChild = this;
                  }
               }
            }
            else
            {
               // we are adding a root.
               // either this is the first root or (after == null) we try to move a root to be the first.
               if (_enclosingInstance._root == null)
               // adding first node
               {
                  _enclosingInstance._root = this;
                  _enclosingInstance._lastRootSibling = this;
               }
               else
               {
                  // one of the roots is moving to be the first.
                  //last sibling was handled when we detached.
                  if (afterSibling == null)
                  {
                     setNextSibling(_enclosingInstance._root);
                     _enclosingInstance._root = this;
                  }
                  else
                  {
                     // inserting not the first root siblings.
                     Debug.Assert(_enclosingInstance._lastRootSibling != null);

                     next = afterSibling.NextSibling;
                     afterSibling.setNextSibling(this);
                     if (next != null)
                        // adding node in the middle of siblings
                        setNextSibling(next);
                     // adding last sibling
                     else
                        _enclosingInstance._lastRootSibling = this;
                  }
               }
            }
         }
         /// <summary>
         ///   creates node in the tree, updates siblings, children, parents
         /// </summary>
         /// <param name = "parent"></param>
         /// <param name = "afterSibling">sibling to add after - null means add first</param>
         public NodeBase(MgTreeBase enclosingInstance, NodeBase parent, NodeBase afterSibling)
         {
            _enclosingInstance = enclosingInstance;
            this.attachToTree(parent, afterSibling);
         }

         /// <summary>
         /// detach node from mgtree
         /// </summary>
         internal void detachFromTree()
         {
            // update tree structure
            if (Parent != null)
            {
               if (PrevSibling != null)
               {
                  PrevSibling.setNextSibling(NextSibling);
                  if (NextSibling == null)
                     Parent.LastChild = PrevSibling;
               }
               else
               {
                  Parent.FirstChild = NextSibling;
                  if (NextSibling == null)
                     Parent.LastChild = null;
                  else
                     NextSibling.PrevSibling = PrevSibling;
               }
            }
            else if (PrevSibling != null)
            // delete root sibling
            {
               PrevSibling.setNextSibling(NextSibling);
               if (NextSibling == null)
                  _enclosingInstance._lastRootSibling = PrevSibling;
            }
            else
            {
               _enclosingInstance._root = NextSibling;
               if (NextSibling == null)
                  _enclosingInstance._lastRootSibling = null;
               else
                  NextSibling.PrevSibling = PrevSibling;
            }

            // free the node from its siblings.
            PrevSibling = null;
            NextSibling = null;
         }

         /// <summary>
         ///   delete node
         /// </summary>
         internal void delete()
         {
            detachFromTree();
            // remove from nodes mapping
            deleteFromMappings();

            if (NodeId == _enclosingInstance._size)
            // this is last record, we can decrease size
            {
               _enclosingInstance._size--;
               _enclosingInstance.updateArrays();
            }

            Commands.addAsync(CommandType.DELETE_TREE_NODE, _enclosingInstance.Task.getForm().getTreeCtrl(), NodeId, 0);
            if (Parent != null && Parent.FirstChild == null)
               // parent has no more children
               Parent.setExpanded(false, true); // collaplse parent
         }

         /// <summary>
         ///   delete node from nodesMap and RecordsMap
         /// </summary>
         virtual public void deleteFromMappings()
         {
             int nodeId = NodeId;
             _enclosingInstance._allNodes.Remove(nodeId);                      
         }

         /// <summary>
         ///   set node as next sibling of this node
         /// </summary>
         /// <param name = "nextSibling"></param>
         private void setNextSibling(NodeBase nextSibling)
         {
            NextSibling = nextSibling;
            if (nextSibling != null)
               nextSibling.PrevSibling = this;
         }

         /// <summary>
         ///   set node expanded
         /// </summary>
         /// <param name = "value">true if node expanded</param>
         /// <param name = "applyNow">if true, send command to GUI to update expand state</param>
         public void setExpanded(bool val, bool applyNow)
         {
            Expanded = val;
            if (applyNow)
               sendExpandedToGUI();
         }

         /// <summary>
         ///   send command to GUI to update expand state
         /// </summary>
         internal void sendExpandedToGUI()
         {
            Commands.addAsync(CommandType.SET_EXPANDED, _enclosingInstance.Task.getForm().getTreeCtrl(), NodeId, Expanded);
         }

         /// <summary>
         ///   set children retrieved state
         /// </summary>
         /// <param name = "value">value</param>
         public void setChildrenRetrieved(bool val)
         {
            ChildrenRetrieved = val;
            sendChildrenRetrievedToGUI();
         }

         /// <summary>
         ///   send command to GUI to update children retrieved state
         /// </summary>
         public void sendChildrenRetrievedToGUI()
         {
            Commands.addAsync(CommandType.SET_CHILDREN_RETRIEVED, _enclosingInstance.Task.getForm().getTreeCtrl(), NodeId, ChildrenRetrieved);
         }

         /// <summary>
         ///   send to GUI command to create node
         /// </summary>
         public void createNodeInGUI()
         {
            int parentId = (Parent == null
                               ? 0
                               : Parent.NodeId);
            int afterSiblingId = (PrevSibling == null
                                     ? 0
                                     : PrevSibling.NodeId);
            Commands.addAsync(CommandType.CREATE_TREE_NODE, _enclosingInstance.Task.getForm().getTreeCtrl(), NodeId, parentId, 0, afterSiblingId);
         }

         /// <summary>
         ///   return next node of tree
         /// </summary>
         /// <returns></returns>
         internal NodeBase getNext()
         {
            if (Expanded && FirstChild != null)
               return FirstChild;
            if (NextSibling != null)
               return NextSibling;
            NodeBase parentNode = Parent;
            while (parentNode != null)
            {
               if (parentNode.NextSibling != null)
                  return parentNode.NextSibling;
               parentNode = parentNode.Parent;
            }
            return null;
         }

         /// <summary>
         ///   return previous node of tree
         /// </summary>
         /// <returns></returns>
         internal NodeBase getPrev()
         {
            if (PrevSibling != null)
            {
               NodeBase previous = PrevSibling;
               while (previous.Expanded && previous.LastChild != null)
                  previous = previous.LastChild;
               return previous;
            }
            if (Parent != null)
               return Parent;
            return null;
         }

         /// <summary>
         ///   Go over all nodes and execute method defined in NodeExecutor
         /// </summary>
         /// <param name = "nodeExecutor">Class implementing NodeExecutor interface</param>
         public void doForAllNodes(NodeExecutor nodeExecutor)
         {
            doForAllNodes(nodeExecutor, 0);
         }

         /// <summary>
         ///   Go over all nodes and execute method defined in NodeExecutor
         /// </summary>
         /// <param name = "nodeExecutor">Class implementing NodeExecutor interface</param>
         /// <param name = "level">level in tree</param>
         protected void doForAllNodes(NodeExecutor nodeExecutor, int level)
         {
            //nodeExecutor can change tree, we should remember this in the beginning, QCR #784544
            bool isFirstSibling = (Parent != null && this == Parent.FirstChild || (this == _enclosingInstance._root));

            if (nodeExecutor.Stop)
               return;
            nodeExecutor.doBeforeChildren(this, level);
            if (FirstChild != null)
               FirstChild.doForAllNodes(nodeExecutor, level + 1);
            if (nodeExecutor.Stop)
               return;
            nodeExecutor.doAfterChildren(this, level);
            if (nodeExecutor.Stop)
               return;
            // if we use recursion for siblings, call stack may explode
            if (isFirstSibling)
            {
               NodeBase curr = NextSibling;
               while (curr != null)
               {
                  if (nodeExecutor.Stop)
                     return;
                  curr.doForAllNodes(nodeExecutor, level);
                  curr = curr.NextSibling;
               }
            }
         }

         /// <summary>
         ///   delete current node with children and its siblings with children
         /// </summary>
         internal void deleteSiblingsWithSubtree()
         {
            NodeBase curr = this;
            while (curr != null)
               curr = curr.deleteSubTree();
         }

         /// <summary>
         ///   delete  subtree starting with this node
         /// </summary>
         /// <returns> returns next sibling</returns>
         internal NodeBase deleteSubTree()
         {
            while (FirstChild != null)
               FirstChild.deleteSiblingsWithSubtree();
            NodeBase next = NextSibling;
            delete();
            return next;
         }
      }

      #endregion

      #region Nested type: NodeCleaner

       /// <summary>
       /// removes all nodes from the mappings.
       /// </summary>
      internal class NodeCleaner : NodeExecutor
      {
         public override void doAfterChildren(NodeBase node, int level)
         {
            node.deleteFromMappings();
         }

         public override void doBeforeChildren(NodeBase node, int level)
         {
         }
      }

      #endregion

      
      #region Nested type: NodeExecutor

      /// <summary>
      ///   interface to be executed on every node on sub tree
      /// </summary>
      public abstract class NodeExecutor
      {
         public bool Stop { get; set; } //stops execution 

         /// <summary>
         ///   this method will be executed on parent before on it's children
         /// </summary>
         public abstract void doBeforeChildren(NodeBase node, int level);

         /// <summary>
         ///   this method will be executed on parent after its children
         /// </summary>
         public abstract void doAfterChildren(NodeBase node, int level);
      }

      #endregion

      #region Nested type: NodeExpandStateUpdater

      /// <summary>
      ///   update node's expand state
      /// </summary>
      internal class NodeExpandStateUpdater : NodeExecutor
      {
         public override void doBeforeChildren(NodeBase node, int level)
         {
            // we must change expand only after children are created
            node.sendExpandedToGUI();
         }

         public override void doAfterChildren(NodeBase node, int level)
         {
         }
      }

      #endregion

      

      #region Nested type: NodeInCreation

      /// <summary>
      ///   This class is used to remember details of node that we r going to create
      /// </summary>
      public class NodeInCreation
      {
         public int ParentLine { get; private set; } //parent line of node to create
         public int PrevLine { get; private set; } //previous sibling line of node to create

         public NodeInCreation(int parentLine, int prevLine)
         {
            ParentLine = parentLine;
            PrevLine = prevLine;
         }
      }

      #endregion

      



   }
}
