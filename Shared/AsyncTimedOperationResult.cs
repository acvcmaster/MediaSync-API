using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MediaSync.Shared
{
    /// <summary>
    /// Represents a the result of a generic timed operation.
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    public class AsyncTimedOperationResult<T>
    {
        /// <summary>
        /// The result of the asynchronous operation.
        /// </summary>
        /// <value></value>
        public T Result { get; set; }
        /// <summary>
        /// Total elapsed time.
        /// </summary>
        /// <value></value>
        public TimeSpan Elapsed { get; private set; }
        public bool Completed { get; private set; } = false;
        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public bool Failed { get => Error != null; }
        /// <summary>
        /// The error message, if an error occurred.
        /// </summary>
        /// <value></value>
        public string Error { get; private set; }
        /// <summary>
        /// Executes an operation as an async task and returns it's result, alongside with exception and elapsed time information.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>AsyncTimedOperationResult instance representing the result of the operation.</returns>
        public static async Task<AsyncTimedOperationResult<T>> GetResult(Func<T> operation)
        {
            AsyncTimedOperationResult<T> asyncTimedOperation = new AsyncTimedOperationResult<T>();
            Stopwatch timer = new Stopwatch();
            timer.Start();
            T operationResult = default;
            await Task.Run(() =>
            {
                try { operationResult = operation(); }
                catch (Exception error) { asyncTimedOperation.Error = error.Message; }
                finally
                {
                    timer.Stop();
                    asyncTimedOperation.Result = operationResult;
                    asyncTimedOperation.Elapsed = timer.Elapsed;
                    if (!asyncTimedOperation.Failed)
                        asyncTimedOperation.Completed = true;
                }
            });
            return asyncTimedOperation;
        }
    }
}