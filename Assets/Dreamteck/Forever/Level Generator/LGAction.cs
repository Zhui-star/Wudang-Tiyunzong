namespace Dreamteck.Forever
{
    using System;

    public class LGAction
    {
        public delegate void LGHandler(Action completeHandler);
        private Action _completeHandler;
        private LGHandler _operation;
        private bool _isStarted = false;

        public LGAction(LGHandler operation, Action completeHandler)
        {
            _operation = operation;
            _completeHandler = completeHandler;
        }

        public void Start()
        {
            if (!_isStarted)
            {
                _operation(OnOperationComplete);
                _isStarted = true;
            }
        }

        private void OnOperationComplete()
        {
            if(_completeHandler != null)
            {
                _completeHandler();
            }
        }
    }
}
