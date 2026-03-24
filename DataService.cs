using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace NOT_VE_GÜNLÜK
{
    public class AppState
    {
        public int? CurrentUserId { get; set; }
        public string UserName { get; set; } = "Mustafa";
        public string? ProfilePhotoPath { get; set; }
        public string? CustomBackgroundPath { get; set; }
        public string SelectedTheme { get; set; } = "Dark";
        public string? SelectedTextColor { get; set; }
        public bool IsMaximized { get; set; } = false;
        public List<JournalModel> Journals { get; set; } = new List<JournalModel>();
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? ProfilePhotoPath { get; set; }
        public bool IsLoggedIn { get; set; }
    }

    public static class DataService
    {
        public static string DatabasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NotVeGunluk.db");



        
        public static AppState CurrentState { get; set; } = new AppState();

        public static event Action<string>? ThemeChanged;
        public static event Action<string?>? BackgroundChanged;
        public static event Action<string?>? TextColorChanged;

        public static void NotifyThemeChanged(string theme)
        {
            CurrentState.SelectedTheme = theme;
            ThemeChanged?.Invoke(theme);
        }

        public static void NotifyBackgroundChanged(string? path)
        {
            CurrentState.CustomBackgroundPath = path;
            BackgroundChanged?.Invoke(path);
        }

        public static void NotifyTextColorChanged(string? color)
        {
            CurrentState.SelectedTextColor = color;
            TextColorChanged?.Invoke(color);
        }



        static DataService()
        {
            try 
            { 
                InitializeDatabase();
                MigratePlaintextPasswords();
            } 
            catch { }
        }

        // Düz metin şifreleri SHA256 hash'e dönüştürür (tek seferlik migration)
        private static void MigratePlaintextPasswords()
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            // Hash'lenmemiş şifreleri bul (SHA256 hex = 64 karakter)
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT Id, Password FROM Users WHERE LENGTH(Password) != 64";
            
            var toUpdate = new List<(int id, string hashed)>();
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["Id"]);
                    string plain = reader["Password"].ToString() ?? "";
                    toUpdate.Add((id, HashPassword(plain)));
                }
            }

            foreach (var (id, hashed) in toUpdate)
            {
                var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = "UPDATE Users SET Password = @p WHERE Id = @id";
                updateCmd.Parameters.AddWithValue("@p", hashed);
                updateCmd.Parameters.AddWithValue("@id", id);
                updateCmd.ExecuteNonQuery();
            }
        }
        
        private static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Password TEXT NOT NULL,
                        DisplayName TEXT NOT NULL,
                        ProfilePhotoPath TEXT,
                        CustomBackgroundPath TEXT,
                        SelectedTheme TEXT DEFAULT 'Dark',
                        IsLoggedIn INTEGER DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS AppSettings (
                        Id INTEGER PRIMARY KEY,
                        UserName TEXT,
                        ProfilePhotoPath TEXT,
                        CustomBackgroundPath TEXT,
                        SelectedTheme TEXT NOT NULL DEFAULT 'Dark',
                        SelectedTextColor TEXT,
                        IsMaximized INTEGER DEFAULT 0,
                        CurrentUserId INTEGER
                    );
                    
                    /* Migration: Add columns to Users if they don't exist */
                    PRAGMA table_info(AppSettings);
                ";
                using (var reader = command.ExecuteReader())
                {
                    bool hasBg = false;
                    bool hasTheme = false;
                    while (reader.Read())
                    {
                        string? colName = reader["name"]?.ToString();
                        if (colName == "CustomBackgroundPath") hasBg = true;
                        if (colName == "SelectedTheme") hasTheme = true;
                    }
                    reader.Close();

                    if (!hasBg)
                    {
                        var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = "ALTER TABLE Users ADD COLUMN CustomBackgroundPath TEXT";
                        alterCmd.ExecuteNonQuery();
                    }
                    if (!hasTheme)
                    {
                        var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = "ALTER TABLE Users ADD COLUMN SelectedTheme TEXT DEFAULT 'Dark'";
                        alterCmd.ExecuteNonQuery();
                    }

                    // Check for SelectedTextColor
                    command.CommandText = "PRAGMA table_info(Users)";
                    bool hasTextColor = false;
                    using (var rdr = command.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr["name"]?.ToString() == "SelectedTextColor") hasTextColor = true;
                        }
                    }
                    if (!hasTextColor)
                    {
                        var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = "ALTER TABLE Users ADD COLUMN SelectedTextColor TEXT";
                        alterCmd.ExecuteNonQuery();
                    }

                    // Also check AppSettings
                    command.CommandText = "PRAGMA table_info(AppSettings)";
                    bool hasTextColorApp = false;
                    using (var rdr = command.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            if (rdr["name"]?.ToString() == "SelectedTextColor") hasTextColorApp = true;
                        }
                    }
                    if (!hasTextColorApp)
                    {
                        var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = "ALTER TABLE AppSettings ADD COLUMN SelectedTextColor TEXT";
                        alterCmd.ExecuteNonQuery();
                    }
                }

                // AppSettings tablosuna IsMaximized kolonu ekleme (Migration)
                command.CommandText = "PRAGMA table_info(AppSettings)";
                using (var reader = command.ExecuteReader())
                {
                    bool hasIsMaximized = false;
                    while (reader.Read())
                    {
                        if (reader["name"]?.ToString() == "IsMaximized") hasIsMaximized = true;
                    }
                    reader.Close();

                    if (!hasIsMaximized)
                    {
                        var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = "ALTER TABLE AppSettings ADD COLUMN IsMaximized INTEGER DEFAULT 0";
                        alterCmd.ExecuteNonQuery();
                    }
                }

                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Journals (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        LastUpdated TEXT,
                        Icon TEXT,
                        UpdateTimeDescription TEXT,
                        SelectedTheme TEXT,
                        Type INTEGER DEFAULT 0,
                        FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS JournalEntries (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        JournalId INTEGER,
                        Date TEXT NOT NULL,
                        Title TEXT NOT NULL,
                        Content TEXT,
                        FOREIGN KEY (JournalId) REFERENCES Journals (Id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EntryId INTEGER,
                        Title TEXT NOT NULL,
                        Status INTEGER DEFAULT 0,
                        FOREIGN KEY (EntryId) REFERENCES JournalEntries (Id) ON DELETE CASCADE
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

        public static bool Register(string username, string password, string displayName)
        {
            try
            {
                username = username.Trim();
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO Users (Username, Password, DisplayName) VALUES (@u, @p, @d)";
                    command.Parameters.AddWithValue("@u", username);
                    command.Parameters.AddWithValue("@p", HashPassword(password));
                    command.Parameters.AddWithValue("@d", displayName);
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Login(string username, string password)
        {
            try
            {
                username = username.Trim();
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Id, DisplayName, ProfilePhotoPath, CustomBackgroundPath, SelectedTheme, SelectedTextColor FROM Users WHERE Username = @u AND Password = @p";
                    command.Parameters.AddWithValue("@u", username);
                    command.Parameters.AddWithValue("@p", HashPassword(password));
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userId = Convert.ToInt32(reader["Id"]);
                            CurrentState.CurrentUserId = userId;
                            CurrentState.UserName = reader["DisplayName"]?.ToString() ?? username;
                            CurrentState.ProfilePhotoPath = reader["ProfilePhotoPath"] as string;
                            CurrentState.CustomBackgroundPath = reader["CustomBackgroundPath"] as string;
                            CurrentState.SelectedTheme = reader["SelectedTheme"]?.ToString() ?? "Dark";
                            CurrentState.SelectedTextColor = reader["SelectedTextColor"] as string;
                            
                            reader.Close();
                            
                            SaveAppSettingsToDatabase(CurrentState, connection);
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static void Logout()
        {
            CurrentState.CurrentUserId = null;
            CurrentState.CustomBackgroundPath = null;
            CurrentState.SelectedTheme = "Dark";
            CurrentState.SelectedTextColor = null;
            Save();
        }

        public static void Load()
        {
            try
            {
                LoadFromDatabase();
            }
            catch
            {
                CurrentState = new AppState();
                CreateDefaultData();
            }
        }



        private static void LoadFromDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
            {
                connection.Open();

                // 1. Önce AppSettings'ten CurrentUserId ve diğer ayarları yükle
                var settingsCommand = connection.CreateCommand();
                settingsCommand.CommandText = "SELECT CurrentUserId, CustomBackgroundPath, SelectedTheme, SelectedTextColor, IsMaximized FROM AppSettings WHERE Id = 1";
                using (var settingsReader = settingsCommand.ExecuteReader())
                {
                    if (settingsReader.Read())
                    {
                        CurrentState.CurrentUserId = settingsReader["CurrentUserId"] != DBNull.Value ? (int?)Convert.ToInt32(settingsReader["CurrentUserId"]) : null;
                        // Not loading global bg/theme here if you want it strictly per-user, 
                        // but let's keep it as fallback if not logged in.
                        if (CurrentState.CurrentUserId == null)
                        {
                            CurrentState.CustomBackgroundPath = settingsReader["CustomBackgroundPath"] as string;
                            CurrentState.SelectedTheme = settingsReader["SelectedTheme"]?.ToString() ?? "Dark";
                            CurrentState.SelectedTextColor = settingsReader["SelectedTextColor"] as string;
                        }
                        CurrentState.IsMaximized = settingsReader["IsMaximized"] != DBNull.Value && Convert.ToInt32(settingsReader["IsMaximized"]) == 1;
                    }
                }

                // 2. Eğer login olan bir kullanıcı varsa, bilgilerini Users tablosundan çek
                if (CurrentState.CurrentUserId != null)
                {
                    var userCmd = connection.CreateCommand();
                    userCmd.CommandText = "SELECT DisplayName, ProfilePhotoPath, CustomBackgroundPath, SelectedTheme, SelectedTextColor FROM Users WHERE Id = @id LIMIT 1";
                    userCmd.Parameters.AddWithValue("@id", CurrentState.CurrentUserId);
                    using (var userReader = userCmd.ExecuteReader())
                    {
                        if (userReader.Read())
                        {
                            CurrentState.UserName = userReader["DisplayName"]?.ToString() ?? "Mustafa";
                            CurrentState.ProfilePhotoPath = userReader["ProfilePhotoPath"] as string;
                            CurrentState.CustomBackgroundPath = userReader["CustomBackgroundPath"] as string;
                            CurrentState.SelectedTheme = userReader["SelectedTheme"]?.ToString() ?? "Dark";
                            CurrentState.SelectedTextColor = userReader["SelectedTextColor"] as string;
                        }
                        else
                        {
                            // Kullanıcı bulunamadıysa (silinmiş olabilir), session'ı temizle
                            CurrentState.CurrentUserId = null;
                        }
                    }
                }

                // Journals yükle (sadece mevcut kullanıcıya ait olanlar)
                CurrentState.Journals = new List<JournalModel>();
                if (CurrentState.CurrentUserId != null)
                {
                    var journalsCommand = connection.CreateCommand();
                    journalsCommand.CommandText = "SELECT * FROM Journals WHERE UserId = @userId ORDER BY Id";
                    journalsCommand.Parameters.AddWithValue("@userId", CurrentState.CurrentUserId);
                    
                    var journals = new List<JournalModel>();
                    var journalIds = new List<int>();
                    
                    using (var journalsReader = journalsCommand.ExecuteReader())
                    {
                        while (journalsReader.Read())
                        {
                            var journal = new JournalModel
                            {
                                Title = journalsReader["Title"].ToString() ?? "",
                                LastUpdated = journalsReader["LastUpdated"] as string,
                                Icon = journalsReader["Icon"] as string,
                                UpdateTimeDescription = journalsReader["UpdateTimeDescription"] as string,
                                SelectedTheme = journalsReader["SelectedTheme"] as string ?? "Light",
                                Type = (JournalType)Convert.ToInt32(journalsReader["Type"])
                            };
                            
                            var journalId = Convert.ToInt32(journalsReader["Id"]);
                            journalIds.Add(journalId);
                            journals.Add(journal);
                        }
                    }
                    
                    // Şimdi her journal için entries yükle
                    for (int i = 0; i < journals.Count; i++)
                    {
                        journals[i].Entries = LoadJournalEntries(connection, journalIds[i]);
                    }
                    
                    CurrentState.Journals = journals;
                }
            }
        }

        private static List<JournalEntry> LoadJournalEntries(SqliteConnection connection, int journalId)
        {
            var entries = new List<JournalEntry>();
            var entryIds = new List<int>();
            
            // Önce entries'leri yükle
            var entriesCommand = connection.CreateCommand();
            entriesCommand.CommandText = "SELECT * FROM JournalEntries WHERE JournalId = @journalId ORDER BY Date DESC";
            entriesCommand.Parameters.AddWithValue("@journalId", journalId);
            
            using (var entriesReader = entriesCommand.ExecuteReader())
            {
                while (entriesReader.Read())
                {
                    var entry = new JournalEntry
                    {
                        Date = DateTime.Parse(entriesReader["Date"].ToString() ?? DateTime.Now.ToString()),
                        Title = entriesReader["Title"].ToString() ?? "",
                        Content = entriesReader["Content"] as string
                    };
                    
                    entryIds.Add(Convert.ToInt32(entriesReader["Id"]));
                    entries.Add(entry);
                }
            }

            // Sonra her entry için tasks yükle
            for (int i = 0; i < entries.Count; i++)
            {
                var tasksCommand = connection.CreateCommand();
                tasksCommand.CommandText = "SELECT * FROM Tasks WHERE EntryId = @entryId";
                tasksCommand.Parameters.AddWithValue("@entryId", entryIds[i]);
                
                using (var tasksReader = tasksCommand.ExecuteReader())
                {
                    while (tasksReader.Read())
                    {
                        entries[i].Tasks.Add(new TaskItem
                        {
                            Title = tasksReader["Title"].ToString() ?? "",
                            Status = (TaskStatus)Convert.ToInt32(tasksReader["Status"])
                        });
                    }
                }
            }

            return entries;
        }

        private static void CreateDefaultData()
        {
            CurrentState.Journals = new List<JournalModel>
            {
                new JournalModel { Title = "📖 Kişisel Günlük", UpdateTimeDescription = "Son güncelleme: Bugün", Icon = "→" },
                new JournalModel { Title = "💼 İş Notları", UpdateTimeDescription = "Son güncelleme: Dün", Icon = "→" },
                new JournalModel { Title = "✈️ Seyahat Günlüğü", UpdateTimeDescription = "Son güncelleme: 3 gün önce", Icon = "→" }
            };
            Save();
        }

        public static void Save()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        SaveAppSettingsToDatabase(CurrentState, connection);
                        SaveJournalsToDatabase(CurrentState.Journals, connection);

                        transaction.Commit();
                    }
                }
            }
            catch
            {
                // Kaydetme hatası sessizce geçilir
            }
        }

        private static void SaveAppSettingsToDatabase(AppState state, SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO AppSettings (Id, UserName, ProfilePhotoPath, CustomBackgroundPath, SelectedTheme, SelectedTextColor, IsMaximized, CurrentUserId)
                VALUES (1, @userName, @profilePhotoPath, @customBackgroundPath, @selectedTheme, @selectedTextColor, @isMaximized, @currentUserId)
            ";
            command.Parameters.AddWithValue("@userName", state.UserName);
            command.Parameters.AddWithValue("@profilePhotoPath", (object?)state.ProfilePhotoPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@customBackgroundPath", (object?)state.CustomBackgroundPath ?? DBNull.Value);
            command.Parameters.AddWithValue("@selectedTheme", state.SelectedTheme);
            command.Parameters.AddWithValue("@selectedTextColor", (object?)state.SelectedTextColor ?? DBNull.Value);
            command.Parameters.AddWithValue("@isMaximized", state.IsMaximized ? 1 : 0);
            command.Parameters.AddWithValue("@currentUserId", (object?)state.CurrentUserId ?? DBNull.Value);
            command.ExecuteNonQuery();

            // Eğer CurrentUserId varsa Users tablosunda DisplayName'i de güncelle
            if (state.CurrentUserId != null)
            {
                var userCmd = connection.CreateCommand();
                userCmd.CommandText = "UPDATE Users SET DisplayName = @d, ProfilePhotoPath = @p, CustomBackgroundPath = @bg, SelectedTheme = @th, SelectedTextColor = @tc WHERE Id = @id";
                userCmd.Parameters.AddWithValue("@d", state.UserName);
                userCmd.Parameters.AddWithValue("@p", (object?)state.ProfilePhotoPath ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@bg", (object?)state.CustomBackgroundPath ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@th", state.SelectedTheme);
                userCmd.Parameters.AddWithValue("@tc", (object?)state.SelectedTextColor ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@id", state.CurrentUserId);
                userCmd.ExecuteNonQuery();
            }
        }

        private static void SaveJournalsToDatabase(List<JournalModel> journals, SqliteConnection connection)
        {
            // Sadece mevcut kullanıcıya ait verileri temizle ve kaydet
            if (CurrentState.CurrentUserId == null) return;

            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM Journals WHERE UserId = @userId";
            deleteCommand.Parameters.AddWithValue("@userId", CurrentState.CurrentUserId);
            deleteCommand.ExecuteNonQuery();

            foreach (var journal in journals)
            {
                // Journal kaydet
                var journalCommand = connection.CreateCommand();
                journalCommand.CommandText = @"
                    INSERT INTO Journals (UserId, Title, LastUpdated, Icon, UpdateTimeDescription, SelectedTheme, Type)
                    VALUES (@userId, @title, @lastUpdated, @icon, @updateTimeDescription, @selectedTheme, @type)
                ";
                journalCommand.Parameters.AddWithValue("@userId", CurrentState.CurrentUserId);
                journalCommand.Parameters.AddWithValue("@title", journal.Title);
                journalCommand.Parameters.AddWithValue("@lastUpdated", (object?)journal.LastUpdated ?? DBNull.Value);
                journalCommand.Parameters.AddWithValue("@icon", (object?)journal.Icon ?? DBNull.Value);
                journalCommand.Parameters.AddWithValue("@updateTimeDescription", (object?)journal.UpdateTimeDescription ?? DBNull.Value);
                journalCommand.Parameters.AddWithValue("@selectedTheme", journal.SelectedTheme ?? "Light");
                journalCommand.Parameters.AddWithValue("@type", (int)journal.Type);
                journalCommand.ExecuteNonQuery();

                // Son eklenen journal ID'sini al
                var getIdCommand = connection.CreateCommand();
                getIdCommand.CommandText = "SELECT last_insert_rowid()";
                var journalIdObj = getIdCommand.ExecuteScalar();
                var journalId = journalIdObj != null ? Convert.ToInt64(journalIdObj) : 0;
                
                // Entries kaydet
                foreach (var entry in journal.Entries)
                {
                    var entryCommand = connection.CreateCommand();
                    entryCommand.CommandText = @"
                        INSERT INTO JournalEntries (JournalId, Date, Title, Content)
                        VALUES (@journalId, @date, @title, @content)
                    ";
                    entryCommand.Parameters.AddWithValue("@journalId", journalId);
                    entryCommand.Parameters.AddWithValue("@date", entry.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                    entryCommand.Parameters.AddWithValue("@title", entry.Title);
                    entryCommand.Parameters.AddWithValue("@content", (object?)entry.Content ?? DBNull.Value);
                    entryCommand.ExecuteNonQuery();

                    var getEntryIdCommand = connection.CreateCommand();
                    getEntryIdCommand.CommandText = "SELECT last_insert_rowid()";
                    var entryIdObj = getEntryIdCommand.ExecuteScalar();
                    var entryId = entryIdObj != null ? Convert.ToInt64(entryIdObj) : 0;

                    // Tasks kaydet
                    foreach (var task in entry.Tasks)
                    {
                        var taskCommand = connection.CreateCommand();
                        taskCommand.CommandText = @"
                            INSERT INTO Tasks (EntryId, Title, Status)
                            VALUES (@entryId, @title, @status)
                        ";
                        taskCommand.Parameters.AddWithValue("@entryId", entryId);
                        taskCommand.Parameters.AddWithValue("@title", task.Title);
                        taskCommand.Parameters.AddWithValue("@status", (int)task.Status);
                        taskCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
