using Avalonia.Controls;
using Avalonia.Interactivity;
using BMC.CCTVMonitoring.ViewModels;

namespace BMC.CCTVMonitoring.Views;

public partial class MainView : UserControl
{
    //MainViewModel vm;
    public MainView()
    {
        InitializeComponent();
        //if (vm == null) vm = new MainViewModel();
        //this.DataContext = vm;
    }
    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        //do something on click
        var vm = this.DataContext as MainViewModel;
        vm.SaveSettings();
    }

    public void Start(object sender, RoutedEventArgs args)
    {
        var vm = this.DataContext as MainViewModel;
        vm.Start();
    }

    public void Stop(object sender, RoutedEventArgs args)
    {
        var vm = this.DataContext as MainViewModel;
        vm.Stop();
    }
}
