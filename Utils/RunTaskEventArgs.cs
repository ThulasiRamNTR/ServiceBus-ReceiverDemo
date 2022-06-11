///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// @Author: Thulasi
//
// Purpose: This should later be the IMessage interface for doing all the tasks
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace QueueReceiver
{
    public class RunTaskEventArgs : EventArgs
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availableRoom"></param>
        public RunTaskEventArgs(Int32 availableRoom)
        {
            this.AvailableRoom = availableRoom;
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Available room
        /// </summary>
        public Int32 AvailableRoom { get; set; }
        #endregion
    }
}