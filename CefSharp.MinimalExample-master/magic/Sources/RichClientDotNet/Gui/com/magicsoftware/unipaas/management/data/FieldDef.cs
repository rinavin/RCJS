using System;
using System.Collections.Generic;
using System.Diagnostics;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.data
{
   public class FieldDef
   {
      #region initiatedFromServer
      

      public String DefaultValue { get; private set; }
      public bool NullAllowed { get; private set; }
      protected bool _nullDefault { get; private set; }
      protected String _nullValue { get; set; } //TODO: modified in com.magicsoftware.richclient.data.Field.getValueForNull 
      protected String _nullDisplay { get; set; } //TODO: modified in com.magicsoftware.unipaas.management.data.Field.setAfterParsing 
      public bool DbModifiable //true, if field is modifiable on field level
      {
         get { return _dbModifiable; }
         private set { _dbModifiable = value; }
      }

      /// <summary>
      /// fields for the .NET types
      /// </summary>
      String _dotNetType = null;
      int _assemblyId = 0;
      Type _dNType = null;
      bool _dotnetTypeLoaded = false;

      private bool _dbModifiable = true;
      private bool _partOfDataview = true;
      protected String _picture;
      protected int _size; //TODO: remove from com.magicsoftware.rte.data.Field.setAttribute and make private
      protected String _varName { get; private set; }
      private char _contentType = BlobType.CONTENT_TYPE_UNKNOWN; //Only for BLOB field. Does it contain a unicode or ANSI data?
      protected char _vecCellsContentType = BlobType.CONTENT_TYPE_UNKNOWN;
      protected int _vecCellsSize { get; private set; }
      protected StorageAttribute _vecCellsType { get; private set; } // !!!!! NOTICE : CAN BE OF TYPE MEMO !!!!
      protected StorageAttribute _type { get; private set; }

      public Type DNType
      {
         get
         {
            if (!_dotnetTypeLoaded)
            {
               _dotnetTypeLoaded = true;
               if (_dotNetType != null)
               {
                  _dNType = ReflectionServices.GetType(_assemblyId, _dotNetType);
                  Debug.Assert(_dNType != null, "Failed retrieving DotNet Type " + _dotNetType + " from assembly " + _assemblyId);
               }
            }
            return _dNType;
         }
      }
      public FldStorage Storage { get; private set; }
      public String VarDisplayName { get; private set; }

      

      #endregion

      #region initiatedByClient

      protected static String _default_date;
      protected readonly int _id;
      protected char[] _spaces;

      #endregion

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="id_"> idx in FieldsTable </param>
      public FieldDef(int id)
      {
         _id = id;
         VarDisplayName = String.Empty;
      }

      public bool PartOfDataview
      {
         get { return _partOfDataview; }
      }

      /// <summary>
      ///   returns the id of the field
      /// </summary>
      public int getId()
      {
         return _id;
      }

      /// <summary>
      ///   size type of current Field: A, N, L, D, T
      /// </summary>
      /// <returns> size member of current Field
      /// </returns>
      public StorageAttribute getType()
      {
         return _type;
      }

      /// <summary>
      ///   return the magic default null display value according to the type
      /// </summary>
      /// <param name = "type">of the data</param>
      public static String getMagicDefaultNullDisplayValue(StorageAttribute type)
      {
         String val = null;

         switch (type)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
            case StorageAttribute.BOOLEAN:
               val = "";
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               // zero
               val = "";
               break;
         }
         return val;
      }
         

      /// <summary>
      ///   return the magic default value according to the type
      /// </summary>
      /// <param name = "type">of the data</param>
      public static String getMagicDefaultValue(StorageAttribute type)
      {
         String val = null;

         switch (type)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               val = "";
               break;

            case StorageAttribute.BLOB_VECTOR:
            case StorageAttribute.BLOB:
               val = BlobType.getEmptyBlobPrefix(((char) 0)) + ";";
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.TIME:
               // zero
               val = "FF00000000000000000000000000000000000000";
               break;

            case StorageAttribute.DATE:
               val = _default_date;
               break;

            case StorageAttribute.BOOLEAN:
               val = "0";
               break;

            case StorageAttribute.DOTNET:
               val = BlobType.createDotNetBlobPrefix(0);
               break;
         }

         return val;
      }

      /// <summary>
      ///   returns the default value of this field
      /// </summary>
      public String getDefaultValue()
      {
         String val = null;
         if (_type != StorageAttribute.BLOB_VECTOR)
         {
            if (_nullDefault && _nullValue != null)
               val = _nullValue;
            else if (DefaultValue != null)
               val = DefaultValue;
            else
               val = getMagicDefaultValue();
         }
         else
            val = getMagicDefaultValue();

         return val;
      }

      /// <summary>
      ///   returns the vectors cells default value
      ///   if the field is not a vector returns field default value
      /// </summary>
      public String getCellDefualtValue()
      {
         String val = null;
         if (_type == StorageAttribute.BLOB_VECTOR)
         {
            if (_nullDefault && _nullValue != null)
               val = _nullValue;
            else if (DefaultValue != null)
               val = DefaultValue;
            else
            {
               val = getMagicDefaultValue(_vecCellsType);
               if (_vecCellsType == StorageAttribute.BLOB)
                  val = BlobType.setContentType(val, _vecCellsContentType);
            }
         }
         else
            val = getDefaultValue();

         return val;
      }

      /// <summary>
      ///   return the magic default value according to the type
      /// </summary>
      public String getMagicDefaultValue()
      {
         String val = getMagicDefaultValue(_type);

         if (_type == StorageAttribute.BLOB)
            val = BlobType.setContentType(val, _contentType);
         else if (_type == StorageAttribute.BLOB_VECTOR)
         {
            val = BlobType.SetVecCellAttr(val, _vecCellsType);
            if (_vecCellsType == StorageAttribute.BLOB)
               val = BlobType.setContentType(val, _vecCellsContentType);
 
         }

         return val;
      }

      /// <summary>
      /// </summary>
      /// <returns> the contentType </returns>
      public char getContentType()
      {
         return _contentType;
      }

      /// <summary>
      ///   returns true if the nullDisplay has a value
      /// </summary>
      public bool hasNullDisplayValue()
      {
         return (_nullDisplay != null);
      }

      /// <summary>
      ///   size member of current Field
      /// </summary>
      /// <returns> size member of current Field </returns>
      public int getSize()
      {
         return _size;
      }

      /// <summary>
      ///   returns the vector cells size
      /// </summary>
      protected internal int getVecCellsSize()
      {
         return _type == StorageAttribute.BLOB_VECTOR
                   ? _vecCellsSize
                   : _size;
      }

      /// <summary>
      /// </summary>
      protected internal void setLengths(StorageAttribute type, bool vecCells)
      {
         int res = _size;
         switch (type)
         {
            case StorageAttribute.ALPHA:
            case StorageAttribute.MEMO:
            case StorageAttribute.UNICODE:
               if (vecCells)
               {
                  res = _vecCellsSize;
                  _spaces = new char[_vecCellsSize];
               }
               else
                  _spaces = new char[_size];

               for (int i = 0;
                    i < _size;
                    i++)
                  _spaces[i] = ' ';
               break;

            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               res = Manager.Environment.GetSignificantNumSize()*2;
               break;

            case StorageAttribute.BOOLEAN:
               res = (!vecCells
                         ? 1
                         : 4);
               break;

            case StorageAttribute.BLOB:
            case StorageAttribute.DOTNET:
               //QCR 758913 FOR VECTORS INTERNAL USE ONLY THE SIZE OF BLOBS AND VECTOR 
               //IS THE SIZE OF THIER STRUCTS IN THE MAGIC ENGIN
               res = !vecCells
                        ? Int32.MaxValue
                        : VectorType.BLOB_TYPE_STRUCT_SIZE;
               break;

            case StorageAttribute.BLOB_VECTOR:
               res = Int32.MaxValue;
               if (!vecCells)
                  setLengths(_vecCellsType, true);
               else
                  res = VectorType.BLOB_TYPE_STRUCT_SIZE; // sizeof(BLOB_TYPE)
               break;
         }

         if (vecCells)
            _vecCellsSize = res;
         else
            _size = res;
      }

      /// <summary>
      ///   returns the cells type of vectors elese returns the field type
      /// </summary>
      public StorageAttribute getCellsType()
      {
         return _type == StorageAttribute.BLOB_VECTOR
                   ? _vecCellsType
                   : _type;
      }

      /// <summary>
      ///   returns the cell content type of vector field
      /// </summary>
      /// <returns>vector's cell content type</returns>
      protected internal char getVecCellsContentType()
      {
         return _vecCellsContentType;
      }

      /// <summary>
      ///   returns Null Value value of this field
      /// </summary>
      public String getNullValue()
      {
         return _nullValue;
      }

      /// <summary>
      ///   returns true if the field is a Null Default
      /// </summary>
      public bool isNullDefault()
      {
         return _nullDefault;
      }

      /// <summary>
      ///   returns Null Display value of this field
      /// </summary>
      public String getNullDisplay()
      {
         return Events.Translate(_nullDisplay);
      }

      /// <summary>
      ///   For BLOB fields, check if they contain UNICODE or ANSI chars.
      /// </summary>
      /// <returns>TRUE if content is UNICODE. False otherwise</returns>
      public bool IsContentUnicode()
      {
         if (_type == StorageAttribute.BLOB && _contentType != BlobType.CONTENT_TYPE_UNICODE)
            return false;

         return true;
      }

      public string GetPicture()
      {
         return _picture;
      }

      /// <summary>
      /// set the field attribute in parsing
      /// </summary>
      protected virtual bool setAttribute(string attribute, string valueStr)
      {
         bool isTagProcessed = true;

         switch (attribute)
         {
            case XMLConstants.MG_ATTR_TYPE:
               _type = (StorageAttribute) valueStr[0];
               break;

            case XMLConstants.MG_ATTR_SIZE:
               _size = XmlParser.getInt(valueStr);
               if (_size <= 0)
                  Events.WriteExceptionToLog("in Field.initElements(): size must be greater than zero");
               break;

            case XMLConstants.MG_ATTR_VAR_NAME:
               _varName = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_VAR_DISP_NAME:
               VarDisplayName = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_PICTURE:
               _picture = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_VEC_CELLS_SIZE:
               _vecCellsSize = Int32.Parse(valueStr);
               break;

            case XMLConstants.MG_ATTR_VEC_CELLS_ATTR:
               _vecCellsType = (StorageAttribute) valueStr[0];
               break;

            case XMLConstants.MG_ATTR_VEC_CELLS_CONTENT:
               _vecCellsContentType = valueStr[0];
               break;

            case XMLConstants.MG_ATTR_NULLVALUE:
               if (_type == StorageAttribute.NUMERIC || _type == StorageAttribute.DATE ||
                   _type == StorageAttribute.TIME)
               {
                  //working in hex or base64
                  if (Manager.Environment.GetDebugLevel() > 1)
                     _nullValue = XmlParser.unescape(valueStr);
                  else
                     _nullValue = Base64.decodeToHex(valueStr);
               }
               else
                  _nullValue = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_NULLDISPLAY:
               _nullDisplay = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_NULLDEFAULT:
               _nullDefault = DisplayConvertor.toBoolean(valueStr);
               break;

            case XMLConstants.MG_ATTR_DB_MODIFIABLE:
               DbModifiable = DisplayConvertor.toBoolean(valueStr);
               break;

            case XMLConstants.MG_ATTR_DEFAULTVALUE:
               DefaultValue = valueStr;
               if (_type == StorageAttribute.ALPHA || _type == StorageAttribute.UNICODE)
               {
                  DefaultValue = XmlParser.unescape(valueStr);
                  DefaultValue = StrUtil.padStr(DefaultValue, _size);
               }
               else if (_type != StorageAttribute.BLOB && _type != StorageAttribute.BOOLEAN)
               //working in hex or base64
               {
                  if ((_type == StorageAttribute.BLOB_VECTOR &&
                       (_vecCellsType == StorageAttribute.NUMERIC ||
                        _vecCellsType == StorageAttribute.DATE ||
                        _vecCellsType == StorageAttribute.TIME)) ||
                      (_type == StorageAttribute.NUMERIC || _type == StorageAttribute.DATE ||
                       _type == StorageAttribute.TIME))
                  {
                     if (Manager.Environment.GetDebugLevel() < 1)
                     {
                        DefaultValue = Base64.decodeToHex(valueStr);
                     }
                  }
               }
               else if (DefaultValue.Length == 0 && _type != StorageAttribute.BLOB &&
                        _type != StorageAttribute.BLOB_VECTOR)
                  DefaultValue = null;
               else if (_type == StorageAttribute.BLOB)
                  DefaultValue = BlobType.createFromString(DefaultValue, _contentType);
               break;

            case XMLConstants.MG_ATTR_NULLALLOWED:
               NullAllowed = DisplayConvertor.toBoolean(valueStr);
               break;

            case XMLConstants.MG_ATTR_BLOB_CONTENT:
               _contentType = valueStr[0];
               break;

            case XMLConstants.MG_ATTR_PART_OF_DATAVIEW:
               _partOfDataview = DisplayConvertor.toBoolean(valueStr);
               break;

            case XMLConstants.MG_ATTR_DOTNET_TYPE:
               _dotNetType = XmlParser.unescape(valueStr);
               break;

            case XMLConstants.MG_ATTR_DOTNET_ASSEMBLY_ID:
               _assemblyId = XmlParser.getInt(valueStr);
               break;

            case XMLConstants.MG_ATTR_STORAGE:
               Storage = (FldStorage)XmlParser.getInt(valueStr);
               break;

            default:
               isTagProcessed = false;
               break;
         }

         return isTagProcessed;
      }

      /// <summary>
      ///   Need part input String to relevant for the Field class data
      /// </summary>
      public void fillData()
      {
         XmlParser parser = Manager.GetCurrentRuntimeContext().Parser;

         string buffer = parser.ReadToEndOfCurrentElement();
         buffer = buffer.Substring(buffer.IndexOf(XMLConstants.MG_TAG_FLDH) + XMLConstants.MG_TAG_FLDH.Length);
         List<string> tokensVector = XmlParser.getTokens(buffer, XMLConstants.XML_ATTR_DELIM);
         initElements(tokensVector);
      }

      /// <summary>
      ///   Make initialization of private elements by found tokens
      /// </summary>
      /// <param name = "tokensVector">found tokens, which consist attribute/value of every foundelement</param>
      /// <param name = "expTab">reference to relevant exp. table</param>
      public virtual void initElements(List<String> tokensVector)
      {
        

         for (int j = 0;
              j < tokensVector.Count;
              j += 2)
         {
            String attribute = (tokensVector[j]);
            String valueStr = (tokensVector[j + 1]);

            setAttribute(attribute, valueStr);
         }


         // the parsing is finished here
         SetAfterParsing();
      }

      /// <summary>
      /// some properties are needed to be set after parsing
      /// </summary>
      public void SetAfterParsing()
      {
         setLengths(_type, false);
         if (NullAllowed && _nullDisplay == null)
            _nullDisplay = "";
      }

      /// <summary>
      ///   get name of variable
      /// </summary>
      public String getVarName()
      {
         return _varName ?? "";
      }

      /// <summary>
      ///   for get VARNAME function use
      ///   A string containing the table name where the variable originates,
      ///   concatenated with '.' and the variable description of the variable in that table.
      ///   If the variable is a virtual one, then the table name would indicate 'Virtual'.
      /// </summary>
      public virtual String getName()
      {
         return getVarName();
      }
   }
}
