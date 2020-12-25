using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTLibrary.Application
{
    public enum ButtonStates
    {
        Off,
        Down,
        Pressed,
        Up
    }

    /// <summary>
    /// Button 对象
    /// </summary>
    public class HTInputButton
    {
        public HTStateMachine<ButtonStates> State { get; protected set; }

        public string ButtoID;

        public delegate void ButtonDownMethodDelegate();

        public delegate void ButtonPressedMethodDelegate();

        public delegate void ButtonUpMethodDelegate();

        public ButtonDownMethodDelegate ButtonDownMethod;
        public ButtonPressedMethodDelegate ButtonPressedMethod;
        public ButtonUpMethodDelegate ButtonUpMethod;

        /// <summary>
        /// 初始化注册Button
        /// </summary>
        /// <param name="buttonID"></param>
        /// <param name="btnDown"></param>
        /// <param name="btnPressed"></param>
        /// <param name="btnUp"></param>
        public HTInputButton(string buttonID, ButtonDownMethodDelegate btnDown=null,ButtonPressedMethodDelegate btnPressed=null
            ,ButtonUpMethodDelegate btnUp=null)
        {
            State = new HTStateMachine<ButtonStates>();
            this.ButtoID = buttonID;
            ButtonDownMethod = btnDown;
            ButtonUpMethod = btnUp;
            ButtonPressedMethod = btnPressed;
        }

        /// <summary>
        /// 触发按钮按下
        /// </summary>
        public virtual void TriggerButtonDown()
        {
            if(ButtonDownMethod!=null)
            {
                State.ChangeState(ButtonStates.Down);
            }
        }

        /// <summary>
        /// 触发按钮一直按着
        /// </summary>
        public virtual void TriggerButtonPressed()
        {
            if(ButtonPressedMethod!=null)
            {  
                State.ChangeState(ButtonStates.Pressed);
            }
        }

        /// <summary>
        /// 触发按钮抬起
        /// </summary>
        public virtual void TriggerButtonUp()
        {
            if(ButtonUpMethod!=null)
            {
                State.ChangeState(ButtonStates.Up);
            }
        }
    }

}
