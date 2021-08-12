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
    /// <summary>TODO: make all transitions sets? no, should remove set edges</summary>
    public sealed class AtomTransition : Transition
    {
        /// <summary>The token type or character value; or, signifies special label.</summary>
        public readonly int label;

        public AtomTransition([NotNull] ATNState target, int label)
            : base(target)
        {
            this.label = label;
        }

        public override TransitionType TransitionType => TransitionType.Atom;

        public override IntervalSet Label => IntervalSet.Of(label);

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return label == symbol;
        }

        [return: NotNull]
        public override string ToString()
        {
            return label.ToString();
        }
    }
}