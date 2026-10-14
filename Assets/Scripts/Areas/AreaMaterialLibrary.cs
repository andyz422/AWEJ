using UnityEngine;

// Holds references to ground/wall materials so areas can use them at runtime.
// The asset lives at Resources/AreaMaterialLibrary; drag more materials into it to use them by name.
[CreateAssetMenu(fileName = "AreaMaterialLibrary", menuName = "AWEJ/Area Material Library")]
public class AreaMaterialLibrary : ScriptableObject
{
    public Material[] materials;

    private static AreaMaterialLibrary instance;

    public static Material Find(string materialName)
    {
        if (instance == null)
        {
            instance = Resources.Load<AreaMaterialLibrary>("AreaMaterialLibrary");
            if (instance == null)
            {
                return null;
            }
        }

        foreach (Material material in instance.materials)
        {
            if (material != null && material.name == materialName)
            {
                return material;
            }
        }

        Debug.LogWarning("AreaMaterialLibrary: no material named " + materialName);
        return null;
    }
}
