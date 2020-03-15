using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

using UIKit;
using Foundation;
using AVFoundation;
using CoreGraphics;

using ZXing;

namespace ZXing.Mobile
{
	public class ZXingScannerViewController : UIViewController, IScannerViewController
	{
		ZXingScannerView scannerView;

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

		UIActivityIndicatorView loadingView;
		UIView loadingBg;

		public MobileBarcodeScanner Scanner { get; private set; }

		public ScannerOverlaySettings<UIView> OverlaySettings { get; private set; }

		public ZXingScannerViewController(ScannerOverlaySettings<UIView> scannerOverlaySettings)
			: this(null, scannerOverlaySettings)
		{
		}

		internal ZXingScannerViewController(MobileBarcodeScanner scanner, ScannerOverlaySettings<UIView> scannerOverlaySettings)
		{
			Scanner = scanner;
			OverlaySettings = scannerOverlaySettings;

			var appFrame = UIScreen.MainScreen.ApplicationFrame;

			View.Frame = new CGRect(0, 0, appFrame.Width, appFrame.Height);
			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
				ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
		}

		public void Cancel()
			=> InvokeOnMainThread(scannerView.Stop);

		UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad()
		{
			loadingBg = new UIView(View.Frame) { BackgroundColor = UIColor.Black, AutoresizingMask = UIViewAutoresizing.FlexibleDimensions };
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

			scannerView = new ZXingScannerView(new CGRect(0, 0, View.Frame.Width, View.Frame.Height), OverlaySettings, this)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
			};
			scannerView.OnCancelButtonPressed += delegate
			{
				Cancel();
			};

			scannerView.OnBarcodeScanned += OnBarcodeScanned;

			//this.View.AddSubview(scannerView);
			View.InsertSubviewBelow(scannerView, loadingView);

			View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
			{
				if (UIApplication.SharedApplication.KeyWindow != null)
					OverrideUserInterfaceStyle = UIApplication.SharedApplication.KeyWindow.RootViewController.OverrideUserInterfaceStyle;
			}
		}

		public void Torch(bool on)
			=> scannerView?.Torch(on);

		public void ToggleTorch()
			=> scannerView?.ToggleTorch();

		public void PauseAnalysis()
			=> scannerView?.PauseAnalysis();

		public void ResumeAnalysis()
			=> scannerView?.ResumeAnalysis();

		public bool IsTorchOn
			=> scannerView?.IsTorchOn ?? false;

		public bool IsAnalyzing => scannerView?.IsAnalyzing ?? false;

		public bool HasTorch => scannerView?.HasTorch ?? false;

		public void AutoFocus()
			=> scannerView?.AutoFocus();

		public void AutoFocus(int x, int y)
			=> scannerView?.AutoFocus(x, y);

		public override void ViewDidAppear(bool animated)
		{
			scannerView.OnScannerSetupComplete += HandleOnScannerSetupComplete;

			originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
			{
				UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
				SetNeedsStatusBarAppearanceUpdate();
			}
			else
				UIApplication.SharedApplication.SetStatusBarStyle(UIStatusBarStyle.BlackTranslucent, false);

			Console.WriteLine("Starting to scan...");

			Task.Factory.StartNew(() =>
				BeginInvokeOnMainThread(() => scannerView.Start()));
		}

		public override void ViewDidDisappear(bool animated)
		{
			scannerView.OnBarcodeScanned -= OnBarcodeScanned;
			scannerView?.Stop();
			scannerView.OnScannerSetupComplete -= HandleOnScannerSetupComplete;
		}

		public override void ViewWillDisappear(bool animated)
			=> UIApplication.SharedApplication.SetStatusBarStyle(originalStatusBarStyle, false);

		public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
			=> scannerView?.DidRotate(this.InterfaceOrientation);

		public override bool ShouldAutorotate()
			=> ScanningOptions?.AutoRotate ?? false;

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
			=> UIInterfaceOrientationMask.All;

		[Obsolete("Deprecated in iOS6. Replace it with both GetSupportedInterfaceOrientations and PreferredInterfaceOrientationForPresentation")]
		public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
			=> ScanningOptions?.AutoRotate ?? false;

		void HandleOnScannerSetupComplete()
			=> BeginInvokeOnMainThread(() =>
			{
				if (loadingView != null && loadingBg != null && loadingView.IsAnimating)
				{
					loadingView.StopAnimating();

					UIView.BeginAnimations("zoomout");

					UIView.SetAnimationDuration(2.0f);
					UIView.SetAnimationCurve(UIViewAnimationCurve.EaseOut);

					loadingBg.Transform = CGAffineTransform.MakeScale(2.0f, 2.0f);
					loadingBg.Alpha = 0.0f;

					UIView.CommitAnimations();


					loadingBg.RemoveFromSuperview();
				}
			});
	}
}