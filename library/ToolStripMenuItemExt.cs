using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Scoutmans.SwissArmyKnife
{

    public class ToolStripMenuItemExt : ToolStripMenuItem
    {
        public string Key { get; set; }
        public string ParentKey { get; set; }
        //public string ParentKey { get; set; }
        private List<MenuCache> Cache { get; set; }
        public ToolStripMenuItemExt(string title, List<MenuCache> mnuCache) : base(title)
        {
            Cache = mnuCache;
        }
        public void AddToCache()
        {
            string baseimage64 = null;
            if (this.Image!= null)
            {
                baseimage64 = DefaultAppIcons.ImageToBase64(this.Image, ImageFormat.Bmp);
            }
            string parentKey = this.ParentKey;
            if (this.Key!= null)
            {
                string[] tb = this.Key.Split('\\');
                if (tb.Length>1)
                {
                    string[] tbtmp = tb.Take(tb.Length - 1).ToArray();
                    parentKey =  String.Join("\\", tbtmp);
                }
                else
                {
                    parentKey = this.Key;
                }

            }
            if (parentKey == this.Key)
            {
                parentKey = null;
            }

            Cache.Add(new MenuCache
            {
                Text = this.Text,
                Tag= this.Tag as ShortcutItem,
                ToolTipText=this.ToolTipText,
                Key = this.Key,
                Image= baseimage64,
                ParentKey= parentKey
            });
        }
    }

}
