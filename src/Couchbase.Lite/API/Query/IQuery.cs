﻿// 
// IQuery.cs
// 
// Author:
//     Jim Borden  <jim.borden@couchbase.com>
// 
// Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
using System;
using System.Threading.Tasks;

namespace Couchbase.Lite.Query
{
    /// <summary>
    /// Arguments for the <see cref="ILiveQuery.Changed" /> event
    /// </summary>
    public sealed class LiveQueryChangedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the error that occurred, if any
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Gets the updated rows of the query
        /// </summary>
        public IResultSet Rows { get; }

        #endregion

        #region Constructors

        internal LiveQueryChangedEventArgs(IResultSet rows, Exception e = null)
        {
            Rows = rows;
            Error = e;
        }

        #endregion
    }

    /// <summary>
    /// An interface representing a runnable query over a data source
    /// </summary>
    public interface IQuery : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets or sets the parameter collection for this query so that parameters may be
        /// added for substitution into the query API (via <see cref="Expression.Parameter(int)"/>
        /// or <see cref="Expression.Parameter(string)"/>)
        /// </summary>
        /// <remarks>
        /// The returned collection is a copy, and must be reset onto the query instance.
        /// Doing so will trigger a re-run and update any listeners.
        /// </remarks>
        QueryParameters Parameters { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a change listener to track when this query instance has a change in
        /// its results.  Adding the first change listener will begin the live semantics.
        /// </summary>
        /// <param name="scheduler">The scheduler to use when firing events</param>
        /// <param name="handler">The handler to call when the query result set changes</param>
        /// <returns>A token that can be used to remove the listener later</returns>
        ListenerToken AddChangeListener(TaskScheduler scheduler, EventHandler<LiveQueryChangedEventArgs> handler);

        /// <summary>
        /// Removes a changes listener based on the token that was received from
        /// <see cref="AddChangeListener"/>
        /// </summary>
        /// <param name="token">The received token from adding the change listener</param>
        void RemoveChangeListener(ListenerToken token);

        /// <summary>
        /// Runs the query
        /// </summary>
        /// <returns>The results of running the query</returns>
        IResultSet Execute();

        #endregion
    }
}
