using System;
using System.Diagnostics;
using System.Text;
using com.magicsoftware.unipaas.dotnet;
using com.magicsoftware.util;
using System.Collections.Generic;

namespace com.magicsoftware.unipaas.management.data
{
   public abstract class RecordUtils
   {
      /// <summary>
      ///   translate byte stream to String
      /// </summary>
      /// <param name = "result">of the translation</param>
      /// <param name = "stream">(in StringBuffer form) to translate from</param>
      private static String byteStreamToString(StringBuilder stream)
      {
         String currStr;
         char currChar;
         var result = new StringBuilder(stream.Length / 2);

         for (int indx = 0; indx < stream.Length; indx += 2)
         {
            currStr = "" + stream[indx] + stream[indx + 1];
            currChar = (char)Convert.ToInt32(currStr, 16);
            result.Append(currChar);
         }

         return result.ToString();
      }

      /// <summary>
      ///   translate byte stream to String
      /// </summary>
      public static String byteStreamToString(String stream)
      {
         return byteStreamToString(new StringBuilder(stream));
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="itemVal"></param>
      /// <param name="itemAttr"></param>
      /// <returns></returns>
      public static byte[] serializeItemVal(String itemVal, StorageAttribute itemAttr)
      {
         Debug.Assert(itemVal != null);

         string valueSize;
         string tmpBufLen = string.Empty;
         Byte []tmpBuf = null;
         List<byte> contentWithLength = new List<byte>();
         int pos = 0;
         int fldValLen = 0;
         String tempItemVal = string.Empty;
         int noOfPackets = 0;
         Byte []tmpNoOfPackets = null;
         String tmpStrNoOfPackets = string.Empty;

         switch (itemAttr)
         {
            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               NUM_TYPE numType = new NUM_TYPE(itemVal);
               tmpBuf = Misc.ToByteArray(numType.Data);
               break;

            case StorageAttribute.BOOLEAN:
               tmpBuf = Manager.Environment.GetEncoding().GetBytes(itemVal);
               break;

            case StorageAttribute.ALPHA:
               itemVal = StrUtil.rtrim(itemVal);
               valueSize = (Convert.ToString(UtilStrByteMode.lenB(itemVal), 16)).ToUpper();

               // add leading zeros (if needed)
               for (int j = 0; j < 4 - valueSize.Length; j++)
                  tmpBufLen += "0";
               tmpBufLen += valueSize;

               contentWithLength.AddRange(Manager.Environment.GetEncoding().GetBytes(tmpBufLen));
               contentWithLength.AddRange(Manager.Environment.GetEncoding().GetBytes(itemVal));
               tmpBuf = new byte[contentWithLength.Count];
               contentWithLength.CopyTo(tmpBuf);
               break;

            case StorageAttribute.UNICODE:
               itemVal = StrUtil.rtrim(itemVal);
               valueSize = (Convert.ToString(itemVal.Length, 16)).ToUpper();

               // add leading zeros (if needed)
               for (int j = 0; j < 4 - valueSize.Length; j++)
                  tmpBufLen += "0";
               tmpBufLen += valueSize;

               contentWithLength.AddRange(Manager.Environment.GetEncoding().GetBytes(tmpBufLen));
               contentWithLength.AddRange(Encoding.Unicode.GetBytes(itemVal));
               tmpBuf = new byte[contentWithLength.Count];
               contentWithLength.CopyTo(tmpBuf);
               break;

            case StorageAttribute.BLOB:
               pos = 0;
               // blob will be serialized in packet of size 0xFFFF. 
               // So format of serialized buffer for blob is 
               // no. of packets (n) + length1 + data1 + length2 + data2 + ......length n + datan
               fldValLen =  ISO_8859_1_Encoding.getInstance().GetByteCount(itemVal);

               noOfPackets = (int)fldValLen / 0xFFFF;

               tmpBufLen = "FFFF";
               tmpNoOfPackets = Manager.Environment.GetEncoding().GetBytes(tmpBufLen);

               for (int i = 0; i < noOfPackets; i++)
               {                  
                  tempItemVal = itemVal.Substring(pos, 0xFFFF);
                  pos += 0xFFFF;
                  contentWithLength.AddRange(tmpNoOfPackets);
                  contentWithLength.AddRange(ISO_8859_1_Encoding.getInstance().GetBytes(tempItemVal));
               }

               int lastPacketSize = fldValLen % 0xFFFF;

               if (lastPacketSize > 0)
               {                  
                  tempItemVal = itemVal.Substring(pos, (fldValLen) - (pos));
                  byte[] tempItemValBytes = ISO_8859_1_Encoding.getInstance().GetBytes(tempItemVal);                  

                  tmpBufLen = tempItemValBytes.Length.ToString("X4");
                  contentWithLength.AddRange(Manager.Environment.GetEncoding().GetBytes(tmpBufLen));
                  contentWithLength.AddRange(ISO_8859_1_Encoding.getInstance().GetBytes(tempItemVal));
                  noOfPackets++;
               }

               tmpStrNoOfPackets = noOfPackets.ToString("D4");
               tmpNoOfPackets = Manager.Environment.GetEncoding().GetBytes(tmpStrNoOfPackets);

               tmpBuf = new byte[contentWithLength.Count + tmpNoOfPackets.Length];

               tmpNoOfPackets.CopyTo(tmpBuf, 0);
               contentWithLength.CopyTo(tmpBuf, tmpNoOfPackets.Length);
               break;
         } //end of the type case block

         return tmpBuf;
      }
      /// <summary>
      ///   serialize an item (field/global param/...) to an XML format (applicable to be passed to the server).
      /// </summary>
      /// <param name = "itemVal">item's value</param>
      /// <param name = "itemAttr">item's attribute</param>
      /// <param name = "cellAttr">cell's attribute - relevant only if 'itemAttr' is vector</param>
      /// <param name = "toBase64">decide Base64 encoding is to be done</param>
      /// <returns>serialized buffer</returns>
      public static String serializeItemVal(String itemVal, StorageAttribute itemAttr, StorageAttribute cellAttr, bool toBase64)
      {
         Debug.Assert(itemVal != null);

         int significantNumSize = Manager.Environment.GetSignificantNumSize() * 2;

         String valueSize;
         int j;
         var tmpBuf = new StringBuilder();

         // for alpha type add the length of the value as hex number of 4 digits
         switch (itemAttr)
         {
            case StorageAttribute.NUMERIC:
            case StorageAttribute.DATE:
            case StorageAttribute.TIME:
               itemVal = !toBase64
                            ? itemVal.Substring(0, significantNumSize)
                            : Base64.encode(byteStreamToString(itemVal.Substring(0, significantNumSize)), Manager.Environment.GetEncoding());
               break;

            case StorageAttribute.ALPHA:
            case StorageAttribute.UNICODE:
               itemVal = StrUtil.rtrim(itemVal);
               int pos = 0;
               int fldValLen = itemVal.Length;

               do
               {
                  int nullChrPos = itemVal.IndexOf((Char)0, pos);
                  if (nullChrPos == -1)
                  {
                     valueSize = (Convert.ToString(fldValLen - pos, 16)).ToUpper();
                     // add leading zeros (if needed)
                     for (j = 0; j < 4 - valueSize.Length; j++)
                        tmpBuf.Append('0');
                     tmpBuf.Append(valueSize);

                     if (pos > 0)
                        itemVal = itemVal.Substring(pos, (fldValLen) - (pos));

                     pos = fldValLen;
                  }
                  else
                  {
                     // If NULL chars exist in the middle of the value - create a spanned record
                     // Turn on the high most bit in the length (to indicate a segment)
                     valueSize = (Convert.ToString(nullChrPos - pos + 0x8000, 16)).ToUpper();
                     tmpBuf.Append(valueSize);
                     tmpBuf.Append(itemVal.Substring(pos, (nullChrPos) - (pos)));

                     // Count number of consecutive NULL chars, and add their count to XML
                     for (j = 1; j < fldValLen - nullChrPos && itemVal[nullChrPos + j] == 0; j++)
                     { }
                     // add leading zeros (if needed)
                     valueSize = "0000" + (Convert.ToString(j, 16)).ToUpper();
                     tmpBuf.Append(valueSize.Substring(valueSize.Length - 4));

                     // Append a hex dump of special chars
                     for (pos = nullChrPos; j > 0; j--, pos++)
                     {
                        string tmpStr = "0" + (Convert.ToString(itemVal[nullChrPos], 16));
                        tmpBuf.Append(tmpStr.Substring(tmpStr.Length - 2));
                     }

                     // If special chars were last, add the length of the last segment (zero)
                     if (pos >= fldValLen)
                     {
                        tmpBuf.Append("0000");
                        itemVal = "";
                        break;
                     }
                  }
               } while (pos < fldValLen);
               break;


            case StorageAttribute.BLOB:
            case StorageAttribute.BLOB_VECTOR:
            case StorageAttribute.DOTNET:
               pos = 0;

               // convert dotnet object into magic equivalent and append as data into blob suffix.
               if (itemAttr == StorageAttribute.DOTNET)
               {
                  Object itmObj = null;
                  int key = BlobType.getKey(itemVal);
                  String itmMagicVal = "";

                  if (key != 0)
                     itmObj = DNManager.getInstance().DNObjectsCollection.GetDNObj(key);

                  // convert dotnet object into magic type
                  if (itmObj != null)
                  {
                     StorageAttribute magicType = DNConvert.getDefaultMagicTypeForDotNetType(itmObj.GetType());
                     itmMagicVal = DNConvert.convertDotNetToMagic(itmObj, magicType);

                     // append to dotnet blob as data
                     if (itmMagicVal.Length > 0)
                        itemVal = BlobType.addDataToDotNetBlob(itemVal, itmMagicVal, magicType);
                  }
               }

               fldValLen = itemVal.Length;

               if (UtilStrByteMode.isLocaleDefLangDBCS() && itemAttr == StorageAttribute.BLOB_VECTOR)
               {
                  if (cellAttr == StorageAttribute.ALPHA || cellAttr == StorageAttribute.MEMO)
                  {
                     itemVal = VectorType.adjustAlphaStringsInFlatData(itemVal);

                     // The flat data will be divided by 0x3FFF characters.
                     // Each segment will be size in 0x3FFF ~ 0x7FFF bytes.
                     // The size depends on the number of DBCS characters, not fixed in 0x7FFF. 
                     do
                     {
                        if (itemVal.Length < pos + 0x3FFF)
                        //(0x8000 - 1) / 2 = 0x3FFF
                        {
                           if (pos > 0)
                              itemVal = itemVal.Substring(pos);

                           valueSize = (Convert.ToString(UtilStrByteMode.lenB(itemVal), 16)).ToUpper();
                           // add leading zeros (if needed)
                           for (j = 0; j < 4 - valueSize.Length; j++)
                              tmpBuf.Append('0');
                           tmpBuf.Append(valueSize);

                           //hex encoding
                           itemVal = !toBase64
                                        ? StrUtil.stringToHexaDump(itemVal, 4)
                                        : Base64.encode(itemVal, true, Manager.Environment.GetEncoding());

                           pos = fldValLen;
                        }
                        else
                        {
                           String strSub = itemVal.Substring(pos, 0x3FFF);
                           // + 0x8000 ... to indicate not the last segment
                           valueSize = (Convert.ToString(UtilStrByteMode.lenB(strSub) + 0x8000, 16)).ToUpper();
                           tmpBuf.Append(valueSize);

                           //hex or base64 encoding
                           tmpBuf.Append(!toBase64
                                            ? StrUtil.stringToHexaDump(strSub, 4)
                                            : Base64.encode(strSub, true, Manager.Environment.GetEncoding()));

                           tmpBuf.Append("0000");
                           pos += 0x3FFF;
                        }
                     } while (pos < fldValLen);

                     break;
                  }
               }

               do
               {
                  if (fldValLen < pos + 0x7FFF)
                  //0x8000 -1 = 0x7FFF
                  {
                     valueSize = (Convert.ToString(fldValLen - pos, 16)).ToUpper();
                     // add leading zeros (if needed)
                     for (j = 0; j < 4 - valueSize.Length; j++)
                        tmpBuf.Append('0');
                     tmpBuf.Append(valueSize);

                     if (pos > 0)
                        itemVal = itemVal.Substring(pos, (fldValLen) - (pos));

                     //hex encoding
                     itemVal = !toBase64
                                  ? StrUtil.stringToHexaDump(itemVal, 4)
                                  : Base64.encode(itemVal, Manager.Environment.GetEncoding());

                     pos = fldValLen;
                  }
                  else
                  {
                     //to indicate the full segment
                     valueSize = "FFFF"; //(Integer.toHexString (0xFFFF)).toUpperCase() 
                     tmpBuf.Append(valueSize);

                     //hex or base64 encoding
                     if (!toBase64)
                        tmpBuf.Append(StrUtil.stringToHexaDump(itemVal.Substring(pos, 0x7FFF), 4));
                     else
                        tmpBuf.Append(Base64.encode(itemVal.Substring(pos, 0x7FFF), Manager.Environment.GetEncoding()));

                     tmpBuf.Append("0000");
                     pos += 0x7FFF;
                  }
               } while (pos < fldValLen);

               break;
         } //end of the type case block

         tmpBuf.Append(itemVal);
         return tmpBuf.ToString();
      }

      /// <summary> Deserializes an item's (field/global param/...) value </summary>
      /// <param name="itemVal">item's value</param>
      /// <param name="itemAttr">item's attribute</param>
      /// <param name="itemLen">item's length</param>
      /// <param name="useHex">indicates whether the itemVal is in Hex or Base64</param>
      /// <param name="cellAttr">cell's attribute - relevant only if 'itemAttr' is vector</param>
      /// <param name="parsedLen">out parameter. Returns the length of itemVal parsed</param>
      /// <returns></returns>
      public static String deSerializeItemVal(String itemVal, StorageAttribute itemAttr, int itemLen, bool useHex, StorageAttribute cellAttr, out int parsedLen)
      {
         String val = null;
         int idx = 0;
         int len, endIdx;
         StringBuilder suffixBuf = null;
         String tmp = null;

         if (itemAttr == StorageAttribute.ALPHA
             || itemAttr == StorageAttribute.UNICODE
             || itemAttr == StorageAttribute.BLOB
             || itemAttr == StorageAttribute.BLOB_VECTOR
             || itemAttr == StorageAttribute.DOTNET)
         {
            // first 4 characters are the length of the string (hex number)
            endIdx = idx + 4;
            tmp = itemVal.Substring(idx, (endIdx) - (idx));
            len = Convert.ToInt32(tmp, 16);
            idx = endIdx;
         }
         else if (itemAttr == StorageAttribute.BOOLEAN)
            len = 1;
         else
         {
            int significantNumSize = Manager.Environment.GetSignificantNumSize();
            if (useHex)
               len = significantNumSize * 2;
            else
               //if working in base64
               len = (((significantNumSize + 2) / 3) * 4);
         }
         // Oops, did we bumped into a spanned record (We identify it when the high-most bit is on)?
         if ((len & 0x8000) > 0)
         {
            suffixBuf = new StringBuilder();
            len = (len & 0x7FFF);

            if (itemAttr == StorageAttribute.BLOB || itemAttr == StorageAttribute.BLOB_VECTOR
                || itemAttr == StorageAttribute.DOTNET)
               if (useHex)
                  len *= 2;
               else
                  len = (((len + 2) / 3) * 4);

            parsedLen = getSpannedField(itemVal, len, idx, itemAttr, suffixBuf, useHex);
            val = suffixBuf.ToString();
            endIdx = idx + parsedLen;
         }
         else
         {
            if (itemAttr == StorageAttribute.BLOB
                || itemAttr == StorageAttribute.BLOB_VECTOR
                || itemAttr == StorageAttribute.DOTNET)
               if (useHex)
                  len *= 2;
               else
                  len = (((len + 2) / 3) * 4);

            endIdx = idx + len;
            if (endIdx > itemVal.Length)
               throw new ApplicationException("in Record.fillFieldsData() data string too short:\n" + itemVal);

            if (UtilStrByteMode.isLocaleDefLangDBCS() && itemAttr == StorageAttribute.BLOB_VECTOR)
               val = getString(itemVal.Substring(idx, (endIdx) - (idx)), itemAttr, useHex, (cellAttr == StorageAttribute.ALPHA));
            else
               val = getString(itemVal.Substring(idx, (endIdx) - (idx)), itemAttr, useHex);
         }

         idx = endIdx;

         if (itemAttr == StorageAttribute.ALPHA || itemAttr == StorageAttribute.UNICODE)
         {
            len = itemLen;
            val = StrUtil.padStr(val, len);

            if (itemAttr == StorageAttribute.ALPHA && UtilStrByteMode.isLocaleDefLangDBCS())
               val = UtilStrByteMode.leftB(val, len);
         }

         parsedLen = endIdx;
         return val;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="itemVal"></param>
      /// <param name="itemAttr"></param>
      /// <param name="itemLen"></param>
      /// <param name="offset"></param>
      /// <returns></returns>
      public static Object deSerializeItemVal(byte[] itemVal, StorageAttribute itemAttr, int itemLen, FldStorage fieldStorage, ref int offset)
      {
         Object val = null;
         int count = 0;
         int endIdx = 0;
         int len = 0;
         string tmp = string.Empty;
         StringBuilder suffixBuf;
         string tempVal = string.Empty;
         short noOfPackets = 0;


         switch (itemAttr)
         {
            case StorageAttribute.ALPHA:
               count = 4;
               endIdx = offset + count;
               tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               len = Convert.ToInt32(tmp, 16);

               count = len;
               offset = endIdx;
               val = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               break;

            case StorageAttribute.UNICODE:
               count = 4;
               endIdx = offset + count;
               tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               len = Convert.ToInt32(tmp, 16);

               count = len * 2;
               offset = endIdx;
               val = Encoding.Unicode.GetString(itemVal, offset, count);
               break;

            case StorageAttribute.BOOLEAN:
               count = 4;
               endIdx = offset + count;
               tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               len = Convert.ToInt32(tmp, 16);

               count = len;
               offset = endIdx;

               val = BitConverter.ToInt16(Manager.Environment.GetEncoding().GetBytes(Manager.Environment.GetEncoding().GetString(itemVal, offset, count)), 0);
               break;

            case StorageAttribute.BLOB:
               count = 2;
               endIdx = offset + count;
               tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               noOfPackets = BitConverter.ToInt16(Manager.Environment.GetEncoding().GetBytes(Manager.Environment.GetEncoding().GetString(itemVal, offset, count)), 0);

               offset = endIdx;

               count = 4;
               endIdx = offset + count;
               tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               len = Convert.ToInt32(tmp, 16);

               count = len;
               offset = endIdx;

               if ((len & 0x8000) > 0)
               {
                  suffixBuf = new StringBuilder();
                  count = getSpannedField(itemVal, len, offset, itemAttr, suffixBuf, false, noOfPackets);
                  tempVal = suffixBuf.ToString();
               }
               else
               {
                  if (UtilStrByteMode.isLocaleDefLangDBCS())
                     tempVal = ISO_8859_1_Encoding.getInstance().GetString(itemVal, offset, count);
                  else
                     tempVal = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               }

               switch (fieldStorage)
               {
                  case FldStorage.AnsiBlob:
                  case FldStorage.UnicodeBlob:
                     val = BlobType.getString(tempVal);
                     break;
                  case FldStorage.Blob:
                     if (UtilStrByteMode.isLocaleDefLangDBCS())
                        val = ISO_8859_1_Encoding.getInstance().GetBytes(BlobType.removeBlobPrefix(tempVal));
                     else
                        val = Manager.Environment.GetEncoding().GetBytes(BlobType.removeBlobPrefix(tempVal));
                     break;
               }


               break;

            case StorageAttribute.NUMERIC:
               {
                  count = 4;
                  endIdx = offset + count;
                  tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
                  len = Convert.ToInt32(tmp, 16);

                  count = len;
                  offset = endIdx;

                  switch (fieldStorage)
                  {
                     case FldStorage.NumericFloat:
                     case FldStorage.NumericSigned:
                        byte[] array = new byte[count];
                        Array.Copy(itemVal, offset, array, 0, array.Length);
                        if (fieldStorage == FldStorage.NumericFloat)
                           val = BitConverter.ToDouble(array, 0);
                        else
                           val = BitConverter.ToInt32(array, 0);
                        break;
                     case FldStorage.NumericPackedDec:
                     case FldStorage.NumericString:
                        val = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
                        break;
                  }
               }

               break;

            case StorageAttribute.TIME:
            case StorageAttribute.DATE:
               count = 4;
               endIdx = offset + count;
               tmp = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
               len = Convert.ToInt32(tmp, 16);

               count = len;
               offset = endIdx;
               switch (fieldStorage)
               {
                  case FldStorage.TimeInteger:
                  case FldStorage.DateInteger:
                     val = BitConverter.ToInt32(Manager.Environment.GetEncoding().GetBytes(Manager.Environment.GetEncoding().GetString(itemVal, offset, count)), 0);
                     break;

                  case FldStorage.TimeString:
                  case FldStorage.DateString:
                     val = Manager.Environment.GetEncoding().GetString(itemVal, offset, count);
                     break;
               }

               break;

         }

         offset = offset + count;

         return val;
      }

      /// <returns> right string in confidence with the type</returns>
      public static String getString(String str, StorageAttribute type, bool useHex)
      {
         return getString(str, type, useHex, false);
      }

      /// <returns> right string in confidence with the type</returns>
      protected static String getString(String str, StorageAttribute type, bool useHex, bool useEnvCharset)
      {
         String result;
         if (useHex)
         {
            if (type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR
                || type == StorageAttribute.DOTNET)
               result = byteStreamToString(str);
            else
               result = str;
         }
         else //working in base64
         {
            if (type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR
                || type == StorageAttribute.DOTNET)
               result = Base64.decode(str, (useEnvCharset ? Manager.Environment.GetEncoding() : null));
            else
               result = Base64.decodeToHex(str);
         }
         return result;
      }

      /// <summary>
      ///   Translate XML -> field value for a spanned ALPHA field, and return the actual
      ///   field's length. A spanned field may be accepted from the server when an 
      ///   ALPHA includes special chars such as NULL which cannot be escaped using a '\' (like,
      ///   for example, '\"').
      /// </summary>
      /// <param name = "fldsVal">string for parsing</param>
      /// <param name = "firstSegLen">length (in chars) of first segment.</param>
      /// <param name = "idx">index in string, from which the first segments data starts.</param>
      /// <param name = "type">of variable is looking for</param>
      /// <param name = "result">the result string, which will contain the parsed field content.</param>
      public static int getSpannedField(String fldsVal, int firstSegLen, int idx, StorageAttribute type,
                                        StringBuilder result, bool useHex)
      {
         int endIdx = idx + firstSegLen;
         int len;
         int begin = idx;
         char asciiCode;
         String tmp;
         StringBuilder suffixBuf = null;
         int parsedLen;

         if (endIdx > fldsVal.Length)
            throw new ApplicationException("in Record.getSpannedField() data string too short:\n" + fldsVal);

         // append first segment
         result.Remove(0, result.Length);
         result.Append(getString(fldsVal.Substring(idx, (endIdx) - (idx)), type, useHex));

         idx += firstSegLen;

         // next 4 characters are the length of the string (hex number) of special bytes.
         endIdx = idx + 4;
         tmp = fldsVal.Substring(idx, (endIdx) - (idx));
         len = Convert.ToInt32(tmp, 16);
         idx = endIdx;

         //if working in hex
         if (useHex)
            endIdx = idx + (len * 2);
         else
            endIdx = idx + (((len + 2) / 3) * 4);

         if (endIdx > fldsVal.Length)
            throw new ApplicationException("in Record.getSpannedField() data string too short:\n" + fldsVal);

         // append special chars, one by one
         while (idx < endIdx)
         {
            tmp = fldsVal.Substring(idx, 2);
            asciiCode = (char)Convert.ToInt32(tmp, 16);
            result.Append(asciiCode);
            idx += 2;
         }

         // next 4 chars are the length of next segment
         endIdx = idx + 4;
         tmp = fldsVal.Substring(idx, (endIdx) - (idx));
         len = Convert.ToInt32(tmp, 16);
         idx = endIdx;

         if ((len & 0x8000) > 0)
         {
            // Oops, next segment may also be spanned
            suffixBuf = new StringBuilder();
            len = (len & 0x7FFF);

            if (type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR)
               if (useHex)
                  len *= 2;
               //working in base64
               else
                  len = (((len + 2) / 3) * 4);

            parsedLen = getSpannedField(fldsVal, len, idx, type, suffixBuf, useHex);
            //after using recursive function teh suffixBuf is in the right transformed form
            result.Append(suffixBuf.ToString());

            idx += parsedLen;
         }
         else
         {
            if (type == StorageAttribute.BLOB || type == StorageAttribute.BLOB_VECTOR)
               if (useHex)
                  len *= 2;
               //working in base64
               else
                  len = (((len + 2) / 3) * 4);

            // next segment isn't spanned. It must be the last one.
            endIdx = idx + len;
            if (endIdx > fldsVal.Length)
               throw new ApplicationException("in Record.fillFieldsData() data string too short:\n" + fldsVal);

            result.Append(getString(fldsVal.Substring(idx, (endIdx) - (idx)), type, useHex));
            idx = endIdx;
         }

         return idx - begin;
      }

      /// <summary>
      /// Translate value of the spanned field to actual value. Field value will be in the format of length and data 
      /// [ (Length | data) ((Length | data)).....]
      /// This function will read length and data of each packet and return actual value.
      /// </summary>
      /// <param name="fldsVal"></param>
      /// <param name="firstSegLen"></param>
      /// <param name="idx"></param>
      /// <param name="type"></param>
      /// <param name="result"></param>
      /// <param name="useHex"></param>
      /// <param name="noOfPackets"> Total number of packets sent from the server</param>
      /// <returns></returns>
      public static int getSpannedField(byte[] fldsVal, int firstSegLen, int idx, StorageAttribute type,
                                        StringBuilder result, bool useHex, short noOfPackets)
      {
         int endIdx = idx + firstSegLen;
         int len;
         int begin = idx;
         //char asciiCode;
         String tmp;
         StringBuilder suffixBuf = null;
         int parsedLen;
         Encoding tmpEnc;
         if (UtilStrByteMode.isLocaleDefLangDBCS())
             tmpEnc = ISO_8859_1_Encoding.getInstance();
         else
             tmpEnc = Manager.Environment.GetEncoding();

         if (endIdx > fldsVal.Length)
            throw new ApplicationException("in Record.getSpannedField() data string too short:\n" + fldsVal);

         // append first segment
         result.Remove(0, result.Length);
         result.Append(tmpEnc.GetString(fldsVal, idx, firstSegLen));
         noOfPackets--;

         if (noOfPackets <= 0)
            return 0;

         idx += firstSegLen;

         // next 4 characters are the length of the string (hex number) of special bytes.
         endIdx = idx + 4;
         tmp = tmpEnc.GetString(fldsVal, idx, 4);
         len = Convert.ToInt32(tmp, 16);
         idx = endIdx;

         if (endIdx > fldsVal.Length)
            throw new ApplicationException("in Record.getSpannedField() data string too short:\n" + fldsVal);

         // next 4 chars are the length of next segment
         endIdx = idx + 4;
         tmp = tmpEnc.GetString(fldsVal, idx, 4);
         len = Convert.ToInt32(tmp, 16);
         idx = endIdx;

         if ((len & 0x8000) > 0)
         {
            // Oops, next segment may also be spanned
            suffixBuf = new StringBuilder();

            parsedLen = getSpannedField(fldsVal, len, idx, type, suffixBuf, useHex, noOfPackets);
            //after using recursive function teh suffixBuf is in the right transformed form
            result.Append(suffixBuf.ToString());
            idx += parsedLen;
         }
         else
         {
            // next segment isn't spanned. It must be the last one.
            endIdx = idx + len;
            if (endIdx > fldsVal.Length)
               throw new ApplicationException("in Record.fillFieldsData() data string too short:\n" + fldsVal);

            result.Append(tmpEnc.GetString(fldsVal, idx, len));
            idx = endIdx;
         }

         return idx - begin;
      }
   }
}
