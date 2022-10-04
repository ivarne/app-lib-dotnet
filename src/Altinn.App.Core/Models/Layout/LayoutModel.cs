using System.Text.Json.Serialization;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models.Expression;
using Altinn.App.Core.Models.Layout.Components;

namespace Altinn.App.Core.Models.Layout;

/// <summary>
/// Class for 
/// </summary>
[JsonConverter(typeof(LayoutModelConverter))]
public class LayoutModel
{
    /// <summary>
    /// Dictionary to hold the different pages that are part of this LayoutModel
    /// </summary>
    public Dictionary<string, PageComponent> Pages { get; init; } = new Dictionary<string, PageComponent>();

    /// <summary>
    /// Get a specific component on a specifc page.
    /// </summary>
    public BaseComponent GetComponent(string pageName, string componentId)
    {
        if (!Pages.TryGetValue(pageName, out var page))
        {
            throw new Exception($"Unknown page name {pageName}");
        }

        if (!page.ComponentLookup.TryGetValue(componentId, out var component))
        {
            throw new Exception($"Unknown component {componentId} on {pageName}");
        }
        return component;
    }

    /// <summary>
    /// Get all components by recursivly walking all the pages.
    /// </summary>
    public IEnumerable<BaseComponent> GetComponents()
    {
        // Use a stack in order to implement a depth first search
        var nodes = new Stack<BaseComponent>(Pages.Values);
        while (nodes.Any())
        {
            var node = nodes.Pop();
            yield return node;
            if(node is GroupComponent groupNode)
            foreach (var n in groupNode.Children) nodes.Push(n);
        }
    }

    /// <summary>
    /// Get data from the `simpleBinding` property of a component on the same page
    /// </summary>
    public object? GetComponentData(string componentId, ComponentContext context, IDataModelAccessor dataModel)
    {
        if (context.Component is GroupComponent)
        {
            //TODO before release
            throw new NotImplementedException("Component lookup for components in groups not implemented");
        }

        var component = GetComponent(context.Component.Page, componentId);

        if (!component.DataModelBindings.TryGetValue("simpleBinding", out var binding))
        {
            throw new Exception("component lookup requires the target component ");
        }
        return dataModel.GetModelData(binding);
    }
}
