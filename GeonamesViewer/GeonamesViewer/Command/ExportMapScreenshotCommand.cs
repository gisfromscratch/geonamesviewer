using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GeonamesViewer.Command
{
    /// <summary>
    /// Exports a screenshot of the current map as an image.
    /// </summary>
    internal class ExportMapScreenshotCommand : ICommand
    {
        private readonly MapView _focusMapView;

        internal ExportMapScreenshotCommand(MapView focusMapView)
        {
            _focusMapView = focusMapView;
            _focusMapView.DrawStatusChanged += MapViewDrawStatusChanged;
        }

        private void MapViewDrawStatusChanged(object sender, DrawStatusChangedEventArgs evt)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return DrawStatus.Completed == _focusMapView.DrawStatus;
        }

        public async void Execute(object parameter)
        {
            var mapImage = await _focusMapView.ExportImageAsync();
            var mapImageSource = await mapImage.ToImageSourceAsync();
            var mapBitmap = mapImageSource as WriteableBitmap;
            if (null != mapBitmap)
            {
                var bitmapFilePath = Path.Combine(Path.GetTempPath(), string.Format("geonames-{0}.png", DateTime.Today.ToString(@"yyyy-MM-dd")));
                using (var bitmapFileStream = new FileStream(bitmapFilePath, FileMode.OpenOrCreate))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(mapBitmap));
                    encoder.Save(bitmapFileStream);
                }
            }
        }
    }
}
