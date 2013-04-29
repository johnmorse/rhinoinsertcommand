using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Insert.Win
{
	/// <summary>
	/// Interaction logic for BlockPropertiesWindow.xaml
	/// </summary>
	public partial class BlockPropertiesWindow : Window
	{
		public BlockPropertiesWindow()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
		}

    private void  OnOkButtonClicked(object sender, RoutedEventArgs e)
    {
      var model = DataContext as Insert.ViewModels.BlockPropertiesViewModel;
      if (null != model && !model.OkayToClose())
        return;
      // DialogResult is set by OkayToClose() method
      Close();
    }
    private void OnCancelButtonClicked(object sender, RoutedEventArgs e)
    {
      DialogResult = null;
      Close();
    }
    private void OnHelpButtonClicked(object sender, RoutedEventArgs e)
    {

    }
  }
}