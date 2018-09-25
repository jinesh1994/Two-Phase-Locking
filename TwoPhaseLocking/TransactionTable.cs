using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoPhaseLocking.Enum;

namespace TwoPhaseLocking
{
    class TransactionTable
    {
        public int transactionID;
        public DateTime timeStamp;
        public TransactionStates transactionState;
        public List<char> lockedItems = new List<char>();
        public List<string> operationWaitlist = new List<string>();
    }
}
