using System;
using System.Collections.Specialized;

namespace com.magicsoftware.util
{
	/// <summary> 
	/// This is an interface for all the classes which needs to parse an XML using MgSAXHandler.
	/// </summary>
	/// <author>  Kaushal Sanghavi</author>
	public interface MgSAXHandlerInterface
	{
      void endElement(String elementName, String elementValue, NameValueCollection attributes);
	}
}