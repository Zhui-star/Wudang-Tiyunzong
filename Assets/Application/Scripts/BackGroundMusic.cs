using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTLibrary.Application
{
    /// <summary>
    /// BGM播放器
    /// </summary>
    public class BackGroundMusic : MonoBehaviour
    {
        private GameManager gameManager;
        public AudioSource audioSource;
        public AudioClip[] bgms;
        private void Awake()
        {
            gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            gameManager.MenueEvent += PlayMenueBGM;
            gameManager.GameEvent += PlayGameBGM;
            gameManager.GameOverEvent += PlayGameOverBGM;
        }

        private void OnDisable()
        {
            gameManager.MenueEvent -= PlayMenueBGM;
            gameManager.GameEvent -= PlayGameBGM;
            gameManager.GameOverEvent -= PlayGameOverBGM;
        }


        void PlayMenueBGM()
        {
            if (bgms[0] == null) return;
            audioSource.clip = bgms[0];
            SoundManager.Instance.PlayBackgroundMusic(audioSource, true);
        }

        void PlayGameBGM()
        {
            if (bgms[1] == null) return;
            audioSource.clip = bgms[1];
            SoundManager.Instance.PlayBackgroundMusic(audioSource, true);
        }

        void PlayGameOverBGM()
        {
            if (bgms[2] == null) return;
            audioSource.clip = bgms[2];
            SoundManager.Instance.PlayBackgroundMusic(audioSource, true);
        }



    }

}
