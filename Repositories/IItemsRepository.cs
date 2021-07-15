using System;
using System.Collections.Generic;
using Catalog.Models;

namespace Catalog.Repositories
{
    public interface IItemsRepository
    {
        Item GetItem(Guid id);
        IEnumerable<Item> GetItems();
    }
}