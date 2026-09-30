using System;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;

public sealed class NoryangjinRewardChoiceEvent : NoryangjinRevampEvent
{
    public Sprite shieldIcon, healIcon;
    public float padOffset = 1.8f, padWidth = 2.4f, healShare = .2f;
    private Transform[] pads = Array.Empty<Transform>();
    private bool claimed;

    private void Start()
    {
        var font = Resources.Load<TMP_FontAsset>("UI/GmarketHarbor SDF");
        pads = new[] { Pad(-padOffset, shieldIcon, "1회 막기 쉴드", new Color(.3f, .6f, 1f), font), Pad(padOffset, healIcon, "체력 회복 +20%", new Color(.35f, .95f, .45f), font) };
        foreach(var pad in pads)pad.gameObject.SetActive(Triggered&&!claimed);
    }

    private Transform Pad(float lateral, Sprite icon, string caption, Color color, TMP_FontAsset font)
    {
        var anchor = new GameObject("RewardPad " + caption).transform;
        anchor.SetParent(transform, false);
        anchor.localPosition = new Vector3(lateral, .02f, 0);
        BonusPadVisual.Ensure(anchor)?.Configure(null, color, BonusPadVisual.Mode.Normal, padWidth);
        var holo = new GameObject("Hologram").transform;
        holo.SetParent(anchor, false);
        holo.localPosition = Vector3.up * 1.9f;
        holo.gameObject.AddComponent<NoryangjinFaceCamera>();
        if (icon != null)
        {
            var sr = new GameObject("Icon").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(holo, false); sr.sprite = icon;
            float size = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            sr.transform.localScale = Vector3.one * (1.3f / Mathf.Max(.01f, size));
        }
        var text = new GameObject("Caption").AddComponent<TextMeshPro>();
        text.transform.SetParent(holo, false);
        text.transform.localPosition = new Vector3(0, -1.05f, 0);
        text.font = font; text.text = "낙찰!\n" + caption; text.fontSize = 4.2f; text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
        text.outlineWidth = .3f; text.outlineColor = new Color32(15, 20, 45, 255);
        text.rectTransform.sizeDelta = new Vector2(4, 2);
        return anchor;
    }

    public override void ResetForRun() { base.ResetForRun(); claimed = false; foreach (var p in pads) if (p) p.gameObject.SetActive(false); }
    protected override void OnTriggered() { foreach(var p in pads)if(p)p.gameObject.SetActive(true); }

    protected override void OnTick(float dt)
    {
        if (claimed || Ahead(Player.transform.position) > 0) return;
        claimed = true;
        bool left = Lateral(Player.transform.position) < 0;
        if (left) Director.GrantShield(); else Director.GrantHeal(healShare);
        foreach (var p in pads) if (p) p.gameObject.SetActive(false);
    }
}
