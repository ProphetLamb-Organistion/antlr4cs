// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Codegen.Model.Decl
{
    /**
     * {@code public Token X() { }}
     */
    public class ContextTokenGetterDecl : ContextGetterDecl
    {
        public bool optional;

        public ContextTokenGetterDecl(OutputModelFactory factory, string name, bool optional)
            : base(factory, name)
        {
            this.optional = optional;
        }
    }
}