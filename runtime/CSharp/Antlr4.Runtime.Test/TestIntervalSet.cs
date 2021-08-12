// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

using Antlr4.Runtime.Utility;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Antlr4.Runtime.Test
{
    [TestClass]
    public class TestIntervalSet
    {
        [TestMethod]
        public void TestSingleElement()
        {
            IntervalSet s = IntervalSet.Of(99);
            string expecting = "99";
            Assert.AreEqual(s.ToString(), expecting);
        }

        [TestMethod]
        public void TestIsolatedElements()
        {
            IntervalSet s = new();
            s.Add(1);
            s.Add('z');
            s.Add('\uFFF0');
            string expecting = "{1, 122, 65520}";
            Assert.AreEqual(s.ToString(), expecting);
        }

        [TestMethod]
        public void TestMixedRangesAndElements()
        {
            IntervalSet s = new();
            s.Add(1);
            s.Add('a', 'z');
            s.Add('0', '9');
            string expecting = "{1, 48..57, 97..122}";
            Assert.AreEqual(s.ToString(), expecting);
        }

        [TestMethod]
        public void TestSimpleAnd()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(13, 15);
            string expecting = "{13..15}";
            string result = s.And(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestRangeAndIsolatedElement()
        {
            IntervalSet s = IntervalSet.Of('a', 'z');
            IntervalSet s2 = IntervalSet.Of('d');
            string expecting = "100";
            string result = s.And(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestEmptyIntersection()
        {
            IntervalSet s = IntervalSet.Of('a', 'z');
            IntervalSet s2 = IntervalSet.Of('0', '9');
            string expecting = "{}";
            string result = s.And(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestEmptyIntersectionSingleElements()
        {
            IntervalSet s = IntervalSet.Of('a');
            IntervalSet s2 = IntervalSet.Of('d');
            string expecting = "{}";
            string result = s.And(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestNotSingleElement()
        {
            IntervalSet vocabulary = IntervalSet.Of(1, 1000);
            vocabulary.Add(2000, 3000);
            IntervalSet s = IntervalSet.Of(50, 50);
            string expecting = "{1..49, 51..1000, 2000..3000}";
            string result = s.Complement(vocabulary).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestNotSet()
        {
            IntervalSet vocabulary = IntervalSet.Of(1, 1000);
            IntervalSet s = IntervalSet.Of(50, 60);
            s.Add(5);
            s.Add(250, 300);
            string expecting = "{1..4, 6..49, 61..249, 301..1000}";
            string result = s.Complement(vocabulary).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestNotEqualSet()
        {
            IntervalSet vocabulary = IntervalSet.Of(1, 1000);
            IntervalSet s = IntervalSet.Of(1, 1000);
            string expecting = "{}";
            string result = s.Complement(vocabulary).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestNotSetEdgeElement()
        {
            IntervalSet vocabulary = IntervalSet.Of(1, 2);
            IntervalSet s = IntervalSet.Of(1);
            string expecting = "2";
            string result = s.Complement(vocabulary).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestNotSetFragmentedVocabulary()
        {
            IntervalSet vocabulary = IntervalSet.Of(1, 255);
            vocabulary.Add(1000, 2000);
            vocabulary.Add(9999);
            IntervalSet s = IntervalSet.Of(50, 60);
            s.Add(3);
            s.Add(250, 300);
            s.Add(10000); // this is outside range of vocab and should be ignored
            string expecting = "{1..2, 4..49, 61..249, 1000..2000, 9999}";
            string result = s.Complement(vocabulary).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSubtractOfCompletelyContainedRange()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(12, 15);
            string expecting = "{10..11, 16..20}";
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSubtractOfOverlappingRangeFromLeft()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(5, 11);
            string expecting = "{12..20}";
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);

            IntervalSet s3 = IntervalSet.Of(5, 10);
            expecting = "{11..20}";
            result = s.Subtract(s3).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSubtractOfOverlappingRangeFromRight()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(15, 25);
            string expecting = "{10..14}";
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);

            IntervalSet s3 = IntervalSet.Of(20, 25);
            expecting = "{10..19}";
            result = s.Subtract(s3).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSubtractOfCompletelyCoveredRange()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(1, 25);
            string expecting = "{}";
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSubtractOfRangeSpanningMultipleRanges()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            s.Add(30, 40);
            s.Add(50, 60); // s has 3 ranges now: 10..20, 30..40, 50..60
            IntervalSet s2 = IntervalSet.Of(5, 55); // covers one and touches 2nd range
            string expecting = "{56..60}";
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);

            IntervalSet s3 = IntervalSet.Of(15, 55); // touches both
            expecting = "{10..14, 56..60}";
            result = s.Subtract(s3).ToString();
            Assert.AreEqual(expecting, result);
        }

        /**
         * The following was broken:
         * {0..113, 115..65534}-{0..115, 117..65534}=116..65534
         */
        [TestMethod]
        public void TestSubtractOfWackyRange()
        {
            IntervalSet s = IntervalSet.Of(0, 113);
            s.Add(115, 200);
            IntervalSet s2 = IntervalSet.Of(0, 115);
            s2.Add(117, 200);
            string expecting = "116";
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSimpleEquals()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(10, 20);
            Assert.AreEqual(s, s2);

            IntervalSet s3 = IntervalSet.Of(15, 55);
            Assert.AreNotEqual(s, s3);
        }

        [TestMethod]
        public void TestEquals()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            s.Add(2);
            s.Add(499, 501);
            IntervalSet s2 = IntervalSet.Of(10, 20);
            s2.Add(2);
            s2.Add(499, 501);
            Assert.AreEqual(s, s2);

            IntervalSet s3 = IntervalSet.Of(10, 20);
            s3.Add(2);
            Assert.AreNotEqual(s, s3);
        }

        [TestMethod]
        public void TestSingleElementMinusDisjointSet()
        {
            IntervalSet s = IntervalSet.Of(15, 15);
            IntervalSet s2 = IntervalSet.Of(1, 5);
            s2.Add(10, 20);
            string expecting = "{}"; // 15 - {1..5, 10..20} = {}
            string result = s.Subtract(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestMembership()
        {
            IntervalSet s = IntervalSet.Of(15, 15);
            s.Add(50, 60);
            Assert.IsFalse(s.Contains(0));
            Assert.IsFalse(s.Contains(20));
            Assert.IsFalse(s.Contains(100));
            Assert.IsTrue(s.Contains(15));
            Assert.IsTrue(s.Contains(55));
            Assert.IsTrue(s.Contains(50));
            Assert.IsTrue(s.Contains(60));
        }

        // {2,15,18} & 10..20
        [TestMethod]
        public void TestIntersectionWithTwoContainedElements()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(2, 2);
            s2.Add(15);
            s2.Add(18);
            string expecting = "{15, 18}";
            string result = s.And(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestIntersectionWithTwoContainedElementsReversed()
        {
            IntervalSet s = IntervalSet.Of(10, 20);
            IntervalSet s2 = IntervalSet.Of(2, 2);
            s2.Add(15);
            s2.Add(18);
            string expecting = "{15, 18}";
            string result = s2.And(s).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestComplement()
        {
            IntervalSet s = IntervalSet.Of(100, 100);
            s.Add(101, 101);
            IntervalSet s2 = IntervalSet.Of(100, 102);
            string expecting = "102";
            string result = s.Complement(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestComplement2()
        {
            IntervalSet s = IntervalSet.Of(100, 101);
            IntervalSet s2 = IntervalSet.Of(100, 102);
            string expecting = "102";
            string result = s.Complement(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestComplement3()
        {
            IntervalSet s = IntervalSet.Of(1, 96);
            s.Add(99, Lexer.MaxCharValue);
            string expecting = "{97..98}";
            string result = s.Complement(1, Lexer.MaxCharValue).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestMergeOfRangesAndSingleValues()
        {
            // {0..41, 42, 43..65534}
            IntervalSet s = IntervalSet.Of(0, 41);
            s.Add(42);
            s.Add(43, 65534);
            string expecting = "{0..65534}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestMergeOfRangesAndSingleValuesReverse()
        {
            IntervalSet s = IntervalSet.Of(43, 65534);
            s.Add(42);
            s.Add(0, 41);
            string expecting = "{0..65534}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestMergeWhereAdditionMergesTwoExistingIntervals()
        {
            // 42, 10, {0..9, 11..41, 43..65534}
            IntervalSet s = IntervalSet.Of(42);
            s.Add(10);
            s.Add(0, 9);
            s.Add(43, 65534);
            s.Add(11, 41);
            string expecting = "{0..65534}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        /**
         * This case is responsible for antlr/antlr4#153.
         * https://github.com/antlr/antlr4/issues/153
         */
        [TestMethod]
        public void TestMergeWhereAdditionMergesThreeExistingIntervals()
        {
            IntervalSet s = new();
            s.Add(0);
            s.Add(3);
            s.Add(5);
            s.Add(0, 7);
            string expecting = "{0..7}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestMergeWithDoubleOverlap()
        {
            IntervalSet s = IntervalSet.Of(1, 10);
            s.Add(20, 30);
            s.Add(5, 25); // overlaps two!
            string expecting = "{1..30}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestSize()
        {
            IntervalSet s = IntervalSet.Of(20, 30);
            s.Add(50, 55);
            s.Add(5, 19);
            string expecting = "32";
            string result = s.Count.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestToList()
        {
            IntervalSet s = IntervalSet.Of(20, 25);
            s.Add(50, 55);
            s.Add(5, 5);
            int[] expecting = {5, 20, 21, 22, 23, 24, 25, 50, 51, 52, 53, 54, 55};
            IList<int> result = s.ToList();
            CollectionAssert.AreEquivalent(expecting, result.ToArray());
        }

        /**
         * The following was broken:
         * {'\u0000'..'s', 'u'..'\uFFFE'} & {'\u0000'..'q', 's'..'\uFFFE'}=
         * {'\u0000'..'q', 's'}!!!! broken...
         * 'q' is 113 ascii
         * 'u' is 117
         */
        [TestMethod]
        public void TestNotRIntersectionNotT()
        {
            IntervalSet s = IntervalSet.Of(0, 's');
            s.Add('u', 200);
            IntervalSet s2 = IntervalSet.Of(0, 'q');
            s2.Add('s', 200);
            string expecting = "{0..113, 115, 117..200}";
            string result = s.And(s2).ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestRmSingleElement()
        {
            IntervalSet s = IntervalSet.Of(1, 10);
            s.Add(-3, -3);
            s.Remove(-3);
            string expecting = "{1..10}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestRmLeftSide()
        {
            IntervalSet s = IntervalSet.Of(1, 10);
            s.Add(-3, -3);
            s.Remove(1);
            string expecting = "{-3, 2..10}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestRmRightSide()
        {
            IntervalSet s = IntervalSet.Of(1, 10);
            s.Add(-3, -3);
            s.Remove(10);
            string expecting = "{-3, 1..9}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }

        [TestMethod]
        public void TestRmMiddleRange()
        {
            IntervalSet s = IntervalSet.Of(1, 10);
            s.Add(-3, -3);
            s.Remove(5);
            string expecting = "{-3, 1..4, 6..10}";
            string result = s.ToString();
            Assert.AreEqual(expecting, result);
        }
    }
}