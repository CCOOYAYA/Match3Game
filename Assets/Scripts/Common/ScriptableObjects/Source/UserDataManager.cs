using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Random = UnityEngine.Random;
using UnityEngine;
using Newtonsoft.Json.Bson;
using System.Linq;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "UserDataManager", menuName = "SO/UserDataManager")]
public class UserDataManager : ScriptableObject
{
    [SerializeField] private TextAsset levelConfig;
    [SerializeField] private int maxLife = 5;
    [SerializeField] private int maxLifeAd = 5;
    [SerializeField] private int maxSceneID = 4;
    [SerializeField] private int maxLevel = 3;
    public static int MaxLevel => Instance.maxLevel;
    [SerializeField] private int lifeRegenInterval = 60;
    [SerializeField] private int lifeAdCooldownInterval = 60;
    [SerializeField] private int lifeAdCountRegenInterval = 21600;

    [NonSerialized] private bool initialized;
    [NonSerialized] private UserData userData;
    [NonSerialized] private GameData gameData;
    //[NonSerialized] public static bool ShortScreen = true;
    private static List<int> boostPowerups = new();
    private static int retryAdPowerup = 0;

    public static bool TopMargin { get; set; } = false;
    public static bool BottomMargin { get; set; } = false;
    public static bool PurchaseComplete { get; set; } = false;
    public static bool IsRetryMode { get; set; } = false;
    public static bool BuildSceneLoadComplete { get; set; } = false;
    public static bool HomeSceneSwitchFlag { get; set; } = true;

    private static int totBuildStage;

    // RecentlyWeight
    private static int _recentlyWeight;
    private readonly static int normalFailAttempt = 1;
    private readonly static int hardFailAttempt = 2;
    private readonly static int superHardFailAttempt = 3;

    [Serializable]
    private struct TimePair
    {
        public long startTime;
        [NonSerialized] public float updateTime;

        public void ResetTime(float updateInterval)
        {
            startTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            updateTime = Time.unscaledTime + updateInterval;
        }

        public bool UpdateCheck => updateTime < Time.unscaledTime;
    }

    [Serializable]
    public struct ResourceWithTimestamp
    {
        public int value;
        public long startTime;
        public long endTime;
        [NonSerialized] public float expireTime;
        [NonSerialized] public bool isInRange;

        public void TimeInit()
        {
            long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            isInRange = (startTime < currentTime) && (currentTime < endTime);
            if (isInRange)
                expireTime = Time.unscaledTime + (endTime - currentTime);
            else
            {
                expireTime = 0f;
                startTime = 0;
                endTime = 0;
            }
        }

        public void AddTime(int minute)
        {
            if (expireTime < Time.unscaledTime)
                isInRange = false;
            startTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (isInRange)
            {
                endTime += minute * 60;
                expireTime += minute * 60f;
            }
            else
            {
                endTime = startTime + minute * 60;
                expireTime = Time.unscaledTime + minute * 60f;
                isInRange = true;
            }
        }

        public void TimeCheck(ref bool needSave)
        {
            if (!isInRange)
                return;
            if (expireTime < Time.unscaledTime)
            {
                isInRange = false;
                startTime = 0;
                endTime = 0;
                needSave = true;
            }
        }
    }

    private struct UserData
    {
        /// <summary>
        /// UserInfo&Setting
        /// </summary>
        public string userName;
        public bool defaultName;
        public long dataCreateTime;
        public int avatarID;
        public int frameID;
        public int themeID;
        public bool musicOn;
        public bool soundOn;
        public bool vibrationOn;
        public bool hintOn;
        /// <summary>
        /// Resources
        /// </summary>
        public int coins;
        public ResourceWithTimestamp life;
        public TimePair lifeRegenTime;
        public int lifeAdCount;
        public TimePair lifeAdCooldownTime;
        public TimePair lifeAdCountRegenTime;
        public ResourceWithTimestamp[] startItems;
        public int[] gameItems;
        public int stars;
        /// <summary>
        /// Level Progress
        /// </summary>
        public int nextLevel;
        public int levelEvent;
        public int currentScene;
        public int[] buildStage;
        public int winStreak;
        public int loseStreak;

        public bool safeFinishFlag;
        public bool winFlag;
        public uint newCoin;
        public SerializedDictionary<int, int> clearedPiecesDict;

        public long initTime;
        public long lastInitTime;

        public SerializedDictionary<int, string> eventDataDict;
    }

    [Serializable]
    public class GameData : IDisposable
    {
        public uint newCoins;
        public Dictionary<int, int> clearedPiecesDic = new();

        public void Dispose()
        {
            newCoins = 0;
            clearedPiecesDic.Clear();
        }
    }

    [NonSerialized] private GameLevel nextLevel = null;
    public static UserDataManager Instance;

    public static AsyncOperation LoadOP { get; set; } = new();

    public static string UserName => Instance.userData.userName;
    public static bool DefaultUserName => Instance.userData.defaultName;
    public static DateTimeOffset FirstPlayTime => DateTimeOffset.FromUnixTimeSeconds(Instance.userData.dataCreateTime);
    public static int UserAvatarID => Instance.userData.avatarID;
    public static int UserAvatarFrameID => Instance.userData.frameID;
    public static int UserThemeID => Instance.userData.themeID;
    public static bool MusicOn => Instance.userData.musicOn;
    public static bool SoundOn => Instance.userData.soundOn;
    public static bool VibrationOn => Instance.userData.vibrationOn;
    public static bool HintOn => Instance.userData.hintOn;
    public static int CurrentSceneID => Instance.userData.currentScene;
    public static int SceneCount => Instance.maxSceneID;
    public static int[] BuildStage => Instance.userData.buildStage;
    public static int TotalBuildStage => totBuildStage;
    public static int LevelID => Math.Abs(Instance.nextLevel.level);
    public static int NextLevelID => Instance.userData.nextLevel;
    public static GameLevel GameLevel
    {
        get
        {
            if (Instance.nextLevel == null)
                LoadNextLevel();
            return Instance.nextLevel;
        }
    }
    public static int Coin => Instance.userData.coins;
    public static int NewCoin => (int)Instance.gameData.newCoins;
    public static int Stars => Instance.userData.stars;
    public static int Life => Instance.userData.life.value;
    public static bool HaveLife => InfiniteLife || (0 < Life);
    public static bool InfiniteLife => Instance.userData.life.isInRange;
    public static float InfiniteLifeExpireTime => Instance.userData.life.expireTime;
    public static bool FullLife => Instance.userData.life.value == Instance.maxLife;
    public static float LifeRegenTime => Instance.userData.lifeRegenTime.updateTime;
    public static int LifeAdCount => Instance.userData.lifeAdCount;
    public static float LifeAdCoolDownTime => Instance.userData.lifeAdCooldownTime.updateTime;
    public static float LifeAdCountRegenTime => Instance.userData.lifeAdCountRegenTime.updateTime;
    public static int StartItemCount(int i) => Instance.userData.startItems[i].value;
    public static bool InfiniteStartItem(int i) => Instance.userData.startItems[i].isInRange;
    public static float InfiniteStartItemTime(int i) => Instance.userData.startItems[i].expireTime;
    public static int PropCount(int i) => Instance.userData.gameItems[i];
    public static List<int> BoostPowerups => boostPowerups;
    public static int WinStreak => Math.Min(Instance.userData.winStreak, 3);
    public static int LoseStreak => Instance.userData.loseStreak;
    public static double DeltaTimeInHour => (DateTimeOffset.Now - DateTimeOffset.FromUnixTimeSeconds(Instance.userData.lastInitTime)).TotalHours;
    public static int RecentlyWeight 
    {
        get         => _recentlyWeight;
        private set => _recentlyWeight = Math.Max(value, 0); 
    }
    public static bool WinFlag => Instance.userData.winFlag;
    public static uint NewCoinToAdd => Instance.userData.newCoin;
    public static SerializedDictionary<int, int> ClearedPieceDict => Instance.userData.clearedPiecesDict;
    public static GameData GameSceneGameData => Instance.gameData;
    public static int RetryPowerup
    {
        get
        {
            return retryAdPowerup;
        }

        set
        {
            retryAdPowerup = GetConvertedPieceId(value);
        }
    }

    public void InitCheck(bool force = false)
    {
        if (initialized && !force)
            return;
        Instance = this;
        LoadMe();
        initialized = true;
    }

    public void OnEnterGameSceneInitializeGameData()
    {
        gameData ??= new GameData();
        gameData?.Dispose();
    }

    public void OnReleasePieceRecordData(int pieceId)
    {
        if (gameData == null)
            throw new NullReferenceException();

        if (!gameData.clearedPiecesDic.TryAdd(pieceId, 1))
            gameData.clearedPiecesDic[pieceId]++;
    }

    public void EarnCoin(uint newCoin)
    {
        if (gameData == null)
            throw new NullReferenceException();

        gameData.newCoins += newCoin;
    }


    private void NewUserData()
    {
        userData = new UserData();
        userData.userName = "Player";
        userData.defaultName = true;
        userData.dataCreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        userData.avatarID = 0;
        userData.frameID = 0;
        userData.themeID = 0;
        userData.musicOn = true;
        userData.soundOn = true;
        userData.vibrationOn = true;
        userData.hintOn = true;

        userData.coins = 1000;
        userData.life.value = 5;
        userData.lifeAdCount = 5;
        userData.startItems = new ResourceWithTimestamp[3];
        userData.startItems[0].value = 3;
        userData.startItems[1].value = 3;
        userData.startItems[2].value = 3;
        userData.gameItems = new int[4];
        userData.stars = 0;

        userData.nextLevel = 1;
        userData.levelEvent = 1;
        userData.currentScene = 1;
        userData.buildStage = new int[5];
        for (int i = 0; i < 5; i++)
            userData.buildStage[i] = 0;
        userData.winStreak = 0;
        userData.loseStreak = 0;

        userData.safeFinishFlag = true;
        userData.winFlag = false;
        userData.newCoin = 0;
        userData.clearedPiecesDict = new();

        userData.initTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        userData.lastInitTime = userData.initTime;

        userData.eventDataDict = new();

        SaveMe();
    }

    private void SaveMe()
    {
        string path = Path.Combine(Application.persistentDataPath, "UserDataTest");
        StreamWriter writer = new StreamWriter(path, false);
        var setting = new JsonSerializerSettings();
        setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        writer.Write(JsonConvert.SerializeObject(userData, setting));
        writer.Close();
    }

    public static void SaveToFile()
    {
        Instance.SaveMe();
    }

    private void TestResourceCount()
    {
        userData.coins = 2000;
        userData.stars = 50; //15
        for (int i = 0; i < 3; i++)
            userData.startItems[i].value = i;
        for (int i = 0; i < 4; i++)
            userData.gameItems[i] = i;
    }

    private void LoadMe()
    {
        string path = Path.Combine(Application.persistentDataPath, "UserDataTest");
        if (!File.Exists(path))
        {
            NewUserData();
            return;
        }
        StreamReader reader = new StreamReader(path);
        string content = reader.ReadToEnd();
        reader.Close();
        userData = JsonConvert.DeserializeObject<UserData>(content);
        userData.life.TimeInit();
        long lifeDeltaTime = DateTimeOffset.Now.ToUnixTimeSeconds() - userData.lifeRegenTime.startTime;
        while ((userData.life.value < 5) && (lifeRegenInterval < lifeDeltaTime))
        {
            userData.life.value++;
            lifeDeltaTime -= lifeRegenInterval;
        }
        if (userData.life.value < 5)
            userData.lifeRegenTime.updateTime = Time.unscaledTime + lifeDeltaTime;
        userData.lifeAdCooldownTime.updateTime = Time.unscaledTime + lifeAdCooldownInterval - (DateTimeOffset.Now.ToUnixTimeSeconds() - userData.lifeAdCooldownTime.startTime);
        userData.lifeAdCountRegenTime.updateTime = Time.unscaledTime + lifeAdCountRegenInterval - (DateTimeOffset.Now.ToUnixTimeSeconds() - userData.lifeAdCountRegenTime.startTime);
        for (int i = 0; i < 3; i++)
            userData.startItems[i].TimeInit();
        TestResourceCount();
        totBuildStage = 0;
        for (int i = 0; i < 5; i++)
            totBuildStage += userData.buildStage[i];
        userData.lastInitTime = userData.initTime;
        userData.initTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        RecentlyWeight = DeltaTimeInHour switch
        {
            var x when x >= 72 && x <= 168 => 100,
            var x when x > 168 => 200,
            _ => 0
        };
        SaveMe();
    }

    public static void LoadLevel(int level)
    {
        string path = $"levels/u_level{level}";
        Instance.levelConfig = Resources.Load<TextAsset>(path);
        if (Instance.levelConfig == null)
            return;
        Instance.nextLevel = JsonConvert.DeserializeObject<GameLevel>(Instance.levelConfig.text);
    }

    public static void LoadNextLevel()
    {
        LoadLevel(NextLevelID);
    }

    public static bool LevelEventCheck()
    {
        if (Instance.userData.levelEvent == NextLevelID)
        {
            Instance.userData.levelEvent++;
            if (Instance.maxLevel < Instance.userData.levelEvent)
                Instance.userData.levelEvent = 1;
            return true;
        }
        return false;
    }

    public static void CostStar(int areaID, int cost, bool instantSave = true)
    {
        Instance.userData.buildStage[areaID]++;
        Instance.userData.stars -= cost;
        if (instantSave)
        {
            Instance.SaveMe();
            totBuildStage = 0;
            for (int i = 0; i < 5; i++)
                totBuildStage += Instance.userData.buildStage[i];
        }
    }

    public static void UpdateBuildStage(int build, int stage, bool instantSave = true)
    {
        Instance.userData.buildStage[build] = stage;
        if (instantSave)
        {
            Instance.SaveMe();
            totBuildStage = 0;
            for (int i = 0; i < 5; i++)
                totBuildStage += Instance.userData.buildStage[i];
        }   
    }

    public static void SetUserName(string newname)
    {
        Instance.userData.userName = newname;
        Instance.userData.defaultName = false;
        Instance.SaveMe();
    }

    public static void SaveProfile(int avartar, int frame, int theme)
    {
        Instance.userData.avatarID = avartar;
        Instance.userData.frameID = frame;
        Instance.userData.themeID = theme;
        Instance.SaveMe();
    }

    public static void SetMusic(bool value)
    {
        Instance.userData.musicOn = value;
        Instance.SaveMe();
    }

    public static void SetSound(bool value)
    {
        Instance.userData.soundOn = value;
        Instance.SaveMe();
    }

    public static void SetVibration(bool value)
    {
        Instance.userData.vibrationOn = value;
        Instance.SaveMe();
    }

    public static void SetHint(bool value)
    {
        Instance.userData.hintOn = value;
        Instance.SaveMe();
    }

    public static void AddLife(int value,bool resetRegen = false, bool instantSave = true)
    {
        if (Instance.userData.life.value == Instance.maxLife)
            return;
        Instance.userData.life.value += value;
        if (Instance.maxLife <= Instance.userData.life.value)
            Instance.userData.life.value = Instance.maxLife;
        else if (resetRegen)
            Instance.userData.lifeRegenTime.ResetTime(Instance.lifeRegenInterval);
        if (instantSave)
            Instance.SaveMe();
    }

    public static void AddLifeByAd()
    {
        if (Instance.userData.life.value == Instance.maxLife)
            return;
        Instance.userData.lifeAdCooldownTime.ResetTime(Instance.lifeAdCooldownInterval);
        if (Instance.userData.lifeAdCount == Instance.maxLifeAd)
            Instance.userData.lifeAdCountRegenTime.ResetTime(Instance.lifeAdCountRegenInterval);
        Instance.userData.lifeAdCount--;
        AddLife(1);
    }

    public static void BuyLife(int price)
    {
        Instance.userData.coins -= price;
        AddLife(5);
    }

    public static void AddLifeTime(int minute, bool instantSave = true)
    {
        Instance.userData.life.AddTime(minute);
        if (instantSave)
            Instance.SaveMe();
    }

    private void UseLife()
    {
        if (0 == userData.life.value)
            return;
        if (userData.life.isInRange)
            return;
        if (userData.life.value == Instance.maxLife)
            userData.lifeRegenTime.ResetTime(lifeRegenInterval);
        Instance.userData.life.value--;
    }

    public static void AddStartItemTime(int itemID, int minute, bool instantSave = true)
    {
        Instance.userData.startItems[itemID].AddTime(minute);
        if (instantSave)
            Instance.SaveMe();
    }

    private void UseStartItem(int itemID)
    {
        if (0 == userData.startItems[itemID].value)
            return;
        if (userData.startItems[itemID].isInRange)
            return;
        userData.startItems[itemID].value--;
    }

    public static void UseStartItems(bool rocket, bool bomb, bool rainbow)
    {
        if (Instance.userData.startItems[0].isInRange || rocket)
        {
            var random = Random.Range(0, 1f);
            boostPowerups.Add(random <= 0.5f ? Constants.PieceHRocketId : Constants.PieceVRocketId);
            if (rocket)
                Instance.UseStartItem(0);
        }
        
        if (Instance.userData.startItems[1].isInRange || bomb)
        {
            boostPowerups.Add(Constants.PieceBombId);
            if (bomb)
                Instance.UseStartItem(1);
        }
            
        if (Instance.userData.startItems[2].isInRange || rainbow)
        {
            boostPowerups.Add(Constants.PieceRainbowId);
            if (rainbow)
                Instance.UseStartItem(2);
        }
        Instance.userData.safeFinishFlag = false;
        Instance.userData.winFlag = false;
        Instance.SaveMe();
    }

    public static void ClearBoostPowerups()
    {
        boostPowerups.Clear();
        retryAdPowerup = 0;
    }

    public static void ConsumeProp()
    {
        Instance.userData.gameItems[(int)GameManager.CurrentProp - 1]--;
        Instance.SaveMe();
    }

    public static void BuyItemByCoin(int itemID,int count,int price)
    {
        Instance.userData.coins -= price;
        if (itemID < 3)
            Instance.userData.startItems[itemID].value += count;
        else
            Instance.userData.gameItems[itemID - 3] += count;
        Instance.SaveMe();
    }

    public static void ReviveByCoin(int price)
    {
        Instance.userData.coins -= price;
        Instance.SaveMe();
    }

    public static void ClearLifeTime_Test()
    {
        Instance.userData.life.expireTime = 0f;
    }

    public static void UpdateTick()
    {
        float time = Time.unscaledTime;
        bool needSave = false;
        Instance.userData.life.TimeCheck(ref needSave);
        if (Instance.userData.lifeRegenTime.UpdateCheck)
        {
            AddLife(1, true, false);
            needSave = true;
        }
        if (Instance.userData.lifeAdCountRegenTime.UpdateCheck)
        {
            Instance.userData.lifeAdCount = Instance.maxLifeAd;
            needSave = true;
        }
        for (int i = 0; i < 3; i++)
            Instance.userData.startItems[i].TimeCheck(ref needSave);
        if (needSave)
            Instance.SaveMe();
    }

    public static void LevelSuccess()
    {
        if (Instance.userData.safeFinishFlag)
            return;
        Instance.userData.nextLevel++;
        if (Instance.maxLevel < Instance.userData.nextLevel)
            Instance.userData.nextLevel = 1;
        Instance.userData.winStreak++;
        Instance.userData.loseStreak = 0;
        Instance.userData.safeFinishFlag = true;
        RecentlyWeight -= 20;
        Instance.SaveMe();
    }

    public static void SaveRewards()
    {
        Instance.userData.winFlag = true;
        Instance.userData.newCoin = Instance.gameData.newCoins;
        Instance.userData.clearedPiecesDict.Clear();
        foreach (var pair in Instance.gameData.clearedPiecesDic)
            Instance.userData.clearedPiecesDict.Add(pair.Key, pair.Value);
        Instance.SaveMe();
    }

    public static void AddNewCoins()
    {
        Instance.userData.coins += (int)Instance.userData.newCoin;
        Instance.userData.newCoin = 0;
        Instance.userData.winFlag = false;
        Instance.SaveMe();
    }

    public static void AddStar()
    {
        Instance.userData.stars++;
        Instance.SaveMe();
    }

    public static void LevelFailed()
    {
        if (Instance.userData.safeFinishFlag)
            return;
        Instance.UseLife();
        Instance.userData.winStreak = 0;
        Instance.userData.loseStreak++;
        Instance.userData.safeFinishFlag = true;
        RecentlyWeight += GameLevel.levelType switch
        {
            0 => (LoseStreak - normalFailAttempt) * 20,
            1 => (LoseStreak - hardFailAttempt) * 20,
            2 => (LoseStreak - superHardFailAttempt) * 20,
            _ => 20
        };
        Instance.SaveMe();
    }

    public static void BuyBundle(Bundle bundle)
    {
        Instance.userData.coins += bundle.Coins;
        if (!bundle.CoinOnly)
        {
            for (int i = 0; i < 3; i++)
            {
                Instance.userData.startItems[i].value += bundle.ItemCount[i];
                Instance.userData.startItems[i].AddTime(bundle.StartItemTime[i]);
            }
            for (int i = 0; i < 4; i++)
                Instance.userData.gameItems[i] += bundle.ItemCount[i + 3];
        }
        PurchaseComplete = true;
        Instance.SaveMe();
    }

    public static void PrevScene_Test()
    {
        totBuildStage = 0;
        if (1 < Instance.userData.currentScene)
        {
            Instance.userData.currentScene--;
            Instance.SaveMe();
        }
    }

    public static void NextScene()
    {
        for (int i = 0; i < 5; i++)
            Instance.userData.buildStage[i] = 0;
        totBuildStage = 0;
        if (Instance.userData.currentScene < Instance.maxSceneID)
        {
            Instance.userData.currentScene++;
            Instance.SaveMe();
        }
    }

    public static void UseLife_Test()
    {
        Instance.UseLife();
        Instance.SaveMe();
    }

    public static int GetConvertedPieceId(int originId) => originId switch
    {
        0 => Random.value < 0.5f ? Constants.PieceHRocketId : Constants.PieceVRocketId,
        1 => Constants.PieceBombId,
        2 => Constants.PieceRainbowId,
        _ => throw new InvalidCastException()
    };

    public static string LoadEventData(int eventID)
    {
        if (Instance.userData.eventDataDict.ContainsKey(eventID))
            return Instance.userData.eventDataDict[eventID];
        else
            return "";
    }

    public static void SaveEventData(int eventID,string eventData)
    {
        if (Instance.userData.eventDataDict.ContainsKey(eventID))
            Instance.userData.eventDataDict[eventID] = eventData;
        else
            Instance.userData.eventDataDict.Add(eventID, eventData);
        Instance.SaveMe();
    }
}
