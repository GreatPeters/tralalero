var opening = OpeningStoryUI.Instance;
return new[] { opening.titleText, opening.captionText, opening.chapterText, opening.nextText }.Select(text => new {
    name=text.name, text=text.text, color=text.color.ToString(), font=text.font.name, size=text.fontSize,
    renderer=text.canvasRenderer.GetColor().ToString(), alpha=text.canvasRenderer.GetInheritedAlpha(), style=text.fontStyle.ToString(),
    weight=text.fontWeight.ToString(), material=text.fontSharedMaterial.name,
    shaderFloats=new[]{"_FaceDilate","_WeightNormal","_WeightBold","_FaceSoftness","_OutlineWidth","_GradientScale"}.ToDictionary(k=>k,k=>text.fontSharedMaterial.HasProperty(k)?text.fontSharedMaterial.GetFloat(k):0)
}).ToArray();
