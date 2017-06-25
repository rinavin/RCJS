using System;
using System.Collections;
using System.Xml;
using System.Collections.Generic;

namespace com.magicsoftware.util
{
   // This class provides interface methods to interact with XML
   public static class XmlServices
   {
      /// <summary>creates a new Xml Document</summary>
      /// <param name="rootNodeStr">root node string</param>
      /// <returns></returns>
      public static XmlDocument CreateDocument(String rootNodeStr)
      {
         if (String.IsNullOrEmpty(rootNodeStr))
            throw new Exception("Root element cannot be null or empty.");

         XmlDocument doc = new XmlDocument();

         // create xml declaration
         XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
         doc.AppendChild(decl);
         
         // Create the root element
         XmlNode root = doc.CreateElement(rootNodeStr);
         doc.AppendChild(root);

         return doc;
      }

      /// <summary>adds a new element into Xml Document</summary>
      /// <param name="doc">xml document</param>
      /// <param name="parentNode">parent node to which new element is a child. If NULL, adds to root node</param>
      /// <param name="currNodeStr">the string for the new element</param>
      /// <returns></returns>
      public static XmlElement AddElement(XmlDocument doc, XmlNode parentNode, String currNodeStr)
      {
         if (parentNode == null)
            // get the root node
            parentNode = doc.DocumentElement;
         
         // create a new element
         XmlElement childElement = doc.CreateElement(currNodeStr);
         // add the new element as child to the parent node
         XmlElement elementAdded = (XmlElement)parentNode.AppendChild(childElement);

         return elementAdded;
      }

      /// <summary>adds a new node to the Xml Document</summary>
      /// <param name="doc">xml document</param>
      /// <param name="parentNode">parent node to which new node is a child. If NULL, adds to root node</param>
      /// <param name="currNodeStr">the string for the new node</param>
      /// <param name="attrValue">the attribute for the new node</param>
      /// <returns></returns>
      public static XmlNode AddNode(XmlDocument doc, XmlNode parentNode, String currNodeStr, String attrValue)
      {
         if (parentNode == null)
            // get the root node
            parentNode = doc.DocumentElement;

         // create a new node
         XmlElement childNode = doc.CreateElement(currNodeStr);
         // add the new node as child to the parent node
         XmlElement nodeAdded = (XmlElement)parentNode.AppendChild(childNode);
         
         // add the attribute to the child node
         setAttribute(nodeAdded, "Value", attrValue);

         return nodeAdded;
      }

      /// <summary>sets attributes to a node</summary>
      /// <param name="currNode">node to add attribute to</param>
      /// <param name="attrName">attribute name</param>
      /// <param name="attrValue">attribute value</param>
      public static void setAttribute(XmlElement currNode, String attrName, String attrValue)
      {
         if (currNode != null && !String.IsNullOrEmpty(attrName))
            currNode.SetAttribute(attrName, attrValue);
      }

      /// <summary>removes the element</summary>
      /// <param name="node"></param>
      public static void removeElement(XmlDocument doc, XmlElement node)
      {
         // if the parent in not root node
         if (node != doc.DocumentElement)
         {
            // get parent node and remove the child from this parent node.
            XmlElement parentElement = (XmlElement)node.ParentNode;
            parentElement.RemoveChild(node);
         }
      }

      /// <summary>remove all elements that matches the tagName</summary>
      /// <param name="doc"></param>
      /// <param name="tagName"></param>
      public static void removeElements(XmlDocument doc, String tagName)
      {
         // get all node matching tagName
         List<XmlElement> matchList = getMatchingChildrens(doc.DocumentElement, tagName, null, null);

         // remove each node of this matched collection
         for (int i = 0; i < matchList.Count; i++)
            removeElement(doc, matchList[i]);
      }

      /// <summary> Gets the immediate childrens that matches the search attributes.
      /// 
      /// Note: Must have a tagName, but attrName and attrValue can be null.
      /// </summary>
      /// <param name="parentNode"></param>
      /// <param name="tagName"></param>
      /// <param name="attrName"></param>
      /// <param name="attrValue"></param>
      public static List<XmlElement> getMatchingChildrens(XmlElement parentNode, String tagName, String attrName, String attrValue)
      {
         List<XmlElement> matchList = new List<XmlElement>();

         // return if tagName is null
         if (tagName == null)
            return null;

         // get all elements that matches the search critera
         foreach (XmlElement currElement in parentNode.ChildNodes)
         {
            if (currElement.Name != tagName)
               continue;

            // check if null or empty attrname or the attrName in currEle matches to attrValue
            if (String.IsNullOrEmpty(attrName) || currElement.GetAttribute(attrName) == attrValue)
               matchList.Add(currElement);
         }

         return matchList;
      }
   }
}
