﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReactiveXaml.Tests
{
    public static class EnumerableTestMixin
    {
        public static void AssertAreEqual<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            var left = lhs.ToArray();
            var right = rhs.ToArray();

            try {
                Assert.AreEqual(left.Length, right.Length);
                for (int i = 0; i < left.Length; i++) {
                    Assert.AreEqual(left[i], right[i]);
                }
            } catch {
                Console.Error.WriteLine("lhs: [{0}]",
                    String.Join(",", lhs.ToArray()));
                Console.Error.WriteLine("rhs: [{0}]",
                    String.Join(",", rhs.ToArray()));
                throw;
            }
        }
    }
}