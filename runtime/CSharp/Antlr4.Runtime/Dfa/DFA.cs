// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Threading;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Dfa
{
    public class DFA
    {
        [NotNull] private static readonly EmptyEdgeMap<DFAState> emptyPrecedenceEdges = new(0, 200);

        /// <summary>From which ATN state did we create this DFA?</summary>
        [NotNull] public readonly ATNState atnStartState;

        public readonly int decision;

        /// <summary>
        ///     <see langword="true" />
        ///     if this DFA is for a precedence decision; otherwise,
        ///     <see langword="false" />
        ///     . This is the backing field for <see cref="IsPrecedenceDfa" />.
        /// </summary>
        private readonly bool precedenceDfa;

        [NotNull] public readonly AtomicReference<DFAState> s0 = new();

        [NotNull] public readonly AtomicReference<DFAState> s0full = new();

        /// <summary>A set of all DFA states.</summary>
        /// <remarks>
        ///     A set of all DFA states. Use
        ///     <see cref="System.Collections.Generic.IDictionary{K, V}" />
        ///     so we can get old state back
        ///     (
        ///     <see cref="HashSet{T}" />
        ///     only allows you to see if it's there).
        /// </remarks>
        [NotNull] public readonly ConcurrentDictionary<DFAState, DFAState> states = new();

        private int nextStateNumber;

        public DFA([NotNull] ATNState atnStartState)
            : this(atnStartState, 0)
        {
        }

        public DFA([NotNull] ATNState atnStartState, int decision)
        {
            this.atnStartState = atnStartState;
            this.decision = decision;
            if (this.atnStartState.atn.grammarType == ATNType.Lexer)
            {
                MinDfaEdge = LexerATNSimulator.MinDfaEdge;
                MaxDfaEdge = LexerATNSimulator.MaxDfaEdge;
            }
            else
            {
                MinDfaEdge = TokenConstants.Eof;
                MaxDfaEdge = atnStartState.atn.maxTokenType;
            }

            EmptyEdgeMap = new EmptyEdgeMap<DFAState>(MinDfaEdge, MaxDfaEdge);
            EmptyContextEdgeMap = new EmptyEdgeMap<DFAState>(-1, atnStartState.atn.states.Count - 1);
            bool isPrecedenceDfa = false;
            if (atnStartState is StarLoopEntryState)
            {
                if (((StarLoopEntryState) atnStartState).precedenceRuleDecision)
                {
                    isPrecedenceDfa = true;
                    s0.Set(new DFAState(emptyPrecedenceEdges, EmptyContextEdgeMap, new ATNConfigSet()));
                    s0full.Set(new DFAState(emptyPrecedenceEdges, EmptyContextEdgeMap, new ATNConfigSet()));
                }
            }

            precedenceDfa = isPrecedenceDfa;
        }

        public int MinDfaEdge { get; }

        public int MaxDfaEdge { get; }

        [field: NotNull] public virtual EmptyEdgeMap<DFAState> EmptyEdgeMap { get; }

        [field: NotNull] public virtual EmptyEdgeMap<DFAState> EmptyContextEdgeMap { get; }

        /// <summary>Gets whether this DFA is a precedence DFA.</summary>
        /// <remarks>
        ///     Gets whether this DFA is a precedence DFA. Precedence DFAs use a special
        ///     start state
        ///     <see cref="s0" />
        ///     which is not stored in
        ///     <see cref="states" />
        ///     . The
        ///     <see cref="DFAState.edges" />
        ///     array for this start state contains outgoing edges
        ///     supplying individual start states corresponding to specific precedence
        ///     values.
        /// </remarks>
        /// <returns>
        ///     <see langword="true" />
        ///     if this is a precedence DFA; otherwise,
        ///     <see langword="false" />
        ///     .
        /// </returns>
        /// <seealso cref="Antlr4.Runtime.Parser.Precedence()" />
        /// <summary>Sets whether this is a precedence DFA.</summary>
        /// <value>
        ///     <see langword="true" />
        ///     if this is a precedence DFA; otherwise,
        ///     <see langword="false" />
        /// </value>
        /// <exception cref="System.NotSupportedException">
        ///     if
        ///     <c>precedenceDfa</c>
        ///     does not
        ///     match the value of
        ///     <see cref="IsPrecedenceDfa()" />
        ///     for the current DFA.
        /// </exception>
        public bool IsPrecedenceDfa
        {
            get => precedenceDfa;

            set
            {
                bool precedenceDfa = value;
                // s0.get() and s0full.get() are never null for a precedence DFA
                // s0full.get() is never null for a precedence DFA
                // s0.get() is never null for a precedence DFA
                if (precedenceDfa != IsPrecedenceDfa)
                {
                    throw new NotSupportedException("The precedenceDfa field cannot change after a DFA is constructed.");
                }
            }
        }

        public virtual bool IsEmpty
        {
            get
            {
                if (IsPrecedenceDfa)
                {
                    return s0.Get().EdgeMap.Count == 0 && s0full.Get().EdgeMap.Count == 0;
                }

                return s0.Get() == null && s0full.Get() == null;
            }
        }

        public virtual bool IsContextSensitive
        {
            get
            {
                if (IsPrecedenceDfa)
                {
                    return s0full.Get().EdgeMap.Count != 0;
                }

                return s0full.Get() != null;
            }
        }

        /// <summary>Get the start state for a specific precedence value.</summary>
        /// <param name="precedence">The current precedence.</param>
        /// <returns>
        ///     The start state corresponding to the specified precedence, or
        ///     <see langword="null" />
        ///     if no start state exists for the specified precedence.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">if this is not a precedence DFA.</exception>
        /// <seealso cref="IsPrecedenceDfa()" />
        public DFAState GetPrecedenceStartState(int precedence, bool fullContext)
        {
            if (!IsPrecedenceDfa)
            {
                throw new InvalidOperationException("Only precedence DFAs may contain a precedence start state.");
            }

            if (fullContext)
            {
                return s0full.Get().GetTarget(precedence);
            }

            return s0.Get().GetTarget(precedence);
        }

        /// <summary>Set the start state for a specific precedence value.</summary>
        /// <param name="precedence">The current precedence.</param>
        /// <param name="startState">
        ///     The start state corresponding to the specified
        ///     precedence.
        /// </param>
        /// <exception cref="System.InvalidOperationException">if this is not a precedence DFA.</exception>
        /// <seealso cref="IsPrecedenceDfa()" />
        public void SetPrecedenceStartState(int precedence, bool fullContext, DFAState startState)
        {
            if (!IsPrecedenceDfa)
            {
                throw new InvalidOperationException("Only precedence DFAs may contain a precedence start state.");
            }

            if (precedence < 0)
            {
                return;
            }

            if (fullContext)
            {
                lock (s0full)
                {
                    s0full.Get().SetTarget(precedence, startState);
                }
            }
            else
            {
                lock (s0)
                {
                    s0.Get().SetTarget(precedence, startState);
                }
            }
        }

        public virtual DFAState AddState(DFAState state)
        {
            state.stateNumber = Interlocked.Increment(ref nextStateNumber) - 1;
            return states.GetOrAdd(state, state);
        }

        public override string ToString()
        {
            return ToString(Vocabulary.EmptyVocabulary);
        }

        [ObsoleteAttribute(@"Use ToString(Antlr4.Runtime.IVocabulary) instead.")]
        public virtual string ToString([AllowNull] string[] tokenNames)
        {
            if (s0.Get() == null)
            {
                return String.Empty;
            }

            DFASerializer serializer = new(this, tokenNames);
            return serializer.ToString();
        }

        public virtual string ToString([NotNull] IVocabulary vocabulary)
        {
            if (s0.Get() == null)
            {
                return String.Empty;
            }

            DFASerializer serializer = new(this, vocabulary);
            return serializer.ToString();
        }

        [ObsoleteAttribute(@"Use ToString(Antlr4.Runtime.IVocabulary, string[]) instead.")]
        public virtual string ToString([AllowNull] string[] tokenNames, [AllowNull] string[] ruleNames)
        {
            if (s0.Get() == null)
            {
                return String.Empty;
            }

            DFASerializer serializer = new(this, tokenNames, ruleNames, atnStartState.atn);
            return serializer.ToString();
        }

        public virtual string ToString([NotNull] IVocabulary vocabulary, [AllowNull] string[] ruleNames)
        {
            if (s0.Get() == null)
            {
                return String.Empty;
            }

            DFASerializer serializer = new(this, vocabulary, ruleNames, atnStartState.atn);
            return serializer.ToString();
        }

        public virtual string ToLexerString()
        {
            if (s0.Get() == null)
            {
                return String.Empty;
            }

            DFASerializer serializer = new LexerDFASerializer(this);
            return serializer.ToString();
        }
    }
}