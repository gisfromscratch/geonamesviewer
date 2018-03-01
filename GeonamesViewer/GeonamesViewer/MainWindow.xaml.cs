using Esri.ArcGISRuntime.UI.Controls;
using GeonamesViewer.ViewModel;
using System.Windows;

namespace GeonamesViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MapViewModel _viewModel;

        public MainWindow()
        {
            // Initialize view model
            _viewModel = new MapViewModel();

            DataContext = _viewModel;
            InitializeComponent();
        }

        private void MapView_DragEnter(object sender, DragEventArgs e)
        {
            // TODO: Use interactions or MVVM framework
            var dragData = e.Data;
            if (dragData.GetDataPresent(DataFormats.FileDrop))
            {
                var files = dragData.GetData(DataFormats.FileDrop);
                if (_viewModel.LoadGeonamesFileCommand.CanExecute(files))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            }
        }

        private void MapView_Drop(object sender, DragEventArgs e)
        {
            // TODO: Use interactions or MVVM framework
            var files = e.Data.GetData(DataFormats.FileDrop);
            _viewModel.LoadGeonamesFileCommand.Execute(files);
        }

        private void MapView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            // TOOD: Use command from UI
            if (_viewModel.CalculateGeonamesStatisticsCommand.CanExecute(null))
            {
                _viewModel.CalculateGeonamesStatisticsCommand.Execute(null);
            }
        }

        private void MapView_SpatialReferenceChanged(object sender, System.EventArgs e)
        {
            _viewModel.StartAnimation();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.StopAnimation();
        }

        // Map initialization logic is contained in MapViewModel.cs
    }
}
