using AssetsTools.NET;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace UABEAvalonia
{
    public class TransformBoneRetrieval
    {
        public string? LoadNew(AssetWorkspace workspace, AssetInfoDataGridItem gridItem, List<AssetInfoDataGridItem> gridItems)
        {
            if (workspace == null)
            {
                return null;
            }

            AssetTypeValueField? baseField = workspace.GetBaseField(gridItem.assetContainer);

            if (baseField == null)
            {
                return null;
            }

            var dumper = new AssetImportExport();
            var token = dumper.DumpJsonAsset(null, baseField);
            var gameObjectToken = token["m_GameObject"];
            if (gameObjectToken == null)
            {
                return null;
            }

            var mPathIDToken = gameObjectToken["m_PathID"];
            if (mPathIDToken == null)
            {
                return null;
            }

            var mPathID = mPathIDToken.Value<long>();
            var boneAssetItem = gridItems.FirstOrDefault(x => x.PathID == mPathID);
            if (boneAssetItem == null)
            {
                return null;
            }

            var boneAssetBaseField = workspace.GetBaseField(boneAssetItem.assetContainer);
            if (boneAssetBaseField == null)
            {
                return null;
            }

            var boneAssetToken = dumper.DumpJsonAsset(null, boneAssetBaseField);
            if (boneAssetToken == null)
            {
                return null;
            }

            var boneNameToken = boneAssetToken["m_Name"];
            if (boneNameToken == null)
            {
                return null;
            }

            var boneName = boneNameToken.Value<string>();
            return boneName;
        }
    }
}
