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
        public bool Completed { get; set; } = false;
        public bool Failed
        {
            get { return Error != null; }
        }
        public string Error { get; set; }
        public void SetResult(T result) => Result = result;
        public static async Task<AsyncTimedOperation<T>> Start(Func<T> operation)
        {
            AsyncTimedOperation<T> asyncTimedOperation = new AsyncTimedOperation<T>();
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