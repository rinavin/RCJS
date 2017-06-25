using System;
using System.Text;

namespace com.magicsoftware.unipaas.gui
{
   /// <summary>
   ///   data for <kbditm>
   /// </summary>
   public class KeyboardItem
   {
      private readonly int _keyCode;
      private readonly Modifiers _modifier = Modifiers.MODIFIER_NONE; // Alt|Ctrl|Shift|None
      private readonly int _states;
      private int _actionId;

      /// <summary>
      ///   CTOR
      /// </summary>
      internal KeyboardItem()
      {
      }

      /// <summary>
      ///   CTOR
      /// </summary>
      /// <param name = "cKeyCode">key code
      /// </param>
      /// <param name = "cModifier">modifier key: Alt|Ctrl|Shift|None
      /// </param>
      public KeyboardItem(int cKeyCode, Modifiers cModifier)
         : this()
      {
         _keyCode = cKeyCode;
         _modifier = cModifier;
      }

      public KeyboardItem(int actionId_, int keyCode_, Modifiers modifier_, int states_)
      {
         _actionId = actionId_;
         _keyCode = keyCode_;
         _modifier = modifier_;
         _states = states_;
      }

      /// <summary>
      ///   returns the key code of the keyboard item
      /// </summary>
      public int getKeyCode()
      {
         return _keyCode;
      }

      /// <summary>
      ///   returns the modifier of the keyboard item
      /// </summary>
      public Modifiers getModifier()
      {
         return _modifier;
      }

      /// <summary>
      ///   returns states of the keyboard item
      /// </summary>
      public int getStates()
      {
         return (_states);
      }

      /// <summary>
      ///   compares this keyboard item to a given keyboard item and returns true if and only if
      ///   the keycode and the modifier are equal
      /// </summary>
      /// <param name = "kbdItm">the keyboard item to compare to
      /// </param>
      public bool equals(KeyboardItem kbdItm)
      {
         if (this == kbdItm || (kbdItm != null && _keyCode == kbdItm._keyCode && _modifier == kbdItm._modifier))
            return true;
         return false;
      }


      /// <summary>
      ///   get the action id
      /// </summary>
      public int getAction()
      {
         return _actionId;
      }

      /// <param name = "actionId">
      /// </param>
      public void setAction(int actionId)
      {
         _actionId = actionId;
      }

      /// <summary>
      ///   translate the Keyboard Item to its String representation
      /// </summary>
      /// <returns> string representation of the KeyBoard Item
      /// </returns>
      public override String ToString()
      {
         StringBuilder buffer = new StringBuilder();
         int counter = 0;

         switch (_modifier)
         {
            case Modifiers.MODIFIER_ALT:
               buffer.Append("Alt+");
               break;


            case Modifiers.MODIFIER_CTRL:
               buffer.Append("Ctrl+");
               break;


            case Modifiers.MODIFIER_SHIFT:
               buffer.Append("Shift+");
               break;

            case Modifiers.MODIFIER_SHIFT_CTRL:
               buffer.Append("Shift+Ctrl+");
               break;

            case Modifiers.MODIFIER_ALT_CTRL:
               buffer.Append("Alt+Ctrl+");
               break;

            case Modifiers.MODIFIER_ALT_SHIFT:
               buffer.Append("Shift+Alt+");
               break;

            case Modifiers.MODIFIER_NONE:
            default:
               break;
         }

         switch (_keyCode)
         {
            case GuiConstants.KEY_SPACE:
               buffer.Append("Space");
               break;


            case GuiConstants.KEY_PG_UP:
               buffer.Append("PgUp");
               break;


            case GuiConstants.KEY_PG_DOWN:
               buffer.Append("PgDn");
               break;


            case GuiConstants.KEY_END:
               buffer.Append("End");
               break;


            case GuiConstants.KEY_HOME:
               buffer.Append("Home");
               break;


            case GuiConstants.KEY_LEFT:
               buffer.Append("Left");
               break;


            case GuiConstants.KEY_UP:
               buffer.Append("Up");
               break;


            case GuiConstants.KEY_RIGHT:
               buffer.Append("Rght");
               break;


            case GuiConstants.KEY_DOWN:
               buffer.Append("Down");
               break;


            case GuiConstants.KEY_TAB:
               buffer.Append("Tab");
               break;


            case GuiConstants.KEY_INSERT:
               buffer.Append("Ins");
               break;


            case GuiConstants.KEY_DELETE:
               buffer.Append("Del");
               break;


            case GuiConstants.KEY_RETURN:
               buffer.Append("Ent");
               break;


            case GuiConstants.KEY_ESC:
               buffer.Append("Esc");
               break;


            case GuiConstants.KEY_BACKSPACE:
               buffer.Append("Back");
               break;


            default:
               if (_keyCode >= GuiConstants.KEY_F1 && _keyCode <= GuiConstants.KEY_F12)
                  // KEY_F1 = 112; KEY_F12 = 123;
               {
                  counter = _keyCode - GuiConstants.KEY_F1 + 1;
                  buffer.Append("F" + counter);
               }
               else if (_keyCode >= GuiConstants.KEY_0 && _keyCode <= GuiConstants.KEY_9)
                  // KEY_0 = 48;KEY_9 = 57;
               {
                  counter = _keyCode - GuiConstants.KEY_0;
                  buffer.Append(counter);
               }
               else if (_keyCode >= GuiConstants.KEY_A && _keyCode <= GuiConstants.KEY_Z)
                  // KEY_A = 65; KEY_Z = 90;
               {
                  counter = _keyCode - GuiConstants.KEY_A + 'A';
                  buffer.Append((char) counter);
               }
               else
                  buffer.Append("? (" + _keyCode.ToString() + ")");
               break;
         }
         return buffer.ToString();
      }
   }
}
