using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;

namespace com.magicsoftware.unipaas.management.data
{
   /// <summary>
   ///   represents the magic vector type
   /// </summary>
   public class VectorType
   {
      // NOTE: the BLOB_TABLE_STR was deliberately set to lower case (while the server uses upper case
      // this is a patch to distinguish between "FLAT DATA" built by the client (therefore are already unicode)
      // and between flat data accepted from the server (therefore are not unicode).
      // global variables
      internal const String BLOB_TABLE_STR = "mgbt";
      internal const long BLOB_TABLE_STR_LEN = 4;
      internal const String VECTOR_STR = "MGVEC";
      internal const long VECTOR_STR_LEN = 5;
      internal const int VERSION = 5; // version no 5 includes the content field
      internal const int EMPTY_BLOB_PREFIX_LEN = 7;

      // FOR EXPLANATIONS REGARDING THIS NUMBER PLEAS SEE DOCUMENTATION INSIDE init METHOD
      internal const int BLOB_TYPE_STRUCT_SIZE = 28;

      private readonly List<CellElement> _cells; // a vector containing CellElements
      private readonly Encoding _encoding; // to be used only with alpha or memo
      private bool _allowNull;
      private char _cellContentType;
      private long _cellSize;
      private StorageAttribute _cellsAttr; // all cells in the vector must be of the same type
      private String _cellsDefaultVal; // all cells in the magic vector has the same default value
      private bool _cellsIsNullDefault; // null default definition for all cells in the magic vector

      // data buffers that we change Dynamically to facilitate
      // the creation of a string representation of the vector
      private StringBuilder _dataBuf;
      private bool _initialized; // marks whether the vector was initialized if it was not that it is an invalid vector
      private StringBuilder _nullBuf;
      private String _originalflatData;

      /// <summary>
      ///   construct the object but does not fill it with data in order to initialize the Vector we must invoke on
      ///   it the its init method a VectorType which was not initialized is an unvalid vector.
      /// </summary>
      public VectorType(StorageAttribute cellsType, char contentType, String defualt, bool isDefNull, bool nullAlowed, long length)
      {
         _cells = new List<CellElement>();
         _cellsAttr = cellsType;
         _cellContentType = contentType;
         _cellsDefaultVal = defualt;
         _cellsIsNullDefault = isDefNull;
         _cellSize = (_cellsAttr == StorageAttribute.UNICODE ? length*2 : length);
         _initialized = true;
         _allowNull = nullAlowed;
         _nullBuf = new StringBuilder();
         _dataBuf = new StringBuilder();
         _originalflatData = ToString();
         _encoding = Manager.Environment.GetEncoding();
      }

      /// <summary>
      ///   construct the object but does not fill it with data in order to initialize the Vector we must invoke on
      ///   it the its init method a VectorType which was not initialized is an invalid vector.
      /// </summary>
      /// <param name = "blobString">a string representation of the blob's data bytes</param>
      public VectorType(String blobString)
      {
         _cells = new List<CellElement>();
         _initialized = false;
         _originalflatData = blobString;
         _encoding = Manager.Environment.GetEncoding();
      }

      /// <summary>
      ///   construct the object with a field
      /// </summary>
      /// <param name = "fld"></param>
      /// <param name = "chars"></param>
      public VectorType(FieldDef fld)
      {
         _cells = new List<CellElement>();
         _cellsAttr = fld.getCellsType();
         _cellContentType = fld.getVecCellsContentType();
         _cellsDefaultVal = fld.getCellDefualtValue();
         _cellsIsNullDefault = fld.isNullDefault();
         _cellSize = (_cellsAttr == StorageAttribute.UNICODE ? fld.getVecCellsSize()*2 : fld.getVecCellsSize());
         _initialized = true;
         _allowNull = fld.NullAllowed;
         _nullBuf = new StringBuilder();
         _dataBuf = new StringBuilder();
         _originalflatData = ToString();
         _encoding = Manager.Environment.GetEncoding();
      }

      /// <summary>
      ///   builds a VectorType from its flattened blob representation
      /// </summary>
      /// <param name = "blobString">a string representation of the blob's data bytes</param>
      /// <returns> false is the given blob is not a vector (the flatten format is wrong)</returns>
      private void init()
      {
         IEnumerator tokens = null;
         String currentToken = null; // the current token we are paring
         bool isFlatDataTranslated = isUnicode(_originalflatData);
         bool isDbcsAlpha = false;  // JPN: DBCS support

         // pos is position in the originalflatData string
         int pos = BlobType.blobPrefixLength(_originalflatData); // skips the ctrl data in the beginig of each
         // blob
         tokens = StrUtil.tokenize(_originalflatData.Substring(pos), ",").GetEnumerator();
         long vecSize = 0;

         if (!_initialized)
         {
            if (validateBlobContents(_originalflatData))
            {
               String blobPrefix = BlobType.getPrefix(_originalflatData);
               _cellContentType = BlobType.getContentType(blobPrefix);

               // skip the the headers of the table and the vector and the first ',';
               pos = (int) (pos + BLOB_TABLE_STR_LEN + VECTOR_STR_LEN + 1);
               tokens.MoveNext();

               // skip the version
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               pos += currentToken.Length + 1;

               // skip the ColumnsCount_ in vectors always contain value of 1
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               pos += currentToken.Length + 1;

               // parse the cell attribute in vector there is only one attribute fo the entire vector
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               _cellsAttr = (StorageAttribute)currentToken[0]; // in vector case only one cell attribute for all cells
               pos += 2; // skipps the delimiter too

               // parse the ColumnsLen_ that contains the number of cells in the vector
               // since there is only one column we increase pos by currentToken.length()
               // this number is same as the ColumnsTotalLen_ for vectors
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               _cellSize = Int32.Parse(currentToken);
               pos += currentToken.Length + 1;

               // parse the cell default value is it is numeric it is sent as NUM_TYPE
               // since the default value may contain as value charcters as the delimeter
               // we can not use the StringTokenizer.
               if (_cellsAttr == StorageAttribute.NUMERIC ||
                   _cellsAttr == StorageAttribute.DATE ||
                   _cellsAttr == StorageAttribute.TIME)
                  _cellsDefaultVal = StrUtil.stringToHexaDump(_originalflatData.Substring(pos, (int)(_cellSize)), 2);
               else if (_cellsAttr == StorageAttribute.ALPHA || _cellsAttr == StorageAttribute.MEMO)
               {
                  // QCR 429445 since the vector is encoded to base64/hex when it is recived from the server
                  // we must use the correct char-set when reciving vector of alpha or memo from the server
                  // in vectors we must concider the char set since when we got it from the server it was encoded
                  if (UtilStrByteMode.isLocaleDefLangDBCS())
                  {
                     isDbcsAlpha = true;
                     isFlatDataTranslated = true; // already traslated in Record.getString()
                     _cellsDefaultVal = UtilStrByteMode.leftB(_originalflatData.Substring(pos), (int)(_cellSize));
                  }
                  else
                     _cellsDefaultVal = _originalflatData.Substring(pos, (int) (_cellSize));
                  if (_encoding != null && !isFlatDataTranslated)
                  {
                     try
                     {
                        byte[] ba = ISO_8859_1_Encoding.getInstance().GetBytes(_cellsDefaultVal);
                        _cellsDefaultVal = _encoding.GetString(ba, 0, ba.Length);
                     }
                     catch (Exception)
                     {
                     }
                  }
               }
               else if (_cellsAttr == StorageAttribute.UNICODE)
               {
                  _cellsDefaultVal = _originalflatData.Substring(pos, (int) (_cellSize));

                  try
                  {
                     byte[] ba = ISO_8859_1_Encoding.getInstance().GetBytes(_cellsDefaultVal);
                     _cellsDefaultVal = Encoding.Unicode.GetString(ba, 0, ba.Length);
                  }
                  catch (Exception)
                  {
                  }
               }
               else
                  _cellsDefaultVal = _originalflatData.Substring(pos, (int) (_cellSize));
               if (isDbcsAlpha)
                  pos = pos + _cellsDefaultVal.Length + 1;
               else
                  pos = (int) (pos + _cellSize + 1); // sikp the ending ','

               // inrorder to continue using the tokenizer for the rest of the headers parsing
               // reinit it
               tokens = StrUtil.tokenize(_originalflatData.Substring(pos), ",").GetEnumerator();

               // parse the cells null default flag
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               _cellsIsNullDefault = DisplayConvertor.toBoolean(currentToken);
               pos += currentToken.Length + 1;

               // parse the cells null allowed flag
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               _allowNull = DisplayConvertor.toBoolean(currentToken);
               pos += currentToken.Length + 1;

               // skip the parsing of ColumnsTotalLen_
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               pos += currentToken.Length + 1;

               // parse the size of the vector ( RecordsCount_)
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               vecSize = Int32.Parse(currentToken);
               pos += currentToken.Length + 1;

               // the blobs_ will always contain value of 1 sine we have in vectors only one colmn
               tokens.MoveNext();
               currentToken = (String) tokens.Current;
               pos += currentToken.Length + 1;

               // blobs offset table in the vector case will alway contain only one value equals to 0
               // since there is only one colms in the vector
               if (_cellsAttr == StorageAttribute.BLOB ||
                   _cellsAttr == StorageAttribute.BLOB_VECTOR)
               {
                  tokens.MoveNext();
                  currentToken = (String) tokens.Current;
                  pos += currentToken.Length + 1;
               }

               // parse the data array
               String data;
               if (isDbcsAlpha)
               {
                  data = UtilStrByteMode.leftB(_originalflatData.Substring(pos), (int)(vecSize * _cellSize));
                  pos = pos + data.Length + 1;
               }
               else
               {
                  data = _originalflatData.Substring(pos, (int)(vecSize * _cellSize));
                  pos = (int)(pos + vecSize * _cellSize + 1);
               }

               // allocate the dataBuf
               _dataBuf = new StringBuilder(data.Length);

               if (_cellsAttr != StorageAttribute.ALPHA && _cellsAttr != StorageAttribute.MEMO &&
                   _cellsAttr != StorageAttribute.UNICODE)
                  _dataBuf.Append(data);

               // parse the null buf
               String nullBuf = _originalflatData.Substring(pos, (int) (vecSize));
               pos = (int) (pos + vecSize + 1);

               // save the nullBuf
               _nullBuf = new StringBuilder(nullBuf);

               // check the type of the cells
               if (_cellsAttr != StorageAttribute.BLOB &&
                   _cellsAttr != StorageAttribute.BLOB_VECTOR)
               {
                  for (int i = 0; i < vecSize; i++)
                  {
                     bool isNull = nullBuf[i] != '\x0000';
                     // numeric type are sent as NUM_TYPE so we translate them to hex
                     if (_cellsAttr == StorageAttribute.NUMERIC ||
                         _cellsAttr == StorageAttribute.DATE ||
                         _cellsAttr == StorageAttribute.TIME)
                        _cells.Add(
                           new CellElement(
                              StrUtil.stringToHexaDump(data.Substring((int)(i * _cellSize), ((int)(_cellSize))), 2), isNull));
                     else if (_cellsAttr == StorageAttribute.ALPHA ||
                              _cellsAttr == StorageAttribute.MEMO)
                     {
                        // QCR 429445 since the vector is encoded to base64/hex when it is recived from the
                        // server we must use the correct char-set when reciving vector of alpha or memo from the server
                        String cellData;
                        if (isDbcsAlpha)
                           cellData = UtilStrByteMode.midB(data, (int)(i * _cellSize), (int)_cellSize);
                        else
                           cellData = data.Substring((int)(i * _cellSize), (int)(_cellSize));

                        if (_encoding != null && !isFlatDataTranslated)
                        {
                           try
                           {
                              byte[] ba = ISO_8859_1_Encoding.getInstance().GetBytes(cellData);
                              cellData = _encoding.GetString(ba, 0, ba.Length);
                           }
                           catch (SystemException)
                           {
                           }
                        }
                        _dataBuf.Append(cellData);
                        _cells.Add(new CellElement(cellData, isNull));
                     }
                     else if (_cellsAttr == StorageAttribute.UNICODE)
                     {
                        String cellData = data.Substring((int) (i*_cellSize), ((int) (_cellSize)));
                        try
                        {
                           byte[] ba = ISO_8859_1_Encoding.getInstance().GetBytes(cellData);
                           cellData = Encoding.Unicode.GetString(ba, 0, ba.Length);
                        }
                        catch (SystemException)
                        {
                        }
                        _dataBuf.Append(cellData);
                        _cells.Add(new CellElement(cellData, isNull));
                     }
                     else
                        _cells.Add(new CellElement(data.Substring((int) (i*_cellSize), (int) (_cellSize)), isNull));
                  }
               }
               else
               {
                  // in case of vectors orf vectors or vectors of blobs we need to treat them differentlly
                  // each blob or vector is "flattened in the end of the null buff
                  // as a string in the following format:
                  // Blob_Size,ObjHandle,VariantIdx,Type,VecCellAttr,....data....;next_blob;
                  // we need to remember that er a blob come to the client noy via vector its format is:
                  // ObjHandle,VariantIdx,Type,VecCellAttr;data there for we need to make the adjustments
                  // in the parsing process and in vecGet and in toString
                  for (int i = 0; i < vecSize; i++)
                  {
                     tokens = StrUtil.tokenize(_originalflatData.Substring(pos), ",").GetEnumerator();

                     // parse blob size
                     tokens.MoveNext();
                     currentToken = (String) tokens.Current;
                     long size = Int32.Parse(currentToken);
                     pos += currentToken.Length + 1;

                     // parse the rest of the blob header
                     String blobHeader = "";
                     for (int j = 0; j < GuiConstants.BLOB_PREFIX_ELEMENTS_COUNT; j++)
                     {
                        tokens.MoveNext();
                        currentToken = (String) tokens.Current;
                        blobHeader = blobHeader + currentToken;
                        pos += currentToken.Length + 1;

                        if (j != GuiConstants.BLOB_PREFIX_ELEMENTS_COUNT - 1)
                           blobHeader += ",";
                     }


                     String cellData = _originalflatData.Substring(pos, (int) (size));

                     // add the cell to the vec
                     bool isNull = nullBuf[i] != '\x0000';
                     _cells.Add(new CellElement(cellData, isNull, blobHeader));
                     pos = (int) (pos + size + 1);
                  }
               }
               // If dataBuf was not built during "cells growth" - allocate it right now, in one-shot.
               if (_dataBuf.Length == 0)
                  _dataBuf = new StringBuilder(data.Length);

               _initialized = true;
            }
            else
               throw new ApplicationException("in VectorType.init wrong vector format");
         }
      }

      /// <summary>
      ///   return a flatten blob string representation we assume here that all changes are dynamically save into the
      ///   buffs each time the vector changes
      /// </summary>
      public override String ToString()
      {
         String res = ""; // take

         // lazy evaluation if the vector was not initialized it wasn't changed so return the original
         if (_initialized)
         {
            // build the headers
            res += (0 + "," + 0 + "," + ((char) 0) + "," + (char)_cellsAttr + "," + _cellContentType + ";");
               //the blob header of this blob
            res = res + buildHeadersString();
            // + dataBuf.toString() + "," + nullBuf.toString() + ",";

            if (_cellsAttr == StorageAttribute.UNICODE)
            {
               char[] dataBufCharArry = new char[_dataBuf.Length*2];

               for (int i = 0; i < _dataBuf.Length; i++)
               {
                  dataBufCharArry[i*2] = (char) (_dataBuf[i]%256);
                  dataBufCharArry[i*2 + 1] = (char) (_dataBuf[i]/256);
               }
               res = res + new String(dataBufCharArry) + ",";
            }
            else
               res = res + _dataBuf + ",";

            res = res + _nullBuf + ",";

            // in blobs and vector we do not update
            if (_cellsAttr == StorageAttribute.BLOB ||
                _cellsAttr == StorageAttribute.BLOB_VECTOR)
               res += getBlobsBuf();

            res += BLOB_TABLE_STR;
         }
         else
            res = _originalflatData;

         return res;
      }

      /// <summary>
      ///   returns the cells attribute ( the sane for all cells in the vector)
      /// </summary>
      public StorageAttribute getCellsAttr()
      {
         if (!_initialized)
            return getCellsAttr(_originalflatData);
         return _cellsAttr;
      }

      /// <summary>
      ///   returns the cells size ( same size for all cells)
      /// </summary>
      public long getCellSize()
      {
         long retVal = 0;

         if (!_initialized)
            retVal = getCellSize(_originalflatData);
         else
            retVal = _cellSize;

         // unicode cell size is saved internally as byte length since that is the way the server save it
         if (getCellsAttr() == StorageAttribute.UNICODE)
            retVal = retVal/2;

         return retVal;
      }

      /// <summary>
      ///   returns the vector size
      /// </summary>
      public long getVecSize()
      {
         if (!_initialized)
            return getVecSize(_originalflatData);
         return _cells.Count;
      }

      /// <summary>
      ///   returns the value of a give cell cells indexes start from 1 wrong indexes or indexes the does not exist
      ///   will return default value
      /// </summary>
      /// <param name = "idx">the cell index</param>
      /// <returns> a string representation of the cell value TODO: yariv check with rina what we should do with
      ///   blobs extra fields
      /// </returns>
      public String getVecCell(int idx)
      {
         String retVal = null;
         if (idx > 0)
         {
            init();
            if (idx <= getVecSize())
            {
               retVal = _cells[idx - 1].data;

               if (_cellsAttr == StorageAttribute.BLOB ||
                   _cellsAttr == StorageAttribute.BLOB_VECTOR)
                  retVal = _cells[idx - 1].blobFieldPrefix + ";" + retVal;

               // QCR 503691 the value of true or false in the server are the numeric values of 1 and 0
               if (StorageAttributeCheck.isTypeLogical(_cellsAttr))
                  retVal = (retVal[0] == 0 ? "0" : "1");

               if (_cells[idx - 1].isNull)
                  retVal = null;
            }
            else if (!_cellsIsNullDefault)
               retVal = _cellsDefaultVal;
         }
         return retVal;
      }

      /// <summary>
      /// Returns the cell values of a vector in string array.
      /// </summary>
      /// <returns></returns>
      public String[] GetCellValues()
      {
         String retVal = null;
         string[] cellValues = null;

         init();

         if (getVecSize() > 0)
         {
            cellValues = new string[getVecSize()];

            //Get the vector value
            for (int idx = 0; idx < getVecSize(); idx++)
            {
               retVal = _cells[idx].data;

               if (_cellsAttr == StorageAttribute.BLOB ||
                   _cellsAttr == StorageAttribute.BLOB_VECTOR)
                  retVal = _cells[idx].blobFieldPrefix + ";" + retVal;

               // QCR 503691 the value of true or false in the server are the numeric values of 1 and 0
               if (StorageAttributeCheck.isTypeLogical(_cellsAttr))
                  retVal = (retVal[0] == 0 ? "0" : "1");

               if (_cells[idx].isNull)
                  retVal = null;

               cellValues[idx] = retVal;
            }
         }
         else if (!_cellsIsNullDefault)
         {
            cellValues = new string[1];
            cellValues[0] = _cellsDefaultVal;
         }

         return cellValues;
      }

      /// <summary>
      ///   inserts or changes a cells value
      /// </summary>
      /// <param name = "idx">the cell index if the index is not sequential creats empty cell till the index</param>
      /// <param name = "newValue">the new value ( if it is a blob type or vector it contains the prefix of the control data</param>
      /// <retuns>  false if the index is wrong or the vector illformed </retuns>
      public bool setVecCell(int idx, String newValue, bool isNull)
      {
         bool res = false;
         bool createBufferValForNumType = true;
         try
         {
            if (idx > 0)
            {
               init();
               long localCellSize = (_cellsAttr == StorageAttribute.UNICODE ? _cellSize/2 : _cellSize);
               // trying to set null value when not allowed
               if (isNull && !_allowNull)
               {
                  isNull = false;
                  newValue = _cellsDefaultVal;
               }

               if (idx <= _cells.Count)
               {
                  CellElement curr = _cells[idx - 1];
                  // if the value passed is not null
                  if (!isNull)
                  {
                     // special treatment for blobs and vectors
                     if (_cellsAttr == StorageAttribute.BLOB ||
                         _cellsAttr == StorageAttribute.BLOB_VECTOR)
                     {
                        // set the data in the cell
                        int blobPrefixLength = BlobType.blobPrefixLength(newValue);
                        curr.blobFieldPrefix = newValue.Substring(0, (blobPrefixLength - 1));

                        // treat empty blob
                        if (newValue.Length > blobPrefixLength)
                           curr.data = newValue.Substring(blobPrefixLength);
                        else
                           curr.data = "";
                     }
                        // simple type
                     else
                     {
                        // QCR 503691 the value of true or false in the server are the numeric values of 1 and 0
                        if (StorageAttributeCheck.isTypeLogical(_cellsAttr))
                        {
                           curr.data = (DisplayConvertor.toBoolean(newValue)
                                           ? new StringBuilder().Append(1).ToString()
                                           : new StringBuilder().Append('\x0000').ToString());
                           newValue = curr.data;
                        }
                        else
                           curr.data = newValue;

                        // numeric types are represented in the data buf as num type so we
                        // translate them before inserting them to the buf
                        String dataBufVal = newValue;
                        if (_cellsAttr == StorageAttribute.NUMERIC ||
                            _cellsAttr == StorageAttribute.DATE ||
                            _cellsAttr == StorageAttribute.TIME)
                           dataBufVal = RecordUtils.byteStreamToString(dataBufVal);

                        if (UtilStrByteMode.isLocaleDefLangDBCS() && (_cellsAttr == StorageAttribute.ALPHA || _cellsAttr == StorageAttribute.MEMO))
                        {
                           byte[] baDataBuf = _encoding.GetBytes(_dataBuf.ToString());
                           byte[] baDataBufVal = _encoding.GetBytes(dataBufVal);
                           
                           // update data buf
                           for (int i = 0; i < baDataBufVal.Length && i < localCellSize; i++)
                              baDataBuf[((int)((idx - 1) * localCellSize) + i)] = baDataBufVal[i];

                           // we do not keep the whole size of the alpha
                           for (int i = baDataBufVal.Length; i < localCellSize; i++)
                              baDataBuf[((int)((idx - 1) * localCellSize) + i)] = (byte)(_cellsAttr == StorageAttribute.ALPHA ? ' ' : '\x0000');

                           _dataBuf = new StringBuilder(_encoding.GetString(baDataBuf, 0, baDataBuf.Length));
                           curr.data = _encoding.GetString(baDataBuf, (int)((idx - 1) * localCellSize), (int)localCellSize); 
                        }
                        else
                        {
                           // update data buf
                           // QCR 987943 trim the value if it is longer the the cell length
                           for (int i = 0; i < dataBufVal.Length && i < localCellSize; i++)
                              _dataBuf[((int) ((idx - 1)*localCellSize) + i)] = dataBufVal[i];

                           // in alpha and num type we do not keep the whole size of the alpha/NUM_TYPE
                           for (int i = dataBufVal.Length; i < localCellSize; i++)
                              _dataBuf[((int) ((idx - 1)*localCellSize) + i)] = (_cellsAttr ==
                                                                                StorageAttribute.ALPHA ||
                                                                                _cellsAttr ==
                                                                                StorageAttribute.UNICODE
                                                                                   ? ' '
                                                                                   : '\x0000');

                           // QCR 987943 trim the value if it is longer the the cell length
                           if (_cellsAttr == StorageAttribute.ALPHA ||
                               _cellsAttr == StorageAttribute.UNICODE || _cellsAttr == StorageAttribute.MEMO)
                              curr.data = _dataBuf.ToString().Substring(((int) ((idx - 1)*localCellSize)),
                                                                    (int) (localCellSize));
                        }
                     }
                     // update the null buf
                     _nullBuf[idx - 1] = '\x0000';
                     curr.isNull = false;
                  }
                     // if the value passed is null
                  else
                  {
                     curr.data = null;

                     // set the prefix flag that indicates whether the blob is vector or not
                     if (_cellsAttr == StorageAttribute.BLOB_VECTOR)
                        curr.blobFieldPrefix = BlobType.getEmptyBlobPrefix(((char) 1));
                     else
                        curr.blobFieldPrefix = BlobType.getEmptyBlobPrefix(((char) 0));
                     curr.isNull = true;
                     _nullBuf[idx - 1] = ((char) 1);

                     // relevant only for none blobs
                     for (int i = 0; i < localCellSize; i++)
                        _dataBuf[((int) ((idx - 1)*localCellSize) + i)] = '\x0000';

                     // update the null buf
                     _nullBuf[idx - 1] = ((char) 1);
                  }
                  res = true;
               }
                  // new record
               else
               {
                  String insertVal;
                  // chooses the value inserted to the skipped cells
                  if (_cellsAttr == StorageAttribute.BLOB ||
                      _cellsAttr == StorageAttribute.BLOB_VECTOR)
                  {
                     // append the is vector flag
                     if (_cellsAttr == StorageAttribute.BLOB_VECTOR)
                        insertVal = BlobType.getEmptyBlobPrefix(((char) 1));
                     else
                        insertVal = BlobType.getEmptyBlobPrefix(((char) 0));

                     if (!_cellsIsNullDefault)
                        insertVal = insertVal + _cellsDefaultVal; // concat regolar blob cntrol data
                  }
                  else if (_cellsIsNullDefault)
                     insertVal = null;
                     // simple null value
                  else
                     insertVal = _cellsDefaultVal; // default simple value

                  // QCR 503691 the value of true or false in the server are the numeric values of 1 and 0
                  if (insertVal != null && StorageAttributeCheck.isTypeLogical(_cellsAttr))
                     insertVal = (DisplayConvertor.toBoolean(insertVal)
                                     ? new StringBuilder().Append((char) 1).ToString()
                                     : new StringBuilder().Append('\x0000').ToString());

                  // create skipped records
                  // when a vector cell is set , if the cell is beyond the existing vector.
                  // we will fill the cells between vector size and set cell with default value.
                  // for example : if vector size is 0, and we do vecset (vec[5]). cell 5 needs to be set
                  // with the value. cells 1 to 4 will be created and set with the default value.
                  String dataBufVal = insertVal;

                  while (_cells.Count < idx)
                  {
                     if (_cellsAttr == StorageAttribute.BLOB ||
                         _cellsAttr == StorageAttribute.BLOB_VECTOR)
                     {
                        _cells.Add(new CellElement(insertVal.Substring(EMPTY_BLOB_PREFIX_LEN), _cellsIsNullDefault,
                                                  insertVal.Substring(0, EMPTY_BLOB_PREFIX_LEN)));
                        // update data buf
                        _dataBuf.Append(getNullString(BLOB_TYPE_STRUCT_SIZE));
                     }
                     else
                     {
                        _cells.Add(new CellElement(insertVal, _cellsIsNullDefault));

                        // update the data buf
                        if (insertVal != null)
                        {
                           // numeric types are represented in the data buf as num type so we
                           // translate them before inserting them to the buf
                           if (_cellsAttr == StorageAttribute.NUMERIC ||
                                _cellsAttr == StorageAttribute.DATE ||
                                _cellsAttr == StorageAttribute.TIME)
                           {
                              if (createBufferValForNumType)
                              {
                                 // create a buffer value for num type only once, and use it 
                                 // over and over again for all the cells to be initialized.
                                 // do the translation only once to improve performance.
                                 createBufferValForNumType = false;
                                 dataBufVal = RecordUtils.byteStreamToString(insertVal);
                              }                              
                           }

                           // dataBufVal either contains original insertVal, or a string of
                           // byteStream for num types.
                           _dataBuf.Append(dataBufVal);

                           // since we don't alway keep the whole alpha
                           int valLen;
                           if (UtilStrByteMode.isLocaleDefLangDBCS() && (_cellsAttr == StorageAttribute.ALPHA || _cellsAttr == StorageAttribute.MEMO))
                              valLen = UtilStrByteMode.lenB(dataBufVal);
                           else
                              valLen = dataBufVal.Length;

                           for (int i = valLen; i < localCellSize; i++)
                              _dataBuf.Append((_cellsAttr == StorageAttribute.ALPHA || _cellsAttr == StorageAttribute.UNICODE ? ' ' : '\x0000'));
                        }
                        else
                           _dataBuf.Append(getNullString(localCellSize));
                     }

                     // update the null buf
                     _nullBuf.Insert(_cells.Count - 1, (new[] {(_cellsIsNullDefault ? ((char) 1) : '\x0000')}));
                  }

                  // vector has been filled till the requested idx.
                  // now its time to set the requested cell.
                  res = setVecCell(idx, newValue, isNull);
               }
            }
         }
         catch (ApplicationException)
         {
            res = false;
         }
         return res;
      }

      /// <summary>
      ///   change the vector to fit the definitions of a different vector field
      /// </summary>
      /// <param name="field">the new field</param>
      public void adjustToFit(FieldDef field)
      {
         if (field.getType() == StorageAttribute.BLOB_VECTOR)
         {
            StorageAttribute srcAttr = getCellsAttr();
            StorageAttribute dstAttr = field.getCellsType();

            if (StorageAttributeCheck.isTheSameType(srcAttr, dstAttr))
            {
               init();

               // trim data if needed ( only for alpha or memo)
               if (StorageAttributeCheck.IsTypeAlphaOrUnicode(srcAttr) &&
                   StorageAttributeCheck.IsTypeAlphaOrUnicode(dstAttr))
               {
                  int dstSizeInChars = field.getVecCellsSize();
                  int srcSizeInChars = (int) (_cellsAttr == StorageAttribute.UNICODE ? _cellSize/2 : _cellSize);
                  bool isByteMode = UtilStrByteMode.isLocaleDefLangDBCS() && 
                                    StorageAttributeCheck.isTypeAlpha(dstAttr);

                  if (srcSizeInChars != dstSizeInChars)
                  {
                     StringBuilder adjData = new StringBuilder();

                     // goes over all cells in the vector
                     for (int i = 0; i < getVecSize(); i++)
                     {
                        CellElement curr = _cells[i];
                        // trim is needed
                        if (!curr.isNull)
                        {
                           // unicode cell size are saved internally as byte length while the field returns char
                           // length
                           if (srcSizeInChars > dstSizeInChars)
                           {
                              if (isByteMode)
                                 curr.data = UtilStrByteMode.leftB(curr.data, dstSizeInChars);
                              else
                                 curr.data = curr.data.Substring(0, dstSizeInChars);
                              adjData.Append(curr.data);
                           }
                           // padding is needed
                           else
                           {
                              StringBuilder tmpData = new StringBuilder();
                              tmpData.Append(curr.data);
                              int dataLen = isByteMode ? UtilStrByteMode.lenB(curr.data) : curr.data.Length;

                              // pad with blanks
                              for (int j = dataLen; j < dstSizeInChars; j++)
                                 tmpData.Append(' ');
                              adjData.Append(tmpData.ToString());

                              // update the data in the vector cell
                              curr.data = tmpData.ToString();
                           }
                        }
                        else
                        {
                           StringBuilder tmpData = new StringBuilder();
                           for (int j = 0; j < dstSizeInChars; j++)
                              tmpData.Append('\x0000');
                           adjData.Append(tmpData.ToString());

                           // update the data in the vector cell
                           curr.data = tmpData.ToString();
                        }
                     } // end loop
                     _dataBuf = adjData;
                  }
               }

               // QCR 747801 in the filed the size of numeric/date/time is thier hex size
               // whereas here it is their NUM_TYPE size
               int newSize = field.getVecCellsSize();

               // change the headers data such as cell type and cells size
               _cellsAttr = field.getCellsType();
               _cellSize = (_cellsAttr == StorageAttribute.UNICODE ? newSize*2 : newSize);
               _cellsDefaultVal = field.getCellDefualtValue();
               _cellsIsNullDefault = field.isNullDefault();
               _allowNull = field.NullAllowed;
               _originalflatData = ToString();
            }
            else
               throw new ApplicationException("in VectorType.adjustToFit vector basic types does not agree");
         }
         else
            throw new ApplicationException("in  VectorType.adjustToFit " + field.getName() + " is not of type vector");
      }

      /// <summary>
      ///   update the vec size in the headers buf
      /// </summary>
      /// <param name = "idx">the new size</param>
      private String buildHeadersString()
      {
         String res = "";

         res = BLOB_TABLE_STR + VECTOR_STR + "," + VERSION + "," + 1 + "," + (char)_cellsAttr + ",";
         res = res + _cellSize + ",";

         // append the default value
         String def = "";
         if (_cellsDefaultVal != null)
            if (_cellsAttr == StorageAttribute.NUMERIC ||
                _cellsAttr == StorageAttribute.DATE || _cellsAttr == StorageAttribute.TIME)
               def = RecordUtils.byteStreamToString(_cellsDefaultVal);
            else
               def = _cellsDefaultVal;

         // in string we do not keep the full length of the string in brwoser client
         if (UtilStrByteMode.isLocaleDefLangDBCS() && (_cellsAttr == StorageAttribute.ALPHA || _cellsAttr == StorageAttribute.MEMO))
            def = def + getEmptyString(_cellSize - UtilStrByteMode.lenB(def));
         else
            def = def + getEmptyString(_cellSize - def.Length);

         // continue building the headers
         res = res + def + "," + (_cellsIsNullDefault ? "1" : "0") + ",";
         res = res + (_allowNull ? "1" : "0") + "," + _cellSize + "," + _cells.Count + ",";
         res = res +
               ((_cellsAttr == StorageAttribute.BLOB ||
                 _cellsAttr == StorageAttribute.BLOB_VECTOR)
                   ? "1"
                   : "0") + ",";

         if (_cellsAttr == StorageAttribute.BLOB ||
             _cellsAttr == StorageAttribute.BLOB_VECTOR)
            res = res + "0" + ",";

         return res;
      }

      // returns a string with size null charecters
      private String getNullString(long size)
      {
         StringBuilder res = new StringBuilder();
         for (long i = 0; i < size; i++)
            res.Append('\x0000');

         return res.ToString();
      }

      // returns a string with size blank charecters
      private String getEmptyString(long size)
      {
         StringBuilder res = new StringBuilder();
         for (long i = 0; i < size; i++)
            res.Append('\x0020');

         return res.ToString();
      }

      /// <summary>
      ///   goes over the cells of the vectors and builds the data buf of blobs since it is the only thing not
      ///   updated dynamically
      /// </summary>
      private String getBlobsBuf()
      {
         StringBuilder res = new StringBuilder();
         for (int i = 0; i < _cells.Count; i++)
         {
            String data = "";
            int blobSize = 0;
            CellElement curr = _cells[i];

            if (curr.data != null)
            {
               data = curr.data;
               blobSize = data.Length;
            }

            res.Append(blobSize);
            res.Append("," + curr.blobFieldPrefix + "," + data + ";");
         }

         return (res + ",");
      }

      /*-------------------------------------------------------------------------------------*/
      /*                                                                                     */
      /* static utility methods */
      /*                                                                                     */
      /*-------------------------------------------------------------------------------------*/

      /// <summary>
      ///   this method checks if contents of a blob indicate that it is a valid verctor i.e. if the blob is in
      ///   vector's flatten format
      /// </summary>
      /// <param name = "blob">the string representation of contents of the blob</param>
      /// <returns> true if valid vector</returns>
      public static bool validateBlobContents(String blob)
      {
         bool valid = false;

         if (!String.IsNullOrEmpty(blob))
         {
            int start = BlobType.blobPrefixLength(blob);
            valid = String.Compare(blob, start, (BLOB_TABLE_STR + VECTOR_STR), 0, (int)(BLOB_TABLE_STR_LEN + VECTOR_STR_LEN),
                    true) == 0;
         }

         return valid;
      }

      /// <summary>
      ///   parses the cell attribute of the vector
      /// </summary>
      /// <param name = "blob">a vector in a flattened format</param>
      /// <returns> att the the vector's cells attribute</returns>
      public static StorageAttribute getCellsAttr(String blob)
      {
         if (validateBlobContents(blob))
         {
            String[] tokens = StrUtil.tokenize(blob.Substring(BlobType.blobPrefixLength(blob)), ",");

            // skip the MGBTMGVEC

            // skip the version

            // skip the ColumnsCount_

            // the next token is the cells attribute
            return (StorageAttribute)(tokens[3])[0];
         }
         else
            throw new ApplicationException("in static getCellsAttr the blob is in the wrong format");
      }

      /// <summary>
      ///   parses the size of each cell in the vector all cell has the same size except when the vector cells are
      ///   of type blob or vector
      /// </summary>
      /// <param name = "blob">a vector in a flattened format</param>
      /// <returns> the cells size or -1 if there is encoding problem</returns>
      public static long getCellSize(String blob)
      {
         StorageAttribute cellsType = getCellsAttr(blob);
         if (cellsType != StorageAttribute.BLOB &&
             cellsType != StorageAttribute.BLOB_VECTOR)
         {
            String[] tokens = StrUtil.tokenize(blob.Substring(BlobType.blobPrefixLength(blob)), ",");

            // skip the MGBTMGVEC

            // skip the version

            // skip the ColumnsCount_

            // skip the cells type

            // the next element is the cells size
            return Int64.Parse(tokens[4]);
         }
         else
            return Int32.MaxValue;
      }

      /// <summary>
      ///   parses the size of the vector
      /// </summary>
      /// <param name = "blob">a vector in a flattened format</param>
      /// <returns> the size of the vector</returns>
      public static long getVecSize(String blob)
      {
         if (validateBlobContents(blob))
         {
            int pos = BlobType.blobPrefixLength(blob);
            String[] tokens = StrUtil.tokenize(blob.Substring(pos), ",");

            // skip the MGBTMGVEC
            pos += tokens[0].Length + 1;

            // skip the version
            pos += tokens[1].Length + 1;

            // skip the ColumnsCount_
            pos += tokens[2].Length + 1;

            // skip the cells type
            pos += tokens[3].Length + 1;

            // skip the cells size
            String cellsSize = tokens[4];
            pos += cellsSize.Length + 1;

            // skip the cell default value and re-init the tokenizer
            // since the default value may contain the delimeter charecter as data
            pos += Int32.Parse(cellsSize) + 1;
            tokens = StrUtil.tokenize(blob.Substring(pos), ",");

            // skip is cell null default

            // skip is null allowed

            // skip columns total length

            // the next element is the vector size
            return Int64.Parse(tokens[3]);
         }
         else
            throw new ApplicationException("in static getVecSize the blob is in the wrong format");
      }

      /// <summary>
      ///   Trim cell strings in the vector if the cells are alpha or memo.  
      ///   Their length should be adjusted in the number of bytes, not the number of characters.
      ///   (DBCS support)
      /// </summary>
      /// <param name = "srcBlob">a vector in a flattened format</param>
      /// <returns> String</returns>
      public static String adjustAlphaStringsInFlatData(String srcBlob)
      {
         if (validateBlobContents(srcBlob))
         {
            StringBuilder destBuf = new StringBuilder();
            IEnumerator tokens;
            String strToken;
            int pos = 0;
            int cellSize = 0;
            int vecSize = 0;

            // copy Blob prefix
            pos = BlobType.blobPrefixLength(srcBlob);
            strToken = srcBlob.Substring(0, pos);
            destBuf.Append(strToken);

            tokens = StrUtil.tokenize(srcBlob.Substring(pos), ",").GetEnumerator();

            // copy the MGBTMGVEC
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the version
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the ColumnsCount_
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // check the cells type
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            if (((StorageAttribute)strToken[0]) != StorageAttribute.ALPHA && ((StorageAttribute)strToken[0]) != StorageAttribute.MEMO)
               return srcBlob;

            // copy the cells type
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the cells size
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            cellSize = Int32.Parse(strToken);
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the default value
            // note: the default value may contain the delimiter character as data
            strToken = UtilStrByteMode.leftB(srcBlob.Substring(pos), cellSize);
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            tokens = StrUtil.tokenize(srcBlob.Substring(pos), ",").GetEnumerator();

            // copy the cells null default flag
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the cells null allowed flag
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the column's total length
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy the vector size
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            vecSize = Int32.Parse(strToken);
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy blobs_
            tokens.MoveNext();
            strToken = (String) tokens.Current;
            destBuf.Append(strToken + ",");
            pos += strToken.Length + 1;

            // copy each vector         
            for (int i = 0; i < vecSize; i++)
            {
               // note: the default value may contain the delimiter character as data
               strToken = UtilStrByteMode.leftB(srcBlob.Substring(pos), cellSize);
               destBuf.Append(strToken);
               pos += strToken.Length;
            }

            // copy the rest
            strToken = srcBlob.Substring(pos);
            destBuf.Append(strToken);

            String destBlob = destBuf.ToString();
            return destBlob;
         }
         else
            return srcBlob;
      }

      /// <summary>
      ///   check if flatData content was translated to unicode or not. flatData contains the vector's cells in flat
      ///   (non-array) format. This is how the server sends us the data. However the server does not send it to us
      ///   in a UNICODE format. Thus we translate it to unicode during the first time we "de-serialize" the
      ///   flat-data into a vector. During the de-serialization operation during the first time we "de-serialize"
      ///   the flat-data into a vector. During the de-serialization operation we modify the BLOB-TABLe eye catcher
      ///   from upper case to lower case, this way we know if a translation was done or is needed.
      /// </summary>
      /// <param name = "name">flatData The vector's flat data</param>
      /// <returns> TRUE if the flat data is already in unicode format</returns>
      protected internal bool isUnicode(String flatData)
      {
         int start = BlobType.blobPrefixLength(flatData);
         String catcher = flatData.Substring(start, (int) BLOB_TABLE_STR_LEN);
         return (String.CompareOrdinal(catcher, BLOB_TABLE_STR) == 0);
      }

      #region Nested type: CellElement

      /// <summary>
      ///   this inner class represents a cell in the magic vector each data that the magic vector keeps per cell
      ///   will be kept in the CellElement
      /// </summary>
      protected internal class CellElement
      {
         internal String blobFieldPrefix;
         protected internal String data;
         protected internal bool isNull;

         // all this variables are used only if the vector cell is of type vector or blob

         // constructs a new cell element for vectors whos cells are not of type blob or vector
         protected internal CellElement(String val, bool is_null)
         {
            data = val;
            isNull = is_null;
         }

         // constructs a new cell element for vectors whos cells are of type blob or vector
         protected internal CellElement(String val, bool is_null, String ctrlData)
         {
            data = val;
            isNull = is_null;
            blobFieldPrefix = ctrlData;
         }
      }

      #endregion
   }
}