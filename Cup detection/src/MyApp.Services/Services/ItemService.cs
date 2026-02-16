using System.Collections.Generic;
using MyApp.Common.Models;

namespace MyApp.Services.Services;

public class ItemService : IItemService
{
    public IEnumerable<ItemModel> GetItems()
    {
        return new List<ItemModel>
        {
            new ItemModel { Id = 1, Name = "Item 1" },
            new ItemModel { Id = 2, Name = "Item 2" },
            new ItemModel { Id = 3, Name = "Item 3" }
        };
    }
}
