/*
 * Copyright 2018 Jan Tschada
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using GeonamesViewer.Command;

namespace GeonamesViewer.ViewModel
{
    /// <summary>
    /// Provides map data to the application
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
        private readonly Basemap _basemap;
        private Map _map;
        private GraphicsOverlayCollection _overlays;
        private GraphicsOverlay _geonamesOverlay;
        private ICommand _loadGeonamesFileCommand;

        public MapViewModel()
        {
            // Initialize a new basemap
            _basemap = Basemap.CreateTopographicVector();
            var status = _basemap.LoadStatus;
            switch (status)
            {
                case LoadStatus.Loaded:
                    // Create a map
                    Map = new Map(_basemap);

                    // Create a graphics overlay
                    _geonamesOverlay = CreateGeonamesOverlay();

                    // Add it to the map view
                    Overlays = new GraphicsOverlayCollection();
                    Overlays.Add(_geonamesOverlay);
                    break;

                default:
                    // TODO: Error handling
                    break;
            }

            // Update the commands
            LoadGeonamesFileCommand = new LoadGeonamesFileCommand(_geonamesOverlay);
        }

        private static GraphicsOverlay CreateGeonamesOverlay()
        {
            var overlay = new GraphicsOverlay();
            overlay.RenderingMode = GraphicsRenderingMode.Static;
            overlay.Renderer = CreateGeonamesRenderer();
            // TODO: Create a kind of spatial binning
            //overlay.MinScale = 2000000;
            return overlay;
        }

        private static Renderer CreateGeonamesRenderer()
        {
            var geonamesSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Colors.Black, 3);
            return new SimpleRenderer(geonamesSymbol);
        }

        /// <summary>
        /// Gets or sets the map
        /// </summary>
        public Map Map
        {
            get { return _map; }
            set
            {
                _map = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the graphics overlays.
        /// </summary>
        public GraphicsOverlayCollection Overlays
        {
            get { return _overlays; }
            set
            {
                _overlays = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the command for loading a geonames file.
        /// </summary>
        public ICommand LoadGeonamesFileCommand
        {
            get { return _loadGeonamesFileCommand; }
            set
            {
                _loadGeonamesFileCommand = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var propertyChangedHandler = PropertyChanged;
            if (propertyChangedHandler != null)
                propertyChangedHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
