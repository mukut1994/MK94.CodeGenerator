using System;
using System.Collections.Generic;
using System.Text;

namespace MK94.CodeGenerator.Attributes
{
    public class IdFeature : ProjectAttribute
    {
        private const string Name = "IdFeature";

        public static Parser Parser { get; } = new Parser(Name);
        public IdFeature() : base(Name)
        {
        }
    }

    public interface IFormFile
    {

    }

    public interface IFileResult
    {

    }

    public interface PropertyAttribute
    {

    }

    public interface TypeAttribute
    {

    }

    public interface TypeOrPropertyAttribute : TypeAttribute, PropertyAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class DependsOnAttribute : Attribute, TypeAttribute, PropertyAttribute
    {
        public Type Type { get; }

        public DependsOnAttribute(Type type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class ProjectAttribute : Attribute, TypeAttribute, PropertyAttribute
    {
        public string Project { get; }

        public ProjectAttribute(string project)
        {
            Project = project;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
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
        public string Project { get; }

        public OnlyOnAttribute(string project)
        {
            Project = project;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class MessageCodeAttribute : Attribute
    {
        public byte Code { get; }

        public MessageCodeAttribute(byte code)
        {
            Code = code;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class DbAttribute : Attribute, TypeAttribute
    {
        public string TableName { get; }

        public DbAttribute(string tableName = null)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class PrimaryKeyAttribute : Attribute, PropertyAttribute
    {

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
