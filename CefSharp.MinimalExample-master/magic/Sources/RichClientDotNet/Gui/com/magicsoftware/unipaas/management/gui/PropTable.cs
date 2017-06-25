using System;
using System.Collections;
using System.Collections.Generic;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.unipaas.management.tasks;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.gui
{
   /// <summary>
   ///   properties table
   /// </summary>
   public class PropTable
   {
      private readonly Hashtable _hashTab; // for fast access to properties
      private readonly List<Property> _props; // of Property
      private PropParentInterface _parent;

      /// <summary>
      ///   CTOR
      /// </summary>
      public PropTable()
      {
         _props = new List<Property>();
         _hashTab = new Hashtable(20);
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      public PropTable(PropParentInterface parent_)
         : this()
      {
         _parent = parent_;
      }

      /// <summary>
      ///   parse the properties
      /// </summary>
      /// <param name = "parentObj">reference to parent object(TaskBase)</param>
      public void fillData(PropParentInterface parentObj, char parType)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         while (initInnerObjects(parser.getNextTag(), parentObj, parType))
         {
         }
      }

      /// <summary>
      ///   For recompute: Parse the existing properties and create references to them
      /// </summary>
      /// <param name = "task">reference to the parent TaskBase</param>
      public void fillDataByExists(TaskBase task)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         while (fillExistInnerObjects(parser.getNextTag(), task))
         {
         }
      }

      /// <summary>
      ///   fill the inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">name of tag, of object, which need be allocated</param>
      /// <param name = "parentObj">reference to the parent object, need be added to every Property</param>
      /// <returns> Manager.XmlParser.getCurrIndex(), the found object tag and ALL its subtags finish</returns>
      private bool initInnerObjects(String foundTagName, PropParentInterface parentObj, char parType)
      {
         if (foundTagName != null && foundTagName.Equals(XMLConstants.MG_TAG_PROP))
         {
            Property property = new Property();
            property.fillData(parentObj, parType);
            addProp(property, true);
         }
         else
            return false;
         return true;
      }

      /// <summary>
      ///   if the property doesn't exist in the table then add it, otherwise change it
      /// </summary>
      /// <param name = "prop">the property to add</param>
      /// <param name = "checkExistence">if true then the property will be added if it is not already exists if false then no existence is checked and the property will not be
      ///   added to the hashtable (use it when many instances of the same property are allowed)
      /// </param>
      public void addProp(Property prop, bool checkExistence)
      {
         if (checkExistence)
         {
            Property existingProp = (Property) _hashTab[prop.getID()];
            if (existingProp == null)
               addProp(prop, prop.getID());
            else
               existingProp.setValue(prop.getValue());
         }
         else
            addProp(prop, prop.getID());
      }

      /// <summary>
      ///   adding property by key
      /// </summary>
      /// <param name = "prop"></param>
      /// <param name = "key"></param>
      private void addProp(Property prop, Int32 key)
      {
         _hashTab[key] = prop;
         _props.Add(prop);
      }

      /// <param name = "id"></param>
      public void delPropById(int id)
      {
         if (_props == null)
            return;

         Property existingProp = (Property) _hashTab[id];
         if (existingProp != null)
         {
            _props.Remove(existingProp);
            _hashTab.Remove(id);
         }
      }

      /// <summary>
      ///   For Recompute: Fill PropTable with reference to existing PropTable
      /// </summary>
      /// <param name = "task">reference to parent TaskBase</param>
      private bool fillExistInnerObjects(String nameOfFound, TaskBase task)
      {
         List<String> tokensVector;
         int endContext = -1;
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;

         if (nameOfFound == null)
            return false;

         if (nameOfFound.Equals(XMLConstants.MG_TAG_CONTROL))
            endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex());
         else if (nameOfFound.Equals(XMLConstants.MG_TAG_PROP))
            endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         else if (nameOfFound.Equals('/' + XMLConstants.MG_TAG_CONTROL) ||
                  MgFormBase.IsEndFormTag(nameOfFound))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }

         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(nameOfFound) + nameOfFound.Length);
            tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            if (nameOfFound.Equals(XMLConstants.MG_TAG_CONTROL))
            {
               // ditidx of Exists Control
               _parent = task.getCtrl(Int32.Parse(fillName(tokensVector)));
               parser.setCurrIndex(++endContext); // to delete ">"
               return true;
            }
            else if (nameOfFound.Equals(XMLConstants.MG_TAG_PROP))
            {
               if (_parent != null)
               {
                  String strPropId = fillName(tokensVector);
                  int propIndex = Int32.Parse(strPropId);
                  Property prop = null;
                  if (_parent != null)
                     prop = _parent.getProp(propIndex);

                  if (prop == null)
                     Events.WriteExceptionToLog(string.Format("in PropTable.fillExistInnerObjects() no property with id={0}", strPropId));
                  else
                     addProp(prop, false);
                  parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length);
                  return true;
               }
               else
                  Events.WriteExceptionToLog("in PropTable.fillExistInnerObjects() missing control");
            }
            else
            {
               Events.WriteExceptionToLog(string.Format("in PropTable.fillExistInnerObjects() illegal tag name: {0}", nameOfFound));
               parser.setCurrIndex(++endContext);
               return true;
            }
         }

         if (nameOfFound.Equals(XMLConstants.MG_TAG_CONTROL))
         {
            parser.setCurrIndex(++endContext);
            return true;
         }
         else if (MgFormBase.IsFormTag(nameOfFound))
         {
            _parent = task.getForm();
            ((MgFormBase)_parent).fillName(nameOfFound);
            return true;
         }
         else if (nameOfFound.Equals('/' + XMLConstants.MG_TAG_FLD))
            return false;

         parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // exit of bounds
         return true;
      }

      /// <summary>
      ///   Find the name of an existing Control, or of an existing Property
      /// </summary>
      /// <returns> Name of control</returns>
      private String fillName(List<String> tokensVector)
      {
         String attribute = null;
         String valueStr = null;

         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            attribute = (tokensVector[j]);
            valueStr = (tokensVector[j + 1]);

            if (attribute.Equals(XMLConstants.MG_ATTR_DITIDX) || attribute.Equals(XMLConstants.MG_ATTR_ID))
               return valueStr;
         }
         Events.WriteExceptionToLog(string.Format("Unrecognized attribute: '{0}'", attribute));
         return null;
      }

      /// <summary>
      ///   get size of the vector props
      /// </summary>
      /// <returns> size of the props member</returns>
      public int getSize()
      {
         return _props.Count;
      }

      /// <summary>
      ///   get a Property by its index in the table
      /// </summary>
      /// <param name = "idx">the index of the property</param>
      /// <returns> reference to the required Property</returns>
      public Property getProp(int idx)
      {
         if (idx < 0 || idx >= _props.Count)
            return null;
         return _props[idx];
      }

      /// <summary>
      ///   get a Property by its id
      /// </summary>
      /// <param name = "id">of the property</param>
      public Property getPropById(int id)
      {
         return (Property) _hashTab[id];
      }

      public bool propExists(int id)
      {
         if (getPropById(id) == null)
            return false;
         return true;
      }

      /// <summary>
      ///   set a property value. in case the table doesn't contain such property, creates a new Property object and add it to the table.
      /// </summary>
      /// <param name = "propId">the id of the property</param>
      /// <param name = "val">the value of the property</param>
      /// <param name = "parent">a reference to the parent object</param>
      /// <param name = "parentType">the type of the parent object</param>
      public void setProp(int propId, String val, PropParentInterface parent, char parentType)
      {
         Property prop = getPropById(propId);

         if (prop == null)
         {
            prop = new Property(propId, parent, parentType);
            addProp(prop, true);
         }
         prop.setValue(val);
         prop.setOrgValue();
      }

      /// <summary>
      ///   refresh display of the properties in the Table
      /// </summary>
      /// <param name = "forceRefresh">if true, refresh is forced regardless of the previous value</param>
      /// <param name = "onlyRepeatableProps">if true, refreshes only repeatable tree properties</param>
      public bool RefreshDisplay(bool forceRefresh, bool onlyRepeatableProps)
      {
         bool allPropsRefreshed = true;
         Property prop;
         int i;
         MgFormBase form = null;
         ArrayList visibleProps = new ArrayList();

         if (_parent != null && _parent is MgControlBase)
            form = ((MgControlBase) _parent).getForm();

         if (form != null)
            form.checkAndCreateRow(form.DisplayLine);

         for (i = 0; i < getSize(); i++)
         {
            prop = getProp(i);
            try
            {
               //if this property is not repeatable in tree or table, and onlyRepeatableProps turned on
               //do not refresh this property
               if (onlyRepeatableProps && !Property.isRepeatableInTree(prop.getID()) && !Property.isRepeatableInTable(prop.getID()))
                  continue;

               // Refresh Visible prop only after refreshing the navigation props like X, Y.
               // Otherwise, the control is shown on its default location first and 
               // then moved somewhere else. And this is clearly visible.
               if (prop.getID() == PropInterface.PROP_TYPE_VISIBLE)
                  visibleProps.Add(prop);
               else
                  prop.RefreshDisplay(forceRefresh);
            }
            catch (ApplicationException ex)
            {
               Events.WriteExceptionToLog(ex);
               allPropsRefreshed = false;
            }
         }
         if (form != null)
            form.validateRow(form.DisplayLine);

         foreach (Property visibleProp in visibleProps)
         {
            visibleProp.RefreshDisplay(forceRefresh);
         }

         return allPropsRefreshed;
      }

      /// <summary>
      ///   update all properties array size
      /// </summary>
      /// <param name = "newSize"></param>
      public void updatePrevValueArray(int newSize)
      {
         for (int i = 0; i < _props.Count; i++)
         {
            Property prop = _props[i];
            prop.updatePrevValueArray(newSize);
         }
      }

      /// <summary>
      /// clear prevValue array of Label property if exists
      /// </summary>
      public void clearLabelPrevValueArray()
      {
         Property labelProperty = getPropById(PropInterface.PROP_TYPE_LABEL);
         if (labelProperty != null)
            labelProperty.clearPrevValueArray();
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      public void RemoveAt(int idx)
      {
         for (int i = 0; i < _props.Count; i++)
         {
            Property prop = _props[i];
            prop.RemoveAt(idx);
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="idx"></param>
      public void InsertAt(int idx)
      {
         for (int i = 0; i < _props.Count; i++)
         {
            Property prop = _props[i];
            prop.InsertAt(idx);
         }
      }

      /// <summary>
      ///   returns control
      /// </summary>
      /// <returns></returns>
      public MgControlBase getCtrlRef()
      {
         return (_parent as MgControlBase);
      }
   }
}
