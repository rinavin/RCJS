using com.magicsoftware.richclient.local.data.gateways;
using com.magicsoftware.gatewaytypes;
using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.util;
using com.magicsoftware.unipaas.management.data;
using System.Diagnostics;
using com.magicsoftware.richclient.local.data.view.RangeDataBuilder;
using System;
using com.magicsoftware.richclient.util;
using com.magicsoftware.richclient.commands;
using com.magicsoftware.richclient.commands.ClientToServer;

namespace com.magicsoftware.richclient.local.data.commands
{
   /// <summary>
   /// locate in query command
   /// </summary>
   class LocalDataViewCommandLocateInQuery : LocalDataViewCommandFetchFirstChunk
   {
      int FldId { get; set; }
      string SearchString { get; set; }
      LocateQueryEventCommand Command { get; set; }
      bool ResetIncrementalSearch { get; set; }

      /// <summary>
      /// CTOR
      /// </summary>
      /// <param name="command"></param>
      public LocalDataViewCommandLocateInQuery(LocateQueryEventCommand command)
         : base(command)
      {
         UseFirstRecord = false;
         FldId = command.FldId;
         SearchString = command.IncrmentalSearchString;
         ResetIncrementalSearch = command.ResetIncrementalSearch;
         Command = command;
      }

      /// <summary>
      ///
      /// </summary>
      /// <returns></returns>
      internal override ReturnResultBase Execute()
      {
         ReturnResultBase result = new GatewayResult();

         if (ResetIncrementalSearch)
         {
            //resetting the search - use the new string only
            if (!String.IsNullOrEmpty(SearchString) && SearchString[0] == (char)0xFF)
               // 'backspace' - empty string
               LocalDataviewManager.IncrementalLocateString = "";
            else
               LocalDataviewManager.IncrementalLocateString = SearchString;
         }
         else
         {
            if (!String.IsNullOrEmpty(SearchString) && SearchString[0] == (char)0xFF)
            {
               // 'backspace' - remove the last character
               if(LocalDataviewManager.IncrementalLocateString.Length > 0)
                  LocalDataviewManager.IncrementalLocateString = LocalDataviewManager.IncrementalLocateString.Substring(0, LocalDataviewManager.IncrementalLocateString.Length - 1);
            }
            else
               //add the new string to already stored string
               LocalDataviewManager.IncrementalLocateString += SearchString;
         }

         
         SearchString = LocalDataviewManager.IncrementalLocateString;

         do
         {
            CalculateStartPositionFromLocateInstructions(SearchString);
            if (startPosition == null)
            {
               SearchString = SearchString.Remove(SearchString.Length - 1, 1);
            }
            else
            {
               break;
            }

         } while (!string.IsNullOrEmpty(SearchString));

         // perform the fetch only if a matching location was located
         if (startPosition != null)
         {
            result = base.Execute();
            LocalDataviewManager.IncrementalLocateString = SearchString;
            SetCursorOffset(SearchString);
         }
         

         return result;
      }

      /// <summary>
      /// set the cursor offset 
      /// </summary>
      /// <param name="searchString"></param>
      private void SetCursorOffset(string searchString)
      {
         StorageAttribute storageAttribute = TaskViews.Fields[FldId].StorageAttribute;
         // if alpha or unicode string, offset the cursor to after the searched string
         if (storageAttribute == StorageAttribute.ALPHA || 
            storageAttribute == StorageAttribute.UNICODE)
            Task.locateQuery.Offset = searchString.Length;
      }



      /// <summary>
      /// initialize the start position by creating and executing a fetch locate expression command
      /// </summary>
      private void CalculateStartPositionFromLocateInstructions(string searchString)
      {
         LocalDataViewCommandIncrementalLocate localDataViewCommandLocate = new LocalDataViewCommandIncrementalLocate(Command as LocateQueryEventCommand, searchString);
         localDataViewCommandLocate.DataviewManager = LocalDataviewManager;
         localDataViewCommandLocate.LocalManager = LocalManager;
         localDataViewCommandLocate.Execute();

         startPosition = localDataViewCommandLocate.ResultStartPosition;
      }
   }
}
