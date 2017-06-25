using System;
using System.Text;
using System.Diagnostics;
using com.magicsoftware.util;

namespace com.magicsoftware.unipaas.management.data
{

   public class BlobType
   {
      public static char CONTENT_TYPE_UNKNOWN = '0';
      public static char CONTENT_TYPE_ANSI = '1';
      public static char CONTENT_TYPE_UNICODE = '2';
      public static char CONTENT_TYPE_BINARY = '3';

      /// <summary> returns the content type of the blob</summary>
      /// <param name="blob">
      /// </param>
      /// <returns>
      /// </returns>
      public static char getContentType(String blob)
      {
         try
         {
            String[] tokens = StrUtil.tokenize(blob, ",;");

            return tokens[4][0];
         }
         catch (Exception)
         {
            throw new ApplicationException(" in BlobType.getContentType blob is in invalid format");
         }
      }

      /// <summary> Creates an empty blob prefix with a given cell attribute</summary>
      /// <param name="vecCellAttr">in case this blob is vector the cells type else 0
      /// </param>
      /// <returns> an empty blob prefix without the ';" at the end
      /// </returns>
      public static String getEmptyBlobPrefix(char vecCellAttr)
      {
          return (String.Format("0,0,{0},{1},0", ((char)0), vecCellAttr));
      }

      /// <param name="contentType">
      /// </param>
      /// <returns>
      /// </returns>
      public static String getBlobPrefixForContentType(char contentType)
      {
          return (String.Format("0,0,{0},{0},{1};", ((char)0), contentType));
      }

      /// <summary> returns the header only</summary>
      /// <param name="str">
      /// </param>
      /// <returns>
      /// </returns>
      public static String getPrefix(String str)
      {
         int idx = str.IndexOf(';');
         return (str.Substring(0, idx + 1));
      }

      /// <param name="ContentType">
      /// </param>
      /// <returns>
      /// </returns>
      public static Encoding getEncodingFromContentType(char ContentType)
      {
         Encoding encoding;

         if (ContentType == CONTENT_TYPE_UNICODE)
            encoding = Encoding.Unicode;
         else if (ContentType == CONTENT_TYPE_ANSI)
            encoding = Manager.Environment.GetEncoding();
         else
            encoding = ISO_8859_1_Encoding.getInstance();

         return encoding;
      }

      /// <summary> This function converts ansi bytes to unicode bytes.
      /// </summary>
      /// <param name="BytesInMb"></param>
      /// <returns></returns>
      private static byte[] MbToUnicode(byte[] BytesInMb)
      {
         byte[] result = null;

         try
         {
            String UnicodeString = Manager.Environment.GetEncoding().GetString(BytesInMb, 0, BytesInMb.Length);
            result = Encoding.Unicode.GetBytes(UnicodeString);
         }
         catch (Exception)
         {
            result = null;
         }

         return result;
      }

      /// <summary> This function converts unicode bytes to ansi bytes.
      /// </summary>
      /// <param name="BytesInUnicode"></param>
      /// <returns></returns>
      private static byte[] UnicodeToMb(byte[] BytesInUnicode)
      {
         byte[] result = null;

         try
         {
            String UnicodeString = Encoding.Unicode.GetString(BytesInUnicode, 0, BytesInUnicode.Length);

            result = Manager.Environment.GetEncoding().GetBytes(UnicodeString);
         }
         catch (Exception)
         {
            result = null;
         }

         return result;
      }

      /// <summary> Assumes the given blob contains a string and returns it. If the content type of the blob is not Unicode
      /// then assume it is ANSI.
      /// 
      /// </summary>
      /// <param name="blob">a valid representation of a blob
      /// </param>
      /// <returns> the string contained by the blob
      /// </returns>
      public static String getString(String blob)
      {
         String result = null;
         byte[] bytes = null;

         if (isValidBlob(blob))
         {
            char contentType = getContentType(blob);
            if (contentType != CONTENT_TYPE_UNICODE)
               contentType = CONTENT_TYPE_ANSI;

            try
            {
               bytes = getBytes(blob);

               Encoding encoding = getEncodingFromContentType(contentType);
               result = encoding.GetString(bytes, 0, bytes.Length);

               int index = result.IndexOf('\0');
               if (index != -1)
                  result = result.Substring(0, index);
            }
            catch (Exception)
            {
               result = null;
            }
         }
         else
            Debug.Assert(false);

         return result;
      }

      /// <summary> Create a blob of the specified content type from the display string
      /// </summary>
      /// <param name="blobStr"></param>
      /// <param name="contentType"></param>
      /// <returns></returns>
      public static String createFromString(String blobStr, char contentType)
      {
         String blob = null;
         char srcContentType = contentType;
         byte[] bytes = null;

         if (contentType != CONTENT_TYPE_UNICODE)
            srcContentType = CONTENT_TYPE_ANSI;

         try
         {
            Encoding encoding = getEncodingFromContentType(srcContentType);

            bytes = encoding.GetBytes(blobStr);

            blob = createFromBytes(bytes, contentType);
         }
         catch (Exception)
         {
            blob = null;
         }

         return blob;
      }

      /// <summary> Get blob contents as byte array
      /// 
      /// </summary>
      /// <param name="blob">contents including blob prefix
      /// </param>
      /// <returns> byte array according to content type in blob prefix
      /// </returns>
      public static byte[] getBytes(String str)
      {
         byte[] bytes = null;
         String data = removeBlobPrefix(str);

         try
         {
            Encoding encoding = ISO_8859_1_Encoding.getInstance();
            bytes = encoding.GetBytes(data);
         }
         catch (Exception)
         {
            bytes = null;
         }

         return bytes;
      }

      /// <summary> Create a blob of the specified content type from the byte array
      /// 
      /// </summary>
      /// <param name="Bytes">
      /// </param>
      /// <param name="contentType">
      /// </param>
      /// <returns> string
      /// </returns>
      public static String createFromBytes(byte[] bytes, char contentType)
      {
         String blobStr = "";
         String blobPrefix;
         String blobData;

         blobPrefix = BlobType.getBlobPrefixForContentType(contentType);

         try
         {
            Encoding encoding = ISO_8859_1_Encoding.getInstance();
            blobData = encoding.GetString(bytes, 0, bytes.Length);
         }
         catch (Exception)
         {
            blobData = null;
         }

         blobStr = blobPrefix + blobData;

         return blobStr;
      }

      /// <param name="dest">
      /// </param>
      /// <param name="src">
      /// </param>
      /// <returns>
      /// </returns>
      public static String copyBlob(String dest, String src)
      {
         byte[] srcBytes;
         byte[] destBytes;

         if (src == null)
            return null;
         else if (dest == null)
            return src;

         srcBytes = BlobType.getBytes(src);

         char destContentType = getContentType(dest);
         char srcContentType = getContentType(src);

         if (srcContentType == CONTENT_TYPE_ANSI && destContentType == CONTENT_TYPE_UNICODE)
            destBytes = MbToUnicode(srcBytes);
         else if (srcContentType == CONTENT_TYPE_UNICODE && destContentType == CONTENT_TYPE_ANSI)
            destBytes = UnicodeToMb(srcBytes);
         else
            destBytes = srcBytes;

         dest = BlobType.createFromBytes(destBytes, destContentType);

         return dest;
      }

      /// <summary> removes blob prefix from source</summary>
      /// <param name="source">- blob value
      /// 
      /// 
      /// </param>
      public static String removeBlobPrefix(String source)
      {
         int idx;
         if (source != null)
         {
            idx = source.IndexOf(';');
            if (idx < 0)
               System.Console.Out.WriteLine("Error: invalid blob prefix");
            return source.Substring(idx + 1);
         }
         else
            return null;
      }

      /// <summary> checks if the given string is a valid blob</summary>
      /// <param name="blob">
      /// </param>
      /// <returns> true if the blob is valid
      /// </returns>
      public static bool isValidBlob(String blob)
      {
         bool isValid = true;

         // check for the existence of a prefix
         if (blob == null || blob.IndexOf(';') < 0)
            isValid = false;

         // check for a valid content type
         if (isValid)
         {
            char contentType = getContentType(blob);
            if (contentType != CONTENT_TYPE_ANSI && contentType != CONTENT_TYPE_BINARY && contentType != CONTENT_TYPE_UNICODE && contentType != CONTENT_TYPE_UNKNOWN)
               isValid = false;
         }

         // TODO: add more validation checks here

         return isValid;
      }

      public static String setContentType(String str, char contentType)
      {
         String result = "";

         try
         {
            int prefixLastIndex = str.IndexOf(";");
            String prefix = str.Substring(0, prefixLastIndex);

            int dataLength = str.Length - (prefix.Length + 1);
            String data = str.Substring(prefixLastIndex + 1, dataLength);

            String[] prefixTokens = StrUtil.tokenize(prefix, ",;");

            for (int i = 0; i < GuiConstants.BLOB_PREFIX_ELEMENTS_COUNT; i++)
            {
               if (i == 4)
                  result = result + contentType + ",";
               else
                  result = result + prefixTokens[i] + ",";
            }

            return result.Substring(0, result.Length - 1) + ";" + data;
         }
         catch (Exception)
         {
            throw new ApplicationException(" in BlobType.setContentType : invalid format");
         }
      }

      /// <summary> Replace the vector's cell attribute in the blob prefix by the specified one</summary>
      /// <param name="str">a valid blob string (i.e. prefix;data) </param>
      /// <param name="vecCellAttr">attribute to insert into the prefix </param>
      /// <returns> modified blob string </returns>
      public static String SetVecCellAttr(String str, StorageAttribute vecCellAttr)
      {
         String result = "";
         try
         {
            int prefixLastIndex = str.IndexOf(";");
            String prefix = str.Substring(0, prefixLastIndex);

            String data = str.Substring(prefixLastIndex + 1);

            String[] tokens = StrUtil.tokenize(prefix, ",;");

            for (int i = 0; i < GuiConstants.BLOB_PREFIX_ELEMENTS_COUNT; i++)
            {
               if (i == 3)
                  result = result + (char)vecCellAttr + ",";
               else
                  result = result + tokens[i] + ",";
            }

            return result.Substring(0, result.Length - 1) + ";" + data;
         }
         catch (Exception)
         {
            throw new ApplicationException(" in XMLparser.blobPrefixLength invalid format");
         }
      }

      /// <summary> Returns the Vector' cell attribute from the prefix. </summary>
      /// <param name="blobStr">A valid blob string  (i.e. prefix;data) </param>
      /// <returns></returns>
      public static char GetVecCellAttr(String blobStr)
      {
         try
         {
            String[] tokens = StrUtil.tokenize(blobStr, ",;");

            return tokens[3][0];
         }
         catch (Exception)
         {
            throw new ApplicationException(" in BlobType.GetVecCellAttr(): blob is in invalid format");
         }
      }

      /// <summary> Calculate the length of the blob prefix. The prefix is in the format:
      /// <tt>"ObjHandle,VariantIdx,ContentType,VecCellAttr;"</tt>. The length includes the commas and the
      /// semicolon.
      /// 
      /// </summary>
      /// <param name="blob">a valid blob
      /// </param>
      /// <returns> the blob prefix length
      /// </returns>
      public static int blobPrefixLength(String blob)
      {
         try
         {
            int prefixLength = blob.IndexOf(';') + 1;
            String prefix = blob.Substring(0, prefixLength);

            // check if the prefix is valid
            if (prefixLength > 0)
            {
               String[] tokens = StrUtil.tokenize(prefix, ",");
               if (tokens.Length == GuiConstants.BLOB_PREFIX_ELEMENTS_COUNT)
                  return prefixLength;
            }
         }
         catch (Exception)
         {
            // invalid prefix
         }

         throw new ApplicationException(" in XMLparser.blobPrefixLength invalid format");
      }

      /// <summary>
      /// create and return a blob prefix for a DotNet Object
      /// format: "<DNObjectCollectionKey>,0,E,,;"
      /// </summary>
      /// <param name="dnObjectCollectionKey"></param>
      /// <returns></returns>
      public static String createDotNetBlobPrefix(int dnObjectCollectionKey)
      {
          return (String.Format("{0},0,E,{1},{2};", dnObjectCollectionKey, ((char)0), CONTENT_TYPE_UNKNOWN));
      }

      /// <summary>
      /// gets the key from a blob string
      /// </summary>
      /// <param name="blobStr"></param>
      /// <returns></returns>
      public static int getKey(String blobStr)
      {
         String[] tokens = StrUtil.tokenize(blobStr, ",;");
         int key = 0;

         if (tokens.Length > 5)
         {
            try
            {
               key = int.Parse(tokens[0]);
            }
            catch (Exception) { }
         }

         return key;
      }

      /// <summary> checks if the given string is a valid blob</summary>
      /// <param name="blob">
      /// </param>
      /// <returns> true if the blob is valid
      /// </returns>
      public static bool isValidDotNetBlob(String blob)
      {
         bool isValid = true;

         // check for the existence of a prefix
         if (blob.IndexOf(';') < 0)
            isValid = false;

         // check for a valid content type
         if (isValid)
         {
            String[] token = StrUtil.tokenize(blob, ",;");

            isValid = false;

            try
            {
               if (token.Length > 3 && ((StorageAttribute)token[2].ToCharArray()[0]) == StorageAttribute.DOTNET)
                  isValid = true;
            }
            catch (Exception) { }
         }

         // TODO: add more validation checks here

         return isValid;
      }

      /// <summary> calculates the blob size held in the string
      /// </summary>
      /// <param name="blob"> The string holding the blob </param>
      /// <returns></returns>
      public static int getBlobSize(String blob)
      {
         int size = 0;

         try
         {
            String[] tokens = StrUtil.tokenize(blob, ",;");
            if (tokens.Length > 5)
            {
               // The blob size we're interested in is the total blob size - size of header parts and separators
               size = blob.Length;
               for (int i = 0; i < 5; i++)
               {
                  size -= tokens[i].Length; // header part size
                  size--;                   // token separator
               }
            }
         }
         catch (Exception) { }
         
         return size;
      }

      /// <summary>
      /// appends data into dotnet blob prefix
      /// </summary>
      /// <param name="blob">dotnet blob</param>
      /// <param name="data">data to add</param>
      /// <param name="type">storage type of data</param>
      /// <returns></returns>
      public static String addDataToDotNetBlob(String blob, String data, StorageAttribute type)
      {
         if (isValidDotNetBlob(blob))
         {
            String blobPrefix = getPrefix(blob);
            String blobData = "";
            char contentType = CONTENT_TYPE_ANSI;

            if (type == StorageAttribute.UNICODE)
               contentType = CONTENT_TYPE_UNICODE;

            // add content type into the prefix
            blobPrefix = setContentType(blobPrefix, contentType);

            // get the data
            if (data != null)
            {
               String  tmpBlob = createFromString(data, contentType);

               blobData = tmpBlob.Substring(blobPrefixLength(tmpBlob));
            }

            return blobPrefix + blobData;
         }
         return blob;
      }

      /// <summary>
      /// Parse content type
      /// </summary>
      /// <param name="contentType"></param>
      /// <returns></returns>
      public static char ParseContentType(int contentType)
      {
         char newContentType = ' ';
         switch(contentType)
         {
            case 0:
               newContentType = CONTENT_TYPE_UNKNOWN;
               break;
            case 1:
               newContentType = CONTENT_TYPE_ANSI;
               break;
            case 2:
               newContentType = CONTENT_TYPE_UNICODE;
               break;
            case 3:
               newContentType = CONTENT_TYPE_BINARY;
               break;
         }

         return newContentType;
      }
   }
}