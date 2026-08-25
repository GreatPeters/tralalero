var images = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(
    UnityEngine.FindObjectsInactive.Exclude,
    UnityEngine.FindObjectsSortMode.None)
    .Where(image => image.enabled && image.gameObject.activeInHierarchy)
    .ToArray();

var texturedImages = images
    .Where(image => image.sprite != null && image.mainTexture != null)
    .ToArray();

return new
{
    activeUiImages = images.Length,
    activeUiTextures = texturedImages
        .Select(image => image.mainTexture.GetInstanceID())
        .Distinct()
        .Count(),
    packedUiImages = texturedImages.Count(image => image.sprite.packed),
    missingActiveSprites = images.Count(image => image.sprite == null),
    screenWidth = UnityEngine.Screen.width,
    screenHeight = UnityEngine.Screen.height
};
