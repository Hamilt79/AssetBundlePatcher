using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using AssetBundlePatcher;
using AssetBundlePatcher.ViewModels;
using AssetBundlePatcher.Views;

namespace AssetBundlePatcher.ViewModels
{
    public class Node
    {
        public static long idIndex = 0;
        public ObservableCollection<Node>? SubNodes { get; }
        public List<InfoNode> infoNodes = new List<InfoNode>();
        public string Title { get; }
        public KeyValues keyValue;

        public string PathID { get; set; }

        public bool back_is_expanded = false;
        private bool hasBeenExpanded = false;
        public bool IsExpanded
        {
            get
            {
                return back_is_expanded;
            }

            set
            {
                back_is_expanded = value;
                if(back_is_expanded && !hasBeenExpanded)
                {
                    hasBeenExpanded = true;
                    foreach (var node in SubNodes)
                    {
                        node.AddChildNodes();
                    }
                }
                //Dispatcher.UIThread.Post(() => { Console.WriteLine(Title); });
            }
        }
        public string Name { get; }

        public Node(string title, KeyValues keyValue = null)
        {
            Title = title;
            SubNodes = new ObservableCollection<Node>();
            this.keyValue = keyValue;
            this.Name = "Node" + idIndex;
            idIndex++;
            if (keyValue != null && keyValue.fileInfo != null)
            {
                PathID = "PathID: " + keyValue.fileInfo.PathId.ToString();
            }
        }

        public Node(string title, ObservableCollection<Node> subNodes)
        {
            Title = title;
            SubNodes = subNodes;
            this.Name = "Node" + idIndex;
            idIndex++;
        }

        public void AddChildNodes()
        {
            if (this == null)
            {
                return;
            }
            foreach (var child in this.keyValue.GetChildrenGameObjects())
            {
                //Console.WriteLine(child.ObjectName());
                Node childNode = new Node(child.ObjectName(), child);
                this.SubNodes.Add(childNode);
                //AddChildNodes(childNode);
            }
        }
    }
}