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
    public sealed class NotSetTransition : SetTransition
    {
        public NotSetTransition([NotNull] ATNState target, [AllowNull] IntervalSet set)
            : base(target, set)
        {
        }

        public override TransitionType TransitionType => TransitionType.NotSet;

        public override bool Matches(int symbol, int minVocabSymbol, int maxVocabSymbol)
        {
            return symbol >= minVocabSymbol && symbol <= maxVocabSymbol && !base.Matches(symbol, minVocabSymbol, maxVocabSymbol);
        }

        public override string ToString()
        {
            return '~' + base.ToString();
        }
    }
}