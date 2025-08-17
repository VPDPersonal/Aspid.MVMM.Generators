using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Aspid.MVVM.Generators.ViewModels.Data;
using Aspid.MVVM.Generators.ViewModels.Extensions;
using Aspid.MVVM.Generators.ViewModels.Data.Members;

namespace Aspid.MVVM.Generators.ViewModels.Factories;

public static class BindablePropertyFactory
{
    private const string SetField = nameof(SetField);
    private const string TrySetField = nameof(TrySetField);
    private const string OnPropertyChanged = nameof(OnPropertyChanged);
    
    public static ImmutableArray<BindableProperty> Create(ClassDeclarationSyntax classDeclaration, params IReadOnlyCollection<IPropertySymbol> propertySymbols)
    {
        var addedProperties = new HashSet<string>();
        var notifyPropertyNames = new HashSet<string>();
        var bindableProperties = new List<BindableProperty>();
        var invocations = classDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        // 1) Add properties that have bind attributes
        foreach (var propertySymbol in propertySymbols)
        {
            var bindMode = propertySymbol.GetBindMode();
            if (bindMode is BindMode.None) continue;
            
            addedProperties.Add(propertySymbol.Name);
            bindableProperties.Add(new BindableProperty(bindMode, propertySymbol));
        }

        // 2) Find property names from OnPropertyChanged/SetField/TrySetField calls in the class
        foreach (var invocation in invocations)
        {
            var expression = invocation.Expression;
            var arguments = invocation.ArgumentList.Arguments;
            
            // OnPropertyChanged(...)
            if (expression
                is IdentifierNameSyntax { Identifier.ValueText: OnPropertyChanged }
                or MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: OnPropertyChanged } })
            {
                if (arguments.Count is 0)
                {
                    // CallerMemberName: if inside a property accessor (set) use that property's name
                    if (TryGetContainingPropertyName(invocation, out var propertyName))
                        notifyPropertyNames.Add(propertyName!);
                }
                else
                {
                    if (TryStringValueFromParameter(arguments[0], out var propertyName)) 
                        notifyPropertyNames.Add(propertyName!);
                }
                
                continue;
            }
            
            // SetField(...), TrySetField(...)
            if (expression
                is IdentifierNameSyntax { Identifier.ValueText: SetField or TrySetField }
                or MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: SetField or TrySetField } })
            {
                if (arguments.Count is 2)
                {
                    // CallerMemberName: if inside a property accessor (set) use that property's name
                    if (TryGetContainingPropertyName(invocation, out var propertyName))
                        notifyPropertyNames.Add(propertyName!);
                }
                else
                {
                    if (TryStringValueFromParameter(arguments[2], out var propertyName))
                        notifyPropertyNames.Add(propertyName!);
                }
            }
        }

        // 3) Map names to symbols and add as TwoWay if not already added by attribute
        foreach (var propertySymbol in propertySymbols)
        {
            var name = propertySymbol.Name;
            if (propertySymbol.IsWriteOnly) continue;
            if (addedProperties.Contains(name)) continue;
            if (!notifyPropertyNames.Contains(name)) continue;
            
            var bindMode = propertySymbol.IsReadOnly ? BindMode.OneWay : BindMode.TwoWay;
            bindableProperties.Add(new BindableProperty(bindMode, propertySymbol));
        }
        
        return bindableProperties.ToImmutableArray();
    }
    
    private static bool TryGetContainingPropertyName(SyntaxNode node, out string? propertyName)
    {
        propertyName = null;
        
        var accessor = node
            .AncestorsAndSelf()
            .OfType<AccessorDeclarationSyntax>()
            .FirstOrDefault(accessor => accessor.Kind() is SyntaxKind.SetAccessorDeclaration);

        if (accessor?.Parent is not AccessorListSyntax accessorList) return false;
        if (accessorList.Parent is not PropertyDeclarationSyntax property) return false;
        
        propertyName = property.Identifier.ValueText;
        return !string.IsNullOrEmpty(propertyName);
    }
    
    private static bool TryStringValueFromParameter(ArgumentSyntax argument, out string? value)
    {
        value = null;
        var expression = argument.Expression;
        
        // "PropertyName"
        if (expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal) 
        {
            value = literal.Token.ValueText;
            return !string.IsNullOrEmpty(value);
        }
        
        // nameof(...)
        if (TryGetNameOfExpression(expression, out var nameofExpr)) 
        {
            value = nameofExpr switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                QualifiedNameSyntax qn => qn.Right.Identifier.ValueText,
                MemberAccessExpressionSyntax { Name: IdentifierNameSyntax id } => id.Identifier.ValueText,
                _ => value
            };
            
            return !string.IsNullOrEmpty(value);
        }

        return false;
        
        static bool TryGetNameOfExpression(ExpressionSyntax argExpression, out ExpressionSyntax? nameOfExpression)
        {
            nameOfExpression = null;

            if (argExpression is not InvocationExpressionSyntax nameofInvocation) return false;
            if (nameofInvocation.ArgumentList.Arguments.Count is not 1) return false;
            if (nameofInvocation.Expression is not IdentifierNameSyntax { Identifier.ValueText: "nameof" } ) return false;

            nameOfExpression = nameofInvocation.ArgumentList.Arguments[0].Expression;
            return true;
        }
    }
}