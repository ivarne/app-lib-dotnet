using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Defines the type of an external reference
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CorrespondenceReferenceType
{
    /// <summary>
    /// A generic reference
    /// </summary>
    Generic,

    /// <summary>
    /// A reference to an Altinn App Instance
    /// </summary>
    AltinnAppInstance,

    /// <summary>
    /// A reference to an Altinn Broker File Transfer
    /// </summary>
    AltinnBrokerFileTransfer,

    /// <summary>
    /// A reference to a Dialogporten Dialog ID
    /// </summary>
    DialogportenDialogId,

    /// <summary>
    /// A reference to a Dialogporten Process ID
    /// </summary>
    DialogportenProcessId,
}
