using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Expression;

namespace Altinn.App.Core.Models.Layout;


/// <summary>
/// Inteface to be able to handle all most components same way.
/// </summary>
/// <remarks>
/// See <see cref="GroupComponent" /> for any components that handle children.
/// Includes <see cref="DataModelBindings" /> that will be initialized to an empty dictionary
/// for components that don't have them.
/// </remarks>
public class BaseComponent
{
    /// <summary>
    /// Constructor that ensures n
    /// </summary>
    public BaseComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, LayoutExpression? hidden, LayoutExpression? required, IReadOnlyDictionary<string, JsonElement>? extra)
    {
        Id = id;
        Type = type;
        DataModelBindings = dataModelBindings ?? ImmutableDictionary<string, string>.Empty;
        Hidden = hidden;
        Required = required;
        Extra = extra ?? ImmutableDictionary<string, JsonElement>.Empty;
    }
    /// <summary>
    /// ID of the component (or pagename for pages)
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Get the page for the component
    /// </summary>
    public string Page
    {
        get
        {
            //Get the Id of the first component without a parent.
            return Parent?.Page ?? Id;
        }
    }

    /// <summary>
    /// Component type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if component should be hidden
    /// </summary>
    public LayoutExpression? Hidden { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if component should be required
    /// </summary>
    public LayoutExpression? Required { get; }

    /// <summary>
    /// Data model bindings for the component or group
    /// </summary>
    public IReadOnlyDictionary<string, string> DataModelBindings { get; }

    /// <summary>
    /// The group or page that this component is part of. NULL for page components
    /// </summary>
    public BaseComponent? Parent { get; internal set; }

    /// <summary>
    /// Extra properties that are not modelled explicitly as a class that inhertits from <see cref="BaseComponent" />
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement> Extra { get; set; }
}
