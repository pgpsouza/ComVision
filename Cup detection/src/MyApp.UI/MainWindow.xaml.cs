using System.Windows;
using MyApp.Services.Services;
using MyApp.UI.ViewModels;

namespace MyApp.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Simple manual DI for this template
        var service = new ItemService();
        DataContext = new MainViewModel(service);
    }
}
