using System.Collections.Generic;

namespace SpaceMaintenance.Core
{
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }

    public class StateMachine
    {
        private IState _currentState;

        public void ChangeState(IState newState)
        {
            if (_currentState != null)
            {
                _currentState.Exit();
            }

            _currentState = newState;

            if (_currentState != null)
            {
                _currentState.Enter();
            }
        }

        public void Update()
        {
            if (_currentState != null)
            {
                _currentState.Execute();
            }
        }
    }
}
