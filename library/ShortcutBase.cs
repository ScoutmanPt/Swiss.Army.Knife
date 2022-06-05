using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scoutmans.SwissArmyKnife
{
    public enum eShortCutItemType
    {
        RootElement,
        ShortCut,
        CmdLine
        
    }
    public enum eShortCutType
    {
        FilesAndFolders,
        JustFolders,
        JustFiles,
        JustOneFile,
        NotApplicable
    }
    public enum eCmdLineType
    {
        Batch,
        Node,
        PowerShell5,
        PowerShellCore,
        NotApplicable
    }
    [Serializable]
    public abstract class ShortcutItemBase
    {
        public string Title { get; set; }
        public string ToolTip { get; set; }
        public eShortCutItemType Type { get; set; }
        public bool Visible { get; set; }
        public int Order { get; set; }
    }
    [Serializable]
    public abstract class ShortcutRootBase
    {
        public string Title { get; set; }
        public string ToolTip { get; set; }
        public bool Visible { get; set; }
        public int Order { get; set; }
    }
}
