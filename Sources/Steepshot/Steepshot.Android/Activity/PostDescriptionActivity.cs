﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Transitions;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Com.Lilarcor.Cheeseknife;
using Java.IO;
using Steepshot.Adapter;
using Steepshot.Base;
using Steepshot.Core;
using Steepshot.Core.Models.Requests;
using Steepshot.Core.Presenters;
using Steepshot.Core.Utils;
using Steepshot.Utils;
using Steepshot.Core.Models;
using Steepshot.Core.Errors;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Models.Enums;
using Orientation = Android.Media.Orientation;

namespace Steepshot.Activity
{
    [Activity(Label = "PostDescriptionActivity", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, WindowSoftInputMode = SoftInput.StateVisible | SoftInput.AdjustPan)]
    public sealed class PostDescriptionActivity : BaseActivityWithPresenter<PostDescriptionPresenter>
    {
        public const string PhotoExtraPath = "PhotoExtraPath";
        public const string IsNeedCompressExtraPath = "SHOULD_COMPRESS";
        private readonly TimeSpan PostingLimit = TimeSpan.FromMinutes(5);

        private string _path;
        private bool _shouldCompress;
        private Timer _timer;
        private SelectedTagsAdapter _localTagsAdapter;
        private TagsAdapter _tagsAdapter;
        private PreparePostModel _model;
        private string _previousQuery;

#pragma warning disable 0649, 4014
        [InjectView(Resource.Id.title)] private EditText _title;
        [InjectView(Resource.Id.description)] private EditText _description;
        [InjectView(Resource.Id.btn_post)] private Button _postButton;
        [InjectView(Resource.Id.local_tags_list)] private RecyclerView _localTagsList;
        [InjectView(Resource.Id.tags_list)] private RecyclerView _tagsList;
        [InjectView(Resource.Id.page_title)] private TextView _pageTitle;
        [InjectView(Resource.Id.photo)] private ImageView _photoFrame;
        [InjectView(Resource.Id.tag)] private NewTextEdit _tag;
        [InjectView(Resource.Id.root_layout)] private RelativeLayout _rootLayout;
        [InjectView(Resource.Id.tags_layout)] private LinearLayout _tagsLayout;
        [InjectView(Resource.Id.tags_list_layout)] private LinearLayout _tagsListLayout;
        [InjectView(Resource.Id.top_margin_tags_layout)] private LinearLayout _topMarginTagsLayout;
        [InjectView(Resource.Id.loading_spinner)] private ProgressBar _loadingSpinner;
        [InjectView(Resource.Id.btn_back)] private ImageButton _backButton;
#pragma warning restore 0649

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.lyt_post_description);
            Cheeseknife.Inject(this);

            _pageTitle.Typeface = Style.Semibold;
            _title.Typeface = Style.Regular;
            _description.Typeface = Style.Regular;
            _postButton.Typeface = Style.Semibold;
            _postButton.Click += OnPost;
            _photoFrame.Clickable = true;
            _photoFrame.Click += PhotoFrameOnClick;
            _postButton.Text = Localization.Texts.PublishButtonText;
            _postButton.Enabled = true;

            _localTagsList.SetLayoutManager(new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false));
            _localTagsAdapter = new SelectedTagsAdapter();
            _localTagsAdapter.Click += LocalTagsAdapterClick;
            _localTagsList.SetAdapter(_localTagsAdapter);
            _localTagsList.AddItemDecoration(new ListItemDecoration((int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 15, Resources.DisplayMetrics)));

            _tagsList.SetLayoutManager(new LinearLayoutManager(this));
            Presenter.SourceChanged += PresenterSourceChanged;
            _tagsAdapter = new TagsAdapter(Presenter);
            _tagsAdapter.Click += OnTagsAdapterClick;
            _tagsList.SetAdapter(_tagsAdapter);

            _tag.TextChanged += OnTagOnTextChanged;
            _tag.KeyboardDownEvent += HideTagsList;
            _tag.OkKeyEvent += HideTagsList;
            _tag.FocusChange += OnTagOnFocusChange;

            _topMarginTagsLayout.Click += OnTagsLayoutClick;
            _backButton.Click += OnBack;
            _rootLayout.Click += OnRootLayoutClick;

            _timer = new Timer(OnTimer);

            InitPhoto();
            SetPostingTimer();
            SearchTextChanged();

            _model = new PreparePostModel(BasePresenter.User.UserInfo);
        }

        private async void SetPostingTimer()
        {
            var timepassed = DateTime.Now - BasePresenter.User.UserInfo.LastPostTime;
            _postButton.Enabled = false;
            while (timepassed < PostingLimit)
            {
                _postButton.Text = (PostingLimit - timepassed).ToString("mm\\:ss");
                await Task.Delay(1000);
                if (IsDestroyed)
                    return;
                timepassed = DateTime.Now - BasePresenter.User.UserInfo.LastPostTime;
            }
            _postButton.Enabled = true;
            _postButton.Text = Localization.Texts.PublishButtonText;
        }

        private void InitPhoto()
        {
            _path = Intent.GetStringExtra(PhotoExtraPath);

            _shouldCompress = Intent.GetBooleanExtra(IsNeedCompressExtraPath, true);
            if (_shouldCompress)
                _path = Compress(_path);

            var photoUri = Android.Net.Uri.Parse(_path);
            _photoFrame.SetImageURI(photoUri);
        }

        private string Compress(string path)
        {
            var photoUri = Android.Net.Uri.Parse(path);

            FileDescriptor fileDescriptor = null;
            Bitmap btmp = null;
            System.IO.FileStream stream = null;
            try
            {
                fileDescriptor = ContentResolver.OpenFileDescriptor(photoUri, "r").FileDescriptor;
                btmp = BitmapUtils.DecodeSampledBitmapFromDescriptor(fileDescriptor, 1600, 1600);
                btmp = BitmapUtils.RotateImageIfRequired(btmp, fileDescriptor, path);

                var directoryPictures = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures);
                var directory = new Java.IO.File(directoryPictures, Constants.Steepshot);
                if (!directory.Exists())
                    directory.Mkdirs();

                path = $"{directory}/{Guid.NewGuid()}.jpeg";
                stream = new System.IO.FileStream(path, System.IO.FileMode.Create);
                btmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

                return path;
            }
            catch (Exception ex)
            {
                _postButton.Enabled = false;
                this.ShowAlert(Localization.Errors.UnknownCriticalError);
                AppSettings.Reporter.SendCrash(ex);
            }
            finally
            {
                fileDescriptor?.Dispose();
                btmp?.Recycle();
                btmp?.Dispose();
                stream?.Dispose();
            }
            return path;
        }

        private void PhotoFrameOnClick(object sender, EventArgs e)
        {
            if (!_photoFrame.Clickable)
                return;

            _photoFrame.Clickable = false;
            var btmp = BitmapFactory.DecodeFile(_path);
            _shouldCompress = true;

            btmp = BitmapUtils.RotateImage(btmp, 90);
            using (var stream = new System.IO.FileStream(_path, System.IO.FileMode.Create))
            {
                btmp.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
            }
            btmp.Recycle();
            btmp.Dispose();

            var photoUri = Android.Net.Uri.Parse(_path);
            _photoFrame.SetImageURI(null);
            _photoFrame.SetImageURI(photoUri);
            _photoFrame.Clickable = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cheeseknife.Reset(this);
            GC.Collect(0);
        }

        private void PresenterSourceChanged(Status status)
        {
            if (IsFinishing || IsDestroyed)
                return;

            RunOnUiThread(() =>
            {
                _tagsAdapter.NotifyDataSetChanged();
            });
        }


        private void LocalTagsAdapterClick(string tag)
        {
            if (!_localTagsAdapter.Enabled)
                return;

            _localTagsAdapter.LocalTags.Remove(tag);
            _localTagsAdapter.NotifyDataSetChanged();
            if (!_localTagsAdapter.LocalTags.Any())
                _localTagsList.Visibility = ViewStates.Gone;
        }

        private void OnTagOnFocusChange(object sender, View.FocusChangeEventArgs e)
        {
            if (e.HasFocus)
            {
                Window.SetSoftInputMode(SoftInput.AdjustResize);
                AnimateTagsLayout(Resource.Id.toolbar);
            }
        }

        private void OnTagOnTextChanged(object sender, TextChangedEventArgs e)
        {
            var txt = e.Text.ToString();
            if (!string.IsNullOrWhiteSpace(txt))
            {
                if (txt.EndsWith(" "))
                {
                    _tag.Text = string.Empty;
                    AddTag(txt);
                }
            }
            _timer.Change(500, Timeout.Infinite);
        }

        private void OnTagsAdapterClick(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            AddTag(tag);
            _tag.Text = string.Empty;
        }

        private async void OnPost(object sender, EventArgs e)
        {
            _postButton.Enabled = false;
            _title.Enabled = false;
            _description.Enabled = false;
            _tag.Enabled = false;
            _localTagsAdapter.Enabled = false;
            _postButton.Text = string.Empty;
            _loadingSpinner.Visibility = ViewStates.Visible;
            await OnPostAsync();
        }

        private void OnBack(object sender, EventArgs e)
        {
            if (_tag.HasFocus)
                HideTagsList();
            else
                OnBackPressed();
        }

        private void OnRootLayoutClick(object sender, EventArgs e)
        {
            HideKeyboard();
        }

        private void OnTagsLayoutClick(object sender, EventArgs e)
        {
            if (!_tag.Enabled)
                return;
            _tag.RequestFocus();
            var imm = GetSystemService(InputMethodService) as InputMethodManager;
            imm?.ShowSoftInput(_tag, ShowFlags.Implicit);
        }

        private void AnimateTagsLayout(int subject)
        {
            TransitionManager.BeginDelayedTransition(_rootLayout);
            _tagsListLayout.Visibility = Resource.Id.toolbar == subject ? ViewStates.Visible : ViewStates.Gone;

            var topPadding = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, Resource.Id.toolbar == subject ? 5 : 45, Resources.DisplayMetrics);
            _topMarginTagsLayout.SetPadding(0, topPadding, 0, 0);

            var currentButtonLayoutParameters = _tagsLayout.LayoutParameters as RelativeLayout.LayoutParams;
            if (currentButtonLayoutParameters != null)
            {
                currentButtonLayoutParameters.AddRule(LayoutRules.Below, subject);
                _tagsLayout.LayoutParameters = currentButtonLayoutParameters;
            }
        }

        private void AddTag(string tag)
        {
            tag = tag.Trim();
            if (_localTagsAdapter.LocalTags.Count >= 20 || _localTagsAdapter.LocalTags.Any(t => t == tag))
                return;

            _localTagsAdapter.LocalTags.Add(tag);
            RunOnUiThread(() =>
            {
                _localTagsAdapter.NotifyDataSetChanged();
                _localTagsList.MoveToPosition(_localTagsAdapter.LocalTags.Count - 1);
                if (_localTagsAdapter.LocalTags.Count() == 1)
                    _localTagsList.Visibility = ViewStates.Visible;
            });
        }

        private void OnTimer(object state)
        {
            RunOnUiThread(async () =>
            {
                await SearchTextChanged();
            });
        }

        private async Task SearchTextChanged()
        {
            if (_previousQuery == _tag.Text || _tag.Text.Length == 1)
                return;

            _previousQuery = _tag.Text;
            _tagsList.ScrollToPosition(0);
            Presenter.Clear();

            ErrorBase error = null;
            if (_tag.Text.Length == 0)
                error = await Presenter.TryGetTopTags();
            else if (_tag.Text.Length > 1)
                error = await Presenter.TryLoadNext(_tag.Text);

            if (IsFinishing || IsDestroyed)
                return;

            this.ShowAlert(error);
        }

        private async Task OnPostAsync()
        {
            var isConnected = BasePresenter.ConnectionService.IsConnectionAvailable();

            if (!isConnected)
            {
                this.ShowAlert(Localization.Errors.InternetUnavailable);
                OnUploadEnded();
                return;
            }

            if (string.IsNullOrEmpty(_title.Text))
            {
                this.ShowAlert(Localization.Errors.EmptyTitleField, ToastLength.Long);
                OnUploadEnded();
                return;
            }

            var operationResult = await UploadPhoto(_path);
            if (IsFinishing || IsDestroyed)
                return;

            if (!operationResult.IsSuccess)
            {
                SplashActivity.Cache.EvictAll();
                operationResult = await UploadPhoto(_path);
                if (IsFinishing || IsDestroyed)
                    return;
            }

            if (!operationResult.IsSuccess)
            {
                this.ShowAlert(operationResult.Error.Message);
                OnUploadEnded();
                return;
            }

            _model.Media = new[] { operationResult.Result };
            _model.Title = _title.Text;
            _model.Description = _description.Text;
            _model.Tags = _localTagsAdapter.LocalTags.ToArray();
            TryUpload();
        }

        private async Task<OperationResult<MediaModel>> UploadPhoto(string path)
        {
            Stream stream = null;
            FileInputStream fileInputStream = null;

            try
            {
                var photo = new Java.IO.File(path);
                fileInputStream = new FileInputStream(photo);
                stream = new StreamConverter(fileInputStream, null);

                var request = new UploadMediaModel(BasePresenter.User.UserInfo, stream, System.IO.Path.GetExtension(path));
                var serverResult = await Presenter.TryUploadMedia(request);
                return serverResult;
            }
            catch (Exception ex)
            {
                AppSettings.Reporter.SendCrash(ex);
                return new OperationResult<MediaModel>(new ApplicationError(Localization.Errors.PhotoProcessingError));
            }
            finally
            {
                fileInputStream?.Close(); // ??? change order?
                stream?.Flush();
                fileInputStream?.Dispose();
                stream?.Dispose();
            }
        }

        private async void TryUpload()
        {
            if (_model.Media == null)
            {
                OnUploadEnded();
                return;
            }

            var resp = await Presenter.TryCreatePost(_model);
            if (IsFinishing || IsDestroyed)
                return;

            if (resp.IsSuccess)
            {
                BasePresenter.User.UserInfo.LastPostTime = DateTime.Now;
                OnUploadEnded();
                BasePresenter.ProfileUpdateType = ProfileUpdateType.Full;
                this.ShowAlert(Localization.Messages.PostDelay, ToastLength.Long);
                Finish();
            }
            else
            {
                if (resp.Error is TaskCanceledError)
                    return;

                this.ShowInteractiveMessage(resp.Error.Message, TryAgainAction, ForgetAction);
            }
        }

        private void OnUploadEnded()
        {
            _postButton.Enabled = true;
            _postButton.Text = Localization.Texts.PublishButtonText;

            _loadingSpinner.Visibility = ViewStates.Gone;

            _title.Enabled = true;
            _description.Enabled = true;
            _tag.Enabled = true;
            _localTagsAdapter.Enabled = true;
        }

        private void ForgetAction(object o, DialogClickEventArgs dialogClickEventArgs)
        {
            OnUploadEnded();
        }

        private void TryAgainAction(object o, DialogClickEventArgs dialogClickEventArgs)
        {
            TryUpload();
        }

        private void HideTagsList()
        {
            var txt = _tag.Text = _tag.Text.Trim();
            if (!string.IsNullOrEmpty(txt))
            {
                _tag.Text = string.Empty;
                AddTag(txt);
            }

            Window.SetSoftInputMode(SoftInput.AdjustPan);
            _tag.ClearFocus();
            AnimateTagsLayout(Resource.Id.description_layout);
            HideKeyboard();
        }
    }
}
