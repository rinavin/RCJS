using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using com.magicsoftware.util.Xml;

namespace com.magicsoftware.unipaas.management.gui
{
   internal class ApplicationMenusSaxHandler : MgSAXHandler
   {
      private const int MNT_TYPE_MENU = 1;
      private const int MNT_TYPE_PROG = 2;
      private const int MNT_TYPE_ACTION = 3;
      private const int MNT_TYPE_OS = 4;
      private const int MNT_TYPE_LINE = 5;
      private const int MNT_TYPE_WINDOW_LIST = 7;

      private readonly List<MgMenu> _mgMenus;
      private readonly Stack _objectsStack;
      private MenuEntry _currentMenuEntry;
      private MgMenu _currentMgMenu;
      private CurrentObjectType _currentObjectType;
      private bool _inAccessKeyTag;
      private bool _inArgumentTag;
      private bool _inEventTag;
      private int _keyCode;
      private Modifiers _modifier;

      /// <summary>
      ///   ApplicationMenusSaxHandler C'tor - updates the passed menus object
      /// </summary>
      /// <param name = "menus">menus object to be updated</param>
      internal ApplicationMenusSaxHandler(List<MgMenu> menus)
      {
         _mgMenus = menus;
         _objectsStack = new Stack();
         _inEventTag = false;
         _inAccessKeyTag = false;
         _modifier = Modifiers.MODIFIER_NONE;
         _keyCode = - 1;
      }

      /// <summary>
      ///   Updates the current object (MgMenu or MenuEntry) by the parsed element values
      /// </summary>
      public override void startElement(String elementName, NameValueCollection attributes)
      {
         String str = "";
         int menuType = 0;
         bool doNothing = false;
         bool boolVal;

         switch (elementName)
         {
            case "Menu":
               _currentMgMenu = new MgMenu();
               pushCurrentObject(_currentMgMenu);
               _mgMenus.Add(_currentMgMenu);
               break;

            case "MenuEntry":
               // we do not allocate the menu entry object yet - depends on the menu type
               _currentObjectType = CurrentObjectType.MENU_TYPE_MENU_ENTRY;
               break;

            case "Name":
               if (isMgMenu())
                  _currentMgMenu.setName(attributes["val"]);
               else
                  _currentMenuEntry.setName(attributes["val"]);
               break;

            case "MenuType":
               {
                  MenuEntry newEntry = null;
                  if (!isMgMenu())
                  {
                     // resolve the menu entry type
                     menuType = getInt(attributes);
                     switch (menuType)
                     {
                        case MNT_TYPE_MENU:
                           newEntry = new MenuEntryMenu(_currentMgMenu);
                           break;

                        case MNT_TYPE_PROG:
                           newEntry = new MenuEntryProgram(_currentMgMenu);
                           break;

                        case MNT_TYPE_ACTION:
                           // we still do not know the type if the event (internal, system or user)
                           newEntry = new MenuEntryEvent(_currentMgMenu);
                           break;

                        case MNT_TYPE_OS:
                           newEntry = new MenuEntryOSCommand(_currentMgMenu);
                           break;

                        case MNT_TYPE_LINE:
                           // nothing to do in case of a separator
                           newEntry = new MenuEntry(GuiMenuEntry.MenuType.SEPARATOR, _currentMgMenu);
                           newEntry.setVisible(true, true, false, null);
                           break;

                        case MNT_TYPE_WINDOW_LIST:
                           newEntry = new MenuEntryWindowMenu(_currentMgMenu);
                           newEntry.setVisible(true, true, false, null);
                           break;

                        default:
                           doNothing = true;
                           break;
                     }

                     if (!doNothing)
                     {
                        // we need to attach the new menu entry to its parent menu
                        if (_objectsStack.Peek() is MgMenu)
                           _currentMgMenu.addSubMenu(newEntry);
                        else
                           ((MenuEntryMenu) _currentMenuEntry).addSubMenu(newEntry);
                        pushCurrentObject(newEntry);
                     }
                  }
                  break;
               }

            case "MenuUid":
               if (isMgMenu())
                  _currentMgMenu.setUid(getInt(attributes));
               else
                  _currentMenuEntry.setUid(getInt(attributes));
               break;

            case "Checked":
               boolVal = getBooleanValue(attributes);
               _currentMenuEntry.setChecked(boolVal, true);
               break;

            case "VISIBLE":
               boolVal = getBooleanValue(attributes);
               _currentMenuEntry.setVisible(boolVal, true, false, null);
               break;

            case "Enabled":
               boolVal = getBooleanValue(attributes);
               _currentMenuEntry.setEnabled(boolVal, false, false);
               break;

            case "IsParallel":
               boolVal = getBooleanValue(attributes);
               ((MenuEntryProgram)_currentMenuEntry).IsParallel = boolVal;
               break;

            case "ImageFor":
               str = attributes["val"];
               switch (str)
               {
                  case "B":
                     _currentMenuEntry.Imagefor = GuiMenuEntry.ImageFor.MENU_IMAGE_BOTH;
                     break;

                  case "M":
                     _currentMenuEntry.Imagefor = GuiMenuEntry.ImageFor.MENU_IMAGE_MENU;
                     break;

                  default:
                     _currentMenuEntry.Imagefor = GuiMenuEntry.ImageFor.MENU_IMAGE_TOOLBAR;
                     break;
               }
               break;

            case "Icon":
               String imageFile = Events.TranslateLogicalName(attributes["val"]);
               _currentMenuEntry.ImageFile = imageFile;
               break;

            case "ToolNumber":
               _currentMenuEntry.ImageNumber = getInt(attributes);
               break;

            case "ToolGroup":
               _currentMenuEntry.ImageGroup = getInt(attributes);
               break;

            case "Tooltip_U":
               _currentMenuEntry.toolTip(attributes["val"]);
               break;

            case "Description_U":
               if (isMgMenu())
                  _currentMgMenu.setText(attributes["val"]);
               else
                  _currentMenuEntry.setText(attributes["val"], true);
               break;

            case "Help":
               {
                  int help = getInt(attributes, "obj");
                  if (_currentMenuEntry is MenuEntryEvent)
                     ((MenuEntryEvent)_currentMenuEntry).Help = help;
                  else if (_currentMenuEntry is MenuEntryOSCommand)
                     ((MenuEntryOSCommand)_currentMenuEntry).Help = help;
                  else if (_currentMenuEntry is MenuEntryProgram)
                     ((MenuEntryProgram)_currentMenuEntry).Help = help;
                  break;
               }

            case "Prompt":
               {
                  String prompt = attributes["val"];
                  if (_currentMenuEntry is MenuEntryEvent)
                     ((MenuEntryEvent) _currentMenuEntry).Prompt = prompt;
                  else if (_currentMenuEntry is MenuEntryOSCommand)
                     ((MenuEntryOSCommand) _currentMenuEntry).Prompt = prompt;
                  else if (_currentMenuEntry is MenuEntryProgram)
                     ((MenuEntryProgram) _currentMenuEntry).Prompt = prompt;
                  break;
               }

            case "DestinationContext":
               {
                  String destContext = attributes["val"];
                  if (_currentMenuEntry is MenuEntryEvent)
                     ((MenuEntryEvent)_currentMenuEntry).DestinationContext = destContext;
                  break;
               }

            case "SourceContext":
               {
                  String val = attributes["val"];
                  ((MenuEntryProgram)_currentMenuEntry).SourceContext = (MenuEntryProgram.SrcContext)XmlParser.getInt(val);
                  break;
               }

            case "FieldID":
               {
                  String val = attributes["val"];
                  ((MenuEntryProgram)_currentMenuEntry).ReturnCtxIdVee = (char)XmlParser.getInt(val);
                  break;
               }

            case "Program":
               ((MenuEntryProgram) _currentMenuEntry).Idx = getInt(attributes, "obj");
               ((MenuEntryProgram) _currentMenuEntry).Comp = getInt(attributes, "comp");
               ((MenuEntryProgram) _currentMenuEntry).ProgramIsn = getInt(attributes, "ObjIsn");
               ((MenuEntryProgram)_currentMenuEntry).CtlIndex = getInt(attributes, "CtlIndex");
               (_currentMenuEntry).setEnabled((_currentMenuEntry).getEnabled(), true, false);
               break;

            case "PublicName":
               ((MenuEntryProgram) _currentMenuEntry).PublicName = attributes["val"];
               break;

            case "COPY_GLOBAL_PARAMS":
               {
                  bool copyGlobalParameters = false;

                  if (attributes["val"].Equals("Y"))
                     copyGlobalParameters = true;

                  ((MenuEntryProgram) _currentMenuEntry).CopyGlobalParameters = copyGlobalParameters;
                  break;
               }

            case "Arguments":
               if (_currentMenuEntry is MenuEntryProgram)
                  ((MenuEntryProgram) _currentMenuEntry).MainProgVars = new List<String>();
               else if (_currentMenuEntry is MenuEntryEvent)
                  ((MenuEntryEvent) _currentMenuEntry).MainProgVars = new List<String>();
               break;

            case "Argument":
               _inArgumentTag = true;
               break;

            case "Variable":
               if (_inArgumentTag)
               {
                  if (_currentMenuEntry is MenuEntryProgram)
                     ((MenuEntryProgram) _currentMenuEntry).MainProgVars.Add(attributes["val"]);
                  else if (_currentMenuEntry is MenuEntryEvent)
                     ((MenuEntryEvent) _currentMenuEntry).MainProgVars.Add(attributes["val"]);
               }
               break;

            case "Skip":
               if (_inArgumentTag && attributes["val"].Equals("Y"))
               {
                  if (_currentMenuEntry is MenuEntryProgram)
                     ((MenuEntryProgram) _currentMenuEntry).MainProgVars.Add("Skip");
                  else if (_currentMenuEntry is MenuEntryEvent)
                     ((MenuEntryEvent) _currentMenuEntry).MainProgVars.Add("Skip");
               }
               break;

            case "Ext":
               ((MenuEntryOSCommand) _currentMenuEntry).OsCommand = attributes["val"];
               break;

            case "Wait":
               ((MenuEntryOSCommand) _currentMenuEntry).Wait = getBooleanValue(attributes);
               break;

            case "Show":
               {
                  String val = attributes["val"];
                  ((MenuEntryOSCommand) _currentMenuEntry).Show = (CallOsShow) XmlParser.getInt(val);
                  break;
               }

            case "EventType":
               {
                  String val = attributes["val"];
                  if (val.Equals("U"))
                     _currentMenuEntry.setType(GuiMenuEntry.MenuType.USER_EVENT);
                  else if (val.Equals("I"))
                     _currentMenuEntry.setType(GuiMenuEntry.MenuType.INTERNAL_EVENT);
                  else if (val.Equals("S"))
                     _currentMenuEntry.setType(GuiMenuEntry.MenuType.SYSTEM_EVENT);
                  break;
               }

            case "Event":
               _inEventTag = true;
               break;

            case "Parent":
               if (_inEventTag)
                  ((MenuEntryEvent) _currentMenuEntry).UserEvtTaskId = attributes["val"];
               break;

            case "PublicObject":
               if (_inEventTag && (_currentMenuEntry).menuType() == GuiMenuEntry.MenuType.USER_EVENT)
               {
                  ((MenuEntryEvent) _currentMenuEntry).UserEvtIdx = getInt(attributes, "obj");
                  ((MenuEntryEvent) _currentMenuEntry).UserEvtCompIndex = getInt(attributes, "comp");
               }
               break;

            case "Modifier":
               if (_inEventTag && (_currentMenuEntry).menuType() == GuiMenuEntry.MenuType.SYSTEM_EVENT ||
                   _inAccessKeyTag)
                  _modifier = (Modifiers) attributes["val"][0];
               break;

            case "Key":
               if (_inEventTag && (_currentMenuEntry).menuType() == GuiMenuEntry.MenuType.SYSTEM_EVENT ||
                   _inAccessKeyTag)
                  _keyCode = getInt(attributes);
               break;

            case "InternalEventID":
               if (_inEventTag && (_currentMenuEntry).menuType() == GuiMenuEntry.MenuType.INTERNAL_EVENT)
               {
                  ((MenuEntryEvent) _currentMenuEntry).InternalEvent = getInt(attributes);
                  _currentMenuEntry.setEnabled((_currentMenuEntry).getEnabled(), true, false);
               }
               break;

            case "AccessKey":
               _inAccessKeyTag = true;
               break;

            case "PrgDescription":
               ((MenuEntryProgram) _currentMenuEntry).Description = attributes["val"];
               break;

            case "PrgFlow":
               ((MenuEntryProgram) _currentMenuEntry).Flow = attributes["val"][0];
               break;
         }
      }

      /// <summary>
      ///   Takes care of end element
      /// </summary>
      public override void endElement(String elementName, String elementValue)
      {
         if (elementName.EndsWith("Menu") || elementName.EndsWith("MenuEntry"))
            popCurrentObject();
         else if (elementName.Equals("Event"))
         {
            if (_keyCode != -1 || _modifier != Modifiers.MODIFIER_NONE)
            {
               // save the keyboard item into the menu entry
               ((MenuEntryEvent) _currentMenuEntry).KbdEvent = new KeyboardItem(_keyCode, _modifier);
               _keyCode = - 1;
               _modifier = Modifiers.MODIFIER_NONE;
            }
            (_currentMenuEntry).setEnabled((_currentMenuEntry).getEnabled(), true, false);
            _inEventTag = false;
         }
         else if (elementName.EndsWith("Argument"))
            _inArgumentTag = false;
         else if (elementName.Equals("AccessKey"))
         {
            if (_keyCode != -1 || _modifier != Modifiers.MODIFIER_NONE)
            {
               // save the keyboard item into the menu entry
               (_currentMenuEntry).AccessKey = new KeyboardItem(_keyCode, _modifier);
               _keyCode = - 1;
               _modifier = Modifiers.MODIFIER_NONE;
            }
            _inAccessKeyTag = false;
         }
      }

      private void pushCurrentObject(MgMenu obj)
      {
         _currentMgMenu = obj;
         _objectsStack.Push(_currentMgMenu);
         _currentObjectType = CurrentObjectType.MENU_TYPE_MENU;
      }

      private void pushCurrentObject(MenuEntry obj)
      {
         _currentMenuEntry = obj;
         _objectsStack.Push(_currentMenuEntry);
         _currentObjectType = CurrentObjectType.MENU_TYPE_MENU_ENTRY;
      }

      private void popCurrentObject()
      {
         _objectsStack.Pop();
         if ((_objectsStack.Count == 0))
         {
            _currentMgMenu = null;
            _currentMenuEntry = null;
         }
         else
         {
            if (_objectsStack.Peek() is MgMenu)
            {
               _currentMgMenu = (MgMenu) _objectsStack.Peek();
               _currentMenuEntry = null;
            }
            else
            {
               _currentMenuEntry = (MenuEntry) _objectsStack.Peek();
            }
         }
      }

      /// <summary>
      ///   *
      /// </summary>
      /// <returns> in indication if the current object is an MgMenu, returns false in case of a menuEntry</returns>
      private bool isMgMenu()
      {
         return (_currentObjectType == CurrentObjectType.MENU_TYPE_MENU);
      }

      private bool getBooleanValue(NameValueCollection atts)
      {
         return (atts["val"].Equals("Y"));
      }

      private int getInt(NameValueCollection atts)
      {
         return (getInt(atts, "val"));
      }

      private int getInt(NameValueCollection atts, String str)
      {
         String val = atts[str];
         if (val != null && val != "")
            return (Int32.Parse(val));
         else
            return (0);
      }

      #region Nested type: CurrentObjectType

      private class CurrentObjectType
      {
         internal static readonly CurrentObjectType MENU_TYPE_MENU = new CurrentObjectType();
         internal static readonly CurrentObjectType MENU_TYPE_MENU_ENTRY = new CurrentObjectType();
      }

      #endregion
   }
}
