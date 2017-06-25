using System;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   public enum ClipFormats
   {
      FORMAT_USER = 0,
      FORMAT_TEXT = 1,
      FORMAT_OEM_TEXT = 2,
      FORMAT_RICH_TEXT = 3,
      FORMAT_HTML_TEXT = 4,
      FORMAT_HYPERLINK_TEXT = 5,
      FORMAT_DROP_FILES = 6,
      FORMAT_UNICODE_TEXT = 7,

      FORMAT_UNKNOWN = 99
   } ;

   public class DroppedData
   {
      private DataObject _dataContent;

      internal DroppedData()
      {
      }

      public int X { get; set; }
      public int Y { get; set; }

      // Store the current selection of the control on which drop occurs.
      public int SelectionStart;
      public int SelectionEnd;


      // only gui project is allowed to create an object

      /// <summary>
      ///   Clear the DataContent and the coordinates.
      /// </summary>
      internal void Clean()
      {
         _dataContent = null;
         X = Y = 0;
         SelectionStart = SelectionEnd = 0;
      }

      /// <summary>
      ///   Set the data content of the dropped data.
      /// </summary>
      /// <param name = "Data">dropped data</param>
      /// <param name = "Format">format of the data</param>
      internal void SetData(String Data, String Format)
      {
         if (_dataContent == null)
            _dataContent = new DataObject();

         _dataContent.SetData(Format, Data);
      }

      ///<summary>
      ///  Get the Data according to Format.
      ///</summary>
      ///<param name = "clipFormat">Magic specific format</param>
      /// <param name="userFormatStr">if userFormat is specified, then use it as a format string</param>
      ///<returns></returns>
      public String GetData(ClipFormats clipFormat, String userFormatStr)
      {
         String format = null;
         String strData = null;

         if (_dataContent == null)
            return "";

         format = GetFormatFromClipFormat(clipFormat);
         if (clipFormat == ClipFormats.FORMAT_USER && !String.IsNullOrEmpty(userFormatStr))
            format = userFormatStr;

         if (format != null && _dataContent.GetDataPresent(format))
            strData = _dataContent.GetData(format).ToString();

         return (strData);
      }

      /// <summary>
      ///   Check whether format is available in dropped data or not.
      /// </summary>
      /// <param name = "clipFormat"></param>
      /// <param name="userFormatStr">if userFormat is specified, then use it as a format string</param>
      /// <returns></returns>
      public bool CheckDropFormatPresent (ClipFormats clipFormat, String userFormatStr)
      {
         bool DataPresent = false;
         String format = GetFormatFromClipFormat(clipFormat);
         if (clipFormat == ClipFormats.FORMAT_USER && !String.IsNullOrEmpty(userFormatStr))
            format = userFormatStr;

         if (_dataContent != null && _dataContent.GetDataPresent(format))
            DataPresent = true;

         return DataPresent;
      }

      /// <summary>
      /// Sets the data to _dataContent, for all supported formats.
      /// </summary>
      /// <param name="Data">Data received in Drop event</param>
      public void SetDroppedData(IDataObject Data)
      {
         ClipFormats clipFormat = ClipFormats.FORMAT_UNKNOWN;
         String tmpData;

         Clean();    // Clear the previously dropped data.
         foreach (String format in Data.GetFormats())
         {
            tmpData = String.Empty;
            clipFormat = GetClipFormatFromFormat(format);
            if (clipFormat != ClipFormats.FORMAT_UNKNOWN)
            {
               if (format.Equals(DataFormats.FileDrop))
               {
                  if (Data.GetData(format) != null)
                  {
                     String[] files = (String[])Data.GetData(format);
                     tmpData = String.Join("|", files);
                  }
               }
               else
                  tmpData = Data.GetData(format).ToString();

               // Update data to Dropped Data.
               if (tmpData.Length != 0)
                  GuiUtils.DroppedData.SetData(tmpData, format);
            }
         }
      }

      /// <summary>
      ///   check Magic specific format is supported or not.
      /// </summary>
      /// <param name = "format"></param>
      /// <returns></returns>
      public static bool IsFormatSupported(ClipFormats format)
      {
         if (GetFormatFromClipFormat(format) != null)
            return true;
         return false;
      }

      /// <summary>
      ///   Returns the string format for a ClipFormat.
      /// </summary>
      /// <param name = "format">Magic specific format</param>
      /// <returns></returns>
      internal static String GetFormatFromClipFormat(ClipFormats format)
      {
         String Format = null;
         switch (format)
         {
            case ClipFormats.FORMAT_UNICODE_TEXT:
               Format = DataFormats.UnicodeText;
               break;

            case ClipFormats.FORMAT_TEXT:
               Format = DataFormats.Text;
               break;

            case ClipFormats.FORMAT_OEM_TEXT:
               Format = DataFormats.OemText;
               break;

            case ClipFormats.FORMAT_RICH_TEXT:
               Format = DataFormats.Rtf;
               break;

            case ClipFormats.FORMAT_HTML_TEXT:
               Format = DataFormats.Html;
               break;

            case ClipFormats.FORMAT_HYPERLINK_TEXT:
               Format = "HyperLink";
               break;

            case ClipFormats.FORMAT_DROP_FILES:
               Format = DataFormats.FileDrop;
               break;

            case ClipFormats.FORMAT_USER:
               Format = "UserFormat";
               break;
         }
         return Format;
      }

      /// <summary>
      ///  Translate the standard clipboard format into magic clip format.
      ///  It also check whether the UserFormat is supported or not.
      /// </summary>
      /// <param name = "format">Standard format</param>
      /// <returns>ClipFormat corrosponds to Standard format</returns>
      internal static ClipFormats GetClipFormatFromFormat(String format)
      {
         ClipFormats clipFormat = ClipFormats.FORMAT_UNKNOWN;

         if (format == DataFormats.UnicodeText)
            clipFormat = ClipFormats.FORMAT_UNICODE_TEXT;
         else if (format == DataFormats.Text)
            clipFormat = ClipFormats.FORMAT_TEXT;
         else if (format == DataFormats.OemText)
            clipFormat = ClipFormats.FORMAT_OEM_TEXT;
         else if (format == DataFormats.Rtf)
            clipFormat = ClipFormats.FORMAT_RICH_TEXT;
         else if (format == DataFormats.Html)
            clipFormat = ClipFormats.FORMAT_HTML_TEXT;
         else if (format == DataFormats.FileDrop)
            clipFormat = ClipFormats.FORMAT_DROP_FILES;
         else if (format == "HyperLink")
            clipFormat = ClipFormats.FORMAT_HYPERLINK_TEXT;
         else
         {
            // Format can be a User-defined format. 
            // Get Environment to check whether a format is a user-defined format or not.
            String usrFormat = Events.GetDropUserFormats();
            if (usrFormat.Length > 0)
            {
               String[] userFormats = usrFormat.Split(',');
               foreach (String userStr in userFormats)
               {
                  if (format == userStr)
                  {
                     clipFormat = ClipFormats.FORMAT_USER;
                     break;
                  }
               }
            }
         }
         return clipFormat;
      }
   }
}