using System;
using System.Collections.Generic;
using System.Text;

namespace MK94.DataGenerator.Attributes
{
    public interface PropertyAttribute
    {

    }

    public interface TypeAttribute
    {

    }

    public interface TypeOrPropertyAttribute : TypeAttribute, PropertyAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class ProjectAttribute : Attribute, TypeAttribute, PropertyAttribute
    {
        public string Project { get; }

        public ProjectAttribute(string project)
        {
            Project = project;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class FileAttribute : Attribute
    {
        public string Name { get; set; }

        public FileAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class OnlyOnAttribute : Attribute, PropertyAttribute
    {
        public string Feature { get; }

        public OnlyOnAttribute(string feature)
        {
            Feature = feature;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class GroupOfAttributes : Attribute, TypeAttribute
    {
        public TypeAttribute[] Attributes { get; }

        public GroupOfAttributes(params TypeAttribute[] attributes)
        {
            Attributes = attributes;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class GroupOfPropertyAttributes : Attribute, TypeAttribute
    {
        public PropertyAttribute[] Attributes { get; }

        public GroupOfPropertyAttributes(params PropertyAttribute[] attributes)
        {
            Attributes = attributes;
        }
    }
}
