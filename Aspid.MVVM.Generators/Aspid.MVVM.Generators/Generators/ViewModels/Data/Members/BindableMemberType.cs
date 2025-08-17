using Microsoft.CodeAnalysis;
using Aspid.Generator.Helpers;
using static Aspid.MVVM.Generators.Descriptions.General;
using static Aspid.MVVM.Generators.Descriptions.Classes;
using BindMode = Aspid.MVVM.Generators.ViewModels.Data.BindMode;

namespace Aspid.MVVM.Generators.ViewModels.Data.Members;

public readonly struct BindableMemberType
{
    public readonly string? FieldName;
    public readonly string? PropertyName;
    private readonly string? _type;
    
    private readonly BindMode _mode;
    private readonly string _propertyName;
    private readonly bool _setValueAsDelegate;
    private readonly ITypeSymbol _propertyType;

    public BindableMemberType(
        BindMode mode,
        string propertyName,
        ITypeSymbol propertyType,
        bool setValueAsDelegate = false)
    {
        _mode = mode;
        _propertyName = propertyName;
        _propertyType = propertyType;
        _setValueAsDelegate = setValueAsDelegate;
        
        PropertyName = mode is BindMode.None 
            ? null
            : $"{propertyName}Bindable";
        
        FieldName = mode is BindMode.None or BindMode.OneTime
            ? null
            : SymbolExtensions.GetFieldName(propertyName, "__");
        
        var typeKind = propertyType.TypeKind;
        
        _type = mode switch
        {
            BindMode.OneWay => typeKind switch
            {
                TypeKind.Enum => OneWayEnumBindableMember,
                TypeKind.Struct => OneWayStructBindableMember,
                _ => OneWayBindableMember
            },
            BindMode.TwoWay => typeKind switch
            {
                TypeKind.Enum => TwoWayEnumBindableMember,
                TypeKind.Struct => TwoWayStructBindableMember,
                _ => TwoWayBindableMember
            },
            BindMode.OneTime => typeKind switch
            {
                TypeKind.Enum => OneTimeEnumBindableMember,
                TypeKind.Struct => OneTimeStructBindableMember,
                _ => OneTimeBindableMember
            },
            BindMode.OneWayToSource => typeKind switch
            {
                TypeKind.Enum => OneWayToSourceEnumBindableMember,
                TypeKind.Struct => OneWayToSourceStructBindableMember,
                _ => OneWayToSourceBindableMember
            },
            _ => null
        };
    }

    public string? ToFieldDeclarationString()
    {
        if (_mode is BindMode.None or BindMode.OneTime) return null;
        
        return _type is null
            ? null
            : $"""
              [{EditorBrowsableAttribute}({EditorBrowsableState}.Never)]
              {GeneratedCodeViewModelAttribute}
              private {_type}<{_propertyType}> {FieldName};
              """;
    }

    public string? ToPropertyDeclarationString()
    {
        var setValue = _setValueAsDelegate
            ? $"(value) => {_propertyName} = value"
            : $"Set{_propertyName}";
        
        var instantiate = _mode switch
        {
            BindMode.OneWay => $"{FieldName} ??= new({_propertyName})",
            BindMode.TwoWay => $"{FieldName} ??= new({_propertyName}, {setValue})",
            BindMode.OneTime => $"{_type}<{_propertyType}>.Get({_propertyName})",
            BindMode.OneWayToSource => $"{FieldName} ??= new({setValue})",
            _ => string.Empty
        };
        
        return $"""
                {GeneratedCodeViewModelAttribute}
                public {_type}<{_propertyType}> {PropertyName} => 
                    {instantiate};
                """;
    }
}