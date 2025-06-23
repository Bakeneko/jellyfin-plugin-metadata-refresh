using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MetadataRefresh.Configuration
{
    /// <summary>
    /// Plugin configuration class for Metadata Refresh plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            IntervalReleasesOfTheDay = 0.125;
            IntervalReleasesOfTheWeek = 1;
            IntervalReleasesOfTheMonth = 7;
            IntervalReleasesOfTheYear = 30;
            MaxInterval = 90;
            MaxItemNumber = 0;
        }

        /// <summary>
        /// Gets or sets the interval between refresh for the releases of the day.
        /// </summary>
        public double IntervalReleasesOfTheDay { get; set; }

        /// <summary>
        /// Gets or sets the interval between refresh for the releases of the week.
        /// </summary>
        public double IntervalReleasesOfTheWeek { get; set; }

        /// <summary>
        /// Gets or sets the interval between refresh for the releases of the month.
        /// </summary>
        public double IntervalReleasesOfTheMonth { get; set; }

        /// <summary>
        /// Gets or sets the interval between refresh for the releases of the year.
        /// </summary>
        public double IntervalReleasesOfTheYear { get; set; }

        /// <summary>
        /// Gets or sets the maximum interval between refresh.
        /// </summary>
        public double MaxInterval { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items refreshed.
        /// </summary>
        public int MaxItemNumber { get; set; }
    }
}
