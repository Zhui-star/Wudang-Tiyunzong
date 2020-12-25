using Dreamteck.Splines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using HTLibrary.Application;
namespace Dreamteck.Forever
{
    public delegate void LevelEnterHandler(ForeverLevel level, int index);

    [AddComponentMenu("Dreamteck/Forever/Level Generator")]
    public class LevelGenerator : MonoBehaviour
    {
        public enum Type { Infinite, Finite }
        private enum ExtrusionState { Idle, Prepare, Extrude, Post }
        public enum LevelIteration { Ordered, OrderedClamp, OrderedLoop, Random, None }

        [SerializeField] private int generateSegmentsAhead = 5;
        [SerializeField] private int activateSegmentsAhead = 1;
        [SerializeField] private int maxSegments = 10;
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool multithreaded = true;
        [SerializeField] private Type type = Type.Infinite;
        [SerializeField] private int finiteSegmentsCount = 10;
        [SerializeField] private bool finiteLoop = false;
        [SerializeField] private ForeverLevel[] _levelCollection = new ForeverLevel[0];
        [SerializeField] private LevelIteration levelIteration = LevelIteration.Ordered;
        [SerializeField] private ForeverRandomizer _levelRandomizer = null;
        [SerializeField] private int startLevel = 0;


        /// <summary>
        /// Deprecated in 1.10. Use <see cref="levelCollection"/>
        /// </summary>
        public Level[] levels = new Level[0];
        private static SplineSample sampleAlloc = new SplineSample();
        private Queue<LGAction> _generationActons = new Queue<LGAction>();

        private Thread _extrudeThread;
        private object _locker = new object();
        private LevelSegment _extrudeSegment = null;
        private volatile ExtrusionState _extrusionState = ExtrusionState.Idle;
        private bool _isBusy = false;
        private AsyncJobSystem _asyncJobSystem;

        public ForeverLevel currentLevel
        {
            get { return _levelCollection[_levelIndex]; }
        }

        public int currentLevelIndex
        {
            get { return _levelIndex; }
        }
        public ForeverLevel[] levelCollection
        {
            get { return _levelCollection; }
        }

        public bool isBusy
        {
            get { return _isBusy; }
        }

        private int _levelIndex = 0;

        private bool isLoadingLevel = false;
        private int segmentIndex = 0;

        private List<LevelSegment> _segments = new List<LevelSegment>();
        public List<LevelSegment> segments
        {
            get
            {
                return _segments;
            }
        }

        public static LevelGenerator instance;

        public delegate void EmptyHandler();

        public static event LevelEnterHandler onLevelEntered;
        public static event LevelEnterHandler onLevelLoaded;
        public static event LevelEnterHandler onWillLoadLevel;
        public static event EmptyHandler onReady;
        public static event EmptyHandler onLevelsDepleted;

        public bool debugMode { get; private set; }
        private GameObject[] debugModeSegments = new GameObject[0];

        private bool _ready = false;
        public bool ready { get { return _ready; } }

        private ForeverLevel _enteredLevel = null;

        int _enteredSegmentIndex = -1;
        public int enteredSegmentIndex
        {
            get { return _enteredSegmentIndex; }
        }

        public delegate int LevelChangeHandler(int currentLevel, int levelCount);
        public LevelChangeHandler levelChangeHandler;

        private float _generationProgress = 0f;
        public float generationProgress
        {
            get { return _generationProgress; }
        }

        [SerializeField]
        [HideInInspector]
        private LevelPathGenerator sharedPathGenerator;
        private LevelPathGenerator overridePathGenerator;
        private LevelPathGenerator pathGeneratorInstance;
        public LevelPathGenerator pathGenerator
        {
            get
            {
                if (Application.isPlaying && usePathGeneratorInstance) return pathGeneratorInstance;
                return sharedPathGenerator;
            }
            set
            {
                if (value == sharedPathGenerator || (usePathGeneratorInstance && value == pathGeneratorInstance)) return;
                if (Application.isPlaying && !usePathGeneratorInstance && sharedPathGenerator != null) value.Continue(sharedPathGenerator);
                
                if (Application.isPlaying && usePathGeneratorInstance)
                {
                    if (pathGeneratorInstance != null) Destroy(pathGeneratorInstance);
                    pathGeneratorInstance = Instantiate(value);
                    pathGeneratorInstance.Continue(sharedPathGenerator);
                }
                sharedPathGenerator = value;
            }
        }
        private LevelPathGenerator currentPathGenerator
        {
            get
            {
                if (overridePathGenerator != null)
                {
                    return overridePathGenerator;
                }
                else
                {
                    return pathGenerator;
                }
            }
        }

        [HideInInspector]
        public bool usePathGeneratorInstance = false;

        void Awake()
        {
            instance = this;
            _asyncJobSystem = GetComponent<AsyncJobSystem>();
            if(_asyncJobSystem == null)
            {
                _asyncJobSystem = gameObject.AddComponent<AsyncJobSystem>();
            }
            LevelSegment.onSegmentEntered += OnSegmentEntered;
            if (buildOnAwake)
            {
                StartGeneration();
            }
        }

        private void OnDestroy()
        {
            LevelSegment.onSegmentEntered -= OnSegmentEntered;
            if (_extrudeThread != null)
            {
                _extrudeThread.Abort();
                _extrudeThread = null;
                instance = null;
            }
            for (int i = 0; i < _levelCollection.Length; i++)
            {
                if (_levelCollection[i].isReady)
                {
                    _levelCollection[i].UnloadImmediate();
                }
            }
            onLevelEntered = null;
            onWillLoadLevel = null;
            onLevelLoaded = null;
        }

        IEnumerator StartRoutine()
        {
            _ready = false;
            _generationProgress = 0f;
            while (isLoadingLevel && !debugMode) yield return null;
            int count = 0;
            if (type == Type.Finite) count = finiteSegmentsCount;
            else count = 1 + generateSegmentsAhead;
            StartCoroutine(ProgressRoutine(count));
            for (int i = 0; i < count; i++)
            {
                EnqueueAction(CreateNextSegment);
            }
            while (segments.Count < count)
            {
                yield return null;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                while (segments[i].type == LevelSegment.Type.Extruded && !segments[i].extruded)
                {
                    yield return null;
                }
                if (i < activateSegmentsAhead)
                {
                    segments[i].Activate();
                }
            }
            while (!segments[0].isReady)
            {
                yield return null;
            }

            if (type == Type.Finite && finiteLoop)
            {
                _segments[_segments.Count - 1].SetLoop(_segments[0]);
            }

            _ready = true;
            if (onReady != null) {
                onReady();
            }
            _segments[0].Enter();
        }

        IEnumerator ProgressRoutine(int targetCount)
        {
            while(!_ready)
            {
                _generationProgress = 0f;
                for (int i = 0; i < segments.Count; i++)
                {
                    if (segments[i].type == LevelSegment.Type.Custom || segments[i].extruded) _generationProgress++;
                }
                _generationProgress /= targetCount;
                yield return null;
            }
            _generationProgress = 1f;
        }

        private IEnumerator ExtrudeRoutine()
        {
            if (multithreaded)
            {
                if (_extrudeThread == null || !_extrudeThread.IsAlive)
                {
                    if (_extrudeThread != null)
                    {
                        Debug.Log("Thread restarted");
                    }

                    _extrudeThread = new Thread(ExtrudeThread);
                    _extrudeThread.Start();
                }
            }

            if(_extrusionState == ExtrusionState.Prepare)
            {
                yield return _extrudeSegment.OnBeforeExtrude();
                _extrusionState++;
                if (!multithreaded)
                {
                    _extrudeSegment.Extrude();
                    _extrusionState = ExtrusionState.Post;
                }
                else
                {
                    _extrudeThread.Interrupt();
                }
            }

            while(_extrusionState == ExtrusionState.Extrude)
            {
                yield return null;
            }

            if (_extrusionState == ExtrusionState.Post)
            {
                yield return _extrudeSegment.OnPostExtrude();
                _extrusionState = ExtrusionState.Idle;
            }
        }

        public void LoadLevel(int index, bool forceHighPriority)
        {
            _levelIndex = index;
            if (onWillLoadLevel != null)
            {
                onWillLoadLevel(currentLevel, _levelIndex);
            }
            currentLevel.onSequenceEntered += OnSequenceEntered;
            if (currentLevel.remoteSequence)
            {
                StartCoroutine(LoadRemoteLevel(currentLevel, forceHighPriority ? UnityEngine.ThreadPriority.High : currentLevel.loadingPriority));
            }
            else
            {
                currentLevel.Initialize();
                if (currentLevel.pathGenerator != null)
                {
                    OverridePathGenerator(currentLevel.pathGenerator);
                }
            }
        }

        public void UnloadLevel(ForeverLevel lvl, bool forceHighPriority)
        {
            if (lvl.remoteSequence && lvl.isReady)
            {
                StartCoroutine(UnloadRemoteLevel(lvl, forceHighPriority ? UnityEngine.ThreadPriority.High : lvl.loadingPriority));
            }
        }

        public void AddLevel(ForeverLevel lvl)
        {
            ArrayUtility.Add(ref _levelCollection, lvl);
        }

        public void RemoveLevel(int index)
        {
            ArrayUtility.RemoveAt(ref _levelCollection, index);
        }

        IEnumerator LoadRemoteLevel(ForeverLevel lvl, UnityEngine.ThreadPriority priority = UnityEngine.ThreadPriority.Normal)
        {
            if (debugMode) yield break;
            while (isLoadingLevel) yield return null;
            isLoadingLevel = true;
            yield return StartCoroutine(lvl.Load());
            if (!lvl.isReady) Debug.LogError("Failed loading remote level " + lvl.name);
            else if (onLevelLoaded != null) 
            {
                int index = 0;
                for (int i = 0; i < _levelCollection.Length; i++)
                {
                    if(_levelCollection[i] == lvl)
                    {
                        index = i;
                        break;
                    }
                }
                if(onLevelLoaded != null)
                {
                    onLevelLoaded.SafeInvoke(lvl, index);
                }
            }
            lvl.Initialize();
            if(lvl.pathGenerator != null)
            {
                OverridePathGenerator(lvl.pathGenerator);
            }
            isLoadingLevel = false;
        }

        private void OverridePathGenerator(LevelPathGenerator overrideGen)
        {
            LevelPathGenerator lastGenerator = currentPathGenerator;
            if (usePathGeneratorInstance)
            {
                if(overridePathGenerator != null)
                {
                    Destroy(overridePathGenerator);
                }
                overridePathGenerator = Instantiate(overrideGen);
            }
            else
            {
                overridePathGenerator = overrideGen;
            }
            overridePathGenerator.Continue(lastGenerator);
        }

        private IEnumerator UnloadRemoteLevel(ForeverLevel lvl, UnityEngine.ThreadPriority priority = UnityEngine.ThreadPriority.Normal)
        {
            if (debugMode)
            {
                yield break;
            }

            while (isLoadingLevel)
            {
                yield return null;
            }

            isLoadingLevel = true;
            
            yield return null; //Make sure the unloading starts on the next frame to give time for resources to get freed up
            yield return StartCoroutine(lvl.Unload());
            yield return Resources.UnloadUnusedAssets();

            System.GC.Collect();

            isLoadingLevel = false;
        }

        public void Clear()
        {
            StartCoroutine(ClearRoutine());
        }

        IEnumerator ClearRoutine()
        {
            _ready = false;
            if (usePathGeneratorInstance && overridePathGenerator != null)
            {
                Destroy(overridePathGenerator);
            }
            overridePathGenerator = null;
            while (isLoadingLevel)
            {
                yield return null;
            }
            StopExtrusion();
            for (int i = 0; i < _levelCollection.Length; i++) {
                if (_levelCollection[i].remoteSequence && _levelCollection[i].isReady)
                {
                    yield return StartCoroutine(UnloadRemoteLevel(_levelCollection[i], UnityEngine.ThreadPriority.High));
                }
            }
            for (int i = 0; i < _segments.Count; i++)
            {
                _segments[i].Destroy();
            }
            ResourceManagement.UnloadResources();
            _segments.Clear();
            _generationActons.Clear();
            _isBusy = false;
            _enteredLevel = null;
            _enteredSegmentIndex = -1;
        }

        public void Restart()
        {
            StartCoroutine(RestartRoutine());
        }

        IEnumerator RestartRoutine()
        {
            if (!_ready && _enteredLevel == null)
            {
                StartGeneration();
                yield break;
            }
            yield return StartCoroutine(ClearRoutine());
            StartGeneration();
        }

        public void StartGeneration()
        {
            if(_levelRandomizer != null)
            {
                _levelRandomizer.Initialize();
            }
            
            StopAllCoroutines();
            if (usePathGeneratorInstance)
            {
                if (overridePathGenerator != null)
                {
                    Destroy(overridePathGenerator);
                }
                if (pathGeneratorInstance != null)
                {
                    Destroy(pathGeneratorInstance);
                }
                pathGeneratorInstance = Instantiate(sharedPathGenerator);
            }
            overridePathGenerator = null;
            if (currentPathGenerator == null)
            {
                Debug.LogError("Level Generator " + name + " does not have a Path Generator assigned");
                return;
            }
            
            _enteredLevel = null;
            _enteredSegmentIndex = -1;
            segmentIndex = 0;
            if (startLevel >= _levelCollection.Length) startLevel = _levelCollection.Length - 1;
            switch (levelIteration)
            {
                case LevelIteration.Ordered: levelChangeHandler = IncrementClamp; break;
                case LevelIteration.OrderedClamp: levelChangeHandler = IncrementClamp; break;
                case LevelIteration.OrderedLoop: levelChangeHandler = IncrementRepeat; break;
                case LevelIteration.Random: levelChangeHandler = RandomLevel; break;
            }
            _levelIndex = startLevel;
            while (!_levelCollection[_levelIndex].enabled)
            {
                _levelIndex++;
                if (_levelIndex >= _levelCollection.Length) break;
            }
            LoadLevel(_levelIndex, true);
            currentPathGenerator.Initialize(this);
            PlayerPrefs.Save();
            StartCoroutine(StartRoutine());


        }

        public LevelSegment GetSegmentAtPercent(double percent)
        {
            int pathIndex;
            GlobalToLocalPercent(percent, out pathIndex);
            if (_segments.Count == 0) return null;
            return _segments[pathIndex];
        }

        public LevelSegment FindSegmentForPoint(Vector3 point)
        {
            Project(point, sampleAlloc);
            return GetSegmentAtPercent(sampleAlloc.percent);
        }

        public void SetDebugMode(bool enabled, GameObject[] debugSegments = null)
        {
            debugMode = enabled;
            if (!debugMode)
            {
                debugModeSegments = null;
            }
            else
            {
                debugModeSegments = debugSegments;
            }
        }

        public void Project(Vector3 point, SplineSample result, bool bypassCache = false)
        {
            if (_segments.Count == 0) return;
            int closestPath = 0;
            float closestDist = Mathf.Infinity;
            for (int i = 0; i < _segments.Count; i++)
            {
                if (!_segments[i].extruded && _segments[i].type == LevelSegment.Type.Extruded) continue;
                _segments[i].Project(point, sampleAlloc, 0.0, 1.0, bypassCache ? SplinePath.EvaluateMode.Accurate : SplinePath.EvaluateMode.Cached);
                float dist = (sampleAlloc.position - point).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPath = i;
                    result.CopyFrom(sampleAlloc);
                }
            }
            result.percent = LocalToGlobalPercent(result.percent, closestPath);
        }

        public float CalculateLength(double from = 0.0, double to = 1.0, double resolution = 1.0)
        {
            if (_segments.Count == 0) return 0f;
            if (to < from)
            {
                double temp = from;
                from = to;
                to = temp;
            }
            int fromSegmentIndex = 0, toSegmentIndex = 0;
            double fromSegmentPercent = 0.0, toSegmentPercent = 0.0;
            fromSegmentPercent = GlobalToLocalPercent(from, out fromSegmentIndex);
            toSegmentPercent = GlobalToLocalPercent(to, out toSegmentIndex);
            float length = 0f;
            for (int i = fromSegmentIndex; i <= toSegmentIndex; i++)
            {
                if (i == fromSegmentIndex) length += segments[i].CalculateLength(fromSegmentPercent, 1.0);
                else if (i == toSegmentIndex) length += segments[i].CalculateLength(toSegmentPercent, 1.0);
                else length += segments[i].CalculateLength();
            }
            return length;
        }

        public double Travel(double start, float distance, Spline.Direction direction)
        {
            if (_segments.Count == 0) return 0.0;
            if (direction == Spline.Direction.Forward && start >= 1.0) return 1.0;
            else if (direction == Spline.Direction.Backward && start <= 0.0) return 0.0;
            if (distance == 0f) return DMath.Clamp01(start);
            float moved = 0f;
            Vector3 lastPosition = EvaluatePosition(start);
            double lastPercent = start;
            int iterations = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                iterations += segments[i].path.spline.iterations;
            }
            int step = iterations - 1;
            int nextSampleIndex = direction == Spline.Direction.Forward ? DMath.CeilInt(start * step) : DMath.FloorInt(start * step);
            float lastDistance = 0f;
            Vector3 pos = Vector3.zero;
            double percent = start;

            while (true)
            {
                percent = (double)nextSampleIndex / step;
                pos = EvaluatePosition(percent);
                lastDistance = Vector3.Distance(pos, lastPosition);
                lastPosition = pos;
                moved += lastDistance;
                if (moved >= distance) break;
                lastPercent = percent;
                if (direction == Spline.Direction.Forward)
                {
                    if (nextSampleIndex == step) break;
                    nextSampleIndex++;
                }
                else
                {
                    if (nextSampleIndex == 0) break;
                    nextSampleIndex--;
                }
            }
            return DMath.Lerp(lastPercent, percent, 1f - (moved - distance) / lastDistance);
        }

        public void Evaluate(double percent, SplineSample result)
        {
            if (_segments.Count == 0) return;
            int pathIndex;
            double localPercent = GlobalToLocalPercent(percent, out pathIndex);
            _segments[pathIndex].Evaluate(localPercent, result);
            result.percent = percent;
        }

        public Vector3 EvaluatePosition(double percent)
        {
            if (_segments.Count == 0) return Vector3.zero;
            int pathIndex;
            double localPercent = GlobalToLocalPercent(percent, out pathIndex);
            return _segments[pathIndex].EvaluatePosition(localPercent);
        }

        public double GlobalToLocalPercent(double percent, out int segmentIndex)
        {
            double segmentValue = percent * _segments.Count;
            segmentIndex = Mathf.Clamp(DMath.FloorInt(segmentValue), 0, _segments.Count - 1);
            if (_segments.Count == 0)
            {
                return 0.0;
            }
            return DMath.InverseLerp(segmentIndex, segmentIndex + 1, segmentValue);
        }

        public double LocalToGlobalPercent(double localPercent, int segmentIndex)
        {
            if (_segments.Count == 0) return 0.0;
            double percentPerPath = 1.0 / _segments.Count;
            return DMath.Clamp01(segmentIndex * percentPerPath + localPercent * percentPerPath);
        }

        public void NextLevel(bool forceHighPriority = false)
        {
            currentLevel.onSequenceEntered -= OnSequenceEntered;
            _levelIndex = GetNextLevelIndex();
            LoadLevel(_levelIndex, forceHighPriority);
        }

        public AsyncJobSystem.AsyncJobOperation ScheduleAsyncJob<T>(AsyncJobSystem.JobData<T> data)
        {
            return _asyncJobSystem.ScheduleJob(data);
        }

        private void OnSequenceEntered(SegmentSequence sequence)
        {
            if(sequence.customPathGenerator != null)
            {
                LevelPathGenerator lastGenerator = currentPathGenerator;
                OverridePathGenerator(sequence.customPathGenerator);
            } else if(overridePathGenerator != null)
            {
                if (currentLevel.pathGenerator != null)
                {
                    OverridePathGenerator(currentLevel.pathGenerator);
                }
                else
                {
                    pathGenerator.Continue(overridePathGenerator);
                    if (usePathGeneratorInstance)
                    {
                        Destroy(overridePathGenerator);
                    }
                    overridePathGenerator = null;
                }
           }
        }

        public void SetLevel(int index, bool forceHighPriority = false)
        {
            if (index < 0 || index >= _levelCollection.Length) return;
            if (currentLevel)
            {
                currentLevel.onSequenceEntered -= OnSequenceEntered;
            }
            LoadLevel(index, forceHighPriority);
        }

        int GetNextLevelIndex()
        {
            int nextLevel = levelChangeHandler(_levelIndex, _levelCollection.Length - 1);
            while (!_levelCollection[nextLevel].enabled)
            {
                nextLevel++;
                if (nextLevel >= _levelCollection.Length)
                {
                    nextLevel = _levelIndex;
                    break;
                }
            }
            return nextLevel;
        }

        int IncrementClamp(int current, int max)
        {
            return Mathf.Clamp(current + 1, 0, max);
        }

        int IncrementRepeat(int current, int max)
        {
            current++;
            if (current > max) current = 0;
            return current;
        }

        int RandomLevel(int current, int max)
        {
            int index = _levelRandomizer.NextInt(0, _levelCollection.Length);
            if (index == current) index++;
            if (index >= max) index = 0;
            return index;
        }

        public void CreateNextSegment(System.Action completeHandler)
        {
            StartCoroutine(CreateSegment(completeHandler));
        }

        public void DestroySegment(int index)
        {
            _segments[index].Destroy();
            _segments.RemoveAt(index);
            segmentIndex--;
            if (index >= _segments.Count)
            {
                currentPathGenerator.Continue(_segments[_segments.Count - 1]);
            }
        }

        IEnumerator CreateSegment(System.Action completeHandler)
        {
            while (!currentLevel.isReady && !debugMode)
            {
                yield return null;
            }
            HandleLevelChange();
            if (currentLevel.IsDone() && !debugMode)
            {
                if(completeHandler != null)
                {
                    completeHandler();
                }
                yield break;
            }
            LevelSegment segment = null;
            
            if (debugMode)
            {
                GameObject go = Instantiate(debugModeSegments[Random.Range(0, debugModeSegments.Length)]);
                segment = go.GetComponent<LevelSegment>();
            }
            else
            {
                segment = currentLevel.InstantiateSegment();
            }
            segment.gameObject.SetActive(false);
            Transform segmentTrs = segment.transform;
            Vector3 spawnPos = segmentTrs.position;
            Quaternion spawnRot = segmentTrs.rotation;
            if (segments.Count > 0)
            {
                SplineSample lastSegmentEndResult = new SplineSample();
                _segments[_segments.Count - 1].Evaluate(1.0, lastSegmentEndResult);
                spawnPos = lastSegmentEndResult.position;
                spawnRot = lastSegmentEndResult.rotation;
                switch (segment.axis)
                {
                    case LevelSegment.Axis.X: spawnRot = Quaternion.AngleAxis(90f, Vector3.up) * spawnRot; break;
                    case LevelSegment.Axis.Y: spawnRot = Quaternion.AngleAxis(90f, Vector3.right) * spawnRot; break;
                }
            }

            if (segment.type == LevelSegment.Type.Custom)
            {
                Quaternion entranceRotationDelta = segment.customEntrance.rotation * Quaternion.Inverse(spawnRot);
                segmentTrs.rotation = segmentTrs.rotation * Quaternion.Inverse(entranceRotationDelta);
                if(segment.customKeepUpright) segmentTrs.rotation = Quaternion.FromToRotation(segment.customEntrance.up, Vector3.up) * segmentTrs.rotation;
                Vector3 entranceOffset = segmentTrs.position - segment.customEntrance.position;
                segmentTrs.position = spawnPos + entranceOffset;
                segment.gameObject.SetActive(true);
            } else
            {
                if (segment.objectProperties[0].extrusionSettings.applyRotation)
                {
                    segmentTrs.rotation = spawnRot;
                }
            }

            if (segmentIndex == int.MaxValue)
            {
                segmentIndex = 2;
            }
            segment.Setup(this, segmentIndex++);
            currentPathGenerator.GeneratePath(segment);
            _segments.Add(segment);

            if(segment.type == LevelSegment.Type.Extruded)
            {
                Extrude(segment);
                while (_extrusionState != ExtrusionState.Idle)
                {
                    yield return null;
                }
            }

            //Remove old segments
            if (type == Type.Infinite && _segments.Count > maxSegments)
            {
                StartCoroutine(CleanupRoutine());
            }
            
            if (currentLevel.IsDone() && !debugMode)
            {
                if (levelIteration == LevelIteration.Ordered && _levelIndex >= _levelCollection.Length - 1)
                {
                    if(onLevelsDepleted != null)
                    {
                        onLevelsDepleted.SafeInvoke();
                    }
                    yield break;
                }
                if (levelIteration == LevelIteration.None && levelChangeHandler == null)
                {
                    if (onLevelsDepleted != null)
                    {
                        onLevelsDepleted.SafeInvoke();
                    }
                    yield break;
                }
                NextLevel();
            }
            yield return null;
            if (completeHandler != null)
            {
                completeHandler();
            }
        }

        private void HandleLevelChange()
        {
            if (!currentLevel.IsDone() || debugMode)
            {
                return;
            }
            
            if ((levelIteration == LevelIteration.Ordered && _levelIndex >= _levelCollection.Length - 1) || levelIteration == LevelIteration.None)
            {
                if(onLevelsDepleted != null)
                {
                    onLevelsDepleted();
                }
                return;
            }

            NextLevel();
        }

        private IEnumerator CleanupRoutine()
        {
            yield return StartCoroutine(DestroySegmentRoutine(0));
            if (_segments.Count > maxSegments) StartCoroutine(CleanupRoutine());
        }

        //First wait for the SegmentBuilder to start building and only after that queue the destruction. Building should come before destruction
        private IEnumerator DestroySegmentRoutine(int index)
        {
            ForeverLevel segmentLevel = _segments[index].level;
            _segments[index].Destroy();
            if (segmentLevel.remoteSequence)
            {
                bool levelFound = false;
                for (int i = 0; i < _segments.Count; i++)
                {
                    if (i != index && _segments[i].level == segmentLevel)
                    {
                        levelFound = true;
                        break;
                    }
                }
                yield return null;
                if (!levelFound)
                {
                    UnloadLevel(segmentLevel, false);
                }
            }
            _segments.RemoveAt(index);
        }

        public void EnterLevel(int index)
        {
            if(_levelCollection[index].isReady)
            {
                if(onLevelEntered != null)
                {
                    onLevelEntered.SafeInvoke(_levelCollection[index], index);
                }
            }
        }

        public void EnqueueAction(LGAction.LGHandler action)
        {
            _generationActons.Enqueue(new LGAction(action, OnActionComplete));
            _isBusy = true;
            if(_generationActons.Count == 1)
            {
                _generationActons.Peek().Start();
            }
        }

        private void OnActionComplete()
        {
            LGAction action = _generationActons.Dequeue();
            if (_generationActons.Count > 0)
            {
                _generationActons.Peek().Start();
            } else
            {
                _isBusy = false;
            }
        }

        private void OnSegmentEntered(LevelSegment entered)
        {
            if (!_ready)
            {
                return;
            }
            if (entered.index <= _enteredSegmentIndex)
            {
                return;
            }

            _enteredSegmentIndex = entered.index;
            if (_enteredLevel != entered.level)
            {
                _enteredLevel = entered.level;
                int enteredIndex = 0;
                for (int i = 0; i < _levelCollection.Length; i++)
                {
                    if (_enteredLevel == _levelCollection[i])
                    {
                        enteredIndex = i;
                        break;
                    }
                }
                if(onLevelEntered != null)
                {
                    onLevelEntered.SafeInvoke(_enteredLevel, enteredIndex);
                }
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].index == _enteredSegmentIndex)
                {
                    if (type == Type.Infinite)
                    {
                        int segmentsAhead = _segments.Count - (i + 1);
                        if (segmentsAhead < generateSegmentsAhead)
                        {
                            for (int j = segmentsAhead; j < generateSegmentsAhead; j++)
                            {
                                EnqueueAction(CreateNextSegment);
                            }
                        }
                        //Segment activation
                        for (int j = i; j <= i + activateSegmentsAhead && j < _segments.Count; j++)
                        {
                            if (!segments[j].activated)
                            {
                                StartCoroutine(ActivateSegmentRoutine(_segments[j]));
                            }
                        }
                    }
                    break;
                }
            }
        }

        private IEnumerator ActivateSegmentRoutine(LevelSegment segment)
        {
            while(segment.type == LevelSegment.Type.Extruded && !segment.extruded)
            {
                yield return null;
            }
            segment.Activate();
        }

        void OnDisable()
        {
            if (_extrudeThread != null)
            {
                _extrudeThread.Abort();
                _extrudeThread = null;
            }
        }

        private void OnApplicationQuit()
        {
            if (_extrudeThread != null)
            {
                _extrudeThread.Abort();
                _extrudeThread = null;
            }
        }

        public void StopExtrusion()
        {
            if (_extrudeThread != null && _extrudeThread.IsAlive)
            {
                _extrudeThread.Abort();
            }
            _extrusionState = ExtrusionState.Idle;
            StopCoroutine(ExtrudeRoutine());
        }

        private void Extrude(LevelSegment segment)
        {
            if (_extrusionState != ExtrusionState.Idle)
            {
                Debug.LogError("Cannot extrude segment " + segment.name + " because another segment is currently being computed");
                return;
            }
            if(segment != null)
            {
                _extrudeSegment = segment;
                _extrusionState = ExtrusionState.Prepare;
                StartCoroutine(ExtrudeRoutine());
            } else
            {
                Debug.LogError("Extrusion error - level segment is NULL");
            }

        }

        private void ExtrudeThread()
        {
            while (true)
            {
                try
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {
                    lock (_locker)
                    {
                        if (_extrusionState == ExtrusionState.Extrude)
                        {
                            try
                            {
                                _extrudeSegment.Extrude();
                                _extrusionState = ExtrusionState.Post;
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 设置开始关卡
        /// </summary>
        /// <param name="index"></param>
        public void SetStartLevel(int index)
        {
            startLevel = index;
        }

    }
}
