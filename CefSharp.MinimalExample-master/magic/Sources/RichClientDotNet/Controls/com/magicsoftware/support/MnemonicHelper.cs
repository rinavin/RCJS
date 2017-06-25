using com.magicsoftware.controls;
using com.magicsoftware.controls.utils;
using com.magicsoftware.win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace com.magicsoftware.support
{

   public delegate Control GetSubFormControlmDelegate(object sender);
   public delegate bool IsSubFormControlDelegate(object sender);
   
   public static class MnemonicHelper
   {

      /// <summary>
      /// return true if we need to handel mnemonic for heb char 
      /// it will be true 2 will be true 
      /// 1. we in mgconst hebrew 
      /// 2. current input is not in hebrew 
      /// </summary>
      public static bool NeedToHandelMnemonicForHebChar
      {
         get
         {
            // only if we not in InputLanguage heb we need to handle the char
            return Utils.IsHebrew && !Utils.IsCurrentInputLanguageHebrew;
         }
      }

      // was "אבגדהוזחטיEEת@נסעףפץצקרשE;, -- to avoid compiler */
      // error in JPN environment      
      /*static long[] heb_key_map = new long[] { 0xe0, 0xe1, 0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7,0xe8, 0xe9,
                                         0xea, 0xeb, 0xec, 0xed, 0xee, 0xef, 0xf0, 0xf1, 0xf2, 0xf3,
                                         0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa, 0x00 };

      static char[] eng_key_map = new char[] { 'T','C','D','S','V','U','Z','J','Y','H',
                                        'L','F','K','O','N','I','B','X','G',';',
                                        'P','.','M','E','R','A',','};
                                        */

      static Dictionary<long, long> EngCharToHebCharDictionary;
                                                  

      static MnemonicHelper()
      {
         EngCharToHebCharDictionary = new Dictionary<long, long>()
         {
            { 'T', 0xe0 }, { 'C', 0xe1 }, { 'D', 0xe2 }, { 'S', 0xe3 }, { 'V', 0xe4 },
            { 'U', 0xe5 }, { 'Z', 0xe6 }, { 'J', 0xe7 }, { 'Y', 0xe8 }, { 'H', 0xe9 },
            { 'L', 0xea }, { 'F', 0xeb }, { 'K', 0xec }, { 'O', 0xed }, { 'N',0xee },
            { 'I', 0xef }, { 'B', 0xf0 }, { 'X', 0xf1 }, { 'G', 0xf2 }, { ';', 0xf3 },
            { 'P', 0xf4 }, { '.', 0xf5 }, { 'M', 0xf6 }, { 'E', 0xf7 }, { 'R', 0xf8 },
            { 'A', 0xf9 }, { ',', 0xfa },
         };             
      }

      /*------------------------------------------------------------------*/
      /* 20/2/97 Shay Z. Bug #766412 If Magic runs in English mode the    */
      /* Hebrew related fubnctions were not initialized - so the array    */
      /* wasn't set - so it caused a nice crash.                          */
      /* The pointer is now a static array, which holds the scan codes    */
      /* for Hebrew characters.                                           */
      /* The values of the array are based on the values of the arrray    */
      /* after running Magic in Hebrew.                                   */
      /* The array is used by the Acceleratoers handler:                  */
      /*------------------------------------------------------------------*/
      public static long[] HebCharScanCodePtr = new long[] { 0x14, 0x2E, 0x20, 0x1F, 0x2F, 0x16, 0x2C, 0x24, 0x15, 0x23,
                                               0x26, 0x21, 0x25, 0x18, 0x31, 0x17, 0x30, 0x2D, 0x22, 0x27,
                                               0x19, 0x34, 0x32, 0x12, 0x13, 0x1E, 0x33};


      public static char GetAcclerator(String checkText)
      {
         char acc = ' ';
         if (checkText != null && checkText.Length > 1)
         {
            int startAcc = checkText.IndexOf('&');
            if (startAcc >= 0 && startAcc + 1 < checkText.Length)
            {
               acc = checkText[startAcc + 1];
            }
         }

         return acc;
      }

      /// <summary>
      /// return true of the char is heb char 
      /// </summary>
      /// <param name="c"></param>
      /// <returns></returns>
      public static bool IsUnicodeHeb(long c)
      {
         return ((c) >= NativeWindowCommon.FIRST_UNICODE_HEB_CHAR && (c) <= NativeWindowCommon.LAST_UNICODE_HEB_CHAR);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="inputChar"></param>
      /// <param name="checkText"></param>
      /// <returns></returns>
      public static bool IsMatchToHebMnemonicForText(char inputChar, string checkText)
      {
         bool isMatchToHebMnemonic = false;

         // get the accelerator of the control 
         char acc = GetAcclerator(checkText);

         // If this Hebrew char 
         if (IsUnicodeHeb(acc))
         {
            // get the scan code of the char 
            long scanCodeOfAccelarator = GuiHebChar2ScanCode((long)acc);
            if (scanCodeOfAccelarator != 0)
            {
               char charInEngOgAccelarator = (char)NativeWindowCommon.MapVirtualKey((uint)scanCodeOfAccelarator, NativeWindowCommon.MAPVK_VSC_TO_VK);
               inputChar = Char.ToUpper(inputChar);

               if (inputChar.Equals(charInEngOgAccelarator))
                  isMatchToHebMnemonic = true;
            }
         }
         return isMatchToHebMnemonic;
      }


      /// <summary>
      /// in cpp : gui_heb_char_2_scan_code
      /// Return the Scan code of a a Ansi Hebrew character:
      /// </summary>
      /// <param name="c"></param>
      /// <returns></returns>
      public static long GuiHebChar2ScanCode(long c)
      {
         if (IsUnicodeHeb(c))
            return (HebCharScanCodePtr[c - NativeWindowCommon.FIRST_UNICODE_HEB_CHAR]);
         return 0;
      }    


      /// <summary>
      /// get the heb char for eng 
      /// </summary>
      /// <param name="engChar"></param>
      /// <returns></returns>
      public static string doWeirdMapping(char engChar)
      {
         Byte[] bytes = new byte[] { (byte)engChar, 0 };
         // get the hebrew encoding 
         Encoding hebrewEncoding = Encoding.GetEncoding("Windows-1255");

         return hebrewEncoding.GetString(bytes);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="engInputChar"></param>
      /// <returns></returns>
      public static  char GetHebCharFromEngChar(char engInputChar)
      {
         char retChar = ' ';
         if (engInputChar >= 'a' && engInputChar <= 'z')
            engInputChar = Char.ToUpper(engInputChar);

         long hebChar = 0;
         EngCharToHebCharDictionary.TryGetValue((long)engInputChar, out hebChar);

         if (hebChar > 0)
         {
            string cunicode = doWeirdMapping((char)hebChar);
            retChar = cunicode[0];
         }

         return retChar;
      }

      /// <summary>
      /// for hebrew char check the scan code of the accelerator is match the scan code of the inputChar
      /// </summary>
      /// <param name="menu"></param>
      /// <param name="inputChar"></param>
      /// <returns></returns>
      public static ToolStripItem GetMenuItemMatchToHebMnemonic(ToolStrip menu, char inputChar)
      {
         ToolStripItem retToolStrip = null;

         for (int i = 0; i < menu.Items.Count && retToolStrip == null; i++)
         {
            if (menu.Items[i].Visible && menu.Items[i].Enabled)
            {
               if (IsMatchToHebMnemonicForText(inputChar, menu.Items[i].Text))
               {
                  if (menu.Items[i].CanSelect)
                     retToolStrip = menu.Items[i];
               }
            }
         }
         return retToolStrip;
      }

      
      /// <summary>
      /// execute the menu item
      /// </summary>
      /// <param name="toolStripItem"></param>
      public static void ExecuteMenu(ToolStripItem toolStripItem)
      {
         // select and click the menu
         toolStripItem.Select();
         toolStripItem.PerformClick();
         
         // show the drop down menu
         if (toolStripItem is ToolStripMenuItem)
            (toolStripItem as ToolStripMenuItem).ShowDropDown();
      }

      /// <summary>
      /// get the input char and check if we have match for that input char in hebrew 
      /// get the menu and execute it 
      /// </summary>
      /// <param name="menu"></param>
      /// <param name="inputChar"></param>
      /// <returns></returns>
      public static bool HandleMnemonicForHebrew(ToolStrip menu, char inputChar)
      {
         bool handled = false;

         // for hebrew version and we not in LanguageHebrew
         if (MnemonicHelper.NeedToHandelMnemonicForHebChar)
         {
            // get the menu that match to Hebrew mnemonic
            ToolStripItem toolStripItem = MnemonicHelper.GetMenuItemMatchToHebMnemonic(menu, inputChar);
            if (toolStripItem != null)
            {
               // execute the menu
               MnemonicHelper.ExecuteMenu(toolStripItem);
               handled = true;
            }
         }

         return handled;
      }
   }
}
