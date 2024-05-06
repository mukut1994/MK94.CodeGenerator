using System;
using System.Collections.Generic;
using System.Text;

namespace MK94.CodeGenerator.Attributes
{
    public class IdFeature : ProjectAttribute
    {
        private const string Name = "IdFeature";

        public static Parser Parser { get; } = new Parser(new ParserConfig { Project = Name });
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
    public class DependsOnAttribute : FeatureAttribute, TypeAttribute, PropertyAttribute
    {
        public Type Type { get; }

        public DependsOnAttribute(Type type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class ProjectAttribute : FeatureAttribute, TypeAttribute, PropertyAttribute
    {
        public string Project { get; }

        public ProjectAttribute(string project)
        {
            Project = project;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class OnlyOnAttribute : FeatureAttribute, PropertyAttribute
    {
        public string Project { get; }

        public OnlyOnAttribute(string project)
        {
            Project = project;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class MessageCodeAttribute : FeatureAttribute
    {
        public byte Code { get; }

        public MessageCodeAttribute(byte code)
        {
            Code = code;
        }
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class DbAttribute : FeatureAttribute, TypeAttribute
    {
        public string TableName { get; }

        public DbAttribute(string tableName = null)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class PrimaryKeyAttribute : FeatureAttribute, PropertyAttribute
    {

    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class GroupOfAttributes : FeatureAttribute, TypeAttribute
    {
        public TypeAttribute[] Attributes { get; }

        public GroupOfAttributes(params TypeAttribute[] attributes)
        {
            Attributes = attributes;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class GroupOfPropertyAttributes : FeatureAttribute, TypeAttribute
    {
        public PropertyAttribute[] Attributes { get; }

        public GroupOfPropertyAttributes(params PropertyAttribute[] attributes)
        {
            Attributes = attributes;
        }
    }
}
