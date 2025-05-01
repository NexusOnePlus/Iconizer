using System.Windows;
using System.Windows.Controls;

namespace Iconizer.Presentation.View.UserControls;

public partial class ClearTextBox : UserControl
{
    public event EventHandler? RequestRemove;
    public ClearTextBox()
    {
        InitializeComponent();
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {   
        RequestRemove?.Invoke(this, EventArgs.Empty);
    }
}