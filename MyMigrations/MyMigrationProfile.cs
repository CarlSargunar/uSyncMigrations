﻿using Umbraco.Cms.Core.Models;

using uSync.Migrations;
using uSync.Migrations.Composing;
using uSync.Migrations.Configuration.Models;
using uSync.Migrations.Extensions;
using uSync.Migrations.Migrators;

namespace MyMigrations;

/***** this code is not hooked up by default, it never runs *****/

/// <summary>
///  A Custom migration profile, to do things in special ways.
/// </summary>

public class MyMigrationProfile : ISyncMigrationProfile
{
    private readonly SyncMigrationHandlerCollection _migrationHandlers;

    public MyMigrationProfile(SyncMigrationHandlerCollection migrationHandlers)
    {
        _migrationHandlers = migrationHandlers;
    }

    public string Name => "My Migration Profile";

    public string Icon => "icon-cloud color-blue";

    public string Description => "My Custom migration with changes";

    public MigrationOptions Options => new()
    {
        // write out to the same folder each time.
        Target = $"{uSyncMigrations.MigrationFolder}/My-Custom-Migration",

        // load all the handlers just enable the content ones.
        Handlers = _migrationHandlers
                        .Handlers
                        .Select(x => x.ToHandlerOption(x.Group == uSync.BackOffice.uSyncConstants.Groups.Content)),

        // eveything beneath is optional... 

        // add a list of things we don't want to import 
        BlockedItems = new Dictionary<string, List<string>>
        {
            { nameof(DataType),
                new List<string> {
                    "Custom.LegacyType", "My.BoxGrid.Things"
                }
            }
        },

        // add a list of properties we are ignoring on all content
        IgnoredProperties = new List<string>
        {
            "SeoMetaDescription", "SeoToastPopup", "Keywords"
        },

        // add things we only want to ignore on certain types

        IgnoredPropertiesByContentType = new Dictionary<string, List<string>>
        {
            { "HomePage", new List<string>
                {
                    "SiteName", "GoogleAnalyticsCode"
                }
            }
        },
    };
}
