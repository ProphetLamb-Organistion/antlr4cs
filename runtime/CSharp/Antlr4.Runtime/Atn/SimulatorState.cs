// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Dfa;

namespace Antlr4.Runtime.Atn
{
    /// <author>Sam Harwell</author>
    public class SimulatorState
    {
        public readonly ParserRuleContext outerContext;

        public readonly ParserRuleContext remainingOuterContext;

        public readonly DFAState s0;

        public readonly bool useContext;

        public SimulatorState(ParserRuleContext outerContext, [NotNull] DFAState s0, bool useContext, ParserRuleContext remainingOuterContext)
        {
            this.outerContext = outerContext != null ? outerContext : ParserRuleContext.EmptyContext;
            this.s0 = s0;
            this.useContext = useContext;
            this.remainingOuterContext = remainingOuterContext;
        }
    }
}