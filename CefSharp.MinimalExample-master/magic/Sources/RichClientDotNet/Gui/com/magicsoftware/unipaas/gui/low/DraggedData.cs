using System;
using System.Windows.Forms;
using com.magicsoftware.win32;

namespace com.magicsoftware.unipaas.gui.low
{
   public enum CursorType
   {
      CURSOR_TYPE_COPY = 1,
      CURSOR_TYPE_NONE = 2
   };

   public class DraggedData
   {
      private DataObject _DataContent;
      public DataObject DataContent 
      {
         get
         {
            if (_DataContent == null)
               _DataContent = new DataObject();

            return _DataContent;
         }
      }

      public Cursor CursorCopy { get; private set; }
      public Cursor CursorNone { get; private set; }

      // only GUI projects are allowed to create an object.
      internal DraggedData()
      {

      }

      /// <summary>
      ///  Maintain the dragged status : DragSet* functions are valid only under BeginDrag
      ///  handler. Hence we need this flag to decide whether to set the data/cursor.
      /// </summary>
      public bool IsBeginDrag { get; set; }

      /// <summary>
      ///  Clear all the data members.
      /// </summary>
      public void Clean()
      {
         _DataContent = null;
         CursorCopy = null;
         CursorNone = null;
         IsBeginDrag = false;
      }

      /// <summary> 
      ///  Set the data to be dragged to DataObject with its format. 
      /// </summary>
      /// <param name="Data"> data to be set for drag</param>
      /// <param name="Format"> magic format of the data </param>
      /// <param name="userFormatStr">if userFormat is specified, then use it as a format string</param>
      internal void SetData (String Data, ClipFormats clipFormat, String userFormatStr)
      {
         if (_DataContent == null)
            _DataContent = new DataObject();

         String formatStr = DroppedData.GetFormatFromClipFormat(clipFormat);
         if (clipFormat == ClipFormats.FORMAT_USER && !String.IsNullOrEmpty(userFormatStr))
            formatStr = userFormatStr;

         if (clipFormat == ClipFormats.FORMAT_DROP_FILES)
         {
            String[] files = Data.Split(',');
            GuiUtils.DraggedData._DataContent.SetData(formatStr, files);
         }
         else
            GuiUtils.DraggedData._DataContent.SetData(formatStr, Data);
      }

      /// <summary>
      ///  Create a cursors based on the cursor type.
      /// </summary>
      /// <param name="fileName">cursor file name</param>
      /// <param name="crsrType">cursor type</param>
      /// <returns> bool - returns true, if cursor is created successfully </returns>
      public bool SetCursor(String fileName, CursorType crsrType)
      {
         bool retVal = false;
         try
         {
            if (IsBeginDrag && fileName.Length != 0)
            {
               if (crsrType == CursorType.CURSOR_TYPE_COPY)
               {
                  if (CursorCopy != null)
                  {
                     CursorCopy.Dispose();
                     CursorCopy = null;
                  }
                  CursorCopy = CreateCursor(fileName);
                  if (CursorCopy != null)
                     retVal = true;
               }
               else if (crsrType == CursorType.CURSOR_TYPE_NONE)
               {
                  if (CursorNone != null)
                  {
                     CursorNone.Dispose();
                     CursorNone = null;
                  }
                  CursorNone = CreateCursor(fileName);
                  if (CursorNone != null)
                     retVal = true;
               }
            }
         }
         catch(Exception)
         {
            // TODO: Exception handling, if we won't be able to create a Cursor.
            Events.WriteExceptionToLog("Unable to create a cursor for File : " + fileName);
         }
         return retVal;
      }

      /// <summary>
      /// Create a cursor using the Native win32 call.
      /// Note : When we use overloaded constructor of SWF.Cursor(Stream/String) to create a 
      ///        cursor for an animated cursor, it throws bad image file exception.
      ///        Hence, we need to use WIN32 API LoadCursorFromFile
      /// </summary>
      /// <param name="fileName">cursor file name</param>
      /// <returns></returns>
      private Cursor CreateCursor(String fileName)
      {
         IntPtr hCursor = NativeWindowCommon.LoadCursorFromFile(fileName);

         if (!IntPtr.Zero.Equals(hCursor))
            return new Cursor(hCursor);
         else
            throw new Exception();
      }
   }

}
