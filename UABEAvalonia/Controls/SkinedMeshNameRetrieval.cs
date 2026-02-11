using AssetsTools.NET;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace UABEAvalonia
{
    public class SkinedMeshNameRetrieval
    {
        public string? LoadName(AssetWorkspace workspace, AssetInfoDataGridItem gridItem, List<AssetInfoDataGridItem> gridItems)
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
            var meshAssetItem = gridItems.FirstOrDefault(x => x.PathID == mPathID);
            if (meshAssetItem == null)
            {
                return null;
            }

            var meshAssetBaseField = workspace.GetBaseField(meshAssetItem.assetContainer);
            if (meshAssetBaseField == null)
            {
                return null;
            }

            var meshAssetToken = dumper.DumpJsonAsset(null, meshAssetBaseField);
            if (meshAssetToken == null)
            {
                return null;
            }

            var meshNameToken = meshAssetToken["m_Name"];
            if (meshNameToken == null)
            {
                return null;
            }

            var boneName = meshNameToken.Value<string>();
            return boneName;
        }
    }
}
