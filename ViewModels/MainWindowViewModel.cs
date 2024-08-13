using System;
using System.Collections.ObjectModel;
using System.IO;
using AssetBundlePatcher.Views;

namespace AssetBundlePatcher.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<Node>? Nodes { get; }
        public ObservableCollection<InfoNode>? InfoNodes { get; }
        public AssetBundleManager? manager = null;
        public Node? SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                Console.WriteLine("Selected: " + selectedItem?.Title);
                if (selectedItem != null) {
                    InfoNodes?.Clear();
                    if (selectedItem.infoNodes.Count == 0)
                    {
                        //Console.WriteLine("Components Length: " + selectedItem.keyValue.Components().Count);
                        InfoNode objectNode = new InfoNode("Enabled", selectedItem.keyValue, isPropNameVisible: true, propName: "PathID: " + selectedItem.keyValue.fileInfo.PathId + " Name: ", valueVisible: true);

                        bool? isActive = objectNode.KeyValues.IsGameObjectActive();
                        if (isActive != null)
                        {
                            objectNode.ShowCheckbox = true;
                            objectNode.IsChecked = isActive == true ? true : false;
                            objectNode.KeyValues.originalField = selectedItem.keyValue.field;
                            selectedItem.infoNodes.Add(objectNode);
                            objectNode.isGameObject = true;
                            objectNode.Value = objectNode.KeyValues.ObjectName();
                        }
                        InfoNodes.Add(objectNode);
                        var comps = selectedItem.keyValue.Components();
                        for (int i = 0; i < comps.Count; i++)
                        {
                            var fieldType = comps[i].GetFieldType();
                            InfoNode compNode;
                            if (fieldType == AssetsTools.NET.Extra.AssetClassID.MonoBehaviour)
                            {
                                compNode = new InfoNode(fieldType + " : " + comps[i].GetClassFromDict(), comps[i]);
                            } else
                            {
                                compNode = new InfoNode(fieldType.ToString(), comps[i]);
                            }
                            compNode.IsPropNameVisible = true;
                            compNode.PropName = "PathID: " + compNode.KeyValues.fileInfo.PathId;
                            compNode.KeyValues.originalField = compNode.KeyValues.field;
                            InfoNodes.Add(compNode);
                            selectedItem.infoNodes.Add(compNode);
                            compNode.AddSubNodes();
                        }
                    } else
                    {
                        foreach(var info in selectedItem.infoNodes)
                        {
                            InfoNodes?.Add(info);
                        }
                    }

                }
            }
        }
        private Node? selectedItem;

        private string currentPath = "";

        public MainWindowViewModel() {
            Nodes = new ObservableCollection<Node>();
            //Nodes.Add(new Node("Test"));
            InfoNodes = new ObservableCollection<InfoNode>();
            //InfoNodes.Add(new InfoNode("Woah", null, "Hoi", true, true, "WQoaaaah Cool Name"));
            selectedItem = null;
        }

        public void SceneEditorReset()
        {
            Nodes?.Clear();
            InfoNodes?.Clear();
            manager?.assetManager.UnloadAll();
            manager = null;
            //classDict = null;
        }
        
        public void SceneEditorSave()
        {
            foreach(var thing in InfoNode.EditedInfos)
            {
                //var uhh = thing.KeyValues.manager.assetManager.GetBaseField(thing.KeyValues.manager.assetFiles[thing.KeyValues.assetFilesIndex], thing.KeyValues.fileInfo);
                thing.KeyValues.fileInfo.SetNewData(thing.KeyValues.originalField);
            }
            manager?.Save();
            SceneEditorReset();
        }

        public void SceneEditorStart(string? path = null)
        {
            SceneEditorReset();
            //string worldId = MainWindow.mainWindow.SceneEditorWorldID.Text;
            //string worldId = MainWindow.mainWindow.SceneEditorWorldID.Text;

            //string path = "";
            //string path = BundleLocator.GetPathById(worldId);
            //Console.WriteLine("Custom Id: " + worldId);
            if (path == null)
            {
                path = currentPath;
            }
            Console.WriteLine("Path: " + path);
            currentPath = path;

            if (path == "" || !File.Exists(path))
            {
                Console.WriteLine("Could not find asset bundle file");
                return;
            }
            manager = new AssetBundleManager(path);
            if (manager.notValid)
            {
                Console.WriteLine("Invalid World");
                return;
            }
            //classDict = manager.GetScriptClasses();
            var all = manager.GetAllOfType(AssetsTools.NET.Extra.AssetClassID.Transform);
            all.AddRange(manager.GetAllOfType(AssetsTools.NET.Extra.AssetClassID.RectTransform));

            foreach (KeyValues val in all)
            {
                //var parent = val.GetParentGameObject();
                //var parent = val.GetParentTrans(true);
                if (val.IsRoot(true))
                {
                    //Node node = new Node(val.GetGameObjectFromTrans().ObjectName(), val.GetGameObjectFromTrans());
                    Node node = new Node(val.GetGameObjectFromTrans().ObjectName(), val.GetGameObjectFromTrans());
                    //node.GetSideButton();
                    Nodes.Add(node);
                    node.AddChildNodes();
                    //AddChildNodes(node);
                }
            }
        }



    }

}
