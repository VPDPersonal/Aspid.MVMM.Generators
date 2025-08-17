using Microsoft.CodeAnalysis;
using Aspid.Generator.Helpers;
using Aspid.MVVM.Generators.Descriptions;
using Aspid.MVVM.Generators.ViewModels.Data;

namespace Aspid.MVVM.Generators.ViewModels.Extensions;

public static class SymbolExtensions
{
    public static BindMode GetBindMode(this ISymbol member)
    {
        if (member.HasAnyAttribute(out var attribute, Classes.BindAttribute, Classes.OneWayBindAttribute, 
                Classes.TwoWayBindAttribute, Classes.OneTimeBindAttribute, Classes.OneWayToSourceBindAttribute))
        {
            var attributeName = attribute!.AttributeClass!.ToDisplayString();

            if (attributeName == Classes.BindAttribute.FullName)
            {
                if (attribute!.ConstructorArguments.Length is 0)
                {
                    if (member is IFieldSymbol field)
                    {
                        if (field.IsReadOnly || field.IsConst) return BindMode.OneTime;
                    }
                    else if (member is IPropertySymbol property)
                    {
                        if (property.IsReadOnly) return BindMode.OneTime;
                        if (property.IsWriteOnly) return BindMode.OneWayToSource;
                    }

                    return BindMode.TwoWay;
                }

                return Determine((BindMode)(int)attribute!.ConstructorArguments[0].Value!);
            }
            
            if (attributeName == Classes.OneWayBindAttribute.FullName)
                return Determine(BindMode.OneWay);
            
            if (attributeName == Classes.TwoWayBindAttribute.FullName)
                return Determine(BindMode.TwoWay);
            
            if (attributeName == Classes.OneTimeBindAttribute.FullName)
                return Determine(BindMode.OneTime);
            
            if (attributeName == Classes.OneWayToSourceBindAttribute.FullName)
                return Determine(BindMode.OneWayToSource);
        }
        
        return BindMode.None;

        BindMode Determine(BindMode current)
        {
            switch (member)
            {
                case IFieldSymbol field:
                    {
                        if (field.IsReadOnly && current is not BindMode.OneTime) return BindMode.None;
                        break;
                    }
            }

            return current;
        }
    }
}