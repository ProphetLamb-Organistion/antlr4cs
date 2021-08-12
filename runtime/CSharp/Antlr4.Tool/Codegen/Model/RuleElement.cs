// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    public class RuleElement : SrcOp
    {
        /**
         * Associated ATN state for this rule elements (action, token, ruleref, ...)
         */
        public int stateNumber;

        public RuleElement(OutputModelFactory factory, GrammarAST ast)
            : base(factory, ast)
        {
            if (ast != null && ast.atnState != null)
            {
                stateNumber = ast.atnState.stateNumber;
            }
        }
    }
}