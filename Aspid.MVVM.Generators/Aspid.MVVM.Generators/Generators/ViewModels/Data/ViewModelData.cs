using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Aspid.MVVM.Generators.ViewModels.Data.Members;
using Aspid.MVVM.Generators.ViewModels.Data.Members.Collections;

namespace Aspid.MVVM.Generators.ViewModels.Data;

public readonly struct ViewModelData(
    Inheritor inheritor,
    INamedTypeSymbol symbol,
    ClassDeclarationSyntax declaration,
    ImmutableArray<BindableMember> members,
    ImmutableArray<IdLengthMemberGroup> idGroups,
    ImmutableArray<BindableProperty> bindableProperties,
    Dictionary<string, CustomViewModelInterface> customViewModelInterfaces)
{
    public readonly string Name = symbol.Name;
    
    public readonly Inheritor Inheritor = inheritor;
    public readonly INamedTypeSymbol Symbol = symbol;
    public readonly ClassDeclarationSyntax Declaration = declaration;

    public readonly ImmutableArray<BindableMember> Members = members;
    public readonly ImmutableArray<IdLengthMemberGroup> IdGroups = idGroups;
    public readonly ImmutableArray<BindableProperty> BindableProperties = bindableProperties;
    public readonly Dictionary<string, CustomViewModelInterface> CustomViewModelInterfaces = customViewModelInterfaces;
}