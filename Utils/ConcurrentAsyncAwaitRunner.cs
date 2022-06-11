///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Purpose: 
//
// TODO: Provide ways to start and stop the ExecuteAsync and ReleaseAsync flow in the ConcurrentAsyncAwaitRunner
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace QueueReceiver
{
    public sealed class ConcurrentAsyncAwaitRunner : IConcurrentAsyncAwaitRunner
    {
        #region Private Members
        /// <summary>
        /// Cancellation Token (used to trigger cancellation)
        /// </summary>
        private readonly CancellationToken cancellationToken;
        /// <summary>
        /// SemaPhoreSlim
        /// </summary>
        private SemaphoreSlim semaphoreSlim;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Max Degree of Parallelism</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <param name="stopExecutionIfFailureEncountered">If true, we should stop execution if any failure or cancel encountered. Otherwise all task is executed</param>
        public ConcurrentAsyncAwaitRunner(Int32 maxDegreeOfParallelism,
                                        CancellationToken cancellationToken = default,
                                        Boolean stopExecutionIfFailureEncountered = false)
        {
            Contract.Assert(maxDegreeOfParallelism > 0, "Degree of Parallelism must be positive");
            Contract.Assert(cancellationToken != null, "Cancellation token cannot be null");

            this.semaphoreSlim = new SemaphoreSlim(maxDegreeOfParallelism);
            this.cancellationToken = cancellationToken;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public async Task ExecuteAsync()
        {
            Int32 availableRoom = GetAvailableRoom();
            //Triggers the event with the available room
            //any module or class using this concurrentAsyncAwaitRunner must implement an handler for this, which internally
            //takes the available room, does it concurrent processes from the availableroom 

            //the handler should start those many concurrent task.Run , fireandforget it
            //messageHandler should subscribe the consumers, register some releaseHandler
            //need to establish communication from messageHandler to consumer to call some release method to release semaphore, and do next execution
            while (availableRoom > 0)
            {
                await this.semaphoreSlim.WaitAsync(this.cancellationToken);
                availableRoom -= 1;
                RunTask?.Invoke(new RunTaskEventArgs(1));
            }

        }
        /// <summary>
        /// 
        /// </summary>
        public async Task ReleaseAsync()
        {
            try
            {
                this.semaphoreSlim.Release();
            }
            catch (ObjectDisposedException) { }

            await ExecuteAsync();
        }
        #endregion

        #region ConcurrentAsyncAwait Events
        /// <summary>
        /// This event is used for subscribing and unsubscribing to TaskReceiver , which would 
        /// help us receive the tasks to run concurrently , whenever semaphoreSlim has some room available
        /// </summary>
        public event Func<RunTaskEventArgs, Task> RunTask;
        #endregion

        #region Private Methods
        /// <summary>
        /// Gets the available room from the current semaphoreSlim state
        /// </summary>
        /// <returns>Available Room</returns>
        private Int32 GetAvailableRoom()
        {
            Int32 currentCount = 0;
            try
            {
                currentCount = this.semaphoreSlim.CurrentCount;
            }
            catch
            {
            }
            return currentCount;
        }
        #endregion
    }
}