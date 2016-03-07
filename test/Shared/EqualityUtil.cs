// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.DotNet.Utilities
{
    public static class EqualityUnit
    {
        public static EqualityUnit<T> Create<T>(T value)
        {
            return new EqualityUnit<T>(value);
        }
    }

    public sealed class EqualityUnit<T>
    {
        private static readonly ReadOnlyCollection<T> s_emptyCollection = new ReadOnlyCollection<T>(new T[] { });

        public readonly T Value;
        public readonly ReadOnlyCollection<T> EqualValues;
        public readonly ReadOnlyCollection<T> NotEqualValues;
        public IEnumerable<T> AllValues
        {
            get { return Enumerable.Repeat(Value, 1).Concat(EqualValues).Concat(NotEqualValues); }
        }

        public EqualityUnit(T value)
        {
            Value = value;
            EqualValues = s_emptyCollection;
            NotEqualValues = s_emptyCollection;
        }

        public EqualityUnit(
            T value,
            ReadOnlyCollection<T> equalValues,
            ReadOnlyCollection<T> notEqualValues)
        {
            Value = value;
            EqualValues = equalValues;
            NotEqualValues = notEqualValues;
        }

        public EqualityUnit<T> WithEqualValues(params T[] equalValues)
        {
            return new EqualityUnit<T>(
                Value,
                EqualValues.Concat(equalValues).ToList().AsReadOnly(),
                NotEqualValues);
        }

        public EqualityUnit<T> WithNotEqualValues(params T[] notEqualValues)
        {
            return new EqualityUnit<T>(
                Value,
                EqualValues,
                NotEqualValues.Concat(notEqualValues).ToList().AsReadOnly());
        }

        public void RunAll(
            bool checkIEquatable = true,
            Func<T, T, bool> compEquality = null,
            Func<T, T, bool> compInequality = null)
        {
            var util = new EqualityUtil<T>(new[] { this }, compEquality, compInequality);
            util.RunAll(checkIEquatable);
        }
    }

    /// <summary>
    /// Base class which does a lot of the boiler plate work for testing that the equality pattern
    /// is properly implemented in objects
    /// </summary>
    public sealed class EqualityUtil<T>
    {
        private readonly ReadOnlyCollection<EqualityUnit<T>> _equalityUnits;
        private readonly Func<T, T, bool> _compareWithEqualityOperator;
        private readonly Func<T, T, bool> _compareWithInequalityOperator;

        public EqualityUtil(
            IEnumerable<EqualityUnit<T>> equalityUnits,
            Func<T, T, bool> compEquality = null,
            Func<T, T, bool> compInequality = null)
        {
            _equalityUnits = equalityUnits.ToList().AsReadOnly();
            _compareWithEqualityOperator = compEquality;
            _compareWithInequalityOperator = compInequality;
        }

        public void RunAll(bool checkIEquatable = true)
        {
            if (_compareWithEqualityOperator != null)
            {
                EqualityOperator1();
                EqualityOperator2();
            }

            if (_compareWithInequalityOperator != null)
            {
                InequalityOperator1();
                InequalityOperator2();
            }

            if (checkIEquatable)
            {
                ImplementsIEquatable();
            }

            ObjectEquals1();
            ObjectEquals2();
            ObjectEquals3();
            GetHashCode1();

            if (checkIEquatable)
            {
                EquatableEquals1();
                EquatableEquals2();
            }
        }

        private void EqualityOperator1()
        {
            foreach (var unit in _equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.True(_compareWithEqualityOperator(unit.Value, value));
                    Assert.True(_compareWithEqualityOperator(value, unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.False(_compareWithEqualityOperator(unit.Value, value));
                    Assert.False(_compareWithEqualityOperator(value, unit.Value));
                }
            }
        }

        private void EqualityOperator2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnits.SelectMany(x => x.AllValues))
            {
                Assert.False(_compareWithEqualityOperator(default(T), value));
                Assert.False(_compareWithEqualityOperator(value, default(T)));
            }
        }

        private void InequalityOperator1()
        {
            foreach (var unit in _equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.False(_compareWithInequalityOperator(unit.Value, value));
                    Assert.False(_compareWithInequalityOperator(value, unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.True(_compareWithInequalityOperator(unit.Value, value));
                    Assert.True(_compareWithInequalityOperator(value, unit.Value));
                }
            }
        }

        private void InequalityOperator2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var value in _equalityUnits.SelectMany(x => x.AllValues))
            {
                Assert.True(_compareWithInequalityOperator(default(T), value));
                Assert.True(_compareWithInequalityOperator(value, default(T)));
            }
        }

        private void ImplementsIEquatable()
        {
            var type = typeof(T);
            var targetType = typeof(IEquatable<T>);
            Assert.True(type.GetTypeInfo().ImplementedInterfaces.Contains(targetType));
        }

        private void ObjectEquals1()
        {
            foreach (var unit in _equalityUnits)
            {
                var unitValue = unit.Value;
                foreach (var value in unit.EqualValues)
                {
                    Assert.True(value.Equals(unitValue));
                    Assert.True(unitValue.Equals(value));
                }
            }
        }

        /// <summary>
        /// Comparison with Null should be false for reference types
        /// </summary>
        private void ObjectEquals2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            var allValues = _equalityUnits.SelectMany(x => x.AllValues);
            foreach (var value in allValues)
            {
                Assert.NotNull(value);
            }
        }

        /// <summary>
        /// Passing a value of a different type should just return false
        /// </summary>
        private void ObjectEquals3()
        {
            var allValues = _equalityUnits.SelectMany(x => x.AllValues);
            foreach (var value in allValues)
            {
                Assert.False(value.Equals((object)42));
            }
        }

        private void GetHashCode1()
        {
            foreach (var unit in _equalityUnits)
            {
                foreach (var value in unit.EqualValues)
                {
                    Assert.Equal(value.GetHashCode(), unit.Value.GetHashCode());
                }
            }
        }

        private void EquatableEquals1()
        {
            foreach (var unit in _equalityUnits)
            {
                var equatableUnit = (IEquatable<T>)unit.Value;
                foreach (var value in unit.EqualValues)
                {
                    Assert.True(equatableUnit.Equals(value));
                    var equatableValue = (IEquatable<T>)value;
                    Assert.True(equatableValue.Equals(unit.Value));
                }

                foreach (var value in unit.NotEqualValues)
                {
                    Assert.False(equatableUnit.Equals(value));
                    var equatableValue = (IEquatable<T>)value;
                    Assert.False(equatableValue.Equals(unit.Value));
                }
            }
        }

        /// <summary>
        /// If T is a reference type, null should return false in all cases
        /// </summary>
        private void EquatableEquals2()
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                return;
            }

            foreach (var cur in _equalityUnits.SelectMany(x => x.AllValues))
            {
                var value = (IEquatable<T>)cur;
                Assert.NotNull(value);
            }
        }
    }
}
