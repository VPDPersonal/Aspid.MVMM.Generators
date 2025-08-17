using System.Linq;
using Microsoft.CodeAnalysis;
using Aspid.Generator.Helpers;
using System.Collections.Immutable;
using Aspid.MVVM.Generators.ViewModels.Data;
using Aspid.MVVM.Generators.ViewModels.Data.Members;
using static Aspid.MVVM.Generators.Descriptions.Classes;
using BindMode = Aspid.MVVM.Generators.ViewModels.Data.BindMode;

namespace Aspid.MVVM.Generators.ViewModels.Body;

public static class OnPropertyChangedBody
{
    public static void Generate(
        string @namespace,
        in ViewModelData data,
        in DeclarationText declaration,
        in SourceProductionContext context)
    {
        var code = new CodeWriter();

        code.AppendClassBegin(@namespace, declaration)
            .AppendBody(data)
            .AppendClassEnd(@namespace);
        
        context.AddSource(declaration.GetFileName(@namespace, "OnPropertyChanged"), code.GetSourceText()); 
    }

    private static CodeWriter AppendBody(this CodeWriter code, in ViewModelData data)
    {
        var properties = data.BindableProperties;
        
        return code
            .AppendFields(properties)
            .AppendProperties(properties)
            .AppendOnPropertyChangedMethods(properties)
            .AppendLine()
            .AppendSetFieldMethods(properties);
    }

    private static CodeWriter AppendFields(this CodeWriter code, in ImmutableArray<BindableProperty> properties)
    {
        foreach (var property in properties)
        {
            var declaration = property.BindableMemberType.ToFieldDeclarationString();
            if (declaration is null) continue;

            code.AppendMultiline(declaration)
                .AppendLine();
        }

        return code;
    }

    private static CodeWriter AppendProperties(this CodeWriter code, in ImmutableArray<BindableProperty> properties)
    {
        foreach (var property in properties)
        {
            var declaration = property.BindableMemberType.ToPropertyDeclarationString();
            if (declaration is null) continue;
            
            code.AppendMultiline(declaration)
                .AppendLine();
        }

        return code;
    }

    private static CodeWriter AppendOnPropertyChangedMethods(this CodeWriter code, in ImmutableArray<BindableProperty> properties)
    {
        var methodDeclaration = $"protected void OnPropertyChanged([{CallerMemberNameAttribute}] string propertyName = null)";
        
        if (properties.Length is 0)
            return code.AppendLine($"{methodDeclaration} {{ }}");
        
        code.AppendLine($"{methodDeclaration}")
            .BeginBlock()
            .AppendLine("switch (propertyName)")
            .BeginBlock();
        
        foreach (var property in properties)
        {
            if (property.Mode is not (BindMode.OneWay or BindMode.TwoWay)) continue;
            code.AppendLine($"case nameof({property.Name}): On{property.Name}Changed(); break;");
        }
        
        code.EndBlock()
            .EndBlock();
        
        foreach (var property in properties)
        {
            if (property.Mode is not (BindMode.OneWay or BindMode.TwoWay)) continue;
            
            code.AppendLine()
                .AppendMultiline(
                    $$"""
                      private void On{{property.Name}}Changed()
                      {
                          {{property.BindableMemberType.FieldName}}?.Invoke({{property.Name}});
                      }
                      """);
        }

        return code;
    }
    
    private static CodeWriter AppendSetFieldMethods(this CodeWriter code, in ImmutableArray<BindableProperty> properties)
    { 
        code.AppendMultiline(
            $$"""
            private bool TrySetField<T>(ref T field, T value, [{{CallerMemberNameAttribute}}] string propertyName = null)
            {
                if (!{{EqualityComparer}}<T>.Default.Equals(field, value)) return false;
                
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            
            private void SetField<T>(ref T field, T value, [{{CallerMemberNameAttribute}}] string propertyName = null) =>
                TrySetField(ref field, value, propertyName);
            """);
        
        foreach (var property in properties.Where(property => property.Mode is BindMode.OneWay or BindMode.TwoWay))
        {
            var methodName = $"Set{property.Name}Field";
            var tryMethodName = $"TrySet{property.Name}Field";
            var onPropertyChanged = $"On{property.Name}Changed()";
            var propertyType = property.Type.ToDisplayStringGlobal();
                
            code.AppendLine()
                .AppendMultiline(
                    $$"""
                    private bool {{tryMethodName}}(ref {{propertyType}} field, {{propertyType}} value)
                    {
                        if (!{{EqualityComparer}}<{{propertyType}}>.Default.Equals(field, value)) return false;
                        
                        field = value;
                        {{onPropertyChanged}};
                        return true;
                    }

                    private void {{methodName}}(ref {{propertyType}} field, {{propertyType}} value) =>
                        {{tryMethodName}}(ref field, value);
                    """);
        }
        
        return code;
    }
}