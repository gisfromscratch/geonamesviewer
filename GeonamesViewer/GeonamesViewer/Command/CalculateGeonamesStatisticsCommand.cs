using GeonamesViewer.Model;
using System;
using System.Windows.Input;

namespace GeonamesViewer.Command
{
    /// <summary>
    /// Calculates the statistics.
    /// </summary>
    internal class CalculateGeonamesStatisticsCommand : ICommand
    {
        private readonly GeonamesOverlay _overlay;

        internal CalculateGeonamesStatisticsCommand(GeonamesOverlay overlay)
        {
            _overlay = overlay;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _overlay.ShowStatistics();
        }
    }
}
