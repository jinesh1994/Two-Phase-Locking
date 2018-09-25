using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoPhaseLocking.Enum;

namespace TwoPhaseLocking
{
    class LockTable
    {
        public char itemName;
        public LockStates lockState;
        public List<int> transactionIds = new List<int>();
        public List<int> waitTransactionIds = new List<int>();
    }
}
