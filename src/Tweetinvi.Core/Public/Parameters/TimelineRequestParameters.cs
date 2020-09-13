﻿namespace Tweetinvi.Parameters
{
    public interface ITimelineRequestParameters : IMinMaxQueryParameters, ITweetModeParameter
    {
        /// <summary>
        /// If set to true, the creator property (IUser) will only contain the id.
        /// </summary>
        bool? TrimUser { get; set; }

        /// <summary>
        /// Include tweet entities.
        /// </summary>
        bool? IncludeEntities { get; set; }
    }

    public abstract class TimelineRequestParameters : MinMaxQueryParameters, ITimelineRequestParameters
    {
        protected TimelineRequestParameters()
        {
        }

        protected TimelineRequestParameters(ITimelineRequestParameters source) : base(source)
        {
            TrimUser = source?.TrimUser;
            IncludeEntities = source?.IncludeEntities;
            TweetMode = source?.TweetMode;
        }

        /// <inheritdoc/>
        public bool? TrimUser { get; set; }
        /// <inheritdoc/>
        public bool? IncludeEntities { get; set; }
        /// <inheritdoc/>
        public TweetMode? TweetMode { get; set; }
    }
}