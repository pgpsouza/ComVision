using System.Collections.Generic;
using MyApp.Common.Models;

namespace MyApp.Services.Services;

public interface IItemService
{
    IEnumerable<ItemModel> GetItems();
}
