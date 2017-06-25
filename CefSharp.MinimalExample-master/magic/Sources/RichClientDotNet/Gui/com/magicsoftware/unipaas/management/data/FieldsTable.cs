using System;
using System.Collections.Generic;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using System.Diagnostics;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.data
{
   public abstract class FieldsTable
   {
      protected readonly List<FieldDef> _fields;

      /// <summary>
      /// </summary>
      public FieldsTable()
      {
         _fields = new List<FieldDef>();
      }

      /// <summary>
      ///   get a Field by its index
      /// </summary>
      /// <param name = "idx">the index of the requested field</param>
      /// <returns> a reference to the field</returns>
      public FieldDef getField(int idx)
      {
         FieldDef fld = null;
         if (idx >= 0 && idx < _fields.Count)
            fld = _fields[idx];
         return fld;
      }

      /// <summary>
      ///   return the number of fields in the table
      /// </summary>
      /// <returns> number of fields in the table</returns>
      public int getSize()
      {
         return _fields.Count;
      }

      /// <summary>
      ///   To parse input string and fill inner data: Vector of commands
      /// </summary>
      /// <param name = "dataview">to the parent</param>
      public void fillData(DataViewBase dataview)
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         while (initInnerObjects(parser.getNextTag(), dataview))
         {
         }
      }

      /// <summary>
      ///   To allocate and fill inner objects of the class
      /// </summary>
      /// <param name = "foundTagName">name of tag, of object, which need be allocated</param>
      /// <param name = "dataview">to data view</param>
      public bool initInnerObjects(String foundTagName, DataViewBase dataview)
      {
         if (foundTagName == null)
            return false;

         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;
         if (foundTagName.Equals(XMLConstants.MG_TAG_FLDH))
         {
            FieldDef field = initField(dataview);
            _fields.Add(field);
         }
         else if (foundTagName.Equals(XMLConstants.MG_TAG_DVHEADER))
         {
            parser.setCurrIndex(parser.getXMLdata().IndexOf(XMLConstants.TAG_CLOSE, parser.getCurrIndex()) +
                                1); // move Index to end of <dvheader> +1 for '>'
         }
         else if (foundTagName.Equals("/" + XMLConstants.MG_TAG_DVHEADER))
         {
            parser.setCurrIndex2EndOfTag();
            return false;
         }
         else
         {
            Events.WriteExceptionToLog("There is no such tag in FieldsTable. Insert else if to FieldsTable.initInnerObjects for " + foundTagName);
            return false;
         }

         return true;
      }

      /// <summary>
      /// create a field and fill it by data 
      /// this function must be virtual because it is used in RichClient but is not used in MgxpaRuntime
      /// </summary>
      /// <param name = "dataview"> Dataview </param>
      /// <returns> the created filled field </returns>
      protected abstract FieldDef initField(DataViewBase dataview);

      /// <summary>
      ///   get a Field by its index
      /// </summary>
      /// <param name = "fldName">is the name of the requested field</param>
      /// <returns> a reference to the field</returns>
      public FieldDef getField(String fldName)
      {
         String currName;
         foreach (var field in _fields)
         {
            currName = field.getVarName();
            if (currName.Equals(fldName))
               return field;
         }
         return null;
      }
   }
}
