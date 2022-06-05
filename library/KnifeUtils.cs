using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace Scoutmans.SwissArmyKnife
{


    public class KnifeUtils
    {
        public const string APP_TITLE= "Scoutman's Swiss Army Knife";
        public const string MENU_ITEM_SKIPONELVEL = "ROOT#";
        const string settings = @".\settings\";
        public const string settingsMenuConfiguration = settings + "\\mnu-config.json";
        public const string settingsMenuCache = settings + "\\mnu-filesys-cache.json";
        public const string eggxamplesCmdItems = @".\resources\samples\eggxamples.json";
        public const string pnpCreateCmdItems = @"\.\resources\samples\pnp.ps1";
        public bool WinTerminalIsInstalled { get; set; }
        public bool NodeIsInstalled { get; set; }
        public bool PsCoreIsInstalled { get; set; }
        public static bool StartupApp;
        public List<MenuCache> MenuCache { get; set; }


        private Form _frm = null;
        public KnifeUtils(Form frm)
        {
            _frm = frm;
            WinTerminalIsInstalled = TestIfAppExists("wt.exe");
            NodeIsInstalled = TestIfAppExists("node.exe");
            PsCoreIsInstalled = TestIfAppExists("pwsh.exe");
            MenuCache = new List<MenuCache>();

        }
        public static string ResolvePath(string basePath, string relativePath)
        {
            string resolvedPath = "";
            if (relativePath !=null && relativePath.Length>0)
            {
                resolvedPath= Path.Combine(basePath, relativePath);
                resolvedPath = Path.GetFullPath(resolvedPath);
            }
           

            return resolvedPath;
        }
        public ToolStripMenuItemExt BuildMenuItem(ShortcutItem item, ImageList imgList)
        {
            string tmpTitle = item.Title;
          
            ToolStripMenuItemExt menuItem = new ToolStripMenuItemExt(tmpTitle, this.MenuCache);
            menuItem.Key = item.Key;
            menuItem.ToolTipText = item.ToolTip;
            switch (item.Type)
            {
                case eShortCutItemType.CmdLine:
                    menuItem.Image =  imgList.Images[2];
                    break;
                case eShortCutItemType.ShortCut:
                    var icon = DefaultAppIcons.FolderLarge;
                    menuItem.Image = icon.ToBitmap();
                    if (item.Command!=null  && item.Command.Trim().Length ==0)
                    {
                        menuItem.Image =null;
                    }
                    if (item.ShortCutType == eShortCutType.JustOneFile)
                    {
                        menuItem.Image = Icon.ExtractAssociatedIcon(item.Command).ToBitmap();
                    }
                    break;
                case eShortCutItemType.RootElement:
                    break;
            }

            if (item.IconPath != null && item.IconPath.Length>0)
            {
                var img = Image.FromFile(item.IconPath);
                var icon = DefaultAppIcons.ConvertToIco(img, 32);
                menuItem.Image = icon.ToBitmap();

            }
            menuItem.ToolTipText =  String.Format("{0} [{1}]", item.ToolTip, item.Type);
            menuItem.Tag=item;
            menuItem.Click+=MenuSubItem_Click;
            menuItem.AddToCache();
            return menuItem;
        }
        public List<ToolStripMenuItemExt> GetFilesFolders(string key, string command, string wildCardFolders, string wildCardFiles, string tooltip, eShortCutItemType itemType, eShortCutType folderType)
        {
            ToolStripMenuItemExt menuSubItemFF = null;

            if (command== null || command.Trim().Length==0) { return new List<ToolStripMenuItemExt>(); }
            String msg = String.Empty;
            FileAttributes att = KnifeUtils.GetAttributes(command, ref msg);

            DirectoryInfo folder = null;
            List<string> ffiles = new List<string>();

            Matcher matcher = new Matcher();

            string[] listofFilesFolders;
            string[] listofFiles = new string[] { };
            string[] listofFolders = new string[] { };
            string[] folders = new string[] { };
            string[] files = new string[] { };


            List<string> ffolders = new List<string>();


            if (folderType ==eShortCutType.JustFiles || folderType ==eShortCutType.FilesAndFolders)
            {

                listofFiles= null;
                if (listofFiles == null)
                {
                    ffiles = new List<string>();
                    files = Directory.EnumerateFiles(command, "*", SearchOption.AllDirectories).ToArray();

                    matcher.AddInclude(wildCardFiles);
                    folder = new DirectoryInfo(command);
                    var resultAllFiles = matcher.Match(folder.FullName, files);
                    foreach (var obj in resultAllFiles.Files)
                    {
                        ffiles.Add(command  + "\\" + obj.Path.Replace("/", "\\"));
                    }

                    listofFiles = ffiles.ToArray();
                }

            }
            if (folderType ==eShortCutType.JustFolders || folderType ==eShortCutType.FilesAndFolders)
            {

                listofFolders= null;
                if (listofFolders == null)
                {
                    ffiles = new List<string>();
                    folders = Directory.EnumerateDirectories(command, "*", SearchOption.AllDirectories).ToArray();
                    matcher.AddInclude(wildCardFolders);
                    folder = new DirectoryInfo(command);
                    var resultJFolders = matcher.Match(folder.FullName, folders);
                    foreach (var obj in resultJFolders.Files)
                    {
                        ffiles.Add(command + "\\" + obj.Path.Replace("/", "\\"));
                    }
                    listofFolders = ffiles.ToArray();
                }
            }
            if (folderType ==eShortCutType.JustOneFile)
            {
                listofFiles= new string[] { command };
            }

            listofFilesFolders =  listofFolders.ToArray();

            List<ToolStripMenuItemExt> listOfMenuItem = new List<ToolStripMenuItemExt>();
            foreach (string myInnerfile in listofFilesFolders)
            {
                string myfile =  myInnerfile;
                menuSubItemFF = new ToolStripMenuItemExt(myfile.Split('\\').Last(), this.MenuCache);
              
                //detect whether its a directory or file
                FileAttributes attr = File.GetAttributes(myfile);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    menuSubItemFF.Image =  DefaultAppIcons.FolderLarge.ToBitmap();
                }
                else
                {
                    try
                    {
                        menuSubItemFF.Image =  Icon.ExtractAssociatedIcon(myfile).ToBitmap();
                    }
                    catch
                    {
                        menuSubItemFF.Image =  DefaultAppIcons.FolderLarge.ToBitmap();
                    }
                }
                menuSubItemFF.ToolTipText =  String.Format("{0}{1} [{2}]", myfile, tooltip, itemType);
                ShortcutItem tmp = new ShortcutItem { Command= myfile, Type=eShortCutItemType.ShortCut };
                menuSubItemFF.Tag=tmp;
                menuSubItemFF.Click+=MenuSubItem_Click;
                menuSubItemFF.Key=null;
                menuSubItemFF.ParentKey=key;
                menuSubItemFF.AddToCache();
                listOfMenuItem.Add(menuSubItemFF);
            }
            listofFilesFolders =  listofFiles.ToArray();
            foreach (var myInnerfile in listofFilesFolders)
            {
                string myfile =  myInnerfile;
                menuSubItemFF = new ToolStripMenuItemExt(myfile.Split('\\').Last(), this.MenuCache);
                FileAttributes attr = File.GetAttributes(myfile);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    menuSubItemFF.Image =  DefaultAppIcons.FolderLarge.ToBitmap();
                }
                else
                {
                    try
                    {
                        menuSubItemFF.Image =  Icon.ExtractAssociatedIcon(myfile).ToBitmap();
                    }
                    catch
                    {
                        menuSubItemFF.Image =  DefaultAppIcons.FolderLarge.ToBitmap();
                    }
                }
                menuSubItemFF.ToolTipText =  String.Format("{0}{1} [{2}]", myfile, tooltip, itemType);
                ShortcutItem tmp = new ShortcutItem { Command= myfile, Type=eShortCutItemType.ShortCut };
                menuSubItemFF.Tag=tmp;
                menuSubItemFF.Click+=MenuSubItem_Click;
                menuSubItemFF.Key=null;
                menuSubItemFF.ParentKey=key;
                menuSubItemFF.AddToCache();
                listOfMenuItem.Add(menuSubItemFF);
            }


            return listOfMenuItem;
        }

        public ContextMenuStrip GetContextMenu(ImageList imgList, bool useCache)
        {
            ContextMenuStrip outMnu = null;
            if (!useCache)
            {
                outMnu=BuldContextMenu(imgList);
            }
            else
            {
                // test if cache exists, if not rebuild the menu
                if (File.Exists(settingsMenuCache))
                {
                    outMnu=GetContextCacheMenu(imgList);
                }else
                {
                    outMnu=BuldContextMenu(imgList);
                }
            }

            ToolStripSeparator menuItemSep = null;
            if (outMnu.Items.Count > 0)
            {
                menuItemSep = new ToolStripSeparator();
                outMnu.Items.Add(menuItemSep);
            }
            ToolStripMenuItemExt menuItem = new ToolStripMenuItemExt("Rebuild menu", this.MenuCache);
            menuItem.Tag= imgList;
            menuItem.Click += RebuildMenuItem; ;
            outMnu.Items.Add(menuItem);

            menuItemSep = new ToolStripSeparator();
            outMnu.Items.Add(menuItemSep);

            menuItem = new ToolStripMenuItemExt("Settings", this.MenuCache);
            menuItem.Click += SettingsMenuItem; ;
            outMnu.Items.Add(menuItem);


            menuItem = new ToolStripMenuItemExt("Exit", this.MenuCache);
            menuItem.Click += ExitMenuItem;
            outMnu.Items.Add(menuItem);
            return outMnu;
        }
        public ContextMenuStrip GetContextCacheMenu(ImageList imgList)
        {

            //return BuldContextMenu(imgList);

            ContextMenuStrip menuStrip = new ContextMenuStrip();
            MenuCache = LoadMenuCacheJson();

            foreach (MenuCache mnu in MenuCache)
            {
                ToolStripMenuItemExt menuItem = new ToolStripMenuItemExt(mnu.Text, this.MenuCache);

                if (mnu.Image!= null)
                {
                    byte[] imgByteArray = Convert.FromBase64String(mnu.Image);
                    using (MemoryStream ms = new MemoryStream(imgByteArray))
                    {
                        Image imgTmp = Image.FromStream(ms);
                        Bitmap b = new Bitmap(imgTmp);
                        b.MakeTransparent(Color.Black);
                        var icon = DefaultAppIcons.ConvertToIco(b, 32);
                        menuItem.Image = icon.ToBitmap();
                    }
                }
                menuItem.Name = mnu.Key;
                menuItem.Key = mnu.Key;
                menuItem.ParentKey = mnu.ParentKey;
                menuItem.ToolTipText =  mnu.ToolTipText;
                menuItem.Tag= mnu.Tag;
                menuItem.Click+=MenuSubItem_Click;


                if (mnu.ParentKey == null)
                {
                    menuStrip.Items.Add(menuItem);
                }
                else
                {
                    var mnuItmFound = menuStrip.Items.Find(mnu.ParentKey, true).FirstOrDefault();
                    if (mnuItmFound == null)
                    {
                        menuStrip.Items.Add(menuItem);
                    }
                    else
                    {
                        ToolStripMenuItemExt itm = mnuItmFound as ToolStripMenuItemExt;
                        itm.DropDownItems.Add(menuItem);
                    }
                }




            }

            return menuStrip;
        }
        public ContextMenuStrip BuldContextMenu(ImageList imgList)
        {
            ContextMenuStrip menuStrip = new ContextMenuStrip();
            this.MenuCache= new List<MenuCache>();

            List<ShortcutItem> itemsTmp = KnifeUtils.LoadRootItemsJson();
            IEnumerable<ShortcutItem> rootItems = itemsTmp.Where(itm => (itm.Visible==true && itm.Type ==eShortCutItemType.RootElement));
            IEnumerable<ShortcutItem> childItems = itemsTmp.Where(itm => (itm.Visible==true && itm.Type !=eShortCutItemType.RootElement));
            ToolStripMenuItemExt menuItem = null;

            foreach (ShortcutItem item in rootItems)
            {

                menuItem = BuildMenuItem(item, imgList);
                menuStrip.Items.Add(menuItem);
                ShortcutItem[] childRootItems = childItems.Where(itm => (itm.Key.Contains(item.Key +"\\"))).OrderBy(x => x.Key).OrderBy(x => x.Order).ToArray();
                string rootStringOriginal = item.Title +"\\";
                string rootString = rootStringOriginal;
                ToolStripMenuItemExt menuOriginalItem = menuItem;
                foreach (ShortcutItem subItem in childRootItems)
                {
                    string[] elems = subItem.Key.Split('\\');
                    string title = subItem.Title; // elems[elems.Count()-1];
                    string parentKey = null;
                    ToolStripMenuItemExt menuSubItem = BuildMenuItem(subItem, imgList);

                    if (elems.Count()>1)
                    {
                        parentKey = string.Join("\\", elems.Take(elems.Length-1).ToArray());
                    }
                    //find parent
                    bool found = false;
                    foreach (ToolStripMenuItemExt m in menuItem.DropDownItems)
                    {
                        if (m.Key == parentKey)
                        {
                            menuItem= m;
                            found= true;
                            break;
                        }
                    }
                    if (!found && menuItem.Key != parentKey)
                    {
                        menuItem = menuOriginalItem;
                    }

                    menuItem.DropDownItems.Add(menuSubItem);
                    if (subItem.Type == eShortCutItemType.ShortCut && (subItem.ShortCutType != eShortCutType.JustOneFile))
                    {
                        List<ToolStripMenuItemExt> mm = GetFilesFolders(subItem.Key, subItem.Command, subItem.WildCardFolders, subItem.WildCardFiles, subItem.ToolTip, subItem.Type, subItem.ShortCutType);
                        foreach (var op in mm)
                        {
                            menuSubItem.DropDownItems.Add(op);
                        }
                    }
                }
            }

           


            SaveMenuCacheJson();

            return menuStrip;

        }
        public static void ExecuteCommand(ShortcutItem item, bool showWindow, bool waitForExit, bool isWinTerminalInstalled, bool isPSCoreInstalled, bool isNodeInstalled)
        {
            // test which script type is about to run
            if ((item.Command ==null) || (item.Command.Trim().Length ==0)) return;

            string scriptCmd = item.Command;
            string tempFilename = Path.ChangeExtension(Path.GetTempFileName(), ".bat").Replace(".bat", "_knife.bat");

            string cmdAction = "";
            Process process = new System.Diagnostics.Process();
            string exeFile;
            switch (item.CmdLineType)
            {
                case eCmdLineType.NotApplicable:
                    Process.Start(scriptCmd);
                    break;
                case eCmdLineType.Batch:
                    ProcessStartInfo psi = new ProcessStartInfo();
                    using (StreamWriter writer = new StreamWriter(tempFilename))
                    {
                        writer.WriteLine(scriptCmd);
                    }
                    psi.FileName = tempFilename;
                    psi.UseShellExecute = showWindow;
                    psi.CreateNoWindow = showWindow;
                    Process proc = Process.Start(psi);
                    if (waitForExit)
                    {
                        proc.WaitForExit();
                    }
                    break;
                case eCmdLineType.PowerShellCore:
                    if (!isPSCoreInstalled)
                    {
                        MessageBox.Show("You dont have pscore installed\nExiting.", KnifeUtils.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                    tempFilename= tempFilename.Replace(".bat", ".ps1");
                    using (StreamWriter writer = new StreamWriter(tempFilename))
                    {
                        writer.WriteLine(scriptCmd);
                    }
                    cmdAction = String.Format(" -NoProfile -ExecutionPolicy unrestricted -File {0}", tempFilename);
                    exeFile = "pwsh.exe";
                    if (isWinTerminalInstalled)
                    {

                        cmdAction = String.Concat(exeFile, cmdAction.Replace(".exe", ""));
                        exeFile = "wt.exe";
                    }
                    process = new System.Diagnostics.Process();
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = exeFile,
                        Arguments = cmdAction,
                        UseShellExecute = true
                    };
                    process.Start();
                    break;
                case eCmdLineType.PowerShell5:
                    tempFilename= tempFilename.Replace(".bat", ".ps1");
                    using (StreamWriter writer = new StreamWriter(tempFilename))
                    {
                        writer.WriteLine(scriptCmd);
                    }
                    cmdAction = String.Format(" -NoProfile -ExecutionPolicy unrestricted -File {0}", tempFilename);
                    exeFile = "powershell.exe";
                    if (isWinTerminalInstalled)
                    {

                        cmdAction = String.Concat(exeFile, cmdAction.Replace(".exe", ""));
                        exeFile = "wt.exe";
                    }
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = exeFile,
                        Arguments = cmdAction,
                        UseShellExecute = true
                    };
                    process.Start();
                    break;
                case eCmdLineType.Node:
                    if (!isNodeInstalled)
                    {
                        MessageBox.Show("You dont have node installed\n Exiting.", KnifeUtils.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                    tempFilename= tempFilename.Replace(".bat", ".js");
                    using (StreamWriter writer = new StreamWriter(tempFilename))
                    {
                        writer.WriteLine(scriptCmd);
                    }

                    process = new System.Diagnostics.Process();
                    cmdAction =  tempFilename;
                    exeFile = "node.exe";
                    if (isWinTerminalInstalled)
                    {

                        cmdAction = String.Concat(exeFile, " ", cmdAction.Replace(".exe", ""));
                        exeFile = "wt.exe";
                    }
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName =  exeFile,
                        Arguments = cmdAction,
                        UseShellExecute = true
                    };
                    process.Start();
                    break;
            }

        }
        public static bool TestIfAppExists(string appExe)
        {
            bool exists = false;
            try
            {
                // test which script type is about to run
                if ((appExe ==null) || (appExe.Trim().Length ==0)) return false;


                string scriptCmd = appExe;

                Process process = new System.Diagnostics.Process();
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = appExe;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = false;
                Process p = new Process();
                p.StartInfo = psi;
                exists = p.Start();

                try
                {
                    int procId = p.Id;
                    p.Kill();
                }
                catch
                {
                    exists = false;
                }

            }
            catch
            {
                return false;
            }

            return exists;

        }
        private void MenuSubItem_Click(object sender, EventArgs e)
        {
            ShortcutItem myItem = (ShortcutItem)((ToolStripMenuItem)sender).Tag;
            if (myItem == null) { return; }
            if (myItem.Type ==eShortCutItemType.ShortCut &&  myItem.Command!=null && myItem.Command.ToLower().EndsWith(".ps1"))
            {
                myItem.Type = eShortCutItemType.CmdLine;
                myItem.CmdLineType=eCmdLineType.PowerShellCore;
                myItem.Command = ". " + myItem.Command + "`n Read-Host";
            }
            switch (myItem.Type)
            {
                case eShortCutItemType.ShortCut:
                    myItem.CmdLineType=eCmdLineType.NotApplicable;
                    ExecuteCommand(myItem, false, false, WinTerminalIsInstalled,PsCoreIsInstalled,NodeIsInstalled);
                    break;
                case eShortCutItemType.CmdLine:
                    ExecuteCommand(myItem, true, false, WinTerminalIsInstalled, PsCoreIsInstalled, NodeIsInstalled);
                    break;
            }

        }

        private void SettingsMenuItem(object sender, EventArgs e)
        {


         
            _frm.StartPosition = FormStartPosition.CenterScreen;
            _frm.WindowState = FormWindowState.Normal;
            _frm.Visible = true;

        }
        private void RebuildMenuItem(object sender, EventArgs e)
        {
            string msg = "Menu will build based on present configuration. \nProcess can take a bit to execute, but at the end, all settings will be cached for future use.\n\nProceed?";
            DialogResult rest = MessageBox.Show(msg, KnifeUtils.APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (rest== DialogResult.No) return;
            ToolStripMenuItemExt menuItem = sender as ToolStripMenuItemExt;
            _frm.Cursor = Cursors.WaitCursor;
            //bool frmVisible = _frm.Visible;
            //_frm.Visible = false;
            GetContextMenu((ImageList)menuItem.Tag, false);
            //_frm.Visible = false;
            _frm.Cursor = Cursors.Default;
            MessageBox.Show("Menu was rebuilt ! ", KnifeUtils.APP_TITLE,  MessageBoxButtons.OK, MessageBoxIcon.Information);
            //_frm.Visible = frmVisible;
          
        }
        private void ExitMenuItem(object sender, EventArgs e)
        {
            ((MainWindow)_frm).SystemTrayIcon.Visible = false;
            Application.Exit();
            Environment.Exit(0);
        }
        public static FileAttributes GetAttributes(string path, ref string msg)
        {
            FileAttributes attr = FileAttributes.Offline;
            try
            {
                attr = File.GetAttributes(path);
                msg= String.Empty;
            }
            catch (Exception ex)
            {
                msg= ex.Message.Replace(" file ", " ");
            }

            return attr;

        }
        public static T CreateDeepCopy<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(ms);
            }
        }
        public static string GetSystemDefaultBrowser()
        {
            string name = string.Empty;
            RegistryKey regKey = null;

            try
            {
                var regDefault = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.htm\\UserChoice", false);
                var stringDefault = regDefault.GetValue("ProgId");

                regKey = Registry.ClassesRoot.OpenSubKey(stringDefault + "\\shell\\open\\command", false);
                name = regKey.GetValue(null).ToString().ToLower().Replace("" + (char)34, "");

                if (!name.EndsWith("exe"))
                    name = name.Substring(0, name.LastIndexOf(".exe") + 4);

            }
            catch (Exception ex)
            {
                name = string.Format("ERROR: An exception of type: {0} occurred in method: {1} in the following module: {2}", ex.GetType(), ex.TargetSite);
            }
            finally
            {
                if (regKey != null)
                    regKey.Close();
            }

            return name;
        }
        public static void SaveRootItemsJson(List<ShortcutItem> rootItems)
        {
            System.IO.Directory.CreateDirectory(settings);
            string jsonString = JsonConvert.SerializeObject(rootItems, Newtonsoft.Json.Formatting.Indented);

            StreamWriter writer = new StreamWriter(settingsMenuConfiguration);
            writer.Write(jsonString);
            writer.Close();
            writer.Dispose();
        }

        public void SaveMenuCacheJson()
        {
            System.IO.Directory.CreateDirectory(settings);
            if (File.Exists(settingsMenuCache))
            {
                File.Delete(settingsMenuCache);
            }
            string jsonString = JsonConvert.SerializeObject(MenuCache, Newtonsoft.Json.Formatting.Indented);
            StreamWriter writer = new StreamWriter(settingsMenuCache, false);
            writer.Write(jsonString);
            writer.Close();
            writer.Dispose();

        }
        public static List<MenuCache> LoadMenuCacheJson()
        {
            List<MenuCache> menuSys = new List<MenuCache>();
            if (File.Exists(settingsMenuCache))
            {
                var listObj = JsonConvert.DeserializeObject<List<MenuCache>>(File.ReadAllText(settingsMenuCache));
                if (listObj != null)
                {
                    menuSys = listObj as List<MenuCache>;
                }
            }
            return menuSys;
        }
        public static List<ShortcutItem> LoadRootItemsJson()
        {
            return LoadRootItemsJson(settingsMenuConfiguration);
        }

        public static List<ShortcutItem> LoadRootItemsJson(string jsonFile)
        {
            List<ShortcutItem> config = new List<ShortcutItem>();
            if (File.Exists(settingsMenuConfiguration))
            {
                var listObj = JsonConvert.DeserializeObject<List<ShortcutItem>>(File.ReadAllText(jsonFile));
                if (listObj != null)
                {
                    config = listObj.OrderBy(x => x.Key).OrderBy(x => x.Order).ToList() as List<ShortcutItem>;
                    foreach(ShortcutItem itm in config)
                    {
                       
                        itm.IconPath= ResolvePath(Environment.CurrentDirectory, itm.IconPath);

                    }
                }
            }
            return config;
        }

        public static string LoadEmbeddedResource(string resourceName)
        {
            string result = null;
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
        public static void SaveEmbeddedResource(string resourceName, string fileName)
        {

            if (File.Exists(fileName)) return;
            if (!Directory.Exists(fileName))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            var assembly = Assembly.GetExecutingAssembly();
            byte[] buf;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                buf = new byte[stream.Length];
                stream.Read(buf, 0, Convert.ToInt32(stream.Length));
                using (FileStream fs = File.Create(fileName))
                {
                    fs.Write(buf, 0, Convert.ToInt32(stream.Length));
                    fs.Close();
                }
            }

        }
        
        public static List<ShortcutItem> StringToShortcutItem(string elements)
        {
            List<ShortcutItem> result = null;
            var listObj = JsonConvert.DeserializeObject<List<ShortcutItem>>(elements);
            if (listObj != null)
            {
                result = listObj.OrderBy(x => x.Key).OrderBy(x => x.Order).ToList() as List<ShortcutItem>;
            }
            return result;
        }
        public static List<ShortcutItem> LoadSamplePnPScriptSamples()
        {
            SaveEmbeddedResource("Scoutmans.SwissArmyKnife.Resources.icons.parker.png", @".\resources\samples\parker.png");
            SaveEmbeddedResource("Scoutmans.SwissArmyKnife.Resources.core.pnp.ps1", @".\resources\samples\pnp.ps1");
            SaveEmbeddedResource("Scoutmans.SwissArmyKnife.Resources.core.phmsg.txt", @".\resources\samples\phmsg.txt");
            string cmd = Directory.GetCurrentDirectory() + KnifeUtils.pnpCreateCmdItems;
            ShortcutItem tmp = new ShortcutItem { Command = " powershell " + cmd, Type= eShortCutItemType.CmdLine, CmdLineType= eCmdLineType.Batch };
            KnifeUtils.ExecuteCommand(tmp, true, true, false,false,false);
            File.Delete(@".\resources\samples\pnp.ps1");
            File.Delete(@".\resources\samples\phmsg.txt");
            List<ShortcutItem> items = LoadRootItemsJson();
            return items ;
        }
        public static List<ShortcutItem> LoadSampleEggXamples(List<ShortcutItem> listIn)
        {
            List<ShortcutItem> l = new List<ShortcutItem>();
            if (listIn!=null)
            {
                string  eggxamples = LoadEmbeddedResource("Scoutmans.SwissArmyKnife.Resources.samples.eggxamples.json");
                string localPath= Environment.CurrentDirectory;
                localPath= localPath.Replace(@"\",@"\\");
                eggxamples = eggxamples.Replace(".\\", localPath + @"\");
                SaveEmbeddedResource("Scoutmans.SwissArmyKnife.Resources.icons.eggxamples.png", @".\resources\samples\eggxamples.png");
                l =StringToShortcutItem(eggxamples);
                foreach (ShortcutItem item in l)
                {
                      listIn.Add(item);
                }
                string folders0 = @".\resources\samples\shortcuts\FilesAndFolders";
                string folders1 = @".\resources\samples\shortcuts\FilesAndFolders\Folder1";
                string folders2 = @".\resources\samples\shortcuts\FilesAndFolders\Folder2";
                string folders3 = @".\resources\samples\shortcuts\FilesAndFolders\Folder3";
                Directory.CreateDirectory(folders1);
                Directory.CreateDirectory(folders2);
                Directory.CreateDirectory(folders3);
                for (int a = 0; a < 4; a++)
                {
                    string folder = folders0;
                    if(a>0)
                    {
                        folder =  folders0 + "\\Folder" + a;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        using (StreamWriter writer = File.CreateText(folder + "\\File" + i + ".txt"))
                        {
                            writer.WriteLine("dummy content");
                        }
                    }
                }
               


                l= listIn;
            }
            else
            {
                l = KnifeUtils.LoadRootItemsJson(KnifeUtils.eggxamplesCmdItems);
            }

            return l;
        }

        public static List<ShortcutItem> LoadSampleBrowserProfiles(List<ShortcutItem> listIn)
        {
            SaveEmbeddedResource("Scoutmans.SwissArmyKnife.Resources.icons.edge.png", @".\resources\samples\edge.png");
            List<ShortcutItem> l = new List<ShortcutItem>();
            if (listIn!=null)
            {
                l = GetBrowserProfiles(); ;
                foreach (ShortcutItem item in l)
                {
                    listIn.Add(item);
                }
                l= listIn;
            }
            else
            {
                l = GetBrowserProfiles(); ;
            }

            return l;
        }
        private static List<ShortcutItem> GetBrowserProfiles()
        {
            var edgeProfiles = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Microsoft\\Edge\\User Data";
            var profiles = Directory.GetDirectories(edgeProfiles, "Profile*");
            var browserPath = KnifeUtils.GetSystemDefaultBrowser();

            List<ShortcutItem> profileItems = new List<ShortcutItem>();
            int ct = 3000;
            string Key = "PRF";

            foreach (var profile in profiles)
            {
                JObject obj = null;
                using (StreamReader file = File.OpenText(profile + "\\Preferences"))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    obj = (JObject)JToken.ReadFrom(reader);

                }
                string title = (string)obj["profile"]["name"];
                string profileName = profile.Split(Path.DirectorySeparatorChar).Last();
                string profileCmdLine = ("\"" + browserPath + "\" --profile-directory=\"" + profileName + "\"").ToString();
                string msg = "This profile exists in the [" + profileName + "] profile folder. ";
                if (ct==0)
                {
                    msg= "If the profile name have a '\\' character, first element is a parent menu,second element is the actual shortcut";
                }
                profileItems.Add(new ShortcutItem
                {
                    Type=eShortCutItemType.CmdLine,
                    Key =  title.ToUpper(),
                    Title = title,
                    CmdLineType= eCmdLineType.Batch,
                    ShortCutType=eShortCutType.NotApplicable,
                    IconPath = (profile + "\\Edge Profile.ico"),
                    ToolTip = msg,
                    Visible = true,
                    Command=  profileCmdLine,
                    Order=899

                });
                ct++;
            }
            profileItems.Add(new ShortcutItem
            {
                Type=eShortCutItemType.RootElement,
                Key = (Key),
                Title = "Browser Profiles",
                CmdLineType= eCmdLineType.Batch,
                ShortCutType=eShortCutType.FilesAndFolders,
                IconPath = Environment.CurrentDirectory + "\\Resources\\samples\\edge.png",
                ToolTip = "Default Browser Profiles",
                Visible = true,
                Command=  null,
                Order=3000
            });
            profileItems=profileItems.OrderBy(x => x.Key).ToList();
            ct=3000;

            foreach (var p in profileItems)
            {
                if (p.Type!=eShortCutItemType.RootElement)
                {

                    // Fix title
                    if (p.Title.IndexOf("\\")>-1)
                    {

                        string[] tb = p.Title.Split('\\');
                        p.Title = tb[tb.Length-1];

                    }
                    p.Key = Key + "\\" + p.Key;

                }
                p.Order=ct;
                ct+=10;
            }
            profileItems=profileItems.OrderBy(x => x.Key).ToList();

            ct=3000;
            // add parent levels in case they dont exist
            List<ShortcutItem> profileItemsTmp = KnifeUtils.CreateDeepCopy(profileItems);

            foreach (var p in profileItems)
            {
                // Fix title
                if (p.Key.IndexOf("\\")>-1)
                {

                    string[] tb = p.Key.Split('\\');
                    string kCrumb = "";
                    foreach (var k in tb)
                    {
                        if (kCrumb == "")
                        {
                            kCrumb+=k;
                        }
                        else
                        {
                            kCrumb+="\\"+k;
                        }

                        ShortcutItem[] found = profileItemsTmp.Where(itm => (itm.Key ==kCrumb)).ToArray();

                        if (found.Length ==0)
                        {
                            // create new item
                            profileItemsTmp.Add(new ShortcutItem
                            {
                                Type=eShortCutItemType.ShortCut,
                                Key = kCrumb.ToUpper(),
                                Title = k,
                                CmdLineType= eCmdLineType.NotApplicable,
                                ShortCutType=eShortCutType.NotApplicable,
                                Visible = true,
                                Order=p.Order-1
                            });
                            break;
                        }
                    }

                }
                p.Order=ct;

                ct+=10;
            }
            ct=3000;
            profileItems= profileItemsTmp.OrderBy(x => x.Key).ToList();
            foreach (var p in profileItems)
            {
                p.Order=ct;
                ct+=10;
            }
            profileItems= profileItemsTmp.OrderBy(x => x.Order).ToList();
            return profileItems;
        }
    }
}
