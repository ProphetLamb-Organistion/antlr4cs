// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Text;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Dfa
{
    /// <summary>A DFA state represents a set of possible ATN configurations.</summary>
    /// <remarks>
    ///     A DFA state represents a set of possible ATN configurations.
    ///     As Aho, Sethi, Ullman p. 117 says "The DFA uses its state
    ///     to keep track of all possible states the ATN can be in after
    ///     reading each input symbol.  That is to say, after reading
    ///     input a1a2..an, the DFA is in a state that represents the
    ///     subset T of the states of the ATN that are reachable from the
    ///     ATN's start state along some path labeled a1a2..an."
    ///     In conventional NFA&#x2192;DFA conversion, therefore, the subset T
    ///     would be a bitset representing the set of states the
    ///     ATN could be in.  We need to track the alt predicted by each
    ///     state as well, however.  More importantly, we need to maintain
    ///     a stack of states, tracking the closure operations as they
    ///     jump from rule to rule, emulating rule invocations (method calls).
    ///     I have to add a stack to simulate the proper lookahead sequences for
    ///     the underlying LL grammar from which the ATN was derived.
    ///     <p>
    ///         I use a set of ATNConfig objects not simple states.  An ATNConfig
    ///         is both a state (ala normal conversion) and a RuleContext describing
    ///         the chain of rules (if any) followed to arrive at that state.
    ///     </p>
    ///     <p>
    ///         A DFA state may have multiple references to a particular state,
    ///         but with different ATN contexts (with same or different alts)
    ///         meaning that state was reached via a different set of rule invocations.
    ///     </p>
    /// </remarks>
    public class DFAState
    {
        [NotNull] public readonly ATNConfigSet configs;

        private AcceptStateInfo acceptStateInfo;

        /// <summary>These keys for these edges are the top level element of the global context.</summary>
        [NotNull] private volatile AbstractEdgeMap<DFAState> contextEdges;

        /// <summary>Symbols in this set require a global context transition before matching an input symbol.</summary>
        [MaybeNull] private BitSet contextSymbols;

        /// <summary>
        ///     <c>edges.get(symbol)</c>
        ///     points to target of symbol.
        /// </summary>
        [NotNull] private volatile AbstractEdgeMap<DFAState> edges;

        /// <summary>
        ///     This list is computed by
        ///     <see cref="Antlr4.Runtime.Atn.ParserATNSimulator.PredicateDFAState(DFAState, Antlr4.Runtime.Atn.ATNConfigSet, int)" />
        ///     .
        /// </summary>
        [MaybeNull] public PredPrediction[] predicates;

        public int stateNumber = -1;

        public DFAState([NotNull] DFA dfa, [NotNull] ATNConfigSet configs)
            : this(dfa.EmptyEdgeMap, dfa.EmptyContextEdgeMap, configs)
        {
        }

        public DFAState([NotNull] EmptyEdgeMap<DFAState> emptyEdges, [NotNull] EmptyEdgeMap<DFAState> emptyContextEdges, [NotNull] ATNConfigSet configs)
        {
            this.configs = configs;
            edges = emptyEdges;
            contextEdges = emptyContextEdges;
        }

        public bool IsContextSensitive => contextSymbols != null;

        public AcceptStateInfo AcceptStateInfo
        {
            get => acceptStateInfo;
            set
            {
                AcceptStateInfo acceptStateInfo = value;
                this.acceptStateInfo = acceptStateInfo;
            }
        }

        public bool IsAcceptState => acceptStateInfo != null;

        public int Prediction
        {
            get
            {
                if (acceptStateInfo == null)
                {
                    return ATN.InvalidAltNumber;
                }

                return acceptStateInfo.Prediction;
            }
        }

        public LexerActionExecutor LexerActionExecutor
        {
            get
            {
                if (acceptStateInfo == null)
                {
                    return null;
                }

                return acceptStateInfo.LexerActionExecutor;
            }
        }

        public virtual ReadOnlyDictionary<int, DFAState> EdgeMap => edges.ToMap();

        public virtual ReadOnlyDictionary<int, DFAState> ContextEdgeMap
        {
            get
            {
                ReadOnlyDictionary<int, DFAState> map = contextEdges.ToMap();
                if (map.ContainsKey(-1))
                {
                    if (map.Count == 1)
                    {
                        return Collections.SingletonMap(PredictionContext.EmptyFullStateKey, map[-1]);
                    }

                    Dictionary<int, DFAState> result = map.ToDictionary(i => i.Key, i => i.Value);
                    result.Add(PredictionContext.EmptyFullStateKey, result[-1]);
                    result.Remove(-1);
                    map = new ReadOnlyDictionary<int, DFAState>(new Dictionary<int, DFAState>(result));
                }

                return map;
            }
        }

        public bool IsContextSymbol(int symbol)
        {
            if (!IsContextSensitive || symbol < edges.minIndex)
            {
                return false;
            }

            return contextSymbols.Get(symbol - edges.minIndex);
        }

        public void SetContextSymbol(int symbol)
        {
            Debug.Assert(IsContextSensitive);
            if (symbol < edges.minIndex)
            {
                return;
            }

            contextSymbols.Set(symbol - edges.minIndex);
        }

        public virtual void SetContextSensitive(ATN atn)
        {
            Debug.Assert(!configs.IsOutermostConfigSet);
            if (IsContextSensitive)
            {
                return;
            }

            lock (this)
            {
                if (contextSymbols == null)
                {
                    contextSymbols = new BitSet();
                }
            }
        }

        public virtual DFAState GetTarget(int symbol)
        {
            return edges[symbol];
        }

        public virtual void SetTarget(int symbol, DFAState target)
        {
            edges = edges.Put(symbol, target);
        }

        public virtual DFAState GetContextTarget(int invokingState)
        {
            lock (this)
            {
                if (invokingState == PredictionContext.EmptyFullStateKey)
                {
                    invokingState = -1;
                }

                return contextEdges[invokingState];
            }
        }

        public virtual void SetContextTarget(int invokingState, DFAState target)
        {
            lock (this)
            {
                if (!IsContextSensitive)
                {
                    throw new InvalidOperationException("The state is not context sensitive.");
                }

                if (invokingState == PredictionContext.EmptyFullStateKey)
                {
                    invokingState = -1;
                }

                contextEdges = contextEdges.Put(invokingState, target);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(7, configs.GetHashCode());
        }

        /// <summary>
        ///     Two
        ///     <see cref="DFAState" />
        ///     instances are equal if their ATN configuration sets
        ///     are the same. This method is used to see if a state already exists.
        ///     <p>
        ///         Because the number of alternatives and number of ATN configurations are
        ///         finite, there is a finite number of DFA states that can be processed.
        ///         This is necessary to show that the algorithm terminates.
        ///     </p>
        ///     <p>
        ///         Cannot test the DFA state numbers here because in
        ///         <see cref="Antlr4.Runtime.Atn.ParserATNSimulator.AddDFAState(DFA, Antlr4.Runtime.Atn.ATNConfigSet, Antlr4.Runtime.Atn.PredictionContextCache)" />
        ///         we need to know if any other state
        ///         exists that has this exact set of ATN configurations. The
        ///         <see cref="stateNumber" />
        ///         is irrelevant.
        ///     </p>
        /// </summary>
        public override bool Equals(object o)
        {
            // compare set of ATN configurations in this set with other
            if (this == o)
            {
                return true;
            }

            if (!(o is DFAState))
            {
                return false;
            }

            DFAState other = (DFAState) o;
            bool sameSet = configs.Equals(other.configs);
            //		System.out.println("DFAState.equals: "+configs+(sameSet?"==":"!=")+other.configs);
            return sameSet;
        }

        public override string ToString()
        {
            StringBuilder buf = new();
            buf.Append(stateNumber).Append(":").Append(configs);
            if (IsAcceptState)
            {
                buf.Append("=>");
                if (predicates != null)
                {
                    buf.Append(Arrays.ToString(predicates));
                }
                else
                {
                    buf.Append(Prediction);
                }
            }

            return buf.ToString();
        }

        /// <summary>Map a predicate to a predicted alternative.</summary>
        public class PredPrediction
        {
            public int alt;

            [NotNull] public SemanticContext pred;

            public PredPrediction([NotNull] SemanticContext pred, int alt)
            {
                // never null; at least SemanticContext.NONE
                this.alt = alt;
                this.pred = pred;
            }

            public override string ToString()
            {
                return "(" + pred + ", " + alt + ")";
            }
        }
    }
}