// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Text;

namespace Antlr4.Runtime.Utility
{
    /// <summary>
    ///     This class implements the
    ///     <see cref="IIntSet" />
    ///     backed by a sorted array of
    ///     non-overlapping intervals. It is particularly efficient for representing
    ///     large collections of numbers, where the majority of elements appear as part
    ///     of a sequential range of numbers that are all part of the set. For example,
    ///     the set { 1, 2, 3, 4, 7, 8 } may be represented as { [1, 4], [7, 8] }.
    ///     <p>
    ///         This class is able to represent sets containing any combination of values in
    ///         the range
    ///         <see cref="int.MinValue" />
    ///         to
    ///         <see cref="int.MaxValue" />
    ///         (inclusive).
    ///     </p>
    /// </summary>
    public class IntervalSet : IIntSet
    {
        public static readonly IntervalSet CompleteCharSet = Of(Lexer.MinCharValue, Lexer.MaxCharValue);

        public static readonly IntervalSet EmptySet = new();

        /// <summary>The list of sorted, disjoint intervals.</summary>
        protected internal IList<Interval> intervals;

        protected internal bool @readonly;

        static IntervalSet()
        {
            CompleteCharSet.SetReadonly(true);
            EmptySet.SetReadonly(true);
        }

        public IntervalSet(IList<Interval> intervals)
        {
            this.intervals = intervals;
        }

        public IntervalSet(IntervalSet set)
            : this()
        {
            AddAll(set);
        }

        public IntervalSet(params int[] els)
        {
            if (els == null)
            {
                intervals = new List<Interval>(2);
            }
            else
            {
                // most sets are 1 or 2 elements
                intervals = new List<Interval>(els.Length);
                foreach (int e in els)
                {
                    Add(e);
                }
            }
        }

        /// <summary>Returns the maximum value contained in the set.</summary>
        /// <returns>
        ///     the maximum value contained in the set. If the set is empty, this
        ///     method returns
        ///     <see cref="TokenConstants.InvalidType" />
        ///     .
        /// </returns>
        public virtual int MaxElement
        {
            get
            {
                if (IsNil)
                {
                    return TokenConstants.InvalidType;
                }

                Interval last = intervals[intervals.Count - 1];
                return last.b;
            }
        }

        /// <summary>Returns the minimum value contained in the set.</summary>
        /// <returns>
        ///     the minimum value contained in the set. If the set is empty, this
        ///     method returns
        ///     <see cref="TokenConstants.InvalidType" />
        ///     .
        /// </returns>
        public virtual int MinElement
        {
            get
            {
                if (IsNil)
                {
                    return TokenConstants.InvalidType;
                }

                return intervals[0].a;
            }
        }

        public virtual bool IsReadOnly =>
            // add [x+1..b]
            @readonly;

        /// <summary>Add a single element to the set.</summary>
        /// <remarks>
        ///     Add a single element to the set.  An isolated element is stored
        ///     as a range el..el.
        /// </remarks>
        public virtual void Add(int el)
        {
            if (@readonly)
            {
                throw new InvalidOperationException("can't alter readonly IntervalSet");
            }

            Add(el, el);
        }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        public virtual bool Contains(int el)
        {
            int n = intervals.Count;
            for (int i = 0;
                i < n;
                i++)
            {
                Interval I = intervals[i];
                int a = I.a;
                int b = I.b;
                if (el < a)
                {
                    break;
                }

                // list is sorted and el is before this interval; not here
                if (el >= a && el <= b)
                {
                    return true;
                }
            }

            // found in this interval
            return false;
        }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        public virtual bool IsNil =>
            /*
		for (ListIterator iter = intervals.listIterator(); iter.hasNext();) {
            Interval I = (Interval) iter.next();
            if ( el<I.a ) {
                break; // list is sorted and el is before this interval; not here
            }
            if ( el>=I.a && el<=I.b ) {
                return true; // found in this interval
            }
        }
        return false;
        */
            intervals == null || intervals.Count == 0;

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        public virtual int SingleElement
        {
            get
            {
                if (intervals != null && intervals.Count == 1)
                {
                    Interval I = intervals[0];
                    if (I.a == I.b)
                    {
                        return I.a;
                    }
                }

                return TokenConstants.InvalidType;
            }
        }

        /// <summary>
        ///     Are two IntervalSets equal?  Because all intervals are sorted
        ///     and disjoint, equals is a simple linear walk over both lists
        ///     to make sure they are the same.
        /// </summary>
        /// <remarks>
        ///     Are two IntervalSets equal?  Because all intervals are sorted
        ///     and disjoint, equals is a simple linear walk over both lists
        ///     to make sure they are the same.  Interval.equals() is used
        ///     by the List.equals() method to check the ranges.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is IntervalSet))
            {
                return false;
            }

            IntervalSet other = (IntervalSet) obj;
            return intervals.SequenceEqual(other.intervals);
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public virtual int Count
        {
            get
            {
                int n = 0;
                int numIntervals = intervals.Count;
                if (numIntervals == 1)
                {
                    Interval firstInterval = intervals[0];
                    return firstInterval.b - firstInterval.a + 1;
                }

                for (int i = 0;
                    i < numIntervals;
                    i++)
                {
                    Interval I = intervals[i];
                    n += I.b - I.a + 1;
                }

                return n;
            }
        }

        public virtual IList<int> ToList()
        {
            IList<int> values = new List<int>();
            int n = intervals.Count;
            for (int i = 0;
                i < n;
                i++)
            {
                Interval I = intervals[i];
                int a = I.a;
                int b = I.b;
                for (int v = a;
                    v <= b;
                    v++)
                {
                    values.Add(v);
                }
            }

            return values;
        }

        public virtual void Remove(int el)
        {
            if (@readonly)
            {
                throw new InvalidOperationException("can't alter readonly IntervalSet");
            }

            int n = intervals.Count;
            for (int i = 0;
                i < n;
                i++)
            {
                Interval I = intervals[i];
                int a = I.a;
                int b = I.b;
                if (el < a)
                {
                    break;
                }

                // list is sorted and el is before this interval; not here
                // if whole interval x..x, rm
                if (el == a && el == b)
                {
                    intervals.RemoveAt(i);
                    break;
                }

                // if on left edge x..b, adjust left
                if (el == a)
                {
                    intervals[i] = Interval.Of(I.a + 1, I.b);
                    break;
                }

                // if on right edge a..x, adjust right
                if (el == b)
                {
                    intervals[i] = Interval.Of(I.a, I.b - 1);
                    break;
                }

                // if in middle a..x..b, split interval
                if (el > a && el < b)
                {
                    // found in this interval
                    int oldb = I.b;
                    intervals[i] = Interval.Of(I.a, el - 1);
                    // [a..x-1]
                    Add(el + 1, oldb);
                }
            }
        }

        IIntSet IIntSet.AddAll(IIntSet set)
        {
            return AddAll(set);
        }

        IIntSet IIntSet.And(IIntSet a)
        {
            return And(a);
        }

        IIntSet IIntSet.Complement(IIntSet elements)
        {
            return Complement(elements);
        }

        IIntSet IIntSet.Or(IIntSet a)
        {
            return Or(a);
        }

        IIntSet IIntSet.Subtract(IIntSet a)
        {
            return Subtract(a);
        }

        /// <summary>Create a set with a single element, el.</summary>
        [return: NotNull]
        public static IntervalSet Of(int a)
        {
            IntervalSet s = new();
            s.Add(a);
            return s;
        }

        /// <summary>Create a set with all ints within range [a..b] (inclusive)</summary>
        public static IntervalSet Of(int a, int b)
        {
            IntervalSet s = new();
            s.Add(a, b);
            return s;
        }

        public virtual void Clear()
        {
            if (@readonly)
            {
                throw new InvalidOperationException("can't alter readonly IntervalSet");
            }

            intervals.Clear();
        }

        /// <summary>Add interval; i.e., add all integers from a to b to set.</summary>
        /// <remarks>
        ///     Add interval; i.e., add all integers from a to b to set.
        ///     If b&lt;a, do nothing.
        ///     Keep list in sorted order (by left range value).
        ///     If overlap, combine ranges.  For example,
        ///     If this is {1..5, 10..20}, adding 6..7 yields
        ///     {1..5, 6..7, 10..20}.  Adding 4..8 yields {1..8, 10..20}.
        /// </remarks>
        public virtual void Add(int a, int b)
        {
            Add(Interval.Of(a, b));
        }

        // copy on write so we can cache a..a intervals and sets of that
        protected internal virtual void Add(Interval addition)
        {
            if (@readonly)
            {
                throw new InvalidOperationException("can't alter readonly IntervalSet");
            }

            //System.out.println("add "+addition+" to "+intervals.toString());
            if (addition.b < addition.a)
            {
                return;
            }

            // find position in list
            // Use iterators as we modify list in place
            for (int i = 0;
                i < intervals.Count;
                i++)
            {
                Interval r = intervals[i];
                if (addition.Equals(r))
                {
                    return;
                }

                if (addition.Adjacent(r) || !addition.Disjoint(r))
                {
                    // next to each other, make a single larger interval
                    Interval bigger = addition.Union(r);
                    intervals[i] = bigger;
                    // make sure we didn't just create an interval that
                    // should be merged with next interval in list
                    while (i < intervals.Count - 1)
                    {
                        i++;
                        Interval next = intervals[i];
                        if (!bigger.Adjacent(next) && bigger.Disjoint(next))
                        {
                            break;
                        }

                        // if we bump up against or overlap next, merge
                        intervals.RemoveAt(i);
                        // remove this one
                        i--;
                        // move backwards to what we just set
                        intervals[i] = bigger.Union(next);
                        // set to 3 merged ones
                    }

                    // first call to next after previous duplicates the result
                    return;
                }

                if (addition.StartsBeforeDisjoint(r))
                {
                    // insert before r
                    intervals.Insert(i, addition);
                    return;
                }
            }

            // if disjoint and after r, a future iteration will handle it
            // ok, must be after last interval (and disjoint from last interval)
            // just add it
            intervals.Add(addition);
        }

        /// <summary>combine all sets in the array returned the or'd value</summary>
        public static IntervalSet Or(IntervalSet[] sets)
        {
            IntervalSet r = new();
            foreach (IntervalSet s in sets)
            {
                r.AddAll(s);
            }

            return r;
        }

        public virtual IntervalSet AddAll(IIntSet set)
        {
            if (set == null)
            {
                return this;
            }

            if (set is IntervalSet)
            {
                IntervalSet other = (IntervalSet) set;
                // walk set and add each interval
                int n = other.intervals.Count;
                for (int i = 0;
                    i < n;
                    i++)
                {
                    Interval I = other.intervals[i];
                    Add(I.a, I.b);
                }
            }
            else
            {
                foreach (int value in set.ToList())
                {
                    Add(value);
                }
            }

            return this;
        }

        public virtual IntervalSet Complement(int minElement, int maxElement)
        {
            return Complement(Of(minElement, maxElement));
        }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        public virtual IntervalSet Complement(IIntSet vocabulary)
        {
            if (vocabulary == null || vocabulary.IsNil)
            {
                return null;
            }

            // nothing in common with null set
            IntervalSet vocabularyIS;
            if (vocabulary is IntervalSet)
            {
                vocabularyIS = (IntervalSet) vocabulary;
            }
            else
            {
                vocabularyIS = new IntervalSet();
                vocabularyIS.AddAll(vocabulary);
            }

            return vocabularyIS.Subtract(this);
        }

        public virtual IntervalSet Subtract(IIntSet a)
        {
            if (a == null || a.IsNil)
            {
                return new IntervalSet(this);
            }

            if (a is IntervalSet)
            {
                return Subtract(this, (IntervalSet) a);
            }

            IntervalSet other = new();
            other.AddAll(a);
            return Subtract(this, other);
        }

        /// <summary>Compute the set difference between two interval sets.</summary>
        /// <remarks>
        ///     Compute the set difference between two interval sets. The specific
        ///     operation is
        ///     <c>left - right</c>
        ///     . If either of the input sets is
        ///     <see langword="null" />
        ///     , it is treated as though it was an empty set.
        /// </remarks>
        [return: NotNull]
        public static IntervalSet Subtract([AllowNull] IntervalSet left, [AllowNull] IntervalSet right)
        {
            if (left == null || left.IsNil)
            {
                return new IntervalSet();
            }

            IntervalSet result = new(left);
            if (right == null || right.IsNil)
            {
                // right set has no elements; just return the copy of the current set
                return result;
            }

            int resultI = 0;
            int rightI = 0;
            while (resultI < result.intervals.Count && rightI < right.intervals.Count)
            {
                Interval resultInterval = result.intervals[resultI];
                Interval rightInterval = right.intervals[rightI];
                // operation: (resultInterval - rightInterval) and update indexes
                if (rightInterval.b < resultInterval.a)
                {
                    rightI++;
                    continue;
                }

                if (rightInterval.a > resultInterval.b)
                {
                    resultI++;
                    continue;
                }

                Interval? beforeCurrent = null;
                Interval? afterCurrent = null;
                if (rightInterval.a > resultInterval.a)
                {
                    beforeCurrent = new Interval(resultInterval.a, rightInterval.a - 1);
                }

                if (rightInterval.b < resultInterval.b)
                {
                    afterCurrent = new Interval(rightInterval.b + 1, resultInterval.b);
                }

                if (beforeCurrent != null)
                {
                    if (afterCurrent != null)
                    {
                        // split the current interval into two
                        result.intervals[resultI] = beforeCurrent.Value;
                        result.intervals.Insert(resultI + 1, afterCurrent.Value);
                        resultI++;
                        rightI++;
                        continue;
                    }

                    // replace the current interval
                    result.intervals[resultI] = beforeCurrent.Value;
                    resultI++;
                    continue;
                }

                if (afterCurrent != null)
                {
                    // replace the current interval
                    result.intervals[resultI] = afterCurrent.Value;
                    rightI++;
                    continue;
                }

                // remove the current interval (thus no need to increment resultI)
                result.intervals.RemoveAt(resultI);
            }

            // If rightI reached right.intervals.size(), no more intervals to subtract from result.
            // If resultI reached result.intervals.size(), we would be subtracting from an empty set.
            // Either way, we are done.
            return result;
        }

        public virtual IntervalSet Or(IIntSet a)
        {
            IntervalSet o = new();
            o.AddAll(this);
            o.AddAll(a);
            return o;
        }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        public virtual IntervalSet And(IIntSet other)
        {
            if (other == null)
            {
                //|| !(other instanceof IntervalSet) ) {
                return null;
            }

            // nothing in common with null set
            IList<Interval> myIntervals = intervals;
            IList<Interval> theirIntervals = ((IntervalSet) other).intervals;
            IntervalSet intersection = null;
            int mySize = myIntervals.Count;
            int theirSize = theirIntervals.Count;
            int i = 0;
            int j = 0;
            // iterate down both interval lists looking for nondisjoint intervals
            while (i < mySize && j < theirSize)
            {
                Interval mine = myIntervals[i];
                Interval theirs = theirIntervals[j];
                //System.out.println("mine="+mine+" and theirs="+theirs);
                if (mine.StartsBeforeDisjoint(theirs))
                {
                    // move this iterator looking for interval that might overlap
                    i++;
                }
                else
                {
                    if (theirs.StartsBeforeDisjoint(mine))
                    {
                        // move other iterator looking for interval that might overlap
                        j++;
                    }
                    else
                    {
                        if (mine.ProperlyContains(theirs))
                        {
                            // overlap, add intersection, get next theirs
                            if (intersection == null)
                            {
                                intersection = new IntervalSet();
                            }

                            intersection.Add(mine.Intersection(theirs));
                            j++;
                        }
                        else
                        {
                            if (theirs.ProperlyContains(mine))
                            {
                                // overlap, add intersection, get next mine
                                if (intersection == null)
                                {
                                    intersection = new IntervalSet();
                                }

                                intersection.Add(mine.Intersection(theirs));
                                i++;
                            }
                            else
                            {
                                if (!mine.Disjoint(theirs))
                                {
                                    // overlap, add intersection
                                    if (intersection == null)
                                    {
                                        intersection = new IntervalSet();
                                    }

                                    intersection.Add(mine.Intersection(theirs));
                                    // Move the iterator of lower range [a..b], but not
                                    // the upper range as it may contain elements that will collide
                                    // with the next iterator. So, if mine=[0..115] and
                                    // theirs=[115..200], then intersection is 115 and move mine
                                    // but not theirs as theirs may collide with the next range
                                    // in thisIter.
                                    // move both iterators to next ranges
                                    if (mine.StartsAfterNonDisjoint(theirs))
                                    {
                                        j++;
                                    }
                                    else
                                    {
                                        if (theirs.StartsAfterNonDisjoint(mine))
                                        {
                                            i++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (intersection == null)
            {
                return new IntervalSet();
            }

            return intersection;
        }

        /// <summary>Return a list of Interval objects.</summary>
        public virtual IList<Interval> GetIntervals()
        {
            return intervals;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (Interval I in intervals)
            {
                hash = HashCode.Combine(hash, I.a, I.b);
            }
            return hash;
        }

        public virtual string ToString(bool elemAreChar)
        {
            StringBuilder buf = new();
            if (intervals == null || intervals.Count == 0)
            {
                return "{}";
            }

            if (Count > 1)
            {
                buf.Append("{");
            }

            bool first = true;
            foreach (Interval I in intervals)
            {
                if (!first)
                {
                    buf.Append(", ");
                }

                first = false;
                int a = I.a;
                int b = I.b;
                if (a == b)
                {
                    if (a == TokenConstants.Eof)
                    {
                        buf.Append("<EOF>");
                    }
                    else
                    {
                        if (elemAreChar)
                        {
                            buf.Append("'").Append((char) a).Append("'");
                        }
                        else
                        {
                            buf.Append(a);
                        }
                    }
                }
                else
                {
                    if (elemAreChar)
                    {
                        buf.Append("'").Append((char) a).Append("'..'").Append((char) b).Append("'");
                    }
                    else
                    {
                        buf.Append(a).Append("..").Append(b);
                    }
                }
            }

            if (Count > 1)
            {
                buf.Append("}");
            }

            return buf.ToString();
        }

        [Obsolete(@"Use ToString(Antlr4.Runtime.IVocabulary) instead.")]
        public virtual string ToString(string[] tokenNames)
        {
            return ToString(Vocabulary.FromTokenNames(tokenNames));
        }

        public virtual string ToString([NotNull] IVocabulary vocabulary)
        {
            StringBuilder buf = new();
            if (intervals == null || intervals.Count == 0)
            {
                return "{}";
            }

            if (Count > 1)
            {
                buf.Append("{");
            }

            bool first = true;
            foreach (Interval I in intervals)
            {
                if (!first)
                {
                    buf.Append(", ");
                }

                first = false;
                int a = I.a;
                int b = I.b;
                if (a == b)
                {
                    buf.Append(ElementName(vocabulary, a));
                }
                else
                {
                    for (int i = a;
                        i <= b;
                        i++)
                    {
                        if (i > a)
                        {
                            buf.Append(", ");
                        }

                        buf.Append(ElementName(vocabulary, i));
                    }
                }
            }

            if (Count > 1)
            {
                buf.Append("}");
            }

            return buf.ToString();
        }

        [Obsolete(@"Use ElementName(Antlr4.Runtime.IVocabulary, int) instead.")]
        protected internal virtual string ElementName(string[] tokenNames, int a)
        {
            return ElementName(Vocabulary.FromTokenNames(tokenNames), a);
        }

        [return: NotNull]
        protected internal virtual string ElementName([NotNull] IVocabulary vocabulary, int a)
        {
            if (a == TokenConstants.Eof)
            {
                return "<EOF>";
            }

            if (a == TokenConstants.Epsilon)
            {
                return "<EPSILON>";
            }

            return vocabulary.GetDisplayName(a);
        }

        public virtual List<int> ToIntegerList()
        {
            var values = new List<int>(Count);
            int n = intervals.Count;
            for (int i = 0;
                i < n;
                i++)
            {
                Interval I = intervals[i];
                int a = I.a;
                int b = I.b;
                for (int v = a;
                    v <= b;
                    v++)
                {
                    values.Add(v);
                }
            }

            return values;
        }

        public virtual HashSet<int> ToSet()
        {
            var s = new HashSet<int>();
            foreach (Interval I in intervals)
            {
                int a = I.a;
                int b = I.b;
                for (int v = a;
                    v <= b;
                    v++)
                {
                    s.Add(v);
                }
            }

            return s;
        }

        public virtual int[] ToArray()
        {
            return ToIntegerList().ToArray();
        }

        public virtual void SetReadonly(bool @readonly)
        {
            if (this.@readonly && !@readonly)
            {
                throw new InvalidOperationException("can't alter readonly IntervalSet");
            }

            this.@readonly = @readonly;
        }
    }
}