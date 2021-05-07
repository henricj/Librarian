using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nyerguds.Util
{
    public class ExtensionSorter : IComparer<String>
    {
        #region IComparer<String> Members
        public Int32 Compare(String x, String y)
        {
            Int32 extCmp = String.Compare(Path.GetExtension(x), Path.GetExtension(y), StringComparison.InvariantCultureIgnoreCase);
            if (extCmp != 0)
                return extCmp;
            return String.Compare(x, y, StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion
    }

}
