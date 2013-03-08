using System;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using ReactiveUI;
using ReactiveUI.Xaml;
using System.Reactive.Linq;

namespace XamarinMacPlayground
{
    public partial class MainWindowController : MonoMac.AppKit.NSWindowController, IViewFor<MainWindowViewModel>
    {
		#region Constructors
		
        // Called when created from unmanaged code
        public MainWindowController (IntPtr handle) : base (handle)
        {
            Initialize ();
        }
		
        // Called when created directly from a XIB file
        [Export ("initWithCoder:")]
        public MainWindowController (NSCoder coder) : base (coder)
        {
            Initialize ();
        }
		
        // Call to load from the XIB/NIB file
        public MainWindowController () : base ("MainWindow")
        {
            Initialize ();
        }
		
        // Shared initialization code
        void Initialize ()
        {
            ViewModel = new MainWindowViewModel();
        }

        public override void WindowDidLoad()
        {
            base.WindowDidLoad();

            this.BindCommand(ViewModel, x => x.DoIt, x => x.doIt);
        }

		#endregion

        public MainWindowViewModel ViewModel { get; set; }

        #region IViewFor implementation

        object IViewFor.ViewModel
        {
            get { return this.ViewModel; }
            set { this.ViewModel = (MainWindowViewModel)value; }
        }

        #endregion
		
        //strongly typed window accessor
        public new MainWindow Window {
            get {
                return (MainWindow)base.Window;
            }
        }
    }

    public class MainWindowViewModel : ReactiveObject
    {
        public ReactiveAsyncCommand DoIt { get; protected set; }
        public MainWindowViewModel()
        {
            DoIt = new ReactiveAsyncCommand();
            DoIt.RegisterAsyncObservable(_ => Observable.Timer(TimeSpan.FromSeconds(5.0), RxApp.TaskpoolScheduler))
                .Subscribe(_ => {
                    Console.WriteLine("Boom");
                });
        }
    }
}