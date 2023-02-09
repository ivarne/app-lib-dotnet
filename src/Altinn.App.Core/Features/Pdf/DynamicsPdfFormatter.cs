using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Pdf;

/// <summary>
/// Custom formatter that reads the `hidden` properties of components and pages
/// to determine if they should be hidden in PDF as well.
/// </summary>
/// <remarks>
/// Add this to dependency injection in order to run dynamics for hiding pages and comonents.
/// <code>
/// services.AddTrancient&lt;IPdfFormatter, DynamicsPdfFormatter&gt;();
/// </code>
/// </remarks>
public class DynamicsPdfFormatter : IPdfFormatter
{
    private readonly LayoutEvaluatorStateInitializer _layoutStateInit;

    /// <summary>
    /// Constructor for <see cref="DynamicsPdfFormatter" />
    /// </summary>
    public DynamicsPdfFormatter(LayoutEvaluatorStateInitializer layoutStateInit)
    {
        _layoutStateInit = layoutStateInit;
    }

    /// <inheritdoc />
    public async Task<LayoutSettings> FormatPdf(LayoutSettings layoutSettings, object data, Instance instance)
    {
        layoutSettings.Pages ??= new();
        layoutSettings.Pages.ExcludeFromPdf ??= new();
        layoutSettings.Components ??= new();
        layoutSettings.Components.ExcludeFromPdf ??= new();

        var state = await _layoutStateInit.Init(instance, data, layoutSetId:null);
        foreach(var pageContext in state.GetComponentContexts())
        {
            var pageHidden = ExpressionEvaluator.EvaluateBooleanExpression(state, pageContext, "hidden", false);
            if(pageHidden)
            {
                layoutSettings.Pages.ExcludeFromPdf.Add(pageContext.Component.Id);
            }
            else
            {
                //TODO: figure out how pdf reacts to groups one level down.
                foreach(var componentContext in pageContext.ChildContexts)
                {
                    var componentHidden = ExpressionEvaluator.EvaluateBooleanExpression(state, componentContext, "hidden", false);
                    if(componentHidden)
                    {
                        layoutSettings.Components.ExcludeFromPdf.Add(componentContext.Component.Id);
                    }
                }
            }
        }
        return layoutSettings;
    }
}