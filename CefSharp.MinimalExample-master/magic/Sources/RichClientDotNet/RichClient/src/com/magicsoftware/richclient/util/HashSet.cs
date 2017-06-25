using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;



namespace com.magicsoftware.richclient.util
{
    /// <summary>
    /// implement HashSet that in missing in .NET 2.0
    /// </summary>
    internal class HashSet : Hashtable
    {
        internal override void Add(object key, object value)
        {
            if (!Contains(key))
                base.Add(key, value);
        }

        internal void Add(object key)
        {
            Add(key, key);
        }
    }
}
