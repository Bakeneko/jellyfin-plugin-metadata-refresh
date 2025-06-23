using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.MetadataRefresh.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MetadataRefresh.ScheduledTasks
{
    /// <summary>
    /// Refresh metadata task.
    /// </summary>
    public class MetadataRefreshTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<MetadataRefreshTask> _logger;

        //ILibraryManager

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataRefreshTask"/> class.
        /// </summary>
        /// <param name="libraryManager">Library Manager.</param>
        /// <param name="providerManager">Provider Manager.</param>
        /// <param name="fileSystem">File System.</param>
        /// <param name="logger">Logger.</param>
        public MetadataRefreshTask(ILibraryManager libraryManager, IProviderManager providerManager, IFileSystem fileSystem, ILogger<MetadataRefreshTask> logger)
        {
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => "Refresh metadata";

        /// <inheritdoc/>
        public string Key => "MetadataRefreshTask";

        /// <inheritdoc/>
        public string Description => "Refresh metadata for libraries items.";

        /// <inheritdoc/>
        public string Category => "Metadata Refresh";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        private PluginConfiguration Configuration => Plugin.Instance!.Configuration;

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            _logger.LogInformation("Checking for items to refresh");

            var toRefreshItems = GetItemsToRefresh();
            _logger.LogInformation("Found {0} items to refresh.", toRefreshItems.Count);
            progress.Report(5);

            MetadataRefreshOptions refreshOptions = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
            {
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ReplaceAllMetadata = true,
                IsAutomated = false,
            };

            double increment = 95.0 / toRefreshItems.Count;
            double currentProgress = 5;

            foreach (BaseItem item in toRefreshItems)
            {
                _logger.LogInformation("Refreshing metadata for item {Id}: {Name} ({Type})", item.Id, item.Name, item.GetBaseItemKind());
                await _providerManager.RefreshSingleItem(
                    item,
                    refreshOptions,
                    cancellationToken
                ).ConfigureAwait(false);
                currentProgress += increment;
                progress.Report(currentProgress);
            }

            progress.Report(100);
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(1).Ticks }
            };
        }

        /// <summary>
        /// Gets items that need a refresh.
        /// </summary>
        /// <returns>List of items that need a refresh.</returns>
        private List<BaseItem> GetItemsToRefresh()
        {
            HashSet<BaseItem> toRefreshItems = new HashSet<BaseItem>();

            // List of types we are interested in
            BaseItemKind[] itemTypes = new[] {
                BaseItemKind.Movie,
                BaseItemKind.Series,
                BaseItemKind.Season,
                BaseItemKind.Episode,
                BaseItemKind.MusicAlbum,
                BaseItemKind.MusicVideo,
            };

            DateTime now = DateTime.UtcNow;

            var maxItemNumber = Configuration.MaxItemNumber;

            bool TryAddItems(IEnumerable<BaseItem> items)
            {
                toRefreshItems.UnionWith(items);
                return maxItemNumber > 0 && toRefreshItems.Count >= maxItemNumber;
            }

            // Released less than 1 day ago
            if (TryAddItems(GetItemsToRefresh(itemTypes, now.AddDays(-1), now.AddDays(1), now.AddDays(-Configuration.IntervalReleasesOfTheDay))))
            {
                return [.. toRefreshItems.Take(maxItemNumber)];
            }

            // Released less than a week ago
            if (TryAddItems(GetItemsToRefresh(itemTypes, now.AddDays(-7), now.AddDays(-1), now.AddDays(-Configuration.IntervalReleasesOfTheWeek))))
            {
                return [.. toRefreshItems.Take(maxItemNumber)];
            }

            // Released less than a month ago
            if (TryAddItems(GetItemsToRefresh(itemTypes, now.AddDays(-30), now.AddDays(-7), now.AddDays(-Configuration.IntervalReleasesOfTheMonth))))
            {
                return [.. toRefreshItems.Take(maxItemNumber)];
            }

            // Released less than a year ago
            if (TryAddItems(GetItemsToRefresh(itemTypes, now.AddDays(-365), now.AddDays(-30), now.AddDays(-Configuration.IntervalReleasesOfTheYear))))
            {
                return [.. toRefreshItems.Take(maxItemNumber)];
            }

            // Refresh items reaching max interval
            toRefreshItems.UnionWith(GetItemsToRefresh(itemTypes, null, null, now.AddDays(-Configuration.MaxInterval)));
             
            return [.. maxItemNumber > 0 ? toRefreshItems.Take(maxItemNumber) : toRefreshItems];
        }

        /// <summary>
        /// Add items that need a refresh.
        /// </summary>
        /// <returns>List of items that need a refresh.</returns>
        private HashSet<BaseItem> GetItemsToRefresh(BaseItemKind[] itemTypes, DateTime? minPremiere, DateTime? maxPremiere, DateTime maxRefresh)
        {
            HashSet<BaseItem> toRefreshItems = new HashSet<BaseItem>();

            InternalItemsQuery query = new InternalItemsQuery();
            query.IncludeItemTypes = itemTypes;
            query.MaxPremiereDate = maxPremiere;
            query.MinPremiereDate = minPremiere;
            List<BaseItem> itemList = _libraryManager.GetItemList(query).FindAll(item => item.DateLastRefreshed <= maxRefresh);
            toRefreshItems.UnionWith(itemList);

            return toRefreshItems;
        }
    }
}
