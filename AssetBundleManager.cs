using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Avalonia.Controls.Documents;
using Avalonia.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBundlePatcher
{
    public class KeyValues
    {
        public AssetFileInfo fileInfo;
        public AssetBundleManager manager;
        public AssetTypeValueField? field_back = null;
        public AssetTypeValueField? originalField = null;
        public AssetTypeValueField field
        {
            get {
                if (field_back == null)
                {
                    field_back = manager.assetManager.GetBaseField(manager.assetManager.Files[assetFilesIndex], fileInfo);
                }
                return field_back;
            }

            set
            {
                field_back = value;
            }
        }
        public int assetFilesIndex;

        public KeyValues(AssetFileInfo fileInfo, AssetBundleManager manager, int assetFilesIndex)
        {
            this.fileInfo = fileInfo;
            this.manager = manager;
            this.assetFilesIndex = assetFilesIndex;
        }
    }
    public class AssetBundleManager
    {
        public AssetsManager assetManager;
        public BundleFileInstance bundleFileInstance;
        public List<AssetsFileInstance> assetFiles;
        public Dictionary<long, string> classDict;
        public Dictionary<string, long> classScriptDict;
        public bool isWorld;
        public string filePath;
        public bool notValid;

        public AssetBundleManager(string filePath)
        {
            assetManager = new AssetsManager();
            classDict = null;
            classScriptDict = null;
            this.filePath = filePath;
            if (!File.Exists(filePath))
            {
                notValid = true;
                return;
            }
            notValid = false;
            bundleFileInstance = assetManager.LoadBundleFile(filePath, true);
            
            assetFiles = new List<AssetsFileInstance>();
            try
            {
                int i = 0;
                while (true)
                {
                    AssetsFileInstance assetFile = assetManager.LoadAssetsFileFromBundle(bundleFileInstance, i, true);
                    
                    if (assetFile != null)
                    {
                        assetFiles.Add(assetFile);
                    }
                    i++;
                }
            } catch { }
            //isWorld = assetFiles.Count >= 2;

            //var ggm = assetManager.LoadAssetsFile(Path.Combine(args[0], "globalgamemanagers"), false);
            //am.LoadClassDatabaseFromPackage(ggm.file.Metadata.UnityVersion);

            
        }

        public static String? GetNameOfGO(AssetTypeValueField field) 
        {
            if(field.FieldName != "DUMMY" && field["m_Name"].AsString != "DUMMY")
            {
                return field["m_Name"].AsString;
            }
            return null;
        }

        public List<KeyValues> GetAllOfType(AssetClassID classId, int assetIndex = 1)
        {
            List<KeyValues> fields = new List<KeyValues>();
            foreach (var goInfo in assetFiles[assetIndex].file.GetAssetsOfType(classId))
            {
                //var goBase = assetManager.GetBaseField(assetFiles[i], goInfo);
                //fields.Add(new KeyValues(goInfo, goBase, i));
                fields.Add(new KeyValues(goInfo, this, assetIndex));
            }
            //for (int i = 0; i < assetFiles.Count; i++)
            //{
            //    foreach (var goInfo in assetFiles[i].file.GetAssetsOfType(classId))
            //    {
            //        //var goBase = assetManager.GetBaseField(assetFiles[i], goInfo);
            //        //fields.Add(new KeyValues(goInfo, goBase, i));
            //        fields.Add(new KeyValues(goInfo, this, i));
            //    }
            //}
            return fields;
        }
        public void Print()
        {
            foreach(AssetsFileInstance fileInst in assetFiles)
            {

                foreach (var goInfo in fileInst.file.GetAssetsOfType(AssetClassID.GameObject))
                {
                    var goBase = assetManager.GetBaseField(fileInst, goInfo);
                    var name = goBase["m_Name"].AsString;
                    Console.WriteLine(name);

                    var components = goBase["m_Components.Array"];
                    foreach (var data in components)
                    {
                        var componentPointer = data["component"];
                        var componentExtInfo = assetManager.GetExtAsset(fileInst, componentPointer);
                        var componentType = (AssetClassID)componentExtInfo.info.TypeId;

                        Console.WriteLine($"  {componentType}");
                    }
                }

            }
        }

        public Dictionary<long, string> GetScriptClasses()
        {
            if (this.classDict != null)
            {
                return this.classDict;
            }
            classDict = new Dictionary<long, string>();
            var monoScripts = this.GetAllOfType(AssetClassID.MonoScript, 0);
            foreach (var monoScript in monoScripts)
            {
                var className = monoScript.field["m_ClassName"];
                if (!className.IsDummy)
                {
                    classDict.Add(monoScript.fileInfo.PathId, className.AsString);
                }
            }
            return classDict;
        }

        public Dictionary<string, long> GetClassesScriptId(bool regen = false)
        {
            if (this.classScriptDict != null && !regen)
            {
                return this.classScriptDict;
            }
            classScriptDict = new Dictionary<string, long>();
            var monoScripts = this.GetAllOfType(AssetClassID.MonoScript, 0);
            foreach (var monoScript in monoScripts)
            {
                var className = monoScript.field["m_ClassName"];
                if (!className.IsDummy)
                {
                    try
                    {
                        classScriptDict.Add(className.AsString, monoScript.fileInfo.PathId);
                    }
                    catch { }
                }
            }
            return classScriptDict;

        }
        public void Save()
        {
            for (int i = 0; i < assetFiles.Count; i++)
            {
                bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos[i].SetNewData(assetFiles[i].file);
            }
            using (AssetsFileWriter writer = new AssetsFileWriter(filePath + "1"))
            {
                if (File.Exists(filePath) && filePath != null)
                {
                    //System.IO.File.Move(filePath, filePath + "2");
                    bundleFileInstance.file.Write(writer);
                    assetManager.UnloadAll();
                    writer.Close();
                    System.IO.File.Delete(filePath);
                    System.IO.File.Move(filePath + "1", filePath);
                }
            }
        }

    }

   

}
