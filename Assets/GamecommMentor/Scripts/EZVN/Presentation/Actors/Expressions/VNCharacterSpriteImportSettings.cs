using System;
using UnityEngine;

namespace ProjectVN.Presentation.Actors
{
    [Serializable]
    public sealed class VNCharacterSpriteImportSettings
    {
        [Tooltip("Editor AssetDatabase folder path used by Preview Scan and Add Missing Only.")]
        [SerializeField]
        private string importFolderAssetPath;

        [Tooltip("Optional filename prefix removed before parsing, for example: MC_Linda_.")]
        [SerializeField]
        private string filenamePrefix;

        [Tooltip("Separator used by the sprite naming convention.")]
        [SerializeField]
        private string separator = "_";

        [Min(0)]
        [Tooltip("Token index treated as the expression after the prefix is removed. For MC_Linda_Mad_OpenEyes_I with prefix MC_Linda_, use 0.")]
        [SerializeField]
        private int expressionTokenIndex;

        [Tooltip("When enabled, sprites that do not start with Filename Prefix are ignored.")]
        [SerializeField]
        private bool requireFilenamePrefix = true;

        [Tooltip("Include every Sprite sub-asset found inside the import folder and its subfolders.")]
        [SerializeField]
        private bool includeSubfolders = true;

        public string ImportFolderAssetPath =>
            string.IsNullOrWhiteSpace(
                importFolderAssetPath)
                ? string.Empty
                : importFolderAssetPath
                    .Trim()
                    .Replace(
                        "\\",
                        "/");

        public string FilenamePrefix =>
            filenamePrefix ?? string.Empty;

        public string Separator =>
            !string.IsNullOrEmpty(separator)
                ? separator
                : "_";

        public int ExpressionTokenIndex =>
            Mathf.Max(
                0,
                expressionTokenIndex);

        public bool RequireFilenamePrefix =>
            requireFilenamePrefix;

        public bool IncludeSubfolders =>
            includeSubfolders;

        public void ConfigureImportFolder(
            string assetPath)
        {
            importFolderAssetPath =
                assetPath ?? string.Empty;

            Sanitize();
        }

        public void ConfigurePrefix(
            string prefix)
        {
            filenamePrefix =
                prefix ?? string.Empty;
        }

        public void Sanitize()
        {
            importFolderAssetPath =
                string.IsNullOrWhiteSpace(
                    importFolderAssetPath)
                    ? string.Empty
                    : importFolderAssetPath
                        .Trim()
                        .Replace(
                            "\\",
                            "/");

            filenamePrefix =
                filenamePrefix != null
                    ? filenamePrefix.Trim()
                    : string.Empty;

            separator =
                !string.IsNullOrEmpty(
                    separator)
                    ? separator
                    : "_";

            expressionTokenIndex =
                Mathf.Max(
                    0,
                    expressionTokenIndex);
        }
    }
}
