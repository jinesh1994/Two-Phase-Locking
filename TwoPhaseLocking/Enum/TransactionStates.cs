using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoPhaseLocking.Enum
{
    public enum TransactionStates
    {
        Active = 1,
        Blocked = 2,
        Aborted = 3,
        Committed = 4
    }
}
