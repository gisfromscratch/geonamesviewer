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
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using GeonamesViewer.Command;
using GeonamesViewer.Model;

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
        private GraphicsOverlay _countriesOverlay;
        private ServiceFeatureTable _countries;
        private ICommand _loadGeonamesFileCommand;
        private ICommand _calculateGeonamesStatisticsCommand;

        public MapViewModel()
        {
            // Initialize a new basemap
            _basemap = Basemap.CreateDarkGrayCanvasVector();
            var status = _basemap.LoadStatus;
            switch (status)
            {
                case LoadStatus.Loaded:
                    // Create a map
                    Map = new Map(_basemap);

                    // Create and load the countries layer
                    _countries = CreateWorldCountriesTable();
                    _countries.LoadAsync();

                    // Create the graphics overlays
                    _geonamesOverlay = CreateGeonamesOverlay();
                    _countriesOverlay = CreateCountriesOverlay();

                    // Add it to the map view
                    Overlays = new GraphicsOverlayCollection();
                    Overlays.Add(_geonamesOverlay);
                    Overlays.Add(_countriesOverlay);
                    break;

                default:
                    // TODO: Error handling
                    break;
            }

            // Update the commands
            var geonamesOverlay = new GeonamesOverlay(_geonamesOverlay, _countries, _countriesOverlay);
            LoadGeonamesFileCommand = new LoadGeonamesFileCommand(geonamesOverlay);
            CalculateGeonamesStatisticsCommand = new CalculateGeonamesStatisticsCommand(geonamesOverlay);
        }

        private static ServiceFeatureTable CreateWorldCountriesTable()
        {
            var table = new ServiceFeatureTable(new Uri(@"http://services.arcgis.com/P3ePLMYs2RVChkJx/ArcGIS/rest/services/World_Countries_(Generalized)/FeatureServer/0"));
            table.FeatureRequestMode = FeatureRequestMode.ManualCache;
            return table;
        }

        private static GraphicsOverlay CreateGeonamesOverlay()
        {
            var overlay = new GraphicsOverlay();
            overlay.RenderingMode = GraphicsRenderingMode.Static;
            overlay.Renderer = CreateGeonamesRenderer();
            overlay.MinScale = 3000000;
            overlay.LabelsEnabled = true;
            var labelDefinition = CreateGeonamesLabelDefinition();
            overlay.LabelDefinitions.Add(labelDefinition);
            return overlay;
        }

        private static GraphicsOverlay CreateCountriesOverlay()
        {
            var overlay = new GraphicsOverlay();
            overlay.RenderingMode = GraphicsRenderingMode.Static;
            overlay.Renderer = CreateCountriesRenderer();
            overlay.Opacity = 0.75;
            overlay.MaxScale = 3000000;
            return overlay;
        }

        private static Renderer CreateGeonamesRenderer()
        {
            var geonamesSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Colors.White, 8);
            return new SimpleRenderer(geonamesSymbol);
        }

        private static Renderer CreateCountriesRenderer()
        {
            var countryBorderSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Colors.Transparent, 0);
            var countryFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Colors.White, countryBorderSymbol);
            return new SimpleRenderer(countryFillSymbol);
        }

        private static LabelDefinition CreateGeonamesLabelDefinition()
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append(@"{");
            jsonBuilder.Append(CreateJsonPropertyAsString(@"labelExpression", @"[Name]"));
            jsonBuilder.Append(@",");
            jsonBuilder.Append(CreateJsonPropertyAsString(@"labelPlacement", @"esriServerPointLabelPlacementAboveCenter"));
            var labelTextSymbol = new TextSymbol();
            labelTextSymbol.Color = Colors.White;
            labelTextSymbol.Size = 12;
            labelTextSymbol.FontFamily = @"Arial";
            labelTextSymbol.FontStyle = FontStyle.Normal;
            labelTextSymbol.FontWeight = FontWeight.Bold;
            var symbolAsJson = labelTextSymbol.ToJson();
            jsonBuilder.Append(@",");
            jsonBuilder.Append("\"symbol\":");
            jsonBuilder.Append(symbolAsJson);
            jsonBuilder.Append(@"}");
            return LabelDefinition.FromJson(jsonBuilder.ToString());
        }

        private static string CreateJsonPropertyAsString(string key, string value)
        {
            return string.Format("\"{0}\":\"{1}\"", key, value);
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
        /// Gets or sets the command for calculating the statistics.
        /// </summary>
        public ICommand CalculateGeonamesStatisticsCommand
        {
            get { return _calculateGeonamesStatisticsCommand; }
            set
            {
                _calculateGeonamesStatisticsCommand = value;
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
