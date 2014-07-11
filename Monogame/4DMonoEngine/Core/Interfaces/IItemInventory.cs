using System.Collections.Generic;

namespace _4DMonoEngine.Core.Interfaces
{
    interface IItemInventory
    {
        bool ContainsItem(int itemId);
        bool ContainsItem(IItem item);
        IItem RemoveItem(int index);
        IItem RemoveItem(int row, int column);
        bool CanAddItem(IItem item);
        int AddItem(IItem item);
        bool AddItemAt(IItem item, int index);
        bool AddItemAt(IItem item, int row, int column);
        int NextEmptyIndex { get; }
        int NumberOfItems { get; }
        int Capacity { get; }
        void TransferAll(out IItemInventory target);
        IEnumerable<IItem> Dump();
        IEnumerable<IItem> Dump(int itemId);
        IEnumerable<IItem> Dump(IItem item);
    }
}
