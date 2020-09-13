﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi.Client.Tools;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Core.Helpers;
using Tweetinvi.Core.Streaming;
using Tweetinvi.Core.Wrappers;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Tweetinvi.Streaming;
using Tweetinvi.Streams.Helpers;
using Tweetinvi.Streams.Properties;

namespace Tweetinvi.Streams
{
    public class FilteredStream : TrackedStream, IFilteredStream
    {
        private IStreamTrackManager<ITweet> StreamTrackManager { get; }
        private readonly ITwitterClient _client;
        private readonly IFilterStreamTweetMatcherFactory _filterStreamTweetMatcherFactory;
        private readonly ITwitterClientFactories _factories;

        // Const
        private const int MAXIMUM_TRACKED_LOCATIONS_AUTHORIZED = 25;
        private const int MAXIMUM_TRACKED_USER_ID_AUTHORIZED = 5000;

        // Filters
        public MatchOn MatchOn { get; set; }
        private IFilterStreamTweetMatcher _filterStreamTweetMatcher;

        // Properties
        private readonly Dictionary<long?, Action<ITweet>> _followingUserIds;
        public Dictionary<long?, Action<ITweet>> FollowingUserIds => _followingUserIds;

        private readonly Dictionary<ILocation, Action<ITweet>> _locations;
        public Dictionary<ILocation, Action<ITweet>> Locations => _locations;

        // Constructor
        public FilteredStream(
            ITwitterClient client,
            IStreamTrackManager<ITweet> streamTrackManager,
            IFilterStreamTweetMatcherFactory filterStreamTweetMatcherFactory,
            IJsonObjectConverter jsonObjectConverter,
            IJObjectStaticWrapper jObjectStaticWrapper,
            IStreamResultGenerator streamResultGenerator,
            ITwitterClientFactories factories,
            ICreateFilteredTweetStreamParameters createFilteredTweetStreamParameters)

            : base(client, streamTrackManager,
                jsonObjectConverter,
                jObjectStaticWrapper,
                streamResultGenerator,
                factories,
                createFilteredTweetStreamParameters)
        {
            StreamTrackManager = streamTrackManager;
            _client = client;
            _filterStreamTweetMatcherFactory = filterStreamTweetMatcherFactory;
            _factories = factories;

            _followingUserIds = new Dictionary<long?, Action<ITweet>>();
            _locations = new Dictionary<ILocation, Action<ITweet>>();

            MatchOn = MatchOn.Everything;
        }

        public async Task StartMatchingAnyConditionAsync()
        {
            _filterStreamTweetMatcher = _filterStreamTweetMatcherFactory.Create(StreamTrackManager, _locations, _followingUserIds);

            ITwitterRequest CreateTwitterRequest()
            {
                var queryBuilder = GenerateORFilterQuery();
                AddBaseParametersToQuery(queryBuilder);

                var request = _client.CreateRequest();
                request.Query.Url = queryBuilder.ToString();
                request.Query.HttpMethod = HttpMethod.POST;
                return request;
            }

            void OnJsonReceived(string json)
            {
                RaiseJsonObjectReceived(json);

                if (IsEvent(json))
                {
                    TryInvokeGlobalStreamMessages(json);
                    return;
                }

                var tweet = _factories.CreateTweet(json);

                var matchingTracksEvenArgs = _filterStreamTweetMatcher.GetMatchingTweetEventArgsAndRaiseMatchingElements(tweet, json, MatchOn);

                var matchingTracks = matchingTracksEvenArgs.MatchingTracks;
                var matchingLocations = matchingTracksEvenArgs.MatchingLocations;
                var matchingFollowers = matchingTracksEvenArgs.MatchingFollowers;

                var isTweetMatching = matchingTracks.Length != 0 || matchingLocations.Length != 0 || matchingFollowers.Length != 0;

                var quotedTweetMatchingTracks = matchingTracksEvenArgs.QuotedTweetMatchingTracks;
                var quotedTweetMatchingLocations = matchingTracksEvenArgs.QuotedTweetMatchingLocations;
                var quotedTweetMatchingFollowers = matchingTracksEvenArgs.QuotedTweetMatchingFollowers;

                var isQuotedTweetMatching = quotedTweetMatchingTracks.Length != 0 || quotedTweetMatchingLocations.Length != 0 || quotedTweetMatchingFollowers.Length != 0;

                RaiseTweetReceived(matchingTracksEvenArgs);

                if (isTweetMatching || isQuotedTweetMatching)
                {
                    RaiseMatchingTweetReceived(matchingTracksEvenArgs);
                }
                else
                {
                    RaiseNonMatchingTweetReceived(new TweetEventArgs(tweet, json));
                }
            }

            await _streamResultGenerator.StartAsync(OnJsonReceived, CreateTwitterRequest).ConfigureAwait(false);
        }

        public async Task StartMatchingAllConditionsAsync()
        {
            _filterStreamTweetMatcher = _filterStreamTweetMatcherFactory.Create(StreamTrackManager, _locations, _followingUserIds);

            ITwitterRequest CreateTwitterRequest()
            {
                var queryBuilder = GenerateANDFilterQuery();
                AddBaseParametersToQuery(queryBuilder);

                var twitterRequest = _client.CreateRequest();
                twitterRequest.Query.Url = queryBuilder.ToString();
                twitterRequest.Query.HttpMethod = HttpMethod.POST;
                return twitterRequest;
            }

            void JsonReceived(string json)
            {
                RaiseJsonObjectReceived(json);

                if (IsEvent(json))
                {
                    TryInvokeGlobalStreamMessages(json);
                    return;
                }

                var tweet = _factories.CreateTweet(json);

                var matchingTracksEvenArgs = _filterStreamTweetMatcher.GetMatchingTweetEventArgsAndRaiseMatchingElements(tweet, json, MatchOn);

                var matchingTracks = matchingTracksEvenArgs.MatchingTracks;
                var matchingLocations = matchingTracksEvenArgs.MatchingLocations;
                var matchingFollowers = matchingTracksEvenArgs.MatchingFollowers;

                var quotedTweetMatchingTracks = matchingTracksEvenArgs.QuotedTweetMatchingTracks;
                var quotedTweetMatchingLocations = matchingTracksEvenArgs.QuotedTweetMatchingLocations;
                var quotedTweetMatchingFollowers = matchingTracksEvenArgs.QuotedTweetMatchingFollowers;

                RaiseTweetReceived(matchingTracksEvenArgs);

                if (DoestTheTweetMatchAllConditions(tweet, matchingTracks, matchingLocations, matchingFollowers) || DoestTheTweetMatchAllConditions(tweet, quotedTweetMatchingTracks, quotedTweetMatchingLocations, quotedTweetMatchingFollowers))
                {
                    RaiseMatchingTweetReceived(matchingTracksEvenArgs);
                }
                else
                {
                    RaiseNonMatchingTweetReceived(new TweetEventArgs(tweet, json));
                }
            }

            await _streamResultGenerator.StartAsync(JsonReceived, CreateTwitterRequest).ConfigureAwait(false);
        }

        public MatchOn CheckIfTweetMatchesStreamFilters(ITweet tweet)
        {
            return _filterStreamTweetMatcher.GetMatchingTweetEventArgsAndRaiseMatchingElements(tweet, null, MatchOn).MatchOn;
        }

        private bool DoestTheTweetMatchAllConditions(ITweet tweet, string[] matchingTracks, ILocation[] matchingLocations, long[] matchingFollowers)
        {
            if (tweet == null || tweet.CreatedBy.Id == default)
            {
                return false;
            }

            bool followMatches = FollowingUserIds.IsEmpty() || matchingFollowers.Any();
            bool tracksMatches = Tracks.IsEmpty() || matchingTracks.Any();
            bool locationMatches = Locations.IsEmpty() || matchingLocations.Any();

            if (FollowingUserIds.Any())
            {
                return followMatches && tracksMatches && locationMatches;
            }

            if (Tracks.Any())
            {
                return tracksMatches && locationMatches;
            }

            if (Locations.Any())
            {
                return locationMatches;
            }

            return true;
        }

        #region Generate Query

        private StringBuilder GenerateORFilterQuery()
        {
            var queryBuilder = new StringBuilder(Resources.Stream_Filter);

            var followPostRequest = QueryGeneratorHelper.GenerateFilterFollowRequest(FollowingUserIds.Keys.ToList());
            var trackPostRequest = QueryGeneratorHelper.GenerateFilterTrackRequest(Tracks.Keys.ToList());
            var locationPostRequest = QueryGeneratorHelper.GenerateFilterLocationRequest(Locations.Keys.ToList());

            if (!string.IsNullOrEmpty(trackPostRequest))
            {
                queryBuilder.Append(trackPostRequest);
            }

            if (!string.IsNullOrEmpty(followPostRequest))
            {
                queryBuilder.Append(queryBuilder.Length == 0 ? followPostRequest : string.Format("&{0}", followPostRequest));
            }

            if (!string.IsNullOrEmpty(locationPostRequest))
            {
                queryBuilder.Append(queryBuilder.Length == 0 ? locationPostRequest : string.Format("&{0}", locationPostRequest));
            }

            return queryBuilder;
        }

        private StringBuilder GenerateANDFilterQuery()
        {
            var queryBuilder = new StringBuilder(Resources.Stream_Filter);

            var followPostRequest = QueryGeneratorHelper.GenerateFilterFollowRequest(FollowingUserIds.Keys.ToList());
            var trackPostRequest = QueryGeneratorHelper.GenerateFilterTrackRequest(Tracks.Keys.ToList());
            var locationPostRequest = QueryGeneratorHelper.GenerateFilterLocationRequest(Locations.Keys.ToList());

            if (!string.IsNullOrEmpty(followPostRequest))
            {
                queryBuilder.Append(followPostRequest);
            }
            else if (!string.IsNullOrEmpty(trackPostRequest))
            {
                queryBuilder.Append(trackPostRequest);
            }
            else if (!string.IsNullOrEmpty(locationPostRequest))
            {
                queryBuilder.Append(locationPostRequest);
            }

            return queryBuilder;
        }

        #endregion

        #region Follow

        public void AddFollow(long userId, Action<ITweet> userPublishedTweet = null)
        {
            if (userId > 0 && _followingUserIds.Count < MAXIMUM_TRACKED_USER_ID_AUTHORIZED)
            {
                _followingUserIds.Add(userId, userPublishedTweet);
            }
        }

        public void AddFollow(IUserIdentifier user, Action<ITweet> userPublishedTweet = null)
        {
            if (user == null || user.Id <= 0)
            {
                throw new Exception("Follow parameter only accept user ids, user names cannot be used.");
            }

            AddFollow(user.Id, userPublishedTweet);
        }

        public void RemoveFollow(long userId)
        {
            _followingUserIds.Remove(userId);
        }

        public void RemoveFollow(IUserIdentifier user)
        {
            if (user != null)
            {
                RemoveFollow(user.Id);
            }
        }

        public bool ContainsFollow(long userId)
        {
            return _followingUserIds.Keys.Contains(userId);
        }

        public bool ContainsFollow(IUserIdentifier user)
        {
            if (user != null)
            {
                return ContainsFollow(user.Id);
            }

            return false;
        }

        public void ClearFollows()
        {
            _followingUserIds.Clear();
        }

        #endregion

        #region Location

        public ILocation AddLocation(ICoordinates coordinate1, ICoordinates coordinate2, Action<ITweet> locationDetected = null)
        {
            ILocation location = new Location(coordinate1, coordinate2);
            AddLocation(location, locationDetected);

            return location;
        }

        public void AddLocation(ILocation location, Action<ITweet> locationDetected = null)
        {
            if (!ContainsLocation(location) && _locations.Count < MAXIMUM_TRACKED_LOCATIONS_AUTHORIZED)
            {
                Locations.Add(location, locationDetected);
            }
        }

        public void RemoveLocation(ICoordinates coordinate1, ICoordinates coordinate2)
        {
            var location = Locations.Keys.FirstOrDefault(x => (x.Coordinate1 == coordinate1 && x.Coordinate2 == coordinate2) ||
                                                              (x.Coordinate1 == coordinate2 && x.Coordinate2 == coordinate1));

            if (location != null)
            {
                Locations.Remove(location);
            }
        }

        public void RemoveLocation(ILocation location)
        {
            RemoveLocation(location.Coordinate1, location.Coordinate2);
        }

        public bool ContainsLocation(ICoordinates coordinate1, ICoordinates coordinate2)
        {
            return Locations.Keys.Any(x => (x.Coordinate1 == coordinate1 && x.Coordinate2 == coordinate2) ||
                                           (x.Coordinate1 == coordinate2 && x.Coordinate2 == coordinate1));
        }

        public bool ContainsLocation(ILocation location)
        {
            return ContainsLocation(location.Coordinate1, location.Coordinate2);
        }

        public void ClearLocations()
        {
            Locations.Clear();
        }



        #endregion
    }
}