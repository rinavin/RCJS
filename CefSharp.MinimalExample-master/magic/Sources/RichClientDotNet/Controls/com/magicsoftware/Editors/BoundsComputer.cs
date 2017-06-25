using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.editors
{
   public interface BoundsComputer
	{
		
		/// <summary> computes bounds of  editor  </summary>
		/// <param name="cellRectangle">
      /// parameter is relevant for table only
		/// </param>
		/// <returns>
		/// </returns>
		Rectangle computeEditorBounds(Rectangle cellRectangle, bool isHeaderEditor);
	}
}
