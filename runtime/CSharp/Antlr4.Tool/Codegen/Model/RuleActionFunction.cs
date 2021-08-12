// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Misc;
using Antlr4.Tool;

namespace Antlr4.Codegen.Model
{
    public class RuleActionFunction : OutputModelObject
    {
        /**
         * Map actionIndex to Action
         */
        [ModelElement] public LinkedHashMap<int, Action> actions =
            new();

        public string ctxType;
        public string name;
        public int ruleIndex;

        public RuleActionFunction(OutputModelFactory factory, Rule r, string ctxType)
            : base(factory)
        {
            name = r.name;
            ruleIndex = r.index;
            this.ctxType = ctxType;
        }
    }
}