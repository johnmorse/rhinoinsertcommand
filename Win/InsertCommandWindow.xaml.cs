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
	/// Interaction logic for InsertCommandWindow.xaml
	/// </summary>
  public partial class InsertCommandWindow : Window
	{
		public InsertCommandWindow()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
		}

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
      var model = DataContext as Insert.ViewModels.InsertCommandViewModel;
      if (null != model && !model.OkayToClose())
        return;
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {

    }
	}
}