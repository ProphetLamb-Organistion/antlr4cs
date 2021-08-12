// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif



namespace Antlr4.Runtime.Atn
{
    public sealed class RuleTransition : Transition
    {
        public readonly int precedence;

        /// <summary>Ptr to the rule definition object for this rule ref</summary>
        public readonly int ruleIndex;

        /// <summary>What node to begin computations following ref to rule</summary>
        [NotNull] public ATNState followState;

        public bool optimizedTailCall;

        public bool tailCall;

        [Obsolete(@"UseRuleTransition(RuleStartState, int, int, ATNState) instead.")]
        public RuleTransition([NotNull] RuleStartState ruleStart, int ruleIndex, [NotNull] ATNState followState)
            : this(ruleStart, ruleIndex, 0, followState)
        {
        }

        public RuleTransition([NotNull] RuleStartState ruleStart, int ruleIndex, int precedence, [NotNull] ATNState followState)
            : base(ruleStart)
        {
            // no Rule object at runtime
            this.ruleIndex = ruleIndex;
            this.precedence = precedence;
            this.followState = followState;
        }

        public override TransitionType TransitionType => TransitionType.Rule;

        public override bool IsEpsilon => true;

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return false;
        }
    }
}