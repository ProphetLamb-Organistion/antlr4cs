// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool;

namespace Antlr4.Codegen.Model
{
    /**
     * @author Sam Harwell
     */
    public class LeftFactoredRuleFunction : RuleFunction
    {
        public LeftFactoredRuleFunction(OutputModelFactory factory, Rule r)
            : base(factory, r)
        {
        }
    }
}