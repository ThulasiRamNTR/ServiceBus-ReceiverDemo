///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Purpose: ServiceBus receiver or processor , when running in PeekLock ReceiveMode provides us PeekLockDuration of maximum 5 minutes only(suppose).
//      And it's auto-renewal can also do renewal upto only some MaxAutoRenewal duration for given number of times in RetryOptions under ReceiverClientOptions
//      These won't be sufficient to do time taking processes in system , and until then the message might expire and move into dead lettered queue
//
//      To avoid those, we can use ServiceBusLeaseObject which renews the lock to default 
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Azure.Messaging.ServiceBus;
using System;
using System.Diagnostics.Contracts;
using System.Timers;

namespace QueueReceiver
{
    public sealed class ServiceBusLeaseObject : IDisposable
    {
        #region Private Members
        /// <summary>
        /// Reference to the storage blob
        /// </summary>
        private readonly ServiceBusReceiver serviceBusReceiver;
        /// <summary>
        /// Internal timer object
        /// </summary>
        private Timer timer;
        /// <summary>
        /// Synchronization Lock
        /// </summary>
        private Object syncLock;
        /// <summary>
        /// In-Progress Flag
        /// </summary>
        private Boolean inProgress;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="storageBlob">Storage Blob</param>
        /// <param name="skipRenewal">Skip Auto Renewal</param>
        public ServiceBusLeaseObject(ServiceBusReceiver serviceBusReceiver, Boolean autoRenewal = false)
        {
            Contract.Assert(serviceBusReceiver != null, "Service Bus receiver cannot be null");

            this.serviceBusReceiver = serviceBusReceiver;
            this.syncLock = new Object();
            if (autoRenewal)
            {
                this.timer = new Timer();
                this.timer.Interval = 8000;
                this.timer.Elapsed += TimerElapsedAsync;
                this.timer.Enabled = true;
                this.timer.Start();
            }

        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~ServiceBusLeaseObject()
        {
            Dispose();
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// ServiceBusReceivedMessage
        /// </summary>
        public ServiceBusReceivedMessage ServiceBusReceivedMessage { get; set; }
        #endregion

        #region Public Methods
        public void Stop()
        {
            lock (this.syncLock)
            {
                if (this.timer != null)
                {
                    this.timer.Stop();
                    this.timer.Enabled = false;
                }
            }
        }

        #endregion

        #region Private Members
        /// <summary>
        /// Timer Elapsed
        /// </summary>
        private async void TimerElapsedAsync(Object sender, ElapsedEventArgs e)
        {
            lock (this.syncLock)
            {
                if (this.inProgress) { return; }
                this.inProgress = true;
            }
            try
            {
                await this.serviceBusReceiver.RenewMessageLockAsync(this.ServiceBusReceivedMessage);
            }
            catch (ServiceBusException ex)
            {
                if (this.timer != null)
                {
                    this.timer.Enabled = false;
                    throw (ex);
                }
            }
            finally
            {
                this.inProgress = false;
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Dispose Method
        /// </summary>
        public void Dispose()
        {
            if (this.timer != null)
            {
                this.timer.Enabled = false;
                Stop();
                this.timer.Dispose();
                this.timer = null;
            }
        }
        #endregion


    }
}
