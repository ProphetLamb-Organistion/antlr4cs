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
    /// <summary>A transition containing a set of values.</summary>
    public class SetTransition : Transition
    {
        [NotNull] public readonly IntervalSet set;

        public SetTransition([NotNull] ATNState target, [AllowNull] IntervalSet set)
            : base(target)
        {
            // TODO (sam): should we really allow null here?
            if (set == null)
            {
                set = IntervalSet.Of(TokenConstants.InvalidType);
            }

            this.set = set;
        }

        public override TransitionType TransitionType => TransitionType.Set;

        public override IntervalSet Label => set;

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return set.Contains(symbol);
        }

        [return: NotNull]
        public override string ToString()
        {
            return set.ToString();
        }
    }
}