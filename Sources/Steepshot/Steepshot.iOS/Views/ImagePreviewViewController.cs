﻿using System;
using CoreGraphics;
using FFImageLoading;
using Steepshot.iOS.Helpers;
using UIKit;

namespace Steepshot.iOS.Views
{
    public partial class ImagePreviewViewController : UIViewController
    {
        protected ImagePreviewViewController(IntPtr handle) : base(handle) { }

        public ImagePreviewViewController()
        {
        }

        public UIImage ImageForPreview;
        public string ImageUrl;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var margin = NavigationController.NavigationBar.Frame.Height + UIApplication.SharedApplication.StatusBarFrame.Height;
            var imageScrollView = new UIScrollView(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, UIScreen.MainScreen.Bounds.Height - margin));
            View.AddSubview(imageScrollView);
            var imageView = new UIImageView(new CGRect(0, 0, imageScrollView.Frame.Width, imageScrollView.Frame.Height));
            //if (ImageForPreview != null)
            //{
                //imageView.Image = ImageForPreview;
                imageScrollView.MinimumZoomScale = 1f;
                imageScrollView.MaximumZoomScale = 6.0f;
                imageScrollView.ViewForZoomingInScrollView += (UIScrollView sv) => { return imageView; };
            //}
            //else
            //{
                View.BackgroundColor = UIColor.White;
                imageView.Image = UIImage.FromBundle("ic_photo_holder");
            //}
            ImageService.Instance.LoadUrl(ImageUrl, Constants.ImageCacheDuration)
                                                     .Retry(2)
                                                     .Into(imageView);
            imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            imageScrollView.ContentSize = imageView.Image.Size;
            imageScrollView.AddSubview(imageView);
            imageScrollView.ContentSize = new CGSize(imageScrollView.Frame.Width, imageScrollView.Frame.Height);
            if (TabBarController != null)
                TabBarController.TabBar.Hidden = true;

            SetBackButton();
        }

        private void SetBackButton()
        {
            //NavigationItem.Title = "Photo preview";
            var leftBarButton = new UIBarButtonItem(UIImage.FromBundle("ic_back_arrow"), UIBarButtonItemStyle.Plain, GoBack);
            leftBarButton.TintColor = Helpers.Constants.R15G24B30;
            NavigationItem.LeftBarButtonItem = leftBarButton;
        }

        private void GoBack(object sender, EventArgs e)
        {
            NavigationController.PopViewController(true);
        }
    }
}
