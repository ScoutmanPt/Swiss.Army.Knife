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
    public class ShortcutItem 
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string ToolTip { get; set; }
        public eShortCutItemType Type { get; set; }
        public string Command { get; set; }
        public string IconPath { get; set; }

        public eShortCutType ShortCutType { get; set; }
        public eCmdLineType CmdLineType { get; set; }

        public string WildCardFolders { get; set; }
        public string WildCardFiles { get; set; }

        public bool Visible { get; set; }
        public int Order { get; set; }

    }



  
}
