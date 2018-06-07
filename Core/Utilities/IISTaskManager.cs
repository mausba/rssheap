using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Core.Utilities;

namespace Core.Utilities
{
    /// <summary>
    /// Static class for running background tasks in IIS.
    /// Any spawned threads are registered properly with IIS
    /// to ensure they finish execution.
    /// </summary>
    public static class IISTaskManager
    {
        /// <summary>
        /// Runs a background task that is registered with the hosting environment
        /// so it is guaranteed to finish executing.
        /// </summary>
        /// <param name="action">The lambda expression to invoke.</param>
        public static void Run(Action action)
        {
            new IISBackgroundTask().DoWork(action);
        }

        /// <summary>
        /// Generic object for completing tasks in a background thread
        /// when the request doesn't need to wait for the results 
        /// in the response.
        /// </summary>
        class IISBackgroundTask : IRegisteredObject
        {
            /// <summary>
            /// Constructs the object and registers itself with the hosting environment.
            /// </summary>
            public IISBackgroundTask()
            {
                HostingEnvironment.RegisterObject(this);
            }

            /// <summary>
            /// Called by IIS, once with <paramref name="immediate"/> set to false
            /// and then again with <paramref name="immediate"/> set to true.
            /// </summary>
            void IRegisteredObject.Stop(bool immediate)
            {
                if (_task.IsCompleted || _task.IsCanceled || _task.IsFaulted || immediate)
                {
                    // Task has completed or was asked to stop immediately, 
                    // so tell the hosting environment that all work is done.
                    HostingEnvironment.UnregisterObject(this);
                }
            }

            /// <summary>
            /// Invokes the <paramref name="action"/> as a Task.
            /// Any exceptions are logged
            /// </summary>
            /// <param name="action">The lambda expression to invoke.</param>
            public void DoWork(Action action)
            {
                try
                {
                    _task = Task.Run(action);
                }
                catch (AggregateException ex)
                {
                    // Log exceptions
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        Mail.SendMeAnEmail("Error in IISTaskManager", innerEx.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Mail.SendMeAnEmail("Error in IISTaskManager", ex.ToString());
                }
            }

            private Task _task;
        }
    }
}