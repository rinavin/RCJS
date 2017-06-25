using com.magicsoftware.richclient.data;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.unipaas;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.events;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using DataView = com.magicsoftware.richclient.data.DataView;
using Field = com.magicsoftware.richclient.data.Field;
using util.com.magicsoftware.util;

internal enum RecordIdx   // should not be here. its from guiEnems
{
    NOT_FOUND = -1
}

namespace com.magicsoftware.richclient.gui
{
    internal class MgTree : MgTreeBase
    {
       public Dictionary<int, List<int>> _recordNodeIDs { get; set; } // maps from a record id to a its nodes ids.

        /// <summary>
        ///   tree constructor
        /// </summary>
        internal MgTree(Task task)
            : base(task)
        {
           ((DataView)Task.DataView).setMgTree(this);
            _recordNodeIDs = new Dictionary<int, List<int>>();
        }

        /// <summary>
        /// _recordNodeIDs hold the list of nodes for every record.
        /// each record is represented by a node. more then one node per record means recursion.
        /// This method adds a nodeId to the list of nodes belonging to the recordId.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="recordId"></param>
        internal void mapNodeTorecordNodeIDs(int nodeId, int recordId)
        {
            List<int> nodeIDs;
            _recordNodeIDs.TryGetValue(recordId, out nodeIDs);
            if (nodeIDs == null)
            {
                nodeIDs = new List<int>();
                _recordNodeIDs[recordId] = nodeIDs;
            }
            nodeIDs.Add(nodeId);
        }

        /// <summary>
        /// add node to tree
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="recId"></param>
        /// <param name="expanded"></param>
        /// <param name="childrenRetrieved"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        internal NodeBase add(NodeBase parent, int recId, bool expanded, bool childrenRetrieved, int nodeId)
        {
            NodeBase NewNode = base.add(parent, expanded, childrenRetrieved, nodeId);
            ((Node)NewNode).RecId = recId;

            mapNodeTorecordNodeIDs(NewNode.NodeId, recId);
            return NewNode;
        }

        /// <summary>
        ///   add node to tree 
        /// </summary>
        /// <param name = "parent"></param>
        /// <param name = "afterSibling"></param>
        /// <param name = "recId"></param>
        /// <returns></returns>
        private NodeBase add(NodeBase parent, NodeBase afterSibling, int recId)
        {
            NodeBase NewNode = base.add(parent, afterSibling);
            ((Node)NewNode).RecId = recId;

            mapNodeTorecordNodeIDs(NewNode.NodeId, recId);
            return NewNode;
        }

        /// <summary>
        ///   build tree XML
        /// </summary>
        internal void buildXML()
        {
            StringBuilder message = new StringBuilder();
            String taskTag = Task.getTaskTag();

            message.Append(XMLConstants.START_TAG + XMLConstants.MG_TAG_TREE + " " + XMLConstants.MG_ATTR_TASKID +
                           "=\"" + taskTag + "\"" + XMLConstants.TAG_CLOSE);
            if (_root != null)
                _root.doForAllNodes(new XMLBuilder(message));
            message.Append("\n   </" + XMLConstants.MG_TAG_TREE + XMLConstants.TAG_CLOSE);
        }

        /// <summary>
        ///   creates nodes on gui low level and sets their properties
        /// </summary>
        /// <param name = "startLine">first node to refresh</param>
        internal void createGUINodes(int startLine)
        {
            allowUpdate(false);
            int displayLine = Task.getForm().DisplayLine;
            updateArrays();
            NodeBase startNode = getNode(startLine);
            if (startNode != null)
                startNode.doForAllNodes(new NodeCreator(this));
            ((MgForm)(Task.getForm())).restoreOldDisplayLine(displayLine);
            allowUpdate(true);
        }

        /// <summary>
        ///   update tree after expand of expandedNodeLine
        /// </summary>
        /// <param name = "expandedNodeLine"></param>
        internal void updateAfterExpand(int expandedNodeLine)
        {
            NodeBase expandNode = getNode(expandedNodeLine);
            if (expandNode != null && expandNode.FirstChild != null)
            {
                createGUINodes(expandNode.FirstChild.NodeId);
                setExpanded(expandedNodeLine, true, true);
            }
            NodeInExpand = null;
        }

        /// <summary>
        ///   get record idx by node id
        /// </summary>
        /// <param name = "nodeId"></param>
        /// <returns> record idx of the node, if node does not exist return -1</returns>
        internal int getRecIdx(int nodeId)
        {
            Node node = (Node)(getNode(nodeId));
            if (node == null)
                return (int)RecordIdx.NOT_FOUND;
            else
                return ((DataView)Task.DataView).getRecIdx(node.RecId);
        }

        /// <summary>
        ///   get first node that belongs to the rec idx
        /// </summary>
        /// <param name = "recIdx"></param>
        /// <returns></returns>
        internal int getFirstNodeIdByRecIdx(int recIdx)
        {
            int recId = ((DataView)Task.DataView).getRecByIdx(recIdx).getId();
            List<int> nodeIDs;
            _recordNodeIDs.TryGetValue(recId, out nodeIDs);
            if (nodeIDs != null && nodeIDs.Count > 0)
                return (nodeIDs[0]);
            // TODO: check this
            return 0; // this display line means that properties will be sent for tree
        }

        /// <summary>
        /// Creates a single node in MgTree and send a command to create ti in GUI.
        /// </summary>
        /// <param name="parentNodeId"></param>
        /// <param name="afterNodeId"></param>
        /// <param name="recId"></param>
        /// <returns></returns>
        internal int addAndSendToGui(int parentNodeId, int afterNodeId, int recId)
        {
            NodeBase NewNode = base.addAndSendToGui(parentNodeId, afterNodeId, true);
            ((Node)NewNode).RecId = recId;

            mapNodeTorecordNodeIDs(NewNode.NodeId, recId);
            return NewNode.NodeId;
        }

        /// <summary>
        ///   We are trying to move to node that has no record this is possible if tree has recursion. In this case we must recursivly go to the parent,
        ///   until it finds parent that has records Nodes with no records must be deleted.
        /// </summary>
        /// <param name = "newLine"></param>
        /// <returns></returns>
        internal bool handleNoRecordException(int newLine)
        {
            // we are here because newLine record was deleted
            int parentLine = getParent(newLine);
            while (parentLine != NODE_NOT_FOUND)
            // find parent with existing record
            {
                int parentRecIdx = ((MgForm)(Task.getForm())).displayLine2RecordIdx(parentLine);
                if (parentRecIdx == (int)RecordIdx.NOT_FOUND)
                {
                    // parent node's record was also deleted
                    newLine = parentLine;
                    parentLine = getParent(newLine);
                }
                else
                {
                    try
                    {
                        // found parent with existing record
                        ((MgForm)(Task.getForm())).setCurrRowByDisplayLine(parentLine, true, true);
                        ((Task)Task).SetRefreshType(Constants.TASK_REFRESH_FORM);
                        Task.RefreshDisplay();
                        deleteWithChildren(newLine);
                        return true;
                    }
                    catch (RecordOutOfDataViewException e)
                    {
                        Logger.Instance.WriteExceptionToLog(e);
                        throw new ApplicationException("in handleNoRecordException() invalid exception");
                    }
                }
            }

            if (parentLine == NODE_NOT_FOUND)
            {
                //if (!_task.IsTryingToStop)  TODO: relocate to MgxpaRIA
                {
                    if (Task.DataView.isEmptyDataview())
                        deleteWithChildren(1);
                    else
                    {
                        if (Task.checkProp(PropInterface.PROP_TYPE_ALLOW_EMPTY_DATAVIEW, true))
                            Task.DataView.setEmptyDataview(true);
                        else
                            // if we deleted last node from tree - exit from the task
                            ((EventsManager)Manager.EventsManager).exitWithError(Task, MsgInterface.RT_STR_NO_CREATE_ON_TREE);
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///   serializes parents node IDs of a record, to be sent in the erquest.
        /// </summary>
        /// <param name = "nodeId"></param>
        /// <returns></returns>
        internal String getPath(int nodeId)
        {
            Node node = (Node)(getNode(nodeId));
            String path = "";
            if (node != null)
            {
                Stack parents = getParents(node);
                path = "" + int.MinValue; // send Integer.MIN_VALUE record id for parent of root
                while (parents.Count != 0)
                {
                    path += ",";
                    node = (Node)(parents.Pop());
                    path += node.RecId;
                }
            }
            return path;
        }

        /// <summary>
        ///   get nodes parents rec ids
        /// </summary>
        /// <param name = "node"></param>
        /// <returns></returns>
        private Stack getParents(NodeBase node)
        {
            Stack parents = new Stack();
            while (node != null)
            {
                parents.Push(node);
                node = node.Parent;
            }
            return parents;
        }

        /// <summary>
        ///   serializes nodes parents values for the request.
        /// </summary>
        /// <param name = "nodeId"></param>
        /// <returns>returns>
        internal String getParentsValues(int nodeId)
        {
            Node node = (Node)(getNode(nodeId));
            String values = "";
            if (node != null)
            {
                Stack parents = getParents(node);
                // initialize with value of parent of root
                values = ((MgControl)(Task.getForm().getTreeCtrl())).getParentFieldXML(((DataView)Task.DataView).getRecIdx(((Node)_root).RecId));
                while (parents.Count != 0)
                {
                    values += ",";
                    node = (Node)(parents.Pop());
                    values += ((MgControl)(Task.getForm().getTreeCtrl())).getFieldXML(((DataView)Task.DataView).getRecIdx(node.RecId));
                }
            }
            return values;
        }

        /// <summary>
        /// serializes the nulls indications to be sent along with the values of the nodes
        /// when the client is sending a request.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        internal String getNulls(int nodeId)
        {
            NodeBase node = getNode(nodeId);
            String nulls = "";
            if (node != null)
            {
                Stack parents = getParents(node);
                nulls = "" + (((MgControl)(Task.getForm().getTreeCtrl())).isTreeParentNull(1)
                                 ? "1"
                                 : "0");
                while (parents.Count != 0)
                {
                    node = (NodeBase)parents.Pop();
                    nulls += (((MgControl)(Task.getForm().getTreeCtrl())).isTreeNull(node.NodeId)
                                 ? "1"
                                 : "0");
                }
            }
            return nulls;
        }

        /// <summary>
        ///   replicate tree
        /// </summary>
        /// <returns></returns>
        internal MgTreeBase replicate()
        {
            MgTree rep = (MgTree)MemberwiseClone();
            rep._root = null;
            rep._allNodes = new Dictionary<int, NodeBase>();
            rep._recordNodeIDs = new Dictionary<int, List<int>>();
            rep._size = 0;
            rep._lastRootSibling = null;
            rep.NodeInExpand = null;
            rep._nodeInCreation = null;
            if (_root != null)
                _root.doForAllNodes(new NodeReplicator(rep));

            return rep;
        }

        /// <summary>
        ///   after tree loads update form
        /// </summary>
        internal void afterLoad()
        {
            if (NodeInExpand == null)
            {
                updateArrays();
                ((Task)Task).SetRefreshType(Constants.TASK_REFRESH_TREE_AND_FORM);
            }
        }

        /// <summary>
        ///   remember details of node that we r creating
        /// </summary>
        /// <param name = "parentLine"></param>
        /// <param name = "prevLine"></param>
        internal void setNodeInCreation(int parentLine, int prevLine)
        {
            _nodeInCreation = new NodeInCreation(parentLine, prevLine);
        }

        /// <summary>
        ///   remove NodeInCreation
        /// </summary>
        internal void removeNodeInCreation()
        {
            _nodeInCreation = null;
        }

        /// <summary>
        ///   are we in process of creation of new node ?
        /// </summary>
        /// <returns></returns>
        internal bool hasNodeInCreation()
        {
            return _nodeInCreation != null;
        }

        /// <summary>
        ///   finish process of adding new node
        /// </summary>
        /// <param name = "recId"></param>
        /// <returns></returns>
        internal int addNodeInCreation(int recId)
        {
            Debug.Assert(_nodeInCreation != null);
            return addAndSendToGui(_nodeInCreation.ParentLine, _nodeInCreation.PrevLine, recId);
        }

        /// <summary>
        ///   find node in tree by value. for goto function.
        /// </summary>
        /// <param name = "mgValue"></param>
        /// <param name = "isNull"></param>
        /// <param name = "type"></param>
        /// <returns> found node id or NODE_NOT_FOUND in case nothing is found</returns>
        internal int findNode(String mgValue, bool isNull, StorageAttribute type)
        {
            NodeFinder nodeFinder = new NodeFinder(this, mgValue, isNull, type);
            if (_root != null)
                _root.doForAllNodes(nodeFinder);
            return nodeFinder.NodeId;
        }

        /// <summary>
        /// MgTreeBase is using this method in order to create the correct type of node.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="afterSibling"></param>
        /// <returns></returns>
        protected override NodeBase ConstructNode(NodeBase parent, NodeBase afterSibling)
        {
            return new Node(this, parent, afterSibling);
        }

        #region Nested type: Node

        /// <summary>
        /// The Rich Client node, differs from RTE node by having RecId.
        /// </summary>
        internal class Node : NodeBase
        {
            internal int RecId { get; set; } // record id

            internal Node(MgTreeBase enclosingInstance, NodeBase parent, NodeBase afterSibling)
                : base(enclosingInstance, parent, afterSibling)
            {
            }

            /// <summary>
            /// deletes also from the _recordNodeIDs.
            /// </summary>
            /// <param name="node"></param>
            public override void deleteFromMappings()
            {
                base.deleteFromMappings();
                int recordId = RecId;
                List<int> nodeIDs = ((MgTree)_enclosingInstance)._recordNodeIDs[recordId];
                nodeIDs.Remove(NodeId);
                if (nodeIDs.Count == 0)
                    ((MgTree)_enclosingInstance)._recordNodeIDs.Remove(recordId);
            }
        }

        #endregion

        #region Nested type: NodeCreator

        /// <summary>
        ///   create node on GUI low level and sets its properties
        /// </summary>
        internal class NodeCreator : NodeExecutor
        {
            private readonly MgTreeBase _enclosingInstance;

            internal NodeCreator(MgTreeBase enclosingInstance)
            {
                _enclosingInstance = enclosingInstance;
            }

            /// <summary>
            ///   create tree item and refresh its properties
            /// </summary>
            public override void doBeforeChildren(NodeBase node, int level)
            {
                node.createNodeInGUI();
                node.sendChildrenRetrievedToGUI();
                try
                {
                    ((MgForm)(_enclosingInstance.Task.getForm())).setCurrRowByDisplayLine(node.NodeId, false, true);
                    _enclosingInstance.Task.getForm().getTreeCtrl().RefreshDisplay(true);
                }
                catch (RecordOutOfDataViewException e)
                {
                    Misc.WriteStackTrace(e, Console.Error);
                }
            }

            public override void doAfterChildren(NodeBase node, int level)
            {
            }
        }

        #endregion

        #region Nested type: NodeFinder

        /// <summary>
        ///   class used to find node
        /// </summary>
        internal class NodeFinder : NodeExecutor
        {
            private readonly MgTreeBase _enclosingInstance;

            private readonly bool _isNull; //is it null
            private readonly String _mgValue; //value to find
            private readonly StorageAttribute _type; //type of value
            private Field _field; //field of node 
            internal int NodeId { get; set; }

            internal NodeFinder(MgTreeBase enclosingInstance, String mgValue, bool isNull, StorageAttribute type)
            {
                _enclosingInstance = enclosingInstance;
                _mgValue = mgValue;
                _isNull = isNull;
                _type = type;
                NodeId = NODE_NOT_FOUND;
                _field = (Field)(_enclosingInstance.Task.getForm().getTreeCtrl().getNodeIdField());
            }

            public override void doBeforeChildren(NodeBase node, int level)
            {
                //check if this node has mgValue
                int recIdx = ((MgForm)(_enclosingInstance.Task.getForm())).displayLine2RecordIdx(node.NodeId);
                if (recIdx != (int)RecordIdx.NOT_FOUND)
                {
                    if (_field.isEqual(_mgValue, _isNull, _type, recIdx))
                    {
                        NodeId = node.NodeId;
                        Stop = true;
                    }
                }
            }

            public override void doAfterChildren(NodeBase node, int level)
            {
            }
        }

        #endregion

        #region Nested type: NodeRelicator

        /// <summary>
        ///   used to replicate tree
        /// </summary>
        private class NodeReplicator : NodeExecutor
        {
            private readonly Stack _nodesStack;
            private readonly MgTree _repDest;

            /// <param name = "repDest">the replication destination tree</param>
            internal NodeReplicator(MgTree repDest)
            {
                _repDest = repDest;
                _nodesStack = new Stack();
            }

            public override void doBeforeChildren(NodeBase node, int level)
            {
                NodeBase parent = (_nodesStack.Count == 0
                                  ? null
                                  : (NodeBase)_nodesStack.Peek());
                NodeBase repNode = _repDest.add(parent, ((Node)node).RecId, node.Expanded, node.ChildrenRetrieved, node.NodeId);
                _nodesStack.Push(repNode);
            }

            public override void doAfterChildren(NodeBase node, int level)
            {
                _nodesStack.Pop();
            }
        }

        #endregion

        #region Nested type: XMLBuilder

        /// <summary>
        ///   creates nodes XML
        /// </summary>
        internal class XMLBuilder : MgTreeBase.NodeExecutor
        {
            private StringBuilder _message;

            internal XMLBuilder(StringBuilder message)
            {
                _message = message;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="node"></param>
            /// <param name="level"></param>
            public override void doBeforeChildren(NodeBase node, int level)
            {
                String expand = (node.Expanded
                                    ? "1"
                                    : "0");
                _message.Append("\n" + getSpaces(level) + "<" + XMLConstants.MG_TAG_NODE + " " + XMLConstants.MG_ATTR_ID +
                                "=\"" + ((Node)node).RecId + "\" " + XMLConstants.MG_ATTR_EXPAND + "=\"" + expand + "\"" +
                                XMLConstants.TAG_CLOSE);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="node"></param>
            /// <param name="level"></param>
            public override void doAfterChildren(NodeBase node, int level)
            {
                _message.Append("\n" + getSpaces(level) + "</" + XMLConstants.MG_TAG_NODE + XMLConstants.TAG_CLOSE);
            }

            /// <summary>
            ///   computes indentation
            /// </summary>
            /// <param name = "level"></param>
            /// <returns></returns>
            static private String getSpaces(int level)
            {
                return new string(' ', (level + 1) * 3);
            }
        }

        #endregion
    }
}
