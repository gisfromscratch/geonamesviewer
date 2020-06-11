using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeonamesViewer.Model
{
    /// <summary>
    /// Represents a geonames overlay wrapping a aggregated and a detailed graphics overlay.
    /// </summary>
    internal class GeonamesOverlay
    {
        private readonly GraphicsOverlay _detailedOverlay;
        private readonly ServiceFeatureTable _countryTable;
        private readonly GraphicsOverlay _countryOverlay;
        private bool _featureQueried;
        private IDictionary<string, CountryEntry> _countries;

        internal GeonamesOverlay(GraphicsOverlay detailedOverlay, ServiceFeatureTable countryTable, GraphicsOverlay countryOverlay)
        {
            _detailedOverlay = detailedOverlay;
            _countryTable = countryTable;
            _countryOverlay = countryOverlay;
            _countries = new Dictionary<string, CountryEntry>();
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
                var countryAttributes = relatedCountry.Attributes;
                if (countryAttributes.ContainsKey(@"FID"))
                {
                    var fid = countryAttributes[@"FID"].ToString();
                    if (_countries.ContainsKey(fid))
                    {
                        var countryEntry = _countries[fid];
                        countryEntry.HitCount++;
                        if (countryAttributes.ContainsKey(@"HitCount"))
                        {
                            countryAttributes[@"HitCount"] = countryEntry.HitCount;
                        }
                    }
                }
            }
        }

        internal void ShowStatistics()
        {
            var countryList = _countries.ToList();
            countryList.Sort((country, otherCountry) => country.Value.HitCount.CompareTo(otherCountry.Value.HitCount));
            foreach (var country in countryList)
            {
#if DEBUG
                Console.WriteLine(country);
#endif
            }
        }

        private async Task QueryAllCountryFeaturesAsync()
        {
            // Query all the country features
            var parameters = new QueryParameters();
            parameters.WhereClause = @"1=1";
            parameters.ReturnGeometry = true;
            //var queryResult = await _countryTable.QueryFeaturesAsync(parameters, QueryFeatureFields.LoadAll);
            var outFields = new[] { @"*" };
            var queryResult = await _countryTable.PopulateFromServiceAsync(parameters, true, outFields);
            foreach (var countryFeature in queryResult)
            {
                var countryAttributes = new Dictionary<string, object>(countryFeature.Attributes);
                countryAttributes.Add(@"HitCount", 0L);
                var countryGraphic = new Graphic(countryFeature.Geometry, countryAttributes);
                countryGraphic.IsVisible = false;
                _countryOverlay.Graphics.Add(countryGraphic);
                const string countryFieldName = @"COUNTRY";
                if (countryAttributes.ContainsKey(@"FID") && countryAttributes.ContainsKey(countryFieldName))
                {
                    var fid = countryAttributes[@"FID"].ToString();
                    var countryName = countryAttributes[countryFieldName].ToString();
                    _countries.Add(fid, new CountryEntry { Name = countryName, HitCount = 0L });
                }
            }
        }

        private Graphic FindRelatedCountry(MapPoint location)
        {
            // TODO: Use a spatial index!
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
