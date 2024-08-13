using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBundlePatcher
{
    internal class CustomTextWriter : TextWriter
    {
        private static TextBlock? textBlock;
        public CustomTextWriter(TextBlock block) {
            textBlock = block;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public override void Write(char value)
        {
            if (textBlock != null)
            {
                textBlock.Text += value;
            }
        }

        public override void Write(string? value)
        {
            if (textBlock != null)
            {
                textBlock.Text += value;
            }
        }

    }
}
