using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    #region 单例
    public static PlayerDataManager Instance { get; private set; }
    #endregion

    #region 事件系统
    public event Action<PlayerData> OnPlayerDataChanged;

    public event Action<int, int, int, int> OnExperienceChanged;
    public event Action<int> OnPlayerLevelUp;

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnCrystalsChanged;
    public event Action<int> OnStaminaChanged;
    public event Action OnPlayerDataListChanged;
    public event Action<string> OnQuestAdded;           // 任务被激活
    public event Action<string> OnQuestProgressUpdated; // 任务进度更新
    public event Action<string> OnQuestCompleted;       // 任务完成
    
    #endregion

    #region 属性
    public PlayerData CurrentPlayerData { get; private set; }
    #endregion

    #region 常量
    private const string LAST_LOGIN_PLAYER_ID_KEY = "LastLoginPlayerID";
    private const string DEFAULT_PASSWORD = "defaultPass123";
    #endregion

    #region 私有字段
    private Dictionary<string, AccountInfo> accountDatabase = new Dictionary<string, AccountInfo>();
    private Dictionary<string, PlayerData> playerDataDatabase = new Dictionary<string, PlayerData>();
    private string accountFilePath;
    private string playerDataFilePath;
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePaths();
            LoadAllDataFromDisk();
            AutoLoginOrCreate();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 初始化与文件IO
    private void InitializePaths()
    {
        string directory = Application.persistentDataPath + "/PlayerData/";
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        accountFilePath = directory + "Accounts.json";
        playerDataFilePath = directory + "AllPlayerData.json";
        Debug.Log($"数据存储路径: {directory}");
    }

    private void LoadAllDataFromDisk()
    {
        try
        {
            if (File.Exists(accountFilePath))
            {
                string accountJson = File.ReadAllText(accountFilePath);
                var accountWrapper = JsonUtility.FromJson<SerializationWrapper<AccountInfo>>(accountJson);
                accountDatabase.Clear();
                foreach (var account in accountWrapper.Items)
                    accountDatabase[account.Username] = account;
            }

            if (File.Exists(playerDataFilePath))
            {
                string playerDataJson = File.ReadAllText(playerDataFilePath);
                var dataWrapper = JsonUtility.FromJson<SerializationWrapper<PlayerData>>(playerDataJson);
                playerDataDatabase.Clear();
                foreach (var data in dataWrapper.Items)
                    playerDataDatabase[data.PlayerID] = data;
            }
            Debug.Log("玩家数据加载完成。");
        }
        catch (Exception e)
        {
            Debug.LogError($"加载数据时出错: {e.Message}");
        }
    }

    private void SaveAllDataToDisk()
    {
        try
        {
            List<AccountInfo> accountList = new List<AccountInfo>(accountDatabase.Values);
            string accountJson = JsonUtility.ToJson(new SerializationWrapper<AccountInfo>(accountList), true);
            File.WriteAllText(accountFilePath, accountJson);

            List<PlayerData> playerDataList = new List<PlayerData>(playerDataDatabase.Values);
            string playerDataJson = JsonUtility.ToJson(new SerializationWrapper<PlayerData>(playerDataList), true);
            File.WriteAllText(playerDataFilePath, playerDataJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"保存数据时出错: {e.Message}");
        }
    }

    [Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> Items;
        public SerializationWrapper(List<T> items) => Items = items;
    }
    #endregion

    #region 账户与登录相关
    public bool TryRegister(string username, string password)
    {
        if (accountDatabase.ContainsKey(username))
        {
            Debug.LogWarning($"用户名 '{username}' 已存在。");
            return false;
        }

        string passwordHash = ComputeSHA256Hash(password);
        PlayerData newPlayerData = new PlayerData(username);
        playerDataDatabase[newPlayerData.PlayerID] = newPlayerData;

        AccountInfo newAccount = new AccountInfo(username, passwordHash, newPlayerData.PlayerID);
        accountDatabase[username] = newAccount;

        SaveAllDataToDisk();
        Debug.Log($"新账户注册成功: {username}");
        return true;
    }

    public bool TryLogin(string username, string password)
    {
        if (!accountDatabase.TryGetValue(username, out AccountInfo account))
        {
            Debug.LogWarning($"用户名 '{username}' 不存在。");
            return false;
        }

        string inputPasswordHash = ComputeSHA256Hash(password);
        if (account.PasswordHash != inputPasswordHash)
        {
            Debug.LogWarning($"用户 {username} 密码错误。");
            return false;
        }

        if (playerDataDatabase.TryGetValue(account.LinkedPlayerDataID, out PlayerData data))
        {
            CurrentPlayerData = data;
            CurrentPlayerData.LastLoginTime = DateTime.Now;
            SaveLastLogin(CurrentPlayerData.PlayerID);
            SaveAllDataToDisk();
            OnPlayerDataListChanged?.Invoke();
            Debug.Log($"用户 {username} 登录成功。");
            return true;
        }
        else
        {
            Debug.LogError($"严重错误：账户 {username} 关联的玩家数据丢失！");
            return false;
        }
    }

    public void Logout()
    {
        if (CurrentPlayerData != null)
        {
            Debug.Log($"用户 {CurrentPlayerData.PlayerName} 已登出。");
            SaveCurrentPlayerData();
            ClearLastLogin();
        }
        CurrentPlayerData = null;
        OnPlayerDataListChanged?.Invoke();
    }

    public bool CheckUsernameExists(string username) => accountDatabase.ContainsKey(username);

    public string GetCurrentUsername() => CurrentPlayerData?.PlayerName ?? "未登录";

    public bool DeleteAccount(string username, string password)
    {
        if (!accountDatabase.TryGetValue(username, out AccountInfo account))
            return false;

        string inputPasswordHash = ComputeSHA256Hash(password);
        if (account.PasswordHash != inputPasswordHash)
            return false;

        playerDataDatabase.Remove(account.LinkedPlayerDataID);
        accountDatabase.Remove(username);

        if (CurrentPlayerData?.PlayerID == account.LinkedPlayerDataID)
            CurrentPlayerData = null;

        SaveAllDataToDisk();
        Debug.Log($"账户 {username} 已被删除。");
        return true;
    }
    #endregion

    #region 数据保存
    public void SaveCurrentPlayerData()
    {
        if (CurrentPlayerData != null)
        {
            playerDataDatabase[CurrentPlayerData.PlayerID] = CurrentPlayerData;
            SaveAllDataToDisk();
        }
    }

    private string ComputeSHA256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }
    }
    #endregion

    #region 存档列表辅助方法（用于UI）
    public List<PlayerData> GetAllPlayerData() => new List<PlayerData>(playerDataDatabase.Values);

    public string GetUsernameByPlayerID(string playerID)
    {
        foreach (var account in accountDatabase.Values)
        {
            if (account.LinkedPlayerDataID == playerID)
                return account.Username;
        }
        return null;
    }

    public bool LoginWithPlayerID(string playerID)
    {
        string username = GetUsernameByPlayerID(playerID);
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError($"未找到玩家 ID {playerID} 对应的用户名");
            return false;
        }
        return TryLogin(username, DEFAULT_PASSWORD);
    }

    public bool DeletePlayerData(string playerID)
    {
        AccountInfo targetAccount = null;
        foreach (var account in accountDatabase.Values)
        {
            if (account.LinkedPlayerDataID == playerID)
            {
                targetAccount = account;
                break;
            }
        }
        if (targetAccount == null) return false;

        if (CurrentPlayerData?.PlayerID == playerID)
            Logout();

        playerDataDatabase.Remove(playerID);
        accountDatabase.Remove(targetAccount.Username);

        SaveAllDataToDisk();
        OnPlayerDataListChanged?.Invoke();
        Debug.Log($"存档 {targetAccount.Username} 已删除");
        return true;
    }

    public bool CreateNewPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("昵称不能为空");
            return false;
        }

        string baseName = playerName.Trim();
        string username = baseName;
        int suffix = 1;
        while (accountDatabase.ContainsKey(username))
        {
            username = $"{baseName}{suffix}";
            suffix++;
            if (suffix > 100)
            {
                Debug.LogError("无法生成唯一用户名，创建失败");
                return false;
            }
        }

        bool success = TryRegister(username, DEFAULT_PASSWORD);
        if (success)
        {
            TryLogin(username, DEFAULT_PASSWORD);
            OnPlayerDataListChanged?.Invoke();
        }
        return success;
    }

    /// <summary>重命名玩家（修改昵称，并自动更新用户名以保持唯一）</summary>
    public bool RenamePlayer(string playerID, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return false;
        if (!playerDataDatabase.TryGetValue(playerID, out PlayerData player))
            return false;

        // 查找关联的账户
        AccountInfo account = null;
        foreach (var acc in accountDatabase.Values)
        {
            if (acc.LinkedPlayerDataID == playerID)
            {
                account = acc;
                break;
            }
        }
        if (account == null) return false;

        string baseName = newName.Trim();
        string newUsername = baseName;
        int suffix = 1;

        // 如果新用户名与原用户名相同，直接修改昵称即可（无需改用户名）
        if (newUsername == account.Username)
        {
            player.PlayerName = newUsername;
            SaveAllDataToDisk();
            // 触发事件
            if (CurrentPlayerData?.PlayerID == playerID)
                OnPlayerDataChanged?.Invoke(CurrentPlayerData);
            OnPlayerDataListChanged?.Invoke();
            return true;
        }

        // 检查新用户名是否已存在（且不是自己）
        while (accountDatabase.ContainsKey(newUsername))
        {
            newUsername = $"{baseName}{suffix}";
            suffix++;
            if (suffix > 100)
            {
                Debug.LogError("无法生成唯一用户名，重命名失败");
                return false;
            }
        }

        // 从字典中移除旧键
        accountDatabase.Remove(account.Username);
        // 更新用户名
        account.Username = newUsername;
        player.PlayerName = newUsername; // 昵称设为新用户名
        // 添加新键
        accountDatabase[newUsername] = account;

        SaveAllDataToDisk();

        // 如果当前登录玩家就是被改名的玩家，触发数据变化事件
        if (CurrentPlayerData?.PlayerID == playerID)
            OnPlayerDataChanged?.Invoke(CurrentPlayerData);

        // 触发列表刷新
        OnPlayerDataListChanged?.Invoke();
        Debug.Log($"玩家重命名成功，新用户名: {newUsername}");
        return true;
    }
    #endregion

    #region 上次登录记录
    public void SaveLastLogin(string playerID)
    {
        PlayerPrefs.SetString(LAST_LOGIN_PLAYER_ID_KEY, playerID);
        PlayerPrefs.Save();
    }

    public void ClearLastLogin()
    {
        PlayerPrefs.DeleteKey(LAST_LOGIN_PLAYER_ID_KEY);
        PlayerPrefs.Save();
    }

    public string LoadLastLogin() => PlayerPrefs.GetString(LAST_LOGIN_PLAYER_ID_KEY, null);
    #endregion

    #region 自动登录
    private void AutoLoginOrCreate()
    {
        var allPlayers = GetAllPlayerData();
        if (allPlayers.Count == 0)
        {
            CreateNewPlayer("New Player");
        }
        else
        {
            string lastLoginID = LoadLastLogin();
            if (!string.IsNullOrEmpty(lastLoginID))
            {
                PlayerData lastPlayer = allPlayers.Find(p => p.PlayerID == lastLoginID);
                if (lastPlayer != null)
                    LoginWithPlayerID(lastLoginID);
                else
                    LoginWithPlayerID(allPlayers[0].PlayerID);
            }
            else
            {
                LoginWithPlayerID(allPlayers[0].PlayerID);
            }
        }
    }
    #endregion

    #region 事件触发方法
    private void TriggerPlayerDataChanged()
    {
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        SaveCurrentPlayerData();
    }

    private void TriggerCoinsChanged(int newValue)
    {
        OnCoinsChanged?.Invoke(newValue);
        TriggerPlayerDataChanged();
    }

    private void TriggerCrystalsChanged(int newValue)
    {
        OnCrystalsChanged?.Invoke(newValue);
        TriggerPlayerDataChanged();
    }

    private void TriggerStaminaChanged(int newValue)
    {
        OnStaminaChanged?.Invoke(newValue);
        TriggerPlayerDataChanged();
    }
    #endregion

    // ====================   玩家数据操作方法   ====================
    #region 玩家数据操作方法

    /// <summary>
    /// 给玩家增加经验
    /// </summary>
    public void AddExperience(int amount)
    {
        int oldLevel = CurrentPlayerData.Level;
        int oldExp = CurrentPlayerData.Experience;

        // 累加经验
        CurrentPlayerData.Experience += amount;

        // 循环升级，保留溢出经验
        int newLevel = CurrentPlayerData.Level;
        while (CurrentPlayerData.Experience >= ExperienceCurve.RequiredExp(newLevel))
        {
            CurrentPlayerData.Experience -= ExperienceCurve.RequiredExp(newLevel);
            newLevel++;
            OnPlayerLevelUp?.Invoke(newLevel); // 每升一级通知一次
        }
        CurrentPlayerData.Level = newLevel;

        // 触发经验变化事件（仅当数据实际变化时）
        if (oldLevel != newLevel || oldExp != CurrentPlayerData.Experience)
        {
            OnExperienceChanged?.Invoke(oldLevel, oldExp, newLevel, CurrentPlayerData.Experience);
        }
    }
    #endregion

    // ====================   任务数据操作方法   ====================
    #region 任务数据操作方法
    /// <summary>
    /// 将任务设为可用（前置任务完成时调用）
    /// </summary>
    public bool UnlockQuest(string questId)
    {
        if (CurrentPlayerData == null) return false;

        // 如果已经完成，不再重复解锁
        if (CurrentPlayerData.completedQuestIds.Contains(questId))
            return false;

        var existing = CurrentPlayerData.activeQuests.Find(q => q.questId == questId);
        if (existing != null)
        {
            // 如果状态是 Locked，升级为 Available
            if (existing.state == QuestProgressState.Locked)
            {
                existing.state = QuestProgressState.Available;
                SaveCurrentPlayerData();
                OnQuestAdded?.Invoke(questId);
                OnPlayerDataChanged?.Invoke(CurrentPlayerData);
                return true;
            }
            return false;
        }

        // 新建任务进度，初始为 Available
        var progress = new PlayerQuestProgress(questId);
        progress.state = QuestProgressState.Available;
        // 如果是战斗任务，需要从静态数据中初始化目标列表
        if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData) &&
            questData.contentType == QuestContentType.Combat &&
            questData.objectives != null)
        {
            foreach (var objDefine in questData.objectives)
            {
                var objProgress = new ObjectiveProgress(
                    objDefine.objectiveId,
                    0,
                    objDefine.requiredAmount,
                    false
                );
                progress.objectives.Add(objProgress);
            }
        }

        CurrentPlayerData.activeQuests.Add(progress);
        SaveCurrentPlayerData();
        OnQuestAdded?.Invoke(questId);
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary>
    /// 战斗失败时重置任务进度，并保持 Available 状态
    /// </summary>
    public bool ResetQuestToAvailable(string questId)
    {
        if (CurrentPlayerData == null) return false;
        var progress = CurrentPlayerData.activeQuests.Find(q => q.questId == questId);
        if (progress == null) return false;

        // 重置目标进度
        if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData) &&
            questData.contentType == QuestContentType.Combat &&
            questData.objectives != null)
        {
            foreach (var objDefine in questData.objectives)
            {
                var obj = progress.objectives.Find(o => o.objectiveId == objDefine.objectiveId);
                if (obj != null)
                {
                    obj.currentAmount = 0;
                    obj.isCompleted = false;
                }
                else
                {
                    // 如果缺失则补充
                    progress.objectives.Add(new ObjectiveProgress(objDefine.objectiveId, 0, objDefine.requiredAmount, false));
                }
            }
        }

        progress.state = QuestProgressState.Available;
        SaveCurrentPlayerData();
        OnQuestProgressUpdated?.Invoke(questId);
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary>
    /// 完成任务：从 activeQuests 移除，添加到 completedQuestIds
    /// </summary>
    public bool CompleteQuest(string questId)
    {
        if (CurrentPlayerData == null) return false;
        var progress = CurrentPlayerData.activeQuests.Find(q => q.questId == questId);
        if (progress == null)
        {
            Debug.LogWarning($"任务 {questId} 不在进行中，无法完成");
            return false;
        }

        CurrentPlayerData.activeQuests.Remove(progress);
        if (!CurrentPlayerData.completedQuestIds.Contains(questId))
            CurrentPlayerData.completedQuestIds.Add(questId);

        SaveCurrentPlayerData();
        OnQuestCompleted?.Invoke(questId);
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary>
    /// 获取所有 Available 状态的任务（激活但未开始）
    /// </summary>
    public List<PlayerQuestProgress> GetAvailableQuests()
    {
        if (CurrentPlayerData == null) return new List<PlayerQuestProgress>();
        return CurrentPlayerData.activeQuests.FindAll(q => q.state == QuestProgressState.Available);
    }

    /// <summary>
    /// 获取指定任务的进度（如果存在）
    /// </summary>
    public PlayerQuestProgress GetQuestProgress(string questId)
    {
        return CurrentPlayerData?.activeQuests.Find(q => q.questId == questId);
    }

    /// <summary>
    /// 更新指定任务的某个目标进度（增加 deltaAmount），自动检测目标是否完成。
    /// 返回 true 表示更新成功，false 表示任务不存在或目标不存在。
    /// </summary>
    public bool UpdateObjective(string questId, string objectiveId, int deltaAmount)
    {
        if (CurrentPlayerData == null) return false;

        var progress = CurrentPlayerData.activeQuests.Find(q => q.questId == questId);
        if (progress == null)
        {
            Debug.LogWarning($"任务 {questId} 不在进行中");
            return false;
        }

        var obj = progress.objectives.Find(o => o.objectiveId == objectiveId);
        if (obj == null)
        {
            Debug.LogWarning($"任务 {questId} 中不存在目标 {objectiveId}");
            return false;
        }

        if (obj.isCompleted) return true; // 已完成的目标不再更新

        obj.currentAmount += deltaAmount;
        // 目标是否完成需要知道 requiredAmount，但这里没有静态数据。因此我们无法判断。
        // 解决方案：要么由调用方传入 requiredAmount，要么让调用方自己判断后设置 isCompleted。
        // 为了保持数据操作集中，我们让调用方在更新时提供当前总量，或者我们存储 requiredAmount 在 ObjectiveProgress 中？
        // 回顾 ObjectiveProgress 定义：它只有 currentAmount 和 isCompleted，没有 requiredAmount。
        // requiredAmount 存在于静态数据 QuestObjectiveDefineSO 中。所以这里无法判断是否完成。
        // 因此，修改设计：UpdateObjective 只更新 currentAmount，不负责判断完成。由调用方（如 QuestManager）在更新后检查所有目标，如果完成则调用 CompleteQuest。
        // 或者我们在 ObjectiveProgress 中增加 requiredAmount 字段（冗余但方便），在初始化时从静态数据填入。
        // 这里选择第二种：在激活任务时，由调用方（QuestManager）从静态数据读取 requiredAmount 并设置到每个 ObjectiveProgress 中。
        // 这样 UpdateObjective 就可以检查是否完成。
        // 为了兼容，我们假设 ObjectiveProgress 中已有 requiredAmount 字段。需要修改 ObjectiveProgress 定义。
        // 但为简化，我们先让 UpdateObjective 只更新数值，不自动完成，由外部检测。
        // 因此，此方法只更新数值并保存，不返回是否完成。

        SaveCurrentPlayerData();
        OnQuestProgressUpdated?.Invoke(questId);
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary>
    /// 直接设置目标的完成状态（用于外部判断后调用）
    /// </summary>
    public bool SetObjectiveCompleted(string questId, string objectiveId, bool completed)
    {
        if (CurrentPlayerData == null) return false;
        var progress = CurrentPlayerData.activeQuests.Find(q => q.questId == questId);
        if (progress == null) return false;
        var obj = progress.objectives.Find(o => o.objectiveId == objectiveId);
        if (obj == null) return false;
        obj.isCompleted = completed;
        SaveCurrentPlayerData();
        OnQuestProgressUpdated?.Invoke(questId);
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary>
    /// 获取进行中的任务列表（返回副本以防外部修改）
    /// </summary>
    public List<PlayerQuestProgress> GetActiveQuests()
    {
        if (CurrentPlayerData == null) return new List<PlayerQuestProgress>();
        return new List<PlayerQuestProgress>(CurrentPlayerData.activeQuests);
    }

    /// <summary>
    /// 检查任务是否已完成
    /// </summary>
    public bool HasCompletedQuest(string questId)
    {
        return CurrentPlayerData != null && CurrentPlayerData.completedQuestIds.Contains(questId);
    }
    #endregion


    // ====================   武器系统业务逻辑   ====================
    #region 武器系统业务逻辑

    /// <summary> 获取当前所有武器（绎语）</summary>
    public List<ExotextData> GetAllExotexts()
    {
        return CurrentPlayerData?.ExotextBag ?? new List<ExotextData>();
    }

    /// <summary> 根据 defineId 获取武器数据 </summary>
    public ExotextData GetExotextByDefineId(string defineId)
    {
        if (CurrentPlayerData == null) return null;
        return CurrentPlayerData.ExotextBag.Find(w => w.Id == defineId);
    }

    /// <summary> 检查武器是否已获得 </summary>
    public bool IsExotextUnlocked(string defineId)
    {
        return GetExotextByDefineId(defineId) != null;
    }

    /// <summary> 为玩家添加一个武器（唯一获得）</summary>
    public bool AddExotext(string defineId)
    {
        if (CurrentPlayerData == null) return false;
        if (IsExotextUnlocked(defineId)) return false;

        // 从静态数据中获取定义
        if (!GameDataManager.Instance.ExotextDict.TryGetValue(defineId, out var def))
        {
            Debug.LogError($"未找到武器定义: {defineId}");
            return false;
        }

        var weapon = new ExotextData(
            id: def.id,
            type: def.type,
            element: def.element,
            stars: def.baseStars,
            maxstars: def.maxStars,
            health: def.baseHealth,
            attack: def.baseAttack,
            defence: def.baseDefence,
            energy: def.baseEnergy,
            critRate: def.baseCritRate,
            critDamage: def.baseCritDamage,
            elementBonus: def.baseElementBonus
        );
        CurrentPlayerData.ExotextBag.Add(weapon);
        CurrentPlayerData.SortedBag(); // 保持排序
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary> 获取某个武器类别当前装备的武器 ID </summary>
    public string GetEquippedExotextId(ExotextType type)
    {
        if (CurrentPlayerData == null) return null;
        int idx = (int)type;
        if (idx < 0 || idx >= CurrentPlayerData.EquippedExotextIds.Length) return null;
        return CurrentPlayerData.EquippedExotextIds[idx];
    }

    /// <summary> 获取某个武器类别当前装备的武器数据 </summary>
    public ExotextData GetEquippedExotext(ExotextType type)
    {
        string id = GetEquippedExotextId(type);
        if (string.IsNullOrEmpty(id)) return null;
        return GetExotextByDefineId(id);
    }

    /// <summary> 装备指定武器（自动替换同类别当前装备）</summary>
    public bool EquipExotext(string defineId)
    {
        if (CurrentPlayerData == null) return false;
        var weapon = GetExotextByDefineId(defineId);
        if (weapon == null)
        {
            Debug.LogError($"未获得武器: {defineId}");
            return false;
        }

        int typeIndex = (int)weapon.Type;
        if (typeIndex < 0 || typeIndex >= CurrentPlayerData.EquippedExotextIds.Length) return false;

        // 检查是否已经装备了同一武器（直接返回 true 避免无意义保存）
        if (CurrentPlayerData.EquippedExotextIds[typeIndex] == defineId)
            return true;

        CurrentPlayerData.EquippedExotextIds[typeIndex] = defineId;
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    /// <summary> 卸下指定类别的武器（置空）</summary>
    public bool UnequipExotext(ExotextType type)
    {
        if (CurrentPlayerData == null) return false;
        int idx = (int)type;
        if (idx < 0 || idx >= CurrentPlayerData.EquippedExotextIds.Length) return false;
        if (CurrentPlayerData.EquippedExotextIds[idx] == null) return false;

        CurrentPlayerData.EquippedExotextIds[idx] = null;
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    #endregion


    // ====================   防具系统业务逻辑   ====================
    #region 防具系统业务逻辑

    public List<NexusVestureData> GetAllNexusVestures()
    {
        return CurrentPlayerData?.NexusVestureBag ?? new List<NexusVestureData>();
    }

    public NexusVestureData GetNexusVestureByDefineId(string defineId)
    {
        return CurrentPlayerData?.NexusVestureBag.Find(a => a.Id == defineId);
    }

    public bool IsNexusVestureUnlocked(string defineId)
    {
        return GetNexusVestureByDefineId(defineId) != null;
    }

    public bool AddNexusVesture(string defineId)
    {
        if (CurrentPlayerData == null) return false;
        if (IsNexusVestureUnlocked(defineId)) return false;

        if (!GameDataManager.Instance.NexusVestureDict.TryGetValue(defineId, out var def))
        {
            Debug.LogError($"未找到防具定义: {defineId}");
            return false;
        }

        var vesture = new NexusVestureData(
            id: def.id,
            position: def.Position,
            element: def.element,
            stars: def.baseStars,
            maxstars: def.maxStars,
            health: def.baseHealth,
            attack: def.baseAttack,
            defence: def.baseDefence,
            energy: def.baseEnergy,
            critRate: def.baseCritRate,
            critDamage: def.baseCritDamage,
            elementBonus: def.baseElementBonus
        );
        CurrentPlayerData.NexusVestureBag.Add(vesture);
        CurrentPlayerData.SortedBag();
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    public string GetEquippedNexusVestureId(NexusVesturePosition pos)
    {
        if (CurrentPlayerData == null) return null;
        int idx = (int)pos;
        if (idx < 0 || idx >= CurrentPlayerData.EquippedNexusVestureIds.Length) return null;
        return CurrentPlayerData.EquippedNexusVestureIds[idx];
    }

    public NexusVestureData GetEquippedNexusVesture(NexusVesturePosition pos)
    {
        string id = GetEquippedNexusVestureId(pos);
        if (string.IsNullOrEmpty(id)) return null;
        return GetNexusVestureByDefineId(id);
    }

    public bool EquipNexusVesture(string defineId)
    {
        if (CurrentPlayerData == null) return false;
        var vesture = GetNexusVestureByDefineId(defineId);
        if (vesture == null)
        {
            Debug.LogError($"未获得防具: {defineId}");
            return false;
        }

        int posIdx = (int)vesture.Position;
        if (posIdx < 0 || posIdx >= CurrentPlayerData.EquippedNexusVestureIds.Length) return false;

        if (CurrentPlayerData.EquippedNexusVestureIds[posIdx] == defineId)
            return true;

        CurrentPlayerData.EquippedNexusVestureIds[posIdx] = defineId;
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    public bool UnequipNexusVesture(NexusVesturePosition pos)
    {
        if (CurrentPlayerData == null) return false;
        int idx = (int)pos;
        if (idx < 0 || idx >= CurrentPlayerData.EquippedNexusVestureIds.Length) return false;
        if (CurrentPlayerData.EquippedNexusVestureIds[idx] == null) return false;

        CurrentPlayerData.EquippedNexusVestureIds[idx] = null;
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
        return true;
    }

    #endregion


    // ==================== 模块配置存取辅助方法 ====================
    #region 模块配置存取
    // 获取指定武器的模块ID列表（返回 List<string>）
    public List<string> GetWeaponModuleList(int weaponIndex)
    {
        if (CurrentPlayerData == null || weaponIndex < 0 || weaponIndex >= 7)
            return null;
        var wrapper = CurrentPlayerData.equippedModuleIdsForWeapons[weaponIndex];
        return wrapper?.moduleIds ?? new List<string>();
    }

    // 设置指定武器的模块ID列表（接收 List<string>）
    public void SetWeaponModuleList(int weaponIndex, List<string> moduleIds)
    {
        if (CurrentPlayerData == null || weaponIndex < 0 || weaponIndex >= 7)
            return;
        if (CurrentPlayerData.equippedModuleIdsForWeapons[weaponIndex] == null)
            CurrentPlayerData.equippedModuleIdsForWeapons[weaponIndex] = new WeaponModuleList();
        CurrentPlayerData.equippedModuleIdsForWeapons[weaponIndex].moduleIds = moduleIds ?? new List<string>();
        SaveCurrentPlayerData();
        OnPlayerDataChanged?.Invoke(CurrentPlayerData);
    }

    // 保存单个武器的模块配置（供UI调用）
    public void SaveWeaponModules(int weaponIndex, List<string> moduleIds)
    {
        SetWeaponModuleList(weaponIndex, moduleIds);
    }
    #endregion
}