// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Analysis;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    /**
     * Any ALT (which can be child of ALT_REWRITE node)
     */
    public class AltAST : GrammarASTWithOptions
    {
        public Alternative alt;

        /**
         * If someone specified an outermost alternative label with #foo.
         * Token type will be ID.
         */
        public GrammarAST altLabel;

        /**
         * If we transformed this alt from a left-recursive one, need info on it
         */
        public LeftRecursiveRuleAltInfo leftRecursiveAltInfo;

        public AltAST(AltAST node)
            : base(node)
        {
            alt = node.alt;
            altLabel = node.altLabel;
            leftRecursiveAltInfo = node.leftRecursiveAltInfo;
        }

        public AltAST(IToken t)
            : base(t)
        {
        }

        public AltAST(int type)
            : base(type)
        {
        }

        public AltAST(int type, IToken t)
            : base(type, t)
        {
        }

        public AltAST(int type, IToken t, string text)
            : base(type, t, text)
        {
        }

        public override ITree DupNode()
        {
            return new AltAST(this);
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}