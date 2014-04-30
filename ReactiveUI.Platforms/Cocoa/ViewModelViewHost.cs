using System;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

#if UIKIT
using MonoTouch.UIKit;
using NSView = MonoTouch.UIKit.UIView;
using NSViewController = MonoTouch.UIKit.UIViewController;
#else
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif

namespace ReactiveUI.Cocoa
{
    /// <summary>
    /// ViewModelViewHost is a helper class that will connect a ViewModel
    /// to an arbitrary NSView and attempt to load the View for the current
    /// ViewModel as a child view of the target. 
    /// 
    /// This is a bit different than the XAML's ViewModelViewHost in the sense
    /// that this isn't a Control itself, it only manipulates other Views.
    /// </summary>
    public class ViewModelViewHost : ReactiveObject 
    {
        public ViewModelViewHost(NSView targetView)
        {
            NSView viewLastAdded = null;

            ViewContractObservable = Observable.Return(default(string));

            var vmAndContract = Observable.CombineLatest(
                this.WhenAny(x => x.ViewModel, x => x.Value),
                this.WhenAnyObservable(x => x.ViewContractObservable),
                (vm, contract) => new { ViewModel = vm, Contract = contract, });

            vmAndContract.Subscribe(x => {
                if (viewLastAdded != null) viewLastAdded.RemoveFromSuperview();

                if (ViewModel == null) {
                    if (DefaultContent != null) targetView.AddSubview(DefaultContent.View);
                    return;
                }

                var viewLocator = ViewLocator ?? ReactiveUI.ViewLocator.Current;
                var view = viewLocator.ResolveView(x.ViewModel, x.Contract);
				if (view == null)
				{
                    var message = string.Format("Unable to resolve view for \"{0}\"", x.ViewModel.GetType());
                    if (x.Contract != null)
                    {
                        message += string.Format(" and contract \"{1}\"", x.Contract.GetType());
                    }
                    message += ".";
					throw new Exception(message);
				}
                view.ViewModel = x.ViewModel;

                viewLastAdded = ((NSViewController)view).View;

                // required for Auto Layout to work correctly,
                // see https://developer.apple.com/library/ios/documentation/userexperience/conceptual/AutolayoutPG/AdoptingAutoLayout/AdoptingAutoLayout.html
                viewLastAdded.TranslatesAutoresizingMaskIntoConstraints = false;

                targetView.AddSubview(viewLastAdded);

                // add edge constraints so that subview trails changes in parent
                AddEdgeConstraint(NSLayoutAttribute.Left,  targetView, viewLastAdded);
                AddEdgeConstraint(NSLayoutAttribute.Right, targetView, viewLastAdded);
                AddEdgeConstraint(NSLayoutAttribute.Top, targetView, viewLastAdded);
                AddEdgeConstraint(NSLayoutAttribute.Bottom,  targetView, viewLastAdded);
            });
        }

        private void AddEdgeConstraint(NSLayoutAttribute edge, NSView parentView, NSView subView)
        {
            var constraint = NSLayoutConstraint.Create(subView, edge, NSLayoutRelation.Equal, parentView, edge, 1, 0);
            parentView.AddConstraint(constraint);
        }

        NSViewController _DefaultContent;
        public NSViewController DefaultContent {
            get { return _DefaultContent; }
            set { this.RaiseAndSetIfChanged(ref _DefaultContent, value); }
        }

        IReactiveObject _ViewModel;
        public IReactiveObject ViewModel {
            get { return _ViewModel; }
            set { this.RaiseAndSetIfChanged(ref _ViewModel, value); }
        }
        
        IObservable<string> _ViewContractObservable;
        public IObservable<string> ViewContractObservable {
            get { return _ViewContractObservable; }
            set { this.RaiseAndSetIfChanged(ref _ViewContractObservable, value); }
        }
       
        public IViewLocator ViewLocator { get; set; }
    }
}
