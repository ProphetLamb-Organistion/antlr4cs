// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;

namespace Antlr4.Tool
{
    /**
     * Track the names of attributes defined in arg lists, return values,
     * scope blocks etc...
     */
    public class AttributeNode
    {
        /**
         * The entire declaration such as "String foo" or "x:int"
         */
        public string decl;

        /**
         * Who contains us?
         */
        public AttributeDict dict;

        /**
         * The optional attribute initialization expression
         */
        public string initValue;

        /**
         * The name of the attribute "foo"
         */
        public string name;

        /**
         * A {@link Token} giving the position of the name of this attribute in the grammar.
         */
        public IToken token;

        /**
         * The type; might be empty such as for Python which has no static typing
         */
        public string type;

        public AttributeNode()
        {
        }

        public AttributeNode(string name)
            : this(name, null)
        {
        }

        public AttributeNode(string name, string decl)
        {
            this.name = name;
            this.decl = decl;
        }

        public override string ToString()
        {
            if (initValue != null)
            {
                return name + ":" + type + "=" + initValue;
            }

            if (type != null)
            {
                return name + ":" + type;
            }

            return name;
        }
    }
}