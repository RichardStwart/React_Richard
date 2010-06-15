﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Interactivity;
using System.Linq;
//using Microsoft.Expression.Interactivity.Core;

namespace ReactiveXaml.Blend
{
#if SILVERLIGHT
    public class FollowObservableStateBehavior : Behavior<Control>
#else
    public class FollowObservableStateBehavior : Behavior<FrameworkElement>
#endif
    {
        public IObservable<string> StateObservable {
            get { return (IObservable<string>)GetValue(StateObservableProperty); }
            set { SetValue(StateObservableProperty, value); }
        }
        public static readonly DependencyProperty StateObservableProperty =
            DependencyProperty.Register("StateObservable", typeof(IObservable<string>), typeof(FollowObservableStateBehavior), new PropertyMetadata(onStateObservableChanged));

#if SILVERLIGHT
        public Control TargetObject {
            get { return (Control)GetValue(TargetObjectProperty); }
            set { SetValue(TargetObjectProperty, value); }
        }
        public static readonly DependencyProperty TargetObjectProperty =
            DependencyProperty.Register("TargetObject", typeof(Control), typeof(FollowObservableStateBehavior), new PropertyMetadata(null));
#else
        public FrameworkElement TargetObject {
            get { return (FrameworkElement)GetValue(TargetObjectProperty); }
            set { SetValue(TargetObjectProperty, value); }
        }
        public static readonly DependencyProperty TargetObjectProperty =
            DependencyProperty.Register("TargetObject", typeof(FrameworkElement), typeof(FollowObservableStateBehavior), new PropertyMetadata(null));
#endif

        public bool AutoResubscribeOnError { get; set; }

        IDisposable watcher;

        protected override void OnDetaching()
        {
            if (watcher != null) {
                watcher.Dispose();
                watcher = null;
            }
            base.OnDetaching();
        }

        protected static void onStateObservableChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            FollowObservableStateBehavior This = (FollowObservableStateBehavior)sender;
            if (This.watcher != null) {
                This.watcher.Dispose();
                This.watcher = null;
            }

            This.watcher = ((IObservable<string>)e.NewValue).ObserveOnDispatcher().Subscribe(
                x => VisualStateManager.GoToState(This.TargetObject ?? This.AssociatedObject, x, true),
                ex => {
                    if (!This.AutoResubscribeOnError)
                        return;
                    onStateObservableChanged(This, e);
                });
        }
    }
}