using Microsoft.JScript;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Scoutmans.SwissArmyKnife
{
    [Serializable]
    public class MenuCache
    {
        public string Key { get; set; }
        public ShortcutItem Tag { get; set; }
        public string ToolTipText { get; set; }
        public string Text { get; set; }
        public string Image { get; set; }
        public string ParentKey { get; set; }
    }

}
