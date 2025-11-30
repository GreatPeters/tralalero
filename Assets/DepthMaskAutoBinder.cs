using UnityEngine;

[ExecuteAlways]
public class DepthMaskAutoBinder : MonoBehaviour
{
    public Renderer targetRenderer;     // Quad의 MeshRenderer
    public float feather = 0.15f;
    public float cutoff  = 0.5f;

    static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    static readonly int HoleSizeID   = Shader.PropertyToID("_HoleSize");
    static readonly int FeatherID    = Shader.PropertyToID("_Feather");
    static readonly int CutoffID     = Shader.PropertyToID("_Cutoff");

    void LateUpdate()
    {
        if (!targetRenderer) return;
        var m = targetRenderer.sharedMaterial; if (!m) return;

        // Quad(1x1) 기준: halfSize = scale * 0.5
        Vector3 s = transform.lossyScale;
        Vector2 size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z)); // 전체 크기
        Vector3 p = transform.position;

        m.SetVector(HoleCenterID, new Vector4(p.x, p.y, p.z, 0));
        m.SetVector(HoleSizeID,   new Vector4(size.x, 0, size.y, 0));
        m.SetFloat(FeatherID, feather);
        m.SetFloat(CutoffID,  cutoff);
    }
}
