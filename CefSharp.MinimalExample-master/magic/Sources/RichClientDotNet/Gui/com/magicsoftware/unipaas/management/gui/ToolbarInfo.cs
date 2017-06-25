namespace com.magicsoftware.unipaas.management.gui
{
   internal class ToolbarInfo
   {
      private int _count;
      private MenuEntry _menuEntrySeperator;

      /// <summary>
      /// </summary>
      internal ToolbarInfo()
      {
         _count = 0;
         _menuEntrySeperator = null;
      }

      /// <summary> </summary>
      /// <returns> </returns>
      internal int getCount()
      {
         return _count;
      }

      /// <summary>
      /// </summary>
      /// <param name = "count"></param>
      internal void setCount(int count)
      {
         _count = count;
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      internal MenuEntry getMenuEntrySeperator()
      {
         return _menuEntrySeperator;
      }

      /// <summary>
      /// </summary>
      /// <param name = "menuEntrySeperator"></param>
      internal void setMenuEntrySeperator(MenuEntry menuEntrySeperator)
      {
         _menuEntrySeperator = menuEntrySeperator;
      }
   }
}