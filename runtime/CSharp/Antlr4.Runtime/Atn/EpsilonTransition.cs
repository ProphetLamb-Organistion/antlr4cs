// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif


namespace Antlr4.Runtime.Atn
{
    public sealed class EpsilonTransition : Transition
    {
        public EpsilonTransition([NotNull] ATNState target)
            : this(target, -1)
        {
        }

        public EpsilonTransition([NotNull] ATNState target, int outermostPrecedenceReturn)
            : base(target)
        {
            this.OutermostPrecedenceReturn = outermostPrecedenceReturn;
        }

        /// <returns>
        ///     the rule index of a precedence rule for which this transition is
        ///     returning from, where the precedence value is 0; otherwise, -1.
        /// </returns>
        /// <seealso cref="ATNConfig.PrecedenceFilterSuppressed()" />
        /// <seealso cref="ParserATNSimulator.ApplyPrecedenceFilter(ATNConfigSet, ParserRuleContext, PredictionContextCache)"></seealso>
        /// <since>4.4.1</since>
        public int OutermostPrecedenceReturn { get; }

        public override TransitionType TransitionType => TransitionType.Epsilon;

        public override bool IsEpsilon => true;

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return false;
        }

        [return: NotNull]
        public override string ToString()
        {
            return "epsilon";
        }
    }
}