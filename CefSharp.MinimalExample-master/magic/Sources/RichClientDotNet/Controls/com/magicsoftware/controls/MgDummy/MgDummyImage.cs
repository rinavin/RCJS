using System.Drawing;
using com.magicsoftware.support;
using com.magicsoftware.controls;
using System.Windows.Forms;
namespace Controls.com.magicsoftware.controls.MgDummy
{
   /// <summary>
   /// Defines dummy control : Bitmap holding an image of the control.
   /// </summary>
   public class MgDummyImage : MgDummyBase, ISetSpecificControlPropertiesForFormDesigner
   {     

      #region IDesignerControl Members
      public MgDummyImage()
         : base()
      {      
      }

      #endregion


      protected override void CreateLableControl()
      {
         base.CreateLableControl();
         DummyInfoLabel.Text = "";
         DummyInfoLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
      }

      public void setSpecificControlPropertiesForFormDesigner(System.Windows.Forms.Control fromControl)
      {
         this.Bounds = fromControl.Bounds;
         Bitmap bm = GetControlImage(fromControl);
         this.DummyInfoLabel.Image = bm;
      }


      /// <summary>
      /// Return a Bitmap holding an image of the control.
      /// </summary>
      /// <param name="fromControl"></param>
      /// <returns></returns>
      private Bitmap GetControlImage(Control fromControl)
      {
         Bitmap bm = null;
         try
         {
            bm = new Bitmap(fromControl.Width, fromControl.Height);
            fromControl.DrawToBitmap(bm, new Rectangle(0, 0, fromControl.Width, fromControl.Height));
         }
         catch { }
         return bm;
      }
   }
}

