///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Hubstream 
//
// @Author: Deva
//
//TODO: Re-write the working comments after re-work and util thought through
//
// Purpose: 
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;

namespace QueueReceiver
{
    public interface IConcurrentAsyncAwaitRunner
    {
        #region Public Methods
        Task ExecuteAsync();
        Task ReleaseAsync();
        #endregion

        #region RunTask
        /// <summary>
        /// This event is used for subscribing and unsubscribing to TaskReceiver , which would 
        /// help us receive the tasks to run concurrently , whenever semaphoreSlim has some room available
        /// </summary>
        event Func<RunTaskEventArgs, Task> RunTask;
        #endregion
    }
}
