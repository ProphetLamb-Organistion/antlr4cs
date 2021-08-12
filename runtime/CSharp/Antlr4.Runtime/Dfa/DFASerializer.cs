// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Dfa
{
    /// <summary>A DFA walker that knows how to dump them to serialized strings.</summary>
    public class DFASerializer
    {
        [MaybeNull] internal readonly ATN atn;

        [NotNull] private readonly DFA dfa;

        [MaybeNull] internal readonly string[] ruleNames;

        [NotNull] private readonly IVocabulary vocabulary;

        [ObsoleteAttribute(@"Use DFASerializer(DFA, Antlr4.Runtime.IVocabulary) instead.")]
        public DFASerializer([NotNull] DFA dfa, [AllowNull] string[] tokenNames)
            : this(dfa, Vocabulary.FromTokenNames(tokenNames), null, null)
        {
        }

        public DFASerializer([NotNull] DFA dfa, [NotNull] IVocabulary vocabulary)
            : this(dfa, vocabulary, null, null)
        {
        }

        public DFASerializer([NotNull] DFA dfa, [AllowNull] IRecognizer parser)
            : this(dfa, parser != null ? parser.Vocabulary : Vocabulary.EmptyVocabulary, parser != null ? parser.RuleNames : null, parser != null ? parser.Atn : null)
        {
        }

        [ObsoleteAttribute(@"Use DFASerializer(DFA, Antlr4.Runtime.IVocabulary, string[], Antlr4.Runtime.Atn.ATN) instead.")]
        public DFASerializer([NotNull] DFA dfa, [AllowNull] string[] tokenNames, [AllowNull] string[] ruleNames, [AllowNull] ATN atn)
            : this(dfa, Vocabulary.FromTokenNames(tokenNames), ruleNames, atn)
        {
        }

        public DFASerializer([NotNull] DFA dfa, [NotNull] IVocabulary vocabulary, [AllowNull] string[] ruleNames, [AllowNull] ATN atn)
        {
            this.dfa = dfa;
            this.vocabulary = vocabulary;
            this.ruleNames = ruleNames;
            this.atn = atn;
        }

        public override string ToString()
        {
            if (dfa.s0.Get() == null)
            {
                return null;
            }

            StringBuilder buf = new();
            if (dfa.states != null)
            {
                var states = new List<DFAState>(dfa.states.Values);
                states.Sort(new _IComparer_79());
                foreach (DFAState s in states)
                {
                    IEnumerable<KeyValuePair<int, DFAState>> edges = s.EdgeMap;
                    IEnumerable<KeyValuePair<int, DFAState>> contextEdges = s.ContextEdgeMap;
                    foreach (KeyValuePair<int, DFAState> entry in edges)
                    {
                        if ((entry.Value == null || entry.Value == ATNSimulator.Error) && !s.IsContextSymbol(entry.Key))
                        {
                            continue;
                        }

                        bool contextSymbol = false;
                        buf.Append(GetStateString(s)).Append("-").Append(GetEdgeLabel(entry.Key)).Append("->");
                        if (s.IsContextSymbol(entry.Key))
                        {
                            buf.Append("!");
                            contextSymbol = true;
                        }

                        DFAState t = entry.Value;
                        if (t != null && t.stateNumber != Int32.MaxValue)
                        {
                            buf.Append(GetStateString(t)).Append('\n');
                        }
                        else
                        {
                            if (contextSymbol)
                            {
                                buf.Append("ctx\n");
                            }
                        }
                    }

                    if (s.IsContextSensitive)
                    {
                        foreach (KeyValuePair<int, DFAState> entry_1 in contextEdges)
                        {
                            buf.Append(GetStateString(s)).Append("-").Append(GetContextLabel(entry_1.Key)).Append("->").Append(GetStateString(entry_1.Value)).Append("\n");
                        }
                    }
                }
            }

            string output = buf.ToString();
            if (output.Length == 0)
            {
                return null;
            }

            //return Utils.sortLinesInString(output);
            return output;
        }

        protected internal virtual string GetContextLabel(int i)
        {
            if (i == PredictionContext.EmptyFullStateKey)
            {
                return "ctx:EMPTY_FULL";
            }

            if (i == PredictionContext.EmptyLocalStateKey)
            {
                return "ctx:EMPTY_LOCAL";
            }

            if (atn != null && i > 0 && i <= atn.states.Count)
            {
                ATNState state = atn.states[i];
                int ruleIndex = state.ruleIndex;
                if (ruleNames != null && ruleIndex >= 0 && ruleIndex < ruleNames.Length)
                {
                    return "ctx:" + i + "(" + ruleNames[ruleIndex] + ")";
                }
            }

            return "ctx:" + i;
        }

        protected internal virtual string GetEdgeLabel(int i)
        {
            return vocabulary.GetDisplayName(i);
        }

        internal virtual string GetStateString(DFAState s)
        {
            if (s == ATNSimulator.Error)
            {
                return "ERROR";
            }

            int n = s.stateNumber;
            string stateStr = "s" + n;
            if (s.IsAcceptState)
            {
                if (s.predicates != null)
                {
                    stateStr = ":s" + n + "=>" + Arrays.ToString(s.predicates);
                }
                else
                {
                    stateStr = ":s" + n + "=>" + s.Prediction;
                }
            }

            if (s.IsContextSensitive)
            {
                stateStr += "*";
                foreach (ATNConfig config in s.configs)
                {
                    if (config.ReachesIntoOuterContext)
                    {
                        stateStr += "*";
                        break;
                    }
                }
            }

            return stateStr;
        }

        private sealed class _IComparer_79 : IComparer<DFAState>
        {
            public int Compare(DFAState o1, DFAState o2)
            {
                return o1.stateNumber - o2.stateNumber;
            }
        }
    }
}