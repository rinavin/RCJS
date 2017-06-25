using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.unipaas.gui.low
{
   /// <summary> 
   /// Manage Tool images for menu and toolbar. This is a singleton class.
   /// </summary>
   /// <author>  ehudm </author>
   internal class ToolImages
   {
      private ImageList _imageList;

      private static ToolImages _instance = null;
      internal static ToolImages getInstance()
      {
         if (_instance == null)
         {
            lock (typeof(ToolImages))
            {
               if (_instance == null)
                  _instance = new ToolImages();
            }
         }
         return _instance;
      }

      /// <summary> CTOR</summary>
      private ToolImages()
      {
      }

      /// <summary> 
      /// Retruns a tool image by its (0-based) index
      /// </summary>
      /// <param name="idx">the index of the tool image </param>
      /// <returns> Image </returns>
      internal Image getToolImage(int idx)
      {
         Image toolImage;

         // on first call initialize the imagelist
         if (_imageList == null)
         {
            _imageList = new ImageList();
            _imageList.ImageSize = new Size(GuiConstants.TOOL_WIDTH, GuiConstants.TOOL_HEIGHT);
            _imageList.ColorDepth = ColorDepth.Depth24Bit;
            Bitmap bitmap = (Bitmap)Events.GetResourceObject("ToolImagesJoined");
            _imageList.TransparentColor = bitmap.GetPixel(0, 0);
            _imageList.Images.AddStrip(bitmap);
         }

         toolImage = _imageList.Images[idx];

         return toolImage;
      }
   }
}