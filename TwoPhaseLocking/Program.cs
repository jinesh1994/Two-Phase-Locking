using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoPhaseLocking.Enum;

namespace TwoPhaseLocking
{
    class Program
    {
        static Dictionary<int, TransactionTable> transactions = new Dictionary<int, TransactionTable>();
        static Dictionary<char, LockTable> locks = new Dictionary<char, LockTable>();
        static string readOperation = "r{0} ({1})";
        static string writeOperation = "w{0} ({1})";
        static string beginOperation = "b{0}";
        static string endOperation = "e{0}";

        static void Main(string[] args)
        {
            var tranFile = File.ReadAllLines(@".\Sample Data\sample7.txt").ToList();
            foreach (string line in tranFile)
            {
                AnalyseOperation(line);
                //Console.WriteLine("\nOperation: " + line);
                //Console.WriteLine("\nTransaction Table: ");
                //Console.WriteLine("TransactionID | TimeStamp | TransactionState | LockedItems | OperationWaitlist");
                //foreach (var transaction in transactions)
                //{
                //    Console.WriteLine(transaction.Value.transactionID + " | " + transaction.Value.timeStamp + " | " + transaction.Value.transactionState + " | " + string.Join(", ", transaction.Value.lockedItems) + " | " + string.Join(", ", transaction.Value.operationWaitlist));
                //}
                //Console.WriteLine("\nLock Table: ");
                //Console.WriteLine("ItemName | LockState | TransactionIds | WaitTransactionIds");
                //foreach (var lockItem in locks)
                //{
                //    Console.WriteLine(lockItem.Value.itemName + " | " + lockItem.Value.lockState + " | " + string.Join(", ", lockItem.Value.transactionIds) + " | " + string.Join(", ", lockItem.Value.waitTransactionIds));
                //}
            }
            Console.WriteLine("\nTransaction Table: ");
            Console.WriteLine("TransactionID | TimeStamp | TransactionState | LockedItems | OperationWaitlist");
            foreach (var transaction in transactions)
            {
                Console.WriteLine(transaction.Value.transactionID + " | " + transaction.Value.timeStamp + " | " + transaction.Value.transactionState + " | " + string.Join(", ", transaction.Value.lockedItems) + " | " + string.Join(", ", transaction.Value.operationWaitlist));
            }
            Console.WriteLine("\nLock Table: ");
            Console.WriteLine("ItemName | LockState | TransactionIds | WaitTransactionIds");
            foreach (var lockItem in locks)
            {
                Console.WriteLine(lockItem.Value.itemName + " | " + lockItem.Value.lockState + " | " + string.Join(", ", lockItem.Value.transactionIds) + " | " + string.Join(", ", lockItem.Value.waitTransactionIds));
            }
        }

        static void AnalyseOperation(string operation)
        {
            if (operation.First().ToString().Equals("b", StringComparison.InvariantCultureIgnoreCase))
            {
                BeginTransaction(Convert.ToInt32(operation.Substring(1, 1)));
            }
            else if (operation.First().ToString().Equals("r", StringComparison.InvariantCultureIgnoreCase))
            {
                ReadLock(Convert.ToInt32(operation.Substring(1, 1)), operation.Substring(operation.IndexOf('(') + 1, 1).FirstOrDefault());
            }
            else if (operation.First().ToString().Equals("w", StringComparison.InvariantCultureIgnoreCase))
            {
                WriteLock(Convert.ToInt32(operation.Substring(1, 1)), operation.Substring(operation.IndexOf('(') + 1, 1).FirstOrDefault());
            }
            else if (operation.First().ToString().Equals("e", StringComparison.InvariantCultureIgnoreCase))
            {
                EndTransaction(Convert.ToInt32(operation.Substring(1, 1)));
            }
            else
            {
                Console.WriteLine("Invalid Operation");
            }
        }

        static void BeginTransaction(int transactionID)
        {
            TransactionTable transactionTable = new TransactionTable();
            transactionTable.transactionID = transactionID;
            transactionTable.transactionState = TransactionStates.Active;
            transactionTable.timeStamp = DateTime.Now;
            transactions.Add(transactionID, transactionTable);
        }

        static void ReadLock(int transactionID, char item)
        {
            if (transactions[transactionID].transactionState == TransactionStates.Active)
            {
                if (locks == null || locks.Count == 0 || !locks.ContainsKey(item))
                {
                    locks.Add(item, new LockTable() { itemName = item, lockState = LockStates.Unlock });
                }

                if (locks[item].lockState == LockStates.Unlock)
                {
                    locks[item].lockState = LockStates.Read;
                    if (!locks[item].transactionIds.Contains(transactionID))
                        locks[item].transactionIds.Add(transactionID);

                    transactions[transactionID].lockedItems.Add(item);
                    Console.WriteLine("Transaction " + transactionID + " got read lock on item " + item);
                }
                else if (locks[item].lockState == LockStates.Read && !locks[item].transactionIds.Contains(transactionID))
                {
                    if (!locks[item].transactionIds.Contains(transactionID))
                        locks[item].transactionIds.Add(transactionID);

                    transactions[transactionID].lockedItems.Add(item);
                    Console.WriteLine("Transaction " + transactionID + " got read lock on item " + item);
                }
                else if (locks[item].lockState == LockStates.Write && !locks[item].transactionIds.Contains(transactionID))
                {
                    CheckDeadLock(transactionID, item, string.Format(readOperation, transactionID, item));
                }
            }
            else if (transactions[transactionID].transactionState == TransactionStates.Blocked)
            {
                string operation = string.Format(readOperation, transactionID, item);
                Console.WriteLine("Transaction " + transactionID + " is already blocked and operation " + operation + " added to waitlist.");
                transactions[transactionID].operationWaitlist.Add(operation);
            }
            else if (transactions[transactionID].transactionState == TransactionStates.Aborted)
                Console.WriteLine("Transaction " + transactionID + " is arleady aborted");
        }

        static void WriteLock(int transactionID, char item)
        {
            if (transactions[transactionID].transactionState == TransactionStates.Active)
            {
                if (locks == null || locks.Count == 0 || locks[item] == null)
                {
                    locks.Add(item, new LockTable() { itemName = item, lockState = LockStates.Unlock });
                }

                if (locks[item].lockState == LockStates.Unlock)
                {
                    locks[item].lockState = LockStates.Write;
                    if (!locks[item].transactionIds.Contains(transactionID))
                        locks[item].transactionIds.Add(transactionID);

                    transactions[transactionID].lockedItems.Add(item);
                    Console.WriteLine("Transaction " + transactionID + " got write lock on item " + item);
                }
                else if (locks[item].lockState == LockStates.Read && locks[item].transactionIds.Count == 1 && locks[item].transactionIds.Contains(transactionID))
                {
                    locks[item].lockState = LockStates.Write;
                    Console.WriteLine("Transaction " + transactionID + " upgraded to write lock on item " + item);
                    //locks[item].transactionIds.Add(transactionID);
                    //transactions[transactionID].lockedItems.Add(item);
                }
                else if (locks[item].lockState == LockStates.Read || locks[item].lockState == LockStates.Write)
                {
                    CheckDeadLock(transactionID, item, string.Format(writeOperation, transactionID, item));
                }
            }
            else if (transactions[transactionID].transactionState == TransactionStates.Blocked)
            {
                string operation = string.Format(writeOperation, transactionID, item);
                Console.WriteLine("Transaction " + transactionID + " is already blocked and operation " + operation + " added to waitlist.");
                transactions[transactionID].operationWaitlist.Add(operation);
            }
            else if (transactions[transactionID].transactionState == TransactionStates.Aborted)
                Console.WriteLine("Transaction " + transactionID + " is arleady aborted");
        }

        static void EndTransaction(int transactionID)
        {
            if (transactions[transactionID].transactionState == TransactionStates.Active)
            {
                Commit(transactionID);
            }
            else if (transactions[transactionID].transactionState == TransactionStates.Blocked)
            {
                string operation = string.Format(endOperation, transactionID);
                Console.WriteLine("Transaction " + transactionID + " is already blocked and operation " + operation + " added to waitlist.");
                transactions[transactionID].operationWaitlist.Add(operation);
            }
            else if (transactions[transactionID].transactionState == TransactionStates.Aborted)
                Console.WriteLine("Transaction " + transactionID + " is arleady aborted");
        }

        static void CheckDeadLock(int transactionID, char item, string operation)
        {
            List<int> currentTransations = locks[item].transactionIds;
            bool abortTransaction = false;

            foreach (int currentTransaction in currentTransations)
            {
                if (transactions[transactionID].timeStamp <= transactions[currentTransaction].timeStamp)
                {
                    continue;
                }
                else
                {
                    abortTransaction = true;
                }
            }

            if (abortTransaction)
            {
                Abort(transactionID);
            }
            else
            {
                transactions[transactionID].transactionState = TransactionStates.Blocked;
                transactions[transactionID].operationWaitlist.Add(operation);
                locks[item].waitTransactionIds.Add(transactionID);
                Console.WriteLine("Transaction " + transactionID + " got blocked.");
            }
        }

        static void Commit(int transactionID)
        {
            Console.WriteLine("Transaction " + transactionID + " committed");
            transactions[transactionID].transactionState = TransactionStates.Committed;
            Unlock(transactionID);
        }

        static void Abort(int transactionID)
        {
            Console.WriteLine("Transaction " + transactionID + " aborted");
            transactions[transactionID].transactionState = TransactionStates.Aborted;
            Unlock(transactionID);
        }

        static void Unlock(int transactionID)
        {
            transactions[transactionID].lockedItems = new List<char>();
            foreach (var lockItem in locks)
            {
                if (lockItem.Value.transactionIds.Contains(transactionID))
                {
                    lockItem.Value.transactionIds.Remove(transactionID);
                }
                if (lockItem.Value.transactionIds.Count == 0)
                {
                    Console.WriteLine("Item " + lockItem.Key + " unlocked");
                    locks[lockItem.Key].lockState = LockStates.Unlock;
                }
                while (locks[lockItem.Key].waitTransactionIds.Count > 0 && (locks[lockItem.Key].lockState == LockStates.Unlock || (locks[lockItem.Key].lockState == LockStates.Read && locks[lockItem.Key].transactionIds.Count == 1 && locks[lockItem.Key].waitTransactionIds.FirstOrDefault() == locks[lockItem.Key].transactionIds.FirstOrDefault())))
                {
                    int waitListTransaction = locks[lockItem.Key].waitTransactionIds.FirstOrDefault();
                    locks[lockItem.Key].waitTransactionIds.Remove(waitListTransaction);
                    transactions[waitListTransaction].transactionState = TransactionStates.Active;

                    while (transactions[waitListTransaction].operationWaitlist.Count > 0 && transactions[waitListTransaction].transactionState == TransactionStates.Active)
                    {
                        string operation = transactions[waitListTransaction].operationWaitlist.FirstOrDefault();
                        if (transactions[waitListTransaction].transactionState == TransactionStates.Active)
                        {
                            transactions[waitListTransaction].operationWaitlist.Remove(operation);
                            AnalyseOperation(operation);
                            if (transactions[waitListTransaction].transactionState == TransactionStates.Blocked)
                            {
                                string lastOperation = transactions[waitListTransaction].operationWaitlist.Last();
                                transactions[waitListTransaction].operationWaitlist.Remove(lastOperation);
                                transactions[waitListTransaction].operationWaitlist.Insert(0, lastOperation);
                            }
                        }
                    }
                }
            }
        }
    }
}
