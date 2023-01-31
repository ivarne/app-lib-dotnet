using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

/// <summary>
/// Interface to customize PDF formatting.
/// </summary>
public interface IPdfFormatter
{
    /// <summary>
    /// Method to format the PDF dynamically
    /// </summary>
    Task<LayoutSettings> FormatPdf(LayoutSettings layoutSettings, object data);

    /// <summary>
    /// If you need the instance, implement this function and it will always be called before <see cref="FormatPdf" />
    /// </summary>
    /// <remarks>
    /// If you use this, you'll probably need to registrer your formatter as Trancient in DI
    /// </remarks>
    void SetInstance(Instance instance)
    {
    }
}

