using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HTLibrary.Application
{
    /// <summary>
    /// 状态管理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HTStateMachine<T> where T:struct,IComparable,IConvertible
    {
        public T CurrentState { get; protected set; }

        public T PreviousState { get; protected set; }

        public HTStateMachine()
        {

        }

        /// <summary>
        /// 改变状态
        /// </summary>
        /// <param name="newState"></param>

        public virtual void ChangeState(T newState)
        {
           if(newState.Equals(CurrentState))
            {
                return;
            }

            PreviousState = CurrentState;

            CurrentState = newState;

           
        }


    }

}
