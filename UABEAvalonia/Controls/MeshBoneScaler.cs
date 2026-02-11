using AssetsTools.NET;
using AvaloniaEdit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public static ScaleRecord FromJson(JToken json, string name, int index)
            {
                var xToken = json["x"] ?? json["X"];
                var yToken = json["y"] ?? json["Y"];
                var zToken = json["z"] ?? json["Z"];

                return new ScaleRecord
                {
                    X = xToken?.Value<float>() ?? 1,
                    Y = yToken?.Value<float>() ?? 1,
                    Z = zToken?.Value<float>() ?? 1,
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

        public bool Process(AssetInfoDataGridItem item, JToken scaleJson)
        {
            if (item.TypeID != 43)
            {
                return false;
            }

            var meshName = GetItemName(item);
            if (meshName == null || string.IsNullOrWhiteSpace(meshName))
            {
                return false;
            }

            var skinnedMesh = gridItems.FirstOrDefault(x => x.TypeID == 137 && x.Name.Contains(meshName));
            if (skinnedMesh == null)
            {
                return false;
            }

            var records = GetBoneScaleRecords(skinnedMesh, scaleJson);

            var modifiedToken = PrepareModifiedToken(item, records);
            if (modifiedToken == null)
            {
                return false;
            }

            AssetImportExport importer = new AssetImportExport();
            AssetTypeTemplateField tempField = workspace.GetTemplateField(item.assetContainer);

            var bytes = importer.ImportJsonAsset(tempField, modifiedToken, out var exceptionMessage);
            if (bytes == null)
            {
                return false;
            }

            AssetsReplacer replacer = AssetImportExport.CreateAssetReplacer(item.assetContainer, bytes);
            workspace.AddReplacer(item.assetContainer.FileInstance, replacer, new MemoryStream(bytes));

            return true;
        }

        private JToken? PrepareModifiedToken(AssetInfoDataGridItem item, Dictionary<int, ScaleRecord> records)
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

            var bindPoseToken = token["m_BindPose"];
            if (bindPoseToken == null)
            {
                return null;
            }

            var arrayBindPoseToken = bindPoseToken["Array"];
            if (arrayBindPoseToken == null || arrayBindPoseToken.Type != JTokenType.Array)
            {
                return null;
            }

            var jarray = arrayBindPoseToken as JArray;
            if (jarray == null)
            {
                return null;
            }

            var isModified = false;

            for (int i = 0; i < jarray.Count; i++)
            {
                if (!records.TryGetValue(i, out var record))
                {
                    continue;
                }

                var boneToken = jarray[i];
                if (boneToken == null)
                {
                    continue;
                }

                if (!ModifyMatrixRow(boneToken, 0, record.Z))
                {
                    return null;
                }

                if (!ModifyMatrixRow(boneToken, 1, record.X))
                {
                    return null;
                }

                if (!ModifyMatrixRow(boneToken, 2, record.Y))
                {
                    return null;
                }

                isModified = true;
            }

            return isModified ? token : null;
        }

        private bool ModifyMatrixRow(JToken matrix, int row, float scale)
        {
            if (scale == 1)
            {
                return true;
            }

            try
            {
                var zero = matrix[$"e{row}0"]?.Parent as JProperty;
                var one = matrix[$"e{row}1"]?.Parent as JProperty;
                var two = matrix[$"e{row}2"]?.Parent as JProperty;
                var three = matrix[$"e{row}3"]?.Parent as JProperty;

                (zero?.Value as JValue)!.Value = zero.Value.Value<float>() * scale;
                (one?.Value as JValue)!.Value = one.Value.Value<float>() * scale;
                (two?.Value as JValue)!.Value = two.Value.Value<float>() * scale;
                (three?.Value as JValue)!.Value = three.Value.Value<float>() * scale;
            }
            catch
            {
                return false;
            }

            return true;
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
