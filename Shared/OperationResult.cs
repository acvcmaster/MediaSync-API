using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MediaSync.Shared
{
    /// <summary>
    /// Represents a generic timed operation that returns a result.
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    public class AsyncTimedOperation<T>
    {
        public T Result { get; set; }
        public TimeSpan Elapsed { get; set; }
        public bool Completed
        {
            get { return Elapsed != null; }
        }
        public void SetResult(T result) => Result = result;
        public static async Task<AsyncTimedOperation<T>> Start(Func<T> operation)
        {
            AsyncTimedOperation<T> asyncTimedOperation = new AsyncTimedOperation<T>();
            Stopwatch timer = new Stopwatch();
            timer.Start();
            T operationResult = default;
            await Task.Run(() => {
                operationResult = operation();
            });
            timer.Stop();
            asyncTimedOperation.Result = operationResult;
            asyncTimedOperation.Elapsed = timer.Elapsed;
            return asyncTimedOperation;
        }
    }
}