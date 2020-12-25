using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HTLibrary.Application
{
    /// <summary>
    /// 游戏获胜面板
    /// </summary>
    public class GameWinPanel : MonoBehaviour
    {
        GameManager gameManager;
        StatusManager statusManager;
        private void Awake()
        {
            gameManager = GameManager.Instance;
            statusManager = StatusManager.Instance;
            gameManager.GameWinEvent += OpenPanel;
        }

        /// <summary>
        /// 打开面板
        /// </summary>
        public void OpenPanel()
        {
            this.gameObject.SetActive(true);
            gameManager.PauseGame();
        }

        /// <summary>
        /// 加载下一关卡
        /// </summary>
        public void LoadNextLevelClick()
        {
            statusManager.AddStatus(1);
            gameManager.LoadNextLevel();
            gameManager.ResumeGame();
        }
    }

}
