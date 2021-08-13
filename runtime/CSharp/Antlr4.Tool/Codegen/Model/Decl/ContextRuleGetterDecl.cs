// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Codegen.Model.Decl
{
    /**
     * {@code public XContext X() { }}
     */
    public class ContextRuleGetterDecl : ContextGetterDecl
    {
        public string ctxName;
        public bool optional;

        public ContextRuleGetterDecl(OutputModelFactory factory, string name, string ctxName, bool optional)
            : base(factory, name)
        {
            this.ctxName = ctxName;
            this.optional = optional;
        }
    }
}