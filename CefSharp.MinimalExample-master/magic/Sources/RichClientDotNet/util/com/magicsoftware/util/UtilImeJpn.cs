/// <summary>JPN: IME support
/// Utility Class for Input Method Editor
/// </summary>
/// <author>  Toshiro Nakayoshi (MSJ)
/// </author>
using System;
using System.Windows.Forms;
using com.magicsoftware.win32;

namespace com.magicsoftware.util
{
   public sealed class UtilImeJpn
   {
      private const int IME_CMODE_ALPHANUMERIC = 0;
      private const int IME_CMODE_NATIVE = 1;
      private const int IME_CMODE_KATAKANA = 2;
      private const int IME_CMODE_FULLSHAPE = 8;
      private const int IME_CMODE_ROMAN = 16;
      private const int IME_CMODE_NOCONVERSION = 256;
      private const int IME_NOT_INITIALIZED = -1;

      public const int IME_ZEN_HIRAGANA_ROMAN = 0x0001;
      public const int IME_FORCE_OFF = 0x000F;        // if (ImeMode property == 0 and (picture has K0 or PIC_S)) or 
                                                      // (attribute is not alpha, unicode, nor blob), set IME_FORCE_OFF.
      public const int IME_DISABLE = 10;              // to completely disable IME (even not allowing to change the mode)

      public bool ImeAutoOff { get; set; }
      public string StrImeRead { get; set; }
      
      private struct PrevImeOpenState
      {
         public bool IsContinue;
         public bool IsOpen;
      }
      private struct PrevImeConvMode
      {
         public bool IsContinue;
         public int  ConvMode;
      }
      private PrevImeOpenState _prevImeState;
      private PrevImeConvMode  _prevImeMode;

      /// <summary> CTOR</summary>
      public UtilImeJpn()
      {
         //prevIme = new PrevImeState();
         _prevImeState.IsContinue = false;
         _prevImeState.IsOpen = false;

         _prevImeMode.IsContinue = false;
         _prevImeMode.ConvMode = IME_NOT_INITIALIZED;
      }

      /// <summary> check if the IME mode is within valid range
      /// </summary>
      /// <param name="imeMode">(IME mode in Magic)
      /// </param>
      /// <returns> bool
      /// </returns>
      public bool isValid(int imeMode)
      {
         if ((0 <= imeMode && imeMode <= 9) || imeMode == IME_FORCE_OFF || imeMode == IME_DISABLE)
            return true;
         else
            return false;
      }

      /// <summary> convert the input method editor mode
      /// </summary>
      /// <param name="imeMode">(IME mode in Magic)
      /// </param>
      /// <returns> imeConvMode (IME conversion mode in imm32.lib)
      /// </returns>
      private static int imeMode2imeConvMode(int imeMode)
      {
         int imeConvMode;

         switch (imeMode)
         {
            // IME_ZEN_HIRAGANA_ROMAN
            case 1:
               imeConvMode = IME_CMODE_NATIVE | IME_CMODE_FULLSHAPE | IME_CMODE_ROMAN;
               break;

            // IME_ZEN_HIRAGANA
            case 2:
               imeConvMode = IME_CMODE_NATIVE | IME_CMODE_FULLSHAPE;
               break;

            // IME_ZEN_KATAKANA_ROMAN
            case 3:
               imeConvMode = IME_CMODE_NATIVE | IME_CMODE_KATAKANA | IME_CMODE_FULLSHAPE | IME_CMODE_ROMAN;
               break;

            // IME_ZEN_KATAKANA
            case 4:
               imeConvMode = IME_CMODE_NATIVE | IME_CMODE_KATAKANA | IME_CMODE_FULLSHAPE;
               break;

            // IME_HAN_KATAKANA_ROMAN
            case 5:
               imeConvMode = IME_CMODE_NATIVE | IME_CMODE_KATAKANA | IME_CMODE_ROMAN;
               break;

            // IME_HAN_KATAKANA
            case 6:
               imeConvMode = IME_CMODE_NATIVE | IME_CMODE_KATAKANA;
               break;

            // IME_ZEN_ALPHANUMERIC
            case 7:
               imeConvMode = IME_CMODE_FULLSHAPE;
               break;

            // IME_HAN_ALPHANUMERIC
            case 8:
            case 9:
               imeConvMode = IME_CMODE_ALPHANUMERIC;
               break;

            // case 0:
            // case IME_DISABLE:
            // case IME_FORCE_OFF:
            default:
               imeConvMode = IME_CMODE_NOCONVERSION;
               break;
         }

         return imeConvMode;
      }

      /// <summary> turn on/off the input method editor
      /// </summary>
      /// <param name="control">
      /// </param>
      public void controlIme(Control control, int imeMode)
      {
         if (control is TextBox || control is ComboBox || control is ListBox)
         {
            int hIMC = NativeWindowCommon.ImmGetContext(control.Handle);
            if (hIMC == 0)
               return;

            if (!isValid(imeMode))
               return;

            if (control is TextBox)
            {
               if (imeMode == IME_DISABLE)
               {
                  ((TextBox)control).ReadOnly = true;
                  return;
               }
            }
            else
               imeMode = 0;
            
            int imeConvMode = imeMode2imeConvMode(imeMode);

            // get current open status
            bool isImeOpen = NativeWindowCommon.ImmGetOpenStatus(hIMC);
            if (!isImeOpen)
               NativeWindowCommon.ImmSetOpenStatus(hIMC, true);

#if !PocketPC
            if (_prevImeState.IsContinue)
#else
            if (_prevImeState.IsContinue || _prevImeMode.ConvMode == IME_NOT_INITIALIZED)
#endif
               _prevImeState.IsOpen = isImeOpen;             // save the current open status as the previous state
            _prevImeState.IsContinue = false;

            // get current conversion mode
            uint currImeConvMode;
            uint saveImeSentence;
            NativeWindowCommon.ImmGetConversionStatus(hIMC, out currImeConvMode, out saveImeSentence);

            if (_prevImeMode.IsContinue || _prevImeMode.ConvMode == IME_NOT_INITIALIZED)
               _prevImeMode.ConvMode = (int)currImeConvMode; // save the current conversion mode as the previous state
            _prevImeMode.IsContinue = false;

            if (imeConvMode == IME_CMODE_NOCONVERSION)
            {
               imeConvMode = _prevImeMode.ConvMode;

               if (imeMode == IME_FORCE_OFF || ImeAutoOff)
               {
                  isImeOpen = false;
#if !PocketPC
                  _prevImeMode.IsContinue = true;   // to save the conversion mode when the forcus is moved to other edit control
#endif
               }
               else
               {
                  isImeOpen = _prevImeState.IsOpen;
                  _prevImeState.IsContinue = true;  // to save the open status when the forcus is moved to other edit control
                  _prevImeMode.IsContinue = true;   // to save the conversion mode when the forcus is moved to other edit control
               }
            }
            else
               isImeOpen = true;

#if PocketPC
            if (isImeOpen && (imeConvMode != currImeConvMode))
#else
            if (imeConvMode != currImeConvMode)
#endif
               NativeWindowCommon.ImmSetConversionStatus(hIMC, (uint)imeConvMode, saveImeSentence);

            if (!isImeOpen)
            {
#if PocketPC
               NativeWindowCommon.ImmSetConversionStatus(hIMC, (uint)IME_CMODE_NOCONVERSION, saveImeSentence);
#endif
               NativeWindowCommon.ImmSetOpenStatus(hIMC, false);
            }

            NativeWindowCommon.ImmReleaseContext(control.Handle, hIMC);
         }
      }

      /// <summary> return true if currently editing composition string in IME
      /// </summary>
      /// <param name="control">
      /// </param>
      /// <returns> bool
      /// </returns>
      public bool IsEditingCompStr(Control control)
      {
         bool ret = false;

         if (control is TextBoxBase)
         {
            IntPtr hWnd = control.Handle;
            if (hWnd == IntPtr.Zero)
               return ret;

            int hIMC = NativeWindowCommon.ImmGetContext(hWnd);
            if (hIMC == 0)
               return ret;

            if (NativeWindowCommon.ImmGetCompositionString(hIMC, NativeWindowCommon.GCS_COMPSTR, null, 0) > 0)
               ret = true;

            NativeWindowCommon.ImmReleaseContext(hWnd, hIMC);
         }
         
         return ret;
      }
   }
}
