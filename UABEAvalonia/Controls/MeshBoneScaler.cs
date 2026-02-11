using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UABEAvalonia.Controls
{
    public class MeshBoneScaler
    {
        private class ScaleRecord
        {
            public string? BoneName { get; set; }
            public int BoneIndex { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }

            public static ScaleRecord FromJson(JToken json, string name, int index)
            {
                var xToken = json["x"] ?? json["X"];
                var yToken = json["y"] ?? json["Y"];
                var zToken = json["z"] ?? json["Z"];

                return new ScaleRecord
                {
                    X = xToken?.Value<double>() ?? 1,
                    Y = yToken?.Value<double>() ?? 1,
                    Z = zToken?.Value<double>() ?? 1,
                    BoneIndex = index,
                    BoneName = name,
                };
            }
        }

        private AssetWorkspace workspace;
        private List<AssetInfoDataGridItem> gridItems;

        public MeshBoneScaler(AssetWorkspace workspace, List<AssetInfoDataGridItem> gridItems)
        {
            this.workspace = workspace;
            this.gridItems = gridItems;
        }

        public void Process(AssetInfoDataGridItem item, JToken scaleJson)
        {
            if (item.TypeID != 43)
            {
                return;
            }

            var meshName = GetItemName(item);
            if (meshName == null || string.IsNullOrWhiteSpace(meshName))
            {
                return;
            }

            var skinnedMesh = gridItems.FirstOrDefault(x => x.TypeID == 137 && x.Name.Contains(meshName));
            if (skinnedMesh == null)
            {
                return;
            }

            var records = GetBoneScaleRecords(skinnedMesh, scaleJson);

            //TODO: prepare new jtoken

            //TODO: use importer

            //TODO: replace
        }

        private Dictionary<int, ScaleRecord> GetBoneScaleRecords(AssetInfoDataGridItem skinnedMeshItem, JToken scaleJson)
        {
            var res = new Dictionary<int, ScaleRecord>();

            var skinnedMeshBaseItem = workspace.GetBaseField(skinnedMeshItem.assetContainer);
            if (skinnedMeshBaseItem == null)
            {
                return res;
            }

            var dumper = new AssetImportExport();
            var token = dumper.DumpJsonAsset(null, skinnedMeshBaseItem);
            if (token == null)
            {
                return res;
            }

            var boneArrayToken = token["m_Bones"];
            if (boneArrayToken == null)
            {
                return res;
            }

            var arrayToken = boneArrayToken["Array"];
            if (arrayToken == null || arrayToken.Type != JTokenType.Array)
            {
                return res;
            }

            var jarray = arrayToken as JArray;
            if (jarray == null)
            {
                return res;
            }

            for (int i = 0; i < jarray.Count; i++)
            {
                var transformToken = jarray[i];
                if (transformToken == null)
                {
                    continue;
                }

                var mPathIDToken = transformToken["m_PathID"];
                if (mPathIDToken == null)
                {
                    continue;
                }

                var mPathID = mPathIDToken.Value<long>();
                var transformItem = gridItems.FirstOrDefault(x => x.PathID == mPathID);
                if (transformItem == null)
                {
                    continue;
                }

                var boneName = GetBoneNameFromTransform(transformItem);
                if (string.IsNullOrWhiteSpace(boneName))
                {
                    continue;
                }

                var boneScaleToken = scaleJson[boneName];
                if (boneScaleToken == null)
                {
                    continue;
                }

                var boneScaleRecord = ScaleRecord.FromJson(boneScaleToken, boneName, i);
                res.Add(i, boneScaleRecord);
            }

            return res;
        }

        private string? GetBoneNameFromTransform(AssetInfoDataGridItem item)
        {
            var baseField = workspace.GetBaseField(item.assetContainer);
            if (baseField == null)
            {
                return null;
            }

            var dumper = new AssetImportExport();
            var token = dumper.DumpJsonAsset(null, baseField);
            if (token == null)
            {
                return null;
            }

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

            return GetItemName(boneAssetItem);
        }

        private string? GetItemName(AssetInfoDataGridItem item)
        {
            var baseField = workspace.GetBaseField(item.assetContainer);
            if (baseField == null)
            {
                return null;
            }

            var dumper = new AssetImportExport();
            var token = dumper.DumpJsonAsset(null, baseField);
            if (token == null)
            {
                return null;
            }

            var nameToken = token["m_Name"];
            if (nameToken == null)
            {
                return null;
            }

            return nameToken.Value<string>();
        }
    }
}
