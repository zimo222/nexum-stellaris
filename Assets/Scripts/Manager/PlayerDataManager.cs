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
    public event Action<int> OnCoinsChanged;
    public event Action<int> OnCrystalsChanged;
    public event Action<int> OnStaminaChanged;
    public event Action OnPlayerDataListChanged;
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
}