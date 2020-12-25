using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Together;
namespace HTLibrary.Application
{
    /// <summary>
    /// 游戏结束面板
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        public GameObject reviveBtn;

        public float delayOpenTime = 1.0f;
        StatusManager statusManager;
        [HideInInspector]
        public GameManager gameManager;

        private void Awake()
        {
            statusManager = StatusManager.Instance;
            gameManager = GameManager.Instance;
            GameManager.Instance.GameOverEvent += OpenPanel;

        }


        private void OnEnable()
        {
            CheckState();
        }

        public void CheckState()
        {
           if(gameManager.gameMode.ReviveCount>0)
            {
                reviveBtn.SetActive(true);
            }
            else
            {
                reviveBtn.SetActive(false);
            }
        }

        public void RestartClick()
        {
            if (!statusManager.CanPlayGame()) return;
            GameManager.Instance.ResumeGame();
            RestartGameEvent.TriggerEvent();
        }

        public void ReviveClik()
        {
            TGSDK.AdCloseCallback = ADSCallBack;
            // UnityAdsManager.Instance.ShowRewardedVideo(AdsType.ReduceSpwanTimeAds);

            if (TGSDK.CouldShowAd("haQK88jEtIzuT88SmE3"))
            {
                TGSDK.ShowAd("haQK88jEtIzuT88SmE3");
            }
        }

        public void OpenPanel()
        {
            Invoke("PauseGame", delayOpenTime);
        }

        void PauseGame()
        {
            this.gameObject.SetActive(true);
            GameManager.Instance.PauseGame();
        }

        void ADSCallBack(string id, string msg, bool award)
        {
            if (id == "haQK88jEtIzuT88SmE3" && award)
            {
                GameManager.Instance.ResumeGame();
                
                ReviveGameEvent.TriggerEvent();
            }
        }
    }

}
