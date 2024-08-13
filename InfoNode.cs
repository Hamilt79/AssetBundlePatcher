using AssetsTools.NET;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;
using AssetBundlePatcher;
using AssetBundlePatcher.Views;

namespace AssetBundlePatcher.ViewModels
{
    public class InfoNode
    {
        public static List<InfoNode> EditedInfos = new List<InfoNode>();
        public ObservableCollection<InfoNode>? SubNodes { get; }


        public bool DebugButton { get; set; }

        public string Type { get; set; }

        public object value;
        public object Value { 
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
                if (this.isGameObject)
                {
                    try
                    {
                        //this.KeyValues.SetGameObjectActive(this.IsChecked);
                        this.KeyValues.SetGameObjectName(this.value.ToString());
                        AddToEditedInfos(this);
                    }
                    catch { }
                    return;
                }
                try
                {
                    if (this.KeyValues.field.Value.ValueType == AssetValueType.Bool)
                    {
                        this.KeyValues.field.Value.AsBool = bool.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.Float)
                    {
                        this.KeyValues.field.Value.AsFloat = float.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.Double)
                    {
                        this.KeyValues.field.Value.AsDouble = double.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.Int16)
                    {
                        this.KeyValues.field.Value.AsInt = int.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.Int32)
                    {
                        this.KeyValues.field.Value.AsInt = int.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.Int64)
                    {
                        this.KeyValues.field.Value.AsInt = int.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.Int8)
                    {
                        this.KeyValues.field.Value.AsInt = int.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.String)
                    {
                        this.KeyValues.field.Value.AsString = value.ToString();
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.UInt16)
                    {
                        this.KeyValues.field.Value.AsUInt = uint.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.UInt32)
                    {
                        this.KeyValues.field.Value.AsUInt = uint.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.UInt64)
                    {
                        this.KeyValues.field.Value.AsUInt = uint.Parse(value.ToString());
                    }
                    else if (this.KeyValues.field.Value.ValueType == AssetValueType.UInt8)
                    {
                        this.KeyValues.field.Value.AsUInt = uint.Parse(value.ToString());
                    }
                    AddToEditedInfos(this);
                }
                catch { }
                
            }
        }
        public bool ValueVisible { get; set; }
        public KeyValues? KeyValues { get; set; }
        public string? PropName { get; set; }
        public bool IsPropNameVisible { get; set; }

        public bool isGameObject = false;

        public bool isChecked;
        public bool IsChecked {
            get
            {
                return isChecked;
            }
            set
            {
                isChecked = value;
                if (this.isGameObject)
                {
                    try
                    {
                        this.KeyValues.SetGameObjectActive(this.IsChecked);
                        AddToEditedInfos(this);
                    }
                    catch { }
                    return;

                }
            }
        }
        public bool ShowCheckbox {  get; set; }

        public bool isExpanded = false;
        private bool hasBeenExpanded = false;


        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }

            set
            {
                isExpanded = value;
                if (isExpanded && !hasBeenExpanded)
                {
                    hasBeenExpanded = true;
                    foreach (var node in SubNodes)
                    {
                        node.AddSubNodes();
                    }
                }
            }
        }
        //public static string DecodeUdonBase64String(string val)
        //{
        //    byte[] bytes = Convert.FromBase64String(val);
        //    //return Encoding.Unicode.GetString(bytes);
        //    //Console.WriteLine(detectTextEncoding(bytes));
        //    string stringBase64 = BitConverter.ToString(bytes);
        //    return EncodingDecoding.FromHexString(stringBase64.Replace("-", ""));
        //    //return BitConverter.ToString(bytes);
        //}

        public static string DecodeUdonBase64String(string val)
        {
            byte[] bytes = Convert.FromBase64String(val);
            //return Encoding.Unicode.GetString(bytes);
            //Console.WriteLine(detectTextEncoding(bytes));
            string stringBase64 = BitConverter.ToString(bytes);
            return FromHexString(stringBase64.Replace("-", ""));
            //return BitConverter.ToString(bytes);
        }
        public static string FromHexString(string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return Encoding.UTF8.GetString(bytes); // returns: "Hello world" for "48656C6C6F20776F726C64"
        }
        public static string CleanString(string input)
        {
            
            return Regex.Replace(input, @"[^\u0009\u000A\u000D\u0020-\u007E]", "[REDACTED]");
            //return input;
        }

        public List<string> GetPropertiesFromPubString(string str)
        {
            List<String> retStrs = new List<String>();
            string[] strL = str.Split("SymbolName", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strL.Length; i++)
            {
                int ind = strL[i].IndexOf("'type");
                if (ind == -1)
                {
                    continue;
                }
                int udonIndex = strL[i].IndexOf("VRC.Udon.Common");
                if (udonIndex < ind && udonIndex != -1)
                {
                    continue;
                }
                var prop = strL[i].Substring(0, ind);
                var comInd = strL[i].IndexOf(", ");
                //Console.WriteLine(strL[i]);
                var type = strL[i].Substring(ind + 5, strL[i].IndexOf(", ") - ind - 5);
                if (type.Length > 0 && !Char.IsLetter(type[0]))
                {
                    type = type.Substring(1);
                }
                if (prop.Length > 0)
                {
                    retStrs.Add(prop);
                    Console.WriteLine(prop + " || " + type);
                }

            }
            return retStrs;
        }
        public bool DebugButtonClick()
        {
            if (this.PropName == "serializedPublicVariablesBytesString")
            {
                
                var bytes = (string)(this.value);
                //char[] chars = new char[bytes.Length];
                //for (int i = 0; (i < bytes.Length); i++)
                //{
                //    chars[i] = bytes[i];
                //}

                //var strByter = Convert.FromBase64CharArray(chars, 0, chars.Length);

                var decoded = Convert.FromBase64String(bytes);

                char[] charArr = new char[decoded.Length];
                for (int i = 0; i < charArr.Length; i++) {
                    charArr[i] = (char)decoded[i];
                }


                string strReg = new string(charArr);
                string strCleaned = CleanString(strReg);
                //string str = Encoding.UTF8.GetString(bytes.Children[0].AsByteArray);
                //string stringBase64 = BitConverter.ToString(strByter);
                //string val = FromHexString(stringBase64.Replace("-", ""));
                var clipboard = MainWindow.mainWindow.Clipboard;
                var props = GetPropertiesFromPubString(strCleaned);
                //clipboard.SetTextAsync(strCleaned);
                clipboard.SetTextAsync(strCleaned);
            } else if(this.PropName == "serializedProgramAsset")
            {
                var asset = ((MainWindowViewModel)MainWindow.mainWindow.DataContext).manager.assetFiles[0].file.GetAssetInfo(long.Parse((this.SubNodes[1].value.ToString())));
                var baseField = ((MainWindowViewModel)MainWindow.mainWindow.DataContext).manager.assetManager.GetBaseField(((MainWindowViewModel)MainWindow.mainWindow.DataContext).manager.assetFiles[0], asset);
                var bytes = baseField["serializedProgramCompressedBytes"];
                var nonBytes = baseField["serializedProgramBytesString"];
                if (bytes.IsDummy)
                {
                    var decoded = Convert.FromBase64String(nonBytes.Value.AsString);

                    char[] charArr = new char[decoded.Length];
                    for (int i = 0; i < charArr.Length; i++)
                    {
                        charArr[i] = (char)decoded[i];
                    }


                    string strReg = new string(charArr);
                    string strCleaned = CleanString(strReg);
                    //string str = Encoding.UTF8.GetString(bytes.Children[0].AsByteArray);
                    //string stringBase64 = BitConverter.ToString(strByter);
                    //string val = FromHexString(stringBase64.Replace("-", ""));
                    var clipboard = MainWindow.mainWindow.Clipboard;
                    clipboard.SetTextAsync(strCleaned);
                }
                else
                {
                    //byte[] str = GZip.Decompress(bytes.Children[0].AsByteArray);
                    //char[] chars = new char[str.Length];
                    //for (int i = 0; i < str.Length; i++)
                    //{
                    //    chars[i] = (char)str[i];
                    //}
                    ////var strByter = Convert.FromBase64CharArray(chars, 0, chars.Length);

                    //string strReg = new string(chars);
                    //string strCleaned = CleanString(strReg);
                    ////string str = Encoding.UTF8.GetString(bytes.Children[0].AsByteArray);
                    ////string stringBase64 = BitConverter.ToString(strByter);
                    ////string val = FromHexString(stringBase64.Replace("-", ""));
                    //var clipboard = MainWindow.mainWindow.Clipboard;
                    //clipboard.SetTextAsync(strCleaned);
                }
               
            }
            
            return false;
        }

        public static void AddToEditedInfos(InfoNode infoNode)
        {
            foreach(var node in EditedInfos)
            {
                if (node.KeyValues.originalField == infoNode.KeyValues.originalField)
                {
                    return;
                }
            }
            Console.WriteLine("Added:" + infoNode.KeyValues.originalField.FieldName + " to the EditedInfos");
            EditedInfos.Add(infoNode);
        }
        public InfoNode(string type, KeyValues? vals = null, object value = null, bool valueVisible = false, bool isPropNameVisible = false, string? propName = "")
        {
            Type = type;
            this.value = value;
            ValueVisible = valueVisible;
            KeyValues = vals;
            SubNodes = new ObservableCollection<InfoNode>();
            //EditedInfos = new List<InfoNode>();
            IsPropNameVisible = isPropNameVisible;
            PropName = propName;
        }

        public void AddSubNodes()
        {
            for(int i = 0; i < KeyValues.field.Children.Count; i++) {
                KeyValues newKey = new KeyValues(this.KeyValues.fileInfo, KeyValues.manager, KeyValues.assetFilesIndex);
                newKey.field_back = KeyValues.field.Children[i];
                InfoNode child = new InfoNode(KeyValues.field.Children[i].TypeName, newKey, isPropNameVisible: true, propName: KeyValues.field.Children[i].FieldName);
                newKey.originalField = KeyValues.originalField;
                if (child.KeyValues.field.Value != null)
                {
                    child.ValueVisible = true;
                    child.value = newKey.field.Value.AsString;
                }
                if (child.PropName == "serializedProgramAsset" || child.PropName == "serializedPublicVariablesBytesString")
                {
                    child.DebugButton = true;
                }
                this.SubNodes.Add(child);
            }
        }
    }
}