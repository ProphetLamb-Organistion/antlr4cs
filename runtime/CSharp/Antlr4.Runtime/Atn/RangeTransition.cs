// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Utility;


namespace Antlr4.Runtime.Atn
{
    public sealed class RangeTransition : Transition
    {
        public readonly int from;

        public readonly int to;

        public RangeTransition([NotNull] ATNState target, int from, int to)
            : base(target)
        {
            this.from = from;
            this.to = to;
        }

        public override TransitionType TransitionType => TransitionType.Range;

        public override IntervalSet Label => IntervalSet.Of(from, to);

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return symbol >= from && symbol <= to;
        }

        [return: NotNull]
        public override string ToString()
        {
            return "'" + (char) from + "'..'" + (char) to + "'";
        }
    }
}