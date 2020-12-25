using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dreamteck.Forever;
using Together;
namespace HTLibrary.Application
{
    public class GameMenuePanel : MonoBehaviour
    {
        public Text levelInfoTxt;
        public Text statusTxt;
        public Text remainTimeTxt;
        public GameObject tipsPanel;
        private LevelGenerator levelGenrator;
        private StatusManager statusManager;

        private void Awake()
        {
            levelGenrator = LevelGenerator.instance;
            statusManager = StatusManager.Instance;
        }

        private void OnEnable()
        {
            LevelGenerator.onLevelEntered += UpdateUI;
            statusManager.AddStatutsEvent += UpdateStatus;
            statusManager.TimeUpdateEvent += UpdateRemainTime;
            statusManager.TipsEvent += OpenTipsPanel;
        }

        private void OnDisable()
        {
            LevelGenerator.onLevelEntered -= UpdateUI;
            statusManager.AddStatutsEvent -= UpdateStatus;
            statusManager.TimeUpdateEvent -= UpdateRemainTime;
            statusManager.TipsEvent -= OpenTipsPanel;
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        public void UpdateUI(ForeverLevel level, int index)
        {
            if(levelGenrator==null)
            {
                levelGenrator = LevelGenerator.instance;
            }

            int currentLevel = levelGenrator.currentLevelIndex + 1;
            int totalLevel = levelGenrator.levelCollection.Length;

            levelInfoTxt.text = "关卡进度: " + currentLevel + " / " + totalLevel;
        }

        public void PauseGameClick()
        {
            GameManager.Instance.PauseGame();
        }

        public void ResumeClick()
        {
            GameManager.Instance.ResumeGame();
        }

        /// <summary>
        /// 添加体力按钮事件
        /// </summary>
        public void AddStatusClick()
        {
            TGSDK.AdCloseCallback = ADSCallBack;
            // UnityAdsManager.Instance.ShowRewardedVideo(AdsType.ReduceSpwanTimeAds);

            if (TGSDK.CouldShowAd("8oQBLjyd1ID0H1hEXp1"))
            {
                TGSDK.ShowAd("8oQBLjyd1ID0H1hEXp1");
            }
        }

        /// <summary>
        /// 更新体力UI
        /// </summary>
        /// <param name="CurrentStatus"></param>
        /// <param name="maxStatus"></param>
        void UpdateStatus(int CurrentStatus,int maxStatus)
        {
            statusTxt.text ="体力: "+  CurrentStatus + " / " + maxStatus;
        }

        /// <summary>
        /// 更新剩余时间UI
        /// </summary>
        /// <param name="remainTime"></param>
        void UpdateRemainTime(long remainTime)
        {
            int min = (int)remainTime / 60;
            int second =(int)remainTime - (min * 60);
            remainTimeTxt.text= string.Format("剩余 {0:D2}:{1:D2} 恢复一点体力", min, second);
        }

        /// <summary>
        /// 打开Tips面板
        /// </summary>
        void OpenTipsPanel()
        {
            GameManager.Instance.PauseGame();
            tipsPanel.SetActive(true);
        }

        void ADSCallBack(string id, string msg, bool award)
        {
            if (id == "8oQBLjyd1ID0H1hEXp1" && award)
            {
                statusManager.AddStatus(5);
                GameManager.Instance.ResumeGame();
                RestartGameEvent.TriggerEvent();
            }
        }


    }

}
