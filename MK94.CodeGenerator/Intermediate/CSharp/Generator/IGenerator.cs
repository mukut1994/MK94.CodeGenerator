using System.Collections.Generic;

namespace MK94.CodeGenerator.Intermediate.CSharp.Generator;

public interface IGenerator
{
    void Generate(CodeBuilder builder);

    void GetRequiredReferences(HashSet<CsharpTypeReference> refs) { }
}
