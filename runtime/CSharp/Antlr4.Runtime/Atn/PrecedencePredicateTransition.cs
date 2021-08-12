// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif



namespace Antlr4.Runtime.Atn
{
    /// <author>Sam Harwell</author>
    public sealed class PrecedencePredicateTransition : AbstractPredicateTransition
    {
        public readonly int precedence;

        public PrecedencePredicateTransition([NotNull] ATNState target, int precedence)
            : base(target)
        {
            this.precedence = precedence;
        }

        public override TransitionType TransitionType => TransitionType.Precedence;

        public override bool IsEpsilon => true;

        public SemanticContext.PrecedencePredicate Predicate => new SemanticContext.PrecedencePredicate(precedence);

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return false;
        }

        public override string ToString()
        {
            return precedence + " >= _p";
        }
    }
}