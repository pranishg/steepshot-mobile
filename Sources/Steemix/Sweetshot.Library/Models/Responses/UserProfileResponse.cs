using System;

namespace Sweetshot.Library.Models.Responses
{
    ///{
    ///  "posting_rewards": 0,
    ///  "curation_rewards": 168,
    ///  "last_account_update": "2017-01-13T14:42:51Z",
    ///  "last_vote_time": "2017-01-16T15:06:45Z",
    ///  "reputation": 34,
    ///  "post_count": 70,
    ///  "comment_count": 0,
    ///  "followers_count": 62,
    ///  "following_count": 13,
    ///  "username": "joseph.kalu",
    ///  "current_username": "joseph.kalu",
    ///  "profile_image": "http://vignette2.wikia.nocookie.net/tomandjerry/images/6/6d/Tom-tom-and-jerry.png/revision/latest?cb=20140627113049",
    ///  "has_followed": 0,
    ///  "estimated_balance": "3.27"
    ///}
    public class UserProfileResponse
    {
        public int PostingRewards { get; set; }
        public int CurationRewards { get; set; }
        public DateTime LastAccountUpdate { get; set; }
        public DateTime LastVoteTime { get; set; }
        public int Reputation { get; set; }
        public int PostCount { get; set; }
        public int CommentCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public string Username { get; set; }
        public string CurrentUsername { get; set; }
        public string ProfileImage { get; set; }
        public int HasFollowed { get; set; }
        public double EstimatedBalance { get; set; }
    }
}