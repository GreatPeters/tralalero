using UnityEngine;

[ExecuteAlways]
public class DepthMaskAutoBinder : MonoBehaviour
{
    public Renderer targetRenderer;
    public float feather = 0.15f;
    public float cutoff  = 0.5f;

    static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    static readonly int HoleSizeID   = Shader.PropertyToID("_HoleSize");
    static readonly int FeatherID    = Shader.PropertyToID("_Feather");
    static readonly int CutoffID     = Shader.PropertyToID("_Cutoff");

    Vector3 _lastPos;
    Vector3 _lastScale;
    float _lastFeather, _lastCutoff;

    void OnEnable() => Apply(true);

    void LateUpdate() => Apply(false);

    void Apply(bool force)
    {
        if (!targetRenderer) return;
        var m = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial;
        if (!m) return;

        // 값 변화 없으면 스킵 (에디터 프리즈 방지)
        if (!force &&
            transform.position == _lastPos &&
            transform.lossyScale == _lastScale &&
            Mathf.Approximately(feather, _lastFeather) &&
            Mathf.Approximately(cutoff, _lastCutoff))
            return;

        _lastPos = transform.position;
        _lastScale = transform.lossyScale;
        _lastFeather = feather;
        _lastCutoff = cutoff;

        Vector3 s = transform.lossyScale;
        Vector2 size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z));
        Vector3 p = transform.position;

        m.SetVector(HoleCenterID, new Vector4(p.x, p.y, p.z, 0));
        m.SetVector(HoleSizeID,   new Vector4(size.x, 0, size.y, 0));
        m.SetFloat(FeatherID, feather);
        m.SetFloat(CutoffID,  cutoff);
    }
}
