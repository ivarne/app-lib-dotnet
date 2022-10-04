using System.Reflection;
using System.Text.RegularExpressions;

namespace Altinn.App.Core.Helpers.DataModel;

/// <summary>
/// Get data fields from a model, using string keys (like "Bedrifter[1].Ansatte[1].Alder")
/// </summary>
public class DataModel : IDataModelAccessor
{
    private readonly object _serviceModel;

    /// <summary>
    /// Constructor that wraps a PCOC data model, and gives extra tool for working with the data
    /// </summary>
    public DataModel(object serviceModel)
    {
        _serviceModel = serviceModel;
    }

    /// <inheritdoc />
    public object? GetModelData(string key, ReadOnlySpan<int> indicies, bool throwOnError = false)
    {
        return GetModelDataRecursive(key.Split('.'), 0, _serviceModel, indicies, throwOnError);
    }

    /// <inheritdoc />
    public int? GetModelDataCount(string key, ReadOnlySpan<int> indicies = default, bool throwOnError = false)
    {
        if (GetModelDataRecursive(key.Split('.'), 0, _serviceModel, indicies, throwOnError) is System.Collections.IEnumerable childEnum)
        {
            int retCount = 0;
            foreach (var _ in childEnum)
            {
                retCount++;
            }
            return retCount;
        }

        return null;
    }

    private object? GetModelDataRecursive(string[] keys, int index, object currentModel, ReadOnlySpan<int> indicies, bool throwOnError)
    {
        if (index == keys.Length)
        {
            return currentModel;
        }

        var (key, groupIndex) = ParseKeyPart(keys[index]);
        var prop = currentModel.GetType().GetProperties().FirstOrDefault(p => IsPropertyWithJsonName(p, key));
        if (prop is null)
        {
            //throw new Exception($"Unknown model property {key} in {string.Join('.', keys)}");
            return null;
        }

        var childModel = prop.GetValue(currentModel);
        if (childModel is null)
        {
            return null;
        }

        // Strings are enumerable in C#
        // Other enumerable types is treated as an collection
        if (childModel is not string && childModel is System.Collections.IEnumerable childModelList)
        {
            if (groupIndex is null)
            {
                if (index == keys.Length - 1)
                {
                    return childModelList;
                }

                if (indicies.Length == 0)
                {
                    return null; // Error index for collection not specified
                }

                groupIndex = indicies[0];
            }
            else
            {
                indicies = default; //when you use a literal index, the context indecies are not to be used later.
            }

            // Return the element with index = groupIndex (could not find anohter way to get the n'th element in non generic enumerable)
            foreach (var arrayElement in childModelList)
            {
                if (groupIndex-- < 1)
                {
                    return GetModelDataRecursive(keys, index + 1, arrayElement, indicies.Length > 0 ? indicies.Slice(1) : indicies, throwOnError);
                }
            }
        }

        return GetModelDataRecursive(keys, index + 1, childModel, indicies, throwOnError);
    }

    private static Regex KeyPartRegex = new Regex(@"^(\w+)\[(\d+)\]?$");
    internal static (string key, int? index) ParseKeyPart(string keypart)
    {
        if (keypart.Last() != ']')
        {
            return (keypart, null);
        }
        var match = KeyPartRegex.Match(keypart);
        return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));

    }

    private static void AddIndiciesRecursive(List<string> ret, Type currentModelType, ReadOnlySpan<string> keys, string fullKey, ReadOnlySpan<int> indicies, bool throwOnError)
    {
        if (keys.Length == 0)
        {
            return;
        }
        var (key, groupIndex) = ParseKeyPart(keys[0]);
        var prop = currentModelType.GetProperties().FirstOrDefault(p => IsPropertyWithJsonName(p, key));
        if (prop is null)
        {
            throw new Exception($"Unknown model property {key} in {fullKey}");
        }

        var type = prop.PropertyType;
        if (type != typeof(string) && type.IsAssignableTo(typeof(System.Collections.IEnumerable)))
        {

            if (groupIndex is null)
            {
                ret.Add($"{key}[{indicies[0]}]");
            }
            else
            {
                ret.Add($"{key}[{groupIndex}]");
                indicies = default;
            }

            AddIndiciesRecursive(ret, type, keys.Slice(1), fullKey, indicies.Slice(1), throwOnError);
            return;
        }

        if (groupIndex is not null)
        {
            throw new Exception("Index on non indexable property");
        }
    }

    /// <inheritdoc />
    public string AddIndicies(string key, ReadOnlySpan<int> indicies, bool throwOnError = false)
    {
        if (indicies.Length == 0)
        {
            return key;
        }

        var ret = new List<string>();
        AddIndiciesRecursive(ret, this._serviceModel.GetType(), key.Split('.'), key, indicies, throwOnError);
        return string.Join('.', ret);
    }

    private static bool IsPropertyWithJsonName(PropertyInfo propertyInfo, string key)
    {
        var ca = propertyInfo.CustomAttributes;
        var system_text_json_attribute = (ca.FirstOrDefault(attr => attr.AttributeType.FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute")?.ConstructorArguments.FirstOrDefault().Value as string);
        if (system_text_json_attribute is not null)
        {
            return system_text_json_attribute == key;
        }

        var newtonsoft_json_attribute = (ca.FirstOrDefault(attr => attr.AttributeType.FullName == "Newtonsoft.Json.JsonPropertyAttribute")?.ConstructorArguments.FirstOrDefault().Value as string);
        if (newtonsoft_json_attribute is not null)
        {
            return newtonsoft_json_attribute == key;
        }

        // Fallback to property name if all attributes could not be found
        var keyName = propertyInfo.Name;
        return keyName == key;
    }

    /// <inheritdoc />
    public void RemoveField(string key, bool throwOnError = false)
    {
        var keys_split = key.Split('.');
        var keys = keys_split[0..^1];
        var (lastKey, lastGroupIndex) = ParseKeyPart(keys_split[^1]);

        if (lastGroupIndex is not null)
        {
            // TODO: Consider implementing. Would be required for rowHidden on groups
            throw new NotImplementedException($"Deleting elements in List is not implemented {key}");
        }

        var containingObject = GetModelDataRecursive(keys, 0, _serviceModel, default, throwOnError);
        if (containingObject is null)
        {
            // Already empty field
            return;
        }

        if (containingObject is System.Collections.IEnumerable)
        {
            throw new NotImplementedException($"Tried to remove field {key}, ended in an enumerable");
        }


        var property = containingObject.GetType().GetProperties().FirstOrDefault(p => IsPropertyWithJsonName(p, lastKey));
        if (property is null)
        {
            if(throwOnError)
            {
                throw new Exception($"{key} can't be deleted, because {lastKey} is not a valid property");
            }

            return;
        }

        var nullValue = property.PropertyType.GetTypeInfo().IsValueType ? Activator.CreateInstance(property.PropertyType) : null;

        property.SetValue(containingObject, nullValue);
    }
}