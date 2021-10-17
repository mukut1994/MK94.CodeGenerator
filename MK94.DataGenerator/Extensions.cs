using MK94.DataGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MK94.DataGenerator
{
    public static class Extensions
    {
        public static IEnumerable<T> GetCustomAttributesUngrouped<T>(this MemberInfo memberInfo)
            where T : Attribute
        {
            foreach (var attr in memberInfo.GetCustomAttributes<T>())
                yield return attr;

            var typeAttr = memberInfo.GetCustomAttributes<GroupOfAttributes>();
            var propAttr = memberInfo.GetCustomAttributes<GroupOfPropertyAttributes>();

            foreach (var group in typeAttr)
            {
                foreach (var attr in group.Attributes)
                {
                    if (attr is T t)
                        yield return t;
                }
            }

            foreach (var group in propAttr)
            {
                foreach (var attr in group.Attributes)
                {
                    if (attr is T t)
                        yield return t;
                }
            }
        }
    }
}
