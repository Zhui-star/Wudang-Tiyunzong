using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HTLibrary.Application
{
    /// <summary>
    /// 处理游戏界面UI
    /// </summary>
    public class MenuePanel : MonoBehaviour
    {
        StatusManager statusManager;
        private void Start()
        {
            statusManager = StatusManager.Instance;
            GameManager.Instance._GameState = GameState.Menue;
        }
        public void StartGameClick()
        {
            if (!statusManager.CanPlayGame()) return;

            GameManager.Instance.StartGame();
            GameManager.Instance._GameState = GameState.Game;
        }
    }

}
