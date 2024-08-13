using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Microsoft.CodeAnalysis;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBundlePatcher
{
    class PathInfo
    {
        public long newPathId;

        public int fileId;

        public PathInfo(long newPathId, int fileId)
        {
            this.newPathId = newPathId;
            this.fileId = fileId;
        }
    }

    static class AssetUtils
    {
        
        public static long pathIdIndexer = 999999;
        static long PathIdIndexer
        {
            get
            {
                return pathIdIndexer++;
            }
            set
            {
                pathIdIndexer = value;
            }
        }
        public static void CopySelfAndChildren(this KeyValues origField, AssetBundleManager newFile, Dictionary<long, long> file0Paths = null, Dictionary<long, long> file1Paths = null)
        {
            if (file0Paths == null)
            {
                file0Paths = new Dictionary<long, long>();
            }
            if (file1Paths == null)
            {
                file1Paths = new Dictionary<long, long>();
            }

            CopyPntrProperties(origField.field, newFile, origField.manager, file0Paths, file1Paths, 1);

            Console.WriteLine("Stop");
        }

        public static void CopyComponentsTo(this KeyValues origField, AssetBundleManager copyTo, Dictionary<long, long> file0Paths, Dictionary<long, long> file1Paths)
        {
            var comps = origField.Components();
            int index = 0;
            foreach(var comp in comps)
            {
                
                var classInt = comp.GetFieldType();
                AssetFileInfo info = null;
                AssetTypeValueField newBaseField = null;
                if (classInt == AssetClassID.MonoBehaviour)
                {
                    var scripts = AssetHelper.GetAssetsFileScriptInfos(copyTo.assetManager, copyTo.assetFiles[1]);
                    string className = comp.GetClassFromDict();
                    var scriptIndex = scripts.Values.ToList().FindIndex(s => s.ClassName == className);
                    info = AssetFileInfo.Create(copyTo.assetFiles[1].file, PathIdIndexer, (int)classInt, (ushort)scriptIndex);

                    newBaseField = copyTo.assetManager.CreateValueBaseField(copyTo.assetFiles[1], (int)classInt, (ushort)scriptIndex);
                    comp.field["m_Script.m_PathID"].AsLong = copyTo.GetClassesScriptId()[className];
                }
                else
                {
                    info = AssetFileInfo.Create(copyTo.assetFiles[1].file, PathIdIndexer, (int)classInt, null, false);
                    newBaseField = copyTo.assetManager.CreateValueBaseField(copyTo.assetFiles[1], (int)classInt);
                }
                //comp.field["m_GameObject.m_PathID"].AsLong = origField.fileInfo.PathId;

                CopyPntrProperties(comp.field, copyTo, comp.manager, file0Paths, file1Paths, 1);

                newBaseField.Read(comp.field.Value, comp.field.TemplateField, comp.field.Children);

                info.SetNewData(newBaseField);

                copyTo.assetFiles[1].file.Metadata.AddAssetInfo(info);
                origField.field["m_Component.Array"].Children[index]["component.m_PathID"].AsLong = info.PathId;

                index++;
            }
        
        }

        public static void CopyPntrProperties(this AssetTypeValueField origField, AssetBundleManager copyTo, AssetBundleManager copyFrom, Dictionary<long, long> file0Paths, Dictionary<long, long> file1Paths, int currentFileId)
        {
            if (origField == null || origField.Children == null)
            {
                return;
            }

            foreach(var field in origField.Children) {
               
                    if (field.TypeName.StartsWith("PPtr<"))
                    {
                        var pathID = field["m_PathID"].AsLong;
                        var fileID = field["m_FileID"].AsInt;
                        if (fileID >= 2)
                        {
                            continue;
                        }

                        if (pathID == 0)
                        {
                            continue;
                        }

                        if (field.FieldName == "m_Script")
                        {
                            string className = copyFrom.GetScriptClasses()[pathID];

                            field["m_PathID"].AsLong = copyTo.GetClassesScriptId()[className];

                            continue;
                        }

                        Dictionary<long, long> filePaths = null;
                        int fileIndexToSaveTo = 0;
                        if (currentFileId == 0 && fileID == 0)
                        {
                            fileIndexToSaveTo = 0;
                            filePaths = file0Paths;
                        }
                        else if (currentFileId == 0 && fileID == 1)
                        {
                            fileIndexToSaveTo = 1;
                            filePaths = file1Paths;
                        }
                        else if (currentFileId == 1 && fileID == 0)
                        {
                            fileIndexToSaveTo = 1;
                            filePaths = file1Paths;
                        }
                        else if (currentFileId == 1 && fileID == 1)
                        {
                            fileIndexToSaveTo = 0;
                            filePaths = file0Paths;
                        }

                        if (filePaths.ContainsKey(pathID))
                        {
                            field["m_PathID"].AsLong = filePaths[pathID];
                            continue;
                        }
                        var newPathId = PathIdIndexer;
                        filePaths.Add(field["m_PathID"].AsLong, newPathId);

                        //var extAsset = copyFrom.assetManager.GetExtAsset(copyFrom.assetFiles[fileIndexToSaveTo], field);

                        var extInfo = copyFrom.assetFiles[fileIndexToSaveTo].file.GetAssetInfo(pathID);
                        var extBaseField = copyFrom.assetManager.GetBaseField(copyFrom.assetFiles[fileIndexToSaveTo], extInfo);
                        var extAsset = new AssetExternal() { baseField = extBaseField, file = copyFrom.assetFiles[fileIndexToSaveTo], info = extInfo };

                        var classId = (AssetClassID)extAsset.info.TypeId;

                        if (classId == AssetClassID.MonoBehaviour)
                        {
                            string className = copyFrom.GetScriptClasses()[extAsset.baseField["m_Script.m_PathID"].AsLong];

                            var scripts = AssetHelper.GetAssetsFileScriptInfos(copyTo.assetManager, copyTo.assetFiles[fileID == 0 ? 1 : 0]);
                            var scriptIndex = scripts.Values.ToList().FindIndex(s => s.ClassName == className);
                            if (scriptIndex == -1)
                            {
                                AddMonoScript(copyTo, copyFrom, className, currentFileId);
                                scripts = AssetHelper.GetAssetsFileScriptInfos(copyTo.assetManager, copyTo.assetFiles[fileID == 0 ? 1 : 0]);
                                scriptIndex = scripts.Values.ToList().FindIndex(s => s.ClassName == className);
                            }
                            AssetFileInfo info = AssetFileInfo.Create(copyTo.assetFiles[fileIndexToSaveTo].file, newPathId, (int)classId, (ushort)scriptIndex, null, false);
                            AssetTypeValueField newBaseField = copyTo.assetManager.CreateValueBaseField(copyTo.assetFiles[fileIndexToSaveTo], (int)classId, (ushort)scriptIndex);


                            // MonoBehaviour doesn't exist yet, so we can't just read
                            // the m_Script field. instead, we look in ScriptTypes.

                            CopyPntrProperties(extAsset.baseField, copyTo, copyFrom, file0Paths, file1Paths, fileIndexToSaveTo);

                            //extAsset.baseField.WriteToByteArray();
                            //newBaseField.
                            //newBaseField.Read(extAsset.baseField.Value, extAsset.baseField.TemplateField, extAsset.baseField.Children);
                            extAsset.baseField.CopyRegProperties(newBaseField);

                            if (!newBaseField["m_StreamData"].IsDummy)
                            {
                                Console.WriteLine("");
                                newBaseField["m_StreamData.offset"].AsUInt = 0U;
                                newBaseField["m_StreamData.size"].AsUInt = 0U;
                                newBaseField["m_StreamData.path"].AsString = "";
                            }

                            info.SetNewData(newBaseField);
                            copyTo.assetFiles[fileIndexToSaveTo].file.Metadata.AddAssetInfo(info);
                            field["m_PathID"].AsLong = newPathId;
                        }
                        else
                        {
                            AssetFileInfo info = AssetFileInfo.Create(copyTo.assetFiles[fileIndexToSaveTo].file, newPathId, (int)classId, null, false);
                            if (info == null)
                            {
                                AddBaseUnityComponent(copyTo, copyFrom, classId);
                                info = AssetFileInfo.Create(copyTo.assetFiles[fileIndexToSaveTo].file, newPathId, (int)classId, null, false);
                            }
                            AssetTypeValueField newBaseField = copyTo.assetManager.CreateValueBaseField(copyTo.assetFiles[fileIndexToSaveTo], (int)classId);

                            CopyPntrProperties(extAsset.baseField, copyTo, copyFrom, file0Paths, file1Paths, fileIndexToSaveTo);
                            if (classId == AssetClassID.Shader)
                            {
                                Console.Write("");
                            }
                            if (classId == AssetClassID.Material)
                            {
                                Console.Write("");
                            }


                            //newBaseField.Read(extAsset.baseField.Value, extAsset.baseField.TemplateField, extAsset.baseField.Children);

                            extAsset.baseField.CopyRegProperties(newBaseField);


                            if (!newBaseField["m_StreamData"].IsDummy)
                            {
                                Console.WriteLine("");
                                newBaseField["m_StreamData.offset"].AsUInt = 0U;
                                newBaseField["m_StreamData.size"].AsUInt = 0U;
                                newBaseField["m_StreamData.path"].AsString = "";
                            }

                            info.SetNewData(newBaseField);
                            KeyValues key = new KeyValues(info, copyTo, 0);
                            copyTo.assetFiles[fileIndexToSaveTo].file.Metadata.AddAssetInfo(info);
                            field["m_PathID"].AsLong = newPathId;

                            if (classId == AssetClassID.Shader)
                            {
                                Console.Write(key.field);
                            }
                            if (classId == AssetClassID.Material)
                            {
                                Console.Write("");
                            }
                        }



                    }
                    else
                    {
                        CopyPntrProperties(field, copyTo, copyFrom, file0Paths, file1Paths, currentFileId);
                    }
                
            }
        
        }

        public static void AddBaseUnityComponent(AssetBundleManager copyTo, AssetBundleManager copyFrom, AssetClassID classId)
        {
            var typeTree = copyFrom.assetFiles[1].file.Metadata.FindTypeTreeTypeByID((int)classId);
            var typeTree2 = copyFrom.assetFiles[0].file.Metadata.FindTypeTreeTypeByID((int)classId);

            if (typeTree != null)
            {
                copyTo.assetFiles[1].file.Metadata.TypeTreeTypes.Add(typeTree);
            }
            if (typeTree2 != null)
            {
                copyTo.assetFiles[0].file.Metadata.TypeTreeTypes.Add(typeTree2);
            }
            Console.WriteLine();
        }
        public static void AddMonoScript(AssetBundleManager copyTo, AssetBundleManager copyFrom, string className, int currentFileId)
        {
            var oldPath = copyFrom.GetClassesScriptId()[className];
            var newId = PathIdIndexer;
            copyTo.assetFiles[currentFileId].file.Metadata.ScriptTypes.Add(new AssetPPtr(currentFileId, newId));

            var oldInfo = copyFrom.assetFiles[0].file.GetAssetInfo(oldPath);
            var oldField = copyFrom.assetManager.GetBaseField(copyFrom.assetFiles[0], oldInfo);


            AssetFileInfo info = AssetFileInfo.Create(copyTo.assetFiles[0].file, newId, (int)AssetClassID.MonoScript, null, false);

            info.SetNewData(oldField.WriteToByteArray());

            copyTo.assetFiles[0].file.Metadata.AddAssetInfo(info);

            var scripts = AssetHelper.GetAssetsFileScriptInfos(copyFrom.assetManager, copyFrom.assetFiles[currentFileId]);
            var scriptIndex = scripts.Values.ToList().FindIndex(s => s.ClassName == className);

            var typeTrees = copyFrom.assetFiles[currentFileId].file.Metadata.TypeTreeTypes;

            //var typeTree = copyFrom.assetFiles[currentFileId].file.Metadata.FindTypeTreeTypeByScriptIndex((ushort)scriptIndex);


            var oldTypeTree = copyFrom.assetFiles[currentFileId].file.Metadata.FindTypeTreeTypeByScriptIndex((ushort)scriptIndex);
            var typeTree = new TypeTreeType() { IsRefType = oldTypeTree.IsRefType, IsStrippedType = oldTypeTree.IsStrippedType, Nodes = oldTypeTree.Nodes, ScriptIdHash = oldTypeTree.ScriptIdHash, ScriptTypeIndex = oldTypeTree.ScriptTypeIndex, StringBuffer = oldTypeTree.StringBuffer, StringBufferBytes = oldTypeTree.StringBufferBytes, TypeDependencies = oldTypeTree.TypeDependencies, TypeHash = oldTypeTree.TypeHash, TypeId = oldTypeTree.TypeId, TypeReference = oldTypeTree.TypeReference};

            typeTree.ScriptTypeIndex = (ushort)(copyTo.assetFiles[currentFileId].file.Metadata.ScriptTypes.Count - 1);
            copyTo.assetFiles[currentFileId].file.Metadata.TypeTreeTypes.Add(typeTree);
            copyTo.GetClassesScriptId(true);
        }
        public static void CopyRegProperties(this AssetTypeValueField origField, AssetTypeValueField newField)
        {


            //if (origField.Value != null && newField.Value != null && origField.Value.ValueType == newField.Value.ValueType)
            //{
            //    newField.Value = origField.Value;
            //}
            
            newField.Value = origField.Value;
            

            //return;

            

            if (newField.Children == null)
            {
                return;
            }

            if (newField.TypeName == "Array")
            {
                newField.Children.Clear();
                foreach (var child in origField.Children)
                {
                    //var newArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(origField);
                    var newArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(newField);
                    if (newArrayItem.TypeName != child.TypeName)
                    {
                        Console.WriteLine("");
                    }
                    if (newArrayItem.FieldName != child.FieldName)
                    {
                        Console.WriteLine("");

                    }
                    if (child.Value != newArrayItem.Value)
                    {
                        Console.WriteLine("");

                    }

                    if (newArrayItem.Value != null && newArrayItem.Value.ValueType == AssetValueType.String)
                    {
                        Console.WriteLine("");
                    }

                    child.CopyRegProperties(newArrayItem);

                    newField.Children.Add(newArrayItem);
                }
                return;
            }

            foreach (var child in origField.Children)
            {
                var regChild = newField.Get(child.FieldName);
                if (regChild.IsDummy)
                {
                    continue;
                }
                child.CopyRegProperties(regChild);
            }
        }

        public static List<KeyValues> GetAllBaseGameObjects(AssetBundleManager bundleManager)
        {
            var all = bundleManager.GetAllOfType(AssetsTools.NET.Extra.AssetClassID.Transform);
            all.AddRange(bundleManager.GetAllOfType(AssetsTools.NET.Extra.AssetClassID.RectTransform));
            List <KeyValues> parents = new List<KeyValues>();
            foreach (KeyValues val in all)
            {
                if (val.IsRoot(true))
                {
                    parents.Add(val.GetGameObjectFromTrans());
                }
            }
            return parents;
        }
        public static string ObjectName(this KeyValues field)
        {
            if (field != null && !field.field["m_Name"].IsDummy)
            {
                return field.field["m_Name"].AsString;
            }
            return "";
        }
        public static bool? IsGameObjectActive(this KeyValues field)
        {
            if (field == null)
            {
                return null;
            }
            var active = field.field["m_IsActive"];
            if (active.IsDummy)
            {
                return null;
            }
            return active.AsBool;
        }
        public static void SetGameObjectActive(this KeyValues field, bool isActive)
        {
            if (field == null)
            {
                return;
            }
            var active = field.field["m_IsActive"];
            if (active.IsDummy)
            {
                return;
            }
            active.AsBool = isActive;
        }
        
        public static void SetGameObjectName(this KeyValues field, string newName)
        {
            if (field == null)
            {
                return;
            }
            var active = field.field["m_Name"];
            if (active.IsDummy)
            {
                return;
            }
            active.AsString = newName;
        }
        public static bool? IsComponentEnabled(this KeyValues field)
        {
            if (field == null)
            {
                return null;
            }
            var active = field.field["m_Enabled"];
            if (active.IsDummy)
            {
                return null;
            }
            return active.AsBool;
        }
        
        public static void SetComponentEnabled(this KeyValues field, bool enabled)
        {
            if (field == null)
            {
                return;
            }
            var active = field.field["m_Enabled"];
            if (active.IsDummy)
            {
                return;
            }
            active.AsBool = enabled;
        }

        public static KeyValues? GetParentTrans(this KeyValues field, bool isTrans = false)
        {
            KeyValues trans;
            if (isTrans)
            {
                trans = field;
            }
            else
            {
                trans = field.Transform();
            }
            if (trans == null)
            {
                return null;
            }
            var father = trans.field["m_Father"];
            if (father.IsDummy)
            {
                return null;
            }
            if (father["m_PathID"].AsInt == 0)
            {
                return null;
            }
            
            return new KeyValues(field.manager.assetManager.GetExtAsset(field.manager.assetFiles[field.assetFilesIndex], father, true).info, field.manager, field.assetFilesIndex);
            //return new KeyValues(field.manager.assetFiles[field.assetFilesIndex].file.AssetInfos[father["m_PathID"].AsInt], field.manager, field.assetFilesIndex);
        }
        public static bool IsRoot(this KeyValues field, bool isTrans = false)
        {
            KeyValues trans;
            if (isTrans)
            {
                trans = field;
            }
            else
            {
                trans = field.Transform();
            }
            if (trans == null)
            {
                return false;
            }
            var father = trans.field["m_Father"];
            if (father.IsDummy)
            {
                return true;
            }
            if (father["m_PathID"].AsLong == 0L)
            {
                return true;
            }
            return false;
            
        }
        public static KeyValues? GetParentGameObject(this KeyValues field)
        {
            var trans = field.GetParentTrans();
            if (trans == null)
            {
                return null;
            }
            
            return new KeyValues(field.manager.assetManager.GetExtAsset(field.manager.assetFiles[field.assetFilesIndex], trans.field["m_GameObject"]).info, field.manager, field.assetFilesIndex);
        }

        public static KeyValues? GetGameObjectFromTrans(this KeyValues field) { 
            if (field == null)
            {
                return null;
            }
            var go = field.manager.assetManager.GetExtAsset(field.manager.assetFiles[field.assetFilesIndex], field.field["m_GameObject"]);
            return new KeyValues(go.info, field.manager, field.assetFilesIndex);
        }

        public static List<KeyValues>? GetChildrenGameObjects(this KeyValues field)
        {
            var list = new List<KeyValues>();
            if (field == null)
            {
                return list;
            }
            var trans = field.Transform();
            if (trans == null)
            {
                return list;
            }
            var array = trans.field["m_Children.Array"];
            if (array.IsDummy)
            {
                return list;
            }
            foreach (var child in array)
            {
                list.Add(new KeyValues(field.manager.assetManager.GetExtAsset(field.manager.assetFiles[field.assetFilesIndex], child).info, field.manager, field.assetFilesIndex).GetGameObjectFromTrans());
            }
            return list;
        }
        public static KeyValues Transform(this KeyValues val)
        {
            if (val == null)
            {
                return null;
            }
            if (val.field == null)
            {
                return null;
            }
            return val.GetComponentOfType(AssetClassID.Transform | AssetClassID.RectTransform);
        }

        public static KeyValues GetComponentOfType(this KeyValues val, AssetClassID id)
        {
            var keys = val.Components();
            foreach (var key in keys)
            {
                AssetClassID keyId = key.GetFieldType();
                if((keyId & id) == keyId)
                {
                    return key;
                }
            }
            return null;
        }
        public static KeyValues GetComponentOfTypes(this KeyValues val, AssetClassID[] id)
        {
            var keys = val.Components();
            foreach (var key in keys)
            {
                if(id.Contains(key.GetFieldType()))
                {
                    return key;
                }
            }
            return null;
        }
        public static List<KeyValues> GetComponentsOfType(this KeyValues val, AssetClassID id)
        {
            var keys = val.Components();
            List<KeyValues> components = new List<KeyValues>();
            foreach (var key in keys)
            {
                if(key.GetFieldType() == id)
                {
                    components.Add(key);
                }
            }
            return components;
        }

        public static AssetClassID GetFieldType(this KeyValues val)
        {
            return (AssetClassID)val.fileInfo.TypeId;
        }

        public static List<KeyValues> Components(this KeyValues val)
        {
            if (val == null || val.field == null)
            {
                Console.WriteLine("Was Null");
                return null;
            }
            var comps = val.field["m_Component.Array"];
            if (comps.IsDummy)
            {
                Console.WriteLine("Was Null");
                return null;
            }
            List<KeyValues> keys = new List<KeyValues>();
            foreach (var comp in comps)
            {
                var componentPointer = comp["component"];
                var componentExtInfo = val.manager.assetManager.GetExtAsset(val.manager.assetFiles[val.assetFilesIndex], componentPointer);
                //var componentExtInfo = val.manager.assetFiles[val.in(val.manager.assetFiles[val.assetFilesIndex], componentPointer);
                //val.manager.assetFiles[val.assetFilesIndex].file.
                //var componentExtInfo = val.manager.assetFiles[val.assetFilesIndex].file.AssetInfos[componentPointer["m_PathID"].AsInt];
                //var componentType = (AssetClassID)componentExtInfo.info.TypeId;
                KeyValues newKey = new KeyValues(componentExtInfo.info, val.manager, val.assetFilesIndex);
                //KeyValues newKey = new KeyValues(componentExtInfo, val.manager, val.assetFilesIndex);
                keys.Add(newKey);
            }
            return keys;
        }

        public static string GetClassFromDict(this KeyValues keys) 
        {
            if (keys == null || keys.field == null)
            {
                return "";
            }
            var classDict = keys.manager.GetScriptClasses();
            var pathId = keys.field["m_Script.m_PathID"];
            if (pathId.IsDummy)
            {
                return "";
            }
            return classDict[pathId.AsLong];
        }
        
    }

}
