﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Steepshot.Core.Models;
using Steepshot.Core.Models.Requests;
using Steepshot.Core.Presenters;
using Steepshot.iOS.Cells;
using Steepshot.iOS.Helpers;
using Steepshot.iOS.ViewControllers;
using Steepshot.iOS.ViewSources;
using UIKit;

namespace Steepshot.iOS.Views
{
    public partial class PreSearchViewController : BaseViewControllerWithPresenter<PreSearchPresenter>
    {
        private PostType _currentPostType = PostType.Hot;
        private string _currentPostCategory;

        private ProfileCollectionViewSource _collectionViewSource;
        private CollectionViewFlowDelegate _gridDelegate;
        private int _lastRow;

        UINavigationController _navController;
        UINavigationItem _navItem;
        UIRefreshControl _refreshControl;
        private bool _isFeedRefreshing;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _navController = TabBarController != null ? TabBarController.NavigationController : NavigationController;
            _collectionViewSource = new ProfileCollectionViewSource(_presenter);
            //TODO:KOA: rewrite as function


            _gridDelegate = new CollectionViewFlowDelegate(collectionView/*, scrolled: async () =>
            {
                var newlastRow = collectionView.IndexPathsForVisibleItems.Max(c => c.Row) + 2;
                if (_presenter.Count <= _lastRow && !_presenter.IsLastReaded && !_isFeedRefreshing)
                    await GetPosts();

                _lastRow = newlastRow;
            }*/, presenter: _presenter);

            if (_navController != null)
                _navController.NavigationBar.Translucent = false;
            
            _collectionViewSource.IsGrid = false;
            _gridDelegate.IsGrid = false;
            collectionView.Source = _collectionViewSource;
            collectionView.RegisterClassForCell(typeof(FeedCollectionViewCell), nameof(FeedCollectionViewCell));
            collectionView.RegisterNibForCell(UINib.FromName(nameof(FeedCollectionViewCell), NSBundle.MainBundle), nameof(FeedCollectionViewCell));
            //flowLayout.EstimatedItemSize = new CGSize(UIScreen.MainScreen.Bounds.Width, 485);

            _collectionViewSource.Voted += async (vote, post, action) =>
            {
                //await Vote(post);
            };
            //_collectionViewSource.Flagged += Flagged;

            _refreshControl = new UIRefreshControl();
            _refreshControl.ValueChanged += async (sender, e) =>
            {
                if (_isFeedRefreshing)
                    return;
                _isFeedRefreshing = true;
                await RefreshTable();
                _refreshControl.EndRefreshing();
            };
            collectionView.Add(_refreshControl);
            collectionView.Delegate = _gridDelegate;

            if (TabBarController != null)
            {
                TabBarController.NavigationController.NavigationBar.TintColor = UIColor.White;
                TabBarController.NavigationController.NavigationBar.BarTintColor = Helpers.Constants.NavBlue;
                TabBarController.NavigationController.SetNavigationBarHidden(true, false);
                TabBarController.TabBar.TintColor = Helpers.Constants.NavBlue;

                foreach (var controler in TabBarController.ViewControllers)
                {
                    controler.TabBarItem.ImageInsets = new UIEdgeInsets(5, 0, -5, 0);
                }
            }

            _collectionViewSource.GoToProfile += username =>
            {
                if (username == BasePresenter.User.Login)
                    return;
                var myViewController = new ProfileViewController();
                myViewController.Username = username;
                NavigationController.PushViewController(myViewController, true);
            };

            _collectionViewSource.GoToComments += postUrl =>
            {
                var myViewController = new CommentsViewController();
                myViewController.PostUrl = postUrl;
                _navController.PushViewController(myViewController, true);
            };

            _collectionViewSource.GoToVoters += postUrl =>
            {
                var myViewController = new VotersViewController();
                myViewController.PostUrl = postUrl;
                NavigationController.PushViewController(myViewController, true);
            };

            _collectionViewSource.ImagePreview += (image, url) =>
            {
                var myViewController = new ImagePreviewViewController();
                myViewController.ImageForPreview = image;
                myViewController.ImageUrl = url;
                _navController.PushViewController(myViewController, true);
            };

            if (!BasePresenter.User.IsAuthenticated)
            {
                loginButton.Layer.CornerRadius = 20;
                loginButton.Layer.BorderWidth = 0;
            }

            loginButton.TouchDown += LoginTapped;

            NavigationController.SetNavigationBarHidden(true, false);
            GetPosts();
        }
        /*
        public override async void ViewWillAppear(bool animated)
        {
            if (CurrentPostCategory != _currentPostCategory && !_isHomeFeed)
            {
                _currentPostCategory = CurrentPostCategory;
                _presenter.Clear();
                _collectionViewSource.FeedStrings.Clear();
                _tw.Text = CurrentPostCategory;
                await GetPosts();
            }

            base.ViewWillAppear(animated);
        }
*/
        protected override void CreatePresenter()
        {
            _presenter = new PreSearchPresenter();
            _presenter.SourceChanged += SourceChanged;
        }

        private async Task GetPosts(bool shouldStartAnimating = true, bool clearOld = false)
        {
            if (shouldStartAnimating)
                activityIndicator.StartAnimating();
            noFeedLabel.Hidden = true;

            List<string> errors;
            if (CurrentPostCategory == null)
            {
                if (clearOld)
                    _presenter.Clear();
                errors = await _presenter.TryLoadNextTopPosts();
            }
            else
            {
                _presenter.Tag = CurrentPostCategory;
                errors = await _presenter.TryGetSearchedPosts();
            }

            if (errors != null && errors.Count != 0)
                ShowAlert(errors);

            if (_refreshControl.Refreshing)
            {
                _refreshControl.EndRefreshing();
                _isFeedRefreshing = false;
            }
            else
            {
                activityIndicator.StopAnimating();
            }
        }

        private async Task RefreshTable()
        {
            //_collectionViewSource.FeedStrings.Clear();
            _presenter.Clear();
            await GetPosts(false, true);
        }

        void LoginTapped(object sender, EventArgs e)
        {
            _navController.PushViewController(new WelcomeViewController(), true);
        }

        void SearchTapped(object sender, EventArgs e)
        {
            var myViewController = new TagsSearchViewController();
            _navController.PushViewController(myViewController, true);
        }

        private void SwitchButtonClick()
        {
            
        }

        private void SourceChanged(Status status)
        {
            collectionView.ReloadData();
            //flowLayout.InvalidateLayout();
        }
    }
}