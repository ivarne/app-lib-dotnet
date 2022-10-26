using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component like object to add Page as a group like object
/// </summary>
[JsonConverter(typeof(PageComponentConverter))]
public class PageComponent : GroupComponent
{
    /// <summary>
    /// Constructor for PageComponent
    /// </summary>
    public PageComponent(BaseComponent baseComponent, List<BaseComponent> children, Dictionary<string, BaseComponent> componentLookup) :
        base(baseComponent, children)
    {
        ComponentLookup = componentLookup;
    }

    /// <summary>
    /// Helper dictionary to find components without traversing childern.
    /// </summary>
    public Dictionary<string, BaseComponent> ComponentLookup { get; }
}
