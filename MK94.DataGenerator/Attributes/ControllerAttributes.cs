using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class ControllerMethodAttribute : Attribute
    {
        protected ControllerMethodAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
    }

    public class GetAttribute : ControllerMethodAttribute
    {
        public GetAttribute(string path = null) : base(path)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AnonymousAttribute : Attribute { }

    public class PostAttribute : ControllerMethodAttribute
    {
        public PostAttribute(string path = null) : base(path)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ParameterAttribute : Attribute { }

    public class RouteAttribute : ParameterAttribute { }
    public class QueryAttribute : ParameterAttribute { }
    public class FormAttribute : ParameterAttribute { }
    public class BodyAttribute : ParameterAttribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class RepliesWithWebsocketAttribute : Attribute
    {
        public Type ResponseType { get; set; }

        public Type? ReceiveType { get; set; }

        public RepliesWithWebsocketAttribute(Type responseType, Type receiveType)
        {
            ResponseType = responseType;
            ReceiveType = receiveType;
        }

        public RepliesWithWebsocketAttribute(Type responseType)
        {
            ResponseType = responseType;
        }
    }
}
