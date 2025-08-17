using Microsoft.CodeAnalysis;
using Aspid.MVVM.Generators.ViewModels.Data;

namespace Aspid.MVVM.Generators.ViewModels.Data.Members;

public readonly struct BindableProperty(
    BindMode mode,
    IPropertySymbol symbol)
{
    public readonly IPropertySymbol Symbol = symbol;
    
    public readonly BindMode Mode = mode;
    public readonly string Name = symbol.Name;
    public readonly ITypeSymbol Type = symbol.Type;
    public readonly BindableMemberType BindableMemberType = new(mode, symbol.Name, symbol.Type, true);
}