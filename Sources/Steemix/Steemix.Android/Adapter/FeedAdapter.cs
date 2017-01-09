﻿using System;
using Android.Support.V7.Widget;
using Android.Views;
using Steemix.Library.Models.Responses;
using Android.Widget;
using Square.Picasso;
using Android.Content;
using Android.Text;
using System.Collections.ObjectModel;

namespace Steemix.Android.Adapter
{

	public class FeedAdapter : RecyclerView.Adapter
	{
		ObservableCollection<UserPost> Posts;
		private Context context;
		string CommentPattern ="<b>{0}</b> {1}";

		public Action<int> LikeAction;

        public FeedAdapter(Context context, ObservableCollection<UserPost> Posts)
        {
            this.context = context;
            this.Posts = Posts;
        }

		public void Clear()
		{
			Posts.Clear();
			NotifyDataSetChanged();
		}

        public UserPost GetItem(int position)
        {
             return Posts[position];
        }
        public override int ItemCount
        {
            get
            {
                return Posts.Count;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            FeedViewHolder vh = holder as FeedViewHolder;
            vh.Photo.SetImageResource(0);
            vh.Author.Text = Posts[position].Author;
			if (Posts[position].Title != null)
			{
				vh.FirstComment.Visibility = ViewStates.Visible;
				vh.FirstComment.TextFormatted = Html.FromHtml(string.Format(CommentPattern, Posts[position].Author, Posts[position].Title));
			}
			else
			{
				vh.FirstComment.Visibility = ViewStates.Gone;
			}

			if (Posts[position].CommentsCount > 0)
			{
				vh.CommentSubtitle.Text = string.Format(context.GetString(Resource.String.view_n_comments), Posts[position].CommentsCount);
			}
			else
			{
				vh.CommentSubtitle.Text = context.GetString(Resource.String.first_title_comment);
			}
			vh.UpdateData(Posts[position], context);
            Picasso.With(context).Load(Posts[position].Body).Into(vh.Photo);
			if (!string.IsNullOrEmpty(Posts[position].Avatar)){
				Picasso.With(context).Load(Posts[position].Avatar).Into(vh.Avatar);
			}
			else {
				vh.Avatar.SetImageResource(Resource.Mipmap.ic_launcher);
			}

			vh.Like.SetImageResource((Posts[position].Vote == 0) ? Resource.Drawable.ic_heart : Resource.Drawable.ic_heart_blue);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                    Inflate(Resource.Layout.lyt_feed_item, parent, false);
			FeedViewHolder vh = new FeedViewHolder(itemView,LikeAction);
            return vh;
        }

		public class FeedViewHolder : RecyclerView.ViewHolder
        {
            public ImageView Photo { get; private set; }
            public ImageView Avatar { get; private set; }
            public TextView Author { get; private set; }
			public TextView FirstComment { get; private set; }
			public TextView CommentSubtitle { get; private set; }
			public TextView Time { get; private set; }
			public TextView Likes { get; private set; }
			public TextView Cost { get; private set; }
			public ImageButton Like { get; private set; }
			UserPost post;
			Action<int> LikeAction;

			public FeedViewHolder(View itemView,Action<int> LikeAction) : base(itemView)
            {
                Avatar = itemView.FindViewById<Refractored.Controls.CircleImageView>(Resource.Id.profile_image);
                Author = itemView.FindViewById<TextView>(Resource.Id.author_name);
                Photo = itemView.FindViewById<ImageView>(Resource.Id.photo);
				FirstComment = itemView.FindViewById<TextView>(Resource.Id.first_comment);
				CommentSubtitle = itemView.FindViewById<TextView>(Resource.Id.comment_subtitle);
				Time = itemView.FindViewById<TextView>(Resource.Id.time);
				Likes = itemView.FindViewById<TextView>(Resource.Id.likes);
				Cost = itemView.FindViewById<TextView>(Resource.Id.cost);
				Like = itemView.FindViewById<ImageButton>(Resource.Id.btn_like);

				this.LikeAction = LikeAction;

				Like.Click += Like_Click;
            }

			void Like_Click(object sender, EventArgs e)
			{
				Like.SetImageResource((post.Vote == 0) ? Resource.Drawable.ic_heart_blue : Resource.Drawable.ic_heart);
				LikeAction?.Invoke(AdapterPosition);
			}

			public void UpdateData(UserPost post,Context context)
			{
				this.post = post;
				DateTime date = DateTime.Parse(post.Created);
				TimeSpan span = DateTime.Now - date;

				Likes.Text = string.Format("{0} likes", post.NetVotes);
				Cost.Text = string.Format("${0}", post.TotalPayoutValue);

				if (span.TotalDays > 1){
					Time.Text = string.Format(context.GetString(Resource.String.time_days), (int)span.TotalDays);
				}else if (span.TotalHours > 1) { 
					Time.Text = string.Format(context.GetString(Resource.String.time_hours), (int)span.TotalHours);
				}else if (span.TotalMinutes > 1){
					Time.Text = string.Format(context.GetString(Resource.String.time_mins), (int)span.TotalMinutes);
				}else if (span.TotalSeconds > 1){
					Time.Text = string.Format(context.GetString(Resource.String.time_seconds), (int)span.TotalSeconds);
				}
			}

		}
    }
}