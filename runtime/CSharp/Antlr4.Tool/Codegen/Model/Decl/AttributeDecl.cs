// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool;

namespace Antlr4.Codegen.Model.Decl
{
    /** */
    public class AttributeDecl : Decl
    {
        public AttributeDecl(OutputModelFactory factory, AttributeNode a)
            : base(factory, a.name, a.decl)
        {
            Type = a.type;
            InitValue = a.initValue;
        }

        public string Type { get; }

        public string InitValue { get; }
    }
}