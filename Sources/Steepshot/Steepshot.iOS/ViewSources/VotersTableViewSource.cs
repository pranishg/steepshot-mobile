﻿using Foundation;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Presenters;
using Steepshot.iOS.Cells;
using UIKit;

namespace Steepshot.iOS.ViewSources
{
    public class VotersTableViewSource : BaseUiTableViewSource<UserFriend>
    {
        private const string CellIdentifier = nameof(UsersSearchViewCell);
        public event RowSelectedHandler RowSelectedEvent;

        public VotersTableViewSource(UserFriendPresenter presenter) : base(presenter) { }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = (UsersSearchViewCell)tableView.DequeueReusableCell(CellIdentifier, indexPath);
            var user = Presenter[indexPath.Row];
            if (user != null)
                cell.UpdateCell(user);
            return cell;
        }

        public override void RowHighlighted(UITableView tableView, NSIndexPath rowIndexPath)
        {
            RowSelectedEvent?.Invoke(rowIndexPath.Row);
        }
    }
}
