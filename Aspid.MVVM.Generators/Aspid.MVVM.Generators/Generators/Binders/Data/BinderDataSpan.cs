using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Aspid.MVVM.Generators.Binders.Data;

public readonly ref struct BinderDataSpan(BinderData data)
{
    public readonly TypeDeclarationSyntax Declaration = data.Declaration;
    public readonly bool HasBinderLogInBaseType = data.HasBinderLogInBaseType;
    public readonly ReadOnlySpan<IMethodSymbol> Methods = data.Methods.AsSpan();
}