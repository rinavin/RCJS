using System;
using System.ComponentModel;
using System.Drawing;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using com.magicsoftware.win32;
using com.magicsoftware.controls.utils;
using com.magicsoftware.util;
using Controls.com.magicsoftware.support;
#if PocketPC
using RightToLeft = com.magicsoftware.mobilestubs.RightToLeft;
using LeftRightAlignment = com.magicsoftware.mobilestubs.LeftRightAlignment;
using HandleRef = com.magicsoftware.mobilestubs.HandleRef;
using ArgumentOutOfRangeException = com.magicsoftware.mobilestubs.MgArgumentOutOfRangeException;
using ContentAlignment = OpenNETCF.Drawing.ContentAlignment2;
using ColorTranslator = OpenNETCF.Drawing.ColorTranslator;
using Control = OpenNETCF.Windows.Forms.Control2;
using CreateParams = OpenNETCF.Windows.Forms.CreateParams;
using Message = Microsoft.WindowsCE.Forms.Message;
#endif

namespace com.magicsoftware.controls
{

#region HeaderSection
   /// <summary>
   /// Types
   /// </summary>

   [Serializable]
   public enum HeaderSectionSortMarks : int
   {
      Non = 0,
      Up = NativeHeader.HDF_SORTUP,
      Down = NativeHeader.HDF_SORTDOWN
   }

   [Serializable]
   public enum VerticalAlignment : int
   {
      Center = 0,
      Top = 1,
      Bottom = 2
   }

   /// <summary>
   /// HeaderSection class.
   /// </summary>
   [
#if !PocketPC
Description("HeaderSection component"),
DefaultProperty("Text"),
ToolboxItem(false),
#endif
 DesignTimeVisible(false),
#if !PocketPC
 SecurityPermission(SecurityAction.LinkDemand,
               Flags = SecurityPermissionFlag.UnmanagedCode)
#endif
]
   public class HeaderSection : Component, ICloneable, IFontOrientation, IFontDescriptionProperty
   {

      public const int FILTER_WIDTH = 18;

      /// <summary>
      /// Data fields
      /// </summary>

      // Owner collection
      private HeaderSectionCollection collection = null;

#if !PocketPC
      [
         Description("Collection which section is kept in."),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         Browsable(false)
      ]
#endif
      internal HeaderSectionCollection Collection
      {
         get { return this.collection; }
         set { this.collection = value; }
      }

      // Owner header control
#if !PocketPC
      [
         Description("Owner header control."),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         Browsable(false)
      ]
#endif
      public Header Header
      {
         get { return collection != null ? collection.Header : null; }
      }

      // Index
#if !PocketPC
      [
         Description("Index of the section."),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         Browsable(false)
      ]
#endif
      public int Index
      {
         get { return collection != null ? collection.IndexOf(this) : -1; }
      }

      // Width
      private int cxWidth = 100;

      internal void _SetWidth(int cx)
      {
         if (cx < 0)
            throw new ArgumentOutOfRangeException("cx", cx, ErrMsg.NegVal());

         this.cxWidth = cx;
      }

#if !PocketPC
      [
         Category("Data"),
         Description("Specifies section width.")
      ]
#endif
      public int Width
      {
         get { return this.cxWidth; }

         set
         {
            if (value != this.cxWidth)
            {
               _SetWidth(value);

               // Notify owner header control
               Header owner = this.Header;
               if (owner != null)
               {
                  owner._OnSectionWidthChanged(this);
               }
            }
         }
      }
      // TODO Support owner drawing HDF_OWNERDRAW

      // Format
      private int fFormat = NativeHeader.HDF_LEFT;

      internal void _SetFormat(int fFormat)
      {
         this.fFormat = fFormat;
      }

#if !PocketPC
      [
         Description("Raw window styles."),
         Browsable(false)
      ]
#endif
      internal int Format
      {
         get
         {
            if (_GetActualRightToLeft() == RightToLeft.Yes)
               return this.fFormat | NativeHeader.HDF_RTLREADING;
            else
               return this.fFormat;
         }
      }

      // Text
      private string text = "";

      internal void _SetText(string text)
      {
         this.text = text;

         if (this.text != null)
            this.fFormat |= NativeHeader.HDF_STRING;
         else
            this.fFormat &= (~NativeHeader.HDF_STRING);
      }

      [
#if !PocketPC
Category("Data"),
Description("Text to be displayed."),
#endif
 DefaultValue("Section")
]
      public string Text
      {
         get { return this.text; }

         set
         {
            if (value != this.text)
            {
               _SetText(value);

               // Notify owner header control
               invalidateHeader();
               Header owner = this.Header;
               if (owner != null)
                  owner._OnSectionTextChanged(this);
            }
         }
      }

      /// <summary>
      /// invalidates the header
      /// </summary>
      public void invalidateHeader()
      {
         Header owner = this.Header;
         if (owner != null)
         {
            //If XP theme is off, Invalidate doesn't clear the old text. 
            //In this case, the new text is drawn on the old one.
            //Somehow, invalidating children of the Header works fine although Header 
            //actually doesn't have any child --- I checked this using "SPY". Strange !!!
            //Don't mind, as long as it works.
#if !PocketPC
            if (!Application.RenderWithVisualStyles)
               owner.Invalidate(owner.GetSectionRect(this), true);
            else
#endif
               owner.Invalidate(owner.GetSectionRect(this));
         }
      }
      // ImageIndex
      private int iImage = -1;

      internal void _SetImageIndex(int index)
      {
         this.iImage = index;

         if (this.iImage >= 0)
            this.fFormat |= NativeHeader.HDF_IMAGE;
         else
         {
            if (this.iImage != -1)
               throw new ArgumentException(ErrMsg.InvVal(index.ToString()), "value");

            this.fFormat &= (~NativeHeader.HDF_IMAGE);
         }
      }

      [
#if !PocketPC
Category("Data"),
Description("Index of image associated with section."),
TypeConverter(typeof(ImageIndexConverter)),
         //      Editor(typeof(ImageIndexEditor), typeof(UITypeEditor)),
Localizable(true),
#endif
 DefaultValue(-1)
]
      public int ImageIndex
      {
         get { return this.iImage; }

         set
         {
            if (value != this.iImage)
            {
               _SetImageIndex(value);

               // Notify owner header control
               Header owner = this.Header;
               if (owner != null)
               {
                  owner._OnSectionImageIndexChanged(this);
               }
            }
         }
      }

      // Bitmap
      private Bitmap bitmap = null;
      private IntPtr hBitmap = IntPtr.Zero;

      internal IntPtr _GetHBitmap()
      {
         if (this.hBitmap == IntPtr.Zero && this.bitmap != null)
         {
            this.hBitmap = this.bitmap.GetHbitmap();
         }

         return this.hBitmap;
      }

      internal void _SetBitmap(Bitmap bitmap)
      {
         if (this.hBitmap != IntPtr.Zero)
         {
            NativeWindowCommon.DeleteObject(this.hBitmap);
            this.hBitmap = IntPtr.Zero;
         }

         this.bitmap = bitmap;

         if (this.bitmap != null)
            this.fFormat |= NativeHeader.HDF_BITMAP;
         else
            this.fFormat &= (~NativeHeader.HDF_BITMAP);
      }


#if !PocketPC
      [
         Category("Data"),
         Description("Bitmap to be drawn on the section."),
      ]
#endif
      public Bitmap Bitmap
      {
         get { return this.bitmap; }
         set
         {
            if (value != this.bitmap)
            {
               _SetBitmap(value);

               // Notify owner header control
               Header owner = this.Header;
               if (owner != null)
               {
                  owner._OnSectionBitmapChanged(this);
               }
            }
         }
      }

      //Color
      private Color _color = Color.Empty;

#if !PocketPC
      [
         Category("Appearance"),
         Description("Specifies section foreground color.")
      ]
#endif

      internal bool TopBorder
      {
         get;
         set;
      }

      internal bool RightBorder
      {
         get;
         set;
      }

      public Color Color
      {
         get { return _color; }
         set
         {
            if (value != this._color)
            {
               this._color = value;
               invalidateHeader();
            }
         }
      }

      //Font
#if !PocketPC
      private Font cxFont = Control.DefaultFont;
#else
      private Font cxFont = new Font("MS Sans Serif", 8, FontStyle.Regular);
#endif

#if !PocketPC
      [
         Category("Appearance"),
         Description("Specifies section font.")
      ]
#endif
      public Font Font
      {
         get { return cxFont; }
         set
         {
            if (value != this.cxFont)
            {
               this.cxFont = value;
               FontDescription = new FontDescription(value);
               invalidateHeader();
            }
         }
      }

      public FontDescription FontDescription { get; set; }

      // RightToLeft
      private RightToLeft enRightToLeft = RightToLeft.No;

      internal RightToLeft _GetActualRightToLeft()
      {
         Header owner = this.Header;

         return (this.enRightToLeft == RightToLeft.Inherit && owner != null)
                  ? owner.RightToLeft
                  : this.enRightToLeft;
      }

      internal void _SetRightToLeft(RightToLeft enRightToLeft)
      {
         this.enRightToLeft = enRightToLeft;
      }

#if !PocketPC
      [
         Category("Appearance"),
         Description("Right to left layout."),
      ]
#endif
      public RightToLeft RightToLeft
      {
         get { return enRightToLeft; }
         set
         {
            if (this.enRightToLeft != value)
            {
               _SetRightToLeft(value);

               // Notify owner header control
               Header owner = this.Header;
               if (owner != null)
               {
                  owner._OnSectionRightToLeftChanged(this);
               }
            }
         }
      }


#if !PocketPC
      [
         Category("Appearance"),
         Description("Specifies content alignment."),
      ]
#endif
      //Alignment
      private ContentAlignment _contentAlignment = ContentAlignment.MiddleLeft;
      public ContentAlignment ContentAlignment
      {
         get { return _contentAlignment; }
         set
         {
            if (value != this._contentAlignment)
            {
               this._contentAlignment = value;
               invalidateHeader();
            }
         }
      }

      // Image align
      internal LeftRightAlignment _GetImageAlign()
      {
         if ((this.fFormat & NativeHeader.HDF_BITMAP_ON_RIGHT) != 0)
            return LeftRightAlignment.Right;
         else
            return LeftRightAlignment.Left;
      }

      internal void _SetImageAlign(LeftRightAlignment enValue)
      {
         int nFlag;
         const int fMask = NativeHeader.HDF_BITMAP_ON_RIGHT;

         switch (enValue)
         {
            case LeftRightAlignment.Left:
               nFlag = 0;
               break;

            case LeftRightAlignment.Right:
               nFlag = NativeHeader.HDF_BITMAP_ON_RIGHT;
               break;

            default:
               throw new NotSupportedException(ErrMsg.InvVal(enValue.ToString()), null);
         }

         this.fFormat &= (~fMask);
         this.fFormat |= nFlag;
      }

#if !PocketPC
      [
         Category("Appearance"),
         Description("Specifies image placement."),
      ]
#endif
      public LeftRightAlignment ImageAlign
      {
         get { return _GetImageAlign(); }

         set
         {
            if (value != _GetImageAlign())
            {
               _SetImageAlign(value);

               // Notify owner header control
               Header owner = this.Header;
               if (owner != null)
               {
                  owner._OnSectionImageAlignChanged(this);
               }
            }
         }
      }

      // Sort mark
      internal HeaderSectionSortMarks _GetSortMark()
      {
         return SortMark;
      }

      internal void _SetSortMark(HeaderSectionSortMarks enValue)
      {
         SortMark = enValue;
      }
#if !PocketPC
      [
         Category("Appearance"),
         Description("Defines sort mark to be shown on the section."),
      ]
#endif
      //SortMark
      private HeaderSectionSortMarks _sortMark;
      public HeaderSectionSortMarks SortMark
      {
         get { return _sortMark; }
         set
         {
            if (value != this._sortMark)
            {
               this._sortMark = value;
               invalidateHeader();
            }
         }
      }

      // Tag
      internal void _SetTag(object tag)
      {
         this.tag = tag;
      }

      private object tag = null;

#if !PocketPC
      [
         Browsable(false)
      ]
#endif
      public object Tag
      {
         get { return this.tag; }
         set
         {
            if (this.tag != value)
            {
               this.tag = value;
            }
         }
      }

      /// <summary>
      /// Construction & finalization
      /// </summary>
      public HeaderSection()
      {
         this.ContentAlignment = ContentAlignment.MiddleLeft;
         FontDescription = new FontDescription(Font);
      }

      public HeaderSection(string text, int cxWidth)
         : this()
      {
         _SetText(text);
         _SetWidth(cxWidth);
      }

      public HeaderSection(string text, int cxWidth, int iImage)
         : this(text, cxWidth)
      {
         _SetImageIndex(iImage);
      }

      public HeaderSection(string text, int cxWidth, object tag)
         : this(text, cxWidth)
      {
         _SetTag(tag);
      }

      public HeaderSection(string text, int cxWidth, int iImage, object tag)
         : this(text, cxWidth, tag)
      {
         _SetImageIndex(iImage);
      }

      public HeaderSection(string text, int cxWidth, Bitmap bitmap)
         : this(text, cxWidth)
      {
         _SetBitmap(bitmap);
      }

      public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap)
         : this(text, cxWidth, iImage)
      {
         _SetBitmap(bitmap);
      }

      public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap,
                      ContentAlignment enContentAlign)
         : this(text, cxWidth, iImage, bitmap)
      {
         ContentAlignment = enContentAlign;
      }

      public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap,
                      ContentAlignment enContentAlign,
                      LeftRightAlignment enImageAlign)
         : this(text, cxWidth, iImage, bitmap, enContentAlign)
      {

         ContentAlignment = enContentAlign;
      }

      public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap,
                      ContentAlignment enContentAlign,
                      LeftRightAlignment enImageAlign, object tag)
         : this(text, cxWidth, iImage, bitmap, enContentAlign, enImageAlign)
      {
         _SetTag(tag);
      }

      public HeaderSection(string text, int cxWidth, int iImage, Bitmap bitmap,
                      RightToLeft enRightToLeft, ContentAlignment enContentAlign,
                      LeftRightAlignment enImageAlign,
                      HeaderSectionSortMarks enSortMark, object tag)
         : this(text, cxWidth, iImage, bitmap, enContentAlign, enImageAlign, tag)
      {

         _SetSortMark(enSortMark);
      }

      protected HeaderSection(int cxWidth, string text, int iImage, Bitmap bitmap,
                        RightToLeft enRightToLeft, int fFormat, object tag)
         : this(text, cxWidth, iImage, bitmap)
      {

         _SetRightToLeft(enRightToLeft);
         _SetFormat(fFormat);
         _SetTag(tag);
      }

      ~HeaderSection()
      {
         Dispose(false);
      }

      /// <summary>
      /// Overrides
      /// </summary>
      public override string ToString()
      {
         return "HeaderSection: {" + this.text + "}";
      }

      protected override void Dispose(bool bDisposing)
      {
         if (this.hBitmap != IntPtr.Zero)
         {
            NativeWindowCommon.DeleteObject(this.hBitmap);
            this.hBitmap = IntPtr.Zero;
         }

         if (bDisposing && this.collection != null)
         {
            this.collection.Remove(this);
         }

         base.Dispose(bDisposing);
      }

      /// <summary>
      /// ICloneable implementation
      /// </summary>
      public object Clone()
      {
         return new HeaderSection(this.cxWidth, this.text, this.iImage, this.bitmap,
                            this.enRightToLeft, this.fFormat, this.tag);
      }

      /// <summary>
      /// Operations
      /// </summary>
      internal void ComposeNativeData(int iOrder, out NativeHeader.HDITEM item)
      {
         item = new NativeHeader.HDITEM();

         // Width
         item.mask = NativeHeader.HDI_WIDTH;
         item.cxy = this.cxWidth;

         // Text
         /*if ( this.text != null )
         {
            item.mask |= NativeHeader.HDI_TEXT;
            item.lpszText = this.text;
            item.cchTextMax = 0;
         }*/

         // ImageIndex
         if (this.iImage >= 0)
         {
            item.mask |= NativeHeader.HDI_IMAGE;
            item.iImage = this.iImage;
         }

         // Bitmap
         if (this.bitmap != null && this.bitmap.GetHbitmap() != IntPtr.Zero)
         {
            item.mask |= NativeHeader.HDI_BITMAP;
            item.hbm = _GetHBitmap();
         }

         // Format
         item.mask |= NativeHeader.HDI_FORMAT;
         item.fmt = this.Format;

         // Order
         if (iOrder >= 0)
         {
            item.mask |= NativeHeader.HDI_ORDER;
            item.iOrder = iOrder;
         }

         //      item.lParam;
         //      item.type;
         //      item.pvFilter;
      }

      /// <summary>
      /// draw header section title
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rc"></param>
      internal void DrawTitle(IntPtr hdc, NativeWindowCommon.RECT rc, int index,  bool supportsMultilineText, bool addEndEllipsesFlag, bool rightToLeftLayout)
      {
         Header.HeaderRenderer.GetTitleTextRect(index, ref rc);

         using (Graphics gr = Graphics.FromHdc(hdc))
         {

            //draw sort icon
            if (SortMark != HeaderSectionSortMarks.Non && !HasFilter)
            {
               int iconWindth = SortIconWidth();
               DrawSortMark(hdc, rc, gr);
               rc.right -= iconWindth + Header.SORT_ICON_LEFT_RIGHT_MARGIN;
            }

            if (HasFilter)
               rc.right -= HeaderSection.FILTER_WIDTH + Header.SORT_ICON_LEFT_RIGHT_MARGIN;

            //draw ... in the end of text if width is too short
            int width = rc.right - rc.left;
            int format = NativeWindowCommon.DT_EDITCONTROL | NativeWindowCommon.DT_EXTERNALLEADING;

            if (supportsMultilineText)
               format |= NativeWindowCommon.DT_WORDBREAK;

            StringBuilder stringBuilder = new StringBuilder(Text);

            if (addEndEllipsesFlag)
               //for windows CE && orientated fonts DT_END_ELLIPSIS style is not supported
               //http://support.microsoft.com/kb/249678
               format |= NativeWindowCommon.DT_END_ELLIPSIS;

            if (Environment.OSVersion.Platform == PlatformID.WinCE)
            {
               if (Text.IndexOf("\n") != -1)
               {

                  SizeF size = gr.MeasureString(text, Font);
                  int cur, len = cur = text.Length;
                  while (size.Width > width && cur > 1)
                  {
                     cur = --len;
                     stringBuilder.Length = len;
                     while (cur > 1 && len - cur < 3)
                        stringBuilder[--cur] = '.';
                     size = gr.MeasureString(stringBuilder.ToString(), Font);
                  }
               }
            }



            NativeWindowCommon.SetBkMode(hdc, NativeWindowCommon.TRANSPARENT);
            NativeWindowCommon.SetTextColor(hdc, ColorTranslator.ToWin32(Color));
#if !PocketPC
            if (FontOrientation != 0)
            {
               Rectangle rectangle = new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);

               ControlRenderer.PrintRotatedText(hdc, FontDescription, FontOrientation, stringBuilder.ToString(), rectangle, ContentAlignment, rc, rightToLeftLayout);
            }
            else
#endif
            {
               //text flags are exactly the same as DrawText DT_*** flags
               format |= (int)Utils.ContentAlignment2TextFlags(ContentAlignment);
               if (isSingleLine())
                  format |= NativeWindowCommon.DT_SINGLELINE;

               IntPtr hFont = FontDescription.FontHandle;
               NativeWindowCommon.SelectObject(hdc, hFont);
               NativeWindowCommon.DrawText(hdc, stringBuilder.ToString(), stringBuilder.Length, ref rc, format);
            }
#if !PocketPC
            //Set the rectangle back
            if (HasFilter)
            {
               rc.right += HeaderSection.FILTER_WIDTH + Header.SORT_ICON_LEFT_RIGHT_MARGIN;
               DrawFilter(hdc, rc);
            }
#endif
         }

      }

      /// <summary>
      /// Indication if Header section should show filter mark
      /// </summary>
      public bool HasFilter { get; set; }

      private Color hsFilterColor = Color.Empty;
      /// <summary>
      /// Color for highlight filter when hovering
      /// </summary>
      public Color FilterColor
      {
         get { return hsFilterColor; }
         set
         {
            if (hsFilterColor != value)
            {
               hsFilterColor = value;
#if !PocketPC
               if (!Application.RenderWithVisualStyles)
                  invalidateHeader();
#endif
            }
         }
      }
#if !PocketPC
      /// <summary>
      /// Draw Header section filter
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rct"></param>
      /// <param name="g"></param>
      public void DrawFilter(IntPtr hdc, NativeWindowCommon.RECT rct)
      {
         //Filter line drawing
         int x = rct.right - FILTER_WIDTH;
         int bottomY = 0;

         Header.HeaderRenderer.DrawLine(hdc, new Point(x, bottomY), new Point(x, Header.FilterHeight));

         //Filter highlight drawing
         NativeWindowCommon.RECT rectHighlight = new NativeWindowCommon.RECT()
         {
            bottom = Header.FilterHeight,
            top = 0,
            left = rct.right - FILTER_WIDTH + 1,
            right = rct.right + 1
         };

         if ((FilterColor != Color.Empty) && !Header.OnSectionResize)
         {
            if (Header.TitleColor != Color.Empty)
               Header.HighlightBrush = NativeWindowCommon.CreateSolidBrush(ColorTranslator.ToWin32(FilterColor));
            else
               Header.HighlightBrush = NativeWindowCommon.CreateSolidBrush(ColorTranslator.ToWin32(Color.FromArgb(255, 149, 202, 255)));
            NativeWindowCommon.FillRect(hdc, ref rectHighlight, Header.HighlightBrush);
            NativeWindowCommon.DeleteObject(Header.HighlightBrush);
         }

         int filterWidthIcon = FILTER_WIDTH / 3;

         //Filter arrow drawing
         NativeWindowCommon.POINT[] points = new NativeWindowCommon.POINT[3];
         //right 
         points[0].x = rct.right - filterWidthIcon;
         points[0].y = rct.top + (rct.bottom - rct.top) / 2 - 2;

         // center point
         points[1].x = rct.right - FILTER_WIDTH / 2;
         points[1].y = points[0].y + filterWidthIcon / 2;

         // left point
         points[2].x = points[0].x - filterWidthIcon;
         points[2].y = points[0].y;

         NativeWindowCommon.SelectObject(hdc, Header.FilterBrush);
         NativeWindowCommon.SelectObject(hdc, Header.FilterPen);
         NativeWindowCommon.Polygon(hdc, points, 3);
      }
#endif

      /// <summary>
      /// return true if must be single line
      /// </summary>
      /// <returns></returns>
      bool isSingleLine()
      {
         switch (ContentAlignment)
         {
            case ContentAlignment.TopCenter:
            case ContentAlignment.TopLeft:
            case ContentAlignment.TopRight:
               return false;
            default:
               return true;
         }
      }


      /// <summary>
      /// return sort icon width
      /// </summary>
      /// <returns></returns>
      int SortIconWidth()
      {
#if !PocketPC
         int width = Font.Height * 3 / 4;
#else
         int width = (int)(Font.Size * 3 / 4);
#endif
         if (width % 2 > 0)
            width++;
         return width;
      }

      /// <summary>
      /// return sort icon height
      /// </summary>
      /// <returns></returns>
      int SortIconHeight()
      {
#if !PocketPC
         if (Application.RenderWithVisualStyles)
            return SortIconWidth() / 2;
         else
#endif
            return SortIconWidth();
      }

      /// <summary>
      /// drow sort marking on the column
      /// </summary>
      /// <param name="hdc"></param>
      /// <param name="rc"></param>
      /// <param name="gr"></param>
      void DrawSortMark(IntPtr hdc, NativeWindowCommon.RECT rc, Graphics gr)
      {
         int iconWidth = SortIconWidth();
         int topBottomMargin = (this.Header.Height - iconWidth) / 2;
         int iconHeight = SortIconHeight();


         NativeWindowCommon.POINT[] points = new NativeWindowCommon.POINT[3];
         if (SortMark == HeaderSectionSortMarks.Up)
         {
            points[0].x = rc.right - Header.SORT_ICON_LEFT_RIGHT_MARGIN;
            points[0].y = (rc.bottom + iconHeight) / 2;

            // center point
            points[1].x = points[0].x - iconWidth / 2;
            points[1].y = points[0].y - iconHeight;

            // left point
            points[2].x = points[0].x - iconWidth;
            points[2].y = points[0].y;
         }
         else if (SortMark == HeaderSectionSortMarks.Down)
         {

            //right 
            points[0].x = rc.right - Header.SORT_ICON_LEFT_RIGHT_MARGIN;
            points[0].y = (rc.bottom - iconHeight) / 2;

            // center point
            points[1].x = points[0].x - iconWidth / 2;
            points[1].y = points[0].y + iconHeight;

            // left point
            points[2].x = points[0].x - iconWidth;
            points[2].y = points[0].y;
         }

#if !PocketPC
         if (Application.RenderWithVisualStyles)
         {
            //draw XP style sort triangle
            NativeWindowCommon.SelectObject(hdc, Header.SortBrush);
            NativeWindowCommon.SelectObject(hdc, Header.SortPen);
            NativeWindowCommon.Polygon(hdc, points, 3);
         }
         else
#endif
         {
            //draw windows 2000 style sort triangle
            Point[] points1 = new Point[3]
            {  
                               new Point(points[0].x, points[0].y),
                               new Point (points[1].x, points[1].y),
                               new Point (points[2].x, points[2].y)
            
            };

            NativeWindowCommon.MoveToEx(hdc, points1[0].X, points1[0].Y, IntPtr.Zero);
            NativeWindowCommon.SelectObject(hdc, Header.WhitePen);
            NativeWindowCommon.LineTo(hdc, points1[1].X, points1[1].Y);
            NativeWindowCommon.SelectObject(hdc, Header.ButtonShadowPen);
            NativeWindowCommon.LineTo(hdc, points1[2].X, points1[2].Y);
            if (SortMark == HeaderSectionSortMarks.Up)
               NativeWindowCommon.SelectObject(hdc, Header.WhitePen);
            else
               NativeWindowCommon.SelectObject(hdc, Header.ButtonShadowPen);
            NativeWindowCommon.LineTo(hdc, points1[0].X, points1[0].Y);
         }

      }

      public int FontOrientation { get; set; }

      /// <summary>
      /// Allow filter property
      /// </summary>
      public bool AllowFilter { get; set; }
   }

#endregion // HeaderSection


   #region Header Event Arguments' classes

   /// <summary>
   /// HeaderSectionEventArgs class
   /// </summary>
   [Serializable]
   public class HeaderSectionEventArgs : EventArgs
   {
      // Fields
      private HeaderSection item = null;
      public HeaderSection Item
      {
         get { return this.item; }
      }

      // Fields
      private MouseButtons enButton = MouseButtons.None;
      public MouseButtons Button
      {
         get { return this.enButton; }
      }

      // Construction
      public HeaderSectionEventArgs(HeaderSection item)
      {
         this.item = item;
      }

      public HeaderSectionEventArgs(HeaderSection item, MouseButtons enButton)
      {
         this.item = item;
         this.enButton = enButton;
      }

   } // HeaderSectionEventArgs

   public delegate void HeaderSectionEventHandler(
                     object sender, HeaderSectionEventArgs ea);


   /// <summary>
   /// HeaderSectionConformableEventArgs class
   /// </summary>
   [Serializable]
   public class HeaderSectionConformableEventArgs : HeaderSectionEventArgs
   {
      // Fields
      private bool bAccepted = true;
      public bool Accepted
      {
         get { return this.bAccepted; }
         set { this.bAccepted = value; }
      }

      // Construction
      public HeaderSectionConformableEventArgs(HeaderSection item)
         : base(item)
      {
      }

      public HeaderSectionConformableEventArgs(HeaderSection item, MouseButtons enButton)
         : base(item, enButton)
      {
      }

   } // HeaderSectionConformableEventArgs

   public delegate void HeaderSectionConformableEventHandler(
                     object sender, HeaderSectionConformableEventArgs ea);


   /// <summary>
   /// HeaderSectionWidthEventArgs class
   /// </summary>
   [Serializable]
   public class HeaderSectionWidthEventArgs : HeaderSectionEventArgs
   {
      // Fields
      private int cxWidth = 0;
      public int Width
      {
         get { return this.cxWidth; }
      }

      // Construction
      public HeaderSectionWidthEventArgs(HeaderSection item)
         : base(item)
      {
      }

      public HeaderSectionWidthEventArgs(HeaderSection item, MouseButtons enButton)
         : base(item, enButton)
      {
      }

      public HeaderSectionWidthEventArgs(HeaderSection item, MouseButtons enButton,
                                 int cxWidth)
         : base(item, enButton)
      {
         this.cxWidth = cxWidth;
      }

   } // HeaderWidthItemEventArgs

   public delegate void HeaderSectionWidthEventHandler(
                     object sender, HeaderSectionWidthEventArgs ea);


   /// <summary>
   /// HeaderSectionWidthConformableEventArgs class
   /// </summary>
   [Serializable]
   public class HeaderSectionWidthConformableEventArgs : HeaderSectionWidthEventArgs
   {
      // Fields
      private bool bAccepted = true;
      public bool Accepted
      {
         get { return this.bAccepted; }
         set { this.bAccepted = value; }
      }

      // Construction
      public HeaderSectionWidthConformableEventArgs(HeaderSection item)
         : base(item)
      {
      }

      public HeaderSectionWidthConformableEventArgs(HeaderSection item, MouseButtons enButton)
         : base(item, enButton)
      {
      }

      public HeaderSectionWidthConformableEventArgs(HeaderSection item, MouseButtons enButton,
                                         int cxWidth)
         : base(item, enButton, cxWidth)
      {
      }

   } // HeaderSectionWidthConformableEventArgs

   public delegate void HeaderSectionWidthConformableEventHandler(
                     object sender, HeaderSectionWidthConformableEventArgs ea);


   /// <summary>
   /// HeaderSectionOrderEventArgs class
   /// </summary>
   [Serializable]
   public class HeaderSectionOrderEventArgs : HeaderSectionEventArgs
   {
      // Fields
      private int iOrder = -1;
      public int Order
      {
         get { return this.iOrder; }
      }

      // Construction
      public HeaderSectionOrderEventArgs(HeaderSection item)
         : base(item)
      {
      }

      public HeaderSectionOrderEventArgs(HeaderSection item, MouseButtons enButton)
         : base(item, enButton)
      {
      }

      public HeaderSectionOrderEventArgs(HeaderSection item, MouseButtons enButton,
                                 int iOrder)
         : base(item, enButton)
      {
         this.iOrder = iOrder;
      }

   } // HeaderSectionOrderEventArgs

   public delegate void HeaderSectionOrderEventHandler(
                     object sender, HeaderSectionOrderEventArgs ea);


   /// <summary>
   /// HeaderSectionOrderConformableEventArgs class
   /// </summary>
   [Serializable]
   public class HeaderSectionOrderConformableEventArgs : HeaderSectionOrderEventArgs
   {
      // Fields
      private bool bAccepted = true;
      public bool Accepted
      {
         get { return this.bAccepted; }
         set { this.bAccepted = value; }
      }

      // Construction
      public HeaderSectionOrderConformableEventArgs(HeaderSection item)
         : base(item)
      {
      }

      public HeaderSectionOrderConformableEventArgs(HeaderSection item, MouseButtons enButton)
         : base(item, enButton)
      {
      }

      public HeaderSectionOrderConformableEventArgs(HeaderSection item, MouseButtons enButton,
                                         int iOrder)
         : base(item, enButton, iOrder)
      {
      }

   } // HeaderSectionOrderConformableEventArgs

   public delegate void HeaderSectionOrderConformableEventHandler(
                     object sender, HeaderSectionOrderConformableEventArgs ea);

   #endregion // HeaderEventArgs
}