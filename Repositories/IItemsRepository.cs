using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Catalog.Models;

namespace Catalog.Repositories
{
    public interface IItemsRepository
    {
        Task<Item> GetItemAsync(Guid id);
        Task<IEnumerable<Item>> GetItemsAsync();
        Task CreateItemAsync(Item item);
        Task UpdatedItemAsync(Item item);
        Task DeleteItemAsync(Guid id);
    }
}