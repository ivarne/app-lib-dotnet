using System.Text.Json;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Configuration;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Expressions;

/// <summary>
/// Utility class for collecting all the services from DI that are needed to initialize <see cref="LayoutEvaluatorState" />
/// </summary>
public class LayoutEvaluatorStateInitializer
{
    // Dependency injection properties (set in ctor)
    private readonly IData _data;
    private readonly IAppResources _appResources;
    private readonly FrontEndSettings _frontEndSettings;

    /// <summary>
    /// Constructor with services from dependency injection
    /// </summary>
    public LayoutEvaluatorStateInitializer(IData data, IAppResources appResources, IOptions<FrontEndSettings> frontEndSettings)
    {
        _data = data;
        _appResources = appResources;
        _frontEndSettings = frontEndSettings.Value;
    }

    /// <summary>
    /// Initialize LayoutEvaluatorState with given Instance, data object and layoutSetId
    /// </summary>
    public Task<LayoutEvaluatorState> Init(Instance instance, object data, string? layoutSetId)
    {
        string formLayoutsFileContent = layoutSetId == null ? _appResources.GetLayouts() : _appResources.GetLayoutsForSet(layoutSetId);
        var layouts = JsonSerializer.Deserialize<ComponentModel>(formLayoutsFileContent)!;
        return Task.FromResult(new LayoutEvaluatorState(new DataModel(data), layouts, _frontEndSettings, instance));
    }
}