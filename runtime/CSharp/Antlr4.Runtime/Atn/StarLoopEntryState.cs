// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Atn
{
    public sealed class StarLoopEntryState : DecisionState
    {
        public StarLoopbackState loopBackState;

        /// <summary>
        ///     For precedence decisions, this set marks states <em>S</em> which have all
        ///     of the following characteristics:
        ///     <ul>
        ///         <li>
        ///             One or more invocation sites of the current rule returns to
        ///             <em>S</em>.
        ///         </li>
        ///         <li>
        ///             The closure from <em>S</em> includes the current decision without
        ///             passing through any rule invocations or stepping out of the current
        ///             rule.
        ///         </li>
        ///     </ul>
        ///     <p>
        ///         This field is
        ///         <see langword="null" />
        ///         when
        ///         <see cref="precedenceRuleDecision" />
        ///         is
        ///         <see langword="false" />
        ///         .
        ///     </p>
        /// </summary>
        public BitSet precedenceLoopbackStates;

        /// <summary>
        ///     Indicates whether this state can benefit from a precedence DFA during SLL
        ///     decision making.
        /// </summary>
        /// <remarks>
        ///     Indicates whether this state can benefit from a precedence DFA during SLL
        ///     decision making.
        ///     <p>
        ///         This is a computed property that is calculated during ATN deserialization
        ///         and stored for use in
        ///         <see cref="ParserATNSimulator" />
        ///         and
        ///         <see cref="Antlr4.Runtime.ParserInterpreter" />
        ///         .
        ///     </p>
        /// </remarks>
        /// <seealso cref="Antlr4.Runtime.Dfa.DFA.IsPrecedenceDfa()" />
        public bool precedenceRuleDecision;

        public override StateType StateType => StateType.StarLoopEntry;
    }
}