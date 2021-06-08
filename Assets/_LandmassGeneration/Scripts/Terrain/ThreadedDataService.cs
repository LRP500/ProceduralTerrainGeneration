using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProceduralTerrain
{
    public class ThreadedDataService : MonoBehaviour
    {
        #region Nested Types

        private readonly struct ThreadInfo
        {
            public readonly Action<object> callback;
            public readonly object parameter;

            public ThreadInfo(Action<object> callback, object parameter)
            {
                this.callback = callback;
                this.parameter = parameter;
            }
        }

        #endregion Nested Types

        #region Private Fields

        private readonly Queue<ThreadInfo> _dataQueue = new Queue<ThreadInfo>();

        #endregion Private Fields

        #region Properties

        private static ThreadedDataService ServiceInstance { get; set; }

        #endregion Properties

        #region MonoBehaviour

        private void Awake()
        {
            ServiceInstance = this;
        }

        private void Update()
        {
            ProcessHeightMapInfoQueue();
        }

        #endregion MonoBehaviour

        #region Public Methods

        public static void RequestData(Func<object> generateDataCallback, Action<object> completedCallback)
        {
            void ThreadStart() => ServiceInstance.DataThread(generateDataCallback, completedCallback);
            new Thread(ThreadStart).Start();
        }
        
        #endregion Public Methods

        #region Private Methods

        private void DataThread(Func<object> generateDataCallback, Action<object> completedCallback)
        {
            object data = generateDataCallback();

            lock (_dataQueue)
            {
                _dataQueue.Enqueue(new ThreadInfo(completedCallback, data));
            }
        }

        private void ProcessHeightMapInfoQueue()
        {
            lock (_dataQueue)
            {
                if (_dataQueue.Count > 0)
                {
                    for (int i = 0, length = _dataQueue.Count; i < length; ++i)
                    {
                        ThreadInfo info = _dataQueue.Dequeue();
                        info.callback?.Invoke(info.parameter);
                    }
                }
            }
        }

        #endregion Private Methods
    }
}
