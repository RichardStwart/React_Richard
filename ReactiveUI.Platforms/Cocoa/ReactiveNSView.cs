using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Reactive.Disposables;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Splat;

#if UIKIT
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using NSView = MonoTouch.UIKit.UIView;
#else
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif

namespace ReactiveUI.Cocoa
{
    /// <summary>
    /// This is an View that is both an NSView and has ReactiveObject powers 
    /// (i.e. you can call RaiseAndSetIfChanged)
    /// </summary>
    public class ReactiveView : NSView, IReactiveNotifyPropertyChanged, IHandleObservableErrors, IReactiveExtension
    {
        protected ReactiveView() : base()
        {
            this.setupReactiveExtension();
        }

        protected ReactiveView(NSCoder c) : base(c)
        {
            this.setupReactiveExtension();
        }

        protected ReactiveView(NSObjectFlag f) : base(f)
        {
            this.setupReactiveExtension();
        }

        protected ReactiveView(IntPtr handle) : base(handle)
        {
            this.setupReactiveExtension();
        }

        protected ReactiveView(RectangleF size) : base(size)
        {
            this.setupReactiveExtension();
        }
                
        [field:IgnoreDataMember]
        public event PropertyChangingEventHandler PropertyChanging;

        void IReactiveExtension.RaisePropertyChanging(PropertyChangingEventArgs args) {
            var handler = PropertyChanging;
            if (handler != null) {
                handler(this, args);
            }
        }

        [field:IgnoreDataMember]
        public event PropertyChangedEventHandler PropertyChanged;

        void IReactiveExtension.RaisePropertyChanged(PropertyChangedEventArgs args) {
            var handler = PropertyChanged;
            if (handler != null) {
                handler(this, args);
            }
        }

        /// <summary>
        /// Represents an Observable that fires *before* a property is about to
        /// be changed.         
        /// </summary>
        [IgnoreDataMember]
        public IObservable<IObservedChange<object, object>> Changing {
            get { return this.getChangingObservable(); }
        }

        /// <summary>
        /// Represents an Observable that fires *after* a property has changed.
        /// </summary>
        [IgnoreDataMember]
        public IObservable<IObservedChange<object, object>> Changed {
            get { return this.getChangedObservable(); }
        }

        [OnDeserialized]
        void setupRxObj(StreamingContext sc) { this.setupReactiveExtension(); }

        /// <summary>
        /// When this method is called, an object will not fire change
        /// notifications (neither traditional nor Observable notifications)
        /// until the return value is disposed.
        /// </summary>
        /// <returns>An object that, when disposed, reenables change
        /// notifications.</returns>
        public IDisposable SuppressChangeNotifications()
        {
            return this.suppressChangeNotifications();
        }

        [IgnoreDataMember]
        public IObservable<Exception> ThrownExceptions { get { return this.getThrownExceptionsObservable(); } }
    }
}
