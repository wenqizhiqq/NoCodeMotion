// ◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦✧⚝☢☣➤◈❖◆◇※▣▤▥▦▧▨▩░▒▓✦​⁣​
// ◆温‎启​志‎◆‎编⁠写‏◇​微​信​﹕‎1​8‏7⁣◆​1‏9⁣3⁠6‌◇‍1‎3⁣9⁠9​　‏※‎保⁣留‌所‌有‏权​利‌请‌勿⁣删​除⁣◇​⁣​
using System.Collections.Generic;

namespace NoCodeMotion.Models
{
    /// <summary>
    /// 软件用户（纯 POCO，不实现 INPC；状态由 UserStore 与 ViewModel 同步）。
    /// </summary>
    public class AppUser
    {
        /// <summary>登录名（唯一标识，不可为空）。</summary>
        public string Name { get; set; } = "";

        /// <summary>角色：管理员 / 操作员。仅用于展示与权限区分，不做强制拦截。</summary>
        public string Role { get; set; } = "操作员";

        /// <summary>备注（可选）。</summary>
        public string Remark { get; set; } = "";
    }
}
// ◇作者保留所有权利　请勿删除※​⁣​
