using System;

namespace Namespace.A;

public class TypeA
{
    [Example]
    public Int32 PropA { get; set; }

    [Example]
    public Int32 MethodA(Int32 a, Int32 b)
    {
        return a + b;
    }
}
namespace Namespace.B;

public class TypeB
{
    public Int32 PropA { get; set; }

    public Int32 MethodA(Int32 c, Int32 d)
    {
        return c + d;
    }
}
