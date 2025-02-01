using MK94.CodeGenerator.Intermediate.Typescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public class IntermediateEnumDefinition : IntermediateMemberDefinition, IGenerator
{
    private List<(string key, string value)> keyValues = new();

    public IntermediateEnumDefinition(string name, MemberFlags flags) : base(flags | MemberFlags.Type, name)
    {
    }

    public IntermediateEnumDefinition WithKeyValue(string key, string value)
    {
        keyValues.Add((key, value));

        return this;
    }

    public void Generate(CodeBuilder builder)
    {
        AppendMemberFlags(builder);

        builder
            .AppendWord("enum")
            .AppendWord(Name)
            .OpenBlock()
            .Append((b, kv) =>
            {
                b.Append($"{kv.key} = {kv.value},")
                .NewLine();
            }, keyValues.Distinct())
            .CloseBlock()
            .NewLine();
    }
}