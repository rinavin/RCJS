using System;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.richclient.exp;
using com.magicsoftware.richclient.rt;
using com.magicsoftware.richclient.tasks;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.management.exp;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util;
using com.magicsoftware.richclient.gui;
using util.com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.data
{
   /// <summary>
   ///   this class represents a record object
   ///   it parses the <rec...> tag
   /// </summary>
   internal class Record : IComparable, IRecord
   {
      // ATTENTION !!! when you add/modify/delete a member variable don't forget to take care
      // about the "setSameAs()" function

      /// <summary>
      ///   CONSTANTS
      /// </summary>
      internal const byte FLAG_NULL = (0x01);

      internal const byte FLAG_INVALID = (0x02);
      internal const byte FLAG_MODIFIED = (0x04);
      internal const byte FLAG_UPDATED = (0x08);
      internal const byte FLAG_CRSR_MODIFIED = (0x10);
      internal const byte FLAG_VALUE_NOT_PASSED = (0x20);
      internal const byte FLAG_MODIFIED_ATLEAST_ONCE = (0x40);

      #region initiatedFromServer
      protected int _id = Int32.MinValue;
      protected int dbViewRowIdx = 0;
      protected DataModificationTypes _mode = DataModificationTypes.None; // Created|Modified|Deleted
      #endregion

      #region initiatedByClient
      //private MgArrayList _dcRefs;
      ObjectReferencesCollection _dcRefs;
      protected bool _inCompute;
      protected bool _inRecompute;
      protected bool _newRec;
      #endregion

      internal bool InCompute
      {
         get { return _inCompute; }
      }

      internal bool InRecompute
      {
         get { return _inRecompute; }
      }

      protected internal const bool INCREASE = true;
      protected internal const bool DECREASE = false;

      protected readonly TableCache _tableCache; // }
                                              // } for use if the record is part of table cache (there is no way both dv and tablecache are not null at the same time - 

      protected bool _inDeleteProcess; // will be set to true before rec suffix from delline.

      protected int _addAfter;
      protected bool _causeInvalidation; //Exit from this record will cause invalidation of all rows in table
      protected String _dbPosBase64Val; //used only in table cache records holds the tables db_pos value in the cursor
      protected DataView _dataview;           // }                                               they are Mutually exclusive).
      private String[] _fieldsData;
      protected byte[] _flags;
      protected byte[] _flagsHistory; // History of flags, used when contacting the server
      protected bool _forceSaveOrg;
      protected Int32 _hashKey;

      protected bool _computed;
      protected bool _lateCompute; //this record must be computed next time we enter the record, but not before 

      protected String _linksFldsPos;
      protected bool _modified;

      protected Record _next; // Points to the next record, when its part of a list
      protected Record _prev; // Points to the prev record, when its part of a list
      protected bool _sendToServer = true; // If false, don't send this record to the server, as part of the modified list 
      protected bool _updated;

      /// <summary>
      ///   constructs a new record to be used by the table cache only!!!
      /// </summary>
      internal Record(object dvOrTableCache)
      {
         if (dvOrTableCache is DataView)
            _dataview = (DataView) dvOrTableCache;
         else
            _tableCache = (TableCache) dvOrTableCache;
         _fieldsData = new String[getFieldsTab().getSize()];
         _flags = new byte[getFieldsTab().getSize()];
         _flagsHistory = new byte[getFieldsTab().getSize()];

         for (int i = 0; i < _flags.Length; i++)
         {
            FieldDef fielddef = getFieldsTab().getField(i);
            _flags[i] = fielddef.isNullDefault() || fielddef.getType() == StorageAttribute.BLOB_VECTOR
                          ? FLAG_NULL
                          : (byte) 0;
            
            if (_dataview != null && ((Field)fielddef).IsVirtual)
            {
               Record record = _dataview.getCurrRec();
               if (record != null && record.IsFldModifiedAtLeastOnce(i))
                  setFlag(i, FLAG_MODIFIED_ATLEAST_ONCE);
            }
            
            _flagsHistory[i] = _flags[i];
            //         flags[i] |= getFieldsTab().getField(i).getFlag(Field.FLAG_VALID) ? Field.FLAG_VALID : '\0' ;
         }

         _dcRefs = new ObjectReferencesCollection();
         setNewRec();
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "cId">the id of the record</param>
      /// <param name = "ft">a reference to the fields table</param>
      /// <param>  clobberedOnly is true if we want to init only the values of the virtuals with no init expression </param>
      protected internal Record(int cId, DataView dataview, bool clobberedOnly)
         : this(cId, dataview, clobberedOnly, false)  { }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "cId">the id of the record</param>
      /// <param name = "ft">a reference to the fields table</param>
      /// <param>  clobberedOnly is true if we want to init only the values of the virtuals with no init expression </param>
      protected internal Record(int cId, DataView dataview, bool clobberedOnly, bool isFirstRecord)
         : this(dataview)
      {
         setId(cId);
         setInitialFldVals(clobberedOnly, !isFirstRecord);
      }

      internal bool Modified
      {
         get { return _modified; }
      }

      internal bool Updated
      {
         get { return _updated; }
      }

      protected internal bool InForceUpdate { set; get; }

      internal bool SendToServer
      {
         get { return _sendToServer; }
      }

      internal bool Synced { get; set; }

      internal bool ValueInBase64 { get; set; }

      #region IComparable Members

      public int CompareTo(object obj)
      {
         int res = 0;
         var compTo = (Record) obj;

         //get the key by which we are doing the Comparison
         Key key = _tableCache.GetKeyById(_tableCache.GetCurrSortKey());

         //if there is no key to compare by then the records are uncomparable
         if (key != null)
         {
            //all we need from the key in the order of comparing the the record fields
            for (int i = 0; i < key.Columns.Count; i++)
            {
               var currFld = key.Columns[i];
               int currFldId = currFld.getId();

               if (IsNull(currFldId) && compTo.IsNull(currFldId))
                  continue;
               else if (IsNull(currFldId) && !compTo.IsNull(currFldId))
               {
                  //null is the greatest value therefore we are the greater
                  res = 1;
                  break;
               }
               else if (!IsNull(currFldId) && compTo.IsNull(currFldId))
               {
                  res = -1;
                  break;
               }
                  //both are not bull
               else
               {
                  try
                  {
                     var first = new GuiExpressionEvaluator.ExpVal(currFld.getType(), false,
                                                                   GetFieldValue(currFldId));
                     var second = new GuiExpressionEvaluator.ExpVal(currFld.getType(), false,
                                                                    compTo.GetFieldValue(currFldId));

                     res = ExpressionEvaluator.val_cmp_any(first, second, false);

                     if (res != 0)
                        break;
                  }
                  catch (ExpressionEvaluator.NullValueException)
                     //one of the values we are comparing using vl_cmp_any  is null we should never get here
                  {
                     Logger.Instance.WriteExceptionToLog(" in Record.CompareTo null value was reached");
                  }
               }
            }

            return res;
         }
         else
         {
            //TODO: throw exception for no key
            //   throw new ArgumentException(" un comparable records - there is not key to compare by"); 
            res = -1;
            return res;
         }
      }

      #endregion

      /// <summary>
      ///   get the mode
      /// </summary>
      public DataModificationTypes getMode()
      {
         return _mode;
      }

      /// <summary>
      ///   get the record id
      /// </summary>
      public int getId()
      {
         return _id;
      }

      /// <summary>
      ///   returns true if this is a new record just created and no record suffix was yet
      ///   processed on it
      /// </summary>
      internal bool isNewRec()
      {
         return _newRec;
      }

      /// <summary>
      ///   build the field value for the XML string of this record
      /// </summary>
      /// <param name = "fldIdx">index of field</param>
      /// <returns></returns>
      internal String getFieldDataXML(int fldIdx, bool getOldVal)
      {
         String fldVal;

         if (!getOldVal)
            fldVal = GetFieldValue(fldIdx);
         else
            fldVal = ((Field) getFieldsTab().getField(fldIdx)).getOriginalValue();
         if (fldVal == null)
            throw new ApplicationException("in Record.buildFieldsData() null field value!\nField id: " + fldIdx);

         String tmpBuf = getXMLForValue(fldIdx, fldVal);
         return XmlParser.escape(tmpBuf.ToString());
      }

      /// <summary>
      ///   build the field value for the XML string of this record
      /// </summary>
      /// <param name = "fldIdx">index of field</param>
      /// <returns></returns>
      internal byte [] getFieldDataXML(int fldIdx)
      {
         String fldVal;

         fldVal = GetFieldValue(fldIdx);
         
         if (fldVal == null)
            throw new ApplicationException("in Record.buildFieldsData() null field value!\nField id: " + fldIdx);

         StorageAttribute fldAttr = getFieldsTab().getType(fldIdx); // field's attribute

         return RecordUtils.serializeItemVal(fldVal, fldAttr);
      }

      /// <summary>
      /// get field table either of DataView ('dvHeader') or of TableCache
      /// dv and tableCache are mutually exclusive
      /// </summary>
      /// <returns> fieldTable </returns>
      internal FieldsTable getFieldsTab()
      {
         if (_dataview != null)
            return (FieldsTable)_dataview.GetFieldsTab();
         else
            return _tableCache.FldsTab;
      }

      /// <summary>
      ///   finalizer method: removes this record from the dc counters
      /// </summary>
      ~Record()
      {
         removeRecFromDc();
         //UPGRADE_NOTE: Call to 'super.finalize()' was removed. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1124'"
      }

      /// <summary>
      ///   set the initial field values for fields that has a null value
      /// </summary>
      /// <param>  clobberedOnly is true if we want to init only the values of the virtuals with no init expression </param>
      protected void setInitialFldVals(bool clobberedOnly, bool isNewRec)
      {
         int i, size;
         Field fld = null;

         size = getSizeFld(true);

         for (i = 0; i < size; i++)
         {
            // put initial values to the fields using the Magic default value
            if (_fieldsData[i] == null)
            {
               fld = (Field) getFieldsTab().getField(i);
               _fieldsData[i] = fld.getNewRecValue(clobberedOnly);

               if (isNewRec && fld.IsVirtual && !fld.isNull())
                  clearFlag(fld.getId(), Record.FLAG_NULL);

               if (fld.IsVirtual && fld.getModifiedAtLeastOnce())
                  setFlag(fld.getId(), Record.FLAG_MODIFIED_ATLEAST_ONCE);

               if (_fieldsData[i] == null)
                  _fieldsData[i] = fld.getDefaultValue();
            }
         }
      }

      /// <summary>
      ///   sets the id of the record and an appropriate hash key
      /// </summary>
      /// <param name = "cId">the id of the record</param>
      internal void setId(int cId)
      {
         _id = cId;
         _hashKey = _id;
      }

      /// <summary>
      /// sets the dbViewRowId of the record 
      /// </summary>
      /// <param name = "cId">the id of the record</param>
      internal void setDBViewRowIdx(int rowId)
      {
         dbViewRowIdx = rowId;
      }

      /// <summary>
      /// return dbViewRowId of the record 
      /// </summary>
      /// <param name = "cId">the id of the record</param>
      internal int getDBViewRowIdx()
      {
         return dbViewRowIdx;
      }
      
      /// <summary>
      ///   parse input string and fill inner data - returns true when the record is the current record
      /// </summary>
      internal bool fillData()
      {
         XmlParser parser = ClientManager.Instance.RuntimeCtx.Parser;
         bool isCurrRec = false;
         int endContext = parser.getXMLdata().IndexOf(XMLConstants.TAG_TERM, parser.getCurrIndex());
         if (endContext != -1 && endContext < parser.getXMLdata().Length)
         {
            // last position of its tag
            String tag = parser.getXMLsubstring(endContext);
            parser.add2CurrIndex(tag.IndexOf(ConstInterface.MG_TAG_REC) + ConstInterface.MG_TAG_REC.Length);

            List<string> tokensVector = XmlParser.getTokens(parser.getXMLsubstring(endContext), XMLConstants.XML_ATTR_DELIM);
            isCurrRec = initElements(tokensVector);
            parser.setCurrIndex(endContext + XMLConstants.TAG_TERM.Length); // to delete ">" too
         }
         else
            Logger.Instance.WriteExceptionToLog("in Record.FillInnerData() out of bounds");
         return isCurrRec;
      }

      /// <summary>
      ///   parse the record - returns true when the record is the current record
      /// </summary>
      /// <param name = "tokensVector">the attributes and their values</param>
      protected bool initElements(List<String> tokensVector)
      {
         String attribute, valueStr;
         String recFieldsData = null;
         String recFlags = null;
         int j;
         bool isCurrRec;

         isCurrRec = peekIsCurrRec(tokensVector);

         for (j = 0; j < tokensVector.Count; j += 2)
         {
            attribute = (tokensVector[j]);
            valueStr = (tokensVector[j + 1]);

            switch (attribute)
            {
               case ConstInterface.MG_ATTR_MODE:
                  _mode = (DataModificationTypes)valueStr[0];
                  break;
               case XMLConstants.MG_ATTR_ID:
                  setId(XmlParser.getInt(valueStr));
                  break;
               case XMLConstants.MG_ATTR_VB_VIEW_ROWIDX:
                  setDBViewRowIdx(XmlParser.getInt(valueStr));
                  break;
               case XMLConstants.MG_ATTR_VALUE:
                  if (ValueInBase64)
                  {
                     recFieldsData = Base64.decode(valueStr);
                  }
                  else
                  {
                     recFieldsData = XmlParser.unescape(valueStr);
                  }
                  break;
               case ConstInterface.MG_ATTR_CURR_REC:
                  /* handled already by the peekIsCurrRec() */
                  {
                  }
                  break;
               case ConstInterface.MG_ATTR_ADD_AFTER:
                  _addAfter = XmlParser.getInt(valueStr);
                  break;
               case ConstInterface.MG_ATTR_FLAGS:
                  recFlags = valueStr;
                  break;
               case ConstInterface.MG_ATTR_MODIFIED:
                  _modified = true;
                  break;
               case ConstInterface.MG_ATTR_DC_REFS:
                  fillDCRef(valueStr);
                  break;
               case ConstInterface.MG_ATTR_DBPOS:
                  _dbPosBase64Val = valueStr;
                  break;
               default:
                  Logger.Instance.WriteExceptionToLog("in Record.initElements() unknown attribute: " + attribute);
                  break;
            }
         }

         if (ValueInBase64)
         {
            byte[] fldValInBytes = ISO_8859_1_Encoding.getInstance().GetBytes(recFieldsData);
            fillFieldsData(fldValInBytes, recFlags);
         }
         else
         {
            fillFieldsData(recFieldsData, recFlags, isCurrRec);
         }

         return isCurrRec;
      }

      /// <summary>
      ///   peek the curr_rec value from the tag
      /// </summary>
      /// <param name = "the">tokens of the attributes and values in the tag</param>
      protected bool peekIsCurrRec(List<String> tokensVector)
      {
         String attribute, valueStr;

         for (int j = 0; j < tokensVector.Count; j += 2)
         {
            attribute = (tokensVector[j]);
            if (attribute.Equals(ConstInterface.MG_ATTR_CURR_REC))
            {
               valueStr = (tokensVector[j + 1]);
               return XmlParser.getBoolean(valueStr);
            }
         }
         return false;
      }

      protected virtual void fillFieldsData(byte[] fldValInBytes, String recFlags)
      {
      }
      /// <summary>
      ///   Fill FieldsData
      /// </summary>
      /// <param name = "fldsVal">- string for parsing</param>
      /// <param name = "isCurrRec">true if this record is the current record</param>
      private void fillFieldsData(String fldsVal, String recFlags, bool isCurrRec)
      {
         String val = null;
         String tmp = null;
         int parsedLen = 0;
         int i, j, from, size;
         StorageAttribute currType;
         bool useHex;
         FieldDef fld = null;
         bool valueNotPassed;

         from = getFromFldIdx(isCurrRec);
         size = getSizeFld(isCurrRec);

         for (i = from, j = 0; j < size; i++, j++)
         {
            fld = getFieldsTab().getField(i);
            currType = fld.getType();

            useHex = (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1 ||
                      currType == StorageAttribute.ALPHA ||
                      currType == StorageAttribute.UNICODE ||
                      StorageAttributeCheck.isTypeLogical(currType));

            // Qcr #940443 : Old fashion flags are still being used by resident table. So check them 1st coz we  might get exception otherwise.
            // The flags for resident are very simple, only 2 options. '.' = false, '/' = true.
            // We keep using it for resident, since in the server side we do not know if we are creating the xml for richclient or for BC at the creation stage.
            // Since we couldn't create different flags for rich, we will use the old flags here.
            if ((byte) (recFlags[j]) == '.')
               _flags[i] = 0;
            else if ((byte) (recFlags[j]) == '/')
               _flags[i] = 1;
            else // New flags style. For the view.
            {
               // Each flag will appear in the xml in 2 chars representing hex value ("42" for 0x42).
               tmp = recFlags.Substring(j*2, 2);
               _flags[i] = Convert.ToByte(tmp, 16);
            }

            // save the ind that the value was not passed from the server.
            valueNotPassed = (FLAG_VALUE_NOT_PASSED == (byte) (_flags[i] & FLAG_VALUE_NOT_PASSED));
            _flags[i] = (byte) (_flags[i] & ~FLAG_VALUE_NOT_PASSED);

            _flagsHistory[i] = _flags[i];

            if (FLAG_UPDATED == (byte) (_flags[i] & FLAG_UPDATED))
               _updated = true;

            if (valueNotPassed)
            {
               if (FLAG_NULL == (byte) (_flags[i] & FLAG_NULL))
               {
                  // null ind is on, just put any value in the field.
                  val = fld.getDefaultValue();
               }
               else
               {
                  // copy the existing value from the existing curr rec.
                  val = ((Record) _dataview.getCurrRec()).GetFieldValue(i);
               }
            }
            else
            {
               val = RecordUtils.deSerializeItemVal(fldsVal, currType, fld.getSize(), useHex, fld.getCellsType(), out parsedLen);
               fldsVal = fldsVal.Substring(parsedLen);
            }

            _fieldsData[i] = val;
         }
         setInitialFldVals(false, false);
      }

      /// <returns> get the actual string according to 'type'</returns>
      internal static String getString(String str, StorageAttribute type)
      {
         bool useHex;
         String temp = null;

         useHex = (ClientManager.Instance.getEnvironment().GetDebugLevel() > 1 || type == StorageAttribute.ALPHA ||
                   type == StorageAttribute.UNICODE || StorageAttributeCheck.isTypeLogical(type));
         // first 4 characters are the length of the string (hex number)
         if (type == StorageAttribute.ALPHA || type == StorageAttribute.UNICODE ||
             type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR)
            temp = str.Substring(4);
         else
            temp = str;

         return RecordUtils.getString(temp, type, useHex);
      }

      /// <summary>
      ///   init dcRefs vector and increase reference counter to DataView.counterArray
      /// </summary>
      /// <param name = "valueStr">in form "dit_idx,dc_id$dit_idx,dc_id$..."</param>
      protected void fillDCRef(String valueStr)
      {
         if (!String.IsNullOrEmpty(valueStr))
         {
            String[] couples = StrUtil.tokenize(valueStr, "$");
            int size = couples.Length;

            for (int i = 0; i < size; i++)
            {
               _dcRefs.Add(DcValuesReference.Parse(couples[i], this._dataview));
            }
         }
      }

      /// <summary>
      ///   build the fields value for the XML string of this record
      /// </summary>
      protected void buildFieldsData(StringBuilder message, bool isCurrRec, bool getOldVal)
      {
         int i, from, size;
         var tmpStr = new StringBuilder();

         from = getFromFldIdx(isCurrRec);
         size = getSizeFld(isCurrRec);

         for (i = from; i < from + size; i++)
         {
            // do not write the value if the shrink flag is on.
            if (FLAG_VALUE_NOT_PASSED == (_flags[i] & FLAG_VALUE_NOT_PASSED))
               continue;

            tmpStr.Append(getFieldDataXML(i, getOldVal));
         }

         // if none of the values had changed. pass " " instead of "".
         if (tmpStr.Length == 0)
            tmpStr.Append(" ");

         message.Append(tmpStr);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="message"></param>
      protected void buildFieldsData(StringBuilder message)
      {
         int i, from, size;
         
         List<byte> fieldValue = new List<byte>();
         from = getFromFldIdx(false);
         size = getSizeFld(false);

         for (i = from; i < from + size; i++)
         {
            // do not write the value if the shrink flag is on.
            if (FLAG_VALUE_NOT_PASSED == (_flags[i] & FLAG_VALUE_NOT_PASSED))
               continue;

            byte [] tmp = getFieldDataXML(i);
            fieldValue.AddRange(tmp);
         }

         byte[] fieldValuesInBytes = new byte[fieldValue.Count];
         fieldValue.CopyTo(fieldValuesInBytes);

         byte[] encodedfieldValuesInBytes = Base64.encode(fieldValuesInBytes);
         string tmpStr = ClientManager.Instance.getEnvironment().GetEncoding().GetString(encodedfieldValuesInBytes, 0, encodedfieldValuesInBytes.Length);

         message.Append(tmpStr);
      }

      /// <summary>
      /// </summary>
      /// <param name="fldIdx"></param>
      /// <param name="fldVal"></param>
      /// <returns></returns>
      internal string getXMLForValue(int fldIdx, String fldVal)
      {
         StorageAttribute cellAttr = StorageAttribute.SKIP; // cell's attribute, if the field's attribute is a vector
         StorageAttribute fldAttr = getFieldsTab().getType(fldIdx); // field's attribute
         if (fldAttr == StorageAttribute.BLOB_VECTOR)
            cellAttr = getFieldsTab().getField(fldIdx).getCellsType();

         bool toBase64 = (ClientManager.Instance.getEnvironment().GetDebugLevel() <= 1);

         return serializeItemVal(fldVal, fldAttr, cellAttr, toBase64);
      }

      /// <summary>
      ///   returns the index of the starting field according to the type of record
      /// </summary>
      /// <param name = "isCurrRec">should be true for the current rec</param>
      protected int getFromFldIdx(bool isCurrRec)
      {
         if (isCurrRec)
            return 0;
         else
            return getFieldsTab().getRMIdx();
      }

      /// <summary>
      ///   returns the number of fields according to the type of record
      /// </summary>
      /// <param name = "isCurrRec">should be true for the current rec</param>
      protected int getSizeFld(bool isCurrRec)
      {
         if (isCurrRec)
            return getFieldsTab().getSize();
         else
            return getFieldsTab().getRMSize();
      }

      protected internal void buildXML(StringBuilder message, bool isCurrRec)
      {
         buildXML(message, isCurrRec, false);
      }
      /// <summary>
      ///   build the XML string of the record
      /// </summary>
      /// <param name = "message">the xml message to append to</param>
      /// <param name = "isCurrRec">indicate this record as the current record</param>
      protected internal void buildXML(StringBuilder message, bool isCurrRec, bool forceBuild)
      {
         StringBuilder recFlags;
         int from, size, i;
         byte aFlag;
         string hexFlag;

         if (!forceBuild)
            if (_mode != DataModificationTypes.Insert && _mode != DataModificationTypes.Update && _mode != DataModificationTypes.Delete &&
             !isCurrRec)
            return;

         message.Append("\n            <" + ConstInterface.MG_TAG_REC);
         if (_id > Int32.MinValue)
            message.Append(" " + XMLConstants.MG_ATTR_ID + "=\"" + _id + "\"");
         message.Append(" " + ConstInterface.MG_ATTR_MODE + "=\"" + (char)_mode + "\"");

         if (_mode == DataModificationTypes.Insert && _prev != null)
            message.Append(" " + ConstInterface.MG_ATTR_ADD_AFTER + "=\"" + _prev.getId() + "\"");

         // current record's mode is never cleared, the whole record is cleared when we move on.
         if (!forceBuild && !isCurrRec)
            clearMode();

         message.Append(" " + XMLConstants.MG_ATTR_VALUE + "=\"");

         if (!forceBuild && isCurrRec)
            setShrinkFlags();

         bool getOldVal;
         if (Synced)
            getOldVal = false;
         else
         {
            Task task = _dataview.getTask();
            getOldVal = (!isCurrRec &&
                         (task.getLevel() == Constants.TASK_LEVEL_RECORD ||
                          task.getLevel() == Constants.TASK_LEVEL_CONTROL) &&
                         _dataview.getCurrRec() != null && ((Record) _dataview.getCurrRec()).getId() == _id);
         }

         buildFieldsData(message, isCurrRec, getOldVal);
         message.Append("\"");
         recFlags = new StringBuilder();

         from = getFromFldIdx(isCurrRec);
         size = getSizeFld(isCurrRec);

         // QCR 479686: send FLAG_CRSR_MOD from the history, thus let the server know if a field
         // was updated since the last time we sent it to IT (hence, a DBMS action is needed).
         for (i = from; i < from + size; i++)
         {
            aFlag = _flagsHistory[i];
            aFlag &= FLAG_CRSR_MODIFIED;
            aFlag |= _flags[i];

            // Each flag will appear in the xml in 2 chars representing hex value ("42" for 0x42).
            hexFlag = aFlag.ToString("X2");

            recFlags.Append(hexFlag);

            // clear the shrink flag
            if (isCurrRec)
               _flags[i] = (byte) (_flags[i] & ~FLAG_VALUE_NOT_PASSED);
         }
         //recFlags = recFlags.substring(from, from+size);
         message.Append(" " + ConstInterface.MG_ATTR_FLAGS + "=\"" + recFlags + "\"");

         if (isCurrRec)
         {
            message.Append(" " + ConstInterface.MG_ATTR_CURR_REC + "=\"1\"");

            if (Modified)
               message.Append(" " + ConstInterface.MG_ATTR_MODIFIED + "=\"1\"");
            MgTree mgTree = (MgTree)(_dataview.getMgTree());
            if (mgTree != null)
               //there is tree in the dv
            {
               int line = _dataview.getForm().DisplayLine;
               String treePath = mgTree.getPath(line);
               String treeValues = mgTree.getParentsValues(line);
               String treeIsNulls = mgTree.getNulls(line);

               if (treePath != null)
                  message.Append(" " + ConstInterface.MG_ATTR_PATH + "=\"" + treePath + "\"");

               if (treeValues != null)
                  message.Append(" " + ConstInterface.MG_ATTR_VALUES + "=\"" + treeValues + "\"");

               if (treeValues != null)
                  message.Append(" " + ConstInterface.MG_ATTR_TREE_IS_NULLS + "=\"" + treeIsNulls + "\"");
            }
         }

         if (_linksFldsPos != null)
            message.Append(" " + ConstInterface.MG_ATTR_DBPOS + "=\"" + XmlParser.escape(_linksFldsPos) + "\" ");

         message.Append(XMLConstants.TAG_TERM);
      }

      /// <summary>
      ///   build the XML string of the record
      /// </summary>
      /// <param name = "message">the xml message to append to</param>
      protected internal void buildXMLForDataViewToDataSource(StringBuilder message)
      {
         StringBuilder recFlags;
         int from, size, i;
         byte aFlag;
         string hexFlag;

         message.Append("\n            <" + ConstInterface.MG_TAG_REC);
         if (_id > Int32.MinValue)
            message.Append(" " + XMLConstants.MG_ATTR_ID + "=\"" + _id + "\"");
         message.Append(" " + ConstInterface.MG_ATTR_MODE + "=\"" + (char)_mode + "\"");

         if (_mode == DataModificationTypes.Insert && _prev != null)
            message.Append(" " + ConstInterface.MG_ATTR_ADD_AFTER + "=\"" + _prev.getId() + "\"");


         message.Append(" " + XMLConstants.MG_ATTR_VALUE + "=\"");

         buildFieldsData(message);

         message.Append("\"");
         recFlags = new StringBuilder();

         from = getFromFldIdx(false);
         size = getSizeFld(false);

         // QCR 479686: send FLAG_CRSR_MOD from the history, thus let the server know if a field
         // was updated since the last time we sent it to IT (hence, a DBMS action is needed).
         for (i = from; i < from + size; i++)
         {
            aFlag = _flagsHistory[i];
            aFlag &= FLAG_CRSR_MODIFIED;
            aFlag |= _flags[i];

            // Each flag will appear in the xml in 2 chars representing hex value ("42" for 0x42).
            hexFlag = aFlag.ToString("X2");

            recFlags.Append(hexFlag);
         }
         //recFlags = recFlags.substring(from, from+size);
         message.Append(" " + ConstInterface.MG_ATTR_FLAGS + "=\"" + recFlags + "\"");

         if (_linksFldsPos != null)
            message.Append(" " + ConstInterface.MG_ATTR_DBPOS + "=\"" + XmlParser.escape(_linksFldsPos) + "\" ");

         message.Append(XMLConstants.TAG_TERM);
      }

      /// <summary>
      ///   scan the record and compare fields with parallel server curr rec,
      ///   set shrink flag on if fields are equal.
      /// </summary>
      protected void setShrinkFlags()
      {
         int i;
         int size = getSizeFld(true);

         Record serverCurrRec = _dataview.getServerCurrRec();
         // cannot shrink if we do not know what curr rec the server holds.
         if (serverCurrRec == null)
            return;

         for (i = 0; i < size; i++)
         {
            if (fldValsEqual(serverCurrRec, i) || (FLAG_NULL == (_flags[i] & FLAG_NULL)))
            {
               _flags[i] |= FLAG_VALUE_NOT_PASSED;
            }
         }
      }


      /// <summary>
      /// set shrink flag for specific field
      /// </summary>
      /// <param name="fldIdx"></param>
      public void setShrinkFlag(int fldIdx)
      {
         _flags[fldIdx] |= FLAG_VALUE_NOT_PASSED;
      }

      /// <summary>
      /// sets field value to its original value from record
      /// </summary>
      /// <param name="idx"></param>
      /// <param name="value"></param>
      public void SetFieldValue(int idx, bool isNull, String value)
      {
         Field field = (Field)getFieldsTab().getField(idx);
         field.UpdateNull(isNull, this);
         //setFieldValue(idx, value, false);
         value = CheckMgValue(value, isNull, field);
         _fieldsData[idx] = value;
         field.takeValFromRec();
         clearFlag(field.getId(), Record.FLAG_INVALID);
         field.invalidate(true, false);
      }
      /// <summary>
      ///   set field value
      /// </summary>
      /// <param name = "fldIdx">the field index</param>
      /// <param name = "mgVal">the new value of the field. must be in the internal storage format</param>
      /// <param name = "setRecordUpdated">tells whether to define this record as updated</param>
      internal void setFieldValue(int fldIdx, String mgVal, bool setRecordUpdated)
      {
         Field fld = null;

         // don't change value when references are equal or if the task mode is Query
         fld = (Field) getFieldsTab().getField(fldIdx);

         if (fld.PrevIsNull() == fld.isNull() && ReferenceEquals(mgVal, GetFieldValue(fldIdx)))
            return;

         mgVal = CheckMgValue(mgVal, fld.isNull(), fld);

         if (fldIdx >= 0 && fldIdx < getFieldsTab().getSize())
         {
            _fieldsData[fldIdx] = mgVal;
            // QCR #986715: The following setFlag() call was removed to solve the bug.
            // it caused marking a variable as modified even if the change is due to the
            // compute of the record. The FLAG_MODIFIED flag is set correctly in the
            // Field.setValue() method.
            // setFlag(fldIdx, FLAG_MODIFIED);
            if (setRecordUpdated)
            {
               // set modify only if the field is part of the dataview.
               if (fld.PartOfDataview)
                  _modified = true;

               _dataview.setChanged(true);
               setMode(DataModificationTypes.Update);
            }
         }
         else
            Logger.Instance.WriteExceptionToLog("in Record.setFieldValue() illegal field index: " + fldIdx);
      }

      /// <summary>
      /// check mgvalue
      /// </summary>
      /// <param name="mgVal"></param>
      /// <param name="isNull"></param>
      /// <param name="fld"></param>
      /// <returns></returns>
      protected String CheckMgValue(String mgVal, bool isNull, Field fld)
      {
         if ((mgVal == null) || isNull)
         {
            mgVal = fld.getNullValue();
            if (mgVal == null)
               mgVal = fld.getMagicDefaultValue();
         }

         int size = fld.getSize();

         // truncate values that are longer than the storage size
         if (UtilStrByteMode.isLocaleDefLangDBCS() && fld.getType() != StorageAttribute.UNICODE)
         {
            // count the number of bytes, not characters (JPN: DBCS support)
            if (mgVal != null && UtilStrByteMode.lenB(mgVal) > size)
               mgVal = UtilStrByteMode.leftB(mgVal, size);
         }
         else
         {
            if (mgVal != null && mgVal.Length > size)
               mgVal = mgVal.Substring(0, size);
         }
         return mgVal;
      }

      /// <summary>
      ///   set the "modified" flag
      /// </summary>
      protected internal void setModified()
      {
         _modified = true;
      }

      /// <summary>
      ///   reset the "modified" flag
      /// </summary>
      internal void resetModified()
      {
         _modified = false;
      }

      /// <summary>
      ///   set the "updated" flag
      /// </summary>
      protected internal void setUpdated()
      {
         _updated = true;
      }

      /// <summary>
      ///   reset the "modified" flag
      /// </summary>
      internal void resetUpdated()
      {
         _updated = false;
      }

      /// <summary>
      ///   get a field value
      /// </summary>
      /// <param name = "fldIdx">the field index</param>
      /// <returns> String the field value</returns>
      public String GetFieldValue(int fldIdx)
      {
         String val = null;

         if (fldIdx >= 0 && fldIdx < _fieldsData.Length)
            val = _fieldsData[fldIdx];
         return val;
      }

      /// <summary>
      ///   returns a copy of this record
      /// </summary>
      internal Record replicate()
      {
         var rec = (Record) MemberwiseClone();
         rec._fieldsData = (String[]) _fieldsData.Clone();
         rec._flags = (byte[]) _flags.Clone();
         rec._flagsHistory = (byte[]) _flagsHistory.Clone();
         rec._dcRefs = _dcRefs.Clone();
         return rec;
      }


      /// <summary>
      ///   set the value of the member variables of this record to be the same as those
      ///   of the given record
      /// </summary>
      /// <param name = "rec">the source record</param>
      /// <param name = "rId">the new Id of the record</param>
      protected internal void setSameAs(Record rec, bool realOnly, int rId)
      {
         setSameAs(rec, realOnly);
         setId(rId);
      }

      /// <summary>
      ///   set the value of the member variables of this record to be the same as those
      ///   of the given record
      /// </summary>
      /// <param name = "rec">the source record</param>
      protected internal void setSameAs(Record rec, bool realOnly)
      {
         Field aField = null;
         int j;

         _id = rec._id;
         _hashKey = rec._hashKey;
         if (!(_dataview.getTask()).transactionFailed(ConstInterface.TRANS_RECORD_PREFIX))
            setMode(rec._mode);
         _addAfter = rec._addAfter;
         if (realOnly)
         {
            for (j = 0; j < getFieldsTab().getSize(); j++)
            {
               aField = (Field) rec.getFieldsTab().getField(j);
               if (!aField.IsVirtual)
               {
                  _fieldsData[j] = rec._fieldsData[j];
                  _flags[j] = rec._flags[j];
                  _flagsHistory[j] = rec._flagsHistory[j];
               }
            }
         }
         else
         {
            _fieldsData = rec._fieldsData;
            _flags = rec._flags;
            _flagsHistory = rec._flagsHistory;
         }
         _modified = rec._modified;
         _dataview = rec._dataview;
         _computed = rec._computed;
         _updated = rec._updated;

         setDcRefs(rec._dcRefs);

         // QCR #423552: partial fix - we need to go over all the rec.next/prev and make sure this record does not appear
         if (rec._next != this)
            _next = rec._next;

         if (rec._prev != this)
            _prev = rec._prev;
      }

      /// <summary>
      ///   returns true if a field has a null value
      /// </summary>
      /// <param name = "fldIdx">the index of the field to check</param>
      public bool IsNull(int fldIdx)
      {
         checkFlags(fldIdx);
         return ((_flags[fldIdx] & FLAG_NULL) == FLAG_NULL);
      }

      /// <summary>
      ///   returns true if a field has a null value
      /// </summary>
      /// <param name = "fldIdx">the index of the field to check</param>
      protected internal bool isLinkInvalid(int fldIdx)
      {
         checkFlags(fldIdx);
         return ((_flags[fldIdx] & FLAG_INVALID) == FLAG_INVALID);
      }

      /// <summary>
      ///   returns true if a field has modified
      /// </summary>
      /// <param name = "fldIdx">the index of the field to check</param>
      public bool isFldModified(int fldIdx)
      {
         checkFlags(fldIdx);
         return ((_flags[fldIdx] & FLAG_MODIFIED) == FLAG_MODIFIED);
      }

      /// <summary>
      ///   returns true if a field has modified at least once
      /// </summary>
      /// <param name = "fldIdx">the index of the field to check</param>
      public bool IsFldModifiedAtLeastOnce(int fldIdx)
      {
         checkFlags(fldIdx);
         return ((_flags[fldIdx] & FLAG_MODIFIED_ATLEAST_ONCE) == FLAG_MODIFIED_ATLEAST_ONCE);
      }

      /// <summary>
      ///   returns true if a field has modified
      /// </summary>
      /// <param name = "fldIdx">the index of the field to check</param>
      protected internal bool isFldUpdated(int fldIdx)
      {
         checkFlags(fldIdx);
         return ((_flags[fldIdx] & FLAG_UPDATED) == FLAG_UPDATED);
      }

      /// <summary>
      ///   set a field to have a null value
      /// </summary>
      /// <param name = "fldIdx">the index of the field</param>
      protected internal void setFlag(int fldIdx, byte aFlag)
      {
         checkFlags(fldIdx);
         _flags[fldIdx] |= aFlag;

         // Note: we only set the history flags. We dont clear them here. We only clear them after XML was dumped.
         if (aFlag == FLAG_CRSR_MODIFIED)
            _flagsHistory[fldIdx] |= aFlag;
      }

      /// <summary>
      ///   set a field to have a non-null value
      /// </summary>
      /// <param name = "fldIdx">the index of the field</param>
      internal void clearFlag(int fldIdx, byte aFlags)
      {
         checkFlags(fldIdx);
         _flags[fldIdx] &= (byte) (~aFlags);
      }

      /// <summary>
      ///   clear the flags history. This should only be done after a commit.
      /// </summary>
      internal void clearFlagsHistory()
      {
         for (int i = 0; i < _flags.Length; i++)
            _flagsHistory[i] = 0;
      }

      /// <summary>
      ///   clear flag for all fields
      /// </summary>
      internal void clearFlags(byte aFlags)
      {
         for (int i = 0; i < _flags.Length; i++)
            clearFlag(i, aFlags);
      }

      /// <summary>
      ///   clear flag for all real fields
      /// </summary>
      internal void clearFlagsForRealOnly(byte aFlags)
      {
         for (int i = 0; i < _flags.Length; i++)
         {
            Field field = (Field)getFieldsTab().getField(i);
            if (!field.IsVirtual)
               clearFlag(i, aFlags);
         }
      }


      /// <summary>
      ///   clear a specific field flag from the history
      /// </summary>
      internal void clearHistoryFlag(int fldIdx)
      {
         if (_flagsHistory != null && fldIdx < getFieldsTab().getSize() && fldIdx >= 0)
            _flagsHistory[fldIdx] = 0;
      }

      /// <summary>
      ///   check the existence of the flags and the validity of the field index
      ///   throws an error in case of a failure
      /// </summary>
      /// <param name = "fldIdx">the index of the field</param>
      protected void checkFlags(int fldIdx)
      {
         if (_flags == null || fldIdx >= getFieldsTab().getSize() || fldIdx < 0)
            throw new ApplicationException("Cannot find flags");
      }

      /// <summary>
      ///   restart the record's settings. This might be needed after a server's rollback
      ///   or restart
      /// </summary>
      /// <param name = "oldMode">the old record mode</param>
      internal void restart(DataModificationTypes oldMode)
      {
         Task task = _dataview.getTask();

         if (oldMode == DataModificationTypes.None)
         {
            var rec = (Record) _dataview.getCurrRec();
            bool isCurrRec = (rec != null && rec.getId() == _id);

            if ((_dataview.getTask()).getMode() == Constants.TASK_MODE_CREATE)
               if (isCurrRec && !task.getAfterRetry() && task.TryingToCommit && !_dataview.inRollback())
                  oldMode = DataModificationTypes.Update;
               else
                  oldMode = DataModificationTypes.Insert;
            else
               oldMode = DataModificationTypes.Update;
         }
         setMode(oldMode);
         if (oldMode == DataModificationTypes.Insert)
            setNewRec();
      }

      /// <summary>
      ///   sets the value of lateCompute flag
      /// </summary>
      /// <param name = "val">the new value of the flag</param>
      internal void setLateCompute(bool val)
      {
         _lateCompute = val;
      }

      /// <summary>
      ///   return the value of the "lateCompute" flag
      /// </summary>
      protected internal bool lateCompute()
      {
         return _lateCompute;
      }

      /// <summary>
      /// "in delete process" flag
      /// </summary>
      /// <param name="val"></param>
      internal void setInDeleteProcess(bool val)
      {
         _inDeleteProcess = val;
      }

      internal bool inDeleteProcess()
      {
         return _inDeleteProcess;
      }

      /// <summary>
      ///   call this function when removing the record, it will decrease the reference count
      ///   of its data controls to their dcValues
      /// </summary>
      protected internal void removeRecFromDc()
      {
         if (_dcRefs != null)
            _dcRefs.Dispose();
      }

      /// <summary>
      ///   compare the data of this record with the data of a different record
      /// </summary>
      /// <param name = "rec">the other record</param>
      /// <param name = "currRec">TRUE if we are comparing two 'current records' otherwise should be false.</param>
      /// <param name = "checkOnlyParetOfDataview">FALSE if we want to check all the fields , including those that are not part of data view.</param>
      internal bool isSameRecData(Record rec, bool currRec, bool checkOnlyParetOfDataview)
      {
         int size = getSizeFld(currRec);
         int start = getFromFldIdx(currRec);
         int i;
         Field field;

         // compare references
         if (this == rec)
            return true;

         try
         {
            if (rec.getSizeFld(currRec) != size)
               return false;

            if (rec.getFromFldIdx(currRec) != start)
               return false;

            for (i = start; i < start + size; i++)
            {
               field = (Field) getFieldsTab().getField(i);

               if (checkOnlyParetOfDataview && !field.PartOfDataview)
                  continue;

               if (!fldValsEqual(rec, i))
                  return false;
            }
         }
         catch (ApplicationException)
         {
            return false;
         }

         return true;
      }

      /// <summary>
      ///   compare two field values between this record and the given record
      /// </summary>
      /// <param name = "rec">the record to compare with</param>
      /// <param name = "fldIdx">the index of the field to compare</param>
      internal bool fldValsEqual(Record rec, int fldIdx)
      {
         StorageAttribute dataType = getFieldsTab().getField(fldIdx).getType();

         return ExpressionEvaluator.mgValsEqual(GetFieldValue(fldIdx), IsNull(fldIdx), dataType, rec.GetFieldValue(fldIdx), rec.IsNull(fldIdx), dataType);
      }

      /// <summary>
      ///   returns hash key object of this record
      /// </summary>
      protected internal Int32 getHashKey()
      {
         return _hashKey;
      }

      /// <summary>
      ///   sets the reference to the records after this one, in a list of recs.
      /// </summary>
      /// <param name = "nextRec">is the next record (or null for the last record)</param>
      protected internal void setNextRec(Record nextRec)
      {
         _next = nextRec;
      }

      /// <summary>
      ///   sets the reference to the records before this one, in a list of recs.
      /// </summary>
      /// <param name = "prevRec">is the previous record (or null for the first record)</param>
      protected internal void setPrevRec(Record prevRec)
      {
         _prev = prevRec;
      }

      /// <summary>
      ///   return the record whose location is before this one
      /// </summary>
      protected internal Record getPrevRec()
      {
         return _prev;
      }

      /// <summary>
      ///   return the record which follows this one
      /// </summary>
      protected internal Record getNextRec()
      {
         return _next;
      }

      /// <summary>
      ///   return DcRefs
      /// </summary>
      protected internal ObjectReferencesCollection getDcRefs()
      {
         return _dcRefs.Clone();
      }

      /// <summary>
      ///   set DcRefs
      /// </summary>
      protected internal void setDcRefs(ObjectReferencesCollection newDcRefs)
      {
         removeRecFromDc();
         _dcRefs = newDcRefs.Clone();
         SetDcValueId();
      }

      /// <summary>
      ///   returns true if one of the real variables was modified
      /// </summary>
      internal bool realModified()
      {
         bool bRc = false;
         int tabSize = getFieldsTab().getSize();
         int j;

         for (j = 0; j < tabSize; j++)
         {
            //for every modified field check if it is a real field
            if ((_flags[j] & FLAG_MODIFIED) == FLAG_MODIFIED)
            {
               if (!(((Field) getFieldsTab().getField(j)).IsVirtual))
               {
                  bRc = true; //if a real modified field was found, return with true
                  break;
               }
            }
         }

         return bRc;
      }

      /// <summary>
      ///   returns the xml representation of the record - this method is defined so we
      ///   can use the record reference in a string concatenation operations and print calls
      /// </summary>
      public override String ToString()
      {
         var str = new StringBuilder();

         buildXML(str, ((Record) _dataview.getCurrRec()).getId() == _id, true);
         return str.ToString();
      }

      /// <summary>
      ///   set the value of the sendToServer flag
      /// </summary>
      internal void setSendToServer(bool val)
      {
         _sendToServer = val;
      }

      /// <summary>
      ///   return the size in kb of the records e.i. the sum of all fields in the record
      /// </summary>
      protected internal int getRecSize()
      {
         Field curr = null;
         int sum = 0;

         for (int i = 0; i < getFieldsTab().getSize(); i++)
         {
            curr = (Field) getFieldsTab().getField(i);
            sum += curr.getSize();
         }

         return sum;
      }

      /// <summary>
      ///   copy the modified crsr flags from a given rec.
      /// </summary>
      /// <param name = "rec">the source record</param>
      internal void copyCrsrModifiedFlags(Record rec)
      {
         int tabSize = getFieldsTab().getSize();
         int j;

         for (j = 0; j < tabSize; j++)
         {
            _flags[j] = rec._flags[j];
            _flags[j] &= FLAG_CRSR_MODIFIED;
         }
      }

      /// <summary>
      ///   for a table cache record returns the record's db_pos value as a base64 string
      /// </summary>
      internal String getDbPosVal()
      {
         return _dbPosBase64Val;
      }

      /// <summary>
      ///   this is the implementation of the IComparable
      ///   since two records can be compared without a key it can only be activated 
      ///   on records that belong to cached table and not on dataview records 
      ///   in order to get the key we'll get access to the table cache (using one of the fields) and take it 
      ///   from there
      /// </summary>
      internal int compare(Object obj1, Object obj2)
      {
         var rec1 = (Record) obj1;
         var rec2 = (Record) obj2;

         try
         {
            return rec1.CompareTo(rec2);
         }
         catch (Exception)
         {
            return 0;
         }
      }

      /// <summary>
      ///   build the string that represent the current links fields db_pos values
      /// </summary>
      internal void buildLinksPosStr()
      {
         DataviewHeaders tbl = (_dataview.getTask()).getDataviewHeaders();
         if (tbl != null)
            _linksFldsPos = tbl.buildDbPosString();
      }

      internal void setForceSaveOrg(bool val)
      {
         _forceSaveOrg = val;
      }

      /// <summary>
      ///   returns the relinked flag
      /// </summary>
      internal bool getForceSaveOrg()
      {
         return _forceSaveOrg;
      }

      /// <summary>
      ///   true, if exit from this record will cause invalidation of the table due to vitrual recompute
      /// </summary>
      /// <returns></returns>
      internal bool isCauseInvalidation()
      {
         return _causeInvalidation;
      }

      /// <summary>
      ///   if causeInvalidation is true,  exit from this record will cause invalidation of the table due to vitrual recompute
      /// </summary>
      /// <param name = "causeInvalidation"></param>
      internal void setCauseInvalidation(bool causeInvalidation)
      {
         _causeInvalidation = causeInvalidation;
      }


      /// <summary>
      ///   make sure the record has newly created blob value for DotNet fields
      /// </summary>
      internal void SetDotNetsIntoRec()
      {
         Field fld;
         int size = getFieldsTab().getSize();
         String fldVal;
         int fldId;

         for (int i = 0; i < size; i++)
         {
            fld = (Field) getFieldsTab().getField(i);
            fldVal = fld.getValue(false);

            // make sure the record has update fld's value for DotNet fields
            if (fld.getType() == StorageAttribute.DOTNET && fldVal != GetFieldValue(i))
            {
               fldId = fld.getId();

               setFieldValue(fldId, fldVal, false);

               // we must clear this flag, so that this update val is sent to server.
               clearFlag(fldId, FLAG_NULL);
            }
         }
      }

      /// <summary>
      ///   return the value of the "computed" flag
      /// </summary>
      internal bool isComputed()
      {
         return _computed;
      }

      /// <summary>
      ///   set the value of the "computed" flag
      /// </summary>
      /// <param name = "val">the new value of the flag</param>
      internal void setComputed(bool val)
      {
         _computed = val;
      }

      /// <summary>
      ///   set the value of the "in compute" flag
      /// </summary>
      /// <param name = "val">the new value of the flag</param>
      internal void setInCompute(bool val)
      {
         if (!val)
            setComputed(true);
         _inCompute = val;
      }

      /// <summary>
      ///   set the value of the "in recompute" flag
      /// </summary>
      /// <param name = "val">the new value of the flag</param>
      internal void setInRecompute(bool val)
      {
         _inRecompute = val;
      }

      /// <summary>
      ///   set the mode
      /// </summary>
      /// <param name = "newMode">the new mode: Created, Modified, Deleted</param>
      internal void setMode(DataModificationTypes newMode)
      {
         switch (newMode)
         {
            case DataModificationTypes.None:
            case DataModificationTypes.Insert:
            case DataModificationTypes.Update:
            case DataModificationTypes.Delete:
               if (_mode == DataModificationTypes.None ||
                   _mode == DataModificationTypes.Update && newMode == DataModificationTypes.Delete)
                  _mode = newMode;
               break;

            default:
               Logger.Instance.WriteExceptionToLog("in Record.setMode(): illegal mode: " + newMode);
               break;
         }
      }

      /// <summary>
      ///   set the record mode to "none"
      /// </summary>
      internal void clearMode()
      {
         _mode = DataModificationTypes.None;
      }

      /// <summary>
      ///   switch off the newRec flag
      /// </summary>
      public void setOldRec()
      {
         _newRec = false;
      }

      /// <summary>
      ///   switch on the newRec flag
      /// </summary>
      internal void setNewRec()
      {
         _newRec = true;
      }

      /// <summary>
      ///   serialize an item (field/global param/...) to an XML format (applicable to be passed to the server).
      /// </summary>
      /// <param name = "val">item's value</param>
      /// <param name = "itemAttr">item's attribute</param>
      /// <param name = "cellAttr">cell's attribute - relevant only if 'itemAttr' is vector</param>
      /// <param name = "ToBase64">>decide Base64 encoding is to be done</param>
      /// <returns>serialized XML format</returns>
      internal static String itemValToXML(String itemVal, StorageAttribute itemAttr, StorageAttribute cellAttr, bool ToBase64)
      {
         String tmpBuf = RecordUtils.serializeItemVal(itemVal, itemAttr, cellAttr, ToBase64);
         return XmlParser.escape(tmpBuf.ToString());
      }

      /// <summary>
      ///   serialize an item (field/global param/...).
      /// </summary>
      /// <param name = "val">item's value</param>
      /// <param name = "itemAttr">item's attribute</param>
      /// <param name = "cellAttr">cell's attribute - relevant only if 'itemAttr' is vector</param>
      /// <param name = "ToBase64">>decide Base64 encoding is to be done</param>
      /// <returns>serialized itemVal </returns>
      internal static String serializeItemVal(String itemVal, StorageAttribute itemAttr, StorageAttribute cellAttr, bool ToBase64)
      {
         return (RecordUtils.serializeItemVal(itemVal, itemAttr, cellAttr, ToBase64));
      }


     
      /// <summary>
      /// sets DCVal Id into MgControls
      /// </summary>
      internal void SetDcValueId()
      {
         MgForm form = (MgForm)_dataview.getTask().getForm();
         MgControl mgControl = null;

         // if the form doesn't have any selection control, dcRefs would be null
         if (form != null && _dcRefs != null)
         {
            foreach (DcValuesReference dcRef in _dcRefs)
            {
               mgControl = (MgControl)form.getCtrl(dcRef.ditIdx);
               if (mgControl != null)
                  mgControl.setDcValId(dcRef.DcValues.getId());
            }
         }
      }

      /// <summary>
      /// reset the DCVal Id in MgControls
      /// </summary>
      internal void resetDcValueId()
      {
         MgForm form = (MgForm)_dataview.getTask().getForm();
         MgControl mgControl = null;

         // if the form doesn't have any selection control, dcRefs would be null
         if (form != null && _dcRefs != null)
         {
            foreach (DcValuesReference dcRef in _dcRefs)
            {
               mgControl = (MgControl)form.getCtrl(dcRef.ditIdx);
               if (mgControl != null)
                  mgControl.setDcValId(DcValues.EMPTY_DCREF);
            }
         }
      }

      #region Nested type: DcItem

      /// <summary>
      /// This class represents a link between a data control and its DcValues object.
      /// </summary>
      protected class DcValuesReference : ObjectReferenceBase
      {
         internal readonly int ditIdx;

         /// <summary>
         ///   CTOR
         /// </summary>
         /// <param name = "couple">a string that represents the ditIdx & dcId separated by a comma</param>
         internal DcValuesReference(int controlId, DcValues referencedDcValues): base(referencedDcValues)
         {
            ditIdx = controlId;
         }

         /// <summary>
         /// Gets the referenced DcValues.
         /// </summary>
         internal DcValues DcValues { get { return (DcValues)Referent; } }

         public override ObjectReferenceBase Clone()
         {
            return new DcValuesReference(ditIdx, (DcValues)Referent);
         }

         /// <summary>
         /// Creates a new DcValuesReference from a string containing two integers, where the
         /// first is the index of the control in the form (dit index) and the second is the 
         /// DcValues object identifier.
         /// </summary>
         /// <param name="couple"></param>
         /// <param name="dcValuesOwner"></param>
         /// <returns></returns>
         internal static DcValuesReference Parse(string couple, DataView dcValuesOwner)
         {
            int commaPos = couple.IndexOf(",");

            int ditIdx = Int32.Parse(couple.Substring(0, commaPos));
            int dcId = Int32.Parse(couple.Substring(commaPos + 1));

            return new DcValuesReference(ditIdx, dcValuesOwner.getDcValues(dcId));
         }
      }

      #endregion

      #region Nested type: ArgumentException

      protected internal class ArgumentException : Exception
      {
         private readonly String _str;

         internal ArgumentException(String msg)
         {
            _str = msg;
         }

         protected internal String getMsg()
         {
            return _str;
         }

         /// <summary> marked the record as relinked which is different then modified</summary>
      }

      #endregion
      

    
      public void AddDcValuesReference(int controlId, int dcValuesId)
      {
         DcValues dcValues = _dataview.getDcValues(dcValuesId);
         var dcRef = new DcValuesReference(controlId, dcValues);
         if (_dcRefs == null)
            _dcRefs = new ObjectReferencesCollection();
         _dcRefs.Add(dcRef);
      }
   }
}
