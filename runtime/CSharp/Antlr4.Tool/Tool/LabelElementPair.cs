// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Tool
{
    public class LabelElementPair
    {
        public static readonly BitSet tokenTypeForTokens = new();
        public GrammarAST element;

        public GrammarAST label;
        public LabelType type;

        static LabelElementPair()
        {
            tokenTypeForTokens.Add(ANTLRParser.TOKEN_REF);
            tokenTypeForTokens.Add(ANTLRParser.STRING_LITERAL);
            tokenTypeForTokens.Add(ANTLRParser.WILDCARD);
        }

        public LabelElementPair(Grammar g, GrammarAST label, GrammarAST element, int labelOp)
        {
            this.label = label;
            this.element = element;
            // compute general case for label type
            if (element.GetFirstDescendantWithType(tokenTypeForTokens) != null)
            {
                if (labelOp == ANTLRParser.ASSIGN)
                {
                    type = LabelType.TOKEN_LABEL;
                }
                else
                {
                    type = LabelType.TOKEN_LIST_LABEL;
                }
            }
            else if (element.GetFirstDescendantWithType(ANTLRParser.RULE_REF) != null)
            {
                if (labelOp == ANTLRParser.ASSIGN)
                {
                    type = LabelType.RULE_LABEL;
                }
                else
                {
                    type = LabelType.RULE_LIST_LABEL;
                }
            }

            // now reset if lexer and string
            if (g.IsLexer())
            {
                if (element.GetFirstDescendantWithType(ANTLRParser.STRING_LITERAL) != null)
                {
                    if (labelOp == ANTLRParser.ASSIGN)
                    {
                        type = LabelType.LEXER_STRING_LABEL;
                    }
                }
            }
        }

        public override string ToString()
        {
            return label.Text + " " + type + " " + element;
        }
    }
}