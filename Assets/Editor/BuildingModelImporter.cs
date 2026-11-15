using UnityEditor;

// Import settings for the Blender models in Assets/Resources/Buildings, /Shopkeepers and /Players,
// plus the character portraits in /Players/Portraits (UI sprites).
// Models are exported with Apply Transform and metre scale.
public class BuildingModelImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Players/Portraits/"))
        {
            return;
        }
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 512;
    }

    void OnPreprocessModel()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.Contains("/Resources/Buildings/") && !path.Contains("/Resources/Shopkeepers/") && !path.Contains("/Resources/Players/"))
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.bakeAxisConversion = true;

        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.importBlendShapes = false;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;

        // Collision comes from AreaBuilder's primitives, not the model.
        importer.addCollider = false;
        importer.isReadable = false;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.importNormals = ModelImporterNormals.Import;

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
    }
}
