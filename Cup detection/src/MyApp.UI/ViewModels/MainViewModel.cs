using System.Collections.ObjectModel;
using System.Windows.Input;
using MyApp.Common.Models;
using MyApp.Services.Services;

namespace MyApp.UI.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly IItemService _itemService;

    public ObservableCollection<ItemModel> Items { get; } = new();

    private ItemModel? _selectedItem;
    public ItemModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public ICommand ShowCommand { get; }

    public MainViewModel(IItemService itemService)
    {
        _itemService = itemService;
        foreach (var item in _itemService.GetItems())
            Items.Add(item);

        ShowCommand = new RelayCommand(Show);
    }

    private void Show()
    {
        var name = SelectedItem?.Name ?? "(none)";
        System.Windows.MessageBox.Show($"Selected: {name}");
    }
}