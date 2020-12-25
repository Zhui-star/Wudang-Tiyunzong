using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace HTLibrary.Application
{
    public class StatusManager : Monosingleton<StatusManager>
    {
        public long timeSpane;

        long lastGetStatusTime;

        public int maxStatus;

        public int perGameStatus;

        [SerializeField]
        private int currentStatus;

        public event Action<int,int> AddStatutsEvent;
        public event Action<long> TimeUpdateEvent;
        public event Action TipsEvent;
        private void Awake()
        {
            LoadCurrentStatus();
            LoadLastLeaveTime();
        }

        private void Start()
        {
            long NowTime = TimeKeeper.GetTimeStamp();
            int getStatus = ComputerTimeTransformStatus(NowTime - lastGetStatusTime);
            AddStatus(getStatus);

            InvokeRepeating("CheckStatus", 0, 1.0f);
        }

        /// <summary>
        /// 加载最后一次领取体力时间
        /// </summary>
        void LoadLastLeaveTime()
        {
            lastGetStatusTime = (long)PlayerPrefs.GetFloat(Consts.LastLeaveTime, TimeKeeper.GetTimeStamp());
        }

        /// <summary>
        /// 得到剩余领取体力时间
        /// </summary>
        /// <returns></returns>
        long GetRemainTime()
        {
            long remainTime = timeSpane - TimeKeeper.TimeStampElasped(lastGetStatusTime, TimeKeeper.GetTimeStamp());
            return remainTime;
        }

        /// <summary>
        /// 保存当前体力
        /// </summary>
        void SaveCurrentStatus()
        {
            PlayerPrefs.SetInt(Consts.CurrentStatus, currentStatus);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 加载当前体力
        /// </summary>

        void LoadCurrentStatus()
        {
            int currenStatus = PlayerPrefs.GetInt(Consts.CurrentStatus, maxStatus);
            AddStatus(currenStatus);
        }

        /// <summary>
        /// 添加体力
        /// </summary>
        /// <param name="status"></param>

        public void AddStatus(int status)
        {
            if (status > 0)
            {
                currentStatus = currentStatus + status > maxStatus ? maxStatus : currentStatus + status;

               
            }
            else
            {
                currentStatus = currentStatus + status < 0 ? 0 : currentStatus + status;
            }

            AddStatutsEvent?.Invoke(currentStatus,maxStatus);
        }

        /// <summary>
        /// 检查体力添加 每1秒检测一次
        /// </summary>

        void CheckStatus()
        {
            if (currentStatus >= maxStatus) return;

            if (GetRemainTime() <= 0)
            {
                AddStatus(1);

                lastGetStatusTime = TimeKeeper.GetTimeStamp();
                PlayerPrefs.SetFloat(Consts.LastLeaveTime, lastGetStatusTime);
                PlayerPrefs.Save();
            }

            TimeUpdateEvent?.Invoke(GetRemainTime());
        }

        /// <summary>
        /// 时间与体力的转化比例
        /// </summary>
        /// <param name="timeSpane"></param>
        /// <returns></returns>
        int ComputerTimeTransformStatus(long timeSpane)
        {
            return Int32.Parse((timeSpane % this.timeSpane).ToString());
        }

        /// <summary>
        /// 剩余体力能否玩游戏
        /// </summary>
        /// <returns></returns>
        public bool CanPlayGame()
        {
            bool success = currentStatus >= perGameStatus;
            if(!success)
            {
                TipsEvent?.Invoke();
            }
            else
            {
                AddStatus(-1);
            }

            return success;
        }

        /// <summary>
        /// 保存剩余时间
        /// </summary>
        void SaveLastGetStatusTime()
        {
            lastGetStatusTime = TimeKeeper.GetTimeStamp();
            PlayerPrefs.SetFloat(Consts.LastLeaveTime, lastGetStatusTime);
            PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentStatus();
            SaveLastGetStatusTime();
        }

    }

}
