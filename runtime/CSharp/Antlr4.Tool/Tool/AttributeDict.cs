// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Misc;
using Antlr4.Tool.Ast;

namespace Antlr4.Tool
{
    /**
     * Track the attributes within retval, arg lists etc...
     * <p>
     *     Each rule has potentially 3 scopes: return values,
     *     parameters, and an implicitly-named scope (i.e., a scope defined in a rule).
     *     Implicitly-defined scopes are named after the rule; rules and scopes then
     *     must live in the same name space--no collisions allowed.
     * </p>
     */
    public class AttributeDict
    {
        public enum DictType
        {
            ARG,
            RET,
            LOCAL,
            TOKEN,
            PREDEFINED_RULE,
            PREDEFINED_LEXER_RULE
        }

        /**
         * All {@link Token} scopes (token labels) share the same fixed scope of
         * of predefined attributes.  I keep this out of the {@link Token}
         * interface to avoid a runtime type leakage.
         */
        public static readonly AttributeDict predefinedTokenDict = new(DictType.TOKEN);

        /**
         * The list of {@link Attribute} objects.
         */
        [NotNull] public readonly LinkedHashMap<string, AttributeNode> attributes =
            new();

        public GrammarAST ast;
        public string name;
        public DictType type;

        static AttributeDict()
        {
            predefinedTokenDict.Add(new AttributeNode("text"));
            predefinedTokenDict.Add(new AttributeNode("type"));
            predefinedTokenDict.Add(new AttributeNode("line"));
            predefinedTokenDict.Add(new AttributeNode("index"));
            predefinedTokenDict.Add(new AttributeNode("pos"));
            predefinedTokenDict.Add(new AttributeNode("channel"));
            predefinedTokenDict.Add(new AttributeNode("int"));
        }

        public AttributeDict()
        {
        }

        public AttributeDict(DictType type)
        {
            this.type = type;
        }

        public virtual AttributeNode Add(AttributeNode a)
        {
            a.dict = this;
            return attributes[a.name] = a;
        }

        public virtual AttributeNode Get(string name)
        {
            AttributeNode result;
            if (!attributes.TryGetValue(name, out result))
            {
                return null;
            }

            return result;
        }

        public virtual string GetName()
        {
            return name;
        }

        public virtual int Size()
        {
            return attributes.Count;
        }

        /**
         * Return the set of keys that collide from
         * {@code this} and {@code other}.
         */
        [return: NotNull]
        public ISet<string> Intersection([AllowNull] AttributeDict other)
        {
            if (other == null || other.Size() == 0 || Size() == 0)
            {
                return new HashSet<string>();
            }

            ISet<string> result = new HashSet<string>(attributes.Keys);
            result.IntersectWith(other.attributes.Keys);
            return result;
        }

        public override string ToString()
        {
            return GetName() + ":" + attributes;
        }
    }
}