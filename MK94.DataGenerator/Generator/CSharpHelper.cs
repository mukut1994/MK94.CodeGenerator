using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Generator.Generators
{
    internal static class CSharpHelper
    {
        public static string CSharpName(Type type)
        {
            if (type == typeof(void))
                return "void";

            if (type.IsGenericType)
            {
                var name = type.GetGenericTypeDefinition().Name;
                var genericPart = type.GetGenericArguments().Select(g => CSharpName(g)).Aggregate((a, b) => $"{a}, {b}");

                return $"{name.Substring(0, name.IndexOf('`'))}<{genericPart}>";
            }

            return type.Name;
        }

        public static Type UnwrapTask(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                return type.GetGenericArguments()[0];

            return type;
        }
    }
}
