// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    public class ExceptionClause : SrcOp
    {
        [ModelElement] public Action catchAction;

        [ModelElement] public Action catchArg;

        public ExceptionClause(OutputModelFactory factory,
            ActionAST catchArg,
            ActionAST catchAction)
            : base(factory, catchArg)
        {
            this.catchArg = new Action(factory, catchArg);
            this.catchAction = new Action(factory, catchAction);
        }
    }
}