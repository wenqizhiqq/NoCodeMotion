// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温启志◆编写◇微信﹕187◆1936◇1399　※保留所有权利请勿删除◇​⁣​
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services
{
    /// <summary>
    /// 用户存储（静态单例）：把用户列表与当前登录用户持久化到
    /// LocalApplicationData/NoCodeMotion/Users.json。首次启动自动播种默认用户（管理员 / 操作员）。
    /// 仅做轻量权限区分（管理员 / 操作员），不强制拦截功能。
    /// </summary>
    public static class UserStore
    {
        private static readonly string Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoCodeMotion");

        private static readonly string FilePath = Path.Combine(Dir, "Users.json");

        private static readonly object _lock = new();

        /// <summary>全部用户（只读视图，外部勿直接增删，请用 Add/Remove）。</summary>
        public static IReadOnlyList<AppUser> Users => _users;

        private static List<AppUser> _users = new();

        /// <summary>当前登录用户（未登录时为 null）。</summary>
        public static AppUser? Current { get; private set; }

        private class Dto
        {
            public List<AppUser> Users { get; set; } = new();
            public string Current { get; set; } = "";
        }

        static UserStore()
        {
            Load();
            EnsureSeed();
        }

        public static void Load()
        {
            lock (_lock)
            {
                _users = new List<AppUser>();
                Current = null;
                try
                {
                    if (File.Exists(FilePath))
                    {
                        var json = File.ReadAllText(FilePath);
                        var dto = JsonSerializer.Deserialize<Dto>(json);
                        if (dto?.Users != null)
                        {
                            _users = dto.Users.Where(u => !string.IsNullOrWhiteSpace(u.Name)).ToList();
                            if (!string.IsNullOrWhiteSpace(dto.Current))
                                Current = _users.FirstOrDefault(u => u.Name == dto.Current);
                        }
                    }
                }
                catch
                {
                    // 文件损坏时忽略，回落到空列表 + 播种
                    _users = new List<AppUser>();
                    Current = null;
                }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(Dir);
                    var dto = new Dto
                    {
                        Users = _users,
                        Current = Current?.Name ?? ""
                    };
                    var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    File.WriteAllText(FilePath, json);
                }
                catch
                {
                    // 持久化失败不影响内存态使用
                }
            }
        }

        private static void EnsureSeed()
        {
            lock (_lock)
            {
                if (_users.Count == 0)
                {
                    _users.Add(new AppUser { Name = "管理员", Role = "管理员", Remark = "默认管理员" });
                    _users.Add(new AppUser { Name = "操作员", Role = "操作员", Remark = "默认操作员" });
                    Current = _users[0];
                    Save();
                }
                else if (Current == null)
                {
                    Current = _users[0];
                    Save();
                }
            }
        }

        /// <summary>切换当前登录用户（按名称）。</summary>
        public static void SetCurrent(string name)
        {
            lock (_lock)
            {
                var u = _users.FirstOrDefault(x => x.Name == name);
                if (u != null)
                {
                    Current = u;
                    Save();
                }
            }
        }

        /// <summary>新增用户；名称重复或为空时返回 false。</summary>
        public static bool Add(string name, string role)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (_lock)
            {
                if (_users.Any(u => u.Name == name)) return false;
                _users.Add(new AppUser { Name = name, Role = role, Remark = "" });
                Save();
                return true;
            }
        }

        /// <summary>删除用户；至少保留 1 个、且不能删除当前登录用户，返回是否成功。</summary>
        public static bool Remove(string name)
        {
            name = (name ?? "").Trim();
            lock (_lock)
            {
                if (_users.Count <= 1) return false;
                var u = _users.FirstOrDefault(x => x.Name == name);
                if (u == null) return false;
                if (Current != null && Current.Name == name) return false;
                _users.Remove(u);
                Save();
                return true;
            }
        }

        /// <summary>重命名用户；新名称为空或与他人重复时返回 false。</summary>
        public static bool Rename(string oldName, string newName)
        {
            oldName = (oldName ?? "").Trim();
            newName = (newName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
            lock (_lock)
            {
                var u = _users.FirstOrDefault(x => x.Name == oldName);
                if (u == null) return false;
                if (_users.Any(x => x.Name == newName && x != u)) return false;
                u.Name = newName;
                if (Current != null && Current.Name == oldName) Current = u;
                Save();
                return true;
            }
        }

        /// <summary>修改用户角色。</summary>
        public static bool SetRole(string name, string role)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;
            lock (_lock)
            {
                var u = _users.FirstOrDefault(x => x.Name == name);
                if (u == null) return false;
                u.Role = role;
                Save();
                return true;
            }
        }
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
