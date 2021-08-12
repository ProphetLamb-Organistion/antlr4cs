// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif


namespace Antlr4.Runtime.Atn
{
    public sealed class ActionTransition : Transition
    {
        public readonly int actionIndex;

        public readonly bool isCtxDependent;
        public readonly int ruleIndex;

        public ActionTransition([NotNull] ATNState target, int ruleIndex)
            : this(target, ruleIndex, -1, false)
        {
        }

        public ActionTransition([NotNull] ATNState target, int ruleIndex, int actionIndex, bool isCtxDependent)
            : base(target)
        {
            // e.g., $i ref in action
            this.ruleIndex = ruleIndex;
            this.actionIndex = actionIndex;
            this.isCtxDependent = isCtxDependent;
        }

        public override TransitionType TransitionType => TransitionType.Action;

        public override bool IsEpsilon => true;

        // we are to be ignored by analysis 'cept for predicates
        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return false;
        }

        public override string ToString()
        {
            return "action_" + ruleIndex + ":" + actionIndex;
        }
    }
}