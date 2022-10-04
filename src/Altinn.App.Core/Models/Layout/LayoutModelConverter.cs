using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Expression;

namespace Altinn.App.Core.Models.Layout;
/// <summary>
/// Custom converter for parsing Layout files in json format to <see cref="LayoutModel" />
/// </summary>
/// <remarks>
/// The layout files in json format contains lots of polymorphism witch is hard for the
/// standard json parser to convert to an object graph. Using <see cref="Utf8JsonReader"/>
/// directly I can convert to a more suitable C# representation directly
/// </remarks>
public class LayoutModelConverter : JsonConverter<LayoutModel>
{

    /// <inheritdoc />
    public override LayoutModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        var componentModel = new LayoutModel();
        // Read dictionary of pages
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }
            var pageName = reader.GetString()!;
            reader.Read();

            componentModel.Pages[pageName] = ReadPage(ref reader, pageName, options);
        }



        return componentModel;
    }

    private PageComponent ReadPage(ref Utf8JsonReader reader, string pageName, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        PageComponent? page = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName == "data")
            {
                page = ReadData(ref reader, pageName, options);
            }
            else
            {
                // Ignore other properties than "data"
                reader.Skip();
            }
        }
        if (page is null)
        {
            throw new JsonException("Missing property \"data\" on layout page");
        }
        return page;
    }

    private PageComponent ReadData(ref Utf8JsonReader reader, string pageName, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var components = new List<BaseComponent>();
        var componentLookup = new Dictionary<string, BaseComponent>();

        // Hidden is the only property that cascades.
        LayoutExpression? hidden = null;

        // extra properties that are not stored in a specific class.
        Dictionary<string, JsonElement> extra = new();


        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            switch (propertyName.ToLowerInvariant())
            {
                case "layout":
                    ReadLayout(ref reader, components, componentLookup, options);
                    break;
                case "hidden":
                    hidden = JsonSerializer.Deserialize<LayoutExpression>(ref reader, options);
                    break;
                default:
                    // read extra properties
                    extra[propertyName] = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        return new PageComponent(pageName, components, componentLookup, hidden, extra);
    }

    private void ReadLayout(ref Utf8JsonReader reader, List<BaseComponent> components, Dictionary<string, BaseComponent> componentLookup, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var component = ReadComponent(ref reader, options)!;
            // Add new component to both collections
            components.Add(component);
            AddChildrenToLookup(component, componentLookup);
        }
    }

    private static void AddChildrenToLookup(BaseComponent component, Dictionary<string, BaseComponent> componentLookup)
    {
        if (componentLookup.ContainsKey(component.Id))
        {
            throw new JsonException($"Duplicate key \"{component.Id}\" detected on page \"{component.Page}\"");
        }
        componentLookup[component.Id] = component;
        if (component is GroupComponent groupComponent)
        {
            foreach (var child in groupComponent.Children)
            {
                AddChildrenToLookup(child, componentLookup);
            }
        }
    }

    private BaseComponent ReadComponent(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        string? id = null;
        string? type = null;
        Dictionary<string, string>? dataModelBindings = null;
        LayoutExpression? hidden = null;
        LayoutExpression? required = null;
        // Custom properities for group
        List<string>? childIds = null;
        int maxCount = 1; // > 1 is repeating, but might not be specified for non-repeating groups
        // Custom properties for Summary
        string? componentRef = null;
        string? pageRef = null;
        // Custom properties for components with optionId or literal options
        string? optionId = null;
        List<AppOption>? literalOptions = null;
        bool secure = false;

        // extra properties that are not stored in a specific class.
        Dictionary<string, JsonElement> extra = new();



        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); // Not possiblie?
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            switch (propertyName.ToLowerInvariant())
            {
                case "id":
                    id = reader.GetString();
                    break;
                case "type":
                    type = reader.GetString();
                    break;
                case "datamodelbindings":
                    dataModelBindings = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                    break;
                // case "textresourcebindings":
                //     break;
                case "children":
                    childIds = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                    break;
                case "maxcount":
                    maxCount = reader.GetInt32();
                    break;
                case "hidden":
                    hidden = JsonSerializer.Deserialize<LayoutExpression>(ref reader, options);
                    break;
                case "required":
                    required = JsonSerializer.Deserialize<LayoutExpression>(ref reader, options);
                    break;
                case "componentref":
                    componentRef = reader.GetString();
                    break;
                case "pageref":
                    pageRef = reader.GetString();
                    break;
                case "optionid":
                    optionId = reader.GetString();
                    break;
                case "options":
                    literalOptions = JsonSerializer.Deserialize<List<AppOption>>(ref reader, options);
                    break;
                case "secure":
                    secure = reader.TokenType == JsonTokenType.True;
                    break;
                default:
                    extra[propertyName] = JsonElement.ParseValue(ref reader);
                    break;
            }
        }
        if (id is null)
        {
            throw new JsonException("\"id\" property of component should not be null");
        }
        if (type is null)
        {
            throw new JsonException("\"type\" property of component should not be null");
        }

        switch (type.ToLowerInvariant())
        {
            case "group":
                if (childIds is null)
                {
                    throw new JsonException("Component with \"type\": \"Group\" requires a \"children\" property");
                }
                var children = ReadChildren(ref reader, id, childIds, options);
                if (maxCount > 1)
                {
                    if (!(dataModelBindings?.ContainsKey("group") ?? false))
                    {
                        throw new JsonException($"A group id:\"{id}\" with maxCount: {maxCount} does not have a \"group\" dataModelBinding");
                    }

                    return new RepeatingGroupComponent(id, type, dataModelBindings, children, maxCount, hidden, required, extra);
                }
                else
                {
                    return new GroupComponent(id, type, dataModelBindings, children, hidden, required, extra);
                }
            case "summary":
                if (componentRef is null || pageRef is null)
                {
                    throw new JsonException("Component with \"type\": \"Summary\" requires \"componentRef\" and \"pageRef\" properties");
                }

                return new SummaryComponent(id, type, hidden, componentRef, pageRef, extra);
            case "checkboxes":
            case "radiobuttons":
            case "dropdown":
                if (optionId is null && literalOptions is null)
                {
                    throw new JsonException("\"optionId\" or \"options\" is required on checkboxes, radiobuttons and dropdowns");
                }
                if (optionId is null || literalOptions is null)
                {
                    throw new JsonException("\"optionId\" and \"options\" can't both be specified");
                }
                if (literalOptions is not null && secure)
                {
                    throw new JsonException("\"secure\": true is invalid for components with literal \"options\"");
                }

                return new OptionsComponent(id, type, hidden, optionId, literalOptions, secure, extra);
        }

        // Most compoents are handled as BaseComponent
        return new BaseComponent(id, type, dataModelBindings, hidden, required, extra);
    }

    private List<BaseComponent> ReadChildren(ref Utf8JsonReader reader, string parentId, List<string> childIds, JsonSerializerOptions options)
    {
        var ret = new List<BaseComponent>();
        foreach (var childId in childIds)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid Group component \"{parentId}\". No components found after group component");
            }
            var component = ReadComponent(ref reader, options)!;
            if (component.Id != childId)
            {
                throw new JsonException($"Invalid Group component \"{parentId}\". Found \"{component.Id}\" instead of \"{childId}\"");
            }
            ret.Add(component);
        }
        return ret;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LayoutModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}