using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace com.magicsoftware.editors
{
   public interface BoundsComputer
	{
		
		/// <summary> computes bounds of table editor  </summary>
		/// <param name="cellRectangle">rectangle with cell's coordinates
		/// </param>
		/// <returns>
		/// </returns>
		Rectangle computeEditorBounds(Rectangle cellRectangle);
	}
}
