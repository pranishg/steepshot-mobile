﻿using System;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Square.Picasso;
using Steepshot.Core;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Presenters;
using Steepshot.Utils;

namespace Steepshot.Adapter
{

    public class CommentAdapter : RecyclerView.Adapter
    {
        private readonly CommentsPresenter _commentsPresenter;
        private readonly Context _context;
        public Action<Post> LikeAction, UserAction, FlagAction;
        public override int ItemCount => _commentsPresenter.Count;
        private bool _isEnableVote;
        public bool IsEnableVote
        {
            get => _isEnableVote;
            set
            {
                _isEnableVote = value;
                NotifyDataSetChanged();
            }
        }

        public CommentAdapter(Context context, CommentsPresenter commentsPresenter)
        {
            _context = context;
            _commentsPresenter = commentsPresenter;
            _isEnableVote = true;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var post = _commentsPresenter[position];
            if (post == null)
                return;

            var vh = (CommentViewHolder)holder;
            vh.UpdateData(post, _context, _isEnableVote);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.lyt_comment_item, parent, false);
            var vh = new CommentViewHolder(itemView, LikeAction, UserAction, FlagAction);
            return vh;
        }
    }

    public class CommentViewHolder : ItemSwipeViewHolder
    {
        private readonly ImageView _avatar;
        private readonly TextView _author;
        private readonly TextView _comment;
        private readonly TextView _cost;
        private readonly TextView _reply;
        private readonly TextView _time;
        private readonly ImageButton _like;
        private readonly Suboption _flag;
        private readonly Action<Post> _likeAction;
        private readonly Action<Post> _flagAction;
        private readonly Animation _likeSetAnimation;
        private readonly Animation _likeWaitAnimation;
        private readonly Action<Post> _userAction;
        private readonly TextView _likes;

        private static bool _isEnableVote = true;

        private Post _post;

        public CommentViewHolder(View itemView, Action<Post> likeAction, Action<Post> userAction, Action<Post> flagAction) : base(itemView)
        {
            _userAction = userAction;
            var context = itemView.RootView.Context;
            _avatar = itemView.FindViewById<Refractored.Controls.CircleImageView>(Resource.Id.avatar);
            _author = itemView.FindViewById<TextView>(Resource.Id.sender_name);
            _comment = itemView.FindViewById<TextView>(Resource.Id.comment_text);
            _likes = itemView.FindViewById<TextView>(Resource.Id.likes);
            _cost = itemView.FindViewById<TextView>(Resource.Id.cost);
            _like = itemView.FindViewById<ImageButton>(Resource.Id.like_btn);
            _reply = itemView.FindViewById<TextView>(Resource.Id.reply_btn);
            _time = itemView.FindViewById<TextView>(Resource.Id.time);

            _author.Typeface = Style.Semibold;
            _comment.Typeface = _likes.Typeface = _cost.Typeface = _reply.Typeface = Style.Regular;

            _likeAction = likeAction;
            _flagAction = flagAction;

            _like.Click += Like_Click;
            _avatar.Click += UserAction;
            _author.Click += UserAction;
            _cost.Click += UserAction;

            _likeSetAnimation = AnimationUtils.LoadAnimation(context, Resource.Animation.like_set);
            _likeSetAnimation.AnimationStart += (sender, e) => _like.SetImageResource(Resource.Drawable.ic_new_like_filled);
            _likeSetAnimation.AnimationEnd += (sender, e) => _like.StartAnimation(_likeWaitAnimation);
            _likeWaitAnimation = AnimationUtils.LoadAnimation(context, Resource.Animation.like_wait);

            _flag = new Suboption(itemView.Context);
            _flag.SetImageResource(Resource.Drawable.ic_flag);
            _flag.Click += Flag_Click;
            SubOptions.Add(_flag);
        }

        private void UserAction(object sender, EventArgs e)
        {
            _userAction?.Invoke(_post);
        }

        private void Flag_Click(object sender, EventArgs e)
        {
            if (!_isEnableVote)
                return;

            if (BasePresenter.User.IsAuthenticated)
                _flag.SetImageResource(!_post.Flag ? Resource.Drawable.ic_flag_active : Resource.Drawable.ic_flag);

            _flagAction?.Invoke(_post);
        }

        private void Like_Click(object sender, EventArgs e)
        {
            if (!_isEnableVote)
                return;

            _likeAction?.Invoke(_post);
        }

        public void UpdateData(Post post, Context context, bool isEnableVote)
        {
            _isEnableVote = isEnableVote;
            _post = post;
            _author.Text = post.Author;
            _comment.Text = post.Body;

            if (!string.IsNullOrEmpty(post.Avatar))
                Picasso.With(context).Load(post.Avatar).Resize(300, 0).Into(_avatar);
            else
                _avatar.SetImageResource(Resource.Drawable.ic_user_placeholder);

            _like.ClearAnimation();
            if (_isEnableVote)
            {
                if (post.Vote.HasValue)
                {
                    _like.SetImageResource(post.Vote.Value ? Resource.Drawable.ic_new_like_filled : Resource.Drawable.ic_new_like_selected);
                }
                else
                    _like.StartAnimation(_likeSetAnimation);
            }
            else
            {
                _like.StartAnimation(post.Vote.HasValue ? _likeWaitAnimation : _likeSetAnimation);
            }

            _flag.SetImageResource(post.Flag ? Resource.Drawable.ic_flag_active : Resource.Drawable.ic_flag);
            _likes.Text = $"{post.NetVotes} {Localization.Messages.Likes}";
            _cost.Text = BasePresenter.ToFormatedCurrencyString(post.TotalPayoutReward);
            _time.Text = post.Created.ToPostTime();
        }
    }
}
