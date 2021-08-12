// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;

namespace Antlr4.Tool.Ast
{
    /**
     * A node representing erroneous token range in token stream
     */
    public class GrammarASTErrorNode : GrammarAST
    {
        private readonly CommonErrorNode @delegate;

        public GrammarASTErrorNode(ITokenStream input, IToken start, IToken stop,
            RecognitionException e)
        {
            @delegate = new CommonErrorNode(input, start, stop, e);
        }

        public override bool IsNil => @delegate.IsNil;

        public override int Type
        {
            get => @delegate.Type;

            set => base.Type = value;
        }

        public override string Text
        {
            get => @delegate.Text;

            set => base.Text = value;
        }

        public override string ToString()
        {
            return @delegate.ToString();
        }
    }
}