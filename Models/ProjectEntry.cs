using System;

namespace NoCodeMotion.Models
{
    /// <summary>项目管理页表格的一行：工程名 + 创建时间 + 修改时间 + 备注。</summary>
    public class ProjectEntry
    {
        public string Name { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Remark { get; set; }
    }
}