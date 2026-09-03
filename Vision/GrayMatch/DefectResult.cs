// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
// ============================================================
// 温启志◆编写◇微信﹕187◆1936◇1399
// ============================================================
namespace GrayMatch;

/// <summary>
/// A single detected defect, located on a matched instance.
/// The defect box is a rotation-invariant tight rect (from cv::minAreaRect): W/H are its
/// side lengths and RectAngle its orientation (already in the UI's -angle convention).
/// ImgCx/ImgCy is the defect center mapped back into image space using the same -angle
/// transform as the match box, so the overlay lines up with the green box exactly.
/// BoxLeft/BoxTop and TextLeft/TextTop are expressed relative to the item canvas origin
/// (LeftTopX/LeftTopY) so the ItemsControl can place them directly.
/// </summary>
public class DefectResult
{
    // match placement (item canvas origin = the green box's LeftTop in image space)
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Angle { get; set; }
    public int Tw { get; set; }
    public int Th { get; set; }
    public int LeftTopX { get; set; }
    public int LeftTopY { get; set; }

    // upright template-local bbox (kept for diagnostics; overlay uses the tight rect below)
    public int X { get; set; }
    public int Y { get; set; }

    // tight rotated defect box: side lengths + orientation in image space
    public double W { get; set; }
    public double H { get; set; }
    public double RectAngle { get; set; }

    // defect center in image space (precomputed, convention-independent)
    public double ImgCx { get; set; }
    public double ImgCy { get; set; }

    // Per-pixel defect mask in UPRIGHT template-local coordinates (length Pw*Ph, 255 = defect).
    // Carried so the UI can paint the actual defective pixels red instead of drawing a box.
    // Pw/Ph equal the template size (Tw/Th) for the instance this defect belongs to.
    public byte[]? Pixels { get; set; }
    public int Pw { get; set; }
    public int Ph { get; set; }

    // rect top-left (pre-rotation) relative to the item canvas, so its center sits on the defect
    public double BoxLeft => ImgCx - LeftTopX - W / 2.0;
    public double BoxTop => ImgCy - LeftTopY - H / 2.0;

    // readable (un-rotated) text anchor relative to the item canvas
    public double TextLeft => ImgCx - LeftTopX;
    public double TextTop => ImgCy - LeftTopY;

    public string Type { get; set; } = "";
    public double Score { get; set; }   // mean absolute diff over the defect region

    public override string ToString()
        => $"[{Type}] @({ImgCx:F1},{ImgCy:F1}) box=({W:F0}x{H:F0}@{RectAngle:F0}deg) sev={Score:F1}";
}
