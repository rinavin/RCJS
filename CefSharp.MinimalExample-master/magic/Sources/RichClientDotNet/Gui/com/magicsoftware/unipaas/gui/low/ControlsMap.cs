using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using GuiMisc = com.magicsoftware.util.Misc;

#if PocketPC
using ToolStrip = com.magicsoftware.mobilestubs.ToolStrip;
using ToolStripItem = com.magicsoftware.mobilestubs.ToolStripItem;
using MenuItem = com.magicsoftware.controls.MgMenu.MgMenuItem;
using MainMenu = com.magicsoftware.controls.MgMenu.MgMainMenu;
#endif

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> This class is used to get a Widget for a given control/form/menu/etc. Widgets are kept in a HashMap. The
   /// key for the hashmap will be Control or Form or Menu objects themselves. Since we will always access this
   /// through GUI thread, we do not need any synchronization. Thus, we can use HashMap and not HashTable. For
   /// every control we will keep array of Widgets. Array will usually contain only 1 widget. For multi-line
   /// controls like radio, table the array will contain all its widgets. The place in the array will be according
   /// to "idx" The class will be implemented as Singleton.</summary>
   /// <author> rinav</author>
   internal sealed class ControlsMap : System.ICloneable
   {
      private readonly Hashtable _controlsMapHash;
      private static ControlsMap _instance;

      private ControlsMap()
      {
         _controlsMapHash = new Hashtable();
      }

      /// <summary>singleton</summary>
      internal static ControlsMap getInstance()
      {
         if (_instance == null)
            _instance = new ControlsMap();
         return _instance;
      }

      /// <summary> translate object to widget</summary>
      /// <param name="object">form, control or other magic object</param>
      /// <param name="idx">for multiline controls - index of the control, for others index must be 0</param>
      /// <returns> widget</returns>
      internal Object object2Widget(Object obj, int idx)
      {
         ArrayList controlsArray = (ArrayList)_controlsMapHash[obj];
         if (controlsArray == null || controlsArray.Count <= idx)
            return null;
         Object widget = controlsArray[idx];
         return (widget);
      }

      /// <summary> translate object to widget, used for not multiline controls</summary>
      /// <param name="object">form, control or other magic object</param>
      /// <returns>widget</returns>
      internal Object object2Widget(Object obj)
      {
         return object2Widget(obj, 0);
      }

      /// <summary> Gets all widgets for forms, control or other magic object</summary>
      /// <param name="object">form, control or other magic object</param>
      /// <returns></returns>
      internal ArrayList object2WidgetArray(Object obj, int line)
      {
         ArrayList controlsArray;
         if (line == GuiConstants.ALL_LINES)
            controlsArray = (ArrayList)_controlsMapHash[obj];
         else
         {
            controlsArray = new ArrayList();
            controlsArray.Add(object2Widget(obj, line));
         }
         return controlsArray;
      }

      /// <summary> sets Map data on a widget</summary>
      /// <param name="object"></param>
      /// <param name="idx"></param>
      /// <param name="control"></param>
      internal void setMapData(Object obj, int idx, Object widget)
      {
         TagData tagData = null;

         // it may be TableChild or ColumnManager
         if (obj is GuiMgForm)
         {
            tagData = (TagData)(((System.Windows.Forms.Control)widget).Tag);
            tagData.MapData = new MapData((GuiMgForm)obj);

         }
         else if (obj is GuiMgControl)
         {
            if (widget is Control)
            {
               // it may be TableChild or ColumnManager
               tagData = (TagData)(((Control)widget).Tag);
               tagData.MapData = new MapData((GuiMgControl)obj, idx);
            }
            else if (widget is TreeNode)
            {
               tagData = (TagData)(((TreeNode)widget).Tag);
               tagData.MapData = new MapData((GuiMgControl)obj, idx);

            }
            else if (widget is ToolStripItem)
            {
               tagData = (TagData)(((ToolStripItem)widget).Tag);
               tagData.MapData = new MapData((GuiMgControl)obj, idx);
            }
         }
         else if (obj is MenuReference)
         {
            if (widget is ToolStrip)
               tagData = (TagData)(((ToolStrip)widget).Tag);
            else if (widget is ToolStripItem)
               tagData = (TagData)(((ToolStripItem)widget).Tag);
            else if (widget is MainMenu)
               tagData = (TagData)(((MainMenu)widget).Tag);
            else if (widget is MenuItem)
               tagData = (TagData)(((MenuItem)widget).Tag);

            if (tagData != null)
               tagData.MapData = new MapData((MenuReference)obj);
         }
      }

      /// <summary> Adds new object->widget mapping, creates Mapdata on the Object</summary>
      /// <param name="object">form, control or other magic object</param>
      /// <param name="idx">for multiline controls - index of the control, for others index must be 0</param>
      /// <param name="control">object's widget</param>
      internal void add(Object obj, int idx, Object widget)
      {
         Debug.Assert(obj is GuiMgForm || obj is GuiMgControl || obj is GuiMgMenu || obj is MenuReference);

         ArrayList controlsArray = (ArrayList)_controlsMapHash[obj];
         if (controlsArray == null)
         {
            controlsArray = new ArrayList();
            _controlsMapHash[obj] = controlsArray;
         }
         int size = controlsArray.Count;

         if (idx >= size)
         {
            //TODO: implement ensureCapacity in MgArrayList and then use here
            //controlsArray.ensureCapacity(idx + 1); // performance improvement

            for (int i = size; i <= idx; i++)
               controlsArray.Insert(i, null); // make sure that array is allocated
         }

         // make sure that we do not overwrite an other widget
         // in table it is possible that we change table child
         // assert ((controlsArray.size() < idx) || (controlsArray.get(idx) == null));

         // set the widget at given index
         controlsArray[idx] = widget;

         // add MapData on the widget
         setMapData(obj, idx, widget);
      }

      /// <summary> Adds new object->widget mapping, creates Mapdata on the Object, used for not multiline controls</summary>
      /// <param name="object">object form, control or other magic object</param>
      /// <param name="control">for multiline controls - index of the control, for others index must be 0</param>
      internal void add(Object obj, Object widget)
      {
         add(obj, 0, widget);
      }

      /// <summary> Adds new object->widget mapping, creates Mapdata on the Object, used for not multiline controls</summary>
      /// <param name="object">object form, control or other magic object</param>
      /// <param name="control">for multiline controls - index of the control, for others index must be 0</param>
      internal void addToEnd(Object obj, Object widget)
      {
         int location = 0;
         ArrayList controlsArray = (ArrayList)_controlsMapHash[obj];
         if (controlsArray != null)
            location = controlsArray.Count;
         add(obj, location, widget);
      }

      /// <summary> removes Object from idx position in the array, does NOT shifts the rest of the array clears object form
      /// hash map, if there are no other widgets in the array</summary>
      /// <param name="object">form, control or other magic object</param>
      /// <param name="idx"></param>
      internal void remove(Object obj, int idx)
      {
         ArrayList controlsArray = (ArrayList)_controlsMapHash[obj];
         int size = controlsArray.Count;

         Debug.Assert(controlsArray != null && size > idx);

         controlsArray[idx] = null;

         // If current entry is last entry, then remove all empty elements at the end of the array
         if (idx == size - 1)
         {
            for (int i = size - 1; i >= 0; i--)
            {
               if (controlsArray[i] == null)
                  controlsArray.RemoveAt(i);
               else
                  break;
            }
         }

         if (controlsArray.Count == 0)
         {
            // remove object from the hashMap
            _controlsMapHash.Remove(obj);
         }
      }

      /// <summary> removes all Widgets of this object from the mapping</summary>
      /// <param name="obj">object form, control or other magic object</param>
      internal void remove(Object obj)
      {
         Debug.Assert(_controlsMapHash[obj] != null);
         _controlsMapHash.Remove(obj);
      }

      /// <summary> removes a range of elements from the ArrayList associated with 'obj'</summary>
      /// <param name="obj">object form, control or other magic object</param>
      /// <param name="startingIdx">starting index from which to remove</param>
      internal void removeFromIdx(Object obj, int startingIdx)
      {
         Debug.Assert(_controlsMapHash[obj] != null);
         ArrayList controlsArray = (ArrayList)_controlsMapHash[obj];
         int size = controlsArray.Count;
         if (startingIdx < size)
            controlsArray.RemoveRange(startingIdx, size - startingIdx);
      }

      /// <summary> returns mapping data for the Widget</summary>
      /// <param name="control"></param>
      /// <returns> MapData</returns>
      internal MapData getMapData(Object obj)
      {
         TagData tagData = null;
         if (obj is Control )
            tagData  = ((Control)obj).Tag as TagData;
         else if (obj is TreeNode)
            tagData  = ((TreeNode)obj).Tag as TagData;
         else if (obj is ToolStripItem)
            tagData  = ((ToolStripItem)obj).Tag as TagData;
         else if (obj is ToolStrip)
            tagData  = ((ToolStrip)obj).Tag as TagData;
         else if (obj is MenuItem)
            tagData  = ((MenuItem)obj).Tag as TagData;
         else if (obj is MainMenu)
            tagData  = ((MainMenu)obj).Tag as TagData;

         return tagData == null ? null : tagData.MapData;
      }

      /// <summary>get control's map data</summary>
      /// <param name="control"></param>
      /// <returns> MapData</returns>
      internal MapData getControlMapData(Control control)
      {
         TagData tagData = control.Tag as TagData;
         if (tagData != null)
            return tagData.MapData;
         return null;
      }

      /// <summary>get form's map data</summary>
      /// <param name="form"></param><returns></returns>
      internal MapData getFormMapData(Form form)
      {
         Control clientPanel;
         clientPanel = ((TagData)form.Tag).ClientPanel;
         if (clientPanel == null)
            clientPanel = form;
         MapData mapData = getMapData(clientPanel);
         return mapData;
      }

      /// <summary>
      /// return the Mg object of the wifget
      /// </summary>
      /// <param name="widget"></param>
      /// <returns></returns>
      internal object GetObjectFromWidget(object widget)
      {
         MapData mapData;
         object ret = null;

         if (widget is Form)
            mapData = getFormMapData((Form)widget);
         else
            mapData = getMapData(widget);

         if (mapData != null)
         {
            ret = mapData.getForm();
            if (ret == null)
               ret = mapData.getMenuReference();
            if (ret == null)
               ret = mapData.getControl();
         }

         return ret;
      }

      /// <summary> remove map data from the widget</summary>
      /// <param name="control"></param>
      internal static void removeMapData(Control control)
      {
         ((TagData)control.Tag).MapData = null;
      }

      /// <summary> check if this is magic widget</summary>
      /// <param name="control"></param>
      /// <returns></returns>
      internal static bool isMagicWidget(Control control)
      {
         return (control.Tag is TagData);
      }

      /// <summary> do not allow to clone singleton</summary>
      public Object Clone()
      {
         throw new Exception("CloneNotSupportedException");
         // that'll teach 'em
      }

      /// <summary> for printing purposes</summary>
      public override String ToString()
      {
         return GuiMisc.CollectionToString(_controlsMapHash);
      }
   }
}