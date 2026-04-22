using Demo.Math;
using System.Windows;
namespace WpfCalculator;


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // MessageBox.Show(Calculator.Add(12, 3).ToString());
        int result = Calculator..Add(12, 3);
        MessageBox.Show(Calculator.AsHex(result));
    }
}