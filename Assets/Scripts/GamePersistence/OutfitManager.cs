using UnityEngine;

public class OutfitManager : MonoBehaviour
{
    public SkinnedMeshRenderer BodyRenderer;
    public Material[] Outfits; // Assign in Inspector

    public void ChangeOutfit(int outfitIndex)
    {
        BodyRenderer.material = Outfits[outfitIndex];
        GameManager.Instance.PlayerData.CurrentOutfit = outfitIndex.ToString();
    }
}
