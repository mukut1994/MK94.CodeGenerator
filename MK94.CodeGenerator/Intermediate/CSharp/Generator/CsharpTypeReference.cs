using System;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public abstract record CsharpTypeReference
{
    public static CsharpTypeReference ToRaw(string type)
    {
        return new NamedTypeReference(type);
    }

    public static CsharpTypeReference ToVoid() => ToRaw("void");

    public static CsharpTypeReference ToType<T>()
    {
        return new NamedTypeReference(CodeGenerator.Generator.CSharpHelper.CSharpName(typeof(T)));
    }

    public static CsharpTypeReference ToType(Type t)
    {
        return new NamedTypeReference(CodeGenerator.Generator.CSharpHelper.CSharpName(t));
    }

    public abstract string Resolve(CSharpCodeGenerator root);
}

internal record NamedTypeReference : CsharpTypeReference
{
    public string Name { get; private init; }

    public NamedTypeReference(string name)
    {
        Name = name;
    }

    public override string Resolve(CSharpCodeGenerator root)
    {
        return Name;
    }
}
