using System;
using System.Collections;
using System.Collections.Specialized;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;
using com.magicsoftware.unipaas.management.gui;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.tasks
{
   /// <summary>
   ///   sax parser for reading tree xml
   /// </summary>
   /// <author>  rinav</author>
   internal class TreeSaxHandler : MgSAXHandler
   {
      private readonly Stack _nodesStack; // stack of parents, exists while reading nodes XML
      private MgTree _mgtree;

      /// <summary>
      /// 
      /// </summary>
      internal TreeSaxHandler()
      {
         _nodesStack = new Stack();
      }

      /// <summary>
      ///   Every time the parser encounters the beginning of a new element, it calls this method, which resets the
      ///   string buffer
      /// </summary>
      public override void startElement(String elementName, NameValueCollection attributes)
      {
         switch (elementName)
         {
            case XMLConstants.MG_TAG_TREE:
               {
                  String taskId = attributes[XMLConstants.MG_ATTR_TASKID];
                  Task task = (Task)MGDataCollection.Instance.GetTaskByID(taskId);


                  //find mgtree
                  _mgtree = ((MgTree)(task.getForm().getMgTree()) ?? new MgTree(task));

                  //expand only send new nodes, they should be added under expanded node
                  MgTreeBase.NodeBase nodeInExpand = _mgtree.getNodeInExpand();
                  if (nodeInExpand != null)
                     _nodesStack.Push(nodeInExpand);
                  break;
               }

            case XMLConstants.MG_TAG_NODE:
               {
                  MgTreeBase.NodeBase parent = (_nodesStack.Count == 0 ? null : (MgTreeBase.NodeBase)_nodesStack.Peek());
                  int recId = getInt(attributes, XMLConstants.MG_ATTR_ID);
                  bool childrenRetrieved = attributes[XMLConstants.MG_ATTR_CHILDREN_RETRIEVED].Equals("1",
                                                                                                      StringComparison.CurrentCultureIgnoreCase);
                  bool expanded = attributes[XMLConstants.MG_ATTR_EXPAND].Equals("1",
                                                                                 StringComparison.CurrentCultureIgnoreCase);
                  MgTreeBase.NodeBase node = _mgtree.add(parent, recId, expanded, childrenRetrieved, MgTreeBase.NODE_NOT_FOUND);
                  _nodesStack.Push(node);
                  break;
               }
         }
      }

      /// <summary>
      ///   Takes care of end element
      /// </summary>
      public override void endElement(String elementName, String elementValue)
      {
         switch (elementName)
         {
            case XMLConstants.MG_TAG_NODE:
               _nodesStack.Pop();
               break;

            case XMLConstants.MG_TAG_TREE:
               {
                  //here we pop expanded node
                  if (_nodesStack.Count != 0)
                     _nodesStack.Pop();
                  _mgtree.afterLoad();
                  break;
               }

            default:
               {
                  break;
               }
         }
      }

      /// <summary>
      ///   get integer attribute by name
      /// </summary>
      /// <param name = "atts">sax attributes</param>
      /// <param name = "str">attribute's name</param>
      /// <returns> int value</returns>
      private static int getInt(NameValueCollection atts, String str)
      {
         String val = atts[str];
         if (val != null)
            return (Int32.Parse(val));
         else
            return (0);
      }
   }
}