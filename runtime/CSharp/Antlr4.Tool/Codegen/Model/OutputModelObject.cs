// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public abstract class OutputModelObject
    {
        public GrammarAST ast;
        public OutputModelFactory factory;

        protected OutputModelObject()
        {
        }

        protected OutputModelObject(OutputModelFactory factory)
            : this(factory, null)
        {
        }

        protected OutputModelObject(OutputModelFactory factory, GrammarAST ast)
        {
            this.factory = factory;
            this.ast = ast;
        }
    }
}