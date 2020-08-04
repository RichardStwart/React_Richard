﻿// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReactiveUI.Winforms
{
    /// <summary>
    /// A converter that can handle setting values on a <see cref="Panel"/> control for binding.
    /// </summary>
    public class PanelSetMethodBindingConverter : ISetMethodBindingConverter
    {
        /// <inheritdoc />
        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            if (toType != typeof(Control.ControlCollection))
            {
                return 0;
            }

            if (fromType?.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>) && x.GetGenericArguments().First().IsSubclassOf(typeof(Control))) ?? false)
            {
                return 10;
            }

            return 0;
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Valid syntax.")]
        public object PerformSet(object? toTarget, object? newValue, object[]? arguments)
        {
            if (toTarget == null)
            {
                throw new ArgumentNullException(nameof(toTarget));
            }

            IEnumerable<Control>? newValueEnumerable = newValue as IEnumerable<Control>;

            if (newValueEnumerable == null)
            {
                throw new ArgumentException($"newValue must be {nameof(newValue)}", nameof(newValue));
            }

            Control.ControlCollection targetCollection = (Control.ControlCollection)toTarget;

            targetCollection.Owner.SuspendLayout();

            targetCollection.Clear();
            targetCollection.AddRange(newValueEnumerable.ToArray());

            targetCollection.Owner.ResumeLayout();

            return targetCollection;
        }
    }
}
