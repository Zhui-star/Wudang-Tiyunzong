using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTLibrary.Application
{
    public class InputManager : Monosingleton<InputManager>
    {
        public bool IsMobile { get; protected set; }
        private List<HTInputButton> buttonList = new List<HTInputButton>();

        public HTInputButton jumpButton { get; private set; }

        public void ControlsModelDetection()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            IsMobile = true;
#endif
#if UNITY_EDITOR
            IsMobile = false;
#endif
        }

        public virtual void InitialButtons()
        {
            if(IsMobile)
            {
                buttonList.Add(jumpButton = new HTInputButton("MobileJump", JumpButtonDown, JumpButtonPressed, JumpButtonUp));
            }
            else
            {
                buttonList.Add(jumpButton = new HTInputButton("Jump", JumpButtonDown, JumpButtonPressed, JumpButtonUp));
            }

        }

        void GetButtonInput()
        {
            foreach(var temp in buttonList)
            {
                if(Input.GetButton(temp.ButtoID))
                {
                    temp.TriggerButtonPressed();
                }
                if(Input.GetButtonDown(temp.ButtoID))
                {
                    temp.TriggerButtonDown();
                }
                if(Input.GetButtonUp(temp.ButtoID))
                {
                    temp.TriggerButtonUp();
                }
            }
        }

        public virtual void ProcessButtonState()
        {
            foreach(var temp in buttonList)
            {
                if(temp.State.CurrentState==ButtonStates.Down)
                {
                    temp.State.ChangeState(ButtonStates.Pressed);
                }
                if(temp.State.CurrentState==ButtonStates.Up)
                {
                    temp.State.ChangeState(ButtonStates.Off);
                }
            }
        }

        private void Awake()
        {
            ControlsModelDetection();
            InitialButtons();
        }

        private void Update()
        {
            GetButtonInput();
          //  ProcessButtonState();

        }

        public virtual void JumpButtonDown() { jumpButton.State.ChangeState(ButtonStates.Down); }

        public virtual void JumpButtonUp() { jumpButton.State.ChangeState(ButtonStates.Up); }

        public virtual void JumpButtonPressed() { jumpButton.State.ChangeState(ButtonStates.Pressed); }
    }

}
