﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;

#if WINRT
using Windows.UI.Xaml;
#endif

namespace ReactiveUI.Xaml
{
    public class ActivationForViewFetcher : IActivationForViewFetcher
    {
        public int GetAffinityForView(Type view)
        {
            return (typeof(FrameworkElement).GetTypeInfo().IsAssignableFrom(view.GetTypeInfo())) ? 10 : 0;
        }

        public Tuple<IObservable<Unit>, IObservable<Unit>> GetActivationForView(IActivatable view)
        {
            var fe = view as FrameworkElement;
            return Tuple.Create(
                Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(x => fe.Loaded += x, x => fe.Loaded -= x).Select(_ => Unit.Default),
                Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(x => fe.Unloaded += x, x => fe.Unloaded -= x).Select(_ => Unit.Default));
        }
    }
}
