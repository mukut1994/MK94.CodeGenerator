using System;
using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediatePropertyDefinition : IntermediateTypedMemberDefinition, IGenerator
{
    private CSharpCodeGenerator root { get; }

    private List<IntermediateAttributeDefinition> attributes { get; } = new();

    public PropertyType PropertyType { get; set; }

    public string NewExpression { get; set; } = string.Empty;

    public IntermediatePropertyDefinition(CSharpCodeGenerator root, MemberFlags flags, CsharpTypeReference type, string name) : base(flags, type, name)
    {
        PropertyType |= PropertyType.Default;

        this.root = root;
    }

    public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
    {
        var ret = new IntermediateAttributeDefinition(root, attribute);

        attributes.Add(ret);

        return ret;
    }

    public void Generate(CodeBuilder builder)
    {
        builder
            .Append((b, a) =>
            {
                a.Generate(b);
                b.NewLine();
            }, attributes)
            .Append(AppendMemberFlags)
            .AppendWord(Type.Resolve(root))
            .Append(MemberName)
            .Append(AppendPropertyType)
            .Append(NewExpression)
            .AppendLine(string.Empty);
    }

    public void GetRequiredReferences(HashSet<CsharpTypeReference> refs)
    {
        refs.Add(Type);
    }

    public void AppendPropertyType(CodeBuilder builder)
    {
        builder.AppendWord("{");

        if (PropertyType == 0 || PropertyType.HasFlag(PropertyType.Getter))
            builder.AppendWord("get;");

        if (PropertyType == 0 || PropertyType.HasFlag(PropertyType.Setter))
            builder.AppendWord("set;");

        if (PropertyType.HasFlag(PropertyType.Init))
            builder.AppendWord("init;");

        builder.AppendWord("}");
    }

    public IntermediatePropertyDefinition WithGetter()
    {
        PropertyType |= PropertyType.Getter;

        return this;
    }

    public IntermediatePropertyDefinition WithSetter()
    {
        if (PropertyType.HasFlag(PropertyType.Init))
            throw new InvalidOperationException("Property has `init` set up already.");

        PropertyType |= PropertyType.Setter;

        return this;
    }

    public IntermediatePropertyDefinition WithInit()
    {
        if (PropertyType.HasFlag(PropertyType.Setter))
            throw new InvalidOperationException("Property has `set` set up already.");

        PropertyType |= PropertyType.Init;

        return this;
    }

    public IntermediatePropertyDefinition WithDefaultNew() => WithDefaultExpression("= new();");

    public IntermediatePropertyDefinition WithDefaultExpression(string newExpression)
    {
        NewExpression = newExpression;

        return this;
    }
}
