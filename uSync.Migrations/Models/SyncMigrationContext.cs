﻿namespace uSync.Migrations.Models;

/// <summary>
///  A uSync migration context, lets us keep a whole list of things in memory while we do the migration.
/// </summary>
public class SyncMigrationContext
{
    private HashSet<string> _blockedTypes = new(StringComparer.OrdinalIgnoreCase);

    private HashSet<string> _ignoredProperties = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///  list of keys to editor aliases used to lookup datatypes in content types !
    /// </summary>

    private Dictionary<Guid, string> _dataTypeDefinitions { get; set; } = new();

    /// <summary>
    ///  when we replace an datatype with something else .
    /// </summary>
    private Dictionary<Guid, Guid> _dataTypeReplacements { get; set; } = new();

    /// <summary>
    ///  datatypes that vary by something (e.g culture)
    /// </summary>
    private Dictionary<Guid, string> _dataTypeVariations { get; set; } = new();

    private Dictionary<Guid, string> _contentKeys { get; set; } = new();
    private Dictionary<Guid, string> _contentPaths { get; set; } = new();

    private Dictionary<string, Guid> _contentTypeKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, HashSet<string>> _contentTypeCompositions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, EditorAliasInfo> _propertyTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Guid> _templateKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);


    public SyncMigrationContext(Guid migrationId)
    {
        MigrationId = migrationId;
    }

    public Guid MigrationId { get; }


    /// <summary>
    ///  Add a template key to the context.
    /// </summary>
    public void AddTemplateKey(string templateAlias, Guid templateKey)
         => _ = _templateKeys.TryAdd(templateAlias, templateKey);

    /// <summary>
    ///  get a template key (Guid) from the context 
    /// </summary>
    public Guid GetTemplateKey(string templateAlias)
         => _templateKeys?.TryGetValue(templateAlias, out var key) == true ? key : Guid.Empty;

    /// <summary>
    ///  Add a ccontent type key to the context.
    /// </summary>
    /// <param name="contentTypeAlias"></param>
    /// <param name="contentTypeKey"></param>

    public void AddContentTypeKey(string? contentTypeAlias, Guid? contentTypeKey)
    {
        _ = string.IsNullOrWhiteSpace(contentTypeAlias) == false &&
            contentTypeKey.HasValue == true &&
            _contentTypeKeys.TryAdd(contentTypeAlias, contentTypeKey.Value);
    }

    /// <summary>
    ///  get the key for a given content type alias from the context.
    /// </summary>
    public Guid GetContentTypeKey(string contentTypeAlias)
        => _contentTypeKeys?.TryGetValue(contentTypeAlias, out var key) == true ? key : Guid.Empty;


    /// <summary>
    ///  add content type compositions to the context
    /// </summary>
    public void AddContentTypeCompositions(string? contentTypeAlias, IEnumerable<string>? compositionAliases)
    {
        _ = string.IsNullOrWhiteSpace(contentTypeAlias) == false &&
            compositionAliases?.Any() == true &&
            _contentTypeCompositions.TryAdd(contentTypeAlias, compositionAliases.ToHashSet());
    }

    /// <summary>
    ///  add the path for a content item to context. 
    /// </summary>
    public void AddContentPath(Guid key, string path)
         => _ = _contentPaths.TryAdd(key, path);

    /// <summary>
    ///  get the content path for a parent item from the context.
    /// </summary>
    public string GetContentPath(Guid parentKey)
        => _contentPaths?.TryGetValue(parentKey, out var path) == true ? path : string.Empty;

    /// <summary>
    ///  Add a editorAlias mapping for a property mapping to the context.
    /// </summary>
    /// <remarks>
    ///  allows you to track when the editor alias of a property changes from original to a new value
    /// </remarks>
   
    public void AddContentProperty(string? contentTypeAlias, string? propertyAlias, string? originalAlias, string? newAlias)
    {
        _ = string.IsNullOrWhiteSpace(contentTypeAlias) == false &&
            string.IsNullOrWhiteSpace(propertyAlias) == false &&
            string.IsNullOrWhiteSpace(originalAlias) == false &&
            string.IsNullOrWhiteSpace(newAlias) == false &&
            _propertyTypes.TryAdd($"{contentTypeAlias}_{propertyAlias}",
            new EditorAliasInfo(originalAlias, newAlias));
    }

    /// <summary>
    ///  get the migrated editro alias for a property based on the content type it is in.
    /// </summary>
    /// <remarks>
    ///  this has to be done by content type, because when we are in content, we don't know
    ///  about the underling data type. 
    ///  
    ///  so when content types are prepped for migration they add this key pair (AddContentProperty)
    ///  and then when we are in content we can say, what is the underling property for this 
    ///  value based on the content type we know we are in. 
    /// </remarks>
    public EditorAliasInfo? GetEditorAlias(string contentType, string propertyAlias)
    {
        if (_propertyTypes?.TryGetValue($"{contentType}_{propertyAlias}", out var alias) == true)
        {
            return alias;
        }
        else if (_contentTypeCompositions?.TryGetValue(contentType, out var compositions) == true)
        {
            foreach (var composition in compositions)
            {
                if (_propertyTypes?.TryGetValue($"{composition}_{propertyAlias}", out var alias1) == true)
                {
                    return alias1;
                }
            }
        }

        return null;
    }
    /// <summary>
    ///  add a content key to the context.
    /// </summary>
    public void AddContentKey(Guid key, string alias)
        => _ = _contentKeys.TryAdd(key, alias);

    /// <summary>
    ///  get a context alias from the context
    /// </summary>
    public string GetContentAlias(Guid key)
        => _contentKeys?.TryGetValue(key, out var alias) == true ? alias : string.Empty;

    /// <summary>
    ///  is this item blocked based on alias and type. 
    /// </summary>
    public bool IsBlocked(string itemType, string alias)
        => _blockedTypes.Contains($"{itemType}_{alias}") == true;

    /// <summary>
    ///  add a blocked item to the context.
    /// </summary>
    public void AddBlocked(string itemType, string alias)
        => _ = _blockedTypes.Add($"{itemType}_{alias}");


    /// <summary>
    ///  ignore a property on a specific content type. 
    /// </summary>
    /// <remarks>
    ///  note this is the final content type, will not calculate compositions.
    /// </remarks>
    public void AddIgnoredProperty(string contentType, string alias)
        => _ = _ignoredProperties.Add($"{contentType}_{alias}");

    /// <summary>
    ///  add a property to ignore for all content types.
    /// </summary>
    public void AddIgnoredProperty(string alias)
    => _ = _ignoredProperties.Add($"{alias}");

    public bool IsIgnoredProperty(string contentType, string alias)
        => _ignoredProperties.Contains($"{contentType}_{alias}") 
        || _ignoredProperties.Contains(alias);

    /// <summary>
    ///  add a datatypedefinion (aka datatype key) to the context.
    /// </summary>
    public void AddDataTypeDefinition(Guid dtd, string editorAlias)
        => _ = _dataTypeDefinitions.TryAdd(dtd, editorAlias);

    /// <summary>
    ///  get a datatype definiton from the context.
    /// </summary>
    public string GetDataTypeFromDefinition(Guid guid)
        => _dataTypeDefinitions?.TryGetValue(guid, out var editorAlias) == true
            ? editorAlias
            : string.Empty;

    /// <summary>
    ///  add the key that replaces a datatype to the context.
    /// </summary>
    public void AddReplacementDataType(Guid orginal, Guid replacement)
        => _ = _dataTypeReplacements.TryAdd(orginal, replacement);

    /// <summary>
    ///  get any replacement key values for a given datatype key
    /// </summary>
    public Guid GetReplacementDataType(Guid orginal)
        => _dataTypeReplacements?.TryGetValue(orginal, out var replacement) == true
            ? replacement
            : orginal;

    /// <summary>
    ///  add a variation (e.g culture, segment or nothing) value for a datatype to the context.
    /// </summary>
    public void AddDataTypeVariation(Guid guid, string variation)
        => _ = _dataTypeVariations?.TryAdd(guid, variation);

    /// <summary>
    ///  retrieve the variation that a datatype will ask a doctype property to perform.
    /// </summary>
    public string GetDataTypeVariation(Guid guid)
        => _dataTypeVariations?.TryGetValue(guid, out var variation) == true
            ? variation : "Nothing";
}


public class EditorAliasInfo
{
    public EditorAliasInfo(string orginalEditorAlias, string updatedEditorAlias)
    {
        OrginalEditorAlias = orginalEditorAlias;
        UpdatedEditorAlias = updatedEditorAlias;
    }

    public string OrginalEditorAlias { get; }
    public string UpdatedEditorAlias { get; }
}