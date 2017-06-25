using System;

namespace com.magicsoftware.util
{
   /// <summary>
   /// 
   /// </summary>
   public class MgFont
   {
      private const int PRIME_NUMBER = 37;
      private const int SEED = 23;

      internal int Index { get; private set; }
      public String TypeFace { get; private set; }
      public int Height { get; private set; }
      public FontAttributes Style { get; private set; }
      public int Orientation { get; private set; }
      public int CharSet  { get; private set; }
      /// <summary>
      /// Return if font style is bold.
      /// </summary>
      public bool Bold
      {
         get
         {
            return ((Style & FontAttributes.FontAttributeBold) != 0);
         }
      }

      /// <summary>
      /// Return if font style is italic.
      /// </summary>
      public bool Italic
      {
         get
         {
            return ((Style & FontAttributes.FontAttributeItalic) != 0);
         }
      }
      
      /// <summary>
      /// Return if font style is strike through.
      /// </summary>
      public bool Strikethrough
      {
         get
         {
            return ((Style & FontAttributes.FontAttributeStrikethrough) != 0);
         }
      }

      /// <summary>
      /// Return if font style is underline.
      /// </summary>
      public bool Underline
      {
         get
         {
            return ((Style & FontAttributes.FontAttributeUnderline) != 0);
         }
      }

      /// <summary>
      /// </summary>
      /// <param name="index"></param>
      /// <param name="typeFace"></param>
      /// <param name="height"></param>
      /// <param name="style"></param>
      public MgFont(int index, String typeFace, int height, FontAttributes style, int orientation, int charSet)
      {
         Index = index;
         TypeFace = typeFace;
         Height = height;
         Style = style;
         Orientation = orientation;
         CharSet = charSet;
      }

      /// <summary>
      /// copy constructor
      /// </summary>
      /// <param name="fromMgFont"></param>
      public MgFont(MgFont fromMgFont)
      {
         Index = fromMgFont.Index;
         Height = fromMgFont.Height;
         Style = fromMgFont.Style;
         TypeFace = fromMgFont.TypeFace;
         Orientation = fromMgFont.Orientation;
         CharSet = fromMgFont.CharSet;
      }

      /// <summary>
      /// add style to font
      /// </summary>
      /// <param name="fontStyle"></param>
      public void addStyle(FontAttributes fontStyle)
      {
         Style |= fontStyle;
      }

      /// <summary>
      /// change the values of this font
      /// </summary>
      /// <param name="typeFace"></param>
      /// <param name="height"></param>
      /// <param name="style"></param>
      /// <param name="orientation"></param>
      public void SetValues(String typeFace, int height, FontAttributes style, int orientation, int charSet)
      {
         // Don't change the font name if the supplied name is empty
         string trimmedTypeFace = typeFace.Trim();
         if(!String.IsNullOrEmpty(trimmedTypeFace)) 
            TypeFace = trimmedTypeFace;

         Height = height;
         Style = style;
         Orientation = orientation;
         CharSet = charSet;
      }

      /// <summary>
      /// </summary>
      /// <returns></returns>
      public override int GetHashCode()
      {
         int hash = SEED;

         hash = PRIME_NUMBER * hash + (TypeFace != null ? TypeFace.GetHashCode() : 0);
         hash = PRIME_NUMBER * hash + Height;
         hash = PRIME_NUMBER * hash + (int)Style;
         hash = PRIME_NUMBER * hash + Orientation;
         hash = PRIME_NUMBER * hash + CharSet;
         return hash;
      }

      /// <summary>
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(Object obj)
      {
         if (obj != null && obj is MgFont)
         {
            MgFont otherFont = (MgFont)obj;

            if (this == otherFont)
               return true;
            else
            {
               if (
                  (Height == otherFont.Height) && (Style == otherFont.Style) && (Orientation == otherFont.Orientation) &&
                  ((TypeFace == null && otherFont.TypeFace == null) || (TypeFace.Equals(otherFont.TypeFace))) &&                   
                  (CharSet == otherFont.CharSet) 
                  )
                  return true;
            }
         }
         return false;
      }
   }

   #region enums

   [Flags]
   public enum FontAttributes
   {
      FontAttributeBold = 0x00000001,            //FNT_ATTR_BOLD
      FontAttributeItalic = 0x00000002,          //FNT_ATTR_ITALIC
      FontAttributeStrikethrough = 0x00000004,   //FNT_ATTR_STRIKE
      FontAttributeUnderline = 0x00000008,       //FNT_ATTR_UNDER
      FontAttributeDefault = 0x10000000          //FNT_ATTR_DEFAULT
   }

   #endregion
}
