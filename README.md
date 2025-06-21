<h1 align="center">Jellyfin Metadata Refresh Plugin</h1>

## About

The Jellyfin Metadata Refresh plugin enables configurable metadata updates.


## Build & Installation Process

1. Clone this repository

2. Ensure you have .NET Core SDK setup and installed

3. Build the plugin with following command:

```bash
dotnet publish --configuration Release --output bin
```

4. Place the resulting `Jellyfin.Plugin.MetadataRefresh.dll` file in a folder called `plugins/` inside your Jellyfin installation / data directory.
