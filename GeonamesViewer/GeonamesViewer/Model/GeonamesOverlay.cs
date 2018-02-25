using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeonamesViewer.Model
{
    /// <summary>
    /// Represents a geonames overlay wrapping a aggregated and a detailed graphics overlay.
    /// </summary>
    internal class GeonamesOverlay
    {
        private readonly GraphicsOverlay _detailedOverlay;
        private readonly FeatureLayer _countryLayer;
        private readonly GraphicsOverlay _countryOverlay;
        private bool _featureQueried;

        internal GeonamesOverlay(GraphicsOverlay detailedOverlay, FeatureLayer countryLayer, GraphicsOverlay countryOverlay)
        {
            _detailedOverlay = detailedOverlay;
            _countryLayer = countryLayer;
            _countryOverlay = countryOverlay;
        }

        /// <summary>
        /// Adds a new geonames record to the graphcis overlays.
        /// Should be called on the UI scheduler!
        /// </summary>
        /// <param name="record">the geonames record to add</param>
        internal async Task AddRecordAsync(GeonamesRecord record)
        {
            // Add the detailed graphic
            var attributes = record.GetAttributes();
            var location = record.GetLocation();
            var detailedGraphic = new Graphic(location, attributes);
            _detailedOverlay.Graphics.Add(detailedGraphic);

            if (!_featureQueried)
            {
                await QueryAllCountryFeaturesAsync();
                _featureQueried = true;
            }

            var relatedCountry = FindRelatedCountry(location);
            if (null != relatedCountry)
            {
                relatedCountry.IsVisible = true;
            }
        }

        private async Task QueryAllCountryFeaturesAsync()
        {
            // Query the country features
            var parameters = new QueryParameters();
            parameters.WhereClause = @"1=1";
            parameters.ReturnGeometry = true;
            var queryResult = await _countryLayer.FeatureTable.QueryFeaturesAsync(parameters);
            foreach (var countryFeature in queryResult)
            {
                var countryGraphic = new Graphic(countryFeature.Geometry);
                countryGraphic.IsVisible = false;
                _countryOverlay.Graphics.Add(countryGraphic);
            }
        }

        private static Graphic CreateCountryGraphicFromFeature(Feature feature)
        {
            var attributes = new Dictionary<string, object>(feature.Attributes);
            var geometryBuilder = new PolygonBuilder((Polygon) feature.Geometry);
            var geometry = geometryBuilder.ToGeometry();
            return new Graphic(geometry);
        }

        private Graphic FindRelatedCountry(MapPoint location)
        {
            foreach (var country in _countryOverlay.Graphics)
            {
                var countryGeometry = country.Geometry;
                var countrySpatialReference = countryGeometry.SpatialReference;
                Geometry locationGeometry;
                if (location.SpatialReference.Wkid != countrySpatialReference.Wkid)
                {
                    locationGeometry = GeometryEngine.Project(location, countrySpatialReference);
                }
                else
                {
                    locationGeometry = location;
                }
                if (GeometryEngine.Intersects(countryGeometry, locationGeometry))
                {
                    return country;
                }
            }
            return null;
        }
    }
}
