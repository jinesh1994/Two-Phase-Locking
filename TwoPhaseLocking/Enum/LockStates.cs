using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoPhaseLocking.Enum
{
    public enum LockStates
    {
        Read = 1,
        Write = 2,
        Unlock = 3
    }
}
