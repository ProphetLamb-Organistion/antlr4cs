// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Atn;

namespace Antlr4.Runtime
{
    /// <summary>A semantic predicate failed during validation.</summary>
    /// <remarks>
    ///     A semantic predicate failed during validation.  Validation of predicates
    ///     occurs when normally parsing the alternative just like matching a token.
    ///     Disambiguating predicate evaluation occurs when we test a predicate during
    ///     prediction.
    /// </remarks>
    [Serializable]
    public class FailedPredicateException : RecognitionException
    {
        private const long serialVersionUID = 5379330841495778709L;

        public FailedPredicateException([NotNull] Parser recognizer)
            : this(recognizer, null)
        {
        }

        public FailedPredicateException([NotNull] Parser recognizer, [AllowNull] string predicate)
            : this(recognizer, predicate, null)
        {
        }

        public FailedPredicateException([NotNull] Parser recognizer, [AllowNull] string predicate, [AllowNull] string message)
            : base(FormatMessage(predicate, message), recognizer, (ITokenStream) recognizer.InputStream, recognizer._ctx)
        {
            ATNState s = recognizer.Interpreter.atn.states[recognizer.State];
            AbstractPredicateTransition trans = (AbstractPredicateTransition) s.Transition(0);
            if (trans is PredicateTransition)
            {
                RuleIndex = ((PredicateTransition) trans).ruleIndex;
                PredIndex = ((PredicateTransition) trans).predIndex;
            }
            else
            {
                RuleIndex = 0;
                PredIndex = 0;
            }

            this.Predicate = predicate;
            OffendingToken = recognizer.CurrentToken;
        }

        public virtual int RuleIndex { get; }

        public virtual int PredIndex { get; }

        [MaybeNull] public virtual string Predicate { get; }

        [return: NotNull]
        private static string FormatMessage([AllowNull] string predicate, [AllowNull] string message)
        {
            if (message != null)
            {
                return message;
            }

            return $"failed predicate: {{{predicate}}}?";
        }
    }
}