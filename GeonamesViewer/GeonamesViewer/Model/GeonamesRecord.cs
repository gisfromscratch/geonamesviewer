using Esri.ArcGISRuntime.Geometry;
using System.Collections.Generic;

namespace GeonamesViewer.Model
{
    /// <summary>
    /// Represents a geonames record.
    /// </summary>
    internal class GeonamesRecord
    {
        private static SpatialReference Wgs84;

        static GeonamesRecord()
        {
            Wgs84 = SpatialReference.Create(4326);
        }

        internal string GeonamesId { get; set; }

        internal string Name { get; set; }

        internal double Latitude { get; set; }

        internal double Longitude { get; set; }

        /// <summary>
        /// Gets the attributes as a newly created dictionary.
        /// </summary>
        internal IDictionary<string, object> GetAttributes()
        {
            var attributes = new Dictionary<string, object>();
            attributes[@"GeonamesId"] = GeonamesId;
            attributes[@"Name"] = Name;
            attributes[@"Latitude"] = Latitude;
            attributes[@"Longitude"] = Longitude;
            return attributes;
        }

        /// <summary>
        /// Gets the underlying location using a spatial reference of WGS84.
        /// The geometry instance is always newly created.
        /// </summary>
        internal MapPoint GetLocation()
        {
            return new MapPoint(Longitude, Latitude, Wgs84);
        }
    }
}
