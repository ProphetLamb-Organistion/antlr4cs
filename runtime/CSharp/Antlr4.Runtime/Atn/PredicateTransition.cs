// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif



namespace Antlr4.Runtime.Atn
{
    /// <summary>
    ///     TODO: this is old comment:
    ///     A tree of semantic predicates from the grammar AST if label==SEMPRED.
    /// </summary>
    /// <remarks>
    ///     TODO: this is old comment:
    ///     A tree of semantic predicates from the grammar AST if label==SEMPRED.
    ///     In the ATN, labels will always be exactly one predicate, but the DFA
    ///     may have to combine a bunch of them as it collects predicates from
    ///     multiple ATN configurations into a single DFA state.
    /// </remarks>
    public sealed class PredicateTransition : AbstractPredicateTransition
    {
        public readonly bool isCtxDependent;

        public readonly int predIndex;
        public readonly int ruleIndex;

        public PredicateTransition([NotNull] ATNState target, int ruleIndex, int predIndex, bool isCtxDependent)
            : base(target)
        {
            // e.g., $i ref in pred
            this.ruleIndex = ruleIndex;
            this.predIndex = predIndex;
            this.isCtxDependent = isCtxDependent;
        }

        public override TransitionType TransitionType => TransitionType.Predicate;

        public override bool IsEpsilon => true;

        public SemanticContext.Predicate Predicate => new SemanticContext.Predicate(ruleIndex, predIndex, isCtxDependent);

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return false;
        }

        [return: NotNull]
        public override string ToString()
        {
            return "pred_" + ruleIndex + ":" + predIndex;
        }
    }
}