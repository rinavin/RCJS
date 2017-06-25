using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using com.magicsoftware.richclient.util;
using com.magicsoftware.unipaas.gui;
using com.magicsoftware.util;
using util.com.magicsoftware.util;

namespace com.magicsoftware.richclient.env
{
   /// <summary>
   ///   data for <kdbmap>...</kdbmap>
   /// </summary>
   internal class KeyboardMappingTable
   {
      private readonly Hashtable _kbdMap; // of KeyboardItem

      /// <summary>
      ///   CTOR
      /// </summary>
      internal KeyboardMappingTable()
      {
         _kbdMap = new Hashtable();
      }

      /// <summary>
      ///   computes the hashcode of a keyboard item
      /// </summary>
      /// <param name = "kbdKey">the keybord key identifier </param>
      /// <param name = "modifier">CTRL / ALT / SHIFT </param>
      /// <returns> hash code </returns>
      private Int32 getKeyboardItemHashCode(int kbdKey, Modifiers modifier)
      {
         return (Int32) (kbdKey*1000 + modifier);
      }

      /// <param name = "kbdKey">   keyboard </param>
      /// <param name = "modifier"> </param>
      /// <returns> action array for the corresponding hash code key of keyboard + modifier </returns>
      internal List<KeyboardItem> getActionArrayByKeyboard(int kbdKey, Modifiers modifier)
      {
         return (List<KeyboardItem>) _kbdMap[getKeyboardItemHashCode(kbdKey, modifier)];
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="keyMapXml"></param>
      internal void fillKbdMapTable(byte[] keyMapXml)
      {
         try
         {
            if (keyMapXml != null)
               new KbdMapTableSaxHandler(this, keyMapXml);
         }
         catch (Exception ex)
         {
            Logger.Instance.WriteExceptionToLog(ex);
         }
      }

      #region Nested type: KbdMapTableSaxHandler

      /// <summary>
      /// 
      /// </summary>
      internal class KbdMapTableSaxHandler : MgSAXHandlerInterface
      {
         private readonly KeyboardMappingTable _enclosingInstance;

         internal KbdMapTableSaxHandler(KeyboardMappingTable enclosingInstance, byte[] keyMapXml)
         {
            _enclosingInstance = enclosingInstance;
            MgSAXHandler mgSAXHandler = new MgSAXHandler(this);
            mgSAXHandler.parse(keyMapXml);
         }

         /// <summary>
         /// 
         /// </summary>
         /// <param name="elementName"></param>
         /// <param name="elementValue"></param>
         /// <param name="attributes"></param>
         public void endElement(String elementName, String elementValue, NameValueCollection attributes)
         {
            if (elementName == ConstInterface.MG_TAG_KBDITM)
            {
               List<KeyboardItem> kbdItemArray = null;
               Int32 arrayKey;
               int actionId = 0;
               int keyCode = 0;
               Modifiers modifier = Modifiers.MODIFIER_NONE;
               int states = 0;

               IEnumerator enumerator = attributes.GetEnumerator();
               while (enumerator.MoveNext())
               {
                  String attr = (String) enumerator.Current;
                  switch (attr)
                  {
                     case ConstInterface.MG_ATTR_ACTIONID:
                        actionId = Int32.Parse(attributes[attr]);
                        break;

                     case ConstInterface.MG_ATTR_KEYCODE:
                        keyCode = Int32.Parse(attributes[attr]);
                        break;

                     case ConstInterface.MG_ATTR_MODIFIER:
                        //modifier = (Modifiers)valueStr[0];
                        modifier = (Modifiers) (attributes[attr])[0];
                        break;

                     case ConstInterface.MG_ATTR_STATES:
                        states = Int32.Parse(attributes[attr]);
                        break;

                     default:
                        Logger.Instance.WriteExceptionToLog(
                           "There is no such tag in KeyboardItem class. Insert case to KeyboardItem.initElements() for: " +
                           attr);
                        break;
                  }
               }
               KeyboardItem kbdItm = new KeyboardItem(actionId, keyCode, modifier, states);
               arrayKey = _enclosingInstance.getKeyboardItemHashCode(kbdItm.getKeyCode(), kbdItm.getModifier());
               kbdItemArray = (List<KeyboardItem>) _enclosingInstance._kbdMap[arrayKey];
               if (kbdItemArray == null)
               {
                  kbdItemArray = new List<KeyboardItem>();
                  _enclosingInstance._kbdMap[arrayKey] = kbdItemArray;
               }
               kbdItemArray.Add(kbdItm);
            }
               // It should come in this function only for KbdMap tag or for Kbditm tag..For other tags we should write error to log
            else if (elementName != ConstInterface.MG_TAG_KBDMAP)
               Logger.Instance.WriteExceptionToLog("There is no such tag in FontTable.endElement(): " + elementName);
         }
      }

      #endregion
   }
}
