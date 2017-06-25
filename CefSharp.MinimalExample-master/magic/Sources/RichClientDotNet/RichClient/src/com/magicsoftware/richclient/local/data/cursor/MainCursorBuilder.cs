using com.magicsoftware.richclient.local.data.view;
using com.magicsoftware.gatewaytypes;

namespace com.magicsoftware.richclient.local.data.cursor
{
   /// <summary>
   /// Main cursor builder
   /// </summary>
   internal class MainCursorBuilder : CursorBuilder
   {
      public MainCursorBuilder(RuntimeRealView view) : base(view)
      {

      }
      /// <summary>
      /// claculate value of the inset flag
      /// </summary>
      /// <param name="cursorDefinition"></param>
      protected override void CalculateInsertFlag(CursorDefinition cursorDefinition)
      {
         cursorDefinition.SetFlag( CursorProperties.Insert);
 
      }

      /// <summary>
      /// calculate value of the delete flag
      /// </summary>
      /// <param name="cursorDefinition"></param>
      protected override void ClaculateDeleteFlag(CursorDefinition cursorDefinition)
      {
         cursorDefinition.SetFlag(CursorProperties.Delete);
      }

      protected override void CalculateLinkFlag(CursorDefinition cursorDefinition)
      {
        //main view is not link
      }
   }
}
