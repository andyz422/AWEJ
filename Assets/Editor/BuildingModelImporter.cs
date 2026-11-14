using UnityEditor;

// Import settings for the Blender building models in Assets/Resources/Buildings.
// They are exported with Apply Transform, metre scale and the door on local -Z.
public class BuildingModelImporter : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Buildings/"))
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
