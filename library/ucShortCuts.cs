using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Scoutmans.SwissArmyKnife
{
    public enum eOperations
    {
        New,
        Edit,
        Delete,
        Save,
        Cancel,
        NewItem,
        EditItem,
        DeleteItem,
        SaveItem,
        CancelItem
    }
    public enum eScope
    {
        Root,
        Items
    }

    public partial class ucShortCuts : UserControl
    {
        private List<ShortcutItem> _tmpShortcutItems { get; set; }

        public int ActiveRootItem { get; set; }
        private List<ShortcutItem> ShortcutItems { get; set; }
        public ShortcutItem SelectItem { get; set; }

        public eOperations PreviousOperation { get; set; }
        public eOperations Operation { get; set; }
        public eScope Scope { get; set; }
        public string Title
        {
            get { return lblTitle.Text; }
            set { lblTitle.Text = value; }
        }
        public ToolStrip strip { get; set; }
        public ucShortCuts()
        {
            InitializeComponent();
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (key.GetValue(KnifeUtils.APP_TITLE) != null)
                {
                    this.launchToolStripMenuItem.Checked = (key.GetValue(KnifeUtils.APP_TITLE).ToString().Length>0);
                }
                else
                {
                    this.launchToolStripMenuItem.Checked = false;
                }

            }
            catch
            {

            }
            mnuInitialState();
        }

        private void mnuInitialState()
        {

            lblDgvRootStatus.Text ="";

            if (ShortcutItems ==null)
            {
                ShortcutItems = new List<ShortcutItem>();
            }
            pnlEditMain.Visible = true;


            dgvRoot.DataSource= null;
            shortcutItemBindingSource.DataSource = ShortcutItems.ToArray();
            dgvRoot.DataSource= shortcutItemBindingSource;
            //dgvRoot.Refresh();

            newRootToolStripMenuItem.Visible = false;
            editRootToolStripMenuItem.Visible = false;
            deleteRootToolStripMenuItem.Visible = false;
            saveRootToolStripMenuItem.Visible = false;
            cancelRootToolStripMenuItem.Visible = false;

            pnlEditElem.Visible= true;
            pnlEditElem.Enabled=  saveItemToolStripMenuItem.Visible;
            newItemToolStripMenuItem.Visible = true;
            editItemToolStripMenuItem.Visible = (ShortcutItems.ToArray().Length >0);
            deleteItemToolStripMenuItem.Visible = (dgvRoot.SelectedRows.Count>0);
            saveItemToolStripMenuItem.Visible = false;
            cancelItemToolStripMenuItem.Visible = false;

            dgvRoot.ReadOnly = true;
            dgvRoot.Enabled = true;

            foreach (DataGridViewColumn c in dgvRoot.Columns)
            {
                Debug.WriteLine(c.Name);

                if ("Command,IconPath,WildCardFolders,WildCardFiles".Contains(c.Name))
                {
                    c.Visible = false;
                }
            }
            pnlView.Visible = !pnlEditElem.Enabled;

        }

        private void NewItemOperation()
        {
            newItemToolStripMenuItem.Visible = false;
            editItemToolStripMenuItem.Visible = false;
            deleteItemToolStripMenuItem.Visible = false;
            saveItemToolStripMenuItem.Visible = true;
            cancelItemToolStripMenuItem.Visible = true;

            txtItemTitle.ReadOnly = false;
            txtItemToolTip.ReadOnly =  txtItemTitle.ReadOnly;
            chkItemVisible.Enabled =!txtItemTitle.ReadOnly;
            txtItemOrder.Enabled = !txtItemTitle.ReadOnly;
            ShortcutItem item = new ShortcutItem
            {
                Title  = ("New Item" + (ShortcutItems.ToArray().Length +1)),
                ToolTip = "",
                Type=  eShortCutItemType.ShortCut,
                Visible= true,
                Order = ShortcutItems.ToArray().Length +1,
                CmdLineType= eCmdLineType.NotApplicable,
                ShortCutType= eShortCutType.NotApplicable,
                WildCardFiles="*.*",
                WildCardFolders="*",
                Command="",
                Key="",
                IconPath=""


            };
            var itemType = (eShortCutItemType)cbType.SelectedItem;

            item.ShortCutType=eShortCutType.NotApplicable;
            item.CmdLineType=eCmdLineType.NotApplicable;
            if (itemType ==eShortCutItemType.ShortCut)
            {
                item.ShortCutType=eShortCutType.FilesAndFolders;

            }
            else
            {
                item.CmdLineType= eCmdLineType.Batch;
            }

            ShortcutItems.Add(item);
            dgvRoot.DataSource =  null; ;
            shortcutItemBindingSource.DataSource = ShortcutItems.ToArray();
            shortcutItemBindingSource.Position = ShortcutItems.ToArray().Length;
            dgvRoot.DataSource =  shortcutItemBindingSource;
            dgvRoot.Enabled = false;
            pnlEditElem.Enabled = true;
            pnlView.Visible = !pnlEditElem.Enabled;
        }




        private void SaveItemOperation()
        {
            txtItemOrder.Focus();

            if (txtItemTitle.Text.Trim().Length==0)
            {

                lblDgvRootStatus.Text=" Title needs to be filled";
            }
            else
            {
                ShortcutItems = shortcutItemBindingSource.List.OfType<ShortcutItem>().OrderBy(x => x.Order).ToList<ShortcutItem>();
                //RootItems.AItem].Items = ShortcutItems;
                KnifeUtils.SaveRootItemsJson(ShortcutItems);

                mnuInitialState();
                pnlEditElem.Enabled = false;
            }
            pnlView.Visible = !pnlEditElem.Enabled;
        }
        private void CancelItemOperation()
        {
            if (ShortcutItems.Count >0)
            {
                ShortcutItems.Remove(ShortcutItems.Last());
            }
            mnuInitialState();
            pnlEditElem.Enabled = false;
            pnlView.Visible = !pnlEditElem.Enabled;
        }

        private void EditItemOperation()
        {
            newItemToolStripMenuItem.Visible = false;
            editItemToolStripMenuItem.Visible = false;
            deleteItemToolStripMenuItem.Visible = false;
            saveItemToolStripMenuItem.Visible = true;
            cancelItemToolStripMenuItem.Visible = true;

            pnlEditElem.Enabled = true;

            dgvRoot.Enabled = true;
            dgvRoot.ReadOnly = false;
            pnlView.Visible = !pnlEditElem.Enabled;

        }

        private void ucShortCuts_Load(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                ShortcutItems = KnifeUtils.LoadRootItemsJson();
                KnifeUtils.SaveRootItemsJson(ShortcutItems);
            }

            Scope= eScope.Root;
            var tmpTypes = Enum.GetValues(typeof(eShortCutItemType)).Cast<eShortCutItemType>().ToList();
            cbType.DataSource = tmpTypes;
            var tmpFilesFolder = Enum.GetValues(typeof(eShortCutType)).Cast<eShortCutType>().ToList();
            cbFilesFolders.DataSource = tmpFilesFolder;
            var tmpFilesCmdLineType = Enum.GetValues(typeof(eCmdLineType)).Cast<eCmdLineType>().ToList();
            cbCmdType.DataSource = tmpFilesCmdLineType;
            //pnlView.Location = pnlEditMain.Location;
            pnlView.Left=-1000;
            pnlView.Top= 335;

            mnuInitialState();
        }

        private void newRootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Operation= eOperations.New;
            this.PreviousOperation = eOperations.New;


        }
        private void newItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Operation= eOperations.NewItem;
            this.PreviousOperation = eOperations.NewItem;
            NewItemOperation();
        }

        private void saveRootToolStripMenuItem_Click(object sender, EventArgs e)
        {

            this.Operation= eOperations.Save;
        }
        private void saveItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Operation= eOperations.SaveItem;
            SaveItemOperation();
        }
        private void cancelItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (PreviousOperation ==eOperations.NewItem)
            {
                this.Operation= eOperations.Cancel;
                CancelItemOperation();
            }
            if (PreviousOperation ==eOperations.EditItem)
            {
                ShortcutItems= _tmpShortcutItems;
                mnuInitialState();
                pnlEditElem.Enabled= false;
            }
            pnlView.Visible = !pnlEditElem.Enabled;
        }
        private void dgvRoot_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string columnName = ((DataGridView)sender).Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "Title":
                    {
                        if (Convert.ToString(e.FormattedValue).Length ==0)
                        {
                            e.Cancel = true;
                            lblDgvRootStatus.Text ="Sorry ... we need a Title! Type it!";
                        }
                        else
                        {
                            lblDgvRootStatus.Text ="";
                        }
                        break;
                    }

                case "Order":
                    {
                        int i;
                        if (!int.TryParse(Convert.ToString(e.FormattedValue), out i))
                        {
                            e.Cancel = true;
                            lblDgvRootStatus.Text ="Yeah, right...! Type a numeric value!";
                        }
                        else
                        {
                            lblDgvRootStatus.Text ="";
                        }
                        break;
                    }
                case "Type":
                    {
                        eShortCutItemType t;
                        if (!eShortCutItemType.TryParse(Convert.ToString(e.FormattedValue), out t))
                        {
                            e.Cancel = true;
                            var validValues = String.Join(",", Enum.GetNames(typeof(eShortCutItemType)));
                            lblDgvRootStatus.Text ="Trying to hack it , hum...? Valid values here are : " + validValues;
                        }
                        else
                        {
                            lblDgvRootStatus.Text ="";
                        }
                        break;
                    }
                case "ShortCutType":
                    {
                        eShortCutType t;
                        if (!eShortCutType.TryParse(Convert.ToString(e.FormattedValue), out t))
                        {
                            e.Cancel = true;
                            var validValues = String.Join(",", Enum.GetNames(typeof(eShortCutType)));
                            lblDgvRootStatus.Text ="Dont' think that work ... Valid values here are : " + validValues;
                        }
                        else
                        {
                            lblDgvRootStatus.Text ="";
                        }
                        break;
                    }

            }
        }






        private void editItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Operation= eOperations.EditItem;
            this.PreviousOperation = eOperations.EditItem;
            _tmpShortcutItems= KnifeUtils.CreateDeepCopy(ShortcutItems);
            EditItemOperation();
        }

        private void dgvRoot_SelectionChanged(object sender, EventArgs e)
        {
            if (this.dgvRoot.Rows.Count > 0)
            {
                //Continue your code here
                switch (Scope)
                {
                    case eScope.Items:
                        deleteItemToolStripMenuItem.Visible = ((dgvRoot.SelectedRows.Count>0) && (!saveItemToolStripMenuItem.Visible)&& (!cancelItemToolStripMenuItem.Visible));
                        break;
                    default:
                        break;
                }
            }



        }


        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvRoot.SelectedRows)
            {
                ShortcutItems.Remove((ShortcutItem)row.DataBoundItem);
            }
            //RootItems[ActiveRootItem].Items = ShortcutItems;
            KnifeUtils.SaveRootItemsJson(ShortcutItems);
            mnuInitialState();
        }

        private void btBrowseIconImage_Click(object sender, EventArgs e)
        {
            // open file dialog   
            OpenFileDialog open = new OpenFileDialog();
            // image filters  
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {

                txtItemIconPath.Text = open.FileName;
                txtItemTitle.Focus();
                txtItemIconPath.Focus();
            }
        }

        private void txtItemIconPath_TextChanged(object sender, EventArgs e)
        {
            pic.Image= null;
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            if (txtItemIconPath.Text.Length>0 && File.Exists(txtItemIconPath.Text))
            {
                string fullName = Path.GetFullPath(txtItemIconPath.Text);
                try
                {
                    pic.Load(fullName);
                }
                catch
                {
                }
                txtItemIconPath.Text= fullName;
            }

        }

        private void manageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Scope == eScope.Items)
            {
                saveItemToolStripMenuItem_Click(null, null);
                Scope= eScope.Root;
                mnuInitialState();
            }
            else
            {
                saveRootToolStripMenuItem_Click(null, null);
                Scope= eScope.Items;
                pnlEditElem.Visible = false;

                ActiveRootItem = dgvRoot.SelectedRows[0].Index;
                //ShortcutItems = RootItems[ActiveRootItem].Items;
                mnuInitialState();
            }
        }

        private void hiddenGemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem mnu = ((ToolStripMenuItem)sender);
            switch (mnu.Name)
            {
                case "eggxamplesToolStripMenuItem":
                    this.ShortcutItems = KnifeUtils.LoadSampleEggXamples(this.ShortcutItems);
                    MessageBox.Show("Eggxamples imported ! \n\nRefreshing configuration ...", KnifeUtils.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    break;
                case "loadBrowserProfilesToolStripMenuItem":
                    this.ShortcutItems=(KnifeUtils.LoadSampleBrowserProfiles(this.ShortcutItems));
                    MessageBox.Show("Profiles imported ! \n\nRefreshing configuration ...", KnifeUtils.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "loadPnPScriptSamplesToolStripMenuItem":
                    this.ShortcutItems =KnifeUtils.LoadSamplePnPScriptSamples();
                    MessageBox.Show("PnP Script Samples imported ! \n\nRefreshing configuration ...", KnifeUtils.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "editCurrentJsonConfigurationToolStripMenuItem":
                    ShortcutItem tmp = new ShortcutItem { Command = "start " + KnifeUtils.settingsMenuConfiguration, Type= eShortCutItemType.CmdLine };
                    KnifeUtils.ExecuteCommand(tmp, false, false, false, false, false);
                    break;
                case "aboutToolStripMenuItem":
                    frmAbout f = new frmAbout();
                    f.ShowDialog();
                    break;
                case "launchToolStripMenuItem":
                    KnifeUtils.StartupApp= mnu.Checked;
                    break;
            }
            if (mnu.Name != "loadPnPScriptSamplesToolStripMenuItem")
            {
                KnifeUtils.SaveRootItemsJson(this.ShortcutItems);
            }


            mnuInitialState();
        }

        private void dgvRoot_DoubleClick(object sender, EventArgs e)
        {
            if (editRootToolStripMenuItem.Visible)
            {
                manageToolStripMenuItem_Click(sender, e);
                hiddenGemsToolStripMenuItem.Visible= false;
                return;
            }
            if (editItemToolStripMenuItem.Visible)
            {
                editItemToolStripMenuItem_Click(sender, e);
            }
        }

        private void cbType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            mnuInitialState();
        }




        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            int pos = 0;
            if (shortcutItemBindingSource.Position> -1)
            {
                pos= shortcutItemBindingSource.Position;
            }
            ShortcutItem currentItem = null;

            if (shortcutItemBindingSource.List.Count > 0)
            {
                currentItem = (ShortcutItem)shortcutItemBindingSource.List[pos];
            }
            var itemType = eShortCutItemType.ShortCut;

            if (cbType.SelectedItem!=null)
            {
                itemType=(eShortCutItemType)cbType.SelectedItem;
            }

                ;
            cbFilesFolders.Visible=(itemType==eShortCutItemType.ShortCut);
            cbCmdType.Visible=(itemType==eShortCutItemType.CmdLine);
            cbType.Enabled =  pnlEditElem.Visible;

            switch (itemType)
            {
                case eShortCutItemType.RootElement:
                    cbCmdType.SelectedItem = eCmdLineType.NotApplicable;
                    cbFilesFolders.SelectedItem = eShortCutType.NotApplicable;

                    txtItemTitle.Focus();
                    break;
                case eShortCutItemType.CmdLine:
                    lblCmd.Text ="Command";
                    cbFilesFolders.SelectedItem = eShortCutType.NotApplicable;
                    txtItemTitle.Focus();
                    break;
                case eShortCutItemType.ShortCut:
                    lblCmd.Text ="File\\Folders";
                    cbCmdType.SelectedItem = eCmdLineType.NotApplicable;
                    txtItemTitle.Focus();
                    break;
            }


            cbCmdType.Location = cbFilesFolders.Location;
            lblCmd.Visible = (itemType != eShortCutItemType.RootElement);
            txtItemCmd.Visible=lblCmd.Visible;
            lblWFiles.Visible =(itemType ==eShortCutItemType.ShortCut);
            txtWFiles.Visible =lblWFiles.Visible;
            lblWFolders.Visible =lblWFiles.Visible;
            txtWFolders.Visible =lblWFiles.Visible;

            hiddenGemsToolStripMenuItem.Visible= true;

            //dgvRoot.Refresh();

        }

        private void dgvRoot_Scroll(object sender, ScrollEventArgs e)
        {
            try
            {
                dgvRoot.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


    }
}
