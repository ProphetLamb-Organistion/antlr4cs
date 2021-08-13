// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime;
using Antlr4.Tool.Ast;

namespace Antlr4.Parse
{
    public class GrammarASTAdaptor : CommonTreeAdaptor
    {
        private readonly ICharStream input; // where we can find chars ref'd by tokens in tree

        public GrammarASTAdaptor()
        {
        }

        public GrammarASTAdaptor(ICharStream input)
        {
            this.input = input;
        }

        public override object Nil()
        {
            return (GrammarAST) base.Nil();
        }

        public override object Create(IToken token)
        {
            return new GrammarAST(token);
        }

        /**
         * Make sure even imaginary nodes know the input stream
         */
        public override object Create(int tokenType, string text)
        {
            GrammarAST t;
            if (tokenType == ANTLRParser.RULE)
            {
                // needed by TreeWizard to make RULE tree
                t = new RuleAST(new CommonToken(tokenType, text));
            }
            else if (tokenType == ANTLRParser.STRING_LITERAL)
            {
                // implicit lexer construction done with wizard; needs this node type
                // whereas grammar ANTLRParser.g can use token option to spec node type
                t = new TerminalAST(new CommonToken(tokenType, text));
            }
            else
            {
                t = (GrammarAST) base.Create(tokenType, text);
            }

            t.Token.InputStream = input;
            return t;
        }

        public override object Create(int tokenType, IToken fromToken, string text)
        {
            return (GrammarAST) base.Create(tokenType, fromToken, text);
        }

        public override object Create(int tokenType, IToken fromToken)
        {
            return (GrammarAST) base.Create(tokenType, fromToken);
        }

        public override object DupNode(object t)
        {
            if (t == null)
            {
                return null;
            }

            return ((GrammarAST) t).DupNode(); //Create(((GrammarAST)t).Token);
        }

        public override object ErrorNode(ITokenStream input, IToken start, IToken stop,
            RecognitionException e)
        {
            return new GrammarASTErrorNode(input, start, stop, e);
        }
    }
}