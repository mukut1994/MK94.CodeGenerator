namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public abstract class IntermediateTypedMemberDefinition : IntermediateMemberDefinition
{
    public CsharpTypeReference Type { get; set; }

    protected IntermediateTypedMemberDefinition(MemberFlags flags, CsharpTypeReference type, string name) : base(flags, name)
    {
        Type = type;
    }
}
