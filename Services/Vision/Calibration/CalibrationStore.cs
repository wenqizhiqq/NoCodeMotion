// === NoCodeMotion 视觉标定结果仓库（工程「标定」表读写） | 作者：温​启‎志‍ ‍|⁣ ⁣微‏信‎：‏1‏8‌7‍1‌9‎3⁠6‏1​3​9‌9‌ ⁠|​ ​保‎留‍所⁠有‍权⁣利‏，⁣请‌勿‍删‎除 ===
using System.Collections.ObjectModel;
using NoCodeMotion.Models;

namespace NoCodeMotion.Services.Vision.Calibration
{
    /// <summary>
    /// 工程标定结果仓库：按相机名增改查，数据落在 ProjectData.Calibrations（xlsx「标定」工作表）。
    /// 视觉流程的「标定」步骤写入，模板匹配读取它把像素位置换算成机台 mm 真实位置。
    /// </summary>
    public static class CalibrationStore
    {
        /// <summary>全部标定记录（工程未载入时为 null）。</summary>
        public static ObservableCollection<CameraCalibration>? All => ProjectStore.Data?.Calibrations;

        /// <summary>按相机名查（忽略大小写与首尾空白）；相机名为空时返回 null。</summary>
        public static CameraCalibration? Find(string? cameraName)
        {
            var name = (cameraName ?? "").Trim();
            if (name.Length == 0) return null;
            var all = All;
            if (all == null) return null;
            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i];
                if (r == null) continue;
                if (string.Equals((r.CameraName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase)) return r;
            }
            return null;
        }

        /// <summary>查一台相机的「可用」标定（成功且像素当量 &gt; 0）；没有则返回 null。</summary>
        public static CameraCalibration? FindUsable(string? cameraName)
        {
            var r = Find(cameraName);
            return r != null && r.IsUsable ? r : null;
        }

        /// <summary>是否已有某相机的可用标定。</summary>
        public static bool HasUsable(string? cameraName) => FindUsable(cameraName) != null;

        /// <summary>新增或覆盖一台相机的标定记录（按相机名匹配），返回写入后的记录。</summary>
        public static CameraCalibration? Upsert(CameraCalibration? rec)
        {
            if (rec == null) return null;
            var all = All;
            if (all == null) return rec;
            var name = (rec.CameraName ?? "").Trim();
            for (int i = 0; i < all.Count; i++)
            {
                var cur = all[i];
                if (cur == null) continue;
                if (string.Equals((cur.CameraName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase))
                {
                    all[i] = rec;
                    return rec;
                }
            }
            all.Add(rec);
            return rec;
        }

        /// <summary>删除一台相机的标定；返回是否真的删掉了。</summary>
        public static bool Remove(string? cameraName)
        {
            var all = All;
            if (all == null) return false;
            var name = (cameraName ?? "").Trim();
            for (int i = 0; i < all.Count; i++)
            {
                var cur = all[i];
                if (cur == null) continue;
                if (string.Equals((cur.CameraName ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase))
                {
                    all.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>清空全部标定。</summary>
        public static void Clear() => All?.Clear();
    }
}
// === NoCodeMotion 视觉标定结果仓库（工程「标定」表读写） | 作者：温⁣启⁠志‌ ‌|​ ​微⁠信​：‎1‎8‎7‎1⁠9‎3​6‏1‎3‎9⁠9⁠ ​|⁣ ‌保⁣留​所⁠有​权‍利‏，⁠请‎勿‏删‍除 ===
