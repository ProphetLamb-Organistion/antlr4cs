// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool.Ast;

namespace Antlr4.Analysis
{
    public class LeftRecursiveRuleAltInfo
    {
        public readonly bool isListLabel;
        public AltAST altAST; // transformed ALT
        public string altLabel;
        public int altNum; // original alt index (from 1)
        public string altText;
        public string leftRecursiveRuleRefLabel;
        public int nextPrec;
        public AltAST originalAltAST;

        public LeftRecursiveRuleAltInfo(int altNum, string altText)
            : this(altNum, altText, null, null, false, null)
        {
        }

        public LeftRecursiveRuleAltInfo(int altNum, string altText,
            string leftRecursiveRuleRefLabel,
            string altLabel,
            bool isListLabel,
            AltAST originalAltAST)
        {
            this.altNum = altNum;
            this.altText = altText;
            this.leftRecursiveRuleRefLabel = leftRecursiveRuleRefLabel;
            this.altLabel = altLabel;
            this.isListLabel = isListLabel;
            this.originalAltAST = originalAltAST;
        }
    }
}