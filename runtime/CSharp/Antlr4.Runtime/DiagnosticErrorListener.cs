// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime
{
    /// <summary>
    ///     This implementation of
    ///     <see cref="IAntlrErrorListener{Symbol}" />
    ///     can be used to identify
    ///     certain potential correctness and performance problems in grammars. "Reports"
    ///     are made by calling
    ///     <see cref="Parser.NotifyErrorListeners(string)" />
    ///     with the appropriate
    ///     message.
    ///     <ul>
    ///         <li>
    ///             <b>Ambiguities</b>: These are cases where more than one path through the
    ///             grammar can match the input.
    ///         </li>
    ///         <li>
    ///             <b>Weak context sensitivity</b>: These are cases where full-context
    ///             prediction resolved an SLL conflict to a unique alternative which equaled the
    ///             minimum alternative of the SLL conflict.
    ///         </li>
    ///         <li>
    ///             <b>Strong (forced) context sensitivity</b>: These are cases where the
    ///             full-context prediction resolved an SLL conflict to a unique alternative,
    ///             <em>and</em> the minimum alternative of the SLL conflict was found to not be
    ///             a truly viable alternative. Two-stage parsing cannot be used for inputs where
    ///             this situation occurs.
    ///         </li>
    ///     </ul>
    /// </summary>
    /// <author>Sam Harwell</author>
    public class DiagnosticErrorListener : BaseErrorListener
    {
        /// <summary>
        ///     When
        ///     <see langword="true" />
        ///     , only exactly known ambiguities are reported.
        /// </summary>
        protected internal readonly bool exactOnly;

        /// <summary>
        ///     Initializes a new instance of
        ///     <see cref="DiagnosticErrorListener" />
        ///     which only
        ///     reports exact ambiguities.
        /// </summary>
        public DiagnosticErrorListener()
            : this(true)
        {
        }

        /// <summary>
        ///     Initializes a new instance of
        ///     <see cref="DiagnosticErrorListener" />
        ///     , specifying
        ///     whether all ambiguities or only exact ambiguities are reported.
        /// </summary>
        /// <param name="exactOnly">
        ///     <see langword="true" />
        ///     to report only exact ambiguities, otherwise
        ///     <see langword="false" />
        ///     to report all ambiguities.
        /// </param>
        public DiagnosticErrorListener(bool exactOnly)
        {
            this.exactOnly = exactOnly;
        }

        public override void ReportAmbiguity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, bool exact, [AllowNull] BitSet ambigAlts,
            [NotNull] ATNConfigSet configs)
        {
            if (exactOnly && !exact)
            {
                return;
            }

            string decision = GetDecisionDescription(recognizer, dfa);
            BitSet conflictingAlts = GetConflictingAlts(ambigAlts, configs);
            string text = ((ITokenStream) recognizer.InputStream).GetText(Interval.Of(startIndex, stopIndex));
            recognizer.NotifyErrorListeners($"reportAmbiguity d={decision}: ambigAlts={conflictingAlts}, input='{text}'");
        }

        public override void ReportAttemptingFullContext([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, [AllowNull] BitSet conflictingAlts,
            [NotNull] SimulatorState conflictState)
        {
            string decision = GetDecisionDescription(recognizer, dfa);
            string text = ((ITokenStream) recognizer.InputStream).GetText(Interval.Of(startIndex, stopIndex));
            recognizer.NotifyErrorListeners($"reportAttemptingFullContext d={decision}, input='{text}'");
        }

        public override void ReportContextSensitivity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, int prediction,
            [NotNull] SimulatorState acceptState)
        {
            string decision = GetDecisionDescription(recognizer, dfa);
            string text = ((ITokenStream) recognizer.InputStream).GetText(Interval.Of(startIndex, stopIndex));
            recognizer.NotifyErrorListeners($"reportContextSensitivity d={decision}, input='{text}'");
        }

        protected internal virtual string GetDecisionDescription([NotNull] Parser recognizer, [NotNull] DFA dfa)
        {
            int decision = dfa.decision;
            int ruleIndex = dfa.atnStartState.ruleIndex;
            string[] ruleNames = recognizer.RuleNames;
            if (ruleIndex < 0 || ruleIndex >= ruleNames.Length)
            {
                return decision.ToString();
            }

            string ruleName = ruleNames[ruleIndex];
            if (String.IsNullOrEmpty(ruleName))
            {
                return decision.ToString();
            }

            return $"{decision} ({ruleName})";
        }

        /// <summary>
        ///     Computes the set of conflicting or ambiguous alternatives from a
        ///     configuration set, if that information was not already provided by the
        ///     parser.
        /// </summary>
        /// <param name="reportedAlts">
        ///     The set of conflicting or ambiguous alternatives, as
        ///     reported by the parser.
        /// </param>
        /// <param name="configs">The conflicting or ambiguous configuration set.</param>
        /// <returns>
        ///     Returns
        ///     <paramref name="reportedAlts" />
        ///     if it is not
        ///     <see langword="null" />
        ///     , otherwise
        ///     returns the set of alternatives represented in
        ///     <paramref name="configs" />
        ///     .
        /// </returns>
        [return: NotNull]
        protected internal virtual BitSet GetConflictingAlts([AllowNull] BitSet reportedAlts, [NotNull] ATNConfigSet configs)
        {
            if (reportedAlts != null)
            {
                return reportedAlts;
            }

            BitSet result = new();
            foreach (ATNConfig config in configs)
            {
                result.Set(config.Alt);
            }

            return result;
        }
    }
}