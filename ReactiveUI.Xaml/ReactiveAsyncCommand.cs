﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Concurrency;
using System.Diagnostics.Contracts;
using ReactiveUI;

namespace ReactiveUI.Xaml
{
    /// <summary>
    /// ReactiveAsyncCommand represents commands that run an asynchronous
    /// operation in the background when invoked. The main benefit of this
    /// command is that it will keep track of in-flight operations and
    /// disable/enable CanExecute when there are too many of them (i.e. a
    /// "Search" button shouldn't have many concurrent requests running if the
    /// user clicks the button many times quickly)
    /// </summary>
    public class ReactiveAsyncCommand : ReactiveCommand, IReactiveAsyncCommand
    {
        /// <summary>
        /// Constructs a new ReactiveAsyncCommand.
        /// </summary>
        /// <param name="canExecute">An Observable representing when the command
        /// can execute. If null, the Command can always execute.</param>
        /// <param name="maximumConcurrent">The maximum number of in-flight
        /// operations at a time - defaults to one.</param>
        /// <param name="scheduler">The scheduler to run the asynchronous
        /// operations on - defaults to the Taskpool scheduler.</param>
        public ReactiveAsyncCommand(
                IObservable<bool> canExecute = null, 
                int maximumConcurrent = 1, 
                IScheduler scheduler = null)
            : base(canExecute, scheduler)
        {
            commonCtor(maximumConcurrent, scheduler);
        }

        protected ReactiveAsyncCommand(
                Func<object, bool> canExecute, 
                int maximumConcurrent = 1, 
                IScheduler scheduler = null)
            : base(canExecute, scheduler)
        {
            Contract.Requires(maximumConcurrent > 0);

            this._normalSched = scheduler;
            commonCtor(maximumConcurrent, scheduler);
        }

        /// <summary>
        /// Create is a helper method to create a basic ReactiveAsyncCommand
        /// in a non-Rx way, closer to how BackgroundWorker works.
        /// </summary>
        /// <param name="calculationFunc">The function that will calculate
        /// results in the background</param>
        /// <param name="callbackFunc">The method to be called once the
        /// calculation function completes. This method is guaranteed to be
        /// called on the UI thread.</param>
        /// <param name="maximumConcurrent">The maximum number of in-flight
        /// operations at a time - defaults to one.</param>
        /// <param name="scheduler">The scheduler to run the asynchronous
        /// operations on - defaults to the Taskpool scheduler.</param>
        public static ReactiveAsyncCommand Create<TRet>(
            Func<object, TRet> calculationFunc,
            Action<TRet> callbackFunc,
            Func<object, bool> canExecute = null, 
            int maximumConcurrent = 1,
            IScheduler scheduler = null)
        {
            var ret = new ReactiveAsyncCommand(canExecute, maximumConcurrent, scheduler);
            ret.RegisterAsyncFunction(calculationFunc).Subscribe(callbackFunc);
            return ret;
        }

        void commonCtor(int maximumConcurrent, IScheduler scheduler)
        {
            AsyncCompletedNotification = new Subject<Unit>();
            this._normalSched = scheduler ?? RxApp.DeferredScheduler;

            ItemsInflight = Observable.Merge(
                this.Select(_ => 1),
                AsyncCompletedNotification.Select(_ => -1)
            ).Scan(0, (acc, x) => {
                var ret = acc + x;
                if (ret < 0) {
                    this.Log().Fatal("Reference count dropped below zero");
                }
                return ret;
            }).PublishToSubject(new BehaviorSubject<int>(0));

            ItemsInflight
                .Subscribe(x => {
                    this.Log().InfoFormat("0x{0:X} - {1} items in flight", this.GetHashCode(), x);
                    this._tooManyItems = (x >= maximumConcurrent && maximumConcurrent > 0);
                    canExecuteSubject.OnNext(!this._tooManyItems);
                });

            _maximumConcurrent = maximumConcurrent;
        }

        int _maximumConcurrent;

        IScheduler _normalSched;

        public IObservable<int> ItemsInflight { get; protected set; }

        public ISubject<Unit> AsyncCompletedNotification { get; protected set; }

        bool _tooManyItems = false;
        public override bool CanExecute(object parameter)
        {
            // HACK: Normally we shouldn't need this, but due to the way that
            // ReactiveCommand.CanExecute works when you provide an explicit
            // Func<T>, it can "trump" the ItemsInflight selector.
            if (this._tooManyItems)
                return false;

            return base.CanExecute(parameter);
        }

        /// <summary>
        /// RegisterAsyncFunction registers an asynchronous method that returns a result
        /// to be called whenever the Command's Execute method is called.
        /// </summary>
        /// <param name="calculationFunc">The function to be run in the
        /// background.</param>
        /// <param name="scheduler"></param>
        /// <returns>An Observable that will fire on the UI thread once per
        /// invocation of Execute, once the async method completes. Subscribe to
        /// this to retrieve the result of the calculationFunc.</returns>
        public IObservable<TResult> RegisterAsyncFunction<TResult>(
            Func<object, TResult> calculationFunc, 
            IScheduler scheduler = null)
        {
            Contract.Requires(calculationFunc != null);

            scheduler = scheduler ?? RxApp.TaskpoolScheduler;

            return this
                .Select(calculationFunc)
                .PublishToSubject(new Subject<TResult>(_normalSched))
                .Do(_ => AsyncCompletedNotification.OnNext(new Unit()));
        }

        /// <summary>
        /// RegisterAsyncAction registers an asynchronous method that runs
        /// whenever the Command's Execute method is called and doesn't return a
        /// result.
        /// </summary>
        /// <param name="calculationFunc">The function to be run in the
        /// background.</param>
        public void RegisterAsyncAction(Action<object> calculationFunc, 
            IScheduler scheduler = null)
        {
            Contract.Requires(calculationFunc != null);
            RegisterAsyncFunction(x => { calculationFunc(x); return new Unit(); }, scheduler);
        }

        /// <summary>
        /// RegisterAsyncObservable registers an Rx-based async method whose
        /// results will be returned on the UI thread.
        /// </summary>
        /// <param name="calculationFunc">A calculation method that returns a
        /// future result, such as a method returned via
        /// Observable.FromAsyncPattern.</param>
        /// <returns>An Observable representing the items returned by the
        /// calculation result. Note that with this method it is possible with a
        /// calculationFunc to return multiple items per invocation of Execute.</returns>
        public IObservable<TResult> RegisterAsyncObservable<TResult>(Func<object, IObservable<TResult>> calculationFunc)
        {
            Contract.Requires(calculationFunc != null);

            // The Do() here essentially ends up being, "When all results are 
            // returned from the Observable, signal completion"
            return this.Select(calculationFunc)
                .Do(x => x.Subscribe(_ => { }, () => AsyncCompletedNotification.OnNext(new Unit())))
                .SelectMany(x => x)
                .PublishToSubject(new Subject<TResult>(_normalSched));
        }

        /// <summary>
        /// RegisterMemoizedFunction is similar to RegisterAsyncFunction, but
        /// caches its results so that subsequent Execute calls with the same
        /// CommandParameter will not need to be run in the background.         
        /// </summary>
        /// <param name="calculationFunc">The function that performs the
        /// expensive or asyncronous calculation and returns the result.
        ///
        /// Note that this function *must* return an equivalently-same result given a
        /// specific input - because the function is being memoized, if the
        /// calculationFunc depends on other varables other than the input
        /// value, the results will be unpredictable.</param>
        /// <param name="maxSize">The number of items to cache. When this limit
        /// is reached, not recently used items will be discarded.</param>
        /// <param name="onRelease">This optional method is called when an item
        /// is evicted from the cache - this can be used to clean up / manage an
        /// on-disk cache; the calculationFunc can download a file and save it
        /// to a temporary folder, and the onRelease action will delete the
        /// file.</param>
        /// <param name="sched">The scheduler to run asynchronous operations on
        /// - defaults to TaskpoolScheduler</param>
        /// <returns>An Observable that will fire on the UI thread once per
        /// invocation of Execute, once the async method completes. Subscribe to
        /// this to retrieve the result of the calculationFunc.</returns>
        public IObservable<TResult> RegisterMemoizedFunction<TResult>(
            Func<object, TResult> calculationFunc, 
            int maxSize = 50, 
            Action<TResult> onRelease = null, 
            IScheduler sched = null)
        {
            Contract.Requires(calculationFunc != null);
            Contract.Requires(maxSize > 0);

            sched = sched ?? RxApp.TaskpoolScheduler;
            return RegisterMemoizedObservable(x => Observable.Return(calculationFunc(x), sched), maxSize, onRelease, sched);
        }

        /// <summary>
        /// RegisterMemoizedObservable is similar to RegisterAsyncObservable, but
        /// caches its results so that subsequent Execute calls with the same
        /// CommandParameter will not need to be run in the background.         
        /// </summary>
        /// <param name="calculationFunc">The function that performs the
        /// expensive or asyncronous calculation and returns the result.
        ///
        /// Note that this function *must* return an equivalently-same result given a
        /// specific input - because the function is being memoized, if the
        /// calculationFunc depends on other varables other than the input
        /// value, the results will be unpredictable. 
        /// </param>
        /// <param name="maxSize">The number of items to cache. When this limit
        /// is reached, not recently used items will be discarded.</param>
        /// <param name="onRelease">This optional method is called when an item
        /// is evicted from the cache - this can be used to clean up / manage an
        /// on-disk cache; the calculationFunc can download a file and save it
        /// to a temporary folder, and the onRelease action will delete the
        /// file.</param>
        /// <param name="sched">The scheduler to run asynchronous operations on
        /// - defaults to TaskpoolScheduler</param>
        /// <returns>An Observable representing the items returned by the
        /// calculation result. Note that with this method it is possible with a
        /// calculationFunc to return multiple items per invocation of Execute.</returns>
        public IObservable<TResult> RegisterMemoizedObservable<TResult>(
            Func<object, IObservable<TResult>> calculationFunc, 
            int maxSize = 50,
            Action<TResult> onRelease = null,  
            IScheduler sched = null)
        {
            Contract.Requires(calculationFunc != null);
            Contract.Requires(maxSize > 0);

            sched = sched ?? RxApp.TaskpoolScheduler;
            var cache = new ObservableAsyncMRUCache<object, TResult>(
                calculationFunc, maxSize, _maximumConcurrent, onRelease, sched);
            return this.RegisterAsyncObservable(cache.AsyncGet);
        }
    }

    public static class ReactiveAsyncCommandMixins
    {
        public static int CurrentItemsInFlight(this IReactiveAsyncCommand This)
        {
            return This.ItemsInflight.First();
        }
    }
}

// vim: tw=120 ts=4 sw=4 et :
