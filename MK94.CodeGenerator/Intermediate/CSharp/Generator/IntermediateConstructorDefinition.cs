using System.Collections.Generic;
using System.IO;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediateConstructorDefinition : IntermediateMemberDefinition, IGenerator
{
    public MemoryStream BodyStream { get; }

    public CodeBuilder Body { get; }

    public List<IntermediateArgumentDefinition> Arguments { get; } = new();

    private CSharpCodeGenerator root { get; }

    private string? baseConstructorCall { get; set; }

    public IntermediateConstructorDefinition(CSharpCodeGenerator root, MemberFlags flags, string name) : base(flags, name)
    {
        Body = CodeBuilder.FromMemoryStream(out var stream);
        BodyStream = stream;
        this.root = root;
    }

    public IntermediateArgumentDefinition WithArgument(CsharpTypeReference type, string name)
    {
        var argument = new IntermediateArgumentDefinition(root, type, name);

        Arguments.Add(argument);

        return argument;
    }

    public IntermediateConstructorDefinition WithBaseConstructorCall(string baseConstructorCall)
    {
        this.baseConstructorCall = baseConstructorCall;

        return this;
    }

    public void Generate(CodeBuilder builder)
    {
        Body.Flush();
        BodyStream.Flush();
        BodyStream.Position = 0;

        builder
            .Append(AppendMemberFlags)
            .Append(MemberName)
            .OpenParanthesis()
                .Append((b, arg) => arg.Generate(b), Arguments)
            .CloseParanthesis()
            .Append(AppendBaseConstructorCall)
            .WithBlock(b => b.Append(BodyStream));
    }

    private void AppendBaseConstructorCall(CodeBuilder builder)
    {
        if (baseConstructorCall is null)
            return;

        builder.AppendWord($": base({baseConstructorCall})");
    }
}
