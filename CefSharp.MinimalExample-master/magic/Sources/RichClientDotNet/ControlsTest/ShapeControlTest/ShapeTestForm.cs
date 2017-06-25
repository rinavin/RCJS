using System.Windows.Forms;

namespace ControlsTest.ShapeControlTest
{
   public partial class ShapeTestForm : Form
   {
      public ShapeTestForm()
      {
         InitializeComponent();
         propertyGrid1.SelectedObject = mgShape1;
      }
   }
}