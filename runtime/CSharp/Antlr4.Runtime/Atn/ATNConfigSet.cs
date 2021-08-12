// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Text;

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Atn
{
    /// <author>Sam Harwell</author>
    public class ATNConfigSet : IEnumerable<ATNConfig>
    {
        /// <summary>This is a list of all configs in this set.</summary>
        private readonly List<ATNConfig> configs;

        /// <summary>
        ///     This maps (state, alt) -&gt; merged
        ///     <see cref="ATNConfig" />
        ///     . The key does not account for
        ///     the
        ///     <see cref="ATNConfig.SemanticContext()" />
        ///     of the value, which is only a problem if a single
        ///     <c>ATNConfigSet</c>
        ///     contains two configs with the same state and alternative
        ///     but different semantic contexts. When this case arises, the first config
        ///     added to this map stays, and the remaining configs are placed in
        ///     <see cref="unmerged" />
        ///     .
        ///     <p />
        ///     This map is only used for optimizing the process of adding configs to the set,
        ///     and is
        ///     <see langword="null" />
        ///     for read-only sets stored in the DFA.
        /// </summary>
        private readonly Dictionary<long, ATNConfig> mergedConfigs;

        /// <summary>
        ///     This is an "overflow" list holding configs which cannot be merged with one
        ///     of the configs in
        ///     <see cref="mergedConfigs" />
        ///     but have a colliding key. This
        ///     occurs when two configs in the set have the same state and alternative but
        ///     different semantic contexts.
        ///     <p />
        ///     This list is only used for optimizing the process of adding configs to the set,
        ///     and is
        ///     <see langword="null" />
        ///     for read-only sets stored in the DFA.
        /// </summary>
        private readonly List<ATNConfig> unmerged;

        private int cachedHashCode = -1;

        private ConflictInfo conflictInfo;

        private bool dipsIntoOuterContext;

        private bool hasSemanticContext;

        /// <summary>
        ///     When
        ///     <see langword="true" />
        ///     , this config set represents configurations where the entire
        ///     outer context has been consumed by the ATN interpreter. This prevents the
        ///     <see cref="ParserATNSimulator.Closure(ATNConfigSet, ATNConfigSet, bool, bool, PredictionContextCache, bool)" />
        ///     from pursuing the global FOLLOW when a
        ///     rule stop state is reached with an empty prediction context.
        ///     <p />
        ///     Note:
        ///     <c>outermostConfigSet</c>
        ///     and
        ///     <see cref="dipsIntoOuterContext" />
        ///     should never
        ///     be true at the same time.
        /// </summary>
        private bool outermostConfigSet;

        private int uniqueAlt;

        public ATNConfigSet()
        {
            // Used in parser and lexer. In lexer, it indicates we hit a pred
            // while computing a closure operation.  Don't make a DFA state from this.
            mergedConfigs = new Dictionary<long, ATNConfig>();
            unmerged = new List<ATNConfig>();
            configs = new List<ATNConfig>();
            uniqueAlt = ATN.InvalidAltNumber;
        }

        protected internal ATNConfigSet(ATNConfigSet set, bool @readonly)
        {
            if (@readonly)
            {
                mergedConfigs = null;
                unmerged = null;
            }
            else
            {
                if (!set.IsReadOnly)
                {
                    mergedConfigs = new Dictionary<long, ATNConfig>(set.mergedConfigs);
                    unmerged = new List<ATNConfig>(set.unmerged);
                }
                else
                {
                    mergedConfigs = new Dictionary<long, ATNConfig>(set.configs.Count);
                    unmerged = new List<ATNConfig>();
                }
            }

            configs = new List<ATNConfig>(set.configs);
            dipsIntoOuterContext = set.dipsIntoOuterContext;
            hasSemanticContext = set.hasSemanticContext;
            outermostConfigSet = set.outermostConfigSet;
            if (@readonly || !set.IsReadOnly)
            {
                uniqueAlt = set.uniqueAlt;
                conflictInfo = set.conflictInfo;
            }
        }

        /// <summary>
        ///     Get the set of all alternatives represented by configurations in this
        ///     set.
        /// </summary>
        [NotNull]
        public virtual BitSet RepresentedAlternatives
        {
            get
            {
                // if (!readonly && set.isReadOnly()) -> addAll is called from clone()
                if (conflictInfo != null)
                {
                    return conflictInfo.ConflictedAlts.Clone();
                }

                BitSet alts = new();
                foreach (ATNConfig config in this)
                {
                    alts.Set(config.Alt);
                }

                return alts;
            }
        }

        public bool IsReadOnly => mergedConfigs == null;

        public virtual bool IsOutermostConfigSet
        {
            get => outermostConfigSet;
            set
            {
                bool outermostConfigSet = value;
                if (this.outermostConfigSet && !outermostConfigSet)
                {
                    throw new InvalidOperationException();
                }

                Debug.Assert(!outermostConfigSet || !dipsIntoOuterContext);
                this.outermostConfigSet = outermostConfigSet;
            }
        }

        public virtual HashSet<ATNState> States
        {
            get
            {
                var states = new HashSet<ATNState>();
                foreach (ATNConfig c in configs)
                {
                    states.Add(c.State);
                }

                return states;
            }
        }

        public virtual int Count => configs.Count;

        public virtual int UniqueAlt => uniqueAlt;

        public virtual bool HasSemanticContext => hasSemanticContext;

        public virtual ConflictInfo ConflictInformation
        {
            get => conflictInfo;
            set
            {
                ConflictInfo conflictInfo = value;
                EnsureWritable();
                this.conflictInfo = conflictInfo;
            }
        }

        public virtual BitSet ConflictingAlts
        {
            get
            {
                if (conflictInfo == null)
                {
                    return null;
                }

                return conflictInfo.ConflictedAlts;
            }
        }

        public virtual bool IsExactConflict
        {
            get
            {
                if (conflictInfo == null)
                {
                    return false;
                }

                return conflictInfo.IsExact;
            }
        }

        public virtual bool DipsIntoOuterContext => dipsIntoOuterContext;

        public virtual ATNConfig this[int index] => configs[index];

        public virtual IEnumerator<ATNConfig> GetEnumerator()
        {
            return configs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void OptimizeConfigs(ATNSimulator interpreter)
        {
            if (configs.Count == 0)
            {
                return;
            }

            for (int i = 0;
                i < configs.Count;
                i++)
            {
                ATNConfig config = configs[i];
                config.Context = interpreter.atn.GetCachedContext(config.Context);
            }
        }

        public virtual ATNConfigSet Clone(bool @readonly)
        {
            ATNConfigSet copy = new(this, @readonly);
            if (!@readonly && IsReadOnly)
            {
                copy.AddAll(configs);
            }

            return copy;
        }

        public virtual bool IsEmpty()
        {
            return configs.Count == 0;
        }

        public virtual bool Contains(object o)
        {
            if (!(o is ATNConfig))
            {
                return false;
            }

            ATNConfig config = (ATNConfig) o;
            long configKey = GetKey(config);
            ATNConfig mergedConfig;
            if (mergedConfigs.TryGetValue(configKey, out mergedConfig) && CanMerge(config, configKey, mergedConfig))
            {
                return mergedConfig.Contains(config);
            }

            foreach (ATNConfig c in unmerged)
            {
                if (c.Contains(config))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual object[] ToArray()
        {
            return configs.ToArray();
        }

        public virtual bool Add(ATNConfig e)
        {
            return Add(e, null);
        }

        public virtual bool Add(ATNConfig e, [AllowNull] PredictionContextCache contextCache)
        {
            EnsureWritable();
            Debug.Assert(!outermostConfigSet || !e.ReachesIntoOuterContext);
            if (contextCache == null)
            {
                contextCache = PredictionContextCache.Uncached;
            }

            bool addKey;
            long key = GetKey(e);
            ATNConfig mergedConfig;
            addKey = !mergedConfigs.TryGetValue(key, out mergedConfig);
            if (mergedConfig != null && CanMerge(e, key, mergedConfig))
            {
                mergedConfig.OuterContextDepth = Math.Max(mergedConfig.OuterContextDepth, e.OuterContextDepth);
                if (e.PrecedenceFilterSuppressed)
                {
                    mergedConfig.PrecedenceFilterSuppressed = true;
                }

                PredictionContext joined = PredictionContext.Join(mergedConfig.Context, e.Context, contextCache);
                UpdatePropertiesForMergedConfig(e);
                if (mergedConfig.Context == joined)
                {
                    return false;
                }

                mergedConfig.Context = joined;
                return true;
            }

            for (int i = 0;
                i < unmerged.Count;
                i++)
            {
                ATNConfig unmergedConfig = unmerged[i];
                if (CanMerge(e, key, unmergedConfig))
                {
                    unmergedConfig.OuterContextDepth = Math.Max(unmergedConfig.OuterContextDepth, e.OuterContextDepth);
                    if (e.PrecedenceFilterSuppressed)
                    {
                        unmergedConfig.PrecedenceFilterSuppressed = true;
                    }

                    PredictionContext joined = PredictionContext.Join(unmergedConfig.Context, e.Context, contextCache);
                    UpdatePropertiesForMergedConfig(e);
                    if (unmergedConfig.Context == joined)
                    {
                        return false;
                    }

                    unmergedConfig.Context = joined;
                    if (addKey)
                    {
                        mergedConfigs[key] = unmergedConfig;
                        unmerged.RemoveAt(i);
                    }

                    return true;
                }
            }

            configs.Add(e);
            if (addKey)
            {
                mergedConfigs[key] = e;
            }
            else
            {
                unmerged.Add(e);
            }

            UpdatePropertiesForAddedConfig(e);
            return true;
        }

        private void UpdatePropertiesForMergedConfig(ATNConfig config)
        {
            // merged configs can't change the alt or semantic context
            dipsIntoOuterContext |= config.ReachesIntoOuterContext;
            Debug.Assert(!outermostConfigSet || !dipsIntoOuterContext);
        }

        private void UpdatePropertiesForAddedConfig(ATNConfig config)
        {
            if (configs.Count == 1)
            {
                uniqueAlt = config.Alt;
            }
            else
            {
                if (uniqueAlt != config.Alt)
                {
                    uniqueAlt = ATN.InvalidAltNumber;
                }
            }

            hasSemanticContext |= !SemanticContext.None.Equals(config.SemanticContext);
            dipsIntoOuterContext |= config.ReachesIntoOuterContext;
            Debug.Assert(!outermostConfigSet || !dipsIntoOuterContext);
        }

        protected internal virtual bool CanMerge(ATNConfig left, long leftKey, ATNConfig right)
        {
            if (left.State.stateNumber != right.State.stateNumber)
            {
                return false;
            }

            if (leftKey != GetKey(right))
            {
                return false;
            }

            return left.SemanticContext.Equals(right.SemanticContext);
        }

        protected internal virtual long GetKey(ATNConfig e)
        {
            long key = e.State.stateNumber;
            key = (key << 12) | (e.Alt & 0xFFFL);
            return key;
        }

        public virtual bool Remove(object o)
        {
            EnsureWritable();
            throw new NotSupportedException("Not supported yet.");
        }

        public virtual bool ContainsAll(IEnumerable<ATNConfig> c)
        {
            foreach (ATNConfig o in c)
            {
                if (!Contains(o))
                {
                    return false;
                }
            }

            return true;
        }

        public virtual bool AddAll(IEnumerable<ATNConfig> c)
        {
            return AddAll(c, null);
        }

        public virtual bool AddAll(IEnumerable<ATNConfig> c, PredictionContextCache contextCache)
        {
            EnsureWritable();
            bool changed = false;
            foreach (ATNConfig group in c)
            {
                changed |= Add(group, contextCache);
            }

            return changed;
        }

        public virtual bool RetainAll<_T0>(ICollection<_T0> c)
        {
            EnsureWritable();
            throw new NotSupportedException("Not supported yet.");
        }

        public virtual bool RemoveAll<_T0>(ICollection<_T0> c)
        {
            EnsureWritable();
            throw new NotSupportedException("Not supported yet.");
        }

        public virtual void Clear()
        {
            EnsureWritable();
            mergedConfigs.Clear();
            unmerged.Clear();
            configs.Clear();
            dipsIntoOuterContext = false;
            hasSemanticContext = false;
            uniqueAlt = ATN.InvalidAltNumber;
            conflictInfo = null;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (!(obj is ATNConfigSet))
            {
                return false;
            }

            ATNConfigSet other = (ATNConfigSet) obj;
            return outermostConfigSet == other.outermostConfigSet && Equals(conflictInfo, other.conflictInfo) && configs.SequenceEqual(other.configs);
        }

        public override int GetHashCode()
        {
            if (IsReadOnly && cachedHashCode != -1)
            {
                return cachedHashCode;
            }

            int hashCode = 1;
            hashCode = (5 * hashCode) ^ (outermostConfigSet ? 1 : 0);
            hashCode = (5 * hashCode) ^ SequenceEqualityComparer<ATNConfig>.Default.GetHashCode(configs);
            if (IsReadOnly)
            {
                cachedHashCode = hashCode;
            }

            return hashCode;
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public virtual string ToString(bool showContext)
        {
            StringBuilder buf = new();
            var sortedConfigs = new List<ATNConfig>(configs);
            sortedConfigs.Sort(new _IComparer_451());
            buf.Append("[");
            for (int i = 0;
                i < sortedConfigs.Count;
                i++)
            {
                if (i > 0)
                {
                    buf.Append(", ");
                }

                buf.Append(sortedConfigs[i].ToString(null, true, showContext));
            }

            buf.Append("]");
            if (hasSemanticContext)
            {
                buf.Append(",hasSemanticContext=").Append(hasSemanticContext);
            }

            if (uniqueAlt != ATN.InvalidAltNumber)
            {
                buf.Append(",uniqueAlt=").Append(uniqueAlt);
            }

            if (conflictInfo != null)
            {
                buf.Append(",conflictingAlts=").Append(conflictInfo.ConflictedAlts);
                if (!conflictInfo.IsExact)
                {
                    buf.Append("*");
                }
            }

            if (dipsIntoOuterContext)
            {
                buf.Append(",dipsIntoOuterContext");
            }

            return buf.ToString();
        }

        public virtual void ClearExplicitSemanticContext()
        {
            EnsureWritable();
            hasSemanticContext = false;
        }

        public virtual void MarkExplicitSemanticContext()
        {
            EnsureWritable();
            hasSemanticContext = true;
        }

        public virtual void Remove(int index)
        {
            EnsureWritable();
            ATNConfig config = configs[index];
            configs.Remove(config);
            long key = GetKey(config);
            ATNConfig existing;
            if (mergedConfigs.TryGetValue(key, out existing) && existing == config)
            {
                mergedConfigs.Remove(key);
            }
            else
            {
                for (int i = 0;
                    i < unmerged.Count;
                    i++)
                {
                    if (unmerged[i] == config)
                    {
                        unmerged.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        protected internal void EnsureWritable()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("This ATNConfigSet is read only.");
            }
        }

        private sealed class _IComparer_451 : IComparer<ATNConfig>
        {
            public int Compare(ATNConfig o1, ATNConfig o2)
            {
                if (o1.Alt != o2.Alt)
                {
                    return o1.Alt - o2.Alt;
                }

                if (o1.State.stateNumber != o2.State.stateNumber)
                {
                    return o1.State.stateNumber - o2.State.stateNumber;
                }

                return String.CompareOrdinal(o1.SemanticContext.ToString(), o2.SemanticContext.ToString());
            }
        }
    }
}