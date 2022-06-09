using System;
using System.Collections.Generic;
using System.IO;

namespace Nyerguds.Util
{
    public class ExtensionSorter : IComparer<string>
    {
        #region IComparer<String> Members
        public int Compare(string x, string y)
        {
            var extCmp = string.Compare(Path.GetExtension(x), Path.GetExtension(y), StringComparison.OrdinalIgnoreCase);
            if (extCmp != 0)
                return extCmp;
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }

}
