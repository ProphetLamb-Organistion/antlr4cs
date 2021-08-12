using System;

namespace Antlr4.Runtime
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class RuleDependencyAttribute : Attribute
    {
        public RuleDependencyAttribute(Type recognizer, int rule, int version)
        {
            Recognizer = recognizer;
            Rule = rule;
            Version = version;
            Dependents = Dependents.Parents | Dependents.Self;
        }

        public RuleDependencyAttribute(Type recognizer, int rule, int version, Dependents dependents)
        {
            Recognizer = recognizer;
            Rule = rule;
            Version = version;
            Dependents = dependents | Dependents.Self;
        }

        public Type Recognizer { get; }

        public int Rule { get; }

        public int Version { get; }

        public Dependents Dependents { get; }
    }
}