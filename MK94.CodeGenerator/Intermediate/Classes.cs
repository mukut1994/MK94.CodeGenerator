using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.CodeGenerator.Intermediate;

public interface IFileGenerator
{
    void Generate(Func<string, CodeBuilder> factory);
}

[Flags]
public enum MemberFlags
{
    Public = 1,
    Static = 2,
    Override = 4
}
