namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public abstract class IntermediateMemberDefinition
{
    public string Name { get; set; }

    public MemberFlags Flags { get; set; }

    public IntermediateMemberDefinition(MemberFlags flags, string name)
    {
        Name = name;
        Flags = flags;
    }

    protected void AppendMemberFlags(CodeBuilder builder)
    {
        if (Flags.HasFlag(MemberFlags.Public))
            builder.AppendWord("public");

        if (Flags.HasFlag(MemberFlags.Partial))
            builder.AppendWord("partial");

        if (Flags.HasFlag(MemberFlags.Static))
            builder.AppendWord("static");

        if (Flags.HasFlag(MemberFlags.Override))
            builder.AppendWord("override");
    }

    protected void MemberName(CodeBuilder builder)
    {
        builder.AppendWord(Name);
    }
}
