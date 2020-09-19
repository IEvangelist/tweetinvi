﻿using System;
using System.Net;
using Tweetinvi.Models;

namespace Tweetinvi.Core.Exceptions
{
    public interface ITwitterException
    {
        WebException WebException { get; }

        string URL { get; }
        int StatusCode { get; }
        string TwitterDescription { get; }
        DateTimeOffset CreationDate { get; }
        ITwitterExceptionInfo[] TwitterExceptionInfos { get; }
        ITwitterQuery TwitterQuery { get; }
    }
}