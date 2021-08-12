// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool;

namespace Antlr4.Codegen.Model
{
    /**
     * The code associated with the outermost alternative of a rule.
     * Sometimes we might want to treat them differently in the
     * code generation.
     */
    public class CodeBlockForOuterMostAlt : CodeBlockForAlt
    {
        /**
         * The alternative.
         */
        public Alternative alt;

        /**
         * The label for the alternative; or null if the alternative is not labeled.
         */
        public string altLabel;

        public CodeBlockForOuterMostAlt(OutputModelFactory factory, Alternative alt)
            : base(factory)
        {
            this.alt = alt;
            altLabel = alt.ast.altLabel != null ? alt.ast.altLabel.Text : null;
        }
    }
}