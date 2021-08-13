// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public abstract class LL1Loop : Choice
    {
        /**
         * The state associated wih the (A|B|...) block not loopback, which
         * is super.stateNumber
         */
        public int blockStartStateNumber;

        [ModelElement] public IList<SrcOp> iteration;

        public int loopBackStateNumber;

        [ModelElement] public OutputModelObject loopExpr;

        public LL1Loop(OutputModelFactory factory,
            GrammarAST blkAST,
            IList<CodeBlockForAlt> alts)
            : base(factory, blkAST, alts)
        {
        }

        public virtual void AddIterationOp(SrcOp op)
        {
            if (iteration == null)
            {
                iteration = new List<SrcOp>();
            }

            iteration.Add(op);
        }

        public virtual SrcOp AddCodeForLoopLookaheadTempVar(IntervalSet look)
        {
            TestSetInline expr = AddCodeForLookaheadTempVar(look);
            if (expr != null)
            {
                CaptureNextTokenType nextType = new(factory, expr.varName);
                AddIterationOp(nextType);
            }

            return expr;
        }
    }
}