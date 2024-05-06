using System.Collections.Generic;
using System.IO;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediateMethodDefinition : IntermediateTypedMemberDefinition, IGenerator
{
    public MemoryStream BodyStream { get; }

    public CodeBuilder Body { get; }

    public List<IntermediateArgumentDefinition> Arguments { get; } = new();

    private CSharpCodeGenerator root { get; }

    private List<IntermediateAttributeDefinition> attributes { get; } = new();

    public IntermediateMethodDefinition(CSharpCodeGenerator root, MemberFlags flags, CsharpTypeReference type, string name) : base(flags, type, name)
    {
        Body = CodeBuilder.FromMemoryStream(out var stream);
        BodyStream = stream;
        this.root = root;
    }

    public IntermediateArgumentDefinition Argument(CsharpTypeReference type, string name)
    {
        var argument = new IntermediateArgumentDefinition(root, type, name);

        Arguments.Add(argument);

        return argument;
    }

    public IntermediateMethodDefinition WithArgument(CsharpTypeReference type, string name)
    {
        Argument(type, name);

        return this;
    }

    public void Generate(CodeBuilder builder)
    {
        Body.Flush();
        BodyStream.Flush();
        BodyStream.Position = 0;

        builder
            .Append((b, a) =>
            {
                a.Generate(b);
                b.NewLine();
            }, attributes)
            .Append(AppendMemberFlags)
            .AppendWord(Type.Resolve(root))
            .Append(MemberName)
            .OpenParanthesis()
                .Append((b, arg) => arg.Generate(b), Arguments)
            .CloseParanthesis();

        if ((Flags.HasFlag(MemberFlags.Partial) || Flags.HasFlag(MemberFlags.Interface)) && BodyStream.Capacity == 0)
        {
            builder.AppendLine(";");
            return;
        }

        builder.WithBlock(b => b.Append(BodyStream));
    }

    public IntermediateAttributeDefinition Attribute(CsharpTypeReference attribute)
    {
        var ret = new IntermediateAttributeDefinition(root, attribute);

        attributes.Add(ret);

        return ret;
    }
}
