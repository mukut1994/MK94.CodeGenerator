using System;
using System.Collections.Generic;
using System.Linq;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediateTypeDefinition : IntermediateTypedMemberDefinition, IGenerator
{
    private CSharpCodeGenerator root { get; }

    public Dictionary<string, IntermediateTypeDefinition> Types = new();

    public Dictionary<string, IntermediatePropertyDefinition> Properties = new();

    public Dictionary<string, IntermediateMethodDefinition> Methods = new();
    private List<IntermediateAttributeDefinition> attributes { get; } = new();

    private List<IntermediateConstructorDefinition> constructors { get; } = new();

    private List<CsharpTypeReference> InheritsFrom { get; } = new();

    private DefinitionType DefinitionType { get; set; }

    private bool PrimaryConstructor { get; set; }

    public IntermediateTypeDefinition(CSharpCodeGenerator root, MemberFlags flags, string name, CsharpTypeReference type) : base(flags, type, name)
    {
        this.root = root;
        DefinitionType |= DefinitionType.Default;
    }

    public IntermediateConstructorDefinition Constructor(MemberFlags flags)
    {
        var definition = new IntermediateConstructorDefinition(root, flags, Name);

        constructors.Add(definition);

        return definition;
    }

    public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
    {
        var ret = new IntermediateAttributeDefinition(root, attribute);

        attributes.Add(ret);

        return ret;
    }

    public IntermediatePropertyDefinition Property(MemberFlags flags, CsharpTypeReference type, string name)
    {
        var definition = Properties.GetOrAdd(name, () => new(root, flags, type, name));

        return definition;
    }

    public IntermediateMethodDefinition Method(MemberFlags flags, CsharpTypeReference returnType, string name)
    {
        var definition = Methods.GetOrAdd(name, () => new(root, flags, returnType, name));

        definition.Flags |= flags;

        // TODO throw exception if return types don't match

        return definition;
    }

    public IntermediateTypeDefinition Type(string name, MemberFlags flags, CsharpTypeReference? type)
    {
        flags |= MemberFlags.Type;

        var definition = Types.GetOrAdd(name, () => new IntermediateTypeDefinition(root, flags: flags, name: name, type: type ?? CsharpTypeReference.ToRaw(name)));

        return definition;
    }

    public IntermediateTypeDefinition WithTypeAsClass()
    {
        if (DefinitionType != DefinitionType.Default)
            throw new InvalidOperationException("The type has already been defined.");

        DefinitionType |= DefinitionType.Class;

        return this;
    }

    public IntermediateTypeDefinition WithTypeAsRecord()
    {
        if (DefinitionType.HasFlag(DefinitionType.Class) || DefinitionType.HasFlag(DefinitionType.Interface))
            throw new InvalidOperationException("The type has already been defined.");

        DefinitionType |= DefinitionType.Record;

        return this;
    }

    public IntermediateTypeDefinition WithTypeAsStruct()
    {
        if (DefinitionType.HasFlag(DefinitionType.Class) || DefinitionType.HasFlag(DefinitionType.Interface))
            throw new InvalidOperationException("The type has already been defined.");

        DefinitionType |= DefinitionType.Struct;

        return this;
    }

    public IntermediateTypeDefinition WithTypeAsInterface()
    {
        if (DefinitionType.HasFlag(DefinitionType.Class) || DefinitionType.HasFlag(DefinitionType.Struct) || DefinitionType.HasFlag(DefinitionType.Record))
            throw new InvalidOperationException("The type cannot be defined as an interface.");

        DefinitionType = DefinitionType.Interface;

        return this;
    }

    public IntermediateTypeDefinition WithInheritsFrom(CsharpTypeReference inheritsFrom)
    {
        InheritsFrom.Add(inheritsFrom);

        return this;
    }

    public IntermediateTypeDefinition WithPrimaryConstructor()
    {
        PrimaryConstructor = true;

        return this;
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
            .Append(AppendDefinitionFlags)
            .Append(base.Type.Resolve(root))
            .Append(AppendPrimaryConstructor)
            .Append(AppendInheritsFrom)
            .OpenBlock();

        if (constructors.Any())
        {
            builder.Append((b, p) => p.Generate(b), constructors);
        }

        if (Types.Any())
        {
            if (constructors.Any())
                builder.NewLine();

            builder.Append((b, p) => p.Value.Generate(b), Types);
        }

        if (!PrimaryConstructor && Properties.Any())
        {
            if (constructors.Any() || Types.Any())
                builder.NewLine();

            builder.Append((b, p) => p.Value.Generate(b), Properties);
        }

        if (Methods.Any())
        {
            if (constructors.Any() || Types.Any() || Properties.Any())
                builder.NewLine();

            builder.Append((b, p) => p.Value.Generate(b), Methods);
        }

        builder.CloseBlock();
    }

    private void AppendDefinitionFlags(CodeBuilder builder)
    {
        if (DefinitionType == DefinitionType.Default || DefinitionType.HasFlag(DefinitionType.Class))
            builder.AppendWord("class");

        if (DefinitionType.HasFlag(DefinitionType.Record))
            builder.AppendWord("record");

        if (DefinitionType.HasFlag(DefinitionType.Struct))
            builder.AppendWord("struct");

        if (DefinitionType.HasFlag(DefinitionType.Interface))
            builder.AppendWord("interface");
    }

    private void AppendInheritsFrom(CodeBuilder builder)
    {
        if (InheritsFrom.Count == 0)
            return;

        builder.AppendWord(":");

        for (var i = 0; i < InheritsFrom.Count; i++)
        {
            if (i == InheritsFrom.Count - 1)
            {
                builder.AppendWord(InheritsFrom[i].Resolve(root));
            }
            else
            {
                builder.AppendWord($"{InheritsFrom[i]}, ");
            }
        }
    }

    private void AppendPrimaryConstructor(CodeBuilder builder)
    {
        if (!PrimaryConstructor)
            return;

        builder.OpenParanthesis();

        foreach (var property in Properties)
        {
            if (property.Value.Flags.HasFlag(MemberFlags.Public))
            {
                builder.AppendWord(property.Value.Type.Resolve(root));
                builder.AppendWord(property.Key);
            }
        }

        builder.CloseParanthesis();

    }
}