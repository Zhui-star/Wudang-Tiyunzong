using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Dreamteck.Forever;
using Together;
namespace HTLibrary.Application
{
    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        Game,
        Menue,  
        GameOver,
        GameWin
    }

    [Serializable]
    public struct GameMode
    {
        public bool IsRevive { get; set; }
        public float ReviveCount;
    }

    public struct RestartGameEvent
    {
        public delegate void Delegate();
        static public event Delegate OnEvent;

       static public void Register(Delegate _onEvent)
        {
            OnEvent += _onEvent;
        }

        static public void UnRegister(Delegate _onEvent)
        {
            OnEvent -= _onEvent;
        }

        static public void TriggerEvent()
        {
            GameManager.Instance.gameMode.ReviveCount = 2;
            OnEvent?.Invoke();
        }
    }

    public struct ReviveGameEvent
    {
        public delegate void Delegate();
        static public event Delegate OnEvent;

        static public void Register(Delegate _onEvent)
        {
            OnEvent += _onEvent;
        }

        static public void UnRegister(Delegate _onEvent)
        {
            OnEvent -= _onEvent;
        }

        static public void TriggerEvent()
        {
            GameManager.Instance.gameMode.ReviveCount--;
            OnEvent?.Invoke();
        }
    }



    /// <summary>
    /// 游戏管理器
    /// </summary>
    public class GameManager :Monosingleton<GameManager>
    {
        public bool TestMode = false;

        [HideInInspector]
        public LevelGenerator levelGenerator;
     
        private GameState _gameState;
        public GameState _GameState
        {
            get
            {
                
                return _gameState;
            }
            set
            {
                switch(value)
                {
                    case GameState.Menue:
                        MenueEvent?.Invoke();
                        break;
                    case GameState.Game:
                        GameEvent?.Invoke();
                        break;
                    case GameState.GameOver:
                        GameOverEvent?.Invoke();
                        break;
                    case GameState.GameWin:
                        GameWinEvent?.Invoke();
                        break;
                }
                _gameState = value;
            }
        }

        public int TargeFrame = 60;

        public event Action MenueEvent;
        public event Action GameEvent;
        public event Action GameOverEvent;
        public event Action GameWinEvent;
        public event Action LoadNextEvent;
        public GameMode gameMode;


        private void Awake()
        {
            TGSDKInitial();

            levelGenerator = LevelGenerator.instance;

            if(TestMode)
            {
                StartGame();
                _GameState = GameState.Game;
            }
        }

        private void Start()
        {
            UnityEngine.Application.targetFrameRate = TargeFrame;
          //  _GameState = GameState.Menue;
        }

        private void FixedUpdate()
        {
            CheckGameWin();
        }
        public void StartGame()
        {
            int currentLevel = PlayerPrefs.GetInt(Consts.CurrentLevel,0);
            levelGenerator.SetStartLevel(currentLevel);
            levelGenerator.StartGeneration();
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            Time.timeScale = 0;
        }

        /// <summary>
        /// 恢复暂停
        /// </summary>
        public void ResumeGame()
        {
            Time.timeScale = 1;
        }

        /// <summary>
        /// 检测游戏是否赢得
        /// </summary>
        public void CheckGameWin()
        {
            if (_GameState == GameState.Game&&levelGenerator.ready)
            {
                Vector3 targetPosition = levelGenerator.EvaluatePosition(0.97f);
                Vector3 transformPosition = GameObject.FindGameObjectWithTag(Tags.Player).transform.position;
                Debug.Log("Target Position:" + targetPosition);

                if (transformPosition.x >=targetPosition.x)
                {
                    _GameState= GameState.GameWin;
                }


            }
          
        }

        /// <summary>
        /// 下一关
        /// </summary>
        public void LoadNextLevel()
        {
            StartCoroutine(ILoadNextLevel());
        }

        IEnumerator ILoadNextLevel()
        {
            int nextIndex = levelGenerator.currentLevelIndex + 1 >= levelGenerator.levelCollection.Length ? 0 : levelGenerator.currentLevelIndex + 1;
            levelGenerator.SetStartLevel(nextIndex);

            PlayerPrefs.SetInt(Consts.CurrentLevel, nextIndex);
            PlayerPrefs.Save();        
            levelGenerator.Restart();
            yield return new WaitUntil(() => levelGenerator.ready);
            _GameState = GameState.Game;

            LoadNextEvent?.Invoke();
        }


        /// <summary>
        /// ADS Initial
        /// </summary>
        void TGSDKInitial()
        {
            TGSDK.Initialize("6A5589ur50220ky9174C");
            TGSDK.SDKInitFinishedCallback = (string msg) => {
                TGSDK.TagPayingUser(TGPayingUser.TGMediumPaymentUser, "CNY", 0, 0);
                TGSDK.SetUserGDPRConsentStatus("yes");
                TGSDK.SetIsAgeRestrictedUser("no");
                TGSDK.PreloadAd();
            };
        }

    }

}
