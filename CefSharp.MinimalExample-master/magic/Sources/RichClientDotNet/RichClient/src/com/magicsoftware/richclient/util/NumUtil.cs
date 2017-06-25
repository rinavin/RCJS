using System;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.gui;
using com.magicsoftware.unipaas.management.data;
using com.magicsoftware.unipaas.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.richclient.util
{
   internal enum NumUtilOperation { ENCODE, DECODE };

   /// <summary> This class implements the methods for encoding/decoding numbers to/from regular
   /// format to the format used to transfer the numbers between the RC and the server.
   /// </summary>
   class NumUtil
   {
      /// <summary> convert numbers to our secret format
      /// </summary>
      /// <param name="fileName"></param>
      private static String Encode(String numStr, StorageAttribute attr, String picStr)
      {
         PIC pic = new PIC(picStr, attr, 0);
         NUM_TYPE num_type = new NUM_TYPE(numStr, pic, 0);

         String encoded_num = num_type.toXMLrecord();
         encoded_num = Base64.encode(RecordUtils.byteStreamToString(encoded_num), ClientManager.Instance.getEnvironment().GetEncoding());
         return XmlParser.escape(encoded_num);
      }

      /// <summary> convert numbers from our secret format 
      /// </summary>
      /// <param name="fileName"></param>
      private static String Decode(String numStr, StorageAttribute attr, String picStr)
      {
         String decoded_num = XmlParser.unescape(numStr);
         decoded_num = XmlParser.unescape(decoded_num);
         decoded_num = Base64.decodeToHex(decoded_num);

         NUM_TYPE num_type = new NUM_TYPE(decoded_num);
         PIC pic = new PIC(picStr, attr, 0);

         return num_type.toDisplayValue(pic);
      }

      /// <summary> Read numbers from file, convert to/from our format and write to output file
      /// </summary>
      /// <param name="fileName"></param>
      internal static void EncodeDecode(String[] args, NumUtilOperation operation)
      {
         String errorMsg = null;

         // Output file
         String fileName;
         if (operation == NumUtilOperation.ENCODE)
            fileName = args[0].Substring(ClientManager.ENCODE.Length);
         else
            fileName = args[0].Substring(ClientManager.DECODE.Length);

         System.IO.StreamWriter output = null;
         try
         {
            output = new System.IO.StreamWriter(fileName + ".out");
         }
         catch (Exception e)
         {
            errorMsg = e.Message;
         }

         // attribute, picture
         StorageAttribute attr = StorageAttribute.NUMERIC;
         String pic = null;
         for (int i = 1; i < args.Length; i++)
         {
            if (args[i].StartsWith("/Attribute=", StringComparison.OrdinalIgnoreCase))
               attr = (StorageAttribute)args[i].Substring(args[i].IndexOf('=') + 1).Trim()[0];
            else if (args[i].StartsWith("/Picture=", StringComparison.OrdinalIgnoreCase))
               pic = args[i].Substring(args[i].IndexOf('=') + 1);
         }
         if (pic == null)
         {
            switch (attr)
            {
               case StorageAttribute.NUMERIC:
                  pic = "N18.2";
                  break;
               case StorageAttribute.DATE:
                  pic = "DD/MM/YY";
                  break;
               case StorageAttribute.TIME:
                  pic = "HH:MM:SS";
                  break;
            }
         }

         // Input
         String numbers = HandleFiles.readToString(fileName);

         if (output != null && !String.IsNullOrEmpty(numbers))
         {
            string[] parsed = numbers.Split(new[] { ',' });
            String result = null;

            ClientManager.Instance.getEnvironment().setSignificantNumSize(10);
            ClientManager.Instance.getEnvironment().setDecimalSeparator('.');
            ClientManager.Instance.getEnvironment().setDateSeparator('/');
            ClientManager.Instance.getEnvironment().setTimeSeparator(':');

            foreach (String numStr in parsed)
            {
               if (numStr.Length == 0)
                  continue;
               switch (operation)
               {
                  case NumUtilOperation.ENCODE:
                     result = Encode(numStr.Trim(), attr, pic);
                     break;
                  case NumUtilOperation.DECODE:
                     result = Decode(numStr.Trim(), attr, pic);
                     break;
               }

               output.Write(result + ",");
            }

            output.Close();
         }
         else
         {
            if (String.IsNullOrEmpty(errorMsg))
            {
               if (operation == NumUtilOperation.ENCODE)
                  errorMsg = "MgxpaRIA.exe /NumericEncode=file-name (comma-delimited plain values) [/Attribute=<...>] [/Picture=<...>]";
               else
                  errorMsg = "MgxpaRIA.exe /NumericDecode=file-name (comma-delimited encoded values) [/Attribute=<...>] [/Picture=<...>]";
            }

            // This function is used only when RC is run for automatic testing. There are no GUI/worker thread, 
            // but a single simple thread, so there should be no problem to use GUI calls here
            System.Windows.Forms.MessageBox.Show(errorMsg,"Error:");
         }
      }
   }
}
