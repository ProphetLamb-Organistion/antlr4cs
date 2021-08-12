// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    public class Loop : Choice
    {
        public readonly int exitAlt;
        public int blockStartStateNumber;

        [ModelElement] public IList<SrcOp> iteration;

        public int loopBackStateNumber;

        public Loop(OutputModelFactory factory,
            GrammarAST blkOrEbnfRootAST,
            IList<CodeBlockForAlt> alts)
            : base(factory, blkOrEbnfRootAST, alts)
        {
            bool nongreedy = blkOrEbnfRootAST is QuantifierAST && !((QuantifierAST) blkOrEbnfRootAST).GetGreedy();
            exitAlt = nongreedy ? 1 : alts.Count + 1;
        }

        public virtual void AddIterationOp(SrcOp op)
        {
            if (iteration == null)
            {
                iteration = new List<SrcOp>();
            }

            iteration.Add(op);
        }
    }
}