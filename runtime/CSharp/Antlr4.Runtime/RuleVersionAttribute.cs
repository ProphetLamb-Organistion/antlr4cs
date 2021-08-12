using System;

namespace Antlr4.Runtime
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class RuleVersionAttribute : Attribute
    {
        public RuleVersionAttribute(int version)
        {
            Version = version;
        }

        public int Version { get; }
    }
}