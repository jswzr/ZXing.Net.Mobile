using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

using UIKit;
using Foundation;
using AVFoundation;
using CoreGraphics;

using ZXing;

namespace ZXing.Mobile
{
	public class AVCaptureScannerViewController : UIViewController, IScannerViewController
	{
		AVCaptureScannerView scannerView;

		public event EventHandler<BarcodeScannedEventArgs> OnBarcodeScanned;

		MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();
		public MobileBarcodeScanningOptions ScanningOptions
		{
			get => Scanner?.ScanningOptions ?? options;
			set
			{
				if (Scanner != null)
					Scanner.ScanningOptions = value;
				options = value;
			}
		}
		public MobileBarcodeScanner Scanner { get; private set; }

		public ScannerOverlaySettings<UIView> OverlaySettings { get; private set; }

		UIActivityIndicatorView loadingView;
		UIView loadingBg;

		internal AVCaptureScannerViewController(MobileBarcodeScanner scanner, ScannerOverlaySettings<UIView> overlaySettings)
		{
			OverlaySettings = overlaySettings;
			Scanner = scanner;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public UIViewController AsViewController()
			=> this;

		public void Cancel()
			=> InvokeOnMainThread(() => scannerView.Stop());

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad()
		{
			loadingBg = new UIView(this.View.Frame)
			{
				BackgroundColor = UIColor.Black,
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
			};
			loadingView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleMargins
			};
			loadingView.Frame = new CGRect((View.Frame.Width - loadingView.Frame.Width) / 2,
				(View.Frame.Height - loadingView.Frame.Height) / 2,
				loadingView.Frame.Width,
				loadingView.Frame.Height);

			loadingBg.AddSubview(loadingView);
			View.AddSubview(loadingBg);
			loadingView.StartAnimating();

			scannerView = new AVCaptureScannerView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height), OverlaySettings, this)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
			};
			scannerView.OnCancelButtonPressed += () =>
				Scanner.Cancel();

			View.AddSubview(scannerView);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
		}

		public void Torch(bool on)
			=> scannerView?.Torch(on);

		public void ToggleTorch()
			=> scannerView?.ToggleTorch();

		public bool IsTorchOn
			=> scannerView?.IsTorchOn ?? false;

		public bool IsAnalyzing => scannerView?.IsAnalyzing ?? false;

		public bool HasTorch => scannerView?.HasTorch ?? false;

		public void PauseAnalysis()
			=> scannerView?.PauseAnalysis();

		public void ResumeAnalysis()
			=> scannerView?.ResumeAnalysis();

		public override void ViewDidAppear(bool animated)
		{
			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
			{
				UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
				SetNeedsStatusBarAppearanceUpdate();
			}
			else
				UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Console.WriteLine("Starting to scan...");

			scannerView.OnBarcodeScanned += OnBarcodeScanned;
		}

		public override void ViewDidDisappear(bool animated)
		{
			scannerView.OnBarcodeScanned -= OnBarcodeScanned;

			if (scannerView != null)
				scannerView.Stop();
		}

		public override void ViewWillDisappear(bool animated)
			=> UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);

		public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
		{
			if (scannerView != null)
				scannerView.DidRotate(this.InterfaceOrientation);
		}

		public override bool ShouldAutorotate()
		{
			if (ScanningOptions.AutoRotate != null)
				return (bool)ScanningOptions.AutoRotate;

			return false;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
			=> UIInterfaceOrientationMask.All;

		public void AutoFocus()
			=> scannerView?.AutoFocus();

		public void AutoFocus(int x, int y)
			=> scannerView?.AutoFocus(x, y);

		//void HandleOnScannerSetupComplete()
		//{
		//	BeginInvokeOnMainThread(() =>
		//	{
		//		if (loadingView != null && loadingBg != null && loadingView.IsAnimating)
		//		{
		//			loadingView.StopAnimating();

		//			UIView.BeginAnimations("zoomout");

		//			UIView.SetAnimationDuration(2.0f);
		//			UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);

		//			loadingBg.Transform = CGAffineTransform.MakeScale(2.0f, 2.0f);
		//			loadingBg.Alpha = 0.0f;

		//			UIView.CommitAnimations();

		//			loadingBg.RemoveFromSuperview();
		//		}
		//	});
		//}
	}
}
