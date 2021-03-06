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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
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
        private DispatcherTimer _timer;
        private bool _raiseOpacity;
        private ServiceFeatureTable _countries;
        private ICommand _loadGeonamesFileCommand;
        private ICommand _calculateGeonamesStatisticsCommand;
        private ICommand _exportMapScreenshotCommand;

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

                    // Create a timer
                    _timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
                    _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    _timer.Tick += Animate;

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

        internal void UpdateMapView(MapView focusMapView)
        {
            ExportMapScreenshotCommand = new ExportMapScreenshotCommand(focusMapView);
        }

        private void Animate(object sender, EventArgs e)
        {
            const double minOpacity = 0.25;
            const double maxOpacity = 0.75;
            const double interval = 0.25;
            var opacity = _countriesOverlay.Opacity;
            if (_raiseOpacity)
            {
                if (opacity < maxOpacity)
                {
                    opacity += interval;
                }
                else
                {
                    opacity -= interval;
                    _raiseOpacity = !_raiseOpacity;
                }
            }
            else
            {
                if (minOpacity < opacity)
                {
                    opacity -= interval;
                }
                else
                {
                    opacity += interval;
                    _raiseOpacity = !_raiseOpacity;
                }
            }

            _countriesOverlay.Opacity = opacity;
        }

        private static ServiceFeatureTable CreateWorldCountriesTable()
        {
            var table = new ServiceFeatureTable(new Uri(@"https://services.arcgis.com/P3ePLMYs2RVChkJx/ArcGIS/rest/services/World_Countries_(Generalized)/FeatureServer/0"));
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
            overlay.LabelsEnabled = true;
            var labelDefinition = CreateCountriesLabelDefinition();
            overlay.LabelDefinitions.Add(labelDefinition);
            return overlay;
        }

        private static Renderer CreateGeonamesRenderer()
        {
            var geonamesSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, Color.White, 8);
            return new SimpleRenderer(geonamesSymbol);
        }

        private static Renderer CreateCountriesRenderer()
        {
            var countryBorderSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Transparent, 0);
            var countryFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Color.White, countryBorderSymbol);

            var breaks = new List<ClassBreak>();
            var firstColor = Color.FromArgb(200, 200, 200);
            var firstSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, firstColor, countryBorderSymbol);
            var firstBreak = new ClassBreak(@"Low hit count", @"Low", 0, 5, firstSymbol);
            breaks.Add(firstBreak);
            var secondColor = Color.FromArgb(210, 210, 210);
            var secondSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, secondColor, countryBorderSymbol);
            var secondBreak = new ClassBreak(@"Medium hit count", @"Medium", 6, 50, secondSymbol);
            breaks.Add(secondBreak);
            var thirdColor = Color.FromArgb(220, 220, 220);
            var thirdSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, thirdColor, countryBorderSymbol);
            var thirdBreak = new ClassBreak(@"Medium hit count", @"Medium", 51, 100, secondSymbol);
            breaks.Add(thirdBreak);
            var renderer = new ClassBreaksRenderer(@"HitCount", breaks);
            renderer.DefaultSymbol = countryFillSymbol;
            return renderer;
        }

        private static LabelDefinition CreateGeonamesLabelDefinition()
        {
            return CreateLabelDefinition(@"[Name]", @"esriServerPointLabelPlacementAboveCenter", Color.White);
        }

        private static LabelDefinition CreateCountriesLabelDefinition()
        {
            return CreateLabelDefinition(@"[HitCount]", @"esriServerPolygonPlacementAlwaysHorizontal", Color.Black);
        }

        private static LabelDefinition CreateLabelDefinition(string expression, string placement, Color textColor)
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append(@"{");
            jsonBuilder.Append(CreateJsonPropertyAsString(@"labelExpression", expression));
            jsonBuilder.Append(@",");
            jsonBuilder.Append(CreateJsonPropertyAsString(@"labelPlacement", placement));
            var labelTextSymbol = new TextSymbol();
            labelTextSymbol.Color = textColor;
            labelTextSymbol.Size = 12;
            labelTextSymbol.FontFamily = @"Arial";
            labelTextSymbol.FontStyle = Esri.ArcGISRuntime.Symbology.FontStyle.Normal;
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

        internal void StartAnimation()
        {
#if DEBUG
#else
            _timer.Start();
#endif
        }

        internal void StopAnimation()
        {
#if DEBUG
#else
            _timer.Stop();
#endif
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
        /// Gets or sets the command for exporting the focus map as a screenshot.
        /// </summary>
        public ICommand ExportMapScreenshotCommand
        {
            get { return _exportMapScreenshotCommand; }
            set
            {
                _exportMapScreenshotCommand = value;
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
