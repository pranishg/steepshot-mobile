﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Models.Requests;
using Steepshot.Core.Models.Responses;
using Steepshot.Core.Errors;
using Steepshot.Core.Models.Enums;

namespace Steepshot.Core.Presenters
{
    public class BasePostPresenter : ListPresenter<Post>
    {
        private const int VoteDelay = 3000;
        public static bool IsEnableVote { get; set; }

        protected BasePostPresenter()
        {
            IsEnableVote = true;
        }

        public async Task<ErrorBase> TryDeletePost(Post post)
        {
            if (post == null)
                return null;

            var error = await TryRunTask(DeletePost, OnDisposeCts.Token, post);

            NotifySourceChanged(nameof(TryDeletePost), true);

            return error;
        }

        private async Task<ErrorBase> DeletePost(Post post, CancellationToken ct)
        {
            var request = new DeleteModel(User.UserInfo, post);
            var response = await Api.DeletePostOrComment(request, ct);
            if (response.IsSuccess)
            {
                lock (Items)
                {
                    Items.Remove(post);
                }
            }
            return response.Error;
        }

        public async Task<ErrorBase> TryDeleteComment(Post post, Post parentPost)
        {
            if (post == null || parentPost == null)
                return null;

            var error = await TryRunTask(DeleteComment, OnDisposeCts.Token, post, parentPost);

            NotifySourceChanged(nameof(TryDeletePost), true);

            return error;
        }

        private async Task<ErrorBase> DeleteComment(Post post, Post parentPost, CancellationToken ct)
        {
            var request = new DeleteModel(User.UserInfo, post, parentPost);
            var response = await Api.DeletePostOrComment(request, ct);
            if (response.IsSuccess)
            {
                lock (Items)
                {
                    Items.Remove(post);
                }
            }
            return response.Error;
        }

        public void HidePost(Post post)
        {
            if (!User.PostBlackList.Contains(post.Url))
            {
                User.PostBlackList.Add(post.Url);
                User.Save();
            }

            lock (Items)
                Items.Remove(post);
            NotifySourceChanged(nameof(HidePost), true);
        }

        public Post FirstOrDefault(Func<Post, bool> func)
        {
            lock (Items)
                return Items.FirstOrDefault(func);
        }

        public int IndexOf(Post post)
        {
            lock (Items)
                return Items.IndexOf(post);
        }

        protected bool ResponseProcessing(OperationResult<ListResponse<Post>> response, int itemsLimit, out ErrorBase error, string sender, bool isNeedClearItems = false, bool enableEmptyMedia = false)
        {
            error = null;
            if (response == null)
                return false;

            if (response.IsSuccess)
            {
                var results = response.Result.Results;
                if (results.Count > 0)
                {
                    var isAdded = false;
                    lock (Items)
                    {
                        if (isNeedClearItems)
                            Clear(false);

                        foreach (var item in results)
                        {
                            if (User.PostBlackList.Contains(item.Url))
                                continue;

                            if (!Items.Any(itm => itm.Url.Equals(item.Url, StringComparison.OrdinalIgnoreCase)) && (enableEmptyMedia || item.Media.Length > 0 && !string.IsNullOrEmpty(item.Media[0].Url)))
                            {
                                Items.Add(item);
                                isAdded = true;
                            }
                        }
                    }

                    if (isAdded)
                    {
                        OffsetUrl = response.Result.Offset;
                    }
                    else if (!string.Equals(OffsetUrl, response.Result.Offset, StringComparison.OrdinalIgnoreCase))
                    {
                        OffsetUrl = response.Result.Offset;
                        return true;
                    }

                    NotifySourceChanged(sender, isAdded);
                }
                if (results.Count < Math.Min(ServerMaxCount, itemsLimit))
                    IsLastReaded = true;
            }
            error = response.Error;
            return false;
        }

        public async Task<ErrorBase> TryVote(Post post)
        {
            if (post == null || post.VoteChanging || post.FlagChanging)
                return null;

            post.VoteChanging = true;
            IsEnableVote = false;
            NotifySourceChanged(nameof(TryVote), true);

            var error = await TryRunTask(Vote, OnDisposeCts.Token, post);

            post.VoteChanging = false;
            IsEnableVote = true;
            NotifySourceChanged(nameof(TryVote), true);

            return error;
        }

        private async Task<ErrorBase> Vote(Post post, CancellationToken ct)
        {
            var wasFlaged = post.Flag;
            var request = new VoteModel(User.UserInfo, post.Vote ? VoteType.Down : VoteType.Up, post.Url);
            var response = await Api.Vote(request, ct);

            if (response.IsSuccess)
            {
                var td = DateTime.Now - response.Result.VoteTime;
                if (VoteDelay > td.Milliseconds)
                    await Task.Delay(VoteDelay - td.Milliseconds, ct);

                post.NetVotes = response.Result.NetVotes;
                ChangeLike(post, wasFlaged);
                post.TotalPayoutReward = response.Result.NewTotalPayoutReward;
            }
            else if (response.Error is BlockchainError
                && response.Error.Message.Contains(Localization.Errors.VotedInASimilarWay)) //TODO:KOA: unstable solution
            {
                response.Error = null;
                ChangeLike(post, wasFlaged);
            }

            return response.Error;
        }

        private void ChangeLike(Post post, bool wasFlaged)
        {
            post.Vote = !post.Vote;
            post.Flag = false;
            post.NetLikes = post.Vote ? post.NetLikes + 1 : post.NetLikes - 1;
            if (wasFlaged)
                post.NetFlags--;
        }

        public async Task<ErrorBase> TryFlag(Post post)
        {
            if (post == null || post.VoteChanging || post.FlagChanging)
                return null;

            post.FlagNotificationWasShown = post.Flag;
            post.FlagChanging = true;
            IsEnableVote = false;
            NotifySourceChanged(nameof(TryFlag), true);

            var error = await TryRunTask(Flag, OnDisposeCts.Token, post);

            post.FlagChanging = false;
            IsEnableVote = true;
            NotifySourceChanged(nameof(TryFlag), true);

            return error;
        }

        private async Task<ErrorBase> Flag(Post post, CancellationToken ct)
        {
            var wasVote = post.Vote;
            var request = new VoteModel(User.UserInfo, post.Flag ? VoteType.Down : VoteType.Flag, post.Url);
            var response = await Api.Vote(request, ct);

            if (response.IsSuccess)
            {
                post.TotalPayoutReward = response.Result.NewTotalPayoutReward;
                post.NetVotes = response.Result.NetVotes;
                ChangeFlag(post, wasVote);
                var td = DateTime.Now - response.Result.VoteTime;
                if (VoteDelay > td.Milliseconds)
                    await Task.Delay(VoteDelay - td.Milliseconds, ct);
            }
            else if (response.Error is BlockchainError
                     && response.Error.Message.Contains(Localization.Errors.VotedInASimilarWay)) //TODO:KOA: unstable solution
            {
                response.Error = null;
                ChangeFlag(post, wasVote);
            }
            return response.Error;
        }

        private void ChangeFlag(Post post, bool wasVote)
        {
            post.Flag = !post.Flag;
            post.Vote = false;
            post.NetFlags = post.Flag ? post.NetFlags + 1 : post.NetFlags - 1;
            if (wasVote)
                post.NetLikes--;
        }
    }
}
